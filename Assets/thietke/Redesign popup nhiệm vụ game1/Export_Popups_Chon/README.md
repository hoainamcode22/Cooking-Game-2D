# Popup đã chốt — bộ giao diện đồng bộ (farm game)

Mở trực tiếp bằng trình duyệt (không cần cài gì). Khung thiết kế 1920×1080, popup nội dung 1220–1300px.

| File | Nội dung | Phương án |
|---|---|---|
| `ShopPopup/ShopPopup_standalone.html` | Cửa hàng (tương tác đầy đủ: tab, stepper, mua, toast) | mẫu khung 3a |
| `BangTinCho_A.html` | Bảng tin chợ | A — tab danh mục dọc trái |
| `QuayHang_C.html` | Quầy hàng | C — 2 cột: quầy trái + panel niêm yết phải |
| `KhoVatPham_B.html` | Kho vật phẩm | B — tab danh mục + panel chi tiết phải |
| `HoSoAvatar_A.html` | Hồ sơ người chơi | A — 2 cột: avatar trái, thông tin phải |

`ShopPopup/` còn có `README.md` riêng (spec chi tiết map vào `ShopManager` / `ShopItemUI`).

## Ngôn ngữ thị giác dùng chung (áp cho mọi popup)

**Khung ngoài (ván gỗ)**
- Viền 7–8px `#4A2508`, bo 38–42px, nền `linear-gradient(180deg,#A9743C,#8A5A2E 14%,#7C4E22)`
- Thớ ván ngang mỗi 158px `rgba(58,28,4,.32)`; bevel: `inset 0 6px 0 rgba(255,230,180,.28)`, `inset 0 -10px 0 rgba(0,0,0,.28)`
- 4 đinh sắt góc: tròn 22px, `radial-gradient(circle at 35% 30%,#FFE9B8,#B98745 55%,#7A4A1A)`, viền `#5A3210`

**Ribbon tiêu đề**
- Plate vàng `#FFD257 → #F0A32F`, viền 5px `#A35C14`, bo 22–24px
- 2 đuôi ribbon đỏ `#D8641F → #A84812` (clip-path notch)
- Chữ `#FFFBE9` outline nâu `#96540F`, cỡ 42–54px

**Panel giấy**
- `#FDF3DA → #FBECCB`, viền 4px `#6E4014`, viền trong `#F3DDB0`, bo 22–26px

**Thẻ / hàng nội dung**
- Nền `#FFFDF4 → #FDF6E3`, viền 3px `#ECD09C`, bo 18–24px, đổ cạnh `0 5px 0 rgba(190,140,70,.35)`
- Icon đặt trên **đĩa tròn kem**: `radial-gradient(circle,#FAF0D6,#F1DFB4 78%)` + `inset 0 3px 8px rgba(150,95,30,.22)`

**Nút (3D, bấm lún `translateY(4px)`)**
| Loại | Nền | Viền | Chữ |
|---|---|---|---|
| Chính / Mua / Nhận / Giao | `#A5E05E → #6CBF2E → #57A51F` | `#3F8A12` | `#FFF` |
| Vàng phụ (Làm mới, Nâng cấp) | `#FFD977 → #F2A636` | `#C07818` | `#7A4A10` |
| Kim cương | `#7CC9F0 → #4AA3DD → #3486C2` | `#2E6FA3` | `#FFF` |
| Huỷ / Gỡ bán | `#E8A19A → #C9645C` | `#A4453E` | `#FFF` |
| Vô hiệu / Khoá | `#B8AE95` | `#9C927C` | `#FFF` / `#8D8266` |
- Cạnh dưới `box-shadow: 0 5–6px 0 rgba(0,0,0,.28)`, gloss `inset 0 3px 0 rgba(255,255,255,.4)`

**Tab**
- Active: `#FFFBE9 → #FDF0D3`, chữ `#5B3417`, `margin-top:0` (nối liền giấy)
- Inactive: `#E2A75F → #C48538`, chữ `#6E4014`, lún `margin-top:14px`
- Viền 4px `#6E4014`, bo `22px 22px 0 0`, icon trong đĩa tròn trắng mờ

**Thanh tiến độ**
- Track `#E8D0A4` + `inset 0 3px 5px rgba(120,75,20,.35)`
- Fill `#A9E470 → #68BD2B`, gloss trắng nửa trên, chữ trắng outline nâu

**Trạng thái khoá**: card `grayscale(.35–.55)` + overlay `rgba(62,40,16,.5)` + ổ khoá + "Mở ở cấp X"
**Chấm đỏ claimable**: 22–24px, `radial-gradient(circle at 35% 30%,#FF8A6E,#EF4B33 60%,#C22C18)`, viền trắng 3px, pulse 1.2s

**Chữ**: Baloo 2 — 700/800. Title 42–54 · tab 26 · tên item/hàng 20–25 · nút 18–26 · chip 18–20 · phụ 15–17

## Assets
`assets/` lấy nguyên từ `Assets/Assetsgame` của project: btnX, btnV, AnhBtnNhanQua, Icon_vang, kimcuong, iconsao, iconlua, conga, cachua, bapcai, thitheo, iconmarrket, icongiay, iconlich, avata_player, hạt giống (`assets/shop/*`), khungchuong, chuongheo, chuongbo, coixoaygio, caythong, img_BaoThoc.

## Lưu ý
Các file `*_A.html` / `*_B.html` / `*_C.html` là **mock tĩnh** (dùng để dev dựng prefab theo đúng màu/kích thước). Riêng popup Cửa hàng là bản tương tác đầy đủ.
