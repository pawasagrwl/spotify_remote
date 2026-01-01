using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SpotifyRemote.App.Services.Implementations
{
    public class ServerService : IServerService
    {
        private HttpListener? _listener;
        private bool _isRunning;
        private readonly IBluetoothManager _bluetoothManager;
        private readonly IAudioManager _audioManager;
        private readonly ISpotifyManager _spotifyManager;

        public ServerService(IBluetoothManager bluetooth, IAudioManager audio, ISpotifyManager spotify)
        {
            _bluetoothManager = bluetooth;
            _audioManager = audio;
            _spotifyManager = spotify;
        }

        private string _targetDeviceName = "Echo Show 5";
        private string _spotifyUri = "spotify:playlist:37i9dQZF1DXcBWIGoYBM5M";

        public async Task StartServerAsync(int port, string targetDeviceName, string spotifyUri)
        {
            if (_isRunning) return;
            
            await Task.Yield(); // Ensure async context yield
            
            _targetDeviceName = targetDeviceName;
            _spotifyUri = spotifyUri;
            _listener = new HttpListener();
            // Note: '*' requires Admin. 'localhost' allows local only.
            // Using '+' is robust for binding all IPs but also needs ACL.
            // For user ease, we try wildcard, if fails, user might need to run as admin.
            string prefix = $"http://localhost:{port}/"; 
            _listener.Prefixes.Add(prefix);

            try
            {
                _listener.Start();
                _isRunning = true;
                Debug.WriteLine($"Server listening on {prefix}");
                
                // Start accept loop
                _ = Task.Run(() => HandleIncomingConnections());
            }
            catch (HttpListenerException ex)
            {
                // Likely access denied (Error 5)
                Debug.WriteLine($"Server Start Failed: {ex.Message}. Try running as Admin or checking netsh acl.");
                throw;
            }
        }

        public Task StopServerAsync()
        {
            _isRunning = false;
            _listener?.Stop();
            return Task.CompletedTask;
        }

        private async Task HandleIncomingConnections()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(ctx); // Fire and forget request processing
                }
                catch (HttpListenerException)
                {
                    // Listener stopped or error
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error accepting context: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext ctx)
        {
            try
            {
                // Simple validation: Ensure it's a POST to /trigger
                if (ctx.Request.Url?.AbsolutePath.Equals("/trigger", StringComparison.OrdinalIgnoreCase) == true && ctx.Request.HttpMethod == "POST")
                {
                    Debug.WriteLine("Trigger received!");
                    
                    // Acknowledge receipt first? Or wait for completion?
                    // User said "send back progress".
                    // We can respond with "Started" and do logic.
                    // Or do logic and respond "Done".
                    // Let's do logic and respond "Done" for simplicity unless it takes too long.
                    
                    byte[] buf = Encoding.UTF8.GetBytes("Processing...");
                    ctx.Response.ContentLength64 = buf.Length;
                    await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
                    
                    // Execute Workflow
                    // 1. Bluetooth ON
                    await _bluetoothManager.TurnOnBluetoothAsync();
                    
                    // 2. Connect Echo Show 5
                    await _bluetoothManager.ConnectToDeviceAsync(_targetDeviceName);
                    
                    // 2a. Switch Audio Output
                    await _audioManager.SetDefaultDeviceAsync(_targetDeviceName);
                    
                    // 3. Unmute & Volume 100
                    _audioManager.Unmute();
                    _audioManager.SetVolume(100);
                    
                    // 4. Smart Spotify Playback (Check-First Approach)
                    
                    // Step A: Check if already playing
                    bool isPlaying = await _spotifyManager.IsSpotifyPlayingAsync();
                    
                    if (isPlaying)
                    {
                         Debug.WriteLine("Spotify is already playing. Skipping Play command.");
                    }
                    else
                    {
                        // Step B: Not playing, try to Resume
                        Debug.WriteLine("Spotify silent. Attempting to Resume...");
                        await _spotifyManager.PlayPauseAsync();
                        await Task.Delay(3000); 
                        
                        isPlaying = await _spotifyManager.IsSpotifyPlayingAsync();
                        
                        if (!isPlaying)
                        {
                        // Step C: Still silent, Launch URI (Fallback)
                            Debug.WriteLine("Resume failed. Launching Playlist URI...");
                            await _spotifyManager.PlayPlaylistAsync(_spotifyUri);
                            await Task.Delay(8000); // Increased wait time for load
                            
                             // Step D: Final Check & Force
                             isPlaying = await _spotifyManager.IsSpotifyPlayingAsync();
                             if (!isPlaying)
                             {
                                 Debug.WriteLine("Playlist didn't auto-play. Forcing Play Key...");
                                 await _spotifyManager.PlayPauseAsync();
                                 await Task.Delay(5000); 
                             }
                        }
                    }
                    Debug.WriteLine($"Final Spotify State: {(isPlaying ? "Playing" : "Silent")}");
                    
                    // 4. Play Sound
                    await _audioManager.PlayConfirmationSoundAsync();
                    
                    // 5. Spotify
                    // Let's assume default for now.
                    await _spotifyManager.StartSpotifyAsync(); // Start/Resume
                    
                    ctx.Response.Close();
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing request: {ex.Message}");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }
    }
}
