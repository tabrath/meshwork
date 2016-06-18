//
// FileTransferManager.cs: Keeps track of ongoing file transfers
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Transport;
using IO = System.IO;

namespace FileFind.Meshwork.FileTransfer
{
    public interface IFileTransferManager
    {
        event EventHandler<FileTransferEventArgs> NewFileTransfer;
        event EventHandler<FileTransferEventArgs> FileTransferRemoved;

        IFileTransferProvider Provider { get; }
        IEnumerable<IFileTransfer> Transfers { get; }

        IFileTransfer StartTransfer(Network network, Node node, IFile file);
        void RemoveTransfer(IFileTransfer transfer);
        void HandleIncomingTransport(ITransport transport);
    }
    
}
