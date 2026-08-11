# -*- coding: utf-8 -*-
"""
Sinh lại BuyPrice / UnlockLevel trong `MarketDatabase.asset` từ `MarketPriceTable.cs`.

VÌ SAO cần script: MarketDatabase.asset là bản NƯỚNG SẴN của bảng giá (BuyPrice =
BasePrice × 1,5). Sửa bảng giá mà không sinh lại asset thì bảng tin chợ vẫn bày giá cũ
trong khi quầy hàng và đơn hàng đã dùng giá mới — hai con số cho cùng một vật phẩm.
Trong Unity có menu Tools/Farm/Chợ/2 làm việc này; script chỉ là bản chạy ngoài Unity.

Làm tròn dùng round-half-to-even ĐÚNG như UnityEngine.Mathf.RoundToInt.
"""
import io, os, re, sys

ROOT  = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TABLE = os.path.join(ROOT, "Assets", "_Game", "Farm", "Scripts", "Market", "MarketPriceTable.cs")
ASSET = os.path.join(ROOT, "Assets", "_Game", "Farm", "data", "Market", "MarketDatabase.asset")

src = io.open(TABLE, encoding="utf-8").read()
rows = {}
pat = re.compile(r'Add\(\s*"([^"]+)"\s*,\s*"[^"]*"\s*,\s*MarketCategory\.(\w+)\s*,\s*(-?\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*(?:marketEnabled:\s*)?(true|false)\s*)?\)')
for m in pat.finditer(src):
    rows[m.group(1)] = dict(base=int(m.group(3)), lv=int(m.group(4)),
                            weight=int(m.group(5)), enabled=(m.group(6) != "false"))
print("đọc được", len(rows), "dòng từ MarketPriceTable")

def round_half_even(x):
    import decimal
    return int(decimal.Decimal(x).quantize(0, rounding=decimal.ROUND_HALF_EVEN))

text  = io.open(ASSET, encoding="utf-8").read()
lines = text.split("\n")

changed = 0
missing = []
i = 0
seen = set()
while i < len(lines):
    m = re.match(r'^  - ItemID: (.*)$', lines[i])
    if not m:
        i += 1; continue
    iid = m.group(1).strip()
    seen.add(iid)
    if iid not in rows:
        missing.append(iid); i += 1; continue
    want_buy = max(1, round_half_even(rows[iid]["base"] * 1.5))
    want_lv  = rows[iid]["lv"]
    for j in range(i + 1, min(i + 9, len(lines))):
        mb = re.match(r'^(    BuyPrice: )(-?\d+)$', lines[j])
        if mb and int(mb.group(2)) != want_buy:
            lines[j] = mb.group(1) + str(want_buy); changed += 1
        ml = re.match(r'^(    UnlockLevel: )(-?\d+)$', lines[j])
        if ml and int(ml.group(2)) != want_lv:
            lines[j] = ml.group(1) + str(want_lv); changed += 1
        if re.match(r'^  - ItemID: ', lines[j]): break
    i += 1

print("số dòng asset:", len(seen), "· số giá trị sửa:", changed)
if missing: print("có trong asset mà KHÔNG có trong bảng giá:", missing)
notin = [k for k, v in rows.items() if v["enabled"] and k not in seen]
if notin: print("bật ở bảng giá mà THIẾU trong asset (cần mở Unity sinh lại):", notin)

if "--write" in sys.argv:
    io.open(ASSET, "w", encoding="utf-8", newline="").write("\n".join(lines))
    print("ĐÃ GHI", ASSET)
else:
    print("(dry-run — thêm --write để ghi)")
