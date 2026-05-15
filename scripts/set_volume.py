#!/usr/bin/env python3
"""Set system master volume on Windows using pycaw.

Usage: set_volume.py 80

Requires: pip install pycaw comtypes
"""
import sys

# Volume configuration constants
DEFAULT_VOLUME_PERCENT = 80.0
MIN_VOLUME_PERCENT = 0.0
MAX_VOLUME_PERCENT = 100.0
VOLUME_SCALAR_MIN = 0.0
VOLUME_SCALAR_MAX = 1.0

def set_volume_percent(percent: float) -> None:
    """Set Windows master volume to specified percentage."""
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
    
    # Convert percentage (0-100) to scalar (0.0-1.0)
    scalar = max(VOLUME_SCALAR_MIN, min(VOLUME_SCALAR_MAX, float(percent) / MAX_VOLUME_PERCENT))
    volume.SetMasterVolumeLevelScalar(scalar, None)


def main():
    """Main entry point."""
    pct = DEFAULT_VOLUME_PERCENT
    if len(sys.argv) > 1:
        try:
            pct = float(sys.argv[1])
            # Validate range
            if not (MIN_VOLUME_PERCENT <= pct <= MAX_VOLUME_PERCENT):
                print(f"Warning: Volume out of range [{MIN_VOLUME_PERCENT}-{MAX_VOLUME_PERCENT}], clamping to {pct}%")
        except ValueError:
            print(f"Invalid percentage argument, using default {DEFAULT_VOLUME_PERCENT}%")
            pct = DEFAULT_VOLUME_PERCENT

    try:
        set_volume_percent(pct)
        print(f"Set system volume to {pct}%")
    except Exception as e:
        print(f"Failed to set system volume: {e}")
        sys.exit(2)


if __name__ == '__main__':
    main()
