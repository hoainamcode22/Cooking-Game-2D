# HANDOFF — Dev B (Khách du lịch lên bờ + 2 Editor tool) · BOAT-002

> Phạm vi: GDD `tourist-boat-system-v2.md` §3.3, §3.4, §3.7-gangplank, §5, §8.
> **Toàn bộ là FILE MỚI (additive). KHÔNG sửa một dòng nào của file có sẵn, cũng không đụng gói Dev A/Dev C.**
> Giao lần 1: 2026-08-29 · **Sửa theo QA + quyết định Sếp/Lead: 2026-08-29 (bản này)**

---

## 0. TÌNH TRẠNG SAU VÒNG SỬA QA

Đã chạy lại đúng 3 pass compile của QA trên gói **A + B + C**:

| Pass | Nội dung | Kết quả |
|---|---|---|
| 1 | 3 gói Dev + source thật + stub, có `UNITY_EDITOR` | **0 error · 0 warning** |
| 2 | Giả lập player build (bỏ `Editor/`) | **0 error · 0 warning** |
| 3 | Mô phỏng merge thật (+ 2 Editor tool V1 của project) | **0 error · 0 warning** |

*(Pass chạy bằng `/home/user/work/qa/compile*.sh` với thư mục `build_verify`. Trong bản build đó có
patch 1 dòng `??=` của Dev C như QA đã ghi — đó là hạn chế `mcs 6.8`, KHÔNG phải lỗi và KHÔNG sửa file giao.)*

### Bảng finding đã xử lý

| Mã | Nội dung | Cách sửa |
|---|---|---|
| **B-1** | Thiếu `TouristQueue` ⇒ tàu kẹt vĩnh viễn | **3 lớp chống kẹt**: ① `EnsureSceneRefs` thiếu queue thì **tự dựng runtime** + `LogError` chỉ dẫn (không đi tiếp với null); ② nghe `OnDockTimeoutForced` của Dev A → khách chưa phục vụ chuyển **tức giận** → về tàu → despawn sau `forcedCleanupSeconds` (2.2s < 3s ân hạn của Dev A) → xoá save + báo rời bến; ③ **watchdog riêng 5s/lần** — chuyến sống quá `patience + 10 phút` thì tự kết thúc. Ngoài ra bubble/đồng hồ **không còn phụ thuộc "đầu hàng"** (xem Sếp chốt 1) nên thiếu queue cũng không làm khách đứng hình. |
| **B-2** | `debugTimeScale` không áp cho patience | Thêm `TouristVisitorManager.EffectiveTimeScale` (tự tính lại y hệt Dev A, kèm guard `Application.isEditor \|\| Debug.isDebugBuild` — **không sửa file Dev A** để lấy API). Chia scale lúc **ĐẶT MỐC** cho: kiên nhẫn (`PatienceSeconds`), giãn cách xuống tàu (`disembarkInterval`), nhịp mở bubble (`bubbleStaggerDelay`), hạn watchdog. Mốc UTC đã persist vẫn đúng sau tắt/mở game. |
| **B-3** | Mất món khi thưởng = 0 / thiếu manager | Đảo thứ tự thành **giao dịch có điều kiện**: ① tính thưởng TRƯỚC → ② `vang <= 0` hoặc thiếu `FarmEconomyManager`/`PlayerProgressManager` ⇒ **HỦY, KHÔNG `RemoveItem`** + hint + `LogError` → ③ mới `RemoveItem` → ④ `RemoveItem` false thì không cộng gì. Thêm **sàn**: `ComputeGold` không bao giờ trả 0 (`sellPrice <= 0` → rơi xuống `BasePriceBook.DefaultBasePrice`, và `Mathf.Max(1, …)` ở mọi đường ra). Không cần `AddItem` hoàn món ⇒ không đụng edge kho đầy. |
| **M-1** | Gangplank sai khi load save đang Docked | Bỏ `ApplyStateInstant` trong `Start`. `Update` giờ: chưa subscribe → thử lại; **manager vừa `IsReady` → chốt trạng thái lần đầu**; sau đó **tự re-sync** mỗi khi `_extended != IsDocked(dock)`. Hết lỗi nhấp nháy theo thứ tự script. |
| **M-2** | Kiên nhẫn nối tiếp ⇒ cận trên 9 tiếng | Xử lý bằng **Sếp chốt 1**: mọi khách đều mở bubble ⇒ 30 phút chạy **song song**. Cận trên rời bến = đi bộ + 30 phút, đúng GDD §3.1. Cộng thêm lưới B-1 ③. |
| **M-3** | Dồn hàng cướp target khách đang đi bộ | `OnQueueSlotChanged` **chỉ đổi target khi khách đã ở khu hàng chờ** (`WaitingServe`/`WalkingToSlot`). Khách đang `Disembarking`/`WalkingPath` chỉ **ghi nhận** slot; tới cuối path `AdvanceAlongPathOrQueue()` tự dùng slot mới nhất. Không còn cảnh quay đầu ngược waypoint. |
| **M-4** | Thưởng cộng cả gia vị | `if (ing.kind == IngredientKind.Seasoning) continue;` + nếu món **toàn gia vị** (`tong == 0`) thì rơi về fallback chứ không trả 0. |
| **m-4** | Placeholder 3 trạng thái trông y hệt nhau | Viết lại `GetPlaceholderFace(FaceKind)`: cache **3 sprite riêng**, vẽ procedural — **món = tròn TRẮNG · mặt cười = VÀNG + miệng cười · mặt tức giận = ĐỎ + miệng cau + 2 lông mày chéo**. Nghiệm thu AC §8.2/§8.5 bằng mắt được ngay khi chưa có art. |
| **m-5** | Spam log thiếu sprite mặt cười | `static bool _warnedNoSprite` — đúng 1 lần/phiên. |
| **m-8** | Xoá save trước khi Dev A nhận lệnh | `TryFinishTrip`: gọi `ReportVisitorsAllAboard` **trước**, rồi kiểm `IsDocked(dock)`. Còn Docked = Dev A từ chối ⇒ **GIỮ chuyến** + `PendingReport = true`, watchdog thử lại mỗi 5s. Chỉ xoá save/RAM khi tàu thật sự rời pha Docked. |
| **m-9** | Cứng `FaceCardinal(Vector2.up)` | `FaceTowardQueue()` suy hướng từ vị trí `QueueAnchor` — Sếp kéo hàng chờ kiểu gì khách cũng quay đúng mặt. |
| **m-10** | Unsubscribe theo `Instance` hiện tại | Gangplank cache `_mgr` lúc subscribe, `OnDestroy` gỡ đúng instance đó. |
| **m-11** | Trùng priority menu | Menu của Dev B đã đúng dải Lead giao: **20 / 21 / 22**. |

