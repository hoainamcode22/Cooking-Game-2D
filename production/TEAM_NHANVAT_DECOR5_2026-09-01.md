# 🏗️ BÀN GIAO — GÓI "NHÂN VẬT & ĐỒ TRANG TRÍ 5 STAGE" (2026-09-01)

> Lead viết cho Sếp. Đọc **PHẦN B** là bấm được ngay.
> Backup toàn bộ file cũ bị chạm: `production/backup_characters_decor_2026-09-01/` — hỏng thì chép ngược lại.
> Vòng lặp đã chạy: **SCAN → PLAN → IMPLEMENT (4 DEV song song) → CHECK (QA + sandbox) → sửa lại → REPORT**.

---

## PHẦN A — ĐÃ LÀM GÌ

### A1. Đội đã dùng
| Vai | Việc | Kết quả |
|---|---|---|
| `unity-specialist` | SCAN hệ xây dựng village | phát hiện có **2 hệ song song**, hệ 5-stage thật là `HouseGrowthController` (không phải `ConstructionManager`) |
| `gameplay-programmer` | SCAN shop → đặt đồ | tìm ra nguyên nhân "đặt xuống hơi dở" chỉ là **1 con số**: `buildTimeSeconds = 0` |
| `gameplay-programmer` | SCAN bảng đơn + NPC | phát hiện `VillageOrderManager`/`HouseOrderController` **đã bị xoá**, thay bằng hệ `OrderBoard` |
| DEV-A `gameplay-programmer` | hệ 5 stage decor | 5 file, 2.149 dòng |
| DEV-B `technical-artist` | nhân vật thợ búa | 5 file, 1.846 dòng |
| DEV-C `gameplay-programmer` | cô gái giỏ hoa | 6 file, 2.474 dòng |
| DEV-D `tools-programmer` | 4 Editor Tool | 4 file, 2.219 dòng |
| `qa-lead` | rà đối kháng 20 file | bắt **4 lỗi CHẶN + 9 lỗi nên sửa** → trả DEV sửa hết |
| Lead | xoá phông + tái căn baseline 75 ảnh | viền trắng **0.06%** (art cũ trong project tới 78%) |

**Tổng: 20 file mới (8.688 dòng) · 75 ảnh PNG · 2 file cũ được thêm guard (5 dòng).**

### A2. Art — Lead tự cắt, đã QC bằng số
15 sheet `Assets/Assetsgame/Buiding trang trí/*.png` **có nền trắng đặc (alpha 0.00%)** → vi phạm `ART_RULES_STUDIO` §2. Đã xử lý:

| Bước | Kết quả đo |
|---|---|
| Dò viền + đường kẻ chia ô từng file (ô KHÔNG đều nhau: 506/502/509px) | 15/15 file cắt đúng |
| Xoá phông bằng **flood-fill từ biên** (không phải ngưỡng trắng toàn cục) → giữ được phần trắng thật của vật | mèo trắng · đài nước trắng · bình tưới xanh nhạt **đều nguyên vẹn** |
| Vá 97 vùng nền **kín** (khe nan bánh xe, lòng khung bàn, lỗ giếng, trong vòng hoa, lòng quai bình tưới) | 84.058 px |
| Ăn rìa trắng 1px (an toàn vì mọi vật đều có outline nâu đậm theo STYLE_CONTRACT) | 18.817 px |
| **Viền trắng sót** | **tb 0.060% · max 0.41% · 0/75 file vượt 1%** |
| **Canvas đồng nhất 5 stage** (chống giật hình khi đổi stage) | **15/15** |
| **Lệch baseline** | **max 1px** |

Đầu ra: `Assets/Art/Decor/Stages/<slug>/stage_1..5.png` — **75 file**.

### A3. Map 15 bộ ảnh → item (Sếp đã duyệt phương án "10 khớp + 4 mới")
| slug | itemID | Tên trong shop | |
|---|---|---|---|
| `gieng` | 1 | Giếng | ✅ |
| `bunhin` | 2 | Bù nhìn | ✅ |
| `chanhoa` | 4 | Chân Hoa | ✅ |
| `coixaygio` | 5 | cối oay gió | ✅ |
| `cotden` | 6 | Cột đèn | ✅ |
| `meovuive` | 9 | Mèo vui vẻ | ⚠ **art vẽ HEO ở stage 3** — xem D2 |
| `rom` | 10 | Rơm | ✅ |
| `vonghoa` | 11 | Vòng Hoa | ✅ |
| `xehoa` | 13 | Xe Hoa | ✅ |
| `dainuoc` | 14 | Đài Nước | ✅ |
| `hoda` | 15 | Hồ đá | ✅ |
| `chaucaythu` | **16** | Chậu Cây Thú | 🆕 gem 150, ô 2×2 |
| `chulun` | **17** | Chú Lùn Sân Vườn | 🆕 gem 200, ô 2×2 |
| `giabanrau` | **18** | Giá Bán Rau | 🆕 gem 250, ô 3×3 |
| `binhtuoihoa` | **19** | Bình Tưới Hoa | 🆕 gem 150, ô 2×2 |

**Chưa có art 5 stage** (giữ hành vi cũ, đặt xuống là hiện ngay): Bảng hiệu (3), Ghế Hoa (7), Heo thần tài (8), Vịt vui vẻ (12).

### A4. Ba task — luồng đã code
**Task 3 — decor 5 stage** (y hệt village):
```
mua ở shop → đặt xuống
  ↓ 0-50%   hiện stage 1 (vật liệu rời)   ← thợ búa đập
  ↓ 50-100% hiện stage 2 (đang xây nửa)   ← thợ búa đập
     click bất kỳ lúc nào → popup: TÊN + THANH TIẾN ĐỘ + NÚT KIM CƯƠNG (giá = max(2, ceil(giây/20)) — giống hệ nhà)
  ↓ hết giờ hiện stage 4 (HỘP QUÀ, thở nhẹ)  ← thợ ĐỔI sang sheet ăn mừng, ĐỨNG IM ở frame 0
  ↓ user click hộp
     stage 5 (hộp bung) 0.35s + pop scale
     pháo hoa = ConstructionCelebrationFX.Play() — DÙNG CHUNG với village, không chép lại
     thợ CHẠY animation ăn mừng 12fps suốt 3.5s
  ↓ hết pháo hoa
     world hiện stage 3 (HOÀN THIỆN) vĩnh viễn · thợ fade rồi biến mất
```
Thời gian xây decor = `kim cương × 0.6`, kẹp [20s, 240s] → gem 20→20s · 50→30s · 150→90s · 200→120s · 300→180s · 400→240s. **Không sửa một file `.asset` nào, giá giữ nguyên 100%.**

