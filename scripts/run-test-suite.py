#!/usr/bin/env python3
"""Validate complete test-project ownership and execute one CI test suite."""

from __future__ import annotations

import argparse
import concurrent.futures
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "scripts" / "test-suites.json"


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


def run_project(project: str, configuration: str, no_restore: bool) -> tuple[str, int, float]:
    name = Path(project).stem
    command = [
        "dotnet", "test", project,
        "--configuration", configuration,
        "--blame-hang-timeout", "10m",
        "--logger", f"trx;LogFileName={name}.trx",
    ]
    if no_restore:
        command.append("--no-restore")

    print(f"::group::Testing {name}", flush=True)
    started = time.monotonic()
    result = subprocess.run(command, cwd=ROOT, check=False)
    elapsed = time.monotonic() - started
    print(f"Completed {name}: exit={result.returncode}, duration={elapsed:.1f}s", flush=True)
    print("::endgroup::", flush=True)
    return project, result.returncode, elapsed


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
    print(
        f"Suite '{args.suite}': {len(parallel_projects)} parallel-eligible projects "
        f"(max concurrency {parallelism}), {len(isolated_projects)} isolated projects."
    )

    failures: list[tuple[str, int]] = []
    started = time.monotonic()
    with concurrent.futures.ThreadPoolExecutor(max_workers=parallelism) as executor:
        futures = [
            executor.submit(run_project, project, args.configuration, args.no_restore)
            for project in parallel_projects
        ]
        for future in concurrent.futures.as_completed(futures):
            project, exit_code, _ = future.result()
            if exit_code:
                failures.append((project, exit_code))

    # Isolated suites run only after parallel projects have stopped. Cosmos uses
    # this path so its emulator cannot compete with other database containers.
    for project in isolated_projects:
        result = run_project(project, args.configuration, args.no_restore)
        if result[1]:
            failures.append((result[0], result[1]))

    print(f"Suite '{args.suite}' completed in {time.monotonic() - started:.1f}s.")
    if failures:
        for project, exit_code in failures:
            print(f"::error::{project} failed with exit code {exit_code}.")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
