# BÁO CÁO VÒNG 9 - 06/09/2026 — SẾP DUYỆT, LEAD LÀM NỐT

> Sếp duyệt toàn bộ mục CẦN SẾP và 3 câu quyết định của vòng 8. Lead tự làm hết.
> Backup: `production/backup_vong9_2026-09-06/` (6 file, **có cả `SCN_Farm.unity` 16 MB**).

---

## 1. ⚠️ ĐÍNH CHÍNH: 2 TRONG 5 FILE "HỎNG FONT" LÀ BÁO ĐỘNG GIẢ

Vòng 8 Dev Q báo 5 file asset sai tên, Lead đã nói với Sếp là "gấp nhất". **Kiểm lại thì chỉ 3 file sai thật.**

Lead giải mã đúng byte trong file (`\xNN` và `\uNNNN` là **cách Unity escape ký tự ngoài ASCII**, không phải
lỗi font). Kết quả thật:

| File | Byte thật trong file | Kết luận |
|---|---|---|
| `Item_BotGao.asset` | `Bột gạo` | ✅ **ĐÚNG SẴN, không phải hỏng font** |
| `Item_NuocMiaEp.asset` | `Nước mía ép` | ✅ **ĐÚNG SẴN, không phải hỏng font** |
| `Item_bo_ham_ca_rot.asset` | `Bò hầm cà rót` | ❌ sai thật |
| `Item_sup_ngo_nam.asset` | `Súp ngo nấm` | ❌ sai thật |
| `Item_trung_op_la_bo_ne.asset` | `Trứng óp la bò né` | ❌ sai thật |

**Dev Q nhầm ở đâu:** nó không đọc byte gốc mà lấy chuỗi đã bị mojibake ở tầng khác, rồi **thêm luôn 2 khoá
mojibake vào bảng dịch** (`"Bá»t gáº¡o"`, `"NÆ°á»c mía ép"`). Hai khoá đó vĩnh viễn không khớp gì, chỉ là rác.

## 2. ĐÃ SỬA 3 TYPO THẬT (file `.asset`, Sếp duyệt)
| File | Cũ | Mới |
|---|---|---|
| `Item_bo_ham_ca_rot.asset` | `Bò hầm cà rót` | **`Bò hầm cà rốt`** |
| `Item_sup_ngo_nam.asset` | `Súp ngo nấm` | **`Súp ngô nấm`** |
| `Item_trung_op_la_bo_ne.asset` | `Trứng óp la bò né` | **`Trứng ốp la bò né`** |

Chỉ đổi đúng chuỗi `displayName`, giữ nguyên cách escape của Unity, header `%YAML` còn nguyên cả 5 file.
Đã kiểm: 3 chuỗi typo **không được tham chiếu ở bất kỳ file `.cs` nào khác**, nên sửa không làm gãy logic nào.

## 3. DỌN 5 KHOÁ RÁC TRONG BẢNG DỊCH
Cả **5 khoá Title Case ĐÚNG đã có sẵn** trong bảng từ trước (`"Bột Gạo"`, `"Nước Mía Ép"`, `"Bò Hầm Cà Rốt"`,
`"Súp Ngô Nấm"`, `"Trứng Ốp La Bò Né"`), và bản vá tra-không-phân-biệt-hoa-thường của vòng 8 đã tự khớp chúng.
Nên 5 dòng Dev Q thêm là thừa hoặc sai hoàn toàn. Đã xoá đúng 5 dòng: **820, 824, 881, 887, 899**.

> **Suýt xoá nhầm:** bộ lọc đầu tiên quét trúng cả dòng 1274 `{ "Xay 4 bột gạo", "Mill 4 Rice Flour" }`
> (tên một nhiệm vụ, phải giữ). Câu `assert` chặn lại, **file chưa hề bị ghi**, làm lại theo đúng 5 số dòng.

Bảng dịch: **1409 → 1404 khoá**, kiểm lại **0 khoá trùng lặp** (khoá trùng sẽ ném `ArgumentException` lúc chạy),
ngoặc cân bằng 1406/1406, LF thuần giữ nguyên.

## 4. QUYẾT ĐỊNH 1: GA TÀU DƯỚI CẤP 3 — GIỮ NGUYÊN "CHỈ KHOÁ"
Sếp duyệt đề xuất của Dev M. **Không phải làm gì thêm**, code vòng 8 đã đúng: nhà ga vẫn đứng đó, click ra
toast "Tàu hoả mở ở cấp 3 nhé!", không im lặng. Lý do giữ: `PermanentBuilding` sinh ra để object không bị ẩn;
nhà ga đứng sẵn là động lực leo cấp; và đúng cách dự án đang làm với cổng Bếp.

