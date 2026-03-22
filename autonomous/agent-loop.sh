#!/bin/bash
set -euo pipefail
cd /workspace/repo

CLAUDE_TIMEOUT="${CLAUDE_TIMEOUT:-600}"
UPSTREAM_REPO="${UPSTREAM_REPO}"
UPSTREAM_BASE_BRANCH="${UPSTREAM_BASE_BRANCH:-main}"
PROMPT_DIR="${PROMPT_DIR:-/usr/local/share/agent-prompts}"

push_or_defer_workflows() {
    local branch="$1"
    local push_stderr

    # Attempt the push, capturing stderr
    if push_stderr=$(git push -u origin "$branch" 2>&1); then
        return 0
    fi

    # Check if the failure is workflow-related
    if ! echo "$push_stderr" | grep -qi "workflow"; then
        echo "ERROR: git push failed: $push_stderr" >&2
        return 1
    fi

    echo "WARNING: push rejected due to workflow file permissions, deferring workflow changes" >&2

    # Save the workflow diff against upstream base
    git diff "upstream/$UPSTREAM_BASE_BRANCH" -- .github/workflows/ > /tmp/workflow_changes.diff

    if [ ! -s /tmp/workflow_changes.diff ]; then
        echo "ERROR: workflow rejection detected but no workflow diff found" >&2
        return 1
    fi

    # Restore .github/workflows/ to upstream state
    rm -rf .github/workflows
    git checkout "upstream/$UPSTREAM_BASE_BRANCH" -- .github/workflows/ 2>/dev/null || true
    git add -A
    git commit --amend --no-edit

    # Retry the push
    if ! push_stderr=$(git push -u origin "$branch" 2>&1); then
        echo "ERROR: git push failed after stripping workflow changes: $push_stderr" >&2
        return 1
    fi

    echo "WARNING: workflow changes deferred to /tmp/workflow_changes.diff" >&2
    return 0
}

sync_state() {
    local msg="${1:-agent: update plan state}"
    git add -A
    if ! git diff --cached --quiet; then
        git commit -m "$msg"
        push_or_defer_workflows "$(git branch --show-current)"
    fi
}

CLAUDE_RC=0

run_claude() {
    local prompt="$1"
    timeout "$CLAUDE_TIMEOUT" claude --dangerously-skip-permissions --print \
        --output-format stream-json --verbose "$prompt" || CLAUDE_RC=$?
}

load_prompt() {
    local name="$1"
    local file="${PROMPT_DIR}/${name}.md"
    if [ ! -f "$file" ]; then
        echo "ERROR: Prompt file not found: $file" >&2
        exit 1
    fi
    cat "$file"
}

get_issue_num() {
    git branch --show-current | grep -oP 'agent/\K\d+'
}

determine_state() {
    if [ -f ./plan/done/brief.md ]; then
        echo "create-pr"
    elif [ -f ./plan/ready/task.md ]; then
        echo "execute-tasks"
    elif [ -f ./plan/ready/brief.md ]; then
        echo "create-tasks"
    elif [ -f ./plan/planning/brief-review.md ]; then
        echo "apply-review"
    elif [ -f ./plan/planning/brief.md ]; then
        echo "review-brief"
    else
        echo "pick-issue"
    fi
}

STATE=$(determine_state)
echo "State: $STATE"

