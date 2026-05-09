# Build, test, and publish

Commands below assume the **repository root** as the current directory. For what the tool computes, see [How churn is calculated](churn-calculation.md). For usage, see the [README](../README.md).

## Solution structure

```
GitChurnCalculator.slnx
├── GitChurnCalculator/                 Class library with all business logic
├── GitChurnCalculator.Console/       CLI application (CSV / JSON output)
├── GitChurnCalculator.Tests/           xUnit tests for the class library
└── GitChurnCalculator.Console.Tests/   xUnit tests for the console app (reporting, etc.)
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- `git` available on PATH

## Build

```bash
dotnet build
```

On Windows you can use [make.ps1](../make.ps1) from the repo root, for example `.\make.ps1 build-release`, `.\make.ps1 test`, `.\make.ps1 publish-single`.

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

Pushing a git tag that matches SemVer **without** a `v` prefix runs the [release workflow](../.github/workflows/release.yml): stable tags like `1.2.3`, or prerelease tags with a hyphen after the patch number such as `1.0.0-rc` or `1.0.0-rc.1`. The workflow publishes self-contained single-file builds for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64` and uploads them to a GitHub Release.

## Running tests

```bash
dotnet test
```

## Code coverage (this repository)

The test project uses [Coverlet](https://github.com/coverlet-coverage/coverlet) (MSBuild integration). HTML reports are produced with [ReportGenerator](https://github.com/danielpalme/ReportGenerator) via a [local dotnet tool](../.config/dotnet-tools.json).

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

## Mutation testing ([Stryker.NET](https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing))

Mutation testing checks whether unit tests would catch small intentional bugs in production code. This repo scopes runs to **critical churn / coverage / CI mapping** surface area only (see `stryker-config.json` next to each production project — `GitChurnCalculator` and `GitChurnCalculator.Console` — which points `test-projects` at the matching test assembly), not every `.cs` file.

**Container** (recommended — the dev image installs `dotnet-stryker` globally):

```bash
make mutation-test
```

HTML reports are written under each **production** project as **`StrykerOutput/<timestamp>/reports/mutation-report.html`** (ignored via `**/StrykerOutput/` in [.gitignore](../.gitignore)). Stryker is started from those folders so the project under test is not resolved via reference-only (ref) assemblies from the test project. The log line `Your html report has been generated at:` shows the exact path for each run.

**Host** (uses the [local tool manifest](../.config/dotnet-tools.json) after `dotnet tool restore`):

```bash
dotnet tool restore
cd GitChurnCalculator && dotnet tool run dotnet-stryker
cd GitChurnCalculator.Console && dotnet tool run dotnet-stryker
```

Interpret **survived** mutants as prompts to add or strengthen tests; see the [Stryker.NET configuration docs](https://stryker-mutator.io/docs/stryker-net/configuration/) to adjust `mutate`, `report-file-name`, thresholds, reporters, or verbosity.

Stryker.NET 4.x does not support `artifact-folder` in `stryker-config.json`; output location follows the working directory and `report-file-name` defaults.
