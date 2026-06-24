# AUTONOMY MODE — Luật tự chủ cho autopilot & mọi agent

> File này định nghĩa **autopilot được tự làm tới đâu** và **khi nào phải dừng hỏi bạn**.
> `/autopilot` và mọi agent PHẢI tuân thủ. Mục tiêu: bạn "chỉ ra lệnh", agent làm tối đa phần
> an toàn, và chỉ làm phiền bạn khi thật cần (Unity Editor, art, tài khoản, quyết định lớn).

---

## 1. Nguyên tắc gốc

> **"Cộng thêm thì cứ làm. Đụng vào cái đang chạy thì hỏi."**

Autopilot ưu tiên việc **CỘNG THÊM** (additive) — tạo mới, mở rộng, sinh data qua tool —
vì loại này gần như không thể làm hỏng game đang chạy. Mọi việc **SỬA/XOÁ cái đang có** đều
rủi ro hơn → phải qua bạn.

---

## 2. ✅ ĐƯỢC TỰ LÀM (không cần hỏi từng bước)

- Tạo **script C# mới**, class/manager mới, Editor Tool mới (`Tools/Farm Game/...`).
- **Mở rộng** code hiện có bằng cách THÊM field/method có default an toàn (không đổi chữ ký public đang dùng).
- Sinh/đổ **data `.asset` QUA TOOL có report + undo** (mission, level reward, shop lock, order…) —
  không sửa tay từng asset.
- Viết/cập nhật **tài liệu, spec, design doc, log phiên** trong `production/`, `docs/`, `design/`, `memory/`.
- Cập nhật cột **Status** trong `AUTOPILOT_BACKLOG.md`.
- Chạy **kiểm thử & review**: `/smoke-check`, `/regression-suite`, `/code-review`, `/balance-check`,
  `/qa-plan`, `/playtest-report` (mô phỏng), build thử trong sandbox.
- Sửa **lỗi rõ ràng** (null ref, lệch key, sai công thức) khi nguyên nhân chắc chắn & sửa là cộng thêm/khoanh vùng.

## 3. 🛑 PHẢI DỪNG & HỎI (DANH SÁCH DỪNG — tuyệt đối)

Ghi vào mục **CẦN BẠN** trong báo cáo, KHÔNG tự làm:

1. **Sửa tay hoặc xoá** file `.unity` (scene), `.prefab`, hoặc `.asset` data quan trọng đang dùng.
2. **Xoá hoặc viết lại logic lớn** đang chạy — đặc biệt: ô đất/plot, kéo hạt, thu hoạch, village order,
   shop, cooking/inventory, level/EXP, save.
3. Đổi **chữ ký public** của method/prefab interface mà UI/scene đang tham chiếu.
4. **Commit / push / git reset / xoá file hàng loạt / rm -rf** (đã chặn cứng trong settings.json — đừng thử lách).
5. **Quyết định thiết kế còn mơ hồ** (chưa có trong backlog/playbook): scope, mô hình kiếm tiền, audience,
   con số kinh tế chưa duyệt, nội dung L21–L30.
6. Bất cứ việc cần **tài khoản/tiền của bạn** (Play Console, Apple, Steam, AdMob, mua/thuê art).
7. Cài thư viện/SDK bên thứ 3 mới vào project (đề xuất trước, bạn duyệt).

## 4. 🤝 VIỆC "LÀM PHẦN LỚN, BẠN FINISH 1 BƯỚC" → đánh dấu REVIEW

Nhiều việc agent code/tool xong nhưng **bước cuối phải ở trong Unity Editor** (bản chất Unity là vậy):

- Chạy menu Editor Tool để sinh hierarchy/data.
- Kéo **sprite/icon/âm thanh** vào Inspector (agent không thấy & không gán được asset nhị phân).
- Wire reference scene, đặt tên object đúng quy ước (vd `Pen_03`, `btn_PenGem`).
- Vào **Play Mode** test cảm giác/nhịp/độ khó.

→ Agent làm xong code/tool, đặt task = `REVIEW`, và ghi **chính xác** bước Unity vào CẦN BẠN
(menu nào, object nào, sprite nào). Bạn làm 1 lượt rồi `/autopilot` chạy tiếp.

## 5. Quy trình bắt buộc mỗi task

`PROPOSE (ngắn) → DELEGATE đúng agent → IMPLEMENT (ưu tiên Editor Tool) → VERIFY (0 lỗi đỏ) → UPDATE status → (lặp)`

- Mỗi đổi data đi qua **tool có verify + undo + report** (tạo gì, gắn gì, cần kéo asset gì).
- Giữ **Definition of Done**: ≥60fps máy tầm trung, 0 lỗi đỏ console, có animation+âm thanh cho hành động cốt lõi,
  QA pass. (Theo `TEAM_BRIEF_TASKBOARD.md` Phần 5.)

## 6. Cách bạn ra lệnh (từ vựng)

| Lệnh | Ý nghĩa |
|------|---------|
| `/autopilot` | Làm tiếp backlog tới khi gặp việc cần bạn |
| `/autopilot 3` | Chỉ làm 3 task rồi báo cáo (chạy thận trọng) |
| `/autopilot status` | Xem tiến độ backlog, không làm gì |
| `/autopilot mission` | Chỉ làm cụm nhiệm vụ (Milestone 1) |
| `tiếp tục roadmap` | Tương đương `/autopilot` (giữ thói quen cũ của bạn) |
| `duyệt CẦN BẠN 1,2,3` | Bạn xác nhận đã làm xong các việc CẦN BẠN → autopilot mở khoá task phụ thuộc |
| `bật full review` | Đổi review-mode sang `full` (nhiều gate hơn, chậm & kỹ hơn) |

## 7. Mức tự chủ (chỉnh được)

Ghi vào `production/review-mode.txt` một dòng:
- `solo` — gate tối thiểu, autopilot chạy nhanh nhất **(mặc định cho autopilot)**.
- `lean` — bỏ gate director không thuộc phase, giữ vài review quan trọng.
- `full` — đầy đủ gate director + lead (kỹ nhất, chậm nhất).

> Rào an toàn ở Mục 3 **không bao giờ tắt**, kể cả ở chế độ `solo`.

---

## 8. Sự thật cần nhớ (kỳ vọng đúng)

Autopilot **không** build được game 100% không-chạm-tay, vì 3 thứ bản chất cần con người:
1. **Unity Editor** — kéo asset, chạy tool, Play-mode test (Unity không có API cho agent làm thay khâu này).
2. **Sáng tạo asset** — vẽ/đặt nhân vật, animation, nhạc, ảnh store.
3. **Tài khoản & phát hành** — Play Console, Apple, Steam, quảng cáo, tiền.

Nhưng autopilot lo trọn phần **code, tool, data, doc, QA** và gom mọi việc-cần-bạn thành **một danh sách
gọn để bạn làm một lượt**. Đó là mức "rảnh tay" thực tế cao nhất — và nó tiết kiệm cho bạn rất nhiều.
