# Phân tích video — BẢNG ĐƠN HÀNG CÚ

Tách 80 frame @2fps từ video 40 giây. Soi từng nút, từng trạng thái, từng nhịp.

---

# PHẦN 1 — OBJECT NGOÀI MAP

Không phải cái bảng trơn. Nó là **một cảnh nhỏ có nhân vật**:

| Thành phần | Mô tả |
|---|---|
| **Bảng đen** | mặt bảng xanh đậm, khung gỗ nâu, đứng trên hai chân |
| **Mái hiên xanh** | vắt ngang đỉnh bảng |
| **Chóp vòm cam** | trên mái, bên trong có **rổ rau củ** — làm bảng có "danh tính" ngay từ xa |
| **Phiếu đơn ghim trên bảng** | 3–4 tờ giấy nhỏ, mỗi tờ có **đinh ghim đỏ** |
| **Chú CÚ** | đứng cạnh bảng, **kéo một chiếc xe đẩy xanh** chở hàng |

## Hai chi tiết đắt nhất

**Phiếu trên bảng phản chiếu đúng nội dung popup.** Tờ nào xong thì **xanh lá**, tờ chưa xong thì **trắng ngà**. Người chơi liếc qua bản đồ là biết có đơn giao được hay không — **không cần mở popup**. Đây chính là thủ pháp mà quầy hàng ở video trước cũng dùng (bày hàng lên mặt quầy).

**Chú cú kéo xe** biến một cái bảng tĩnh thành nhân vật. Nó cũng giải thích luôn cơ chế: cú là người *chuyển hàng đi*, không phải người mua.

---

# PHẦN 2 — POPUP

## 2.1 Khung ngoài

- **Title pill** bo tròn, **viền nét đứt bên trong**, kèm **icon mặt cú**
- **Nút X đỏ tròn**, nằm **lồi hẳn ra ngoài mép** panel
- Panel cam, bên trong là vùng lõm màu đậm hơn — cùng ngôn ngữ với popup Quầy Hàng

## 2.2 Cột trái — lưới phiếu đơn 3 × 3

### Hình dáng phiếu

Tờ giấy có **mép dưới răng cưa** (như xé ra từ tập), **đinh ghim tròn đỏ** ở giữa mép trên. Nội dung là **PHẦN THƯỞNG**, viết trên hai dòng kẻ:

```
   ⭐ 3      ← sao xanh = EXP
   🪙 6      ← đồng vàng
```

> Điểm thiết kế đáng học: **phiếu chỉ hiện PHẦN THƯỞNG, không hiện yêu cầu.** Người chơi quét mắt cả lưới để tìm đơn *đáng giá nhất*, rồi mới bấm vào xem cần những gì. Nếu nhồi cả yêu cầu lên phiếu thì lưới rối và mất hẳn nhịp "chọn".

### Bốn trạng thái phiếu

| Trạng thái | Hiển thị |
|---|---|
| **Chưa đủ hàng** | nền **trắng ngà** |
| **Đủ hàng, giao được** | nền **XANH LÁ** + **dấu tích xanh** to góc trên phải |
| **Đang chọn** | **viền phát sáng vàng** bao quanh |
| **Ô trống** | ô vuông bo góc **viền nét đứt** |

Phiếu xếp từ trái sang phải, trên xuống dưới. Ô trống nằm sau cùng.

## 2.3 Cột phải — chi tiết đơn

```
┌─────────────────────────────────────┐
│  [avatar khách]      ⭐ 5           │  ← thưởng
│    (con heo)         🪙 11          │
│ - - - - - - - - - - - - - - - - - - │  ← gạch nét đứt
│  [🌾✓]   [🥔✓]   [🥚✓]              │  ← lưới 3×2 = 6 ô
│   2/1     8/1     8/1               │
│  [ ]      [ ]      [ ]              │
└─────────────────────────────────────┘
        [🗑]        [ GIAO HÀNG ]
```

- **Avatar khách hàng** to, nằm góc trên trái (con heo đội mũ)
- **Ô thưởng** góc trên phải: sao EXP + vàng
- **Gạch nét đứt** chia hai phần
- **Lưới yêu cầu 3 cột × 2 hàng = 6 ô**: icon vật phẩm + **dấu tích xanh** khi đủ
- Dưới cùng: **nút thùng rác đỏ** (bỏ đơn) + **nút xanh dương "GIAO HÀNG"**

### Chi tiết dễ bỏ sót nhất: cách đọc con số

Ghi là **`6/2`** — nghĩa là **đang có 6, cần 2**. Không phải "2/2 xong".

Đây là lựa chọn có chủ đích và **rộng lượng hơn hẳn**: người chơi thấy luôn kho mình đang dư bao nhiêu, không phải thoát ra mở kho kiểm tra. Con số vượt mức (`8/1`) vẫn hiện đúng chứ không cắt về `1/1`.

---

# PHẦN 3 — LUỒNG HOẠT ĐỘNG

## Nhịp chính

```
Bấm bảng ngoài map
      ↓
Popup mở, lưới phiếu hiện ra
      ↓
Bấm một phiếu  →  phiếu sáng viền vàng
                  cột phải đổ chi tiết đơn
      ↓
Bấm GIAO HÀNG
      ↓
① Trừ vật phẩm khỏi kho
② Phiếu TAN THÀNH KHÓI TRẮNG tại chỗ
③ Sao EXP + đồng vàng BAY LÊN kèm nhãn "+1"
④ Các phiếu còn lại DỒN LẠI lấp chỗ trống
⑤ Số vàng / EXP trên thanh HUD nhảy lên
      ↓
Đơn mới xuất hiện sau một lúc
```

