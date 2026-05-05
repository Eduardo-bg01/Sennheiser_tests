using System;
using NAudio.CoreAudioApi;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Monitors the system default audio endpoint volume in real time.
    /// </summary>
    public class VolumeMonitor : IDisposable
    {
        public event Action<float> VolumeChanged;

        private MMDeviceEnumerator _enumerator;
        private MMDevice _device;
        private AudioEndpointVolume _vol;
        private AudioEndpointVolumeCallback _callback;

        public float CurrentVolume => _vol != null ? _vol.MasterVolumeLevelScalar * 100f : 0f;

        public VolumeMonitor()
        {
            try
            {
                _enumerator = new MMDeviceEnumerator();
                _device     = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _vol        = _device.AudioEndpointVolume;
                _callback   = new AudioEndpointVolumeCallback(v =>
                    VolumeChanged?.Invoke(v.MasterVolume * 100f));
                _vol.OnVolumeNotification += _callback.OnNotify;
            }
            catch { /* No audio device available */ }
        }

        public void Dispose()
        {
            if (_vol != null && _callback != null)
                _vol.OnVolumeNotification -= _callback.OnNotify;
            _device?.Dispose();
            _enumerator?.Dispose();
        }
    }

    internal class AudioEndpointVolumeCallback
    {
        private readonly Action<AudioVolumeNotificationData> _action;
        public AudioEndpointVolumeCallback(Action<AudioVolumeNotificationData> action) => _action = action;
        public void OnNotify(AudioVolumeNotificationData data) => _action(data);
    }
}
