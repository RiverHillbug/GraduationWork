import os, zipfile, glob, json, math, shutil, colorsys
import numpy as np
import matplotlib.pyplot as plt

# ------------------ CONFIG ------------------
INPUT_DIR = "./benchmarks"   # folder where you put the .zip files
OUTPUT_DIR = "./charts"      # folder for generated PNGs

# --------------------------------------------

# Helpers
def extract_runs_from_zip(zip_path, work_root):
    name = os.path.splitext(os.path.basename(zip_path))[0]
    out_dir = os.path.join(work_root, name)
    if os.path.exists(out_dir):
        shutil.rmtree(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    with zipfile.ZipFile(zip_path, 'r') as zf:
        zf.extractall(out_dir)
    
    json_files = sorted(glob.glob(os.path.join(out_dir, '*.json')))
    runs = []
    for jf in json_files:
        with open(jf, 'r', encoding='utf-8', errors='ignore') as f:
            try:
                data = json.load(f)
            except:
                data = json.loads(f.read())
        frames = data.get('frames', [])
        run_vals = []
        for fr in frames:
            funcs = fr.get('functions', [])
            if not funcs:
                continue
            values = funcs[0].get('values', [])
            st = None
            for item in values:
                if str(item.get('column', '')).strip().lower() == 'selftime':
                    st_raw = str(item.get('value', '0')).replace('ms','').replace(' ', '')
                    try:
                        st = float(st_raw)
                    except:
                        num = ''.join(ch for ch in st_raw if (ch.isdigit() or ch in '.-'))
                        st = float(num) if num not in ('', '.', '-', '-.') else 0.0
                    break
            run_vals.append(st if st is not None else 0.0)
        if run_vals:
            runs.append(run_vals)
    return name, runs

def chunked_average(series, window=10):
    n = len(series)
    if n == 0: return [], []
    xs, ys = [], []
    i = window-1
    while i < n:
        xs.append(i)
        ys.append(np.mean(series[i-window+1:i+1]))
        i += window
    if xs and xs[-1] != n-1:
        rem = n % window
        if rem == 0: rem = window
        xs.append(n-1)
        ys.append(np.mean(series[n-rem:]))
    elif not xs:
        xs.append(n-1)
        ys.append(np.mean(series))
    return xs, ys

def overall_mean_series(runs):
    n = max(len(r) for r in runs) if runs else 0
    out = []
    for i in range(n):
        vals = [r[i] for r in runs if i < len(r)]
        out.append(np.mean(vals) if vals else np.nan)
    return out

def run_palette(n=10, saturation=0.40, lightness=0.55):
    cols = []
    for k in range(n):
        h = (k/n) % 1.0
        r,g,b = colorsys.hls_to_rgb(h, lightness, saturation)
        cols.append((r,g,b))
    return cols

# Collect all zips
zip_files = [os.path.join(INPUT_DIR, f) for f in os.listdir(INPUT_DIR) if f.endswith(".zip")]
if not zip_files:
    print("No zip files found in", INPUT_DIR)
    exit()

# Temporary extraction root
tmp_root = os.path.join(OUTPUT_DIR, "_tmp")
if os.path.exists(tmp_root):
    shutil.rmtree(tmp_root)
os.makedirs(tmp_root, exist_ok=True)

# Load all data
all_runs_map = {}
for zp in zip_files:
    name, runs = extract_runs_from_zip(zp, tmp_root)
    # If you want Raycast_High_High divided by 10, uncomment:
    # if "Raycast_High_High" in name:
    #     runs = [[v/10.0 for v in r] for r in runs]
    all_runs_map[name] = runs

# Compute global axes
global_min = float('inf')
global_max = -float('inf')
global_max_len = 0
for runs in all_runs_map.values():
    for r in runs:
        if not r: continue
        global_min = min(global_min, min(r))
        global_max = max(global_max, max(r))
        global_max_len = max(global_max_len, len(r))

rng = max(0.001, global_max - global_min)
pad = max(0.05 * rng, 0.1)
ymin = max(0.0, global_min - pad)
ymax = global_max + pad
x_axis_end = int(math.ceil(max(global_max_len-1, 300) / 50.0) * 50)

palette = run_palette(10)
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Generate charts
for name, runs in all_runs_map.items():
    avg_series = overall_mean_series(runs)
    xs, ys = chunked_average(avg_series, window=10)
    
    fig = plt.figure(figsize=(12,6), dpi=150)
    ax = fig.add_axes([0.09,0.12,0.86,0.78])
    for i,r in enumerate(runs[:10]):
        x = np.arange(len(r))
        ax.plot(x, r, linewidth=1.0, alpha=0.95, color=palette[i], label='_nolegend_')
    ax.plot(xs, ys, linewidth=2.8, color='black', label='Smoothed overall average (10 frames)')
    ax.scatter(xs, ys, s=14, color='black', zorder=5)

    ax.set_title(f"Desktop_{name}", fontsize=14, pad=10)    # Desktop hard coded in name here
    ax.set_xlabel("FrameIndex", fontsize=12)
    ax.set_ylabel("SelfTime (ms)", fontsize=12)
    ax.set_xlim(0, x_axis_end)
    ax.set_ylim(ymin, ymax)

    leg = ax.legend(loc='upper right', frameon=True, fontsize=10)
    leg.get_frame().set_alpha(0.85)
    ax.grid(True, linewidth=0.6, alpha=0.25)

    out_path = os.path.join(OUTPUT_DIR, f"Desktop_{name}.png")
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    print("Saved:", out_path)

print("All charts done!")
