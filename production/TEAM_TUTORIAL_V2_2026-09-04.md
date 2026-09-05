# 🏗️ KẾ HOẠCH TUTORIAL V2 — chia Dev & bàn giao art (04/09/2026, vòng 12)

> Lead: Tech Lead · Duyệt scope: Sếp Huy · Nguồn: SCAN vòng 12 (2 Dev quét song song).
> Luật thi công: `production/AUTONOMY.md` · Backup: `production/backup_round12_2026-09-04/`
> **Luật vàng chống lỗi ghép nối: MỖI FILE MỘT CHỦ.** Không Dev nào được sửa file của Dev khác.

---

## 0. Vì sao phải làm lại tutorial (bằng chứng SCAN, không cảm tính)

| Vấn đề đo được | Bằng chứng |
|---|---|
| Khung hội thoại **trong suốt hoàn toàn** | `NPC_Background` trong SCN_Farm: `m_Sprite: {fileID: 0}`, `m_Color.a: 0` — chữ trần trên nền game |
| **Không có NPC** | `NPC_Portrait` là sprite TĨNH `avata_player.png`, và **0/31 step gán `npcPortrait`** ⇒ luôn null |
| **Không có nút "Tiếp tục"** | Cả panel 800×200 là Button vô hình → người chơi không biết phải bấm đâu |
| Typewriter tạo rác GC | `TutorialManager.cs:861` nối chuỗi từng ký tự `text += c` |
| **Camera zoom xung đột** | `TutorialCameraZoom.cs` ghi `orthographicSize` 8→4, trong khi thang dự án là **~460** ⇒ code chết, tranh chấp `CameraController` |
| 31 bước nhét trong scene | 31 GUID nằm trong `SCN_Farm.unity` (600k dòng) ⇒ đổi thứ tự = sửa scene = dễ vỡ |
| Bước mồ côi | `L1L2_04b_FirstHarvest.asset` tồn tại nhưng KHÔNG trong `_steps` ⇒ failsafe `TutorialPrePlant.cs:137` không bao giờ khớp |

**Điểm sáng — không phải làm lại từ đầu:**
`CameraController.CinematicFocus(worldPos, orthoSize, lockInput)` (dòng 580) **đã có sẵn**, chạy SmoothDamp
với `cinematicSmoothTime = 0.45`. `TutorialCameraFocus.cs` đã bọc sẵn `FocusOnRice/Flower/Pen`.
23 điều kiện chờ `TutorialWaitAction` đã đủ dùng. **V2 kế thừa hết, chỉ thay lớp trình diễn.**

---

## 1. Quyết định thiết kế Sếp đã chốt (04/09)

| # | Chốt |
|---|---|
| 1 | Card hội thoại: **`Assets/Export_Kitchen_UI_Package/Sprites/panel_paper_cream.png`** (9-slice border 24) — không vẽ mới |
| 2 | NPC tutorial: **vẽ mới riêng**, 3 clip × 12 frame + blink → gói A prompt đội vẽ |
| 3 | 4 NPC popup lên cấp: **giữ chế độ puppet 1 hình master + code tự diễn** (nhẹ máy) — không import sheet 12 frame |

---

## 2. Chia Dev — mỗi Dev sở hữu riêng file của mình

### 🟦 DEV-UI — khung hội thoại (chủ 2 file, KHÔNG ai khác đụng)
**Sở hữu:** `Assets/_Game/Farm/Scripts/Tutorial/TutorialDialogueCard.cs` (mới)
· `Assets/_Game/Farm/Editor/TutorialV2CardBuilderTool.cs` (mới)

- Card 9-slice `panel_paper_cream`, neo **đáy màn hình**, chừa chỗ NPC bên trái.
- Typewriter đổi sang **`TMP_Text.maxVisibleCharacters`** (0 rác GC, thay `text += c`).
- **Nút "Tiếp tục" thật**: nền `btn_paper_small` (cùng gói art), chevron ▶ nảy nhẹ, chỉ hiện **sau khi gõ xong chữ**.
- Vào: card trượt từ dưới lên + phóng nhẹ 0.92→1.00 (ease-out-back, 0.28s). Ra: mờ + tụt xuống 0.18s.
- Bấm giữa lúc đang gõ = **hiện hết chữ ngay** (giữ hành vi `SkipTyping` cũ, không phá thói quen).
- Editor tool dựng nguyên cụm thành **prefab `PF_TutorialV2_Dialogue.prefab`** — thoát khỏi cảnh mọi thứ nằm trong scene.

### 🟩 DEV-ANIM — NPC & camera (chủ 2 file)
**Sở hữu:** `Assets/_Game/Farm/Scripts/Tutorial/TutorialNpcActor.cs` (mới)
· `Assets/_Game/Farm/Scripts/Tutorial/TutorialCameraDirector.cs` (mới)

- `TutorialNpcActor`: 3 clip (`Talk` / `Wave` / `Point`) × 12 frame, fps riêng từng clip.
  - **Blink chèn ngẫu nhiên 3–6s**: đè `guide_blink.png` trong 0.12s rồi trả về frame đang chạy.
  - `Point` **dừng lại ở frame 06–12 lặp nhẹ** trong lúc chờ người chơi thao tác (khớp cách vẽ ở prompt).
  - **Thiếu frame ⇒ tự lùi về 1 sprite tĩnh, KHÔNG crash, KHÔNG mất NPC** (cùng triết lý fallback của `BuilderWorkerConfig`).
  - Chạy được với **placeholder** ngay hôm nay — art về là thay sprite, không đụng code.
