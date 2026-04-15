# Git Churn Calculator

A .NET 8 tool that calculates a **Churn Risk Score** for every file in a git repository by combining change frequency, author spread, and optional test coverage data.

## How churn is calculated

The CLI reports several git-derived metrics plus a **Churn Risk Score**. Only tracked files with at least one commit in history are included (`TotalCommits > 0`).

### Inputs from git

- **TotalCommits** — how many times the file appears across `git log` (each commit that lists the file counts once).
- **FirstCommitDate** / **LastCommitDate** — timestamps from the log for that file’s first and latest appearance.
- **TotalUniqueAuthors** — count of distinct author emails that touched the file over all time (other author columns use the same idea over rolling windows).

### Change frequency (velocity)

Let **now** be the analysis time (UTC), **first** the file’s first commit date, and:

```math
\text{AgeDays} = \max\bigl(1,\ \text{whole days from first to now}\bigr)
```

Average commit rates use fixed day-length denominators (same as the implementation):

```math
\text{ChangesPerWeek} = \frac{\text{TotalCommits}}{\text{AgeDays} / 7}
\qquad
\text{ChangesPerMonth} = \frac{\text{TotalCommits}}{\text{AgeDays} / 30.44}
\qquad
\text{ChangesPerYear} = \frac{\text{TotalCommits}}{\text{AgeDays} / 365.25}
```

These values are rounded for display (two decimal places in output). The risk score uses the unrounded `ChangesPerWeek` internally, then the final score is rounded to four decimals.

### Churn Risk Score

Let $c = \text{ChangesPerWeek}$, $A = \text{TotalUniqueAuthors}$, and $p$ be line coverage in percent ($0 \le p \le 100$) when Cobertura data exists for that file.

**Without coverage** (no `--coverage` file): coverage is not applied and the risk multiplier is always $1$:

```math
\text{ChurnRiskScore} = c \times A
```

**With coverage** (`--coverage` and a mapped Cobertura class for that path):

```math
\text{ChurnRiskScore} = c \times A \times \left(1 - \frac{p}{100}\right)
```

The factor $\left(1 - \frac{p}{100}\right)$ is **higher when coverage is lower** (no coverage → multiplier $1$; full coverage → multiplier $0$, so the score is $0$ aside from rounding). If a Cobertura file is supplied but a git file is **not** matched to any class, it is treated as **$p = 0$** (same multiplier as “no tests” in the formula above).

| Symbol / field | Meaning |
|----------------|---------|
| $c$ (**ChangesPerWeek**) | Commit velocity over the file’s lifetime |
| $A$ (**TotalUniqueAuthors**) | How many different people have touched the file |
| $1 - p/100$ | Test-gap multiplier from line coverage (only when `--coverage` is used and the file maps into the report) |

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
| `--format <csv\|json\|html>` | `csv` | Output format (`html` = Bootstrap-styled table page) |
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

HTML table report (Bootstrap via CDN; open the file in a browser):

```bash
dotnet run --project GitChurnCalculator.Console -- /path/to/repo --format html --output churn.html
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

## Code coverage (this repository)

The test project uses [Coverlet](https://github.com/coverlet-coverage/coverlet) (MSBuild integration). HTML reports are produced with [ReportGenerator](https://github.com/danielpalme/ReportGenerator) via a [local dotnet tool](.config/dotnet-tools.json).

**PowerShell** (from the repo root):

```powershell
.\make.ps1 coverage
```

This runs `dotnet tool restore`, tests in **Release** with coverage, writes **Cobertura** XML and an **HTML** site under `artifacts/coverage/`:

| Output | Path |
|--------|------|
| Cobertura | `artifacts/coverage/coverage.cobertura.xml` |
| HTML report | `artifacts/coverage/html/index.html` |

**Manual equivalent** (from the repository root; use an **absolute** `CoverletOutput` prefix so the file lands under `artifacts/coverage/`):

```bash
dotnet tool restore
dotnet test GitChurnCalculator.slnx -c Release \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$PWD/artifacts/coverage/coverage" \
  /p:CoverletOutputFormat=cobertura
dotnet tool run reportgenerator -- \
  -reports:artifacts/coverage/coverage.cobertura.xml \
  -targetdir:artifacts/coverage/html \
  -reporttypes:Html
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