### Quyết định Sếp/Lead đã thi hành

- **Sếp chốt 1 — bubble mở LẦN LƯỢT HẾT khách.** Mọi khách trong hàng đều có bubble, nở lần lượt cách nhau
  `bubbleStaggerDelay` (mặc định **0.4s**), bắt đầu từ người tới hàng trước (= đứng đầu). Đồng hồ kiên nhẫn
  30 phút chạy **song song**, tính từ lúc bubble của CHÍNH khách đó mở. **Khách nào cũng tap giao được**,
  không bắt buộc theo thứ tự hàng.
  → Cơ chế: `TouristVisitorManager.TakeBubbleStaggerDelay()` cấp lượt chung cho toàn hệ; agent chờ đúng
  lượt của mình rồi mới `ShowRequest`.
- **Sếp chốt 2 — hết kiên nhẫn hiện MẶT TỨC GIẬN.** `BubbleState.Sad` → **`BubbleState.Angry`**,
  `sadSprite` → **`angryFaceSprite`**, `ShowSad()` → **`ShowAngry()`**, `AgentState.Sad` → **`AgentState.Angry`**.
  Placeholder khi chưa có art là mặt **đỏ cau mày** vẽ procedural.
- **Mission event: GIỮ TẮT** (`banMissionEvent = false`) — ô tick vẫn còn trên Inspector.
- **Hàng chờ: MỘT hàng chung cho cả 3 bến** — giữ nguyên.
- **Lệch làn khách về:** ĐÃ LÀM (không để lại polish). Khách `WalkingBack` đi trên waypoint đã dịch
  vuông góc `walkBackLaneOffset` = **26 unit** (chỉnh trên prefab, đặt 0 để tắt).
- **Gangplank 4 frame:** frame 1 = rút hết → frame 4 = bắc xong. Code play xuôi khi bắc, ngược khi rút.

---

## 1. File đã giao

Đường dẫn là **đường dẫn tương đối trong dự án** — copy nguyên cây từ `deliver/devB/Assets/...` đè vào `Assets/...`.

