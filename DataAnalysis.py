import os
import json
import numpy as np
import pandas as pd
import re

# Path to the folder containing your JSON files, e.g.:
# Desktop/
#   DepthMap_High_High_1.json
#   Raycast_Low_High_9.json
#   RaycastWithOptimization_High_Low_5.json
base_path = "Desktop"

# Map labels to counts
map_dict = {"Low": 10, "High": 300}

# Recognized method names
METHOD_TOKENS = {"DepthMap", "Raycast", "RaycastWithOptimization"}

def parse_filename(fname: str):
    """
    Robustly parse <...>DepthMap|Raycast|RaycastWithOptimization_<Agents>_<Targets>_<Run>.json
    Accepts optional leading tokens like 'Desktop_'.
    Returns (method_name, agents_label, targets_label, run_id) or None if not parseable.
    """
    name = fname[:-5] if fname.lower().endswith(".json") else fname
    parts = name.split("_")

    # Find the method token within parts
    idx = next((i for i,p in enumerate(parts) if p in METHOD_TOKENS), None)
    if idx is None or idx + 3 >= len(parts):
        return None

    method_name = parts[idx]
    agents_label = parts[idx+1]
    targets_label = parts[idx+2]
    run_token = parts[-1]

    # Run may be like "5" or "Run5"
    m = re.search(r"(\d+)$", run_token)
    if not m:
        return None
    run_id = int(m.group(1))

    if agents_label not in map_dict or targets_label not in map_dict:
        return None

    return method_name, agents_label, targets_label, run_id

def extract_selftime_ms(data: dict):
    """
    Extract a list of SelfTime (ms) per frame from your JSON schema.
    Falls back to 'frame_times' if present.
    """
    # If your files truly have 'frame_times', use them
    if "frame_times" in data and isinstance(data["frame_times"], list):
        # assume already in ms
        out = []
        for v in data["frame_times"]:
            try:
                out.append(float(v))
            except:
                pass
        return out

    # Expected schema seen in your earlier files
    frames = data.get("frames", [])
    vals = []
    for fr in frames:
        funcs = fr.get("functions", [])
        if not funcs:
            continue
        values = funcs[0].get("values", [])
        st = None
        for item in values:
            if str(item.get("column", "")).strip().lower() == "selftime":
                raw = str(item.get("value", "")).strip()
                raw = raw.replace("ms", "").strip()
                try:
                    st = float(raw)
                except:
                    # last resort: keep only number chars
                    num = "".join(ch for ch in raw if ch.isdigit() or ch in ".-")
                    st = float(num) if num not in {"", ".", "-", "-."} else None
                break
        if st is not None:
            vals.append(st)
    return vals

def pretty_method(name: str) -> str:
    if name == "DepthMap":
        return "Depth Map"
    if name == "Raycast":
        return "Raycasting"
    if name == "RaycastWithOptimization":
        return "Raycasting (Optimized)"
    return name

results = []

# Walk all .json files inside base_path (handles flat or nested)
for root, _, files in os.walk(base_path):
    for file in files:
        if not file.lower().endswith(".json"):
            continue

        parsed = parse_filename(file)
        if not parsed:
            # Skip files that don't match the naming convention
            continue

        method_name, agents_label, targets_label, run_id = parsed
        method_name = os.path.basename(root)  # use folder name instead of filename
        file_path = os.path.join(root, file)

        try:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                data = json.load(f)
        except Exception as e:
            print(f"Skipping unreadable JSON: {file_path} ({e})")
            continue

        frame_times = extract_selftime_ms(data)
        if not frame_times:
            # No usable data found in this file
            print(f"No SelfTime data in: {file_path}")
            continue

        frame_times = np.array(frame_times, dtype=float)
        mean_time = float(frame_times.mean())
        std_time  = float(frame_times.std(ddof=0))  # population std to match your original

        results.append({
            "Agents": map_dict[agents_label],
            "Targets": map_dict[targets_label],
            "Method": pretty_method(method_name),
            "Run": run_id,
            "Mean Frame Time (ms)": mean_time,
            "Std. Dev. (ms)": std_time
        })

# Convert to DataFrame
df = pd.DataFrame(results)

if df.empty:
    raise SystemExit(
        "No data collected. Check that:\n"
        " - base_path points to the folder with your JSON files\n"
        " - filenames match the pattern <Method>_<Low|High>_<Low|High>_<run>.json\n"
        " - JSONs contain either 'frame_times' or 'frames/functions/values/SelfTime'"
    )

# Group by scenario to average across runs (10 runs expected, but robust to fewer)
summary = df.groupby(["Agents", "Targets", "Method"], as_index=False).agg(
    **{
        "Mean Frame Time (ms)": ("Mean Frame Time (ms)", "mean"),
        "Std. Dev. (ms)": ("Std. Dev. (ms)", "mean"),  # avg of per-run stds
    }
)

# Nice formatting
summary["Mean Frame Time (ms)"] = summary["Mean Frame Time (ms)"].round(2)
summary["Std. Dev. (ms)"] = summary["Std. Dev. (ms)"].round(2)

# Save + print
summary.to_csv("summary_table.csv", index=False)
print(summary.to_markdown(index=False))
