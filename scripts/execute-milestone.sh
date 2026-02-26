#!/usr/bin/env bash
set -euo pipefail

# Run all issues for a milestone in sequence.
# Iterates over task files in numbered order (01-, 02-, etc.) and runs
# the Ralph Wiggum loop for each. The last task file (*-close.tasks.md)
# handles milestone completion — updating specs, cleanup, and closure.
#
# Assumes task files are numbered in dependency order. For parallel
# issues, run their execute-issue.sh loops concurrently instead.
#
# Usage:
#   ./scripts/execute-milestone.sh                 # auto-detects if only one milestone
#   ./scripts/execute-milestone.sh plan/v1/tasks

if [ -n "${1:-}" ]; then
  PLAN_DIR="$1"
else
  # Auto-detect: if exactly one milestone exists under plan/, use it
  MILESTONES=(plan/*/tasks)
  if [ ${#MILESTONES[@]} -eq 1 ] && [ -d "${MILESTONES[0]}" ]; then
    PLAN_DIR="${MILESTONES[0]}"
    echo "Auto-detected milestone: ${PLAN_DIR}"
  elif [ ${#MILESTONES[@]} -eq 0 ] || [ ! -d "${MILESTONES[0]}" ]; then
    echo "Error: No milestones found under plan/" >&2
    exit 1
  else
    echo "Error: Multiple milestones found. Specify one:" >&2
    printf "  %s\n" "${MILESTONES[@]}" >&2
    exit 1
  fi
fi

if [ ! -d "$PLAN_DIR" ]; then
  echo "Error: Tasks directory not found: $PLAN_DIR" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

PREV_BRANCH="main"

for TASK_FILE in "${PLAN_DIR}"/*.tasks.md; do
  # Derive branch name from filename: 03-sre-domain.tasks.md → feat/03-sre-domain
  PREFIX=$(basename "$TASK_FILE" .tasks.md)
  BRANCH="feat/${PREFIX}"

  # Skip completed issues
  INCOMPLETE=$(grep -c '^\- \[ \]' "$TASK_FILE" || true)
  if [ "$INCOMPLETE" -eq 0 ]; then
    echo -e "\033[0;33m=== Skipping (complete): ${TASK_FILE} ===\033[0m"
    # Track branch for next issue's base (if branch exists, content is there;
    # if not, content was merged to main/upstream, PREV_BRANCH stays as-is)
    git rev-parse --verify "$BRANCH" >/dev/null 2>&1 && PREV_BRANCH="$BRANCH"
    continue
  fi

  # Create or checkout branch
  if git rev-parse --verify "$BRANCH" >/dev/null 2>&1; then
    git checkout "$BRANCH"
  else
    echo "Creating branch ${BRANCH} from ${PREV_BRANCH}..."
    git checkout -b "$BRANCH" "$PREV_BRANCH"
  fi

  echo -e "\033[0;32m=== Starting issue: ${TASK_FILE} ===\033[0m"
  "${SCRIPT_DIR}/execute-issue.sh" "$TASK_FILE" "$PREV_BRANCH"
  echo -e "\033[0;32m=== Completed issue: ${TASK_FILE} ===\033[0m"
  echo

  PREV_BRANCH="$BRANCH"
done

# --- Milestone cleanup: delete plan directory and close milestone ---
# The close-out task file lives inside the plan directory, so the agent
# cannot delete it during execution. The script handles this after all
# issues (including close-out) are complete.

MILESTONE_DIR="$(dirname "$PLAN_DIR")"

# Ensure we're on the close-out branch, not main. When all issues were
# skipped (already complete), the loop never checks out a branch.
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" = "main" ] && [ "$PREV_BRANCH" != "main" ]; then
  echo "Checking out ${PREV_BRANCH} for cleanup..."
  git checkout "$PREV_BRANCH"
fi

if [ -d "$MILESTONE_DIR" ]; then
  echo -e "\033[0;32m=== Milestone cleanup: deleting plan directory ===\033[0m"
  git rm -rf "$MILESTONE_DIR"
  git commit -m "$(cat <<'EOF'
Delete plan directory — plans are transient, specs are permanent

Plan remains auditable via git history, PRs, Issues, and Milestones.
Handled by execute-milestone.sh after all close-out tasks completed.
EOF
  )"
  git push
  echo "Plan directory deleted and pushed."
fi

# Attempt to close the GitHub Milestone (best-effort).
# Derives milestone name from the plan directory (e.g. plan/code-review-v1 → "code-review-v1").
MILESTONE_SLUG="$(basename "$MILESTONE_DIR")"
MILESTONE_NUMBER=$(gh api repos/:owner/:repo/milestones --jq \
  ".[] | select(.title | ascii_downcase | gsub(\" \"; \"-\") | test(\"${MILESTONE_SLUG}\")) | .number" 2>/dev/null || true)

if [ -n "$MILESTONE_NUMBER" ]; then
  echo -e "\033[0;32m=== Closing GitHub Milestone #${MILESTONE_NUMBER} ===\033[0m"
  gh api -X PATCH "repos/:owner/:repo/milestones/${MILESTONE_NUMBER}" -f state=closed || \
    echo "Warning: Could not close milestone. Close it manually." >&2
else
  echo "Warning: Could not find GitHub Milestone matching '${MILESTONE_SLUG}'." >&2
  echo "Close the milestone manually." >&2
fi

echo "All issues complete."
