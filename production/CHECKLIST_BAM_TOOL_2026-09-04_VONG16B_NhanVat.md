# VÒNG 16B — 4 nhân vật popup Lên Cấp · 2026-09-04

## Sếp chốt: KHÔNG đặt đội vẽ. Lấy sẵn từ bộ khách du lịch.

Nguồn: `Assets/NV_NPC/NVGAME/` — 11 khách du lịch, cùng bộ với 11 prefab
`Assets/_Game/Farm/Prefabs/Tourists/Tourist_NV01..NV11`.
Ảnh gốc **1664 × 2562** (spritesheet 4 hướng × 3 frame) → thừa sức cắt bán thân sắc nét.

### 4 slot sau khi chốt

| Slot | Nhân vật | Nguồn | Trạng thái |
|---|---|---|---|
| 1 | `char_01` — ông thám hiểm râu, mũ be | NV01 | đã có sẵn |
| 2 | `char_03` — cô mũ pith tóc xoăn, máy ảnh | NV03 | đã có sẵn |
| 3 | `char_05` — **cô cầm kính lúp** | NV09 | **Lead vừa cắt** |
| 4 | `char_06` — **ông lão kính tròn áo xanh** | NV06 | **Lead vừa cắt** |

**Bỏ ra:**
- `char_02` — mũ đầu bếp **VỠ răng cưa** ở toàn bộ f02–f12 (f01 thì nguyên vẹn).
- `char_04` — nhìn gần trùng `char_03`. File vẫn còn nguyên, muốn dùng lại chỉ đổi 1 dòng
  trong `BANG_NHAN_VAT` của `LevelUpPopupRewireTool.cs`.

### Cách cắt (để sau này lặp lại được)

1. Tách nền trắng bằng **flood fill từ 4 biên** — không dùng ngưỡng màu, nên
   **không làm thủng vùng trắng bên trong** nhân vật (áo trắng, kính lúp).
2. Lấy **ô giữa của hàng `down`** = tư thế đứng yên, quay mặt về người chơi.
3. Cắt **72% chiều cao** từ đỉnh xuống = đầu + vai + ngực, khớp tỉ lệ đầu/thân
   với `char_01` và `char_03` đang chạy.
4. Dọn mảnh vụn < 400px, làm mềm biên 0.6px, scale về cao 463px, đặt vào canvas
   **512 × 512** — đúng quy cách 2 bộ cũ.

Kết quả: `char_05` 397×463 · `char_06` 316×463, mỗi file **1 mảng liền nhau**, sạch.

### Chế độ hiển thị
`CelebrationCharacterSlot` chạy **PUPPET**: chỉ cần **1 hình**, code tự làm hiệu ứng
thở / nghiêng / nảy. Không cần 12 frame, không cần blink.

---

## VIỆC SẾP CẦN BẤM

1. Mở `Assets/_Game/Scenes/SCN_Farm.unity`
2. `Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (DRY-RUN)` → đọc Console
3. `Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (APPLY)`
4. **Ctrl + S**

Tool tự ép import setting Sprite cho 2 ảnh mới (ảnh copy tay vào Assets hay bị Unity
import nhầm thành Texture ⇒ slot hiện trống).

Nếu sai: `Ctrl + Z`, hoặc khôi phục từ `production/backup_round16_2026-09-04/`.

---

## Đã huỷ

`production/_huy_vong16/` — prompt đặt vẽ "chàng nông dân trẻ" và tool nạp art vòng 16.
Không cần nữa vì lấy được từ khách du lịch có sẵn.
