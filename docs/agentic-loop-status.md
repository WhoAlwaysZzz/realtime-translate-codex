# Agentic Loop Status

Updated: 2026-09-01 02:05:00

- Run: agent iteration 9
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: a fixed `WordNet example usage; confidence 90%` label is not
  evidence-backed because individual hints differ in whether their retained
  example contains the lemma itself or only a conventional inflection. The
  card must expose the actual source and verification scope instead of a
  blanket confidence claim.
- Change: `IUsageHintService` now returns `UsageHintResult`, which binds hint
  text to `Open English WordNet 2025`, target-form verification, and a derived
  confidence. A direct target match yields 90%; conventional inflections lower
  confidence according to the retained-example coverage. Empty evidence is
  fail-closed. Both OCR lookup and manual lookup use this result; the context
  trust gate and abstention behavior are unchanged.
- Metric delta: source-bearing learning hints remain 90/200 in
  `artifacts/card-audit-provenance-derived-20260901.tsv` (no coverage claim).
  Their provenance is now auditable: 68 show `例句直含目标词` at 90%, and 22
  show `例句含常规词形` at a derived 75%–85%, rather than all being labeled
  90%. This is provenance-quality improvement, not a learning acceptance
  result.
- Real examples: `bleach` remains an abstaining two-sense card and labels its
  WordNet comparison as `例句含常规词形；置信度 85%` because `bleached` is one
  retained example. `compact` behaves the same for `compacts`. A selected
  `dwindle` usage with only `dwindled` is labeled 75%. Direct matches such as
  `abrupt` retain 90% with the explicit direct-match label.
- Regressions/limits: no screenshot or gold-corpus OCR data changed, and the
  required 50-screenshot / 30-word independent review datasets do not exist
  in this repository. The latest independent reviewer outcome remains
  `insufficient-evidence`; its cited absence of actual card captures and
  completed review decisions is unresolved. Do not create
  `docs/agent-loop.auto-accepted.md` or modify `docs/agent-loop.accepted`.
- Verification: `dotnet build RealtimeTranslateCodex.sln -c Release
  --no-restore` completed with 0 warnings and 0 errors; the executable focused
  suite passed 22/22. Tests cover direct forms, inflections, empty evidence,
  complete two-way comparisons, incomplete comparisons, and three-way
  abstention.
- Next question: can a reviewer assess captured first-screen cards against a
  real, dual-transcribed game-screenshot corpus before further learning-card
  wording or confidence tuning?
