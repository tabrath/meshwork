//
// BitTorrentFileTransfer.cs: IFileTransfer implementation using MonoTorrent
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//


//#define RIDICULOUS_DEBUG_OUTPUT

using System;
using System.Linq;
using IO = System.IO;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Filesystem;
using MonoTorrent.Common;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Destination;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Errors;
using Meshwork.Logging;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	internal class BitTorrentFileTransfer : FileTransferBase
	{
        private readonly BitTorrentFileTransferProvider provider;
		private TorrentManager manager;
		private double hashingPercent = 0;
		private bool isCanceled = false;
		private bool startCalled = false;
		private int maxUploadSpeed = 0;
		private int maxDownloadSpeed = 0;

        public override ulong TotalDownloadSpeed
        {
            get { return (ulong)(this.manager?.Monitor.DownloadSpeed ?? 0); }
        }

        public override ulong TotalUploadSpeed
        {
            get { return (ulong)(this.manager?.Monitor.UploadSpeed ?? 0); }
        }

        public override ulong BytesDownloaded
        {
            get { return (ulong)(this.manager != null ? (manager.Progress * 0.01) * File.Size : 0); }
        }

        public override ulong BytesUploaded
        {
            get { return (ulong)(manager?.Monitor.DataBytesUploaded ?? 0); }
        }

        public override int UploadSpeedLimit
        {
            get { return this.manager?.Settings.MaxUploadSpeed ?? this.maxUploadSpeed; }
            set
            {
                if (this.maxUploadSpeed != value)
                {
                    this.maxUploadSpeed = value;
                    if (this.manager != null)
                        this.manager.Settings.MaxUploadSpeed = value;
                }
            }
        }

        public override int DownloadSpeedLimit
        {
            get { return this.manager?.Settings.MaxDownloadSpeed ?? this.maxDownloadSpeed; }
            set
            {
                if (this.maxDownloadSpeed != value)
                {
                    this.maxDownloadSpeed = value;
                    if (this.manager != null)
                        this.manager.Settings.MaxDownloadSpeed = value;
                }
            }
        }

        public override FileTransferDirection Direction
        {
            get { return (File is LocalFile) ? FileTransferDirection.Upload : FileTransferDirection.Download; }
        }

        public override FileTransferStatus Status
        {
            get
            {
#if RIDICULOUS_DEBUG_OUTPUT
                if (manager != null)
                    LoggingService.LogDebug("Transfer Internal Status -- Canceled: " + isCanceled + " StartCalled: " + startCalled + " State: " + manager.State + " Progress: " + manager.Progress);
                else
                    LoggingService.LogDebug("Transfer Internal Status -- Canceled: " + isCanceled + " StartCalled: " + startCalled);
#endif

                if (!startCalled)
                    return FileTransferStatus.Queued;

                if (isCanceled)
                    return FileTransferStatus.Canceled;

                if (manager == null)
                {
                    if (File.Pieces.Length == 0)
                        return (File is LocalFile) ? FileTransferStatus.Hashing : FileTransferStatus.WaitingForInfo;

                    // File was updated, but DetailsReceived() not yet called
                    return FileTransferStatus.WaitingForInfo;
                }

                switch (manager.State)
                {
                    case TorrentState.Paused:
                        return FileTransferStatus.Paused;

                    case TorrentState.Hashing:
                        return FileTransferStatus.Hashing;

                    case TorrentState.Stopped:
                        if (manager.Progress.Equals(100))
                        {
                            if (Direction == FileTransferDirection.Download)
                                return FileTransferStatus.Completed;

                            // XXX: For uploads, this isnt always right.
                            // Need to check that other peer got the entire file.
                            return FileTransferStatus.Completed;
                        }

                        if (!isCanceled)
                        {
                            // XXX: I think this might happen for just a breif moment while
                            // we're going from Transferring -> Canceled.
                            Core.LoggingService.LogWarning("This shouldn't happen ever, right? " + manager.Progress);
                        }

                        return FileTransferStatus.Canceled;

                    /*
                    case TorrentState.Queued:
                        return FileTransferStatus.Queued;
                    */

                    case TorrentState.Seeding:
                    case TorrentState.Downloading:
                        if (peers.Count > 0)
                            return (manager.OpenConnections == 0) ? FileTransferStatus.Connecting : FileTransferStatus.Transfering;

                        return FileTransferStatus.NoPeers;

                    default:
                        // XXX:
                        Core.LoggingService.LogWarning("Add a case for this: " + manager.State);
                        return FileTransferStatus.WaitingForInfo;
                }
            }
        }

        public override double Progress
        {
            get
            {
                if (this.manager == null)
                    return -1;

                if (this.manager.State == TorrentState.Hashing)
                    return hashingPercent;

                if (Direction == FileTransferDirection.Upload)
                    return (this.peers.Count > 1) ? this.peers.Average(p => p.Progress) : this.peers.First()?.Progress ?? -1;

                return this.manager.Progress;
            }
        }

        internal TorrentManager Manager { get { return this.manager; } }
		
		public BitTorrentFileTransfer(BitTorrentFileTransferProvider provider, IFile file)
            : base(file)
		{
            this.provider = provider;
		}

		public override void ErrorReceived(Node node, FileTransferError error)
		{
			Core.LoggingService.LogError("Received File Transfer Error: {0}", error.Message);
			StatusDetail = error.Message;
			Cancel();
            RaiseError(error.ToException());
		}

		public override void Start()
		{
			this.isCanceled = false;
			this.startCalled = true;

			// UPLOAD: Do we need to hash the file?
			if (File is LocalFile)
            {
				if (File.Pieces.Length == 0)
                {
					// Yep!
                    Core.ShareHasher.HashFile((LocalFile)File).ContinueWith(HashCallback);
				}
                else
                {
					// Nope, we're good! Just start!
					DetailsReceived();
				}
			
			// DOWNLOAD: Request file!
			}
            else
            {
				// Tell the other side that we want this file.
				// They will response with a FileDetails message, and DetailsReceived will be called.
				
				// XXX: If we already have the file pieces, we still need to send this message,
				// but the response (FileDetails) doesn't need to include pieces.
                this.peers
                    .Where(p => p.Node.NodeID != Core.MyNodeID).ToList()
                    .ForEach(p => p.Network.SendRoutedMessage(p.Network.MessageBuilder.CreateRequestFileMessage(p.Node, this)));

                if (File.Pieces.Any())
					DetailsReceived();
			}
		}

		private void HashCallback(IAsyncResult result)
		{
			try
            {
				// Start the transfer				
				DetailsReceived();
			}
            catch (Exception ex)
            {
				Core.LoggingService.LogError("Error in callback:", ex);
                RaiseError(ex);
			}
		}

		public override void DetailsReceived()
		{
			if (this.isCanceled)
				return;

			// Restart transfer. 
			if (this.manager != null)
            {
				this.manager.Start();
				return;
			}

			Core.LoggingService.LogDebug("{0}: Calling Start:\n{1}", Environment.TickCount, Environment.StackTrace);

            if (File.Pieces.Length == 0)
                throw new InvalidOperationException("No pieces");

            if (string.IsNullOrEmpty(File.InfoHash))
				throw new InvalidOperationException("No info hash");

			var torrent = CreateTorrent(File);
			
			#if RIDICULOUS_DEBUG_OUTPUT
			// Dump the hashes to the screen
			for (int i=0; i < torrent.Pieces.Count; i++)
				LoggingService.LogDebug(string.Format("{0}) {1}", i, BitConverter.ToString(torrent.Pieces.ReadHash(i))));
			#endif
			
			this.manager = this.provider.CreateTorrentManager(torrent, File);
			this.manager.Settings.MaxUploadSpeed = maxUploadSpeed;
			this.manager.Settings.MaxDownloadSpeed = maxDownloadSpeed;
			this.manager.PeersFound += manager_PeersFound;
			this.manager.PieceHashed += manager_PieceHashed;
			this.manager.TorrentStateChanged += manager_TorrentStateChanged;
			this.manager.PeerConnected += manager_PeerConnected;
			this.manager.PeerDisconnected += manager_PeerDisconnected;

			#if RIDICULOUS_DEBUG_OUTPUT
			LoggingService.LogDebug("Engine ID: {0}", provider.Engine.PeerId);
			#endif

			this.manager.Start();

			if (File is LocalFile)
                this.peers.ForEach(p => p.Network.SendFileDetails(p.Node, (LocalFile)File));
		}

		public override void Cancel()
		{
			// Torrent has been started
			if (this.manager != null)
            {
				// Don't try to stop twice.
				if (this.manager.State != TorrentState.Stopped)
					this.manager.Stop();
			
			// Torrent has not been started, may be hashing.
			}
            else
            {
				/* XXX:
				if (hashingThread != null) {
					hashingThread.Abort();
				}
				*/
			}
			
			Core.LoggingService.LogDebug("Transfer Cancel() {0}", Environment.StackTrace);

			this.isCanceled = true;
		}

		public override void Pause()
		{
			if (this.manager == null)
                throw new InvalidOperationException("Transfer has not been started.");

            this.manager.Pause();
		}

		public override void Resume()
		{
			// To resume a paused torrent, just hit start
			if (this.manager == null)
                throw new InvalidOperationException("Transfer has not been started.");
        
            this.manager.Start();
		}

		public override void AddPeer(Network network, Node node)
		{
			// Don't allow adding the same node (regardless of network)
			// more than once.
            if (this.peers.Exists(p => p.Node.NodeID == node.NodeID))
                throw new Exception("This node is already a peer.");

			var peer = new BitTorrentFileTransferPeer(network, node);
			peers.Add(peer);

            if (this.manager != null)
            {
                if (Direction == FileTransferDirection.Upload && File.Pieces.Any())
                    peer.Network.SendFileDetails(node, (LocalFile)File);

                if (this.manager.State != TorrentState.Stopped)
                    ConnectToPeer(peer);
            }
		}

		private static BEncodedDictionary GetTorrentData(IFile file)
		{
			var infoDict = new BEncodedDictionary();
			infoDict[new BEncodedString("piece length")] = new BEncodedNumber(file.PieceLength);
			infoDict[new BEncodedString("pieces")] = new BEncodedString(Common.StringToBytes(string.Join("", file.Pieces)));
			infoDict[new BEncodedString("length")] = new BEncodedNumber(file.Size);
			infoDict[new BEncodedString("name")] = new BEncodedString(file.Name);

			var dict = new BEncodedDictionary();
			dict[new BEncodedString("info")] = infoDict;

			var announceTier = new BEncodedList();
			announceTier.Add(new BEncodedString(string.Format("meshwork://transfers/{0}", file.InfoHash)));
			var announceList = new BEncodedList();
			announceList.Add(announceTier);
			dict[new BEncodedString("announce-list")] = announceList;
			
			return dict;
		}

		private static Torrent CreateTorrent(IFile file)
		{
			return Torrent.Load(GetTorrentData(file));
		}

		private void manager_PeerConnected(object sender, PeerConnectionEventArgs args)
		{
			try
            {
				Core.LoggingService.LogDebug("PEER CONNECTED: {0} {1}", args.PeerID.Uri, args.PeerID.GetHashCode());
			
				// XXX: This check can probably be removed.
				if (args.TorrentManager != this.manager)
					throw new Exception("PeerConnected for wrong manager. This should NEVER happen.");

                // Now, match the peer to the internal BittorrentFileTransferPeer.
                BitTorrentFileTransferPeer peer;
                lock (this.peers)
                {
                    peer = this.peers.Cast<BitTorrentFileTransferPeer>().SingleOrDefault(p => p.Node.NodeID == args.PeerID.Uri.AbsolutePath);
                }
                if (peer == null)
                    throw new Exception("Unexpected peer!!!! - " + args.PeerID.Uri.ToString());

                var transport = ((TorrentConnection)args.PeerID.Connection).Transport;
                transport.Operation = new FileTransferOperation(transport, this, peer);

                peer.AddPeerId(args.PeerID);
                RaisePeerAdded(peer);
			}
            catch (Exception ex)
            {
				Core.LoggingService.LogError("Error in manager_PeerConnected.", ex);
				args.PeerID.CloseConnection();
                RaiseError(ex);
			}
		}

		private void manager_PeerDisconnected(object sender, PeerConnectionEventArgs args)
		{
			try
            {
				Core.LoggingService.LogDebug("Peer Disconnected: {0}", args.PeerID.Uri);

                BitTorrentFileTransferPeer peer;
                lock (this.peers)
                {
                    peer = this.peers.Cast<BitTorrentFileTransferPeer>().SingleOrDefault(p => p.Node.NodeID == args.PeerID.Uri.AbsolutePath);
                    if (peer != null)
                    {
                        this.peers.Remove(peer);
                        RaisePeerRemoved(peer);
                    }
                    else
                        Core.LoggingService.LogWarning("PeerDisconnected: Unknown peer!");
                }

                if (!this.peers.Any())
                {
                    if (!this.manager.Progress.Equals(100))
                    {
						// Transfer didn't finish, cancel!
						Core.LoggingService.LogWarning("No more peers - canceling torrent!");
						Cancel();
                    }
                    else
                    {	
						// Transfer was complete (or an upload), just stop normally.
						this.manager.Stop();
					}
				}
			}
            catch (Exception ex)
            {
				Core.LoggingService.LogError("Error in manager_PeerDisconnected:", ex);
				Cancel();
                RaiseError(ex);
			}
		}
		
		private void manager_PeersFound(object sender, PeersAddedEventArgs args)
		{
			Core.LoggingService.LogDebug("Peers Found!");
		}
		
		private void manager_PieceHashed(object sender, PieceHashedEventArgs args)
		{	
			try
            {
				if (this.manager.State == TorrentState.Hashing)
					this.hashingPercent = (((double)args.PieceIndex / (double)this.manager.Torrent.Pieces.Count) * 100);

				#if RIDICULOUS_DEBUG_OUTPUT
				LoggingService.LogDebug("Piece Hashed!");
				#endif
			}
            catch (Exception ex)
            {
				Core.LoggingService.LogError("Error in manager_PieceHashed.", ex);
				Cancel();
                RaiseError(ex);
			}
		}

		private void manager_TorrentStateChanged(object sender, TorrentStateChangedEventArgs args)
		{
			try
            {
				Core.LoggingService.LogDebug("State: {0}", args.NewState);
				Core.LoggingService.LogDebug("Progress: {0:0.00}", this.manager.Progress);

				if (args.NewState == TorrentState.Downloading || (args.NewState == TorrentState.Seeding && args.OldState != TorrentState.Downloading))
                {
					// XXX: Only have the requesting end connect for now,
					// so we dont end up with redundant conncetions in each direction.
					// We need a solution for this that can handle reverse connections.
					if (!(File is LocalFile))
                    {
						Core.LoggingService.LogDebug("Torrent is ready! connecting to peers!");

						lock (this.peers)
                        {
                            if (!this.peers.Cast<BitTorrentFileTransferPeer>().Any(ConnectToPeer))
                            {
                                StatusDetail = "Unable to connect to any peers";
                                Cancel();
                            }
						}
					}
                    else
						Core.LoggingService.LogDebug("Torrent is ready! Waiting for connections from peers!");
				}

                if (args.NewState == TorrentState.Seeding && this.manager.Progress.Equals(100))
                {
					if (Direction == FileTransferDirection.Download)
                    {
						if (Core.Settings.IncompleteDownloadDir != Core.Settings.CompletedDownloadDir)
                        {
							// Ensure torrent is stopped before attempting to move file, to avoid access violation.
							this.manager.Stop();
							
                            //TODO: Move this out, yeah?!
							IO.File.Move(IO.Path.Combine(Core.Settings.IncompleteDownloadDir, File.Name), 
							             IO.Path.Combine(Core.Settings.CompletedDownloadDir, File.Name));
						}
					}

                    if (this.peers.Cast<BitTorrentFileTransferPeer>().Any(p => p == null || !p.Peer.IsSeeder))
                        return;
					
                    if (this.manager == null || !this.manager.Progress.Equals(100))
                    {
						// If we got here, then everyone is a seeder.
						// No need to keep the transfer active.
						Cancel();
					}
                    else
                    {
						// Success! Ensure torrent is stopped.
						this.manager.Stop();
					}
				}
			}
            catch (Exception ex)
            {
				Core.LoggingService.LogError("Error in manager_TorrentStateChanged.", ex);
				Cancel();
                RaiseError(ex);
			}
		}

		private bool ConnectToPeer(BitTorrentFileTransferPeer peer)
		{
			IDestination destination = peer.Node.FirstConnectableDestination;
			if (destination != null)
            {
				var transport = destination.CreateTransport(ConnectionType.TransferConnection);
				Core.LoggingService.LogDebug("New outgoing connection");
				peer.Network.ConnectTo(transport, OutgoingPeerTransportConnected);
				return true;
			}

            // FIXME: Mark peer as bad!
			Core.LoggingService.LogError("Transfer can't connect to peer {0} - no destinations available!", peer.Node);
			return false;
		}
		
		private void OutgoingPeerTransportConnected(ITransport t)
		{
			try
            {
                this.provider.HandleTransport(t, this.manager);
			}
            catch (Exception ex) 
            {
				// XXX: Better error handling here! Stop the torrent! Kill connections! Wreak havoc!
				Core.LoggingService.LogError(ex);
                RaiseError(ex);
			}
		}
	}
}
