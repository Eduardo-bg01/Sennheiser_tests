# Coding Conventions

**Analysis Date:** 2026-09-28

## Scope

Two languages carry the actual logic, plus two shell dialects:

| Language | Where | Notes |
|---|---|---|
| C# (.NET 8 / .NET 9, WinForms) | `apps/`, `tools/` | ~7,000 lines across 7 projects |
| Python 3 (stdlib only) | `scripts/` | ~1,100 lines across 5 modules + 1 test file |
| PowerShell | `show_bluetooth*.ps1` | WinForms dialogs built at runtime |
| Windows batch | `batch/*.bat` | build + launcher |

**The language of the product is Spanish.** UI strings, comments, XML doc, and
commit-visible identifiers are Spanish (`Dispositivo`, `Play / Pausa`,
`Subir Volumen`, `calibracion.txt`, `estacion`, `audífonos`). Code identifiers,
file names, and the Python API stay English. Mixed-language code is normal here —
do not "fix" it.

## Naming Patterns

### Files

**C#** — one type per file, filename == type name. Two sub-conventions coexist:

- `PascalCase.cs` — `TestSession.cs`, `TestStepManager.cs`, `DeviceSelectForm.cs`,
  `MainForm.cs`, `SummaryPanel.cs`, `BluetoothDetector.cs`, `AppCommandRouter.cs`,
  `AudioPlayer.cs`, `Colors.cs`, `UIHelper.cs`, `SharedTheme.cs`
- `PascalCamelConcat.cs` (single leading capital, rest lowercase) — `Deviceprofile.cs`,
  `Deviceprofileregistry.cs`, `Deviceassets.cs`

**Use `PascalCase.cs`.** The concat form (`Deviceprofile.cs`) is the minority
mistake; new types should not copy it. If a rename is ever done, fix all three
together — nothing references them by name, so it is a pure file rename.

**Python** — `snake_case.py`, with one exception: `scripts/getFinalResults.py`
is `camelCase.py`. Do not rename it: the runner and README both reference it
(`apps/SennheiserTestRunner/Program.cs:385`) and renaming breaks the pipeline for
zero benefit. New scripts go `snake_case.py`.

**Designer files** — `Form1.Designer.cs`, `Form1.resx` are Visual Studio
generated. Never hand-edit; they follow the `InitializeComponent()` +
`Dispose(bool)` shape automatically.

### Types and members (C#)

| Element | Convention | Example |
|---|---|---|
| Class / struct / interface | `PascalCase` | `TestPanel`, `BluetoothDeviceInfo` |
| Method | `PascalCase` (verb first) | `RunControlsTest()`, `ApplyCohesiveTheme()` |
| Property | `PascalCase` | `TestSession.AllPassed` |
| Enum member | `PascalCase` | `TestResult.NotApplicable` (`apps/FunctionalButtonTest/TestSession.cs:7`) |
| Event | `PascalCase` noun | `TestCompleted`, `OnRestart`, `VolumeChanged` |
| Public constant | `SNAKE_CASE` | `SERIAL_FILE`, `MIN_WINDOW_WIDTH` (`apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Form1.cs:5-9`) |
| Private field | `_camelCase` | `_steps`, `_log`, `_vol` (`apps/FunctionalButtonTest/TestStepManager.cs:15`) |
| Parameter / local | `camelCase` | `totalTests`, `startOffset` |

**Private field prefix is inconsistent** — `_` is used in `FunctionalButtonTest`
and `SennheiserTestRunner`, but plain `camelCase` in `MicroTestCloud/Form1.cs` and
`LevelTest/Form1.cs`. **Prefer `_camelCase`**; do not rename existing fields
while adding features in those files.

**Public constants are `SNAKE_CASE` in C# and Python alike.** `SERIAL_FILE`
(`AskForSerial2/Form1.cs:5`), `MIN_WINDOW_WIDTH` (same file), `RESULT_PASS`
(`scripts/getFinalResults.py:34`). The rest of the codebase uses
`private static readonly` camelCase for the equivalent
(`BgCard`, `AccentCyan`), so the rule is: `const` → `SNAKE_CASE`,
`static readonly` → `camelCase`.

### Functions (Python)

`snake_case` without exception — verified across all 5 modules, including
`getFinalResults.py` (`read_device_model`, `analyze_audio_levels`,
`knob_verdict`, `_build_audio_test`). Private helpers take a leading underscore
(`_stamp_station_calibration`, `_build_audio_test`). **No camelCase locals
anywhere.**

