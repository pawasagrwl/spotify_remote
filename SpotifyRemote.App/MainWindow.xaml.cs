using System.Drawing;
using System.Windows;
using SpotifyRemote.App.Services.Interfaces;
using SpotifyRemote.App.ViewModels;

namespace SpotifyRemote.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(IServerService server, IBluetoothManager bt, IAudioManager audio, ISpotifyManager spotify)
        {
            InitializeComponent();
            DataContext = new MainViewModel(server, bt, audio, spotify);
            
            // Set default icon for Tray
            if (MyNotifyIcon != null)
            {
                MyNotifyIcon.Icon = SystemIcons.Application;
            }
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Don't close, just hide
            e.Cancel = true;
            this.Hide();
        }
    }
}