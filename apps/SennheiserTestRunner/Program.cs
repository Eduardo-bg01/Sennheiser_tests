using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioSwitcher.AudioApi.CoreAudio;

namespace SennheiserTestRunner;

static class Program
{
    static string BaseDir => AppContext.BaseDirectory;
    static int MaxRetries => 5;
    static int RetryDelayMs => 2000;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Environment.CurrentDirectory = BaseDir;
        Console.WriteLine($"Working directory: {BaseDir}");

        KillOldProcesses();
        CleanOldFiles();

        var refurbishTool = Path.Combine(BaseDir, "RefurbishToolArvato", "RefurbishTool.exe");
        if (File.Exists(refurbishTool))
        {
            Console.WriteLine("Opening RefurbishTool...");
            RunProcess(refurbishTool, "", wait: true);
        }

        ShowBluetoothConnectPrompt();

        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText("tiempo1.txt", startTime.ToString());

        string? serial = GetSerial();
        if (serial is null)
        {
            Console.Error.WriteLine("No serial provided. Exiting.");
            Environment.Exit(1);
        }

        string? deviceName = RunControlsTest();
        if (deviceName is null)
        {
            Console.Error.WriteLine("[CONTROLS] Failed - max retries exceeded");
            Environment.Exit(3);
        }

        RunAudioTest();

        RunMicrophoneTest();

        int levelVolume = string.Equals(deviceName, "MOMENTUM TW 4", StringComparison.OrdinalIgnoreCase) ? 80 : 100;
        SetVolume(levelVolume);

        RunLevelTest();

        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText("tiempo2.txt", endTime.ToString());

        RunResultsScripts();

        CleanupBluetooth();
        ShowBluetoothDisconnectPrompt();

