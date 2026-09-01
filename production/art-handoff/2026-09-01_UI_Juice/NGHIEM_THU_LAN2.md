# ✅ BIÊN BẢN NGHIỆM THU LẦN 2 — 4 MASTER + 4 BLINK (2026-09-01)
> Người nghiệm thu: Tech Lead — QC bằng pixel thật trên cả 8 file, đối chiếu spec `PROMPT_SPRITE_FORGE_STYLE_UNIFY_DOT2.md`.

## KẾT LUẬN: **ĐẠT — cho phép gắn vào game.**

| Kiểm mục | Kết quả đo |
|---|---|
| Canvas 512×512, alpha 4 góc = 0 | ✅ 8/8 file |
| Đáy thân y=470±4 | ✅ 469–470 (cả 8 file) |
| Tâm X=256±6 | ✅ 255–256 (cả 8 file) |
| Blink chỉ khác master ở VÙNG MẮT | ✅ vùng khác-mạnh chỉ 0.55–1.28% diện tích, đúng quanh mắt cả 4 con → chớp mắt sẽ không giật hình |
| Nhân dạng | ✅ char_01 nông dân nam trẻ (yếm đỏ caro) · char_02 ĐẦU BẾP nón trắng viền đỏ + khăn nơ đỏ (nhìn phát biết ngay) · char_03/04 giữ nguyên bản đã duyệt |
| Đồng bộ style | ✅ cả 4 cùng họ chibi đầu to má hồng; ghi chú nhỏ: nét outline char_01/02 đậm-sắc hơn 03/04 một chút — chấp nhận được, sẽ tiệp hẳn khi đứng trong khung tròn popup |

## Ghi chú vận hành
- Art đã nằm đúng đường dẫn tool đọc: `Assets/Art/UI/LevelUpV2/characters/char_0N/char_0N_master.png` + `_blink.png`.
  Tool `Gắn art PUPPET` ƯU TIÊN `_master.png` và tự bắt `_blink.png` → không cần chỉnh gì thêm.
- Nhắc đội vẽ (lần 3): quy trình chuẩn là giao vào `art-handoff` để Lead QC TRƯỚC khi vào `Assets/` — lần này QC đạt nên cho qua, lần sau giao thẳng Assets mà QC trượt là tự tay dọn.
- Kích hoạt: Sếp bấm `Tools/Farm Game/★★★ UI JUICE — LÀM TẤT CẢ (1 nút)` → Save → Debug Preview L5:
  4 nhân vật mới thở + CHỚP MẮT ngẫu nhiên trong khung 230px quanh banner.
