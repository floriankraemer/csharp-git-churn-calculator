#Requires -Version 5.1
<#
.SYNOPSIS
  Build and test helpers (make-style targets).

.DESCRIPTION
  Run from the repository root, for example:
    .\make.ps1 build-debug
    .\make.ps1 build-release
    .\make.ps1 test
    .\make.ps1 clean
    .\make.ps1 publish-single
    .\make.ps1 publish-single linux-x64

.PARAMETER Target
  The target to run. Use 'help' or omit to list targets.

.PARAMETER RuntimeIdentifier
  Optional RID for publish-single (e.g. win-x64, linux-x64, osx-arm64). Defaults to this machine's RID.
#>
param(
    [Parameter(Position = 0)]
    [string] $Target = "help",

    [Parameter(Position = 1)]
    [string] $RuntimeIdentifier = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Sln = Join-Path $PSScriptRoot "GitChurnCalculator.slnx"
$ConsoleProj = Join-Path $PSScriptRoot "GitChurnCalculator.Console\GitChurnCalculator.Console.csproj"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)] [string[]] $Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Show-Help {
    @"
Git Churn Calculator — make.ps1

Usage:
  .\make.ps1 <target> [runtime-identifier]

Targets:
  restore          dotnet restore
  build-debug      dotnet build (Debug)
  build-release    dotnet build (Release)
  build            same as build-debug
  test             dotnet test (Release)
  test-debug       dotnet test (Debug)
  test-release     dotnet test (Release)
  clean            remove bin/obj folders under the solution
  ci               restore, build-release, test-release
  publish-single   self-contained single-file publish -> artifacts\publish-<RID>
  help             show this message
"@
}

switch ($Target.ToLowerInvariant()) {
    "help" { Show-Help }
    "restore" {
        Invoke-DotNet @("restore", $Sln)
    }
    "build-debug" {
        Invoke-DotNet @("build", $Sln, "-c", "Debug")
    }
    "build" {
        Invoke-DotNet @("build", $Sln, "-c", "Debug")
    }
    "build-release" {
        Invoke-DotNet @("build", $Sln, "-c", "Release")
    }
    "test" {
        Invoke-DotNet @("test", $Sln, "-c", "Release", "--verbosity", "normal")
    }
    "test-debug" {
        Invoke-DotNet @("test", $Sln, "-c", "Debug", "--verbosity", "normal")
    }
    "test-release" {
        Invoke-DotNet @("test", $Sln, "-c", "Release", "--verbosity", "normal")
    }
    "clean" {
        Get-ChildItem -Path $PSScriptRoot -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $PSScriptRoot -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Removed bin/obj directories."
    }
    "ci" {
        Invoke-DotNet @("restore", $Sln)
        Invoke-DotNet @("build", $Sln, "-c", "Release", "--no-restore")
        Invoke-DotNet @("test", $Sln, "-c", "Release", "--verbosity", "normal", "--no-build")
    }
    "publish-single" {
        $rid = $RuntimeIdentifier
        if ([string]::IsNullOrWhiteSpace($rid)) {
            $rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
        }
        $outDir = Join-Path $PSScriptRoot (Join-Path "artifacts" "publish-$rid")
        Write-Host "Publishing self-contained single-file for RID: $rid -> $outDir"
        Invoke-DotNet @(
            "publish", $ConsoleProj,
            "-c", "Release",
            "-r", $rid,
            "--self-contained", "true",
            "-o", $outDir
        )
        Write-Host "Done. Output folder: $outDir"
    }
    default {
        Write-Host "Unknown target: $Target" -ForegroundColor Red
        Write-Host ""
        Show-Help
        exit 1
    }
}
