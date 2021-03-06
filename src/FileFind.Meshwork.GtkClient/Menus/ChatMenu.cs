//
// ChatMenu: Chat room context menu
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net (http://filefind.net)
//

using Gtk;
using Glade;
using System;

namespace FileFind.Meshwork.GtkClient
{
	public class ChatMenu
	{
		[Widget] MenuItem mnuChatJoinRoom;
		[Widget] SeparatorMenuItem mnuChatJoinRoomSeporator;
		[Widget] MenuItem mnuChatCreateNewChatroom;
		[Widget] Statusbar statusBar;
		Gtk.Menu mnuChat;
		ChatRoom selectedRoom;

		public ChatMenu ()
		{
			Glade.XML xmlMnuChat = new Glade.XML(null, "FileFind.Meshwork.GtkClient.meshwork.glade","mnuChat",null); 
			mnuChat = (xmlMnuChat.GetWidget("mnuChat") as Gtk.Menu);
			xmlMnuChat.Autoconnect(this);
		}

		public void Popup (ChatRoom selectedRoom)
		{
			this.selectedRoom = selectedRoom;
			mnuChat.Popup ();
		}

		private void on_mnuChat_show (object o, EventArgs args)
		{
			if (selectedRoom != null) {
				if (selectedRoom.InRoom == true) {
					(mnuChatJoinRoom.Child as Gtk.Label).Markup = "<b>Show " + selectedRoom.Name + "</b>";
				} else {
					(mnuChatJoinRoom.Child as Gtk.Label).Markup = "<b>Join " + selectedRoom.Name + "</b>";
				}
				mnuChatJoinRoom.Visible = true;
				mnuChatJoinRoomSeporator.Visible = true;
			} else {
				mnuChatJoinRoom.Visible = false;
				mnuChatJoinRoomSeporator.Visible = false;
			}
		}

		public void on_mnuChatJoinRoom_activate (object o, EventArgs e)
		{
			if (selectedRoom.InRoom == false) {
				Gui.JoinChatRoom(selectedRoom);
			} else {
				(selectedRoom.Properties["Window"] as ChatRoomSubpage).GrabFocus();
			}
		}

		public void on_mnuChatCreateNewChatroom_activate (object o, EventArgs e)
		{
			JoinChatroomDialog w = new JoinChatroomDialog (Gui.MainWindow.Window);		
			w.Run ();
		}

	}
}
