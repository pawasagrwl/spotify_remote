# actions/launch_spotify.py
import subprocess, time
import datetime, os

LOGFILE = os.path.join(os.path.dirname(__file__), "..", "spotify_remote.log")

def log(msg: str):
    ts = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"{ts}  [launch_spotify] {msg}"
    print(line, flush=True)
    with open(LOGFILE, "a", encoding="utf-8") as f:
        f.write(line + "\n")

def is_spotify_running() -> bool:
    p = subprocess.run(
        ['tasklist', '/FI', 'IMAGENAME eq spotify.exe', '/FO', 'CSV', '/NH'],
        capture_output=True, text=True
    )
    return 'spotify.exe' in (p.stdout or '')

def run(stabilize_sec: float = 1.8):
    """Launch Spotify if not already running. Waits a short time for it to start."""
    if is_spotify_running():
        print("Spotify already running; skipping launch.")
    else:
        subprocess.Popen(['cmd', '/c', 'start', '', 'spotify:'], shell=False)
        print("Launched Spotify via spotify: protocol.")
    time.sleep(stabilize_sec)
    return True
