# 🎮 CHECKLIST BẤM TOOL — VÒNG 12 (04/09/2026)

> Tên menu lấy trực tiếp từ code, không viết theo trí nhớ.
> **Làm đúng thứ tự.** Tool nào có DRY-RUN thì chạy DRY-RUN xem báo cáo trước, sạch mới APPLY.
> Backup toàn bộ file gốc: `production/backup_round12_2026-09-04/` (kèm `_CHECKSUM.txt`).

---

## BƯỚC 0 — Trước khi bắt đầu
| # | Việc |
|---|---|
| 0.1 | Mở **`SCN_Farm.unity`** (bắt buộc — 2 trong 3 tool đụng scene) |
| 0.2 | **Ctrl+R** build lại → Console phải **0 lỗi đỏ** |
| 0.3 | Còn lỗi đỏ thì DỪNG, gửi log cho Lead. Đừng chạy tool khi chưa compile được |

---

## BƯỚC 1 — 3 THỢ XÂY KHÁC NHAU  ⭐ nhanh nhất, thấy kết quả ngay

Art `w02`/`w03` đội vẽ đã giao **và đã cắt xong từ 03/09** (72 sprite). Vấn đề chỉ là chưa ai nối dây:
`workerSpriteSets` trong `BuilderWorkerConfig.asset` có 3 ô nhưng **cả 3 đều rỗng** → cả 3 thợ rơi về
bộ dùng chung của thợ 01. Tool này nối 72 sprite trong 1 nút.

| # | Menu | Kỳ vọng |
|---|---|---|
| 1.1 | `Tools ▸ Farm Game ▸ Worker ▸ ★ Nối 3 bộ sprite riêng cho 3 thợ (DRY-RUN)` | Báo cáo: 3 thợ × (búa 12f + mừng 12f), **0 cảnh báo** |
| 1.2 | `Tools ▸ Farm Game ▸ Worker ▸ ★ Nối 3 bộ sprite riêng cho 3 thợ (APPLY)` | Tự chạy kiểm tra cuối, phải ra **"✅ 3 thợ dùng 3 bộ sprite KHÁC NHAU"** |
| 1.3 | Nếu báo `⚠ enabled ĐANG TẮT` → `Tools ▸ Farm Game ▸ ★ BẬT TOÀN BỘ GÓI Nhân vật + Decor 5 stage` | Hệ thợ mới chạy |

> Lỡ tay: **Ctrl+Z**. File gốc còn ở `backup_round12_2026-09-04/BuilderWorkerConfig.asset`.

---

## BƯỚC 2 — POPUP LÊN CẤP: 4 NHÂN VẬT + PHÁO HOA NỔ TRÊN MẶT UI

3 lỗi dây đã đo được trong scene, tool sửa cả 3:
1. `celebrationSlots` = `{fileID: 0}` × 4 → popup **không cầm được slot nào**
2. `V2_CharSlot_03/04` trỏ **guid lạ**, không phải `char_03/04_master.png`
3. `fireworkSprites` rỗng → pháo hoa là **khối màu built-in của Unity**

Và lỗi Sếp báo — *"pháo hoa nằm sau lớp kia, bị che phủ"* — đã sửa ở code:
pháo hoa trước đây là **con của `contentPanel`** (chính cái card) nên nổ gọn trong card
và ăn theo animation phóng to 0.6→1.0 của card. Nay bắn ở **lớp `FX_Fireworks_Layer` phủ toàn khung**,
con trực tiếp của `popupRoot`, `SetAsLastSibling()` ⇒ nằm **trên cả nền mờ lẫn card**.

| # | Menu | Kỳ vọng |
|---|---|---|
| 2.1 | `Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (DRY-RUN)` | Liệt kê 4 slot + 7 sprite pháo hoa, **0 lỗi** |
| 2.2 | `Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nối lại dây popup (APPLY)` | Báo `fireworksOnTopLayer: FALSE → TRUE` |
| 2.3 | **Ctrl+S lưu scene** | 🔴 Tool cố ý KHÔNG tự lưu (luật studio) |
| 2.4 | Play → lên cấp thử | 4 nhân vật hiện đủ · pháo hoa **confetti thật** nổ **phủ toàn khung**, không bị card che |

> Hạt bay quá đà? Hạ `Firework Spread Boost` trên Inspector `LevelUpPopupUI` từ **1.55 → 1.20**.
> Muốn về pháo hoa cũ: bỏ tick **`Fireworks On Top Layer`**.
> Lỡ tay: **Ctrl+Z rồi ĐỪNG lưu**.

