using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Router central de señales multimedia.
    /// Captura comandos por 3 canales y los reenvía al AudioPlayer activo
    /// Y dispara el evento OnMediaKey para que los paneles los observen.
    /// </summary>
    internal static class AppCommandRouter
    {
        public static event Action<Keys> OnMediaKey;

        // AudioPlayer activo — se asigna desde MiniPlayerWidget
        public static AudioPlayer ActivePlayer { get; set; }

        private const int WM_HOTKEY    = 0x0312;
        private const int WM_APPCOMMAND= 0x0319;
        private const int WM_KEYDOWN   = 0x0100;

        private static IntPtr _hwnd = IntPtr.Zero;

        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static void Register(IntPtr hwnd)
        {
            _hwnd = hwnd;
            RegisterHotKey(hwnd, 0xB3, 0, 0xB3); // MediaPlayPause
            RegisterHotKey(hwnd, 0xB0, 0, 0xB0); // MediaNext
            RegisterHotKey(hwnd, 0xB1, 0, 0xB1); // MediaPrev
            RegisterHotKey(hwnd, 0xAF, 0, 0xAF); // VolumeUp
            RegisterHotKey(hwnd, 0xAE, 0, 0xAE); // VolumeDown
        }

        public static void Unregister()
        {
            if (_hwnd == IntPtr.Zero) return;
            UnregisterHotKey(_hwnd, 0xB3);
            UnregisterHotKey(_hwnd, 0xB0);
            UnregisterHotKey(_hwnd, 0xB1);
            UnregisterHotKey(_hwnd, 0xAF);
            UnregisterHotKey(_hwnd, 0xAE);
            _hwnd = IntPtr.Zero;
        }

        public static void Fire(Keys key)
        {
            // 1. Control the audio player
            DispatchToPlayer(key);
            // 2. Notify subscribers (panels)
            OnMediaKey?.Invoke(key);
        }

        private static void DispatchToPlayer(Keys key)
        {
            var p = ActivePlayer;
            if (p == null) return;
            switch (key)
            {
                case Keys.MediaPlayPause:     p.TogglePlayPause(); break;
                case Keys.MediaNextTrack:     p.NextTrack();       break;
                case Keys.MediaPreviousTrack: p.PreviousTrack();   break;
                case Keys.VolumeUp:           p.SetVolume(p.Volume + 0.05f); break;
                case Keys.VolumeDown:         p.SetVolume(p.Volume - 0.05f); break;
            }
        }

        public static bool ProcessMessage(ref Message m)
        {
            // WM_HOTKEY
            if (m.Msg == WM_HOTKEY)
            {
                int vk = m.WParam.ToInt32();
                Keys key = vk switch
                {
                    0xB3 => Keys.MediaPlayPause,
                    0xB0 => Keys.MediaNextTrack,
                    0xB1 => Keys.MediaPreviousTrack,
                    0xAF => Keys.VolumeUp,
                    0xAE => Keys.VolumeDown,
                    _    => Keys.None
                };
                if (key != Keys.None) { Fire(key); return true; }
            }

            // WM_APPCOMMAND (AVRCP directo)
            if (m.Msg == WM_APPCOMMAND)
            {
                int cmd = (int)(m.LParam.ToInt64() >> 16) & 0xFFF;
                Keys key = cmd switch
                {
                    14 => Keys.MediaPlayPause,
                    11 => Keys.MediaNextTrack,
                    12 => Keys.MediaPreviousTrack,
                    10 => Keys.VolumeUp,
                     9 => Keys.VolumeDown,
                     _  => Keys.None
                };
                if (key != Keys.None) { Fire(key); return true; }
            }

            // WM_KEYDOWN
            if (m.Msg == WM_KEYDOWN)
            {
                Keys key = (Keys)m.WParam.ToInt32();
                if (key == Keys.MediaPlayPause    ||
                    key == Keys.MediaNextTrack    ||
                    key == Keys.MediaPreviousTrack||
                    key == Keys.VolumeUp          ||
                    key == Keys.VolumeDown)
                {
                    Fire(key);
                }
            }

            return false;
        }
    }
}