**Task 2 — thợ búa:** 3 prefab, ẩn mặc định. Số thợ tự tính theo kích thước: decor 2×2 → 1 thợ · 4×4 → 2 · chuồng 7×5 → 3. Đập búa lệch nhịp nhau, frame 8/9/10 (búa chạm đất) bắn bụi + tiếng (chặn 1 SFX/0.25s cho cả cụm).

**Task 1 — cô gái giỏ hoa:** đứng cạnh bảng đơn → user giao 1 đơn → đi bộ theo **đường line bao quanh khu nhà village** tới 1/5 nhà random → **mũi tên bồng bềnh** trên nhà đích → đứng trước nhà 1.6s → đi về (lệch làn 40 unit để đường đi/về không trùng). Đo thật: **4,6–11,3 giây/chiều**.

### A5. Quyết định của Lead trong lúc làm
1. **Chuồng + máy chế biến dùng "WorkerOnly"** — chúng KHÔNG có art 5 stage, nên giữ nguyên hình từ đầu tới cuối, chỉ có: timer + popup tiến độ + thợ búa + pháo hoa lúc xong. **Bỏ qua hộp quà.** (Sếp yêu cầu thợ hiện cả khi mua chuồng — đây là cách làm đúng mà không cần chờ art.)
2. **Thời gian xây chuồng dùng BẢNG số tròn** 100→45s · 600→90s · 950→120s · 2000→180s (nội suy tuyến tính giữa các mốc, sửa được trong Inspector — theo `coding-standards`: "gameplay values must be data-driven").
3. **Toàn gói mặc định TẮT.** 3 công tắc `enabled` đều `false`. Chưa tick thì game chạy **y như trước** — đây là điều kiện để 1 dòng thêm vào `PlacementManager` hợp `AUTONOMY.md` §2.

### A6. 8 bug của hệ nhà cũ — hệ mới KHÔNG lặp lại
| Bug hệ cũ (`HouseGrowthController`) | Hệ mới làm gì |
|---|---|
| Save key băm theo **toạ độ** → di chuyển công trình là mất tiến độ | key theo `itemID + slotIndex`. **Đã test: dời vật tới (9999,−12345) vẫn giữ nguyên tiến độ** |
| Truyền `currentItem.name` làm id nhưng đọc `houseId` → tra sai key → **nhảy thẳng Completed** | 1 nguồn id duy nhất |
| Không có key ⇒ mặc định `Completed` | không tự suy diễn, tự tháo component |
| `UpdateVisuals()` gán sprite + tính lại collider **60 lần/giây** | chỉ vẽ khi stage đổi. **Đã đo: 4 lần thay vì 3.660 lần** trong 1 lần xây 60s |
| `TrySpeedUpWithGem` thiếu manager ⇒ **rush MIỄN PHÍ** | eco null ⇒ return false, KHÔNG finish |
| Click không kiểm Edit Mode / popup / input lock | qua **cả 3 chốt** |
| Đọc `UtcNow` trực tiếp ⇒ **vặn giờ máy là xong game** | `NowUnix()` chống lùi giờ. **Đã test lùi 1 giờ: remaining không nhảy** |
| Coroutine không phòng Destroy giữa chừng | `OnDestroy` dừng coroutine + ghi state cuối |

### A7. QA bắt được gì (đã sửa hết)
| | Lỗi | Kịch bản | |
|---|---|---|---|
| 🔴 R1 | `RestoreAll` chạy ở `AfterSceneLoad` = **TRƯỚC** `PlacementManager.Start()` → decor chưa tồn tại | mua Giếng 20s, thoát ở giây 5, mở lại → **tiến độ đóng băng vĩnh viễn** | ✅ đổi sang coroutine chờ 2 frame + `sceneLoaded` |
| 🔴 R2 | y như trên với nhà village | mua Home1, thoát ở giây 20, mở lại → **nhà xây mà không có thợ** | ✅ runtime host + quét lại mỗi 2s (nhà mua mới cũng có thợ) |
| 🔴 R3 | thứ tự 2 `RuntimeInitializeOnLoadMethod` **không xác định** → lỗi nhấp nháy, lần được lần không | decor khôi phục đôi khi có thợ đôi khi không | ✅ replay event cho subscriber muộn |
| 🔴 R4 | `Image.Type.Filled` + sprite null ⇒ Unity **bỏ qua `fillAmount`** | **thanh tiến độ luôn đầy 100%** ngay từ giây đầu | ✅ đổi sang `anchorMax`. Đo thật: 0/25/50/100% |
| 🟠 A1 | click popup **xuyên** xuống nhà phía sau | bấm nút kim cương của decor → **nhà cũng nhận click** | ✅ lớp chặn raycast + 3 guard vào `HouseGrowthController` |
| 🟠 A2 | thợ đứng **lơ lửng trong lòng** công trình khi decor đổi stage | | ✅ xếp lại khi bounds đổi >10% |
| 🟠 A4 | mua-bán 200 lần ⇒ **600 key rác** trong PlayerPrefs | | ✅ test 50 vòng: PlayerPrefs **trống hoàn toàn** |
| 🟠 A6 | popup đặt `timeScale=0` ⇒ **pháo hoa nổ mà thợ đứng chết** | | ✅ ăn mừng dùng unscaled time |
| 🟠 A7 | 30 decor × 3 syscall/frame = 90 syscall/frame | | ✅ cache 1 lần/frame: **10 thay vì 1.200** |

