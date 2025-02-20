﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows;
using FontStyle = System.Drawing.FontStyle;
using MessageBox = System.Windows.MessageBox;
using Cursors = System.Windows.Input.Cursors;
//using System.Windows.Controls;

using System.Threading;
using System.Diagnostics;

namespace ChampSelectHelperApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private NotifyIcon icon;
        public static bool settsOpen = false;
        public static bool settsLoaded = false;

        public App()
        {
            icon = new NotifyIcon();
            icon.Text = Program.APP_NAME;
            icon.Visible = true;
            using (Stream stream = GetResourceStream(new Uri(@"pack://application:,,,/Resources/System Icons/icon.ico")).Stream)
            {
                icon.Icon = new Icon(stream);       
            }

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripLabel label = new ToolStripLabel();
            ToolStripMenuItem launchAtStartup = new ToolStripMenuItem();
            ToolStripMenuItem openSettings = new ToolStripMenuItem();
            ToolStripMenuItem quitApp = new ToolStripMenuItem();
            icon.ContextMenuStrip = contextMenu;

            icon.MouseClick += (sender, ev) => { if (ev.Button == MouseButtons.Left) OpenSettingsWindow(); };

            label.Text = Program.APP_NAME + " v" + Program.APP_VERSION;
            label.Font = new Font(label.Font, FontStyle.Bold);
            contextMenu.Items.Add(label);

            contextMenu.Items.Add(new ToolStripSeparator());

            launchAtStartup.Text = "Launch At System Startup";
            launchAtStartup.CheckOnClick = true;
            launchAtStartup.Checked = FileHandler.LaunchesAtStartup();
            launchAtStartup.CheckedChanged += (sender, ev) => FileHandler.ToggleLaunchAtStartup();
            contextMenu.Items.Add(launchAtStartup);

            openSettings.Text = "Settings";
            using (Stream stream = GetResourceStream(new Uri(@"pack://application:,,,/Resources/System Icons/settings.ico")).Stream)
            {
                openSettings.Image = Image.FromStream(stream);
            }
            openSettings.Click += async (sender, ev) => { if (settsOpen) return; else settsOpen = true; await OpenSettingsWindow(); };
            contextMenu.Items.Add(openSettings);

            quitApp.Text = "Quit";
            using (Stream stream = GetResourceStream(new Uri(@"pack://application:,,,/Resources/System Icons/quit.ico")).Stream)
            {
                quitApp.Image = Image.FromStream(stream);
            }
            quitApp.Click += (sender, ev) => Shutdown();
            contextMenu.Items.Add(quitApp);

            if (!Program.LAUNCHED_AT_STARTUP) ShowNotification(Program.APP_NAME + " is running in the background.");
        }

        public void ShowNotification(string text)
        {
            icon.BalloonTipTitle = Program.APP_NAME;
            icon.BalloonTipText = text;
            icon.ShowBalloonTip(5000);
        }

        public void ShowMessageWindow(string text)
        {
            // what was this for lol
        }

        private async Task OpenSettingsWindow()
        {
            if (SettingsWindow.Current is not null)
            {
                if (!SettingsWindow.Current.IsActive)
                    SettingsWindow.Current.Activate();

                if (!SettingsWindow.Current.IsFocused)
                    SettingsWindow.Current.Focus();
            }
            else
            {
                //create window with empty elements
                //load all the info
                //make visible all the elements
                settsLoaded = false;
                var loadingWindow = new LoadingWindow();
                var settWindow = new SettingsWindow();
                loadingWindow.Show();
                settWindow.Show();
                await settWindow.InitializeWindow();
                settsLoaded = true;
                loadingWindow.Close();
                //var task = Task.Run(settWindow.InitializeWindow).ContinueWith((x) => { settsLoaded = true; Dispatcher.Invoke(loadingWindow.Close); });
                //add try catch block for and add a messagebox when an error is thrown
            }
        }
    }
}
