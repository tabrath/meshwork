//
// TcpTransport.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;
using Meshwork.Logging;

namespace FileFind.Meshwork.Transport
{
	public class TcpTransport : TransportBase
	{
		public static readonly int DefaultPort = 7332;

		Socket socket = null;
		IPAddress address = IPAddress.Any;
		int port = 0;
		TransportCallback connectCallback = null;

		object sendLock = new object();
		object receiveLock = new object();

        public override EndPoint RemoteEndPoint
        {
            get
            {
                EndPoint ep = null;
                if (socket != null && socket.Connected)
                {
                    try
                    {
                        ep = socket.RemoteEndPoint;
                    }
                    catch (SocketException ex)
                    {
                        Core.LoggingService.LogError("Failed to get remote end point. I am pretty sure this is a bug in mono!", ex);
                    }
                }

                return ep ?? new IPEndPoint(this.address, this.port);
            }
        }

		internal TcpTransport(Socket socket)
		{
			this.socket = socket;
			this.address = (socket.RemoteEndPoint as IPEndPoint).Address;
			this.port = (socket.RemoteEndPoint as IPEndPoint).Port;
			this.transportState = TransportState.Connected;
            Incoming = true;
            RaiseConnected();
		}

		public TcpTransport(IPAddress address, int port, ulong connectionType)
		{
			this.address = address;
			this.port = port;
			ConnectionType = connectionType;
			Incoming = false;
			this.transportState = TransportState.Waiting;
		}

		public override void Connect(TransportCallback callback)
		{
			if (this.socket != null)
				throw new InvalidOperationException("This socket is already connected.");
			
			if (this.address.Equals(IPAddress.Any) || this.address.Equals(IPAddress.None) || this.port == 0)
				throw new Exception("Invalid IP Address/Port");
			
			this.transportState = TransportState.Connecting;
			this.connectCallback = callback;
			
			if (this.address.IsIPv6LinkLocal)
				this.address.ScopeId = Core.Settings.IPv6LinkLocalInterfaceIndex;
			
			var remoteEndpoint = new IPEndPoint(this.address, this.port);
			this.socket = new Socket(this.address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			this.socket.BeginConnect(remoteEndpoint, new AsyncCallback(OnConnected), null);
		}

        private void OnConnected(IAsyncResult result)
        {
            try
            {
                this.socket.EndConnect(result);
                this.transportState = TransportState.Connected;
                RaiseConnected();
                this.connectCallback(this);
            }
            catch (Exception ex)
            {
                Disconnect(ex);
            }
        }

		public override int Send(byte[] buffer, int offset, int size)
		{
			lock (sendLock)
            {
				int totalSent = 0;
				while (totalSent < size)
                {
					int sent = socket.Send(buffer, offset + totalSent, size - totalSent, SocketFlags.None);
					if (sent == 0)
						throw new Exception("No data was sent.");

                    totalSent += sent;
				}
				if (totalSent > size)
					throw new Exception("Sent too much! " + totalSent + " " + size);

				return totalSent;
			}
		}

		public override int Receive(byte[] buffer, int offset, int size)
		{
			if (size <= 0)
				throw new ArgumentException("Cannot receive <= 0 bytes");

			int totalReceived = 0;
            int count = 0;
            while (totalReceived < size)
            {
                if (socket == null)
					return 0;

                lock (receiveLock)
                {
					count = socket.Receive(buffer, offset + totalReceived, size - totalReceived, SocketFlags.None);
				}

				if (count == 0)
                {
					// This means the connection was closed.
					Disconnect();
					return 0;
				}

                totalReceived += count;
				
				if (totalReceived > size)
					throw new Exception("Somehow received too much! This shouldn't ever happen!");
			}
			return totalReceived;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append("TCP/");
            builder.Append(Incoming ? "INCOMING/" : "OUTGOING/");

			var addr = (this.socket != null) ? (this.socket.RemoteEndPoint as IPEndPoint).Address : this.address;
			if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
				builder.Append("[");
				builder.Append(addr.ToString());
				builder.Append("]");
			}
            else
				builder.Append(addr.ToString());

			builder.Append(":");
			builder.Append(this.port.ToString());

			return builder.ToString();
		}

		public override void Disconnect()
		{
			Disconnect(null);
		}

		public override void Disconnect(Exception ex)
		{
			if (this.transportState != TransportState.Disconnected)
            {
				this.transportState = TransportState.Disconnected;
				
				if (this.socket != null) {
					this.socket.Close();
					this.socket = null;
				}
				
				if (ex != null)
					if (ex is SocketException)
						Core.LoggingService.LogInfo("Transport {0} disconnected ({1}).", this.ToString(), ex.Message);
					else
						Core.LoggingService.LogInfo("Transport {0} disconnected with error: {1}", this.ToString(), ex.ToString());
				else
					Core.LoggingService.LogInfo("Transport {0} disconnected", this.ToString());
				
				RaiseDisconnected(ex);
			}
		}
	}
}
