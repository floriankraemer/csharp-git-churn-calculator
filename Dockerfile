# Dev image: run builds/tests via Makefile with the repo bind-mounted at /src.
# Canonical image definition (compose uses this path). Containerfile duplicates this for `podman build .`
#
# Docker: docker compose -f compose.yaml build && docker compose run --rm dev make ci
# Podman: podman compose -f compose.yaml build && podman compose run --rm dev make ci
#         podman-compose -f podman-compose.yaml build && podman-compose -f podman-compose.yaml run --rm dev make ci
#
# SDK 9.x: GitChurnCalculator.slnx requires .NET SDK 9.0.200+ (MSBuild slnx support).
# Projects still target net8.0.
FROM mcr.microsoft.com/dotnet/sdk:9.0

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends git make curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Projects target net8.0; SDK 9 is required for .slnx but the 8.0 shared runtime is still needed to run tests and the console app.
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 8.0 --runtime dotnet --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

WORKDIR /src

CMD ["make", "help"]
