#!/bin/bash
set -euo pipefail

# ── Firewall ──
sudo /usr/local/bin/init-firewall.sh

# ── Git identity (the agent's digital twin) ──
git config --global user.name  "${GIT_USER_NAME}"
git config --global user.email "${GIT_USER_EMAIL}"

# ── Clone from the agent's fork ──
echo "Cloning fork: https://${FORK_URL}..."
git clone "https://x-access-token:${GH_TOKEN}@${FORK_URL}" /workspace/repo
cd /workspace/repo

# ── Set upstream to the parent repo ──
UPSTREAM_BASE_BRANCH="${UPSTREAM_BASE_BRANCH:-main}"
git remote add upstream "https://github.com/${UPSTREAM_REPO}"
git fetch upstream
git branch --set-upstream-to="upstream/$UPSTREAM_BASE_BRANCH" "$UPSTREAM_BASE_BRANCH"


# ── Restore + build to warm the cache ──
echo "Restoring NuGet packages..."
dotnet restore
echo "Building..."
dotnet build --no-restore

# ── Run the agent ──
MAX_ITERATIONS="${MAX_ITERATIONS:-30}"
COOLDOWN="${COOLDOWN_SECONDS:-30}"
export CLAUDE_TIMEOUT="${TIMEOUT_SECONDS:-1800}"

for i in $(seq 1 "$MAX_ITERATIONS"); do
    echo ""
    echo "=== Iteration $i/$MAX_ITERATIONS ==="
    find ./plan -name "*.md" 2>/dev/null | sort || echo "  (no plan state)"

    EXIT_CODE=0
    bash /usr/local/bin/agent-loop.sh || EXIT_CODE=$?

    if [ "$EXIT_CODE" -ne 0 ]; then
        echo "Iteration $i exited with code $EXIT_CODE, continuing to next state..."
    fi

    # Done?
    if [ ! -d "./plan" ]; then
        echo "Work complete."
        break
    fi

    [ "$i" -lt "$MAX_ITERATIONS" ] && sleep "$COOLDOWN"
done

echo "Agent finished after $i iterations."
