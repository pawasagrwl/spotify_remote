<#
  build_and_install.ps1
  Usage: run this in an elevated PowerShell or double-click it. It will re-launch elevated when needed.

  What it does:
   - Creates a venv at $ProjectDir\.venv
   - Installs minimal dependencies into the venv (pyinstaller, pycaw, comtypes, winrt modules)
   - Runs PyInstaller in --onedir mode (no UPX, less likely to trigger AV)
   - Creates a Task Scheduler entry to run the produced EXE at user logon
   - Adds a firewall rule (Private profile) for the EXE path
   - Adds a Windows Defender exclusion for the output folder
#>

# --- configuration (edit if your project is in a different path) ---
$ProjectDir = "C:\code\projects\spotify_remote"
$VenvDir   = Join-Path $ProjectDir ".venv"
$ExeName   = "spotify_remote"                 # EXE base name
$PyInstallerName = "spotify_remote"           # name used for build output folder
$PythonExe  = "$VenvDir\Scripts\python.exe"   # will be created
$DistDir    = Join-Path $ProjectDir "dist\$PyInstallerName"
$TaskName   = "SpotifyRemote"
$Port       = 8765

# Packages to install into venv (minimal list used by the project)
$Packages = @(
  "pyinstaller",
  "pycaw",
  "comtypes",
  "pystray",
  "pillow",
  "pyyaml",
  # WinRT modular packages used in project (install runtime + projections)
  "winrt-runtime",
  "winrt-Windows.Foundation",
  "winrt-Windows.Foundation.Collections",
  "winrt-Windows.Devices.Radios",
  "winrt-Windows.Devices.Bluetooth",
  "winrt-Windows.Devices.Enumeration",
  "winrt-Windows.Media.Control"
)

# Hidden imports for PyInstaller to reduce missing-module runtime errors
$HiddenImports = @(
  "winrt.windows.foundation",
  "winrt.windows.foundation.collections",
  "winrt.windows.devices.bluetooth",
  "winrt.windows.devices.enumeration",
  "winrt.windows.devices.radios",
  "winrt.windows.media.control"
)

# --- helper functions ---
function Write-Log($m){ Write-Host "$(Get-Date -Format 'HH:mm:ss')  $m" }

function Ensure-PathExists {
    param($p)
    if (-not (Test-Path $p)) {
        New-Item -ItemType Directory -Path $p | Out-Null
    }
}

# --- Step 0: sanity check project dir ---
if (-not (Test-Path $ProjectDir)) {
    Write-Host "Project directory $ProjectDir does not exist. Edit the script and set the correct path." -ForegroundColor Red
    exit 1
}

Set-Location $ProjectDir

# --- Step 1: create venv if missing ---
if (-not (Test-Path $VenvDir)) {
    Write-Log "Creating virtual environment in $VenvDir ..."
    py -3 -m venv $VenvDir
    if ($LASTEXITCODE -ne 0) { Write-Host "Failed to create venv"; exit 1 }
} else {
    Write-Log "Virtual environment already exists: $VenvDir"
}

# Ensure venv python exists
if (-not (Test-Path $PythonExe)) {
    Write-Host "Could not find venv python at $PythonExe" -ForegroundColor Red
    exit 1
}

# Upgrade pip
Write-Log "Upgrading pip in venv..."
& $PythonExe -m pip install --upgrade pip setuptools wheel | Out-Null

# --- Step 2: install runtime deps into venv ---
Write-Log "Installing packages into venv (this may take a minute)..."
foreach ($pkg in $Packages) {
    Write-Log "  pip install $pkg"
    & $PythonExe -m pip install $pkg
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install $pkg. You can retry manually: `"$PythonExe -m pip install $pkg`"" -ForegroundColor Yellow
    }
}

# --- Step 3: run PyInstaller (onedir, no UPX) ---
# Build command
$hiddenArgs = $HiddenImports | ForEach-Object { "--hidden-import=$_" } | Out-String
$hiddenArgs = $hiddenArgs -replace "`r`n"," "
$adddata = ""  # no config.yaml right now; if you add one, set: "--add-data `"$ProjectDir\config.yaml;.`""

# Ensure previous build cleaned
Write-Log "Cleaning previous build/dist folders..."
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $ProjectDir "build")
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $ProjectDir "dist\$PyInstallerName")
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $ProjectDir "$PyInstallerName.spec")

Write-Log "Running PyInstaller (--onedir, no UPX) ..."
$piArgs = @(
    "--noconfirm",
    "--onedir",
    "--clean",
    "--name", $PyInstallerName
)
foreach ($hi in $HiddenImports) { $piArgs += "--hidden-import"; $piArgs += $hi }
# add data if you want to embed config.yaml: $piArgs += "--add-data"; $piArgs += "$ProjectDir\config.yaml;."
$piArgs += "server.py"

