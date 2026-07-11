---
intent: Unify every user-visible WPF surface into a compact utility-style interface and publish the project as a safe, conventional public GitHub repository.
success_criteria: All windows share one restrained visual system, build and tests pass, repository health files and Windows CI exist, secret scanning is clean, and the public GitHub repository is reachable.
risk_level: high
auto_approve: true
branch: codex/public-release-ui
worktree: host
dirty_worktree: allow
---

## Steps

- [ ] **Step 1: Record the approved design and workflow**
action: Save the approved phase design and this executable workflow, then commit only those documents while leaving idea.md untouched.
loop: false
verify: git diff --check && git status --short

- [ ] **Step 2: Add deterministic UI theme coverage**
action: Add tests for shared visual tokens and window-independent layout values that define the compact utility style.
loop: false
verify: dotnet run --project tests/RealtimeTranslate.Tests/RealtimeTranslate.Tests.csproj

- [ ] **Step 3: Implement the shared compact theme**
action: Extend UiTheme with reusable typography, button, input, panel, section, and title-bar styling while retaining existing animation behavior.
loop: until the focused UI tests pass
max_iterations: 3
verify: dotnet run --project tests/RealtimeTranslate.Tests/RealtimeTranslate.Tests.csproj

- [ ] **Step 4: Apply the theme to every visible window**
action: Update lookup, explanation, manual input, hotkey settings, and selection surfaces to use consistent spacing, typography, focus states, controls, and restrained shadows without changing interactions.
loop: until the solution builds and the app remains alive during a startup smoke check
max_iterations: 3
verify: dotnet build RealtimeTranslateCodex.sln

- [ ] **Step 5: Commit the cohesive UI change**
action: Review the UI diff, run formatting validation, and commit only the complete visual-system change.
loop: until formatting is clean
max_iterations: 3
verify: dotnet format RealtimeTranslateCodex.sln --verify-no-changes --verbosity minimal

- [ ] **Step 6: Add public repository health files**
action: Add MIT licensing, ignore rules, public-facing README sections, contribution and security policies, code of conduct, issue forms, pull request template, and Windows CI for restore, build, test, and format validation.
loop: until repository metadata checks pass
max_iterations: 3
verify: git diff --check

- [ ] **Step 7: Audit publication safety**
action: Scan tracked content and commit history for credential patterns, local secrets, generated archives, and private absolute paths; correct any publication blocker without staging idea.md.
loop: until scans report no publication blocker
max_iterations: 3
verify: git grep -n -I -E "(ghp_|github_pat_|sk-[A-Za-z0-9]|DEEPSEEK_API_KEY[[:space:]]*=|apiKey[[:space:]]*:[[:space:]]*\"[^\"]+\")" -- . ":(exclude)docs/plans/2026-07-11-public-release-ui-workflow.md"
gate: human

- [ ] **Step 8: Verify and commit repository readiness**
action: Run the complete build, test, formatting, status, and documentation checks, then commit repository metadata as a focused change.
loop: until every verification succeeds
max_iterations: 3
verify: dotnet build RealtimeTranslateCodex.sln && dotnet run --project tests/RealtimeTranslate.Tests/RealtimeTranslate.Tests.csproj && dotnet format RealtimeTranslateCodex.sln --verify-no-changes --verbosity minimal

- [ ] **Step 9: Create and publish the public GitHub repository**
action: Create the public WhoAlwaysZzz/realtime-translate-codex repository using the authenticated GITHUB_TOKEN, add origin, push the current branch, and verify repository visibility and URLs.
loop: until GitHub reports the repository public and the branch is present
max_iterations: 3
verify: gh repo view WhoAlwaysZzz/realtime-translate-codex --json nameWithOwner,visibility,url,defaultBranchRef
gate: human
