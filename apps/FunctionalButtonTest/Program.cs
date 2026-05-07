using System;
using System.Linq;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Intentar auto-seleccionar si hay exactamente un dispositivo conectado
            try
            {
                var devices = BluetoothDetector.GetPairedDevices();
                var connected = devices.Where(d => d.IsConnected).ToList();
                if (connected.Count == 1)
                {
                    var device = connected[0];
                    DeviceAssets.DeviceName = device.Name;
                    var mainForm = new MainForm();
                    mainForm.Session.SelectedDevice = device;
                    Application.Run(mainForm);
                    return;
                }
            }
            catch { /* no bloquear, caerá a selección manual */ }

            using var selectForm = new DeviceSelectForm();
            if (selectForm.ShowDialog() != DialogResult.OK)
                return;

            DeviceAssets.DeviceName = selectForm.SelectedDevice?.Name ?? string.Empty;

            var mainForm = new MainForm();
            mainForm.Session.SelectedDevice = selectForm.SelectedDevice;
            Application.Run(mainForm);
        }
    }
}