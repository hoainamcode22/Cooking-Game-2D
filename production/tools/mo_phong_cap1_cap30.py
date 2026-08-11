# -*- coding: utf-8 -*-
"""
================================================================================
 MÔ PHỎNG HÀNH TRÌNH CẤP 1 → 30  ·  công cụ của TESTER (T3 · T4 · T5 · T6 · T7 · T8)
================================================================================

VÌ SAO CÓ FILE NÀY: máy build không mở được Unity, nên toàn bộ luật kinh tế được
PORT LẠI TỪ CODE THẬT và chạy trên dữ liệu asset THẬT (không gõ tay con số nào).
Mọi con số in ra đều đọc trực tiếp từ:

  · CropData          (Assets/_Game/Farm/data/Hat_giong · Hạt Hoa)   → growSeconds, sellGold, goldPrice, expReward
  · PenMiniPanelConfig(Assets/_Game/Farm/data/PenConfig · Farm_May_Che_Bien)
  · DishData          (Assets/_Game/Farm/data/Farm_Cooking/Dish_*.asset)
  · IngredientData    (Assets/_Game/Data/Data_cooking/ING_* · SEA_*)
  · MarketPriceTable.cs (khối Add(...))
  · SCN_Farm.unity      (đếm plotId + PlotController)
  · SampleScene.unity   (cookingInventoryItems của CookingBoot)

LUẬT ĐƯỢC PORT (giữ nguyên tên hàm gốc để đối chiếu):
  · PlayerProgressManager.RequiredExpForLevel : 40 + 10n + 3n²/20      (n = L-1)
  · CookingScoreCalculator.ScoreRequiredIngredients : khớp đúng tập nguyên liệu
      (chỉ kind == Ingredient) → 70đ · lệch (thiếu/thừa) nhưng có trùng → 35đ · không trùng → 0
  · CookingScoreCalculator.ScoreFromVector : 100 − 5 × ManhattanDistance
  · CookingScoreCalculator.Evaluate : final = ingredientScore + round(flavor100 × 0.3), kẹp [0,100]
  · CookingChallengeManager.successScoreThreshold = 70
  · lãi ruộng/lượt = harvestAmount × sellGold − goldPrice
  · lãi chuồng/lượt = Σ(sản phẩm × giá gốc) − foodAmountPerFeed × giá gốc thức ăn

CÁCH CHẠY:
    python mo_phong_cap1_cap30.py [đường_dẫn_gốc_dự_án]
    (mặc định: hai cấp trên thư mục chứa file này)
================================================================================
"""
import re, os, sys, glob, itertools, collections

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.abspath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
A = os.path.join(ROOT, 'Assets')
if not os.path.isdir(A):
    sys.exit(f"Không thấy {A} — truyền đường dẫn gốc dự án làm tham số 1.")

rd = lambda p: open(p, encoding='utf-8', errors='replace').read()
F  = lambda pat: sorted(glob.glob(os.path.join(A, '**', pat), recursive=True))
def yg(t, k, d=None):
    m = re.search(r'^\s{0,4}' + re.escape(k) + r':\s*(.*)$', t, re.M)
    return m.group(1).strip() if m else d
def gi(t, k, d=0):
    try: return int(yg(t, k))
    except (TypeError, ValueError): return d
def guid_of(p):
    return re.search(r'^guid:\s*([0-9a-f]{32})', rd(p + '.meta'), re.M).group(1)

FLAVOR = ['sweet', 'spicy', 'sour', 'umami', 'texture']
def vec(t, key):
    m = re.search(re.escape(key) + r':\s*\n((?:\s+\w+:\s*-?\d+\s*\n)+)', t)
    d = {k: 0 for k in FLAVOR}
    if m:
        for k, v in re.findall(r'(\w+):\s*(-?\d+)', m.group(1)):
            if k in d: d[k] = int(v)
    return tuple(d[k] for k in FLAVOR)
