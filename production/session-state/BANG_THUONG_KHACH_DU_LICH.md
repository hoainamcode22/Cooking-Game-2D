# Bảng thưởng khách du lịch — công thức V2.1 (BOAT-002)

> Người viết: Dev A · Lead chốt công thức 2026-08-29 · thay công thức V2.0 "Σ giá nguyên liệu × 2".

---

## ⚠️ TÌNH TRẠNG DỮ LIỆU — ĐỌC TRƯỚC

**File này CHƯA có đủ 38 dòng, và cố ý không bịa cho đủ.**

38 asset `Assets/_Game/Farm/data/Farm_Cooking/Dish_*.asset` **không có trong môi trường tôi làm việc** — thư mục đó trong bản staged rỗng, chỉ có `DishData.cs` (định nghĩa lớp). Tôi kiểm được:

```
$ ls Assets/_Game/Farm/data/Farm_Cooking/     → 0 file
$ find . -name "Dish_*.asset"                 → 0 file
```

Nên bảng dưới đây chỉ gồm **7 món có số liệu THẬT** — chính 7 dòng Lead đã parse từ project của Sếp và gửi cho tôi. 31 món còn lại tôi không có `sellPrice` / `rewardExp` / danh sách nguyên liệu, và **bịa số vào bảng cân bằng thì tệ hơn là thiếu bảng**.

**Cách lấy đủ 38 dòng (1 nút, 5 giây):** mở Unity trên máy Sếp →
`Tools/Farm Game/Tourist Boat/Xuất bảng thưởng khách (38 món)`
Tool (`Assets/_Game/Farm/Editor/TouristRewardTableExporter.cs`, giao kèm) quét mọi asset `DishData`, gọi **đúng** `TouristRewardCalculator` mà game đang chạy, rồi **ghi đè chính file này** với đủ số dòng + cột so sánh công thức cũ + đánh dấu món nào còn thiếu data. Chạy lại sau mỗi lần tuning là bảng luôn khớp build.

*(Muốn cột "vàng CŨ" đúng giá nguyên liệu thật thì vào Play Mode rồi chạy tool — ở Edit Mode `BasePriceBook` thường chưa có provider giá, tool sẽ tự ghi cảnh báo vào file.)*

---

## Công thức (đã vào code, verify bằng test chạy thật)

```
vàng = round( sellPrice × diffMult × rarityBonus × touristGoldMultiplier )   [sàn 1]
    diffMult:  Easy 1.00 · Normal 1.15 · Hard 1.35        (config)
    rarityBonus = 1 + 0.05×(số nguyên liệu Rare) + 0.12×(số Epic), trần 1.50   (config: rarityBonusCap)
    touristGoldMultiplier = 1.00                          (config — núm chỉnh lạm phát)
    sellPrice <= 0  → FALLBACK: Σ giá nguyên liệu chính × rewardIngredientMultiplier (2)

exp = round( rewardExp × expMult × touristExpMultiplier )                    [sàn 1]
    expMult: Easy 1.00 · Normal 1.10 · Hard 1.25          (hằng trong code)
    touristExpMultiplier = 0.40                           (config — HÃM LẠM PHÁT CẤP ĐỘ, xem QA M-9)
    rewardExp <= 0   → FALLBACK: (8 + unlockLevel × 1.5) rồi mới nhân 2 hệ số trên
```

**EXP chỉ làm tròn MỘT LẦN ở cuối** (nhân hết hệ số rồi mới `round`) — round 2 lần lệch tới 1 EXP ở món nhỏ và bảng này sẽ không tái lập được.

Làm tròn dùng `MidpointRounding.AwayFromZero` (không dùng `Mathf.RoundToInt` vì nó tròn về số chẵn: 66.5 → 66, lệch khỏi chữ `round()` trong bảng cân bằng).

### Vì sao đổi khỏi "Σ giá nguyên liệu × 2"

