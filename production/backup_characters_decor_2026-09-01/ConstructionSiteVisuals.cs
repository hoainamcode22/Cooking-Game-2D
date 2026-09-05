using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// DỰNG HIỆN VẬT CÔNG TRƯỜNG BẰNG CODE (N2): thảm đất · giàn giáo gỗ · công nhân · khói bụi.
///
/// MỌI MẢNH ĐỀU ĐI QUA <see cref="ConstructionArtKit.ResolveSafe"/>:
///   • ô art còn TRỐNG → hình vẽ thủ tục (<see cref="ConstructionSpriteFactory"/>)
///     được TÔ ĐÚNG MÀU NHẬN DẠNG của ô đó → nhìn màu là biết chỗ nào cần art.
///   • ô art đã GÁN    → sprite thật, không tint.
/// Kit được phép null (chưa ai kéo asset vào ConstructionManager) — khi đó tất cả là
/// placeholder có màu, game vẫn chạy y như trước.
///
/// ĐƠN VỊ: mọi con số ở đây là WORLD UNIT, với `PlacementManager.CELL = 100`.
/// Giàn giáo LUÔN phủ đúng `gridSize.x * CELL  ×  gridSize.y * CELL` (yêu cầu §3.5 doc đội)
/// — cùng con số Ghost dùng cho thảm xanh, nên giàn giáo khít đúng vùng vừa đặt.
/// </summary>
public static class ConstructionSiteVisuals
{
    /// <summary>Kết quả dựng — ConstructionSite giữ để nhấp nhô công nhân và tắt khi xây xong.</summary>
    public class Handle
    {
        public GameObject      Root;
        public ParticleSystem  Dust;
        public readonly List<Transform> Workers = new List<Transform>();
        public float           GroundY;   // mép dưới công trường (world Y, tương đối tâm)
    }

    private const float PostWidth   = 16f;   // bề dày cọc đứng
    private const float RailHeight  = 14f;   // bề dày thanh ngang

    /// <summary>Cỡ chữ nhãn tên ô, tính bằng WORLD UNIT (1 ô lưới = 100) — xem AttachSlotLabel.</summary>
    private const float LabelFontSize = 22f;

    /// <summary>Nhãn phải nằm trên tất cả — cao hơn cả canvas UI công trường (30000).</summary>
    private const int   LabelOrder    = 32000;

    // Material hạt bụi, cache THEO TEXTURE: ô "Hạt bụi" của kit có thể đổi texture,
    // dùng chung một material tĩnh như trước sẽ khiến mọi công trường dính texture đầu tiên.
    private static readonly Dictionary<Texture, Material> DustMaterials =
        new Dictionary<Texture, Material>();

    // ─────────────────────────────────────────────────────────────────────────

    public static Handle Build(Transform parent, Vector2Int gridSize, Sprite legacyWorkerSprite,
                               string sortingLayer, int baseOrder, ConstructionArtKit kit = null)
    {
        float cell = PlacementManager.CELL;
        float w = Mathf.Max(1, gridSize.x) * cell;
        float h = Mathf.Max(1, gridSize.y) * cell;

        var handle = new Handle { GroundY = -h * 0.5f };

        GameObject root = new GameObject("Construction_Visuals");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        handle.Root = root;

        // ── 1. THẢM ĐẤT ──────────────────────────────────────────────────────
        // Nền tối phủ đúng footprint để mắt thấy ngay "chỗ này đã bị chiếm".
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.GroundPatch,
            ConstructionSpriteFactory.Panel(96, 96, 20), out Sprite groundSpr, out Color groundCol);

        SpriteRenderer ground = NewSprite(root.transform, "Ground_Patch",
            groundSpr, groundCol, sortingLayer, baseOrder);
        Fit(ground, w * 0.97f, h * 0.97f, Vector2.zero);
        AttachSlotLabel(ground.transform, ConstructionArtKit.Slot.GroundPatch, kit);

        // ── 2. GIÀN GIÁO ─────────────────────────────────────────────────────
        // Cao bằng ~72 % chiều sâu ô nhưng kẹp trong [80, 260] để nhà 2 ô và
        // chuồng 5 ô đều ra dáng giàn giáo, không thành cái que hay bức tường.
        float postH = Mathf.Clamp(h * 0.72f, 80f, 260f);
        float baseY = -h * 0.5f;

        Sprite plank = ConstructionSpriteFactory.Plank();

