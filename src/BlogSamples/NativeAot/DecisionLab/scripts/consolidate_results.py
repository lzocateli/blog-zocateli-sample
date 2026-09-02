"""Consolida resultados brutos do DecisionLab sem transcrição manual."""

from __future__ import annotations

import argparse
import csv
import json
import re
import statistics
from collections import defaultdict
from collections.abc import Iterable
from pathlib import Path
from typing import TypedDict


class SummaryRow(TypedDict):
    source: str
    profile: str
    category: str
    metric: str
    unit: str
    samples: int
    median: float
    minimum: float
    maximum: float
    mad: float
    ratio_to_jit_fdd: float | None
    within_noise: bool | None


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results", type=Path, help="Diretório com os arquivos *-raw.csv")
    parser.add_argument("--noise-threshold", type=float, default=0.03)
    return parser.parse_args()


def read_successful_rows(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8", newline="") as source:
        rows = list(csv.DictReader(source))
    return [row for row in rows if row["succeeded"].lower() == "true"]


def read_crank_rows(results: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    pattern = re.compile(r"crank-(jit-fdd|jit-scd|r2r-scd|native-aot)-(\d{2})\.json$")
    for path in sorted(results.glob("crank-*.json")):
        match = pattern.fullmatch(path.name)
        if match is None:
            continue

        document = json.loads(path.read_text(encoding="utf-8"))
        if document["returnCode"] != 0:
            raise ValueError(f"Rodada Crank falhou: {path}")

        jobs = document["jobResults"]["jobs"]
        application = jobs["application"]["results"]
        load = jobs["load"]["results"]
        if load["http/requests/badresponses"] != 0:
            raise ValueError(f"Rodada Crank contém respostas inválidas: {path}")

        rows.append(
            {
                "profile": match.group(1),
                "category": "http",
                "round": match.group(2),
                "requests_per_second": str(load["http/rps/mean"]),
                "latency_p50_ms": str(load["http/latency/50"]),
                "latency_p95_ms": str(load["http/latency/95"]),
                "latency_p99_ms": str(load["http/latency/99"]),
                "cpu_percent": str(application["benchmarks/cpu"]),
                "working_set_mb": str(application["benchmarks/working-set"]),
            }
        )

    if rows:
        expected_profiles = {"jit-fdd", "jit-scd", "r2r-scd", "native-aot"}
        counts = {profile: 0 for profile in expected_profiles}
        for row in rows:
            counts[row["profile"]] += 1
        incomplete = {profile: count for profile, count in counts.items() if count != 5}
        if incomplete:
            details = ", ".join(
                f"{profile}={count}" for profile, count in sorted(incomplete.items())
            )
            raise ValueError(f"Campanha Crank incompleta; esperado 5 por perfil: {details}")

    return rows


def read_benchmarkdotnet_rows(results: Path) -> list[SummaryRow]:
    reports = sorted((results / "benchmarkdotnet" / "results").glob("*-report-full.json"))
    if not reports:
        raise ValueError("Relatório completo do BenchmarkDotNet não encontrado")

    document = json.loads(reports[-1].read_text(encoding="utf-8"))
    rows: list[SummaryRow] = []
    for benchmark in document["Benchmarks"]:
        statistics_data = benchmark.get("Statistics")
        if statistics_data is None:
            raise ValueError(f"Benchmark sem resultados: {benchmark['DisplayInfo']}")

        profile = "native-aot" if "NativeAOT" in benchmark["DisplayInfo"] else "jit-fdd"
        values = statistics_data["OriginalValues"]
        median = statistics.median(values)
        rows.append(
            {
                "source": "benchmarkdotnet",
                "profile": profile,
                "category": "workload",
                "metric": "duration_ns",
                "unit": "ns",
                "samples": len(values),
                "median": median,
                "minimum": min(values),
                "maximum": max(values),
                "mad": statistics.median(abs(value - median) for value in values),
                "ratio_to_jit_fdd": None,
                "within_noise": None,
            }
        )
        allocated = float(benchmark["Memory"]["BytesAllocatedPerOperation"])
        rows.append(
            {
                "source": "benchmarkdotnet",
                "profile": profile,
                "category": "workload",
                "metric": "allocated_bytes",
                "unit": "bytes",
                "samples": 1,
                "median": allocated,
                "minimum": allocated,
                "maximum": allocated,
                "mad": 0,
                "ratio_to_jit_fdd": None,
                "within_noise": None,
            }
        )

    duration_profiles = [
        row["profile"] for row in rows if row["metric"] == "duration_ns"
    ]
    expected_profiles = ["jit-fdd", "native-aot"]
    if sorted(duration_profiles) != expected_profiles:
        raise ValueError(
            "BenchmarkDotNet incompleto; esperado um resultado para "
            f"cada perfil: {', '.join(expected_profiles)}"
        )

    return rows


def summarize(
    source: str,
    rows: Iterable[dict[str, str]],
    group_fields: tuple[str, ...],
    metrics: dict[str, str],
    noise_threshold: float,
) -> list[SummaryRow]:
    grouped: dict[tuple[str, ...], list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[tuple(row[field] for field in group_fields)].append(row)

    partial: list[SummaryRow] = []
    for group, group_rows in sorted(grouped.items()):
        dimensions = dict(zip(group_fields, group, strict=True))
        for metric, unit in metrics.items():
            values = [float(row[metric]) for row in group_rows]
            median = statistics.median(values)
            partial.append(
                {
                    "source": source,
                    "profile": dimensions["profile"],
                    "category": dimensions.get("category", "startup"),
                    "metric": metric,
                    "unit": unit,
                    "samples": len(values),
                    "median": median,
                    "minimum": min(values),
                    "maximum": max(values),
                    "mad": statistics.median(abs(value - median) for value in values),
                    "ratio_to_jit_fdd": None,
                    "within_noise": None,
                }
            )

    baselines = {
        (row["source"], row["category"], row["metric"]): row["median"]
        for row in partial
        if row["profile"] == "jit-fdd"
    }
    for row in partial:
        baseline = baselines.get((row["source"], row["category"], row["metric"]))
        if baseline is None or baseline == 0:
            continue
        row["ratio_to_jit_fdd"] = row["median"] / baseline
        row["within_noise"] = abs(row["median"] - baseline) / baseline <= noise_threshold

    return partial


def add_baseline_ratios(rows: list[SummaryRow], noise_threshold: float) -> None:
    baselines = {
        (row["source"], row["category"], row["metric"]): row["median"]
        for row in rows
        if row["profile"] == "jit-fdd"
    }
    for row in rows:
        baseline = baselines.get((row["source"], row["category"], row["metric"]))
        if baseline is None or baseline == 0:
            continue
        row["ratio_to_jit_fdd"] = row["median"] / baseline
        row["within_noise"] = abs(row["median"] - baseline) / baseline <= noise_threshold


def write_csv(path: Path, rows: list[SummaryRow]) -> None:
    fieldnames = list(SummaryRow.__annotations__)
    with path.open("w", encoding="utf-8", newline="") as destination:
        writer = csv.DictWriter(destination, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> None:
    args = parse_args()
    publish = read_successful_rows(args.results / "publish-raw.csv")
    startup = read_successful_rows(args.results / "startup-raw.csv")

    rows = summarize(
        "publish",
        publish,
        ("profile", "category"),
        {
            "duration_ms": "ms",
            "uncompressed_bytes": "bytes",
            "compressed_bytes": "bytes",
        },
        args.noise_threshold,
    )
    rows.extend(
        summarize(
            "startup",
            startup,
            ("profile",),
            {"duration_ms": "ms"},
            args.noise_threshold,
        )
    )

    crank = read_crank_rows(args.results)
    if crank:
        rows.extend(
            summarize(
                "crank",
                crank,
                ("profile", "category"),
                {
                    "requests_per_second": "requests/s",
                    "latency_p50_ms": "ms",
                    "latency_p95_ms": "ms",
                    "latency_p99_ms": "ms",
                    "cpu_percent": "%",
                    "working_set_mb": "MB",
                },
                args.noise_threshold,
            )
        )

    rows.extend(read_benchmarkdotnet_rows(args.results))
    add_baseline_ratios(rows, args.noise_threshold)

    write_csv(args.results / "summary.csv", rows)
    (args.results / "summary.json").write_text(
        json.dumps(rows, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Consolidados {len(rows)} grupos em {args.results}")


if __name__ == "__main__":
    main()