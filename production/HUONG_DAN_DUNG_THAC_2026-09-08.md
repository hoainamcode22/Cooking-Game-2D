# HƯỚNG DẪN — DỰNG NÚI + THÁC (vòng 14)

## Làm gì: mở Unity, bấm 1 nút

```
Menu Tools ▸ Farm ▸ Nui va Thac ▸ 1. Dung nui + thac
```

Xong thì **Ctrl+S** để lưu scene. Hết.

Không ưng thì:
```
Menu Tools ▸ Farm ▸ Nui va Thac ▸ 2. Xoa nui + thac
```
hoặc **Ctrl+Z**. Mọi thứ tool tạo ra đều nằm trong **một** GameObject tên `NUI_THAC_ROOT`
dưới `Grid_Iso45` — xoá nó là sạch, không sót gì.

**Trước khi bấm:** mở scene `SCN_Farm` (tool cần `Grid_Iso45`; không có thì nó báo và
không làm gì cả).

---

## Vì sao là tool chứ không phải em ghi sẵn vào scene

`SCN_Farm.unity` nặng **19 MB**. Dựng núi cần ghi khoảng **500 entry tilemap** + 7 GameObject
vào YAML bằng tay. Em không mở được Unity từ đây nên **không có cách nào kiểm chứng** file
còn hợp lệ trước khi Sếp mở — hỏng scene là hỏng cả buổi.

Chạy bằng Editor API thì Unity tự lo phần serialize, có Undo, và Sếp bấm lại bao nhiêu lần
cũng được. Backup scene vẫn có ở `production/backup_scene_vong14/SCN_Farm_before_thac.unity`.

---

## Tool dựng ra cái gì

| Đối tượng | Nội dung | sortingOrder |
|---|---|---|
| `VachNui_Tang0..3` | 4 tầng tường đá xếp chồng, mỗi tầng cao 110 world | 8, 9, 10, 11 |
| `VachNui_Dinh` | mặt đỉnh, dùng `RuleTile_IsoCliff45` để tự ra viền cỏ | 12 |
| `Suoi_TrenDinh` | dòng suối chảy trên đỉnh + ô mép tràn ở đầu thác | 13 |
| `Suoi_ChanNui` | hồ nước chân thác (3×3) + suối chảy ra 6 ô | 7 |
| `ThacNuoc` | SpriteRenderer + `SimpleSpriteAnimator`, 8 khung, 12 fps, **lặp vô hạn** | 14 |

Núi cao **440 world** (4 tầng × 110). Thác **175 × 700 world**, bắt đầu từ mép đỉnh và kết
thúc đúng chân vách.

Tool cũng tạo 3 Tile asset trong `Assets/_Game/Farm/Tiles/NuiThac/` (ô đá 30, 31, mép tràn 40,
hồ 43). Bấm lại thì dùng lại, không tạo trùng.

---

## Vị trí: ô (15,11) → (24,20)

Em chọn bằng cách quét cả bản đồ tìm khối 10×10 thoả đủ 4 điều kiện: toàn ô cỏ · không
đè công trình / plot / decor / hàng rào · có vành cỏ 2 ô bao quanh · nằm **phía sau** khu farm.

Cả bản đồ chỉ có **đúng 1 khối** thoả hết. Công trình gần nhất cách **26 ô**.

Muốn dời chỗ: mở `NuiThacBuilder.cs`, sửa `O_GOC_X` / `O_GOC_Y` ở đầu file rồi bấm Dựng lại.
Các số khác chỉnh được luôn:

| Hằng số | Đang là | Ý nghĩa |
|---|---|---|
| `O_GOC_X`, `O_GOC_Y` | 15, 11 | góc khối núi |
| `BE_RONG` | 10 | số ô mỗi cạnh |
| `SO_TANG_VACH` | 4 | núi cao mấy tầng |
| `CAO_MOI_TANG` | 110 | world unit mỗi tầng (đo từ art, đừng đổi) |
| `O_SPILL_X` | 20 | cột đặt thác |
| `THAC_CAO` | 700 | chiều cao thác (rộng = /4) |
| `ORDER_GOC` | 8 | sortingOrder tầng dưới cùng |

---

## Xem trước khi bấm
`production/_qc_preview_nui_thac.jpg` — em mô phỏng **đúng bộ số trên** bằng renderer riêng,
dựng lại từ dữ liệu tile thật. Bấm tool ra gần y như ảnh đó.

---

## ⚠️ 2 việc tool CHƯA làm

1. **Chưa chặn đặt công trình xuống nước.** Người chơi vẫn đặt được nhà giữa suối và giữa hồ.
   Sửa được nhưng phải đụng `PlacementManager.cs` — em chờ Sếp gật rồi mới động vào code.
2. **Chưa có tiếng nước và bụi nước.** Kéo tay được ngay:
   - `Day_Night/VFX/Water/VFX_CliffWater_Front.vfx` thả vào chân thác
   - `AudioSource` 3D + `Day_Night/Audio/Ambience/Water flowing.wav`, bán kính ~1200 world

## Ghi chú
Núi đặt ở `sortingOrder` 8–14, tức **vẽ đè lên toàn bộ lớp nền** (cỏ 1, đất 2, đá 5, hàng rào 6,
decor 7). Vì khu đó trống nên không va chạm gì. Nếu sau này Sếp đặt công trình **phía trước**
núi mà thấy núi đè lên, hạ `ORDER_GOC` xuống là xong.
