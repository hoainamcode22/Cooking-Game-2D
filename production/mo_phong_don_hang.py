# -*- coding: utf-8 -*-
"""
================================================================================
 MO PHONG HE DON HANG (OrderBoard)  --  port 1:1 tu C# sang Python
================================================================================
Nguon logic:
  Assets/_Game/Farm/Scripts/OrderBoard/OrderGenerator.cs
  Assets/_Game/Farm/Scripts/OrderBoard/OrderBoardManager.cs
  Assets/_Game/Farm/Scripts/OrderBoard/OrderData.cs
  Assets/_Game/Farm/Scripts/Market/MarketPriceTable.cs
Nguon so lieu san xuat:
  Assets/_Game/Farm/data/Hat_giong/*.asset, Hat Hoa/*.asset   (CropData)
  Assets/_Game/Farm/data/PenConfig/*.asset                    (PenMiniPanelConfig)
  Assets/_Game/Farm/data/Farm_Cooking/Dish_*.asset            (DishData)
  Assets/_Game/Farm/Scripts/Managers/FarmManager.cs           (realTimeMultiplier = 0.3)
  Assets/_Game/Scripts/Progression/PlayerProgressManager.cs   (duong cong EXP)
  Assets/_Game/Scenes/SCN_Farm.unity                          (8 o dat + 6 chau hoa, 400 vang)

Chay:  python3 mo_phong_don_hang.py
"""

import math
import random
import statistics
from collections import Counter, defaultdict

RNG = random.Random(20260810)

# ==============================================================================
#  0. HAM TOAN CUA UNITY  (Mathf)
# ==============================================================================

def round_to_int(f):
    """Unity Mathf.RoundToInt == (int)Math.Round(f) == banker's rounding."""
    fl = math.floor(f)
    diff = f - fl
    if abs(diff - 0.5) < 1e-9:
        return int(fl) if int(fl) % 2 == 0 else int(fl) + 1
    return int(math.floor(f + 0.5)) if diff > 0.5 else int(fl)

def ceil_to_int(f):
    return int(math.ceil(f - 1e-9))

def clamp(v, lo, hi):
    return lo if v < lo else (hi if v > hi else v)

def clamp01(v):
    return clamp(v, 0.0, 1.0)

def lerp(a, b, t):
    return a + (b - a) * clamp01(t)

def inverse_lerp(a, b, v):
    if a == b:
        return 0.0
    return clamp01((v - a) / float(b - a))

# ==============================================================================
#  1. MarketPriceTable  (port nguyen van)
# ==============================================================================

MARKET_BUY_MULT = 1.5
SUGGESTED_SELL_MULT = 1.3

NONGSAN, HOA, HATGIONG, CHANNUOI, CHEBIEN, GIAVI, MONAN, VATLIEU = (
    "NongSan", "Hoa", "HatGiong", "ChanNuoi", "CheBien", "GiaVi", "MonAn", "VatLieu")

ALIASES = {"chicken": "chicken_meat"}

# (id, ten, danh muc, gia goc, cap mo khoa, trong so, MarketEnabled)
_TABLE = [
    # NONG SAN
    ("rice", "Lua", NONGSAN, 7, 1, 100, True),
    ("ngo", "Ngo", NONGSAN, 13, 2, 95, True),
    ("bapcai", "Bap Cai", NONGSAN, 15, 1, 95, True),
    ("carot", "Ca Rot", NONGSAN, 16, 3, 90, True),
    ("cachua", "Ca Chua", NONGSAN, 20, 3, 90, True),
    ("khoaitay", "Khoai Tay", NONGSAN, 25, 5, 80, True),
    ("mushroom", "Nam", NONGSAN, 30, 6, 70, True),
    ("sugarcane", "Mia", NONGSAN, 36, 7, 65, True),
    ("lemon", "Chanh", NONGSAN, 38, 8, 60, True),
    ("chili", "Ot", NONGSAN, 48, 9, 55, True),
    ("pepper", "Tieu", NONGSAN, 55, 10, 50, True),
    # HOA
    ("huong_duong", "Huong Duong", HOA, 12, 1, 70, True),
    ("tulip", "Tulip", HOA, 20, 9, 50, True),
    ("hoa_lan", "Hoa Lan", HOA, 22, 7, 55, True),
    ("hoa_hong", "Hoa Hong", HOA, 24, 4, 65, True),
    ("hoa_cuc_trang", "Hoa Cuc Trang", HOA, 24, 7, 55, True),
    ("hoa_cuc_van_tho", "Hoa Cuc Van Tho", HOA, 26, 9, 50, True),
    ("hoa_mau_don", "Hoa Mau Don", HOA, 28, 10, 45, True),
    ("hoa_oai_huong", "Hoa Oai Huong", HOA, 30, 4, 60, True),
    ("hoa_cam_tu_cau", "Hoa Cam Tu Cau", HOA, 30, 10, 45, True),
    ("hoa_anh_thao", "Hoa Anh Thao", HOA, 32, 10, 45, True),
    # HAT GIONG
    ("seed_rice", "Hat Lua", HATGIONG, 11, 1, 100, True),
    ("seed_huong_duong", "Hat Huong Duong", HATGIONG, 19, 1, 70, True),
    ("seed_ngo", "Hat Ngo", HATGIONG, 22, 2, 95, True),
    ("seed_bapcai", "Hat Bap Cai", HATGIONG, 25, 1, 95, True),
    ("ca_rot", "Hat Ca Rot", HATGIONG, 28, 3, 90, True),
    ("seed_tulip", "Hat Tulip", HATGIONG, 33, 9, 50, True),
    ("seed_cachua", "Hat Ca Chua", HATGIONG, 36, 3, 90, True),
    ("seed_hoa_lan", "Hat Hoa Lan", HATGIONG, 39, 7, 55, True),
    ("khoai_tay", "Hat Khoai Tay", HATGIONG, 44, 5, 80, True),
    ("seed_hoa_hong", "Hat Hoa Hong", HATGIONG, 44, 4, 65, True),
    ("seed_hoa_mau_don", "Hat Hoa Mau Don", HATGIONG, 50, 10, 45, True),
    ("seed_nam", "Hat Nam", HATGIONG, 55, 6, 70, True),
    ("seed_hoa_cuc_van_tho", "Hat Hoa Cuc Van Tho", HATGIONG, 55, 9, 50, True),
    ("seed_hoa_oai_huong", "Hat Hoa Oai Huong", HATGIONG, 55, 4, 60, True),
    ("seed_hoa_cuc_trang", "Hat Hoa Cuc Trang", HATGIONG, 61, 7, 55, True),
    ("seed_sugarcane", "Hat Mia", HATGIONG, 66, 7, 65, True),
    ("seed_hoa_cam_tu_cau", "Hat Hoa Cam Tu Cau", HATGIONG, 66, 10, 45, True),
    ("seed_lemon", "Hat Chanh", HATGIONG, 72, 8, 60, True),
    ("seed_hoa_anh_thao", "Hat Hoa Anh Thao", HATGIONG, 72, 10, 45, True),
    ("seed_chili", "Hat Ot", HATGIONG, 94, 9, 55, True),
    ("seed_pepper", "Hat Tieu", HATGIONG, 105, 10, 50, True),
    # CHAN NUOI
    ("egg", "Trung", CHANNUOI, 35, 4, 85, True),
    ("milk", "Sua", CHANNUOI, 40, 6, 75, True),
    ("chicken_meat", "Thit Ga", CHANNUOI, 45, 5, 75, True),
    ("pork", "Thit Heo", CHANNUOI, 55, 6, 65, True),
    ("beef", "Thit Bo", CHANNUOI, 65, 7, 60, True),
    # CHE BIEN
    ("bot_gao", "Bot Gao", CHEBIEN, 30, 5, 60, False),
    ("nuoc_mia_ep", "Nuoc Mia Ep", CHEBIEN, 60, 8, 50, False),
    ("pho_mai", "Pho Mai", CHEBIEN, 85, 9, 45, False),
    # GIA VI
    ("salt", "Muoi", GIAVI, 12, 1, 90, True),
    ("herbs", "Rau Thom", GIAVI, 18, 3, 85, True),
    ("soysauce", "Nuoc Tuong", GIAVI, 26, 4, 80, True),
    ("fishsauce", "Nuoc Mam", GIAVI, 28, 4, 80, True),
    # MON AN
    ("khoai_tay_chien", "Khoai Tay Chien", MONAN, 95, 5, 60, True),
    ("com_chien_trung", "Com Chien Trung", MONAN, 110, 5, 60, True),
    ("nuoc_mia_chanh", "Nuoc Mia Chanh", MONAN, 120, 8, 45, True),
    ("trung_chien_ca_chua", "Trung Chien Ca Chua", MONAN, 125, 5, 55, True),
    ("salad_bap_cai_chanh", "Salad Bap Cai Chanh", MONAN, 130, 8, 45, True),
    ("trung_op_la_bo_ne", "Trung Op La Bo Ne", MONAN, 145, 8, 45, True),
    ("bap_cai_xao_nam", "Bap Cai Xao Nam", MONAN, 160, 6, 50, True),
    ("sup_ngo_nam", "Sup Ngo Nam", MONAN, 165, 6, 50, True),
    ("salad_nam_rau", "Salad Nam Va Rau", MONAN, 175, 7, 45, True),
    ("thit_heo_luoc_cuon_rau", "Thit Heo Luoc Cuon Rau", MONAN, 185, 7, 45, True),
    ("canh_khoai_tay_thit_heo", "Canh Khoai Tay Thit Heo", MONAN, 190, 6, 45, True),
    ("ga_nuong_lu", "Ga Nuong Lu Mat Mia", MONAN, 195, 7, 40, True),
    ("nam_xao_thit_bo", "Nam Xao Thit Bo", MONAN, 225, 8, 40, True),
    ("ga_xao_ot", "Ga Xao Ot", MONAN, 240, 9, 35, True),
    ("bo_xao_tieu", "Bo Xao Tieu", MONAN, 270, 10, 30, True),
    ("bo_ham_ca_rot", "Bo Ham Ca Rot", MONAN, 280, 8, 35, True),
    ("suon_heo_xao_chua_ngot", "Suon Heo Xao Chua Ngot", MONAN, 295, 9, 30, True),
    ("pho_bo_tai", "Pho Bo Tai", MONAN, 320, 9, 30, True),
    ("canh_chua_ca", "Canh Chua Ca", MONAN, 290, 99, 20, False),
    ("ca_nuong_tieu", "Ca Nuong Tieu", MONAN, 300, 99, 20, False),
    # VAT LIEU
    ("da", "Da", VATLIEU, 40, 6, 55, True),
    ("go", "Go", VATLIEU, 45, 6, 55, True),
    ("dinh", "Dinh", VATLIEU, 55, 7, 50, True),
    ("son", "Son", VATLIEU, 60, 8, 45, True),
    ("kinh", "Kinh", VATLIEU, 70, 8, 45, True),
]


