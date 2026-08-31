#!/usr/bin/env python3
"""Build a precision-first local word-card index from ECDICT and MorphyNet."""

from __future__ import annotations

import argparse
import collections
import csv
import random
import re
from pathlib import Path

import openpyxl


PREFIXES = {
    "un": "不/相反", "in": "不", "im": "不", "ir": "不", "il": "不",
    "dis": "否定/分离", "re": "再次/返回", "pre": "之前", "post": "之后",
    "non": "非", "mis": "错误地", "over": "过度/在上", "under": "不足/在下",
    "inter": "在……之间", "sub": "在下/次级", "super": "超越/在上",
    "anti": "反对", "counter": "相反/对应", "co": "共同", "de": "去除/向下",
    "fore": "在前", "out": "超过/向外", "trans": "跨越/转变", "ultra": "超越",
    "semi": "半", "multi": "多", "micro": "微小", "macro": "宏大",
    "auto": "自身/自动", "bi": "二", "tri": "三",
    "be": "使/加强语气",
}

# Surface-valid relations that are lexicalized or misleading for a learner-facing card.
DENIED_RELATIONS = {
    ("gloss", "glossary"),
    ("hear", "overhear"),
    ("corporate", "incorporate"),
    ("casual", "casualty"),
}

DENIED_MORPHOLEX = {
    "commute", "consist", "consistent", "consistently", "denote",
    "discontented", "everlasting", "forsake", "glossary", "hospitality", "incorporate", "intent",
    "jewellery", "nickname", "overhear", "paralyse", "retort", "withhold",
}

BOUND_ROOTS = {
    "bene": "好/善", "mal": "坏/恶", "cred": "相信", "dict": "说",
    "duc": "引导", "duct": "引导", "fer": "携带", "flect": "弯曲",
    "chrono": "时间", "flu": "流动", "fract": "打破", "graph": "写/记录", "ject": "投掷",
    "log": "说/学科", "magn": "大", "morph": "形态", "pel": "推动",
    "plic": "折叠", "pose": "放置", "rupt": "破裂", "scrib": "写",
    "script": "写", "sect": "切", "sens": "感觉", "spect": "看",
    "struct": "建造", "terr": "恐惧", "tract": "拉", "vert": "转",
    "vis": "看", "vol": "意愿",
}

SUFFIXES = {
    "ly": ("以……方式/具有……性质", {("J", "R"), ("N", "J")}),
    "ness": ("性质/状态", {("J", "N")}),
    "ment": ("行为/结果", {("V", "N")}),
    "tion": ("行为/结果", {("V", "N")}),
    "sion": ("行为/结果", {("V", "N")}),
    "ation": ("行为/结果", {("V", "N")}),
    "ition": ("行为/结果", {("V", "N")}),
    "ity": ("性质/状态", {("J", "N")}),
    "ty": ("性质/状态", {("J", "N")}),
    "ce": ("性质/状态", {("J", "N")}),
    "cy": ("性质/状态", {("J", "N")}),
    "able": ("能够……的", {("V", "J")}),
    "ible": ("能够……的", {("V", "J")}),
    "al": ("与……有关的", {("N", "J")}),
    "ial": ("与……有关的", {("N", "J")}),
    "ic": ("与……有关的", {("N", "J")}),
    "ical": ("与……有关的", {("N", "J")}),
    "ous": ("充满/具有……的", {("N", "J")}),
    "ious": ("充满/具有……的", {("N", "J")}),
    "ive": ("具有……性质的", {("V", "J"), ("N", "J")}),
    "ative": ("具有……性质的", {("V", "J")}),
    "ful": ("充满……的", {("N", "J")}),
    "less": ("没有……的", {("N", "J")}),
    "er": ("执行者/相关事物", {("V", "N")}),
    "or": ("执行者/相关事物", {("V", "N")}),
    "ist": ("从事者/信奉者", {("N", "N"), ("J", "N")}),
    "ism": ("主义/体系", {("N", "N"), ("J", "N")}),
    "ize": ("使成为", {("N", "V"), ("J", "V")}),
    "ise": ("使成为", {("N", "V"), ("J", "V")}),
    "ify": ("使成为", {("N", "V"), ("J", "V")}),
    "en": ("使成为", {("N", "V"), ("J", "V")}),
    "ship": ("身份/状态", {("N", "N")}),
    "hood": ("身份/时期", {("N", "N")}),
    "dom": ("状态/领域", {("N", "N")}),
    "ance": ("行为/状态", {("V", "N"), ("J", "N")}),
    "ence": ("行为/状态", {("V", "N"), ("J", "N")}),
    "ant": ("执行者/具有……性质", {("V", "N"), ("V", "J")}),
    "ent": ("执行者/具有……性质", {("V", "N"), ("V", "J")}),
    "ary": ("与……有关的人/物", {("N", "J"), ("N", "N")}),
    "ory": ("与……有关的", {("V", "J"), ("N", "J")}),
    "ian": ("相关的人/事物", {("N", "N"), ("N", "J")}),
    "ician": ("专业人员", {("N", "N")}),
    "ee": ("动作承受者", {("V", "N")}),
    "ess": ("女性/雌性", {("N", "N")}),
    "ward": ("朝向……", {("N", "R"), ("N", "J")}),
    "wards": ("朝向……", {("N", "R")}),
    "wise": ("以……方式/方向", {("N", "R")}),
    "ish": ("有点/像……的", {("N", "J"), ("J", "J")}),
}

