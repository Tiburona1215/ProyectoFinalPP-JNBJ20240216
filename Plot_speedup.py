"""
plot_speedup.py
Genera la grafica de strong scaling (speed-up y eficiencia) a partir de
scaling_results.csv producido por run_scaling.ps1.

Uso:
    python plot_speedup.py
"""

import csv
import os
import matplotlib
matplotlib.use("Agg") 
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
import numpy as np

CSV_FILE   = "scaling_results.csv"
OUTPUT_PNG = "speedup_chart.png"

rows = []
with open(CSV_FILE, newline="") as f:
    reader = csv.DictReader(f)
    for row in reader:
        rows.append(row)

seq_row  = next(r for r in rows if r["threads"] == "1_seq")
par_rows = [r for r in rows if r["threads"] != "1_seq"]

threads    = [int(r["threads"])           for r in par_rows]
speedups   = [float(r["speedup"])         for r in par_rows]
efficiency = [float(r["efficiency"]) * 100 for r in par_rows]   # en %
times      = [float(r["time_s"])          for r in par_rows]
seq_time   = float(seq_row["time_s"])

t_max   = max(threads)
t_ideal = list(range(1, t_max + 1))
s_ideal = t_ideal[:]

fig, axes = plt.subplots(1, 3, figsize=(14, 4.5))
fig.patch.set_facecolor("#0F172A")
DARK   = "#0F172A"
PANEL  = "#1E293B"
TEXT   = "#E2E8F0"
ACCENT = "#38BDF8"
GREEN  = "#4ADE80"
AMBER  = "#FBBF24"
GRID   = "#334155"

for ax in axes:
    ax.set_facecolor(PANEL)
    for spine in ax.spines.values():
        spine.set_edgecolor(GRID)
    ax.tick_params(colors=TEXT, labelsize=9)
    ax.title.set_color(TEXT)
    ax.xaxis.label.set_color(TEXT)
    ax.yaxis.label.set_color(TEXT)
    ax.grid(True, color=GRID, linewidth=0.5, linestyle="--")

ax = axes[0]
ax.plot(t_ideal, s_ideal, "--", color=GRID, linewidth=1.2, label="Ideal (lineal)")
ax.plot(threads, speedups, "o-", color=ACCENT, linewidth=2,
        markersize=7, markerfacecolor=DARK, markeredgewidth=2, label="Medido")
for x, y in zip(threads, speedups):
    ax.annotate(f"{y:.2f}×", xy=(x, y), xytext=(4, 6),
                textcoords="offset points", color=ACCENT, fontsize=8.5)
ax.set_title("Speed-up (strong scaling)", fontsize=11, fontweight="bold", pad=10)
ax.set_xlabel("Threads")
ax.set_ylabel("Speed-up")
ax.set_xticks(threads)
ax.legend(fontsize=8, facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT)

ax = axes[1]
ax.axhline(100, color=GRID, linewidth=1, linestyle="--", label="Ideal 100 %")
ax.plot(threads, efficiency, "s-", color=GREEN, linewidth=2,
        markersize=7, markerfacecolor=DARK, markeredgewidth=2, label="Medida")
for x, y in zip(threads, efficiency):
    ax.annotate(f"{y:.0f}%", xy=(x, y), xytext=(4, 6),
                textcoords="offset points", color=GREEN, fontsize=8.5)
ax.set_title("Eficiencia paralela", fontsize=11, fontweight="bold", pad=10)
ax.set_xlabel("Threads")
ax.set_ylabel("Eficiencia (%)")
ax.set_xticks(threads)
ax.set_ylim(0, 115)
ax.yaxis.set_major_formatter(ticker.PercentFormatter(xmax=100))
ax.legend(fontsize=8, facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT)

ax = axes[2]
all_labels = ["Secuencial"] + [f"{t} thread{'s' if t>1 else ''}" for t in threads]
all_times  = [seq_time] + times
colors     = [AMBER] + [ACCENT] * len(times)
bars       = ax.bar(range(len(all_labels)), all_times, color=colors,
                    width=0.55, edgecolor=DARK, linewidth=0.8)
for bar, t_val in zip(bars, all_times):
    ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.5,
            f"{t_val:.1f}s", ha="center", va="bottom", color=TEXT, fontsize=8.5)
ax.set_title("Tiempo de ejecución", fontsize=11, fontweight="bold", pad=10)
ax.set_ylabel("Segundos")
ax.set_xticks(range(len(all_labels)))
ax.set_xticklabels(all_labels, rotation=20, ha="right", fontsize=8.5)

fig.suptitle("Experimento de Strong Scaling — Simulación SIR 1000×1000, 365 días",
             color=TEXT, fontsize=12, fontweight="bold", y=1.02)

plt.tight_layout()
fig.savefig(OUTPUT_PNG, dpi=150, bbox_inches="tight",
            facecolor=DARK, edgecolor="none")
print(f"Grafica guardada en: {OUTPUT_PNG}")
print(f"\nResumen:")
print(f"  Tiempo secuencial : {seq_time:.3f} s")
for t, s, e, ti in zip(threads, speedups, efficiency, times):
    print(f"  {t:2d} thread(s)       : {ti:.3f} s | speed-up {s:.2f}x | eficiencia {e:.0f}%")