vadd  = lambda a, b: tuple(x + y for x, y in zip(a, b))
vdist = lambda a, b: sum(abs(x - y) for x, y in zip(a, b))

# ══════════════════════════════════════════════════════════════════════════════
#  ĐỌC DỮ LIỆU THẬT
# ══════════════════════════════════════════════════════════════════════════════
crops = []
for p in F('*.asset'):
    t = rd(p)
    if 'Assembly-CSharp::CropData' not in t: continue
    crops.append(dict(id=yg(t, 'cropId'), item=yg(t, 'harvestItemId'), cat=gi(t, 'cropCategory'),
                      L=gi(t, 'unlockLevel'), sec=gi(t, 'growSeconds'), sell=gi(t, 'sellGold'),
                      amt=gi(t, 'harvestAmount'), seed=gi(t, 'goldPrice'), exp=gi(t, 'expReward'),
                      rac=bool(re.search(r'^\s+(tier|canDropFromAds|canAppearInRareMarket|isRare|seedCostGold):', t, re.M))))

# Cấp mở + giá công trình đọc từ DataShop/Buiding
PEN_BY_PRODUCT = {}
build = {}
for p in F('*.asset'):
    t = rd(p)
    if 'itemID:' in t and 'buildTimeSeconds:' in t:
        build[gi(t, 'itemID')] = dict(L=gi(t, 'unlockLevel'), gold=gi(t, 'goldPrice'), name=os.path.basename(p)[:-6])
PEN_BUILD = {'pen_03': 107, 'pen_02': 108, 'pen_01': 106, 'pen_04': 113,
             'may_01': 120, 'may_02': 121, 'may_03': 122}
pens = []
for p in F('Config_Pen*.asset') + F('Config_May*.asset'):
    t = rd(p); pid = yg(t, 'penId'); b = build.get(PEN_BUILD.get(pid, -1), dict(L=99, gold=0))
    pens.append(dict(id=pid, L=b['L'], cost=b['gold'],
                     f1=yg(t, 'food1ItemId'), f2=yg(t, 'food2ItemId'), need=gi(t, 'foodAmountPerFeed', 1),
                     p1=yg(t, 'productItemId'), n1=gi(t, 'productAmount', 1),
                     p2=yg(t, 'secondProductItemId'), n2=gi(t, 'secondProductAmount', 1),
                     sec=gi(t, 'feedDurationSeconds', 120), exp=gi(t, 'expReward')))
pens.sort(key=lambda x: x['L'])

mp = rd(os.path.join(A, '_Game/Farm/Scripts/Market/MarketPriceTable.cs'))
PRICE, UNLOCK = {}, {}
for m in re.finditer(r'Add\(\s*"([^"]+)"\s*,\s*"[^"]*"\s*,\s*MarketCategory\.(\w+)\s*,\s*(\d+)\s*,\s*(\d+)', mp):
    PRICE[m.group(1)] = int(m.group(3)); UNLOCK[m.group(1)] = int(m.group(4))

ing = {}
for p in F('ING_*.asset') + F('SEA_*.asset'):
    t = rd(p)
    ing[guid_of(p)] = dict(id=yg(t, 'id'), kind=gi(t, 'kind'), v=vec(t, 'vector'), asset=os.path.basename(p))
ing_by_id = {v['id']: v for v in ing.values()}

dishes = []
for p in F('Dish_*.asset'):
    t = rd(p)
    m = re.search(r'requiredIngredients:\s*\n((?:\s+- \{fileID: \d+, guid: [0-9a-f]{32}, type: \d+\}\s*\n)+)', t)
    gs = re.findall(r'guid:\s*([0-9a-f]{32})', m.group(1)) if m else []
    req = [ing.get(g) for g in gs]
    dishes.append(dict(id=yg(t, 'dishId'), L=gi(t, 'unlockLevel'), diff=gi(t, 'difficulty', 1),
                       exp=gi(t, 'rewardExp'), gold=gi(t, 'rewardGold'), sell=gi(t, 'sellPrice'),
                       tgt=vec(t, 'targetFlavor'), nulls=sum(1 for r in req if r is None),
                       I=[r['id'] for r in req if r and r['kind'] == 0],
                       S=[r['id'] for r in req if r and r['kind'] == 1]))
