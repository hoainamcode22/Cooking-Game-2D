# 🎨 PROMPT ĐỘI VẼ — VÒNG 13 (04/09/2026)

> Gửi: **agent-sprite-forge**. Người ra đề: Tech Lead. Duyệt: Sếp Huy.
> **Chỉ vẽ đúng 3 gói dưới.** Lead đã kiểm tồn kho — nhiều thứ Sếp tưởng thiếu thì **đã có sẵn**, xem mục ❌ cuối file.

---

## ⛔ RANH GIỚI CÔNG VIỆC (nhắc lại — lệnh Sếp)

**Đội vẽ CHỈ VẼ. Không chèn logic.** Không sửa `.cs`/`.asset`/`.prefab`/`.unity`/`.meta`, không tự đặt fps,
không ghép sprite-sheet, không đổi tên file (tên là hợp đồng), không import thẳng vào `Assets/`.
Chỉ thả PNG vào thư mục art-handoff rồi nhắn Lead.

## 🔒 LUẬT ART STUDIO

1. ❌ **KHÔNG TEXT** trên asset — trừ đúng 1 ngoại lệ ghi rõ ở gói A (chữ trên lá cờ Anh là hoạ tiết cờ, không phải label).
2. ❌ **KHÔNG NỀN, KHÔNG BÓNG ĐỔ** — alpha trong suốt 100%.
3. ✅ Meta Unity chuẩn: spriteMode Single từng file.
4. ✅ Style: burgundy `#8E1F3B` + đồng vàng `#D9A441`, gỗ nâu ấm, outline nâu đậm cartoon, dễ thương.
5. ✅ Giao đúng tên file + thư mục, không thêm file phụ.

---

# GÓI A — CỜ NGÔN NGỮ  ⭐ ƯU TIÊN 1

Hiện tại 2 lá cờ trong màn Cài đặt **đang được vẽ bằng code** (`SettingsPopupUI.cs:606-632`) — chỉ là
hình chữ nhật đỏ + ngôi sao, và hình xanh + chữ "EN". Nhìn rất thô. Cần art thật.

Giao vào: **`production/art-handoff/2026-09-04_Round13/A_Co_NgonNgu/`**

| # | Tên file | Canvas | Vẽ gì |
|---|---|---|---|
| A1 | `flag_vn.png` | **96 × 64 px** | **Cờ Việt Nam** — nền đỏ `#DA251D`, ngôi sao vàng 5 cánh `#FFFF00` chính giữa. Bo góc nhẹ ~6px, viền ngoài nâu đậm 2px cho khớp tông UI game. Có nếp gấp vải rất nhẹ (gradient mờ) cho đỡ phẳng |
| A2 | `flag_en.png` | **96 × 64 px** | **Cờ Anh (Union Jack)** — nền xanh `#012169`, chữ thập đỏ `#C8102E` viền trắng, các dải chéo trắng-đỏ. Cùng bo góc ~6px + viền nâu đậm 2px, cùng độ dày nếp gấp như A1 |

**Bắt buộc:** hai lá **CÙNG kích thước, cùng độ bo góc, cùng độ dày viền** — chúng nằm cạnh nhau trong
một hàng, lệch nhau là lộ ngay.

---

# GÓI B — ICON GIA VỊ & RAU  ⭐ ƯU TIÊN 2

Sếp yêu cầu vẽ lại **rau, chai nước mắm, chai nước tương**. Hiện các icon này nằm rải rác ở
`Assets/Anh/` dưới dạng ảnh AI tách nền (`fish_sauce.png`, `soy_sauce.png`, `rau.png`), **phong cách
lệch hẳn** so với bộ icon chuẩn của game.

### 📌 CHUẨN PHẢI BÁM THEO
Bộ icon đồng bộ nhất dự án: **`Assets/Assetsgame/hatgiong/SHOP/icons/`** (26 file, đặt tên
`seed_cabbage`, `seed_carrot`, `seed_chili`, `seed_corn`, `seed_mushroom`, `seed_lemon`,
`seed_pepper`…). **Mở vài file đó ra xem trước khi vẽ** — phải cùng: độ dày outline, độ bão hoà màu,
góc nhìn (hơi chếch 3/4), độ bo tròn hình khối, cách đánh sáng (nguồn sáng trên-trái).

Giao vào: **`production/art-handoff/2026-09-04_Round13/B_Icon_GiaVi/`**

