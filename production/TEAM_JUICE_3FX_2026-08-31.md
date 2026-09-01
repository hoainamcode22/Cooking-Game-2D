# 🎆 TEAM JUICE PACK — PHÂN TÍCH 3 VIDEO & KẾ HOẠCH 3 TASK (2026-08-31)

## PHẦN A — PHÂN TÍCH CHUYÊN SÂU 3 VIDEO (đã bóc ~40 khung hình)

### Video "Xây xong công trình" (Township, 13.7s)
Nhịp hiệu ứng: công trình xây xong → **bọc thành "bánh kem"** ruy băng + 5-6 BÓNG BAY màu
+ dấu "!" → user CHẠM → bóng bay thả bay lên, **nổ starburst xanh lá to** che một phần công trình
(sorting TRÊN công trình), số dân "+13" bật lên, khói bụi tan. Toàn bộ 1.5-2s, mọi mảnh đều TO
(bóng bay ~1/4 chiều cao công trình), rõ, màu bão hoà.

### Video "Tiền vàng" (Township, 34.4s)
1. Chạm công trình → đồng xu vàng TO (~90px) + số "50" bật giữa công trình, nảy 1 nhịp.
2. Nhận thưởng nhiệm vụ: hộp quà tím-vàng bật giữa màn → NỔ confetti pháo hoa (nền tối lại
   ~30% để hiệu ứng nổi) → 2 vật phẩm (gem xanh "6" + tiền "25") BUNG RA to → bay CONG lên HUD.
3. HUD nhận: counter đếm số chạy + **icon HUD nhún nảy** (scale ~1.3 rồi hồi).
→ Điểm ăn tiền: bung TRÒN ĐỀU → khựng 0.2s cho mắt kịp thấy → hút lần lượt so le → HUD nhún.

### Video "Level-Up popup" (16.2s)
Huy hiệu tròn lớn giữa màn: ngôi sao vàng số cấp trên đỉnh + **MASCOT (sói) cười toe** trong khung
tròn mây, ribbon đỏ "LEVEL UP", lấp lánh sao quanh viền. Dưới: **dải 6 Ô QUÀ** (xu 100, gói hạt NEW,
cà rốt NEW, bơ NEW, bánh NEW, vé x3) — từng ô NẢY VÀO lần lượt (easeOutBack), tag NEW đỏ xoay nhẹ.
Nút "Mở quà" xanh to. Nền game tối 60%.

## PHẦN B — HỌP SANDBOX: CHIA 3 TASK, 3 NHÓM

| Task | Nhóm | Code (đã giao khung sườn hôm nay) | Art chờ đội vẽ |
|---|---|---|---|
| T1 Pháo hoa chạm công trình | VFX Dev | `FX/CelebrationTapFX.cs` — MỚI: burst spark tròn + mưa confetti, layer Foreground order 500 (LUÔN trên công trình), to gấp ~1.8 FX cũ, unscaled time, fallback mảnh màu runtime | confetti_01..06, spark_star |
| T2 Vàng/gem bung tròn bay HUD | UI Dev | `UI/CurrencyBurstFlyFX.cs` — MỚI: PlayCoins/PlayGems(worldPos, n): bung vòng tròn easeOutBack → khựng 0.22s → bay bezier so le về Vangicon/GemIcon → HUD nhún (công thức CoDapHud) | icon_gold_v2 + icon_gem_v2 (BỘ ICON THỐNG NHẤT) |
| T3 Level-Up quà + mascot | UI Dev + Game Designer | `UI/LevelUpMascotUI.cs` — MỚI: mascot 12 frame @12fps xoay vòng theo level, nhún + lắc lư vô hạn, fallback avatar tĩnh (chạy được NGAY) | 5 mascot × 12 frame |

Cả 3 file THUẦN CỘNG THÊM — không sửa file nào đang chạy, không đụng scene, compile độc lập.
Đội vẽ giao đúng folder Resources là hiệu ứng TỰ đổi sang art xịn, không cần sửa code lần 2.

## PHẦN C — 3 ĐIỂM WIRE (1 dòng/điểm — CHỜ SẾP GẬT là tôi chèn)

1. **T1**: `ConstructionManager` đã có event "công trình xây xong (data, điểm neo world…)" →
   nghe event, lần chạm ĐẦU TIÊN vào công trình mới xây: `CelebrationTapFX.Play(anchor, 1.3f);`
