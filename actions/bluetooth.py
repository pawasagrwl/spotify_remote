# actions/bluetooth.py
# Pure WinRT: ensure radio ON, locate Echo device, open it, and "poke" services.
# For classic A2DP devices, Windows auto-connects when audio starts (no explicit Connect() in WinRT).

import asyncio, datetime, os, time, re

from winrt.windows.devices.radios import Radio, RadioKind, RadioState
from winrt.windows.devices import bluetooth, enumeration

LOGFILE = os.path.join(os.path.dirname(__file__), "..", "spotify_remote.log")

# Set either ECHO_NAME (substring) or ECHO_MAC (AA:BB:CC:DD:EE:FF). Name is used first; MAC is fallback.
ECHO_NAME = "Echo Show 5-1MM"
ECHO_MAC  = None  # e.g. "A0:E7:0B:ED:D6:3F" (optional but nice to have)

def log(msg: str):
    ts = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"{ts}  [bluetooth] {msg}"
    print(line, flush=True)
    try:
        with open(LOGFILE, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except Exception:
        pass

def _mac_to_uint64(mac: str) -> int:
    # WinRT selectors take the 48-bit address as UInt64 (big-endian MAC in an integer).
    hexstr = re.sub(r'[^0-9A-Fa-f]', '', mac)
    if len(hexstr) != 12:
        raise ValueError("MAC must be 6 bytes like AA:BB:CC:DD:EE:FF")
    return int(hexstr, 16)

async def ensure_radio_on():
    radios_list = await Radio.get_radios_async()
    bt = next((r for r in radios_list if r.kind == RadioKind.BLUETOOTH), None)
    if not bt:
        raise RuntimeError("No Bluetooth radio found")
    if bt.state != RadioState.ON:
        log("Bluetooth OFF → toggling ON…")
        await bt.set_state_async(RadioState.ON)
        # recheck
        radios_list = await Radio.get_radios_async()
        bt = next((r for r in radios_list if r.kind == RadioKind.BLUETOOTH), None)
        if not bt or bt.state != RadioState.ON:
            raise RuntimeError("Failed to enable Bluetooth radio")
    log("Bluetooth radio is ON.")

async def _find_by_name() -> enumeration.DeviceInformation | None:
    aqs = bluetooth.BluetoothDevice.get_device_selector_from_pairing_state(True)
    devices = await enumeration.DeviceInformation.find_all_async_aqs_filter(aqs)
    wanted = ECHO_NAME.lower()
    for d in devices:
        if d.name and wanted in d.name.lower():
            return d
    return None

async def _find_by_mac() -> enumeration.DeviceInformation | None:
    if not ECHO_MAC:
        return None
    addr = _mac_to_uint64(ECHO_MAC)
    aqs = bluetooth.BluetoothDevice.get_device_selector_from_bluetooth_address(addr)
    devices = await enumeration.DeviceInformation.find_all_async_aqs_filter(aqs)
    return devices[0] if len(devices) else None

    if not ECHO_MAC:
        return None
    addr = _mac_to_uint64(ECHO_MAC)
    aqs = bluetooth.BluetoothDevice.get_device_selector_from_bluetooth_address(addr)
    devices = await enumeration.DeviceInformation.find_all_async(aqs)
    # Some stacks return the base device; grab the first
    return devices[0] if len(devices) else None

async def open_device(di: enumeration.DeviceInformation):
    dev = await bluetooth.BluetoothDevice.from_id_async(di.id)
    if not dev:
        raise RuntimeError("BluetoothDevice.from_id_async returned None")
    return dev

async def nudge_connection(dev: bluetooth.BluetoothDevice, wait_seconds: float = 2.0):
    # There is no public Connect(). Accessing RFCOMM services can wake the link on some stacks.
    try:
        _ = await dev.get_rfcomm_services_async()
    except Exception:
        # Not all classic audio devices expose RFCOMM; ignore.
        pass
    # brief settle time
    await asyncio.sleep(wait_seconds)
    return dev.connection_status

async def connect_echo_winrt(timeout_sec: int = 6):
    log(f"Searching for device '{ECHO_NAME}' (paired) …")
    di = await _find_by_name()
    if not di and ECHO_MAC:
        log("Name match failed, trying MAC selector…")
        di = await _find_by_mac()
    if not di:
        raise RuntimeError(f"Device not found (name ~ '{ECHO_NAME}' or MAC {ECHO_MAC or 'n/a'})")

    log(f"Found: {di.name or '(unnamed)'} → opening…")
    dev = await open_device(di)

    if dev.connection_status == bluetooth.BluetoothConnectionStatus.CONNECTED:
        log("Device already connected.")
        return True

    # Try to "nudge" then rely on playback to finalize the link.
    log("Device paired but not connected; nudging services…")
    status = await nudge_connection(dev, wait_seconds=1.5)
    if status == bluetooth.BluetoothConnectionStatus.CONNECTED:
        log("Device connected after nudge.")
        return True

    log("No explicit Connect() in WinRT for classic A2DP; Windows should auto-connect when audio starts.")
    return True

async def wait_until_connected(name: str, timeout_sec: int = 12):
    """Poll the target device until it's CONNECTED or timeout."""
    deadline = time.time() + timeout_sec
    aqs = bluetooth.BluetoothDevice.get_device_selector_from_pairing_state(True)
    while time.time() < deadline:
        devices = await enumeration.DeviceInformation.find_all_async_aqs_filter(aqs)
        for d in devices:
            if d.name and name.lower() in d.name.lower():
                dev = await bluetooth.BluetoothDevice.from_id_async(d.id)
                if dev and dev.connection_status == bluetooth.BluetoothConnectionStatus.CONNECTED:
                    log(f"{name} connected (within wait).")
                    return True
        await asyncio.sleep(1.0)
    log(f"{name} not connected within {timeout_sec}s.")
    return False


def run():
    asyncio.run(ensure_radio_on())
    asyncio.run(connect_echo_winrt())  # opens device, nudges services
    # Now wait up to 10 seconds for Windows to actually connect
    connected = asyncio.run(wait_until_connected(ECHO_NAME, timeout_sec=12))
    if not connected:
        raise RuntimeError(f"{ECHO_NAME} did not connect in time")
    return True

    # radio + device open are awaited; any failure raises → caller stops the flow
    asyncio.run(ensure_radio_on())
    asyncio.run(connect_echo_winrt())
    return True

if __name__ == "__main__":
    run()
