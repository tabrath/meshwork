//
// Runtime.cs:
//
// Authors:
//   Eric Butler <eric@filefind.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// Software), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Gtk;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.IO;
using Mono.Unix;
using FileFind.Meshwork.GtkClient.Windows;
using Meshwork.Logging;

namespace FileFind.Meshwork.GtkClient
{
	public class Runtime
	{
		static SplashWindow splashWindow;
		static BuiltinActionGroup builtin_actions;
		static UIManager ui_manager;
		static GtkMeshworkOptions options;
		static TrayIcon trayIcon;

		private static Settings tmpSettings;

		public static void Main (string[] args)
		{
			/* Initialize our catalog */
			//   Catalog.Init (Defines.Name, Defines.LocaleDir);

			/* Process our args */
			options = new GtkMeshworkOptions ();
			options.ProcessArgs (args);
			
			Common.SetProcessName("meshwork-gtk");

			/* Initialize the GTK application */
			Gtk.Application.Init();
			
			if (!System.Diagnostics.Debugger.IsAttached) {
				/* If we crash, attempt to log the error */
				GLib.ExceptionManager.UnhandledException += UnhandledGLibExceptionHandler;
				AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			}
			
			//XXX: Implement Gunique code here!

			splashWindow = new SplashWindow();
			splashWindow.Show();

			/* Load settings */
			if (options.ConfigPath != null) {
				Core.LoggingService.LogDebug("Using config dir: " + options.ConfigPath);
				Settings.OverrideConfigPath(options.ConfigPath);
			}
			tmpSettings = Settings.ReadSettings();

			// First run, create initial settings.
			if (tmpSettings == null || !tmpSettings.HasKey) {
				tmpSettings = new Settings();
				tmpSettings.NickName = Core.OS.UserName;
				tmpSettings.RealName = Core.OS.RealName;
				tmpSettings.IncompleteDownloadDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				tmpSettings.CompletedDownloadDir  = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				tmpSettings.SetFirstRun(true);
			}
		
			/* Load Icons */
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				new Gdk.Pixbuf(null, "FileFind.Meshwork.GtkClient.tray_icon.png")
			};
		
			// Windows specific. Override stock icons to use embeded files.
			var sizes = new [] { 16, 22, 24, 34 };
			var iconNames = new[] { 
				"application-exit", "application-x-executable", "audio-x-generic", 
				"computer", "dialog-error", "dialog-information", "dialog-password", 
				"gtk-preferences", "dialog-question",  "dialog-warning", "folder", 
				"go-down", "go-home", "go-next", "go-previous", "go-up", "image-x-generic",
				"internet-group-chat", "list-add", "list-remove", "mail-attachment", 
				"mail_generic", "mail-message-new", "mail-signed-verified", 
				"network-transmit-receive", "stock_channel", "stock_internet", 
				"system-search", "text-x-generic", "user-home", "video-x-generic",
				"view-refresh", "x-office-document"
			};
			
			foreach (var size in sizes) {
				foreach (var iconName in iconNames) {
					
					if (Environment.OSVersion.Platform != PlatformID.Unix ||
					    !IconTheme.Default.HasIcon(iconName) || 
					    !IconTheme.Default.GetIconSizes(iconName).Contains(size)) 
					{					
						var pixbuf = Gui.LoadIconFromResource(iconName, size);
						if (pixbuf != null)
							Gtk.IconTheme.AddBuiltinIcon(iconName, size, pixbuf);
						else
							Core.LoggingService.LogWarning("Missing embeded icon: {0} ({1}x{1})", iconName, size);
					}
				}
			}
			
			/* Set up UI actions */
			builtin_actions = new BuiltinActionGroup ();
			ui_manager = new UIManager ();
			ui_manager.InsertActionGroup (builtin_actions, 0);
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.MainWindow.xml");
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.TrayPopupMenu.xml");
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.SearchPopupMenu.xml");
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.MapPopupMenu.xml");

			/* Create the Tray Icon */
			trayIcon = new TrayIcon();
			
			/* Start the event loop */
			GLib.Idle.Add (new GLib.IdleHandler (FinishLoading));
			Gtk.Application.Run ();
		}

		public static UIManager UIManager {
			get {
				return ui_manager;
			}
		}

		public static BuiltinActionGroup BuiltinActions {
			get {
				return builtin_actions;
			}
		}