- `TutorialCameraDirector`: bọc `CameraController.CinematicFocus`, thêm `AnimationCurve` ease-in-out
  + overshoot nhẹ 3% rồi trả về (cảm giác "camera có trọng lượng" thay vì trôi phẳng).
  - ⚠️ **Đồng thời vô hiệu `TutorialCameraZoom.cs`** (thang 8→4 sai, tranh chấp CameraController) — bằng cờ, KHÔNG xoá file.

### 🟨 DEV-VFX — hiệu ứng (chủ 1 file)
**Sở hữu:** `Assets/_Game/Farm/Scripts/Tutorial/TutorialVfxDirector.cs` (mới)

- 4 mốc bắn hiệu ứng: `OnStepEnter` (burst ray) · `OnHighlight` (glow ring + arrow nảy) ·
  `OnStepComplete` (sparkle rải + dust puff) · `OnTutorialDone` (confetti Lana).
- Tái dùng prefab Lana có sẵn (`Confetti_blast_multicolor`, `Sparkle_ellow`, `Flash_round_ellow`)
  theo đúng tiền lệ `Assets/_Game/Farm/Prefabs/VFX/LevelUp/`.
- Sprite gói B về là gắn vào field, **thiếu vẫn chạy** (bỏ qua hiệu ứng đó, không lỗi đỏ).

### 🟥 LEAD — nối dây & không ai khác đụng
**Sở hữu:** `TutorialManager.cs` (file đang chạy — **CHỈ Lead sửa**, và chỉ bằng cách CỘNG THÊM)

- Thêm hook gọi sang 3 director trên. **Không đổi chữ ký public nào** (UI/scene đang tham chiếu).
- Thêm 3 field mới vào `TutorialStepData.cs` **có default an toàn** (asset cũ không vỡ):
  `npcClip` (Talk/Wave/Point) · `cameraFocusTargetId` · `vfxOnComplete`.
- Nối lại bước mồ côi `L1L2_04b_FirstHarvest`.

---

## 3. Thứ tự thi công & phụ thuộc

```
HÔM NAY (không chờ ai)          →  ĐỘI VẼ CHẠY SONG SONG
├─ ✅ Tool 3 thợ xây (xong)         └─ Gói A: NPC 37 file  ⭐ ưu tiên 1
├─ ✅ Tool nối dây popup (xong)        Gói B: VFX 10 file
├─ ✅ Prompt + cây thư mục (xong)
└─ ⏳ CHỜ SẾP DUYỆT changeset 5 file mới ở Mục 2

SAU KHI DUYỆT                    SAU KHI ART VỀ
├─ DEV-UI  : card + prefab       ├─ Lead import gói A → gắn TutorialNpcActor
├─ DEV-ANIM: NPC + camera        ├─ Lead import gói B → gắn TutorialVfxDirector
├─ DEV-VFX : vfx director        └─ QA: chạy 31 bước, 0 lỗi đỏ, ≥60fps
└─ LEAD    : nối TutorialManager
```

**Khung sườn chạy được TRƯỚC khi art về** — dùng placeholder. Đó là điểm mấu chốt:
đội vẽ và đội code không chờ nhau.

---

## 4. Rủi ro đã lường & cách chặn

| Rủi ro | Chặn thế nào |
|---|---|
| Sửa `TutorialManager.cs` làm vỡ 31 bước đang chạy | Chỉ CỘNG THÊM, không đổi chữ ký public. Backup trước. Field mới có default = hành vi cũ |
| 2 Dev cùng sửa 1 file → build sạch nhưng chạy code cũ (bài học 31/08) | **Mỗi file một chủ**, ghi rõ ở Mục 2. Lead là người duy nhất chạm file đang chạy |
| Art về trễ, code đứng chờ | Mọi director **chạy được với placeholder**; thiếu sprite = bỏ qua hiệu ứng, không lỗi đỏ |
| Card mới đè lên tutorial cũ, hỏng cả hai | Cờ `useV2Dialogue` trên TutorialManager — **tắt là về nguyên bản cũ 100%** |
| Prefab hoá làm lệch tham chiếu scene | Tool builder chỉ **dựng thêm** prefab mới, KHÔNG xoá `NPC_Dialog_Popup` cũ cho tới khi Sếp nghiệm thu V2 |

---

## 5. Definition of Done (theo TEAM_BRIEF_TASKBOARD Phần 5)

- [ ] Console **0 lỗi đỏ**, giữ **≥60fps** máy tầm trung khi tutorial chạy
- [ ] Chạy trọn **31 bước** không kẹt, `TUTORIAL_MAIN_DONE` set đúng
- [ ] Tắt cờ `useV2Dialogue` → tutorial cũ chạy y nguyên (chứng minh không phá gì)
- [ ] NPC có nói + quơ tay + chớp mắt; card có gõ chữ + nút Tiếp tục
- [ ] Camera zoom có easing, không giật, không tranh chấp `CameraController`
