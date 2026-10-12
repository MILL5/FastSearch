#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.10"
# dependencies = ["matplotlib==3.10.8"]
# ///
"""Regenerate benchmark PNGs: uv run scripts/generate_benchmark_charts.py.

README.md is the sole numeric source. This does not run new benchmarks.
Optimization entries describe the implementations in FastSearch/Algorithms.
"""

from pathlib import Path
import re
import shutil

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ALGORITHMS = ["NGramSearch", "SuffixArraySearch", "CharSequenceSearch", "HashSearch", "LinqSearch", "StringSearch"]
COLORS = ["#087f8c", "#5865bb", "#bd6835", "#ad477c", "#55744e", "#637386"]
SOURCE = "Source: README.md • v1.1 / .NET 10 • bundled password data • DOTNET_TieredCompilation=0"


def read_tables():
    tables = []
    current = None
    for line in (ROOT / "README.md").read_text().splitlines():
        if line.startswith("| Items | Algorithm |"):
            current = []
            tables.append(current)
        elif current is not None and line.startswith("|"):
            cells = [cell.strip().replace("**", "").replace(" (new)", "") for cell in line.strip("|").split("|")]
            if cells[0].replace(",", "").isdigit():
                current.append(cells)
        else:
            current = None
    if len(tables) != 2:
        raise ValueError("Expected README memory and latency tables")
    for table in tables:
        for size in {row[0] for row in table}:
            if {row[1] for row in table if row[0] == size} != set(ALGORITHMS):
                raise ValueError(f"Missing algorithm at {size} items")
    return tables


def number(cell, kind):
    match = re.fullmatch(r"([\d.]+)\s*(\S+)?", cell)
    if not match:
        raise ValueError(f"Invalid measurement: {cell}")
    value, unit = match.groups()
    factors = {"time": {"µs": 1, "ms": 1000, "s": 1_000_000}, "heap": {"MiB": 1, "GiB": 1024}}
    return float(value) * factors[kind][unit]


def save(fig, filename, mirror=None, note="", source=SOURCE):
    fig.text(0.02, 0.055, note, fontsize=9, color="#405164")
    fig.text(0.02, 0.022, source, fontsize=8, color="#637386")
    fig.savefig(ROOT / filename, dpi=170, facecolor="white")
    plt.close(fig)
    if mirror:
        shutil.copyfile(ROOT / filename, ROOT / "docs/images" / mirror)


def style(ax):
    ax.grid(axis="x", alpha=0.18, which="major")
    ax.set_axisbelow(True)
    for spine in ("top", "right", "left"):
        ax.spines[spine].set_visible(False)
    ax.tick_params(axis="y", length=0)


def bars(ax, values, label):
    # Zeros stay zero in labels; use a marked plotting floor only on log axes.
    positives = [v for v in values if v > 0]
    floor = min(positives) / 3
    plotted = [v if v > 0 else floor for v in values]
    ax.barh(ALGORITHMS, plotted, color=COLORS, height=0.6)
    ax.invert_yaxis()
    ax.set_xscale("log")
    ax.set_xlim(floor / 1.7, max(positives) * 8)
    ax.set_xlabel(label)
    for i, value in enumerate(values):
        text = f"{value:,.2f}".rstrip("0").rstrip(".") if value else "0 (reported)"
        ax.text(plotted[i] * 1.15, i, text, va="center", fontsize=9)
    style(ax)


