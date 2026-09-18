# BÁO CÁO BÀN GIAO - VÒNG 1 ĐẾN VÒNG 4

Kính gửi Sếp,

Đây là báo cáo đầy đủ của bốn vòng làm việc vừa rồi. Em viết theo thứ tự quan trọng: lỗi nặng trước, việc Sếp phải tự làm trong Unity ở mục 8, rủi ro còn lại ở mục 9.

---

## 1. Tóm tắt

- **37 file C#** được sửa hoặc thêm mới, trải qua 4 vòng.
- **Không đụng vào một file scene, prefab hay asset nào.** Không có `.unity`, `.prefab`, `.asset`, `.meta` nào bị ghi. Đây là ranh giới em tự đặt ra để Sếp không bao giờ phải lo mất dữ liệu scene.
- Toàn bộ đã ghi xuống đĩa, Sếp chỉ cần pull về là có.
- Vì chỉ sửa được code, nên có một danh sách việc tay bắt buộc trong Unity. Xem mục 8. Nếu bỏ qua mục 8 thì một phần công việc của bốn vòng này sẽ không có tác dụng.

---

## 2. Bug nghiêm trọng đã vá

### 2.1. Bảng dịch chưa bao giờ nạp được - 73 key trùng trong `LocStringTable`

Đây là phát hiện quan trọng nhất của cả bốn vòng.

`LocStringTable` khai báo bảng dịch bằng collection initializer của C#. Cú pháp đó thực chất gọi `Add()` cho từng dòng, mà `Dictionary.Add()` gặp key trùng thì **ném `ArgumentException`**. Bảng có **73 key bị lặp**, nên ngay ở lần khởi tạo static đầu tiên, exception bắn ra và **toàn bộ bảng dịch không bao giờ nạp xong**. Không phải mất 73 câu, mà là **mất sạch**. Mọi câu dịch trong game đều chết, game chỉ còn hiển thị chuỗi gốc. Người chơi không thấy game "dịch sai", họ thấy game "không có đa ngôn ngữ".

Đã dọn xong: hiện **1677 key duy nhất, 0 key trùng**. Bảng nạp sạch.

### 2.2. `TutorialManager` so chữ với một câu tiếng Việt cứng

Chỗ tìm nút bỏ qua hướng dẫn, code đi so text của một nhãn TMP với một literal tiếng Việt. Khi game chạy tiếng Anh, nhãn đó đã là tiếng Anh nên **không bao giờ khớp**, nút bỏ qua coi như biến mất. Người chơi tiếng Anh bị kẹt trong tutorial không thoát ra được. Đã đổi sang cách nhận diện không phụ thuộc ngôn ngữ.

### 2.3. `new CultureInfo("vi-VN")` làm sập màn Shop trên bản release

`ShopItemUI` và `ShopManager` dựng `new CultureInfo("vi-VN")` để định dạng số. Trên bản build IL2CPP đã strip, culture data không có sẵn, dòng này **ném `CultureNotFoundException`**. Trong Editor thì chạy ngon nên không ai thấy, nhưng trên máy thật, người chơi bấm mở Shop là màn hình đứng. Đã thay bằng cách định dạng không cần culture.

### 2.4. Popup tàu nằm trong `#if UNITY_EDITOR` nên không tồn tại trong bản build

Đoạn code tạo ba popup của ga tàu bị bọc trong `#if UNITY_EDITOR`. Trong Editor bấm vào ga thì popup hiện, nên nhìn như đã xong. Nhưng bản build thật thì đoạn đó bị cắt bỏ hoàn toàn, **bấm vào ga không có gì xảy ra**. Đã chuyển sang đường tham chiếu prefab qua `[SerializeField]` có trong build, kèm dự phòng `Resources.Load`. Lưu ý: phần này **chỉ hết lỗi sau khi Sếp kéo 3 prefab vào Inspector** - xem mục 8c.

---

## 3. Hiệu năng

