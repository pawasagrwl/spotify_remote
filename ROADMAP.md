# Roadmap

## 1. Core Automation (Completed)
- [x] **Project Setup**: C# WPF .NET 8 structure.
- [x] **Audio Control**: Mute/Unmute, Volume Control (NAudio).
- [x] **Audio Switching**: Programmatic default device switching (IPolicyConfig).
- [x] **Bluetooth Control**: Toggle Radio (Stubbed), Connect to Device.
- [x] **Spotify Control**:
    - [x] Launch Process.
    - [x] URI Launching.
    - [x] Smart Playback Detection (Media Transport Controls).
    - [x] Reliable Input Simulation (Foreground + Spacebar).
- [x] **Server**: HTTP Listener for localized triggers.

## 2. User Experience (Completed)
- [x] **System Tray**: Minimize to tray, context menu.
- [x] **Settings UI**: Configure Port, Device Name, Playlist URI.
- [x] **Status Logs**: Real-time feedback in the UI window.

## 3. Future Enhancements
- [ ] **Run on Startup**: Add registry key toggle in Settings.
- [ ] **Offline Detection**: Check internet connection before launching Spotify.
- [ ] **Custom Authentication**: Secure the HTTP server with a token/password.
- [ ] **Advanced Error Handling**: Auto-retry logic for failed Bluetooth connections.
- [ ] **Mobile Companion App**: A simple UI for sending the trigger request.