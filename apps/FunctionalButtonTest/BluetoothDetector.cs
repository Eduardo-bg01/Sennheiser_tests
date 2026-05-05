using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BluetoothHeadphoneTest
{
    public class BluetoothDeviceInfo
    {
        public string Name        { get; set; }
        public string Address     { get; set; }   // MAC como string "XX:XX:XX:XX:XX:XX"
        public bool   IsConnected { get; set; }

        public override string ToString() =>
            IsConnected ? $"{Name}  ✔ Conectado" : $"{Name}  (no conectado)";
    }

    public static class BluetoothDetector
    {
        // ── Win32 structs ──────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_FIND_RADIO_PARAMS { public uint dwSize; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_RADIO_INFO
        {
            public uint    dwSize;
            public ulong   address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string  szName;
            public uint    ulClassofDevice;
            public ushort  lmpSubversion;
            public ushort  manufacturer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public uint  dwSize;
            public bool  fReturnAuthenticated;
            public bool  fReturnRemembered;
            public bool  fReturnUnknown;
            public bool  fReturnConnected;
            public bool  fIssueInquiry;
            public byte  cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_INFO
        {
            public uint    dwSize;
            public ulong   Address;
            public uint    ulClassofDevice;
            public bool    fConnected;
            public bool    fRemembered;
            public bool    fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string  szName;
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

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve todos los dispositivos BT pareados (y/o conectados) visibles en el radio local.
        /// </summary>
        public static List<BluetoothDeviceInfo> GetPairedDevices()
        {
            var list = new List<BluetoothDeviceInfo>();
            try
            {
                // Obtener el primer radio BT
                var radioParams = new BLUETOOTH_FIND_RADIO_PARAMS
                    { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>() };
                var hRadioFind = BluetoothFindFirstRadio(ref radioParams, out IntPtr hRadio);
                if (hRadioFind == IntPtr.Zero) return list;

                do
                {
                    EnumerateDevices(hRadio, list);
                    CloseHandle(hRadio);
                }
                while (BluetoothFindNextRadio(hRadioFind, out hRadio));

                BluetoothFindRadioClose(hRadioFind);
            }
            catch
            {
                // Fallback: leer del registro si BluetoothApis falla
                list.AddRange(GetDevicesFromRegistry());
            }
            return list;
        }

        private static void EnumerateDevices(IntPtr hRadio, List<BluetoothDeviceInfo> list)
        {
            var sp = new BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize               = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
                fReturnAuthenticated = true,
                fReturnRemembered    = true,
                fReturnConnected     = true,
                fReturnUnknown       = false,
                fIssueInquiry        = false,
                cTimeoutMultiplier   = 2,
                hRadio               = hRadio
            };

            var devInfo = new BLUETOOTH_DEVICE_INFO
                { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };

            var hDevFind = BluetoothFindFirstDevice(ref sp, ref devInfo);
            if (hDevFind == IntPtr.Zero) return;

            do
            {
                var addr = devInfo.Address;
                var mac  = $"{(addr >> 40) & 0xFF:X2}:{(addr >> 32) & 0xFF:X2}:" +
                           $"{(addr >> 24) & 0xFF:X2}:{(addr >> 16) & 0xFF:X2}:" +
                           $"{(addr >>  8) & 0xFF:X2}:{addr & 0xFF:X2}";

                list.Add(new BluetoothDeviceInfo
                {
                    Name        = string.IsNullOrWhiteSpace(devInfo.szName) ? $"Dispositivo {mac}" : devInfo.szName,
                    Address     = mac,
                    IsConnected = devInfo.fConnected
                });

                devInfo = new BLUETOOTH_DEVICE_INFO
                    { dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>() };
            }
            while (BluetoothFindNextDevice(hDevFind, ref devInfo));

            BluetoothFindDeviceClose(hDevFind);
        }

        /// <summary>Fallback: leer dispositivos del registro de Windows.</summary>
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

                    // El nombre puede estar como byte[] o string
                    string name = null;
                    var rawName = sub.GetValue("Name");
                    if (rawName is byte[] bytes)
                        name = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    else if (rawName is string s)
                        name = s;

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Formatear MAC desde el nombre de subclave
                    string mac = subName.Length == 12
                        ? $"{subName[0..2]}:{subName[2..4]}:{subName[4..6]}:{subName[6..8]}:{subName[8..10]}:{subName[10..12]}"
                        : subName;

                    list.Add(new BluetoothDeviceInfo
                    {
                        Name        = name,
                        Address     = mac.ToUpper(),
                        IsConnected = false  // Registro no indica estado en tiempo real
                    });
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// Verifica si el dispositivo seleccionado (por dirección MAC) está actualmente conectado.
        /// </summary>
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
