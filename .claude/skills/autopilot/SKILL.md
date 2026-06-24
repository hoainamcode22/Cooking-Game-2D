---
name: autopilot
description: "Tự lái build game từ A→Z theo AUTOPILOT_BACKLOG.md. Producer chọn task chưa bị chặn kế tiếp, giao đúng agent, làm theo AUTONOMY.md (chỉ việc cộng thêm an toàn), QA, cập nhật trạng thái, lặp tới khi gặp việc cần bạn rồi gom thành 1 danh sách CẦN BẠN. Gõ /autopilot, /autopilot 5 (làm 5 task), /autopilot status, hoặc /autopilot mission."
argument-hint: "[no args | <số task> | status | <từ khoá milestone/epic>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion, TodoWrite
model: sonnet
context: |
  !cat production/AUTONOMY.md 2>/dev/null
  !sed -n '1,9999p' production/AUTOPILOT_BACKLOG.md 2>/dev/null
  !cat memory/MEMORY.md 2>/dev/null
  !ls -t production/session-state/ 2>/dev/null | head -5
---

# AUTOPILOT — Một lệnh, đội agent build tiếp

Đây là điều phối viên (producer) tự lái. Mục tiêu: bạn gõ **`/autopilot`**, đội agent làm tối đa
phần việc CỘNG THÊM AN TOÀN từ `AUTOPILOT_BACKLOG.md`, và chỉ dừng lại khi thật sự cần bạn.

> **Bắt buộc đọc trước:** `production/AUTONOMY.md` (luật tự chủ + rào an toàn) đã được nạp ở context.
> Tuân thủ tuyệt đối. Khi nghi ngờ → DỪNG & hỏi, không đoán.

---

## Phase 0 — Parse lệnh & chế độ review

1. Đọc tham số:
   - không có / số N → chạy vòng lặp tối đa N task (mặc định: tới khi blocked, trần an toàn 8 task/phiên).
   - `status` → chỉ in bảng tiến độ backlog (DONE/DOING/BLOCKED/TODO theo milestone) rồi dừng.
   - từ khoá (vd `mission`, `M1`, `tutorial`) → chỉ chọn task trong milestone/epic khớp.
2. Resolve review mode: đọc `production/review-mode.txt`. Nếu chưa có → mặc định `solo` cho autopilot
   (gate nhẹ); ghi lại file. (Xem `.claude/docs/director-gates.md`.)

## Phase 1 — Định hướng (orient)

1. Đọc `AUTOPILOT_BACKLOG.md` (đã ở context). Đọc báo cáo phiên gần nhất trong
   `production/session-state/` và `memory/MEMORY.md` để biết đã làm tới đâu.
2. Xây danh sách ứng viên: task `TODO` mà **mọi Dep = DONE**.
3. Lọc theo `AUTONOMY.md`:
   - **Được tự làm (🤖):** code cộng thêm, tạo/sửa script mới, viết Editor Tool, sinh data .asset
     QUA TOOL có verify, viết doc/spec, chạy check/QA, sửa lỗi rõ ràng.
   - **Tự làm phần lớn (🤝):** làm code/tool xong, để lại đúng 1 bước Unity cho bạn → đánh dấu `REVIEW`.
   - **Phải dừng & hỏi (🧑 hoặc bất kỳ việc trong "DANH SÁCH DỪNG" của AUTONOMY.md):** ghi vào CẦN BẠN.
4. Dùng `TodoWrite` tạo to-do cho các task sẽ làm phiên này.

## Phase 2 — Vòng lặp thực thi (mỗi task)

Lặp cho tới khi: hết task 🤖 đủ điều kiện, hoặc đạt trần N, hoặc gặp việc cần bạn.

Với mỗi task:
1. **PROPOSE (ngắn):** 1–2 dòng "sẽ làm gì, đụng file nào, có an toàn không". Nếu task nằm trong
   "DANH SÁCH DỪNG" → KHÔNG làm, đẩy sang CẦN BẠN.
2. **DELEGATE:** spawn **Owner agent** qua `Task` (vd gameplay-programmer, ui-programmer). Giao kèm:
   "Đọc CLAUDE.md + AUTONOMY.md + dòng task này trong AUTOPILOT_BACKLOG.md. Chỉ làm việc cộng thêm
   an toàn. KHÔNG commit. KHÔNG xoá logic/scene/prefab. Báo lại file đã đụng + bước Unity (nếu có)."
   Task độc lập → spawn song song trong cùng một lượt.
3. **IMPLEMENT:** ưu tiên tạo **Unity Editor Tool** (menu `Tools/Farm Game/...`) để sinh hierarchy/
   prefab/data thay vì sửa tay file `.unity/.prefab/.asset`. Mọi đổi data đi qua tool có report + undo.
4. **VERIFY:** chạy check phù hợp — `/smoke-check`, `/code-review`, `/regression-suite`,
   `/balance-check` (kinh tế), hoặc test script. Yêu cầu: **0 lỗi đỏ console**.
5. **UPDATE:** sửa cột **Status** của task trong `AUTOPILOT_BACKLOG.md`:
   - xong hẳn (không cần Unity) → `DONE`
   - xong code, chờ bạn chạy tool/test trong Unity → `REVIEW` + thêm dòng vào CẦN BẠN
   - bị chặn → `BLOCKED` + lý do
6. Ghi 1 dòng tiến độ vào `production/session-state/` (log phiên) + cập nhật `memory/MEMORY.md` nếu có quyết định lớn.

## Phase 3 — Báo cáo & dừng

In **báo cáo cuối** gồm:
1. ✅ **Đã làm xong phiên này** (task ID + tóm tắt + file đụng).
2. 🧑 **CẦN BẠN** (gom hết, đánh số rõ): mỗi việc ghi *chính xác phải làm gì trong Unity/art/account*,
   menu tool nào chạy, sprite/asset nào cần kéo, quyết định nào cần chốt.
3. ⏭️ **Task kế tiếp** autopilot sẽ làm khi bạn gõ `/autopilot` lần sau (và cái gì đang chặn nó).
4. 🔢 **Tiến độ tổng** (đếm DONE/REVIEW/BLOCKED/TODO trên tổng).

> Sau báo cáo: **DỪNG**. Không tự commit, không làm việc trong DANH SÁCH DỪNG. Bạn xử lý CẦN BẠN,
> rồi gõ `/autopilot` để chạy tiếp.

---

## Ghi nhớ quan trọng
- **Không bao giờ** vượt rào an toàn trong `AUTONOMY.md`, kể cả khi việc còn lại đều bị chặn —
  thà dừng và báo CẦN BẠN còn hơn đoán liều làm hỏng scene/logic.
- Ưu tiên **đóng trọn 1 milestone** trước khi nhảy milestone sau (trừ khi bị chặn hết).
- Hệ nhiệm vụ: lấy data từ `production/MISSIONS_MASTER_LIST.md` (đừng tự bịa số).
- Nếu backlog hết task 🤖: báo "Backlog đã hết việc tự làm được — đây là các việc CẦN BẠN để mở khoá tiếp."