Module-level constants: `SCREAMING_SNAKE_CASE` — `SIGNAL_MIN_DBFS`,
`KNOB_SEPARATION_DB`, `FILE_PATTERN_STATION_CALIB`, `DEFAULTS`. Config-shaped
dictionaries also use `SCREAMING_SNAKE_CASE` for their string keys when they map
to JSON field names (`DEFAULTS` in `scripts/converter.py:21`,
`scripts/station_calibration.py:23`).

## Code Style

### Formatting

**C#** — 4 spaces. No `.editorconfig`, no formatter config, no analyzer config;
this is hand-maintained Visual Studio default. `.csproj` files are **2 spaces**
except `apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj`, which uses
**tabs**. Match the file you are editing.

**Python** — 4 spaces, PEP 8 line length informally respected (the longest lines
are the ASCII table in `scripts/db_chart.py:142`). No `black`/`ruff`/`flake8`
config exists; do not add one as a side effect of another change.

**Namespaces** — block-scoped `namespace X { }` in 20 of 21 hand-written `.cs`
files. The one exception is `apps/SennheiserTestRunner/Program.cs:5`
(`namespace SennheiserTestRunner;`), which is the newest file in the repo.
**Use block-scoped** to match; do not convert files.

**`AskForSerial2/SharedTheme.cs` and `UIHelper.cs` have no namespace at all** —
they sit in the global namespace and are referenced unqualified from
`AskForSerial2/Form1.cs`. That is the only way cross-project theme sharing was
achieved (both files live inside one project only). Do not put new shared
helpers there expecting reuse from other projects — it will not compile.

### Language-level settings drift

| Project | TFM | `Nullable` | `ImplicitUsings` |
|---|---|---|---|
| `apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj` | `net9.0-windows` | **disable** | **disable** |
| `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj` | `net9.0-windows` | enable | enable |
| `apps/SennheiserTestRunner/SennheiserTestRunner.csproj` | `net9.0-windows` | enable | enable |
| `tools/VolumeHelper/VolumeHelper.csproj` | `net9.0-windows` | enable | enable |
| `apps/pruebasAudifonos/AudioTest/AudioTest/AudioTest.csproj` | **`net8.0-windows`** | enable | enable |
| `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/HeadPhoneTest2.csproj` | **`net8.0-windows`** | enable | enable |
| `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/AskForSerial2.csproj` | **`net8.0-windows`** | enable | enable |

Three projects still target net8.0 while the orchestrator that references them
targets net9.0. This builds (net8 libs are consumable by net9) but is drift.
**Do not bump a TFM as an incidental edit** — `build-all.bat` publishes
self-contained `win-x64`, and a TFM change alters the runtime pack.

`BluetoothHeadphoneTest` is the only project with explicit `using` blocks instead
of implicit usings, and the only one with `AllowUnsafeBlocks`.

### Idioms

- **`using var` for disposables** — preferred in all newer code
  (`tools/VolumeHelper/Program.cs:12-13`, `apps/SennheiserTestRunner/Program.cs:34`,
  `apps/FunctionalButtonTest/Program.cs:14`). The older `using (...) { }` form
  survives only in the runner's log-stream scope and in `TestPanels`.
- **`var` is used freely** for locals when the type is obvious from the RHS
  (`var profile = form.Session.SelectedDevice;`, `var sw = Stopwatch.StartNew();`).
  Explicit types appear when the RHS is a constructor call with a long argument
  list or when a field needs declaring.
- **Object initializers** dominate over constructor argument lists for WinForms
  controls (`apps/FunctionalButtonTest/TestPanels.cs:64-75`).
- **Lambdas for event wiring** — `resize += (_, _) => ApplyProfessionalLayout();`
  (discard both params) when unused, `(s, e) =>` when used.
- **Expression-bodied members** for trivial members — `protected void FireTestCompleted(bool passed) => TestCompleted?.Invoke(passed);`
  (`TestPanels.cs:28`), `public static void StylePrimaryButton(Button btn) => StyleButton(btn, SharedTheme.Accent, true);`
  (`UIHelper.cs:11`).
- **Pattern matching** in newer code — `selected is { IsWired: true }`
  (`SennheiserTestRunner/Program.cs:278`), `device?.Name ?? string.Empty`.
- **String comparison is explicit** where it matters —
  `StringComparison.OrdinalIgnoreCase` for file/CLI comparisons.

