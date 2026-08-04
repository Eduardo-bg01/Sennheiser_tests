using System.Collections.Generic;
using System.Linq;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Catálogo de perfiles por modelo.
    /// - _btProfiles: modelos BT, USB-C, USB-A (Windows los reconoce por nombre propio).
    /// - _jackProfiles: modelos Jack 3.5 mm o genéricos (el operador elige el modelo).
    /// - GenericAudioNames: nombres genéricos que Windows asigna en algunos equipos.
    /// </summary>
    public static class DeviceProfileRegistry
    {
        // ════════════════════════════════════════════════════════════════════
        //  MODELOS CON NOMBRE PROPIO (Bluetooth, USB-C, USB-A)
        //  El nombre debe coincidir exactamente con el que aparece en Windows.
        // ════════════════════════════════════════════════════════════════════
        private static readonly List<DeviceProfile> _btProfiles = new()
        {
            new DeviceProfile("Momentum 4")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            new DeviceProfile("Momentum TW 4")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            // ── USB-C ────────────────────────────────────────────────────────
            new DeviceProfile("Headphones (HD 400U)")
            {
                HasBluetooth     = false,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },
             new DeviceProfile("ACCENTUM")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },
             new DeviceProfile("ACCENTUM PLUS")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },
              new DeviceProfile("HDB 630")
            {
                HasBluetooth     = true,
                HasPlayPause     = true,
                HasPreviousTrack = true,
                HasNextTrack     = true,
                HasVolumeUp      = true,
                HasVolumeDown    = true,
            },

            // ════════════════════════════════════════════════════════════════
            //  AGREGA AQUÍ MODELOS NUEVOS (BT, USB-C o USB-A):
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
        //  MODELOS JACK 3.5 MM Y GENÉRICOS
        //  Aparecen en Windows como "Headphone (Realtek(R) Audio)",
        //  "Speakers/Headphones", "Headphones", etc.
        //  El nombre aquí es el nombre comercial que el operador elige.
        // ════════════════════════════════════════════════════════════════════
        private static readonly string[] JackModelNames = new[]
        {
            "HD 550", "HD 560S", "HD 569", "HD 599", "HD 600", "HD 650", "HD 660S2",
            "HDR 175", "RS 120-W", "RS 195", "RS 275", "IE 200", "IE 600", "IE 900", "HD 400U",
        };

        private static readonly List<DeviceProfile> _jackProfiles =
            JackModelNames.Select(name => new DeviceProfile(name)
            {
                HasBluetooth     = false,
                HasPlayPause     = name == "HD 400U",
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            }).ToList();

        // ════════════════════════════════════════════════════════════════════
        //  NOMBRES GENÉRICOS DE WINDOWS
        //  En algunos equipos Windows asigna estos nombres en lugar del nombre
        //  del fabricante. Se tratan igual que el jack: combo de modelo.
        //  Si en algún equipo aparece con otro nombre genérico, agrégalo aquí.
        // ════════════════════════════════════════════════════════════════════
        private static readonly string[] GenericAudioNames = new[]
        {
            "Speakers/Headphones",
            "Speakers / Headphones",
            "Headphones",
        };

        // ── API pública ────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve el perfil si está registrado en _btProfiles; null si no.
        /// Usado por BluetoothDetector para dispositivos con nombre propio (USB-C, etc.).
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
        /// Busca el perfil en _btProfiles por nombre.
        /// Si no está registrado devuelve un perfil genérico con todo habilitado.
        /// </summary>
        public static DeviceProfile GetProfile(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return new DeviceProfile("(genérico)");

            foreach (var p in _btProfiles)
                if (p.ModelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            return new DeviceProfile(modelName);
        }

        /// <summary>
        /// Devuelve el perfil de un modelo jack/genérico por nombre comercial.
        /// </summary>
        public static DeviceProfile GetJackProfile(string jackModelName)
        {
            if (string.IsNullOrWhiteSpace(jackModelName))
                return new DeviceProfile("(jack genérico)") { HasBluetooth = false };

            foreach (var p in _jackProfiles)
                if (p.ModelName.Equals(jackModelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            return new DeviceProfile(jackModelName) { HasBluetooth = false };
        }

        /// <summary>
        /// Devuelve la lista de perfiles jack/genéricos para el combo de selección.
        /// </summary>
        public static List<DeviceProfile> GetWiredProfiles() => _jackProfiles;

        /// <summary>
        /// Devuelve true si el nombre es uno de los nombres genéricos de Windows
        /// (Speakers/Headphones, Headphones, etc.) que requieren selección manual.
        /// </summary>
        public static bool IsGenericAudioName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (var g in GenericAudioNames)
                if (name.Equals(g, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}