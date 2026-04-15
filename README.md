# Git Churn Calculator

A .NET 8 tool that calculates a **Churn Risk Score** for every file in a git repository by combining change frequency, author spread, and optional test coverage data.

## Documentation

- **[How churn is calculated](docs/churn-calculation.md)** — formulas, git-derived inputs, and Cobertura-to-repo file mapping.
- **[Build, test, and publish](docs/build.md)** — solution layout, prerequisites, `dotnet` / `make.ps1` workflows, single-file publish, and coverage for this repo.

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

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