1. **Bảng 38 món đã được cân bằng rất kỹ** theo level (`sellPrice` 62 → 884, Lv1 → Lv30). Tái dùng `sellPrice` là cách **duy nhất** giữ được đường cong kinh tế của Sếp; mọi công thức tự tính từ nguyên liệu đều vẽ lại một đường cong khác.
2. **Công thức cũ LỖ HƠN BÁN CHỢ:** `khoai_tay_chien` có 1 nguyên liệu → Σ×2 = **50 vàng**, trong khi bán chợ được **95**. Phục vụ khách du lịch thành lựa chọn tệ ⇒ không ai làm. Đây là lỗi cân bằng nặng hơn cả chuyện "thưởng không theo độ khó".
3. **`dish.rewardGold` cố ý KHÔNG dùng:** nó luôn bằng đúng `round(sellPrice × 0.25)` ở cả 7 mẫu kiểm được (62→16 · 76→19 · 95→24 · 315→79 · 400→100 · 823→206 · 884→221) ⇒ đó là "vàng khi nấu đạt trong minigame", không phải giá trị món. Dùng nó cho khách sẽ trả quá bèo.
4. `rarityBonus` là **chỗ duy nhất** dùng `IngredientTier`, có trần 1.5 để món 5 nguyên liệu Epic không trả gấp đôi.

### [QA M-9] Vì sao EXP phải nhân 0.4

QA vòng 3 chỉ ra chỗ cả tôi và Lead đều chưa lường: **nấu xong trong minigame ĐÃ cộng `rewardExp × hệ số điểm`** (`CookingChallengeManager:403`), rồi phục vụ khách lại cộng **thêm** một lần nữa ⇒ mỗi món cho **~2× EXP thiết kế**.

Đo cụ thể: trần level 30, tổng EXP L10 → L30 chỉ **5.619**, mà một chuyến khách (3-6 khách) cho **128-306 EXP** ⇒ ở L10 người chơi lên **0,9-2,2 level MỘT CHUYẾN**, và từ lúc mở bến tới hết nội dung game chỉ còn **1,2-3,7 giờ**. `expMult` lúc đó là `const` nên không có núm nào hãm.

Chốt: thêm knob `touristExpMultiplier = 0.4`. Lý do chọn 0.4 — **nấu ăn là phần chơi chính nên giữ trọn EXP của nó; phục vụ khách là thưởng thêm, ~40% là đủ khích lệ mà không phá đường cong level.** Tổng còn ~**1,4×** thiết kế thay vì 2,25×.

---

## Bảng — 7 món có số liệu thật

| dishId | difficulty | Lv | sellPrice | Vàng khách trả | **EXP khách trả** (×0.4) | EXP nếu KHÔNG hãm | rewardExp gốc | rewardGold (minigame) | Mới vs bán chợ | Số nguyên liệu |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `com_chien_bap_cai` | Easy | 1 | 62 | **62** | **1** | 3 | 3 | 16 | +0% | 3 |
| `sup_ngo_vang` | Easy | 2 | 76 | **76** | **2** | 6 | 6 | 19 | +0% | 3 |
| `khoai_tay_chien` | Easy | 5 | 95 | **95** | **6** | 15 | 15 | 24 | +0% | 1 |
| `bo_xao_tieu` | Normal | 10 | 315 | **362** | **20** | 50 | 45 | 79 | +15% | 3 |
| `pho_bo_tai` | Hard | 9 | 400 | **540** | **27** | 68 | 54 | 100 | +35% | 5 |
| `bo_ham_bi_do_kem` | Hard | 26 | 823 | **1111** | **78** | 195 | 156 | 206 | +35% | 5 |
| `salad_dua_hau_bo_ap_chao` | Hard | 30 | 884 | **1193** | **90** | 225 | 180 | 221 | +35% | 5 |

Cột "Vàng/EXP khách trả" ở trên **không phải tính tay**: có bộ test console `tests/unit/touristboat/TouristRewardCalculatorTests.cs` gọi **đúng** `TouristRewardCalculator` của game với 7 món này (nguyên liệu tier Basic, `touristGoldMultiplier = 1.0`, `touristExpMultiplier = 0.4`) — **64/64 PASS**. Cột vàng khớp đúng 4 mốc Lead tính tay (62 · 362 · 540 · 1193); cột EXP là số **sau khi hãm** theo QA M-9.

### So sánh với công thức CŨ

Chỉ có 1 điểm dữ liệu Lead đo được (tôi không tra được giá nguyên liệu trong sandbox nên **không điền cột này cho 6 món kia**):

| dishId | Vàng CŨ (Σ×2) | Vàng MỚI | Bán chợ | Nhận xét |
|---|---:|---:|---:|---|
| `khoai_tay_chien` | 50 | **95** | 95 | Cũ **lỗ 47%** so với bán chợ ⇒ hệ boat vô nghĩa. Mới bằng đúng giá chợ. |

Tool xuất bảng sẽ điền đủ cột này cho cả 38 món.

---

### EXP mỗi chuyến sau khi hãm