| # | File | Vai trò |
|---|---|---|
| 1 | `Assets/_Game/Farm/Scripts/TouristBoat/Visitors/TouristVisitorManager.cs` | Singleton trung tâm: dựng/khôi phục chuyến, điều phối nhịp bubble, giao món + thưởng, 3 lớp chống kẹt tàu, persist `TouristTrip_{dock}`. |
| 2 | `.../Visitors/TouristAgent.cs` | Máy trạng thái 1 khách (Disembark → WalkPath → QueueSlot → WaitServe → Served/TimedOut → WalkBack → Board). |
| 3 | `.../Visitors/TouristQueue.cs` | Hàng chờ: slot trống nhỏ nhất, dồn hàng khi có người rời. |
| 4 | `.../Visitors/TouristRequestBubble.cs` | Bubble world-space: Requesting / Happy / **Angry**, scale-in ease out-back, placeholder procedural 3 màu. |
| 5 | `.../Visitors/TouristSmileyFlyFX.cs` | FX mặt cười bay lên HUD **và** `TouristRewardCalculator` (công thức thưởng §3.4, lọc gia vị, sàn chống 0). |
| 6 | `.../Visitors/GangplankController.cs` | Tấm gỗ bắc/rút theo event, tự re-sync trạng thái, art frame hoặc placeholder scale-X. |
| 7 | `Assets/_Game/Farm/Editor/NPCAnimationSetupTool.cs` | Menu **Setup NPC Animations** (prio 20): 132 ảnh → 88 clip → 11 controller → 11 prefab. |
| 8 | `Assets/_Game/Farm/Editor/TouristVisitorSetupTool.cs` | Menu **Setup Tourist Visitors (Scene)** (prio 21) + **Xóa** (prio 22): dựng `TouristSystem`, gangplank, đường đi, hàng chờ, wire toàn bộ reference. |

---

## 2. Hai tool làm hộ những gì

### `Setup NPC Animations`
- Quét `Assets/NV_NPC/NVGAME/Processed/NV01..NV11/` (`NVxx_{down|left|right|up}_{1|2|3}.png`, 256px, nền trong, chân chạm đáy).
- Đặt lại TextureImporter từng file: **Sprite (Single) · pivot Bottom-Center · PPU 100 · mipmap OFF · alphaIsTransparency · Compressed**.
- Sinh **4 clip đi** (3 frame ping-pong `1-2-3-2` @ 8fps, loop) + **4 clip đứng** (frame 2 mỗi hướng) → `Assets/_Game/Farm/Animations/Tourists/NVxx/`.
- Sinh **AnimatorController** 8 state + transition từ AnyState theo `DirX`/`DirY` (float), `IsMoving` (bool).
  *4-state + AnyState thay vì BlendTree 2D vì `TouristAgent` đã snap hướng về 4 hướng chính — đơn giản, chắc chạy.*
- Sinh prefab `Assets/_Game/Farm/Prefabs/Tourists/Tourist_NV01..11.prefab` (SpriteRenderer + Animator + SortingGroup + BoxCollider2D tap + `TouristAgent` + `TouristRequestBubble`), scale theo hằng `TouristWorldHeight = 170` unit world.
- **Idempotent**: clip/controller ghi đè nội dung (không mất tham chiếu), prefab **cập nhật tại chỗ** (giữ mọi chỉnh tay).

### `Setup Tourist Visitors (Scene)`
- Tìm `BoatSystem/Dock_01..03`. Không có → dừng, báo chạy tool V1 trước.
- Tạo (find-or-create, **không phá vị trí đã kéo**): `TouristSystem` + `TouristVisitorManager` + `Visitors`;
  `BoatSystem/Dock_0X/Gangplank` (dò sprite `wood`/`plank`/`khunggo`/`tamgo`/`cauvan`, không có thì placeholder nâu) + `GangplankController`;
  `TouristPath_Dock01/02/03` mỗi cái **4 WP**; `QueueAnchor` + `TouristQueue` cạnh object tên chứa `Cooking`/`NhaHang`/`Restaurant`/`Bep`/`Kitchen`.
- **Wire hộ** (chỉ điền field trống): `config`, `touristPrefabs` (11), `dishDatabase` (quét `t:DishData`), `queue`, `visitorsRoot`, `dockPathRoots[3]`, `gangplanks[3]`.
- Undo toàn bộ (Ctrl+Z), kết thúc bằng dialog report + ping.

---

## 3. Sếp cần làm gì trong Unity (theo thứ tự)

