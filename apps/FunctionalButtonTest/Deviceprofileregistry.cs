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
        //  El nombre debe ser el que aparece en Windows. GetProfile tolera que
        //  Windows lo reporte con un sufijo/ prefijo distinto ("X (Refurb)") y
        //  aun así cae en el perfil correcto.
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
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
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

                new DeviceProfile("HD 400U")
              {
                  HasBluetooth     = false,
                  HasPlayPause     = true,
                  HasPreviousTrack = false,
                  HasNextTrack     = false,
                  HasVolumeUp      = false,
                  HasVolumeDown    = false,
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
        //  El nombre aquí es la etiqueta que elige el operador y lo que queda
        //  escrito en "Dispositivo :" del reporte.
        //  Orden: agrupado por familia (HD, HDR, RS, IE) porque el operador
        //  elige por familia y las pruebas son iguales dentro de cada bloque.
        //
        //  ATENCIÓN: agrupar por familia es válido porque el nombre del modelo
        //  NUNCA viaja al XML (converter.py solo sube serial + resultados), y
        //  las demas apps solo lo leen como interruptor de comportamiento:
        //  run.bat (HD|IE)[0-9] y prefijo RS, AudioTest (prefijo rs).
        //  Ese mismo criterio de prefijo hace imposible que estas etiquetas se
        //  desincronicen de las demas apps — antes habia 3 listas de modelos
        //  sin volumen que habia que mantener a mano y ya se habian separado.
        // ════════════════════════════════════════════════════════════════════
        private static readonly string[] JackModelNames = new[]
        {
            "HD 550 / 560S / 569 / 599 / 600 / 650 / 660S2",
            "HD 400U",
            "HDR 175",
            "RS 120-W",
            "RS 195",
            "RS 255",
            "RS 275",
            "IE 200 / 600 / 900",
        };

        // Etiquetas jack cuyo cable tiene botón de Play / Pausa.
        // El resto no tiene botones: solo se prueban en audio y niveles.
        // HD 400U va aparte del grupo HD justamente por esto.
        private static readonly HashSet<string> JackWithPlayPause =
            new() { "HD 400U" };

        private static readonly List<DeviceProfile> _jackProfiles =
            JackModelNames.Select(name => new DeviceProfile(name)
            {
                HasBluetooth     = false,
                HasPlayPause     = JackWithPlayPause.Contains(name),
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
        /// 1) Coincidencia exacta (case-insensitive): el caso normal.
        /// 2) Si no hay exacta, coincidencia por subcadena quedándose con la MÁS
        ///    LARGA. Cubre los equipos donde Windows reporta el dispositivo con otro
        ///    nombre ("Momentum 4 (Refurb)", "ACCENTUM PLUS usado"...).
        ///    Gana la más larga porque "ACCENTUM" es subcadena de "ACCENTUM PLUS"
        ///    y esos dos perfiles NO tienen los mismos botones.
        /// Si no hay ninguna, devuelve un perfil genérico con todo habilitado, que
        /// es la falla segura en un banco de refaccion: nunca se salta una prueba.
        /// </summary>
        public static DeviceProfile GetProfile(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return new DeviceProfile("(genérico)");

            foreach (var p in _btProfiles)
                if (p.ModelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            DeviceProfile best = null;
            foreach (var p in _btProfiles)
            {
                if (modelName.IndexOf(p.ModelName, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (best == null || p.ModelName.Length > best.ModelName.Length)
                    best = p;
            }
            if (best != null)
                return best;

            return new DeviceProfile(modelName);
        }

        /// <summary>
        /// Devuelve el perfil de un modelo jack/genérico por nombre comercial.
        /// </summary>
        public static DeviceProfile GetJackProfile(string jackModelName)
        {
            string name = string.IsNullOrWhiteSpace(jackModelName)
                ? "(jack genérico)"
                : jackModelName;

            foreach (var p in _jackProfiles)
                if (p.ModelName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return p;

            // Un modelo jack desconocido NO hereda los flags true por defecto de
            // DeviceProfile: se trata como "sin pruebas de botones", que es lo
            // seguro para un audífono alambrico.
            return new DeviceProfile(name)
            {
                HasBluetooth     = false,
                HasPlayPause     = false,
                HasPreviousTrack = false,
                HasNextTrack     = false,
                HasVolumeUp      = false,
                HasVolumeDown    = false,
            };
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