class ItemInfo(object):
    __slots__ = ("ItemId", "DisplayName", "Category", "BasePrice",
                 "UnlockLevel", "Weight", "MarketEnabled")

    def __init__(self, row):
        (self.ItemId, self.DisplayName, self.Category, self.BasePrice,
         self.UnlockLevel, self.Weight, self.MarketEnabled) = row


ALL_ITEMS = [ItemInfo(r) for r in _TABLE]
LOOKUP = {}
for _it in ALL_ITEMS:
    LOOKUP.setdefault(_it.ItemId, _it)


def canonical(item_id):
    if not item_id:
        return ""
    k = item_id.strip().lower()
    return ALIASES.get(k, k)


def base_price(item_id):
    it = LOOKUP.get(canonical(item_id))
    return it.BasePrice if it else 0


def category(item_id):
    it = LOOKUP.get(canonical(item_id))
    return it.Category if it else "All"


def display_name(item_id):
    it = LOOKUP.get(canonical(item_id))
    return it.DisplayName if it else item_id


def suggested_unit_price(item_id):
    bp = base_price(item_id)
    return 0 if bp <= 0 else max(1, round_to_int(bp * SUGGESTED_SELL_MULT))


def market_buy_price(item_id):
    bp = base_price(item_id)
    return 0 if bp <= 0 else max(1, round_to_int(bp * MARKET_BUY_MULT))


def unlock_level(item_id):
    it = LOOKUP.get(canonical(item_id))
    return it.UnlockLevel if it else 1

# ==============================================================================
#  2. OrderGenerator  (port nguyen van)
# ==============================================================================

TIER_TAPSU, TIER_QUENTAY, TIER_LANHNGHE, TIER_BACTHAY = 1, 2, 3, 4
TIER_NAME = {1: "TapSu", 2: "QuenTay", 3: "LanhNghe", 4: "BacThay"}

TIER_GOLD_MULT = [1.00, 1.15, 1.30, 1.50]
DISH_GOLD_MULT = 1.40
REWARD_RANDOM_MIN = 0.90
REWARD_RANDOM_MAX = 1.15
EXP_PER_GOLD = 8
MIN_REWARD_EXP = 3
PROFIT_OVER_STALL = 1.10
VALUE_REF_CHEAP = 7
VALUE_REF_PREMIUM = 300
ALLOWED_CATEGORIES = {NONGSAN, HOA, CHANNUOI, MONAN}


def get_tier_for_level(level):
    if level <= 5:
        return TIER_TAPSU
    if level <= 12:
        return TIER_QUENTAY
    if level <= 20:
        return TIER_LANHNGHE
    return TIER_BACTHAY


def roll_line_count(tier, rng):
    r = rng.random()
    if tier == TIER_TAPSU:
        return 1
    if tier == TIER_QUENTAY:
        return 1 if r < 0.60 else 2
    if tier == TIER_LANHNGHE:
        if r < 0.20:
            return 1
        return 2 if r < 0.70 else 3
    if tier == TIER_BACTHAY:
        if r < 0.10:
            return 1
        if r < 0.40:
            return 2
        return 3 if r < 0.80 else 4
    return 1


def get_amount_range(tier):
    return {TIER_TAPSU: (2, 5), TIER_QUENTAY: (2, 8),
            TIER_LANHNGHE: (3, 10), TIER_BACTHAY: (4, 12)}.get(tier, (2, 5))


def get_dish_amount_range(tier):
    return {TIER_QUENTAY: (1, 2), TIER_LANHNGHE: (1, 3),
            TIER_BACTHAY: (2, 4)}.get(tier, (1, 1))


def max_dishes_for(tier):
    return 2 if tier == TIER_BACTHAY else 1


def max_dish_price_for(tier):
    if tier == TIER_TAPSU:
        return 0
    if tier == TIER_QUENTAY:
        return 175
    if tier == TIER_LANHNGHE:
        return 250
    return 2 ** 31 - 1


def is_category_allowed_for_tier(cat, tier):
    if cat not in ALLOWED_CATEGORIES:
        return False
    if tier == TIER_TAPSU:
        return cat == NONGSAN
    return True


def build_pool(tier, player_level):
    mdp = max_dish_price_for(tier)
    pool = []
    for info in ALL_ITEMS:
        if not info.MarketEnabled:
            continue
        if not is_category_allowed_for_tier(info.Category, tier):
            continue
        if info.UnlockLevel > player_level:
            continue
        if info.Category == MONAN and info.BasePrice > mdp:
            continue
        if info.BasePrice <= 0:
            continue
        pool.append(info)
    return pool


def pick_weighted(pool, rng):
    total = sum(max(1, p.Weight) for p in pool)
    roll = rng.randrange(total)
    acc = 0
    for p in pool:
        acc += max(1, p.Weight)
        if roll < acc:
            return p
    return pool[-1]


def roll_amount(bp, min_amt, max_amt, rng):
    if max_amt < min_amt:
        max_amt = min_amt
    t = inverse_lerp(VALUE_REF_CHEAP, VALUE_REF_PREMIUM, bp)
    anchor = round_to_int(lerp(max_amt, min_amt, t))
    jitter = rng.randint(-1, 1)
    return clamp(anchor + jitter, min_amt, max_amt)


class Order(object):
    __slots__ = ("tier", "lines", "reward_gold", "reward_exp", "base_gold",
                 "theme", "order_id", "floored")

    def __init__(self, tier):
        self.tier = tier
        self.lines = []          # list of (itemId, amount)
        self.reward_gold = 0
        self.reward_exp = 0
        self.base_gold = 0
        self.theme = None
        self.order_id = None
        self.floored = False

    def __str__(self):
        s = " + ".join("%dx%s" % (a, i) for i, a in self.lines)
        return "[%s] (%s) -> %dv %dexp" % (TIER_NAME[self.tier], s,
                                           self.reward_gold, self.reward_exp)


_UID = [0]


def compute_reward(order, rng):
    bg = 0
    has_dish = False
    for iid, amt in order.lines:
        bg += base_price(iid) * amt
        if category(iid) == MONAN:
            has_dish = True
    order.base_gold = bg

    ti = clamp(order.tier - 1, 0, len(TIER_GOLD_MULT) - 1)
    mult = TIER_GOLD_MULT[ti]
    if has_dish:
        mult *= DISH_GOLD_MULT

    noise = REWARD_RANDOM_MIN + rng.random() * (REWARD_RANDOM_MAX - REWARD_RANDOM_MIN)
    gold = round_to_int(bg * mult * noise)

    floor_gold = ceil_to_int(bg * SUGGESTED_SELL_MULT * PROFIT_OVER_STALL)
    if gold < floor_gold:
        gold = floor_gold
        order.floored = True

    order.reward_gold = max(1, gold)
    order.reward_exp = max(MIN_REWARD_EXP, round_to_int(order.reward_gold / 8.0))


