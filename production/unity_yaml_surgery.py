#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Dao mo file YAML cua Unity (prefab / scene) o muc DOCUMENT.

Dung de go component + xoa cay GameObject ma KHONG mo Unity.
Moi buoc deu kiem tra lai: sau khi xoa, khong duoc con tham chieu treo.
"""
import re
import sys

DOC_RE = re.compile(r'^--- !u!(\d+) &(\d+)( stripped)?\s*$')


class Doc:
    __slots__ = ("classid", "anchor", "stripped", "lines")

    def __init__(self, classid, anchor, stripped, lines):
        self.classid = classid
        self.anchor = anchor
        self.stripped = stripped
        self.lines = lines          # gom ca dong '--- !u!...'

    @property
    def body(self):
        return "\n".join(self.lines)

    def field(self, name):
        pat = re.compile(r'^\s{0,4}' + re.escape(name) + r':\s*(.*)$')
        for ln in self.lines:
            m = pat.match(ln)
            if m:
                return m.group(1).strip()
        return None

    def ref_list(self, header):
        """Doc danh sach '{fileID: N}' nam duoi mot header nhu m_Children / m_Component."""
        out = []
        grabbing = False
        for ln in self.lines:
            if re.match(r'^\s{0,4}' + re.escape(header) + r':\s*$', ln):
                grabbing = True
                continue
            if grabbing:
                m = re.match(r'^\s*- (?:component: )?\{fileID: (-?\d+)\}\s*$', ln)
                if m:
                    out.append(m.group(1))
                    continue
                if re.match(r'^\s*-', ln):
                    continue
                grabbing = False
        return out


def parse(path):
    with open(path, "r", encoding="utf-8") as f:
        raw = f.read()
    newline = "\r\n" if "\r\n" in raw else "\n"
    lines = raw.replace("\r\n", "\n").split("\n")

    header, docs, cur = [], [], None
    for ln in lines:
        m = DOC_RE.match(ln)
        if m:
            if cur:
                docs.append(cur)
            cur = Doc(m.group(1), m.group(2), bool(m.group(3)), [ln])
        elif cur is not None:
            cur.lines.append(ln)
        else:
            header.append(ln)
    if cur:
        docs.append(cur)
    return header, docs, newline


def write(path, header, docs, newline):
    parts = list(header)
    for d in docs:
        parts.extend(d.lines)
    out = "\n".join(parts)
    if not out.endswith("\n"):
        out += "\n"
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(out.replace("\n", newline))


def build_index(docs):
    by_anchor = {d.anchor: d for d in docs}
    go_of_transform = {}
    transform_of_go = {}
    for d in docs:
        if d.classid in ("4", "224"):          # Transform / RectTransform
            go = d.field("m_GameObject")
            if go:
                mm = re.search(r'fileID: (-?\d+)', go)
                if mm:
                    go_of_transform[d.anchor] = mm.group(1)
                    transform_of_go[mm.group(1)] = d.anchor
    return by_anchor, go_of_transform, transform_of_go


def collect_subtree(go_anchor, by_anchor, transform_of_go):
    """Tra ve tap anchor cua ca cay: GameObject + moi component + moi con."""
    doomed = set()
    stack = [go_anchor]
    while stack:
        go = stack.pop()
        if go in doomed:
            continue
        d = by_anchor.get(go)
        if d is None:
            continue
        doomed.add(go)
        for comp in d.ref_list("m_Component"):
            doomed.add(comp)
        tr = transform_of_go.get(go)
        if tr and tr in by_anchor:
            for child_tr in by_anchor[tr].ref_list("m_Children"):
                cd = by_anchor.get(child_tr)
                if cd is None:
                    doomed.add(child_tr)
                    continue
                gmm = re.search(r'fileID: (-?\d+)', cd.field("m_GameObject") or "")
                if gmm:
                    stack.append(gmm.group(1))
                else:
                    doomed.add(child_tr)
    return doomed


def scrub(docs, doomed, verbose=True):
    """Xoa doc bi diet + don sach moi tham chieu con tro toi chung."""
    kept = [d for d in docs if d.anchor not in doomed]

    ref_line = re.compile(r'^\s*- (?:component: )?\{fileID: (-?\d+)\}\s*$')
    dead_ref = 0
    dangling = 0
    for d in kept:
        new_lines = []
        for ln in d.lines:
            m = ref_line.match(ln)
            if m and m.group(1) in doomed:
                dead_ref += 1
                continue                       # bo hang trong danh sach
            # tham chieu le -> tra ve 0 (Unity hieu la "khong gan")
            def repl(mm):
                nonlocal dangling
                if mm.group(1) in doomed:
                    dangling += 1
                    return "fileID: 0"
                return mm.group(0)
            ln = re.sub(r'fileID: (-?\d+)', repl, ln)
            new_lines.append(ln)
        d.lines = new_lines

    if verbose:
        print(f"    xoa {len(docs) - len(kept)} doc | go {dead_ref} muc danh sach | "
              f"{dangling} tham chieu le -> 0")
    return kept


def verify(docs):
    """Khong con anchor nao duoc tro toi ma khong ton tai."""
    alive = {d.anchor for d in docs}
    bad = set()
    for d in docs:
        for mm in re.finditer(r'fileID: (-?\d+)(?!,)', d.body):
            v = mm.group(1)
            if v == "0":
                continue
            # bo qua tham chieu toi asset ngoai (co kem guid tren cung dong)
            line_start = d.body.rfind("\n", 0, mm.start()) + 1
            line_end = d.body.find("\n", mm.end())
            line = d.body[line_start: line_end if line_end > 0 else len(d.body)]
            if "guid:" in line:
                continue
            if v not in alive:
                bad.add((d.anchor, v))
    return bad