| Chỗ sửa | Trước | Sau | Vì sao quan trọng |
|---|---|---|---|
| Mesh mưa | dựng lại **mỗi frame**, 3-8 ms/frame, field cho phép tới 6000 hạt | dựng **một lần**, sau đó chỉ dịch chuyển | Mỗi frame mất 3-8 ms là ăn đứt một nửa ngân sách 16.6 ms của 60fps, chỉ để vẽ mưa |
| Quét localization lúc chạm | chạy ngay ở **touch-down**, khựng 5-40 ms | bỏ khỏi đường chạm, chuyển sang nhịp nền | Khựng xảy ra **đúng khoảnh khắc ngón tay bắt đầu kéo map** - đây là cảm giác "game giật" mà Sếp hay gặp nhất |
| `PointerEventData` + `RaycastAll` của camera | cấp phát mới **mỗi frame** khi đang kéo | cache theo frame + vị trí chạm | Cấp phát liên tục khi đang kéo sinh rác GC, GC chạy giữa lúc kéo là một cú khựng thấy rõ |
| `FindObjectsByType` | gọi lặp lại | cache lại | `FindObjectsByType` quét toàn scene, càng nhiều object càng chậm, gọi mỗi frame là lãng phí thuần |
| `TilemapExpander` | đặt từng tile một, spike 10-60 ms | gom về `SetTiles` một lượt | Spike rơi đúng lúc người chơi kéo tới **mép bản đồ**, nghĩa là giật ngay lúc đang thao tác |
| `MapBoundary` | tính lại đầy đủ mỗi lần | thoát sớm + vòng lặp hội tụ | Giảm chi phí cố định mỗi frame của camera |
| `Application.targetFrameRate` | **chưa từng được đặt** trong lịch sử project | `= 60`, kèm `vSyncCount = 0` | Unity mobile mặc định khoá **30fps**. Máy chạy được 60 nhưng bị ép xuống 30. Đây là thay đổi một dòng cho cảm giác mượt gấp đôi |

Ghi chú về `targetFrameRate`: phải tắt vSync trước, vì khi `vSyncCount > 0` thì Unity **bỏ qua hoàn toàn** `targetFrameRate`. File `MobilePerformanceBootstrap.cs` làm cả hai, chạy bằng `[RuntimeInitializeOnLoadMethod]` nên không cần kéo component vào scene nào và không thể quên kéo.

---

## 4. Kéo map mượt

Đây là chỗ em muốn Sếp đọc kỹ, vì nó giải thích vì sao game "giật" ngay cả khi FPS đã đủ 60.

Công thức cũ trong `CameraController`:

- `panSpeed = 3f` nhân quãng dịch chuyển của ngón tay lên **3 lần**. Ngón tay đi 1 cm thì camera được lệnh đi 3 cm.
- Ngay sau đó `SmoothDamp` với `panSmoothTime = 0.08` **kéo ngược lại** cho khỏi văng.

Hai lực đánh nhau: một bên đẩy vọt lên 3 lần, một bên hãm lại. Kết quả là map luôn **trễ và nảy** so với ngón tay, kiểu dây thun. Mắt người đọc hiện tượng đó **y hệt như giật hình**, dù máy đang khoá đủ 60fps. Đây không phải vấn đề hiệu năng, mà là vấn đề công thức.

Đã thêm một field serialize mới:

- **`dragOneToOne`** - mặc định **BẬT**. Khi bật, đường kéo bỏ hẳn hệ số `panSpeed` (map đi đúng bằng ngón tay, 1:1) và dùng `dragSmoothTime = 0.02` thay cho `panSmoothTime`. 0.02 đủ nhỏ để map dính tay nhưng vẫn lọc được rung tay.
- Khi **TẮT**, code chạy y hệt bản cũ (`panSpeed * delta` + `panSmoothTime`). Không mất gì.

**Nếu Sếp thích cảm giác cũ hơn, đường revert chính xác:**

