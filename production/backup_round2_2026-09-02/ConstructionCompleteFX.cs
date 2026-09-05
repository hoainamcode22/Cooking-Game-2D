using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HIỆU ỨNG HOÀN THÀNH (N5 — video f_075).
/// Công trình lộ ra từ một "hộp quà": khung trắng + ruy băng hồng, bóng bay đỏ/vàng
/// bay lên, icon mũ bảo hộ kèm dấu tick xanh bật lên trên đầu, rồi hộp mở ra.
///
/// KHÔNG CÓ DOTWEEN trong project (đã kiểm tra Packages/manifest.json) → mọi chuyển
/// động viết bằng Coroutine + hàm easing tay, đúng như các file khác của dự án
/// (`PlacementManager.AnimateGhostActionBar`, `PlacementGhostVisualController.SpawnPop`).
///
/// Dùng Time.unscaledDeltaTime: công trình có thể xây xong ĐÚNG LÚC một popup đang mở
/// và Time.timeScale = 0 — khi đó hiệu ứng vẫn phải chạy chứ không đứng hình.
/// </summary>
public class ConstructionCompleteFX : MonoBehaviour
{
    private const float BoxPopDuration    = 0.26f;
    private const float RevealDelay       = 0.30f;   // sau khi hộp bung mới lộ công trình
    private const float BoxOpenDuration   = 0.34f;
    private const float BalloonRiseTime   = 1.55f;
    private const float LifeTime          = 2.10f;

    private static readonly Color TickGreen = ConstructionSpriteFactory.Hex("#4CC93F");

    private static readonly Color[] BalloonColors =
    {
        ConstructionSpriteFactory.Hex("#E8433C"),
        ConstructionSpriteFactory.Hex("#FFC531"),
        ConstructionSpriteFactory.Hex("#E8433C"),
        ConstructionSpriteFactory.Hex("#FFD966"),
        ConstructionSpriteFactory.Hex("#F0568C"),
        ConstructionSpriteFactory.Hex("#FFC531")
    };

    private readonly List<SpriteRenderer> _boxParts = new List<SpriteRenderer>();
    private Transform _boxRoot;

