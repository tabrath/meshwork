//
// FileTransferBase.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Errors;

namespace FileFind.Meshwork.FileTransfer
{
	public abstract class FileTransferBase : IFileTransfer, IFileTransferInternal
	{
        protected readonly List<IFileTransferPeer> peers;

		public event EventHandler<FileTransferPeerEventArgs> PeerAdded;
		public event EventHandler<FileTransferPeerEventArgs> PeerRemoved;
		public event EventHandler<ErrorEventArgs> Error;

        public string StatusDetail { get; protected set; }
        public string Id { get; protected set; }
        public IFile File { get; }
        public IFileTransferPeer[] Peers { get { return peers.ToArray(); } }

        public abstract FileTransferDirection Direction { get; }
        public abstract FileTransferStatus Status { get; }
        public abstract double Progress { get; }
        public abstract ulong TotalDownloadSpeed { get; }
        public abstract ulong TotalUploadSpeed { get; }
        public abstract ulong BytesDownloaded { get; }
        public abstract ulong BytesUploaded { get; }
        public abstract int UploadSpeedLimit { get; set; }
        public abstract int DownloadSpeedLimit { get; set; }
		
		protected FileTransferBase(IFile file)
		{
			Id = Guid.NewGuid().ToString();
            File = file;

            this.peers = new List<IFileTransferPeer>();
		}

		public abstract void Start();
		public abstract void Cancel();
		public abstract void Pause();
		public abstract void Resume();
		public abstract void AddPeer(Network network, Node node);
		public abstract void DetailsReceived();
		public abstract void ErrorReceived (Node node, FileTransferError error);

        protected virtual void RaisePeerAdded(IFileTransferPeer peer)
        {
            PeerAdded?.Invoke(this, new FileTransferPeerEventArgs(peer));
        }

        protected virtual void RaisePeerRemoved(IFileTransferPeer peer)
        {
            PeerRemoved?.Invoke(this, new FileTransferPeerEventArgs(peer));
        }

        protected virtual void RaiseError(Exception exception)
        {
            Error?.Invoke(this, new ErrorEventArgs(exception));
        }
	}
}