2. **T2**: gọi tại các điểm NHẬN THƯỞNG (không gọi trong FarmEconomyManager vì nó bắn cả lúc TRỪ tiền):
   claim nhiệm vụ/thành tựu (UnifiedTaskPopupUI), giao đơn (OrderBoard), thưởng tàu (TrainManager),
   phục vụ khách (TouristRewardCalculator chỗ cộng vàng). CoinFlyFX/GemFlyFX cũ giữ nguyên,
   chuyển dần từng call site sau khi Sếp nhìn bản mới thấy ưng.
3. **T3**: trong `LevelUpPopupUI` sau khi dựng badge: `LevelUpMascotUI.AttachTo(badgeRect, level);`

## PHẦN D — BẢNG QUÀ LEVEL-UP L1→L30, 6 Ô/CẤP (KINH TẾ — CẦN SẾP DUYỆT trước khi đổ data)

`LevelRewardConfig.giftItems` là List → KHÔNG cần sửa code, chỉ đổ thêm data qua tool
`LevelUpRewardDataSetupTool` (mở rộng generator, sinh lại L2-L30 với 6 ô).
Công thức đề xuất mỗi cấp N gồm 6 ô:
1. Vàng (giữ bảng cũ đã duyệt 700→2600)
2. Gem (giữ bảng cũ theo band)
3. Hạt giống unlock gần nhất ×(3+N/3) — quà "dùng ngay"
4. Nguyên liệu nấu phổ biến (trứng/sữa/bột xoay vòng) ×(2+N/4)
5. Món ăn thành phẩm unlock ở cấp đó ×1-2 — khoe món mới
6. Vé/booster (L5+: vé tàu ×1; L10+: thêm phân bón ×2; L20+: gem +5 bonus)
→ Con số chi tiết tôi sinh bảng preview cho Sếp duyệt TRƯỚC, duyệt xong mới chạy tool đổ asset.

## PHẦN E — RỦI RO ĐÃ TÍNH
- Không compile từ xa được → cả 3 file chỉ dùng API chuẩn (UGUI/SpriteRenderer/Coroutine),
  không generic lạ, không API Editor. Sếp mở Unity: 0 đỏ là chuẩn.
- Sorting: layer "Foreground" ĐÃ tồn tại (bài học vụ khách tàu bị cỏ che — không bịa tên layer).
- FX chạy unscaled time (bài học ConstructionCompleteFX: xây xong đúng lúc popup mở).
- MaxIcons=14 chống spam object khi nhận 999 vàng.
- Backup: 3 file đều MỚI 100% — muốn gỡ chỉ việc xoá file, không cần backup bảng cũ.

## ✅ CẬP NHẬT 2026-08-31 (tối): SẾP ĐÃ DUYỆT TOÀN BỘ — ĐÃ WIRE XONG
- T1: `CelebrationTapWatcher.cs` (MỚI) tự nghe OnConstructionComplete, chạm công trình mới xây → nổ pháo hoa.
- T2: `CurrencyFXDirector.cs` (MỚI) nghe OnGoldAddedFx/OnGemAddedFx → burst vòng tròn mới; TẮT ÊM CoinFlyFX/GemFlyFX cũ bằng enabled=false (OnDisable của chúng tự gỡ event) — hoàn tác = xoá file này.
- T3: 1 dòng trong `LevelUpPopupUI.PopulateUI` gắn mascot; tool reward mở rộng 4 ô quà/cấp (ExtraGifts, id đã verify); bảng preview: `BANG_QUA_LEVELUP_L2_L30_2026-08-31.md`.
- Backup file bị sửa: `production/backup_juice_wire_2026-08-31/`.

## CẦN SẾP (một lượt Unity)
1. Mở Unity đợi compile — **0 lỗi đỏ** (7 file mới/sửa đợt này).
2. Bấm `Tools ▸ Farm Game ▸ Setup Level Up Popup ▸ Setup Reward Data (L2-L30)` → đọc log icon ✅/⚠ → Ctrl+S.
3. Play test: xây 1 công trình → chạm thử · nhận vàng bất kỳ → xem burst tròn + HUD nhún · lên cấp → xem mascot (tạm là avatar tĩnh nhún nhảy, có sheet 12 frame sẽ tự mượt).
4. Chuyển prompt pack `PROMPT_SPRITE_FORGE_JUICE_PACK_2026-08-31.md` cho GPT/sprite-forge (kèm folder ref_avatars).
