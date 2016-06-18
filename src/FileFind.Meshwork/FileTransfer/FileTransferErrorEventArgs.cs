//
// FileTransferManager.cs: Keeps track of ongoing file transfers
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.FileTransfer
{
    public class FileTransferErrorEventArgs : FileTransferEventArgs
    {
        public Exception Exception { get; }

        public FileTransferErrorEventArgs(IFileTransfer transfer, Exception exception)
            : base(transfer)
        {
            Exception = exception;
        }
    }	
}
