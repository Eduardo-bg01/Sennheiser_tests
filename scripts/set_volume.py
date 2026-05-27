#!/usr/bin/env python3
"""Set the Windows master volume without external Python dependencies."""
from __future__ import annotations

import os
import subprocess
import sys
import tempfile
from textwrap import dedent


DEFAULT_VOLUME_PERCENT = 80.0
MIN_VOLUME_PERCENT = 0.0
MAX_VOLUME_PERCENT = 100.0


def clamp_volume(value: float) -> float:
    return max(MIN_VOLUME_PERCENT, min(MAX_VOLUME_PERCENT, value))


def build_powershell_script(volume_percent: float) -> str:
    return dedent(
        f'''
        param([double]$Volume = {volume_percent})

        $ErrorActionPreference = 'Stop'

        function Get-AudioSwitcherAssemblies {{
            $searchRoots = @(
                (Join-Path $env:USERPROFILE '.nuget\packages'),
                (Join-Path $env:LOCALAPPDATA 'NuGet\Cache')
            ) | Where-Object {{ Test-Path $_ }}

            $assemblies = @()

            foreach ($root in $searchRoots) {{
                foreach ($packageName in @('audioswitcher.audioapi.coreaudio', 'audioswitcher.audioapi')) {{
                    $packageRoot = Join-Path $root $packageName
                    if (Test-Path $packageRoot) {{
                        $assemblies += Get-ChildItem -Path $packageRoot -Recurse -Filter 'AudioSwitcher*.dll' -ErrorAction SilentlyContinue | ForEach-Object {{ $_.FullName }}
                    }}
                }}
            }}

            $assemblies | Sort-Object -Unique
        }}

        function Set-Volume-WithAudioSwitcher {{
            $refs = @(Get-AudioSwitcherAssemblies)
            if ($refs.Count -eq 0) {{
                return $false
            }}

            $code = @"
using System;
using AudioSwitcher.AudioApi.CoreAudio;

public static class AudioVolumeSetter {{
    public static void SetVolume(double volumePercent) {{
        var controller = new CoreAudioController();
        var device = controller.DefaultPlaybackDevice;
        if (device == null) {{
            throw new Exception("No default playback device found.");
        }}

        device.Volume = volumePercent;
        device.Mute(false);
    }}
}}
"@

            Add-Type -TypeDefinition $code -Language CSharp -ReferencedAssemblies $refs
            [AudioVolumeSetter]::SetVolume($Volume)
            return $true
        }}

        function Set-Volume-WithCom {{
            $code = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
public class MMDeviceEnumerator {{ }}

public enum EDataFlow {{ eRender = 0, eCapture = 1, eAll = 2 }}
public enum ERole {{ eConsole = 0, eMultimedia = 1, eCommunications = 2 }}

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator {{
    int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    int RegisterEndpointNotificationCallback(IntPtr pClient);
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}}

[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice {{
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    int GetState(out int pdwState);
}}

[ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume {{
    int RegisterControlChangeNotify(IntPtr pNotify);
    int UnregisterControlChangeNotify(IntPtr pNotify);
    int GetChannelCount(out uint pnChannelCount);
    int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
    int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
    int GetMasterVolumeLevel(out float pfLevelDB);
    int GetMasterVolumeLevelScalar(out float pfLevelDB);
    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevelDB);
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
    int GetMute(out bool pbMute);
    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
    int VolumeStepUp(ref Guid pguidEventContext);
    int VolumeStepDown(ref Guid pguidEventContext);
    int QueryHardwareSupport(out uint pdwHardwareSupportMask);
    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}}

public static class AudioVolumeSetter {{
    public static void SetVolume(double volumePercent) {{
        var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")));
        IMMDevice device;
        int hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
        if (hr != 0 || device == null) {{
            throw new Exception($"GetDefaultAudioEndpoint failed: {{hr}}");
        }}

        object endpointObject;
        var endpointGuid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        hr = device.Activate(ref endpointGuid, 23, IntPtr.Zero, out endpointObject);
        if (hr != 0 || endpointObject == null) {{
            throw new Exception($"Activate failed: {{hr}}");
        }}

        var endpoint = (IAudioEndpointVolume)endpointObject;
        var scalar = Math.Max(0.0, Math.Min(1.0, volumePercent / 100.0));
        var eventContext = Guid.Empty;
        hr = endpoint.SetMasterVolumeLevelScalar((float)scalar, ref eventContext);
        if (hr != 0) {{
            throw new Exception($"SetMasterVolumeLevelScalar failed: {{hr}}");
        }}
    }}
}}
"@

            Add-Type -TypeDefinition $code -Language CSharp
            [AudioVolumeSetter]::SetVolume($Volume)
        }}

        if (-not (Set-Volume-WithAudioSwitcher)) {{
            Set-Volume-WithCom
        }}
        "Set system volume to $([math]::Round($Volume, 2))%"
        '''.strip()
    )


def main() -> int:
    if os.name != "nt":
        print("This utility only runs on Windows.")
        return 2

    pct = DEFAULT_VOLUME_PERCENT
    if len(sys.argv) > 1:
        try:
            pct = float(sys.argv[1])
        except ValueError:
            print(f"Invalid percentage argument, using default {DEFAULT_VOLUME_PERCENT}%")
            pct = DEFAULT_VOLUME_PERCENT

    pct = clamp_volume(pct)
    script_text = build_powershell_script(pct)

    with tempfile.NamedTemporaryFile("w", suffix=".ps1", delete=False, encoding="utf-8") as handle:
        handle.write(script_text)
        script_path = handle.name

    try:
        completed = subprocess.run(
            ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script_path],
            capture_output=True,
            text=True,
            check=False,
        )

        if completed.returncode != 0:
            if completed.stdout:
                print(completed.stdout.strip())
            if completed.stderr:
                print(completed.stderr.strip(), file=sys.stderr)
            print("Failed to set system volume")
            return completed.returncode or 2

        if completed.stdout:
            print(completed.stdout.strip())
        return 0
    finally:
        try:
            os.remove(script_path)
        except OSError:
            pass


if __name__ == "__main__":
    raise SystemExit(main())
