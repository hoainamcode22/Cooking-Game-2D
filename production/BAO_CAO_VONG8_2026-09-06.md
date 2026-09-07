# BÁO CÁO VÒNG 8 - 06/09/2026 — 10 TASK SẾP GIAO

> 7 Dev chạy SONG SONG, mỗi Dev một nhóm file riêng, không va chạm.
> Backup: `production/backup_vong8_2026-09-06/` (**43 file .bak**). 4 file .cs MỚI.
> **Chưa commit. 0 file `.unity`/`.prefab`/`.asset`/`.meta` bị đụng.**
>
> **Lead CHECK chéo, kết quả 43/43 ĐẠT:** cân bằng `{}` `()` `[]`, `#if`/`#endif` khớp, line-ending giữ
> đúng kiểu gốc từng file, không còn `if` rỗng. 4 file mới cũng cân bằng, Unity đã sinh `.meta` cho cả 4.
> **Không bản vá vòng 6/7 nào bị mất:** đã kiểm còn nguyên `giuNutCloseChinhTay`, `RutGonNhan`,
> `MERGED_CAPTION_BAND`, `CapCuoiTutorialDay`, `BeginProcessing`, `BiAnViThieuArt`, `anNutCong`, `LaBanDiLac`.

---

## TASK 1 — SẢN PHẨM CHUỒNG NẰM DƯỚI ĐẤT (Dev L)

**Thứ trong ảnh là `_readyBubble`**, do `PenMiniPanelUI.EnsureReadyBubble()` tạo bằng code lúc chạy.
Chuồng gà = Pen_03, có cả `productItemId: chicken_meat` lẫn `secondProductItemId: egg` nên bong bóng là khung
2 icon. "Nền trắng" là `readyBubbleBgSprite` null nên rơi về sprite sinh runtime tô màu kem `(255,246,214)`.

**NGUYÊN NHÂN THẬT — không phải nó thấp, mà là SAI LAYER:**
Canvas bong bóng tạo bằng `AddComponent<Canvas>()` và **chưa bao giờ được gán `sortingLayerName`**, nên nằm ở
id mặc định 0 = `"Default"`, chỉ là layer thứ 2 từ dưới trong 5 layer. Order 1500 vô nghĩa khi khác layer.
Bằng chứng: **mưa** (`"Foreground"`, order 240) và `Gangplank` (`"Objects"`, order 900) đều phủ lên bong bóng
order 1500. Ảnh Sếp chụp **đúng lúc trời mưa**.

> ⚠️ **PHÁT HIỆN LỚN:** **207 trong 251 SpriteRenderer** của `SCN_Farm` đang đeo sorting layer id
> **1669604809**, id này **KHÔNG tồn tại** trong `TagManager.asset`. Đây là bom hẹn giờ cho mọi bug che khuất
> về sau. Nên có một vòng dọn riêng. Lead ghi vào nợ kỹ thuật.

**Đã sửa:** đo tại runtime rồi mới đặt, không gõ số ma. Đo `bounds.max.y` thật của chuồng + con vật + mọi
bụi cỏ **đè lên bề ngang bong bóng**, đẩy đáy bong bóng cao hơn đỉnh đó 120 world unit. **Chỉ NÂNG, không hạ**
(`Mathf.Max` với số Sếp đã chỉnh tay). Layer lên `"Foreground"` qua `TouristSortingLayers.ResolveOrOverride`,
order = max(order đo được) + 200. Nhấp nhô biên độ 16, chu kỳ 2.4s, lệch pha theo `GetInstanceID()` để 4 chuồng
không nhô đồng loạt như máy.

**Click 1 lần là nhận:** nối vào `TryHarvest()` đã có, **không viết lại logic**. Dùng
`FarmInputLock.BlockWorldClickBySceneOrPopup` chứ KHÔNG dùng `BlockWorldInteraction` (hàm kia gọi
`ConTroTrenUiThat()` sẽ khiến bong bóng **tự chặn chính nó**, bấm mãi không ăn). Chống double-click 3 lớp.
`PenClickDetector` thêm chốt để cú bấm nhận sản phẩm không đồng thời mở khay cho ăn.

**Giữ nguyên 100% chữ ký public**, 7 field mới đều private có mặc định ⇒ Sếp không phải kéo lại gì.

