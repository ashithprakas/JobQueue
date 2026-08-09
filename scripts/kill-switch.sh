#!/usr/bin/env bash
#
# Scales API, Worker, and RetrySweepWorker to 0 replicas to stop compute
# billing. Doesn't touch SQL (auto-pauses) or Redis (no pause option) or
# ACR (no running/stopped concept).
#
# Usage: ./scripts/kill-switch.sh

set -euo pipefail

RESOURCE_GROUP="rg-jobqueue-dev"
APPS=("jobqueue-api" "jobqueue-worker" "jobqueue-sweep-worker")

for APP in "${APPS[@]}"; do
  echo "==> Scaling ${APP} to 0 replicas"
  # max-replicas can't be 0 (Azure rejects it) and doesn't need to change —
  # billing is based on replicas actually running, not the ceiling.
  az containerapp update \
    --name "${APP}" \
    --resource-group "${RESOURCE_GROUP}" \
    --min-replicas 0
done

echo ""
echo "==> Done. Run ./scripts/revive.sh to bring them back."
