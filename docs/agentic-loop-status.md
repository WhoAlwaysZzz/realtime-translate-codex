# Agentic Loop Status

Updated: 2026-09-01 04:15:00

- Run: agent iteration 16
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis tested and rejected: WordNet examples from a part-of-speech bucket with more than one English definition are too weak to display as a learning hook, because the compact lexicon merges source synsets before the card can bind an example to the displayed Chinese gloss.
- Evidence: `artifacts/learning-evidence-before-20260901.tsv` and `artifacts/learning-evidence-after-20260901.tsv` preserve 200 rendered-card outputs. `commute` demonstrates the defect: its broad verb gloss includes exchange, sentence reduction, and travel, while its displayed WordNet example is only "these operators commute with each other" (the mathematical sense).
- Metric delta: the temporary fail-closed guard removed 76 weak/mixed cards, reducing source-bearing learning hooks from 90/200 (45%) to 14/200 (7%). This is below the goal's >=70% useful-learning-hint target, so the product code and test were removed rather than shipping a coverage regression. The retained 14 hooks were single-definition source buckets; no OCR or context-selection behavior was changed.
- Real examples and regressions: `commute` correctly lost its misleading hook under the probe, while `bizarre`, `calibration`, and `therein` retained direct WordNet examples. The guard also removed useful-looking examples for 76 cards, so it is not an acceptable release behavior. The normal test suite passed while the probe was present; no product behavior is retained.
- Next question: can the lexicon generator retain WordNet definition/example pairs as individual evidence units, and can a manually reviewed bilingual mapping bind an example to a specific displayed Chinese sense, without dropping learning coverage below 70%?
