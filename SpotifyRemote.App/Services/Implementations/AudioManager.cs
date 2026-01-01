using NAudio.CoreAudioApi;
using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;
using System.Media;

namespace SpotifyRemote.App.Services.Implementations
{
    public class AudioManager : IAudioManager
    {
        public void Unmute()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (device.AudioEndpointVolume.Mute)
                {
                    device.AudioEndpointVolume.Mute = false;
                    Debug.WriteLine("System Audio Unmuted");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unmuting audio: {ex.Message}");
            }
        }

        public void SetVolume(int level)
        {
            try
            {
                // Clamp level between 0 and 100
                level = Math.Max(0, Math.Min(100, level));

                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                
                // Set volume (0.0 to 1.0)
                device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100.0f;
                Debug.WriteLine($"System Volume set to {level}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting volume: {ex.Message}");
            }
        }

        public async Task SetDefaultDeviceAsync(string friendlyName)
        {
             try
             {
                 await Task.Run(() =>
                 {
                     using var enumerator = new MMDeviceEnumerator();
                     var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                     var device = devices.FirstOrDefault(d => d.FriendlyName.Contains(friendlyName, StringComparison.OrdinalIgnoreCase));
                     
                     if (device != null)
                     {
                         Debug.WriteLine($"Found audio device: {device.FriendlyName}. Switching default...");
                         var policyConfig = new SpotifyRemote.App.Services.Com.PolicyConfig() as SpotifyRemote.App.Services.Com.IPolicyConfig;
                         policyConfig?.SetDefaultEndpoint(device.ID, (int)Role.Multimedia);
                         policyConfig?.SetDefaultEndpoint(device.ID, (int)Role.Communications);
                         Debug.WriteLine("Switched default audio device.");
                     }
                     else
                     {
                         Debug.WriteLine($"Audio device '{friendlyName}' not found.");
                     }
                 });
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"Error switching audio device: {ex.Message}");
             }
        }

        public async Task PlayConfirmationSoundAsync()
        {
            try
            {
                // Simple beep for now, or system sound.
                // Running on a separate task to not block UI
                await Task.Run(() => SystemSounds.Beep.Play());
                Debug.WriteLine("Played confirmation sound");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }
    }
}
