# BÁO CÁO VÒNG 14c — 09/09/2026
6 việc Sếp nêu. **6/6 đã sửa xong.** (mục 6 cập nhật sau khi Sếp đính chính bộ nguyên liệu)

---

## 1. 🔴 MUA CHUỒNG THỨ 2 BỊ THAY THẾ CHUỒNG CŨ — ĐÃ SỬA

### Gốc rễ: **không phải lỗi save**, là code chủ động tắt chuồng cũ
`PlacementManager.DisablePlaceholderInScene()` (dòng 1692) tắt mọi object trong scene
**trùng đúng tên prefab**. Vòng 11 đã vá lỗi y hệt cho ô đất, nhưng **chỉ chừa
`PlotController`, quên chuồng**.

Chuỗi đã kiểm chứng bằng dữ liệu thật:
1. `Chuồng Gà.asset` → `prefabToBuild` = **`Pen_03`**, giá 100 vàng, itemID 107.
2. Scene có **đúng 2 object tên `Pen_03`**: dòng 500850 `m_IsActive: 1` (**chuồng thật Sếp đang nuôi**)
   và dòng 665812 `m_IsActive: 0` (placeholder).
3. Mua → clone tên `"Pen_03(Clone)"` nên **tự nó thoát**, nhưng **chuồng thật bị `SetActive(false)`**.
4. Kết quả: 1 tắt + 1 mới = **vẫn đúng 1 chuồng**, tiền đã trừ.
5. `LoadBuildings()` gọi lại hàm này mỗi lần mở game → chuồng cũ tắt vĩnh viễn.

### Đã sửa
Thêm guard `PenMiniPanelUI` song song với guard `PlotController` — **chuồng thứ 2, thứ 3 sẽ hiện ra bình thường.**

### ⚠️ CÒN MỘT LỖI THỨ HAI CHƯA SỬA — chuồng thứ 2 **vẫn chưa tăng năng suất**
`penId` nằm trên **ScriptableObject dùng chung** (`Config_Pen03_Ga.asset` → `penId: "pen_03"`),
không phải trên prefab. Nên mọi chuồng gà đọc/ghi chung 3 khoá
`PenState_pen_03` / `PenFood_pen_03` / `PenStartTime_pen_03`: cho ăn chuồng A thì chuồng B
cũng "đang nuôi", thu hoạch một cái là mất lượt cái kia.

Sửa được, nhưng **rủi ro làm mất chuồng đang nuôi dở** nên em không tự làm. Cách an toàn đã
thiết kế sẵn (giống khuôn `PlotController` vòng 11): thêm `penInstanceId` ở tầng MonoBehaviour,
**chuồng gốc giữ id = 0 để rơi về đúng khoá cũ**, chuồng mới mới cấp id tăng dần. Tuyệt đối
không dùng Guid, không bump `PenSaveVersion` (bump là hoàn thức ăn + reset mọi chuồng). Sếp gật là em làm.

### Cùng lỗi này còn ở đâu
| Hệ | Khoá | Mức |
|---|---|---|
| **Máy xay / máy chế biến** | `MILL_S{i}_*` — **khoá toàn cục, không có id máy**, UI lại là singleton | 🔴 P0 — mua máy thứ 2 hoàn toàn vô nghĩa |
| Nhà | `HouseSave_{houseId}_{x}_{y}` | 🟠 P1 — **dời nhà trong Edit Mode là mất tiến độ xây** |
| Ô đất / chậu hoa | `PLOT_*_{plotId}` | 🟢 Đã vá vòng 11 |

---

## 2. 🔴 KHUNG NGUYÊN LIỆU NÂNG CẤP — ĐÃ LÀM

### Sự thật: backend đã chạy đủ từ vòng 11, chỉ thiếu UI
- 5 asset nguyên liệu **đã có**: `go, da, kinh, dinh, son` (`Farm/data/item_taulua/`). **Thiếu `gach`** — code đã sẵn sàng, chỉ cần tạo asset, không phải sửa code.
- **Tàu lửa đã thưởng thật**: `TrainRewardData.asset` cho `da/go/dinh/kinh` mỗi thứ 8–10.
- **Bảng chi phí đã có**: cấp 1 = 1.500 vàng + gỗ 10 + đá 6 … cấp 6 = 55.000 vàng + 4 loại.
- **Trừ nguyên liệu đã đúng và an toàn** (hụt thì hoàn tiền).

