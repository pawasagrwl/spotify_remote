# actions/ensure_playlist_priority.py
# Policy: 
# (a) if an old session exists, try to play it; 
# (b) otherwise open your playlist and play;
# (c) if still nothing, open a known track from that playlist and play; 
# (d) final fallback: media key.
import asyncio, subprocess, time, ctypes
from typing import Optional
from winrt.windows.media.control import GlobalSystemMediaTransportControlsSessionManager as GSMTC
from winrt.windows.media.control import GlobalSystemMediaTransportControlsSessionPlaybackStatus as PB

# === configure these two ===
PLAYLIST_URI = "spotify:playlist:25NRSu3YOREVhyBkXQrGeE"  # <-- put your playlist here
FALLBACK_TRACK_URI = None  # e.g., "spotify:track:6rqhFgbbKwnb9MLmUQDhG6" (a track from that playlist)

# media key (last fallback)
VK_MEDIA_PLAY_PAUSE = 0xB3
KEYEVENTF_KEYUP = 0x0002
class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                ("dwFlags", ctypes.c_uint), ("time", ctypes.c_uint),
                ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]
class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_uint), ("ki", KEYBDINPUT)]
SendInput = ctypes.windll.user32.SendInput
def _media_playpause():
    inp = INPUT(); inp.type = 1; inp.ki = KEYBDINPUT(VK_MEDIA_PLAY_PAUSE,0,0,0,None)
    SendInput(1, ctypes.byref(inp), ctypes.sizeof(inp))
    inp.ki = KEYBDINPUT(VK_MEDIA_PLAY_PAUSE,0,KEYEVENTF_KEYUP,0,None)
    SendInput(1, ctypes.byref(inp), ctypes.sizeof(inp))

def _launch(uri: Optional[str]):
    subprocess.Popen(['cmd','/c','start','', uri or 'spotify:'], shell=False)

async def _mgr(): return await GSMTC.request_async()
async def _get_spotify_session():
    mgr = await _mgr()
    for s in mgr.get_sessions():
        try:
            if s.source_app_user_model_id and "spotify" in s.source_app_user_model_id.lower():
                return s
        except Exception:
            pass
    return None
async def _wait_for_session(timeout=8.0):
    end = time.time() + timeout
    while time.time() < end:
        s = await _get_spotify_session()
        if s: return s
        await asyncio.sleep(0.4)
    return None
async def _is_playing(session) -> bool:
    try:
        info = await session.get_playback_info()
        return bool(info and info.playback_status == PB.PLAYING)
    except Exception:
        return False

async def _ensure_playing_via_session(session, spin_sec=5.0) -> bool:
    start = time.time()
    while time.time() - start < spin_sec:
        try:
            await session.try_play_async()
        except Exception:
            pass
        if await _is_playing(session): return True
        await asyncio.sleep(0.4)
    return False

async def run(playlist_uri: Optional[str] = PLAYLIST_URI,
              fallback_track_uri: Optional[str] = FALLBACK_TRACK_URI) -> bool:
    # Step 1: if a session exists, try to play it first (your “last session” rule)
    sess = await _get_spotify_session()
    if sess and await _ensure_playing_via_session(sess, 3.0):
        return True

    # Step 2: open the playlist UI, then try to play
    if playlist_uri:
        _launch(playlist_uri)
        sess = await _wait_for_session(8.0)
        if sess and await _ensure_playing_via_session(sess, 4.0):
            return True

        # Some builds honor a legacy ":play" suffix on the URI; cheap extra try, harmless if ignored
        _launch(playlist_uri + ":play")
        sess = await _wait_for_session(4.0)
        if sess and await _ensure_playing_via_session(sess, 3.0):
            return True

    # Step 3: force something from that playlist by opening a known track URI, then play
    if fallback_track_uri:
        _launch(fallback_track_uri)
        sess = await _wait_for_session(6.0)
        if sess and await _ensure_playing_via_session(sess, 3.0):
            return True

    # Step 4: final fallback — fire the global Play/Pause key
    _media_playpause()
    return True

if __name__ == "__main__":
    asyncio.run(run())
