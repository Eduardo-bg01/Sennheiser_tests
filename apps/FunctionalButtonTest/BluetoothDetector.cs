using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BluetoothHeadphoneTest
{
    public enum DeviceConnectionType { Bluetooth, WiredJack }

    public class BluetoothDeviceInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }  // MAC para BT, "JACK" para auxiliar
        public bool IsConnected { get; set; }
        public DeviceConnectionType ConnectionType { get; set; } = DeviceConnectionType.Bluetooth;

        /// <summary>
        /// Para audífonos jack con múltiples modelos, aquí se guarda
        /// el modelo elegido por el operador en el paso de selección.
        /// </summary>
        public string SelectedJackModel { get; set; }

        public bool IsWired => ConnectionType == DeviceConnectionType.WiredJack;

        public override string ToString() => IsWired
            ? (string.IsNullOrEmpty(SelectedJackModel)
                ? $"🎧  {Name}  (Jack 3.5 mm)"
                : $"🎧  {SelectedJackModel}  (Jack 3.5 mm)")
            : (IsConnected
                ? $"{Name}  ✔ Conectado"
                : $"{Name}  (no conectado)");
    }

    public static class BluetoothDetector
    {
        // ── Win32 structs ──────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_FIND_RADIO_PARAMS { public uint dwSize; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_RADIO_INFO
        {
            public uint dwSize;
            public ulong address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
            public uint ulClassofDevice;
            public ushort lmpSubversion;
            public ushort manufacturer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public uint dwSize;
            public bool fReturnAuthenticated;
            public bool fReturnRemembered;
            public bool fReturnUnknown;
            public bool fReturnConnected;
            public bool fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_INFO
        {
            public uint dwSize;
            public ulong Address;
            public uint ulClassofDevice;
            public bool fConnected;
            public bool fRemembered;
            public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear, wMonth, wDayOfWeek, wDay,
                          wHour, wMinute, wSecond, wMilliseconds;
        }

        // ── P/Invoke ───────────────────────────────────────────────────────────
        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern IntPtr BluetoothFindFirstRadio(
            ref BLUETOOTH_FIND_RADIO_PARAMS p, out IntPtr phRadio);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern bool BluetoothFindNextRadio(IntPtr hFind, out IntPtr phRadio);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern bool BluetoothFindRadioClose(IntPtr hFind);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS pSearchParams,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern bool BluetoothFindNextDevice(
            IntPtr hFind, ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern bool BluetoothFindDeviceClose(IntPtr hFind);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // ── Nombre de dispositivo que Windows asigna al jack 3.5 mm ───────────
        // Si en tu equipo aparece con otro nombre, agrégalo aquí.
        // Solo los dispositivos Realtek que SÍ son audífonos (sin Speakers).
        // Agrega aquí si en tu equipo el jack aparece con otro nombre.
        private static readonly string[] WiredJackNames = new[]
        {
            "Headphone (Realtek(R) Audio)",
            "Headset (Realtek(R) Audio)"
        };

        // Palabras que identifican parlantes/bocinas — se excluyen siempre.
        private static readonly string[] SpeakerKeywords = new[]
        {
            "Speaker", "Altavoz", "Bocina"
        };

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve todos los dispositivos disponibles:
        /// - Dispositivos Bluetooth pareados/conectados
        /// - Dispositivo de salida Jack 3.5 mm si está activo (Realtek Audio)
        /// </summary>
        public static List<BluetoothDeviceInfo> GetPairedDevices()
        {
            var list = new List<BluetoothDeviceInfo>();

            // 1) Dispositivos Bluetooth
            try
            {
                var radioParams = new BLUETOOTH_FIND_RADIO_PARAMS
                { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>() };
                var hRadioFind = BluetoothFindFirstRadio(ref radioParams, out IntPtr hRadio);
                if (hRadioFind != IntPtr.Zero)
                {
                    do
                    {
                        EnumerateDevices(hRadio, list);
                        CloseHandle(hRadio);
                    }
                    while (BluetoothFindNextRadio(hRadioFind, out hRadio));

                    BluetoothFindRadioClose(hRadioFind);
                }
            }
            catch
            {
                list.AddRange(GetDevicesFromRegistry());
            }

            // 2) Dispositivos de audio cableados (Jack 3.5mm, USB-C, USB-A)
            //    Se detectan vía NAudio enumerando todos los endpoints activos
            //    y se cruzan contra los perfiles registrados en DeviceProfileRegistry.
            var wiredDevices = DetectWiredAudioDevices();
            list.AddRange(wiredDevices);

            return list;
        }

        /// <summary>
        /// Detecta dispositivos de audio cableados activos (jack, USB-C, USB-A, etc.)
        /// usando NAudio MMDeviceEnumerator. Hay dos categorías:
        ///   - Jack Realtek → ConnectionType.WiredJack (requiere selección manual de modelo)
        ///   - Cualquier otro con perfil registrado → ConnectionType.Bluetooth (nombre propio)
        /// </summary>
        private static List<BluetoothDeviceInfo> DetectWiredAudioDevices()
        {
            var result = new List<BluetoothDeviceInfo>();
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.DeviceState.Active);

                foreach (var dev in devices)
                {
                    string friendlyName = dev.FriendlyName ?? "";

                    // ── Excluir parlantes/bocinas siempre ─────────────────────
                    bool isSpeaker = false;
                    foreach (var kw in SpeakerKeywords)
                        if (friendlyName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        { isSpeaker = true; break; }
                    if (isSpeaker) continue;

                    // ── Jack Realtek: requiere selección manual de modelo ──────
                    bool isRealtek = false;
                    foreach (var wiredName in WiredJackNames)
                    {
                        if (friendlyName.IndexOf(wiredName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            wiredName.IndexOf(friendlyName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isRealtek = true;
                            break;
                        }
                    }

                    if (isRealtek)
                    {
                        result.Add(new BluetoothDeviceInfo
                        {
                            Name = friendlyName,
                            Address = "JACK",
                            IsConnected = true,
                            ConnectionType = DeviceConnectionType.WiredJack,
                            SelectedJackModel = null
                        });
                        continue;
                    }

                    // ── USB-C / USB-A: nombre propio registrado en perfiles ───
                    var profile = DeviceProfileRegistry.GetProfileIfRegistered(friendlyName);
                    if (profile != null)
                    {
                        result.Add(new BluetoothDeviceInfo
                        {
                            Name = friendlyName,
                            Address = "USB",
                            IsConnected = true,
                            ConnectionType = DeviceConnectionType.Bluetooth  // flujo automático
                        });
                    }
                }
            }
            catch { }
            return result;
        }

        private static void EnumerateDevices(IntPtr hRadio, List<BluetoothDeviceInfo> list)
        {
            var sp = new BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
                fReturnAuthenticated = true,
                fReturnRemembered = true,
                fReturnConnected = true,
                fReturnUnknown = false,
                fIssueInquiry = false,
                cTimeoutMultiplier = 2,
                hRadio = hRadio
            };

            var devInfo = new BLUETOOTH_DEVICE_INFO
            { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };

            var hDevFind = BluetoothFindFirstDevice(ref sp, ref devInfo);
            if (hDevFind == IntPtr.Zero) return;

            do
            {
                var addr = devInfo.Address;
                var mac = $"{(addr >> 40) & 0xFF:X2}:{(addr >> 32) & 0xFF:X2}:" +
                           $"{(addr >> 24) & 0xFF:X2}:{(addr >> 16) & 0xFF:X2}:" +
                           $"{(addr >> 8) & 0xFF:X2}:{addr & 0xFF:X2}";

                list.Add(new BluetoothDeviceInfo
                {
                    Name = string.IsNullOrWhiteSpace(devInfo.szName) ? $"Dispositivo {mac}" : devInfo.szName,
                    Address = mac,
                    IsConnected = devInfo.fConnected,
                    ConnectionType = DeviceConnectionType.Bluetooth
                });

                devInfo = new BLUETOOTH_DEVICE_INFO
                { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };
            }
            while (BluetoothFindNextDevice(hDevFind, ref devInfo));

            BluetoothFindDeviceClose(hDevFind);
        }

        private static List<BluetoothDeviceInfo> GetDevicesFromRegistry()
        {
            var list = new List<BluetoothDeviceInfo>();
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices");
                if (key == null) return list;

                foreach (var subName in key.GetSubKeyNames())
                {
                    using var sub = key.OpenSubKey(subName);
                    if (sub == null) continue;

                    string name = null;
                    var rawName = sub.GetValue("Name");
                    if (rawName is byte[] bytes)
                        name = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    else if (rawName is string s)
                        name = s;

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string mac = subName.Length == 12
                        ? $"{subName[0..2]}:{subName[2..4]}:{subName[4..6]}:{subName[6..8]}:{subName[8..10]}:{subName[10..12]}"
                        : subName;

                    list.Add(new BluetoothDeviceInfo
                    {
                        Name = name,
                        Address = mac.ToUpper(),
                        IsConnected = false
                    });
                }
            }
            catch { }
            return list;
        }

        public static bool IsDeviceConnected(string macAddress)
        {
            try
            {
                var devices = GetPairedDevices();
                foreach (var d in devices)
                    if (string.Equals(d.Address, macAddress, StringComparison.OrdinalIgnoreCase))
                        return d.IsConnected;
            }
            catch { }
            return false;
        }
    }
}