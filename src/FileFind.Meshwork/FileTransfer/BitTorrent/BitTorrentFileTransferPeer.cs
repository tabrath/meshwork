//
// BitTorrentFileTransferPeer.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Linq;
using FileFind.Meshwork.FileTransfer;
using MonoTorrent.Client;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	public class BitTorrentFileTransferPeer : FileTransferPeerBase
	{
        private readonly List<PeerId> peers;
		
		public PeerId Peer
        {
			get
            {
                this.peers.Where(p => !p.IsConnected).ToList()
                    .ForEach(p => this.peers.Remove(p));

                return this.peers.FirstOrDefault(p => p.IsConnected);

				/*List<PeerId> removeMe = new List<PeerId>();
				PeerId returnMe = null;
				foreach (PeerId p in peers)
                {
					if (p.IsConnected)
                    {
						if (returnMe != null)
                        {
							LoggingService.LogWarning("!!! Found more than one valid peer!!");
						}
						returnMe = p;
					} 
                    else
                    {
						LoggingService.LogDebug("Removing invalid peer !! WOHOO!!");
						removeMe.Add(p);
					}
				}
				foreach (PeerId p in removeMe) 
                {
					peers.Remove(p);
				}

				return returnMe;*/
			}
		}

        public override ulong DownloadSpeed
        {
            get { return (ulong)(Peer?.Monitor.DownloadSpeed ?? 0); }
        }

        public override ulong UploadSpeed
        {
            get { return (ulong)(Peer?.Monitor.UploadSpeed ?? 0); }
        }

        public override double Progress
        {
            get { return Peer?.BitField.PercentComplete ?? 0; }
        }

        public override FileTransferPeerStatus Status
        {
            get
            {
                if (Peer == null)
                    // XXX: This could also mean hashing.
                    return FileTransferPeerStatus.WaitingForInfo;

                if (Peer.IsConnected)
                    return FileTransferPeerStatus.Transfering;

                // XXX: It may be possible that this sometimes means 'connecting'
                return FileTransferPeerStatus.Error;
            }
        }

        public override string StatusDetail
        {
            get { return string.Empty; }
        }
		
		public BitTorrentFileTransferPeer(Network network, Node node)
            : base(network, node)
		{
            this.peers = new List<PeerId>();
		}

        public void AddPeerId(PeerId peer)
        {
            this.peers.Add(peer);
        }
	}
}
