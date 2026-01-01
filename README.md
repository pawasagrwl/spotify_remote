# Spotify Remote for Windows

A lightweight, system-tray application for Windows that automates your audio environment. This tool runs a local HTTP server to receive triggers (e.g., from a mobile automation app), connects to a Bluetooth speaker (like an Echo Show), sets up your audio output, and controls Spotify playback with smart state detection.

## Features

- **Local HTTP Server**: Listens for POST commands to trigger automation.
- **Bluetooth Management**: Automatically turns on Bluetooth and ensures connection to your target device.
- **Audio Output Switcher**: Programmatically sets the default audio output device (e.g., switches to "Echo Show 5").
- **Spotify Automation**:
  - Checks if Spotify is playing.
  - If silent, launches a specific Playlist URI.
  - Ensures Spotify is the foreground window to reliably receive media keys.
  - Uses `Spacebar` simulation for robust Play/Pause toggling.
- **System Tray Integration**: Minimized to tray, access settings via right-click or double-click.
- **Settings**: Configure Target Device Name, Server Port, and Spotify Playlist URI.

## Prerequisites

- **Windows 10/11**: Tested on Windows 10 (Build 19041+).
- **.NET 8 SDK**: [Download Here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- **Spotify Desktop App**: Must be installed.

## Usage

1.  **Build & Run**:
    ```bash
    cd SpotifyRemote.App
    dotnet run
    ```
2.  **Configuration**:
    - Open the Settings window (double-click the tray icon).
    - Set **Target Device** (e.g., "Echo Show 5").
    - Set **Server Port** (default: `5000`).
    - Set **Spotify URI** (e.g., `spotify:playlist:...`).
    - Click **Start Server**.

3.  **Triggering**:
    - Send a POST request to `http://<YOUR_PC_IP>:5000/trigger`.
    - Example using curl:
      ```bash
      curl -X POST http://localhost:5000/trigger
      ```

## Architecture

- **Language**: C# (.NET 8)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Services**:
  - `ServerService`: HttpListener for handling requests.
  - `BluetoothManager`: Windows.Devices.Bluetooth APIs.
  - `AudioManager`: NAudio & PolicyConfig (COM) for device switching.
  - `SpotifyManager`: Process handling and Input Simulation (`user32.dll`).

## Contributing

1.  Fork the repository.
2.  Create a feature branch.
3.  Commit your changes.
4.  Open a Pull Request.
