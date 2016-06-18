//
// Network.cs: Maintains the state of the network.
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net/)
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Search;
using FileFind.Collections;
using FileFind.Meshwork.Errors;
using System.Collections.ObjectModel;
using Meshwork.Logging;

namespace FileFind.Meshwork 
{
	// Public delegates for events
	public delegate void ErrorEventHandler (object sender, Exception ex);
	public delegate void NetworkEventHandler (Network network);
	public delegate bool ReceivedKeyEventHandler (Network network, ReceivedKeyEventArgs args);
	public delegate void MemoEventHandler (Network network, Memo memo);
	public delegate void JoinPartChatEventHandler (Network network, ChatEventArgs args);
	public delegate void UpdateNodeInfoEventHandler (Network network, string oldNick, Node theNode);
	public delegate void NodeOnlineOfflineEventHandler (Network network, Node theNode);
	public delegate void PrivateMessageEventHandler (Network network, Node messageFrom, string messageText);
	public delegate void NetworkLocalNodeConnectionEventHandler (Network network, LocalNodeConnection connection);
	public delegate void ConnectionUpDownEventHandler (INodeConnection c);
	public delegate void ReceivedChatInviteEventHandler (Network network, Node inviteFrom, ChatRoom room, ChatInviteInfo invitation);
	public delegate void ChatMessageEventHandler (ChatRoom room, Node messageFromNode, string messageText);
	public delegate void ReceivedDirListingEventHandler (Network network, Node node, RemoteDirectory directory);
	public delegate void ReceivedSearchResultEventHandler (Network network, SearchResultInfoEventArgs args);
	public delegate void ReceivedNonCriticalErrorEventHandler (Network network, Node from, MeshworkError error);
	public delegate void ReceivedCriticalErrorEventHandler (INodeConnection errorFrom, MeshworkError error);
	public delegate void DebugWriteEventHandler (DebugInfo debugInfo);
	public delegate void AvatarEventHandler (Network network, Node node, byte[] avatarData);
	public delegate void RemoteFileEventHandler (Network network, RemoteFile remoteFile);
	//public delegate void FileOfferedEventHandler (Network network, FileOfferedEventArgs args);

	public class Network : FileFind.Meshwork.Object
	{
		// Public variables
		public readonly static string BroadcastNodeID = new string('0', 128);
		public bool AllowUploading = true;
		public int MaxSimltaniousUploads = 2;
		public int MaxSimltaniousUploadsPerUser = 1;
		public bool NoConnectionChecking = false;

		// Private Variables
		AutoconnectManager autoConnect;
		NodeConnectionCollection connections;
		List<Node> seenNodes = new List<Node>();
		string networkName;
		string networkId;
		Node localNode;
		NetworkDirectory directory;
		AutoResetEvent reset = new AutoResetEvent (false);
		MessageIdCollection processedMessages = new MessageIdCollection();
		MessageIdCollection routedMessages = new MessageIdCollection();
		MessageProcessor processor;
		Dictionary<string, TrustedNodeInfo> trustedNodes = new Dictionary<string, TrustedNodeInfo>();
		Dictionary<string, Node>            nodes        = new Dictionary<string, Node>();
		Dictionary<string, ChatRoom>        chatRooms    = new Dictionary<string, ChatRoom>();
		Dictionary<string, Memo>            memos        = new Dictionary<string, Memo>();
		Dictionary<string, AckMethod>       ackMethods   = new Dictionary<string, AckMethod>();

		// Internal static variables
		internal static List<MessageType> UnencryptedMessageTypes = new List<MessageType>();
		internal static List<MessageType> InsecureMessageTypes    = new List<MessageType>();
		internal static List<MessageType> LocalOnlyMessageTypes   = new List<MessageType>();
		internal static List<MessageType> MessageTypesToAck       = new List<MessageType>();

		// Internal variables
		internal MessageBuilder MessageBuilder;

		// Public Events
		public event JoinPartChatEventHandler JoinedChat;
		public event JoinPartChatEventHandler LeftChat;
		public event NetworkLocalNodeConnectionEventHandler ConnectingTo;
		public event NetworkLocalNodeConnectionEventHandler NewIncomingConnection;
		public event NodeOnlineOfflineEventHandler UserOnline;
		public event NodeOnlineOfflineEventHandler UserOffline;
		public event UpdateNodeInfoEventHandler UpdateNodeInfo;
		public event EventHandler CleanupFinished;
		public event DebugWriteEventHandler DebugWrite;
		public event MemoEventHandler MemoAdded;
		public event MemoEventHandler MemoDeleted;
		public event MemoEventHandler MemoUpdated;
		public event ConnectionUpDownEventHandler ConnectionUp;
		public event ConnectionUpDownEventHandler ConnectionDown;
		public event ReceivedChatInviteEventHandler ReceivedChatInvite;
		public event ChatMessageEventHandler ChatMessage;
		public event PrivateMessageEventHandler PrivateMessage;
		public event ReceivedKeyEventHandler ReceivedKey;
		public event ReceivedDirListingEventHandler ReceivedDirListing;
		public event ReceivedNonCriticalErrorEventHandler ReceivedNonCriticalError;
		public event ReceivedCriticalErrorEventHandler ReceivedCriticalError;
		public event AvatarEventHandler ReceivedAvatar;
		public event RemoteFileEventHandler ReceivedFileDetails;

		// Internal Events
		internal event ReceivedSearchResultEventHandler ReceivedSearchResult;

		static Network ()
		{
			// Only send ACKs for these types.
			MessageTypesToAck.Add(MessageType.PrivateMessage);
			MessageTypesToAck.Add(MessageType.RequestFile);
			MessageTypesToAck.Add(MessageType.NewSessionKey);
			MessageTypesToAck.Add(MessageType.Test);

			// These message types may only be sent to people
			// we are directly connected to. They are not encrypted.
			LocalOnlyMessageTypes.Add(MessageType.Auth);
			LocalOnlyMessageTypes.Add(MessageType.AuthReply);
			LocalOnlyMessageTypes.Add(MessageType.CriticalError);
			LocalOnlyMessageTypes.Add(MessageType.Ping);
			LocalOnlyMessageTypes.Add(MessageType.Pong);
			LocalOnlyMessageTypes.Add(MessageType.Ready);

			// These message types are not encrypted,
			// and they can be received by people who dont trust you.
			// (or who you don't trust)
			InsecureMessageTypes.Add(MessageType.Hello);
			InsecureMessageTypes.Add(MessageType.Auth);
			InsecureMessageTypes.Add(MessageType.AuthReply);
			InsecureMessageTypes.Add(MessageType.JoinChat);
			InsecureMessageTypes.Add(MessageType.LeaveChat);
			InsecureMessageTypes.Add(MessageType.ChatroomMessage);
			InsecureMessageTypes.Add(MessageType.ConnectionDown);
			InsecureMessageTypes.Add(MessageType.AddMemo);
			InsecureMessageTypes.Add(MessageType.DeleteMemo);
			InsecureMessageTypes.Add(MessageType.RequestKey);
			InsecureMessageTypes.Add(MessageType.MyKey);
			InsecureMessageTypes.Add(MessageType.Ack);
			InsecureMessageTypes.Add(MessageType.NonCriticalError);
			InsecureMessageTypes.Add(MessageType.SearchRequest);
			InsecureMessageTypes.Add(MessageType.SearchResult);
			
			// These message types are not encrypted,
			// but you still have to have a mutual trust relationship.
			UnencryptedMessageTypes.Add(MessageType.NewSessionKey);
		}

