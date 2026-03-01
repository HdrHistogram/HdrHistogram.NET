#!/bin/bash
# fleet.sh — Spin up N agents, each assigned a different issue
set -euo pipefail

FLEET_SIZE="${1:-3}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/.env"

# Build the image
docker build -t hdrhistogram-agent -f "$SCRIPT_DIR/Dockerfile" "$SCRIPT_DIR/"

echo "Launching $FLEET_SIZE agents..."

for i in $(seq 1 "$FLEET_SIZE"); do
    AGENT_NAME="hdrhistogram-agent-$i"
    echo "Starting $AGENT_NAME..."

    docker run -d --rm \
        --name "$AGENT_NAME" \
        --cap-add NET_ADMIN \
        --cap-add NET_RAW \
        --memory=4g \
        --cpus=2 \
        --env-file "$SCRIPT_DIR/.env" \
        -e MAX_ITERATIONS=15 \
        -v nuget-cache:/home/agent/.nuget/packages \
        hdrhistogram-agent
done

echo "Fleet launched. Monitor with: docker ps --filter name=hdrhistogram-agent"
echo "Logs: docker logs -f hdrhistogram-agent-1"
