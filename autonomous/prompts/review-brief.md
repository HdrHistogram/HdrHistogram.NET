Study ./spec/README.md.
Read ./plan/planning/brief.md carefully.

Use the Task tool to delegate codebase exploration to a subagent:
- Verify the files mentioned in the brief actually exist
- Check that the proposed changes are feasible given the current code
- Identify any dependencies or side effects not mentioned in the brief

Review the brief for:
- Clarity: Would another developer understand what to do?
- Scope: Is this one PR's worth of work? If too large, suggest splitting.
- Feasibility: Do the proposed changes align with what the code actually looks like?
- Test strategy: Are there specific test cases identified?
- Acceptance criteria: Are they measurable and verifiable?

If changes are needed: create ./plan/planning/brief-review.md with specific,
actionable suggestions.

If the brief is good as-is: move it to ./plan/ready/brief.md