case "$STATE" in

    create-pr)
        BRANCH=$(git branch --show-current)
        ISSUE_NUM=$(get_issue_num)

        # Back up plan state for recovery on failure
        cp -r ./plan /tmp/plan-backup 2>/dev/null || true

        # Remove plan/ so the PR commit is clean
        # (upstream uses squash merge, so plan artifacts never appear in main history)
        rm -rf ./plan
        git add -A
        git diff --cached --quiet || git commit -m "feat(#${ISSUE_NUM}): complete implementation"

        if ! push_or_defer_workflows "$BRANCH"; then
            echo "ERROR: git push failed, restoring plan state" >&2
            cp -r /tmp/plan-backup ./plan 2>/dev/null || true
            exit 1
        fi

        PR_TITLE="feat(#${ISSUE_NUM}): $(gh issue view "$ISSUE_NUM" \
            --repo "$UPSTREAM_REPO" --json title --jq .title)"

        # Build PR body from plan artifacts
        PR_BODY=""
        if [ -f /tmp/plan-backup/done/brief.md ]; then
            PR_BODY=$(cat /tmp/plan-backup/done/brief.md)
        fi
        if [ -f /tmp/plan-backup/done/task.md ]; then
            PR_BODY="${PR_BODY}

<details>
<summary>Task breakdown</summary>

$(cat /tmp/plan-backup/done/task.md)

</details>"
        fi
        if [ -f /tmp/plan-backup/benchmarks/comparison.md ]; then
            PR_BODY="${PR_BODY}

<details>
<summary>Benchmark comparison</summary>

$(cat /tmp/plan-backup/benchmarks/comparison.md)

</details>"
        fi
        PR_BODY="${PR_BODY}

