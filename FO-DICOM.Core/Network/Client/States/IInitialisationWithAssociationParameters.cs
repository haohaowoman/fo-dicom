// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace FellowOakDicom.Network.Client.States
{

    public interface IInitialisationWithAssociationParameters : IInitialisationWithConnectionParameters
    {
        DicomAssociation Association { get; set; }
    }
}