> Chọn object có `CameraController` trong Hierarchy → Inspector → component **Camera Controller** → bỏ tick **`Drag One To One`**.

Bỏ tick là quay lại ngay lập tức, không cần build lại, không cần sửa code. Hai field `panSpeed` và `panSmoothTime` vẫn nguyên giá trị cũ trong scene.

---

## 5. Đa ngôn ngữ

**Hiện trạng:** mặc định game đã chạy **tiếng Anh**. Bảng dịch có **1677 key**, 0 key trùng, nạp sạch.

**Tự co chữ (auto-fit) - mới:** `LocRuntimeInterceptor` có thêm một lượt quét, khi vẽ tiếng Anh mà chữ bị tràn khỏi khung thì nó **thu nhỏ cỡ chữ lại**, sàn dưới là **72% cỡ thiết kế gốc** (và không bao giờ nhỏ hơn 8pt). Nhãn một dòng thì đặt thêm `Ellipsis` để cắt bằng dấu ba chấm thay vì đè lên khung. Cỡ chữ gốc được nhớ lại, nên khi chuyển về tiếng Việt thì **trả lại đúng nguyên trạng**, không để lại chữ bé.

Tắt tính năng này nếu cần:

```csharp
LocRuntimeInterceptor.AutoFitEnabled = false;
```

**Đã bổ sung trong bảng dịch:**

- **206 key còn thiếu** đã được thêm.
- **186 giá trị tiếng Anh được rút gọn** cho vừa khung: 145 tiêu đề đơn hàng cắt còn tối đa 24 ký tự, 29 tên món còn 18 ký tự, 11 nhãn UI.
- Sửa các chuỗi **tiếng Việt viết không dấu** lọt vào bảng: `"Khu dat"`, `"MO O CAP 40"`, `"vang"`, `"kim cuong"`. Kèm thêm một **lớp dự phòng bỏ dấu** trong hàm tra cứu, nên chuỗi thiếu dấu vẫn tra ra đúng câu dịch thay vì rơi ra ngoài.
- Khi thoát game trong Editor hoặc dev build, hệ thống ghi file **`loc_missing.txt`** vào `Application.persistentDataPath`, liệt kê mọi chuỗi vẫn chưa có bản dịch. Sếp cứ chơi một vòng rồi mở file đó ra là có danh sách việc còn lại, không phải đoán.

---

## 6. Ảnh nét lại

Chi tiết đầy đủ nằm ở **`ROUND2_TEXTURE_NOTES.md`** (cùng thư mục). Tóm tắt phần Sếp cần biết:

- **Crunch không phải thủ phạm.** `crunchedCompression: 0` ở khắp nơi. Đừng đi sửa chỗ đó.
- Mờ đến chủ yếu từ hai nguồn: **sprite bị vẽ ở tỉ lệ không đúng kích thước gốc** (project đang có **sáu giá trị PPU khác nhau**: 64 / 90 / 100 / 128 / 132 / 256), và **nén ASTC 6x6** (chỉ 3.56 bpp, không đủ bit cho gradient mềm và cạnh chéo 45 độ của isometric). Cộng thêm `maxTextureSize` bị ép xuống 1024 và mipmap bị tắt toàn bộ.
- Công cụ mới **`Tools ▸ Farm Game ▸ Restore Texture Quality`** khôi phục: `maxTextureSize` về **2048**, format về **ASTC 5x5**, **bật mipmap**, đồng bộ filter Bilinear, `compressionQuality` lên 100 (khoản này **miễn phí về bộ nhớ**, ASTC là bitrate cố định).
- **Cảnh báo về bộ nhớ, đọc trước khi bấm Apply:**
  - 6x6 lên 5x5 là **+44%** bộ nhớ texture.
  - 1024 lên 2048 là **tới ×4** với những ảnh gốc lớn hơn 1024 (ảnh gốc nhỏ hơn thì không tốn thêm gì).
  - Bật mipmap là **+33%** nữa.
  - Trường hợp xấu nhất cộng dồn khoảng **×7.7**.
