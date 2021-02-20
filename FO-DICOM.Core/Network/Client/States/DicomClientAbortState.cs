// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Network.Client.Events;
using FellowOakDicom.Network.Client.Tasks;

namespace FellowOakDicom.Network.Client.States
{

    public class DicomClientAbortState : DicomClientWithConnectionState
    {
        private readonly DicomClient _dicomClient;
        private readonly InitialisationParameters _initialisationParameters;
        private readonly TaskCompletionSource<DicomAbortedEvent> _onAbortReceivedTaskCompletionSource;
        private readonly TaskCompletionSource<ConnectionClosedEvent> _onConnectionClosedTaskCompletionSource;
        private readonly CancellationTokenSource _associationAbortTimeoutCancellationTokenSource;

        public class InitialisationParameters : IInitialisationWithConnectionParameters
        {
            public IDicomClientConnection Connection { get; set; }

            public InitialisationParameters(IDicomClientConnection connection)
            {
                Connection = connection;
            }
        }

        public DicomClientAbortState(DicomClient dicomClient, InitialisationParameters initialisationParameters) : base(initialisationParameters)
        {
            _dicomClient = dicomClient ?? throw new ArgumentNullException(nameof(dicomClient));
            _initialisationParameters = initialisationParameters ?? throw new ArgumentNullException(nameof(initialisationParameters));
            _onAbortReceivedTaskCompletionSource = TaskCompletionSourceFactory.Create<DicomAbortedEvent>();
            _onConnectionClosedTaskCompletionSource = TaskCompletionSourceFactory.Create<ConnectionClosedEvent>();
            _associationAbortTimeoutCancellationTokenSource = new CancellationTokenSource();
        }

        // Ignore, we're aborting now
        public override Task SendAsync(DicomClientCancellation cancellation) => Task.CompletedTask;

        public override Task OnReceiveAssociationAcceptAsync(DicomAssociation association) => Task.CompletedTask;

        public override Task OnReceiveAssociationRejectAsync(DicomRejectResult result, DicomRejectSource source, DicomRejectReason reason) => Task.CompletedTask;

        public override Task OnReceiveAssociationReleaseResponseAsync() => Task.CompletedTask;

        public override Task OnReceiveAbortAsync(DicomAbortSource source, DicomAbortReason reason)
        {
            _onAbortReceivedTaskCompletionSource.TrySetResult(new DicomAbortedEvent(source, reason));

            return Task.CompletedTask;
        }

        public override Task OnConnectionClosedAsync(Exception exception)
        {
            _onConnectionClosedTaskCompletionSource.TrySetResult(new ConnectionClosedEvent(exception));

            return Task.CompletedTask;
        }

        public override Task OnSendQueueEmptyAsync() => Task.CompletedTask;

        public override Task OnRequestCompletedAsync(DicomRequest request, DicomResponse response) => Task.CompletedTask;

        public override Task OnRequestTimedOutAsync(DicomRequest request, TimeSpan timeout) => Task.CompletedTask;

        public override async Task<IDicomClientState> GetNextStateAsync(DicomClientCancellation cancellation)
        {
            var sendAbort = _initialisationParameters.Connection.SendAbortAsync(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
            var onReceiveAbort = _onAbortReceivedTaskCompletionSource.Task;
            var onDisconnect = _onConnectionClosedTaskCompletionSource.Task;
            var onTimeout = Task.Delay(100, _associationAbortTimeoutCancellationTokenSource.Token);

            var winner = await Task.WhenAny(sendAbort, onReceiveAbort, onDisconnect, onTimeout).ConfigureAwait(false);

            if (winner == sendAbort)
            {
                _dicomClient.Logger.Debug($"[{this}] Abort notification sent to server. Disconnecting...");
                return await _dicomClient.TransitionToCompletedState(_initialisationParameters, cancellation).ConfigureAwait(false);
            }
            if (winner == onReceiveAbort)
            {
                _dicomClient.Logger.Debug($"[{this}] Received abort while aborting. Neat.");
                return await _dicomClient.TransitionToCompletedState(_initialisationParameters, cancellation).ConfigureAwait(false);
            }

            if (winner == onDisconnect)
            {
                _dicomClient.Logger.Debug($"[{this}] Disconnected while aborting. Perfect.");
                var connectionClosedEvent = await onDisconnect.ConfigureAwait(false);
                if (connectionClosedEvent.Exception == null)
                {
                    return await _dicomClient.TransitionToCompletedState(_initialisationParameters, cancellation).ConfigureAwait(false);
                }
                else
                {
                    return await _dicomClient.TransitionToCompletedWithErrorState(_initialisationParameters, connectionClosedEvent.Exception, cancellation).ConfigureAwait(false);
                }
            }

            if (winner == onTimeout)
            {
                _dicomClient.Logger.Debug($"[{this}] Abort notification timed out. Disconnecting...");
                return await _dicomClient.TransitionToCompletedState(_initialisationParameters, cancellation).ConfigureAwait(false);
            }

            throw new DicomNetworkException("Unknown winner of Task.WhenAny in DICOM client, this is likely a bug: " + winner);
        }

        public override Task AddRequestAsync(DicomRequest dicomRequest)
        {
            _dicomClient.QueuedRequests.Enqueue(new StrongBox<DicomRequest>(dicomRequest));
            return Task.CompletedTask;
        }

        public override string ToString() => $"ABORTING ASSOCIATION";

        public override void Dispose()
        {
            _associationAbortTimeoutCancellationTokenSource.Cancel();
            _associationAbortTimeoutCancellationTokenSource.Dispose();
            _onAbortReceivedTaskCompletionSource.TrySetCanceled();
            _onConnectionClosedTaskCompletionSource.TrySetCanceled();
        }
    }
}