---

## TASK 2 + 3 — TÀU HOẢ (Dev M)

### Đầu tàu lệch: sửa prefab là VÔ ÍCH
`BuildOrFixHierarchy()` **ghi đè vị trí đầu tàu vô điều kiện** mỗi lần mở popup
(`TrainStationMasterPopupUI.cs` dòng 425-437, gọi từ `Awake()` 78, `OnEnable()` 137, `OpenPopup()` 224,
cả 3 đều runtime thật). Kéo tay hay mổ prefab đều bị code ghi đè lại ngay.

**Số đo:** không có object `Rail` nào, ray được vẽ chết trong ảnh nền + mỗi sprite toa tự mang một đoạn ray.
Cả 4 toa và đầu tàu cùng anchor `(0, 0.5)` pivot `(0.5,0.5)` nên không cần quy đổi anchor; cái phải quy đổi là
`preserveAspect` (ảnh bị co trong ô). Tính ra **vạch ray = Y -106.31**, đầu tàu cần tâm Y **-30**.
Số cũ là **+30**, tức **treo cao hơn 4 toa đúng 60 đơn vị**. Khớp 100% với ảnh Sếp gửi.

**Đã sửa trong code** (prefab md5 KHÔNG đổi), thay số rời bằng hằng số đặt tên:
`LocoX = 4*185 + 130 = 870` · `LocoY = -30` · `LocoSize = 240` · gốc khói `(917, 85)`.
Sửa kèm 1 bug: khối gốc khói trước bọc trong `if (isNewSmk)` nên **chỉ chạy khi tạo mới**, prefab đã có sẵn
⇒ hạ đầu tàu mà khói vẫn treo. Nay luôn đặt lại.

**Về "chính diện": KHÔNG cần art vẽ lại.** Dev M đã mở ảnh
`Sprites/flat_locomotive_horizontal.png` (1204x772) xem tận mắt: đây là **hình cắt ngang phẳng hoàn toàn**,
không phải góc chéo 3/4. Nó *trông* xiêu chỉ vì bị treo cao 60 đơn vị và chồng lên nhà ga.

Một điểm art thật sự lệch (đưa vào đơn hàng art sau, không gấp): sprite **toa có vẽ kèm đoạn ray ở đáy** còn
sprite **đầu tàu thì không** ⇒ 2 vạch ray lệch nhau ~30 đơn vị. Muốn sạch tuyệt đối thì nhờ art xoá đoạn ray
khỏi sprite toa, để duy nhất ray của ảnh nền làm chuẩn.

### Gate cấp 3
File mới `Scripts/Train/TrainGateAccess.cs`, bắt chước nguyên mẫu `CookingGateAccess.cs` dự án đã dùng cho
cổng Bếp. Số 3 nằm **một chỗ duy nhất**: `public const int RequiredLevel = 3`.
**Số 3 không phải bịa:** trùng luôn với `AnimalGuideController.cs:80 TrainMinLevel = 3` đã có sẵn.

Chặn **3 đường mở** (grep hết mọi chỗ gọi `OpenPopup`, không còn đường thứ 4). Chưa đủ cấp thì hiện toast thân
thiện qua `FarmUIManager.ShowHint`, **không im lặng** (trẻ em sẽ tưởng game lỗi).
`CanOpen = ĐủCấp || CóViệcDởDang` ⇒ save cũ còn chuyến đang chạy vẫn mở được để thu nốt, không giam hàng.
Đã kiểm: không mission nào bắt nạp tàu trước cấp 3, không tutorial/deeplink nào tự mở popup tàu.

**Dev M đề xuất KHÔNG ẩn `gataulua`, chỉ khoá.** Lý do: (a) nó mang `PermanentBuilding`, component này sinh ra
để object không bao giờ bị ẩn; (b) nhà ga đứng sẵn + câu "mở ở cấp 3" là **động lực leo cấp**, ẩn đi thì trẻ em
không biết có tính năng; (c) đúng cách dự án đang làm với cổng Bếp. **Cần Sếp gật.**

---

## TASK 4 — NÚT ĐÓNG NHIỆM VỤ / THÀNH TỰU / ĐĂNG NHẬP (Dev N)

