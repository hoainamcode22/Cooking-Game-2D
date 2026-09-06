# BÁO CÁO VÒNG 7 - 06/09/2026 — CHỮA GỐC POPUP TÀU

> Sếp duyệt cho đụng `.prefab` (nằm trong danh sách dừng). Lead thực hiện, **Sếp không phải làm gì cả**.
> Backup: `production/backup_vong7_2026-09-06/` (3 file, gồm cả `.prefab` và `.prefab.meta`).

---

## 1. TRẢ LỜI CÂU HỎI CỦA SẾP: "4 component đó đâu ra?"

**Sếp không thêm. Cũng không phải dev nào gắn sai component.**

Thủ phạm: `Assets/Export_Train_UI_Package/Editor/TrainPackageBuildTool.cs` **dòng 376**
```csharp
var slotUI = GetOrAddComponent<StationWagonSlotUI>(wGo);
```
Tool dựng prefab tàu, gắn script ô toa vào từng toa. **Ý định đúng 100%.**

**Vì sao ý đúng mà kết quả sai — giới hạn của Unity:**
Một file `.cs` chỉ sinh ra **ĐÚNG MỘT script asset**, ứng với class trùng tên file. Class MonoBehaviour
thứ hai nằm chung file thì **không có asset riêng nào**. `StationWagonSlotUI` nằm chung file với
`TrainStationMasterPopupUI.cs` ⇒ không có asset ⇒ khi tool `AddComponent<StationWagonSlotUI>()`,
Unity không có tham chiếu đúng để ghi nên ghi cái duy nhất nó có: `fileID 11500000` = class chính.

Bằng chứng trong prefab (bản gốc): cả 5 khối đều `fileID: 11500000` nhưng dòng ghi chú khác nhau:
```
dòng  278  fileID 11500000  ->  m_EditorClassIdentifier: ...StationWagonSlotUI        (Wagon_1)
dòng  676  fileID 11500000  ->  m_EditorClassIdentifier: ...StationWagonSlotUI        (Wagon_2)
dòng 1918  fileID 11500000  ->  m_EditorClassIdentifier: ...TrainStationMasterPopupUI (gốc, ĐÚNG)
dòng 2948  fileID 11500000  ->  m_EditorClassIdentifier: ...StationWagonSlotUI        (Wagon_3)
dòng 4025  fileID 11500000  ->  m_EditorClassIdentifier: ...StationWagonSlotUI        (Wagon_4)
```
`m_EditorClassIdentifier` là Unity tự ghi "cái tôi đáng lẽ phải là". Bốn toa khai mình là ô toa nhưng
con trỏ script chỉ về popup ⇒ chúng chạy code popup ⇒ mỗi cái tự dựng một popup.

⇒ **Bất kỳ ai chạy tool đó, người thật hay agent, cũng ra kết quả y hệt.** Tool không hỏng.

---

## 2. ⚠️ ĐÍNH CHÍNH DEV H (vòng 6): POPUP **CÓ** TRONG SCENE

Vòng 6 Dev H kết luận "popup KHÔNG nằm trong scene, đếm GUID script = 0" và Lead đã nói với Sếp
"không cần kéo popup nữa". **Cả hai đều SAI.**

Lỗi phương pháp: Dev H grep **guid của SCRIPT** trong scene. Nhưng **prefab instance KHÔNG ghi component
ra file scene** (component được kế thừa từ prefab), nên grep guid script luôn ra 0. Đây là **âm tính giả**.

Đếm lại bằng **guid của PREFAB** (`c4c6499270a0dd140b6ae1100658b2d6`):
- `m_SourcePrefab` = **1** ⇒ có đúng 1 instance trong `SCN_Farm.unity`
- Khối `PrefabInstance &4105157295546141520`, `m_TransformParent: {fileID: 1561892010}` = RectTransform
  của `Popup_LevelUp_Township`, `m_IsActive: 0`

⇒ **Dev A vòng 3 ĐÚNG.** Popup nằm dưới `Popup_LevelUp_Township` đang tắt, đúng như vòng 3 đã nói.
Bản vá bật lại tổ tiên của vòng 3 **là cần thiết**, không phải no-op.

> **BÀI HỌC PHƯƠNG PHÁP (ghi vào MEMORY):** muốn biết một prefab có trong scene hay không thì phải đếm
> **guid của PREFAB** (`m_SourcePrefab`), KHÔNG được đếm guid của script. Đếm guid script trong scene
> luôn cho âm tính giả với mọi prefab instance.

---

## 3. ĐÃ LÀM (Lead tự tay, Sếp không phải làm gì)

### 3a. Tách file, chữa tận gốc
Tạo mới `Assets/Export_Train_UI_Package/Scripts/StationWagonSlotUI.cs`, chuyển nguyên class
`StationWagonSlotUI` (dòng 963-1317 của file cũ) sang, cùng namespace `ExportTrainUIPackage`,
kèm khối chú thích giải thích vì sao phải ở file riêng để người sau không gộp lại.