		internal static Network FromNetworkInfo (NetworkInfo networkInfo)
		{
			Network network = new Network (networkInfo.NetworkName);

			foreach (TrustedNodeInfo node in networkInfo.TrustedNodes.Values) {
				network.AddTrustedNode(node);
			}
			
			foreach (MemoInfo m in networkInfo.Memos) {
				var memoInfo = m;
				memoInfo.FromNodeID = Core.MyNodeID;
				network.PostMemo(new Memo(network, memoInfo));
			}

			return network;
		}

		private Network (string networkName)
		{
			if (networkName == null) {
				throw new ArgumentException("networkName cannot be null");
			}
			this.networkName = networkName;
			this.networkId = Common.SHA512Str(networkName);

			localNode = Core.CreateLocalNode(this);
			AddNode(localNode);
			
			connections = new NodeConnectionCollection(this);
			MessageBuilder = new MessageBuilder(this);
			processor = new MessageProcessor(this);
			autoConnect = new AutoconnectManager(this, Core.Settings.AutoConnectCount);
			
			directory = new NetworkDirectory(this);
		}

		public string NetworkName {
			get {
				return networkName;
			}
		}

		public string NetworkID {
			get {
				return networkId;
			}
		}

		public NetworkDirectory Directory {
			get {
				return directory;
			}
		}
		
		internal AutoResetEvent Reset {
			get {
				return reset;
			}
		}
		
		public Node LocalNode {
			get {
				return localNode;
			}
		}

		public IReadOnlyDictionary<string, Node> Nodes {
			get {
				return new ReadOnlyDictionary<string, Node>(nodes);
			}
		}

		internal void AddNode (Node node)
		{
			nodes.Add(node.NodeID, node);
			
			Core.LoggingService.LogInfo("User online: " + node.NickName);
					
			if (UserOnline != null)
				UserOnline (this, node);
		}

		internal void RemoveNode (Node node)
		{
			nodes.Remove(node.NodeID);
		}

		public Node GetNode(string nodeId)
		{
			if (!nodes.ContainsKey(nodeId)) {
				return null;
			} else {
				return nodes[nodeId];
			}
		}

		public IDictionary<string, TrustedNodeInfo> TrustedNodes {
			get {
				return new ReadOnlyDictionary<string, TrustedNodeInfo>(trustedNodes);
			}
		}

		public void AddPublicKey (PublicKey key)
		{
			TrustedNodeInfo info = new TrustedNodeInfo(key);
			this.trustedNodes.Add(info.NodeID, info);
			if (nodes.ContainsKey(info.NodeID)) {
				Node node = nodes[info.NodeID];
				if (!node.FinishedKeyExchange) {
					node.CreateNewSessionKey();
				}
			}
			Core.Settings.SyncNetworkInfo();
		}

		internal void AddTrustedNode(TrustedNodeInfo info)
		{
			if (info.NodeID == Core.MyNodeID)
				throw new InvalidOperationException("Cannot add a TrustedNodeInfo with your own key!");
			
			this.trustedNodes.Add(info.NodeID, info);
			
			if (nodes.ContainsKey(info.NodeID)) {
				Node node = nodes[info.NodeID];
				if (!node.FinishedKeyExchange) {
					node.CreateNewSessionKey();
				}
			}
		}

		internal void UpdateTrustedNodes(IDictionary<string, TrustedNodeInfo> newNodes)
		{
			List<string> toRemove = new List<string>();
			foreach (string nodeId in this.trustedNodes.Keys) {
				if (!newNodes.ContainsKey(nodeId)) {
					toRemove.Add(nodeId);
				}
			}
			foreach (string nodeId in toRemove) {
				if (nodes.ContainsKey(nodeId)) {
					Node node = nodes[nodeId];
					node.ClearSessionKey();
				}
				this.trustedNodes.Remove(nodeId);
			}

			foreach (LocalNodeConnection connection in LocalConnections) {
				if (connection.NodeRemote != null && toRemove.Contains(connection.NodeRemote.NodeID)) {
					connection.Disconnect(new Exception("Remote node is no longer trusted."));
				}
			}

			foreach (KeyValuePair<string, TrustedNodeInfo> pair in newNodes) {
				if (!this.trustedNodes.ContainsKey(pair.Key)) {
					AddTrustedNode(pair.Value);
				}
			}
		}

		internal void RaiseDebugWrite(DebugInfo d)
		{
			if (DebugWrite != null) {
				DebugWrite(d);
			}
		}

		public void Start ()
		{
			autoConnect.Start();
		}

		public void Stop ()
		{
			autoConnect.Stop();

			foreach (LocalNodeConnection connection in LocalConnections) {
				connection.Disconnect();
			}
		}

		public long CountTotalSharedFiles()
		{
			long result = 0;
			foreach (Node node in nodes.Values) {
				result += node.Files;
			}
			return result;
		}

		public long CountTotalSharedBytes()
		{
			long result = 0;
			foreach (Node node in nodes.Values) {
				result += node.Bytes;
			}
			return result;
		}
		
		internal void AddConnection (INodeConnection connection)
		{
			lock (connections)
				connections.Add(connection);
		
			if (connection is LocalNodeConnection) {
				var localConnection = (LocalNodeConnection)connection;
				if (localConnection.Incoming) {
					Core.LoggingService.LogInfo("New incoming connection from {0}.", localConnection.RemoteAddress);
					if (NewIncomingConnection != null)
						NewIncomingConnection(this, localConnection);
				} else {					
					Core.LoggingService.LogInfo("New outgoing connection to {0}.", localConnection.RemoteAddress);
					if (ConnectingTo != null)
						ConnectingTo(this, localConnection);
				}
			} else if (connection is RemoteNodeConnection) {
				Core.LoggingService.LogInfo("Added new connection between {0} and {1}.", connection.NodeLocal.NickName, connection.NodeRemote.NickName);
				if (ConnectionUp != null)
					ConnectionUp(connection);
			}
		}
		
