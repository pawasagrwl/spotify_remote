namespace SpotifyRemote.App.Services.Interfaces
{
    public interface IStartupManager
    {
        bool IsRunOnStartupEnabled();
        void SetRunOnStartup(bool enable);
    }
}
