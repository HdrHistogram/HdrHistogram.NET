#!/usr/bin/env bash
set -euo pipefail

# Start an interactive planning session for a milestone.
# The agent follows PLAN.prompt.md — Socratic elicitation, BRIEF.md,
# task files, GitHub Milestone and Issues. No code is written.
#
# Usage:
#   ./scripts/plan.sh
#   ./scripts/plan.sh "Build the code review plugin"

REQUEST="${1:-}"

if [ -n "$REQUEST" ]; then
  echo "Follow PLAN.prompt.md — ${REQUEST}" \
    | claude 2>&1
else
  echo "Follow PLAN.prompt.md" \
    | claude 2>&1
fi