def choose_theme(order, rng):
    all_flowers = True
    all_livestock = True
    has_dish = False
    total_qty = 0
    for iid, amt in order.lines:
        c = category(iid)
        if c != HOA:
            all_flowers = False
        if c != CHANNUOI:
            all_livestock = False
        if c == MONAN:
            has_dish = True
        total_qty += amt
    if all_flowers:
        return "BoHoa"
    if order.tier == TIER_BACTHAY and rng.random() < 0.40:
        return "DonGap"
    if has_dish:
        return "QuanAn"
    if all_livestock:
        return "TrangTraiBan"
    if len(order.lines) >= 3:
        return "TiecMung"
    if total_qty >= 5 and rng.random() < 0.35:
        return "ChoPhien"
    return "BuaComGiaDinh"


def finalize(order, rng):
    order.theme = choose_theme(order, rng)
    _UID[0] += 1
    order.order_id = "o%06d" % _UID[0]
    compute_reward(order, rng)


def generate(player_level, rng):
    tier = get_tier_for_level(player_level)
    pool = build_pool(tier, player_level)
    if not pool:
        return None
    want_lines = min(roll_line_count(tier, rng), len(pool))
    min_amt, max_amt = get_amount_range(tier)

    order = Order(tier)
    dishes_used = 0
    max_dishes = max_dishes_for(tier)
    used = set()

    attempt = 0
    while attempt < 24 and len(order.lines) < want_lines:
        attempt += 1
        pick = pick_weighted(pool, rng)
        if pick.ItemId in used:
            continue
        is_dish = pick.Category == MONAN
        if is_dish and dishes_used >= max_dishes:
            continue
        lo, hi = (get_dish_amount_range(tier) if is_dish else (min_amt, max_amt))
        amt = roll_amount(pick.BasePrice, lo, hi, rng)
        order.lines.append((canonical(pick.ItemId), amt))
        used.add(pick.ItemId)
        if is_dish:
            dishes_used += 1

    if not order.lines:
        return None
    finalize(order, rng)
    return order


def generate_deliverable(player_level, owned_lookup, rng):
    if owned_lookup is None:
        return None
    tier = get_tier_for_level(player_level)
    pool = build_pool(tier, player_level)
    if not pool:
        return None
    min_amt, max_amt = get_amount_range(tier)

    affordable = []
    for p in pool:
        iid = canonical(p.ItemId)
        need = min_amt
        if p.Category == MONAN:
            need = get_dish_amount_range(tier)[0]
        if owned_lookup(iid) >= need:
            affordable.append(p)
    if not affordable:
        return None

    want_lines = min(2 if tier >= TIER_LANHNGHE else 1, len(affordable))
    order = Order(tier)
    used = set()
    dishes_used = 0
    max_dishes = max_dishes_for(tier)

    attempt = 0
    while attempt < 24 and len(order.lines) < want_lines:
        attempt += 1
        pick = pick_weighted(affordable, rng)
        iid = canonical(pick.ItemId)
        if iid in used:
            continue
        is_dish = pick.Category == MONAN
        if is_dish and dishes_used >= max_dishes:
            continue
        lo, hi = (get_dish_amount_range(tier) if is_dish else (min_amt, max_amt))
        owned = owned_lookup(iid)
        amt = roll_amount(pick.BasePrice, lo, min(hi, owned), rng)
        if amt > owned:
            amt = owned
        if amt < 1:
            continue
        order.lines.append((iid, amt))
        used.add(iid)
        if is_dish:
            dishes_used += 1

    if not order.lines:
        return None
    finalize(order, rng)
    return order

# ==============================================================================
#  3. OrderBoardManager  (port: 9 o, luat >=2 don giao duoc)
# ==============================================================================

SLOT_COUNT = 9
MIN_DELIVERABLE = 2


class Board(object):
    def __init__(self, level, inventory, rng):
        self.level = level
        self.inv = inventory                # dict itemId -> amount
        self.rng = rng
        self.orders = [None] * SLOT_COUNT
        self.refill_and_balance()

    def owned(self, iid):
        return self.inv.get(canonical(iid), 0)

    def is_deliverable(self, o):
        if o is None or not o.lines:
            return False
        return all(self.owned(i) >= a for i, a in o.lines)

    def count_deliverable(self):
        return sum(1 for o in self.orders if self.is_deliverable(o))

    def create_order(self, prefer_deliverable):
        o = None
        if prefer_deliverable:
            o = generate_deliverable(self.level, self.owned, self.rng)
        if o is None:
            o = generate(self.level, self.rng)
        return o

    def find_least_progressed_slot(self):
        worst_slot, worst_ratio = -1, float("inf")
        for i, o in enumerate(self.orders):
            if o is None or not o.lines:
                continue
            if self.is_deliverable(o):
                continue
            s, n = 0.0, 0
            for iid, amt in o.lines:
                if amt <= 0:
                    continue
                s += clamp01(self.owned(iid) / float(amt))
                n += 1
            ratio = (s / n) if n else 0.0
            if ratio < worst_ratio:
                worst_ratio, worst_slot = ratio, i
        return worst_slot

    def refill_and_balance(self):
        while len(self.orders) < SLOT_COUNT:
            self.orders.append(None)
        del self.orders[SLOT_COUNT:]

        for i in range(len(self.orders)):
            if self.orders[i] is not None:
                continue
            need_easy = self.count_deliverable() < MIN_DELIVERABLE
            self.orders[i] = self.create_order(need_easy)

        guard = 0
        while guard < 3 and self.count_deliverable() < MIN_DELIVERABLE:
            guard += 1
            victim = self.find_least_progressed_slot()
            if victim < 0:
                break
            easy = generate_deliverable(self.level, self.owned, self.rng)
            if easy is None:
                break
            self.orders[victim] = easy

    def try_deliver(self, slot):
        o = self.orders[slot]
        if o is None:
            return None
        for iid, amt in o.lines:
            if self.inv.get(iid, 0) < amt:
                return None
        for iid, amt in o.lines:
            self.inv[iid] -= amt
        del self.orders[slot]
        self.refill_and_balance()
        return o

    def on_inventory_changed(self):
        """LateUpdate: kho doi -> neu < 2 don giao duoc thi can lai bang."""
        if self.count_deliverable() < MIN_DELIVERABLE:
            self.refill_and_balance()

# ==============================================================================
#  4. DU LIEU SAN XUAT
# ==============================================================================

REAL_TIME_MULT = 0.3        # FarmManager.realTimeMultiplier (scene = 0.3)
NORMAL_PLOTS = 8            # SCN_Farm: 8 Plot_01 active
FLOWER_POTS = 6             # SCN_Farm: 6 Chauhoa_1 active
LAND_COST = 50              # DataShop/Buiding/Dat.asset goldPrice

# cropId -> (harvestItemId, growSeconds, harvestAmount, unlockLevel, seedGold, loaiChau)
CROPS = {
    "rice":            ("rice",            180, 4, 1,  20, "dat"),
    "bapcai":          ("bapcai",          300, 4, 1,  45, "dat"),
    "ngo":             ("ngo",             360, 4, 2,  40, "dat"),
    "carot":           ("carot",           400, 4, 3,  50, "dat"),
    "cachua":          ("cachua",          480, 4, 3,  65, "dat"),
    "khoaitay":        ("khoaitay",        500, 4, 5,  80, "dat"),
    "nam":             ("mushroom",        600, 4, 6, 100, "dat"),
    "sugarcane":       ("sugarcane",       420, 4, 7, 120, "dat"),
    "lemon":           ("lemon",           780, 4, 8, 130, "dat"),
    "chili":           ("chili",           540, 4, 9, 170, "dat"),
    "pepper":          ("pepper",          660, 4, 10,190, "dat"),
    "huong_duong":     ("huong_duong",     180, 4, 1,  35, "chau"),
    "hoa_hong":        ("hoa_hong",        180, 4, 4,  80, "chau"),
    "hoa_oai_huong":   ("hoa_oai_huong",   180, 4, 4, 100, "chau"),
    "hoa_lan":         ("hoa_lan",         180, 4, 7,  70, "chau"),
    "hoa_cuc_trang":   ("hoa_cuc_trang",   180, 4, 7, 110, "chau"),
    "tulip":           ("tulip",           180, 4, 9,  60, "chau"),
    "hoa_cuc_van_tho": ("hoa_cuc_van_tho", 180, 4, 9, 100, "chau"),
    "hoa_mau_don":     ("hoa_mau_don",     180, 4, 10, 90, "chau"),
    "hoa_cam_tu_cau":  ("hoa_cam_tu_cau",  180, 4, 10,120, "chau"),
    "hoa_anh_thao":    ("hoa_anh_thao",    180, 4, 10,130, "chau"),
}

