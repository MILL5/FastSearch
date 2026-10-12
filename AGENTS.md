# FastSearch — agent guidance

## Branching (mandatory)
- All development happens on `release/*` branches (e.g. `release/1.2`).
- `release/*` → `develop` via pull request only.
- `develop` → `main` via pull request only. This is the only way changes reach `main`; never commit or push to `main` or `develop` directly.
- Version tags (`vX.Y.Z`) go on `main` after the develop → main PR merges; pushing the tag runs `.github/workflows/release.yml`, which publishes to NuGet.
- Resolve diverged branches with merge commits, never rebase.

## Build & test
- `dotnet build FastSearch.slnx -c Release -warnaserror` (the WPF sample builds on macOS/Linux via `EnableWindowsTargeting`; it only runs on Windows).
- `dotnet test FastSearch.UnitTests -c Release`
- Heap/latency harness: `cd PasswordSearch.Benchmark && DOTNET_TieredCompilation=0 dotnet run -c Release -- heap <string|linq|hash|charseq|suffixarray|ngram> <items>`. Run one algorithm per process; keep `DOTNET_TieredCompilation=0` so every algorithm is measured with fully optimized code.

## Conventions
- The library targets `net10.0` only, with nullable enabled, AOT/trim compatible, zero warnings.
- Every algorithm implements `ISearch<T>` and must match `StringSearch<T>` (the oracle) on the consistency tests; new algorithms need the same coverage.
- Test data lives once in `data/` and is linked into projects that set `<UsesPasswordData>true</UsesPasswordData>` (see `Directory.Build.props`).
- When benchmark numbers change, update them everywhere together: README tables, the chart PNGs (root and `docs/images/`), `docs/index.html` (table, FAQ text and JSON-LD), `docs/llms.txt`, `docs/*-Spec.md`, and the current benchmark sheets in `archive/*.xlsx`. Regenerate charts with `uv run scripts/generate_benchmark_charts.py`; PNG filenames are lowercase.
