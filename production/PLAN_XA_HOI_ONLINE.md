# PLAN — XÃ HỘI ONLINE KIỂU HAY DAY (thăm làng + chợ async)

> Người duyệt: Sếp Edric · Người lập: Tech Lead · Ngày: 2026-08-19
> Nguyên tắc: async social (snapshot), KHÔNG realtime multiplayer. Nền tảng: Firebase
> (Firestore chính + RTDB presence), theo thiết kế đã có `production/firebase/FIREBASE_DATABASE_DESIGN.md`.

---

## 0. Làm rõ khái niệm "thăm làng = bản chụp"

KHÔNG phải ảnh chụp màn hình, cũng không phải video. Snapshot = **tờ hướng dẫn lắp ráp bằng chữ**:

```json
{
  "username": "FarmCuaTi",
  "level": 12,
  "plots":  [ {"id": 3, "crop": "lua",    "stage": "growing", "readyAtUtc": "..."},
              {"id": 4, "crop": "cachua", "stage": "ready"} ],
  "pens":   [ {"id": "pen_03", "level": 2, "producing": "egg"} ],
  "stall":  [ {"itemId": "egg", "qty": 5, "price": 90} ],
  "decor":  [ {"id": 109, "x": 12, "y": 5} ]
}
```

Máy người thăm nhận tờ này (vài KB) → dùng **prefab + sprite + animation CÓ SẴN trong game** dựng
lại làng: gà vẫn chạy, cây vẫn đung đưa, nước vẫn chảy — vì mọi chuyển động là do game engine của
máy mình render, server không gửi hình ảnh/chuyển động nào cả. Làng người khác nhìn "sống" y như
làng mình, chỉ khác: **chế độ chỉ-xem** (không cày cuốc hộ được), trừ đúng 1 hành động: mua ở quầy.

**Lợi thế lớn của mình:** gói SaveSystem M0-2 vừa xong đã có sẵn bộ "chụp trạng thái" (SaveAdapters
capture kho/ô đất/chuồng/quầy). FarmSnapshot chính là bản RÚT GỌN của SaveData → phần khó nhất
đã làm xong hôm qua mà không biết.

---

## 1. LỘ TRÌNH 4 GIAI ĐOẠN

### GIAI ĐOẠN A — Setup hạ tầng Firebase (phiên Chrome, ~30-45 phút, 0 đồng)
| # | Việc | Ai làm |
|---|------|--------|
| A1 | Tạo Firebase project `cooking-farm-2d` trên console | 🤖 Claude lái Chrome, sếp ngồi xem |
| A2 | Thêm app Android (+iOS nếu cần) → tải `google-services.json` | 🤖 Claude; sếp lưu file vào project |
| A3 | Bật Authentication: Anonymous (+ Google link để sau) | 🤖 Claude |
| A4 | Tạo Cloud Firestore (region `asia-southeast1` — gần VN) + Realtime Database | 🤖 Claude |
| A5 | Dán bộ Security Rules Phương án A (đã viết sẵn trong design doc) | 🤖 Claude |
| A6 | Import catalog: chạy `import_catalog.js` với bộ JSON đã xuất | 🤖 Claude hướng dẫn / chạy nếu có Node trên máy |
| A7 | Đăng nhập Google, đồng ý điều khoản, đặt tên project | 🧑 Sếp (dính tài khoản — luật AUTONOMY) |

> Gói Spark miễn phí, KHÔNG cần thẻ, KHÔNG billing. App Check + budget alert để giai đoạn D.

### GIAI ĐOẠN B — Nền client Unity (đội dev code, sếp làm vài bước Editor)
| # | Việc | Loại |
|---|------|------|
| B1 | Import Firebase Unity SDK (Auth + Firestore + Database) — sếp tải .unitypackage, kéo vào | 🧑 |
| B2 | `FirebaseBootstrap.cs`: init SDK, đăng nhập Anonymous, log `[Fire]` | 🤖 |
| B3 | Đặt username duy nhất (collection `usernames/`, claim qua batched write) + UI nhập tên | 🤖 code, 🤝 sếp test |
| B4 | `FarmSnapshotBuilder.cs`: tái dùng SaveAdapters → build JSON snapshot, đẩy lên `farmSnapshots/{uid}` (debounce 5 phút + khi pause/quit) | 🤖 |
| B5 | Smoke test: 2 thiết bị/2 account thấy snapshot của nhau trên console | 🤝 |

### GIAI ĐOẠN C — Tính năng nhìn thấy được
| # | Việc | Loại |
|---|------|------|
| C1 | **Chế độ thăm làng**: scene farm mở ở mode read-only, dựng từ snapshot người khác; nút "Về nhà" | 🤖 code, 🤝 wire scene |
| C2 | **Chợ async MVP**: đăng bán từ quầy lên `market/`, người thăm bấm mua (mô hình claim theo Phương án A rules-only), người bán mở game nhận vàng | 🤖 |
| C3 | **Danh sách làng để thăm**: tab "Hàng xóm" — trộn user thật + 5-10 SEED ACCOUNT làng đẹp mình tự tạo + làng NPC offline (chống thế giới vắng) | 🤝 |
| C4 | **Leaderboard** theo `lbScore = level*100000 + exp`, cache 15 phút | 🤖 |
| C5 | Kết bạn + presence chấm xanh (RTDB onDisconnect) | 🤖 (làm sau cùng, có thể dời) |

### GIAI ĐOẠN D — Trước soft launch (chưa làm bây giờ)
Nâng Blaze + 4 Cloud Functions (purchaseListing... chống hack), App Check, budget alert $5,
TTL dọn listing, index tổng hợp, đo read/write thật với 12 tester.

---

## 2. VIỆC GÌ *KHÔNG* LÀM (chốt để khỏi lạc scope)
- ❌ Realtime multiplayer kiểu Play Together (thấy người khác chạy nhảy live) — sai thể loại, đắt gấp trăm lần.
- ❌ Đưa ảnh/asset lên cloud — ảnh ở nguyên trong build (đã phân tích, đã chốt).
- ❌ Đưa định nghĩa vật phẩm lên DB để game đọc lúc chơi — chỉ đưa bản catalog đối soát cho chợ.
- ❌ Chat realtime — để rất sau, kèm kiểm duyệt.

## 3. ĐIỀU KIỆN TRƯỚC KHI CODE GIAI ĐOẠN B/C
1. ✅ M0-2 SaveSystem đã giao — nhưng **sếp phải test PASS** (bài test 5 phút) vì SnapshotBuilder đứng trên vai nó.
2. 🔴 Bug "mất vật phẩm khi thoát Play Mode" (M0-2b) phải chốt nguyên nhân — snapshot đẩy data sai lên mạng thì hàng xóm nhìn thấy làng sai.
3. Fix Pho Beef Missions (APPLY) — 1 phút, tiện tay cùng lượt Unity.

## 4. CHI PHÍ & RỦI RO
- Giai đoạn A-C: **0 đồng** (Spark). Rủi ro chính: gian lận số dư khi chưa có Functions — chấp nhận được vì
  tiền ảo 100%, không nạp; nâng cấp ở giai đoạn D trước khi mở rộng.
- Công sức ước tính: A = 1 buổi; B = 2-4 ngày dev + 2 bước Editor; C = 1-2 tuần tuỳ polish.