### Font and visual constants

Every WinForms app uses `"Segoe UI"`, sizes `9f` (labels) / `10F` (form base) /
`11F` (status) / `16F` (buttons) / `20F`–`34f` (headline, icon). Emoji glyphs are
inlined in control text for status icons: `"✔  "`, `"✘  "`, `"⏳  "`, `"🎧  "`.
Keep that convention for new status strings.

## The De-facto Theme Convention (and its drift)

**The convention:** each themed app declares a block of
`private static readonly Color` fields at the top of its form class, built with
`ColorTranslator.FromHtml("#RRGGBB")` and **UPPERCASE hex**, then a single
`ApplyCohesiveTheme()` method applies them to designer-created controls, followed
by `ApplyProfessionalLayout()` for pixel layout.

```csharp
// apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:10-18
private static readonly Color BgApp      = ColorTranslator.FromHtml("#F4F7FC");
private static readonly Color BgCard     = ColorTranslator.FromHtml("#FFFFFF");
private static readonly Color Border     = ColorTranslator.FromHtml("#D7E1F0");
private static readonly Color Accent     = ColorTranslator.FromHtml("#0099BB");
private static readonly Color AccentMuted= ColorTranslator.FromHtml("#EAF1FA");
private static readonly Color Success    = ColorTranslator.FromHtml("#00A85A");
private static readonly Color Danger     = ColorTranslator.FromHtml("#CC2222");
private static readonly Color TextPrimary= ColorTranslator.FromHtml("#1A2640");
private static readonly Color TextMuted  = ColorTranslator.FromHtml("#5A6F90");
```

**This exact 9-colour block is duplicated verbatim** in:
- `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:10-18`
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:11-19`

and its values are **renamed but re-declared** in two more places:
- `apps/FunctionalButtonTest/Colors.cs:7-16` — `AppColors`, 10 colours
  (`BgDark`, `AccentCyan`, `AccentYellow`, `AccentGreen`, `AccentRed`,
  `AccentOrange`, `BorderColor`)
- `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/SharedTheme.cs:10-31` —
  `SharedTheme`, 14 colours, grouped with `// Background Colors` /
  `// Status Colors` / `// Text Colors` banners

**Four incompatible colour vocabularies** for the same roles:

| Role | AudioTest / LevelTest | AppColors (FunctionalButtonTest) | SharedTheme (AskForSerial2) |
|---|---|---|---|
| Page background | `BgApp` `#F4F7FC` | `BgDark` `#F4F7FC` | `BgApp` `#F4F7FC` |
| Card | `BgCard` `#FFFFFF` | `BgCard` `#FFFFFF` | `BgCard` `#FFFFFF` |
| Accent | `Accent` `#0099BB` | `AccentCyan` `#0099BB` | `Accent` `#0099BB` |
| Success | `Success` `#00A85A` | `AccentGreen` `#00A85A` | `Success` `#00A85A` |
| Danger | `Danger` `#CC2222` | `AccentRed` `#CC2222` | `Danger` `#CC2222` |
| Warning | — | `AccentYellow` `#D4A000` | `Warning` `#FF9800` |
| Border | `Border` `#D7E1F0` | `BorderColor` **`#C8D4E8`** | `Border` `#D7E1F0` |
| Text muted | `TextMuted` `#5A6F90` | `TextMuted` `#5A6F90` | `TextMuted` `#5A6F90` |

The palettes have **already diverged**: `AppColors.BorderColor` is `#C8D4E8`
where the other two use `#D7E1F0`, and the warning yellow is `#D4A000` in
FunctionalButtonTest but `#FF9800` in AskForSerial2. A second blue,
`AccentBlue #3B6EC8` (`apps/FunctionalButtonTest/DeviceSelectForm.cs:17`),
exists in no palette at all, and the table zebra colours
`BgRow #F0F4FB` / `BgRowAlt #E8EEF8` (`apps/FunctionalButtonTest/SummaryPanel.cs:14-15`)
are also un-palettised.

**Additional inline literals that bypass every palette:**
- `#E0F4FA` — test-number and device chip background, `TestPanels.cs:69,85`
- `#1A2640` — re-typed literally instead of `TextPrimary`, `TestPanels.cs:84`
- `#EAF0FA` — status bar and steps panel, `TestPanels.cs:126,141`

