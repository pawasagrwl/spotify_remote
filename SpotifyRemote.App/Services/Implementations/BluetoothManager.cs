using SpotifyRemote.App.Services.Interfaces;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace SpotifyRemote.App.Services.Implementations
{
    public class BluetoothManager : IBluetoothManager
    {
        public async Task TurnOnBluetoothAsync()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();
                var bluetoothRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);

                if (bluetoothRadio != null)
                {
                    if (bluetoothRadio.State != RadioState.On)
                    {
                        Debug.WriteLine("Turning Bluetooth Radio ON...");
                        await bluetoothRadio.SetStateAsync(RadioState.On);
                    }
                    else
                    {
                        Debug.WriteLine("Bluetooth Radio is already ON.");
                    }
                }
                else
                {
                    Debug.WriteLine("No Bluetooth Radio found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error turning on Bluetooth: {ex.Message}");
            }
        }

        public async Task ConnectToDeviceAsync(string deviceName)
        {
            try
            {
                Debug.WriteLine($"Scanning for paired device '{deviceName}'...");
                
                // Find all paired devices
                // Note: Connect usually involves switching default audio execution info.
                // Or just ensuring it is 'seen'.
                
                string deviceSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);

                var targetDevice = devices.FirstOrDefault(d => d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

                if (targetDevice != null)
                {
                    Debug.WriteLine($"Found paired device: {targetDevice.Name} ({targetDevice.Id})");
                    
                    // Attempt to retrieve BluetoothDevice to refresh connection status
                    var btDevice = await BluetoothDevice.FromIdAsync(targetDevice.Id);
                    
                    if (btDevice != null)
                    {
                        Debug.WriteLine($"Connection Status: {btDevice.ConnectionStatus}");
                        // Note: WinRT doesn't have a direct "Connect" method for A2DP.
                        // We rely on Windows' auto-connect properties or setting it as default audio endpoint.
                    }
                }
                else
                {
                    Debug.WriteLine($"Device '{deviceName}' not found in paired devices.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to device: {ex.Message}");
            }
        }
    }
}
