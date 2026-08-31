# Agentic Loop Status

Updated: 2026-09-01 01:12:00

- Run: agent iteration 7
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: the legacy four-column word-card index combines ECDICT inflections, MorphyNet relations, MorphoLex analyses, and uncited curated hints. Labelling every non-empty row as MorphoLex makes its learning evidence false; fail-closed presentation plus field-level provenance in future exports should remove unsupported claims while retaining attributable learning examples.
- Change: `MorphemeWordInsightService` now treats four-column rows as non-evidenced, and `AppController` excludes them from the learning layer. The card generator now emits per-row source and confidence for ECDICT inflections, MorphyNet derivations, and MorphoLex analyses; uncited curated hints remain suppressed. The fallback Open English WordNet usage example stays visible with its existing explicit evidence label.
- Metric delta: on the fixed 200-word rendered-card audit, the previous output had 189 cards with a `MorphoLex (2018)` evidence label and 189 visible morphology/family blocks despite the row schema having no source column. The new production-shaped audit has 200/200 source-bearing WordNet usage hooks, 0/200 visible legacy morphology/family blocks, and 0 usage blocks lacking evidence: `artifacts/card-audit-provenance-failclosed-20260901.tsv`. Focused executable suite: 21/21 passed. `dotnet build RealtimeTranslateCodex.sln --no-restore`: 0 warnings, 0 errors.
- Real examples: `abrupt` changed from an unsafely attributed family card (`abruptly · abruptness`, labelled MorphoLex) to `an abrupt reply · came to an abrupt stop` with `WordNet example usage; confidence 90%`. `adolescent` keeps its abstaining multi-sense first screen and receives source-labelled usage examples rather than a claimed morphological relation.
- Regressions/limits: this deliberately hides 189 legacy morphology/family prompts until the bundled TSV is regenerated with field-level evidence; it is a provenance correction, not proof that WordNet examples meet the 80% learning-helpfulness gate. No real screenshot files, two-agent transcriptions, 50-row OCR gold corpus, 30-word learning evaluation, or completed independent review exist in this checkout, so OCR and learning completion thresholds remain unmeasured.
- Independent review: required after this committed change. Do not create `docs/agent-loop.auto-accepted.md` or modify `docs/agent-loop.accepted`.
- Next question: after regenerating the word-card TSV from available source datasets, do field-level citations restore useful morphology/family insights without any unsupported-source rows, and can a blinded 30-word learning evaluation show at least 80% helpfulness?
