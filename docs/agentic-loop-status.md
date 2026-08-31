# Agentic Loop Status

Updated: 2026-08-31 23:59:00

- Run: agent iteration 2
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: evidence was computed but could disappear from the five-block card, and manual lookup/audit export could omit it; making provenance a reserved visible block should improve trust without changing sentence meaning.
- Change: card formatter now reserves the evidence line before optional usage when the five-block limit is reached; manual lookup populates the same source/confidence metadata as OCR lookup; rendered-card audit exports an explicit evidence column.
- Metric delta: automated suite 20/20 passed (19 prior checks plus crowded-card evidence assertion); solution build 0 warnings/0 errors. Evidence visibility changed from potentially 0/20 on crowded cards to 20/20 by construction for cards carrying evidence (structural metric, not independent learning acceptance).
- Real examples: `ineffable` keeps its sentence gloss and MorphoLex source/confidence line; a crowded card with breakdown, family, usage, and evidence retains evidence while staying at five lines. Typed/manual lookup now follows the same evidence path.
- Regressions/limits: usage examples can yield space to provenance on already-full first screens; no screenshot-backed human review or per-entry provenance audit exists, and OCR/context gates remain unproven.
- Next question: does an independent blind review judge the reserved evidence line useful and factually supported across real rendered cards, rather than merely present?
