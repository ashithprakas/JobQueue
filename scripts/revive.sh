#!/usr/bin/env bash
#
# Undoes kill-switch.sh. Worker/RetrySweepWorker MUST go back to min 1, not
# 0 — neither has ingress or any trigger to wake back up on its own once
# scaled to zero.
#
# Usage: ./scripts/revive.sh

set -euo pipefail

RESOURCE_GROUP="rg-jobqueue-dev"

echo "==> Restoring jobqueue-api (min 0 / max 10)"
az containerapp update --name "jobqueue-api" --resource-group "${RESOURCE_GROUP}" \
  --min-replicas 0 --max-replicas 10

echo "==> Restoring jobqueue-worker (min 1 / max 10)"
az containerapp update --name "jobqueue-worker" --resource-group "${RESOURCE_GROUP}" \
  --min-replicas 1 --max-replicas 10

echo "==> Restoring jobqueue-sweep-worker (min 1 / max 10)"
az containerapp update --name "jobqueue-sweep-worker" --resource-group "${RESOURCE_GROUP}" \
  --min-replicas 1 --max-replicas 10

echo ""
echo "==> Done. Give Worker/RetrySweepWorker a minute to spin up before testing."
