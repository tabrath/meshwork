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
    public class FileTransferPeerEventArgs : EventArgs
    {
        public IFileTransferPeer Peer { get; }

        public FileTransferPeerEventArgs(IFileTransferPeer peer)
            : base()
        {
            Peer = peer;
        }
    }
}
