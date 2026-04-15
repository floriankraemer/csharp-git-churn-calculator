# Git Churn Calculator

A .NET 8 tool that calculates a **Churn Risk Score** for every file in a git repository by combining change frequency, author spread, and optional test coverage data.

## Formula

```
ChurnRiskScore = ChangesPerWeek * TotalUniqueAuthors * (1 - CoveragePercent / 100)
```

| Factor | What it measures |
|--------|-----------------|
| **ChangesPerWeek** | How often the file changes (churn velocity) |
| **TotalUniqueAuthors** | How many different people have touched the file (knowledge fragmentation) |
| **(1 - Coverage/100)** | Risk multiplier from missing tests (1.0 = no tests, 0.0 = fully covered) |

Without a coverage file the risk multiplier defaults to 1.0, so the score becomes `ChangesPerWeek * TotalUniqueAuthors`.

## Solution Structure

```
GitChurnCalculator.slnx
├── GitChurnCalculator/           Class library with all business logic
├── GitChurnCalculator.Console/   CLI application (CSV / JSON output)
└── GitChurnCalculator.Tests/     xUnit tests
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- `git` available on PATH

## Build

```bash
dotnet build
```

On Windows you can use [make.ps1](make.ps1) from the repo root, for example `.\make.ps1 build-release`, `.\make.ps1 test`, `.\make.ps1 publish-single`.

## Single-file executable

The console project is configured for [single-file publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview). Publish a **self-contained** bundle (one main executable; no .NET runtime required on the target machine):

```bash
dotnet publish GitChurnCalculator.Console/GitChurnCalculator.Console.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ./artifacts
```

Replace `win-x64` with the [runtime identifier](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) you need (`linux-x64`, `osx-arm64`, etc.).

**PowerShell helper** (defaults to the current machine’s RID):

```powershell
.\make.ps1 publish-single
.\make.ps1 publish-single linux-x64
```

Output is written under `artifacts/publish-<RID>/` (ignored by git).

For a smaller publish that still needs the .NET 8 runtime installed on the target, use `--self-contained false` instead.

## Usage

```bash
dotnet run --project GitChurnCalculator.Console -- <repo-path> [options]
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `<repo-path>` | *(required)* | Path to the git repository to analyze |
| `--format <csv\|json>` | `csv` | Output format |
| `--coverage <path>` | *(none)* | Path to a Cobertura XML coverage file |
| `--output <path>` | stdout | Write output to a file instead of stdout |

### Examples

Analyze a repository and print CSV to stdout:

```bash
dotnet run --project GitChurnCalculator.Console -- /path/to/repo
```

Output as JSON to a file:

```bash
dotnet run --project GitChurnCalculator.Console -- /path/to/repo --format json --output churn.json
```

Include Cobertura coverage data:

```bash
dotnet run --project GitChurnCalculator.Console -- /path/to/repo --coverage coverage.cobertura.xml
```

## Output Fields

| Column | Description |
|--------|-------------|
| File | Tracked file path |
| TotalCommits | Total commits touching this file |
| FirstCommitDate | Date of the first commit |
| LastCommitDate | Date of the most recent commit |
| AgeDays | Days since first commit |
| ChangesPerWeek | Average commits per week over the file's lifetime |
| ChangesPerMonth | Average commits per month |
| ChangesPerYear | Average commits per year |
| CommitsLast7Days | Commits in the last 7 days |
| CommitsLast30Days | Commits in the last 30 days |
| CommitsLast365Days | Commits in the last 365 days |
| TotalUniqueAuthors | Distinct authors (by email) who ever touched the file |
| UniqueAuthorsLast7Days | Distinct authors in the last 7 days |
| UniqueAuthorsLast30Days | Distinct authors in the last 30 days |
| UniqueAuthorsLast365Days | Distinct authors in the last 365 days |
| CoveragePercent | Line coverage from Cobertura XML (empty if not provided) |
| ChurnRiskScore | Computed risk score, sorted descending |

Results are always sorted by **ChurnRiskScore descending** so the highest-risk files appear first.

## Cobertura Coverage Mapping

The tool maps Cobertura XML `<class filename="...">` entries to git-tracked files using:

1. **Source prefix stripping** -- uses `<source>` elements to turn absolute paths into relative ones
2. **Exact match** against git file paths
3. **Suffix match** -- finds the git file whose path ends with the coverage filename
4. **Filename match** -- falls back to matching just the filename portion

## Running Tests

```bash
dotnet test
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
