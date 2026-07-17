#!/usr/bin/env python3
"""Build the compact runtime lexicon used for sense-aware lookup cards."""

from __future__ import annotations

import argparse
import csv
import gzip
import json
import re
from pathlib import Path


POS_MAP = {
    "n": "n", "v": "v", "vi": "v", "vt": "v",
    "a": "adj", "s": "adj", "ad": "adv", "adv": "adv",
    "prep": "prep", "conj": "conj", "pron": "pron", "num": "num",
}


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ecdict", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


def normalize_pos(value: str) -> str:
    return POS_MAP.get(value.lower().rstrip("."), value.lower().rstrip("."))


def parse_lines(value: str) -> list[tuple[str, str]]:
    result: list[tuple[str, str]] = []
    for raw in re.split(r"\\n|\r?\n", value or ""):
        text = raw.strip()
        if not text or text.startswith("["):
            continue
        match = re.match(r"^([a-z]+)\.\s*(.+)$", text, re.I)
        if match:
            result.append((normalize_pos(match.group(1)), match.group(2).strip()))
        else:
            result.append(("", text))
    return result


def frequency_band(row: dict[str, str]) -> int:
    ranks = [int(row[key]) for key in ("bnc", "frq") if (row.get(key) or "").isdigit() and int(row[key]) > 0]
    rank = min(ranks) if ranks else 10**9
    if rank <= 3_000:
        return 3
    if rank <= 8_000:
        return 2
    return 1


def shorten(value: str) -> str:
    parts = [part.strip() for part in re.split(r"[,，]", value) if part.strip()]
    return "、".join(parts[:3])


def main() -> None:
    args = arguments()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    written = 0
    with args.ecdict.open(encoding="utf-8-sig", newline="") as source, gzip.open(args.output, "wt", encoding="utf-8", newline="\n") as target:
        for row in csv.DictReader(source):
            word = (row.get("word") or "").strip().lower()
            if not re.fullmatch(r"[a-z][a-z0-9+' .-]*", word):
                continue
            translations = parse_lines(row.get("translation") or "")
            if not translations:
                continue
            english_by_pos: dict[str, list[str]] = {}
            for pos, definition in parse_lines(row.get("definition") or ""):
                english_by_pos.setdefault(pos, []).append(definition)
            senses = []
            for pos, translation in translations:
                short = shorten(translation)
                if not short:
                    continue
                english = english_by_pos.get(pos, [])
                if pos == "v" and not english:
                    english = english_by_pos.get("", [])
                senses.append({"p": pos, "z": short, "e": english[:8]})
            if not senses:
                continue
            record = {"w": word, "f": frequency_band(row), "s": senses}
            target.write(json.dumps(record, ensure_ascii=False, separators=(",", ":")) + "\n")
            written += 1
    print(f"entries={written} output={args.output}")


if __name__ == "__main__":
    main()
