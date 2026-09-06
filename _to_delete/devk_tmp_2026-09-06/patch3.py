# -*- coding: utf-8 -*-
import hashlib, sys
P = "Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs"
raw = open(P, "rb").read(); txt = raw.decode("utf-8")

OLD = u"""    private const float MERGED_ROW_GAP   = 10f;   // khoảng cách dọc giữa 2 hàng\r\n"""
NEW = (u"""    // [V7 — 2026-09-06] 10f → 62f. Flow-layout tính chiều cao hàng CHỈ bằng chiều cao ô
    // (sizes[i].y = 190), KHÔNG kể bảng chữ treo dưới ô. Bảng chữ nay cao
    // CAPTION_GAP_Y(4) + CAPTION_H(52) = 56px, nên khe 10px cũ khiến nhãn hàng trên đâm
    // thẳng vào ô hàng dưới. 62 > 56 → hết chồng. Chỉ ảnh hưởng khi dải phải xuống 2 hàng;
    // ca 1 hàng (ảnh Sếp chụp: 10 ô một hàng) không đổi gì.
    private const float MERGED_ROW_GAP   = 62f;   // khoảng cách dọc giữa 2 hàng (chừa bảng chữ)\r\n""").replace("\n", "\r\n").replace("\r\r\n", "\r\n")

if txt.count(OLD) != 1:
    print("FAIL count=%d" % txt.count(OLD)); sys.exit(1)
txt = txt.replace(OLD, NEW, 1)
out = txt.encode("utf-8")
open(P, "wb").write(out)
print("WROTE md5=%s crlf=%d lf=%d" % (hashlib.md5(out).hexdigest(), out.count(b"\r\n"), out.count(b"\n")))
