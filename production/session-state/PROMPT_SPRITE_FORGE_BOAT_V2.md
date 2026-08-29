# PROMPT GỬI ĐỘI VẼ (GPT → agent-sprite-forge) — BỘ ASSET TÀU KHÁCH DU LỊCH V2

> Sếp copy nguyên file này gửi GPT. Ngày phát: 2026-08-29 · Hệ: BOAT-002 (khách du lịch lên bờ ăn món)
> Code đã chạy với sprite placeholder, gắn art vào là xong — KHÔNG cần sửa code.

---

## 🎨 LUẬT ART STUDIO — BẮT BUỘC ĐỌC TRƯỚC KHI VẼ

1. ❌ **TUYỆT ĐỐI KHÔNG TEXT**: không chữ, không số, không logo, không label, không biển hiệu chữ
   trên BẤT KỲ asset nào. Text do game render bằng TMP. Chỗ nào thiết kế có biển → vẽ BIỂN TRỐNG.
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ**: alpha trong suốt 100%, không drop-shadow, không nền trắng/mờ
   dưới chân object — bóng do game tự vẽ để đồng nhất theo giờ trong ngày.
3. ✅ **Meta Unity chuẩn**: spriteMode: 1 (Single) từng file · pivot Bottom-Center cho object đứng đất.
4. ✅ **Frame animation**: mọi frame cùng hướng = CÙNG kích thước canvas, thân đứng yên cùng vị trí;
   frame 01 = tư thế nghỉ; KHÔNG khói/hiệu ứng bake vào frame (code phun runtime).
5. ✅ **Style chuẩn**: theo bộ Export_Train_UI_Package (burgundy #8E1F3B + đồng vàng #D9A441,
   gỗ nâu ấm, outline nâu đậm cartoon, dễ thương cho phụ nữ & trẻ em).
6. ✅ Giao đúng TÊN FILE + THƯ MỤC được đặt trong prompt, không thêm file phụ (_single, @2x tự ý...).

---

## BỐI CẢNH (để đội vẽ hiểu mình đang vẽ cho cảnh gì)

Tàu du lịch cập bến biển của nông trại → **bắc một tấm ván gỗ từ mạn tàu xuống bờ** → 3–6 khách du lịch
đi xuống, theo đường đất tới xếp hàng trước nhà hàng → trên đầu mỗi khách nổi lên **bong bóng thoại
hình món ăn** họ muốn → người chơi nấu xong giao món → bong bóng đổi thành **mặt cười** rồi bay lên trời
→ khách vui vẻ quay về tàu → tàu rời bến. Bến 2 và 3 ban đầu **bị khóa**, người chơi bấm vào bảng khóa
để mua bằng vàng/kim cương.

Nhân vật khách du lịch ĐÃ CÓ (11 bộ, 4 hướng), **không cần vẽ lại**.

---

## HẠNG MỤC 1 — TẤM VÁN GỖ BẮC TỪ TÀU XUỐNG BỜ (gangplank) — ƯU TIÊN CAO NHẤT

**Thư mục giao:** `Assets/_Game/Farm/Art/TouristBoat/Gangplank/`

| Tên file | Nội dung |
|---|---|
| `gangplank_01.png` | Ván **rút hết** — chỉ còn mẩu ván gấp gọn nằm sát mạn tàu, chưa chạm bờ |
| `gangplank_02.png` | Ván **duỗi ra 1/3**, đầu ván còn lơ lửng trên mặt nước |
| `gangplank_03.png` | Ván **duỗi ra 2/3**, đầu ván gần chạm bờ |
| `gangplank_04.png` | Ván **bắc xong**, hai đầu tì chắc: một đầu trên mạn tàu, một đầu trên bờ cát |

**Kích thước canvas: 512 × 200 px cho CẢ 4 FILE** (bắt buộc bằng nhau, ván duỗi dài dần sang phải trong khung).
Hướng: ván đi từ **phải sang trái** (tàu ở bên phải, bờ ở bên trái). Pivot: Bottom-**Right** cho bộ này
(điểm neo là chỗ ván gắn vào mạn tàu, đứng yên suốt animation) — ghi rõ trong meta.

**Mô tả tạo hình:** ván gỗ nâu ấm 3–4 thanh ghép dọc, viền outline nâu đậm, có 2 thanh ngang chống trượt,
2 sợi dây thừng be vàng buộc ở đầu phía tàu. Đủ rộng để một nhân vật đi vừa. Không có tay vịn (che mất nhân vật).
Đúng tông với tàu du lịch hiện có trong game.

---

## HẠNG MỤC 2 — BONG BÓNG THOẠI + BIỂU CẢM

**Thư mục giao:** `Assets/_Game/Farm/Art/TouristBoat/Bubble/`