1. **Copy `devA/` TRƯỚC**, rồi `devB/`, `devC/`. Đợi Console 0 lỗi đỏ.
2. Mở `TouristBoatConfig.asset` → điền 12 field mới theo HANDOFF Dev A.
3. Chạy `Tools/Farm Game/Tourist Boat/Setup NPC Animations` → dialog báo **11/11**.
4. Chạy `Tools/Farm Game/Tourist Boat/Setup Tourist Visitors (Scene)` trong `SCN_Farm`.
5. **KÉO WP theo đường đất (REVIEW — bắt buộc):** `TouristSystem/TouristPath_Dock01..03`, kéo `WP_01..WP_04` bám đường đất Sếp đã vẽ. Tool chỉ đặt được đường THẲNG.
6. **Kéo `QueueAnchor`** ra đúng chỗ khách đầu hàng đứng trước cửa nhà hàng. Hướng nối dài hàng: field `queueDirection` của `TouristQueue` (gizmo vàng hiện 6 slot trong Scene view). Khách tự quay mặt về phía anchor nên kéo kiểu gì cũng đúng hướng nhìn.
7. **Canh `Gangplank`** từng bến cho tấm gỗ nối đúng mạn tàu ↔ bờ.
8. **Test ca hết kiên nhẫn (AC §8.5):** đặt `debugTimeScale = 60` trong `TouristBoatConfig` → 30 phút kiên nhẫn ≈ **30 giây thực**, và lịch tàu cũng nhanh tương ứng.
   *(Bản trước hướng dẫn hạ `patienceMinutes` xuống 0.5 — CÁCH ĐÓ ĐÃ BỎ: `OnValidate` của Dev A kẹp sàn 1 phút, và giờ `debugTimeScale` đã ăn cho patience nên không cần nữa.)*
9. Muốn thử lưới an toàn: đặt `maxDockMinutes` nhỏ (vd 1) → xem khách chuyển **mặt tức giận** rồi về tàu, tàu rời bến.
10. **Cỡ khách** sai thì sửa hằng `TouristWorldHeight` trong `NPCAnimationSetupTool.cs` rồi chạy lại tool (tool không đè scale prefab đã chỉnh tay).
11. **Khách bị decor che** thì chỉnh trên prefab: `TouristAgent.sortingLayerName` (mặc định `CongTrinh`) / `baseSortingOrder` (mặc định 5000).
12. Ctrl+S lưu scene.

---

## 4. Khớp contract Dev A

| Dùng gì | Ở đâu |
|---|---|
| `OnBoatDocked` | dựng/khôi phục chuyến · gangplank bắc gỗ |
| `OnBoatDeparting` | dọn chuyến sót · rút gỗ |
| `OnNextTripScheduled(dock, arrivalUtc, gap)` | lưu `arrivalUtc` làm **seed random** của chuyến kế (GDD §4) |
| `OnDockTimeoutForced` | **(mới)** ép khách tức giận về tàu + dọn save trong 2.2s ân hạn |
| `IsDocked(dock)` | quét khôi phục lúc boot · gangplank re-sync · xác nhận Dev A đã nhận lệnh rời bến (m-8) |
| `IsReady` | chờ trong `BootRoutine` và trong gangplank trước khi chốt trạng thái (M-1) |
| `UnlockedDockCount` | log tổng kết lúc boot |
| `ReportVisitorsAllAboard(dock)` | khách cuối lên tàu · khôi phục thấy mọi khách đã xong · lưới an toàn |
| `Config` | fallback khi field `config` trên manager trống; đọc `PatienceSeconds`, `debugTimeScale`, … |
| `BoatNumber(dock)` | log "Tàu số 0X" |
| `GetDockBerth(dock)` (API V1) | điểm khách lên/xuống tàu |

**Lưu ý contract:** `OnBoatDocked` không bắn lại khi load → `BootRoutine` chờ `IsReady` rồi **quét `IsDocked(i)` cả 3 bến**;
có save → khôi phục · không save mà tàu đang đậu → dựng chuyến mới · không đậu mà còn save → **xoá key** (chống hồi sinh khách chuyến cũ).

---

## 5. Giả định (cần xác nhận nếu sai)

1. **`IngredientData.id` == itemId kho == khoá tra giá.** `CookingItemConsumer` không có trong source được cấp
   nên không xác minh trực tiếp; căn cứ: kho và `BasePriceBook` đều normalize lowercase, bảng dự phòng của
   `BasePriceBook` dùng đúng dạng id đó. Sai thì chỉ rơi vào fallback thưởng + warning, **không** hỏng kho.
2. **`bubbleStaggerDelay` (0.4s) nằm trên `TouristVisitorManager`, KHÔNG nằm trong `TouristBoatConfig`** —
   vì file config thuộc gói Dev A và luật của em là không sửa file người khác. Lead muốn gom về config thì
   Dev A thêm 1 field, em đổi đúng 1 dòng trong `TakeBubbleStaggerDelay()`.
3. **`visitorWalkSpeed = 150` / `queueSpacing = 120`** vẫn là số phỏng đoán theo scale map (bến cách ~740 unit,
   khách cao ~170) — cần canh bằng mắt trong scene thật.
