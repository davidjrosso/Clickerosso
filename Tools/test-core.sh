#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/toolchains/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/toolchains/nuget}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
sdk="${REACTOR_DOTNET:-/workspace/toolchains/dotnet/dotnet}"
if [[ ! -x "$sdk" ]]; then sdk="$(command -v dotnet)"; fi
"$sdk" restore Tests/CoreTests.csproj --locked-mode
"$sdk" test Tests/CoreTests.csproj --no-restore --configuration Release \
  --logger "trx;LogFileName=core.trx" --results-directory TestResults/Core