		internal void RemoveConnection (INodeConnection connection)
		{
			lock (connections)
				connections.Remove(connection);
			
			if (connection is RemoteNodeConnection) {
				Core.LoggingService.LogInfo("Removed connection between {0} and {1}.", connection.NodeLocal.NickName, connection.NodeRemote.NickName);			
				if (ConnectionDown != null)
					ConnectionDown(connection);
			}
		}
		
		public INodeConnection FindConnection (string firstNodeID, string secondNodeID)
		{
			lock (connections) {
				foreach (INodeConnection connection in connections) {
					if (connection.NodeLocal != null & connection.NodeRemote != null) {
						if (connection.NodeLocal.NodeID == firstNodeID & connection.NodeRemote.NodeID == secondNodeID)
							return connection;
						else if (connection.NodeRemote.NodeID == firstNodeID & connection.NodeLocal.NodeID == secondNodeID)
							return connection;
					}
				}
			}
			return null;
		}
		
		public INodeConnection[] Connections {
			get {
				return connections.ToArray();
			}
		}

		public LocalNodeConnection[] LocalConnections {
			get {
				return connections
					.FindAll(c => c is LocalNodeConnection)
					.Cast<LocalNodeConnection>()
					.ToArray();
			}
		}
		
		public LocalNodeConnection[] ReadyLocalConnections { 
			get {
				return connections
					.FindAll(c => c is LocalNodeConnection &&
					         ((LocalNodeConnection)c).ConnectionState == ConnectionState.Ready)
					.Cast<LocalNodeConnection>()
					.ToArray();
			}
		}

		internal AutoconnectManager AutoconnectManager {
			get {
				return autoConnect;
			}
		}

		/*
		public INodeConnection GetNodeConnection(string FirstNodeID, string SecondNodeID) {
			if (FirstNodeID != null & SecondNodeID != null) {
				IDictionaryEnumerator e = Connections.Clone().GetEnumerator();
				while (e.MoveNext()) {
					INodeConnection c = (INodeConnection)e.Value;
					if (c.NodeLocal != null & c.NodeRemote != null) {
						if (c.NodeLocal.NodeID == FirstNodeID & c.NodeRemote.NodeID == SecondNodeID) {
							return c;
						} else if (c.NodeRemote.NodeID == FirstNodeID & c.NodeLocal.NodeID == SecondNodeID) {
							return c;
						}
					} else {
						Console.WriteLine("Error -2 in FromIDs: Can we ignore this??");
					}
				}
			} else {
				throw new Exception("Error -1 in FromIDs");
			}
			return null;
		}
		*/

		public Message SendPrivateMessage(Node SendTo, string MessageText)
		{
			Message m = MessageBuilder.CreateMessageMessage(SendTo, MessageText);
			SendRoutedMessage(m);
			return m;
		}

		public void FileSearch (FileSearch search)
		{
			// XXX: Add support for paganation.
			Message m = MessageBuilder.CreateSearchRequestMessage(search.Id, search.Query, 0);
			SendBroadcast(m, null);
		}

		public void SendChatMessage(ChatRoom room, string messageText)
		{
			if (room.InRoom == true) {
				if (room.HasPassword) {
					byte[] saltBytes = System.Text.Encoding.UTF8.GetBytes(room.Id);
					SendBroadcast(MessageBuilder.CreateChatMessageMessage(room, Security.Encryption.PasswordEncrypt(room.Password, messageText, saltBytes)), LocalNode);
				} else {
					SendBroadcast(MessageBuilder.CreateChatMessageMessage(room, messageText), LocalNode);
				}
			} else {
				throw new Exception("You cannot send messages to chatrooms that you are not in");
			}
		}
		
		public void JoinOrCreateChat (string name, string password)	
		{
			string roomId = String.IsNullOrEmpty(password) ? Common.SHA512Str(name) : Common.SHA512Str(name + password);
			lock (chatRooms) {
				ChatRoom room = null;
				if (chatRooms.ContainsKey(roomId)) {
					room = chatRooms[roomId];
				} else {
					room = new ChatRoom(this, roomId, name);
					AddChatRoom(room);
				}
				JoinChat(room, password);
			}
		}		

		public void JoinChat (ChatRoom room, string password)
		{
			if (!room.TestPassword(password))
				throw new ArgumentException("Incorrect password");
			
			room.Password = password;
			
			JoinChat(room);
		}
		
		public void JoinChat (ChatRoom room)
		{
			if (room == null) {
				throw new ArgumentNullException ("room");
			}

			lock (chatRooms) {
				if (!room.Users.ContainsKey(this.LocalNode.NodeID)) {
					room.AddUser(this.LocalNode);
					SendBroadcast(MessageBuilder.CreateJoinChatMessage(room), this.LocalNode);
					OnJoinedChat(new ChatEventArgs(this.LocalNode, room));
				} else {
					throw new Exception("Already in room");
				}
			}
		}
		
		/*

		internal void JoinChat(string roomId, string roomName)
		{
			JoinChat(roomId, roomName, null);
		}

		internal void JoinChat(string roomName, string roomId, string password)
		{
			lock (chatRooms) {
				ChatRoom room;
				if (!chatRooms.ContainsKey(roomId)) {
					room = new ChatRoom (this, roomId, roomName);
					AddChatRoom(c);
				} else {
					c = chatRooms[roomId];
					if (c.PasswordTest == "") {
						throw new Exception("That chatroom is not password-protected.");
					}
					if (c.TestPassword(password) == false) {
						throw new PasswordIncorrectException();
					}
				}
			}

			c.Password = password;
			if (!c.Users.ContainsKey(LocalNode.NodeID)) {
				c.AddUser(LocalNode);
				SendBroadcast(MessageBuilder.CreateJoinChatMessage(c.Name), LocalNode);
				OnJoinedChat (new ChatEventArgs (LocalNode, c));
			}
		}
		
		*/

		public void LeaveChat(ChatRoom room)
		{
			SendBroadcast(MessageBuilder.CreateLeaveChatMessage(room), LocalNode);
			room.RemoveUser(LocalNode);
			OnLeftChat (new ChatEventArgs (LocalNode, room));
			if (room.Users.Count == 0) {
				RemoveChatRoom(room);
			}
		}