---

## BƯỚC 3 — TUTORIAL V2 (khung sườn — chạy được TRƯỚC khi có art)

| # | Menu | Nó làm gì |
|---|---|---|
| 3.1 | `Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Dựng card hội thoại V2 (1 nút)` | Dựng `TutorialV2_Dialogue` (card 9-slice `panel_paper_cream` + NPC bên trái + nút Tiếp tục + chevron ▶), dựng VFX/Camera director, và **nối 3 ref vào TutorialManager** |
| 3.2 | `Tools ▸ Farm Game ▸ Tutorial V2 ▸ Kiểm tra sẵn sàng (chỉ đọc)` | Phải ra **"✅ Khung sườn đủ"** và dòng `Nối TutorialManager: CÓ ✔ (V2 đang bật)` |
| 3.3 | **Ctrl+S lưu scene** | 🔴 Bắt buộc |
| 3.4 | Play → chạy tutorial từ đầu | Card kem bo góc hiện · chữ gõ từng ký tự · nút **Tiếp tục** hiện sau khi gõ xong · camera lia có easing · NPC là **ô mờ placeholder** (đúng, chưa có art) |

> 🚨 **NÚT LÙI VỀ TUTORIAL CŨ**: bỏ tick **`Use V2 Dialogue`** trên `TutorialManager` → chạy y nguyên bản cũ 100%.

---

## BƯỚC 4 — NẠP ART ĐỘI VẼ ĐÃ GIAO  ⭐ LÀM LUÔN HÔM NAY

Đội vẽ đã giao. Lead nghiệm thu bằng đo pixel: **35/47 file ĐẠT** (25 gói A + 10 gói B).
12 file `guide_talk_*` bị trả lại (dính viền ô lưới alpha đặc + sai khung hình) — **đã xoá**.

| # | Việc | Kỳ vọng |
|---|---|---|
| 4.1 | `Tools ▸ Farm Game ▸ Tutorial V2 ▸ ★ Nạp art NPC + VFX từ art-handoff (1 nút)` | Báo `NPC: talk 12/12 · wave 12/12 · point 12/12` và dòng `⚠ TẠM MƯỢN: talkFrames ... mượn 12 frame của waveFrames` |
| 4.2 | **Ctrl+S lưu scene** | 🔴 Bắt buộc |
| 4.3 | Play → chạy tutorial | NPC là **cô gái tóc nâu đuôi ngựa, yếm nâu** — không còn ô mờ placeholder. Có vẫy tay, chỉ tay, chớp mắt |

Tool tự: lọc bỏ file rác → copy vào `Assets/Art/UI/TutorialV2/` → set import chuẩn Sprite
(Single, alpha, không mipmap) → gán thẳng vào component. **Sếp không phải kéo ô nào.**

### Khi đội vẽ giao lại 12 file `guide_talk`
Thả vào `A_NPC_Guide/` → bấm lại đúng nút 4.1 → **art thật tự thay bộ mượn**. Không phải sửa gì.

---

## ✅ NGHIỆM THU — Sếp test đúng 6 điều này

1. **3 thợ xây**: đặt 1 công trình lớn (chuồng 7×5) → phải thấy **3 người KHÁC NHAU** (mũ vàng / mũ cam râu quai nón / mũ trắng khăn đỏ), đập búa **lệch nhịp** nhau.
2. **Lên cấp**: pháo hoa confetti thật nổ **phủ toàn màn**, thấy rõ, không bị card che.
3. **Lên cấp**: đủ **4 nhân vật** — 2 bên trái, 2 bên phải badge.
4. **Tutorial**: card kem bo góc, chữ gõ dần, nút Tiếp tục hiện đúng lúc, **NPC cô gái tóc nâu đứng bên trái** có vẫy/chỉ tay + chớp mắt.
5. **Tutorial**: tới bước "dùng kim cương cho hoa nở nhanh" (`L1L2_15_FlowerSpeedUp`) — **chạm vào card phải đi tiếp được**. (QA bắt được đây là chỗ dễ kẹt cứng nhất; đã vá.)
6. **Console**: **0 lỗi đỏ** suốt cả 3 phần.

Chỗ nào sai → chụp Console gửi Lead, kèm tên bước tutorial đang chạy.