# san pham -> (cycleSeconds, amount, feed, buildGold, buildLevel)
PENS = {
    "chicken_meat": (30, 4, "rice",   100, 2),
    "egg":          (30, 4, "rice",   100, 2),   # san pham phu cua Chuong Ga
    "pork":         (30, 4, "bapcai", 600, 4),
    "beef":         (30, 4, "rice",  1500, 6),
    "milk":         (30, 4, "rice",  2000, 8),
}

# dishId -> (unlockLevel, [kind=Ingredient BAT BUOC], [kind=Seasoning TUY CHON])
#
# CookingScoreCalculator: ingredientScore max 70 (khop DUNG tap Ingredient-kind),
# seasoningScore max 30 (khoang cach flavor). Nguong thanh cong = 70.
# => CHI can du tap Ingredient-kind la nau thanh cong; Seasoning chi lam dep diem.
# => Mon KHONG co Ingredient-kind nao (nuoc_mia_chanh) thi tran diem = 30 -> KHONG BAO GIO NAU DUOC.
DISHES = {
    "khoai_tay_chien":        (5, ["khoaitay"], []),
    "com_chien_trung":        (5, ["rice", "egg"], ["soysauce"]),
    "trung_chien_ca_chua":    (5, ["egg", "cachua"], []),
    "bap_cai_xao_nam":        (6, ["bapcai", "mushroom"], ["fishsauce"]),
    "sup_ngo_nam":            (6, ["ngo", "mushroom", "egg"], []),
    "canh_khoai_tay_thit_heo":(6, ["pork", "khoaitay"], ["salt"]),
    "salad_nam_rau":          (7, ["mushroom", "herbs"], []),
    "thit_heo_luoc_cuon_rau": (7, ["pork", "herbs"], []),
    "ga_nuong_lu":            (7, ["chicken_meat"], ["pepper", "salt", "sugarcane"]),
    "nuoc_mia_chanh":         (8, [], ["sugarcane", "lemon"]),
    "salad_bap_cai_chanh":    (8, ["bapcai", "herbs"], ["lemon"]),
    "trung_op_la_bo_ne":      (8, ["egg", "beef", "cachua"], ["pepper"]),
    "nam_xao_thit_bo":        (8, ["mushroom", "beef"], ["soysauce"]),
    "bo_ham_ca_rot":          (8, ["beef", "carot"], ["pepper", "salt"]),
    "ga_xao_ot":              (9, ["chicken_meat"], ["chili"]),
    "suon_heo_xao_chua_ngot": (9, ["pork", "cachua"], ["chili", "lemon"]),
    "pho_bo_tai":             (9, ["beef", "rice", "herbs"], ["lemon", "chili"]),
    "bo_xao_tieu":           (10, ["beef"], ["pepper", "soysauce"]),
}

# Gia vi KHONG farm duoc (chi mua o cho). 'sugar' == cay Mia (Item_sugarcane.cookingData
# = SEA_Sugar) nen sugar KHONG nam trong danh sach nay.
UNFARMABLE = {"salt", "herbs", "soysauce", "fishsauce"}

# Mon KHONG BAO GIO nau thanh cong (tran diem 30 < nguong 70)
NEVER_COOKABLE = {"nuoc_mia_chanh"}

# Chi phi thoi gian THAT cua mot lan nau (khong chi la minigame):
#   - keo do tu Kho sang Bep (WarehousePopupUI.SendPendingItemsToKitchen)  ~15s
#   - doi scene Farm -> Cooking va nguoc lai                               ~20s (2 lan)
#   - chon the + minigame (5-10s) + cookSubmitDelay 0.8s + man hinh diem 5s ~16s
#   - bam thu mon ve kho (CollectCookedDishToWarehouse)                     ~4s
COOK_MINIGAME_SECONDS = 11.0   # rieng phan minigame theo code
COOK_OVERHEAD_PER_ORDER = 35.0 # doi scene + keo do, chia deu cho ca don
COOK_SECONDS = 20.0            # moi MON them 20s (chon the, minigame, diem, thu ve)

# --- CAP MO KHOA THAT SU (theo NGUON SAN XUAT, khong phai theo MarketPriceTable) ---
PRODUCTION_UNLOCK = {}
BUILD_COST = {}
for _cid, (_hid, _gs, _ha, _ul, _sg, _kind) in CROPS.items():
    PRODUCTION_UNLOCK[_hid] = _ul
    BUILD_COST[_hid] = 0
for _pid, (_c, _a, _f, _cost, _bl) in PENS.items():
    PRODUCTION_UNLOCK[_pid] = _bl
    BUILD_COST[_pid] = _cost
for _did, (_ul, _ing, _sea) in DISHES.items():
    _gate = _ul
    _cost = 0
    for _g in _ing:
        if _g in UNFARMABLE:
            continue
        _gate = max(_gate, PRODUCTION_UNLOCK.get(_g, 1))
        _cost = max(_cost, BUILD_COST.get(_g, 0))
    PRODUCTION_UNLOCK[_did] = _gate
    BUILD_COST[_did] = _cost


def grow_real(secs):
    return max(5, round_to_int(secs * REAL_TIME_MULT))


def crop_of_item(item_id):
    for cid, (hid, gs, ha, ul, sg, kind) in CROPS.items():
        if hid == item_id:
            return (cid, gs, ha, ul, sg, kind)
    return None


def rate_per_hour(item_id, level, plots_dat=NORMAL_PLOTS, plots_chau=FLOWER_POTS,
                  uptime=1.0):
    """Don vi/gio khi NGUOI CHOI DON TOAN LUC vao mot mon (can tren)."""
    c = crop_of_item(item_id)
    if c:
        cid, gs, ha, ul, sg, kind = c
        if ul > level:
            return 0.0
        n = plots_dat if kind == "dat" else plots_chau
        return uptime * n * ha * 3600.0 / grow_real(gs)
    if item_id in PENS:
        cyc, amt, feed, cost, blvl = PENS[item_id]
        if blvl > level:
            return 0.0
        return uptime * amt * 3600.0 / cyc
    if item_id in DISHES:
        ul, ing, sea = DISHES[item_id]
        if ul > level:
            return 0.0
        if item_id in NEVER_COOKABLE:
            return 0.0
        # CHI nguyen lieu kind=Ingredient moi bat buoc (70 diem la du qua nguong).
        slowest = 0.0
        for g in ing:
            if g in UNFARMABLE:
                continue          # mua o cho -> khong ton thoi gian, chi ton vang
            r = rate_per_hour(g, level, plots_dat, plots_chau, uptime)
            if r <= 0:
                return 0.0
            slowest = max(slowest, 1.0 / r)
        per_dish_hours = COOK_SECONDS / 3600.0 + slowest
        return 1.0 / per_dish_hours if per_dish_hours > 0 else 0.0
    return 0.0


def order_gather_hours(order, level):
    """SUM(so luong / toc do moi gio) theo dung yeu cau kiem chung.
    Cong them chi phi doi scene MOT LAN neu don co mon nau."""
    total = 0.0
    has_dish = False
    for iid, amt in order.lines:
        if iid in DISHES:
            has_dish = True
        r = rate_per_hour(iid, level)
        if r <= 0:
            return float("inf")
        total += amt / r
    if has_dish:
        total += COOK_OVERHEAD_PER_ORDER / 3600.0
    return total


def order_gold_cost(order, level):
    """Vang phai bo ra de gom don (hat giong + thuc an + gia vi mua cho)."""
    cost = 0.0
    for iid, amt in order.lines:
        cost += unit_input_cost(iid, level) * amt
    return cost


_UIC_CACHE = {}


def unit_input_cost(item_id, level):
    key = (item_id, level)
    if key in _UIC_CACHE:
        return _UIC_CACHE[key]
    _UIC_CACHE[key] = 0.0
    c = crop_of_item(item_id)
    if c:
        cid, gs, ha, ul, sg, kind = c
        v = sg / float(ha)
    elif item_id in PENS:
        cyc, amt, feed, cost, blvl = PENS[item_id]
        v = unit_input_cost(feed, level) / float(amt)
        if item_id in ("chicken_meat", "egg"):
            v = v / 2.0     # 1 lua -> 4 thit ga + 4 trung
    elif item_id in DISHES:
        ul, ing, sea = DISHES[item_id]
        v = 0.0
        for g in ing:                       # chi Ingredient-kind la bat buoc
            if g in UNFARMABLE:
                v += market_buy_price(g)    # herbs 27v/don vi
            else:
                v += unit_input_cost(g, level)
        for g in sea:                       # gia vi: chi tinh cai farm duoc (mien phi cong)
            if g not in UNFARMABLE:
                v += unit_input_cost(g, level)
    else:
        v = market_buy_price(item_id)
    _UIC_CACHE[key] = v
    return v

