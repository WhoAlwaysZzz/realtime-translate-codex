---
design_type: feature
created_at: 2026-06-19
---

## Intent Contract

intent: Add Tab completion to manual lookup by reusing the recent lookup history as the completion source.

constraints:
- Preserve the existing manual lookup Enter and Escape behavior.
- Preserve the existing recent lookup JSON storage format.
- Keep completion matching reusable behind the recent lookup abstraction instead of making the manual input window parse history records directly.
- Avoid touching unrelated modified files already present in the working tree.

success_criteria:
- When a word has already been looked up, typing a prefix in manual lookup and pressing Tab completes to the most recent matching history word.
- Pressing Enter after completion submits the completed word for lookup.
- Empty input or unmatched input leaves the text unchanged.

risk_level: low

## Verification Contract

verify_steps:
- run tests: `dotnet test`
- check: unit coverage proves recent lookup completion returns the newest case-insensitive prefix match, ignores blank prefixes, and preserves existing recent replay behavior.
- confirm: the WPF app project builds and the manual lookup window accepts reusable completion candidates without changing existing submit/cancel behavior.

## Governance Contract

approval_gates:
- Human approval of this design before implementation.

rollback:
- Revert the documentation commit and the focused implementation commit for this feature.

ownership:
- Codex implements and verifies the change; the user reviews the final behavior.

## Scope

| Area | In | Out |
| --- | --- | --- |
| Recent lookup history | Add reusable completion lookup behavior to the history abstraction. | Change history file format or storage location. |
| Manual lookup UI | Apply Tab completion from history-backed candidates. | Add dropdowns, fuzzy search, or multi-candidate selection UI. |
| Tests | Cover the history completion behavior and preserve replay behavior. | Add full WPF interaction automation. |
| Git hygiene | Commit docs and implementation as focused commits. | Commit unrelated existing working-tree changes. |

## Decisions

| # | Decision | Choice | Rejected alternatives |
| --- | --- | --- | --- |
| 1 | Completion source | `IRecentLookupStore` owns completion matching. | Passing raw history into the window and matching there. |
| 2 | Match rule | Case-insensitive prefix match, newest result wins. | Fuzzy search or alphabetical ranking. |
| 3 | Empty input behavior | Do nothing on Tab. | Complete to the newest word with no prefix. |
| 4 | Duplicate history entries | Treat repeated words as one completion candidate, preserving newest priority. | Return duplicate candidates to UI. |
| 5 | UI behavior | Tab fills the text and moves the caret to the end. | Insert only the suffix or show a candidate list. |

## Surface

APIs:
The recent lookup store interface gains a reusable operation for asking history to complete a manual lookup prefix.

Storage:
Recent lookup persistence stays compatible with the existing JSON entries because completion can be derived from stored presentation titles.

Components:
The manual lookup window remains responsible for keyboard handling and submission. It receives a completion provider rather than reading recent lookup records itself.

Files touched:
Expected implementation work is limited to the recent lookup interface and implementation, manual lookup window construction/keyboard handling, controller wiring, and focused tests.

## Risks & Open Questions

- WPF Tab normally participates in focus traversal, so the window must intercept it before focus handling moves away from the input.
- If a previous lookup title is an error message rather than a real word, it could become a candidate unless history storage already filters it; implementation should prefer non-error lookup presentations.
- Full UI automation is not currently present, so verification relies on unit tests for matching and a project build for WPF integration.