### A8. Kiểm thử (không có Unity ở đây nên tôi dùng 2 lớp)
- **Sandbox Python** mô phỏng từng state machine: **182 case PASS / 0 FAIL** (DEV-A 62 · DEV-B 58 · DEV-C 39 · vòng đầu 23). Sandbox tự bắt được **6 bug thật** mà đọc code không thấy: mitre góc nhọn làm vòng đường phình 1.530 unit · bevel cắt sát nhà 44 unit · điểm lệch làn tụt còn 27 unit · `OnFrameEntered` bắn 13 lần/vòng · nhịp poll trôi thành 0,217s · đường về đi chéo xuyên khu nhà khi nhà bị xoá giữa đường.
- **Parser C# thật (tree-sitter)** trên 22 file: **0 lỗi cú pháp · 0 type trùng tên · 1 top-level type/file · 0 identifier non-ASCII · 0 `using` thiếu**. Đã đối chiếu **từng symbol ngoài** với định nghĩa thật trên đĩa (kể cả `com.unity.2d.sprite` có trong `manifest.json`, `TextWrappingModes` trong ugui 2.0.0).

> ⚠ **Thành thật:** tôi **không** compile được bằng Unity (môi trường không có). 2 lớp trên bắt được cú pháp + thuật toán + tồn tại symbol, **không** bắt được 100% lỗi kiểu. Sếp mở Unity xem Console sạch rồi mới bấm menu.

---

## PHẦN B — SẾP BẤM GÌ TRONG UNITY (theo đúng thứ tự)

### B0. Mở Unity, đợi compile. **Console phải 0 lỗi đỏ.**
Nếu có lỗi đỏ → **dừng lại, gửi tôi ảnh Console**, đừng bấm tiếp.
Muốn quay về nguyên trạng: chép 7 file trong `production/backup_characters_decor_2026-09-01/` ngược lại, và xoá 3 thư mục `Scripts/DecorGrowth`, `Scripts/Characters`, `Scripts/Shipper` + 4 file Editor.

### B1. Cắt spritesheet nhân vật
1. `Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (DRY-RUN)` → đọc report
2. `Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY)`
3. `Tools/Farm Game/Characters/Kiểm tra sprite con đã slice` → phải ra **3 × 12/12**

### B2. Nạp art 5 stage cho decor
4. `Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage (DRY-RUN)` → phải thấy **15/15 slug, 5/5 file, canvas không lệch**
5. `Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage (APPLY)`

### B3. Tạo 4 item decor mới
6. `Tools/Farm Game/Decor 5 Stage/Tạo 4 DecorData item mới (DRY-RUN)`
7. `Tools/Farm Game/Decor 5 Stage/Tạo 4 DecorData item mới (APPLY)`

### B4. Thợ búa
8. `Tools/Farm Game/Worker/★ SETUP thợ búa (1 nút)` → tự tạo config + 3 prefab `Worker_Builder_01/02/03`

### B5. Cô gái giỏ hoa
9. `Tools/Farm Game/Shipper/★ SETUP cô gái giỏ hoa (1 nút)`
10. `Tools/Farm Game/Shipper/Tạo Shipper_HomeAnchor trong scene (cần Sếp bấm riêng)` → **Ctrl+S NGAY** (đây là bước duy nhất sửa scene, tool không tự save)
11. `Tools/Farm Game/Shipper/Kiểm tra sẵn sàng` → mọi dòng phải ✅

### B6. BA VIỆC TAY (tool cố ý không làm — sửa scene/kinh tế thuộc DANH SÁCH DỪNG)
12. Kéo **4 asset mới** vào `ShopManager.decorList` trong scene `SCN_Farm`:
    `Assets/_Game/Farm/CÔNG TRÌNH/Chau Cay Thu.asset` · `Chu Lun.asset` · `Gia Ban Rau.asset` · `Binh Tuoi Hoa.asset`
13. Kéo 3 prefab thợ vào `BuilderWorkerConfig.workerPrefabs[0..2]` (nếu tool chưa gán được)
14. **TICK `enabled = true` trên 3 asset** (đây là công tắc duy nhất bật cả gói):
    - `Assets/_Game/Resources/DecorGrowthConfig.asset`
    - `Assets/_Game/Resources/BuilderWorkerConfig.asset`
    - `Assets/_Game/Resources/ShipperConfig.asset`
15. **Ctrl+S**

### B7. PLAY TEST — 6 kịch bản
| # | Làm gì | Phải thấy |
|---|---|---|
| 1 | Mua **Giếng** (gem 20) rồi đặt xuống | stage 1 (vòng đá + xẻng), **1 thợ búa** đập búa |
| 2 | Click vào giếng đang xây | popup: **"GIẾNG"** + thanh tiến độ **nhích thật** + nút kim cương |
| 3 | Bấm nút kim cương | trừ gem → hiện **hộp quà** thở nhẹ, **thợ đứng im** |
| 4 | Click hộp quà | hộp bung → **pháo hoa 3,5s** → thợ **nhảy ăn mừng suốt pháo hoa** → hiện giếng hoàn thiện → thợ fade mất |
| 5 | **Thoát Play, vào lại** giữa lúc đang xây 1 món | tiến độ **đúng chỗ cũ**, thợ vẫn đó (đây là bug R1 đã sửa — test kỹ mục này) |
| 6 | Mua 1 **nhà village**, giao 1 đơn ở bảng đơn hàng | thấy **đường line bao quanh khu nhà** + **mũi tên** trên nhà đích + **cô gái đi bộ** tới rồi về |
| 7 | Mua **Chuồng Gà** | chuồng giữ nguyên hình, **3 thợ đập búa 45s**, xong thì pháo hoa (KHÔNG có hộp quà — đúng thiết kế) |

---

## PHẦN C — CẦN BẠN (việc tôi không tự làm được)

| # | Việc | Vì sao |
|---|---|---|
| C1 | 15 bước ở PHẦN B | Unity không có API cho agent bấm menu / kéo asset / vào Play Mode |
| C2 | Kéo 4 asset mới vào `ShopManager.decorList` | sửa `.unity` — `AUTONOMY.md` §3.1 |
| C3 | Tick 3 công tắc `enabled` | feature flag, phải do Sếp duyệt |
| C4 | Xác nhận **pivot** — nếu decor bị **dịch lên** sau khi đổi sprite thì báo tôi | art mới pivot Bottom-Center, prefab cũ có thể đang Center |
| C5 | Duyệt giá 4 item mới (gem 150/200/250/150) | con số kinh tế — `AUTONOMY.md` §3.5 |
| C6 | Quyết `Mèo vui vẻ` (xem D2) | quyết định thiết kế |

## PHẦN D — CÒN NỢ / RỦI RO TÔI TỰ THẤY

