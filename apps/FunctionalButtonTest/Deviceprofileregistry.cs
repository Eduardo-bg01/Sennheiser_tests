using System.Collections.Generic;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Catálogo de perfiles por modelo.
    /// - Agrega un bloque en _btProfiles para modelos Bluetooth.
    /// - Agrega un bloque en _jackProfiles para modelos que se conectan por Jack 3.5 mm.
    ///   El nombre debe ser el nombre comercial/interno que tú elijas; el operador
    ///   lo seleccionará manualmente en la pantalla de selección de dispositivo.
    /// </summary>
    public static class DeviceProfileRegistry
    {
        // ════════════════════════════════════════════════════════════════════
        //  MODELOS BLUETOOTH
        //  El nombre debe coincidir con el nombre Bluetooth del dispositivo
        //  (el que aparece en la lista automática de DeviceSelectForm).
        // ════════════════════════════════════════════════════════════════════
        private static readonly List<DeviceProfile> _btProfiles = new()
        {
            // ── Bluetooth completo ───────────────────────────────────────────
            new DeviceProfile("Momentum 4")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            // ── Bluetooth sin botones de canción ────────────────────────────
            new DeviceProfile("Momentum TW 4")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            // ── USB-C (detectado automático por nombre de audio de Windows) ──
            new DeviceProfile("Headphones (HD 400U)")
            {
                HasBluetooth     = false,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            // ════════════════════════════════════════════════════════════════
            //  AGREGA AQUÍ MODELOS NUEVOS (BT, USB-C o cualquier tipo que
            //  Windows reconozca automáticamente por nombre):
            //
            //  new DeviceProfile("Nombre exacto como aparece en Windows")
            //  {
            //      HasBluetooth     = true/false,
            //      HasPlayPause     = true/false,
            //      HasPreviousTrack = true/false,
            //      HasNextTrack     = true/false,
            //      HasVolumeUp      = true/false,
            //      HasVolumeDown    = true/false,
            //  },
            // ════════════════════════════════════════════════════════════════
        };

        // ════════════════════════════════════════════════════════════════════
        //  MODELOS JACK 3.5 MM
        //  Todos aparecen en Windows como "Headphone (Realtek(R) Audio)".
        //  El nombre aquí es el nombre comercial que tú asignas; el operador
        //  lo elige en el combo de la pantalla de selección.
        // ════════════════════════════════════════════════════════════════════
        private static readonly List<DeviceProfile> _jackProfiles = new()
        {
            new DeviceProfile("HD 660S2")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

            new DeviceProfile("RS 195")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

             new DeviceProfile("RS 120-W")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

              new DeviceProfile("RS 275")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

             new DeviceProfile("HD 569")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

              new DeviceProfile("HD 599")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

               new DeviceProfile("HD 650")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

                new DeviceProfile("HD 550")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

                 new DeviceProfile("HD 600")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

              new DeviceProfile("HD 560S")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

              new DeviceProfile("RS 195")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },

               new DeviceProfile("HDR 175")
            {
                HasBluetooth     = false,   // sin prueba de conexión BT
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            },
            // ── Agrega aquí más modelos jack ─────────────────────────────────
            //
            //  new DeviceProfile("Nombre Comercial del Modelo Jack")
            //  {
            //      HasBluetooth     = false,  // siempre false para jack
            //      HasPlayPause     = true/false,
            //      HasPreviousTrack = true/false,
            //      HasNextTrack     = true/false,
            //      HasVolumeUp      = true/false,
            //      HasVolumeDown    = true/false,
            //  },
        };

        // ── API pública ────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve el perfil si el nombre está explícitamente registrado; null si no.
        /// Usado por BluetoothDetector para saber si un dispositivo de audio
        /// (USB-C, USB-A) debe aparecer en la lista.
        /// </summary>
        public static DeviceProfile GetProfileIfRegistered(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            foreach (var p in _btProfiles)
                if (p.ModelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;
            return null;
        }

        /// <summary>
        /// Busca el perfil para un dispositivo Bluetooth por nombre.
        /// Si no está registrado devuelve un perfil genérico con todo habilitado.
        /// </summary>
        public static DeviceProfile GetProfile(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return new DeviceProfile("(genérico)");

            foreach (var p in _btProfiles)
                if (p.ModelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            // Fallback: modelo desconocido → todas las pruebas activas
            return new DeviceProfile(modelName);
        }

        /// <summary>
        /// Devuelve el perfil de un modelo Jack por su nombre comercial.
        /// </summary>
        public static DeviceProfile GetJackProfile(string jackModelName)
        {
            if (string.IsNullOrWhiteSpace(jackModelName))
                return new DeviceProfile("(jack genérico)") { HasBluetooth = false };

            foreach (var p in _jackProfiles)
                if (p.ModelName.Equals(jackModelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            // Fallback: jack desconocido → sin BT, resto activo
            return new DeviceProfile(jackModelName) { HasBluetooth = false };
        }

        /// <summary>
        /// Devuelve la lista de perfiles jack para poblar el combo de selección.
        /// </summary>
        public static List<DeviceProfile> GetWiredProfiles() => _jackProfiles;
    }
}