**When adding a colour:** if you are in `FunctionalButtonTest`, use `AppColors.*`
from `apps/FunctionalButtonTest/Colors.cs` and add the entry there — do not add
a new local `static readonly Color`. If you are in `AudioTest`, `LevelTest`, or
`MicroTestCloud`, there is no shared file to add to; declare it in the form's
colour block with the `Bg*`/`Accent*`/`Success`/`Danger`/`Text*` naming and
accept the duplication, because centralising requires a new shared project that
none of these three `.csproj` files reference today.

### The de-facto `Style*Button` convention (and its drift)

**The convention:** a private static `StyleButton` plus typed wrappers, called
once from the theme method.

Three independent implementations exist:

1. **`apps/pruebasAudifonos/AskForSerial2/AskForSerial2/UIHelper.cs:11-26`** —
   `public static`, `Tag`-driven bulk application via `ApplyThemeToControl`
   (`:28-61`), optional `Color? border`. This is the only reusable one.
2. **`apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:614-648`** —
   `private static`, plus a `StyleHeadline(Label)` that the other two lack.
3. **`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:1390-1424`** —
   **byte-identical to #2** (`StyleButton`, `StylePrimaryButton`,
   `StyleSecondaryButton`, `StyleHeadline`), copied verbatim.

Signatures also differ between the two families:
- AudioTest/LevelTest: `StylePrimaryButton(Button, string text, Color bg, Color fore)` — text is a parameter
- UIHelper: `StylePrimaryButton(Button)` — text is already on the control

**Call sites for buttons are the same three lines in two files**
(`AudioTest/Form1.cs:553-556`, `LevelTest/Form1.cs:1322-1326`):

```csharp
ApplyThemeToControlTree(this);
StylePrimaryButton(btnNext, "Siguiente", Accent, Color.White);
StyleSecondaryButton(btnCancel, "Cancelar");
StylePrimaryButton(btnPass, "Si", Success, Color.White);
StylePrimaryButton(btnFail, "No", Danger, Color.White);
```

**When adding a styled button:** inside a themed form, use the local
`Style*Button` helper and add the call to `ApplyCohesiveTheme()` — not to a
`Load`/constructor. Do not introduce a fifth implementation.

## Import Organization

**C#** — flat, alphabetical, `System.*` first. No namespace aliases, no
`global using`. Examples: `apps/FunctionalButtonTest/TestPanels.cs:1-7`,
`apps/SennheiserTestRunner/Program.cs:1-3`. With `ImplicitUsings=enable` the
explicit `using System;`/`using System.Drawing;` lines are redundant but
harmless and are kept. `BluetoothHeadphoneTest` has `ImplicitUsings=disable` and
therefore genuinely needs them.

Fully-qualified names appear only to break a real collision:
- `System.IO.Directory` / `System.IO.File` inside
  `apps/SennheiserTestRunner/Program.cs:131,133,314,338,468` (the runner
  references a `Volume` concept that would otherwise shadow)
- `System.Windows.Forms.Timer` vs `System.Timers.Timer` in
  `apps/FunctionalButtonTest/TestPanels.cs:191,199`

**Python** — stdlib imports, blank line, then local imports last.

```python
# scripts/station_calibration.py:17-21
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from common import load_baseline, load_config, channel_dbfs
```

Local imports are **top-level, not guarded** — all five scripts assume they are
run from a directory where their siblings resolve, except that
`scripts/test_signal_detection.py:17` inserts its own directory onto `sys.path`
so it can import them from anywhere. `db_chart.py:11` and
`converter.py:8` do *not* do this, which is why the test file must set it up
once on their behalf. No `if __name__` guard on the imports, by design.

**Path aliases:** none in C# (no `Directory.Build.props`). In Python, the only
aliasing is the `sys.path.insert` above.

## Error Handling

**Strategy:** never throw at the operator. Three tiers, in order of frequency.

### Tier 1 — user-facing failure → `MessageBox.Show`

24 call sites. The shape is consistent:

```csharp
// apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:172
MessageBox.Show(
    "No se detectaron dispositivos de audio. Verifique que los audifonos/microfono esten conectados.",
    "AudioTest",
    MessageBoxButtons.OK,
    MessageBoxIcon.Warning);
```

Rules to follow:
- Message text is Spanish, imperative, tells the operator what to do next.
- Title is short and Spanish (`"Error"`, `"Calibración"`, `"Resumen"`,
  `"Playback Error"`, `"Audio Library Missing"`).
