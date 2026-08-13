# THAY ÁO 3 POPUP: KHO VẬT PHẨM · HỒ SƠ AVATAR · SHOP — HỒ SƠ ĐỘI

Nguồn: `Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/`

---

## 1 · PHÂN TÍCH THƯ MỤC — THỨ TỰ ƯU TIÊN

README của nhà thiết kế chia rõ hai hạng:

| File | Loại | Ưu tiên |
|---|---|---|
| `ShopPopup/` | **Bản tương tác đầy đủ + README spec riêng** map thẳng vào `ShopManager`/`ShopItemUI` | **LÀM TRƯỚC** — được nhắc chi tiết nhất |
| `KhoVatPham_B.html` | Mock tĩnh (phương án B: tab danh mục + panel chi tiết phải) | làm cùng đợt, theo yêu cầu |
| `HoSoAvatar_A.html` | Mock tĩnh (phương án A: 2 cột avatar trái / thông tin phải) | làm cùng đợt, theo yêu cầu |
| `BangTinCho_A.html` | Mock tĩnh — bảng tin chợ | **ĐỂ SAU** (không nằm trong 3 mục yêu cầu) |
| `QuayHang_C.html` | Mock tĩnh — quầy hàng | **ĐỂ SAU** |

Chú ý dòng quan trọng trong README: *"Các file `*_A/B/C.html` là **mock tĩnh** (dùng để dev
dựng prefab theo đúng màu/kích thước)"* — tức Kho và Hồ sơ chỉ cần đúng **màu + chất liệu**,
không bắt buộc đổi bố cục.

### Ngôn ngữ thị giác dùng chung (trích README)

Trùng khớp 100% với bộ token `TaskPopupDesign` đã có từ popup Nhiệm vụ — cùng nhà thiết kế:
ván gỗ `#A9743C→#7C4E22` viền `#4A2508` · giấy `#FDF3DA→#FBECCB` viền `#6E4014` ·
thẻ `#FFFDF4→#FDF6E3` viền `#ECD09C` · nút 5 loại (xanh lá / vàng / **xanh dương kim cương** /
**đỏ huỷ** / xám khoá) · tab lún 14px · chấm đỏ pulse.

Hai màu MỚI so với bộ cũ (đã thêm vào `SkinKit`): nút kim cương `#7CC9F0→#3486C2` và
nút huỷ `#E8A19A→#C9645C`.

---

## 2 · HIỆN TRẠNG 3 CHỨC NĂNG — VÌ SAO KHÔNG DÙNG CÁCH CŨ

| Chức năng | Script | Kiểu UI |
|---|---|---|
| Kho vật phẩm | `WarehousePopupUI` (1.306 dòng, **40 SerializeField**) | dựng sẵn trong scene |
| Hồ sơ avatar | `AvatarProfilePopupUI` (874 dòng, 15 SerializeField) | dựng sẵn trong scene |
| Shop | `ShopManager` + `ShopItemUI` + prefab `KhungHatGiong` | scene + prefab |

Khác hẳn popup Nhiệm vụ (tự dựng bằng code lúc chạy). Ba UI này có hàng chục tham chiếu
serialize trỏ chéo nhau — **đập dựng lại là đứt hết**, đúng loại tai nạn đã gặp một lần.

**Nên chọn cách "thay áo tại chỗ":** giữ nguyên từng GameObject, chỉ đổi sprite/màu của
Image đang có và gắn thêm lớp trang trí (cạnh 3D, gloss, viền) làm con mới tên `Skin_*`.
Logic, dữ liệu, onClick: không sờ một dòng.

---

## 3 · SẢN PHẨM CỦA ĐỘI

### DEV-A — `SkinKit.cs` (bộ đồ nghề chung)

- `BoGoc(r)` / `DaiGradient()` — bản sprite đã kiểm chứng từ popup Nhiệm vụ (tránh lại
  bẫy gradient-Sliced làm trắng cả tấm)
- `MacAoNut(button, kieu)` — nút 3D: nền gradient + viền + cạnh dưới dày + chữ trắng bóng.
  **Không đổi kích thước, vị trí, onClick**
- `MacAoGiay` / `MacAoVanGo` / `MacAoThe` — ba chất liệu bề mặt theo README
- Chạy lại không nhân đôi lớp trang trí (kiểm tên `Skin_` trước khi tạo)

### DEV-B — `PopupSkinApplier.cs` + `PopupSkinTool.cs`

- Applier gắn lên root, giữ 4 danh sách **duyệt được trong Inspector** (gạch bỏ từng dòng)
- Nút **phân loại theo màu hiện tại** để giữ đúng ý nghĩa cũ: xanh→Mua/Nhận, vàng→phụ,
  xanh dương→kim cương, đỏ→huỷ, xám→khoá
- Tool tìm popup **bằng component** (`WarehousePopupUI`…) chứ không theo tên object —
  ai đổi tên cũng không hỏng
- Phân loại bề mặt theo diện tích; **nhận diện icon/art thật và BỎ QUA** (sprite có tên +
  preserveAspect) — áo mới không được đè lên hình vẽ tay
- Chỉ đổi bề mặt **lúc Play**; Editor chỉ gắn component + điền danh sách → gỡ component
  là về nguyên trạng 100%

### TESTER — kiểm tĩnh

| Bài | Kết quả |
|---|---|
| Cân ngoặc 3 file | OK |
| 3 kiểu tham chiếu (`WarehousePopupUI`, `AvatarProfilePopupUI`, `ShopManager`) tồn tại, là MonoBehaviour | OK |
| `KieuNut` public, constructor 5 tham số khớp | OK |
| Không nhân đôi lớp `Skin_*` khi áp lại | OK (kiểm tên trước khi tạo) |

---

## 4 · BẠN CHẠY

```
1. Tools ▸ Farm ▸ Thay Áo Popup ▸ 1 · Gắn + phân loại cho CẢ BA popup
2. Đọc 3 báo cáo trong Console (cái gì thành ván gỗ / giấy / thẻ / nút)
3. Mở Inspector từng root → gạch bỏ dòng nào không muốn thay
4. Ctrl+S → Play → mở lần lượt Kho / Hồ sơ / Shop
5. Popup nào không ưng: bỏ tick "Bật Áo" (giữ nguyên áo cũ) — hoặc menu 9 gỡ hết
```

## 5 · GIỚI HẠN NÓI TRƯỚC

- Đây là **lớp áo màu + chất liệu**, chưa phải bố cục mới của mock B/A (tab danh mục dọc,
  2 cột hồ sơ). Đổi bố cục nghĩa là dời hàng chục object có tham chiếu — làm sau, từng
  popup một, khi bạn duyệt áo màu này đã ổn.
- Thớ ván + đinh sắt + ribbon đuôi đỏ chưa vẽ lên 3 popup này (cần biết chỗ trống thật
  trên từng popup sau khi bạn Play nhìn) — thêm được ngay khi có ảnh chụp.
- Font Baloo 2: việc chung toàn dự án, vẫn treo.
- `BangTinCho_A` và `QuayHang_C`: đúng yêu cầu, **chưa đụng**.
