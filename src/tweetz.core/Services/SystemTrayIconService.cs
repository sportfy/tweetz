﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using tweetz.core.Extensions;
using tweetz.core.Interfaces;
using Application = System.Windows.Application;

namespace tweetz.core.Services
{
    public class SystemTrayIconService : ISystemTrayIconService
    {
        private bool       disposed;
        private ISettings  Settings      { get; }
        private NotifyIcon NotifyIcon    { get; }
        private Icon?      NormalIcon    { get; set; }
        private Icon?      NewTweetsIcon { get; set; }

        public SystemTrayIconService(ISettings settings)
        {
            Settings   = settings;
            NotifyIcon = new NotifyIcon();
        }

        public void Initialize(Window window)
        {
            NotifyIcon.Tag  = window;
            NotifyIcon.Text = App.GetString("title");

            using var stream1 = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"))!.Stream;
            NormalIcon      = new Icon(stream1);
            NotifyIcon.Icon = NormalIcon;

            using var stream2 = Application.GetResourceStream(new Uri("pack://application:,,,/app-pending.ico"))!.Stream;
            NewTweetsIcon = new Icon(stream2);

            ShowInSystemTray(window);
            NotifyIcon.Click         += OnClick;
            Settings.PropertyChanged += UpdateVisibility;

            window.StateChanged += delegate
            {
                if (window.WindowState != WindowState.Minimized)
                {
                    UpdateIcon();
                }
            };
        }

        private void OnClick(object? _, EventArgs __)
        {
            // Bring window to front
            if (NotifyIcon.Tag is Window window)
            {
                window.WindowState = WindowState.Minimized;
                window.Show();
                window.WindowState = WindowState.Normal;
            }
        }

        private void UpdateVisibility(object? _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.IsEqualTo(nameof(Settings.ShowInSystemTray))
             && NotifyIcon.Tag is Window window)
            {
                ShowInSystemTray(window);
            }
        }

        public void Close()
        {
            if (disposed is true)
            {
                disposed                 =  true;
                NotifyIcon.Tag           =  null;
                NotifyIcon.Visible       =  false;
                NotifyIcon.Click         -= OnClick;
                Settings.PropertyChanged -= UpdateVisibility;
                NotifyIcon.Dispose();
            }
        }

        private void ShowInSystemTray(Window window)
        {
            window.ShowInTaskbar = !Settings.ShowInSystemTray;
            NotifyIcon.Visible   = Settings.ShowInSystemTray;
        }

        public void UpdateIcon(bool newTweetsAvailable = false)
        {
            NotifyIcon.Icon = newTweetsAvailable
                ? NewTweetsIcon
                : NormalIcon;
        }
    }
}