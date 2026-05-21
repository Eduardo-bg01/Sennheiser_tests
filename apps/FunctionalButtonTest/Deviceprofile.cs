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

        /// <summary>
        /// Cantidad total de pasos: 1 preparación + pruebas activas del perfil.
        /// </summary>
        public int TotalSteps
        {
            get
            {
                int count = 1; // HeadphonesOnPanel siempre presente
                if (HasBluetooth) count++;
                if (HasPlayPause) count++;
                if (HasPreviousTrack) count++;
                if (HasNextTrack) count++;
                if (HasVolumeUp) count++;
                if (HasVolumeDown) count++;
                return count;
            }
        }

        /// <summary>
        /// Pruebas reales (sin contar el paso de preparación).
        /// </summary>
        public int TotalRealTests => TotalSteps - 1;
    }
}