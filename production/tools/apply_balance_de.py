# -*- coding: utf-8 -*-
"""
Nhóm D + E — ghi bảng số đã chốt vào asset.

Cây trồng (giây THẬT vì realTimeMultiplier = 1.0):
  profit = harvestAmount(4) x sellGold - goldPrice(giá hạt)
  Nguyên tắc: lãi/giây KHÔNG GIẢM khi cấp tăng, và không cây nào lỗ.
  expReward = growSeconds/10 -> mọi cây cho 0.1 EXP/giây, cây cấp 10 không còn
  "lâu gấp 13 lần mà EXP y như lúa".
"""
import io, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
D = os.path.join(ROOT, "Assets", "_Game", "Farm", "data")

# file, cropId, cấp, growSeconds, sellGold, goldPrice(hạt)
CROPS = [
    ("Hat_giong/Crop_Rice.asset",   "rice",             1,  50,  7,  20),
    ("Hạt Hoa/HuongDuong.asset",    "huong_duong",      1,  55,  8,  23),
    ("Hat_giong/BapCai.asset",      "bapcai",           1,  70, 10,  28),
    ("Hat_giong/Ngo.asset",         "ngo",              2,  95, 13,  35),
    ("Hat_giong/Ca_Rot.asset",      "carot",            3, 120, 17,  45),
    ("Hat_giong/CaChua.asset",      "cachua",           3, 145, 20,  52),
    ("Hạt Hoa/HoaHong.asset",       "hoa_hong",         4, 170, 23,  57),
    ("Hạt Hoa/HoaOaiHuong.asset",   "hoa_oai_huong",    4, 195, 27,  67),
    ("Hat_giong/Khoai_Tay.asset",   "khoaitay",         5, 220, 30,  71),
    ("Hat_giong/nam.asset",         "nam",              6, 250, 34,  76),
    ("Hạt Hoa/HoaLan.asset",        "hoa_lan",          7, 280, 38,  80),
    ("Hạt Hoa/HoaCucTrang.asset",   "hoa_cuc_trang",    7, 310, 42,  88),
    ("Hat_giong/Mia.asset",         "sugarcane",        7, 340, 46,  96),
    ("Hat_giong/chanh.asset",       "lemon",            8, 380, 52, 105),
    ("Hạt Hoa/Tulip.asset",         "tulip",            9, 420, 57, 107),
    ("Hạt Hoa/HoaCucVanTho.asset",  "hoa_cuc_van_tho",  9, 460, 63, 119),
    ("Hat_giong/Ot.asset",          "chili",            9, 500, 68, 127),
    ("Hat_giong/caytieu.asset",     "pepper",          10, 560, 76, 134),
    ("Hạt Hoa/HoaMauDon.asset",     "hoa_mau_don",     10, 600, 81, 141),
    ("Hạt Hoa/HoaCamTuCau.asset",   "hoa_cam_tu_cau",  10, 650, 88, 152),
    ("Hạt Hoa/HoaAnhThao.asset",    "hoa_anh_thao",    10, 700, 95, 164),
]

# file, penId, feedDuration, foodAmountPerFeed, food1, food2, productAmount, secondAmount, exp
PENS = [
    ("PenConfig/Config_Pen03_Ga.asset",     "pen_03",  90, 2, "rice",  "ngo",       1, 1, 11),
    ("PenConfig/Config_Pen02_Heo.asset",    "pen_02", 150, 2, "bapcai","carot",     1, 1, 19),
    ("PenConfig/Config_Pen01_BoThit.asset", "pen_01", 240, 3, "ngo",   "cachua",    1, 1, 30),
    ("PenConfig/Config_Pen04_BoSua.asset",  "pen_04", 300, 3, "carot", "khoaitay",  2, 1, 38),
    ("Farm_May_Che_Bien/Config_May01_XayBot.asset", "may_01", 360, 1, "rice",      "", 2, 1, 45),
    ("Farm_May_Che_Bien/Config_May02_EpMia.asset",  "may_02", 420, 1, "sugarcane", "", 2, 1, 53),
    ("Farm_May_Che_Bien/Config_May03_PhoMai.asset", "may_03", 480, 1, "milk",      "", 2, 1, 60),
]