- Vì vậy: **chạy theo từng thư mục, không chạy cả `Assets/` một lượt**, và **build thử lên máy thật xem Profiler ▸ Memory giữa các lượt**. Thứ tự khuyên dùng: `Assets/maptitle` → `Assets/Art` → `Assets/Day_Night` → phần còn lại.
- Công cụ **chỉ báo cáo** về PPU, cố tình **không tự sửa**, vì đổi PPU là đổi ngay kích thước vật lý của sprite trong scene, kéo theo lệch prefab, sai collider, hở khe lưới isometric.

---

## 7. Chia lô đất

Chi tiết đầy đủ nằm ở **`ROUND4_LAND_REPORT.md`** (cùng thư mục). Tóm tắt:

- **Runtime chưa bao giờ cần hàng rào.** Code chạy game chỉ đọc `LandRegionData.cellRects`. Hàng rào thuần tuý là đồ hoạ. Sự phụ thuộc vào hàng rào chỉ nằm trong **thuật toán flood-fill của công cụ quét trong Editor**, không nằm trong game.
- **Vùng phía Bắc bị bỏ trống không phải vì thiếu hàng rào**, mà vì hai bộ lọc: `BuildGreenLandMask()` chỉ nhận tilemap có tên chứa `grass/dirt/stone/co_/dat_nen/mong` nên lớp cỏ Bắc bị loại, và `BuildDuongRayMask()` lấy **hộp bao AABB** của đường ray rồi nới thêm 2 ô mỗi phía - đường ray chéo và dài nên hộp bao nuốt cả vùng Bắc.
- Công cụ mới **`Tools ▸ Farm Game ▸ Land Region Author`**: Sếp **kéo một cái hộp ngay trong Scene view**, công cụ tự đổi sang toạ độ ô, tự bẻ hình thoi thành các `RectInt` theo bậc thang, tự kiểm tra chồng lấn với **mọi** khu đất đang có, và định giá **theo diện tích** thay vì theo chỉ số.
- **Bắt buộc dùng tiền tố `North_`, tuyệt đối không dùng `Lot_`.** Lý do: `IsoFenceLotScanner` mỗi lần chạy lại sẽ **xoá và đánh số lại toàn bộ asset `Lot_*`**, mà save của người chơi là danh sách `regionId` trong `PlayerPrefs["FARM_UNLOCKED_REGIONS"]` - đánh số lại nghĩa là lô đã mua trỏ sang mảnh đất khác.
- Vòng này đã thêm **hộp thoại chặn có xác nhận** trước khi `IsoFenceLotScanner` ghi đè, và vòng lặp xoá nay **bỏ qua mọi file không bắt đầu bằng `Lot_`**, nên `North_*` và `Land_*` an toàn tuyệt đối.

### Lỗi định giá cần Sếp quyết

`IsoFenceLotScanner.PriceOf(i, total)` tính giá theo **chỉ số lô**, không theo diện tích. Hậu quả:

| Lô | Số ô | Giá vàng | Vàng/ô |
|---|---:|---:|---:|
| `lot_27` | **13** | **390.130** | **30.010** |
| `lot_03` | **104** | **11.600** | **111** |
| Trung vị các lô cỡ thường | - | - | **960** |

`lot_27` bé hơn `lot_03` tám lần mà đắt hơn ba mươi ba lần. Người chơi sẽ thấy vô lý ngay lần đầu bấm vào biển báo. Công cụ mới đã định giá theo diện tích để không lặp lại, nhưng **các lô cũ vẫn đang sai giá** và cần Sếp quyết có chỉnh lại không.

---

## 8. ⚠️ ANH CẦN LÀM TRONG UNITY

Đây là mục quan trọng nhất của cả báo cáo. Không việc nào dưới đây làm được từ code, vì scene và prefab nằm ngoài phạm vi em được phép đụng.

**a. Pull code an toàn**

