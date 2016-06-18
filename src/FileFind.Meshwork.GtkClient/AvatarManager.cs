//
// AvatarManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Gdk;
using Meshwork.Logging;

namespace FileFind.Meshwork.GtkClient
{
	public class AvatarManager : IAvatarManager
	{
		Dictionary<String, Pixbuf> avatars = new Dictionary<String, Pixbuf>();
		Dictionary<String, Pixbuf> smallAvatars = new Dictionary<String, Pixbuf>();
		Dictionary<String, Pixbuf> miniAvatars = new Dictionary<String, Pixbuf>();
		long      avatarSize = 0;
		string    avatarsPath;

		Pixbuf genericAvatar;
		Pixbuf smallGenericAvatar;
		Pixbuf miniGenericAvatar;
		
		public event EventHandler AvatarsChanged;

		static AvatarManager instance = null;

		public AvatarManager ()
		{
			if (instance != null) {
				throw new Exception ("One instance please!");
			} else {
				instance = this;
			}
			
			genericAvatar = new Pixbuf(null, "FileFind.Meshwork.GtkClient.avatar-generic-large.png");
			smallGenericAvatar = new Pixbuf(null, "FileFind.Meshwork.GtkClient.avatar-generic-medium.png");
			miniGenericAvatar = new Pixbuf(null, "FileFind.Meshwork.GtkClient.avatar-generic-small.png");

			avatarsPath = Path.Combine (Settings.ConfigurationDirectory, "avatars");

			if (Directory.Exists (avatarsPath) == false) {
				Directory.CreateDirectory (avatarsPath);
			}

			foreach (Network network in Core.Networks) {
				AddNetwork (network);
			}

			Core.NetworkAdded += AddNetwork;

			UpdateMyAvatar ();
		}
		
		private void AddNetwork (Network network)
		{
			network.UpdateNodeInfo += (UpdateNodeInfoEventHandler)DispatchService.GuiDispatch(new UpdateNodeInfoEventHandler(network_UpdateNodeInfo));
			network.UserOffline    += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOffline));
			network.ReceivedAvatar += (AvatarEventHandler)DispatchService.GuiDispatch(new AvatarEventHandler(network_ReceivedAvatar));
		
			network.LocalNode.AvatarSize = this.avatarSize;
		}

		public void UpdateMyAvatar ()
		{
			string myAvatarFile = Path.Combine(avatarsPath, String.Format("{0}.png", Core.MyNodeID));

			if (File.Exists (myAvatarFile)) {

				var origPixbuf = new Pixbuf(myAvatarFile);
				avatars[Core.MyNodeID] = origPixbuf;
				
				var pixbuf = origPixbuf.ScaleSimple(22,22, InterpType.Hyper);
				smallAvatars[Core.MyNodeID] = pixbuf;
				
				pixbuf = origPixbuf.ScaleSimple(16, 16, InterpType.Hyper);
				miniAvatars[Core.MyNodeID] = pixbuf;

				this.avatarSize = new FileInfo(myAvatarFile).Length;

				foreach (Network network in Core.Networks) {
					network.LocalNode.AvatarSize = this.avatarSize;
				}
			} else {
				foreach (Network network in Core.Networks) {
					network.LocalNode.AvatarSize = 0;
				}
			}
			
			if (AvatarsChanged != null) {
				AvatarsChanged(this, EventArgs.Empty);
			}
		}

		private void network_ReceivedAvatar (Network network, Node node, byte[] avatarData)
		{
			try {
				string dest = GetAvatarPath(node);

				using (FileStream stream = new FileStream(dest, FileMode.Create)) {
					stream.Write(avatarData, 0, avatarData.Length);
				}

				LoadAvatar(dest, node);
			} catch (Exception ex) {
				//TODO: What to do here?
				Core.LoggingService.LogError(ex);
			}
		}

		private void network_UpdateNodeInfo (Network network, string oldnickname, Node node)
		{
			if (node == network.LocalNode) {
				return;
			}

			FileInfo existingFile = new FileInfo(GetAvatarPath(node));
			if (existingFile.Exists) {
				LoadAvatar(existingFile.FullName, node);
			}

			if (node.GetTrustedNode() != null && node.AvatarSize > 0 && (!existingFile.Exists || node.AvatarSize != existingFile.Length)) {
				network.RequestAvatar(node);
			}
		}

		private void LoadAvatar (string filePath, Node node)
		{
			// If we have never loaded this user's avatar this will do nothing
			RemoveAvatars(node.NodeID);

			if (File.Exists(filePath)) {
				var origAvatar = new Pixbuf(filePath);
				avatars.Add(node.NodeID, origAvatar);
				
				var pixbuf = origAvatar.ScaleSimple(22,22, InterpType.Hyper);
				smallAvatars.Add(node.NodeID, pixbuf);
				
				pixbuf = origAvatar.ScaleSimple(16, 16, InterpType.Hyper);
				miniAvatars.Add(node.NodeID, pixbuf);
			}
		}

		public string AvatarsPath {
			get {
				return avatarsPath;
			}
		}
		
		private void network_UserOffline (Network network, Node node)
		{
			if (node != network.LocalNode) {
				RemoveAvatars(node.NodeID);
			}
		}
		
		private void RemoveAvatars(string nodeid)
		{
			if (avatars.ContainsKey (nodeid) == true)
				avatars.Remove (nodeid);
			
			if (smallAvatars.ContainsKey (nodeid) == true)
				smallAvatars.Remove (nodeid);
			
			if (miniAvatars.ContainsKey(nodeid))
				miniAvatars.Remove(nodeid);

			if (AvatarsChanged != null)
				AvatarsChanged (this, EventArgs.Empty);
		}

		public Pixbuf GetAvatar (string nodeID)
		{
			if (avatars.ContainsKey(nodeID) == true)
				return avatars[nodeID];
			else
				return genericAvatar;
		}

		public Pixbuf GetAvatar (Node node)
		{
			return GetAvatar(node.NodeID);
		}

		public Pixbuf GetSmallAvatar (string nodeID)
		{
			if (smallAvatars.ContainsKey(nodeID) == true)
				return smallAvatars[nodeID];
			else
				return smallGenericAvatar;
		}

		public Pixbuf GetSmallAvatar (Node node)
		{
			return GetSmallAvatar(node.NodeID);
		}
		
		public Pixbuf GetMiniAvatar (string nodeID)
		{
			if (miniAvatars.ContainsKey(nodeID))
				return miniAvatars[nodeID];
			else
				return miniGenericAvatar;
		}
		
		public Pixbuf GetMiniAvatar (Node node)
		{
			return GetMiniAvatar(node.NodeID);
		}

		public byte[] GetAvatarBytes (string nodeId)
		{
			Pixbuf pixbuf = GetAvatar(nodeId);
			return pixbuf.SaveToBuffer("png");
		}

		public byte[] GetSmallAvatarBytes (string nodeId)
		{
			Pixbuf pixbuf = GetSmallAvatar(nodeId);
			return pixbuf.SaveToBuffer("png");
		}
		
		public byte[] GetAvatarBytes (Node node)
		{
			return GetAvatarBytes(node.NodeID);
		}

		public byte[] GetSmallAvatarBytes (Node node)
		{
			return GetSmallAvatarBytes(node.NodeID);
		}

		private string GetAvatarPath (Node node) 
		{
			return Path.Combine(avatarsPath, String.Format("{0}.png", node.NodeID));
		}
	}
}
