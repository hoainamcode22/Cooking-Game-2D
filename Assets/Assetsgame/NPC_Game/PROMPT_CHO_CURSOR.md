# PROMPT DÁN VÀO CURSOR — Setup NPC nông dân (Unity 6, 2D top-down) A→Z

> Copy toàn bộ phần dưới đây dán vào Cursor. Mọi con số (lưới, frame, hướng) đã được **đo trực tiếp từ 3 sheet**, KHÔNG phải phỏng đoán — Cursor cứ theo đúng, đừng tự đoán lại.

---

Bạn là dev Unity 6 (phiên bản 6000.3.x) chuyên 2D. Hãy setup **HOÀN CHỈNH 1 NPC nông dân** từ 3 sprite sheet của tôi: cắt sprite → tạo animation clip → Animator Controller → script "bộ não" điều khiển → prefab chạy được ngay. Tiêu chí xuyên suốt: **chuyển động mượt và kích thước frame đồng đều (không frame to frame nhỏ, không nảy lên xuống)**.

**TRƯỚC KHI VIẾT CODE:** đọc kỹ codebase hiện có của tôi (hệ thống ô đất / tilemap / kéo-thả hạt / vòng đời cây trồng). Phần NPC phải **tách rời** qua một "bảng việc" (job board) để KHÔNG phá logic cũ — bạn chỉ chèn đúng **1 dòng gọi** vào điểm tích hợp. Chỗ nào không chắc thì hỏi lại tôi, đừng tự chế.

---

## 0. Assets & chuẩn bị

- Thư mục chứa sheet: `Assets/Assetsgame/NPC_Game` (ổ đĩa: `E:\game1\Cooking-Game-2D\Assets\Assetsgame\NPC_Game`).
- Có **3 file PNG**. Hãy **đổi tên về ASCII** để tránh lỗi đường dẫn dấu tiếng Việt + khoảng trắng:
  - `..._đầu_tiên...png`  → **`farmer_walks.png`**
  - `..._thứ_2...png`     → **`farmer_field.png`**
  - `..._cuối_cùng...png` → **`farmer_emote.png`**
- **Kiểm tra nền trong suốt:** nếu PNG đang có **nền đen đặc** (alpha = 255 toàn ảnh), phải làm nền trong suốt TRƯỚC khi cắt. Dùng flood-fill từ 4 cạnh ảnh để chỉ biến vùng nền thành trong suốt, **giữ nguyên viền tối của nhân vật** (đừng key hết màu đen kẻo thủng nhân vật). Nếu khó tự động hoá an toàn thì báo tôi xuất lại bản nền trong suốt.

---

## 1. THÔNG SỐ LƯỚI & MAP FRAME (đã xác minh — dùng đúng như vậy)

Thứ tự frame: trái → phải, trên → xuống. `index` = `hàng * số_cột + cột` (0-based).

### `farmer_walks.png` — lưới **8 cột × 4 hàng** (32 frame). *(Cả 4 hướng đều cầm sẵn bình nước.)*
| Clip | Hàng | index frame | Ghi chú |
|---|---|---|---|
| `Walk_Up`    | 1 | 0–7   | đi LÊN (thấy lưng) |
| `Walk_Down`  | 2 | 8–15  | đi XUỐNG (thấy mặt) |
| `Walk_Left`  | 3 | 16–23 | đi TRÁI |
| `Walk_Right` | 4 | 24–31 | đi PHẢI |

### `farmer_field.png` — lưới **8 cột × 4 hàng** (32 frame).
| Clip | Hàng | index frame | Ghi chú |
|---|---|---|---|
| `Sow`       | 1 | 0–7   | gieo / trồng hạt |
| `CarryWalk` | 2 | 8–15  | cầm bình đi (hướng mặt) — dự phòng |
| `Water`     | 3 | 16–23 | tưới nước (có tia nước) |
| `Tend`      | 4 | 24–31 | chăm sóc cây bằng tay |

### `farmer_emote.png` — lưới **6 cột × 4 hàng** (24 frame).
| Clip | Hàng | index frame | Ghi chú |
|---|---|---|---|
| `Celebrate` | 1–2 | 0–11  | nhảy ăn mừng (chiếm 2 hàng) |
| `Wave`      | 3   | 12–17 | vẫy tay |
| `Idle`      | 4   | 18–23 | đứng trông ngóng |

> Tổng = 32 + 32 + 24 = **88 frame**, khớp đúng.

---

## 2. CẮT SPRITE (slicing) — yêu cầu bắt buộc

Dùng **API Unity 6 `UnityEditor.U2D.Sprites.ISpriteEditorDataProvider`** (KHÔNG dùng `TextureImporter.spritesheet` đã deprecated).

