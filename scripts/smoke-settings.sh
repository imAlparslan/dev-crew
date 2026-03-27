#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "[smoke-settings] Running settings persistence smoke tests..."
dotnet test tests/DevCrew.Core.Tests/DevCrew.Core.Tests.csproj --filter "FullyQualifiedName~SettingsPersistenceSmokeTests"
echo "[smoke-settings] Completed successfully."
