using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace SennheiserTestRunner;

static class Program
{
    static string BaseDir => AppContext.BaseDirectory;
    static string RootDir
    {
        get
        {
            var dir = BaseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetDirectoryName(dir) ?? dir;
        }
    }
    static string VolumeHelperExe => Path.Combine(BaseDir, "VolumeHelper.exe");

    static string LogFile => Path.Combine(BaseDir, "runner_log.txt");
    static int MaxRetries => 5;
    static int RetryDelayMs => 2000;

    static StreamWriter? _log;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Environment.CurrentDirectory = BaseDir;

        using (_log = new StreamWriter(LogFile, append: false) { AutoFlush = true })
        {
            Log($"Working directory: {BaseDir}");
            Log($"Root directory: {RootDir}");
            Log($"VolumeHelper path: {VolumeHelperExe}");

            KillOldProcesses();
            CleanOldFiles();

            RunDailyStationCalibration();

            LaunchRefurbishTool();

            ShowBluetoothConnectPrompt();

            long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            File.WriteAllText("tiempo1.txt", startTime.ToString());

            string? serial = GetSerial();
            if (serial is null)
            {
                Log("No serial provided. Exiting.", isError: true);
                Environment.Exit(1);
            }

            string? deviceName = RunControlsTest();
            if (deviceName is null)
            {
                Log("[CONTROLS] Failed - max retries exceeded", isError: true);
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
            Log($"Tests completed. Total time: {minutes} min");
        }
    }