# ==============================================================================
#  5. DUONG CONG EXP
# ==============================================================================

def exp_required(level):
    n = level - 1
    return 40 + (n * 10) + (n * n * 3) // 20


# ==============================================================================
#  5b. KIEM CHEO: cap mo khoa o CHO vs cap mo khoa SAN XUAT
# ==============================================================================

def audit_unlock_gaps():
    hr("KIEM CHEO - CAP MO KHOA TRONG CHO vs CAP MO KHOA SAN XUAT THAT")
    print("Bo sinh don loc theo MarketPriceTable.UnlockLevel. Neu NGUON SAN XUAT")
    print("mo khoa MUON HON thi don ra o khoang cap giua = DON CHET.\n")
    print("%-24s %-9s %8s %10s %10s %10s" %
          ("VAT PHAM", "loai", "cap CHO", "cap SX", "cong trinh", "KET LUAN"))
    print("-" * 78)
    gaps = []
    for info in ALL_ITEMS:
        if not info.MarketEnabled:
            continue
        if info.Category not in ALLOWED_CATEGORIES:
            continue
        iid = info.ItemId
        prod = PRODUCTION_UNLOCK.get(iid)
        if prod is None:
            print("%-24s %-9s %8d %10s %10s %10s" %
                  (iid, info.Category, info.UnlockLevel, "?", "-", "KHONG CO NGUON!"))
            gaps.append((iid, info.UnlockLevel, None))
            continue
        cost = BUILD_COST.get(iid, 0)
        verdict = "ok"
        if prod > info.UnlockLevel:
            verdict = "LECH %d cap" % (prod - info.UnlockLevel)
            gaps.append((iid, info.UnlockLevel, prod))
        if iid in NEVER_COOKABLE:
            verdict = "KHONG NAU NOI"
            gaps.append((iid, info.UnlockLevel, 999))
        if verdict != "ok":
            print("%-24s %-9s %8d %10d %10d %10s" %
                  (iid, info.Category, info.UnlockLevel, prod, cost, verdict))
    if not gaps:
        print("  (khong co lech nao)")
    print("\nCong trinh phai MUA truoc khi san xuat duoc (chan bang VANG, khong phai cap):")
    for pid, (cyc, amt, feed, cost, blvl) in sorted(PENS.items(), key=lambda x: x[1][3]):
        print("  %-14s -> chuong %5d vang, mo o cap %d" % (pid, cost, blvl))
    return gaps

# ==============================================================================
#  BAO CAO
# ==============================================================================

def hr(t=""):
    print("\n" + "=" * 78)
    if t:
        print(" " + t)
        print("=" * 78)


def fmt_hours(h):
    if h == float("inf"):
        return "  KHONG BAO GIO"
    if h < 1 / 60.0:
        return "%5.1f giay" % (h * 3600)
    if h < 1:
        return "%5.1f phut" % (h * 60)
    return "%5.2f gio " % h


# ------------------------------------------------------------------ BUOC 2
def buoc2_bang_san_xuat():
    hr("BUOC 2 - BANG TOC DO SAN XUAT (don vi / gio, choi tich cuc, don toan luc)")
    print("realTimeMultiplier = %.2f  =>  thoi gian trong THAT = growSeconds x %.2f"
          % (REAL_TIME_MULT, REAL_TIME_MULT))
    print("O dat: %d  |  Chau hoa: %d  |  Mua them dat: %d vang/o (khong gioi han)\n"
          % (NORMAL_PLOTS, FLOWER_POTS, LAND_COST))

    print("%-22s %-9s %6s %6s %5s %9s %10s %9s" %
          ("VAT PHAM", "NGUON", "chuky", "sl/ck", "cap", "dv/gio", "vang goc", "chiphi/dv"))
    print("-" * 88)
    rows = []
    for cid, (hid, gs, ha, ul, sg, kind) in CROPS.items():
        n = NORMAL_PLOTS if kind == "dat" else FLOWER_POTS
        rows.append((hid, "trong(%s)" % kind, grow_real(gs), ha * n, ul,
                     n * ha * 3600.0 / grow_real(gs), base_price(hid),
                     unit_input_cost(hid, 99)))
    for pid, (cyc, amt, feed, cost, blvl) in PENS.items():
        rows.append((pid, "chuong", cyc, amt, blvl, amt * 3600.0 / cyc,
                     base_price(pid), unit_input_cost(pid, 99)))
    for did, (ul, ing, sea) in DISHES.items():
        rows.append((did, "nau", int(COOK_SECONDS), 1, ul,
                     rate_per_hour(did, 99), base_price(did),
                     unit_input_cost(did, 99)))
    for r in rows:
        print("%-22s %-9s %6d %6d %5d %9.1f %10d %9.1f" % r)
    print("\nGhi chu: 'sl/ck' cho cay trong = tong thu hoach cua CA %d o (hoac %d chau)."
          % (NORMAL_PLOTS, FLOWER_POTS))
    print("Chuong: 30s/chu ky, ton 1 don vi thuc an -> 4 san pham (Chuong Ga: 4 thit + 4 trung).")
    print("Nau: %.1fs/mon (minigame), nut co chai la nguyen lieu cham nhat." % COOK_SECONDS)


# ------------------------------------------------------------------ KICH BAN A
def kich_ban_a():
    hr("KICH BAN A - NGUOI CHOI MOI, KHO RONG, CAP 1")
    rng = random.Random(1)
    inv = {}
    b = Board(1, inv, rng)
    print("Pool cap 1 (bac TapSu, chi NongSan, unlock<=1): %s"
          % [i.ItemId for i in build_pool(TIER_TAPSU, 1)])
    print("\nBang 9 don luc vao game (kho rong):")
    for i, o in enumerate(b.orders):
        print("  o %d: %-46s %s" % (i, str(o), "GIAO DUOC" if b.is_deliverable(o) else ""))
    print("\n=> So don giao duoc ngay: %d / 9" % b.count_deliverable())

    # thu hoach 1 luot 8 o lua
    inv["rice"] = inv.get("rice", 0) + NORMAL_PLOTS * 4
    b.on_inventory_changed()
    print("\nSau khi thu hoach 1 luot 8 o LUA (+%d lua, mat %ds):"
          % (NORMAL_PLOTS * 4, grow_real(180)))
    for i, o in enumerate(b.orders):
        print("  o %d: %-46s %s" % (i, str(o), "GIAO DUOC" if b.is_deliverable(o) else ""))
    print("=> So don giao duoc: %d / 9" % b.count_deliverable())

    # co don nao doi thu cap 1 khong the co khong
    print("\nKiem 10.000 don cap 1: co mon nao KHONG THE co o cap 1?")
    bad = Counter()
    rng2 = random.Random(7)
    for _ in range(10000):
        o = generate(1, rng2)
        for iid, amt in o.lines:
            if unlock_level(iid) > 1:
                bad[iid] += 1
            c = crop_of_item(iid)
            if c is None and iid not in PENS and iid not in DISHES:
                bad["KHONG-SAN-XUAT-DUOC:" + iid] += 1
    print("  Ket qua: %s" % (dict(bad) if bad else "KHONG CO - tat ca deu farm duoc o cap 1"))
    print("\n  LUU Y: kho ban dau chi co 10 hat lua + 10 hat huong duong, 400 vang.")
    print("  Bap Cai (cap 1) phai MUA hat 45 vang/hat -> 8 o = 360 vang (gan het von).")


