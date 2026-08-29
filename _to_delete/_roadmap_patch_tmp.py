# -*- coding: utf-8 -*-
import io, re, sys
p = "production/ROADMAP_GAME_COMPLETE.md"
s = io.open(p, encoding="utf-8").read()

entry = u"""## Nhật ký sprint (agent tự ghi thêm mỗi phiên)

### Hệ Tàu Khách Du Lịch V2 — 2026-08-29 (3 Dev song song + QA 2 vòng, verdict SHIP)
- Chuyển hệ boat từ CHU KỲ CỐ ĐỊNH (đậu 40p) sang HƯỚNG SỰ KIỆN: tàu cập bến → bắc ván gỗ → 3-6 khách
  du lịch (random 11 nhân vật NVGAME) xuống tàu → đi theo waypoint đường đất → xếp hàng trước cooking →
  bubble món mở LẦN LƯỢT hết khách (stagger 0.4s) → tap giao món (bất kỳ khách nào, không cần đúng thứ tự) →
  thưởng + mặt cười bay lên HUD → khách về tàu → khách cuối lên tàu thì tàu rời bến → chuyến kế 5p (1 bến) /
  10p so le (nhiều bến).
- Kinh tế (Sếp chốt): vàng = Σ giá nguyên liệu CHÍNH × 2 (loại gia vị), EXP = dish.rewardExp, món random trong
  38 DishData lọc theo unlockLevel. Kiên nhẫn 30p/khách chạy SONG SONG (UTC, offline vẫn trôi); hết giờ =
  MẶT TỨC GIẬN, bỏ về tàu, không trả tiền. Lưới an toàn maxDockMinutes=35 → tàu tự rời bến, hệ không bao giờ kẹt.
- 21 file C# / 11.161 dòng: Dev A lịch tàu V2 (BoatScheduleCore/BoatDockManager/Config/Controller +
  BoatShoreAdjustTool + vá TouristBoatDiagnosticTool) · Dev B khách du lịch (VisitorManager/Agent/Queue/Bubble/
  SmileyFX/Gangplank + NPCAnimationSetupTool + TouristVisitorSetupTool) · Dev C UI (popup báo tàu, popup mua slot,
  FX mở bến, rework UnlockFlow + BoatDockSlot, TouristBoatUIPopupSetupTool).
- Pipeline art: 11 sheet NVGAME (lưới 4x3 nền trắng) → 132 frame đã xóa phông + chuẩn hoá canvas + pivot
  bottom-center tại `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/`.
- QA: compile 3 pass 0 error/0 warning · test console 119 PASS/0 FAIL · vòng 1 tìm 4 BLOCKING + 6 MAJOR + 11 minor
  (kẹt tàu vĩnh viễn, mất món trả 0 vàng, popup chết sau khi vào bếp, không tua nhanh test được) → vòng 2 đóng 21/21.
- Backup 9 file gốc: `production/backup_boat_2026-08-29/`. Báo cáo: `production/session-state/BOAT_V2_IMPLEMENTATION_REPORT.md`
  (mục 4 = ANH CẦN LÀM TRONG UNITY) · QA + checklist Play Mode 50 bước: `production/session-state/QA_REPORT_BOAT_V2.md` §7.8 ·
  Prompt đội vẽ 15 asset: `production/session-state/PROMPT_SPRITE_FORGE_BOAT_V2.md`.
- CẦN SẾP: chạy 4 tool + điền 13 field config + KÉO WAYPOINT theo đường đất & QueueAnchor trước nhà hàng (chỉ Sếp làm được).
"""

old_header = u"## Nhật ký sprint (agent tự ghi thêm mỗi phiên)"
assert s.count(old_header) >= 1, "khong tim thay header nhat ky"
s = s.replace(old_header, entry, 1)

# cap nhat dong Sprint 7 trong bang trang thai
s = s.replace(
    u"| 7 | Content L16-L22: hồ cá (mở 2 món cá), pet/trang trí nâng cao, event đơn giản | ⬜ | |",
    u"| 7 | Content L16-L22: hồ cá (mở 2 món cá), pet/trang trí nâng cao, event đơn giản | ⬜ | |\n| 7b | **Tàu khách du lịch V2** (khách lên bờ, xếp hàng, đặt món, trả vàng+EXP, popup báo tàu, mua slot bến) | ✅ CODE XONG + QA SHIP — chờ Sếp chạy tool + kéo waypoint + playtest | 2026-08-29 |")

io.open(p, "w", encoding="utf-8").write(s)
print("ROADMAP updated")