### 3 lỗi làm Sếp không thấy gì
1. **Thông báo bị ghi đè trong cùng 1 frame**: `ShowUpgradeBlockedMessage()` rồi `RefreshUI()` ngay dòng sau → `RefreshUpgradeBox()` ghi đè lại text. Đúng hiện tượng "chữ nhấp nháy rồi mất".
2. **Nút bị disable khi thiếu đồ** → bấm không ra gì, không biết thiếu cái nào.
3. **`BuildMaterials.IconOf()` luôn trả `null`** — nó chỉ đọc `Resources.LoadAll`, mà 5 asset nguyên liệu **không nằm trong thư mục `Resources/`**. Mọi ô icon đều trống.

### Đã sửa
- **Khung mới `WarehouseUpgradeReqUI.cs`** — dựng **toàn bộ bằng code lúc chạy**, Sếp **không phải kéo tham chiếu nào trong Editor**. Gồm: nền mờ · bảng bo góc · tiêu đề "Cấp X → Y" · dòng vàng (xanh/đỏ) · **tối đa 6 ô nguyên liệu bo góc** (icon + tên + `đang có/cần`, ô xanh + dấu ✓ khi đủ, ô đỏ khi thiếu) · **nút X đóng** · nút NÂNG CẤP (xám + đổi chữ "CHƯA ĐỦ" khi thiếu) · tự cập nhật mỗi 0,5 giây.
- Bấm "Nâng cấp" giờ **mở khung** thay vì báo lỗi thoáng qua.
- Nút không còn bị disable khi thiếu đồ.
- `IconOf()` thêm registry, `WarehousePopupUI` nạp `extraItemDatabase` (đã chứa đủ 5 asset) vào → **icon hiện được, khỏi phải di chuyển asset**.

Em cố ý **không dùng** `Popup_WarehouseUpgrade` dựng sẵn trong scene: nó đang chết hoàn toàn
(không script nào wire, còn text placeholder "Warehouse"), nối lại tốn 12+ thao tác kéo tay và dễ sai.

---

## 3. 🟠 DECOR MỚI KHÔNG NHẤC LÊN ĐƯỢC — ĐÃ SỬA 24 PREFAB

### Gốc rễ: **hộp bấm nằm hoàn toàn BÊN DƯỚI hình vẽ**
Không phải thiếu component — 24/24 prefab **đã có đủ** `EditableBuilding` + `BoxCollider2D` +
`BuildingFootprintKit`. Vấn đề là collider chưa bao giờ được nắn theo art: tất cả đều để
`m_Offset (0, 0.5)` `m_Size (1, 1)` → hộp bấm world **y ∈ [0, 100]**.

Nhưng vòng 13 đã dịch con `Visual` lên để neo tâm ô. Kết quả đo:

| Prefab | Art bắt đầu ở y | Dải bấm còn lại |
|---|---|---|
| Đèn Lồng · Hoa Sen · **Đóm Củi Lửa** · Đèn Tường · Hoa Trắng · Hoa Đỏ · Bụi Cây Lớn | 100–215 | **0 — bấm không bao giờ trúng** |
| Đá Lớn · Thùng Gỗ · Cột Đèn · Bụi Cỏ | 87–99 | 1–13 |
| *(decor CŨ nhấc được)* | 75 | 25 |

### Đã sửa
Nắn lại `m_Offset`/`m_Size` của cả **24 prefab** theo đúng hộp bao hình vẽ thật (đọc kích thước
từ `.png.meta`, xử lý đúng pivot alignment 0/7/9). Hộp bấm mới **125–280 world rộng, 85–300 cao**,
trùng khít với art.

**Lỗi phụ sửa kèm:** cả 24 prefab đang mang `m_SortingLayerID: 1669604809` — **id này không tồn tại**
trong `TagManager.asset`, Unity rơi về Default nên decor mới vẽ chìm dưới nhà. Đã đổi sang
`Objects` (1471039481).

---

