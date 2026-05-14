#!/usr/bin/env python3
"""Set system master volume on Windows using pycaw.

Usage: set_volume.py 80

Requires: pip install pycaw comtypes
"""
import sys

def set_volume_percent(percent: float) -> None:
    try:
        from ctypes import POINTER, cast
        from comtypes import CLSCTX_ALL
        from pycaw.pycaw import AudioUtilities, IAudioEndpointVolume
    except Exception as e:
        print("Missing dependency: please install 'pycaw' and 'comtypes' (pip install pycaw comtypes)")
        raise

    devices = AudioUtilities.GetSpeakers()
    interface = devices.Activate(IAudioEndpointVolume._iid_, CLSCTX_ALL, None)
    volume = cast(interface, POINTER(IAudioEndpointVolume))
    # percent: 0.0 - 100.0 -> scalar 0.0 - 1.0
    scalar = max(0.0, min(1.0, float(percent) / 100.0))
    volume.SetMasterVolumeLevelScalar(scalar, None)


def main():
    pct = 80.0
    if len(sys.argv) > 1:
        try:
            pct = float(sys.argv[1])
        except Exception:
            print("Invalid percent argument, using 80")
            pct = 80.0

    try:
        set_volume_percent(pct)
        print(f"Set system volume to {pct}%")
    except Exception as e:
        print(f"Failed to set system volume: {e}")
        sys.exit(2)


if __name__ == '__main__':
    main()