1. **Đóng Unity trước khi pull.**
2. Pull về.
3. Mở lại Unity, chờ recompile xong hẳn.
4. **Mở Console và kiểm tra: phải 0 lỗi đỏ.** Nếu có lỗi đỏ, dừng lại và báo em, đừng làm tiếp các bước sau.

**b. Gắn `LanguageToggleUI` vào màn Cài đặt**

1. Chọn object **cha của hai chip ngôn ngữ** trong màn Cài đặt.
2. Add component **`LanguageToggleUI`**.
3. **Chuột phải lên phần header của component** → chọn **"Auto-find buttons in children"**.
4. **Đọc dòng nó in ra trong Console.** Nếu nó báo tìm thấy hai nút thì xong.
5. Nếu nó báo **không tìm thấy**, kéo tay hai Button vào ô **`nutVI`** và **`nutEN`**.
6. **Đổi tên hai object chữ trên chip cho kết thúc bằng `[NoLoc]`** - ví dụ `Txt_VI [NoLoc]` và `Txt_EN [NoLoc]`. Nếu không làm bước này, bộ dịch chạy nền sẽ dịch mất chính nhãn của nút chọn ngôn ngữ, và người chơi không còn biết nút nào là nút nào.

**c. Kéo 3 prefab popup tàu vào `TrainStationBuilding`**

Trong scene, chọn object ga tàu có component **`TrainStationBuilding`**, kéo ba prefab từ `Assets/Export_Train_UI_Package/Prefabs/` vào ba ô mới:

| Ô trong Inspector | Prefab |
|---|---|
| `Prefab Master Popup` | `Popup_Train_MasterStation.prefab` |
| `Prefab Item Popup` | `Popup_item_Train.prefab` |
| `Prefab Process Popup` | `Popup_train.prefab` |

**Chưa làm bước này thì popup tàu vẫn không hiện trong bản build.** Code đã sửa xong phần của nó, nhưng nó cần tham chiếu prefab mà chỉ Inspector mới gán được.

**d. Đổi định dạng nén texture Android sang ASTC**

> **Edit ▸ Project Settings ▸ Player ▸ Android ▸ Texture compression format → đổi từ ETC1 sang ASTC**

**ETC1 không có kênh alpha.** Với một game 2D sống bằng sprite trong suốt, đây là lỗi nặng nhất trong danh sách. iOS đã đúng, chỉ Android sai. Công cụ **không** tự sửa được vì `ProjectSettings.asset` không có API an toàn để ghi, sửa bằng script là cách nhanh nhất làm hỏng project. Đổi tay mất 10 giây.

Tiện thể mở luôn: **Project Settings ▸ Quality ▸ (tier Android) ▸ Anisotropic Textures → Per Texture**. Đang là Disabled, mà Disabled thì `anisoLevel` công cụ đặt trên từng texture bị bỏ qua hoàn toàn.

**e. Chạy công cụ khôi phục texture**

> `Tools ▸ Farm Game ▸ Restore Texture Quality`

1. Bấm **Scan (dry run)** trước. Không có gì bị sửa.
2. **Đọc báo cáo** trong cửa sổ và trong Console, gồm cả phần thống kê PPU.
3. Bấm **Apply**, **từng thư mục một**, không chạy cả `Assets/`.
4. Build thử lên máy thật, xem Profiler ▸ Memory, rồi mới chạy thư mục tiếp theo.

**f. Thêm lô đất phía Bắc**

> `Tools ▸ Farm Game ▸ Land Region Author`

1. Mở scene bản đồ chính (scene có `LandExpansionManager`).
2. Bật "kéo hộp trong Scene view", kéo hộp quanh mảnh đất muốn bán, đặt giá, bấm tạo asset `North_xx`.
3. Bấm nút **"Gắn vào scene (LandExpansionManager.regions)"**.
4. **Tự bấm Ctrl+S lưu scene.** Công cụ **không bao giờ tự lưu scene** - đây là chủ ý, để Sếp còn kiểm tra trước.