## 4. 🟠 ĐÓM CỦI LỬA KHÔNG CÓ LỬA — ĐÃ GẮN

**Lửa chưa bao giờ được gắn** — không phải bị tắt, không phải thiếu asset. Prefab chỉ có đúng
sprite `Sprite_logpile.png` (đống củi tĩnh), không có ParticleSystem / VFX / Light2D / Animator nào.

Trong project đã có sẵn `DayNightProceduralFire.cs` — script tự vẽ ngọn lửa bằng SpriteRenderer
unlit + flicker, **và comment trong nó ghi đúng `logpile=700`, tức được viết cho chính đống củi này**,
nhưng chưa ai gắn.

Đã thêm con `Fire` vào `Decor_DomCuiLua.prefab`: `Width 95`, `Height 150`, layer `Objects`,
order 1000, đặt ở y +210 world (ngay trên đống củi ở 185).

Em **không dùng** `Lửa.prefab` (Light2D 12.4 × 3 — quá sáng) và không dùng `VFX_Fire.prefab`
(VFX Graph, đã từng gây `MissingComponentException`, lại kèm 3 Light2D).

---

## 5. 🟠 SCENE CÂU CÁ QUÁ SÁNG — ĐÃ SỬA

### Gốc rễ: cộng dồn Light2D, **không phải post-processing**
`SCN_Fishing` **không có Light2D nào của riêng nó** — toàn bộ sáng đến từ 1 instance
`DayNightWeatherSetup.prefab`, và scene **không override giá trị đèn nào**.

URP 2D cộng dồn mọi Light2D cùng blend style rồi **NHÂN** vào màu sprite (blend style 0 = "Multiply"):

| Thời điểm | Ambient | DayLight | Đèn nhân vật | **Tổng hệ số nhân** |
|---|---|---|---|---|
| Sáng sớm | 1.15 | 0.84 | 0.8 | **≈ 2.79** |
| Trưa | 1.45 | 0.25 | 0.8 | **≈ 2.50** |
| Chiều | 1.25 | 0.75 | 0.8 | **≈ 2.80** |
| Đêm | 0.95 × xanh đậm | 0 | 1.28 | tối, có màu — **đẹp** |

Mọi pixel sáng từ 0.36 trở lên là **bão hoà trắng**. `DayDuration = 300s` nên 1 ngày chỉ 5 phút,
đỉnh sáng quét qua **2 lần mỗi 5 phút** → đúng hiện tượng "lâu lâu trắng xoá". Ban đêm ambient
nhân với màu xanh đậm nên đẹp — **khớp 100% mô tả của Sếp**.

Đã loại trừ Bloom/Exposure: Main Camera có `m_RenderPostProcessing: 0`, HDR tắt.

### Đã sửa — chỉ động vào scene câu cá, **không đụng farm**
Prefab `DayNightWeatherSetup` **dùng chung với `SCN_Farm`**, hạ curve là farm tối theo mà Sếp
không chê farm. Nên em ha riêng bằng code:

- **`FishingLightDamper.cs` (mới)** — chạy ở `LateUpdate` (sau khi chu kỳ ngày/đêm ghi intensity),
  nhân lại: đèn Global ×0.62, đèn điểm ×0.40, và **chặn trần tổng ở 1.15** để dù curve có đổi cũng
  không bao giờ cháy sáng nữa. **Bỏ qua `PlayerLantern`** — vòng tròn sáng theo nhân vật Sếp khen
  đẹp nên không đụng vào.
- `playerLanternIntensity` **0.8 → 0.35** (asset chưa serialize key này nên đổi default trong code là ăn ngay).
- Ban ngày đèn nhân vật mờ hẳn (`Lerp(1f, …)` → `Lerp(0.45f, …)`), ban đêm vẫn rực ×1.6.

3 hằng số trong `FishingLightDamper` chỉnh được ngay trong Inspector nếu Sếp thấy còn sáng/tối.

---

## 6. ✅ 4 NGUYÊN LIỆU = GỖ / ĐINH / KÍNH / ĐÁ — ĐÃ CHỐT VÀ DỌN SẠCH