Kết quả kiểm:
| File | Dòng | Class | Ngoặc | `#if`/`#endif` | Line-ending |
|---|---|---|---|---|---|
| `TrainStationMasterPopupUI.cs` | 962 | 1 (`TrainStationMasterPopupUI`) | 92/92 · 468/468 | 1/1 | LF (nguyên) |
| `StationWagonSlotUI.cs` (mới) | 372 | 1 (`StationWagonSlotUI`) | 35/35 · 188/188 | 0/0 | LF |

Từ nay `StationWagonSlotUI` có script asset riêng ⇒ tool gắn đúng ⇒ **bug không tái phát được nữa**.
Đây đúng là luật `memory/MEMORY.md` đã dặn: **mỗi file một chủ**.

### 3b. Mổ prefab, xoá 4 component ma
Quy trình 5 bước kiểm chứng, không sửa tay:
1. Dry-run: xác định 4 khối `!u!114` có guid script + `m_EditorClassIdentifier` chứa `StationWagonSlotUI`.
2. **Kiểm tham chiếu trước khi xoá:** mỗi fileID chỉ xuất hiện **1 lần** trong prefab (chính dòng
   `m_Component` của toa), không chỗ nào khác trỏ tới. Và **0 lần** trong `SCN_Farm.unity`
   ⇒ xoá không để lại tham chiếu mồ côi ở đâu cả.
3. Xoá dòng `- component: {fileID: X}` khỏi `m_Component` của từng toa.
4. Xoá nguyên khối component.
5. **Kiểm sau khi xoá:** 243 block → 239 block (đúng chênh 4); mọi `m_Component` còn lại đều trỏ tới
   block có thật (`m_Component tro vao hu vo: []`); line-ending giữ nguyên.

Trạng thái prefab sau khi mổ:
```
Con lai 1 MasterPopupUI:  fid=8845219234218148582  object=Popup_Train_MasterStation   ĐÚNG
Wagon_1   2 component: RectTransform, Button
Wagon_2   2 component: RectTransform, Button
Wagon_3   2 component: RectTransform, Button
Wagon_4   2 component: RectTransform, Button
```

**Vì sao xoá mà toa không mất script ô toa:** `TrainStationMasterPopupUI.cs` dòng 420 có
```csharp
wagonSlots[i] = wTr.GetComponent<StationWagonSlotUI>() ?? wTr.gameObject.AddComponent<StationWagonSlotUI>();
wagonSlots[i].BuildWagonHierarchy();
```
Lúc chạy, popup tự gắn lại script ô toa và tự dựng hình cho từng toa. **Không cần chạy lại
`TrainPackageBuildTool`.** (Chạy lại cũng được, giờ nó sẽ gắn đúng class.)

---

## 4. 🧑 SẾP CHỈ CẦN LÀM 1 VIỆC

**Unity đang mở** (có `Temp/UnityLockfile`). Lead đã sửa file trên đĩa, nên:

1. **Đóng Unity.** Nếu nó hỏi lưu `Popup_Train_MasterStation` thì chọn **Don't Save / Discard**
   (bản trên đĩa mới là bản đúng, đừng để Unity ghi bản cũ trong RAM đè lên).
2. **Mở lại Unity.** Nó sẽ tự import `StationWagonSlotUI.cs` (sinh `.meta` mới) và tự đọc lại prefab.
3. Chờ compile, Console 0 lỗi đỏ, rồi Play và click ga tàu.
   Console lọc `[Train]`, dòng `soBanMasterPopupUI=` phải **= 1**. Popup hiện đúng **một** khung.

Không phải xoá component nào, không phải kéo object nào, không phải chạy tool nào.

**Việc kéo `Popup_Train_MasterStation` sang `Canvas_Popup`: KHÔNG bắt buộc.** Bản vá vòng 3 tự bật lại
tổ tiên nên popup mở được ngay ở vị trí hiện tại. Kéo chỉ là dọn cho gọn, làm sau cũng được.

---

## 5. HOÀN TÁC

Chép ngược 3 file từ `production/backup_vong7_2026-09-06/` rồi xoá `StationWagonSlotUI.cs`:

| File | md5 TRƯỚC (backup) | md5 SAU |
|---|---|---|
| `Popup_Train_MasterStation.prefab` | `41ab13ea7bb693d0686966dc0a6cb611` | `b2fca134e87396625a0f96437c52b405` |
| `Popup_Train_MasterStation.prefab.meta` | `27c55fdd7fad655fc09d089758ec0c41` | không đụng |
| `TrainStationMasterPopupUI.cs` | `ae04c4d3f502ee39841c88ace20f3a95` | `772b9e7ab26b...` |
| `StationWagonSlotUI.cs` | (file mới) | `0d5e9295259b...` |

## 6. NỢ KỸ THUẬT: ĐÃ ĐÓNG 1 MỤC
Mục 1 của vòng 6 ("tách `StationWagonSlotUI` ra file riêng") **ĐÃ XONG**.
Mục 2 ("`EnsurePopupsExist` trong `#if UNITY_EDITOR` nên bản build không có popup tàu") **VẪN CÒN**,
nhưng nay đã biết scene CÓ 1 instance sẵn ⇒ nhẹ hơn tưởng, cần Sếp xác nhận 2 popup phụ
(`Popup_train`, `Popup_item_Train`) có trong scene hay không trước khi quyết cách xử lý.
