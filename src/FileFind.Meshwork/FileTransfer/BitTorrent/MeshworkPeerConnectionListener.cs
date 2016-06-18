using System;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Transport;
using Meshwork.Logging;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	internal class MeshworkPeerConnectionListener : PeerListener
	{
		public MeshworkPeerConnectionListener ()
			: base (new System.Net.IPEndPoint (System.Net.IPAddress.Loopback, 0))
		{
			// Nothing
		}
		
		public override void Start()
		{
			// Nothing
		}
		
		public override void Stop()
		{
			// Nothing
		}			
		
		public void AddConnection (TorrentConnection connection, TorrentManager manager)
		{
			string remoteId = String.Empty;

			Core.LoggingService.LogDebug("AddConnection(): Start");

			if (!connection.IsIncoming) {
				// Send my identity.
				// XXX: This absolutely needs to be signed.
				connection.Transport.SendMessage(System.Text.Encoding.ASCII.GetBytes(Core.MyNodeID));

				// Get other end's identity.
				byte[] message = connection.Transport.ReceiveMessage();
				remoteId = System.Text.Encoding.ASCII.GetString(message);

			} else {
				// Get other end's identity.
				byte[] message = connection.Transport.ReceiveMessage();
				remoteId = System.Text.Encoding.ASCII.GetString(message);

				// Send my identity.
				// XXX: This absolutely needs to be signed.
				connection.Transport.SendMessage(System.Text.Encoding.ASCII.GetBytes(Core.MyNodeID));
			}

			Core.LoggingService.LogDebug("Pushing connection to engine: {0} - {1}", connection.IsIncoming ? "Incoming" : "Outgoing",
			                  ((Meshwork.Transport.TcpTransport)connection.Transport).RemoteEndPoint.ToString());

			Peer p = new Peer("", new Uri(String.Format("meshwork:{0}", remoteId)), EncryptionTypes.PlainText);
			RaiseConnectionReceived(p, connection, manager);

			Core.LoggingService.LogDebug("AddConnection(): End");
		}
	}
}
