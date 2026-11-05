#!/usr/bin/env python3
"""Validate package READMEs embedded in published NuGet packages."""

from pathlib import Path
import sys

PACKAGE_README_GLOB = "sources/*/readme.md"
MINIMUM_PACKAGE_READMES = 60
FORBIDDEN_TEXT = (
    "private azure",
    "private nuget feed",
    "private feed",
    "your-private-feed-url",
    "azureprivatefeed",
    "zureprivatefeed",
    "dotnet nuget add source",
    "store-password-in-clear-text",
    "--username",
    "--password",
    "hosted privately",
    "not publicly available",
    "internal licensing terms",
    "private franz framework",
)

readmes = sorted(Path(".").glob(PACKAGE_README_GLOB))
errors: list[str] = []

if len(readmes) < MINIMUM_PACKAGE_READMES:
    errors.append(
        f"Expected at least {MINIMUM_PACKAGE_READMES} package READMEs, found {len(readmes)}."
    )

for path in readmes:
    content = path.read_text(encoding="utf-8-sig")
    normalized = content.casefold()

    for phrase in FORBIDDEN_TEXT:
        if phrase in normalized:
            errors.append(f"{path}: contains prohibited wording ({phrase}).")

    lines = content.splitlines()
    if "dotnet add package" not in normalized:
        errors.append(f"{path}: missing a public NuGet installation command.")

    if not any(line.startswith("> ") and line[2:].strip() for line in lines):
        errors.append(f"{path}: missing a concise package description blockquote.")

    def section_text(heading: str) -> str | None:
        start = next(
            (index for index, line in enumerate(lines) if line.strip().casefold() == heading.casefold()),
            None,
        )
        if start is None:
            return None
        end = next(
            (index for index in range(start + 1, len(lines)) if lines[index].startswith("## ")),
            len(lines),
        )
        return "\n".join(lines[start + 1 : end]).strip()

    overview = section_text("## Overview")
    if overview is None or len(overview) < 40:
        errors.append(f"{path}: missing a meaningful Overview description.")

    license_text = section_text("## License")
    if license_text is None or "mit" not in license_text.casefold():
        errors.append(f"{path}: License section must explicitly state MIT.")

if errors:
    print("Package README validation failed:")
    for error in errors:
        print(f"  - {error}")
    sys.exit(1)

print(
    f"Validated {len(readmes)} package READMEs: public installation guidance and MIT licensing confirmed."
)
