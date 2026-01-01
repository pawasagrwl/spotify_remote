using Microsoft.Win32;
using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SpotifyRemote.App.Services.Implementations
{
    public class RegistryStartupManager : IStartupManager
    {
        private const string AppName = "SpotifyRemoteServer";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public bool IsRunOnStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
            return key?.GetValue(AppName) != null;
        }

        public void SetRunOnStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            if (key == null) return;

            if (enable)
            {
                // Get the path to the executable
                // When published as single file, Process.GetCurrentProcess().MainModule.FileName is reliable
                // Or Environment.ProcessPath in .NET 6+
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
    }
}
