using System;
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