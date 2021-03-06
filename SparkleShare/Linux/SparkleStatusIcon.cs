//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using SparkleLib;

using Gtk;
#if HAVE_APP_INDICATOR
using AppIndicator;
#endif

namespace SparkleShare {

    public class SparkleStatusIcon {

        public SparkleStatusIconController Controller = new SparkleStatusIconController ();

        private Menu menu;
        private MenuItem recent_events_item;
        private MenuItem quit_item;
        private MenuItem state_item;

		private static Image folderIcon = new Image (IconTheme.Default.LoadIcon ("folder", 16, IconLookupFlags.GenericFallback));
		private static Image syncingUpIcon = new Image (IconTheme.Default.LoadIcon ("folder-syncing-up", 16, IconLookupFlags.GenericFallback));
		private static Image syncingDownIcon = new Image (IconTheme.Default.LoadIcon ("folder-syncing-down", 16, IconLookupFlags.GenericFallback));
		private static Image errorIcon = new Image (IconTheme.Default.LoadIcon ("folder-syncing-error", 16, IconLookupFlags.GenericFallback));
		private static Image warningIcon = new Image (IconTheme.Default.LoadIcon ("dialog-warning", 16, IconLookupFlags.GenericFallback));

		private List<ImageMenuItem> folderItems = new List<ImageMenuItem>();

        #if HAVE_APP_INDICATOR
        private ApplicationIndicator indicator;
        #else
        private StatusIcon status_icon;
        #endif