**"Nhiệm vụ" + "Thành tựu" + "Đăng nhập" là 3 TAB CỦA CÙNG MỘT POPUP**, dùng chung MỘT nút đóng.
Tab "Hằng ngày" tiêu đề đổi thành **"ĐIỂM DANH"**, nội dung là streak 7 ngày ⇒ đúng cái Sếp gọi là "đăng nhập".
Không có popup Thành tựu riêng nào. Vòng 6 đã vá đủ cho cả 3 tab.

> **NHƯNG có một sự thật chưa ai nói:** trong `SCN_Farm`, object `UnifiedTaskPopupRoot` **có `m_Children` RỖNG,
> không một con nào**, và ô `btnCloseChinhTay` **đang TRỐNG** (`fileID: 0`). Cả popup do code dựng lúc Play.
> ⇒ Ngoài Edit Mode **Sếp không có gì để kéo cả**. Vá vòng 6 mới chỉ mở sẵn cái móc, chưa có nút treo vào móc.
> **Đây mới là lý do thật Sếp vẫn thấy không chỉnh được.**

**Đã làm:** file mới `Editor/NutDongChinhTayTool.cs`, menu
`Tools/Farm Game/Nut dong CHINH TAY - Popup Nhiem vu` (cố ý đặt xa nhánh `Tools/Farm/UI/` chứa công cụ phá hoại).
Tool tạo sẵn MỘT nút đóng thật trong scene và gắn vào ô Inspector để Sếp kéo tay thoải mái. Không phá: ô đã gắn
thì không đụng; có nút sẵn thì dùng lại giữ nguyên; chỉ khi trống mới dựng mới 64x64 sprite chuẩn và
**LUÔN hiện chữ X**. Chạy lại nhiều lần không nhân đôi, có Undo.

Vá kèm `PopupEwarManager.cs:45`: `btnClose.onClick.AddListener()` gọi thẳng không kiểm null ⇒ ô Inspector trống
là NullReference ngay dòng đầu `Awake`, **cuốn theo cả `MissionProgressTracker.OnProgressChanged` bên dưới**
⇒ tiến độ nhiệm vụ hết cập nhật realtime. Nay đã rào.

---

## TASK 5 + 6 — HIỆU ỨNG THƯỞNG BAY VÀO HUD (Dev O)

**Hệ FX đã có sẵn và rất tốt:** `Scripts/UI/RewardFlyFX.cs`, API `RewardFlyFX.Fly(kind, amount, ...)`.
Nó **tự nghe 3 sự kiện** `OnGoldAddedFx`, `OnGemAddedFx`, `OnExpAddedFx` ⇒ mọi lời gọi `AddGold/AddGems/AddExp`
đã tự bung FX sẵn. Nhịp: bung xoáy 0.32s ease-out-back, khựng 0.06s, bay bezier 0.58s. Cỡ icon Gold 82 / Gem 74
/ EXP 108 px. Dùng `Time.unscaledDeltaTime` nên chạy được khi popup pause game.

**NGUYÊN NHÂN THẬT — không phải "thiếu animation":**
FX **đã chạy** mỗi lần bấm Nhận. Nhưng icon gắn vào `Canvas_HUD` sortingOrder **100**, còn popup nhiệm vụ tự
bật `overrideSorting = true; sortingOrder = 120` ⇒ **FX bay phía sau popup nên vô hình**.
Chồng thêm: bảng nhiệm vụ tự vẽ FX riêng chỉ **44px**, và **vàng thì hoàn toàn không bay**.

**Đã sửa:** dựng node `RewardFlyFX_Overlay` mang Canvas riêng `overrideSorting`, sortingOrder **420**.
Căn cứ chọn 420: trên **mọi** popup phát thưởng (cao nhất là 410) nhưng vẫn **dưới bàn tay hướng dẫn 440**
(trẻ con phải luôn thấy bàn tay), dưới khay Pen 800, ghost kéo 999, chuyển cảnh 9999.
Lớp này kéo giãn phủ kín canvas cha nên **không phải sửa một dòng toán nào** của FX cũ.

**Nối vào 3 tab và Bảng Đơn Hàng bằng "ngắm điểm", không gọi `Fly()`:** vì FX đã tự nghe sự kiện, gọi thêm sẽ
ra **hai chùm icon**. Cách này bảo đảm đúng một chùm và **tuyệt đối không chạm đường cộng thưởng**.

