# actions/volume.py
from ctypes import POINTER, cast
from comtypes import CLSCTX_ALL
from pycaw.pycaw import AudioUtilities, IAudioEndpointVolume

def run(target_percent: int = 100):
    if target_percent < 0: target_percent = 0
    if target_percent > 100: target_percent = 100

    devices = AudioUtilities.GetSpeakers()
    interface = devices.Activate(IAudioEndpointVolume._iid_, CLSCTX_ALL, None)
    volume = cast(interface, POINTER(IAudioEndpointVolume))

    # Unmute
    volume.SetMute(0, None)

    # Set master scalar [0.0 .. 1.0]
    scalar = target_percent / 100.0
    volume.SetMasterVolumeLevelScalar(scalar, None)

    return True

if __name__ == "__main__":
    run(100)
