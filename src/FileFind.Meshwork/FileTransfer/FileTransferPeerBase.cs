using System;
using FileFind.Meshwork;

namespace FileFind.Meshwork.FileTransfer
{
	public abstract class FileTransferPeerBase : IFileTransferPeer
	{
        public Network Network { get; }
        public Node Node { get; }

        public FileTransferPeerBase(Network network, Node node)
        {
            Network = network;
            Node = node;
        }

        public abstract ulong UploadSpeed { get; }
        public abstract ulong DownloadSpeed { get; }
        public abstract FileTransferPeerStatus Status { get; }
        public abstract string StatusDetail { get; }
        public abstract double Progress { get; }
	}
}
