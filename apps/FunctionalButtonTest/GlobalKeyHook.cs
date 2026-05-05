using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    /// <summary>
    /// Hook de teclado de bajo nivel como canal secundario.
    /// El canal principal es AppCommandRouter (WM_APPCOMMAND desde MainForm).
    /// </summary>
    public class GlobalKeyHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN     = 0x0100;

        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;

        public GlobalKeyHook()
        {
            _proc = HookCallback;
            try
            {
                using var curProcess = Process.GetCurrentProcess();
                using var curModule  = curProcess.MainModule;
                _hookId = NativeMethods.SetWindowsHookEx(
                    WH_KEYBOARD_LL, _proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
            catch { }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;
                if (key == Keys.MediaPlayPause    ||
                    key == Keys.MediaPreviousTrack ||
                    key == Keys.MediaNextTrack    ||
                    key == Keys.VolumeUp          ||
                    key == Keys.VolumeDown)
                {
                    // Reenviar al router central
                    AppCommandRouter.Fire(key);
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }

    internal static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