        double minutes = Math.Round((endTime - startTime) / 60000.0, 2);
        File.WriteAllText("diferencia_minutos.txt", minutes.ToString());
        Console.WriteLine($"Tests completed. Total time: {minutes} min");
    }

    static void KillOldProcesses()
    {
        foreach (var name in new[] { "AskForSerial2", "AudioTest", "BluetoothHeadphoneTest", "MicroTestCloud", "LevelTest" })
        {
            try { foreach (var p in Process.GetProcessesByName(name)) p.Kill(); } catch { }
        }
    }

    static void CleanOldFiles()
    {
        foreach (var pattern in new[] { "Prueba_*", "results.json", "MicroTest_*", "test_results*", "hearingPass*", "recorded*", "final_results*", "tiempo*", "diferen*" })
        {
            foreach (var f in System.IO.Directory.GetFiles(BaseDir, pattern))
            {
                try { System.IO.File.Delete(f); } catch { }
            }
        }
    }

    static void ShowBluetoothConnectPrompt()
    {
        var ps = Path.Combine(BaseDir, "show_bluetooth_connect.ps1");
        if (File.Exists(ps))
            RunProcess("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{ps}\"", wait: true);
    }

    static void ShowBluetoothDisconnectPrompt()
    {
        var ps = Path.Combine(BaseDir, "show_bluetooth_disconnect.ps1");
        if (File.Exists(ps))
            RunProcess("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{ps}\"", wait: true);
    }

    static string? GetSerial()
    {
        using var form = new AskForSerial2.Form1();
        form.ShowDialog();

        var serialFile = Path.Combine(BaseDir, "serial.txt");
        return File.Exists(serialFile) ? File.ReadAllText(serialFile).Trim() : null;
    }

    static string? RunControlsTest()
    {
        Console.WriteLine("Setting volume to 50% before controls test...");
        SetVolume(50);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Console.WriteLine($"Controls test attempt {attempt}/{MaxRetries}...");

            using var selectForm = new BluetoothHeadphoneTest.DeviceSelectForm();
            if (selectForm.ShowDialog() != DialogResult.OK)
            {
                Console.Error.WriteLine("Device selection cancelled.");
                return null;
            }

            BluetoothHeadphoneTest.DeviceAssets.DeviceName = selectForm.SelectedDevice?.Name ?? string.Empty;

            using var mainForm = new BluetoothHeadphoneTest.MainForm();
            mainForm.Session.SelectedDevice = selectForm.SelectedDevice;
            mainForm.ShowDialog();

            var resultFile = WaitForFile("Prueba_*.txt", 5);
            if (resultFile is not null)
            {
                Console.WriteLine("[CONTROLS] PASSED");
                string? deviceName = ParseDeviceName(resultFile);
                Console.WriteLine($"Device: {deviceName}");
                Console.WriteLine("Setting volume to 100% before audio test...");
                SetVolume(100);
                return deviceName;
            }

            Console.WriteLine($"[CONTROLS] FAILED - attempt {attempt}/{MaxRetries}");
        }

        return null;
    }

    static void RunAudioTest()
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Console.WriteLine($"Audio test attempt {attempt}/{MaxRetries}...");

            using var form = new AudioTest.Form1();
            form.ShowDialog();

            Thread.Sleep(RetryDelayMs);

            if (System.IO.Directory.GetFiles(BaseDir, "hearingPass*.txt").Length > 0)
            {
                Console.WriteLine("[AUDIO] PASSED");
                Console.WriteLine("Setting volume to 100% before microphone test...");
                SetVolume(100);
                return;
            }

            Console.WriteLine($"[AUDIO] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Console.Error.WriteLine("[AUDIO] FAILED - max retries exceeded");
        Environment.Exit(2);
    }

    static void RunMicrophoneTest()
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Console.WriteLine($"Microphone test attempt {attempt}/{MaxRetries}...");

            using var form = new MicroTestCloud.Form1();
            form.ShowDialog();

            var resultFile = WaitForFile("MicroTest_*.txt", 5);
            if (resultFile is not null)
            {
                Console.WriteLine("[MICROPHONE] PASSED");
                return;
            }

            Console.WriteLine($"[MICROPHONE] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Console.Error.WriteLine("[MICROPHONE] FAILED - max retries exceeded");
        Environment.Exit(4);
    }

    static void RunLevelTest()
    {
        EnsurePythonRequests();

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Console.WriteLine($"Level test attempt {attempt}/{MaxRetries}...");

            using var form = new HeadPhoneTest2.Form1();
            form.ShowDialog();

            Thread.Sleep(RetryDelayMs);

            if (File.Exists(Path.Combine(BaseDir, "results.json")))
            {
                Console.WriteLine("[LEVELS] PASSED");
                return;
            }

            Console.WriteLine($"[LEVELS] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Console.Error.WriteLine("[LEVELS] FAILED - max retries exceeded");
        Environment.Exit(5);
    }

    static void RunResultsScripts()
    {
        var getFinalResults = Path.Combine(BaseDir, "getFinalResults.exe");
        if (File.Exists(getFinalResults))
        {
            RunProcess(getFinalResults, "", wait: true);
        }
        else
        {
            var py = Path.Combine(BaseDir, "scripts", "getFinalResults.py");
            if (File.Exists(py))
                RunProcess("python", $"\"{py}\"", wait: true);
        }

        var converter = Path.Combine(BaseDir, "converter.exe");
        if (File.Exists(converter))
        {
            RunProcess(converter, "", wait: true);
        }
        else
        {
            var py = Path.Combine(BaseDir, "scripts", "converter.py");
            if (File.Exists(py))
                RunProcess("python", $"\"{py}\"", wait: true);
        }
    }

    static void CleanupBluetooth()
    {
        RunProcess("powershell", "-NoProfile -Command \"$ErrorActionPreference = 'SilentlyContinue'; Get-PnpDevice -Class Bluetooth | Where-Object { $_.FriendlyName -and $_.FriendlyName -notmatch 'Radio|Adapter|Enumerator|LE Enumerator|Microsoft|Intel|Qualcomm|Broadcom' } | Remove-PnpDevice -Confirm:$false -Force\"", wait: true);
    }

    static void EnsurePythonRequests()
    {
        try
        {
            var psi = new ProcessStartInfo("python", "-c \"import requests\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            if (proc?.ExitCode != 0)
            {
                Console.WriteLine("Installing requests dependency...");
                RunProcess("python", "-m pip install --user requests", wait: true);
            }
        }
        catch { }
    }

    static void SetVolume(int percent)
    {
        percent = Math.Max(0, Math.Min(100, percent));
        try
        {
            var controller = new CoreAudioController();
            var device = controller.DefaultPlaybackDevice;
            if (device is null)
            {
                Console.Error.WriteLine("No default playback device found for volume control.");
                return;
            }
            device.Volume = percent;
            device.Mute(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Volume control error: {ex.Message}");
        }
    }

    static string? ParseDeviceName(string filePath)
    {
        try
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                if (line.Contains("Dispositivo", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0)
                        return line[(idx + 1)..].Trim();
                }
            }
        }
        catch { }
        return null;
    }

    static string? WaitForFile(string pattern, int maxSeconds)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < maxSeconds)
        {
            var files = System.IO.Directory.GetFiles(BaseDir, pattern);
            if (files.Length > 0)
                return files[0];
            Thread.Sleep(1000);
        }
        return null;
    }

    static int RunProcess(string fileName, string arguments, bool wait)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = true,
                WorkingDirectory = BaseDir
            };
            using var proc = Process.Start(psi);
            if (wait)
                proc?.WaitForExit();
            return proc?.ExitCode ?? -1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to run {fileName}: {ex.Message}");
            return -1;
        }
    }
}
