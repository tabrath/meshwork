//
// TransportBase.cs: Handles stuff common to all transport implementations.
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Runtime.Remoting.Messaging;

namespace FileFind.Meshwork.Transport
{
	public abstract class TransportBase : ITransport
	{
		private delegate int SendReceiveCaller (byte[] buffer, int offset, int size);
		private delegate void MessageSendCaller (byte[] buffer);
		private delegate byte[] MessageReceiveCaller ();
			
		protected TransportState transportState;
		
		public event EventHandler Connected;
        public event EventHandler<ErrorEventArgs> Disconnected;
		
        public IMeshworkOperation Operation { get; set; }
        public ulong ConnectionType { get; set; }
        public bool Incoming { get; protected set; }
        public ITransportEncryptor Encryptor { get; set; }
        public Network Network { get; set; }

        public TransportState State
        {
            get { return (this.transportState == TransportState.Connected && Encryptor != null && !Encryptor.Ready) ? TransportState.Securing : this.transportState; }
		}

        public abstract EndPoint RemoteEndPoint { get; }

        public abstract int Send(byte[] buffer, int offset, int size);
        public abstract int Receive(byte[] buffer, int offset, int size);

        public abstract void Connect(TransportCallback callback);

        public abstract void Disconnect();
        public abstract void Disconnect(Exception ex);

        protected void RaiseConnected()
		{
            Connected?.Invoke(this, EventArgs.Empty);
		}

		protected void RaiseDisconnected(Exception ex)
		{
            Disconnected?.Invoke(this, new ErrorEventArgs(ex));
		}

		public int Send(byte[] buffer)
		{
			return Send(buffer, 0, buffer.Length);
		}

		public int Receive(byte[] buffer)
		{
			return Receive(buffer, 0, buffer.Length);
		}

		public void SendMessage(byte[] buffer)
		{
			if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

			if (Encryptor != null)
				buffer = Encryptor.Encrypt(buffer);

			byte[] dataSizeBytes = EndianBitConverter.GetBytes(buffer.Length);
			byte[] realBuffer = new byte[buffer.Length + dataSizeBytes.Length];
			Array.Copy(dataSizeBytes, 0, realBuffer, 0, dataSizeBytes.Length);
			Array.Copy(buffer, 0, realBuffer, dataSizeBytes.Length, buffer.Length);

			Send(realBuffer);
		}

		public IAsyncResult BeginSendMessage(byte[] buffer, AsyncCallback callback, object state)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			
            if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			
			return new MessageSendCaller(SendMessage).BeginInvoke(buffer, callback, state);
		}
		
		object foo = new object();

		public byte[] ReceiveMessage()
		{
			try
            {
				lock (foo)
                {
					// get the message size 
					byte[] messageSizeBytes = new byte[4];
					int dataLength;
	
					int count = Receive(messageSizeBytes, 0, 4);
	
					if (count != 4)
						throw new Exception(string.Format("Received wrong amount in message size! Got: {0}, Expected: {1}", count, 4));
	
					dataLength = EndianBitConverter.ToInt32(messageSizeBytes, 0);
	
					// get the message
					byte[] messageBytes = new byte[dataLength];
					
					count = Receive(messageBytes, 0, dataLength);
	
					if (count != dataLength)
						throw new Exception(string.Format("Received wrong amount! Got: {0}, Expected: {1}", count, dataLength));
					
					if (Encryptor != null)
						messageBytes = Encryptor.Decrypt(messageBytes);
					
					return messageBytes;
				}
			}
            catch (Exception ex)
            {
				Disconnect(ex);
				return null;
			}
		}

		public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			return new SendReceiveCaller(Receive).BeginInvoke(buffer, offset, size, callback, state);
		}

		public int EndReceive(IAsyncResult asyncResult)
		{
			return ((SendReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
		
		public IAsyncResult BeginSend(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			return new SendReceiveCaller(Send).BeginInvoke(buffer, offset, size, callback, state);
		}
		
		public int EndSend(IAsyncResult asyncResult)
		{
			return ((SendReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}

		public IAsyncResult BeginReceiveMessage(AsyncCallback callback, object state)
		{
			return new MessageReceiveCaller(ReceiveMessage).BeginInvoke(callback, state);
		}
		
		public void EndSendMessage(IAsyncResult asyncResult)
		{
			((MessageSendCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
		
		public byte[] EndReceiveMessage(IAsyncResult asyncResult)
		{
			return ((MessageReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
	}
}