INFLECTION_LABELS = {
    "p": "过去式", "d": "过去分词", "i": "现在分词", "3": "第三人称单数",
    "s": "复数", "r": "比较级", "t": "最高级",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ecdict", required=True, type=Path)
    parser.add_argument("--derivational", required=True, type=Path)
    parser.add_argument("--morpholex", type=Path)
    parser.add_argument("--contexts", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--audit-sample", type=Path)
    parser.add_argument("--audit-size", type=int, default=200)
    return parser.parse_args()


def primary_translation(value: str) -> str:
    text = value.replace("\\n", "; ").split(";", 1)[0].strip()
    text = re.sub(r"^(?:a|ad|adv|n|v|vi|vt)\.\s*", "", text, flags=re.I)
    return text.split(",", 1)[0].strip()


def surface_matches(source: str, target: str, affix: str, kind: str) -> bool:
    if kind == "prefix":
        return target == affix + source
    if not target.endswith(affix):
        return False
    stem = target[:-len(affix)]
    return (
        stem == source
        or (source.endswith("e") and stem == source[:-1])
        or (source.endswith("y") and stem == source[:-1] + "i")
        or (len(stem) > 1 and stem[-1] == stem[-2] and stem[:-1] == source)
    )


def load_curated(path: Path | None) -> dict[str, tuple[str, str, str]]:
    curated: dict[str, tuple[str, str, str]] = {}
    if path is None or not path.exists():
        return curated
    with path.open(encoding="utf-8", newline="") as source:
        for row in csv.reader(source, delimiter="\t"):
            if len(row) == 4 and row[0] and not row[0].startswith("#"):
                curated[row[0].lower()] = (row[1].strip(), row[2].strip(), row[3].strip())
    return curated


def load_morpholex(path: Path | None) -> dict[str, list[tuple[str, str]]]:
    if path is None or not path.exists():
        return {}
    workbook = openpyxl.load_workbook(path, read_only=True, data_only=True)
    analyses: dict[str, list[tuple[str, str]]] = {}
    token_pattern = re.compile(r"<([a-z]+)<|\(([a-z]+)\)|>([a-z]+)>", re.I)
    for sheet in workbook.worksheets[1:33]:
        rows = sheet.iter_rows(values_only=True)
        header = list(next(rows))
        if "Word" not in header or "MorphoLexSegm" not in header:
            continue
        word_index, segmentation_index = header.index("Word"), header.index("MorphoLexSegm")
        for row in rows:
            if not row[word_index] or not row[segmentation_index]:
                continue
            parts = []
            for match in token_pattern.finditer(str(row[segmentation_index]).lower()):
                if match.group(1):
                    parts.append(("prefix", match.group(1)))
                elif match.group(2):
                    parts.append(("root", match.group(2)))
                else:
                    parts.append(("suffix", match.group(3)))
            if len(parts) >= 2:
                analyses[str(row[word_index]).lower()] = parts
    return analyses


def main() -> None:
    args = parse_args()
    entries: dict[str, dict] = {}
    with args.ecdict.open(encoding="utf-8-sig", newline="") as source:
        for row in csv.DictReader(source):
            word = row["word"].strip().lower()
            if not re.fullmatch(r"[a-z]+", word):
                continue
            ranks = [int(row[key]) for key in ("bnc", "frq") if (row.get(key) or "").isdigit() and int(row[key]) > 0]
            entries[word] = {
                "rank": min(ranks) if ranks else 10**9,
                "core": bool(row.get("tag") or row.get("oxford") or int(row.get("collins") or 0) > 0),
                "tags": set((row.get("tag") or "").split()),
                "meaning": primary_translation(row.get("translation") or ""),
                "exchange": row.get("exchange") or "",
            }

    lemma_of: dict[str, tuple[str, str]] = {}
    forms: dict[str, set[str]] = collections.defaultdict(set)
    for word, entry in entries.items():
        for item in entry["exchange"].split("/"):
            if ":" not in item:
                continue
            code, form = item.split(":", 1)
            form = form.lower()
            if form not in entries:
                continue
            if code == "0":
                previous = lemma_of.get(word)
                if previous is None or len(form) > len(previous[0]):
                    lemma_of[word] = (form, "")
                forms[form].add(word)
            elif code in INFLECTION_LABELS:
                forms[word].add(form)

    outgoing: dict[str, list[tuple[str, str, str]]] = collections.defaultdict(list)
    incoming: dict[str, list[tuple[str, str, str]]] = collections.defaultdict(list)
    with args.derivational.open(encoding="utf-8") as source:
        for line in source:
            source_word, target_word, source_pos, target_pos, affix, kind = line.rstrip("\n").split("\t")
            source_word, target_word, affix = source_word.lower(), target_word.lower(), affix.lower()
            if source_word not in entries or target_word not in entries:
                continue
            if (source_word, target_word) in DENIED_RELATIONS:
                continue
            if kind == "prefix":
                valid = affix in PREFIXES and source_pos == target_pos
            else:
                valid = affix in SUFFIXES and (source_pos, target_pos) in SUFFIXES[affix][1]
            if not valid or not surface_matches(source_word, target_word, affix, kind):
                continue
            outgoing[source_word].append((target_word, affix, kind))
            incoming[target_word].append((source_word, affix, kind))

    curated = load_curated(args.contexts)
    morpholex = load_morpholex(args.morpholex)

    def known(word: str) -> bool:
        return entries[word]["rank"] < 100_000 or entries[word]["core"]

    def rank(words: set[str]) -> list[str]:
        return sorted(words, key=lambda word: (entries[word]["rank"], len(word), word))

    def morpholex_breakdown(word: str) -> tuple[str, list[str]] | None:
        if word in DENIED_MORPHOLEX or word not in morpholex:
            return None
        parts = morpholex[word]
        root_parts = [value for kind, value in parts if kind == "root"]
        if not root_parts:
            return None
        independent_roots = [root for root in root_parts if root in entries and known(root) and root not in BOUND_ROOTS]
        if len(root_parts) > 1 and len(independent_roots) != len(root_parts):
            return None
        if len(root_parts) == 1 and not independent_roots and root_parts[0] not in BOUND_ROOTS:
            return None
        rendered = []
        for kind, value in parts:
            if kind == "prefix":
                if value not in PREFIXES:
                    return None
                rendered.append(f"{value}-（{PREFIXES[value]}）")
            elif kind == "suffix":
                if value not in SUFFIXES:
                    return None
                if not word.endswith(value) and not (value == "y" and word.endswith("ly")):
                    return None
                rendered.append(f"-{value}（{SUFFIXES[value][0]}）")
            else:
                meaning = BOUND_ROOTS.get(value)
                rendered.append(f"{value}（{meaning}）" if meaning else value)
        return " + ".join(rendered), independent_roots

    output_rows = []
    advanced_total = advanced_family = 0
    for word in sorted(entries):
        lemma, inflection_code = lemma_of.get(word, (word, ""))
        evidence_source = ""
        confidence = ""
        derivations = sorted(incoming.get(lemma, []), key=lambda item: (entries[item[0]]["rank"], len(item[0]), item[0]))
        if lemma != word:
            base = lemma
            suffix = word[len(lemma):] if word.startswith(lemma) else ""
            suffix_text = f" + -{suffix}" if suffix else ""
            breakdown = f"{lemma}（{entries[lemma]['meaning']}）{suffix_text}（词形变化）"
            evidence_source, confidence = "ECDICT inflection table", "0.90"
        elif derivations:
            base, affix, kind = derivations[0]
            affix_meaning = PREFIXES[affix] if kind == "prefix" else SUFFIXES[affix][0]
            marker = f"{affix}-（{affix_meaning}）"
            breakdown = f"{marker} + {base}" if kind == "prefix" else f"{base} + -{affix}（{affix_meaning}）"
            evidence_source, confidence = "MorphyNet (2021) derivational relation", "0.85"
        else:
            base = lemma
            morpholex_result = morpholex_breakdown(word)
            breakdown = morpholex_result[0] if morpholex_result else ""
            if breakdown:
                evidence_source, confidence = "MorphoLex (2018) analysis", "0.80"

        family: list[str] = []
        tiers = [
            {target for target, _, _ in outgoing.get(word, []) if known(target)},
            {base} if base != word else set(),
            set(forms.get(lemma, set())) if lemma == word else set(),
        ]
        if lemma == word and not derivations and (morpholex_result := morpholex_breakdown(word)) and len(morpholex_result[1]) == 1:
            tiers.insert(0, set(morpholex_result[1]))
        for tier in tiers:
            for candidate in rank(tier):
                if candidate != word and candidate not in family:
                    family.append(candidate)
                if len(family) == 3:
                    break
            if len(family) == 3:
                break

        if word in curated:
            breakdown, curated_family, context = curated[word]
            family = [candidate for candidate in curated_family.split(",") if candidate and candidate != word]
            # Curated hints do not yet contain per-item external citations.
            # Do not claim a source or surface them until that evidence exists.
            evidence_source, confidence = "", ""
        else:
            context = ""
        output_rows.append((word, breakdown, ",".join(family[:3]), context, evidence_source, confidence))
        tags = entries[word]["tags"]
        if "cet6" in tags and "cet4" not in tags and len(word) >= 6:
            advanced_total += 1
            advanced_family += bool(family)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8", newline="") as target:
        writer = csv.writer(target, delimiter="\t", lineterminator="\n")
        writer.writerow(("# word", "breakdown", "family", "context hint", "evidence source", "confidence"))
        writer.writerows(output_rows)

    coverage = advanced_family / advanced_total if advanced_total else 0
    print(f"entries={len(output_rows)} advanced={advanced_total} family_coverage={coverage:.3%}")

    if args.audit_sample:
        population = sorted(
            word for word, entry in entries.items()
            if "cet6" in entry["tags"] and "cet4" not in entry["tags"] and len(word) >= 6
        )
        sample = sorted(random.Random(20260716).sample(population, min(args.audit_size, len(population))))
        args.audit_sample.parent.mkdir(parents=True, exist_ok=True)
        args.audit_sample.write_text("\n".join(sample) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
