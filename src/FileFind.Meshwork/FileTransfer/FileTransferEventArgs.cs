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
    public class FileTransferEventArgs : EventArgs
    {
        public IFileTransfer Transfer { get; }

        public FileTransferEventArgs(IFileTransfer transfer)
            : base()
        {
            Transfer = transfer;
        }
    }
}