| | Việc | Mức |
|---|---|---|
| D1 | **Popup tiến độ decor hiện bằng màu phẳng** (burgundy + đồng vàng) vì `Resources` chưa có sprite khung/gem. Không crash, nhưng nhìn khác popup cây/chuồng. Copy art vào `Resources/UI/DecorProgress_BG` + `Resources/UI/kimcuong` là nó tự nhận, không cần sửa code | 🟠 |
| D2 | **`Mèo vui vẻ` (id 9): art stage 2 vẽ MÈO nhưng stage 3 vẽ HEO** — bộ ảnh tự mâu thuẫn. Đã viết đơn đặt lại: `production/PROMPT_SPRITE_FORGE_DECOR_FIX_2026-09-01.md` | 🟠 |
| D3 | 3 prefab thợ hiện chỉ khác nhau **lật ngang + co nhỏ 6%** (cùng 1 spritesheet). Muốn 3 người thật khác nhau thì cần đội vẽ — đã ghi trong đơn D2 | ⚪ |
| D4 | Đường line quanh khu nhà **chưa tránh vật cản** (chỉ quét nhà). Đường vẽ ở layer `Bottom` order −50 nên không che gì, nhưng **cô gái có thể đi xuyên chuồng**. Sếp xem trên máy rồi nói, tôi thêm bước tránh vật cản sau | ⚪ |
| D5 | `RestoreAll` khớp vật bằng **tên + toạ độ gần nhất**. Nếu có ≥2 vật CÙNG LOẠI đặt sát nhau và người chơi dời rồi tắt game ngay, có thể gán chéo (tiến độ vẫn đúng vì cùng itemID, chỉ khác vật nào hiện hộp quà). Sửa sạch cần thêm 1 dòng ở đường `LoadBuildings()` của `PlacementManager` — **chờ Sếp cho phép** | ⚪ |
| D6 | 4 decor **chưa có art 5 stage**: Bảng hiệu, Ghế Hoa, Heo thần tài, Vịt vui vẻ → giữ hành vi cũ. Đã ghi trong đơn D2 | ⚪ |
| D7 | **Chưa compile bằng Unity thật.** 2 lớp kiểm bắt được cú pháp + thuật toán + tồn tại symbol, không bắt được 100% lỗi kiểu | 🟠 |

## PHẦN E — FILE ĐÃ TẠO / CHẠM

**Mới — code (16 file):**
```
Assets/_Game/Farm/Scripts/DecorGrowth/  DecorStageSet · DecorGrowthConfig · DecorGrowthController
                                        DecorGrowthBootstrap · DecorProgressPopupBridge
Assets/_Game/Farm/Scripts/Characters/   SpriteSequencePlayer · BuilderWorkerConfig · BuilderWorker
                                        BuilderWorkerCrew · HouseWorkerBridge
Assets/_Game/Farm/Scripts/Shipper/      FourDirWalkAnimator · ShipperConfig · FlowerGirlShipper
                                        VillageRoadRing · DeliveryArrowFX · ShipperManager
```
**Mới — Editor tool (4 file):** `CharacterSheetSliceTool` · `DecorStageArtTool` · `BuilderWorkerSetupTool` · `ShipperSetupTool`

**Mới — art:** `Assets/Art/Decor/Stages/<15 slug>/stage_1..5.png` (75 file)

**File CŨ bị chạm — chỉ 2 file, 5 dòng, đều là CỘNG THÊM:**
| File | Dòng | Nội dung | Hoàn tác |
|---|---|---|---|
| `Scripts/Managers/PlacementManager.cs` | 1280-1281 | 1 comment + `DecorGrowthBootstrap.OnDecorPlaced(spawnedObj, currentItem);` | xoá 2 dòng đó |
| `Scripts/Gameplay/HouseGrowthController.cs` | 4, 187-194 | `using UnityEngine.EventSystems;` + 3 dòng guard đầu `CheckInputClick()` | xoá khối `[DECOR-5STAGE-GUARD]` + dòng using |

**KHÔNG chạm:** không `.unity` · không `.prefab` · không `.asset` · **không git (0 commit, 0 push)**.

---

## PHỤ LỤC 1 — SỬA LỖI COMPILE VÒNG 1 (2026-09-01, sau khi Sếp mở Unity)

Unity báo **7 lỗi `CS1503: cannot convert from 'string' to 'int'`**.

**Nguyên nhân:** `BaseItemData.itemID` là **`string`**, không phải `int` — `CONTRACT.md` §3 mà Lead viết cho đội **ghi sai kiểu**. QA đã phát hiện đúng điều này nhưng kết luận sai là "không có lỗi compile", vì lúc đó DEV-A còn giữ overload `FindSet(string)` bắc cầu. Bản v2 (viết lại sau khi QA trả về 4 lỗi chặn) thêm 7 call site mới truyền thẳng `data.itemID` vào các API nhận `int` (`FindSet`, `IsExcludedItem`, `AllocateSlotIndex`, `Initialize`, `AddActive`) và overload bắc cầu không còn phủ được ⇒ lỗi lộ ra. **Đây là lỗi của Lead, không phải của DEV.**

**Đã sửa — 2 file, 7 call site + 1 cầu chuyển kiểu duy nhất:**

| File | Thay đổi |
|---|---|
| `DecorGrowthConfig.cs` (322 → **349** dòng) | Thêm `public static int ParseItemId(string)` + `public static int ItemIdOf(PlaceableItemData)`. Vá 3 call site (dòng 202, 284, 293) |
| `DecorGrowthBootstrap.cs` (647 → **650** dòng) | Đổi kiểu **một lần** ở đầu `OnDecorPlaced`: `int itemIdInt = DecorGrowthConfig.ItemIdOf(data);` rồi dùng ở 4 call site (dòng 243, 249, 251) + `Debug.Log` in cả 2 dạng để dễ đối chiếu |

**Chi tiết `ParseItemId`:** chuỗi là số ⇒ parse thường. Chuỗi KHÔNG phải số ⇒ băm **FNV-1a tất định**, trả về dải `≥ 1.000.000`.
- Cố ý **không** dùng `string.GetHashCode()`: .NET Core randomize hash mỗi lần chạy ⇒ save key sẽ đổi sau mỗi lần mở game, mất toàn bộ tiến độ xây.
- Dải `≥ 1.000.000` để không thể đụng itemID thật (hiện 1..122).
- Hiện **cả 33 asset trong project đều có itemID là chuỗi số** nên nhánh băm chưa bao giờ chạy — nó chỉ là lưới an toàn nếu sau này Sếp đặt itemID dạng chữ.

