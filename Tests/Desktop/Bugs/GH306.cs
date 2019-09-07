﻿// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System.Threading.Tasks;
using Dicom.Helpers;
using Dicom.Network.Client;
using Xunit.Abstractions;

namespace Dicom.Bugs
{
    using System;
    using System.Text;
    using Dicom.Log;
    using Dicom.Network;
    using Xunit;

    public class GH306
    {
        private readonly XUnitDicomLogger _logger;

        public GH306(ITestOutputHelper testOutputHelper)
        {
            _logger = new XUnitDicomLogger(testOutputHelper).IncludeTimestamps().IncludeThreadId();
        }

        #region Unit tests

        [Fact]
        public void OldDicomClientSend_StoreNonPart10File_ShouldSucceed()
        {
            var port = Ports.GetNext();

            using (var server = DicomServer.Create<CStoreScp>(port))
            {
                server.Logger = _logger.IncludePrefix("CStoreScp");

                var file = DicomFile.Open(@".\Test Data\CR-MONO1-10-chest");

                var client = new DicomClient
                {
                    Logger = _logger.IncludePrefix("DicomClient")
                };
                client.AddRequest(new DicomCStoreRequest(file));

                var exception = Record.Exception(() => client.Send("127.0.0.1", port, false, "SCU", "SCP"));
                Assert.Null(exception);
            }
        }

        [Fact]
        public async Task DicomClientSend_StoreNonPart10File_ShouldSucceed()
        {
            var port = Ports.GetNext();

            using (var server = DicomServer.Create<CStoreScp>(port))
            {
                server.Logger = _logger.IncludePrefix("CStoreScp");

                var file = DicomFile.Open(@".\Test Data\CR-MONO1-10-chest");

                var client = new Dicom.Network.Client.DicomClient("127.0.0.1", port, false, "SCU", "SCP")
                {
                    Logger = _logger.IncludePrefix("DicomClient")
                };
                await client.AddRequestAsync(new DicomCStoreRequest(file)).ConfigureAwait(false);

                var exception = await Record.ExceptionAsync(async () => await client.SendAsync().ConfigureAwait(false));
                Assert.Null(exception);
            }
        }

        [Fact]
        public void OldDicomClientSend_StorePart10File_ShouldSucceed()
        {
            var port = Ports.GetNext();

            using (var server = DicomServer.Create<CStoreScp>(port))
            {
                server.Logger = _logger.IncludePrefix("CStoreScp");

                var file = DicomFile.Open(@".\Test Data\CT-MONO2-16-ankle");

                var client = new DicomClient
                {
                    Logger = _logger.IncludePrefix("DicomClient")
                };
                client.AddRequest(new DicomCStoreRequest(file));

                var exception = Record.Exception(() => client.Send("127.0.0.1", port, false, "SCU", "SCP"));
                Assert.Null(exception);
            }
        }

        [Fact]
        public async Task DicomClientSend_StorePart10File_ShouldSucceed()
        {
            var port = Ports.GetNext();

            using (var server = DicomServer.Create<CStoreScp>(port))
            {
                server.Logger = _logger.IncludePrefix("CStoreScp");

                var file = DicomFile.Open(@".\Test Data\CT-MONO2-16-ankle");

                var client = new Dicom.Network.Client.DicomClient("127.0.0.1", port, false, "SCU", "SCP")
                {
                    Logger = _logger.IncludePrefix("DicomClient")
                };
                await client.AddRequestAsync(new DicomCStoreRequest(file)).ConfigureAwait(false);

                var exception = await Record.ExceptionAsync(async () => await client.SendAsync());
                Assert.Null(exception);
            }
        }

        #endregion

        #region Support types

        private class CStoreScp : DicomService, IDicomCStoreProvider, IDicomServiceProvider
        {
            private DicomTransferSyntax[] AcceptedImageTransferSyntaxes =
            {
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            };

            public CStoreScp(INetworkStream stream, Encoding fallbackEncoding, Logger log)
                : base(stream, fallbackEncoding, log)
            {
            }

            public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
            {
                return new DicomCStoreResponse(request, DicomStatus.Success);
            }

            public void OnCStoreRequestException(string tempFileName, Exception e)
            {
            }

            public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
            {
                foreach (var pc in association.PresentationContexts)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }

                return SendAssociationAcceptAsync(association);
            }

            public Task OnReceiveAssociationReleaseRequestAsync()
            {
                return SendAssociationReleaseResponseAsync();
            }

            public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
            {
            }

            public void OnConnectionClosed(Exception exception)
            {
            }
        }

        #endregion
    }
}