# ------------------------------------------------------------------ KICH BAN B
def kich_ban_b(levels=(1, 3, 5, 8, 12, 16, 20, 25, 30), n=10000):
    hr("KICH BAN B - 10.000 LUOT SINH DON MOI CAP")
    header = ("%-4s %-9s %-22s %6s %8s %8s %8s %8s %8s %8s %6s" %
              ("Cap", "Bac", "Phan bo so dong", "sl/tb", "Vmin", "Vtb", "Vmax",
               "Emin", "Etb", "Emax", "%san"))
    print(header)
    print("-" * len(header))
    results = {}
    for lv in levels:
        rng = random.Random(1000 + lv)
        lc = Counter()
        amts, golds, exps = [], [], []
        locked = Counter()
        disabled = Counter()
        spice = Counter()
        floored = 0
        item_hits = Counter()
        orders = []
        for _ in range(n):
            o = generate(lv, rng)
            orders.append(o)
            lc[len(o.lines)] += 1
            golds.append(o.reward_gold)
            exps.append(o.reward_exp)
            if o.floored:
                floored += 1
            for iid, amt in o.lines:
                amts.append(amt)
                item_hits[iid] += 1
                if unlock_level(iid) > lv:
                    locked[iid] += 1
                info = LOOKUP.get(iid)
                if info and not info.MarketEnabled:
                    disabled[iid] += 1
                if iid in ("salt", "herbs", "soysauce", "fishsauce", "sugar"):
                    spice[iid] += 1
        dist = " ".join("%d:%4.1f%%" % (k, 100.0 * lc[k] / n) for k in sorted(lc))
        print("%-4d %-9s %-22s %6.2f %8d %8d %8d %8d %8d %8d %5.1f%%" %
              (lv, TIER_NAME[get_tier_for_level(lv)], dist,
               statistics.mean(amts),
               min(golds), int(statistics.median(golds)), max(golds),
               min(exps), int(statistics.median(exps)), max(exps),
               100.0 * floored / n))
        # loi gian tiep: don doi mon an chua nguyen lieu KHONG farm duoc,
        # hoac doi mon KHONG BAO GIO nau noi, hoac doi item chua co nguon san xuat
        indirect = Counter()
        prodgate = Counter()
        never = Counter()
        for o in orders:
            for iid, amt in o.lines:
                if iid in DISHES:
                    for g in DISHES[iid][1]:
                        if g in UNFARMABLE:
                            indirect[g] += 1
                    if iid in NEVER_COOKABLE:
                        never[iid] += 1
                if PRODUCTION_UNLOCK.get(iid, 1) > lv:
                    prodgate[iid] += 1
        results[lv] = dict(orders=orders, locked=locked, disabled=disabled,
                           spice=spice, item_hits=item_hits, golds=golds,
                           floored=floored, indirect=indirect,
                           prodgate=prodgate, never=never)

    print("\nKIEM LOI NANG:")
    any_err = False
    for lv in levels:
        r = results[lv]
        if r["locked"]:
            print("  [LOI] cap %d: don doi item CHUA MO KHOA: %s" % (lv, dict(r["locked"])))
            any_err = True
        if r["disabled"]:
            print("  [LOI] cap %d: don doi item MarketEnabled=false: %s" % (lv, dict(r["disabled"])))
            any_err = True
        if r["spice"]:
            print("  [LOI] cap %d: don doi GIA VI khong farm duoc: %s" % (lv, dict(r["spice"])))
            any_err = True
    if not any_err:
        print("  [OK] 3 hang muc TRUC TIEP deu sach: khong don nao doi item chua mo khoa,")
        print("       khong don nao doi item MarketEnabled=false,")
        print("       khong don nao doi thang salt/herbs/soysauce/fishsauce/sugar.")

    print("\nKIEM LOI GIAN TIEP (bo loc danh muc KHONG bat duoc):")
    err2 = False
    for lv in levels:
        r = results[lv]
        if r["prodgate"]:
            print("  [LOI] cap %d: don doi item ma NGUON SAN XUAT chua mo o cap nay: %s"
                  % (lv, dict(r["prodgate"])))
            err2 = True
        if r["never"]:
            tot = sum(r["never"].values())
            print("  [LOI] cap %d: %d/%d don doi mon KHONG BAO GIO NAU NOI: %s"
                  % (lv, tot, n, dict(r["never"])))
            err2 = True
        if r["indirect"]:
            tot = sum(r["indirect"].values())
            print("  [CANH BAO] cap %d: %d/%d don (%.1f%%) doi mon an CAN gia vi khong farm duoc: %s"
                  % (lv, tot, n, 100.0 * tot / n, dict(r["indirect"])))
            err2 = True
    if not err2:
        print("  (khong co)")

    print("\n%% don bi CHAM SAN loi nhuan (=> he so bac + nhieu ngau nhien VO NGHIA):")
    for lv in levels:
        print("  cap %-3d -> %5.1f%%" % (lv, 100.0 * results[lv]["floored"] / n))
    return results


# ------------------------------------------------------------------ KICH BAN C
def kich_ban_c(results, levels=(1, 3, 5, 8, 12, 16, 20, 25, 30)):
    hr("KICH BAN C - TINH KHA THI (thoi gian gom + vang/gio)")
    hdr = ("%-4s %11s %11s %11s %7s %8s %11s %11s" %
           ("Cap", "gom-min", "gom-tb", "gom-max", ">2gio", "impos", "vang/don", "vang/gio"))
    print(hdr)
    print("-" * len(hdr))
    rows = {}
    for lv in levels:
        orders = results[lv]["orders"]
        hours, heavy, impossible = [], 0, 0
        for o in orders:
            h = order_gather_hours(o, lv)
            if h == float("inf"):
                impossible += 1
                continue
            hours.append(h)
            if h > 2.0:
                heavy += 1
        gold_mean = statistics.mean(o.reward_gold for o in orders)
        gph = gold_mean / statistics.mean(hours) if hours else 0
        print("%-4d %11s %11s %11s %6.2f%% %7.2f%% %11.0f %11.0f" %
              (lv, fmt_hours(min(hours)), fmt_hours(statistics.mean(hours)),
               fmt_hours(max(hours)), 100.0 * heavy / len(orders),
               100.0 * impossible / len(orders), gold_mean, gph))
        rows[lv] = (hours, heavy, impossible, gold_mean, gph)

    print("\nGhi chu: 'vang/gio' = vang tb moi don / thoi gian gom tb moi don")
    print("         (nguoi choi chi lam mot don mot luc, dung toan bo o dat cho mon do).")
    print("         Bang 9 o cho phep gom SONG SONG => con so that con cao hon nhieu.")

    print("\nDON NANG NHAT tung cap (top 3 theo thoi gian gom):")
    for lv in levels:
        orders = results[lv]["orders"]
        scored = sorted(((order_gather_hours(o, lv), o) for o in orders),
                        key=lambda x: -x[0])
        seen = set()
        shown = 0
        for h, o in scored:
            key = tuple(sorted(i for i, _ in o.lines))
            if key in seen:
                continue
            seen.add(key)
            print("  cap %-3d %s  %-52s %d vang" %
                  (lv, fmt_hours(h), " + ".join("%dx%s" % (a, i) for i, a in o.lines),
                   o.reward_gold))
            shown += 1
            if shown >= 3:
                break
    return rows


# ------------------------------------------------------------------ KICH BAN D
# --- ngan sach thao tac: mot nguoi choi mobile nhanh ~30 cham/phut ---
TAPS_PLANT = 2        # chon hat + cham o
TAPS_HARVEST = 1
TAPS_PEN_FEED = 1
TAPS_PEN_COLLECT = 1
TAPS_DELIVER = 3      # mo bang + chon phieu + bam Giao


