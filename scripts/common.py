"""Shared IO helpers for the test-bench scripts: per-machine config.json and
the daily ambient baseline (calibracion.txt)."""
import json
from pathlib import Path


def load_config(path=None):
    """Read per-machine config.json.

    Explicit path wins; otherwise the first config.json found is used (script
    directory, then cwd). Returns {} when nothing is found.
    """
    if path:
        p = Path(path)
        if p.exists():
            try:
                cfg = json.loads(p.read_text(encoding="utf-8"))
            except Exception:
                return {}
            return cfg if isinstance(cfg, dict) else {}
        return {}

    for base in (Path(__file__).resolve().parent, Path.cwd()):
        p = base / "config.json"
        if p.exists():
            try:
                cfg = json.loads(p.read_text(encoding="utf-8"))
            except Exception:
                return {}
            return cfg if isinstance(cfg, dict) else {}
    return {}


def load_baseline(path):
    """Read calibracion.txt -> dict(left_dbfs, right_dbfs) or None."""
    if not path:
        return None
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


def channel_dbfs(payload, channel):
    """Numeric dBFS for a channel from a db_chart JSON payload, else None."""
    for m in payload.get("measurements", []):
        if str(m.get("channel", "")).lower() == channel:
            v = m.get("dbfs")
            return v if isinstance(v, (int, float)) else None
    return None