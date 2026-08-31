# Agentic Loop Status

Updated: 2026-09-01 00:42:00

- Run: agent iteration 6
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: a ratio-based OCR gate can still pass a long crop containing one-character or truncated tokens, and single-sense ranking can phrase an untrusted crop as “本句中”. Rejecting any unexpected one/two-character token and downgrading single-sense cards to the dictionary gloss when the gate fails should reduce unsafe context steering.
- Change: `ContextQualityGate` now hard-rejects unexpected one/two-character OCR tokens instead of allowing them when diluted by a long crop. `LocalContextSenseRanker` now returns the source definition (without “本句中” and without context alignment) for a single-sense word when context is empty or fails the gate. Added replay-shaped regression tests for readable dialogue, `th slender`, `s a e`, and split-letter noise.
- Metric delta: on the existing 20-row replay summary `blind-review-source-explicit-uncertainty-clean-20260831.json` (context strings only; not a gold screenshot evaluation), old gate logic marked 17/20 safe and the new gate marks 12/20 safe, newly rejecting 5 noisy rows. Focused suite: 21/21 passed. `dotnet build RealtimeTranslateCodex.sln --no-restore`: 0 warnings, 0 errors. This is a safety metric, not evidence that the OCR recovery gate meets its 50-screenshot target.
- Real examples: `Her voice softens ... th slender girl ...` and `... calamity ... Yc upon ...` are now rejected rather than allowed to steer; `The dog began to bark at the stranger.` and `She looks at the frail, slender girl ...` remain safe. A single-sense `dwindle` card with `d w i n d l e d` context now shows `v. 逐渐减少、缩小` instead of claiming an in-sentence reading.
- Regressions/limits: strict rejection can lower context Top-1 on rows whose intended sentence is recoverable despite OCR debris (notably the `frail` replay row), so those cards abstain or fall back to source gloss. There are still no screenshot-linked manual transcriptions, no 50-row OCR gold set, and no completed independent 30-word learning review; the 95%/85% OCR and 80% learning gates remain unmeasured.
- Independent review: required after this committed change; prior reviewer result remains `insufficient-evidence` because the repository lacks post-commit screenshots, manual transcriptions, and completed blind decisions.
- Next question: with a fresh screenshot-linked gold set, does this hard reject improve unsafe-context precision to at least 95% while keeping readable subtitle recall at least 85%, and how many of the five newly rejected rows are genuinely recoverable after better line-level OCR?
