using System;
using System.IO;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    internal static class Program
    {
        private const string CRASH_LOG = "crash_BluetoothHeadphoneTest.log";

        [STAThread]
        static void Main()
        {
            // Si la maquina tiene un depurador JIT registrado (por ejemplo AeDebug de
            // un third-party), una excepcion no manejada solo muestra un popup sin texto
            // util y run.bat reporta "[CONTROLS] FAILED" sin decir por que.
            // Este log deja la causa escrita junto al run.bat.
            //
            // ThrowException mantiene el comportamiento actual: la excepcion sigue
            // matando el proceso (y run.bat reintentando). NO usar ThreadException,
            // que se la tragaria y dejaria la app viva en estado roto.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                WriteCrashLog(e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var selectForm = new DeviceSelectForm();
            if (selectForm.ShowDialog() != DialogResult.OK)
                return;

            DeviceAssets.DeviceName = selectForm.SelectedDevice?.Name ?? string.Empty;

            var mainForm = new MainForm();
            mainForm.Session.SelectedDevice = selectForm.SelectedDevice;
            Application.Run(mainForm);
        }

        private static void WriteCrashLog(Exception ex)
        {
            try
            {
                File.AppendAllText(CRASH_LOG,
                    $"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\r\n{ex}\r\n\r\n");
            }
            catch { /* un log fallido no debe tapar el error real */ }
        }
    }
}