**Kiểm không cộng 2 lần (bằng máy, không đoán):** đếm lời gọi kinh tế sau khi sửa — `RewardFlyFX.cs` **0 lần**,
`OrderBoardPopupUI.cs` **0 lần**, `UnifiedTaskPopupUI.cs` **đúng 1 lần mỗi loại**, đều trong `GrantRewards`.
Chặn spam vẫn nguyên (ghi cờ đã nhận TRƯỚC khi phát thưởng).

**"Nếu có kim cương":** đã đọc dữ liệu, `OrderData.cs` **không có trường gem nào**, chỉ `rewardGold` và
`rewardExp`. Nên đơn hàng không bắn gem, **không bắn gem giả**. Hôm nào bên đơn hàng thêm `AddGems` thì chùm gem
tự bay ra, không phải sửa lại file.

---

## TASK 7 + 8 — HUD ĐÈ NHAU + XOÁ DẤU + (Dev P)

**Bác bỏ giả thuyết "khung nở theo số" bằng số đo:** trong toàn bộ `Canvas_HUD` **không có một
`ContentSizeFitter`, `HorizontalLayoutGroup` hay `LayoutElement` nào**. Cả 4 ô chữ đều `enableAutoSizing = 0`,
`overflowMode = Overflow` ⇒ số dài thì CHỮ tràn chứ khung không giãn.

**THỦ PHẠM THẬT: `JuicyPulseFX.cs:113` `target.localScale = baseScale * scaleMultiplier`** (1.20 tới 1.25),
được gọi từ **5 chỗ với 3 đích khác nhau**, không chỗ nào là file HUD:
`RewardFlyFX:384` → Level_Star_Badge (1.25) · `HarvestFeedbackSpawner:269` → EXP_Bar_Container (1.22) ·
`CoinFlyFX:182` → Gold_Container (1.20) · `GemFlyFX:192` → Diamond_Container (1.20) ·
`UnifiedTaskPopupUI:2220` → cả ba (1.22).

**Vì sao "chơi một hồi mới lệch":** cú phóng to chỉ chạy khi có thưởng bay tới. Nhận càng nhiều thì trạng thái
phình gần như thường trực. Cộng thêm khe hở lúc đứng yên vốn đã mỏng khủng khiếp:
**avatar tới ngôi sao cấp chỉ hở 1.3 px**, **khung vàng tới icon gem hở 16.6 px**. Một cú nảy là đè.

**Chồng lấn tính ra được:** ngôi sao là **CON của thanh EXP**, nên thanh EXP phóng 1.22 kéo ngôi sao trượt thêm
48.8 px sang trái ⇒ **đè khung avatar 59.4 px**. Hai khung tiền cùng nảy ⇒ **đè nhau 27.6 px**.

**Sửa gốc, không kê số:**
1. **Gỡ `Level_Star_Badge` ra khỏi `EXP_Bar_Container`**, treo lên cụm cha, giữ nguyên 100% vị trí và thứ tự vẽ.
   Một cái badge phải đứng yên thì không được nằm trong object bị FX phóng to. Xoá đứt 59.4 px chồng lấn.
2. File mới `HudChongDeKhiPhongTo.cs`: ở nhịp `LateUpdate` đầu tiên đo khe hở THẬT bằng `GetWorldCorners`,
   suy ra trần scale (mỗi bên chỉ ăn 45% khe hở). Các nhịp sau **chỉ kéo về trần khi vượt, không vượt thì không
   ghi gì cả** ⇒ chỉnh tay của Sếp không bị đụng. Trần tự tính lại nếu Sếp kéo cho chúng xa nhau ra.
3. Chống tràn chữ theo đúng cách dự án đã dùng ở `UnlockSlotUI`: `enableAutoSizing` khoảng HẸP, `NoWrap`.
   **Không tự chế định dạng "12,5K"** vì đã grep, dự án không có sẵn hàm rút gọn nào.
4. Vá kèm 1 lỗi build: `TownshipHUDController` dùng `new CultureInfo("vi-VN")` trong static field ⇒ build
   IL2CPP bật Invariant Globalization sẽ ném `CultureNotFoundException` **chết cả HUD**. Đã đổi sang
   `NumberFormatInfo` tự khai, giữ y nguyên ký tự phân nhóm đang thấy.