**g. Kiểm thử**

1. Vào Cài đặt, **đổi ngôn ngữ cả hai chiều** (Việt → Anh → Việt). Kiểm tra không có chữ nào tràn khung, và khi về tiếng Việt thì cỡ chữ trở lại như cũ.
2. **Kéo map** và xác nhận cảm giác 1:1, map dính tay, không nảy.
3. **Mở lần lượt mọi popup khi đang ở tiếng Anh** và nhìn xem có chữ nào bị cắt cụt hoặc đè khung không.

---

## 9. Việc còn lại và rủi ro đã biết

Em nói thẳng, không giấu:

1. **Chưa có gì được compile-test.** Workspace trên cloud không có toolchain Unity, nên không build thử được. **Lần domain reload đầu tiên khi Sếp mở Unity chính là bài kiểm tra thật.** Đó là lý do bước 8a bắt kiểm tra Console trước khi làm bất cứ việc gì khác.
2. **`device_bash` trên máy Sếp đang hỏng** do bản cập nhật Windows ngày 8 tháng 9. Toàn bộ công việc vòng này phải đi qua đường staging file thay vì chạy lệnh trực tiếp. Chậm hơn nhưng kết quả không đổi.
3. **`LandRegionSignBoard.cs:305` vẫn còn nguyên lỗi cùng loại với mục 2.2**: nó so chuỗi bằng `StartsWith("Mở ở cấp")`. Khi chạy tiếng Anh thì so không bao giờ khớp. Nằm ngoài phạm vi vòng này nên em chưa đụng, nhưng Sếp cần biết nó còn đó.
4. **`UnlockSlotUI`** cũng vậy: các phép so `CUM_MO_BAN_SHOP` / `CUM_TIEN_TO_MO_KHOA` là **cùng một loại lỗi**. So chuỗi hiển thị thay vì so trạng thái.
5. **137 chuỗi ghép kiểu `$"..."` vẫn không dịch được, theo thiết kế.** Chuỗi ghép lúc chạy thì không có key cố định để tra bảng. `loc_missing.txt` sẽ liệt kê hết ra cho Sếp thấy chúng là những câu nào, rồi tính sau.
6. **21 class ScriptableObject giữ tên hiển thị trong file `.asset`**, code không localize được vì dữ liệu nằm trong asset chứ không nằm trong code. Những cái chính: `BaseItemData.itemName`, `TutorialStepData.npcText`, `DishData`, `LandRegionData.displayName`, `FishData.displayName`. Muốn dịch thì phải đổi cấu trúc dữ liệu hoặc nhập bằng tay trong Inspector.
7. **6 chỗ `PlayerPrefs.SetString(key, num.ToString())` không có `InvariantCulture`.** Lỗi có sẵn từ trước. Trên máy có locale dùng dấu phẩy làm dấu thập phân, số ghi ra sẽ đọc lại sai. Chưa gây sự cố vì hiện chạy locale Việt, nhưng là bom hẹn giờ khi phát hành quốc tế.
8. **`TrainStationBuilding.cs` khai báo 2 MonoBehaviour trong cùng một file**, vi phạm chính quy tắc Sếp đặt ra. Lỗi có sẵn từ trước, em không sửa vì tách file ra sẽ làm mất tham chiếu script trong scene và prefab.

---

## 10. Rollback

Sếp đã push lên git trước khi em bắt đầu, nên:

```
git checkout -- Assets/
```

là hoàn tác toàn bộ 37 file, về đúng trạng thái trước vòng 1.

Riêng phần texture có đường lùi riêng: công cụ `Restore Texture Quality` **chép toàn bộ `.meta` bị ảnh hưởng ra `_MetaBackup_<timestamp>/` kèm `_manifest.txt` trước khi ghi**, và trong cùng cửa sổ có mục **"5. Restore from backup"** để phục hồi lại. Nên kể cả khi đã Apply rồi thấy không ưng, vẫn quay về được mà không cần đụng tới git.
