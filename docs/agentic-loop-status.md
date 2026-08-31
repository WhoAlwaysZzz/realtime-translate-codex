# Agentic Loop Status

Updated: 2026-09-01 01:28:00

- Run: agent iteration 8
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: provenance must describe the actual displayed item, not merely its service. A WordNet definition or a prefix collision (for example `go` in `good`) cannot truthfully be presented as a target-word example; allowing only target-bearing example sentences will remove another class of false learning evidence.
- Change: `WordNetExampleUsageHintService` no longer falls back to definitions, and its matcher accepts only the exact target or a small conventional inflection set. New tests reject definition-only and `go`/`good` prefix cases while retaining `dwindle`/`dwindled`. This follows commit `78f5d85`, which suppresses legacy four-column morphology/family rows and adds provenance fields to future generated rows.
- Metric delta: on the same 200-word audit, the first fail-closed pass showed 200/200 purported WordNet examples. After requiring an actual target-bearing example, 113/200 cards retain source-labelled WordNet usage and 87/200 honestly omit a learning hook: `artifacts/card-audit-targeted-wordnet-20260901.tsv`. No remaining audit card shows a usage line without the matching evidence label. Executable suite: 21/21 passed. `dotnet build RealtimeTranslateCodex.sln --no-restore`: 0 warnings, 0 errors.
- Real examples: `abrupt` retains `an abrupt reply · came to an abrupt stop`; a `go` lookup no longer treats `a good plan` as a target-word example; a definition such as `move from one place to another` no longer appears as “example usage”.
- Regressions/limits: 87/200 audit words now have no learning hook, so useful source-bearing coverage is 56.5%, below the 70% goal. Legacy TSV rows still lack field-level evidence; the generator currently writes one source/confidence per row, which is insufficient when breakdown and family came from different inputs. The independent read-only review of `78f5d85` was `insufficient-evidence`: there is no screenshot gold corpus, two-agent transcriptions, 30-word learning evaluation, regenerated production TSV, or completed blind reviewer report. OCR and learning completion thresholds remain unmeasured.
- Independent review: a fresh review is required after this committed change. Do not create `docs/agent-loop.auto-accepted.md` or modify `docs/agent-loop.accepted`.
- Next question: can the regenerated TSV carry separate evidence per displayed breakdown/family item and raise useful, source-bearing learning hooks to at least 70% in a blinded 30-word evaluation without reintroducing false provenance?
