"""
Laboratory Benchmark Chart Generator
Generates the 5 mandatory charts required by the academic laboratory guide:
1. Request Load vs Response Time
2. Request Load vs Success Rate
3. Request Load vs Failure Count
4. Test Round vs CPU Usage (%)
5. Test Round vs Memory Usage (MB)
"""

import os
import sys
import glob
import json

def get_latest_benchmark_file(results_dir: str) -> str:
    files = glob.glob(os.path.join(results_dir, "benchmark_report_*.json"))
    if not files:
        # Fallback to any json file in results
        files = glob.glob(os.path.join(results_dir, "*.json"))
    if not files:
        raise FileNotFoundError(f"No benchmark JSON reports found in '{results_dir}'.")
    files.sort(key=os.path.getmtime, reverse=True)
    return files[0]

def generate_charts(json_path: str, output_dir: str):
    try:
        import matplotlib.pyplot as plt
        import matplotlib.ticker as ticker
    except ImportError:
        print("[ERROR] matplotlib is not installed. Install it with: pip install matplotlib")
        sys.exit(1)

    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    rounds = data.get("Rounds", [])
    if not rounds:
        print("[WARNING] No rounds found in the benchmark file.")
        return

    os.makedirs(output_dir, exist_ok=True)
    plt.style.use("seaborn-v0_8-whitegrid" if "seaborn-v0_8-whitegrid" in plt.style.available else "default")

    names = [r.get("RoundName", f"R{i+1}") for i, r in enumerate(rounds)]
    concurrencies = [r.get("ConcurrencyWorkers", i+1) for i, r in enumerate(rounds)]
    total_reqs = [r.get("TotalRequests", 0) for r in rounds]
    success_rates = [r.get("SuccessRatePercent", 0.0) for r in rounds]
    failures = [r.get("FailedRequests", 0) for r in rounds]
    
    mean_latencies = [r.get("Latency", {}).get("MeanMs", 0.0) for r in rounds]
    p95_latencies = [r.get("Latency", {}).get("P95Ms", 0.0) for r in rounds]
    max_latencies = [r.get("Latency", {}).get("MaxMs", 0.0) for r in rounds]
    
    cpu_avgs = [r.get("Telemetry", {}).get("CpuUsageAvg", 0.0) for r in rounds]
    cpu_peaks = [r.get("Telemetry", {}).get("CpuUsagePeak", 0.0) for r in rounds]
    mem_avgs = [r.get("Telemetry", {}).get("MemoryUsageAvgMb", 0.0) for r in rounds]
    mem_peaks = [r.get("Telemetry", {}).get("MemoryUsagePeakMb", 0.0) for r in rounds]

    x_labels = [f"{names[i]}\n(C={concurrencies[i]})" for i in range(len(rounds))]

    # Chart 1: Request Load vs Response Time
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    ax.plot(x_labels, mean_latencies, marker="o", color="#1f77b4", linewidth=2, label="Mean Latency (ms)")
    ax.plot(x_labels, p95_latencies, marker="s", color="#ff7f0e", linewidth=2, linestyle="--", label="P95 Latency (ms)")
    ax.set_title("Request Load vs Response Time", fontsize=13, fontweight="bold", pad=12)
    ax.set_xlabel("Load Round (Concurrency Level)", fontsize=11, labelpad=8)
    ax.set_ylabel("Latency (ms)", fontsize=11, labelpad=8)
    ax.legend(frameon=True)
    plt.tight_layout()
    chart1_path = os.path.join(output_dir, "01_load_vs_response_time.png")
    fig.savefig(chart1_path)
    plt.close(fig)
    print(f"[OK] Generated: {chart1_path}")

    # Chart 2: Request Load vs Success Rate
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    bars = ax.bar(x_labels, success_rates, color="#2ca02c", width=0.55, edgecolor="black", alpha=0.85)
    ax.set_ylim(0, 105)
    ax.set_title("Request Load vs Success Rate", fontsize=13, fontweight="bold", pad=12)
    ax.set_xlabel("Load Round (Concurrency Level)", fontsize=11, labelpad=8)
    ax.set_ylabel("Success Rate (%)", fontsize=11, labelpad=8)
    for bar in bars:
        height = bar.get_height()
        ax.annotate(f"{height:.1f}%",
                    xy=(bar.get_x() + bar.get_width() / 2, height),
                    xytext=(0, 3), textcoords="offset points",
                    ha="center", va="bottom", fontsize=10, fontweight="bold")
    plt.tight_layout()
    chart2_path = os.path.join(output_dir, "02_load_vs_success_rate.png")
    fig.savefig(chart2_path)
    plt.close(fig)
    print(f"[OK] Generated: {chart2_path}")

    # Chart 3: Request Load vs Failure Count
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    bars = ax.bar(x_labels, failures, color="#d62728", width=0.55, edgecolor="black", alpha=0.85)
    ax.set_title("Request Load vs Failure Count", fontsize=13, fontweight="bold", pad=12)
    ax.set_xlabel("Load Round (Concurrency Level)", fontsize=11, labelpad=8)
    ax.set_ylabel("Failed Requests", fontsize=11, labelpad=8)
    for bar in bars:
        height = bar.get_height()
        ax.annotate(f"{int(height)}",
                    xy=(bar.get_x() + bar.get_width() / 2, height),
                    xytext=(0, 3), textcoords="offset points",
                    ha="center", va="bottom", fontsize=10, fontweight="bold")
    plt.tight_layout()
    chart3_path = os.path.join(output_dir, "03_load_vs_failure_count.png")
    fig.savefig(chart3_path)
    plt.close(fig)
    print(f"[OK] Generated: {chart3_path}")

    # Chart 4: Test Round vs CPU Usage
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    ax.plot(x_labels, cpu_avgs, marker="o", color="#9467bd", linewidth=2, label="Average CPU (%)")
    ax.plot(x_labels, cpu_peaks, marker="^", color="#e377c2", linewidth=2, linestyle=":", label="Peak CPU (%)")
    ax.set_ylim(0, 105)
    ax.set_title("Test Round vs CPU Usage", fontsize=13, fontweight="bold", pad=12)
    ax.set_xlabel("Test Round", fontsize=11, labelpad=8)
    ax.set_ylabel("Host CPU (%)", fontsize=11, labelpad=8)
    ax.legend(frameon=True)
    plt.tight_layout()
    chart4_path = os.path.join(output_dir, "04_round_vs_cpu_usage.png")
    fig.savefig(chart4_path)
    plt.close(fig)
    print(f"[OK] Generated: {chart4_path}")

    # Chart 5: Test Round vs Memory Usage
    fig, ax = plt.subplots(figsize=(8, 5), dpi=300)
    ax.plot(x_labels, mem_avgs, marker="o", color="#17becf", linewidth=2, label="Average Memory (MB)")
    ax.plot(x_labels, mem_peaks, marker="^", color="#bcbd22", linewidth=2, linestyle=":", label="Peak Memory (MB)")
    ax.set_title("Test Round vs Memory Usage", fontsize=13, fontweight="bold", pad=12)
    ax.set_xlabel("Test Round", fontsize=11, labelpad=8)
    ax.set_ylabel("Memory (MB)", fontsize=11, labelpad=8)
    ax.legend(frameon=True)
    plt.tight_layout()
    chart5_path = os.path.join(output_dir, "05_round_vs_memory_usage.png")
    fig.savefig(chart5_path)
    plt.close(fig)
    print(f"[OK] Generated: {chart5_path}")

    print(f"\nAll 5 charts saved successfully in '{output_dir}'.")

if __name__ == "__main__":
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    results_folder = os.path.join(base_dir, "results")
    charts_folder = os.path.join(results_folder, "charts")

    if len(sys.argv) > 1:
        target_file = sys.argv[1]
    else:
        try:
            target_file = get_latest_benchmark_file(results_folder)
            print(f"Processing latest benchmark: {os.path.basename(target_file)}")
        except FileNotFoundError as e:
            print(f"[ERROR] {e}")
            sys.exit(1)

    generate_charts(target_file, charts_folder)