| Tên file | Kích thước | Nội dung |
|---|---|---|
| `bubble_frame.png` | 256 × 256 | **Khung bong bóng thoại RỖNG** (game sẽ chèn icon món ăn vào giữa). Bong bóng tròn/bo tròn màu kem trắng, viền nâu đậm cartoon, có cái đuôi nhọn chỉ xuống dưới-trái (trỏ vào miệng nhân vật). Vùng giữa PHẢI trống trơn, đường kính vùng trống ≥ 160px |
| `face_happy.png` | 256 × 256 | **Mặt cười vui vẻ** — mặt tròn vàng ấm (#FFC93C), hai mắt cong hình vòng cung hạnh phúc, miệng cười to, hai má ửng hồng. Dễ thương, đọc rõ ở cỡ nhỏ |
| `face_angry.png` | 256 × 256 | **Mặt tức giận** — mặt tròn đỏ cam (#E8574A), hai lông mày chau chéo xuống, miệng cau xuống, có thể thêm 1 dấu gân nổi kiểu manga ở thái dương. Vẫn giữ nét **dễ thương/hờn dỗi**, KHÔNG dữ tợn (game cho trẻ em & phụ nữ) |

Pivot cả 3 file: **Center**. Cả 3 phải cùng cỡ 256×256 để game hoán đổi không giật.

---

## HẠNG MỤC 3 — BẢNG KHÓA BẾN TÀU (slot chưa mua)

**Thư mục giao:** `Assets/_Game/Farm/Art/TouristBoat/Lock/`

| Tên file | Kích thước | Nội dung |
|---|---|---|
| `dock_lock_board.png` | 520 × 250 | **Bảng gỗ TRỐNG** treo ở bến chưa mở — khung gỗ nâu ấm bo góc, 2 cọc gỗ chống hai bên cắm xuống cát, mặt bảng để TRỐNG HOÀN TOÀN (game in chữ "Mở ở Lv12 · 2.000 vàng" bằng TMP lên trên). Bề mặt bảng phải phẳng và sáng đủ để chữ nâu/vàng đọc rõ |
| `dock_lock_icon.png` | 128 × 128 | **Ổ khóa** vàng đồng cartoon, thân khóa mập tròn dễ thương, có lỗ khóa hình tròn, outline nâu đậm |

Pivot: `dock_lock_board.png` = Bottom-Center (cắm xuống cát) · `dock_lock_icon.png` = Center.

---

## HẠNG MỤC 4 — KHUNG CARD POPUP (dùng cho 2 popup: "tàu sắp cập bến" và "mua bến")

**Thư mục giao:** `Assets/_Game/Farm/Art/TouristBoat/UI/`

| Tên file | Kích thước | Nội dung |
|---|---|---|
| `card_frame_wood.png` | 720 × 480 | **Khung card bo góc RỖNG** kiểu bảng gỗ: viền gỗ nâu ấm dày ~40px bo góc tròn mềm, ruột màu kem sáng (#F6E4C3) phẳng trơn để in chữ lên. Bốn góc có 4 đinh tán đồng nhỏ trang trí. **Ruột phải trống hoàn toàn** |
| `card_ribbon_top.png` | 460 × 120 | **Dải ruy băng tiêu đề TRỐNG** màu burgundy #8E1F3B viền đồng vàng #D9A441, dạng cờ đuôi nheo hai đầu, đắp lên mép trên của khung card (game in tiêu đề bằng TMP) |
| `btn_confirm.png` | 300 × 110 | **Nút bấm TRỐNG** màu xanh lá tươi cartoon, bo góc tròn, có gờ nổi 3D nhẹ và highlight phía trên. Không chữ (game in "Đã rõ" / "MUA") |
| `btn_close_x.png` | 96 × 96 | Nút X tròn nhỏ màu đỏ cam, dấu X trắng dày bo tròn đầu |

Pivot: tất cả **Center**.

> **QUAN TRỌNG:** `card_frame_wood.png` sẽ được game kéo giãn 9-slice. Vui lòng vẽ sao cho phần **viền
> gỗ nằm gọn trong 40px mỗi cạnh** và phần ruột kem là mảng phẳng đồng màu — như vậy giãn ra không bị méo hoa văn.

---

## HẠNG MỤC 5 (tùy chọn, làm sau nếu còn thời gian) — HIỆU ỨNG MỞ BẾN

**Thư mục giao:** `Assets/_Game/Farm/Art/TouristBoat/FX/`

| Tên file | Kích thước | Nội dung |
|---|---|---|
| `fx_star_gold.png` | 128 × 128 | Ngôi sao 4 cánh vàng lấp lánh (kiểu tia sáng), tâm trắng sáng, dùng cho pháo hoa lúc mua được bến |
| `fx_confetti_piece.png` | 64 × 64 | Một mảnh giấy kim tuyến hình chữ nhật bo góc, vẽ 4 màu trong 4 file? **KHÔNG** — vẽ 1 file màu trắng, game tự nhuộm màu |

---

## TỔNG KẾT SỐ LƯỢNG

| Hạng mục | Số file | Ưu tiên |
|---|---|---|
| 1. Ván gỗ gangplank (4 frame) | 4 | ⭐ Cao nhất — thiếu là khách đi trên mặt nước |
| 2. Bong bóng + 2 mặt biểu cảm | 3 | ⭐ Cao |
| 3. Bảng khóa bến + ổ khóa | 2 | Trung bình |
| 4. Khung card + ruy băng + 2 nút | 4 | Trung bình |
| 5. FX sao + confetti | 2 | Thấp (game đã tự vẽ được) |
| **TỔNG** | **15 file** | |

Giao xong bỏ vào đúng thư mục ghi trên, báo lại để đội code gắn vào khung sườn.
