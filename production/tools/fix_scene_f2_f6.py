# -*- coding: utf-8 -*-
"""
F2 + F6 — hai vết sửa nhỏ trong SCN_Farm.unity.

F2: `dailyMissionDatabase: {fileID: 0}` ở CẢ HAI popup nhiệm vụ (UnifiedTaskPopupUI và
    EwarPopup) → tab "Nhiệm vụ hằng ngày" trống rỗng. Gán MissionDatabase_Daily.asset
    (guid cf0e0d125e10a44409b6ca7039f17474) — asset đã có sẵn từ đầu, chỉ là chưa ai kéo vào.

F6: 3 nút trong `popup_SKPhucLoi` (btn_PhucLoiNap, btn_GoiQGioVang, btn_DiemDanh (3)) mỗi
    nút có HAI lời gọi SetActive, trong đó lời gọi ĐẦU có `m_Target: {fileID: 0}` → bấm
    không làm gì. Popup này chỉ có ĐÚNG MỘT panel bên phải (`ObjectBtnPhucLoiNap`), tức là
    không có object nào để gán vào. Nên XOÁ lời gọi rỗng (đúng lựa chọn "gán hoặc xoá"),
    giữ lại lời gọi thật. Xoá đúng 12 dòng của một mục m_Calls, KHÔNG chạm object nào.
"""
import io, os, re, sys

ROOT  = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCENE = os.path.join(ROOT, "Assets", "_Game", "Scenes", "SCN_Farm.unity")

DAILY_DB = "{fileID: 11400000, guid: cf0e0d125e10a44409b6ca7039f17474, type: 2}"
PLOTCTRL = "0599daee45716234d97eb4adc0e092d6"

def stats(lines):
    t = "\n".join(lines)
    return {
        "blocks":         len(re.findall(r'^--- !u!', t, re.M)),
        "GameObject":     len(re.findall(r'^GameObject:$', t, re.M)),
        "Transform":      len(re.findall(r'^Transform:$', t, re.M)),
        "RectTransform":  len(re.findall(r'^RectTransform:$', t, re.M)),
        "PrefabInstance": len(re.findall(r'^PrefabInstance:$', t, re.M)),
        "MonoBehaviour":  len(re.findall(r'^MonoBehaviour:$', t, re.M)),
        "Button(onClick)": len(re.findall(r'^  m_OnClick:$', t, re.M)),
        "PlotController": t.count(PLOTCTRL),
        "lines":          len(lines),
    }

lines  = io.open(SCENE, encoding="utf-8").read().split("\n")
before = stats(lines)

# ── F2 ────────────────────────────────────────────────────────────────────────
f2 = 0
for i, l in enumerate(lines):
    if l == "  dailyMissionDatabase: {fileID: 0}":
        lines[i] = "  dailyMissionDatabase: " + DAILY_DB
        f2 += 1

# ── F6 ────────────────────────────────────────────────────────────────────────
# Mục m_Calls rỗng luôn là 12 dòng, bắt đầu bằng "      - m_Target: {fileID: 0}" và
# kết thúc bằng "        m_CallState: 2". Chỉ xoá khi mục NGAY SAU nó cũng là m_Target
# (tức nút vẫn còn một lời gọi thật) — không bao giờ để một nút thành 0 lời gọi.
EMPTY = [
    "      - m_Target: {fileID: 0}",
    "        m_TargetAssemblyTypeName: UnityEngine.GameObject, UnityEngine",
    "        m_MethodName: SetActive",
    "        m_Mode: 6",
    "        m_Arguments:",
    "          m_ObjectArgument: {fileID: 0}",
    "          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine",
    "          m_IntArgument: 0",
    "          m_FloatArgument: 0",
    "          m_StringArgument: ",
    "          m_BoolArgument: 0",
    "        m_CallState: 2",
]
f6 = 0
i = 0
out = []
while i < len(lines):
    if lines[i:i + len(EMPTY)] == EMPTY:
        nxt = i + len(EMPTY)
        prev_is_calls = i >= 1 and lines[i - 1].strip() == "m_Calls:"
        next_is_call  = nxt < len(lines) and lines[nxt].startswith("      - m_Target: {fileID: ") \
                        and lines[nxt] != "      - m_Target: {fileID: 0}"
        if prev_is_calls and next_is_call:
            i = nxt        # bỏ qua 12 dòng = xoá mục rỗng
            f6 += 1
            continue
    out.append(lines[i]); i += 1
lines = out

after = stats(lines)
print("F2 · gán dailyMissionDatabase :", f2)
print("F6 · xoá lời gọi SetActive rỗng:", f6)
print()
print("%-17s %10s %10s" % ("", "TRƯỚC", "SAU"))
for k in before:
    flag = "" if before[k] == after[k] else "  <== ĐỔI"
    print("%-17s %10d %10d%s" % (k, before[k], after[k], flag))

ok = (f2 == 2 and f6 == 3)
for k in ("blocks","GameObject","Transform","RectTransform","PrefabInstance","MonoBehaviour","Button(onClick)","PlotController"):
    if before[k] != after[k]:
        print("\nSỐ OBJECT ĐỔI (%s) — KHÔNG GHI" % k); ok = False
if after["lines"] != before["lines"] - 3 * 12:
    print("\nSỐ DÒNG XOÁ KHÔNG ĐÚNG 36 — KHÔNG GHI"); ok = False
if not ok: sys.exit(1)

if "--write" in sys.argv:
    io.open(SCENE, "w", encoding="utf-8", newline="").write("\n".join(lines))
    print("\nĐÃ GHI", SCENE)
else:
    print("\n(dry-run — thêm --write để ghi)")
