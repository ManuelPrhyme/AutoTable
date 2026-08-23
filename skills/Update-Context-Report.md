# Update Context Report

Purpose
- Record what changed after each successful iteration and how it maps to the operational plan so future work is traceable.

When to run
- Immediately after an iteration completes with Success or Partial Success.
- Also run when ad-hoc/unscheduled changes are implemented that affect scope or code.

Build & Run rule
- After every successful build of the application (local or CI) run the app and exercise the primary UI flows for the changes made in that iteration. Running the app verifies runtime behavior (DB migrations, UI wiring, dialogs) and produces runtime evidence (logs, screenshots) to include in the context report.
- If the build is for a feature branch, run the app using that branch's build artifacts and attach the branch name and commit hash to the report entry.

When to run the app
- Locally after a successful dotnet build or Visual Studio build.
- In CI immediately after the build stage (if CI has a GUI runner or an instrumentation path, run headless acceptance steps where possible).

What to capture when running
- Startup logs and any temp diagnostic files produced (autotable_init_error.txt, *_startup_error.txt, unhandled exception files).
- Screenshots of any new or modified UI dialogs/pages relevant to the iteration.
- Environment flags used (AUTOTABLE_DEV_EPHEMERAL_DB, AUTOTABLE_DEMO_MODE) and OS/architecture.
- Any console or test output produced by the run.

How to document the run in the report
- Include a short Run Evidence section in the iteration entry with: RunTimestamp, CommitHash, Branch, FlagsUsed, ScreenshotsPaths, LogPaths, Summary of runtime behavior (success / errors / manual follow-ups).


Location & storage
- skills/Update-Context-Report.md (instruction file)
- reports/context-report.md (append entries here)
- Commit and push the updated report alongside iteration changes.

Required metadata for each entry
- Iteration ID
- Timestamp (ISO 8601)
- Author (git user)
- Success Level: Success / Partial / Rejected
- Operational Plan Reference: plan file or id and step id(s)
- Commit(s): hash + one-line message
- Files changed: list or globs
- Tests & Results: tests run and outcome
- Impact Summary: brief user-visible effects
- Next Actions: follow-up items
- Tags: feature, bugfix, ad-hoc, doc, plan-revision

Template (append to reports/context-report.md)
---
Iteration ID: {id}
Timestamp: {ISO}
Author: {name}
Success Level: {Success/Partial/Rejected}
Operational Plan Reference: {plan-file-or-id} ; Step(s): {step-1, step-2}
Commits:
- {hash} — {one-line message}
Files changed:
- src/...
Tests:
- {test-suite} — {Passed/Failed} (summary)
Aligned changes:
- Step: {step-id} — {short description} — Files: {paths} — Commit: {hash}
Ad-hoc changes:
- {short description} — Reason: {who/why} — Files: {paths} — Commit: {hash} — Action: {reconcile step}
Impact Summary:
- {2–4 sentences}
Next Actions:
- {clear TODOs}
Tags: {tag1, tag2}
---

Extended runbook fields
- RunEvidence: details about the runtime execution (RunTimestamp, CommitHash, Branch, FlagsUsed, ScreenshotsPaths, LogPaths).
- NextAction: the single highest-priority action to take after this iteration (one-liner).
- NextSteps: a short ordered list of concrete tasks to perform in the next iteration that move toward the operational plan goal.
- Recommendations: any suggestions (testing, CI, rollback, documentation) relevant to the changes made.

How to include them
- Add the following sections to each report entry when applicable:

RunEvidence:
- RunTimestamp: {ISO}
- CommitHash: {hash}
- Branch: {branch}
- FlagsUsed: {flags}
- Screenshots: {path1,path2}
- Logs: {path1,path2}

NextAction: {one-line task}

NextSteps:
- 1. {task 1}
- 2. {task 2}
- 3. {task 3}

Recommendations:
- {short list of recommendations}


Procedure (3 steps)
1. Collect evidence: commits, test logs, plan step ids, diff links.
2. Append entry to reports/context-report.md using the template and commit with message "context-report: {iteration-id}".
3. Update the operational plan status (mark step done/modified). If plan changed, add a short Plan revision entry and link it.

If ad-hoc change occurred
- Mark as Ad-hoc in the report, provide justification and create a reconciliation action (add new plan step or mark plan revised).

Automation suggestions
- Small script to append YAML/MD entry and attach HEAD commit hash.
- CI check: require context-report entry for merged feature branches (optional).

Governance
- Review context-report entries during retrospectives and ensure reconciliations were performed.

Example entry
Iteration ID: iteration-2026-08-22-1
Timestamp: 2026-08-22T15:02:00Z
Author: Manuel
Success Level: Success
Operational Plan Reference: plans/operational-plan.md ; step-3
Commits:
- ab12cd3 — Implement AssignStream error logging
Files changed:
- Views/ClassesView.xaml.cs
Tests:
- Unit.Tests — Passed
Aligned changes:
- step-3 — Improve AssignStream exception handling — Views/ClassesView.xaml.cs — ab12cd3
Ad-hoc changes:
- None
Impact Summary:
- Improved diagnostics on AssignStream failures; saves exception to temp file and surfaces full text to dialog.
Next Actions:
- Update unit tests for UI dialog content (owner: Manuel)
Tags: bugfix, diagnostics
