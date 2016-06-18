//
// ITransport.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;

namespace FileFind.Meshwork.Transport
{
	public delegate void TransportCallback (ITransport transport);

	public interface ITransport
	{
        event EventHandler Connected;
        event EventHandler<ErrorEventArgs> Disconnected;

        // XXX: Get rid of this setter
        ITransportEncryptor Encryptor { get; set; }
        Network Network { get; set; }
        ulong ConnectionType { get; set; }
        bool Incoming { get; }
        EndPoint RemoteEndPoint { get; }
        TransportState State { get; }
        IMeshworkOperation Operation { get; set; }

		int Send(byte[] buffer);
		int Send(byte[] buffer, int offset, int size);
		int Receive(byte[] buffer);
		int Receive(byte[] buffer, int offset, int size);

		IAsyncResult BeginSend(byte[] buffer, int offset, int size, AsyncCallback callback, object state);
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, AsyncCallback callback, object state);

		int EndSend(IAsyncResult asyncResult);
		int EndReceive(IAsyncResult asyncResult);
		
		void SendMessage(byte[] buffer);
		byte[] ReceiveMessage();

		IAsyncResult BeginSendMessage(byte[] buffer, AsyncCallback callback, object state);
		IAsyncResult BeginReceiveMessage(AsyncCallback callback, object state);
		
		void EndSendMessage(IAsyncResult asyncResult);
		byte[] EndReceiveMessage(IAsyncResult asyncResult);		
		
		void Connect(TransportCallback callback);

		void Disconnect();
		void Disconnect(Exception ex);
	}
}
