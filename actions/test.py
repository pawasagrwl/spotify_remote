import asyncio
from winrt.windows.devices.radios import Radio, RadioKind, RadioState

async def main():
    radios_list = await Radio.get_radios_async()
    bt = next((r for r in radios_list if r.kind == RadioKind.BLUETOOTH), None)
    if not bt:
        print("No Bluetooth radio found")
        return
    print("Before:", bt.state.name)
    if bt.state != RadioState.ON:
        result = await bt.set_state_async(RadioState.ON)
        print("SetState result:", result)
    print("After:", bt.state.name)

asyncio.run(main())