**Xoá 2 dấu +:** `SetActive(false)` có cờ `anNutCong` bật lại được, **không Destroy**.
Đã grep: 2 nút đó chỉ mở Shop, mà lối vào Shop vẫn còn nguyên ở tab CỬA HÀNG góc trái dưới ⇒ **không mất chức
năng nào**. Chỗ trống để lại thành phòng cho số dài ra.

> **Cái giá phải trả:** cú nảy trên 2 khung tiền và ngôi sao sẽ **nhẹ đi rõ rệt** (1.046 và 1.011 thay vì 1.20
> và 1.25), vì khe hở hiện chỉ 16.6 px và 1.3 px. Muốn nảy đầy đặn lại thì nới khe hở, số cụ thể ở mục CẦN SẾP.

---

## TASK 9 — CÒN ITEM CHƯA DỊCH SANG TIẾNG ANH (Dev Q)

**Hệ dịch tự viết** ở `Scripts/Localization/`: bảng chuỗi là `Dictionary<string,string>` với **khoá chính là câu
tiếng Việt**. Có `LocRuntimeInterceptor` quét mọi `TMP_Text` mỗi 0.2s và thay câu ⇒ **chuỗi tiếng Việt
hard-code trong code KHÔNG phải bug, miễn là câu đó có trong bảng.**

**NGUYÊN NHÂN GỐC:** bảng gõ **Kiểu Tên Riêng** (`"Phở Bò Tái"`, `"Cà Rốt"`) nhưng file `.asset` và code viết
**thường** (`"Phở bò tái"`, `"Cà rốt"`). Tra khớp từng ký tự nên **TRƯỢT HẾT** ⇒ bấm English, tên món và tên
item vẫn tiếng Việt. Đúng y triệu chứng Sếp mô tả.

**Sửa gốc:** `LocalizationManager.T()` thêm **tra lần hai không phân biệt hoa/thường**, có chỉnh kiểu chữ.
Tra khớp tuyệt đối vẫn chạy TRƯỚC nên cặp `"Cửa hàng"/"CỬA HÀNG"` không lẫn.
**Một thay đổi này cứu 89 chuỗi**, gồm toàn bộ 38 tên món.

**Con số:** bảng dịch **760 → 1409 khoá**. Quét 583 file `.cs` + 863 file `.asset`.
**674 chuỗi người chơi thấy chưa dịch → đã vá 647 (96%)**, chuỗi trong `.asset` phủ **555/555 = 100%**.
Còn **27 chuỗi** đều nằm trong file dev khác đang giữ vòng này (Train 6, UnifiedTask 8, Avatar 7, Pen 3, TrainManager 3).
Kiểm cả 1409 khoá: **0 khoá trống, 0 khoá bản EN còn dấu tiếng Việt, 0 khoá trùng lặp**
(khoá trùng sẽ ném `ArgumentException` lúc chạy).

Cố tình KHÔNG dịch: 50 tên người bán (danh từ riêng), tên nông trại mặc định (Interceptor bỏ qua ô nhập liệu),
6 mảnh chuỗi dùng để **so khớp** chứ không hiển thị (dịch là hỏng logic).

**3 khe hở còn lại (chưa vá, cần nhiều file dev khác):** chuỗi có tham số (`"Cấp {0}"`) không refresh nóng
được vì Interceptor tra khớp cả câu ⇒ popup đang mở phải đóng mở lại; nháy 1 khung hình khi mở popup mới;
nhãn cập nhật mỗi frame có thể nhấp nháy Việt/Anh.

---

## TASK 10 — POPUP AVATAR HIỆN DỮ LIỆU CŨ (Dev R)

**KHÔNG PHẢI dữ liệu cũ. Là UI chưa từng ghi được số nào.**

Popup không có tab, nó là lưới **4 thẻ thống kê**. Chuỗi 3 mắt xích:
1. `BuildOrFormatUI` thấy `Board_Wooden` + `Badge_Check` thì `return` sớm, không dựng lại. Nhánh này **luôn chạy**.
2. `AutoWireNewHierarchy` tìm node tên `Txt_WarehouseVal / Txt_CookingVal / Txt_GoldVal / Txt_AchievementVal`.
   Đếm trong scene: **cả 4 đều = 0**. Tên thật đang dùng là `Txt_Value` (6 cái). Scene dựng bằng bản code cũ,
   sau này code đổi sang tên riêng nhưng **scene không được dựng lại** ⇒ cả 4 ref `TMP_Text` = **null**.