        // ── 2a. CỌC ĐỨNG ─────────────────────────────────────────────────────
        bool postIsArt = ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.ScaffoldPost,
            plank, out Sprite postSpr, out Color postCol);

        // Ván thủ tục vẽ NẰM NGANG nên phải xoay 90° mới thành cọc. Art thật của Edric
        // theo tooltip đã là "sprite dọc" → dựng thẳng, không xoay, và giữ tỉ lệ gốc.
        float postW = postIsArt
            ? Mathf.Clamp(postH * AspectOf(postSpr, 0.14f), PostWidth, w * 0.35f)
            : PostWidth;

        // Số cọc: công trình càng rộng càng nhiều cọc (1 cọc mỗi ~120 unit, tối thiểu 2).
        int postCount = Mathf.Clamp(Mathf.RoundToInt(w / 120f) + 1, 2, 6);
        // Art cọc có thể rất bè → kẹp lại, nếu không `usable` âm và các cọc lộn ngược thứ tự.
        float usable = Mathf.Max(40f, w - postW * 2f - 24f);
        for (int i = 0; i < postCount; i++)
        {
            float t = postCount == 1 ? 0.5f : i / (float)(postCount - 1);
            float x = -usable * 0.5f + usable * t;
            SpriteRenderer post = NewSprite(root.transform, $"Scaffold_Post_{i + 1}",
                postSpr, postCol, sortingLayer, baseOrder + 2);

            if (postIsArt)
            {
                Fit(post, postW, postH, new Vector2(x, baseY + postH * 0.5f));
            }
            else
            {
                Fit(post, postH, postW, new Vector2(x, baseY + postH * 0.5f));
                post.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Chỉ gắn nhãn cho cọc ĐẦU TIÊN — 6 cái nhãn giống nhau chỉ làm rối mắt.
            if (i == 0) AttachSlotLabel(post.transform, ConstructionArtKit.Slot.ScaffoldPost, kit);
        }

        // ── 2b. THANH NGANG (2 thanh + 1 thanh chân) ─────────────────────────
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.ScaffoldRail,
            plank, out Sprite railSpr, out Color railCol);

        float[] railT = { 0.30f, 0.72f, 0.02f };
        for (int i = 0; i < railT.Length; i++)
        {
            SpriteRenderer rail = NewSprite(root.transform, $"Scaffold_Rail_{i + 1}",
                railSpr, railCol, sortingLayer, baseOrder + 3);
            Fit(rail, w * 0.90f, RailHeight, new Vector2(0f, baseY + postH * railT[i]));

            if (i == 0) AttachSlotLabel(rail.transform, ConstructionArtKit.Slot.ScaffoldRail, kit);
        }

        // ── 2c. THANH CHỐNG CHÉO ─────────────────────────────────────────────
        // Cho giàn giáo trông có kết cấu chứ không phải cái thang.
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.ScaffoldBrace,
            plank, out Sprite braceSpr, out Color braceCol);

        float braceLen = Mathf.Sqrt(w * w * 0.42f * 0.42f + postH * postH * 0.70f * 0.70f);
        float braceAngle = Mathf.Atan2(postH * 0.70f, w * 0.42f) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
        {
            SpriteRenderer brace = NewSprite(root.transform, s < 0 ? "Scaffold_Brace_L" : "Scaffold_Brace_R",
                braceSpr, braceCol, sortingLayer, baseOrder + 1);
            Fit(brace, braceLen, RailHeight * 0.85f, new Vector2(s * w * 0.21f, baseY + postH * 0.36f));
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, s * braceAngle);

            if (s < 0) AttachSlotLabel(brace.transform, ConstructionArtKit.Slot.ScaffoldBrace, kit);
        }

        // ── 2d. VÁN DỰA ──────────────────────────────────────────────────────
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.LeaningBoard,
            plank, out Sprite leanSpr, out Color leanCol);

        SpriteRenderer lean = NewSprite(root.transform, "Scaffold_LeaningBoard",
            leanSpr, leanCol, sortingLayer, baseOrder + 4);
        Fit(lean, postH * 0.85f, PostWidth * 1.1f, new Vector2(-w * 0.34f, baseY + postH * 0.30f));
        lean.transform.localRotation = Quaternion.Euler(0f, 0f, 68f);
        AttachSlotLabel(lean.transform, ConstructionArtKit.Slot.LeaningBoard, kit);

        // ── 3. CÔNG NHÂN ─────────────────────────────────────────────────────
        BuildWorkers(root.transform, w, h, baseY, legacyWorkerSprite, sortingLayer, baseOrder, kit, handle);

        // ── 4. KHÓI BỤI ──────────────────────────────────────────────────────
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.DustParticle,
            ConstructionSpriteFactory.SoftDot(64), out Sprite dustSpr, out Color dustCol);
        bool dustIsArt = kit != null && kit.dustParticle != null;

        handle.Dust = BuildDust(root.transform, w, baseY, sortingLayer, baseOrder + 8,
                                dustSpr, dustCol, dustIsArt);
        if (handle.Dust != null)
            AttachSlotLabel(handle.Dust.transform, ConstructionArtKit.Slot.DustParticle, kit);

        return handle;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CÔNG NHÂN
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildWorkers(Transform root, float w, float h, float baseY,
                                     Sprite legacyWorkerSprite, string sortingLayer, int baseOrder,
                                     ConstructionArtKit kit, Handle handle)
    {
        GameObject prefab = kit != null ? kit.workerPrefab : null;

        // Ô cũ `ConstructionManager.workerSprite` làm dự phòng: kit chưa có công nhân
        // nhưng scene đã gán sprite thật thì phải dùng sprite đó và KHÔNG tô xanh nhận dạng.
        Sprite fallback = legacyWorkerSprite != null
                        ? legacyWorkerSprite
                        : ConstructionSpriteFactory.WorkerSilhouette();

        bool isArt = ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.Worker,
            fallback, out Sprite wSpr, out Color wCol);

        if (!isArt && legacyWorkerSprite != null)
        {
            isArt = true;
            wCol  = (kit != null && kit.forcePlaceholderColors)
                  ? ConstructionArtKit.ColorOf(ConstructionArtKit.Slot.Worker)
                  : Color.white;
        }

        int workerCount = w >= 300f ? 2 : 1;
        float targetH = Mathf.Clamp(h * 0.55f, 70f, 150f);

        for (int i = 0; i < workerCount; i++)
        {
            float x = workerCount == 1 ? -w * 0.18f : (i == 0 ? -w * 0.26f : w * 0.24f);
            Transform workerT;

            if (prefab != null)
            {
                // Prefab công nhân (có Animator, hiệu ứng búa…) thay hẳn SpriteRenderer.
                GameObject inst = Object.Instantiate(prefab, root, false);
                inst.name = $"Worker_{i + 1}_Prefab";
                workerT = inst.transform;

                // Co prefab về đúng chiều cao mong muốn: art của Edric có thể vẽ theo
                // đơn vị bất kỳ, còn map này 1 ô = 100 unit.
                float scale = 1f;
                Renderer rend = inst.GetComponentInChildren<Renderer>();
                if (rend != null && rend.bounds.size.y > 0.0001f)
                    scale = targetH / rend.bounds.size.y;

                workerT.localScale    = new Vector3(i == 1 ? -scale : scale, scale, scale);
                workerT.localPosition = new Vector3(x, baseY + targetH * 0.45f, 0f);

                // Prefab có thể để sorting mặc định → sẽ chui xuống dưới giàn giáo.
                foreach (SpriteRenderer sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = sortingLayer;
                    sr.sortingOrder     = baseOrder + 6;
                }
            }
            else
            {
                SpriteRenderer worker = NewSprite(root,
                    isArt ? $"Worker_{i + 1}" : $"Worker_{i + 1}  ◄ THẢ ART CÔNG NHÂN VÀO ĐÂY",
                    wSpr, wCol, sortingLayer, baseOrder + 6);

                // Giữ tỉ lệ gốc của sprite, cao ~0.9 ô. Nếu Edric gán art thật thì
                // art cao bao nhiêu cũng tự co về đúng chiều cao này.
                float ratio = AspectOf(wSpr, 0.66f);
                Fit(worker, targetH * ratio, targetH, new Vector2(x, baseY + targetH * 0.45f));
                if (i == 1) worker.flipX = true;

                workerT = worker.transform;
            }

            if (i == 0) AttachSlotLabel(workerT, ConstructionArtKit.Slot.Worker, kit);
            handle.Workers.Add(workerT);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NHÃN TÊN Ô — thứ Edric nhìn vào để biết thả art vào đâu
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn nhãn chữ ghi TÊN Ô lên một mảnh placeholder. Chỉ chạy khi bật
    /// `showSlotLabels` trong asset kit → build thật không tốn một byte nào.
    /// Dùng chung cho CẢ mảnh world (SpriteRenderer) lẫn mảnh UI (Image trong Canvas):
    /// tự nhận biết qua việc <paramref name="target"/> có phải RectTransform hay không.
    /// </summary>
    /// <returns>Component chữ vừa tạo (null nếu không bật nhãn) — nơi gọi đổi text sau được.</returns>
    public static TMP_Text AttachSlotLabel(Transform target, ConstructionArtKit.Slot slot,
                                           ConstructionArtKit kit)
    {
        if (target == null || !ConstructionArtKit.WantLabels(kit)) return null;

        string caption = ConstructionArtKit.LabelOf(slot);

        // ── MẢNH UI: nằm trong Canvas, mọi RectTransform đều localScale = 1 nên
        //    làm con trực tiếp là an toàn.
        if (target is RectTransform hostRect)
            return AttachUguiLabel(hostRect, caption);

        return AttachWorldLabel(target, caption);
    }

    private static TMP_Text AttachUguiLabel(RectTransform host, string caption)
    {
        var go = new GameObject($"Nhãn_{caption}", typeof(RectTransform));
        go.transform.SetParent(host, false);
        go.layer = host.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.localScale       = Vector3.one;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        ConfigureLabel(tmp, caption);
        // Thêm sau cùng trong danh sách con ⇒ UGUI vẽ đè lên icon/chữ có sẵn.
        rt.SetAsLastSibling();
        return tmp;
    }

    private static TMP_Text AttachWorldLabel(Transform target, string caption)
    {
        // ⚠ KHÔNG làm con của chính mảnh đó.
        // `Fit()` ép kích thước bằng localScale KHÔNG ĐỀU (vd cọc: 500 × 16) và nhiều mảnh
        // còn bị xoay 90°/68°. Ma trận (xoay × co-giãn-không-đều) sinh ra SHEAR — chữ con sẽ
        // bị kéo xiên, không có cách nào bù lại chỉ bằng localScale. Vì vậy nhãn được đặt
        // NGANG HÀNG với mảnh (cùng cha, cha luôn scale 1) và đứng đúng vị trí của mảnh.
        Transform host = target.parent != null ? target.parent : target;

        // TMP kế thừa Graphic ⇒ BẮT BUỘC có RectTransform. Tạo sẵn để Unity khỏi phải
        // đổi Transform → RectTransform ngầm (chỗ này từng gây lỗi im lặng ở vài bản Unity).
        var go = new GameObject($"Nhãn_{caption}", typeof(RectTransform));
        go.transform.SetParent(host, false);
        go.layer = host.gameObject.layer;
        go.transform.localPosition = target.localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var tmp = go.AddComponent<TextMeshPro>();
        ConfigureLabel(tmp, caption);

        // isOrthographic = true ⇒ TMP bỏ hệ số 0.1 nội bộ, 1 point cỡ chữ = 1 world unit.
        // Nhờ vậy LabelFontSize đọc thẳng được theo thang 1 ô = 100 unit.
        tmp.isOrthographic = true;
        tmp.rectTransform.sizeDelta = new Vector2(320f, 40f);

        // Nổi trên mọi thứ, kể cả canvas UI công trường.
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = ConstructionManager.TopSortingLayerName;
            mr.sortingOrder     = LabelOrder;
        }

        go.AddComponent<ConstructionSlotLabelBillboard>();
        return tmp;
    }

    /// <summary>Phần cấu hình chữ dùng chung cho cả hai loại nhãn.</summary>
    private static void ConfigureLabel(TMP_Text tmp, string caption)
    {
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text             = caption;
        tmp.fontSize         = LabelFontSize;
        tmp.color            = Color.white;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        tmp.raycastTarget    = false;

        // Viền đen: nhãn phải đọc được cả khi nằm trên mảnh sáng lẫn mảnh tối.
        // Bọc null-check vì nếu project chưa có default font asset thì fontMaterial = null
        // và UpdateMeshPadding() sẽ ném NRE — nhãn debug không được phép làm chết game.
        Material mat = tmp.fontMaterial;
        if (mat != null)
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            tmp.outlineColor = new Color(0f, 0f, 0f, 1f);
            tmp.outlineWidth = 0.30f;
            tmp.UpdateMeshPadding();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tỉ lệ ngang/dọc của sprite, trả <paramref name="fallback"/> nếu không đo được.</summary>
    public static float AspectOf(Sprite sprite, float fallback)
    {
        if (sprite == null) return fallback;
        Vector2 b = sprite.bounds.size;
        return b.y > 0.0001f ? b.x / b.y : fallback;
    }

    /// <summary>Tạo một SpriteRenderer con. Public để ConstructionCompleteFX dùng lại.</summary>
    public static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite,
                                           Color color, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>
    /// Ép một SpriteRenderer về đúng kích thước WORLD mong muốn bằng localScale.
    /// Dùng scale thay vì drawMode = Sliced vì sprite thủ tục có PPU = 100 nên
    /// bounds gốc chỉ vài đơn vị, còn ô lưới của game là 100 unit — chênh ~100 lần.
    /// </summary>
    public static void Fit(SpriteRenderer sr, float worldW, float worldH, Vector2 localPos)
    {
        if (sr == null || sr.sprite == null) return;

        Vector2 b = sr.sprite.bounds.size;
        float sx = b.x > 0.0001f ? worldW / b.x : 1f;
        float sy = b.y > 0.0001f ? worldH / b.y : 1f;

        sr.transform.localScale    = new Vector3(sx, sy, 1f);
        sr.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
    }

    private static ParticleSystem BuildDust(Transform parent, float worldW, float baseY,
                                            string sortingLayer, int order,
                                            Sprite dustSprite, Color dustColor, bool dustIsArt)
    {
        var go = new GameObject("Dust_FX");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, baseY + 12f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration         = 3f;
        main.loop             = true;
        main.playOnAwake      = false;
        main.maxParticles     = 40;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(14f, 34f);
        main.startSize        = new ParticleSystem.MinMaxCurve(14f, 34f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier  = 0f;
        main.simulationSpace  = ParticleSystemSimulationSpace.Local;

        // Có art thật → KHÔNG tint (art đã có màu riêng), chỉ hạ alpha cho nhẹ.
        // Còn placeholder → tô màu nhận dạng của ô "Hạt bụi" (nâu đất).
        main.startColor = dustIsArt
            ? new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.85f))
            : new ParticleSystem.MinMaxGradient(
                  new Color(dustColor.r, dustColor.g, dustColor.b, 0.55f),
                  new Color(dustColor.r * 0.85f, dustColor.g * 0.85f, dustColor.b * 0.85f, 0.40f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 9f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(worldW * 0.66f, 8f, 0.01f);

        // Phun NHẸ LÊN TRÊN + tản ngang ngẫu nhiên (yêu cầu N2).
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-14f, 14f);
        vel.y = new ParticleSystem.MinMaxCurve(18f, 46f);

        // Mờ dần: hiện nhanh rồi tan.
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f,    0f),
                new GradientAlphaKey(1f,    0.22f),
                new GradientAlphaKey(0f,    1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Nở dần ra cho giống bụi bốc.
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.35f));

        var rend = go.GetComponent<ParticleSystemRenderer>();
        if (rend != null)
        {
            rend.renderMode       = ParticleSystemRenderMode.Billboard;
            rend.sortingLayerName = sortingLayer;
            rend.sortingOrder     = order;
            rend.material         = GetDustMaterial(dustSprite);
        }

        ps.Play();
        return ps;
    }

    /// <summary>
    /// Material cho hạt bụi. Dự án chạy URP nên thử shader 2D của URP trước,
    /// rồi mới tới "Sprites/Default" (built-in, vẫn hoạt động dưới URP).
    /// Không tìm được shader nào thì trả null — Unity dùng material mặc định,
    /// hạt có thể ra màu hồng nhưng KHÔNG crash.
    ///
    /// ⚠ Nếu ô "Hạt bụi" của Edric là sprite nằm trong ATLAS thì material sẽ lấy CẢ atlas
    /// (ParticleSystemRenderer không đọc được uv của một sprite lẻ ở chế độ Billboard).
    /// Muốn dùng art riêng thì để sprite đó ở texture độc lập.
    /// </summary>
    private static Material GetDustMaterial(Sprite dot)
    {
        if (dot == null) dot = ConstructionSpriteFactory.SoftDot(64);

        Texture tex = dot != null ? dot.texture : null;
        if (tex == null) return null;

        if (DustMaterials.TryGetValue(tex, out Material cached) && cached != null)
            return cached;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("UI/Default");
        if (sh == null) return null;

        var mat = new Material(sh)
        {
            name        = "Construction_Dust_Mat",
            hideFlags   = HideFlags.HideAndDontSave,
            mainTexture = tex
        };

        DustMaterials[tex] = mat;
        return mat;
    }
}

/// <summary>
/// Giữ nhãn tên ô luôn hướng camera. CHỈ tồn tại ở chế độ dựng nền (showSlotLabels),
/// không bao giờ chạy trong bản build thật.
/// (Class phụ nằm chung file với ConstructionSiteVisuals vì nó là chi tiết nội bộ của nhãn;
///  AddComponent bằng code không yêu cầu tên file trùng tên class.)
/// </summary>
public class ConstructionSlotLabelBillboard : MonoBehaviour
{
    private Camera _cam;

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        transform.rotation = _cam.transform.rotation;
    }
}
