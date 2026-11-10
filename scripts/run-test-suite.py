#!/usr/bin/env python3
"""Validate test-project ownership, build safely, and execute one CI test suite."""

from __future__ import annotations

import argparse
import concurrent.futures
import json
import os
import shutil
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "scripts" / "test-suites.json"
REPORT_ROOT = ROOT / "artifacts" / "test-results"


def load_manifest() -> dict:
    with MANIFEST_PATH.open(encoding="utf-8") as stream:
        manifest = json.load(stream)
    if manifest.get("version") != 1 or not isinstance(manifest.get("suites"), dict):
        raise ValueError("Unsupported or malformed test-suite manifest.")
    return manifest


def validate_coverage(manifest: dict) -> None:
    discovered = {
        path.relative_to(ROOT).as_posix()
        for path in (ROOT / "tests").rglob("*.csproj")
    }
    assigned: dict[str, str] = {}
    for suite_name, suite in manifest["suites"].items():
        projects = suite.get("projects", [])
        isolated = suite.get("isolatedProjects", [])
        if not isinstance(projects, list) or not isinstance(isolated, list):
            raise ValueError(f"Suite '{suite_name}' must define project arrays.")
        if not isinstance(suite.get("parallelism"), int) or suite["parallelism"] < 1:
            raise ValueError(f"Suite '{suite_name}' has invalid parallelism.")
        for project in [*projects, *isolated]:
            if not isinstance(project, str) or not project.endswith(".csproj"):
                raise ValueError(f"Invalid project entry in suite '{suite_name}': {project!r}")
            if project in assigned:
                raise ValueError(
                    f"Test project is assigned more than once: {project} "
                    f"({assigned[project]} and {suite_name})"
                )
            assigned[project] = suite_name
            if not (ROOT / project).is_file():
                raise ValueError(f"Manifest project does not exist: {project}")

    missing = sorted(discovered - assigned.keys())
    stale = sorted(assigned.keys() - discovered)
    if missing or stale:
        details = []
        if missing:
            details.append("Unassigned test projects:\n  " + "\n  ".join(missing))
        if stale:
            details.append("Manifest entries not discovered:\n  " + "\n  ".join(stale))
        raise ValueError("\n".join(details))


def tail(path: Path, line_count: int = 200) -> str:
    try:
        return "\n".join(path.read_text(encoding="utf-8", errors="replace").splitlines()[-line_count:])
    except OSError:
        return "(log file unavailable)"


def run_build(project: str, configuration: str, no_restore: bool, log_path: Path) -> dict:
    command = [
        "dotnet", "build", project,
        "--configuration", configuration,
        "--verbosity", "minimal",
        "-m:2",
    ]
    if no_restore:
        command.append("--no-restore")

    started = time.monotonic()
    with log_path.open("w", encoding="utf-8") as log:
        result = subprocess.run(
            command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=False
        )
    return {
        "project": project,
        "status": "built" if result.returncode == 0 else "build_failed",
        "build_exit_code": result.returncode,
        "build_duration_seconds": round(time.monotonic() - started, 2),
        "build_log": str(log_path.relative_to(ROOT)),
    }


def read_trx(path: Path) -> dict:
    report = {
        "total": 0, "passed": 0, "failed": 0, "skipped": 0,
        "failed_tests": [],
    }
    if not path.is_file():
        return report

    root = ET.parse(path).getroot()
    counters = next((node for node in root.iter() if node.tag.endswith("Counters")), None)
    if counters is not None:
        values = counters.attrib
        report["total"] = int(values.get("total", 0))
        report["passed"] = int(values.get("passed", 0))
        report["failed"] = (
            int(values.get("failed", 0))
            + int(values.get("error", 0))
            + int(values.get("timeout", 0))
            + int(values.get("aborted", 0))
        )
        report["skipped"] = (
            int(values.get("notExecuted", 0))
            + int(values.get("notRunnable", 0))
            + int(values.get("inconclusive", 0))
        )

    for result in root.iter():
        if not result.tag.endswith("UnitTestResult"):
            continue
        if result.attrib.get("outcome", "").lower() not in {"failed", "error", "timeout", "aborted"}:
            continue
        message = ""
        for node in result.iter():
            if node.tag.endswith("Message") and node.text:
                message = node.text.strip()
                break
        report["failed_tests"].append({
            "name": result.attrib.get("testName", "Unknown test"),
            "outcome": result.attrib.get("outcome", "Failed"),
            "message": message,
        })
    return report


