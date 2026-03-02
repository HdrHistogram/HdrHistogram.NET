#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(realpath "$SCRIPT_DIR/..")"

# Load env
if [ -f "$REPO_ROOT/autonomous/.env" ]; then
    set -a; source "$REPO_ROOT/autonomous/.env"; set +a
fi

# Build
docker build -t hdrhistogram-agent -f "$REPO_ROOT/autonomous/Dockerfile" "$REPO_ROOT/autonomous/"
echo "Built Docker image: hdrhistogram-agent"
echo "Starting container with .env file and mounted nuget cache volume. Named 'hdrhistogram-agent-0'"

# Run
docker run --rm \
    --name hdrhistogram-agent-0 \
    --cap-add NET_ADMIN \
    --cap-add NET_RAW \
    --memory=4g \
    --cpus=2 \
    --env-file "$REPO_ROOT/autonomous/.env" \
    -v nuget-cache:/home/agent/.nuget/packages \
    hdrhistogram-agent
