#!/usr/bin/env bash
set -euo pipefail

# Run all tasks for a single issue (one task file) using the execution loop.
# Each iteration invokes a stateless agent that completes one task and stops.
# The loop exits when no unchecked tasks remain in the task file.
# Stuck detection: if incomplete count is unchanged between iterations, exit with error.
#
# Usage:
#   ./scripts/execute-issue.sh plan/v1/tasks/01-scaffolding.tasks.md

TASK_FILE="${1:?Usage: $0 <task-file> [base-branch]}"
BASE_BRANCH="${2:-main}"

if [ ! -f "$TASK_FILE" ]; then
  echo "Error: Task file not found: $TASK_FILE" >&2
  exit 1
fi

echo "Starting issue: ${TASK_FILE}"

mkdir -p .logs
LOG_FILE=".logs/$(date -u +%Y-%m-%dT%H%M%SZ).log"

PREV_INCOMPLETE=-1

while :; do
  echo "Follow EXECUTE.prompt.md for ${TASK_FILE}. Base branch for PR: ${BASE_BRANCH}" \
    | claude --print --output-format stream-json --verbose --permission-mode dontAsk 2>&1 | tee -a "$LOG_FILE"

  # Check task file state
  INCOMPLETE=$(grep -c '^\- \[ \]' "$TASK_FILE" || true)

  # Completion: all tasks done (PR created in same iteration as last task)
  if [ "$INCOMPLETE" -eq 0 ]; then
    echo "---"
    echo "Issue complete: ${TASK_FILE}"
    echo "Log: ${LOG_FILE}"
    echo "==="
    break
  fi

  # Stuck detection: no tasks completed this iteration
  if [ "$INCOMPLETE" -eq "$PREV_INCOMPLETE" ]; then
    echo "Error: No progress detected. ${INCOMPLETE} tasks remain incomplete." >&2
    echo "Human intervention required for: ${TASK_FILE}" >&2
    echo "Log: ${LOG_FILE}" >&2
    exit 1
  fi

  PREV_INCOMPLETE=$INCOMPLETE
done