		public void SendPong (Node node, ulong timestamp)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreatePongMessage(node, timestamp));
		}

		internal void SendCriticalError (Node node, MeshworkError error)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreateCriticalErrorMessage(node, error));
		}
		
		// XXX : Move this
		internal void SendAuthReply (INodeConnection connection, Node node)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreateAuthReplyMessage(connection, this.TrustedNodes[node.NodeID]));
		}

		public void SendMyKey (Node sendTo)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreateMyKeyMessage(sendTo));
		}

		internal void SendSearchReply(Node node, SearchResultInfo reply)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreateSearchReplyMessage(node, reply));
		}

		internal void SendNonCriticalError (Node node, MeshworkError error)
		{
			this.SendRoutedMessage(this.MessageBuilder.CreateNonCriticalErrorMessage(node, error));

		}

		/*
		public Message SendFile (Node SendTo, string FilePath, long FileSize)
		{
			Message m = MessageBuilder.CreateSendFileMessage(SendTo, FilePath, FileSize);
			SendRoutedMessage(m);
			return m;
		}
		*/

		public void SendFile (Node node, string filePath)
		{
			throw new NotImplementedException();
			/*
			if (this.LocalNode.ExternalIP != node.ExternalIP) {
				if (this.LocalNode.AcceptsExternalConnections == false & node.AcceptsExternalConnections == false)
					throw new Exception (String.Format ("You cannot send a file to {0} because neither of you can accept incoming connections.", this.ToString()));
			}
			

			Directory directory = Core.FileSystem.RootDirectory.GetSubdirectory(this.LocalNode.NodeID);

			string sendfileDirName = String.Format(".sendfile-{0}", node.NodeID);
			Directory sendfileDir = directory.GetSubdirectory(sendfileDirName);
			if (sendfileDir == null) {
				sendfileDir = directory.CreateSubdirectory(sendfileDirName);
			}
			
			System.IO.FileInfo info = new System.IO.FileInfo (filePath);

			
			File file = sendfileDir.CreateFile(info);
			this.SendRoutedMessage (this.MessageBuilder.CreateSendFileMessage (node, file.FullPath, file.Size));
			*/
		}

		//XXX: This method is a huge mess.
		public void SendRoutedMessage(Message message)
		{
			if (nodes.ContainsKey(message.To)) {
				if (UnencryptedMessageTypes.IndexOf(message.Type) == -1 && 
				    InsecureMessageTypes.IndexOf(message.Type) == -1 &&
				    LocalOnlyMessageTypes.IndexOf(message.Type) == -1 && 
				    Nodes[message.To].FinishedKeyExchange == false) 
				{
					throw new KeyNotAvaliableException(Nodes[message.To], message.Type);
				}

				// Don't route the same message twice.
				if (routedMessages.ContainsKey(message.MessageID) == false) {
					routedMessages.Add(message.MessageID);
				} else {
					return;
				}

				if (message.To == LocalNode.NodeID) {
					throw new Exception("you cannot send messages to yourself!");
				}

				if (message.To == BroadcastNodeID) {
					throw new Exception("Invalid To");
				}
				
				// Send LocalOnly message types regardless of ConnectionState
				if (LocalOnlyMessageTypes.IndexOf(message.Type) > -1) {

					foreach (LocalNodeConnection connection in LocalConnections) {
						if (connection.NodeRemote != null) {
							if (connection.NodeRemote.NodeID == message.To) {
								connection.SendMessage(message);
								return;
							}
						}
					}
					throw new Exception("No connection to " + Nodes[message.To].ToString());

				} else {
					// If we're connected directly to this person, send it through that connection.
					foreach (LocalNodeConnection c in LocalConnections) {
						if (c.ConnectionState == ConnectionState.Ready && c.NodeRemote != null) {
							if (c.NodeRemote.NodeID == message.To) {
								c.SendMessage(message);
								return;
							}
						}
					}

					// Otherwise, broadcast it (this will go away once we have real routing).
					routedMessages.Remove(message.MessageID);
					SendBroadcast(message, null);
					return;
				} 
			} else {
				throw new Exception("Node " + message.To + " does not exist on the network!");
			}
			
			//throw new Exception("Message was not sent for some reason :( " + Message.Type);
		}

		public void SendBroadcast(Message message)
		{
			SendBroadcast (message, null);
		}

		internal void SendBroadcast(Message message, Node nodeFrom)
		{
			var messageID = message.MessageID;
			if (routedMessages.ContainsKey(messageID) == false) {
				routedMessages.Add(messageID);
				
				int count = 0;

				foreach (LocalNodeConnection c in LocalConnections) {
					if (c.ConnectionState == ConnectionState.Ready && (nodeFrom == null || c.NodeRemote != nodeFrom)) {
						c.SendMessage(message);
						count ++;
					}
				}
				
				if (count == 0) {
					if (message.To != Network.BroadcastNodeID) {
						throw new Exception("ERROR: Message didn't end up going anywhere!" + 
						                    " Type: " + message.Type +
						                    " From: " + message.From +
						                    " To: " + message.To);
					}
				}
			}
		}

		private bool CheckForRoute(Node FirstNode, Node SecondNode) {
			if (FirstNode == SecondNode) {
				return true;
			} else {
				seenNodes.Clear();
				bool bb = SearchNode(FirstNode, SecondNode);
				return bb;
			}
		}

		private bool SearchNode(Node NodeToSearch, Node NodeToFind)
		{
			if (NodeToSearch == null | NodeToFind == null) {
				return false;
			}
			if (seenNodes.IndexOf(NodeToSearch) < 0) {
				seenNodes.Add(NodeToSearch);
				 foreach (INodeConnection CurrentConnection in NodeToSearch.GetConnections()) {
					if (NodeToSearch == CurrentConnection.NodeRemote) {
						if (CurrentConnection.NodeLocal == NodeToFind) {
							return true;
						}
					} else {
						if (CurrentConnection.NodeRemote == NodeToFind) {
							return true;
						}
					}
				}

				 foreach (INodeConnection CurrentConnection in NodeToSearch.GetConnections()) {
					if (NodeToSearch == CurrentConnection.NodeRemote) {
						if (SearchNode(CurrentConnection.NodeLocal, NodeToFind) == true) {
							return true;
						}
					} else {
						if (SearchNode(CurrentConnection.NodeRemote, NodeToFind) == true) {
							return true;
						}
					}
				}
			}
			return false;
		}

		public ICollection<Memo> Memos {
			get {
				lock (memos)
					return memos.Values;
			}
		}

		internal void AddMemo (Memo memo)
		{
			if (memo.ID == null) {
				throw new Exception("Cannot add a memo with no ID");
			}

			lock (memos) {
				memos.Add(memo.ID, memo);
			}

			OnMemoAdded(memo);
		}
		
		internal void RemoveMemo (Memo memo)
		{
			lock (memos) {
				memos.Remove(memo.ID);
			}

			OnMemoDeleted(memo);
		}
		
		internal bool HasMemo (string id)
		{
			lock (memos)
				return memos.ContainsKey(id);
		}
		
		internal Memo GetMemo (string id)
		{
			lock (memos) {
				return memos[id];
			}
		}
		
		internal void AddOrUpdateMemo (Memo memo)
		{
			lock (memos) {
				if (memos.ContainsKey(memo.ID)) {
					Memo existingMemo = memos[memo.ID];
					//existingMemo.FileLinks = memo.FileLinks;
					existingMemo.Subject = memo.Subject;
					existingMemo.Text = memo.Text;
					OnMemoUpdated(memo);					
				} else {
					AddMemo(memo);
				}	
			}
		}

		public ICollection<ChatRoom> ChatRooms {
			get {
				return chatRooms.Values;
				//return new ReadOnlyDictionary<string, ChatRoom>(chatRooms);
			}
		}

		internal void AddChatRoom (ChatRoom room)
		{
			lock (chatRooms) {
				chatRooms.Add(room.Id, room);
			}
		}

		internal void RemoveChatRoom (ChatRoom room)
		{
			lock (chatRooms) {
				chatRooms.Remove(room.Id);
			}
		}

		internal bool HasChatRoom (string id)
		{
			lock (chatRooms) {
				return chatRooms.ContainsKey(id);
			}
		}
		
		public ChatRoom GetChatRoom (string id)
		{
			lock (chatRooms) {
				if (!chatRooms.ContainsKey(id)) {
					return null;
				} else {
					return chatRooms[id];
				}
			}
		}

		public void PostMemo (Memo memo)
		{
			lock (memos) {
				if (!memos.ContainsKey(memo.ID)) {
					AddMemo(memo);
				} else {
					OnMemoUpdated(memo);
				}
			}
			SendBroadcast(MessageBuilder.CreateAddMemoMessage(memo), LocalNode);
			
			Core.Settings.SyncNetworkInfoAndSave();
		}

		public void DeleteMemo(Memo m)
		{
			if (Core.IsLocalNode(m.Node)) {
				RemoveMemo(m);
				SendBroadcast(MessageBuilder.CreateDelMemoMessage(m), LocalNode);
			} else {
				throw new InvalidOperationException();
			}
			
			Core.Settings.SyncNetworkInfoAndSave();
		}

		public void SendInfoToTrustedNode(Node node)
		{
			this.SendRoutedMessage(MessageBuilder.CreateMyInfoMessage(node));
		}

		public void SendInfoToTrustedNodes()
		{
			foreach (Node n in nodes.Values) {
				if (n.FinishedKeyExchange == true) {
					SendInfoToTrustedNode(n);
				}
			}
		}

		internal void SendFileDetails (Node to, LocalFile file)
		{
			Message m = MessageBuilder.CreateFileDetailsMessage(to, file);
			this.SendRoutedMessage(m);
		}

		public void RequestAvatar (Node node)
		{
			Message m = MessageBuilder.CreateRequestAvatarMessage(node);
			SendRoutedMessage(m);
		}

		public void SendAvatar (Node node)
		{
			if (localNode.AvatarSize > 0) {
				byte[] data = Core.AvatarManager.GetAvatarBytes(localNode.NodeID);
				Message m = MessageBuilder.CreateAvatarMessage(node, data);
				SendRoutedMessage(m);
			} else {
				throw new Exception("You do not have an avatar.");
			}
		}
		
		public void SendChatInvitation (Node node, ChatRoom room, string message, string password)
		{
			Message m = MessageBuilder.CreateChatInviteMessage(node, room, message, password);
			SendRoutedMessage (m);
		}

		public void RequestPublicKey(Node node)
		{
			Core.LoggingService.LogInfo("Requesting public key from {0}.", node);
			Message m = MessageBuilder.CreateRequestKeyMessage(node);
			SendRoutedMessage(m);
		}
		
		internal void RequestDirectoryListing (string path)
		{
			var node = PathUtil.GetNode(path);
			var remotePath = "/" + String.Join("/", path.Split('/').Slice(3));
			var message = MessageBuilder.CreateRequestDirectoryMessage(node, remotePath);
			SendRoutedMessage(message);
		}
		
		internal void RequestFileDetails (string path)
		{
			var remotePath = "/" + String.Join("/", path.Split('/').Slice(3));
			var message = new Message(this, MessageType.RequestFileDetails);
			message.To = PathUtil.GetNode(path).NodeID;
			message.Content = remotePath;
			SendRoutedMessage(message);
		}

		public IFileTransfer DownloadFile (Node node, RemoteFile file)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (file == null)
				throw new ArgumentNullException("file");

			return Core.FileTransferManager.StartTransfer(this, node, file);
		}

		internal string CreateMessageID()
		{
			while (true) {
				string id = Guid.NewGuid().ToString();
				if ((!routedMessages.ContainsKey(id)) && (!processedMessages.ContainsKey(id))) {
					return id;
				}
			}
		}

		internal void NewSessionKeyReady(DateTime timeReceived, object[] args)
		{
			try {
				Node n = (Node)args[0];
				if (n.FinishedKeyExchange == false & n.RemoteHasKey == false) {
					n.RemoteHasKey = true;

					Core.LoggingService.LogDebug("{0} received our session key request!", n);

					if (n.LocalHasKey == true) {
						Core.LoggingService.LogInfo("Secure communication channel to {0} is now available", n);
						SendInfoToTrustedNode(n);
					}
				}
			} catch (Exception ex) {
				Core.LoggingService.LogError(ex);
			}
		}

		internal void AppendNetworkState (NetworkState stateObject)
		{
			if (stateObject.KnownConnections != null) {
				foreach (ConnectionInfo connection in stateObject.KnownConnections) {
					this.ProcessNewConnection (connection);
				}
			}

			if (stateObject.KnownChatRooms != null) {
				foreach (ChatRoomInfo roomInfo in stateObject.KnownChatRooms) {
					lock (chatRooms) {
						if (!chatRooms.ContainsKey(roomInfo.Id)) {
							ChatRoom newRoom = new ChatRoom(this, roomInfo);
							AddChatRoom(newRoom);
						}
					}

					ChatRoom realRoom = chatRooms[roomInfo.Id];

					foreach (string nodeId in roomInfo.Users) {
						Node currentNode = GetNode(nodeId);
						if (currentNode != null) {
							if (!realRoom.Users.ContainsKey(currentNode.NodeID)) {
								if (currentNode.NodeID == Core.MyNodeID) {
									// err.. but.. i'm not in here!!
									Core.LoggingService.LogWarning("Someone thought I was in {0} but I'm not!!", realRoom.Name);
									this.LeaveChat(realRoom);

								} else {
									realRoom.AddUser(currentNode);
									OnJoinedChat (new ChatEventArgs (currentNode, realRoom));
								}
							}
						} else {
							Core.LoggingService.LogWarning("TRIED TO ADD NON-EXISTANT NODE {0} TO CHATROOM {1}", nodeId, roomInfo.Name);
						}
					}
				}
			}

			if (stateObject.KnownMemos != null) {
				foreach (MemoInfo memoInfo in stateObject.KnownMemos) {
					lock (memos) {
						if (memos.ContainsKey(memoInfo.ID)) {
							Memo existingMemo = memos[memoInfo.ID];
							existingMemo.Subject = memoInfo.Subject;
							existingMemo.Text = memoInfo.Text;
							OnMemoUpdated (existingMemo);
						} else {
							Memo memo = new Memo (this, memoInfo);
							AddMemo(memo);
						}	
					}
				}
			}
		}

		internal void ForwardMessage(Message m, LocalNodeConnection connection) {
			if (m.To != LocalNode.NodeID) {
				if (m.To == Network.BroadcastNodeID) {
					this.SendBroadcast(m, connection.NodeRemote);
				} else {
					this.SendRoutedMessage(m);
					return;
				}
			} else {
				throw new InvalidOperationException();
			}
		}

		// XXX: Refactor this entire method!
		internal void ProcessMessage (object state)
		{
			Message message = null;
			LocalNodeConnection connection;
			try {
				MessageInfo info = (MessageInfo)state;

				connection = info.Connection;
				message = info.Message;

				if (Connections.Contains(connection) == false || connection.ConnectionState == ConnectionState.Disconnected) {
					Core.LoggingService.LogWarning("Network.ProcessMessage: Ignored message from disconnected connection.");
					return;
				}

				if (this.processedMessages.ContainsKey(message.MessageID)) {
					return;
				}

				this.processedMessages.Add(message.MessageID);

				if (message.To == Network.BroadcastNodeID || message.To != this.LocalNode.NodeID) {
					this.SendBroadcast(message, connection.NodeRemote);
					if (message.To != Network.BroadcastNodeID)
						return;
				}

				TrustedNodeInfo trustedNode = null;
				if (trustedNodes.ContainsKey(message.From)) {
					trustedNode = trustedNodes[message.From];
				}

				object content = message.Content;

				if (message.From == this.LocalNode.NodeID) {
					if (message.To != Network.BroadcastNodeID) {
						// This shouldnt have happened, ever.
						throw new Exception ("Attempt to process our own message. Type: " + message.Type.ToString()); 
					} else {
						// It's normal to receive our own messages again. Routing ain't so smart.
						return;
					}
				}

				Node messageFrom = null;
				
				lock (nodes) {
					messageFrom = GetNode(message.From);

					if (messageFrom == null) {
						// We don't know about this node! Lets add it.. oh, let's do.

						if (trustedNode != null) {
							// If its a trusted node that means we verified the message's signature,
							// so we know its valid (and thus know this node actually exists).
							Node node = new Node (this, message.From);
							node.NickName = trustedNode.Identifier;
							node.Verified = true;
							AddNode(node);
							messageFrom = node;
							
							node.CreateNewSessionKey();

						} else {
							// Even if we can't verify that this node exists, we add it anyway.
							// This has certain potential security issues, such as a flood of messages from random IDs,
							// but there is really no way around that.
							// The node will be marked Verified = false by default, so the GUI can ignore it.
							// (This is added so things like ChatMessages work properly)

							Node node = new Node (this, message.From);
							node.NickName = "[" + message.From + "]";
							AddNode(node);
							messageFrom = node;
						}
					}
				}

				if (trustedNode == null) {
					if (InsecureMessageTypes.IndexOf(message.Type) == -1) {
						this.SendNonCriticalError (messageFrom, new NotTrustedError());
						return;
					}
				}

				// Make sure nobody is trying to screw with us
				if (LocalOnlyMessageTypes.IndexOf (message.Type) > -1) {
					if (messageFrom != connection.NodeRemote) {
						this.SendCriticalError (messageFrom, new MeshworkError ("That message type is only valid for local connections."));
					}
				}

				// Well, if they sent us something we can assume they trust us 
				if (message.To != Network.BroadcastNodeID) {
					messageFrom.RemotelyUntrusted = false;
				}
				
				if (connection != null && (message.From == connection.RemoteNodeInfo.NodeID & message.To == this.LocalNode.NodeID & message.Type == MessageType.CriticalError)) {
					MeshworkError error = ((MeshworkError)(content));
					Core.LoggingService.LogError("RECIEVED CRITICAL ERROR", error.Message);
					if (ReceivedCriticalError != null) {
						ReceivedCriticalError((INodeConnection)connection, error);
					}
					connection.Disconnect (error.ToException ());
					return;
				}

				switch (message.Type) {
					case MessageType.Ping:
						processor.ProcessPingMessage(messageFrom, (ulong)content);
						break;
					case MessageType.Pong:
						connection.ReceivedPong ((ulong)content);
						//processor.ProcessPongMessage(messageFrom, (ulong)content);
						break;
					case MessageType.Auth:
						processor.ProcessAuthMessage(connection, messageFrom, (AuthInfo)content, false);
						break;
					case MessageType.AuthReply:
						processor.ProcessAuthMessage(connection, messageFrom, (AuthInfo)content, true);
						break;
					case MessageType.RequestKey:
						processor.ProcessRequestKeyMessage(messageFrom);
						break;
					case MessageType.MyKey:
						processor.ProcessMyKeyMessage(messageFrom, (KeyInfo)content);
						break;
					case MessageType.RequestInfo:
						processor.ProcessRequestInfoMessage(messageFrom);
						break;
					case MessageType.NewSessionKey:
						processor.ProcessNewSessionKeyMessage(messageFrom, (byte[])content);
						break;
					case MessageType.MyInfo:
						processor.ProcessMyInfoMessage(messageFrom, (NodeInfo)content);
						break;
					case MessageType.NonCriticalError:
						processor.ProcessNonCriticalErrorMessage(messageFrom, 
								(MeshworkError)content);
						break;
					case MessageType.SearchRequest:
						processor.ProcessSearchRequestMessage (messageFrom, (SearchRequestInfo)content);
						break;
					case MessageType.SearchResult:
						processor.ProcessSearchResultMessage (messageFrom, (SearchResultInfo)content);
						break;
					/*
					case MessageType.SendFile:
						processor.ProcessSendFileMessage (messageFrom, (SharedFileInfo)content);
						break;
					*/
					case MessageType.RequestFile:
						processor.ProcessRequestFileMessage(messageFrom, (RequestFileInfo)content);
						break;
					case MessageType.ConnectionDown:
						processor.ProcessConnectionDownMessage (messageFrom, (ConnectionInfo)content);
						break;
					case MessageType.JoinChat:
						processor.ProcessJoinChatMessage (messageFrom, (ChatAction)content);
						break;
					case MessageType.LeaveChat:
						processor.ProcessLeaveChatMessage (messageFrom, (ChatAction)content);
						break;
					case MessageType.ChatInvite:
						processor.ProcessChatInviteMessage (messageFrom, (ChatInviteInfo)content);
						break;
					case MessageType.ChatroomMessage:
						processor.ProcessChatMessage (messageFrom, (ChatMessage)content);
						break;
					case MessageType.PrivateMessage:
						processor.ProcessPrivateMessage (messageFrom, content.ToString());
						break;
					case MessageType.Ready:
						processor.ProcessReadyMessage (connection, messageFrom); //, (NetworkState)content);
						break;	
					case MessageType.AddMemo:
						processor.ProcessAddMemoMessage (messageFrom, (MemoInfo)content);
						break;
					case MessageType.DeleteMemo:
						processor.ProcessDeleteMemoMessage (messageFrom, content.ToString ());
						break;
					case MessageType.RequestDirListing:
						processor.ProcessRequestDirListingMessage (messageFrom, content.ToString());
						break;
					case MessageType.RespondDirListing:
						Core.FileSystem.ProcessRespondDirListingMessage (this, messageFrom, (SharedDirectoryInfo)content);
						break;
					case MessageType.Ack:
						processor.ProcessAckMessage(messageFrom, content.ToString());
						break;
					case MessageType.Hello:
						processor.ProcessHelloMessage (messageFrom, (HelloInfo)content);
						break;
				//	case MessageType.NetworkState:
				//		AppendNetworkState ( content as NetworkState );
				//		//processor.ProcessNetworkStateMessage (
				//		break;
					case MessageType.RequestFileDetails:
						processor.ProcessRequestFileDetails(messageFrom, (string)content);					
						break;
					case MessageType.FileDetails:
						Core.FileSystem.ProcessFileDetailsMessage(this, messageFrom, (SharedFileListing)content);
						break;
					case MessageType.RequestAvatar:
						processor.ProcessRequestAvatarMessage(messageFrom);
						break;
					case MessageType.Avatar:
						processor.ProcessAvatarMessage(messageFrom, (byte[])content);
						break;
					case MessageType.Test:
						// Do nothing here.
						break;
					default:
						Core.LoggingService.LogWarning("Received unhandled Message type: {0}, content: {1}", message.Type, content);
						break;
				}
				

	//			 else if (Message.Type == MessageTypes.networkState) 
	//				AppendNetworkState(((NetworkState)(content)));

	//			//TODO: Why are we checking mesage types
	//			if (message.Type != MessageType.Ack & message.Type != MessageType.Ping & message.Type != MessageType.Pong & message.Type != MessageType.Auth & message.Type != MessageType.AuthReply) {
				if (nodes.ContainsKey(message.From)) {
					if (this.TrustedNodes.ContainsKey(message.From) && MessageTypesToAck.IndexOf(message.Type) > -1) {
						this.SendRoutedMessage((this.MessageBuilder.CreateAckMessage(message.MessageID, messageFrom)));
					}
				}
	//			}
			} catch (Exception ex) {
				// XXX: Better error handling!
				string messageType = (message != null) ? message.Type.ToString() : "(Unknown)";
				string messageFrom = (message != null) ? message.From : "(Unknown)";
				Core.LoggingService.LogError("Network.ProcessMessage: Error processing message of type {0} from {1}: {2}", messageType, messageFrom, ex.ToString());
			}
		}

		internal void Cleanup()
		{
			List<Node> usersToDelete = new List<Node>();
			List<ChatRoom> chatRoomsToDelete = new List<ChatRoom>();

			lock (nodes) {
				foreach (Node n in nodes.Values) {
					if (this.CheckForRoute(this.LocalNode, n) == false & !(n == this.LocalNode)) {
						lock (chatRooms) {
							foreach (ChatRoom r in chatRooms.Values) {
								List<Node> usersInRoomToDelete = new List<Node>();
								lock (r.Users) {
									foreach (Node n1 in r.Users.Values) {
										if (n1 == n) {
											usersInRoomToDelete.Add(n1);
										}
									}
								}

								foreach (Node r2 in usersInRoomToDelete) {
									r.RemoveUser(r2);
									OnLeftChat (new ChatEventArgs (r2, r));
								}

								if (r.Users.Count == 0) {
									chatRoomsToDelete.Add(r);
								}
							}
						}
						usersToDelete.Add(n);
					}
				}
			}

			foreach (ChatRoom room in chatRoomsToDelete) {
				this.RemoveChatRoom(room);
			}

			foreach (Node n in usersToDelete) {
				// XXX: Refactor all this into a DeleteNode() method.
				foreach (INodeConnection c in n.GetConnections ()) {
					if (c != null && this.Connections.Contains(c)) {
						RemoveConnection(c);
					}
				}
				if (this.Nodes.ContainsKey(n.NodeID)) {
					RemoveNode(n);

					lock (memos) {
						List<Memo> memosToRemove = new List<Memo>();
						foreach (Memo m in memos.Values) {
							if (m.Node == n) {
								memosToRemove.Add(m);
							}
						}
						memosToRemove.ForEach(delegate (Memo m) { RemoveMemo(m); });
						memosToRemove = null;
					}
					
					Core.LoggingService.LogInfo("{0} has disconnected from the network.", n);
			
					if (UserOffline != null) {
						UserOffline(this, n);
					}
				}

				Cleanup();
			}
			if (CleanupFinished != null) {
				CleanupFinished(this, new EventArgs());
			}
		}

		private void ProcessNewConnection(ConnectionInfo connection)
		{
			lock (nodes) {
				Node DestNode = GetNode(connection.DestNodeID);
				Node SourceNode = GetNode(connection.SourceNodeID);

				if (DestNode != this.LocalNode & SourceNode != this.LocalNode) {
					if (DestNode != SourceNode) {

						if (this.FindConnection(connection.SourceNodeID, connection.DestNodeID) == null) {

							if (SourceNode == null) {
								SourceNode = new Node(this, connection.SourceNodeID);
								SourceNode.NickName = connection.SourceNodeNickname;
								AddNode(SourceNode);
								if (this.TrustedNodes.ContainsKey(SourceNode.NodeID)) {
									SourceNode.CreateNewSessionKey();
								}
							} else {
								SourceNode.NickName = connection.SourceNodeNickname;
							}
							
							if (DestNode == null) {
								DestNode = new Node(this, connection.DestNodeID);
								DestNode.NickName = connection.DestNodeNickname;
								AddNode(DestNode);
								if (this.TrustedNodes.ContainsKey(DestNode.NodeID)) {
									DestNode.CreateNewSessionKey();
								}
							} else {
								DestNode.NickName = connection.DestNodeNickname;
							}

							RemoteNodeConnection c = new RemoteNodeConnection(this);
							c.NodeLocal = SourceNode;
							c.NodeRemote = DestNode;
							c.ConnectionState = ConnectionState.Remote;
							AddConnection(c);
						} else {
							// I am not really sure if this is actually useful...
							SourceNode.NickName = connection.SourceNodeNickname;
							DestNode.NickName = connection.DestNodeNickname;
						}
					} else {
						Core.LoggingService.LogWarning("Someone told me about an invalid connection - both sides are the same?! Thats no good!! " + connection.SourceNodeNickname + " <-> " + connection.DestNodeNickname);
					}
				} else {
					if (this.FindConnection(connection.SourceNodeID, connection.DestNodeID) == null) {
						Core.LoggingService.LogWarning("THAT CONNECTION DOESNT EXIST!!!" + connection.SourceNodeNickname + " <-> " + connection.DestNodeNickname);
						this.SendBroadcast(this.MessageBuilder.CreateConnectionDownMessage(connection.SourceNodeID, connection.DestNodeID), this.LocalNode);
					}
				}
			}
		}

	
		internal void RaiseUpdateNodeInfo (string oldNickname, Node node)
		{
			if (oldNickname != node.NickName)
				Core.LoggingService.LogInfo("{0} has changed their nickname to {1}.", oldNickname, node.NickName);
				
			if (UpdateNodeInfo != null) {
				UpdateNodeInfo(this, oldNickname, node);
			}
		}

		internal void RaiseReceivedNonCriticalError (Node from, MeshworkError error)
		{
			Core.LoggingService.LogWarning("RECEIVE NONCRITICALERROR: " + error.Message);
			
			if (ReceivedNonCriticalError != null)
				ReceivedNonCriticalError(this, from, error);
		}

		protected virtual void OnMemoDeleted (Memo memo)
		{
			Core.LoggingService.LogInfo("Memo deleted: " + memo.Subject);

			if (MemoDeleted != null) {
				MemoDeleted (this, memo);
			}
		}

		internal void RaiseReceivedDirListing (Node node, RemoteDirectory directory)
		{
			if (ReceivedDirListing != null)
				ReceivedDirListing(this, node, directory);
	    }
		
		internal void RaiseReceivedFileDetails (RemoteFile file)
		{
			if (ReceivedFileDetails != null)
				ReceivedFileDetails(this, file);
		}
	       
		internal void RaiseLeftChat (Node node, ChatRoom room)
		{
			OnLeftChat (new ChatEventArgs (node, room));
		}
			
		protected virtual void OnLeftChat (ChatEventArgs args)
		{
			Core.LoggingService.LogInfo("{0} has left {1}", args.Node.NickName, args.Room.Name);
		    
			if (LeftChat != null) {
				LeftChat (this, args);
			}
		}
		
		protected virtual void OnMemoUpdated (Memo memo)
		{
			Core.LoggingService.LogInfo("Memo updated: {0} by {1}.", memo.Subject, memo.Node);
			
			if (MemoUpdated != null) {
				MemoUpdated (this, memo);
			}
		}

		protected virtual void OnMemoAdded (Memo memo)
		{
			Core.LoggingService.LogInfo("Memo added: {0} by {1}", memo.Subject, memo.Node);
			
			if (MemoAdded != null) {
				MemoAdded(this, memo);
			}
		}

		internal void RaiseJoinedChat (Node node, ChatRoom room)
		{
			OnJoinedChat (new ChatEventArgs (node, room));
		}

		protected virtual void OnJoinedChat (ChatEventArgs args)
		{
			Core.LoggingService.LogInfo("{0} has joined {1}", args.Node.NickName, args.Room.Name);

			if (JoinedChat != null) {
				JoinedChat (this, args);
			}
		}

		internal void RaisePrivateMessage (Node messageFrom, string messageText)
		{
			if (PrivateMessage != null)
				PrivateMessage (this, messageFrom, messageText);
		}
	
		/*
		internal void RaiseFileOffered (Node messageFrom, SharedFileInfo file)
		{
			OnFileOffered (new FileOfferedEventArgs (messageFrom, file));
		}

		protected virtual void OnFileOffered (FileOfferedEventArgs args)
		{
			if (FileOffered != null) {
				FileOffered (this, args);
			}
		}
		*/
		
		internal void RaiseChatMessage (ChatRoom room, Node messageFrom, string messageText)
		{
			if (ChatMessage != null) 
				ChatMessage(room, messageFrom, messageText);
		}

		internal void RaiseReceivedChatInvite (Node from, ChatInviteInfo invitation)
		{
			if (ReceivedChatInvite != null) {
				if (chatRooms.ContainsKey(invitation.RoomId)) {
					ChatRoom room = chatRooms[invitation.RoomId];
					ReceivedChatInvite(this, from, room, invitation);
				} else {
					Core.LoggingService.LogWarning("Ignored invitation for non-existent chatroom: {0}", invitation.RoomName);
				}
			}
		}

		internal bool RaiseReceivedKey (LocalNodeConnection connection, KeyInfo key)
		{
			return OnReceivedKey(new ReceivedKeyEventArgs(connection, key));
		}
			
		internal bool RaiseReceivedKey (Node node, KeyInfo key)
		{
			return OnReceivedKey(new ReceivedKeyEventArgs(node, key));
		}

		protected virtual bool OnReceivedKey (ReceivedKeyEventArgs args)
		{
			if (ReceivedKey != null) {
				return ReceivedKey (this, args);
			} else {
				return false;
			}
		}

		internal void RaiseReceivedSearchResult (Node node, SearchResultInfo result)
		{
			if (ReceivedSearchResult != null) {
				ReceivedSearchResult(this, new SearchResultInfoEventArgs(node, result));
			}
		}

		internal void RaiseReceivedAvatar (Node node, byte[] avatarData)
		{
			if (ReceivedAvatar != null) {
				ReceivedAvatar(this, node, avatarData);
			}
		}

		public void ConnectTo (ITransport transport)
		{
			ConnectTo(transport, null);
		}

		public void ConnectTo (ITransport transport, TransportCallback callback)
		{
			transport.Network = this;
			Core.ConnectTo(transport, callback);
		}

		public IDictionary<string, AckMethod> AckMethods {
			get {
				return ackMethods;
			}
		}
	}
}