def set_scalar(text, key, value, path):
    pat = re.compile(r'^(  %s:)[ \t]*(.*)$' % re.escape(key), re.M)
    if not pat.search(text):
        print("  THIẾU key %s trong %s" % (key, path)); sys.exit(1)
    v = str(value)
    return pat.sub(lambda m: m.group(1) + ((" " + v) if v != "" else ""), text, count=1)

def check(text, key, expect, path):
    m = re.search(r'^  %s:[ \t]*(.*)$' % re.escape(key), text, re.M)
    if m is None or m.group(1).strip() != expect:
        print("  KHÔNG KHỚP %s: %r != %r (%s)" % (key, m.group(1).strip() if m else None, expect, path))
        sys.exit(1)

write = "--write" in sys.argv
print("=" * 78)
print("CÂY TRỒNG — %-16s %5s %5s %5s %5s %5s %7s" % ("cropId","cấp","giây","bán","hạt","lãi","lãi/giây"))
prev = -1.0
for rel, cid, lv, grow, sell, seed in CROPS:
    p = os.path.join(D, rel)
    t = io.open(p, encoding="utf-8").read()
    check(t, "cropId", cid, rel)
    check(t, "unlockLevel", str(lv), rel)
    exp = max(5, round(grow / 10))
    t = set_scalar(t, "growSeconds", grow, rel)
    t = set_scalar(t, "sellGold",    sell, rel)
    t = set_scalar(t, "goldPrice",   seed, rel)
    t = set_scalar(t, "expReward",   exp,  rel)
    profit = 4 * sell - seed
    pps = profit / grow
    warn = ""
    if profit <= 0: warn = "  <== LỖ!"
    if pps + 1e-9 < prev: warn += "  <== LÃI/GIÂY TỤT!"
    prev = pps
    print("             %-16s %5d %5d %5d %5d %5d %7.4f  exp %d%s" % (cid, lv, grow, sell, seed, profit, pps, exp, warn))
    if warn: sys.exit(1)
    if write: io.open(p, "w", encoding="utf-8", newline="").write(t)

print()
print("CHUỒNG/MÁY — %-8s %6s %5s %-22s %s" % ("penId","giây","ăn","thức ăn","sản phẩm"))
for rel, pid, dur, amt, f1, f2, pa, spa, exp in PENS:
    p = os.path.join(D, rel)
    t = io.open(p, encoding="utf-8").read()
    check(t, "penId", pid, rel)
    t = set_scalar(t, "feedDurationSeconds", dur, rel)
    t = set_scalar(t, "food1ItemId", f1, rel)
    t = set_scalar(t, "food2ItemId", f2, rel)
    t = set_scalar(t, "productAmount", pa, rel)
    t = set_scalar(t, "secondProductAmount", spa, rel)
    t = set_scalar(t, "expReward", exp, rel)
    # foodAmountPerFeed là field MỚI -> chèn ngay trước feedDurationSeconds nếu chưa có
    if re.search(r'^  foodAmountPerFeed: ', t, re.M):
        t = set_scalar(t, "foodAmountPerFeed", amt, rel)
    else:
        t = re.sub(r'^(  feedDurationSeconds: )', "  foodAmountPerFeed: %d\n\\1" % amt, t, count=1, flags=re.M)
    prod = re.search(r'^  productItemId:[ \t]*(.*)$', t, re.M).group(1).strip()
    sec  = re.search(r'^  secondProductItemId:[ \t]*(.*)$', t, re.M).group(1).strip()
    print("             %-8s %6d %5d %-22s %s x%d%s" % (pid, dur, amt, f1 + ("/" + f2 if f2 else ""),
          prod, pa, (" + " + sec + " x%d" % spa) if sec else ""))
    if write: io.open(p, "w", encoding="utf-8", newline="").write(t)

print()
print("ĐÃ GHI" if write else "(dry-run — thêm --write để ghi)")