def kich_ban_d(minutes=60, start_level=5, seed=42, taps_per_min=30, label=""):
    hr("KICH BAN D%s - VONG DOI %d PHUT (bat dau cap %d, %s)"
       % (label, minutes, start_level,
          "gioi han %d thao tac/phut" % taps_per_min if taps_per_min else "KHONG gioi han thao tac"))
    rng = random.Random(seed)
    inv = defaultdict(int)
    level = start_level
    exp = 0
    gold = 400
    delivered = 0
    gold_earned = 0
    exp_earned = 0
    seed_spent = 0
    taps_used = 0
    dry_seconds = 0
    dry_streak = 0
    dry_max = 0
    dry_long = 0         # so lan bang kho lien tuc > 60s
    churn = 0            # so don bi HE THONG thay the (nguoi choi mat cong gom do)
    exp_farm = 0         # EXP tu trong trot / chuong (de so voi EXP tu don)

    board = Board(level, inv, rng)
    board_sig = [id(o) for o in board.orders]

    plot_free_at = [0.0] * NORMAL_PLOTS
    plot_crop = [None] * NORMAL_PLOTS
    pen_ready_at = None
    has_chicken_pen = False

    T = minutes * 60.0
    t = 0.0
    STEP = 1.0
    log = []
    lvl_at = {}

    def budget_left():
        if not taps_per_min:
            return 10 ** 9
        return int(taps_per_min / 60.0 * (t + STEP)) - taps_used

    def crop_choice():
        want = Counter()
        for o in board.orders:
            if o is None:
                continue
            for iid, amt in o.lines:
                c = crop_of_item(iid)
                if c and c[3] <= level:
                    want[c[0]] += max(0, amt - inv.get(iid, 0))
        for cid, _ in want.most_common():
            return cid
        return "rice"

    def track_churn():
        nonlocal churn, board_sig
        new_sig = [id(o) for o in board.orders]
        old = set(board_sig)
        churn += sum(1 for s in new_sig if s not in old)
        board_sig = new_sig

    while t < T:
        # do "bang kho" TRUOC khi nguoi choi lam gi
        if board.count_deliverable() == 0:
            dry_seconds += STEP
            dry_streak += STEP
            dry_max = max(dry_max, dry_streak)
        else:
            if dry_streak > 60:
                dry_long += 1
            dry_streak = 0

        # ---- thu hoach + trong lai
        for p in range(NORMAL_PLOTS):
            if plot_free_at[p] > t:
                continue
            if plot_crop[p] is not None:
                if budget_left() < TAPS_HARVEST:
                    continue
                taps_used += TAPS_HARVEST
                cid = plot_crop[p]
                hid, gs, ha, ul, sg, kind = CROPS[cid]
                inv[hid] += ha
                exp += 5
                exp_farm += 5
                plot_crop[p] = None
            if budget_left() < TAPS_PLANT:
                continue
            cid = crop_choice()
            hid, gs, ha, ul, sg, kind = CROPS[cid]
            if gold >= sg:
                gold -= sg
                seed_spent += sg
                taps_used += TAPS_PLANT
                plot_crop[p] = cid
                plot_free_at[p] = t + grow_real(gs)

        # ---- chuong ga
        if not has_chicken_pen and level >= 2 and gold >= 300:
            gold -= 100
            seed_spent += 100
            has_chicken_pen = True
        if has_chicken_pen:
            if pen_ready_at is not None and pen_ready_at <= t and budget_left() >= TAPS_PEN_COLLECT:
                taps_used += TAPS_PEN_COLLECT
                inv["chicken_meat"] += 4
                inv["egg"] += 4
                exp += 25
                exp_farm += 25
                pen_ready_at = None
            if pen_ready_at is None and inv.get("rice", 0) >= 1 and budget_left() >= TAPS_PEN_FEED:
                taps_used += TAPS_PEN_FEED
                inv["rice"] -= 1
                pen_ready_at = t + 30

        # ---- giao don
        board.level = level
        changed = True
        while changed and budget_left() >= TAPS_DELIVER:
            changed = False
            for i in range(len(board.orders)):
                if board.is_deliverable(board.orders[i]):
                    o = board.try_deliver(i)
                    if o:
                        taps_used += TAPS_DELIVER
                        gold += o.reward_gold
                        gold_earned += o.reward_gold
                        exp += o.reward_exp
                        exp_earned += o.reward_exp
                        delivered += 1
                        log.append((t, str(o)))
                        changed = True
                        break
            track_churn()

        board.on_inventory_changed()
        track_churn()

        while exp >= exp_required(level):
            exp -= exp_required(level)
            level += 1
            lvl_at.setdefault(level, t)
            board.level = level
            board.refill_and_balance()
            track_churn()

        t += STEP

    print("Bat dau: cap %d, 400 vang, kho rong, %d o dat, 0 chuong." % (start_level, NORMAL_PLOTS))
    print("Bot: luon trong cay ma bang don dang can nhat; mua Chuong Ga (100v) khi du tien;")
    print("     giao ngay khi du hang; khong bo don nao.\n")
    print("  So don giao duoc trong %d phut : %d  (%.1f don/phut)"
          % (minutes, delivered, delivered / float(minutes)))
    print("  Vang thuong nhan duoc          : %d" % gold_earned)
    print("  Vang chi (hat giong + chuong)  : %d" % seed_spent)
    print("  Vang rong                      : %+d  (%.0f vang/gio)"
          % (gold_earned - seed_spent, (gold_earned - seed_spent) * 60.0 / minutes))
    print("  EXP tu don hang                : %d  (chua tinh EXP trong/chuong)" % exp_earned)
    print("  Cap cuoi phien                 : %d  (bat dau %d, +%d cap)"
          % (level, start_level, level - start_level))
    print("  Vang trong tui cuoi phien      : %d" % gold)
    print("  Tong thao tac (cham man hinh)  : %d  (%.1f/phut)"
          % (taps_used, taps_used / float(minutes)))
    print("  EXP tu trong trot + chuong     : %d   (don hang chiem %.0f%% tong EXP)"
          % (exp_farm, 100.0 * exp_earned / max(1, exp_earned + exp_farm)))
    print("  Bang kho (0 don giao duoc)     : chuoi DAI NHAT %.0fs, so lan kho >60s: %d"
          % (dry_max, dry_long))
    print("     (tong %.0fs = %.1f%% phien, nhung phan lon la ngay SAU khi vua don sach bang)"
          % (dry_seconds, 100.0 * dry_seconds / T))
    print("  So don bi HE THONG thay the    : %d  (%.1f/phut - nguoi choi thay bang tu doi)"
          % (churn - SLOT_COUNT, max(0, churn - SLOT_COUNT) / float(minutes)))
    if lvl_at:
        ks = sorted(lvl_at)
        print("  Moc len cap: " + ", ".join("L%d@%.0fs" % (k, lvl_at[k]) for k in ks[:12]))
    print("\n  8 don dau tien duoc giao:")
    for tt, s in log[:8]:
        print("    t=%5.0fs  %s" % (tt, s))
    return dict(delivered=delivered, gold=gold_earned, exp=exp_earned,
                level=level, dry=dry_seconds, taps=taps_used)


# ------------------------------------------------------------------ KICH BAN E
def kich_ban_e():
    hr("KICH BAN E - DON HANG MON AN (cap 6-10)")
    print("Bac QuenTay (cap 6-12): tran gia mon an = %d  =>  chi mon <= 175 vang."
          % max_dish_price_for(TIER_QUENTAY))
    print("So luong mon nau moi dong: %s   |   Toi da %d mon nau / don\n"
          % (str(get_dish_amount_range(TIER_QUENTAY)), max_dishes_for(TIER_QUENTAY)))

    for lv in (6, 7, 8, 9, 10):
        pool = build_pool(get_tier_for_level(lv), lv)
        dishes = [p for p in pool if p.Category == MONAN]
        print("cap %-3d  mon an vao duoc pool (%d): %s"
              % (lv, len(dishes), ", ".join(p.ItemId for p in dishes) or "(khong co)"))

    print("\nPhan tich tung mon an co the ra don o cap 6-12:")
    hdr = ("%-24s %5s %8s %-34s %10s %9s %9s" %
           ("MON", "cap", "gia", "NGUYEN LIEU (1 moi thu)", "gom 1 mon", "x2 mon", "canhbao"))
    print(hdr)
    print("-" * len(hdr))
    for did, (ul, ing, sea) in sorted(DISHES.items(), key=lambda x: base_price(x[0])):
        if base_price(did) > 175:
            continue
        lv = max(ul, 6)
        r = rate_per_hour(did, lv)
        warn = []
        for g in ing + sea:
            if g in UNFARMABLE:
                warn.append("MUA:" + g)
        if did == "nuoc_mia_chanh":
            warn.append("KHONG-NAU-NOI(score<=30)")
        one = 1.0 / r if r > 0 else float("inf")
        print("%-24s %5d %8d %-34s %10s %9s  %s" %
              (did, ul, base_price(did), ",".join(ing + sea)[:34],
               fmt_hours(one), fmt_hours(2 * one), " ".join(warn)))

    print("\nMo phong 10.000 don o cap 6..12, chi giu don CO MON AN:")
    hdr2 = "%-4s %8s %8s %10s %10s %10s %10s" % (
        "Cap", "%co mon", "sl mon", "vang tb", "gom tb", "% >2gio", "vang/gio")
    print(hdr2)
    print("-" * len(hdr2))
    for lv in range(6, 13):
        rng = random.Random(5000 + lv)
        dish_orders = []
        tot = 10000
        for _ in range(tot):
            o = generate(lv, rng)
            if any(category(i) == MONAN for i, _ in o.lines):
                dish_orders.append(o)
        if not dish_orders:
            print("%-4d %8s" % (lv, "0%"))
            continue
        qty = [a for o in dish_orders for i, a in o.lines if category(i) == MONAN]
        hs = [order_gather_hours(o, lv) for o in dish_orders]
        hs_f = [h for h in hs if h != float("inf")]
        heavy = sum(1 for h in hs_f if h > 2.0)
        gm = statistics.mean(o.reward_gold for o in dish_orders)
        print("%-4d %7.1f%% %8.2f %10.0f %10s %9.1f%% %10.0f" %
              (lv, 100.0 * len(dish_orders) / tot, statistics.mean(qty), gm,
               fmt_hours(statistics.mean(hs_f)) if hs_f else "-",
               100.0 * heavy / len(hs_f) if hs_f else 0,
               gm / statistics.mean(hs_f) if hs_f else 0))

    print("\nSo lan mo bep can thiet cho 1 don mon an (bac QuenTay 1-2 mon, LanhNghe 1-3, BacThay 2-4):")
    for tier in (TIER_QUENTAY, TIER_LANHNGHE, TIER_BACTHAY):
        lo, hi = get_dish_amount_range(tier)
        print("  %-9s: %d-%d lan nau x %.0fs = %.0f-%.0fs bam bep + di chuyen 2 scene"
              % (TIER_NAME[tier], lo, hi, COOK_SECONDS, lo * COOK_SECONDS, hi * COOK_SECONDS))