    static void Log(string message, bool isError = false)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} | {message}";
        _log?.WriteLine(line);
        if (isError)
            Console.Error.WriteLine(line);
        else
            Console.WriteLine(line);
    }

    static void LaunchRefurbishTool()
    {
        string[] candidates =
        {
            Path.Combine(RootDir, "RefurbishToolArvato", "RefurbishTool.exe"),
            Path.Combine(BaseDir, "RefurbishToolArvato", "RefurbishTool.exe"),
        };
        foreach (var path in candidates)
        {
            Log($"Checking RefurbishTool path: {path} (exists: {File.Exists(path)})");
            if (File.Exists(path))
            {
                Log($"Opening RefurbishTool: {path}");
                RunProcess(path, "", wait: true);
                return;
            }
        }
        Log("RefurbishTool.exe not found at any checked path.", isError: true);
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
        foreach (var pattern in new[] { "Prueba_*", "results.json", "MicroTest_*", "test_results*", "hearingPass*", "recorded*", "final_results*", "tiempo*", "diferen*", "knob_*" })
        {
            foreach (var f in System.IO.Directory.GetFiles(BaseDir, pattern))
            {
                try { System.IO.File.Delete(f); } catch { }
            }
        }
    }

    // Daily station calibration gate (TV Listeners). Runs the 4-check golden-unit
    // verification via LevelTest (STATION_CALIB=1) whenever station_calibration.json
    // is missing, FAIL, or older than calibration_max_age_hours (config, default 12h,
    // UTC). On failure the pipeline is locked with exit code 6.
    static void RunDailyStationCalibration()
    {
        if (StationCalibrationCurrent())
        {
            Log("[STATION CALIB] PASS verificada y vigente - sin recalibrar");
            return;
        }

        Log("[STATION CALIB] Calibración de estación requerida (ausente, FAIL o vencida). Iniciando 4 pasos...");

        Environment.SetEnvironmentVariable("STATION_CALIB", "1");
        try
        {
            using var form = new HeadPhoneTest2.Form1();
            form.ShowDialog();
        }
        finally
        {
            Environment.SetEnvironmentVariable("STATION_CALIB", null);
        }

        if (StationCalibrationCurrent())
        {
            Log("[STATION CALIB] PASS - estación liberada para producción");
            return;
        }

        Log("[STATION CALIB] FAIL - estación BLOQUEADA", isError: true);
        MessageBox.Show(
            "ESTACIÓN NO LIBERADA.\r\n\r\n" +
            "ACCIÓN ANTE FALLA: Detener liberación, revisar ambiente, posicionamiento, " +
            "conexiones USB, configuración de REW y nivel de salida.\r\n" +
            "Corregir, registrar y repetir desde CHECK 1.\r\n\r\n" +
            "No se liberará la estación hasta que la calibración pase.",
            "Calibración de estación - LIBERACIÓN BLOQUEADA",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        Environment.Exit(6);
    }

    static bool StationCalibrationCurrent()
    {
        var file = Path.Combine(BaseDir, "station_calibration.json");
        if (!File.Exists(file))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            if (!root.TryGetProperty("station_calibration", out var verdict) || verdict.GetString() != "PASS")
                return false;

            string timeStr = root.TryGetProperty("time", out var timeProp) ? timeProp.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(timeStr) || !DateTimeOffset.TryParse(
                    timeStr, null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var calibTime))
            {
                return false;
            }

            return (DateTimeOffset.UtcNow - calibTime).TotalHours <= CalibrationMaxAgeHours();
        }
        catch
        {
            return false;
        }
    }

    static double CalibrationMaxAgeHours()
    {
        const double defaultHours = 12;
        foreach (var path in new[] { Path.Combine(BaseDir, "scripts", "config.json"), Path.Combine(BaseDir, "config.json") })
        {
            if (!File.Exists(path))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("calibration_max_age_hours", out var v) && v.ValueKind == JsonValueKind.Number)
                    return v.GetDouble();
            }
            catch
            {
                // ignore unreadable config, fall back to default
            }
        }
        return defaultHours;
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
        Log("Setting volume to 50% before controls test...");
        SetVolume(50);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Log($"Controls test attempt {attempt}/{MaxRetries}...");

            using var selectForm = new BluetoothHeadphoneTest.DeviceSelectForm();
            if (selectForm.ShowDialog() != DialogResult.OK)
            {
                Log("Device selection cancelled.", isError: true);
                return null;
            }

            var selected = selectForm.SelectedDevice;
            BluetoothHeadphoneTest.DeviceAssets.DeviceName = selected?.Name ?? string.Empty;

            // Plumb the selected model to AudioTest/LevelTest (both run in this process).
            // Wired devices carry the operator's choice from the jack-model combo (e.g. "RS 195"),
            // which AudioTest/LevelTest normalize to "rs195" to enable RS-specific flows.
            string model = selected is { IsWired: true } && !string.IsNullOrWhiteSpace(selected.SelectedJackModel)
                ? selected.SelectedJackModel
                : selected?.Name ?? string.Empty;
            Environment.SetEnvironmentVariable("DEVICE_NAME", model);
            Log($"DEVICE_NAME set to: {model}");

            using var mainForm = new BluetoothHeadphoneTest.MainForm();
            mainForm.Session.SelectedDevice = selectForm.SelectedDevice;
            mainForm.ShowDialog();

            var resultFile = WaitForFile("Prueba_*.txt", 5);
            if (resultFile is not null)
            {
                Log("[CONTROLS] PASSED");
                string? deviceName = ParseDeviceName(resultFile);
                Log($"Device: {deviceName}");
                Log("Setting volume to 100% before audio test...");
                SetVolume(100);
                return deviceName;
            }

            Log($"[CONTROLS] FAILED - attempt {attempt}/{MaxRetries}");
        }

        return null;
    }

    static void RunAudioTest()
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Log($"Audio test attempt {attempt}/{MaxRetries}...");

            using var form = new AudioTest.Form1();
            form.ShowDialog();

            if (System.IO.Directory.GetFiles(BaseDir, "hearingPass*.txt").Length > 0)
            {
                Log("[AUDIO] PASSED");
                Log("Setting volume to 100% before microphone test...");
                SetVolume(100);
                return;
            }

            Log($"[AUDIO] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Log("[AUDIO] FAILED - max retries exceeded", isError: true);
        Environment.Exit(2);
    }

    static void RunMicrophoneTest()
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Log($"Microphone test attempt {attempt}/{MaxRetries}...");

            using var form = new MicroTestCloud.Form1();
            form.ShowDialog();

            var resultFile = WaitForFile("MicroTest_*.txt", 5);
            if (resultFile is not null)
            {
                Log("[MICROPHONE] PASSED");
                return;
            }

            Log($"[MICROPHONE] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Log("[MICROPHONE] FAILED - max retries exceeded", isError: true);
        Environment.Exit(4);
    }

    static void RunLevelTest()
    {
        EnsurePythonRequests();

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            Log($"Level test attempt {attempt}/{MaxRetries}...");

            using var form = new HeadPhoneTest2.Form1();
            form.ShowDialog();

            if (File.Exists(Path.Combine(BaseDir, "results.json")))
            {
                Log("[LEVELS] PASSED");
                return;
            }

            Log($"[LEVELS] FAILED - attempt {attempt}/{MaxRetries}");
        }

        Log("[LEVELS] FAILED - max retries exceeded", isError: true);
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
                Log("Installing requests dependency...");
                RunProcess("python", "-m pip install --user requests", wait: true);
            }
        }
        catch { }
    }

    static void SetVolume(int percent)
    {
        percent = Math.Max(0, Math.Min(100, percent));
        Log($"Setting volume to {percent}% via VolumeHelper.exe...");

        if (!File.Exists(VolumeHelperExe))
        {
            Log($"VolumeHelper.exe not found at {VolumeHelperExe}", isError: true);
            return;
        }

        var exitCode = RunProcess(VolumeHelperExe, percent.ToString(), wait: true);
        Log($"VolumeHelper.exe exited with code {exitCode}");
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
            Log($"Running: {fileName} {arguments}");
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = true,
                WorkingDirectory = BaseDir
            };
            using var proc = Process.Start(psi);
            if (wait)
                proc?.WaitForExit();
            var code = proc?.ExitCode ?? -1;
            Log($"Process exit code: {code}");
            return code;
        }
        catch (Exception ex)
        {
            Log($"Failed to run {fileName}: {ex.Message}", isError: true);
            return -1;
        }
    }
}
