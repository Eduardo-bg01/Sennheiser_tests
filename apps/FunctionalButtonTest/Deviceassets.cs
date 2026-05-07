using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public static class DeviceAssets
    {
        // Se asigna en Program.cs con el valor de SelectedDevice.Name
        // Ejemplo: "TRUEFREE 01", "Momentum 4"
        public static string DeviceName { get; set; } = string.Empty;

        // ── DEBUG temporal: muestra todos los recursos embebidos ─────────────
        // Llama a DeviceAssets.DumpResources() en Program.cs para diagnosticar.
        public static void DumpResources()
        {
            var all = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string list = string.Join("\n", all);
            MessageBox.Show(
                $"DeviceName: \"{DeviceName}\"\n\nRecursos:\n{list}",
                "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Imagen del dispositivo ───────────────────────────────────────────
        public static Image LoadDeviceImage()
        {
            if (string.IsNullOrWhiteSpace(DeviceName)) return null;

            // Busca cualquier archivo llamado device.* dentro de una carpeta
            // cuyo segmento coincida con el nombre del dispositivo (ignora mayúsculas).
            // No importa cómo el SDK transformó los espacios — comparamos segmento a segmento.
            return FindResource(DeviceName, new[] { "device.jpg", "device.png", "device.jpeg" });
        }

        // ── GIFs de prueba ───────────────────────────────────────────────────
        public static Image LoadGif(string gifName)
        {
            // Primero busca en la carpeta del dispositivo
            if (!string.IsNullOrWhiteSpace(DeviceName))
            {
                var img = FindResource(DeviceName, new[] { gifName });
                if (img != null) return img;
            }

            // Fallback: GIF genérico en la raíz de assets (sin subcarpeta)
            var genericImg = FindResource(null, new[] { gifName });
            if (genericImg != null) return genericImg;

            // Fallback final: usar Momentum 4 como GIF por defecto para modelos no identificados
            if (DeviceName != "Momentum 4")
            {
                var defaultImg = FindResource("Momentum 4", new[] { gifName });
                if (defaultImg != null) return defaultImg;
            }

            return null;
        }

        // ────────────────────────────────────────────────────────────────────
        //  BUSQUEDA ROBUSTA
        //  En lugar de construir el nombre del recurso y esperar que coincida,
        //  recorremos todos los recursos embebidos y comparamos cada segmento
        //  del nombre por separado — así los espacios, guiones bajos u otras
        //  transformaciones que aplique el SDK no nos afectan.
        //
        //  Un recurso embebido tiene esta forma:
        //    BluetoothHeadphoneTest.assets.TRUEFREE_01.device.jpg
        //  Sus segmentos separados por punto son:
        //    [0] BluetoothHeadphoneTest
        //    [1] assets
        //    [2] TRUEFREE_01          ← aquí comparamos contra DeviceName normalizado
        //    [3] device.jpg           ← aquí comparamos contra fileName
        // ────────────────────────────────────────────────────────────────────
        private static Image FindResource(string subfolder, string[] fileNames)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allResources = assembly.GetManifestResourceNames();

            // Normaliza el nombre de carpeta igual que lo hace el SDK:
            // espacios → guión bajo. También quitamos acentos o caracteres raros.
            string folderKey = subfolder != null
                ? Normalize(subfolder)
                : null;

            foreach (string fileName in fileNames)
            {
                foreach (string res in allResources)
                {
                    // El recurso debe contener "assets" en su ruta
                    if (!res.Contains(".assets.")) continue;

                    // Extraer la parte después de ".assets."
                    int assetsIdx = res.IndexOf(".assets.") + ".assets.".Length;
                    string afterAssets = res.Substring(assetsIdx); // ej: "TRUEFREE_01.device.jpg"

                    if (folderKey != null)
                    {
                        // Con subcarpeta: debe ser exactamente "{folder}.{fileName}"
                        string expected = $"{folderKey}.{fileName}";
                        if (string.Equals(afterAssets, expected, StringComparison.OrdinalIgnoreCase))
                            return LoadStream(assembly, res);
                    }
                    else
                    {
                        // Sin subcarpeta: debe ser exactamente "{fileName}" (solo un segmento)
                        if (string.Equals(afterAssets, fileName, StringComparison.OrdinalIgnoreCase))
                            return LoadStream(assembly, res);
                    }
                }
            }

            return null;
        }

        // Aplica la misma normalización que el SDK de .NET usa al embeber recursos:
        // espacios y guiones → guión bajo. Puedes agregar más reglas si aparecen.
        private static string Normalize(string name)
            => name.Replace(' ', '_').Replace('-', '_');

        private static Image LoadStream(Assembly assembly, string resName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resName);
                if (stream == null) return null;
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return Image.FromStream(ms);
            }
            catch { return null; }
        }
    }
}