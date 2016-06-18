//
// IFileTransfer.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.FileTransfer
{
    public class FileTransferOperation : IMeshworkOperation
	{
        public IFileTransfer Transfer { get; }
        public IFileTransferPeer Peer { get; }
        public ITransport Transport { get; }

		internal FileTransferOperation(ITransport transport, IFileTransfer transfer, IFileTransferPeer peer)
		{
			Transport = transport;
			Transfer = transfer;
			Peer = peer;
		}
	}
}
