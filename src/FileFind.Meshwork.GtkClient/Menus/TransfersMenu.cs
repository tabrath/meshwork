//
// TransfersMenu.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using FileFind.Meshwork;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.GtkClient.Windows;

namespace FileFind.Meshwork.GtkClient
{
	public class TransfersMenu 
	{
		Gtk.Menu menu;
		
		Gtk.TreeView  transfersList;
		IFileTransfer transfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuShowTransferDetails;
		
		[Glade.Widget]
		Gtk.MenuItem mnuPauseTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuResumeTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuCancelTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuCancelAndRemoveTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuClearFinishedFailedTransfers;
		
		
		public TransfersMenu(TreeView transfersList, IFileTransfer transfer)
		{
			Glade.XML glade = new Glade.XML(null, "FileFind.Meshwork.GtkClient.meshwork.glade", "TransfersMenu", null);
			glade.Autoconnect(this);
			this.menu = (Gtk.Menu) glade.GetWidget("TransfersMenu");
			
			this.transfersList = transfersList;
			this.transfer = transfer;
			
			if (transfer != null) {
				mnuCancelAndRemoveTransfer.Visible = true;
				mnuShowTransferDetails.Sensitive = true;
				mnuClearFinishedFailedTransfers.Sensitive = true;
				if (transfer.Status == FileTransferStatus.Paused) {
					mnuPauseTransfer.Visible = false;
					mnuResumeTransfer.Visible = true;
					mnuResumeTransfer.Sensitive = true;
					mnuCancelTransfer.Sensitive = true;
				} else if (transfer.Status == FileTransferStatus.Canceled || transfer.Status == FileTransferStatus.Completed) {
					mnuPauseTransfer.Sensitive = false;
					mnuResumeTransfer.Visible = false;
					mnuCancelTransfer.Sensitive = false;
				}
			} else {
				mnuCancelAndRemoveTransfer.Visible = false;
				mnuShowTransferDetails.Sensitive = false;
				mnuPauseTransfer.Sensitive = false;
				mnuResumeTransfer.Visible = false;
				mnuCancelTransfer.Sensitive = false;
			}
		}
		
		public void Popup() 
		{
			menu.Popup();
		}
		
		public void on_mnuShowTransferDetails_activate (object o, EventArgs args) 
		{
			FileTransferWindow window = new FileTransferWindow(transfer);
			window.Show();
		}
		
		public void on_mnuPauseTransfer_activate (object o, EventArgs args) 
		{
			try {
				transfer.Pause();
			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
		public void on_mnuResumeTransfer_activate (object o, EventArgs args)
		{
			try {
				transfer.Resume();
			} catch (Exception ex) {
				Gui.ShowErrorDialog(ex.ToString ());
			}
		}
		
		public void on_mnuCancelTransfer_activate (object o, EventArgs args)
		{
			try {
				transfer.Cancel();
			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
		public void on_mnuCancelAndRemoveTransfer_activate (object o, EventArgs args) 
		{
			Core.FileTransferManager.RemoveTransfer(transfer);
		}

		public void on_mnuClearFinishedFailedTransfers_activate (object o, EventArgs args) 
		{
			try {
				List<IFileTransfer> toRemove = new List<IFileTransfer>();
				foreach (IFileTransfer transfer in Core.FileTransferManager.Transfers) {
					if (transfer.Status == FileTransferStatus.Canceled || transfer.Status == FileTransferStatus.Completed) {
						toRemove.Add(transfer);
					}
				}

				toRemove.ForEach(delegate (IFileTransfer transfer) { Core.FileTransferManager.RemoveTransfer(transfer); });

			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
	}
}