**Đã kiểm lại sau khi sửa:**
- `grep`: **0** chỗ còn truyền `.itemID` (string) vào hàm nhận `int`.
- **Type-flow checker mới** (Lead tự viết, `/tmp/typecheck.py`): thu 98 chữ ký method + 112 member từ 20 file, đối chiếu mọi call site về **kiểu tham số + số tham số** → **0 điểm nghi vấn**. Đây là lớp kiểm mà vòng QA trước còn thiếu; từ nay chạy nó trước khi bàn giao.
- tree-sitter 2 file sửa: **0 lỗi cú pháp, 1 top-level type/file**.
- Không chạm file nào khác. Không `.asset/.prefab/.unity`. Không git.

---

## PHỤ LỤC 2 — SAU KHI SẾP CHẠY 11 BƯỚC TOOL (2026-09-01)

**11/11 bước tool chạy sạch, 0 lỗi:** 36 sprite nhân vật · config 15 stageSet · 4 prefab + 4 DecorData mới · 3 prefab thợ · prefab cô gái + `Shipper_HomeAnchor` tại `(-879, -760)`.

### 🔴 LEAD ĐÍNH CHÍNH — TÔI ĐÃ BÁO SAI VỀ "MÈO VUI VẺ"

Log của tool làm lộ ra: asset `Mèo vui vẻ.asset` (itemID 9) có **`itemName = "Heo Vui Vẻ"`**. `itemName` mới là thứ **người chơi thấy trong shop** — tên file asset chỉ là tên file.

⇒ **Art stage 3 vẽ HEO là ĐÚNG.** Cái sai là **stage 2 vẽ MÈO**. Đơn đặt art trước của tôi ghi ngược — đã sửa lại `PROMPT_SPRITE_FORGE_DECOR_FIX_2026-09-01.md` ĐƠN 1: chỉ vẽ lại `stage_2` thành HEO, giữ nguyên stage 3.

### Hai lỗi lệch tên đã vá

| itemID | `itemName` thật (shop hiện) | config tool đặt | |
|---|---|---|---|
| 9 | **Heo Vui Vẻ** | "Mèo Vui Vẻ" | lệch → popup sẽ hiện khác shop |
| 10 | **Rơm Hoa** | "Rơm" | lệch |

**Vá gốc, không vá ngọn** — `DecorGrowthBootstrap.cs` (650 → 655 dòng): đảo thứ tự ưu tiên tên.
Trước: `stageSet.displayName` → `data.itemName` → `spawned.name`.
Sau: **`data.itemName`** → `stageSet.displayName` → `spawned.name`.
Lý do: `itemName` là nguồn sự thật (người chơi thấy nó trong shop); `displayName` chỉ là nhãn tiện tay đặt trong tool nên dễ trôi. Từ nay tên popup **không thể** lệch shop, kể cả với item Sếp thêm sau này.

Kèm sửa bảng map của `DecorStageArtTool.cs` cho 2 tên trên (chỉ để report của tool đọc đúng — **không cần chạy lại tool**, vì bản vá runtime ở trên đã xử lý).

tree-sitter 2 file: **0 lỗi cú pháp**.

### Trạng thái `Kiểm tra sẵn sàng` của Shipper — 6 ✅ / 2 ❌

| | |
|---|---|
| ❌ `nhà đã Completed: 0/11` | 8 trong số đó là **placeholder `House_02..12` đang TẮT trong scene** — code đã lọc `activeInHierarchy` nên chúng bị loại đúng, không phải lỗi. 3 nhà còn lại là nhà Sếp đã đặt nhưng **chưa xây xong**. Để test ngay: **bỏ tick `onlyDeliverToCompletedHouses`** trong `ShipperConfig`. Khi ship thật thì tick lại (không nên giao hàng tới nhà đang là công trường). |
| ❌ `ShipperConfig.enabled = FALSE` | đúng thiết kế — công tắc cuối |

---

## PHỤ LỤC 3 — NÚT CHỐT HẠ (theo lệnh Sếp: "fix sạch sẽ luôn, build 1 thể")

Sếp ra lệnh trực tiếp ⇒ việc sửa scene (thêm 4 item vào `decorList`) được phê duyệt, không còn nằm chờ ở CẦN BẠN. Lead viết thêm 1 tool cuối:

**`Assets/_Game/Farm/Editor/FinalizeDecor5Tool.cs`** (193 dòng, tree-sitter 0 lỗi) — 2 menu:

| Menu | Làm gì |
|---|---|
| `Tools/Farm Game/★ BẬT TOÀN BỘ GÓI Nhân vật + Decor 5 stage (1 nút cuối)` | ① thêm 4 DecorData (id 16-19) vào `ShopManager.decorList` qua **SerializedObject + Undo** (idempotent — bấm 2 lần không nhân đôi) · ② tick `enabled = true` cả 3 config · ③ tắt `onlyDeliverToCompletedHouses` (chỉ để test, in nhắc bật lại trước khi ship) · ④ **SAVE scene** (gồm `Shipper_HomeAnchor` còn dirty) + save asset. Có hộp xác nhận, chặn khi đang Play Mode |
| `Tools/Farm Game/TẮT KHẨN CẤP toàn bộ gói (enabled = false cả 3)` | 1 cú bấm đưa game về đúng hành vi trước khi có gói này (code mới ngủ đông toàn bộ) |

Ghi chú kỹ thuật: sửa scene qua SerializedObject bên trong Unity Editor (đúng pattern các tool sẵn có — `ShopManager` đã được 3 editor tool khác trong project tham chiếu), KHÔNG đụng YAML bằng tay. PHẦN B bước 12-14 + Ctrl+S nay gộp thành 1 cú bấm menu này.

---

## PHỤ LỤC 4 — VÒNG 2 (2026-09-02): 5 task Sếp báo sau Play test

