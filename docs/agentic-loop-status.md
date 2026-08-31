# Agentic Loop Status

Updated: 2026-09-01 04:20:05 +08:00

- Run: agent iteration 21
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required for this relevant change after commit
- Completion: automatic thresholds not met; do not create `docs/agent-loop.auto-accepted.md`
- Hypothesis: the binary card label `整句句意对齐：已/未用于筛选` hides whether context was absent, rejected by the OCR trust gate, trusted but unnecessary for sense filtering, or actually used to select the main meaning. Carrying the gate result as typed data into the first-screen card will make unsafe context visibly non-authoritative without changing OCR or sense selection.
- Change: added `ContextTrustStatus` with `Unavailable`, `Rejected`, `TrustedNotUsed`, and `UsedForMeaning`; `LocalContextSenseRanker` now preserves that state and `LookupCardTextFormatter` renders a distinct Chinese trust line. OCR variants, the quality-gate rules, ranking scores, dictionary data, and learning insights are unchanged.
- Metric delta: fresh production replay of the same 20 archived screenshots stayed at OCR exact 20/20, context available 16/16, context Top-1 15/16, and labelled abstention 4/4, with 0 timeouts/failures before and after. A five-case manually transcribed card audit exposed the expected trust state in 5/5 after versus 0/5 explicitly distinguished before; all five main meanings were unchanged. The executable suite passed 23/23 and Debug/Release builds completed with 0 warnings and 0 errors.
- Real examples: archive 939 (`thrust`) keeps the abstaining candidate list but changes the ambiguous `未用于筛选` line to `上下文：OCR不可靠，未参与释义`; archive 1123 (`bark`) does the same when context OCR contains only the selected word. Archive 500 (`frail`) says trusted context locked the sentence meaning. Archive 657 (`-neglected`) says context was trusted but did not filter senses because the dictionary entry has one sense. Exact transcriptions, OCR text, primary meanings, and before/after card lines are in `artifacts/context-trust-card-audit-20260901.tsv`.
- Regressions/limits: no OCR or meaning regression was observed across the 20 replay cases (`result_field_differences=0`). This iteration improves disclosure, not the underlying 15/16 context Top-1 result, OCR WER, or learning usefulness. The five manual transcriptions are developer-reviewed probes, not the required two-agent 50-screenshot gold corpus. There is still no completed 30-word independent learning evaluation or accepted reviewer report.
- Evidence: `artifacts/context-trust-before-20260901.json`, `artifacts/context-trust-after-20260901.json`, and `artifacts/context-trust-card-audit-20260901.tsv`.
- Next question: can the remaining context Top-1 miss be repaired from screenshot evidence without lowering trust-gate precision or changing the four labelled abstentions?