## 5. QUYẾT ĐỊNH 2: "NÔNG DÂN VUI VẺ" — GIỮ NGUYÊN TIẾNG VIỆT
Sếp duyệt. Không phải làm gì. Lý do: bộ dịch runtime **bỏ qua `TMP_InputField`**, nên nếu dịch thì HUD sẽ hiện
"Happy Farmer" còn ô "Tên nông trại" trong Cài đặt vẫn "Nông Dân Vui Vẻ" ⇒ lệch nhau, khó hiểu cho trẻ em.

## 6. QUYẾT ĐỊNH 3: VÁ `FarmResetTool.cs` (Dev S)

**Đường dẫn thật là `Assets/_Game/Farm/Scripts/Editor/FarmResetTool.cs`** (brief của Lead ghi thiếu chữ `Scripts`).

### Đối chiếu 2 đường reset — thiếu tới 11 bước, không phải 3
Đường A (đúng, đầy đủ) = `SettingsPopupUI.cs:322-395`. Đường B (menu Editor) thiếu:

| Bước | Đường A | Đường B cũ |
|---|---|---|
| `SaveSystem.DeleteSave()` | dòng 328 | **THIẾU** |
| `TutorialManager.ClearTutorialDoneFlag()` | 332 | **THIẾU** |
| `SaveVersionGuard.ClearAll()` | 333 | **THIẾU** |
| FarmEconomy / PlayerProgress / Warehouse / FarmInventory | reset **+ Destroy** | chỉ reset, **thiếu Destroy** |
| KitchenTransferManager | 361 Destroy | **THIẾU** |
| MissionProgressTracker | 366 Destroy | **THIẾU** |
| AnimalGuideController | 371 Destroy | **THIẾU** |
| TutorialManager | 376 Destroy | **THIẾU** |
| TownshipHUDController | 381 Destroy | **THIẾU** |
| Xoá lại 3 cờ hay mọc lại | 387-389 | **THIẾU** |
| Nạp lại scene | 393-394 | **THIẾU** |

### Đính chính rà soát vòng 8
Vòng 8 nói "bỏ sót 5 manager **DontDestroyOnLoad**". **Số 5 đúng, nhưng mô tả sai:**
chỉ **2/5** thật sự là `DontDestroyOnLoad` (`KitchenTransferManager` dòng 71, `MissionProgressTracker` dòng 89).
Ba cái còn lại là singleton theo scene. Và vòng 8 **bỏ sót lỗi thứ 4**: 4 manager mà đường B có đụng thì
**chỉ reset RAM chứ không `Destroy`**, nên `Instance` tĩnh vẫn sống và ghi lại trạng thái cũ.

**Triệu chứng dễ thấy nhất** là `TownshipHUDController`: reset xong HUD vẫn hiện số vàng và level cũ.

### Cách vá: uỷ quyền thật khi được, soi gương khi không được
- **Play Mode + tìm thấy `SettingsPopupUI`**: gọi thẳng `settings.OnResetProgressClicked()`.
  **Không chép lại một dòng nào** ⇒ đúng nghĩa một nguồn sự thật.
- **Play Mode + không tìm thấy**: chạy `SoiGuongDuongA()` mô phỏng đủ 9 manager, mỗi khối có chú thích
  `[Đường A dòng xxx-yyy]` để người sau đối chiếu.
- **Edit Mode**: chỉ xoá đĩa. Không `Destroy`, không `LoadScene`, vì ngoài Play Mode `Object.Destroy` sẽ ném lỗi.
  Đây chính là lý do không thể gọi mù đường A ở mọi nơi.

### Rào an toàn đã thêm
`DisplayDialogComplex` 3 lựa chọn. **① Thoát Play rồi xoá (khuyên dùng)**: đặt cờ EditorPrefs, tự thoát Play,
hook `playModeStateChanged` chờ `EnteredEditMode` rồi mới xoá — dùng lại thủ thuật đã được chứng minh ở
`ChoiLaiTuDauTool.cs:40-58`, key riêng nên 2 tool không kích nhầm nhau. **② Reset ngay trong Play**: uỷ quyền
đường A. **Huỷ**. Cuối cùng in 1 dòng log liệt kê chính xác đã xoá gì, đường dẫn `save.json` thật, số khoá
trước/sau, và tên từng manager đã dọn kèm đếm N/9.