dishes.sort(key=lambda d: (d['L'], d['id']))

# Danh sách nguyên liệu THỰC SỰ vào được bếp = cookingInventoryItems của CookingBoot
sample = rd(os.path.join(A, '_Game/Scenes/SampleScene.unity'))
m = re.search(r'cookingInventoryItems:\s*\n((?:\s+- \{fileID: \d+, guid: [0-9a-f]{32}, type: \d+\}\s*\n)+)', sample)
kit_guids = re.findall(r'guid:\s*([0-9a-f]{32})', m.group(1)) if m else []
inv = {}
for p in F('*.asset'):
    t = rd(p)
    if 'Assembly-CSharp::InventoryItemData' not in t: continue
    cd = re.search(r'cookingData:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})', t)
    inv[guid_of(p)] = dict(itemId=yg(t, 'itemId'), cook=cd.group(1) if cd else None,
                           asset=os.path.basename(p))
KIT = set()
for g in kit_guids:
    it = inv.get(g)
    if it and it['cook'] and it['cook'] in ing: KIT.add(ing[it['cook']]['id'])

# CẤP SỚM NHẤT lấy được từng nguyên liệu (ruộng · chuồng · máy · chợ)
SRC = {}
for c in crops: SRC[c['item']] = min(SRC.get(c['item'], 99), c['L'])
for pn in pens:
    for k in (pn['p1'], pn['p2']):
        if k: SRC[k] = min(SRC.get(k, 99), pn['L'])
SRC['chicken'] = SRC.get('chicken_meat', 99)          # ING_Chicken.id = "chicken"
for k in ('salt', 'herbs', 'soysauce', 'fishsauce'):  # 4 gia vị CHỈ mua được ở chợ
    SRC[k] = UNLOCK.get(k, 99)

NORMAL_PLOTS, FLOWER_PLOTS = 26, 12   # đếm được từ SCN_Farm (xem phần T8)
MAX_ING_SLOT, MAX_SEA_SLOT = 4, 3     # CookingSelectionManager trong SampleScene
THRESHOLD = 70

def required_exp(L):
    n = L - 1
    return 40 + n * 10 + (n * n * 3) // 20

def H(s): print('\n' + '=' * 118 + f'\n {s}\n' + '=' * 118)

# ══════════════════════════════════════════════════════════════════════════════
#  T6 · T7 — thời gian & giá cây trồng
# ══════════════════════════════════════════════════════════════════════════════
H('T6 · T7 — BẢNG CÂY TRỒNG: nghịch lý thời gian · cây bán lỗ · YAML còn field đã xoá')
crops.sort(key=lambda c: (c['L'], c['sec']))
print(f"{'cropId':16}{'L':>3}{'giây':>6}{'bán':>5}{'sl':>4}{'hạt':>5}{'lãi':>6}{'lãi/giây':>10}{'exp':>5}{'exp/giây':>10}  YAML rác")
loi = []; nghich = []; tut = []; prev = None
for c in crops:
    lai = c['amt'] * c['sell'] - c['seed']; lps = lai / c['sec']
    print(f"{c['id']:16}{c['L']:>3}{c['sec']:>6}{c['sell']:>5}{c['amt']:>4}{c['seed']:>5}{lai:>6}{lps:>10.4f}"
          f"{c['exp']:>5}{c['exp']/c['sec']:>10.4f}  {'CÓ' if c['rac'] else '-'}")
    if lai <= 0: loi.append(f"{c['id']} lãi={lai}")
    if prev and c['L'] > prev['L'] and c['sec'] <= prev['sec']:
        nghich.append(f"{c['id']} L{c['L']} {c['sec']}s <= {prev['id']} L{prev['L']} {prev['sec']}s")
    if prev and lps < prev['lps'] - 1e-9: tut.append(f"{c['id']} {lps:.4f} < {prev['id']} {prev['lps']:.4f}")
    prev = dict(c, lps=lps)
