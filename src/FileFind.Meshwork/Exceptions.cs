//
// Exceptions.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Xml.Serialization;
namespace FileFind.Meshwork.Exceptions
{
	public class FileAlreadyDownloadedException : Exception
	{

		public FileAlreadyDownloadedException ()
		{
		}

		public override string Message {
			get { return "You have already finished downloading the selected file."; }
		}
	}

	public class KeyNotAvaliableException : Exception
	{
		Node _node;
		string _messageType;

		public KeyNotAvaliableException (Node N, MessageType messageType)
		{
			_node = N;
			_messageType = messageType.ToString();
		}

		public KeyNotAvaliableException ()
		{
		}

		public override string Message {
			get { return string.Format("Unable to send {1} message to {0} because a private key has not been generated for this node!", _node.ToString(), _messageType); }
		}
	}
	
	public class UnableToDecryptException : Exception
	{

		public UnableToDecryptException ()
		{
		}

		public override string Message {
			get { return string.Format("Unable to decrypt message contents!"); }
		}
	}
	
	public class InvalidSignatureException : Exception
	{

		public InvalidSignatureException ()
		{
		}

		public override string Message {
			get { return string.Format("Message had an invalid signature!"); }
		}
	}

	public class PasswordIncorrectException : Exception
	{

		public override string Message {
			get { return "Password Incorrect"; }
		}
	}
	
	public class UnableToConnectException : Exception
	{

		public override string Message {
			get { return string.Format("Unable to connect to the remote host"); }
		}
	}
	
	public class AlreadyConnectedException : Exception
	{
		string _IP;

		public AlreadyConnectedException (string IP)
		{
			_IP = IP;
		}

		public override string Message {
			get { return string.Format("Connection to was closed because a connection to " + _IP + " already exists."); }
		}
	}
	
	public class ConnectNotTrustedException : Exception
	{
		//private string _nodeid;

		public ConnectNotTrustedException ()
		{
			//public ConnectNotTrustedException(string nodeid) {
			//	_nodeid = nodeid;
		}

		public override string Message {
				//return string.Format("Connection to was closed because remote node is not in trusted node list (NodeID: {0}).", _nodeid);
			get { return "Not Trusted"; }
		}
	}
	
	public class ConnectNotAllowedException : Exception
	{
		private string _nodeid;

		public ConnectNotAllowedException (string nodeid)
		{
			_nodeid = nodeid;
		}

		public override string Message {
			get { return string.Format("Connection to was closed because you have selected to not allow connections with this node (NodeID: {0}).", _nodeid); }
		}
	}
	
	public class ConnectToSelfException : Exception
	{

		public override string Message {
			get { return string.Format("Connection was closed because you tried to connect to yourself! Naughty boy!"); }
		}
	}
	
	public class ConnectionTimeoutException : Exception
	{
		string _Host;

		public ConnectionTimeoutException (string host)
		{
			_Host = host;
		}

		public override string Message {
			get { return "Unable to connect to " + _Host + ": Connection timed out."; }
		}
	}
	
	public class ConnectionFailedException : Exception
	{

		public override string Message {
			get { return "No connection could be made because the target machine actively refused it"; }
		}
	}
}