- `MessageBoxButtons.OK` unless you actually branch on the answer. The two
  exceptions are retry dialogs that return a `DialogResult`:
  `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:473` (retry the
  sweep) and `:1210` (repeat the knob test).
- Icon: `MessageBoxIcon.Warning` for recoverable/environmental problems
  (missing device, missing `results.json`), `MessageBoxIcon.Error` for hard
  failures (invalid serial, calibration failure, missing media feature pack).
- Exception messages are appended raw, never swallowed into a generic string:
  `$"Error al reproducir:\n{ex.Message}"` (`MicroTestCloud/Form1.cs:1037`).

Long multi-line operator instructions use `\r\n\r\n` paragraph breaks
(`apps/SennheiserTestRunner/Program.cs:170-178`).

### Tier 2 — `catch (Exception ex)` with a recovery action

The block shows a dialog, writes a fallback artifact, or returns a neutral
value. Representative:

```csharp
// apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:185-190
catch (Exception ex)
{
    MessageBox.Show("Error al iniciar audio/grabacion:\n" + ex.Message, "AudioTest", ...);
    try { File.WriteAllText("hearingPassResults.txt", "False"); } catch { }
    try { StopRecording(); } catch { }
    try { StopAudio(); } catch { }
}
```

The **artifact-first** rule matters most: on a fatal audio error the app still
writes `hearingPassResults.txt = "False"` so the runner sees a verdict instead
of retrying five times and exiting 2. This is marked as a deliberate
simplification:

```csharp
// apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:136
// ponytail: write FAIL so batch/run.bat can continue with operator FAIL instead of 5 retries -> exit 2
```