Cho từng sheet, set TextureImporter:
- `textureType = Sprite`, `spriteImportMode = Multiple`.
- **Pivot mỗi sprite = Bottom-Center `(0.5, 0)`** (`SpriteAlignment.BottomCenter`). → chân đứng yên, không nảy.
- **`spritePixelsPerUnit = chiều_cao_ảnh / số_hàng`** (tính riêng từng sheet). → mỗi ô cao đúng **1 unit**, nên nhân vật **đồng đều kích thước giữa mọi động tác** dù sheet emote có ô lớn hơn (102px) so với 2 sheet kia (88px).
- `filterMode = Point`, `textureCompression = Uncompressed`, `mipmapEnabled = false`, `alphaIsTransparency = true` (pixel-art sắc nét).
- Cắt theo lưới ở Mục 1; **làm tròn biên ô** (`x0=round(c*W/cols)`, `x1=round((c+1)*W/cols)`, tương tự cho hàng) để lấp kín, không hở/đè; nhớ rect Unity gốc ở **góc dưới-trái** nên phải đảo trục Y.
- Đặt tên sprite `{prefix}_{index:D2}` (prefix: `walk` / `field` / `emote`) để sắp xếp lại đúng thứ tự.
- Sau khi `SetSpriteRects`, **bắt buộc** đăng ký cặp `ISpriteNameFileIdDataProvider.SetNameFileIdPairs(...)` rồi `Apply()` và `SaveAndReimport()`.

---

## 3. ANIMATION CLIPS

Tạo 1 `AnimationClip` cho mỗi clip ở Mục 1. Bind sprite vào `SpriteRenderer.m_Sprite` qua `EditorCurveBinding` + `AnimationUtility.SetObjectReferenceCurve` (path rỗng vì SpriteRenderer cùng GameObject với Animator). Set loop qua `AnimationClipSettings.loopTime`.

| Clip | fps | Loop |
|---|---|---|
| `Walk_Up/Down/Left/Right`, `CarryWalk` | 10 | ✅ loop |
| `Sow`, `Water`, `Tend` | 12 | ❌ một lần |
| `Celebrate` | 12 | ❌ một lần |
| `Wave` | 8 | ✅ loop |
| `Idle` | 6 | ✅ loop |

Lưu clip + controller vào `Assets/Assetsgame/NPC_Game/Generated/`.

---

## 4. ANIMATOR CONTROLLER (`UnityEditor.Animations.AnimatorController`)

**Parameters:** `Speed`(float), `MoveX`(float), `MoveY`(float), `Sow`/`Water`/`Tend`/`Celebrate`(trigger), `Waving`(bool).

**States & transitions:**
- `Idle` (default state) = clip `Idle`.
- `Walk` = **Blend Tree 2D Simple Directional**, blendParameter `MoveX`, blendParameterY `MoveY`, 4 con:
  - `Walk_Down` (0, −1), `Walk_Up` (0, 1), `Walk_Left` (−1, 0), `Walk_Right` (1, 0).
- `Idle → Walk`: `Speed Greater 0.1`, hasExitTime = false, duration ≈ 0.08s.
- `Walk → Idle`: `Speed Less 0.1`, hasExitTime = false, duration ≈ 0.08s.
- One-shot từ **AnyState** (mỗi cái 1 trigger): `Sow`, `Water`, `Tend`, `Celebrate`. Mỗi state này có transition về `Idle` với **hasExitTime = true, exitTime ≈ 0.95**. Đặt `canTransitionToSelf = false`, duration ≈ 0.05s.
- `AnyState → Wave` khi `Waving = true`; `Wave → Idle` khi `Waving = false`.
- **Vì 4 hướng đi đã cầm sẵn bình nước**, dùng luôn Blend Tree `Walk` cho MỌI di chuyển (kể cả lúc đi tưới). Không cần state walk riêng cho lúc cầm bình.

---

## 5. "BỘ NÃO" NPC (script runtime) — tách rời qua Job Board

### 5a. Hệ thống việc (file `FarmingJobSystem.cs`)
- `enum FarmJobType { Plant, Water, Tend, Harvest }`
- `class FarmJob { FarmJobType type; Vector3 worldPos; MonoBehaviour source; Action onArrived; Action onCompleted; }`
- `static class FarmJobBoard` với hàng đợi nội bộ: `Post(FarmJob)`, `bool TryGetNext(out FarmJob)`, `event Action<FarmJob> JobPosted`, `int Count`.
- → Code trồng trọt CỦA TÔI chỉ gọi `FarmJobBoard.Post(...)`. NPC không cần biết gì về tilemap/ô đất.

### 5b. `FarmerNPC.cs` — coroutine FSM
Yêu cầu hành vi đúng theo luồng game của tôi:
1. **Vòng lặp chính:** nếu bảng có việc → nhận; nếu không → đi về `homeAnchor` rồi đứng `Idle` (trông ngóng).
2. **Nhận job →** đi tới `job.worldPos`:
   - Mỗi frame set `Speed=1`, set `MoveX/MoveY` theo vector di chuyển, nhưng **"cardinal hoá"** (chỉ chọn 1 trong 4 hướng theo trục trội) để facing dứt khoát, không lưỡng lự — cho mượt.
   - Tới nơi: `Speed=0`, quay mặt về ô (`FaceTarget`), gọi `job.onArrived`.
