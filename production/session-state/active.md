# ACTIVE SESSION - 2026-09-09 (VÒNG 16 — ÁNH SÁNG · TẦM NHÌN · VÙNG CÂU · CÁ LÊN QUẦY · CÔNG CỤ · NÚT THOÁT)

<!-- STATUS -->
Epic: Hồ Câu (SCN_Fishing) — scene câu cá có bạn bè, offline trước, Firebase sau
Feature: Vòng 16 — dimmer ánh sáng, menu 8 kẹp zoom + dời spawn, menu 10 vùng câu theo ô nước, cá lên gian hàng + hint NPC mua, búa/kéo, stack Escape, ẩn avatar HUD, prompt V4
Task: Chờ Sếp compile → menu 6 → 10 → 8 → 7 → (SCN_Farm) 5 → Play test. Chi tiết `production/BAO_CAO_VONG16_2026-09-09.md` mục 8.
<!-- /STATUS -->

## Đã đóng vòng này
- Nguyên nhân chói/trắng: prefab ngày-đêm farm ~2.4x + 3 Point bán kính 43.59 phủ map 1-unit. `FishingLightDimmer` (LateUpdate, ExecuteAlways, Edit Mode chỉ nhân 5 đèn controller) × `cfg.sceneLightMultiplier` 0.5, sàn ambient 0.25.
- Menu 8: kẹp `cameraZoomMax/cameraOrthoSize` theo map (aspect 21:9), `FishingCameraFollow.MinBoundsViewRatio` 1.5→1.0, dời PlayerSpawnPoint vào ô đất trong map.
- Menu 10: `Tilemap_IsoWaterAnim` + TilemapCollider2D(trigger)+Composite+Rigidbody2D Static+FishingZone; tile collider Grid; tắt Zone_01 khi có nước.
- Dev A: `StallSourceStore.FishBasket`, `IStallExternalStore`/`StallExternalStores`, `FishBasketStallStore`, `StallSaleHintUI` (phải-dưới, Canvas_FishingPopup), category `Ca`. Farm chỉ cộng thêm, backup devA/.
- Dev B: búa 900/lv4, kéo 600/lv3 (đề xuất); khung anim `Sheet_Fishing` 4×3 (F 294×590, M 314×568 đo thật); PROMPT V4 51 file.
- Dev C: `FishingPopupStack`, `hudShowProfileBlock=false`, `hudPanelTapOutsideCloses=false`, 7 popup soát 6 tiêu chí.
- Reviewer: 0 CHẶN, 2 NẶNG + 9 NHẸ đã sửa, 1 NHẸ → cờ config.

## Quyết định kỹ thuật (đừng đổi nếu không có lý do)
- Không sửa prefab/curve ngày-đêm của farm — chỉ nhân sau bằng dimmer. Edit Mode không nhân đèn Point (nhân dồn khi Ctrl+S).
- Vùng câu = chính lớp nước (không kéo Zone tay). Zone_01 mẫu chỉ tắt, không xoá.
- Panel HUD (Giỏ/Bạn bè/Chat) KHÔNG modal mặc định; popup toàn màn có nền mờ.

## Việc còn treo
- CHƯA COMPILE THẬT vòng 13→16. Lỗi đỏ → Sếp gửi nguyên văn.
- CẦN SẾP: duyệt sáng 0.5/0.25; giá rìu/búa/kéo; giá cá ×1.3 ở quầy; có bật chạm-ngoài-đóng panel HUD; tab "Cá" riêng ở quầy (phải nới UI farm); 4 cần/10 cá/M5-1 audience/Firebase SDK/chức năng rìu-búa-kéo.
- Chờ art V4 (51 file). Khi có `Sheet_Fishing.png` → chạy lại ★/menu 2 để dựng clip cầm cần.
- Đề xuất chưa duyệt: `FishingArtImportTool` 1 nút gắn art vào slot.

## Vòng 11 còn treo — chưa động
- 3 hệ trùng khoá save, Pen_03/House_01 SetActive(false), plot khít lưới, Chậu Hoa3/4 đảo. Xem `BAO_CAO_VONG11_2026-09-06.md` mục 7.