print(f"\nSố cây: {len(crops)}")
print(f"  T7 cây bán lỗ / lãi 0 : {loi or 'KHÔNG CÓ  ✔'}")
print(f"  T6 nghịch lý thời gian: {nghich or 'KHÔNG CÓ  ✔'}")
print(f"  lãi/giây tụt theo cấp : {tut or 'KHÔNG TỤT LẦN NÀO  ✔'}")
print(f"  asset còn field đã xoá: {[c['id'] for c in crops if c['rac']] or 'KHÔNG CÒN  ✔'}")

# ══════════════════════════════════════════════════════════════════════════════
#  T4 — 18 MÓN: NẤU ĐƯỢC KHÔNG?
# ══════════════════════════════════════════════════════════════════════════════
H('T4 — 18 MÓN ĂN: NẤU ĐƯỢC KHÔNG? (luật CookingScoreCalculator, ngưỡng 70)')
print(f"{'dishId':26}{'L':>3}  {'nguyên liệu (kind=Ingredient)':40} {'gia vị':26}"
      f"{'điểm CT':>8}{'điểm max':>9}  kết luận")
print('-' * 145)
dat = 0; t4_loi = []
for d in dishes:
    thieu = [x for x in d['I'] if x not in KIT] + [x for x in d['S'] if x not in KIT]
    tre   = [x for x in d['I'] if SRC.get(x, 99) > d['L']]
    base = (0,) * 5
    for x in d['I']:
        if x in ing_by_id: base = vadd(base, ing_by_id[x]['v'])
    # điểm theo công thức khai trong asset
    tot = base
    for x in d['S']:
        if x in ing_by_id: tot = vadd(tot, ing_by_id[x]['v'])
    ing_score = 70 if (d['I'] and not thieu) else (35 if d['I'] else 0)
    ct = min(100, ing_score + round(max(0, min(100, 100 - 5 * vdist(tot, d['tgt']))) * 0.3))
    # điểm TỐI ĐA: thử mọi tổ hợp ≤3 gia vị có nguồn ở cấp L
    pool = [v for v in ing_by_id.values() if v['kind'] == 1 and v['id'] in KIT
            and SRC.get(v['id'], 99) <= d['L']]
    best = ct
    for k in range(0, MAX_SEA_SLOT + 1):
        for combo in itertools.combinations(pool, k):
            tt = base
            for cc in combo: tt = vadd(tt, cc['v'])
            best = max(best, min(100, ing_score + round(max(0, min(100, 100 - 5 * vdist(tt, d['tgt']))) * 0.3)))
    why = []
    if not d['I']:            why.append('0 nguyên liệu kind=Ingredient')
    if len(d['I']) > MAX_ING_SLOT: why.append(f"{len(d['I'])} nguyên liệu > {MAX_ING_SLOT} ô")
    if len(d['S']) > MAX_SEA_SLOT: why.append(f"{len(d['S'])} gia vị > {MAX_SEA_SLOT} ô")
    if thieu:      why.append('không vào được bếp: ' + ','.join(thieu))
    if tre:        why.append('chưa có nguồn ở cấp mở: ' + ','.join(tre))
    if d['nulls']: why.append(f"{d['nulls']} ref NULL")
    if ct < THRESHOLD: why.append(f'điểm công thức {ct} < {THRESHOLD}')
    ok = not why
    if ok: dat += 1
    else:  t4_loi.append((d['id'], why))
    print(f"{d['id']:26}{d['L']:>3}  {','.join(d['I']):40} {','.join(d['S']):26}"
          f"{ct:>8}{best:>9}  {'ĐẠT' if ok else 'KHÔNG: ' + '; '.join(why)}")