		private static bool FinishLoading ()
		{
			Core.Started += (EventHandler)DispatchService.GuiDispatch(new EventHandler(Core_Started));

			if (tmpSettings.FirstRun) {
				Core.LoggingService.LogDebug("First run");

				// Generate key
				if (!tmpSettings.HasKey) {
					GenerateKeyDialog keyDialog = new GenerateKeyDialog (null);
					keyDialog.Run();
					tmpSettings.SetKey(keyDialog.Key);
				}
				
				/* Init the core */
				if (!Core.Init(tmpSettings)) {
					throw new Exception("Core failed to init on first run!");
				}
				
				/* Show change password dialog */
				var dialog = new ChangeKeyPasswordDialog(splashWindow.Window);
				dialog.Run();
				
				splashWindow.Close();

				PreferencesDialog preferences = new PreferencesDialog ();
				if (preferences.Run () != (int)ResponseType.Ok) {
					// Abort !!
					Gtk.Application.Quit();
					Environment.Exit(1);
					return false;
				}
				Core.ReloadSettings();
			} else {
				/* Init the core */
				
				Core.PasswordPrompt += delegate {
					var dialog = new FileFind.Meshwork.GtkClient.Windows.UnlockKeyDialog(splashWindow.Window);
					dialog.Run();
				};

				bool didInit = Core.Init(tmpSettings);
				if (!didInit) {
					// Right now this means the password dialog was aborted, or already running.
					Gtk.Application.Quit();
					Environment.Exit(1);
					return false;					
				}
			}

			tmpSettings = null;

			Core.AvatarManager = (IAvatarManager) new AvatarManager();
			
			/* Load the gui */
			Gui.MainWindow = new MainWindow ();

			Gdk.Screen screen = Gdk.Screen.Default;
		
			/*
			if (Common.OSName == "Linux") {
				Gdk.Colormap colormap = screen.RgbaColormap;
				if (colormap != null) {
					Widget.DefaultColormap = colormap;
					Gtk.Widget.PushColormap(colormap);
				}
			}
			*/
			
			splashWindow.Close();

			if ((!Gui.Settings.StartInTray && options.MainWindowState != "hidden") ||
			    (Gui.Settings.StartInTray && (options.MainWindowState == "shown" | options.MainWindowState == "iconified"))) {
				Gui.MainWindow.Show();
				if (options.MainWindowState == "iconified") {
					Gui.MainWindow.Iconify();
				}
			}

			Thread thread = new Thread(delegate () {
				Core.Start();
			});
			thread.Start();

			return false;
		}

		public static bool QuitMeshwork()
		{
			try {
				int result = Gui.ShowMessageDialog ("Are you sure you want to quit Meshwork?", Gui.MainWindow.Window, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo);
				if (result == (int)ResponseType.Yes) {
					Gui.Settings.SaveSettings ();
					Core.Stop();
					Gtk.Application.Quit();
					Environment.Exit(0);
					return true;
				} else {
					return false;
				}
			} catch (Exception ex) {
				Core.LoggingService.LogError(ex);
				throw ex;
			}
		}
		
		private static void UnhandledExceptionHandler (object sender, UnhandledExceptionEventArgs args)
		{
			Console.Error.WriteLine("UNHANDLED EXCEPTION!! " + args.ExceptionObject.ToString());
			string crashFileName = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), String.Format("meshwork-crash-{0}.log", DateTime.Now.ToFileTime()));
			string crashLog = args.ExceptionObject.ToString();
			File.WriteAllText(crashFileName, crashLog);
		}
		
		private static void UnhandledGLibExceptionHandler (GLib.UnhandledExceptionArgs args) 
		{
			string exceptionText = args.ExceptionObject.ToString();
			
			Console.Error.WriteLine("UNHANDLED EXCEPTION!! " + exceptionText);
			string crashFileName = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), String.Format("meshwork-crash-{0}.log", DateTime.Now.ToFileTime()));
			string crashLog = exceptionText;
			File.WriteAllText(crashFileName, crashLog);
			
			Gui.ShowErrorDialog("Meshwork has encountered an unhandled error and must be closed.\n\nAn error report has been created on your desktop, please file a bug.\n\n" + exceptionText);
			
			args.ExitApplication = true;			
		}

		private static void Core_Started (object sender, EventArgs args)
		{
			if (Core.FailedTransportListeners.Length > 0) {
				StartupProblemsDialog dialog = new StartupProblemsDialog();
				dialog.Run();
			}

		}
	}
}
