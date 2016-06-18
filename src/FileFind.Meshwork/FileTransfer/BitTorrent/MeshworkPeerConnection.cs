//
// MeshworkPeerConnection.cs:
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using MonoTorrent.Client;
using MonoTorrent.Client.Connections;
using MonoTorrent.Client.Messages;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	internal class TorrentConnection : IConnection
	{
		ITransport transport;
		
		public ITransport Transport
		{
			get { return transport; }
		}
		
		public TorrentConnection (ITransport transport)
		{
			this.transport = transport;
		}
		
		public byte[] AddressBytes
		{
			get { return new byte[4]; }  // Not 100% what i need to do here
		}

		public bool Connected
		{
			get { return transport.State == TransportState.Connected; }
		}

		public bool CanReconnect
		{
			get { return false; }
		}

		public bool IsIncoming
		{
			get { return transport.Incoming; }
		}

		public System.Net.EndPoint EndPoint
		{
			get { return transport.RemoteEndPoint; }
		}

		public Uri Uri {
			get {
				return null;
			}
		}

		public void Dispose ()
		{
			transport.Disconnect();
		}

		public IAsyncResult BeginConnect (AsyncCallback callback, object state)
		{
			throw new NotSupportedException();  // Will never be called because 'CanReconnect == false'
		}

		public void EndConnect (IAsyncResult result)
		{
			throw new NotSupportedException();  // Will never be called because BeginConnect will never be called
		}

		public IAsyncResult BeginReceive (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return transport.BeginReceive (buffer, offset, count, callback, state);
		}
			
		public int EndReceive (IAsyncResult result)
		{
			return transport.EndReceive(result);
		}

		public IAsyncResult BeginSend (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return transport.BeginSend(buffer, offset, count, callback, state);
		}

		public int EndSend (IAsyncResult result)
		{
			return transport.EndSend (result);
		}
	}
}