### Chân tướng "không thấy gì trong game" — 3 tầng chồng nhau
1. **Project KHÔNG compile được sáng 02/09**: lỗi `CS0111` trùng method `DongAnimRoutine` trong `BoatAnnouncePopupUI.cs` (Editor.log ghi lặp) → mọi test hôm nay chạy trên assembly CŨ. Đã sửa (xoá bản trùng cũ, giữ bản toast-trượt).
2. **Cả 3 công tắc `enabled` vẫn = 0 và decorList chưa có 4 item mới** — Sếp test TRƯỚC khi nút ★ BẬT TOÀN BỘ GÓI tồn tại/compile. Hệ decor 5 stage chưa từng được bật.
3. **3 dòng guard Lead thêm vào `HouseGrowthController` (A1 vòng 1) đã bị REVERT** — file về đúng từng byte bản gốc (diff với backup: khớp 100%), rồi DEV-E sửa tiếp trên nền sạch.

### Kết quả 4 DEV (chi tiết từng báo cáo trong session log)
| Task Sếp | Nguyên nhân tìm được (file:dòng trong báo cáo DEV) | Fix |
|---|---|---|
| 2. Hộp quà nhà không mở + không pháo hoa | (a) click nhà là chỗ DUY NHẤT còn poll `Input.GetMouseButton*` + ngưỡng nhả 18px ≈ 1mm trên phone (chuẩn 24px+); (b) pháo hoa ép `sortingOrder=32767` nhưng KHÔNG đổi layer → kẹt `Default`, bị nhà (`Objects`) đè | `HouseGrowthController` dùng `TouchInput` (helper sẵn có) + slop theo DPI; pháo hoa đổi sang `ConstructionCelebrationFX.Play()` dùng chung; `ConstructionCompleteFX` (chuồng/máy) ép từng `ParticleSystemRenderer` lên `Foreground/1000+` theo đúng pattern LevelUpPopupUI |
| 3. Popup lên cấp | Card quà thiếu icon vì tool đổ data V3 ghi `icon=null` chờ gán sau; KHÔNG có bước tra id→icon runtime. Chữ "Kính" KHÔNG phải bug — là tấm kính vật liệu xây dựng (id `kinh`, có icon thật trong `item_taulua/Kính.asset`) | MỚI `LevelUpRewardIconResolver` (asset → RewardIconLibrary → StallItemCatalog → placeholder+log); gộp card quà vào chung khung trắng `Dai_MoKhoa` (flow-layout, tự xuống hàng, co đều); sandbox 49 id → **0 id thiếu icon**. Nhân vật lệch style → ĐƠN 4 trong file prompt art |
| 4. Trùng 2 hệ EXP | Hệ CŨ `ExpFlyToAvatarFX` (orb đè item 2s, giữ EXP 3.12s) vs hệ MỚI `RewardFlyFX` (bắn trễ 3.12s, nổ 3 lần) | Ruộng: tắt orb cũ (cờ `legacyExpOrbsEnabled=false` revert được), `AddExp` ngay t=0 → FX mới nổ 1 lần CÙNG LÚC item rơi; size sao EXP 72→108px (field Inspector) |
| 5. Cooking hỏng | **FONT VÔ TỘI.** `KitchenSceneV2UI:762-763`: `Destroy(HLG)` deferred + `AddComponent<GridLayoutGroup>` cùng frame → null → NRE giết cả `Start()` → board trống + mọi nút chết + không về farm được (bằng chứng: Editor-prev.log đêm 01/09) | `EnsureNeedChipsGrid()` dùng `DestroyImmediate` + null-check; `Start()` rào từng bước (1 tài nguyên hỏng không giết cả scene); `ApplyFont` fallback `TMP_Settings.defaultFontAsset`; tool ★ Sửa font gãy (DRY-RUN/APPLY) phòng hờ |

### Bug PHÁT SINH tìm thấy, CHƯA sửa (chờ Sếp duyệt — CẦN BẠN)
1. **EXP tàu lửa cộng ĐÔI**: `TrainManager.cs:372` `AddExp` trực tiếp RỒI `:786` orb cũ chạm đích cộng lần nữa. Sửa ~5 phút sau khi Sếp gật.
2. **Nhà đang xây/hộp quà MẤT trạng thái sau thoát game** (bug §7 cũ của hệ nhà: save key theo toạ độ + restore không `Initialize`): nếu Sếp thoát game lúc nhà chưa mở hộp → mở lại nhảy Completed. Sửa cần đụng đường `LoadBuildings` của PlacementManager.
3. Chuồng + tàu vẫn dùng orb EXP cũ (chỉ ruộng được nâng cấp theo lệnh) — nâng đồng bộ khi Sếp muốn.
4. 12 guid mồ côi legacy trong SampleScene (script/sprite đã xoá từ đợt dọn minigame) — vô hại, nên dọn 1 đợt riêng.

### QA vòng 2: tree-sitter 12/12 file = 0 lỗi cú pháp · 0 type trùng tên · mọi symbol liên-file (`TouchInput`, `StallItemCatalog.GetIcon`, `OrderBoardIconResolver.TintFromId`, `PlayerProgressManager.AddExp/OnExpAddedFx`, `RewardIconLibrary.Instance`) đã xác minh tồn tại đúng chữ ký · backup đủ trong `production/backup_round2_2026-09-02/`.

---

## PHỤ LỤC 5 — VÒNG 4 (2026-09-03): cắt sạch frame + afterimage cho xe cộ/NPC

### A. Cắt frame sát & sạch (việc Sếp giao cho Lead: "cắt ảnh, xoá phông")

**Chẩn đoán bằng số:** sheet AI vẽ **nhân vật TRÀN RA NGOÀI ô**, không phải "cắt lệch":
- `flowergirl` hàng 0 (down): tràn XUỐNG 25/17/23px ⇒ **giày rơi vào ô hàng 1**
- `flowergirl` hàng 3 (up): tràn LÊN 15/16/16px ⇒ **đỉnh đầu + vòng hoa lấn ô hàng 2**
- `hammer`: búa + mảnh vụn tràn ngang 8-10px sang ô kề
⇒ Hệ quả: frame nào cũng có mẩu người bên cạnh (Sếp thấy "dính layout frame khác"), và nếu chỉ xoá phần tràn thì nhân vật **bị cụt chân / cụt đầu**.

**Bài học Lead tự mắc:** thử V1 xoá theo component toàn cục → **ăn mất giày đỏ + vòng hoa** (chân người trên chạm đầu người dưới nên thành 1 khối, phần "thiểu số" bị xoá oan). QC bằng mắt bắt được, đã revert khớp **từng byte** bản gốc rồi làm lại.

