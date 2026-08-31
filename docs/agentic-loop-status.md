# Agentic Loop Status

Updated: 2026-09-01 01:19:00

- Run: agent iteration 9
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: when the OCR/context trust gate abstains, source-backed study
  examples can still help memory if they are explicitly and completely bound to
  every candidate sense. A partial set of examples must remain omitted because
  it would quietly choose the sense that the first screen declined to choose.
- Change: `WordNetExampleUsageHintService` now permits a study-only comparison
  only for exactly two candidate senses, only when each has at least one
  target-bearing WordNet example, and prefixes each example group with that
  sense's POS and Chinese definition. The first-screen meaning continues to
  abstain and says `未用于筛选`. Selected-sense cards keep their existing
  compact, unlabeled examples.
- Metric delta: the 200-word rendered-card audit moves from 80 to 90
  source-bearing learning hooks (+10, 40.0% to 45.0%) relative to
  `artifacts/card-audit-sense-bound-learning-20260901.tsv`. Exactly 10 formerly
  blank abstaining two-sense cards gain a complete contrast in
  `artifacts/card-audit-multisense-learning-20260901.tsv`; 23 other abstaining
  cards remain honestly blank because they have three or more senses or an
  unsupported candidate. This is not the required 30-word learning evaluation
  and does not claim the 70% goal is met.
- Real examples: untrusted `bleach` now keeps `无法仅凭这一句确定` and adds
  `v. 漂白、变白：bleach the laundry ... · n. 漂白、漂白剂：a complete
  bleach ...`; `compact` likewise contrasts adjective and verb examples.
  The `bark` regression fixture yields the same two labeled alternatives.
  A three-way `bark` fixture and a two-way fixture with a missing counterpart
  both omit the learning line.
- Regressions/limits: the 10 examples are dictionary evidence, not real-game
  context, and no real screenshot/gold corpus was changed. Learning-hook
  coverage is still far below the 70% target. The only independent review
  outcomes on record are `insufficient-evidence`; the blind-review files have
  no completed decisions. Automatic acceptance criteria are not met. Do not
  create `docs/agent-loop.auto-accepted.md` or modify `docs/agent-loop.accepted`.
- Verification: focused test runner passes all 22 checks; `dotnet build
  RealtimeTranslateCodex.sln -c Release --no-restore` completes with 0 warnings
  and 0 errors. New regression checks cover complete two-way, incomplete
  two-way, and three-way abstaining examples.
- Next question: in a 30-word, independent learning review with real game
  screenshots, do these explicitly labeled two-way comparisons improve useful
  learning without reviewers treating them as a hidden current-sentence gloss?
