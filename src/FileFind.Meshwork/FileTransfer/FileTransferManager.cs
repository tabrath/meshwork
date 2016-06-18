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
using Meshwork.Logging;
using IO = System.IO;

namespace FileFind.Meshwork.FileTransfer
{
    [Export(typeof(IFileTransferManager)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class FileTransferManager : IFileTransferManager
	{
		public event EventHandler<FileTransferEventArgs> NewFileTransfer;
		public event EventHandler<FileTransferEventArgs> FileTransferRemoved;

        public IFileTransferProvider Provider { get; }

        public IEnumerable<IFileTransfer> Transfers
        {
            get
            {
                lock (this.transfers)
                {
                    return this.transfers.ToArray();
                }
            }
        }

		private readonly List<IFileTransfer> transfers = new List<IFileTransfer>();
        private readonly ILoggingService loggingService;

        [ImportingConstructor]
        public FileTransferManager(IFileTransferProvider provider, ILoggingService loggingService)
		{
            //TODO: Support other providers?!
            Provider = provider;
            this.loggingService = loggingService;
		}

		// Starts a new file transfer, or adds a new peer if one
		// already exists.
		public IFileTransfer StartTransfer(Network network, Node node, IFile file)
		{
			if (node.NodeID == Core.MyNodeID)
				throw new ArgumentException("You cannot start a file transfer with yourself.");
			
			// Don't download files if it already exists in the completed downloads directory.
			// If the remote file is different, but has the same filename, it'll globber your copy.
			if (!(file is LocalFile)) {
				if (IO.File.Exists(IO.Path.Combine(Core.Settings.CompletedDownloadDir, file.Name))) {
					throw new Exception("A file by that name already exists in your download directory.");
				}
			}

            IFileTransfer transfer = null;
            lock (this.transfers)
            {
                transfer = this.transfers.SingleOrDefault(t => t.File == file);
            }

			if (transfer == null)
            {
				transfer = Provider.CreateFileTransfer(file);
                lock (this.transfers)
                {
                    this.transfers.Add(transfer);
                }
				RaiseNewTransfer(transfer);
			}
			
			transfer.AddPeer(network, node);
			transfer.Start();

			return transfer;
		}

		public void RemoveTransfer(IFileTransfer transfer)
		{
			if (!transfers.Contains(transfer))
				throw new ArgumentException("Unknown transfer");
			
			transfer.Cancel();

            lock (this.transfers)
            {
                this.transfers.Remove(transfer);
            }
            Provider.RemoveFileTransfer(transfer);

			RaiseTransferRemoved(transfer);
		}

		public void HandleIncomingTransport(ITransport transport)
		{
            Provider.HandleTransport(transport);
		}
	
		private void RaiseNewTransfer(IFileTransfer transfer)
		{
			this.loggingService.LogInfo("Transfer added: {0}", transfer.File.Name);

            NewFileTransfer?.Invoke(this, new FileTransferEventArgs(transfer));
		}

		private void RaiseTransferRemoved(IFileTransfer transfer)
		{
			this.loggingService.LogInfo("Transfer removed: {0}", transfer.File.Name);

            FileTransferRemoved?.Invoke(this, new FileTransferEventArgs(transfer));
		}
	}
}