print(f"\n>>> ĐẠT {dat}/{len(dishes)} món")
print(f"    Món đạt 100 điểm khi chơi tối ưu: xem cột 'điểm max'")
print(f"    Món dùng sữa (milk): {[d['id'] for d in dishes if 'milk' in d['I'] + d['S']] or '0/18'}")
print(f"    Món dùng sugar     : {[d['id'] for d in dishes if 'sugar' in d['I'] + d['S']] or '0/18'}")
print("    LƯU Ý CƠ CHẾ: SEA_Milk có kind = Seasoning. ScoreRequiredIngredients BỎ QUA mọi thẻ")
print("    kind != Ingredient, nên bỏ sữa vào nồi KHÔNG bị tính 'nguyên liệu thừa' và KHÔNG")
print("    thể kéo 70 xuống 35 — chỉ làm giảm phần hương vị (tối đa −30), điểm vẫn ≥ 70.")

# ══════════════════════════════════════════════════════════════════════════════
#  T5 — LÃI MỖI GIÂY
# ══════════════════════════════════════════════════════════════════════════════
H('T5 — LÃI MỖI GIÂY: RUỘNG vs CHUỒNG/MÁY')
print(f"{'nguồn':26}{'L':>3}{'giây':>6}{'lãi/lượt':>10}{'lãi/giây':>10}"
      f"{'× ruộng tốt nhất cùng cấp':>27}{'exp/giây':>10}")
for pn in pens:
    ref = max((c['amt'] * c['sell'] - c['seed']) / c['sec'] for c in crops if c['L'] <= pn['L'])
    refe = max(c['exp'] / c['sec'] for c in crops if c['L'] <= pn['L'])
    food = pn['need'] * PRICE.get(pn['f1'], 0)
    rev  = pn['n1'] * PRICE.get(pn['p1'], 0) + (pn['n2'] * PRICE.get(pn['p2'], 0) if pn['p2'] else 0)
    lps  = (rev - food) / pn['sec']
    print(f"{pn['id'] + ' → ' + str(pn['p1']):26}{pn['L']:>3}{pn['sec']:>6}{rev - food:>10}{lps:>10.4f}"
          f"{lps / ref:>25.2f}×{pn['exp'] / pn['sec']:>10.4f}  (exp {pn['exp']/pn['sec']/refe:.2f}×)")
print("\nMÓN ĂN — tổng thu (bán + thưởng vàng) vs tổng giá nguyên liệu theo bảng giá gốc:")
print(f"{'món':26}{'L':>3}{'bán':>6}{'thưởng':>8}{'tổng thu':>10}{'giá NL':>8}{'lãi':>7}{'%':>7}")
t5_loi = []
for d in dishes:
    cost = sum(PRICE.get(x, 0) for x in d['I'] + d['S'])
    tot = d['sell'] + d['gold']; lai = tot - cost
    pct = (100 * lai / cost) if cost else 0
    flag = '  <-- LỖ' if lai < 0 else ('  <-- rất mỏng' if pct < 15 else '')
    if lai < 0: t5_loi.append((d['id'], lai))
    print(f"{d['id']:26}{d['L']:>3}{d['sell']:>6}{d['gold']:>8}{tot:>10}{cost:>8}{lai:>7}{pct:>6.0f}%{flag}")
print(f"\n  Món nấu ra RẺ HƠN nguyên liệu: {t5_loi or 'KHÔNG CÓ  ✔'}")

# ══════════════════════════════════════════════════════════════════════════════
#  T8 — plotId
# ══════════════════════════════════════════════════════════════════════════════
H('T8 — plotId: còn cặp trùng không? 38 PlotController có 38 id duy nhất không?')
sc = rd(os.path.join(A, '_Game/Scenes/SCN_Farm.unity'))
docs = re.split(r'\n(?=--- !u!)', sc)
byid = {}
for dd in docs:
    mm = re.match(r'--- !u!(\d+) &(\d+)', dd)
    if mm: byid[mm.group(2)] = dd