**Honor this pattern for new fatal errors:** write the neutral result file, then
show the dialog. Same in `LevelTest/Form1.cs:1056` (delete stale result files so
the runner cannot read a previous unit's output).

### Tier 3 — bare `catch { }` swallow-all

~30 sites. This is the repo's most-used pattern for *cleanup*, *optional
side-effects*, and *device probing*, and it is accepted here. The convention is
that **the swallow is annotated with a one-line Spanish comment naming the
reason**, inside the braces:

```csharp
catch { }                                                    // runner: KillOldProcesses, CleanOldFiles
catch { return null; }                                        // Deviceassets.cs:132
catch { /* No audio device available */ }                     // VolumeMonitor.cs:31
catch { /* control destruido justo en este momento */ }        // TestPanels.cs:330
catch { /* Si falla el guardado, continuar sin interrumpir */ } // SummaryPanel.cs:256
catch { /* best-effort, never block close */ }               // MicroTestCloud/Form1.cs:1097
catch                                                        // bare, no comment — SennheiserTestRunner/Program.cs:206,225
```

`SennheiserTestRunner/Program.cs:206,225` shows the accepted form when the
"reason" is obvious from context — a corrupt `station_calibration.json` or
`config.json` returns `false` / falls through to the default, and the code says
so at `:227` (`// ignore unreadable config, fall back to default`).

**When adding a bare catch:** add the reason comment. An uncommented `catch { }`
is a bug waiting to happen.

### Specific exception types

One instance only: `catch (System.DllNotFoundException)` at
`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:718`, giving a specific
"install Windows Media Feature Pack" dialog before the generic handler. Add
targeted catches like this when a known environment failure has a known fix.

### Python: neutral fallback, never raise

Python modules do not propagate errors. The pattern is
`try / except Exception:` → print a bracketed warning → return a neutral value.

```python
# scripts/common.py:37-47
def load_baseline(path):
    try:
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        left = data.get("left_dbfs")
        right = data.get("right_dbfs")
        if left is None or right is None:
            return None
        return {"left_dbfs": left, "right_dbfs": right}
    except Exception:
        print(f"[WARNING] Could not read baseline file: {path}. Using fixed thresholds only.")
        return None
```

`except Exception:` (never bare `except:`) appears in `common.py:18,28,45`,
`getFinalResults.py:77,118,241,365,390`, `converter.py:168`. The neutral value
is domain-specific and load-bearing:

- unreadable baseline → `None`, use fixed thresholds (`common.py:47`)
- unreadable `station_calibration.json` → `"SKIPPED"` (`getFinalResults.py:78`)
- unreadable `audio_plays.json` → `missing` (`getFinalResults.py:243`)
- unparseable timestamp → omit `StartTime`/`EndTime` entirely
  (`getFinalResults.py:390-392`)
- upload failure → `(None, str(exc))` and a printed `Upload status: FAILED`
  (`converter.py:168-169`)

**Do not replace a neutral value with an exception.** `PASS`/`FAIL`/`N/A`/
`SKIPPED` are the domain's neutral vocabulary; a missing input must produce
`SKIPPED`, never `FAIL` and never a crash. This is asserted directly in
`scripts/test_signal_detection.py:398` (`assert fr["audio_test"] == "SKIPPED"  # neutral, like the other missing tests`).

`raise` is reserved for programmer errors in pure functions:
`raise ValueError(f"Unsupported sample width: {sample_width} bytes")`
(`scripts/db_chart.py:65`), `raise ValueError("Input WAV must be stereo (2 channels)")`
(`scripts/db_chart.py:85`), `raise FileNotFoundError(...)`
(`scripts/converter.py:58`).

### Exit codes

`apps/SennheiserTestRunner/Program.cs` uses `Environment.Exit(n)` with the
documented contract in `README.md:46-53`:

| Code | Call site | Meaning |
|---|---|---|
| 1 | `:56` | no serial |
| 2 | `:326` | AudioTest exhausted retries |
| 3 | `:63` | controls test exhausted retries |
| 4 | `:349` | microphone test exhausted retries |
| 5 | `:373` | LevelTest exhausted retries |
| 6 | `:179` | station calibration failed — station LOCKED |

**Never introduce a new exit code without adding it to the README table** — the
table is the contract with the operator and with any automation reading
`run.bat`'s `%ERRORLEVEL%`.

## Logging

**No logging framework. No `System.Diagnostics.Debug`, no `Trace`, no Serilog,
no NLog, no Python `logging`.** Verified: the only `Console.WriteLine` calls in
the whole C# tree are `SennheiserTestRunner/Program.cs:96` and
`tools/VolumeHelper/Program.cs:25`.

### C#: one log file, one method

`apps/SennheiserTestRunner/Program.cs:20,34,89-97` is the only logging facility
in the repo.

```csharp
static string LogFile => Path.Combine(BaseDir, "runner_log.txt");
...
using (_log = new StreamWriter(LogFile, append: false) { AutoFlush = true })
...
static void Log(string message, bool isError = false)
{
    var line = $"{DateTime.Now:HH:mm:ss.fff} | {message}";
    _log?.WriteLine(line);
    if (isError) Console.Error.WriteLine(line);
    else Console.WriteLine(line);
}
```

Conventions: truncated (ms, no date — the file is per-run and truncated on
open), `|` separator, `[TAG]` prefix for stage messages
(`"[CONTROLS] PASSED"`, `"[STATION CALIB] FAIL - estación BLOQUEADA"`,
`"[MICROPHONE] FAILED - max retries exceeded"`). `isError: true` also routes to
stderr. `_log` is a nullable static so `Log` is safe to call before the stream
opens.

**The other six C# apps have no log at all.** Their observable output is the
result files they write. This is the contract:

| File | Written by |
|---|---|
| `serial.txt` | `AskForSerial2/Form1.cs:42` |
| `Prueba_*.txt` | `FunctionalButtonTest` (Bluetooth controls) |
| `hearingPassResults.txt` | `AudioTest/Form1.cs` |
| `MicroTest_*.txt` | `MicroTestCloud/Form1.cs` |
| `results.json` | `db_chart.py` via `LevelTest.RunPythonScript` |
| `knob_left.json`, `knob_right.json` | `db_chart.py` (RS195 knob takes) |
| `audio_plays.json` | `LevelTest/Form1.cs` |
| `station_calibration.json` | `station_calibration.py` |
| `final_results.json` | `getFinalResults.py` |
| `tiempo1.txt` / `tiempo2.txt` / `diferencia_minutos.txt` | runner |

> Note: `.gitignore` excludes `*.log` but the runner writes `runner_log.txt`,
> which is **not** covered. It is also written into `bin/` (which *is* ignored),
> so in practice it stays untracked. Worth knowing if the layout ever changes.

### Python: `print()` with a bracketed prefix

No `logging`, no `sys.stderr` discipline. Prefixes: `[WARNING]`
(`common.py:46`, `db_chart.py:275`, `converter.py:16`), `[info]`
(`test_signal_detection.py:101`), `Upload status:` / `Upload error:`
(`converter.py:190-196`). `station_calibration.py` prints only the bare verdict
word (`print(verdict["station_calibration"])`, `:135`) because the runner parses
nothing from it — the value rides in the JSON.

## Comments

### When to comment

Comment the **why**, in Spanish. Existing comment density by file:

| File | `///` doc lines | `//` lines | Total lines |
|---|---|---|---|
| `apps/FunctionalButtonTest/Deviceprofileregistry.cs` | 24 | 55 | 166 |
| `apps/FunctionalButtonTest/TestSession.cs` | 18 | 18 | 164 |
| `apps/FunctionalButtonTest/TestStepManager.cs` | 14 | 27 | 191 |
| `apps/FunctionalButtonTest/BluetoothDetector.cs` | 13 | 29 | 305 |
| `apps/MicroTestCloud/MicroTestCloud/Form1.cs` | 9 | 116 | 1551 |
| `apps/pruebasAudifonos/LevelTest/.../Form1.cs` | **0** | 40 | 1463 |
| `apps/pruebasAudifonos/AudioTest/.../Form1.cs` | **0** | 23 | 769 |
| `apps/SennheiserTestRunner/Program.cs` | **0** | ~25 | 499 |

**The pattern: the small, newer `FunctionalButtonTest` classes are
well-documented; the large Form1.cs files and the runner carry plain `//`
comments only, with zero XML doc.** The runner compensates with long
explanatory comments on the non-obvious business rules —
`Program.cs:138-141` (the station-calibration gate) and `:275-277` (why the
model is plumbed through `DEVICE_NAME`) are the models to follow for
orchestration code.

### JSDoc / XML doc

Use `/// <summary>` on **types and public/behavioral methods**, especially where
a non-obvious rule is encoded. Examples:

```csharp
// apps/FunctionalButtonTest/Deviceprofile.cs:5-8
/// <summary>
/// Define qué pruebas aplican a un modelo específico.
/// Agrega una entrada en DeviceProfileRegistry por cada modelo nuevo.
/// </summary>
```

```csharp
// apps/FunctionalButtonTest/TestPanels.cs:30
/// <summary>Show the step counter ("PRUEBA X / Y"); called by TestStepManager with the final step count.</summary>
```

A doc comment that carries a worked example is welcome —
`TestStepManager.cs:148-157` (`RecordIndex`) documents the index-mapping rule
with a concrete scenario, which is exactly the kind of comment that prevents
the next bug.

### Section banners

Box-drawing rules are used to delimit sections in the long files:

```csharp
// apps/FunctionalButtonTest/TestPanels.cs:11-13
// ═══════════════════════════════════════════════════════════════════════════
//  BASE PANEL
// ═══════════════════════════════════════════════════════════════════════════

// apps/FunctionalButtonTest/Deviceassets.cs:15
// ── Imagen del dispositivo ─────────────────────────────────────────────
```

`SharedTheme.cs:9,13,17,22,28` uses plain `// Background Colors` style banners.
Match the surrounding file.

### The `ponytail:` marker

Nine sites use a `ponytail:` comment to record a **deliberate simplification
together with the reason it is acceptable** — this is an established, load-
bearing convention. Keep writing them for new shortcuts.

- `scripts/db_chart.py:16-17` — `// ponytail: provisional values until calibrated against known-good units in the field.`
- `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:136,166,349,448`
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:277,290,699,856`

Format: one line, the tradeoff and its consequence in the same sentence.

### Known comment defects — do not propagate

- **Leftover mod markers.** `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:669` and `:683` bracket a hand-tweaked layout block with `//START-MOD FONG` / `//END-MOD FONG`. These are personal, machine-generated markers with no meaning for a reader. Remove them if you touch that block; do not copy them.
- **Mojibake.** `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:50-53` contains `2. �ptico` and `Anal�gico` — cp1252 bytes read as UTF-8. Same in `Show_bluetooth.ps1` strings (`"Configuracion"`, `"Si"`, `"Listo"`, `"limpiaran"` — accents stripped or mangled). New UI strings should be valid UTF-8; `.gitattributes` does not set `working-tree-encoding`, so be careful.
- **Misspellings in shipped strings.** `"Error de preuba"` (`LevelTest/Form1.cs:945,956`) should be `"Error de prueba"`. Fix when seen; do not replicate.

## Function Design

**Size:** no hard limit, but the distribution is bimodal. Business logic lives
in 10–40-line methods. The outliers are the three `Form1.cs` files at 769,
1463, and 1551 lines, plus `TestPanels.cs` at 1009.

**The established decomposition for a themed WinForms form** — follow it for
new forms:

1. `Constructor` → `InitializeComponent()`, event wiring, then one call each to
   `ApplyCohesiveTheme()` and `ApplyProfessionalLayout()`
2. `ApplyCohesiveTheme()` → colours, fonts, `ApplyThemeToControlTree(this)`,
   `Style*Button(...)`, then `ApplyProfessionalLayout()`
3. `ApplyThemeToControlTree(Control root)` → recursive `switch (child) { case Panel: case Label: }`
4. `ApplyProfessionalLayout()` → explicit pixel `Location`/`Size` math,
   recomputed on `Resize`
5. `PaintCardBorder(object? sender, PaintEventArgs e)` → 1px card outline
6. One private method per operator action, named `btnX_Click` (designer handler)
   or a verb phrase (`EvaluateResults`, `RunPythonScript`, `RecordedAudioPath`)

**Parameters:** explicit and typed; nullable via `?` and a `?` return
(`string? GetSerial()`, `string? WaitForFile(string pattern, int maxSeconds)`).
`MaxRetries`/`RetryDelayMs` are expression-bodied static properties
(`SennheiserTestRunner/Program.cs:21-22`) so they are tunable in one place.

**Return values:** `void` for UI mutations; `int` for `tools/VolumeHelper`
exit codes; `(bool, str)` / `(str, str)` tuples for the Python-mirrored verdict
helpers — `channel_signal_ok` returns `(ok, reasons)`
(`scripts/db_chart.py:196`), `channel_active` returns `(active, reason)`
(`:230`), `evaluate_signal` returns `(ok, reason)` (`:211`), `knob_verdict`
returns a dict (`:218`). **Follow the `(value, reason)` tuple pattern for any
new Python verdict function** — `getFinalResults.py:224` joins them with
`"; ".join(reason)` and `converter.py` surfaces them in `MiscInfo`.

**Single responsibility in Python:** each script has exactly one pure decision
function that the tests target, plus a thin `main()`/`run()` that does IO.
`db_chart.channel_active`, `getFinalResults.knob_verdict`,
`getFinalResults.analyze_audio_levels`, `station_calibration.station_verdict`,
`converter.build_xml`. **New logic belongs in a pure function like these**, not
inside `main()` — that is precisely what makes the Python side testable at all
(see TESTING.md).

## Module Design

**Exports:** no `__all__` in Python. C# has no visibility modifiers on
`internal static class Program` in the usual pattern, and domain classes are
`public`.

**Barrel files:** none. C# has no re-export file; each type is imported
directly. `apps/FunctionalButtonTest/Colors.cs` is the closest thing to a
facade, and it works only because every other file in that project sits in the
same namespace and can write `AppColors.BgCard` unqualified
(`TestPanels.cs:16-25`).

**Static mutable state — two instances, both intentional:**

1. `DeviceAssets.DeviceName` — `public static string DeviceName { get; set; }`
   (`apps/FunctionalButtonTest/Deviceassets.cs:13`), set by the runner
   (`SennheiserTestRunner/Program.cs:273`) or `Program.cs:18`, read by
   `TestPanels` and `Deviceassets`. This is how the selected model crosses
   forms without DI.
2. `getFinalResults.FILE_PATTERN_STATION_CALIB` — a module global monkey-patched
   by the test (`scripts/test_signal_detection.py:288-290,306`). If you add a
   file pattern to `getFinalResults.py`, be aware tests may need to override it.

Also `Environment.SetEnvironmentVariable("DEVICE_NAME", ...)` /
`("STATION_CALIB", ...)` (`SennheiserTestRunner/Program.cs:152,281`) is the
cross-project channel to `AudioTest` and `LevelTest`; both read it in their
constructor. Document new env vars in the README table
(`README.md:73-82`).

**No interfaces, no DI, no factories.** `TestStepManager` takes a concrete
`MainForm` (`TestStepManager.cs:17`) and reaches into `form.Session`,
`form.BtnPass`, `form.LabelStatus`, `form.panelTestArea`. The runner likewise
`new`s forms directly (`SennheiserTestRunner/Program.cs:249,265,284,311,335,360`).
**Do not introduce an interface layer to make something testable** — it will not
match the surrounding code and no consumer needs it. The Python pure-function
split (`TESTING.md`) is the repo's actual answer to testability.

---

*Convention analysis: 2026-09-28*
