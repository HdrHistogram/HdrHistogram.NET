Study ./spec/README.md
Read ./plan/ready/brief.md.

Use the Task tool to delegate codebase exploration to a subagent:
- Examine the files identified in the brief
- Map the current implementation, dependencies, and test coverage
- Return a summary of what exists and what needs to change

Using the exploration results, create ./plan/ready/task.md with a checklist.
Each task should be:
- Specific: which file, what change, why
- Atomic: one logical change per task
- Verifiable: how to confirm it's done
- Ordered: dependencies respected

Include tasks for:
- Implementation changes
- Updating or adding unit tests
- Updating XML doc comments if public API changes

Use `[ ]` for each task. 
Validate the task list is complete by cross-referencing every acceptance criterion in the brief — each criterion must be covered by at least one task.