using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;

namespace SpotifyRemote.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IServerService? _serverService;
        private readonly IBluetoothManager? _bluetoothManager;
        private readonly IAudioManager? _audioManager;
        private readonly ISpotifyManager? _spotifyManager;

        // Settings
        private string _deviceName = "Echo Show 5";
        private int _port = 5000;
        private string _spotifyUri = "spotify:playlist:37i9dQZF1DXcBWIGoYBM5M"; // Global Top 50 default
        private bool _isServerRunning;
        private string _statusLog = "Ready.";

        public string DeviceName
        {
            get => _deviceName;
            set { _deviceName = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public string SpotifyUri
        {
            get => _spotifyUri;
            set { _spotifyUri = value; OnPropertyChanged(); }
        }

        public bool IsServerRunning
        {
            get => _isServerRunning;
            set { _isServerRunning = value; OnPropertyChanged(); }
        }
        
        public string StatusLog
        {
            get => _statusLog;
            set { _statusLog = value; OnPropertyChanged(); }
        }

        public ICommand? ToggleServerCommand { get; private set; }
        public ICommand? TestTriggerCommand { get; private set; }

        // Default constructor for XAML design time
        public MainViewModel() { }

        public MainViewModel(IServerService server, IBluetoothManager bt, IAudioManager audio, ISpotifyManager spotify)
        {
            _serverService = server;
            _bluetoothManager = bt;
            _audioManager = audio;
            _spotifyManager = spotify;

            ToggleServerCommand = new RelayCommand(async (_) => await ToggleServerAsync());
            TestTriggerCommand = new RelayCommand(async (_) => await TestTriggerAsync());
            
            // Auto-start server?
            _ = ToggleServerAsync();
        }

        private async Task ToggleServerAsync()
        {
            try
            {
                if (IsServerRunning)
                {
                    if (_serverService != null) await _serverService.StopServerAsync();
                    StatusLog = "Server Stopped.";
                    IsServerRunning = false;
                }
                else
                {
                    if (_serverService != null) await _serverService.StartServerAsync(Port, DeviceName, SpotifyUri);
                    StatusLog = $"Server Running on Port {Port}";
                    IsServerRunning = true;
                }
            }
            catch (Exception ex)
            {
                StatusLog = $"Error: {ex.Message}";
            }
        }

        private async Task TestTriggerAsync()
        {
            StatusLog = "Testing Trigger...";
            try
            {
                // Simulate Flow
                if (_bluetoothManager != null) await _bluetoothManager.TurnOnBluetoothAsync();
                if (_bluetoothManager != null) await _bluetoothManager.ConnectToDeviceAsync(DeviceName);
                if (_audioManager != null)
                {
                    _audioManager.Unmute();
                    _audioManager.SetVolume(100);
                    await _audioManager.PlayConfirmationSoundAsync();
                }
                if (_spotifyManager != null) await _spotifyManager.PlayPlaylistAsync(SpotifyUri);
                StatusLog = "Test Complete.";
            }
            catch (Exception ex)
            {
                StatusLog = $"Test Failed: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        public RelayCommand(Func<object?, Task> execute) => _execute = execute;
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute(parameter);
    }
}
