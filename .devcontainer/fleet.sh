#!/bin/bash
# fleet.sh — Fetch available issues and spin up one agent per issue
set -euo pipefail

FLEET_SIZE="${1:-3}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/.env"

# Build the image
docker build -t hdrhistogram-agent -f "$SCRIPT_DIR/Dockerfile" "$SCRIPT_DIR/"

# Fetch assigned issues as tab-separated "number\ttitle" lines
ASSIGNED=$(GH_TOKEN="$GH_TOKEN_UPSTREAM" gh issue list --assignee "$GIT_USER_NAME" --state open \
    --repo "$UPSTREAM_REPO" --json number,title --limit "$FLEET_SIZE" \
    --search "sort:created-asc" \
    --jq '.[] | [.number, .title] | @tsv' 2>/dev/null || true)

declare -a ISSUE_NUMS=()
declare -a ISSUE_TITLES=()
ASSIGNED_NUMS=""

while IFS=$'\t' read -r num title; do
    [ -z "$num" ] && continue
    ISSUE_NUMS+=("$num")
    ISSUE_TITLES+=("$title")
    ASSIGNED_NUMS+=" $num "
done <<< "$ASSIGNED"

ISSUE_COUNT=${#ISSUE_NUMS[@]}

if [ "$ISSUE_COUNT" -lt "$FLEET_SIZE" ]; then
    REMAINING=$((FLEET_SIZE - ISSUE_COUNT))
    EXTRA=$(GH_TOKEN="$GH_TOKEN_UPSTREAM" gh issue list --label agent --state open \
        --repo "$UPSTREAM_REPO" --json number,title --limit "$REMAINING" \
        --search "sort:created-asc" \
        --jq '.[] | [.number, .title] | @tsv' 2>/dev/null || true)

    while IFS=$'\t' read -r num title; do
        [ -z "$num" ] && continue
        # Skip already-assigned issues
        if [[ "$ASSIGNED_NUMS" == *" $num "* ]]; then
            continue
        fi
        ISSUE_NUMS+=("$num")
        ISSUE_TITLES+=("$title")
    done <<< "$EXTRA"
fi

ISSUE_COUNT=${#ISSUE_NUMS[@]}
if [ "$ISSUE_COUNT" -eq 0 ]; then
    echo "No issues available."
    exit 0
fi

echo "Found $ISSUE_COUNT issue(s). Launching agents..."

for i in $(seq 0 $((ISSUE_COUNT - 1))); do
    ISSUE_NUM="${ISSUE_NUMS[$i]}"
    ISSUE_TITLE="${ISSUE_TITLES[$i]}"
    AGENT_NAME="hdrhistogram-agent-${ISSUE_NUM}"

    echo "Starting $AGENT_NAME for issue #${ISSUE_NUM}: ${ISSUE_TITLE}"

    docker run -d --rm \
        --name "$AGENT_NAME" \
        --cap-add NET_ADMIN \
        --cap-add NET_RAW \
        --memory=4g \
        --cpus=2 \
        --env-file "$SCRIPT_DIR/.env" \
        -e ISSUE_NUMBER="$ISSUE_NUM" \
        -e MAX_ITERATIONS=50 \
        -v nuget-cache:/home/agent/.nuget/packages \
        hdrhistogram-agent
done

echo ""
echo "Fleet launched. Monitor with: docker ps --filter name=hdrhistogram-agent"
echo "Logs: docker logs -f hdrhistogram-agent-<issue-number>"
