#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Load env
if [ -f "$SCRIPT_DIR/.env" ]; then
    set -a; source "$SCRIPT_DIR/.env"; set +a
fi

# Build
docker build -t hdrhistogram-agent -f "$SCRIPT_DIR/Dockerfile" "$SCRIPT_DIR/"
echo "Built Docker image: hdrhistogram-agent"
echo "Starting container with .env file and mounted nuget cache volume. Named 'hdrhistogram-agent-0'"

# Run
docker run --rm \
    --name hdrhistogram-agent-0 \
    --cap-add NET_ADMIN \
    --cap-add NET_RAW \
    --memory=4g \
    --cpus=2 \
    --env-file "$SCRIPT_DIR/.env" \
    -v nuget-cache:/home/agent/.nuget/packages \
    hdrhistogram-agent