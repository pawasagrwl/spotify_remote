# actions/ensure_play.py
import asyncio, subprocess, time, ctypes, os
from winrt.windows.media.control import GlobalSystemMediaTransportControlsSessionManager as GSMTC
from winrt.windows.media.control import GlobalSystemMediaTransportControlsSessionPlaybackStatus as PB
from winrt.windows.foundation import TimeSpan

# Optional: put a real URI here to guarantee something is selected
# e.g., "spotify:playlist:37i9dQZF1DX0XUsuxWHRQd"
DEFAULT_URI = None

VK_MEDIA_PLAY_PAUSE = 0xB3
INPUT_KEYBOARD = 1
KEYEVENTF_KEYUP = 0x0002

class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                ("dwFlags", ctypes.c_uint), ("time", ctypes.c_uint),
                ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]
class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_uint), ("ki", KEYBDINPUT)]
SendInput = ctypes.windll.user32.SendInput

def _media_playpause():
    inp = INPUT()
    inp.type = INPUT_KEYBOARD
    inp.ki = KEYBDINPUT(VK_MEDIA_PLAY_PAUSE, 0, 0, 0, None)
    SendInput(1, ctypes.byref(inp), ctypes.sizeof(inp))
    inp.ki = KEYBDINPUT(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, 0, None)
    SendInput(1, ctypes.byref(inp), ctypes.sizeof(inp))

async def _get_spotify_session():
    mgr = await GSMTC.request_async()
    sessions = mgr.get_sessions()
    for s in sessions:
        try:
            info = await s.try_get_media_properties_async()
            # Executable id is usually "Spotify.exe", but matching by source app is more reliable:
            if s.source_app_user_model_id and "spotify" in s.source_app_user_model_id.lower():
                return s
        except Exception:
            pass
    return None

def _launch_spotify(uri: str | None = None):
    if uri:
        # Ensure the protocol handler kicks in
        subprocess.Popen(['cmd', '/c', 'start', '', uri], shell=False)
    else:
        subprocess.Popen(['cmd', '/c', 'start', '', 'spotify:'], shell=False)

async def _wait_for_session(timeout_sec: float = 6.0):
    deadline = time.time() + timeout_sec
    while time.time() < deadline:
        s = await _get_spotify_session()
        if s is not None:
            return s
        await asyncio.sleep(0.5)
    return None

async def run(default_uri: str | None = DEFAULT_URI,
              wait_session_sec: float = 6.0,
              play_timeout_sec: float = 4.0):
    """
    Ensure Spotify actually plays something.
    1) Find or create a Spotify GSMTC session
    2) If paused/stopped -> TryPlayAsync
    3) If no session yet -> open Spotify (optionally at URI), wait, TryPlayAsync
    4) Fallback: send media Play/Pause key
    """
    session = await _get_spotify_session()
    if session is None:
        _launch_spotify(default_uri)
        session = await _wait_for_session(wait_session_sec)

    if session is not None:
        # TryPlayAsync until status == Playing or timeout
        start = time.time()
        while time.time() - start < play_timeout_sec:
            try:
                await session.try_play_async()
                info = await session.get_playback_info()
                if info and info.playback_status == PB.PLAYING:
                    return True
            except Exception:
                pass
            await asyncio.sleep(0.5)

    # Final fallback: media key
    _media_playpause()
    return True

if __name__ == "__main__":
    asyncio.run(run())
