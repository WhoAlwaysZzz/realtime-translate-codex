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
    parser.add_argument("--wordnet-dir", type=Path)
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


def load_wordnet(path: Path | None) -> dict[str, dict[str, tuple[list[str], list[str]]]]:
    if path is None or not path.exists():
        return {}
    evidence: dict[str, dict[str, tuple[list[str], list[str]]]] = {}
    pos_map = {"n": "n", "v": "v", "a": "adj", "s": "adj", "r": "adv"}
    for json_path in path.glob("*.json"):
        if json_path.name.startswith("entries-"):
            continue
        with json_path.open(encoding="utf-8") as source:
            synsets = json.load(source)
        for synset in synsets.values():
            if not isinstance(synset, dict) or "members" not in synset:
                continue
            pos = pos_map.get(str(synset.get("partOfSpeech", "")), str(synset.get("partOfSpeech", "")))
            definitions = [str(value) for value in synset.get("definition", [])]
            examples = [str(value) for value in synset.get("example", [])]
            for member in synset.get("members", []):
                word = str(member).replace("_", " ").lower()
                by_pos = evidence.setdefault(word, {})
                current_definitions, current_examples = by_pos.setdefault(pos, ([], []))
                current_definitions.extend(value for value in definitions if value not in current_definitions)
                current_examples.extend(value for value in examples if value not in current_examples)
    return evidence


def main() -> None:
    args = arguments()
    wordnet = load_wordnet(args.wordnet_dir)
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
            grouped: dict[str, dict[str, list[str] | str]] = {}
            for pos, translation in translations:
                short = shorten(translation)
                if not short:
                    continue
                item = grouped.setdefault(pos, {"z": [], "d": [], "x": []})
                item["z"].extend(part for part in short.split("、") if part not in item["z"])
            for pos, item in grouped.items():
                definitions, examples = wordnet.get(word, {}).get(pos, (english_by_pos.get(pos, []), []))
                item["d"] = definitions[:16]
                item["x"] = examples[:12]
            senses = [
                {"p": pos, "z": "、".join(item["z"][:5]), "d": item["d"], "x": item["x"]}
                for pos, item in grouped.items()
            ]
            if not senses:
                continue
            record = {"w": word, "f": frequency_band(row), "s": senses}
            target.write(json.dumps(record, ensure_ascii=False, separators=(",", ":")) + "\n")
            written += 1
    print(f"entries={written} output={args.output}")


if __name__ == "__main__":
    main()