### Dev S phát hiện thêm 2 việc (chưa vá, ngoài phạm vi)
1. **Có đường reset THỨ BA chưa ai nhắc:** `Editor/ChoiLaiTuDauTool.cs`, menu `Tools/Farm/CHƠI LẠI TỪ ĐẦU`.
   Nó cũng **thiếu `SaveVersionGuard.ClearAll()`** — cùng lỗ hổng. Sếp cho phép thì vá vòng sau.
2. `SaveBootstrap` là `DontDestroyOnLoad` tự lưu định kỳ mà **đường A cũng không dọn**. Về lý thuyết có cửa sổ
   hẹp để nó ghi lại dữ liệu cũ. Thực tế đường A vẫn chạy đúng vì scene nạp lại sinh manager sạch.
   Lối ① của Dev S né hẳn rủi ro này.

## 7. CĂN LẠI 3 SỐ HUD TRONG SCENE (Lead tự làm)
Sếp bảo "setup nữa là ok hết" nên Lead làm luôn cả bước tuỳ chọn này.

| Object | Field | Cũ | Mới |
|---|---|---|---|
| `TopLeft_Township_HUD/EXP_Bar_Container` | `m_AnchoredPosition.x` | 371.1 | **443** |
| `TopRight_Township_HUD/Gold_Container` | `m_AnchoredPosition.x` | -500 | **-574** |
| `TopRight_Township_HUD/Diamond_Container` | `m_AnchoredPosition.x` | -220 | **-248** |

Quy trình an toàn: dò theo `fileID` của đúng RectTransform (mỗi tên tìm thấy **đúng 1 lần**, giá trị hiện tại
khớp y báo cáo Dev P), neo regex vào cả `x` lẫn `y` để không đụng nhầm object khác, `assert` chỉ 1 kết quả
trước khi thay, **giữ nguyên `y`**.

**Kiểm sau khi ghi:** số block YAML **7219 trước = 7219 sau** (cấu trúc không đổi), chênh đúng **2 byte**
(`371.1` 5 ký tự thành `443` 3 ký tự), line-ending giữ nguyên.

Nhờ vậy cú nảy khi nhận thưởng lấy lại được độ đầy đặn: trần scale thanh EXP từ 1.083 lên **1.249**,
ngôi sao cấp từ 1.011 lên **1.73**, hai khung tiền từ 1.046 lên **1.20**.
`HudChongDeKhiPhongTo` tự đo lại khe hở mới nên không phải sửa code.

---

## 8. 🧑 SẾP CHỈ CÒN 2 VIỆC

**Bước 1 — Đóng Unity rồi mở lại. QUAN TRỌNG.**
Unity đang mở và đang giữ `SCN_Farm` trong RAM. Lead vừa sửa file scene và 3 file asset trên đĩa.
**Nếu Unity hỏi lưu Scene thì chọn `Don't Save`.** Bản trên đĩa mới là bản đúng.
Mở lại, chờ compile, Console 0 lỗi đỏ.

**Bước 2 — Bấm 1 menu tạo nút đóng** (việc DUY NHẤT Lead không làm thay được, vì phải chạy menu Unity):
`Tools` > `Farm Game` > `Nut dong CHINH TAY - Popup Nhiem vu` > OK.
Tool tự tạo nút, tự gắn vào ô Inspector, tự chọn sẵn trong Hierarchy. Sếp kéo vị trí, sửa Width/Height,
đổi Source Image cho ưng mắt rồi **Ctrl+S**. Từ đó Play giữ y nguyên.
Sprite vuông 64x64 có sẵn dấu X: `Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/btn_close.png`

> 🚫 **ĐỪNG BẤM** `Tools/Farm/UI/Dong bo nut dong - 3. APPLY` (xoá chỉnh tay cả 8 nút)
> 🚫 **ĐỪNG BẤM** `Editor/TownshipHUDBuilderTool` (dựng lại HUD, xoá 3 số vừa căn)

Xong 2 bước đó là Sếp test được toàn bộ. Bảng test 5 bộ lọc Console nằm ở mục "CẦN SẾP" của
`BAO_CAO_VONG8_2026-09-06.md`.

---

## 9. HOÀN TÁC & md5

| File | md5 trước | md5 sau |
|---|---|---|
| `SCN_Farm.unity` | `871a0279b96e106ece76148e63b79eb4` | `353c914e9628da8beb23acf545523ff0` |
| `FarmResetTool.cs` | `00f44111192c16a7a346ed5d4dbfff07` | `4babb8f1f8bd12050dc73ac23ab53138` |
| `LocStringTable.cs` | (bản vòng 8) | `76a4110f03fe...` |
| 3 file `.asset` | (bản gốc trong backup) | đã sửa typo |

Chép ngược từ `production/backup_vong9_2026-09-06/` là về nguyên trạng, **kể cả scene**.