    /// <summary>Bộ ô art. Được phép null → mọi mảnh là hình vẽ code tô màu nhận dạng.</summary>
    private ConstructionArtKit _kit;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chạy hiệu ứng tại <paramref name="center"/>.
    /// <paramref name="onReveal"/> được gọi ở giữa chuỗi — đó là lúc DEV-2 Instantiate
    /// prefab thật, để công trình "lộ ra từ trong hộp" chứ không bụp ra ngay từ frame 0.
    /// </summary>
    public static ConstructionCompleteFX Play(Vector3 center, Vector2Int gridSize,
                                              string sortingLayer, int baseOrder,
                                              GameObject reusableVfxPrefab, float reusableVfxScale,
                                              ConstructionArtKit artKit,
                                              System.Action onReveal)
    {
        var go = new GameObject("Construction_CompleteFX");
        go.transform.position = center;

        var fx = go.AddComponent<ConstructionCompleteFX>();
        fx._kit = artKit;
        fx.StartCoroutine(fx.Routine(gridSize, sortingLayer, baseOrder,
                                     reusableVfxPrefab, reusableVfxScale, onReveal));
        return fx;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Routine(Vector2Int gridSize, string sortingLayer, int baseOrder,
                                GameObject reusableVfxPrefab, float reusableVfxScale,
                                System.Action onReveal)
    {
        float cell = PlacementManager.CELL;
        float w = Mathf.Max(1, gridSize.x) * cell;
        float h = Mathf.Max(1, gridSize.y) * cell;

        // ── VFX có sẵn trong project (Confetti / Flash của Lana Studio) ──────
        // Chỉ dùng nếu Edric đã gán prefab; không có thì hiệu ứng tự vẽ vẫn đủ.
        if (reusableVfxPrefab != null)
        {
            GameObject vfx = Instantiate(reusableVfxPrefab,
                                         transform.position + new Vector3(0f, h * 0.5f, 0f),
                                         Quaternion.identity);
            vfx.transform.localScale = Vector3.one * Mathf.Max(0.01f, reusableVfxScale);
            Destroy(vfx, 4f);
        }

        BuildGiftBox(w, h, sortingLayer, baseOrder);
        BuildHatBadge(w, h, sortingLayer, baseOrder + 20);
        SpawnBalloons(w, h, sortingLayer, baseOrder + 15);

        // 1. Hộp bung ra (nảy nhẹ quá mốc rồi về)
        yield return ScaleRoutine(_boxRoot, 0.15f, 1f, BoxPopDuration, backOut: true);

        // 2. Chờ một nhịp rồi LỘ CÔNG TRÌNH THẬT
        yield return WaitUnscaled(RevealDelay);
        onReveal?.Invoke();

        // 3. Hộp mở: phóng to + tan dần, để công trình bên dưới hiện ra
        yield return OpenBox(BoxOpenDuration);

        // 4. Đợi bóng bay + huy hiệu diễn xong rồi tự dọn
        yield return WaitUnscaled(LifeTime - BoxPopDuration - RevealDelay - BoxOpenDuration);
        Destroy(gameObject);
    }

    // ── Dựng hình ────────────────────────────────────────────────────────────

    private void BuildGiftBox(float w, float h, string sortingLayer, int order)
    {
        var root = new GameObject("GiftBox");
        root.transform.SetParent(transform, false);
        _boxRoot = root.transform;

        Sprite panel  = ConstructionSpriteFactory.Panel(96, 96, 22);
        Sprite circle = ConstructionSpriteFactory.Circle();

        // 3 ô art của hộp quà. Trống → panel/đĩa tròn thủ tục + màu nhận dạng
        // (trắng ngà / hồng đậm) — vốn đã gần đúng bảng màu Township cũ.
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.GiftBoxSide,
            panel, out Sprite boxSpr, out Color boxCol);
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.Ribbon,
            panel, out Sprite ribSpr, out Color ribCol);
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.Rosette,
            circle, out Sprite rosSpr, out Color rosCol);

        float t = Mathf.Clamp(Mathf.Min(w, h) * 0.06f, 10f, 22f);   // bề dày khung
        float bw = w * 1.02f;
        float bh = h * 1.02f;

        // Khung trắng 4 cạnh
        SpriteRenderer frameTop = Add(boxSpr, boxCol, bw, t, new Vector2(0f, bh * 0.5f),
                                      sortingLayer, order, "Frame_Top");
        Add(boxSpr, boxCol, bw, t, new Vector2(0f, -bh * 0.5f), sortingLayer, order, "Frame_Bottom");
        Add(boxSpr, boxCol, t, bh, new Vector2(-bw * 0.5f, 0f), sortingLayer, order, "Frame_Left");
        Add(boxSpr, boxCol, t, bh, new Vector2( bw * 0.5f, 0f), sortingLayer, order, "Frame_Right");

        // Ruy băng 3 mặt: dọc giữa + ngang giữa
        float rib = Mathf.Clamp(Mathf.Min(w, h) * 0.11f, 18f, 44f);
        SpriteRenderer ribbonV = Add(ribSpr, ribCol, rib, bh, new Vector2(0f, 0f),
                                     sortingLayer, order + 1, "Ribbon_V");
        Add(ribSpr, ribCol, bw, rib, new Vector2(0f, -bh * 0.06f), sortingLayer, order + 1, "Ribbon_H");

        // Nơ trên đỉnh: 2 cánh + nút giữa
        float bowR = rib * 1.25f;
        SpriteRenderer bowLeft = Add(rosSpr, rosCol, bowR * 1.6f, bowR,
                                     new Vector2(-bowR * 0.8f, bh * 0.5f + bowR * 0.35f),
                                     sortingLayer, order + 2, "Bow_Left");
        Add(rosSpr, rosCol, bowR * 1.6f, bowR, new Vector2( bowR * 0.8f, bh * 0.5f + bowR * 0.35f),
            sortingLayer, order + 2, "Bow_Right");
        // Nút giữa nơ tối hơn cánh một chút cho có khối — dẫn xuất từ chính màu ô Rosette
        // để đổi art/đổi màu là nó tự theo.
        Add(rosSpr, Color.Lerp(rosCol, Color.black, 0.22f), bowR * 0.7f, bowR * 0.7f,
            new Vector2(0f, bh * 0.5f + bowR * 0.35f), sortingLayer, order + 3, "Bow_Knot");

        // Mỗi nhóm chỉ gắn MỘT nhãn — 4 cạnh khung 4 nhãn giống nhau chỉ tổ rối.
        if (frameTop != null)
            ConstructionSiteVisuals.AttachSlotLabel(frameTop.transform, ConstructionArtKit.Slot.GiftBoxSide, _kit);
        if (ribbonV != null)
            ConstructionSiteVisuals.AttachSlotLabel(ribbonV.transform, ConstructionArtKit.Slot.Ribbon, _kit);
        if (bowLeft != null)
            ConstructionSiteVisuals.AttachSlotLabel(bowLeft.transform, ConstructionArtKit.Slot.Rosette, _kit);

        _boxRoot.localScale = Vector3.one * 0.15f;
    }

    private SpriteRenderer Add(Sprite sprite, Color color, float w, float h, Vector2 pos,
                               string sortingLayer, int order, string name)
    {
        SpriteRenderer sr = ConstructionSiteVisuals.NewSprite(_boxRoot, name, sprite, color, sortingLayer, order);
        ConstructionSiteVisuals.Fit(sr, w, h, pos);
        _boxParts.Add(sr);
        return sr;
    }

    /// <summary>Mũ bảo hộ + huy hiệu tick xanh bật lên trên đầu (đúng video f_075).</summary>
    private void BuildHatBadge(float w, float h, string sortingLayer, int order)
    {
        var root = new GameObject("HatBadge");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, h * 0.5f + 90f, 0f);
        root.transform.localScale = Vector3.zero;

        float size = Mathf.Clamp(Mathf.Min(w, h) * 0.55f, 80f, 150f);

        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.HardHatDone,
            ConstructionSpriteFactory.HardHat(), out Sprite hatSpr, out Color hatCol);

        SpriteRenderer hat = ConstructionSiteVisuals.NewSprite(root.transform, "Icon_MuBaoHo",
            hatSpr, hatCol, sortingLayer, order);
        ConstructionSiteVisuals.Fit(hat, size, size, Vector2.zero);
        ConstructionSiteVisuals.AttachSlotLabel(hat.transform, ConstructionArtKit.Slot.HardHatDone, _kit);

        SpriteRenderer disc = ConstructionSiteVisuals.NewSprite(root.transform, "Badge_Tron",
            ConstructionSpriteFactory.Circle(), TickGreen, sortingLayer, order + 1);
        ConstructionSiteVisuals.Fit(disc, size * 0.52f, size * 0.52f,
                                    new Vector2(size * 0.32f, -size * 0.28f));

        SpriteRenderer tick = ConstructionSiteVisuals.NewSprite(root.transform, "Badge_Tick",
            ConstructionSpriteFactory.CheckMark(), Color.white, sortingLayer, order + 2);
        ConstructionSiteVisuals.Fit(tick, size * 0.32f, size * 0.32f,
                                    new Vector2(size * 0.32f, -size * 0.28f));

        StartCoroutine(HatRoutine(root.transform));
    }

    private IEnumerator HatRoutine(Transform hatRoot)
    {
        yield return WaitUnscaled(0.18f);
        yield return ScaleRoutine(hatRoot, 0f, 1f, 0.30f, backOut: true);
        yield return WaitUnscaled(0.85f);

        // Bay lên rồi tan
        float elapsed = 0f;
        const float dur = 0.45f;
        Vector3 from = hatRoot.localPosition;
        var renderers = hatRoot.GetComponentsInChildren<SpriteRenderer>(true);

        while (elapsed < dur && hatRoot != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            hatRoot.localPosition = from + new Vector3(0f, k * 60f, 0f);
            SetAlpha(renderers, 1f - k);
            yield return null;
        }

        if (hatRoot != null) hatRoot.gameObject.SetActive(false);
    }

    private void SpawnBalloons(float w, float h, string sortingLayer, int order)
    {
        Sprite panel = ConstructionSpriteFactory.Panel(32, 32, 8);

        // Ô "Bóng bay" TRỐNG  → tất cả bóng tô MÀU NHẬN DẠNG (đỏ) để Edric nhận ra ngay.
        // Ô đã có ART        → mới rải bảng màu đỏ/vàng/hồng cho vui mắt (đúng tooltip của ô).
        bool balloonIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.Balloon,
            ConstructionSpriteFactory.Balloon(), out Sprite balloonSprite, out Color balloonCol);

        for (int i = 0; i < BalloonColors.Length; i++)
        {
            var go = new GameObject($"Balloon_{i + 1}");
            go.transform.SetParent(transform, false);

            float x = Mathf.Lerp(-w * 0.42f, w * 0.42f, (i + 0.5f) / BalloonColors.Length);
            go.transform.localPosition = new Vector3(x, -h * 0.25f, 0f);

            float bw = Mathf.Clamp(Mathf.Min(w, h) * 0.20f, 34f, 62f);

            SpriteRenderer body = ConstructionSiteVisuals.NewSprite(go.transform, "Body",
                balloonSprite, balloonIsArt ? BalloonColors[i] : balloonCol, sortingLayer, order + i);
            ConstructionSiteVisuals.Fit(body, bw, bw * 1.3f, Vector2.zero);

            if (i == 0)
                ConstructionSiteVisuals.AttachSlotLabel(body.transform, ConstructionArtKit.Slot.Balloon, _kit);

            SpriteRenderer str = ConstructionSiteVisuals.NewSprite(go.transform, "String",
                panel, new Color(1f, 1f, 1f, 0.75f), sortingLayer, order + i - 1);
            ConstructionSiteVisuals.Fit(str, 3f, bw * 1.1f, new Vector2(0f, -bw * 1.15f));

            StartCoroutine(BalloonRoutine(go.transform, i));
        }
    }

    private IEnumerator BalloonRoutine(Transform balloon, int index)
    {
        yield return WaitUnscaled(0.10f + index * 0.05f);

        var renderers = balloon.GetComponentsInChildren<SpriteRenderer>(true);
        Vector3 from = balloon.localPosition;
        float rise   = 240f + index * 26f;
        float sway   = 22f + index * 5f;
        float elapsed = 0f;

        while (elapsed < BalloonRiseTime && balloon != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / BalloonRiseTime);

            balloon.localPosition = from + new Vector3(
                Mathf.Sin(k * Mathf.PI * 2.2f + index) * sway,
                Mathf.Sqrt(k) * rise,      // vọt nhanh lúc đầu rồi chậm dần
                0f);
            balloon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 6f + index) * 9f);

            SetAlpha(renderers, k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f);
            yield return null;
        }

        if (balloon != null) balloon.gameObject.SetActive(false);
    }

    /// <summary>Hộp "mở": phóng to nhanh + tan hết, để lộ công trình thật bên dưới.</summary>
    private IEnumerator OpenBox(float duration)
    {
        if (_boxRoot == null) yield break;

        float elapsed = 0f;
        Vector3 from = _boxRoot.localScale;

        while (elapsed < duration && _boxRoot != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            _boxRoot.localScale = Vector3.LerpUnclamped(from, from * 1.45f, k);
            SetAlpha(_boxParts, 1f - k);
            yield return null;
        }

        if (_boxRoot != null) _boxRoot.gameObject.SetActive(false);
    }

    // ── Tiện ích ─────────────────────────────────────────────────────────────

    private static IEnumerator ScaleRoutine(Transform target, float from, float to,
                                            float duration, bool backOut)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float e;
            if (backOut)
            {
                const float c1 = 1.70158f, c3 = c1 + 1f;
                e = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            }
            else
            {
                e = 1f - Mathf.Pow(1f - t, 3f);
            }

            target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, e);
            yield return null;
        }

        if (target != null) target.localScale = Vector3.one * to;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        if (seconds <= 0f) yield break;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static void SetAlpha(IList<SpriteRenderer> renderers, float alpha)
    {
        if (renderers == null) return;

        alpha = Mathf.Clamp01(alpha);
        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
