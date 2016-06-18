//
// BitTorrentFileTransferProvider.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

//#define RIDICULOUS_DEBUG_OUTPUT

using System;
using System.ComponentModel.Composition;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Transport;
using Meshwork.Logging;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
    [Export(typeof(IFileTransferProvider)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class BitTorrentFileTransferProvider : IFileTransferProvider
	{
		private readonly TorrentSettings torrentDefaults;
        private readonly MeshworkPeerConnectionListener listener;
        private readonly ClientEngine engine;
        private readonly ILoggingService loggingService;

        public int GlobalUploadSpeedLimit
        {
            get { return this.engine.Settings.GlobalMaxUploadSpeed; }
            set { this.engine.Settings.GlobalMaxUploadSpeed = value; }
        }

        public int GlobalDownloadSpeedLimit
        {
            get { return this.engine.Settings.GlobalMaxDownloadSpeed; }
            set { this.engine.Settings.GlobalMaxDownloadSpeed = value; }
        }

        [ImportingConstructor]
        public BitTorrentFileTransferProvider(ILoggingService loggingService)
		{
            this.loggingService = loggingService;
			MonoTorrent.Client.Logger.AddListener(new System.Diagnostics.ConsoleTraceListener());

			string downloadPath = Core.Settings.IncompleteDownloadDir;
			EngineSettings settings = new EngineSettings(downloadPath, 1);
			
			this.torrentDefaults = new TorrentSettings(4, 60, 0, 0);
			this.listener = new MeshworkPeerConnectionListener();
            this.engine = new ClientEngine(settings, this.listener);

			#if RIDICULOUS_DEBUG_OUTPUT
			engine.ConnectionManager.PeerMessageTransferred += delegate (object sender, PeerMessageEventArgs e) {
				LoggingService.LogDebug("{0}: {1}", e.Direction, e.Message.GetType().Name);
			};
			#endif
		}

        internal TorrentManager CreateTorrentManager(Torrent torrent, IFile file)
        {
            string localPath = (file is LocalFile) ? System.IO.Path.GetDirectoryName(((LocalFile)file).LocalPath) : this.engine.Settings.SavePath;
            this.loggingService.LogDebug("Local path: {0}", localPath);
            TorrentManager manager = new TorrentManager(torrent,
                                         localPath,
                                         torrentDefaults);
            this.engine.Register(manager);
            this.loggingService.LogDebug("{0}: Registered Manager with engine", Environment.TickCount);
            return manager;
        }

        public IFileTransfer CreateFileTransfer(IFile file)
        {
            return new BitTorrentFileTransfer(this, file);
        }
		
		public void HandleTransport(ITransport transport, object state = null)
		{
            var connection = new TorrentConnection(transport);
			this.loggingService.LogDebug("Transfer handled connection: {0}", connection.IsIncoming ? "Incoming" : "Outgoing");
            this.listener.AddConnection(connection, state != null ? (TorrentManager)state : null);
		}

        public void RemoveFileTransfer(IFileTransfer transfer)
		{
			if (transfer is BitTorrentFileTransfer)
            {
				var manager = ((BitTorrentFileTransfer)transfer).Manager;
				if (manager != null)
                {
					this.loggingService.LogDebug("Removing torrent from engine!");
					this.engine.Unregister(manager);
				}
			}
		}
    }
}