**Cách làm đúng (V3, đã áp dụng):**
1. Component toàn cục → gán mỗi khối cho ô chứa **nhiều pixel nhất** ⇒ tách được "nhân vật của ô này (trọn vẹn, kể cả phần tràn)" khỏi "mẩu của người bên cạnh".
2. **Dịch nhân vật vào trong ô**: `flowergirl` mỗi hướng 1 offset (giữ nhịp bước trong hướng, thống nhất baseline giữa 4 hướng); `hammer`/`celebrate` 1 offset chung cả sheet (giữ nguyên nhịp búa + độ cao cú nhảy).
3. **Căn giữa ngang + baseline chung** ⇒ pivot Bottom-Center trỏ đúng bàn chân, nhân vật không nhấp nhô khi đổi hướng.
4. Mảnh rời (vụn búa) được **dịch riêng** cho vừa ô — hạt bay nên vô hại, nhờ đó không phải cắt cụt.

**Kết quả đo:** `flowergirl` **0 px** bị cắt mất · `celebrate` **0 px** · `hammer` **14 px** (0.001%, một mẩu vụn rộng hơn ô). **Kích thước sheet giữ nguyên** ⇒ rect slice + tên sprite không đổi ⇒ **mọi tham chiếu prefab/config nguyên vẹn, KHÔNG cần chạy lại tool slice.**

### B. Afterimage cho tàu lửa / tàu thủy / phà / NPC cảnh

**Bug gốc (bằng chứng `TrainPathFollower.cs:163`):** `Vector3.MoveTowards(trainRoot.position, …)` — script đứng trên object CHA **bất động** và di chuyển một Transform KHÁC. Emitter bản cũ đo `transform.position` của chính nó ⇒ tốc độ **= 0 vĩnh viễn** ⇒ xe cộ không bao giờ nhả bóng dù `moveSpeed = 300`. Phà + tàu thủy cùng pattern.

**Fix:** `SpriteAfterimageEmitter` (129 → 193 dòng) đo tốc độ **theo TỪNG SpriteRenderer** (mỗi SR một mốc `lastPos` riêng), nhịp spawn vẫn chung. Phủ mọi kiểu mover: move ở root, ở con, hay Animator ghi transform. SR mới vào cache khởi tạo mốc = vị trí hiện tại ⇒ không nhả ghost oan frame đầu.

**NPC cảnh (bà lão / quân nhân / nhân viên tàu):** object `NPC_Villagers` trong scene **KHÔNG có con nào** (Lead đọc scene YAML xác minh) ⇒ menu quét theo tên không tới được họ. Thêm menu **`★ Gắn tag cho object ĐANG CHỌN (Selection)`**: Sếp click chọn từng người trong Hierarchy (Ctrl+click nhiều người) → bấm menu → tự gắn `AfterimageTag` + đặt `minSpeedOverride = 20 u/s` (NPC đi lững thững, ngưỡng chung 60 quá cao nên trước đó không thấy bóng).

`AfterimageTag` thêm field `minSpeedOverride`; Emitter tự đọc tag nên **Bootstrap không phải sửa dòng nào**.

**QC:** tree-sitter 3 file = 0 lỗi cú pháp · 0 type trùng · 3 overload `Setup` tương thích ngược (Bootstrap gọi bản 4 tham số vẫn chạy). Backup: `production/backup_round4_2026-09-03/` (3 PNG gốc + 3 .cs gốc).

---

## PHỤ LỤC 6 — VÒNG 5 (2026-09-03): 4 báo cáo của Sếp

### 1. "Con tàu của tôi mất rồi" → **DỮ LIỆU TÀU HOÀN TOÀN LÀNH — bến chưa mua**
Đã soi tận gốc, không đoán:
- 3 object `Boat` (con của `Dock_01/02/03`) đều `m_IsActive: 1`, `pos=(0,0)`, `scale=(1,1)` — không ai tắt, không ai co nhỏ.
- `directionalSprites`: **đủ 12/12 sprite** cho cả 3 tàu, không ô nào null.
- `boatWorldWidth = 680` (scene) và `TouristBoatConfig.boatVisualWidth = 680` (asset) — khớp nhau, không bị tool nào ghi đè thành số bé.
- Tàu **dựng lúc runtime** (3 object Boat không mang SpriteRenderer nào trong scene), scale tính bằng `desiredWidth / nativeWidth` tại `TouristBoatController.cs:352-360`.

⟹ **Trong ảnh Sếp gửi, giữa cầu tàu có bảng "Mở ở Lv12 – 2.000 vàng"** ⇒ bến đó **chưa được mua** (đủ level 29 nhưng chưa trả 2.000 vàng). Bến chưa mở thì không có chuyến ⇒ không có tàu. Khách du lịch trong ảnh đang đi trong làng (từ bến đã mở, hoặc chuyến trước đã cập rồi rời). **Không có gì phải sửa** — Sếp mua bến rồi xem lại; nếu mua xong vẫn không thấy tàu thì đó mới là bug và tôi sẽ đào tiếp hệ lịch chuyến (`HANDOFF_DevA_Schedule.md`).

### 2. "Frame nhân vật xây dựng vẫn dính frame khác" → **Lead làm mới một nửa việc**
Nguyên nhân: `CharacterSheetSliceTool` KHÔNG chia ô đều mà dùng **tight rect** — `ComputeTightRects()` (dòng 389) tự đo hộp giới hạn alpha từng ô. Rect trong `.meta` hiện tại được tính từ sheet **CŨ**, ví dụ `hammer_10: x=304 w=292` (gần trọn ô 300) vì lúc đo, mẩu người/vụn của ô bên cạnh vẫn còn nằm trong ô ⇒ rect phình ra bao cả mẩu lạ.

Vòng 4 Lead đã làm sạch **pixel** (đúng và cần) nhưng **chưa cập nhật rect** ⇒ rect cũ vẫn trỏ vùng rộng cũ ⇒ Sếp vẫn thấy dính.

**Cách sửa: chỉ cần bấm lại `Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY)`** — tool tự đo lại bbox trên sheet đã sạch, tên 36 sprite giữ nguyên nên mọi tham chiếu prefab/config nguyên vẹn. Không cần code thêm.

