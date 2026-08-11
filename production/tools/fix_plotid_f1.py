# -*- coding: utf-8 -*-
"""
F1 — Cấp lại plotId DUY NHẤT cho 38 PlotController trong SCN_Farm.unity.

VÌ SAO phải sửa: SaveKey của PlotController là "PLOT_NORMAL_{plotId}" (không có
category trong khoá). Trong scene hiện có 8 cặp trùng plotId (normal 1..6 và
flower 26, 27) => hai ô đất khác nhau ghi/đọc CÙNG MỘT khoá PlayerPrefs.
Trồng ô này, thoát vào lại thì ô kia hiện cây => mất dữ liệu.

VÌ SAO chọn dải 101..108 cho 8 ô bị đổi: PlacementManager.GetNextPlotId() =
max(plotId trong scene) + 1. Trước khi sửa max = 30, nên người chơi ĐANG CÓ SAVE
đã được cấp id 31, 32, 33... cho các ô đất họ mua. Nếu đặt id mới vào 31..38 thì
đè lên chính save của họ. 101..108 nằm ngoài vùng đó và vẫn < 200 (giới hạn của
PlotController.DebugClearData).

Script chỉ SỬA GIÁ TRỊ tại chỗ + THÊM 1 mục modification, KHÔNG xoá object nào.
"""
import io, re, sys, os

SCENE = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Scenes", "SCN_Farm.unity")
SCENE = os.path.abspath(SCENE)

PLOT_PREFAB_GUID   = "81b3515210adc2f44a33581bc3a6d843"   # Plot_01.prefab
FLOWER_PREFAB_GUID = "4beadf9ad27192142b4e968377f37368"   # Chauhoa_1.prefab
PLOT_MB_FILEID     = "5737610210004506391"                # PlotController trong Plot_01.prefab
FLOWER_MB_FILEID   = "7632391457620492841"                # PlotController trong Chauhoa_1.prefab
PLOTCONTROLLER_GUID = "0599daee45716234d97eb4adc0e092d6"

# prefabInstance anchor -> (id cũ, id mới, guid prefab, fileID component trong prefab)
REMAP = {
    162360093:  (2,  101, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),
    757718900:  (3,  102, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),
    549963596:  (4,  103, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),
    1346891834: (5,  104, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),
    1475242445: (6,  105, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),
    1711578162: (1,  106, PLOT_PREFAB_GUID,   PLOT_MB_FILEID),    # chưa có mod plotId -> phải THÊM
    1434509900: (26, 107, FLOWER_PREFAB_GUID, FLOWER_MB_FILEID),
    501516254:  (27, 108, FLOWER_PREFAB_GUID, FLOWER_MB_FILEID),
}

# Xoá stale removed-component: {fileID: 5492468578113693702} không còn tồn tại trong
# Plot_01.prefab (prefab đã được tạo lại, fileID đổi) => PlotController gốc của prefab
# KHÔNG bị xoá, cộng với component được add thêm (&837023709) => MỘT GameObject có
# HAI PlotController cùng plotId 1. Trỏ lại đúng fileID hiện tại để Unity xoá thật.
STALE_REMOVED = "5492468578113693702"

def block_ranges(lines):
    """trả về list (start_idx, end_idx_exclusive, classid, anchor)"""
    out = []
    cur = None
    for i, l in enumerate(lines):
        m = re.match(r'^--- !u!(\d+) &(\d+)', l)
        if m:
            if cur: out.append((cur[0], i, cur[1], cur[2]))
            cur = (i, int(m.group(1)), int(m.group(2)))
    if cur: out.append((cur[0], len(lines), cur[1], cur[2]))
    return out

def count_stats(lines):
    txt = "\n".join(lines)
    return {
        "blocks":         len(re.findall(r'^--- !u!', txt, re.M)),
        "GameObject":     len(re.findall(r'^GameObject:$', txt, re.M)),
        "Transform":      len(re.findall(r'^Transform:$', txt, re.M)),
        "PrefabInstance": len(re.findall(r'^PrefabInstance:$', txt, re.M)),
        "MonoBehaviour":  len(re.findall(r'^MonoBehaviour:$', txt, re.M)),
        "SpriteRenderer": len(re.findall(r'^SpriteRenderer:$', txt, re.M)),
        "PlotController": txt.count(PLOTCONTROLLER_GUID),
        "lines":          len(lines),
    }

