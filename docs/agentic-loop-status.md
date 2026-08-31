# Agentic Loop Status

Updated: 2026-09-01 03:42:00

- Run: agent iteration 18
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: single-sense WordNet entries without examples can still provide a useful learning layer when one bounded English definition is shown as a sourced core concept; this should increase memorable, evidence-backed clues without steering sense selection.
- Change: `WordNetExampleUsageHintService` now emits `核心概念` only for a single sense and a 20-160 character WordNet definition. Multi-sense comparisons and short/weak definitions still omit the insight. Added a regression test for the definition-only fallback.
- Metric delta: rendered 200-word audit sourced learning hooks increased from 90/200 (45.0%) in `artifacts/card-audit-hypothesis.tsv` to 161/200 (80.5%) in `artifacts/card-audit-hypothesis-after.tsv`; this clears the 70% coverage target. Existing OCR replay evidence is unchanged at 20/20 exact and 14/16 labelled context Top-1. Tests: 22/22 passed; solution build 0 warnings, 0 errors.
- Real examples: `go` with WordNet definition `move from one place to another` now shows a source-labelled core concept while keeping the Chinese gloss `移动`; `bark` with no trusted context remains an abstention and does not receive a guessed main sense; a one-word definition such as `move` is still omitted as too weak.
- Regressions: no first-screen definition, context alignment, or multi-sense abstention changes observed; 39/200 audit words still omit learning hooks by design because no bounded sourced example/definition is available.
- Next question: have an independent blind reviewer judge whether the new English core concepts help learners beyond the Chinese gloss, and measure the learning-layer pass rate and any cases where the definition merely repeats the gloss.