Closes #${ISSUE_NUM}"

        if ! PR_URL=$(GH_TOKEN="$GH_TOKEN_UPSTREAM" gh pr create \
            --title "$PR_TITLE" \
            --body "$PR_BODY" \
            --repo "$UPSTREAM_REPO" \
            --head "${GIT_USER_NAME}:${BRANCH}" \
            --base "$UPSTREAM_BASE_BRANCH"); then
            echo "ERROR: PR creation failed, restoring plan state" >&2
            cp -r /tmp/plan-backup ./plan 2>/dev/null || true
            sync_state "plan(#${ISSUE_NUM}): restore after failed PR"
            exit 1
        fi

        if [ -n "$ISSUE_NUM" ]; then
            GH_TOKEN="$GH_TOKEN_UPSTREAM" gh issue comment "$ISSUE_NUM" \
                --repo "$UPSTREAM_REPO" --body "PR created: $PR_URL"
        fi

        # Post deferred workflow changes as a PR comment
        if [ -s /tmp/workflow_changes.diff ]; then
            DIFF_CONTENT=$(cat /tmp/workflow_changes.diff)
            WORKFLOW_COMMENT=$(cat <<EOF
:warning: **Workflow file changes require manual application**

The agent's PAT lacks the \`workflow\` scope needed to push changes under \`.github/workflows/\`.
Please apply the following diff manually:

\`\`\`diff
${DIFF_CONTENT}
\`\`\`
EOF
)
            PR_NUMBER=$(echo "$PR_URL" | grep -oP '\d+$')
            GH_TOKEN="$GH_TOKEN_UPSTREAM" gh pr comment "$PR_NUMBER" \
                --repo "$UPSTREAM_REPO" --body "$WORKFLOW_COMMENT"
            rm -f /tmp/workflow_changes.diff
            echo "WARNING: workflow changes posted as PR comment"
        fi

        # Clean up backup after successful PR creation
        rm -rf /tmp/plan-backup

        echo "PR created: $PR_URL"
        ;;

    execute-tasks)
        ISSUE_NUM=$(get_issue_num)
        run_claude "$(load_prompt execute-tasks)"
        sync_state "feat(#${ISSUE_NUM}): implement tasks"
        ;;

    create-tasks)
        ISSUE_NUM=$(get_issue_num)
        run_claude "$(load_prompt create-tasks)"
        sync_state "plan(#${ISSUE_NUM}): create task breakdown"
        ;;

    apply-review)
        ISSUE_NUM=$(get_issue_num)
        run_claude "$(load_prompt apply-review)"
        sync_state "plan(#${ISSUE_NUM}): apply brief review feedback"
        ;;

    review-brief)
        ISSUE_NUM=$(get_issue_num)
        run_claude "$(load_prompt review-brief)"
        sync_state "plan(#${ISSUE_NUM}): review brief"
        ;;

    pick-issue)
        if [ -n "${ISSUE_NUMBER:-}" ]; then
            # Issue pre-assigned by fleet.sh
            ISSUE_NUM="$ISSUE_NUMBER"
            ISSUE_TITLE=$(GH_TOKEN="$GH_TOKEN_UPSTREAM" gh issue view "$ISSUE_NUM" \
                --repo "$UPSTREAM_REPO" --json title --jq .title)
        else
            # Self-select: assigned first, then labelled 'agent'
            ISSUE_JSON=$(gh issue list --assignee @me --state open \
                --repo "$UPSTREAM_REPO" \
                --json number,title --limit 1 \
                --search "sort:created-asc" 2>/dev/null || echo "[]")

            if [ "$ISSUE_JSON" = "[]" ] || [ "$ISSUE_JSON" = "" ]; then
                ISSUE_JSON=$(gh issue list --label agent --state open \
                    --repo "$UPSTREAM_REPO" \
                    --json number,title --limit 1 \
                    --search "sort:created-asc" 2>/dev/null || echo "[]")
            fi

            ISSUE_NUM=$(echo "$ISSUE_JSON" | jq -r '.[0].number // empty')
            if [ -z "$ISSUE_NUM" ]; then
                echo "No work available."
                exit 0
            fi

            ISSUE_TITLE=$(echo "$ISSUE_JSON" | jq -r '.[0].title')
        fi
        BRANCH_SLUG=$(echo "$ISSUE_TITLE" | tr '[:upper:]' '[:lower:]' | \
            sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | head -c 40)

        # Check for existing branch (resume interrupted work)
        EXISTING_BRANCH=$(git ls-remote --heads origin "agent/${ISSUE_NUM}-*" \
            | head -1 | awk '{print $2}' | sed 's|refs/heads/||')

        if [ -n "$EXISTING_BRANCH" ]; then
            echo "Resuming branch: $EXISTING_BRANCH"
            git checkout -b "$EXISTING_BRANCH" "origin/$EXISTING_BRANCH"

            # If plan state doesn't exist on the branch, re-initialise
            if [ ! -d ./plan ]; then
                echo "No plan state found on branch, starting fresh"
                mkdir -p ./plan/planning ./plan/ready ./plan/done ./plan/benchmarks

                ISSUE_BODY=$(gh issue view "$ISSUE_NUM" --repo "$UPSTREAM_REPO" \
                    --json body,title --jq '"# Issue #'"$ISSUE_NUM"': " + .title + "\n\n" + .body')

                PROMPT=$(load_prompt pick-issue)
                PROMPT="${PROMPT//\{\{ISSUE_BODY\}\}/$ISSUE_BODY}"

                run_claude "$PROMPT"
                sync_state "plan(#${ISSUE_NUM}): initial brief from issue"
            fi
        else
            echo "Starting fresh: agent/${ISSUE_NUM}-${BRANCH_SLUG}"
            git fetch upstream
            git checkout -b "agent/${ISSUE_NUM}-${BRANCH_SLUG}" "upstream/$UPSTREAM_BASE_BRANCH"
            mkdir -p ./plan/planning ./plan/ready ./plan/done ./plan/benchmarks

            ISSUE_BODY=$(gh issue view "$ISSUE_NUM" --repo "$UPSTREAM_REPO" \
                --json body,title --jq '"# Issue #'"$ISSUE_NUM"': " + .title + "\n\n" + .body')

            # Load template and substitute issue body
            PROMPT=$(load_prompt pick-issue)
            PROMPT="${PROMPT//\{\{ISSUE_BODY\}\}/$ISSUE_BODY}"

            run_claude "$PROMPT"
            sync_state "plan(#${ISSUE_NUM}): initial brief from issue"
        fi
        ;;
esac

exit $CLAUDE_RC
