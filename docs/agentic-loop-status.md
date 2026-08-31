# Agentic Loop Status

Updated: 2026-09-01 04:08:00

- Run: agent iteration 20
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: a bounded WordNet definition is not, by itself, a dependable learning insight because it can merely restate the Chinese gloss; requiring a source-backed example containing the target word or a verified regular inflection will remove weak claims while retaining useful word-level evidence.
- Change: removed the definition-only fallback from `WordNetExampleUsageHintService`. The learning layer now abstains when no target-bearing WordNet example exists; it does not affect sense ranking or first-screen meanings. Added a regression that `go` with only `move from one place to another` produces no learning hook.
- Metric delta: deterministic 200-word rendered-card audit changed from 161/200 source-bearing hooks before to 90/200 after. The 71 removed hooks were exactly all definition-only hooks (71 -> 0); verified-example hooks stayed 90/200 (90 -> 90). Focused suite: 23/23 passing. Release solution build: 0 warnings, 0 errors.
- Real examples: `go` with Chinese gloss `移动` and only the definition `move from one place to another` now omits the learning layer rather than presenting a gloss repeat. `dwindle` retains `their savings dwindled away`; `bark` still offers a two-sense comparison only when each sense has a target-bearing example. Context ranking and abstention remain outside this change.
- Regressions/limits: lower hook coverage is intentional and does not establish learner usefulness. This checkout contains no real-game screenshot files, manual transcription set, 50-screenshot gold corpus, or completed independent card judgments; OCR metrics therefore remain unmeasured here. Before/after learning outputs are preserved at `artifacts/learning-example-gate-before-20260901.tsv` and `artifacts/learning-example-gate-after-20260901.tsv`.
- Next question: can a sourced 30-word real-game learning evaluation establish whether the retained target-bearing examples are useful memory cues rather than just accurate usage snippets?