Một chuyến 3-6 khách, lấy `pho_bo_tai` (27 EXP) làm mốc giữa: **81-162 EXP/chuyến** thay vì 204-408 nếu không hãm. Với tổng 5.619 EXP cho cả đoạn L10 → L30, người chơi cần khoảng **35-70 chuyến** thay vì 14-27 — và đó là chưa tính EXP họ đã nhận từ chính việc nấu.

---

## Kiểm chứng hành vi (chạy thật, không phải suy luận)

| Ca | Kết quả |
|---|---|
| Món Easy, sellPrice 62 | 62 vàng — bằng đúng giá bán chợ, không lỗ |
| Món Hard cùng tầm giá | +35% so với Easy — nấu món khó có lời hơn |
| 2 Epic + 1 Rare + 1 Epic **gia vị**, sell 100 Easy | **129** vàng (1 + 0.12×2 + 0.05×1 = 1.29) — gia vị bị loại đúng [QA M-4] |
| 5 Epic, sell 100 Easy | **150** vàng — rarityBonus 1.60 bị **kẹp trần 1.50** |
| `touristGoldMultiplier = 0.8`, `pho_bo_tai` | 540 → **432** — núm chống lạm phát ăn ngay |
| `sellPrice = 0` (asset chưa điền) | rơi về đường cũ, **≥ 1 vàng**, log cảnh báo 1 lần/món |
| `rewardExp = 0`, Lv7 Normal | suy `(8 + 7×1.5) × 1.10 × 0.4` = **8 EXP** |
| `touristExpMultiplier = 1.0` (tắt hãm) | `pho_bo_tai` về đúng **68 EXP** — chứng minh knob ăn thẳng vào EXP |
| Món 3 EXP × 0.4 = 1.2 | **1 EXP** — sàn 1, không bao giờ 0 (0 EXP nhìn như bug) |
| `config == null` (manager chưa Awake) | dùng **0.4**, không rơi về 1.0 — mặc định an toàn phải là mặc định đã hãm |

Thứ tự an toàn của bên gọi **không đổi** (`TouristVisitorManager` ①②③④): tính thưởng TRƯỚC → thiếu điều kiện thì huỷ, **không trừ kho** → mới `RemoveItem` → mới `AddGold/AddExp`.

---

## Núm chỉnh cho Sếp (TouristBoatConfig)

| Field | Default | Dùng khi |
|---|---:|---|
| `touristGoldMultiplier` | 1.00 | Thấy lạm phát vàng → hạ 0.9/0.8; thấy khách trả bèo → nâng 1.1. Ăn vào **tất cả** vàng khách trả |
| `diffMultEasy` | 1.00 | Món Easy trả đúng giá chợ |
| `diffMultNormal` | 1.15 | Nới/thu khoảng cách Normal vs Easy |
| `diffMultHard` | 1.35 | Nới/thu khoảng cách Hard vs Easy |
| `rarityBonusCap` | 1.50 | Trần thưởng thêm cho nguyên liệu quý |
| `touristExpMultiplier` | **0.40** | **[QA M-9]** Hãm lạm phát cấp độ. Nấu xong đã cộng EXP món một lần rồi — **đừng đặt > 1.0**, người chơi sẽ lên hết cấp trần trong 1-2 giờ |
| `rewardIngredientMultiplier` | 2 | **Chỉ còn dùng cho đường fallback** (món chưa điền `sellPrice`) |

Rare +5% / Epic +12% mỗi nguyên liệu là **hằng trong code** (`TouristRewardCalculator`), không mở knob — mở thêm 2 ô nữa chỉ làm bảng config rối mà gần như không ai chỉnh.

---

## Ghi chú giá mở bến 2 (để Sếp tự quyết sau khi chơi thật)

QA đề nghị tăng `dock2GoldCost` từ **2.000 → 6.000-8.000** vàng vì vàng giờ dồi dào hơn công thức cũ.

**Lead quyết định GIỮ 2.000**: con số này nằm trong GDD BOAT-001 đã được Sếp duyệt, và cổ chai thật của hệ boat là **cái bếp** — muốn đạt trần lý thuyết phải nấu ~4,5 món mỗi 5 phút — nên không cần siết thêm ở chỗ mua bến.

Ghi lại đây để nếu chơi thật thấy mở bến 2 quá dễ thì Sếp có sẵn con số QA đã tính: `dock2GoldCost = 6000` (hoặc 8000 nếu muốn chặt), sửa 1 field trong `TouristBoatConfig`, không cần đụng code.