# ------------------------------------------------------------------ BUOC 4
def buoc4_so_gia(results, levels=(1, 3, 5, 8, 12, 16, 20, 25, 30)):
    hr("BUOC 4 - SO GIA: GIAO DON vs BAN THANG O QUAY HANG (x1.3)")
    hdr = ("%-4s %10s %10s %8s %10s %9s %9s" %
           ("Cap", "vang don", "ban quay", "ti le", "chenh", "%don lo", "muaCho?"))
    print(hdr)
    print("-" * len(hdr))
    for lv in levels:
        orders = results[lv]["orders"]
        ratios, dons, quays, loss = [], [], [], 0
        exploit = 0
        for o in orders:
            stall = sum(suggested_unit_price(i) * a for i, a in o.lines)
            buy = sum(market_buy_price(i) * a for i, a in o.lines)
            dons.append(o.reward_gold)
            quays.append(stall)
            ratios.append(o.reward_gold / float(stall) if stall else 0)
            if o.reward_gold <= stall:
                loss += 1
            if o.reward_gold > buy:
                exploit += 1
        print("%-4d %10.0f %10.0f %7.3fx %+10.0f %8.1f%% %8.1f%%" %
              (lv, statistics.mean(dons), statistics.mean(quays),
               statistics.mean(ratios),
               statistics.mean(dons) - statistics.mean(quays),
               100.0 * loss / len(orders), 100.0 * exploit / len(orders)))
    print("\n  'ti le'    = vang don / vang neu ban thang o Quay Hang (gia goc x1.3)")
    print("  '%don lo'  = ti le don ma giao con it vang hon ban thang (PHAI = 0%) -> DAT")
    print("  'muaCho?'  = ti le don ma MUA THANG O CHO (gia goc x1.5) roi giao van CO LAI.")
    print("               Cho co HAM: 10 tin/lan, refresh 300s, moi item toi da 2 tin,")
    print("               MonAn 1-3 don vi/tin, gia dao dong x0.75-1.25 (LocalMarketProvider).")
    print("               => khong phai may in tien, nhung O BAC THAY (cap 21+) day la duong")
    print("               tat hop le: ~85%% don co the mua lai roi giao ma van lai.")


def bang_exp():
    hr("PHU LUC - DUONG CONG EXP (PlayerProgressManager: 40 + n*10 + n*n*3/20)")
    cum = 0
    line = []
    for lv in range(1, 31):
        need = exp_required(lv)
        cum += need
        line.append("L%d->%d:%d(tong %d)" % (lv, lv + 1, need, cum))
    for i in range(0, len(line), 4):
        print("  " + "   ".join(line[i:i + 4]))


def do_nhay_thoi_gian_trong():
    """Neu chinh realTimeMultiplier thi thoi gian gom don doi the nao."""
    global REAL_TIME_MULT
    hr("DO NHAY - CHINH realTimeMultiplier (dang la 0.30)")
    old = REAL_TIME_MULT
    lvls = (1, 5, 12, 20, 30)
    print("%-8s " % "mult" + " ".join("%14s" % ("cap %d" % l) for l in lvls))
    print("-" * (8 + 15 * len(lvls)))
    for m in (0.3, 0.5, 1.0):
        REAL_TIME_MULT = m
        _UIC_CACHE.clear()
        cells = []
        for lv in lvls:
            rng = random.Random(999 + lv)
            hs = []
            for _ in range(3000):
                o = generate(lv, rng)
                h = order_gather_hours(o, lv)
                if h != float("inf"):
                    hs.append(h)
            cells.append(fmt_hours(statistics.mean(hs)) if hs else "-")
        print("%-8.2f " % m + " ".join("%14s" % c for c in cells))
    REAL_TIME_MULT = old
    _UIC_CACHE.clear()
    print("\n  => Ngay o muc 1.00 (thoi gian trong GOC trong CropData) thoi gian gom mot don")
    print("     van chi tinh bang PHUT. Nut co chai KHONG nam o thoi gian trong.")


def kiem_lo_hong_milk(n=20000):
    hr("KIEM RIENG - LO HONG 'SUA' (cap CHO 6, Chuong Bo Sua mo cap 8 / 2000 vang)")
    print("%-6s %10s %12s %14s" % ("Cap", "%don co sua", "sl sua tb", "ket luan"))
    print("-" * 46)
    for lv in (6, 7, 8):
        rng = random.Random(31337 + lv)
        hit, qty = 0, []
        for _ in range(n):
            o = generate(lv, rng)
            for iid, a in o.lines:
                if iid == "milk":
                    hit += 1
                    qty.append(a)
                    break
        verdict = "DON CHET (chua co chuong)" if lv < 8 else "ok (neu du 2000 vang)"
        print("%-6d %9.2f%% %12.2f %14s"
              % (lv, 100.0 * hit / n, statistics.mean(qty) if qty else 0, verdict))
    print("\n  => O cap 6 va 7 don doi Sua la don KHONG THE hoan thanh bang LAO DONG:")
    print("     Chuong Bo Sua (Config_Pen04_BoSua, productItemId=milk) mo o cap 8 / 2000 vang,")
    print("     nhung MarketPriceTable ghi milk UnlockLevel=6 nen bo sinh don cho ra tu cap 6.")
    print("     Loi thoat: mua sua o Cho (levelCeiling = cap+2 nen co ban tu cap 4, gia ~60v/dv,")
    print("     stock gioi han 2-6 dv/tin) hoac bam Bo Don. Trung binh don doi 7 don vi sua.")


def tom_tat():
    hr("TOM TAT - TRA LOI TRUC TIEP TUNG CAU HOI")
    print("""
A) Nguoi choi moi, kho rong, cap 1
   - Bang 9 don luc vao game: 0/9 giao duoc (dung nhu ky vong, kho rong).
   - Sau 1 luot thu hoach 8 o lua (54 giay): 6/9 giao duoc.
   - Khong co don nao doi thu cap 1 KHONG THE co. Rổ cap 1 chi co {rice, bapcai}.
   - Rui ro nho: bapcai can mua hat 45v x 8 o = 360/400 vang von ban dau.

B) 10.000 luot sinh don moi cap
   - Phan bo so dong, so luong, vang, exp: xem bang KICH BAN B.
   - Don doi item CHUA MO KHOA           : 0  (bo loc UnlockLevel chay dung)
   - Don doi item MarketEnabled=false    : 0  (bo loc cờ chay dung)
   - Don doi thang gia vi khong farm duoc: 0  (bo loc danh muc GiaVi chay dung)
   - NHUNG: 3-5% don doi 'nuoc_mia_chanh' = mon KHONG BAO GIO nau thanh cong,
     va 5-18% don doi mon an CAN 'herbs' (chi mua duoc o cho).

C) Tinh kha thi
   - 0.00% don can hon 2 gio de gom, o MOI cap. Khong co don "qua nang".
   - Don nang nhat toan game: ~7 phut (cap 30, 4 dong x 12 don vi).
   - Van de NGUOC LAI: don qua NHE. Vang/gio tu 22.000 den 55.000.

D) 60 phut choi cap 5 (gioi han 30 thao tac/phut)
   - 142 don, +26.888 vang rong, 6.794 EXP tu don, cap 5 -> 36.
   - Bang chi kho toi da 58 giay, khong lan nao kho qua 60 giay.

E) Don mon an
   - Cap 6-12 chi cho 1 mon nau/don, so luong 1-2 -> KHONG bat nau 3 mon. Hop ly.
   - 1 don mon an trung binh ton ~1.5 phut, tra ~500 vang. Kha thi.
   - Loi that o day khong phai "qua nang" ma la "co mon khong nau noi".
""")


def main():
    print("#" * 78)
    print("#  MO PHONG HE DON HANG - Cooking-Game-2D")
    print("#" * 78)
    buoc2_bang_san_xuat()
    audit_unlock_gaps()
    kich_ban_a()
    res = kich_ban_b()
    kich_ban_c(res)
    kich_ban_d(60, 5, 42, 30, "-1")
    kich_ban_d(60, 1, 43, 30, "-2")
    kich_ban_d(60, 5, 42, 0, "-3")
    kich_ban_e()
    buoc4_so_gia(res)
    kiem_lo_hong_milk()
    do_nhay_thoi_gian_trong()
    bang_exp()
    tom_tat()


if __name__ == "__main__":
    main()
