# CHẨN ĐOÁN — VÌ SAO PHÁO HOA KHÁNH THÀNH NHỎ / NẰM SAU CÔNG TRÌNH / KHÔNG CAO

> VFX/Gameplay Programmer · 2026-09-01
> Phạm vi đọc: `ConstructionManager.cs` (852 dòng), `ConstructionSiteUI.cs`, `GiftBoxReveal.cs`,
> `RisingBalloon.cs`, `FloatingNumber.cs`, `FxEase.cs`, `MillCelebrationFX.cs`,
> `LevelUpPopupUI.cs` (nguồn prefab confetti bị mượn), `ConstructionArtKitWindow.cs`,
> `production/TEAM_PLACEMENT_CONSTRUCTION.md`, `production/PHAN_TICH_TOWNSHIP_ANIMATION.md`.
>
> ⚠ **Một giới hạn cần khai thật:** file `ConstructionCompleteFX.cs` (nơi Instantiate prefab
> pháo hoa) **không có trong bản source được giao** (thư mục `Scripts/Gameplay/` chỉ còn
> `ConstructionManager.cs`, `ConstructionSiteUI.cs`, `HarvestFeedbackSpawner.cs`). Kết luận
> số 1 dưới đây vì vậy dẫn chứng từ **điểm gọi** + **cách LevelUpPopupUI xử lý đúng cùng một
> prefab** + **hành vi Sếp quan sát được**; các kết luận còn lại dẫn số dòng trực tiếp.

---

## TÓM TẮT 1 DÒNG

Pháo hoa hiện tại là **prefab ParticleSystem demo của Lana Studio bị mượn qua reflection**,
spawn ở **tâm-chân công trình**, phóng to bằng **transform scale** (thứ ParticleSystem gần như
bỏ qua), và **không ai ghi đè sorting của ParticleSystemRenderer** — nên hạt vừa bé, vừa thấp,
vừa bị chính công trình (layer `Objects`) vẽ đè lên.

---

## NGUYÊN NHÂN 1 — NẰM SAU CÔNG TRÌNH: sorting của ParticleSystemRenderer không được ghi đè

**Chuỗi bằng chứng:**

1. Điểm gọi hiệu ứng cũ — `ConstructionManager.cs:558-561`:
   ```csharp
   ConstructionCompleteFX.Play(
       center, size, SiteSortingLayerName, SiteBaseOrder + 60,   // ← "ObjectsFront", 570
       ResolveCompleteVfxPrefab(), completeVfxScale, artKit,
       () => SpawnFinishedBuilding(site, data, anchor, rot));
   ```
   Chỉ **MỘT** cặp (layer, order) được truyền vào: `SiteSortingLayerName` resolve theo
   `ConstructionManager.cs:133-141` + `:154-165` → `"CongTrinh"` **không tồn tại** trong
   TagManager (xác nhận ở `TEAM_PLACEMENT_CONSTRUCTION.md:893` — project chỉ có
   `Bottom · Default · Objects · ObjectsFront · Foreground`) → rơi về `"ObjectsFront"`,
   order = `SiteBaseOrder + 60` = 510 + 60 = **570** (`ConstructionManager.cs:128`).
   Cặp này đủ cho **mảnh hộp quà/ruy băng mà FX tự dựng** — nhưng prefab pháo hoa là chuyện khác:

2. Prefab pháo hoa là hàng mượn — `ConstructionManager.cs:600-612`
   (`ResolveCompleteVfxPrefab`): reflection vào field private `vfxConfettiPrefab` của
   `LevelUpPopupUI` để lấy `LevelUp_Confetti_Lana02`.

3. **Chính LevelUpPopupUI khi dùng prefab này phải ÉP LẠI sorting cho TỪNG
   ParticleSystemRenderer** — `LevelUpPopupUI.cs:794-800`:
   ```csharp
   foreach (ParticleSystemRenderer particleRenderer in
            instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
   {
       particleRenderer.sortingLayerName = "Foreground";
       particleRenderer.sortingOrder = 1000 + sortingOffset++;
   }
   ```
   Nếu sorting serialize sẵn trong prefab mà dùng được thì đoạn này không cần tồn tại.
   Tức là prefab Lana ship với sorting mặc định (**layer `Default`, order 0** — chuẩn của
   asset demo).

4. Ghép lại: `Default` nằm **DƯỚI** `Objects` trong bảng layer của project
   (`TEAM_PLACEMENT_CONSTRUCTION.md:893`), còn công trình xây xong được
   `RegisterCompletedBuilding` đặt ở `Objects`. Hạt confetti nằm `Default/0` ⇒ **vẽ sau
   toàn bộ công trình, sau cả cây cối decor** — đúng hệt triệu chứng "pháo hoa nằm SAU,
   không rõ". Việc hộp quà/bóng bay (sprite FX tự dựng, ăn cặp `ObjectsFront/570`) vẫn
   nhìn thấy còn pháo hoa thì không, càng khớp với chẩn đoán này.

