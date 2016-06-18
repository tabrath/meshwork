//
// TcpListener.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Meshwork.Logging;

namespace FileFind.Meshwork.Transport
{
    //TODO: rewrite for real async - refactor for di, take port from settings?
	public class TcpTransportListener : ITransportListener
	{
		private int port;
		private TcpListener listener;
		private Thread listenThread;

        public int Port
        {
            get { return this.port; }
            set
            {
                if (this.port != value)
                {
                    this.port = value;
                    if (Listening)
                    {
                        StopListening();
                        StartListening();
                    }
                }
            }
        }

        public bool Listening
        {
            get { return (this.listener != null || this.listenThread != null); }
        }
		
		public TcpTransportListener(int port)
		{
			this.port = port;
		}

		public void StartListening()
		{
			if (this.listener != null || this.listenThread != null)
				throw new InvalidOperationException("Already started");

            this.listener = new TcpListener(Common.SupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any, this.port);
			this.listener.Start ();

			this.listenThread = new Thread(new ThreadStart(Listen));
			this.listenThread.Start();
		}

		public void StopListening()
		{
			if (this.listenThread != null)
            {
				this.listenThread.Abort ();
				this.listenThread = null;
			}

			if (this.listener != null)
            {
				this.listener.Stop();
				this.listener = null;
			}
		}
		
		private void Listen()
		{
			try
            {
				while (true) 
                {
					Socket socket = listener.AcceptSocket();
					try
                    {
						ITransport transport = new TcpTransport(socket);
						Core.LoggingService.LogInfo("New incoming transport: {0}.", transport.ToString());
						Core.TransportManager.Add(transport);
						// TransportManager will take care of this 
						// connection now
					}
                    catch (Exception ex) 
                    {
						Core.LoggingService.LogError(ex.ToString());
					}
				}
			} 
            catch (ThreadAbortException)
            {
				// Someone called StopListening(), that's OK...
			} 
            catch (Exception ex) 
            {
				Core.LoggingService.LogError("Error in TcpListener.Listen()", ex);
			}
		}

		public override string ToString()
		{
			return string.Format("TCP listener on port {0}", this.port);
		}
	}
}
