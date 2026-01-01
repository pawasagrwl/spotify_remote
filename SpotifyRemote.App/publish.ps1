# Publish to a single file executable
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o ./publish
Write-Host "Build complete! Executable is at ./publish/SpotifyRemote.App.exe"
