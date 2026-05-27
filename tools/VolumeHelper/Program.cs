using AudioSwitcher.AudioApi.CoreAudio;

if (args.Length == 0 || !double.TryParse(args[0], out double volumePercent))
{
    Console.Error.WriteLine("Usage: VolumeHelper <volumePercent>");
    return 2;
}

volumePercent = Math.Max(0, Math.Min(100, volumePercent));

try
{
    var controller = new CoreAudioController();
    var device = controller.DefaultPlaybackDevice;

    if (device is null)
    {
        Console.Error.WriteLine("No default playback device found.");
        return 1;
    }

    device.Volume = volumePercent;
    device.Mute(false);

    Console.WriteLine($"Set default playback volume to {volumePercent}%.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