3. **Diễn động tác theo loại job:** `Plant→trigger Sow`, `Water→trigger Water`, `Tend→trigger Tend`. Chờ đúng thời lượng clip (serialized field, mặc định ≈ số_frame/fps). Xong gọi `job.onCompleted`.
4. **Harvest (cây chín):** tới ô → `trigger Celebrate` → chờ → `Waving=true` (vẫy tay loop) → đứng chờ tới khi game gọi `npc.OnPlayerHarvested()` → `Waving=false` → về Idle.
5. Public method `void OnPlayerHarvested()`.
- Serialized fields chỉnh được: `moveSpeed`, `arriveDistance`, `homeAnchor`, và thời lượng `sowTime/waterTime/tendTime/celebrateTime`.
- Di chuyển: dùng `Vector3.MoveTowards` trên `transform` (đơn giản, tới đúng ô). **Nếu game tôi có Tilemap/NavMesh2D/A***, hãy tách hàm `MoveTo()` ra để dễ thay bằng pathfinding, và ghi chú rõ chỗ thay.
- Dùng `Animator.StringToHash` cho parameter (hiệu năng).

---

## 6. PREFAB

- Tạo GameObject `FarmerNPC` gồm: `SpriteRenderer` (gán sprite Idle đầu tiên) + `Animator` (gán controller ở Mục 4) + `BoxCollider2D` nhỏ đặt ở chân + `Rigidbody2D` (**Kinematic**, gravityScale 0, freeze Z rotation) + script `FarmerNPC`.
- Lưu prefab vào `Generated/`. Ghi chú để tôi tự set **Sorting Layer / Order** cho khớp game.
- **NẾU tôi đã có prefab/hierarchy NPC sẵn:** đừng tạo mới — chỉ gắn Animator Controller + script `FarmerNPC` vào prefab có sẵn và báo tôi.

---

## 7. MENU EDITOR để chạy

Đặt editor scripts trong folder tên **`Editor`**. Tạo menu:
- `Tools ▶ Farmer NPC ▶ 1. Slice Sheets`
- `Tools ▶ Farmer NPC ▶ 2. Build Animations + Controller (+Prefab)`

Gom cấu hình (tên file, lưới, map frame, fps, loop) vào **1 file config dùng chung** cho cả Slicer lẫn Builder, để sau này sửa 1 chỗ.

---

## 8. ĐIỂM TÍCH HỢP (Cursor tự tìm trong code tôi rồi chèn)

Tìm chỗ xử lý **khi user kéo-thả hạt vào ô đất hợp lệ**. Ngay sau khi xác nhận ô hợp lệ, chèn:

```csharp
FarmJobBoard.Post(new FarmJob(FarmJobType.Plant, plotWorldPos, plot)
{
    onCompleted = () => { /* hiện sprite hạt vừa trồng / set ô = "đã gieo" */ }
});
```

Tương tự cho `Water` (tới lượt tưới), `Tend` (cây lên cấp), `Harvest` (cây chín — nhớ gọi `npc.OnPlayerHarvested()` khi người chơi gặt xong để NPC ngừng vẫy). **Không sửa logic trồng trọt cũ**, chỉ thêm các lời gọi này.

---

## 9. CHECKLIST A→Z (để tôi nghiệm thu)

1. Đổi tên 3 file PNG về ASCII + đảm bảo nền trong suốt.
2. Chạy `Tools ▶ Farmer NPC ▶ 1. Slice Sheets` → 88 sprite con, pivot bottom, PPU theo hàng.
3. Chạy `2. Build Animations + Controller` → đủ clip + controller + (prefab nếu tôi chưa có).
4. Kéo prefab vào scene, set Sorting Layer.
5. Bấm Play, gọi thử `FarmJobBoard.Post(...)` (hoặc kéo hạt) → NPC đi tới ô đúng hướng, gieo, rồi về chỗ đứng Idle.
6. Thử Water / Tend / Harvest (Celebrate → Wave → `OnPlayerHarvested()`).

---

## 10. NHẮC LẠI (quan trọng)

- **Đọc kỹ logic codebase trồng trọt/kéo-thả/ô đất TRƯỚC khi code.** Viết đúng những gì tôi mô tả ở trên.
- Phần NPC phải **tách rời** — chỉ chèn lời gọi `FarmJobBoard.Post()` vào đúng điểm tích hợp, **không phá** hệ thống có sẵn.
- Ưu tiên **mượt + đồng đều**: pivot bottom, PPU theo hàng, blend tree hướng, facing cardinal, transition blend ngắn, fps nhất quán.
- Chỗ nào trong code tôi không rõ ràng để tích hợp → **hỏi lại tôi**, đừng tự bịa.
