using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyRemote.App.Services.Interfaces;
using SpotifyRemote.App.Services.Implementations;

namespace SpotifyRemote.App
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<IBluetoothManager, BluetoothManager>();
                    services.AddSingleton<IAudioManager, AudioManager>();
                    services.AddSingleton<ISpotifyManager, SpotifyManager>();
                    services.AddSingleton<IServerService, ServerService>();
                })
                .Build();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Prevent sleep (runs in background)
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);

                await AppHost!.StartAsync();

                var startupForm = AppHost.Services.GetRequiredService<MainWindow>();
                startupForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}\n\n{ex.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost!.StopAsync();
            base.OnExit(e);
        }
    }
}
