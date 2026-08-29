# 🎨 SỔ TAY ĐỒNG BỘ STYLE — ICON MÓN ĂN (dùng lại mãi cho mọi đợt vẽ món)

> File này để **dán nguyên khối** vào mọi prompt vẽ icon món ăn từ nay về sau.
> Mục đích: mọi món vẽ ở nhiều đợt, nhiều người, nhiều lúc khác nhau vẫn nhìn như **cùng một bộ**.
> Ban hành 2026-08-27. Nguồn tham chiếu: 18 icon món đang chạy trong game (`Assets/Assetsgame/Món ăn/`).

---

## 1. HAI PHONG CÁCH TRONG GAME — ĐỪNG LẪN

| Nơi dùng | Góc nhìn | Ghi chú |
|---|---|---|
| **Nông trại (farm)** | **Isometric 45°** — cây cối, công trình, ô đất | KHÔNG áp dụng cho icon món ăn |
| **Bếp (cooking) + icon món ăn + UI** | **Hình phẳng, chính diện hơi chúc xuống** | ← phần này là của icon món ăn |

Icon món ăn thuộc nhóm thứ hai. **Không vẽ món ăn theo phối cảnh isometric.**
Sổ tay này nói về **nét vẽ, màu sắc, vật đựng, quy cách file** — KHÔNG đổi phối cảnh của bên nào.

---

## 2. QUY TẮC BẮT BUỘC CHO ICON MÓN ĂN

**Góc nhìn & bố cục**
- Nhìn chếch từ trên xuống khoảng **40–50°** (thấy được mặt trong bát/đĩa, vẫn thấy thành bát).
- **1 phần ăn duy nhất**, đặt **chính giữa khung**, thức ăn chiếm **70–85%** chiều rộng khung.
- Không cắt mép: toàn bộ bát/đĩa nằm trong khung, chừa lề trống ~8% mỗi bên.

**Vật đựng (chọn theo loại món)**
- Món canh / súp / hầm → **bát gốm mộc**: thân màu kem (#F2E4CF), vành và chân màu nâu đất (#8B5E3C).
- Món xào / salad / chiên / cơm → **đĩa gốm mộc** cùng bảng màu, vành hơi dày, có thể hơi méo tay cho mộc.
- Món nướng nguyên con / miếng lớn → **thớt gỗ** nâu ấm, vân gỗ nhẹ.
- Nước uống / chè → **ly thuỷ tinh trong**, thấy lớp màu bên trong, có ống hút/lát chanh nếu hợp.
- Bánh → **đĩa nhỏ** hoặc **lá chuối**, tuỳ món.

**Nét & tô màu**
- **Outline nâu đậm** (#4A2B18 → #5C3A22), nét ngoài **dày** quanh vật đựng, nét trong **mảnh hơn**.
- Tô kiểu **cartoon bán thực**: khối màu phẳng + chuyển sắc mềm (airbrush nhẹ), **không** vẽ pixel-art,
  **không** vẽ 3D render, **không** vẽ ảnh chụp thực.
- Có **highlight bóng** trên bề mặt thức ăn (mỡ/nước sốt/men gốm) để món trông ngon.
- Bảng màu ấm: nâu, kem, cam-đỏ, xanh lá dịu. Tránh màu neon, tránh tím/xanh lam lạnh làm màu chủ đạo.
- Màu nhấn chuẩn của game khi cần: **burgundy #8E1F3B**, **đồng vàng #D9A441**.
- Cảm giác chung: **dễ thương, ấm áp, dành cho phụ nữ & trẻ em** — không tả thực máu/xương/nội tạng.

**Nền & bóng**
- Nền **trong suốt 100%** (hoặc magenta #FF00FF nếu công cụ không xuất được alpha — bên dev sẽ tách).
- **KHÔNG** vẽ nền bàn, nền cỏ, nền đất, khăn trải, hoa văn trang trí phía sau.
- **KHÔNG** đổ bóng xuống "sàn" (drop shadow tách rời). Chỉ được có bóng tiếp xúc **rất nhẹ, sát đáy**
  bát/đĩa, nằm trong silhouette của vật đựng.
- Khói: chỉ vẽ cho món nóng, dạng **vài sợi khói mảnh trong suốt** phía trên, không quá 20% chiều cao.

**Tuyệt đối không**
- ❌ Không chữ, không số, không logo, không nhãn, không watermark trên asset.
- ❌ Không viền khung, không badge, không tia sáng, không hiệu ứng lấp lánh (game tự thêm khi cần).
- ❌ Không vẽ tay người, không dụng cụ (dao, nĩa, thìa) trừ khi món bắt buộc (ví dụ ống hút cho nước uống).

---

## 3. QUY CÁCH FILE

| Hạng mục | Chuẩn |
|---|---|
| Định dạng | PNG, nền trong suốt (alpha) |
| Kích thước | **512 × 512** (vùng thức ăn thực chiếm ~380–430 px) — lớn hơn bộ cũ (371×426) cho màn hình lớn |
| Tên file | **đúng tên món tiếng Việt có dấu**, ví dụ `Súp bí đỏ kem sữa.png` (giống bộ 18 icon cũ) |
| Thư mục giao | `GIAO_FILE_TAI_DAY/` (ngay trong hồ sơ này) — bên dev soát rồi mới chuyển vào `Assets/` |
| 1 file | 1 món, **không** ghép nhiều món vào một sheet |
| Meta | **Không cần** kèm `.meta` — bên dev tự cài (Single, pivot Center, PPU 100) |

---

## 4. CHECKLIST TRƯỚC KHI GIAO (tự soát 8 điểm)

1. Nền trong suốt, không sót viền màu nền quanh mép? ✅
2. Không có bóng đổ rời khỏi bát/đĩa? ✅
3. Không có chữ/số/logo? ✅
4. Món nằm giữa khung, không bị cắt mép? ✅
5. Outline nâu đậm rõ ràng, nét ngoài dày hơn nét trong? ✅
6. Bảng màu ấm, không neon? ✅
7. Đặt cạnh 3 icon món cũ bất kỳ (mở `THAM_CHIEU_ICON_CU/`) — có nhìn như cùng một bộ? ✅
8. Đúng tên file tiếng Việt có dấu, đúng thư mục? ✅

---

## 5. THAM CHIẾU NHANH — 18 ICON ĐANG CHẠY (mở ra xem trước khi vẽ)

Mở thư mục `THAM_CHIEU_ICON_CU/` ngay trong hồ sơ này (bản sao 20 icon đang chạy) — ví dụ điển hình để bắt style:
- **Bát canh/hầm**: `Bò hầm cà rốt.png`, `Canh khoai tây thịt heo.png`
- **Đĩa xào**: `Bắp cải xào nấm.png`, `Bò xào tiêu.png`
- **Đĩa cơm có khói**: `Cơm chiên trứng.png`
- **Thớt gỗ nướng nguyên con**: `Gà nướng lu.png`
- **Ly nước**: `Nước mía chanh.png`

Cách so nhanh nhất: mở `THAM_CHIEU_ICON_CU/` ở chế độ xem ảnh lớn, kéo file bạn vừa vẽ vào cùng thư mục, rồi nhìn cả dãy một lượt — lệch style sẽ lộ ngay.