### 3. "Cho thợ xây sát vào công trình hơn"
`TinhViTri()` bản cũ đặt thợ ở `min.x - padding` / `max.x + padding` ⇒ **luôn NGOÀI rìa** bounds; hộp quà rộng ~480 unit thì 2 thợ cách nhau ~560 unit, nhìn như đứng canh chứ không phải đang xây.

Sửa sang đo **theo bán kính**: `x = tâm ± (extents.x × insetRatio + padding)`.
- `placementRadiusPadding`: **40 → 6**
- Thêm `placementInsetRatio = 0.62` (data-driven, Sếp kéo trong Inspector để tinh chỉnh)
- Kết quả: thợ vào gần hơn **~45%**, sát mép công trình mà không bị thân công trình vẽ đè (y-sort dùng chân thợ).
- Giữ overload `TinhViTri(b, padding)` cũ ⇒ preview của Editor tool không gãy.

### 4. "Kéo map khó" → ⚠ CHẨN ĐOÁN BAN ĐẦU CỦA LEAD SAI — xem PHỤ LỤC 7
Lead ban đầu quy cho vòng quét Afterimage (hiệu năng). Sếp phản hồi: **build EXE cũng vậy** và
**"cầm chuột kéo nó không nhúc nhích luôn"** ⇒ KHÔNG phải lag mà là **input bị chặn cứng**.
Phần tối ưu vòng quét dưới đây vẫn giữ (nó có ích thật), nhưng KHÔNG phải nguyên nhân.
Đo thật: `SCN_Farm` có **1.517 MonoBehaviour** / 1.878 GameObject *trong file scene* (chưa kể decor đã đặt, thợ, ghost sinh lúc chạy). `AfterimageBootstrap.Update()` gọi `FindObjectsByType<MonoBehaviour>` **mỗi 2 giây** ⇒ mỗi lượt cấp phát mảng ~2.000 phần tử + duyệt + type-check ⇒ **giật nhẹ định kỳ**, đúng cảm giác kéo map bị rít. Đây là hồi quy do gói Afterimage, không phải Sếp tắt object nào.

Sửa 2 tầng:
- `rescanInterval`: **2s → 10s** (code default + asset)
- Thêm **backoff**: quét mà không thấy mục tiêu mới thì giãn ×1.5 tới trần **30s**; thấy mục tiêu mới thì về ngay 10s. An toàn vì xe cộ/NPC có sẵn từ lúc load, còn thợ xây đi qua event `OnControllerSpawned` **không phụ thuộc vòng quét này**.
⟹ Tải vòng quét giảm **5–15 lần** ở trạng thái ổn định.

**QC:** tree-sitter 4 file = 0 lỗi cú pháp · `TinhViTri` 2 overload · asset xác minh `padding=6, insetRatio=0.62, rescan=10`. Backup: `production/backup_round4_2026-09-03/` (3 PNG + 6 .cs + 2 .asset).

---

## PHỤ LỤC 7 — KÉO MAP "KHÔNG NHÚC NHÍCH": chẩn đoán lại cho đúng

**Lead nhận sai ở vòng trước.** Tôi quy cho hiệu năng (vòng quét Afterimage mỗi 2s). Sếp phản hồi hai dữ kiện phá vỡ giả thuyết đó:
1. **Build EXE cũng vậy** ⇒ không phải Editor chậm.
2. **"Cầm chuột kéo nó không nhúc nhích luôn"** ⇒ không phải giật/rít mà là **không phản hồi**.

Lag và bị-chặn là hai bệnh khác nhau; tôi đã kết luận trước khi hỏi đủ. Xin lỗi Sếp vì làm mất một lượt.

**Cơ chế thật (có dòng code):** `CameraController.cs:241` và `:333` — cả hai đường kéo map đều `return` khi
`EventSystem.current.IsPointerOverGameObject()` == true. Nên **một** lớp UI trong suốt phủ kín màn hình mà còn bật `Raycast Target` là đủ làm map chết cứng, bất kể máy mạnh hay yếu, Editor hay EXE.

*(Điều này cũng giải thích hồi tố vì sao vòng trước tôi phải bỏ `IsPointerOverGameObject` khỏi router click decor — lúc đó tôi thấy "nó trả true cả trên world trống" nhưng chưa truy ra nguyên nhân. Cùng một thủ phạm.)*

**Quét scene tĩnh → 8 lớp phủ full-screen + `raycastTarget=1` + đang BẬT cả chuỗi cha:**
`Tutorial_Canvas/Dim_Background` · `Tutorial_Canvas/Tutorial_GuideBoard` · `Tutorial_Canvas/NPC_Dialog_Popup/NPC_Background` · `Canvas_Popup/MillPopup_Root/PopupRoot/Dim` · `Canvas_MarketPopup/Panel_Dim` (3840×2160) · `Popup_LevelUp_Township/Root_HienThi/Bg_NenToi` · `…/V2_TapCatcher` · `Canvas_Popup/Sickle_Bottom_Tray/BG_Image`

Trạng thái lưu trong scene KHÔNG kết luận được cái nào thật sự chặn lúc chạy (popup thường tự tắt ở `Awake`/`Start`). **Lead không đoán nữa** — dựng công cụ để nó tự khai.

### `Assets/_Game/Farm/Scripts/Debug/UiBlockerProbe.cs` (135 dòng, chỉ ĐỌC + Debug.Log)
| Khi nào | Nó in gì |
|---|---|
| Tự động: Sếp **giữ chuột kéo map** mà EventSystem báo "con trỏ trên UI" | ⛔ đường dẫn hierarchy **mọi UI dưới con trỏ**, dòng đầu = thủ phạm; mỗi UI kèm nhãn `nút bấm thật=CÓ/KHÔNG` (KHÔNG ⇒ lớp phủ mồ côi) |
| **F9** | in ngay UI dưới con trỏ (bấm bất cứ lúc nào) |
| **F10** | liệt kê MỌI lớp phủ đang bật + ăn raycast + phủ ≥80% màn hình, kèm hệ số ×màn hình |

Tự dừng sau 12 lần in để không rác Console. Không sửa/tắt gì — xoá file là xong, không hệ nào phụ thuộc.

**Sếp làm:** Play → kéo map → dán Console cho Lead. Có tên đích danh là sửa được ngay (bỏ tick object, hoặc bỏ `Raycast Target` trên Image đó, hoặc sửa popup tự tắt cho đúng).
