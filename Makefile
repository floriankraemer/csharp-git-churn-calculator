# Git Churn Calculator — same targets as make.ps1 (restore, build, test, ci, coverage, …).
#
# Requires: .NET SDK 9.0.200+ on PATH (for GitChurnCalculator.slnx); projects target net8.0.
# Targets clean, coverage, and mkdir use POSIX shell
# utilities — use WSL, Git Bash, macOS/Linux, or: docker compose run --rm dev make <target>
#
# Container (bind-mounts this repo at /src):
#   docker compose run --rm dev make ci
#   podman compose run --rm dev make ci
#   podman-compose -f podman-compose.yaml run --rm dev make ci
#
# publish-single: override RID, e.g. make publish-single RID=linux-x64
# Default RID is taken from `dotnet --info`, else linux-x64.

SLN := GitChurnCalculator.slnx
CONSOLE := GitChurnCalculator.Console/GitChurnCalculator.Console.csproj
COV_DIR := $(CURDIR)/artifacts/coverage

_DOTNET_RID := $(shell dotnet --info 2>/dev/null | sed -n 's/^[[:space:]]*RID:[[:space:]]*//p' | head -1)
PUBLISH_RID := $(if $(strip $(RID)),$(strip $(RID)),$(if $(strip $(_DOTNET_RID)),$(_DOTNET_RID),linux-x64))

.DEFAULT_GOAL := help

.PHONY: help restore build-debug build build-release test test-debug test-release clean ci publish-single coverage

help:
	@echo "Git Churn Calculator — Makefile (same targets as make.ps1)"
	@echo ""
	@echo "Usage:"
	@echo "  make <target>"
	@echo "  make publish-single RID=<runtime-identifier>"
	@echo ""
	@echo "Targets:"
	@echo "  restore          dotnet restore"
	@echo "  build-debug      dotnet build (Debug)"
	@echo "  build-release    dotnet build (Release)"
	@echo "  build            same as build-debug"
	@echo "  test             dotnet test (Release)"
	@echo "  test-debug       dotnet test (Debug)"
	@echo "  test-release     dotnet test (Release)"
	@echo "  clean            remove bin/obj folders under the solution"
	@echo "  ci               restore, build-release, test-release"
	@echo "  publish-single   self-contained single-file publish -> artifacts/publish-<RID>"
	@echo "  coverage         run tests with Coverlet + HTML report -> artifacts/coverage"
	@echo "  help             show this message"

restore:
	dotnet restore $(SLN)

build-debug:
	dotnet build $(SLN) -c Debug

build: build-debug

build-release:
	dotnet build $(SLN) -c Release

test:
	dotnet test $(SLN) -c Release --verbosity normal

test-debug:
	dotnet test $(SLN) -c Debug --verbosity normal

test-release:
	dotnet test $(SLN) -c Release --verbosity normal

clean:
	@echo Removing bin/obj directories...
	@find . -type d -name bin -prune -exec rm -rf {} +
	@find . -type d -name obj -prune -exec rm -rf {} +

ci:
	dotnet restore $(SLN)
	dotnet build $(SLN) -c Release --no-restore
	dotnet test $(SLN) -c Release --verbosity normal --no-build

publish-single:
	@echo Publishing self-contained single-file for RID: $(PUBLISH_RID) -> artifacts/publish-$(PUBLISH_RID)
	dotnet publish $(CONSOLE) -c Release -r $(PUBLISH_RID) --self-contained true -o artifacts/publish-$(PUBLISH_RID)
	@echo Done. Output folder: artifacts/publish-$(PUBLISH_RID)

coverage:
	mkdir -p $(COV_DIR)
	@echo "Restoring dotnet tools (ReportGenerator)..."
	dotnet tool restore
	@echo "Running tests with code coverage (Coverlet)..."
	dotnet test $(SLN) -c Release --verbosity minimal \
		/p:CollectCoverage=true \
		/p:CoverletOutput=$(COV_DIR)/coverage \
		/p:CoverletOutputFormat=cobertura
	test -f $(COV_DIR)/coverage.cobertura.xml
	rm -rf $(COV_DIR)/html
	@echo "Generating HTML report (ReportGenerator)..."
	dotnet tool run reportgenerator -- \
		-reports:artifacts/coverage/coverage.cobertura.xml \
		-targetdir:artifacts/coverage/html \
		-reporttypes:Html
	@echo ""
	@echo "Coverage complete."
	@echo "  Cobertura: artifacts/coverage/coverage.cobertura.xml"
	@echo "  HTML:      artifacts/coverage/html/index.html"