# Run PyInstaller via venv python
& $PythonExe -m PyInstaller @piArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "PyInstaller build failed. Inspect PyInstaller output above." -ForegroundColor Red
    exit 1
}
Write-Log "PyInstaller build finished."

# Check EXE exists
$ExePath = Join-Path $DistDir "$PyInstallerName.exe"
if (-not (Test-Path $ExePath)) {
    # fallback: look for any exe in dist\$PyInstallerName
    $exeFiles = Get-ChildItem -Path $DistDir -Filter "*.exe" -ErrorAction SilentlyContinue
    if ($exeFiles.Count -gt 0) {
        $ExePath = $exeFiles[0].FullName
    } else {
        Write-Host "Could not find built EXE under $DistDir" -ForegroundColor Red
        exit 1
    }
}
Write-Log "Built EXE: $ExePath"

# --- Step 4: ensure log destination exists (persistent path) ---
$LogDir = Join-Path $env:LOCALAPPDATA "spotify_remote"
Ensure-PathExists $LogDir
Write-Log "Logs will be stored in $LogDir (ensure your server writes logs there)"

# --- Step 5: prepare commands that need admin (firewall, defender, scheduled task) ---
# We'll re-run the rest elevated if current user is not admin.
function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdmin)) {
    Write-Log "Not running as Administrator. Re-launching elevated to complete firewall/scheduler/defender steps..."
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "powershell.exe"
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -ElevatedRun"
    $psi.Verb = "runAs"
    try {
        $proc = [System.Diagnostics.Process]::Start($psi)
        Write-Log "Elevation requested. You may see a UAC prompt. After granting, the script will continue and exit this session."
        exit 0
    } catch {
        Write-Host "Elevation canceled or failed. The script will continue but firewall/task/defender changes will be skipped." -ForegroundColor Yellow
    }
}

# If script was invoked with -ElevatedRun, continue admin steps
param([switch]$ElevatedRun)

# --- Step 6: create scheduled task to run at user logon ---
Write-Log "Creating Task Scheduler entry ($TaskName) to run at user logon..."
# Use Register-ScheduledTask for per-user task (works on modern Windows)
$action = New-ScheduledTaskAction -Execute $ExePath
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -Hidden:$false
try {
    $task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings
    Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force
    Write-Log "Registered scheduled task: $TaskName"
} catch {
    Write-Host "Failed to register scheduled task via Register-ScheduledTask: $_.Exception.Message" -ForegroundColor Yellow
    Write-Log "Attempting fallback with schtasks.exe..."
    $exeQuoted = "`"$ExePath`""
    schtasks /Create /SC ONLOGON /TN $TaskName /TR $exeQuoted /F | Out-Null
    Write-Log "Fallback schtasks registration attempted."
}

# --- Step 7: open firewall for the EXE (Private profile only) ---
Write-Log "Adding firewall rule to allow inbound connections (Private profile) for the EXE..."
try {
    # Remove existing with same name if present
    if (Get-NetFirewallRule -DisplayName $TaskName -ErrorAction SilentlyContinue) {
        Remove-NetFirewallRule -DisplayName $TaskName -Confirm:$false
    }
    New-NetFirewallRule -DisplayName $TaskName -Direction Inbound -Program $ExePath -Action Allow -Profile Private -EdgeTraversalPolicy Block | Out-Null
    Write-Log "Firewall rule added."
} catch {
    Write-Host "Failed to add firewall rule: $_" -ForegroundColor Yellow
}

# --- Step 8: add Windows Defender exclusion for the dist folder ---
Write-Log "Adding Windows Defender exclusion for $DistDir ..."
try {
    Add-MpPreference -ExclusionPath $DistDir
    Write-Log "Defender exclusion added."
} catch {
    Write-Host "Add-MpPreference failed (you may not have Windows Defender or insufficient rights). Error: $_" -ForegroundColor Yellow
}

# --- Final notes & quick test ---
Write-Log "Build + install steps completed."
Write-Host ""
Write-Host "Quick manual checks:"
Write-Host "  1) Run the EXE once manually to verify it starts and logs to $LogDir"
Write-Host "     -> $ExePath"
Write-Host "  2) From your phone test: http://<your-pc-ip>:$Port/ping"
Write-Host "  3) If Task Scheduler didn't run automatically, open Task Scheduler and run the task 'SpotifyRemote' manually."
Write-Host ""
Write-Host "If the scheduled task doesn't run as expected, open Task Scheduler UI and ensure 'Run only when user is logged on' is selected for interactive media key behavior."

# end of script