3. `RefreshStats` bọc mọi lệnh gán trong `if (txt != null)` ⇒ **không gán gì cả, im lặng, không lỗi**.

Chữ trên thẻ giữ nguyên giá trị bake vào scene: `120 ô` / `35 món` / `1 520` / `18 đã xong` —
**khớp từng chữ với 4 số mockup hardcode ở dòng 893-896 của chính file**.
⇒ "Dữ liệu cũ" thực ra là **số mockup của designer bị đóng băng vĩnh viễn**. Reset kiểu gì cũng không đổi.

Đã loại trừ giả thuyết (a): `OpenPopup` đã gọi `RefreshAll()` mỗi lần mở, `SlotCapacity` đọc live,
`SaveBootstrap.AutoRestoreMissingPrefs = false`. Cả 3 đều sạch.

**"Thời gian chơi" không tồn tại** trong game: grep mọi biến thể `PLAY_TIME/TOTAL_TIME/...` = **0 kết quả**.

**Đã sửa:** helper `FindStatValueText` thử tên riêng trước, không có thì lấy `Txt_Value` **giới hạn bên trong
đúng thẻ đó** (phải giới hạn vì cả 4 thẻ đều có node trùng tên). Thêm LogWarning để loại bug "im lặng" này
không tái diễn. Và "Điểm nấu ăn" trước luôn ra `0 món` vì 2 khoá prefs kia **không ai ghi** ⇒ nay đọc nguồn thật
`MissionProgressTracker`. **Không phát sinh khoá mới nào cần bảo trì.**

**Phát hiện phụ:** `Tools/SCN Farm/Hard Reset Everything` (`FarmResetTool.cs`) có lỗ hổng: không gọi
`SaveSystem.DeleteSave()`, không gọi `SaveVersionGuard.ClearAll()`, và **bỏ sót 5 manager DontDestroyOnLoad**
còn sống với dữ liệu cũ trong RAM. Đường reset trong Cài đặt thì **đầy đủ, không sót gì**.

---

## 🧑 CẦN SẾP LÀM

**Bước 0.** Compile, Console 0 lỗi đỏ. Có **4 file .cs mới**, Unity đã tự sinh `.meta` cho cả 4.

**Bước 1 — Tạo nút đóng cho popup Nhiệm vụ (giải đúng cái Sếp hỏi):**
Menu `Tools` > `Farm Game` > `Nut dong CHINH TAY - Popup Nhiem vu` > OK.
Tool tạo nút `Btn_Close_ChinhTay` dưới `Canvas_Popup/UnifiedTaskPopupRoot`, tự gắn vào ô Inspector, tự chọn sẵn
trong Hierarchy. Sếp kéo vị trí, sửa Width/Height, đổi Source Image cho ưng mắt. **Ctrl+S**. Play giữ y nguyên.
Sprite vuông 64x64 **có sẵn dấu X**: `Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/btn_close.png`
> 🚫 **TUYỆT ĐỐI KHÔNG bấm `Tools/Farm/UI/Dong bo nut dong - 3. APPLY`** — xoá sạch chỉnh tay của cả 8 nút.

**Bước 2 — Test theo Console** (mỗi mục một bộ lọc):
| Lọc | Kỳ vọng |
|---|---|
| `BONGBONG_DAT` | `layer=Foreground`, `order>=1500`. **Gửi Lead dòng này**, nó chốt một mục CHƯA CHẮC |
| `BONGBONG_CLICK` | bấm 1 lần ra `nhan=True`, bong bóng tắt, **không mở khay** |
| `[Train]` | đầu tàu nằm đúng ray; đặt cấp 1 click ga phải ra chữ "Tau hoa mo o cap 3 nhe!" và popup KHÔNG mở |
| `[RewardFlyFX]` | `Dựng lớp vẽ RewardFlyFX_Overlay (sortingOrder = 420)` đúng **1 lần** |
| `[AvatarProfile]` | **KHÔNG** được có dòng "Thiếu ô chữ giá trị..." |

