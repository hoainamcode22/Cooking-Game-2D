# NGHIỆM THU CẮT KHUNG — VÒNG 16 · 2026-09-04

Sếp chốt: **"chỉ cần cắt nó đều sạch khung, không cần vẽ lại"**
→ KHÔNG gửi lại đội vẽ. Lead tự cắt. Bản gốc giữ nguyên, không đụng vào.

---

## 1. Quét sạch toàn bộ 24 file gói MASTER

Đếm **mảng liền nhau (connected component)** của kênh alpha — nếu có mảnh
frame khác dính vào thì sẽ ra ≥ 2 mảng.

| Gói | Số file | Kết quả |
|---|---|---|
| A — Cờ ngôn ngữ | 2 | **1 mảng / file — SẠCH** |
| B — Icon gia vị | 3 | **1 mảng / file — SẠCH** |
| C — Nhân vật lên cấp | 2 | **1 mảng / file — SẠCH** |
| D — NPC 12 khẩu hình | 12 | **1 mảng / file — SẠCH** |
| E — Guide Board | 5 | **1 mảng / file — SẠCH** |

→ **24/24 sạch, không còn mảnh dính frame khác.** Vấn đề Sếp thấy ở đợt trước
đã hết ở gói MASTER này.

---

## 2. Gói D — NPC 12 khẩu hình: CẮT ĐỀU

**Trước:** canvas `512 × 640`, nội dung chỉ nằm ở `(93, 52) → (401, 640)`
→ **thừa 45% khung trong suốt** (93px trống trái, 111px phải, 52px trên).

**Đo cả 12 frame:** bbox **giống hệt nhau** `(93, 52, 401, 640)`, mỗi frame
đúng `146.862` pixel đặc — nghĩa là dáng người trùng khít 100%, chỉ khác miệng.

**Sau khi cắt:** dùng **một khung chung duy nhất** cho cả 12 frame
→ `308 × 588`, nội dung sát 4 mép (thừa 0%), **không rung frame** khi chạy.

```
production/art-handoff/2026-09-04_MASTER/D_NPC_CatDeu/   ← BẢN DÙNG
production/art-handoff/2026-09-04_MASTER/D_NPC_Mieng_SuaLai/  ← gốc, giữ nguyên
```

`308` và `588` đều chia hết cho 4 → an toàn cho nén texture của Unity.

**Không phải sửa toạ độ trong scene.** `NPC_Guide` đã bật `preserveAspect`,
nên khi thay ảnh sát khung, NPC tự **to lên ~9%** và **đứng gần như đúng chỗ cũ**:

| | Bản 512×640 | Bản 308×588 |
|---|---|---|
| Nội dung hiện ra | 180 × 344 px | **196 × 375 px** |
| Vị trí ngang (canvas 1920) | 474 → 655 | **472 → 668** |

Layout đã đo ở vòng 15 (hở hàng nút 11px trái, hở card 10px phải) vẫn giữ nguyên.

### Về "mất động tác tay"
Đã xem lại ở cỡ thật: đây **không phải lỗi**. Bộ art này là **chân dung bán thân**
(đầu + vai + ngực), cắt ngang ngực — trong khung vốn **không có bàn tay** để mất.
Đúng như Sếp nói: không cần vẽ lại.

---

## 3. Gói B — 3 icon gia vị: CẮT ĐỀU (làm thêm)

Ba icon này thừa **45–66%** khung trong suốt → hiện ra trong ô sẽ bé tí.

| File | Nội dung thật | Thừa |
|---|---|---|
| `ing_nuoc_tuong.png` | 105 × 215 | 66% |
| `ing_nuoc_mam.png` | 145 × 210 | 54% |
| `ing_rau.png` | 177 × 205 | 45% |

Đã cắt sát rồi **đặt lại vào khung vuông chung `232 × 232`, căn giữa** — cách này
bỏ được phần trống mà **vẫn giữ đúng tỉ lệ to/nhỏ giữa 3 icon với nhau**
(cắt sát riêng từng cái sẽ làm chai nước tương to bằng bó rau).

```
production/art-handoff/2026-09-04_MASTER/B_Icon_CatDeu/   ← BẢN DÙNG
production/art-handoff/2026-09-04_MASTER/B_Icon_GiaVi/    ← gốc, giữ nguyên
```

---

## 4. Không cắt (có lý do)

| File | Thừa | Vì sao giữ nguyên |
|---|---|---|
| `tut_board_frame.png` | 1% | **9-slice** border `{72,72,72,72}` — cắt là hỏng bo góc |
| `tut_board_ribbon.png` | 33% | **9-slice** border `{60,0,60,0}` — cắt là hỏng dải ruy băng |
| `tut_slot_illustration.png` | 4% | 9-slice `{40,40,40,40}` |
| `tut_step_dot_on/off.png` | 23% | Chấm tròn 48×48, lề đều 4 phía — cắt không lợi gì |
| `char_03/04_master.png` | 12% | Lề đối xứng 16px, dùng cho popup lên cấp — an toàn |
| `flag_vn/en.png` | 0% | Đã sát khung sẵn |

---

## 5. Tool đã trỏ sang bản đã cắt

| Tool | Sửa gì |
|---|---|
| `TutorialV2SetupTool.cs` | `HANDOFF_NPC_DS` thêm `D_NPC_CatDeu` lên **đầu** danh sách ưu tiên |
| `MasterArtImportTool.cs` | 3 dòng gói B trỏ sang `B_Icon_CatDeu` |

Bản gốc vẫn nằm dưới trong danh sách ưu tiên → nếu bản cắt có vấn đề, xoá thư mục
`D_NPC_CatDeu` là tool tự quay về bản gốc.