        public SparkleStatusIcon ()
        {
            #if HAVE_APP_INDICATOR
            this.indicator = new ApplicationIndicator ("sparkleshare", "sparkleshare", Category.ApplicationStatus);
            this.indicator.IconName = "process-syncing-idle";
            this.indicator.Status   = Status.Active;
            #else
			this.status_icon          = new StatusIcon ();
            this.status_icon.IconName = "sparkleshare";

            this.status_icon.Activate  += ShowMenu; // Primary mouse button click
            this.status_icon.PopupMenu += ShowMenu; // Secondary mouse button click
            #endif

            CreateMenu ();

            Controller.UpdateIconEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    switch (state) {
                    case IconState.Idle: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-idle";
                        #else
                        this.status_icon.IconName = "sparkleshare";
                        #endif
                        break;
                    }
                    case IconState.SyncingUp: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-up";
                        #else
                        this.status_icon.IconName = "process-syncing-up";
                        #endif
                        break;
                    }
                    case IconState.SyncingDown: {                   
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-down";
                        #else
                        this.status_icon.IconName = "process-syncing-down";
                        #endif
                        break;
                    }
                    case IconState.Syncing: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing";
                        #else
                        this.status_icon.IconName = "process-syncing";
                        #endif
                        break;
                    }
                    case IconState.Error: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-error";
                        #else
                        this.status_icon.IconName = "process-syncing-error";
                        #endif
                        break;
                    }
                    }

                    #if HAVE_APP_INDICATOR
                    // Force update of the status icon
                    this.indicator.Status = Status.Attention;
                    this.indicator.Status = Status.Active;
                    #endif
                });
            };

			Controller.UpdateFolderIconEvent += delegate (SyncStatus[] folderStates) {
                Application.Invoke (delegate {
					int i = 0;
					foreach(ImageMenuItem item in this.folderItems) {
						if (i >= folderStates.Length)
							break;

						switch(folderStates[i]) {
						case SyncStatus.Idle:
							item.Image = folderIcon;
							break;
						case SyncStatus.SyncUp:
							item.Image = syncingUpIcon;
							break;
						case SyncStatus.SyncDown:
							item.Image = syncingDownIcon;
							break;
						case SyncStatus.Error:
							item.Image = errorIcon;
							break;
						}

	                    item.ShowAll ();

						i++;
					}
                });
			};

            Controller.UpdateStatusItemEvent += delegate (string state_text) {
                Application.Invoke (delegate {
                    (this.state_item.Child as Label).Text = state_text;
                    this.state_item.ShowAll ();
                });
            };

            Controller.UpdateQuitItemEvent += delegate (bool item_enabled) {
                Application.Invoke (delegate {
                    this.quit_item.Sensitive = item_enabled;
                    this.quit_item.ShowAll ();
                });
            };

            Controller.UpdateMenuEvent += delegate (IconState state) {
                Application.Invoke (delegate { CreateMenu (); });
            };
        }


        public void CreateMenu ()
        {
            this.menu        = new Menu ();
            this.state_item  = new MenuItem (Controller.StateText) { Sensitive = false };
			this.folderItems = new List<ImageMenuItem>();

            ImageMenuItem folder_item = new SparkleMenuItem ("SparkleShare");
            folder_item.Image = new Image (SparkleUIHelpers.GetIcon ("sparkleshare", 16));

            this.menu.Add (this.state_item);
            this.menu.Add (new SeparatorMenuItem ());                
            this.menu.Add (folder_item);

            if (Program.Controller.Folders.Count > 0) {
                int i = 0;
                foreach (string folder_name in Controller.Folders) {
                    ImageMenuItem item = new SparkleMenuItem (folder_name);

                    if (!string.IsNullOrEmpty (Controller.FolderErrors [i])) {
						item.Image = warningIcon;
                        item.Submenu = new Menu ();
                            
                        MenuItem error_item = new MenuItem (Controller.FolderErrors [i]) { Sensitive = false };
                        MenuItem try_again_item = new MenuItem ("Try Again");
                        try_again_item.Activated += Controller.TryAgainDelegate (folder_name);

                        (item.Submenu as Menu).Add (error_item);
                        (item.Submenu as Menu).Add (new SeparatorMenuItem ());
                        (item.Submenu as Menu).Add (try_again_item);

                    } else {
						item.Image = folderIcon;
                        item.Activated += Controller.OpenFolderDelegate (folder_name);
                    }

                    (item.Child as Label).UseUnderline = false;
                    this.menu.Add (item);
					this.folderItems.Add(item);
                    
                    i++;
                }
            }

			this.recent_events_item = new MenuItem ("Recent Changes…");
			this.recent_events_item.Sensitive = Controller.RecentEventsItemEnabled;
            this.quit_item    = new MenuItem ("Quit") { Sensitive = Controller.QuitItemEnabled };
            MenuItem add_item = new MenuItem ("Add Hosted Project…");
            MenuItem notify_item;
                                                             
            if (Program.Controller.NotificationsEnabled)
                notify_item = new MenuItem ("Turn Notifications Off");
            else
                notify_item = new MenuItem ("Turn Notifications On");

            notify_item.Activated += delegate {
                Program.Controller.ToggleNotifications ();

				Application.Invoke (delegate {				
				    if (Program.Controller.NotificationsEnabled)
                    	(notify_item.Child as Label).Text = "Turn Notifications Off";
                	else
                    	(notify_item.Child as Label).Text = "Turn Notifications On";
				});
            };

            MenuItem link_code_item = new MenuItem ("Client ID");
            
            if (Controller.LinkCodeItemEnabled) {
                link_code_item.Submenu = new Menu ();
                
                string link_code = Program.Controller.CurrentUser.PublicKey.Substring (0, 20) + "...";
                MenuItem code_item = new MenuItem (link_code) { Sensitive = false };
                
                MenuItem copy_item = new MenuItem ("Copy to Clipboard");
                copy_item.Activated += delegate { Controller.CopyToClipboardClicked (); };
                
                (link_code_item.Submenu as Menu).Add (code_item);
                (link_code_item.Submenu as Menu).Add (new SeparatorMenuItem ());
                (link_code_item.Submenu as Menu).Add (copy_item);
            }

            MenuItem about_item = new MenuItem ("About SparkleShare");
            
            about_item.Activated              += delegate { Controller.AboutClicked (); };
            add_item.Activated                += delegate { Controller.AddHostedProjectClicked (); };
			this.recent_events_item.Activated += delegate { Controller.RecentEventsClicked (); };
            this.quit_item.Activated          += delegate { Controller.QuitClicked (); };

            folder_item.Submenu = new Menu ();
			(folder_item.Submenu as Menu).Add (this.recent_events_item);
            (folder_item.Submenu as Menu).Add (add_item);
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (notify_item);
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (link_code_item);
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (about_item);

            this.menu.Add (new SeparatorMenuItem ());            
            this.menu.Add (this.quit_item);
            this.menu.ShowAll ();

            #if HAVE_APP_INDICATOR
            this.indicator.Menu = this.menu;
            #endif
        }
		

        #if !HAVE_APP_INDICATOR
        // Makes the menu visible
        private void ShowMenu (object o, EventArgs args)
        {
            this.menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);
        }


        // Makes sure the menu pops up in the right position
        private void SetPosition (Menu menu, out int x, out int y, out bool push_in)
        {
            StatusIcon.PositionMenu (menu, out x, out y, out push_in, this.status_icon.Handle);
        }
        #endif
    }

    
    public class SparkleMenuItem : ImageMenuItem {

        public SparkleMenuItem (string text) : base (text)
        {
            SetProperty ("always-show-image", new GLib.Value (true));
        }
    }
}