def main():
    memory, latency = read_tables()
    largest = max(int(row[0].replace(",", "")) for row in memory)
    size_text = f"{largest:,} items"
    latest_mem = {row[1]: row for row in memory if int(row[0].replace(",", "")) == largest}
    latest_lat = {row[1]: row for row in latency if int(row[0].replace(",", "")) == largest}
    plt.rcParams.update({"font.family": "DejaVu Sans", "font.size": 10, "axes.titleweight": "bold"})

    fig, ax = plt.subplots(figsize=(11, 5.6))
    bars(ax, [number(latest_lat[a][-1], "time") for a in ALGORITHMS], "Mean Search call latency (µs; log scale) — lower is better")
    ax.set_title(f"Cold random substring searches • {size_text}", pad=16)
    fig.subplots_adjust(left=0.20, right=0.97, top=0.87, bottom=0.22)
    save(fig, "search_performance.png", "search-performance.png", "1,000 random queries (1–8 characters). Search call only; excludes result enumeration.")

    fig, axes = plt.subplots(1, 3, figsize=(17, 6))
    panels = [(2, "time", "Build time (ms)", 1000), (3, "heap", "Index heap (MiB)", 1), (5, "heap", "Heap after queries (MiB)", 1)]
    for ax, (col, kind, title, divisor) in zip(axes, panels):
        bars(ax, [number(latest_mem[a][col], kind) / divisor for a in ALGORITHMS], "Log scale — lower is better")
        ax.set_title(title, pad=13)
    fig.suptitle(f"Indexing and retained heap • {size_text}", fontsize=17, weight="bold")
    fig.subplots_adjust(left=0.13, right=0.97, wspace=0.85, top=0.83, bottom=0.23)
    save(fig, "speeds.png", "indexing-cost.png", "Heap excludes input strings; after-queries heap follows random and named query workloads. Reported zeros use a display floor.")

    fig, ax = plt.subplots(figsize=(12, 6.5))
    queries = ["a", "12", "love", "phoenix", "catherine"]
    for a, color in zip(ALGORITHMS, COLORS):
        ax.plot(queries, [number(v, "time") for v in latest_lat[a][2:7]], marker="o", lw=2, label=a, color=color)
    ax.set_yscale("log")
    ax.set_ylabel("Mean latency (µs; log scale) — lower is better")
    ax.set_xlabel("Named query")
    ax.set_title(f"Warm named queries, including all results • {size_text}", pad=15)
    ax.grid(axis="y", alpha=0.2)
    ax.legend(loc="upper left", bbox_to_anchor=(1.01, 1), frameon=False)
    fig.subplots_adjust(left=0.09, right=0.77, top=0.87, bottom=0.22)
    save(fig, "searchresults.png", "search-latency.png", "2,000 warm calls per query (20 for scans); every result is enumerated. Separate from the cold random Search-call metric.")

    fig, ax = plt.subplots(figsize=(11, 5.6))
    sizes = sorted({int(row[0].replace(",", "")) for row in memory})
    for a, color in zip(ALGORITHMS, COLORS):
        values = [number(next(row[2] for row in memory if row[1] == a and int(row[0].replace(",", "")) == n), "time") / 1000 for n in sizes]
        ax.plot(sizes, values, marker="o", label=a, color=color)
    ax.set_xscale("log")
    ax.set_yscale("symlog", linthresh=1)
    ax.set_ylim(0, max(number(row[2], "time") / 1000 for row in memory) * 1.5)
    ax.set_xticks(sizes, [f"{n:,}" for n in sizes])
    ax.set_xlabel("Items indexed (log scale)")
    ax.set_ylabel("Build time (ms; symlog, linear from 0 to 1 ms)")
    ax.set_title("Build time across dataset sizes", pad=15)
    ax.grid(alpha=0.18)
    ax.legend(loc="upper left", bbox_to_anchor=(1.01, 1), frameon=False)
    fig.subplots_adjust(left=0.1, right=0.75, top=0.87, bottom=0.23)
    save(fig, "index_performance.png", note="Lower is better. StringSearch build time is reported as 0 ms at each size; zero is shown without substitution.")

    features = ["Pre-normalized text", "Parallel index build", "Parallel query scan", "All-substring index", "Prefix range tables", "Bounded query cache", "Compressed postings", "Lazy posting results"]
    # Code-backed properties, not performance measurements or a quality ranking.
    properties = [
        [1, 0, 0, 0, 0, 0, 1, 1],  # NGramSearch
        [1, 0, 0, 0, 1, 1, 0, 0],  # SuffixArraySearch
        [1, 1, 0, 1, 0, 0, 0, 0],  # CharSequenceSearch
        [1, 1, 0, 1, 0, 0, 0, 0],  # HashSearch
        [1, 0, 1, 0, 0, 0, 0, 0],  # LinqSearch
        [0, 0, 0, 0, 0, 0, 0, 0],  # StringSearch
    ]
    fig, ax = plt.subplots(figsize=(13, 5.9))
    ax.imshow(np.array(properties), cmap=ListedColormap(["#edf1f5", "#087f8c"]), vmin=0, vmax=1, aspect="auto")
    ax.set_xticks(range(len(features)), features, rotation=28, ha="right")
    ax.set_yticks(range(len(ALGORITHMS)), ALGORITHMS)
    for y, row in enumerate(properties):
        for x, value in enumerate(row):
            ax.text(x, y, "Yes" if value else "—", ha="center", va="center", color="white" if value else "#637386", fontsize=11)
    ax.set_title("Implementation features across all six algorithms", pad=18)
    ax.tick_params(length=0)
    for spine in ax.spines.values():
        spine.set_visible(False)
    fig.subplots_adjust(left=0.18, right=0.98, top=0.86, bottom=0.36)
    save(fig, "optimizations.png", "optimizations.png", "Lazy posting results apply to indexed short grams; longer n-gram queries verify candidates.", source="Source: FastSearch/Algorithms/*.cs • implementation properties, not performance measurements")
    print("Regenerated five root charts and four matching docs/images charts from README.md.")


if __name__ == "__main__":
    main()
