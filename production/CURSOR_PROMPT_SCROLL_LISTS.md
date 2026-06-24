# PROMPT DÁN VÀO CURSOR — Cho danh sách Nhiệm vụ / Thành tựu cuộn được + list dài

Mục tiêu: 3 tab (Nhiệm vụ, Hằng ngày, Thành tựu) trong popup gộp **cuộn được** để hiện đầy đủ mọi mục
khi user lướt xuống; và tab Nhiệm vụ hiện thêm các nhiệm vụ CHƯA tới cấp ở trạng thái KHOÁ để list dài.

## ĐỌC TRƯỚC
`Assets/_Game/Scripts/Mission/UnifiedTaskPopupUI.cs` (cách dựng Panel_Mission/Panel_Daily/Panel_Achievement
+ spawn item theo MissionData), `PopupEwarManager.cs` (contentTransform + filter `requiredLevel > playerLevel`),
`MissionItemUI.cs` (item + trạng thái lock/claim).

## LUẬT
Chỉ sửa/thêm UI, KHÔNG phá logic mission/claim. Không đổi chữ ký public đang dùng. 0 lỗi đỏ. Không commit.

## TASK 1 — Làm mỗi danh sách CUỘN ĐƯỢC (ScrollRect)
Áp cho cả 3 panel (Mission, Daily, Achievement):
- Cấu trúc: `Panel` → `ScrollRect` → `Viewport` (gắn `RectMask2D`) → `Content`. Spawn item vào **Content**.
- `Content`: gắn `VerticalLayoutGroup` (spacing ~12, padding top/bottom ~8, childForceExpandHeight=false,
  childControlHeight=true, childAlignment=UpperCenter) + `ContentSizeFitter` (verticalFit = PreferredSize).
- Mỗi item (prefab MissionItemUI): gắn `LayoutElement` với `preferredHeight` = chiều cao 1 dòng (vd ~110).
- `ScrollRect`: horizontal=false, vertical=true, movementType=Elastic, gán `viewport` + `content`; (tuỳ chọn) thêm Scrollbar dọc.
- Nếu UI dựng bằng CODE trong `UnifiedTaskPopupUI` → tạo ScrollRect/Viewport/Content khi build từng panel,
  rồi spawn item vào Content thay vì panel phẳng. Nếu `PopupEwarManager` dùng prefab có `contentTransform` →
  biến `contentTransform` thành `Content` nằm trong ScrollRect (thêm Viewport+ScrollRect bọc ngoài).
- **Quan trọng:** giữ panel có chiều cao cố định (vùng nhìn), CHỈ Content giãn theo số item → mới cuộn được.

## TASK 2 — Tab Nhiệm vụ: hiện thêm nhiệm vụ KHOÁ cho list dài
- Trong chỗ đổ nhiệm vụ chính (PopupEwarManager / UnifiedTaskPopupUI): thay vì BỎ QUA
  `requiredLevel > playerLevel`, hãy VẪN spawn chúng ở **trạng thái khoá** (nền xám + nút đổi thành
  "Mở ở cấp X", không bấm được) — giống cách tab Thành tựu đang hiện "Khóa".
- Sắp xếp thứ tự: (1) đang làm/chưa xong (mở) → (2) đã hoàn thành chờ nhận → (3) khoá theo cấp tăng dần.
- Daily giữ nguyên (3 việc/ngày). Thành tựu giữ nguyên logic (đã hiện khoá) — chỉ cần cuộn ở TASK 1.
- `MissionItemUI`: thêm 1 state hiển thị "khoá theo cấp" nếu chưa có (tái dùng style khoá của Thành tựu).

## TASK 3 (bonus) — Sửa lỗi font ★ trong Console
Console báo: ký tự ★ (★) không có trong `LiberationSans SDF` → bị thay bằng □ ở `Txt_IconPlaceholder`.
Chọn 1 cách: (a) thay ký tự ★ bằng 1 sprite icon ngôi sao; (b) thêm glyph ★ vào TMP Font Asset (Window →
TextMeshPro → Font Asset Creator / Add Glyph); hoặc (c) đổi text đó sang font có ★. (Icon thưởng x50/x5/x10
đang là ô □ vì chưa gán sprite — phần đó tôi tự gán trong Inspector.)

## VERIFY
Vào Play → mở popup → mỗi tab kéo xuống thấy thêm mục (cuộn mượt); tab Nhiệm vụ hiện cả nhiệm vụ khoá
"Mở ở cấp X"; tab Thành tựu cuộn hết ~16 mục; 0 lỗi đỏ; không còn cảnh báo ★ ở Txt_IconPlaceholder.
