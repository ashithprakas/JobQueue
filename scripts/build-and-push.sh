#!/usr/bin/env bash
#
# Builds and pushes all four JobQueue images to ACR for linux/amd64
# (Azure Container Apps rejects arm64; Apple Silicon builds arm64 by default).
#
# Usage: ./scripts/build-and-push.sh [tag]   (defaults to "latest")

set -euo pipefail

REGISTRY="jobqueueregisty-cvd8hgcmema9b5hj.azurecr.io"
TAG="${1:-latest}"

echo "==> Logging in to ${REGISTRY}"
az acr login --name jobqueueregisty

# Parallel arrays, not declare -A — macOS ships bash 3.2, no associative array support.
IMAGE_NAMES=(
  "jobqueue-api"
  "jobqueue-worker"
  "jobqueue-retry-sweep-worker"
  "jobqueue-migrator"
)
DOCKERFILES=(
  "JobQueue.API/Dockerfile"
  "JobQueue.Worker/Dockerfile"
  "JobQueue.RetrySweepWorker/Dockerfile"
  "JobQueue.Migrator/Dockerfile"
)

for i in "${!IMAGE_NAMES[@]}"; do
  IMAGE_NAME="${IMAGE_NAMES[$i]}"
  DOCKERFILE="${DOCKERFILES[$i]}"
  FULL_TAG="${REGISTRY}/${IMAGE_NAME}:${TAG}"

  echo ""
  echo "==> Building ${IMAGE_NAME}"
  docker build --platform linux/amd64 -f "${DOCKERFILE}" -t "${FULL_TAG}" .

  echo "==> Pushing ${FULL_TAG}"
  docker push "${FULL_TAG}"
done

echo ""
echo "==> Done. Verify all four repositories in the ACR portal."
