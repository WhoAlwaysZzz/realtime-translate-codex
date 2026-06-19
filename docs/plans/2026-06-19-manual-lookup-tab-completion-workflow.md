---
intent: Add Tab completion to manual lookup by reusing the recent lookup history as the completion source.
success_criteria: Typing a previously looked-up word prefix in manual lookup and pressing Tab completes to the most recent matching history word, and Enter submits it.
risk_level: low
auto_approve: true
worktree: host
dirty_worktree: allow
---

## Steps

- [ ] **Step 1: Add failing history completion tests**
action: Update `tests/RealtimeTranslate.Tests/Program.cs` with focused tests proving `JsonRecentLookupStore` returns the newest case-insensitive prefix completion, ignores blank prefixes, ignores error presentations, and preserves existing recent replay behavior.
loop: false
verify: `dotnet test`
gate: auto

- [ ] **Step 2: Implement reusable history completion**
action: Update `src/RealtimeTranslate.Core/History/IRecentLookupStore.cs` and `src/RealtimeTranslate.Core/History/JsonRecentLookupStore.cs` so callers can request a completion for a manual lookup prefix without parsing recent records themselves.
loop: until tests pass
max_iterations: 3
verify: `dotnet test`
gate: auto

- [ ] **Step 3: Wire Tab completion into manual lookup**
action: Update `src/RealtimeTranslate.App/ManualInput/ManualLookupWindow.cs` and `src/RealtimeTranslate.App/AppController.cs` so the manual input window asks the recent lookup store for a completion when Tab is pressed, fills the text when a candidate exists, and keeps Enter/Escape behavior unchanged.
loop: until build succeeds
max_iterations: 3
verify: `dotnet test`
gate: auto

- [ ] **Step 4: Final verification and commit**
action: Run the full test suite, inspect the diff for unrelated files, and commit only the files changed for this feature.
loop: false
verify: `git diff -- src/RealtimeTranslate.Core/History/IRecentLookupStore.cs src/RealtimeTranslate.Core/History/JsonRecentLookupStore.cs src/RealtimeTranslate.App/ManualInput/ManualLookupWindow.cs src/RealtimeTranslate.App/AppController.cs tests/RealtimeTranslate.Tests/Program.cs`
gate: auto
