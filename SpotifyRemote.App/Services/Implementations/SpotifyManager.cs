using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Media.Control;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace SpotifyRemote.App.Services.Implementations
{
    public class SpotifyManager : ISpotifyManager
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const byte VK_SPACE = 0x20;
        private const int KEYEVENTF_EXTENDEDKEY = 0x01;
        private const int KEYEVENTF_KEYUP = 0x02;

        public async Task StartSpotifyAsync()
        {
            await BringSpotifyToFrontAsync();
            if (Process.GetProcessesByName("Spotify").Length == 0)
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var spotifyPath = Path.Combine(appData, "Spotify", "Spotify.exe");
                if (File.Exists(spotifyPath))
                {
                    Process.Start(new ProcessStartInfo { FileName = spotifyPath, UseShellExecute = true });
                    await Task.Delay(2000); 
                }
            }
        }

        public async Task BringSpotifyToFrontAsync()
        {
            var spotifyProcs = Process.GetProcessesByName("Spotify");
            var mainProc = spotifyProcs.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            if (mainProc != null)
            {
                SetForegroundWindow(mainProc.MainWindowHandle);
                await Task.Delay(500); // Wait for focus
            }
        }

        public async Task<bool> IsSpotifyPlayingAsync()
        {
            try 
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var session = manager.GetCurrentSession();
                if (session != null)
                {
                    if (session.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                    {
                        var info = session.GetPlaybackInfo();
                        return info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    }
                }
                return false;
            }
            catch 
            {
                return false; 
            }
        }

        public Task PlayPlaylistAsync(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching Spotify: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        public async Task PlayPauseAsync()
        {
            await BringSpotifyToFrontAsync(); 
            
            Debug.WriteLine("Sending SPACE key to Spotify...");
            // Audible debug confirmation
            Console.Beep(800, 200); 

            // Simulate SPACE Bar (Standard Play/Pause for Spotify when focused)
            keybd_event(VK_SPACE, 0, 0, 0); // Down
            keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, 0); // Up
            
            await Task.CompletedTask;
        }
    }
}
