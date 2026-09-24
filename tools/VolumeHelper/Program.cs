using NAudio.CoreAudioApi;

if (args.Length == 0 || !double.TryParse(args[0], out double volumePercent))
{
    Console.Error.WriteLine("Usage: VolumeHelper <volumePercent>");
    return 2;
}

volumePercent = Math.Max(0, Math.Min(100, volumePercent));

try
{
    using var enumerator = new MMDeviceEnumerator();
    using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

    if (device is null)
    {
        Console.Error.WriteLine("No default playback device found.");
        return 1;
    }

    device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)(volumePercent / 100.0);
    device.AudioEndpointVolume.Mute = false;

    Console.WriteLine($"Set default playback volume to {volumePercent}%.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}