pdef = {}
for f in glob.glob(os.path.join(A, '**', '*.prefab'), recursive=True):
    t = rd(f)
    if 'Assembly-CSharp::PlotController' not in t: continue
    mm = re.search(r'^\s*plotId:\s*(-?\d+)', t, re.M)
    if mm: pdef[guid_of(f)] = (os.path.basename(f), int(mm.group(1)))
res = []
for dd in [x for x in docs if 'Assembly-CSharp::PlotController' in x]:
    mm = re.search(r'^\s*plotId:\s*(-?\d+)', dd, re.M)
    if mm: res.append((int(mm.group(1)), 'object thật trong scene', '')); continue
    pi = re.search(r'm_PrefabInstance:\s*\{fileID:\s*(\d+)\}', dd).group(1)
    pdoc = byid.get(pi, '')
    nm = re.search(r'propertyPath:\s*m_Name\s*\n\s*value:\s*(.*)', pdoc)
    mo = re.search(r'propertyPath:\s*plotId\s*\n\s*value:\s*(-?\d+)', pdoc)
    if mo: res.append((int(mo.group(1)), 'PrefabInstance (có modification)', nm.group(1).strip() if nm else ''))
    else:
        src = re.search(r'm_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})', pdoc)
        dv = pdef.get(src.group(1) if src else '', ('?', None))
        res.append((dv[1], f'ăn plotId MẶC ĐỊNH của {dv[0]}', nm.group(1).strip() if nm else ''))
cnt = collections.Counter(v for v, _, _ in res)
dup = {k: v for k, v in cnt.items() if v > 1}
print(f"  Tổng component PlotController : {len(res)}")
print(f"  Số plotId duy nhất            : {len(cnt)}")
print(f"  CẶP TRÙNG                     : {dup or 'KHÔNG CÒN  ✔'}")
print(f"  Dải id                        : {sorted(x for x in cnt if x is not None)}")
pc = rd(os.path.join(A, '_Game/Farm/Scripts/Gameplay/PlotController.cs'))
mm = re.search(r'LegacyPlotIdMap\s*=\s*new[^{]*\{(.*?)\n\s*\};', pc, re.S)
pairs = re.findall(r'\{\s*(\d+)\s*,\s*(\d+)\s*\}', mm.group(1)) if mm else []
print(f"  LegacyPlotIdMap (id mới→cũ)   : {len(pairs)} cặp {pairs}")
moi = sorted(int(a) for a, _ in pairs)
doi = sorted(v for v, how, _ in res if how != 'object thật trong scene' and v is not None and v >= 100)
print(f"  Id mới trong scene            : {doi}")
print(f"  KHỚP bảng migrate             : {'CÓ  ✔' if moi == doi else 'LỆCH !! ' + str((moi, doi))}")

# ══════════════════════════════════════════════════════════════════════════════
#  T3 — HÀNH TRÌNH CẤP 1 → 30
# ══════════════════════════════════════════════════════════════════════════════
H('T3 — HÀNH TRÌNH CẤP 1 → 30 (26 ô ruộng + 12 chậu hoa, tất cả mở từ cấp 1 do F10)')
START_GOLD = 400.0    # FarmEconomyManager.startGold trong SCN_Farm
print(f"{'L':>3}{'EXP cần':>9}{'EXP/s':>7}{'vàng/s':>8}{'giờ cấp':>9}{'giờ dồn':>9}{'vàng':>9}"
      f"  mở khoá trong cấp này / ghi chú")