## Ba hiệu ứng khi giao hàng

Frame 57 bắt trọn khoảnh khắc này — cả ba chạy **cùng lúc**, không nối đuôi:

1. **Cụm khói trắng** bung ra đúng vị trí phiếu vừa biến mất
2. **Sao xanh + đồng vàng** bay chéo lên trên kèm chữ **"+1"**
3. **Lưới dồn lại** — phiếu phía sau trượt lên lấp chỗ

Rẻ mà hiệu quả: người chơi thấy rõ *cái gì vừa mất đi* và *được gì*, không cần popup xác nhận.

## Nút thùng rác — bỏ đơn

Đơn nào yêu cầu thứ mình không có thì **vứt đi cho khuất mắt**, nhường chỗ cho đơn mới. Đây là van xả áp: không có nó thì lưới sẽ bị kẹt cứng bởi vài đơn không bao giờ làm nổi.

## Không có đồng hồ đếm ngược

Suốt 40 giây video **không hề có timer nào** trong popup. Đơn tự sinh lại âm thầm. Khác hẳn Bảng Tin Chợ (có "Làm mới sau: 1m42s" + nút trả tiền).

Nghĩa là: đơn hàng là **nguồn thu đều đặn, không phải trò săn hàng**. Hai hệ thống cố ý có nhịp khác nhau.

---

# PHẦN 4 — SO VỚI HỆ THỐNG ĐANG CÓ CỦA BẠN

Dự án đã có `VillageOrderManager` — nhưng **mô hình khác hẳn**:

| | **Của bạn hiện tại** | **Video này** |
|---|---|---|
| Nơi nhận đơn | **bong bóng trên đầu 5 nhà dân**, rải khắp map | **một bảng tập trung** |
| Xem tổng quan | không có — phải đi khắp map | **liếc một cái thấy hết** |
| Chọn đơn | bấm từng nhà | lưới 9 ô, bấm chọn |
| Bỏ đơn không thích | **không có** | **nút thùng rác** |
| Thấy kho có đủ chưa | không | **`6/2` ngay trên đơn** |
| Lưu qua phiên | **KHÔNG** — mất khi thoát game | (không rõ) |
| Thưởng | vàng + EXP×2 | vàng + EXP |

**Ba thứ video làm tốt hơn rõ rệt:**

**Tập trung một chỗ.** Đơn rải trên 5 nhà buộc người chơi phải đi tuần khắp bản đồ. Với game cho phụ nữ và trẻ em thì đó là ma sát vô ích.

**Hiện `có/cần` ngay trên đơn.** Không phải thoát ra mở kho đếm.

**Cho bỏ đơn.** Hệ của bạn không có, nên một đơn đòi thứ chưa mở khoá sẽ chiếm chỗ nhà đó **vĩnh viễn**.

> ⚠️ Và lỗi nặng nhất đang có: **`VillageOrderManager` không lưu gì cả.** Đơn hàng là runtime thuần, thoát game là mất sạch. Người chơi gom nửa chừng cho một đơn, tắt app, quay lại thì đơn biến mất.

---

# PHẦN 5 — NẾU LÀM

## Giữ hay thay?

Không nhất thiết phải bỏ bong bóng trên nhà dân. **Hai cái bổ sung nhau được:**

- **Bong bóng trên nhà** = đơn lẻ, nhanh, tiện tay khi đi ngang
- **Bảng đơn tập trung** = xem tổng quan, chọn đơn to, quản lý

Nhưng nếu phải chọn một thì **bảng tập trung thắng**, vì nó giải quyết cả ba nhược điểm trên cùng lúc.

## Danh sách việc

| # | Việc | Ghi chú |
|---|---|---|
| 1 | **Lưu đơn hàng** | lỗi đang có, phải sửa dù chọn mô hình nào |
| 2 | Object bảng + phiếu ghim phản chiếu trạng thái | nhìn từ ngoài biết có đơn giao được |
| 3 | Popup: lưới phiếu 3×3, 4 trạng thái | trắng / xanh+tích / viền vàng / ô trống |
| 4 | Cột phải: avatar khách, thưởng, lưới yêu cầu 3×2 | hiện **`có/cần`** chứ không phải `đủ/cần` |
| 5 | Nút thùng rác bỏ đơn | van xả áp, bắt buộc |
| 6 | Hiệu ứng giao hàng | khói trắng + sao/vàng bay + lưới dồn |
| 7 | Nhân vật cú kéo xe | art, làm bảng sống |

## Cần bạn vẽ

Bảng đen + khung gỗ · mái hiên + chóp vòm · phiếu giấy mép răng cưa (2 màu: trắng ngà + xanh lá) · đinh ghim đỏ · **nhân vật cú** · xe đẩy · avatar khách hàng (mỗi con vật một cái)

## Cố ý làm khác video

Đổi **chú cú** sang con vật khác (gấu trúc, thỏ, mèo) · đổi bảng đen sang **bảng gỗ hoặc bảng nút chai** · phiếu đổi từ mép răng cưa sang **mép xé giấy** · bảng màu khác cam đất.