4. **Thời lượng cảm xúc (mặt cười 0.5s, mặt tức giận 2s) cố ý KHÔNG chia `debugTimeScale`** — chúng là nhịp
   animation, không phải countdown gameplay; cùng lý do Dev A giữ `ForcedDepartGraceSeconds` ở giây thực.
5. **Sorting layer `CongTrinh`**, `baseSortingOrder = 5000`, Y-sort **kẹp ±8000** (công thức `-y*50` kiểu
   LivestockAI sẽ tràn giới hạn ±32767 với toạ độ map lớn).
6. Hệ khách chỉ chạy ở scene farm (GDD §5 edge 6) — đúng thiết kế, không cần code thêm.

---

## 6. Rủi ro còn lại

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Chưa merge Dev A → thiếu field config V2 ⇒ không compile | Cao | **Merge Dev A trước Dev B.** Đã verify 3 pass compile với gói A thật. |
| Đường đi bộ chưa kéo → khách đi xuyên nhà/nước | Trung bình | Tool sinh WP mặc định + report ghi REVIEW; logic vẫn đúng. |
| 18 khách cùng một hàng ⇒ hàng dài ~2.100 unit | Trung bình | Đúng quyết định "1 hàng chung". Thấy dài quá thì giảm `queueSpacing` hoặc chuyển 3 hàng riêng (việc nhỏ). |
| Chưa có art bubble/mặt | Thấp | Placeholder procedural 3 màu phân biệt rõ, cảnh báo đúng 1 lần. |
| `dishSprite` của vài `DishData` trống | Thấp | Fallback tròn trắng, không NRE. |
| Save `TouristTrip_{dock}` hỏng/đời cũ | Thấp | try-catch + kiểm độ dài mảng khớp nhau, lệch thì vứt và dựng chuyến mới. |
| 18 khách đồng thời (AC §8.7) | Thấp | SpriteRenderer + Animator thường, `MoveTowards` transform, không physics; alloc chỉ ở FX mặt cười (1 lần/khách). |
| Watchdog 5s/lần quét 3 bến | Không đáng kể | Vòng lặp 3 phần tử, `WaitForSeconds` tái dùng. |

---

## 7. Còn tồn / câu hỏi mở

1. **`visitorWalkSpeed` / `queueSpacing`** — cần Sếp cho số chuẩn sau khi nhìn scene thật (hiện 150 / 120).
2. **m-1 (của Dev A):** `OnValidate` kẹp `patienceMinutes ≥ 1`. Sau khi B-2 xong thì **không còn cản trở test**
   (đã chuyển sang `debugTimeScale`), nên em đề nghị **giữ nguyên sàn 1 phút** — hạ xuống 0.05 chỉ tạo rủi ro
   ai đó lỡ tay để số bé trên bản release. Lead chốt hộ.
3. **m-2 (ngoài phạm vi 3 Dev):** menu "8. Xóa Save Tàu" của `TouristBoatDiagnosticTool` (file CŨ) **chưa xoá**
   key `TouristTrip_0/1/2` của em. Ai sửa file đó thì thêm 3 key này, không thì QA "reset save" không sạch.
   Em **không tự sửa** vì đó là file có sẵn của project.
4. **Mission event** vẫn tắt — bật bằng 1 ô tick khi Sếp muốn. Nếu muốn tách riêng thống kê "phục vụ khách"
   thì phải thêm `MissionEventType` mới (đụng file có sẵn → cần Lead duyệt).
5. **Bubble của 18 khách cùng hiện** có thể hơi rối mắt ở mức zoom xa. Nếu Sếp thấy rối, chỉnh nhanh bằng
   cách giảm `frameWorldSize`/`iconWorldSize` trên prefab, hoặc giới hạn N bubble đầu (cần Sếp chốt lại).

---

## 8. Xác nhận không đụng file có sẵn

Không sửa: `BoatDockManager`, `TouristBoatConfig`, `TouristBoatController`, `BoatScheduleCore`,
`TouristBoatSetupTool`, `TouristBoatDiagnosticTool`, `CookingChallengeManager`, `FarmInventoryManager`,
`FarmEconomyManager`, `PlayerProgressManager`, `MissionData`, `MissionProgressTracker`, `FarmUIManager`,
`LivestockAI`, `BasePriceBook` — và **không đụng gói Dev A / Dev C**.

Luồng cooking giữ nguyên: hệ khách chỉ `HasItem` → `RemoveItem` (chỉ trừ, không cộng ⇒ không chạm edge kho đầy §5.3).
