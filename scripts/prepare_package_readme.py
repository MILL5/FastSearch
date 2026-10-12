#!/usr/bin/env python3
"""Resolve repository-relative README links for NuGet packaging only."""

import os
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
revision = os.environ.get("GITHUB_REF_NAME")
if not revision or not re.fullmatch(r"v\d+\.\d+\.\d+", revision):
    raise SystemExit("Run on a version tag to pin package documentation to the release.")


def resolve(match):
    label, target = match.groups()
    if re.match(r"(?:[a-z]+:|#|/)", target):
        return match.group(0)
    location = "raw.githubusercontent.com/MILL5/FastSearch" if target.endswith(".png") else "github.com/MILL5/FastSearch/blob"
    return f"{label}(https://{location}/{revision}/{target})"


readme = root / "README.md"
readme.write_text(re.sub(r"(!?\[[^\]\n]*\])\(([^)\n]+)\)", resolve, readme.read_text()))