def main():
    raw = io.open(SCENE, encoding="utf-8", errors="strict").read()
    lines = raw.split("\n")
    before = count_stats(lines)

    blocks = block_ranges(lines)
    byanchor = {b[3]: b for b in blocks}

    changed = 0
    added   = 0

    for anchor, (old_id, new_id, guid, mb_fileid) in REMAP.items():
        blk = byanchor.get(anchor)
        if blk is None:
            print("LỖI: không thấy PrefabInstance", anchor); sys.exit(1)
        s, e = blk[0], blk[1]
        # tìm mục modification plotId trong block
        found = False
        for i in range(s, e):
            if lines[i].strip() == "propertyPath: plotId":
                # dòng trước phải là target đúng component
                if mb_fileid not in lines[i-1]:
                    continue
                vline = i + 1
                m = re.match(r'^(\s*value: )(-?\d+)\s*$', lines[vline])
                if not m:
                    print("LỖI: value lạ ở dòng", vline+1, repr(lines[vline])); sys.exit(1)
                if int(m.group(2)) != old_id:
                    print("LỖI: id cũ không khớp ở", anchor, m.group(2), "!=", old_id); sys.exit(1)
                lines[vline] = m.group(1) + str(new_id)
                found = True
                changed += 1
                break
        if not found:
            # chưa có mod plotId -> thêm ngay SAU dòng "m_Modifications:"
            ins = None
            for i in range(s, e):
                if lines[i].strip() == "m_Modifications:":
                    ins = i + 1
                    break
            if ins is None:
                print("LỖI: không thấy m_Modifications ở", anchor); sys.exit(1)
            lines[ins:ins] = [
                "    - target: {fileID: %s, guid: %s, type: 3}" % (mb_fileid, guid),
                "      propertyPath: plotId",
                "      value: %d" % new_id,
                "      objectReference: {fileID: 0}",
            ]
            added += 1
            # block ranges lệch đi sau khi chèn -> tính lại
            blocks = block_ranges(lines)
            byanchor = {b[3]: b for b in blocks}

    # sửa stale removed-component (chỉ dòng trong m_RemovedComponents, KHÔNG sửa dòng target)
    fixed_removed = 0
    for i, l in enumerate(lines):
        if l.strip() == "- {fileID: %s, guid: %s, type: 3}" % (STALE_REMOVED, PLOT_PREFAB_GUID):
            lines[i] = l.replace(STALE_REMOVED, PLOT_MB_FILEID)
            fixed_removed += 1

    after = count_stats(lines)

    print("sửa plotId có sẵn :", changed)
    print("thêm mod plotId   :", added)
    print("sửa removedComp   :", fixed_removed)
    print()
    print("%-16s %10s %10s" % ("", "TRƯỚC", "SAU"))
    for k in before:
        flag = "" if before[k] == after[k] else "  <== ĐỔI"
        print("%-16s %10d %10d%s" % (k, before[k], after[k], flag))

    if changed != 7 or added != 1 or fixed_removed != 1:
        print("\nSỐ LƯỢNG KHÔNG ĐÚNG — KHÔNG GHI FILE"); sys.exit(1)
    for k in ("blocks","GameObject","Transform","PrefabInstance","MonoBehaviour","SpriteRenderer","PlotController"):
        if before[k] != after[k]:
            print("\nSỐ OBJECT THAY ĐỔI (%s) — KHÔNG GHI FILE" % k); sys.exit(1)

    if "--write" in sys.argv:
        io.open(SCENE, "w", encoding="utf-8", newline="").write("\n".join(lines))
        print("\nĐÃ GHI", SCENE)
    else:
        print("\n(dry-run — thêm --write để ghi)")

main()