| # | Tên file | Canvas | Vẽ gì |
|---|---|---|---|
| B1 | `ing_rau.png` | **256 × 256** | **Bó rau xanh** — vài cọng rau lá xanh mướt buộc lại, lá có gân, tông xanh lá `#4CAF50` → `#8BC34A`, outline nâu đậm |
| B2 | `ing_nuoc_mam.png` | **256 × 256** | **Chai nước mắm** — chai thuỷ tinh dáng thấp mập, nước màu nâu hổ phách `#8B4513`, nắp đỏ burgundy, **nhãn chai để TRỐNG** (không chữ). Có ánh sáng phản chiếu trên thân chai |
| B3 | `ing_nuoc_tuong.png` | **256 × 256** | **Chai nước tương** — chai dáng cao thon (khác rõ B2 để không lẫn), nước màu nâu đen `#2B1810`, nắp vàng đồng `#D9A441`, **nhãn để TRỐNG**. Cùng kiểu ánh sáng như B2 |

**Bắt buộc:** vật thể chiếm ~80% khung, căn giữa, chừa lề đều ~25px mỗi cạnh — để xếp cạnh 26 icon
cũ trong cùng một lưới không cái nào to nhỏ lệch nhau.

---

# GÓI C — SỬA 2 NHÂN VẬT POPUP LÊN CẤP  ⭐ ƯU TIÊN 3

Lead đo được: 4 nhân vật popup lên cấp **không cùng kích thước**.

```
char_01_master.png : 512 × 512   ✅
char_02_master.png : 512 × 512   ✅
char_03_master.png : 304 × 304   ❌ nhỏ hơn 40%
char_04_master.png : 305 × 305   ❌ nhỏ hơn 40%, kích thước LẺ
```

⇒ 2 nhân vật bên phải sẽ **mờ và nhỏ hơn hẳn** 2 nhân vật bên trái khi hiển thị cùng cỡ.

Giao vào: **`production/art-handoff/2026-09-04_Round13/C_Char_LenCap/`**

| # | Tên file | Yêu cầu |
|---|---|---|
| C1 | `char_03_master.png` | Xuất lại ở **512 × 512**, **vẽ lại ở độ phân giải gốc** — KHÔNG phóng to ảnh 304px lên (phóng to = mờ nhoè, không sửa được gì) |
| C2 | `char_04_master.png` | Xuất lại ở **512 × 512**, cùng yêu cầu như C1 |

Giữ nguyên tạo hình 2 nhân vật hiện có, chỉ vẽ lại cho nét ở kích thước lớn. Nhân vật căn giữa khung,
chừa lề trên ~20px, chân chạm mép dưới — khớp cách `char_01` / `char_02` đang làm.

---

# ❌ KHÔNG VẼ TRONG VÒNG NÀY — Lead đã kiểm, ĐÃ CÓ SẴN

Để đội vẽ khỏi phí công:

| Sếp nêu | Thực tế |
|---|---|
| "Btn **Bắt đầu nào** thiếu assets" | ✅ **ĐÃ CÓ** `Assets/Export_Kitchen_UI_Package/Sprites/btn_big_green.png` (9-slice border 48). Lỗi là **chưa ai gán vào nút** — việc của Dev, không phải đội vẽ |
| "Btn **Tất cả / Dễ / Vừa / Khó** chưa có assets" | ✅ **ĐÃ CÓ** `tab_pill_on.png` + `tab_pill_off.png` (border 24), thậm chí đã nạp sẵn vào skin. Lead đã sửa code ở vòng 13 để dùng chúng |
| Khung popup **Hồ Sơ** "như cái sườn" | ⏸ Khung đang sinh procedural bằng code (`SkinKit.BoGoc`). Cần Lead chốt layout với Sếp trước rồi mới đặt vẽ — **chưa đặt hàng vòng này** |
| Badge **NEW** trên item lên cấp | ✅ **ĐÃ CÓ** sprite sinh sẵn ở `PopupSpriteFactory.cs:353`, và chỗ gắn ở `LevelUpPopupTownshipTool.cs:606` |
| Animation dải item | Là **code**, không phải art |

---

# ✅ CHECKLIST TỰ KIỂM TRƯỚC KHI GIAO

- [ ] Gói A: 2 lá cờ **cùng 96×64**, cùng bo góc, cùng độ dày viền? Đặt cạnh nhau xem có lệch không?
- [ ] Gói B: mở cạnh 3-4 file trong `hatgiong/SHOP/icons/` — outline, độ bão hoà, góc nhìn có **cùng một bộ** không?
- [ ] Gói B: chai nước mắm và nước tương có **phân biệt được ngay từ dáng chai** không? Nhãn để trống chứ?
- [ ] Gói C: đúng **512×512**, và là vẽ lại chứ không phải phóng to ảnh cũ? (zoom 400% kiểm độ nét)
- [ ] Tất cả: nền alpha 0, không bóng đổ, không chữ/số/logo (trừ hoạ tiết cờ)?
- [ ] Đúng **7 file**, đúng tên, đúng thư mục, không file phụ?

Xong hết mới nhắn Lead: **"đã giao vòng 13"**.
