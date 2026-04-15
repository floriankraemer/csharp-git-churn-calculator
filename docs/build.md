# Build, test, and publish

Commands below assume the **repository root** as the current directory. For what the tool computes, see [How churn is calculated](churn-calculation.md). For usage, see the [README](../README.md).

## Solution structure

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