def run_tests(
    build_result: dict,
    configuration: str,
    results_directory: Path,
    log_path: Path,
) -> dict:
    results_directory.mkdir(parents=True, exist_ok=True)
    project = build_result["project"]
    trx_path = results_directory / "results.trx"
    command = [
        "dotnet", "test", project,
        "--configuration", configuration,
        "--no-build", "--no-restore",
        "--blame-hang-timeout", "10m",
        "--results-directory", str(results_directory),
        "--logger", "trx;LogFileName=results.trx",
    ]

    started = time.monotonic()
    with log_path.open("w", encoding="utf-8") as log:
        result = subprocess.run(
            command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=False
        )

    counts = read_trx(trx_path)
    return {
        **build_result,
        "status": "passed" if result.returncode == 0 else "test_failed",
        "test_exit_code": result.returncode,
        "test_duration_seconds": round(time.monotonic() - started, 2),
        "test_log": str(log_path.relative_to(ROOT)),
        "trx": str(trx_path.relative_to(ROOT)) if trx_path.is_file() else None,
        **counts,
    }


def write_reports(suite_name: str, results: list[dict], duration: float, output: Path) -> None:
    totals = {
        key: sum(item.get(key, 0) for item in results)
        for key in ("total", "passed", "failed", "skipped")
    }
    failures = [item for item in results if item["status"] != "passed"]
    report = {
        "suite": suite_name,
        "status": "failed" if failures else "passed",
        "duration_seconds": round(duration, 2),
        "totals": totals,
        "projects": results,
    }
    (output / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8")

    lines = [
        f"# {suite_name.title()} test suite",
        "",
        f"**Result:** {'FAILED' if failures else 'PASSED'}  ",
        f"**Duration:** {duration:.1f}s  ",
        f"**Tests:** {totals['passed']} passed, {totals['failed']} failed, "
        f"{totals['skipped']} skipped, {totals['total']} total",
        "",
        "| Project | Result | Passed | Failed | Skipped | Total | Build (s) | Tests (s) |",
        "|---|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for item in results:
        lines.append(
            f"| {Path(item['project']).stem} | {item['status']} | "
            f"{item.get('passed', 0)} | {item.get('failed', 0)} | "
            f"{item.get('skipped', 0)} | {item.get('total', 0)} | "
            f"{item.get('build_duration_seconds', 0):.1f} | "
            f"{item.get('test_duration_seconds', 0):.1f} |"
        )

    for item in failures:
        lines.extend(["", f"## {Path(item['project']).stem}: {item['status']}"])
        if item.get("failed_tests"):
            for failed in item["failed_tests"]:
                lines.extend([
                    f"- **{failed['name']}** ({failed['outcome']})",
                    f"  {failed['message'] or 'No failure message was recorded.'}",
                ])
        else:
            log = item.get("build_log") if item["status"] == "build_failed" else item.get("test_log")
            lines.extend([
                f"See full log artifact: `{log}`",
                "",
                "```text",
                tail(ROOT / log, 80) if log else "No log was produced.",
                "```",
            ])

    markdown = "\n".join(lines) + "\n"
    (output / "summary.md").write_text(markdown, encoding="utf-8")
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        with Path(summary_path).open("a", encoding="utf-8") as summary:
            summary.write(markdown)


def execute_parallel(
    build_results: list[dict],
    configuration: str,
    parallelism: int,
    output: Path,
) -> dict[str, dict]:
    tested: dict[str, dict] = {}
    if not build_results:
        return tested

    test_logs = output / "test-logs"
    test_logs.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=parallelism) as executor:
        futures = {
            executor.submit(
                run_tests,
                item,
                configuration,
                output / "trx" / Path(item["project"]).stem,
                test_logs / f"{Path(item['project']).stem}.log",
            ): item["project"]
            for item in build_results
        }
        for future in concurrent.futures.as_completed(futures):
            result = future.result()
            tested[result["project"]] = result
            print(
                f"Tested {Path(result['project']).stem}: {result['status']} "
                f"({result['passed']} passed, {result['failed']} failed, "
                f"{result['skipped']} skipped; {result['test_duration_seconds']:.1f}s)",
                flush=True,
            )
            if result["status"] == "test_failed":
                print(f"::error::{result['project']} failed during test execution.")
                for failed in result["failed_tests"]:
                    print(f"::error::{failed['name']}: {failed['message']}")
                print(tail(ROOT / result["test_log"]))
    return tested


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("suite", nargs="?", help="Suite name; omit to validate coverage only.")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--no-restore", action="store_true")
    args = parser.parse_args()

    try:
        manifest = load_manifest()
        validate_coverage(manifest)
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"::error::Test-suite manifest validation failed: {error}", file=sys.stderr)
        return 2

    if args.suite is None:
        total = sum(
            len(suite.get("projects", [])) + len(suite.get("isolatedProjects", []))
            for suite in manifest["suites"].values()
        )
        print(f"Validated exact CI ownership for all {total} test projects.")
        return 0

    suite = manifest["suites"].get(args.suite)
    if suite is None:
        print(f"::error::Unknown test suite '{args.suite}'.", file=sys.stderr)
        return 2

    parallel_projects = suite.get("projects", [])
    isolated_projects = suite.get("isolatedProjects", [])
    parallelism = suite["parallelism"]
    output = REPORT_ROOT / args.suite
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)
    build_logs = output / "build-logs"
    build_logs.mkdir()

    print(
        f"Suite '{args.suite}': serial build of {len(parallel_projects) + len(isolated_projects)} "
        f"projects; test concurrency is capped at {parallelism}."
    )

    suite_started = time.monotonic()
    build_results: dict[str, dict] = {}
    # Projects share bin/obj outputs through their dependency graph. Never overlap
    # separate MSBuild invocations in one runner workspace.
    for project in [*parallel_projects, *isolated_projects]:
        name = Path(project).stem
        print(f"Building {name}...", flush=True)
        result = run_build(
            project, args.configuration, args.no_restore, build_logs / f"{name}.log"
        )
        build_results[project] = result
        print(
            f"Build {name}: {result['status']} ({result['build_duration_seconds']:.1f}s)",
            flush=True,
        )
        if result["status"] == "build_failed":
            print(f"::error::{project} failed to build.")
            print(tail(ROOT / result["build_log"]))

    regular_builds = [
        build_results[project]
        for project in parallel_projects
        if build_results[project]["status"] == "built"
    ]
    tested = execute_parallel(
        regular_builds, args.configuration, parallelism, output
    )

    # Isolated projects (Cosmos emulator) execute only after every regular project
    # has stopped, preserving the manifest's explicit isolation contract.
    for project in isolated_projects:
        result = build_results[project]
        if result["status"] != "built":
            continue
        tested.update(execute_parallel([result], args.configuration, 1, output))

    final_results = [
        tested.get(project, build_results[project])
        for project in [*parallel_projects, *isolated_projects]
    ]
    duration = time.monotonic() - suite_started
    write_reports(args.suite, final_results, duration, output)

    failures = [item for item in final_results if item["status"] != "passed"]
    totals = {
        key: sum(item.get(key, 0) for item in final_results)
        for key in ("total", "passed", "failed", "skipped")
    }
    print(
        f"Suite '{args.suite}': {'FAILED' if failures else 'PASSED'} in {duration:.1f}s; "
        f"{totals['passed']} passed, {totals['failed']} failed, "
        f"{totals['skipped']} skipped, {totals['total']} total."
    )
    for item in failures:
        print(f"::error::{item['project']} ended with {item['status']}.")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
