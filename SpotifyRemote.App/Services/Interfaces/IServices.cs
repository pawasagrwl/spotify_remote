namespace SpotifyRemote.App.Services.Interfaces
{
    public interface IBluetoothManager
    {
        Task TurnOnBluetoothAsync();
        Task ConnectToDeviceAsync(string deviceName);
    }

    public interface IAudioManager
    {
        void SetVolume(int level);
        void Unmute();
        Task SetDefaultDeviceAsync(string friendlyName);
        Task PlayConfirmationSoundAsync();
    }

    public interface ISpotifyManager
    {
        Task StartSpotifyAsync();
        Task PlayPlaylistAsync(string uri);
        Task PlayPauseAsync();
        Task<bool> IsSpotifyPlayingAsync();
        Task BringSpotifyToFrontAsync();
    }

    public interface IServerService
    {
        Task StartServerAsync(int port, string targetDeviceName, string spotifyUri);
        Task StopServerAsync();
    }
}