Popup avatar phải hết `120 ô / 35 món / 1 520 / 18 đã xong`, thay bằng số thật.
Bấm Nhận ở cả 3 tab: chùm vàng/gem/sao **to** bung xoáy bay về HUD, số cộng **đúng 1 lần**.
Giao hàng: hiệu ứng khói+sao cũ **vẫn còn**, **thêm** chùm vàng+EXP bay về HUD, **không có gem**.

**Bước 3 — Sửa 5 asset lỗi dữ liệu** (người chơi Việt cũng đang thấy sai):
| File | Hiện tại | Sửa thành |
|---|---|---|
| `Farm_May_Che_Bien/Item_BotGao.asset` | `Bá»t gáº¡o` (**hỏng font**) | `Bột gạo` |
| `Farm_May_Che_Bien/Item_NuocMiaEp.asset` | `NÆ°á»c mía ép` (**hỏng font**) | `Nước mía ép` |
| `Farm_dong_vat/Item_bo_ham_ca_rot.asset` | `Bò hầm cà rót` | `Bò hầm cà rốt` |
| `Farm_dong_vat/Item_sup_ngo_nam.asset` | `Súp ngo nấm` | `Súp ngô nấm` |
| `Farm_dong_vat/Item_trung_op_la_bo_ne.asset` | `Trứng óp la bò né` | `Trứng ốp la bò né` |
> Sửa xong **phải xoá 2 khoá hỏng font** ở `LocStringTable.cs` dòng 820 và 881.

**Bước 4 — Tên nhiệm vụ đang lòi mã nội bộ ra trước mặt người chơi** (`data/Data_Ewa/`):
`"Nấu 10 món bo_ham_ca_rot"`, `"Thu hoạch 100 hoa_hong"`, `"Thu hoạch 10 rice"`, `"Thu hoạch 105 carot"`...
Đây là bug dữ liệu, nên sửa thành tên hiển thị.

**Bước 5 (tuỳ chọn) — Lấy lại cú nảy đầy đặn cho HUD:**
| Object | Field | Cũ | Mới |
|---|---|---|---|
| `TopLeft_Township_HUD/EXP_Bar_Container` | Pos X | 371.1 | **443** |
| `TopRight_Township_HUD/Gold_Container` | Pos X | -500 | **-574** |
| `TopRight_Township_HUD/Diamond_Container` | Pos X | -220 | **-248** |

---

## ⚠️ CẦN SẾP QUYẾT (3 việc)
1. **Ẩn hay chỉ khoá `gataulua` dưới cấp 3?** Dev M đề xuất **chỉ khoá** (lý do ở Task 3). Ẩn thì phải sửa scene.
2. **Vá `FarmResetTool.cs`** cho uỷ quyền sang `SettingsPopupUI.OnResetProgressClicked()` thay vì tự làm thiếu?
3. **Có dịch `"Nông Dân Vui Vẻ"` (tên nông trại mặc định) sang `"Happy Farmer"` không?**
   Dev Q để nguyên vì Interceptor bỏ qua ô nhập liệu, dịch sẽ khiến HUD và ô "Tên nông trại" lệch nhau.

## NỢ KỸ THUẬT MỚI
1. **207/251 SpriteRenderer trong `SCN_Farm` đeo sorting layer id chết `1669604809`.** Bom hẹn giờ cho mọi bug
   che khuất. Cần một vòng dọn riêng, gán lại về 5 layer thật.
2. `Editor/TownshipHUDBuilderTool.cs` `DestroyImmediate` cả gốc HUD rồi dựng lại ⇒ cùng loại nguy hiểm với
   `CloseButtonSyncTool`. **ĐỪNG BẤM.**
3. `TrainPackageBuildTool.cs:485` còn giữ số đầu tàu CŨ (`4*190+115`, size 230). Runtime vẫn đúng vì code ghi đè,
   nhưng nên đồng bộ để lần sau không ai bị lừa.
4. `Mission/SkinVi.cs:48` ghi đè sprite mọi nút tên chứa "close". Hiện ngủ đông (`batAo = false`), bật lên là hỏng.
5. Chuỗi có tham số không refresh nóng khi đổi ngôn ngữ (cần thêm `OnChanged += Refresh` cho 4 popup).
6. `HomeMenuManager.cs:27-30` cùng họ NullReference với `PopupEwarManager`, chưa sửa.