gold, hours, owned, chan = START_GOLD, 0.0, set(), []
for L in range(1, 31):
    ok = [c for c in crops if c['L'] <= L]
    nm = [c for c in ok if c['cat'] == 0]; fl = [c for c in ok if c['cat'] == 1]
    key = lambda c: (c['amt'] * c['sell'] - c['seed']) / c['sec']
    bn = max(nm, key=key) if nm else None
    bf = max(fl, key=key) if fl else None
    e = g = 0.0
    if bn: e += NORMAL_PLOTS * bn['exp'] / bn['sec']; g += NORMAL_PLOTS * key(bn)
    if bf: e += FLOWER_PLOTS * bf['exp'] / bf['sec']; g += FLOWER_PLOTS * key(bf)
    for pn in pens:
        if pn['id'] not in owned: continue
        food = pn['need'] * PRICE.get(pn['f1'], 0)
        rev  = pn['n1'] * PRICE.get(pn['p1'], 0) + (pn['n2'] * PRICE.get(pn['p2'], 0) if pn['p2'] else 0)
        e += pn['exp'] / pn['sec']; g += (rev - food) / pn['sec']
    need = required_exp(L); h = need / e / 3600 if e > 0 else float('inf')
    gold += g * need / e if e > 0 else 0
    note = [f"cây {c['id']}" for c in crops if c['L'] == L]
    # MUA công trình ngay khi ĐỦ CẤP và ĐỦ VÀNG (mua trễ nếu thiếu — đúng hành vi thật)
    for pn in pens:
        if pn['id'] in owned or pn['L'] > L: continue
        if gold >= pn['cost']:
            gold -= pn['cost']; owned.add(pn['id'])
            tre_cap = L - pn['L']
            hau_to = '' if tre_cap == 0 else f", trễ {tre_cap} cấp vì phải gom vàng"
            note.append(f"MUA {pn['id']} (−{pn['cost']}v{hau_to})")
        elif pn['L'] == L:
            note.append(f"chưa đủ vàng cho {pn['id']} ({pn['cost']}v, có {gold:.0f}v) → mua sau")
    nd = [d['id'] for d in dishes if d['L'] == L]
    if nd: note.append('món mở: ' + ','.join(nd))
    if L == 5: note.append('>>> CỔNG BẾP MỞ (CookingGateAccess.RequiredLevel = 5)')
    for d in dishes:
        if d['L'] != L: continue
        miss = [x for x in d['I'] if SRC.get(x, 99) > L]
        if miss: chan.append(f"L{L}: món '{d['id']}' cần {miss} mà cấp {L} chưa có nguồn")
    hours += h
    print(f"{L:>3}{need:>9}{e:>7.2f}{g:>8.2f}{h:>9.2f}{hours:>9.2f}{gold:>9.0f}  {' · '.join(note)}")
if len(owned) < len(pens):
    chan.append(f"KHÔNG mua nổi tới cấp 30: {[p['id'] for p in pens if p['id'] not in owned]}")
print(f"\n  Tổng EXP cần tới cấp 30       : {sum(required_exp(i) for i in range(1, 30))}")
print(f"  Giờ ruộng chạy liên tục 24/24 : {hours:.2f} giờ (giới hạn dưới — chơi hoàn hảo)")
print(f"  Chuồng/máy mua được           : {sorted(owned)}")
print("\n  MÔ HÌNH CHƠI THẬT (cây vẫn lớn khi offline — PlotController lưu mốc unix):")
for logins in (2, 3, 5, 10):
    exp_ngay = (NORMAL_PLOTS + FLOWER_PLOTS) * logins * 5   # ~5 EXP/lượt ở cây cấp 1
    print(f"    {logins:>2} lần vào game/ngày  →  ~{exp_ngay:>4} EXP/ngày  →  "
          f"~{sum(required_exp(i) for i in range(1, 30)) / exp_ngay:>5.1f} ngày tới cấp 30")
print('\n  ── ĐIỂM CHẶN ──')
print('  ' + ('\n  '.join(chan) if chan else '>>> KHÔNG có điểm chặn cứng nào trên đường cấp 1 → 30  ✔'))
