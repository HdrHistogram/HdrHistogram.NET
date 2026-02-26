# GitHub — CLI Reference

> How to manage GitHub resources via the `gh` CLI.
> The `gh` CLI does not have a dedicated `milestone` subcommand; milestone operations use `gh api` against the GitHub REST API.

## Milestones

### Key concepts

- The REST API identifies milestones by **number** (an integer, assigned sequentially, never reused after deletion).
- The `gh issue` and `gh pr` commands identify milestones by **name** (a string).
- These are different identifiers — do not confuse them.

### List milestones

```bash
# Open milestones (default)
gh api repos/{owner}/{repo}/milestones

# Closed milestones
gh api 'repos/{owner}/{repo}/milestones?state=closed'

# All milestones regardless of state
gh api 'repos/{owner}/{repo}/milestones?state=all'

# Filtered output (number, title, state, description)
gh api repos/{owner}/{repo}/milestones --jq '.[] | {number, title, state, description}'
```

### Create a milestone

```bash
gh api repos/{owner}/{repo}/milestones \
  -X POST \
  -f title="Code-Review Plugin v1" \
  -f description="First release of the code review plugin" \
  -f state="open"
```

Optional: add `-f due_on="2026-03-31T00:00:00Z"` for a due date (ISO 8601 format).

Returns the full milestone JSON including the `number` field needed for subsequent operations.

### Get a single milestone

```bash
gh api repos/{owner}/{repo}/milestones/{number}

# Filtered output
gh api repos/{owner}/{repo}/milestones/{number} --jq '{number, title, state, open_issues, closed_issues}'
```

### Update a milestone

```bash
# Close a milestone
gh api repos/{owner}/{repo}/milestones/{number} -X PATCH -f state="closed"

# Reopen a milestone
gh api repos/{owner}/{repo}/milestones/{number} -X PATCH -f state="open"

# Update title and description
gh api repos/{owner}/{repo}/milestones/{number} -X PATCH \
  -f title="New Title" \
  -f description="Updated description"

# Set a due date
gh api repos/{owner}/{repo}/milestones/{number} -X PATCH \
  -f due_on="2026-06-30T00:00:00Z"
```

### Delete a milestone

```bash
gh api repos/{owner}/{repo}/milestones/{number} -X DELETE
```

Returns no output (HTTP 204) on success.

### Assign an issue to a milestone

There is no separate "add issue to milestone" endpoint.
Set the milestone when creating or editing an issue, using the milestone **name** (not number).

```bash
# At creation time
gh issue create --title "Create SRE code review" --body "..." --milestone "Code-Review Plugin v1"

# On an existing issue
gh issue edit 42 --milestone "Code-Review Plugin v1"

# Remove a milestone from an issue
gh issue edit 42 --remove-milestone
```

### Assign a PR to a milestone

```bash
# At creation time
gh pr create --title "feat: SRE domain" --body "..." --milestone "Code-Review Plugin v1"

# On an existing PR
gh pr edit 7 --milestone "Code-Review Plugin v1"

# Remove a milestone from a PR
gh pr edit 7 --remove-milestone
```

## Issues

Standard `gh issue` subcommands are available.
See `gh issue --help` for the full reference.

```bash
# Create an issue linked to a milestone
gh issue create --title "Title" --body "Body" --milestone "Milestone Name"

# List issues for a milestone
gh issue list --milestone "Milestone Name"

# Close an issue
gh issue close 42

# Reopen an issue
gh issue reopen 42

# Edit an issue
gh issue edit 42 --title "New title" --body "New body"
```

## Pull Requests

Standard `gh pr` subcommands are available.
See `gh pr --help` for the full reference.

```bash
# Create a PR linked to a milestone
gh pr create --title "Title" --body "Body" --milestone "Milestone Name"

# View PR checks
gh pr checks 7

# View PR comments
gh api repos/{owner}/{repo}/pulls/7/comments
```

## Quick Reference

| Operation | Command | Identifier |
|-----------|---------|------------|
| Create milestone | `gh api repos/{owner}/{repo}/milestones -X POST -f title="..."` | Returns `number` |
| List milestones | `gh api repos/{owner}/{repo}/milestones` | Query param `?state=open\|closed\|all` |
| Get milestone | `gh api repos/{owner}/{repo}/milestones/{number}` | Milestone `number` (integer) |
| Update milestone | `gh api repos/{owner}/{repo}/milestones/{number} -X PATCH` | Milestone `number` (integer) |
| Delete milestone | `gh api repos/{owner}/{repo}/milestones/{number} -X DELETE` | Milestone `number` (integer) |
| Assign issue | `gh issue create --milestone "Name"` or `gh issue edit N --milestone "Name"` | Milestone **name** (string) |
| Assign PR | `gh pr create --milestone "Name"` or `gh pr edit N --milestone "Name"` | Milestone **name** (string) |