5. Đội từng đâm phải đúng hố này ở popup máy xay và ghi lại thành văn bản —
   `MillCelebrationFX.cs:17-36` ("VÌ SAO KHÔNG Instantiate THẲNG PREFAB ParticleSystem CỦA
   LANA", lý do số 1 = **THỨ TỰ VẼ**). Bài học đã có, nhưng nhánh khánh thành công trình
   chưa áp dụng.

> Lưu ý thêm: kể cả khi FX có copy cặp `ObjectsFront/570` vào particle renderer thì vẫn còn
> rủi ro — 570 là **số cứng**, không đọc từ sortingOrder thật của công trình (Y-sort/prefab
> có thể đặt order cao hơn). V2 chữa tận gốc: đọc `max(sortingOrder)` của chính công trình
> rồi cộng 100.

## NGUYÊN NHÂN 2 — NHỎ: phóng to bằng transform scale, ParticleSystem không nghe theo

- `ConstructionManager.cs:69-70`:
  ```csharp
  [Tooltip("VFX của Lana Studio dựng cho world unit nhỏ; map này 1 ô = 100 unit nên phải phóng to.")]
  [SerializeField] private float completeVfxScale = 40f;
  ```
  Chính tooltip thừa nhận prefab dựng cho world unit nhỏ. Nhưng ×40 vào `transform.localScale`
  chỉ có tác dụng khi `ParticleSystem.main.scalingMode = Hierarchy`; prefab demo của Lana để
  `Local`/`Shape` thì **startSize, startSpeed, gravity giữ nguyên đơn vị bé** — chỉ có vùng
  phát (shape) to ra, hạt vẫn li ti.
- Vì sao cùng prefab mà ở popup Level-Up nhìn ổn? `LevelUpPopupUI.cs:751-765`: popup spawn hạt
  ở `nearClipPlane + 1` **sát ống kính camera** và scale theo tỉ lệ
  `orthographicSize / 15.09`, nên vài world unit cũng phủ kín màn hình. Còn khánh thành công
  trình spawn **giữa map**, nơi 1 ô đất = 100 unit và cái chuồng bò rộng 694×446 unit
  (`TEAM_PLACEMENT_CONSTRUCTION.md:210-218`) — hạt cỡ vài unit gần như tàng hình.

## NGUYÊN NHÂN 3 — THẤP + BỊ THÂN NHÀ CHE: spawn ở TÂM vùng ô, cao độ mặt đất

- `ConstructionManager.cs:552`: `Vector3 center = site.CenterWorld;` — là **tâm khối ô ở cao
  độ mặt đất** (comment dòng 557: "VFX phủ đúng vùng ô nên phải nhận TÂM"), không phải đỉnh
  công trình.
- Nhà của project cao 380–560 unit (bảng đo `TEAM_PLACEMENT_CONSTRUCTION.md:210-218`), pivot
  ở chân → điểm nổ nằm **ngay giữa thân nhà**. Vận tốc bay lên của hạt lại là đơn vị nhỏ của
  prefab (Nguyên nhân 2) nên không bao giờ vượt nóc → "không cao".
- Tệ hơn: đúng giữa chuỗi, callback `SpawnFinishedBuilding` (dòng 561) dựng công trình thật
  **đè lên điểm nổ** — hạt đã sau layer (Nguyên nhân 1) lại thêm cả khối nhà án ngữ trước mặt.

## NGUYÊN NHÂN 4 — KHÔNG RÕ / LÚC CÓ LÚC KHÔNG: dây reflection dễ đứt, không có nhịp so le

- `ConstructionManager.cs:600-612`: nếu Edric chưa gán `completeVfxPrefab` **và** scene không
  có `LevelUpPopupUI` (hoặc field bị đổi tên) thì `ResolveCompleteVfxPrefab()` trả `null`
  **im lặng** → hoàn toàn không có pháo hoa. `MillCelebrationFX.cs:28-30` gọi thẳng đây là
  "một sợi dây rất dễ đứt".
- Prefab chỉ nổ **một cụm một lần**, không có 3–4 đợt so le, không khớp nhịp
  "NHIỀU đợt confetti nổ SO LE" của Township (`PHAN_TICH_TOWNSHIP_ANIMATION.md` Phần 2C, §4.2–4.4).
- Màu sắc của prefab Lana không ăn theo palette game (vàng `#D9A441` / burgundy `#8E1F3B`).

---

## VÌ SAO V2 CHỮA ĐƯỢC TỪNG BỆNH

| Bệnh | Gốc rễ (dòng) | V2 xử lý |
|---|---|---|
| Sau công trình | sorting prefab không bị ghi đè (`LevelUpPopupUI.cs:794-800` là chứng cứ ngược) | KHÔNG dùng ParticleSystem prefab. Mọi mảnh là SpriteRenderer, layer đọc từ chính công trình, `order = maxOrder con + 100` (fallback `Default`/5000) |
| Nhỏ | ×40 transform scale (`ConstructionManager.cs:70`) không vào startSize/Speed | Kích thước/vận tốc viết thẳng bằng **world unit** (mảnh 12–42 unit, vận tốc 220–420 unit/s) |
| Thấp | spawn tại `site.CenterWorld` (`:552`) | Poof ở **chân**, sao EXP ở **đỉnh**, confetti quanh **nửa trên** (đọc `SpriteRenderer.bounds` tổng), bóng bay `RisingBalloon` bay ~550 unit |
| Lúc có lúc không | reflection mượn prefab (`:600-612`) | Không phụ thuộc prefab/scene — sprite vẽ runtime, null-guard mọi bước |
| Không "đã" | 1 cụm 1 lần | 4 đợt confetti SO LE (0.2/0.65/1.1/1.5s) + poof + sao EXP + bóng bay, tổng ≤ 3.5s |