Sếp đính chính: **không có gạch**, đúng 4 món **gỗ, đinh, kính, đá**. Kiểm lại thì cả 4 đã có
asset đầy đủ, **không phải tạo gì cả**. Và bảng chi phí nâng cấp kho vốn đã dùng đúng 4 món này.

### Đối chiếu: món nào thật sự được tiêu
| Món | itemId | Số asset dùng làm chi phí |
|---|---|---|
| Gỗ | `go` | **34** |
| Đá | `da` | **34** |
| Đinh | `dinh` | **25** |
| Kính | `kinh` | **21** |
| ~~Sơn~~ | `son` | **0 — không tiêu được ở đâu** |

### 3 chỗ lệch đã sửa
1. **Tàu chuyến 2 thưởng Sơn thay vì Đinh.** `TrainRewardData.asset` preset 1 có `son x8`.
   Sơn **không có chỗ tiêu nào** → người chơi gom về rồi để đó. Tệ hơn: slot Sơn đang xài
   **icon của Đá** (`fileID -8240987795322086458`), dấu hiệu rõ là hàng tạm.
   → Đổi thành `dinh x8` kèm đúng icon Đinh.
   Giờ cả 2 chuyến tàu chỉ thưởng đúng 4 món: `đá, gỗ, đinh, kính`.

2. **Preset dự phòng của tàu dùng 2 id ma.** `TrainManager.cs:834` fallback dùng `"gach"` và
   `"kim"` — **cả hai đều không có asset `InventoryItemData`**. Rơi vào nhánh này là kho nhận
   vật phẩm không tên, không icon, không tiêu được.
   → Đổi thành `go/da/dinh/kinh`.

3. **`BuildMaterialCost.cs`** còn `case "gach": return "Gach";` và comment hứa hẹn thêm gạch.
   → Bỏ, sửa comment cho khớp bộ 4 món đã chốt.

### ⚠️ Còn 1 chỗ Sếp cần quyết
**Sơn vẫn được phát khi lên cấp** — `LevelReward_L8 / L12 / L17 / L22 / L27`. Người chơi lên cấp
nhận Sơn nhưng **không tiêu được ở đâu**, nằm chiếm slot kho (kho tính sức chứa theo **số LOẠI**
vật phẩm, không phải tổng số lượng, nên một món vô dụng vẫn ăn mất 1 slot).

Hai hướng, Sếp chọn:
- **(a)** Bỏ Sơn khỏi 5 phần thưởng lên cấp, thay bằng 1 trong 4 món thật.
- **(b)** Giữ Sơn, thêm nó vào chi phí của một nhóm công trình nào đó (ví dụ nhà dân cấp cao).

Em không tự đổi vì đây là quyết định thiết kế kinh tế, không phải lỗi.

## FILE ĐÃ SỬA — 10 file code + 24 prefab + 1 prefab lửa
Đã kiểm cân bằng ngoặc `{}` trên cả 10 file, line ending giữ nguyên LF, không file nào lệch.

| Backup | Nội dung |
|---|---|
| `production/backup_scene_vong14/` | PlacementManager · LandExpansionManager · WarehousePopupUI · BuildMaterialCost · FishingConfig · PlayerLanternLight · FishingSceneBootstrap · SCN_Farm.unity |
| `production/backup_decor_vong14b/` | 24 prefab decor bản gốc |
| `production/backup_art_vong14/` | các sheet art |

*Không commit, không push, không sửa GitHub. Không đụng file scene.*

## SẾP KIỂM NHANH
1. **Chuồng**: mua chuồng gà thứ 2 → phải thấy **2 chuồng** trên map.
2. **Nguyên liệu**: mở Kho → bấm Nâng cấp → phải hiện **khung có ô nguyên liệu, icon, `đang có/cần`**.
3. **Decor**: vào Edit Mode, bấm giữ đóm củi lửa / đèn lồng / hoa sen → phải nhấc lên được.
4. **Lửa**: đặt Đóm Củi Lửa → phải thấy ngọn lửa nhấp nháy.
5. **Câu cá**: vào scene câu cá ban ngày → không còn trắng xoá, nước giữ màu, vòng tròn sáng vẫn còn.
6. **Camera** (vòng 14b): bảng debug phải hiện `4400 x 6750` thay vì `3300 x 2250`, kéo được xuống bến tàu.
