# Roadmap Notes

This document records deferred ideas and product direction notes so small fixes can stay focused while larger jumps have a place to wait.

## Paused Ideas

### Sentence Or Paragraph Explanation

Status: paused.

Goal: allow a separate hotkey/mode to select a sentence or short paragraph, then show a larger explanation panel with original text, Chinese translation, and key phrase notes.

Current judgment:

- This should not be folded into the word lookup flow.
- The feature should reuse selection, screenshot, OCR, archive, and hotkey plumbing, but use separate text post-processing, explanation, and presentation services.
- A fully offline implementation is possible but likely heavy because translation/explanation quality depends on local model choices, packaging, speed, and machine requirements.
- An AI/API implementation would be much simpler and higher quality, but changes the privacy and offline-first character of the project.

Likely future shape:

- Add a separate command hotkey such as `F6 Explain selection`.
- Add text-oriented interfaces for OCR cleanup, explanation generation, and a larger scrollable/copyable explanation panel.
- Keep the first experiment small: recognized text, Chinese translation, and several key phrase notes.

Decision: revisit after the core word-lookup workflow feels stable.
