# -*- coding: utf-8 -*-
"""Kiểm tra kinh tế sau khi cân bằng: ruộng vs chuồng vs máy."""
import io, os, re
D = "Assets/_Game/Farm/data"
def crop(f):
    t = io.open(os.path.join(D, f), encoding="utf-8").read()
    g = lambda k: int(re.search(r'^  %s: (-?\d+)' % k, t, re.M).group(1))
    name = re.search(r'^  itemName: (.*)$', t, re.M).group(1).strip().strip('"')
    return dict(name=name, lv=g("unlockLevel"), t=g("growSeconds"), sell=g("sellGold"),
                seed=g("goldPrice"), amt=g("harvestAmount"), exp=g("expReward"),
                hid=re.search(r'^  harvestItemId: (.*)$', t, re.M).group(1).strip())
FILES = ["Hat_giong/Crop_Rice.asset","Hạt Hoa/HuongDuong.asset","Hat_giong/BapCai.asset",
"Hat_giong/Ngo.asset","Hat_giong/Ca_Rot.asset","Hat_giong/CaChua.asset","Hạt Hoa/HoaHong.asset",
"Hạt Hoa/HoaOaiHuong.asset","Hat_giong/Khoai_Tay.asset","Hat_giong/nam.asset","Hạt Hoa/HoaLan.asset",
"Hạt Hoa/HoaCucTrang.asset","Hat_giong/Mia.asset","Hat_giong/chanh.asset","Hạt Hoa/Tulip.asset",
"Hạt Hoa/HoaCucVanTho.asset","Hat_giong/Ot.asset","Hat_giong/caytieu.asset","Hạt Hoa/HoaMauDon.asset",
"Hạt Hoa/HoaCamTuCau.asset","Hạt Hoa/HoaAnhThao.asset"]

# giá gốc lấy từ MarketPriceTable (nguồn sự thật cho sản phẩm chuồng/máy)
tbl = io.open("Assets/_Game/Farm/Scripts/Market/MarketPriceTable.cs", encoding="utf-8").read()
PRICE = {m.group(1): int(m.group(2)) for m in
         re.finditer(r'Add\(\s*"([^"]+)"\s*,\s*"[^"]*"\s*,\s*MarketCategory\.\w+\s*,\s*(-?\d+)', tbl)}

print("── RUỘNG " + "─"*70)
print("%-16s %3s %5s %5s %5s %6s %8s %7s %8s" % ("cây","cấp","giây","bán","hạt","lãi","lãi/giây","exp","exp/giây"))
prev=-1; bad=[]
for f in FILES:
    c = crop(f)
    profit = c["amt"]*c["sell"] - c["seed"]
    pps = profit/c["t"]; eps = c["exp"]/c["t"]
    flag = ""
    if profit <= 0: flag += " LỖ!"; bad.append(c["name"])
    if pps + 1e-9 < prev: flag += " TỤT!"; bad.append(c["name"])
    prev = pps
    mk = PRICE.get(c["hid"])
    if mk != c["sell"]: flag += f" LỆCH-BẢNG-GIÁ({mk})"; bad.append(c["name"])
    print("%-16s %3d %5d %5d %5d %6d %8.4f %7d %8.4f%s" % (c["name"],c["lv"],c["t"],c["sell"],c["seed"],profit,pps,c["exp"],eps,flag))

def pen(f):
    t = io.open(os.path.join(D, f), encoding="utf-8").read()
    g  = lambda k: re.search(r'^  %s:[ \t]*(.*)$' % k, t, re.M).group(1).strip()
    gi = lambda k: int(float(g(k)))
    return dict(id=g("penId"), dur=gi("feedDurationSeconds"), food=gi("foodAmountPerFeed"),
                f1=g("food1ItemId"), f2=g("food2ItemId"), p=g("productItemId"),
                pa=gi("productAmount"), s=g("secondProductItemId"), sa=gi("secondProductAmount"),
                exp=gi("expReward"))
PENS = [("PenConfig/Config_Pen03_Ga.asset","Chuồng Gà",2),("PenConfig/Config_Pen02_Heo.asset","Chuồng Heo",4),
        ("PenConfig/Config_Pen01_BoThit.asset","Chuồng Bò",6),("PenConfig/Config_Pen04_BoSua.asset","Chuồng Bò Sữa",8),
        ("Farm_May_Che_Bien/Config_May01_XayBot.asset","Máy Xay Bột",11),
        ("Farm_May_Che_Bien/Config_May02_EpMia.asset","Máy Ép Mía",13),
        ("Farm_May_Che_Bien/Config_May03_PhoMai.asset","Máy Phô Mai",15)]
# lãi/giây ruộng theo cấp (dùng để so tỷ lệ)
plot_pps = {}
for f in FILES:
    c = crop(f); plot_pps.setdefault(c["lv"], []).append((c["amt"]*c["sell"]-c["seed"])/c["t"])
def ruong(lv):
    ks=[k for k in plot_pps if k<=lv]
    return max(sum(plot_pps[k])/len(plot_pps[k]) for k in ks) if ks else 0.2

print()
print("── CHUỒNG / MÁY " + "─"*62)
print("%-14s %3s %5s %4s %-18s %6s %7s %8s %6s %8s" % ("chuồng","cấp","giây","ăn","thức ăn(rẻ nhất)","thu","lãi","lãi/giây","×ruộng","exp/giây"))
for f, name, lv in PENS:
    p = pen(f)
    costs=[PRICE.get(p["f1"],0)]
    if p["f2"]: costs.append(PRICE.get(p["f2"],0))
    cost = min(costs)*p["food"]
    rev  = PRICE.get(p["p"],0)*p["pa"] + (PRICE.get(p["s"],0)*p["sa"] if p["s"] else 0)
    profit = rev - cost; pps = profit/p["dur"]
    ratio = pps/ruong(lv)
    flag = " LỖ!" if profit<=0 else (" QUÁ CAO!" if ratio>4 else "")
    if flag: bad.append(name)
    print("%-14s %3d %5d %4d %-18s %6d %7d %8.4f %6.2f %8.4f%s" % (name,lv,p["dur"],p["food"],
          (p["f1"]+"/"+p["f2"]) if p["f2"] else p["f1"], rev, profit, pps, ratio, p["exp"]/p["dur"], flag))

print()
print("KẾT LUẬN:", "CÓ VẤN ĐỀ → " + ", ".join(sorted(set(bad))) if bad else "SẠCH — không lỗ, lãi/giây không tụt, chuồng ≤ 4× ruộng")
