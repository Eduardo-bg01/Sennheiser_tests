using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace SennheiserTestRunner;

static class Program
{
    static string BaseDir => Path.GetDirectoryName(Environment.ProcessPath!) ?? AppContext.BaseDirectory;
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

    // Returns false if anything survived. A caller that then reads a result file
    // would be reading the PREVIOUS run's verdict and calling it this attempt's,
    // so every call site must check. Callers that ignore the result reintroduce the
    // false-passed bug this whole change exists to close, on the one path where the
    // file is locked (Explorer preview, editor, antivirus) instead of merely stale.
    static bool Purge(params string[] patterns)
    {
        bool clean = true;
        foreach (var pattern in patterns)
        {
            foreach (var f in System.IO.Directory.GetFiles(BaseDir, pattern))
            {
                try { System.IO.File.Delete(f); }
                catch { clean = false; }
            }
        }
        return clean;
    }

    static void CleanOldFiles()
    {
        // serial* must be here too. GetSerial() used to return whatever serial.txt
        // survived from the previous unit, so a cancelled serial dialog stamped unit B
        // with unit A's serial. main's run.bat deleted serial* too, but behind a
        // SKIP_SERIAL_PROMPT escape hatch that no longer exists in this tree, so
        // deleting unconditionally removes nothing real. GetSerial also fails closed.
        // Return value deliberately ignored: nothing reads a result file before each
        // stage purges again, so a survivor here is re-checked at that point.
        Purge("Prueba_*", "results.json", "MicroTest_*", "test_results*", "hearingPass*",
              "recorded*", "final_results*", "tiempo*", "diferen*", "knob_*", "audio_plays*",
              "serial*");
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
        // Fail closed. A cancelled dialog writes nothing, so any serial.txt still on
        // disk at this point is the previous unit's. CleanOldFiles purges serial* at
        // startup, but build-all.bat:49 seeds bin\serial.txt from the repo root and
        // anything can drop a file in before this runs — so clear it again here. Any
        // file that exists after the dialog closed was therefore written by the dialog.
        // If the purge fails, that reasoning does not hold: a locked serial.txt from the
        // previous unit would survive the cancelled dialog and be stamped on this one.
        if (!Purge("serial*")) return null;

        using var form = new AskForSerial2.Form1();
        form.ShowDialog();

        var serialFile = Path.Combine(BaseDir, "serial.txt");
        if (!File.Exists(serialFile)) return null;

        var serial = File.ReadAllText(serialFile).Trim();
        return string.IsNullOrWhiteSpace(serial) ? null : serial;
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

            // Required, not hygiene: WriteFallbackReportIfMissing (MainForm.cs:78) returns
            // early when a Prueba_*.txt already exists, so a file left by attempt N-1 both
            // satisfies WaitForFile instantly and suppresses the report for this attempt.
            if (!Purge("Prueba_*.txt"))
            {
                Log("[CONTROLS] report from a previous attempt is locked and could not be deleted; retrying", isError: true);
                continue;
            }

            using var mainForm = new BluetoothHeadphoneTest.MainForm();
            mainForm.Session.SelectedDevice = selectForm.SelectedDevice;
            mainForm.ShowDialog();

            var resultFile = WaitForFile("Prueba_*.txt", 5);
            if (resultFile is not null && ControlsReportPassed(resultFile))
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

            if (!Purge("hearingPass*"))
            {
                Log("[AUDIO] report from a previous attempt is locked and could not be deleted; retrying", isError: true);
                continue;
            }
            using var form = new AudioTest.Form1();
            form.ShowDialog();

            var audioFile = WaitForFile("hearingPass*.txt", 5);
            if (audioFile is not null && FileVerdict(audioFile) == Verdict.Pass)
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

            // Only the report, not the recording: the .wav is the operator's only
            // diagnostic for a failed attempt. Still leaves exactly one .txt and one
            // .wav, so getFinalResults.py's unsorted MicroTest_* glob has one of each
            // to choose between (that ambiguity is tracked separately, see UAT test 5).
            if (!Purge("MicroTest_*.txt"))
            {
                Log("[MICROPHONE] report from a previous attempt is locked and could not be deleted; retrying", isError: true);
                continue;
            }
            using var form = new MicroTestCloud.Form1();
            form.ShowDialog();

            var resultFile = WaitForFile("MicroTest_*.txt", 5);
            if (resultFile is not null)
            {
                switch (FileVerdict(resultFile))
                {
                    case Verdict.Pass:
                        Log("[MICROPHONE] PASSED");
                        return;
                    case Verdict.NotApplicable:
                        // The operator's "EL DISPOSITIVO NO TIENE MICROFONO" button
                        // (MicroTestCloud/Form1.cs:471-479) writes "Resultado : N/A" and
                        // closes. That is a deliberate decision about the hardware, not a
                        // test failure, and it released the unit before the verdict was
                        // read at all. Keep releasing it: holding a mic-less unit for a
                        // deliberate N/A would need a test-procedure change to authorize.
                        Log("[MICROPHONE] N/A - device declared mic-less by operator, releasing unit");
                        return;
                }
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

            // Required, not hygiene: the daily station calibration runs BEFORE this stage
            // and FinishCalibration writes results.json into the same directory, so without
            // this purge the level test reported PASSED on a fresh unit with zero operator
            // action. A failed attempt N also leaves the file, which made attempt N+1 pass
            // the instant its form closed.
            if (!Purge("results.json"))
            {
                Log("[LEVELS] results.json from a previous stage is locked and could not be deleted; retrying", isError: true);
                continue;
            }

            using var form = new HeadPhoneTest2.Form1();
            form.ShowDialog();

            var resultFile = WaitForFile("results.json", 5);
            if (resultFile is not null && LevelTestPassed(resultFile))
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

    enum Verdict { Pass, Fail, NotApplicable }

    // The stage apps always write a result file, even on cancel — AudioTest writes
    // False, MicroTestCloud writes "Resultado : No definido". Existence is therefore
    // not a verdict; read the verdict.
    //
    // Three states, not two: MicroTestCloud also writes "Resultado : N/A" when the
    // operator declares the device mic-less. Verdict vocabulary is set at
    // MicroTestCloud/Form1.cs:1380 (PASS), :1391 (FAIL), :1406 (No definido) and
    // :478/:504 (N/A) — all ASCII, so an exact match is safe and a substring test
    // is not needed. See REVIEW-WORKTREE.md WR-07: an earlier comment here claimed
    // the accented "PASÓ" / "FALLÓ" and was fiction.
    //
    // This is NOT the same rule getFinalResults.py applies (:341), which is
    // substring-based and has an unguarded parts[2] that raises IndexError on a
    // malformed line. The two can disagree; that gap is tracked separately.
    static Verdict FileVerdict(string path)
    {
        try
        {
            var text = File.ReadAllText(path);

            if (text.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
                return Verdict.Pass;   // AudioTest writes True/False verbatim

            foreach (var line in text.Split('\n'))
            {
                if (!line.Contains("Resultado", StringComparison.Ordinal)) continue;
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 2) continue;
                var value = parts[2];
                if (value.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return Verdict.NotApplicable;
                return value.Equals("PASS", StringComparison.OrdinalIgnoreCase) ? Verdict.Pass : Verdict.Fail;
            }
        }
        catch { }

        return Verdict.Fail;
    }

    // Controls writes a summary line "  Resultado final: APROBADO  (3/3)  •  N/A: 0"
    // (FunctionalButtonTest/TestSession.cs, BuildReportText). AllPassed is false unless
    // every applicable record is Pass, so a check left Pending by a cancelled form makes
    // the line read FALLIDO. That token is the whole verdict — the per-check rows are
    // for humans.
    static bool ControlsReportPassed(string path)
    {
        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.Contains("Resultado final", StringComparison.OrdinalIgnoreCase)) continue;
                return line.Contains("APROBADO", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }

        return false;
    }

    // results.json is db_chart.py's per-capture output, and db_chart writes it for the
    // AMBIENT station calibration too — so its mere existence means nothing. The level
    // verdict is the same three checks getFinalResults.py:165-179 computes, with the same
    // thresholds (getFinalResults.py:28-31). Reading them here rather than inventing a
    // second rule keeps the two from disagreeing on the same unit.
    const double LevelBalanceDb = 2.0;    // CHANNEL_BALANCE_THRESHOLD
    const double LevelVolumeMin = -30.0;   // VOLUME_MIN
    const double LevelVolumeMax = -10.0;   // VOLUME_MAX
    const double LevelClippingDb = 0.0;    // CLIPPING_THRESHOLD

    static bool LevelTestPassed(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("measurements", out var list))
                return false;

            double? left = null, right = null, leftPeak = null, rightPeak = null;
            foreach (var m in list.EnumerateArray())
            {
                if (m.TryGetProperty("channel", out var ch) && m.TryGetProperty("dbfs", out var db))
                {
                    if (ch.GetString() == "Left") left = db.GetDouble();
                    else if (ch.GetString() == "Right") right = db.GetDouble();
                }
                if (m.TryGetProperty("peak_dbfs", out var pk))
                {
                    if (m.TryGetProperty("channel", out var c2) && c2.GetString() == "Left") leftPeak = pk.GetDouble();
                    else if (m.TryGetProperty("channel", out var c3) && c3.GetString() == "Right") rightPeak = pk.GetDouble();
                }
            }

            // A capture with no real signal has no L/R to judge; signal_present is
            // db_chart's own verdict and is the honest gate for "was anything captured".
            if (doc.RootElement.TryGetProperty("signal_present", out var sp)
                && sp.ValueKind == JsonValueKind.False)
                return false;

            if (left is null || right is null || leftPeak is null || rightPeak is null)
                return false;

            bool balance = Math.Abs(right.Value - left.Value) <= LevelBalanceDb;
            bool volume = left.Value >= LevelVolumeMin && left.Value <= LevelVolumeMax
                       && right.Value >= LevelVolumeMin && right.Value <= LevelVolumeMax;
            bool clipping = Math.Max(leftPeak.Value, rightPeak.Value) <= LevelClippingDb;
            return balance && volume && clipping;
        }
        catch { }

        return false;
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
