using System;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Define qué pruebas aplican a un modelo específico.
    /// Agrega una entrada en DeviceProfileRegistry por cada modelo nuevo.
    /// </summary>
    public class DeviceProfile
    {
        public string ModelName { get; }
        public bool HasBluetooth { get; init; } = true;
        public bool HasPlayPause { get; init; } = true;
        public bool HasPreviousTrack { get; init; } = true;
        public bool HasNextTrack { get; init; } = true;
        public bool HasVolumeUp { get; init; } = true;
        public bool HasVolumeDown { get; init; } = true;

        public DeviceProfile(string modelName)
        {
            ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        }
    }
}