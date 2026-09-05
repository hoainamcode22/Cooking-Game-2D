using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ĐẠO DIỄN HIỆU ỨNG TUTORIAL V2 — bắn VFX ở 4 mốc của một bước.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// CHỦ FILE: DEV-VFX. Không Dev nào khác sửa file này.
///
/// VÌ SAO LÀM BẰNG UI (Image) CHỨ KHÔNG PHẢI ParticleSystem:
///   Bài học rút ra từ popup lên cấp (LevelUpPopupUI.cs, comment dòng 1387-1399):
///   Canvas ở chế độ Screen Space - Overlay LUÔN vẽ sau cùng, nên ParticleSystem dù đặt
///   `sortingOrder = 5000` cũng KHÔNG BAO GIỜ nổi lên trên UI. Tutorial cần hiệu ứng
///   nằm TRÊN card hội thoại và TRÊN lớp dim ⇒ bắt buộc dùng UI Image.
///   (Lana prefab vẫn dùng được cho hiệu ứng trong THẾ GIỚI — xem `confettiWorldPrefab`.)
///
/// TRIẾT LÝ: THIẾU SPRITE VẪN CHẠY — mốc nào không có art thì bỏ qua mốc đó, ghi log 1 lần,
/// KHÔNG lỗi đỏ, tutorial không đứng. Art gói B về là kéo vào Inspector, không đụng code.
///
/// KHỚP GÓI B PROMPT ĐỘI VẼ:
///   tut_glow_ring · tut_arrow_down · tut_sparkle_01..04 · tut_burst_ray · tut_dust_puff_01..03
///
/// [TutorialV2]
/// </summary>
[DisallowMultipleComponent]
public class TutorialVfxDirector : MonoBehaviour
{
    [Header("◆ Nơi vẽ hiệu ứng")]
    [Tooltip("Lớp UI chứa mọi hiệu ứng. Bỏ trống → tự tạo 'FX_Tutorial_Layer' phủ toàn màn " +
             "dưới Canvas gần nhất, luôn SetAsLastSibling để nằm trên card + dim.")]
    [SerializeField] private RectTransform fxLayer;

    [Header("◆ Art gói B (thiếu cái nào thì bỏ qua hiệu ứng đó)")]
    [Tooltip("tut_glow_ring.png — vòng sáng bao quanh nút cần bấm.")]
    [SerializeField] private Sprite glowRing;

    [Tooltip("tut_arrow_down.png — mũi tên nảy trên đầu mục tiêu.")]
    [SerializeField] private Sprite arrowDown;

    [Tooltip("tut_sparkle_01..04.png — sao lấp lánh rải khi hoàn thành bước.")]
    [SerializeField] private Sprite[] sparkles;

    [Tooltip("tut_burst_ray.png — chùm tia toả khi vào bước mới.")]
    [SerializeField] private Sprite burstRay;

    [Tooltip("tut_dust_puff_01..03.png — cụm khói bụi ở chân khi thao tác trên đất.")]
    [SerializeField] private Sprite[] dustPuffs;

    [Header("◆ Hiệu ứng thế giới (tái dùng Lana Studio — tuỳ chọn)")]
    [Tooltip("Prefab confetti bắn khi XONG cả tutorial. Gợi ý: " +
             "Assets/_Game/Resources/VFX/Confetti_blast_multicolor.prefab")]
    [SerializeField] private GameObject confettiWorldPrefab;

    [Header("◆ Nhịp")]
    [SerializeField] private float glowPulseCycle   = 1.15f;
    [SerializeField] private float arrowBouncePixels = 14f;
    [SerializeField] private float arrowBounceCycle  = 0.7f;
    [SerializeField] private float burstDuration     = 0.55f;
    [SerializeField] private float sparkleDuration   = 0.9f;

    [Tooltip("Số sao rải mỗi lần hoàn thành bước.")]
    [Range(3, 24)]
    [SerializeField] private int sparkleCount = 10;

    // ═══════════════════════════════════════════════════════════════════════
    private readonly List<GameObject> _dangChay = new List<GameObject>();
    private GameObject _highlightRoot;   // glow + arrow của mục tiêu hiện tại
    private bool _daCanhBaoThieuArt;

    private const string kLayerName = "FX_Tutorial_Layer";

    // ═══════════════════════════════════════════════════════════════════════
    // API công khai — TutorialManager gọi ở 4 mốc
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>MỐC 1 — vừa vào một bước mới: chùm tia toả tại tâm card (hoặc điểm chỉ định).</summary>
    public void OnStepEnter(RectTransform tai = null)
    {
        if (burstRay == null) { CanhBaoThieuArt("burstRay"); return; }

        var layer = EnsureLayer();
        if (layer == null) return;

        var go = TaoAnh(layer, burstRay, 220f);
        DatViTri(go, tai, layer);
        ChayCoroutineAnToan(ChayBurst(go));
    }

    /// <summary>
    /// MỐC 2 — nêu bật thứ người chơi phải bấm: vòng sáng nhấp nháy quanh nó + mũi tên nảy phía trên.
    /// Gọi lại với target khác sẽ tự dọn cái cũ. Gọi <see cref="ClearHighlight"/> khi bước xong.
    /// </summary>
    public void OnHighlight(RectTransform mucTieu)
    {
        ClearHighlight();
        if (mucTieu == null) return;

        var layer = EnsureLayer();
        if (layer == null) return;

        _highlightRoot = new GameObject("FX_Highlight", typeof(RectTransform));
        var rootRt = (RectTransform)_highlightRoot.transform;
        rootRt.SetParent(layer, false);

        // BẮT BUỘC kéo giãn full + pivot giữa cho TRÙNG hệ toạ độ của `layer`.
        // Thiếu bước này, RectTransform mới neo mặc định ở góc dưới-trái, trong khi `tam`
        // được tính theo `layer` (pivot 0.5 = tâm màn) ⇒ vòng sáng lệch ~nửa màn hình
        // so với nút cần bấm (QA bắt 04/09).
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.pivot     = new Vector2(0.5f, 0.5f);
        rootRt.localScale = Vector3.one;

        Vector2 tam = QuyDoiViTri(mucTieu, layer);
        float duong = Mathf.Max(mucTieu.rect.width, mucTieu.rect.height) * 1.45f;
        if (duong < 80f) duong = 120f;

        if (glowRing != null)
        {
            var g = TaoAnh(rootRt, glowRing, duong);
            ((RectTransform)g.transform).anchoredPosition = tam;
            ChayCoroutineAnToan(ChayNhipVongSang((RectTransform)g.transform));
        }

        if (arrowDown != null)
        {
            var a = TaoAnh(rootRt, arrowDown, 96f);
            var art = (RectTransform)a.transform;
            art.anchoredPosition = tam + new Vector2(0f, duong * 0.5f + 60f);
            ChayCoroutineAnToan(ChayNayMuiTen(art, art.anchoredPosition));
        }

        if (glowRing == null && arrowDown == null) CanhBaoThieuArt("glowRing/arrowDown");
    }

    /// <summary>Dọn vòng sáng + mũi tên của bước hiện tại.</summary>
    public void ClearHighlight()
    {
        if (_highlightRoot != null) { Destroy(_highlightRoot); _highlightRoot = null; }
    }

    /// <summary>MỐC 3 — người chơi vừa làm đúng: rải sao lấp lánh (+ khói bụi nếu là thao tác trên đất).</summary>
    public void OnStepComplete(RectTransform tai = null, bool coKhoiBui = false)
    {
        var layer = EnsureLayer();
        if (layer == null) return;

        ClearHighlight();

        Vector2 goc = tai != null ? QuyDoiViTri(tai, layer) : Vector2.zero;

        if (sparkles != null && sparkles.Length > 0)
        {
            for (int i = 0; i < sparkleCount; i++)
            {
                var sp = sparkles[Random.Range(0, sparkles.Length)];
                if (sp == null) continue;

                var go = TaoAnh(layer, sp, Random.Range(26f, 52f));
                var rt = (RectTransform)go.transform;
                rt.anchoredPosition = goc;

                float goc2 = Random.Range(0f, Mathf.PI * 2f);
                Vector2 v  = new Vector2(Mathf.Cos(goc2), Mathf.Sin(goc2)) * Random.Range(90f, 230f);
                ChayCoroutineAnToan(ChayBayVaTan(rt, go.GetComponent<Image>(), v, sparkleDuration));
            }
        }
        else CanhBaoThieuArt("sparkles");

        if (coKhoiBui && dustPuffs != null && dustPuffs.Length > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                var sp = dustPuffs[Random.Range(0, dustPuffs.Length)];
                if (sp == null) continue;

                var go = TaoAnh(layer, sp, Random.Range(60f, 100f));
                var rt = (RectTransform)go.transform;
                rt.anchoredPosition = goc + new Vector2(Random.Range(-40f, 40f), -20f);
                ChayCoroutineAnToan(ChayBayVaTan(rt, go.GetComponent<Image>(),
                                                 new Vector2(Random.Range(-60f, 60f), Random.Range(40f, 90f)), 0.7f));
            }
        }
    }

    /// <summary>MỐC 4 — xong cả tutorial: confetti ăn mừng (UI sparkle + prefab Lana nếu có).</summary>
    public void OnTutorialDone()
    {
        OnStepComplete(null, coKhoiBui: false);

        if (confettiWorldPrefab == null) return;
        var go = Instantiate(confettiWorldPrefab);
        go.transform.position = Camera.main != null ? Camera.main.transform.position + Vector3.forward * 10f : Vector3.zero;
        Destroy(go, 4f);
    }

    /// <summary>Dọn sạch mọi hiệu ứng (gọi khi thoát tutorial).</summary>
    public void ClearAll()
    {
        ClearHighlight();
        for (int i = _dangChay.Count - 1; i >= 0; i--)
            if (_dangChay[i] != null) Destroy(_dangChay[i]);
        _dangChay.Clear();
    }

    private void OnDisable() { ClearAll(); }

    // ═══════════════════════════════════════════════════════════════════════
    // Bên trong
    // ═══════════════════════════════════════════════════════════════════════

    private RectTransform EnsureLayer()
    {
        if (fxLayer != null) { fxLayer.SetAsLastSibling(); return fxLayer; }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            CanhBaoThieuArt("Canvas");
            return null;
        }

        var found = canvas.transform.Find(kLayerName) as RectTransform;
        if (found == null)
        {
            var go = new GameObject(kLayerName, typeof(RectTransform));
            found = (RectTransform)go.transform;
            found.SetParent(canvas.transform, false);
        }

        found.anchorMin = Vector2.zero;
        found.anchorMax = Vector2.one;
        found.offsetMin = Vector2.zero;
        found.offsetMax = Vector2.zero;
        found.localScale = Vector3.one;
        found.SetAsLastSibling();

        fxLayer = found;
        return fxLayer;
    }

    private GameObject TaoAnh(Transform parent, Sprite sprite, float kichThuoc)
    {
        var go = new GameObject("FX_" + sprite.name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(kichThuoc, kichThuoc);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;   // BẮT BUỘC — hiệu ứng không được nuốt click của người chơi

        _dangChay.Add(go);
        return go;
    }

    private void DatViTri(GameObject go, RectTransform tai, RectTransform layer)
    {
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = tai != null ? QuyDoiViTri(tai, layer) : Vector2.zero;
    }

    /// <summary>Đổi vị trí một RectTransform bất kỳ sang toạ độ trong lớp FX (qua màn hình).</summary>
    private Vector2 QuyDoiViTri(RectTransform muc, RectTransform layer)
    {
        var canvas = layer.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        Vector2 manHinh = RectTransformUtility.WorldToScreenPoint(cam, muc.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, manHinh, cam, out Vector2 cucBo);
        return cucBo;
    }

    private void ChayCoroutineAnToan(IEnumerator r)
    {
        if (isActiveAndEnabled) StartCoroutine(r);
    }

    // ── Các coroutine hiệu ứng ──────────────────────────────────────────────

    private IEnumerator ChayBurst(GameObject go)
    {
        var rt  = (RectTransform)go.transform;
        var img = go.GetComponent<Image>();
        float dur = Mathf.Max(0.1f, burstDuration);
        float t = 0f;

        while (t < dur && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / dur);
            float s = Mathf.Lerp(0.35f, 1.5f, r);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localEulerAngles = new Vector3(0f, 0f, r * 45f);
            if (img != null) { var c = img.color; c.a = 1f - r; img.color = c; }
            yield return null;
        }
        DonRac(go);
    }

    private IEnumerator ChayNhipVongSang(RectTransform rt)
    {
        float chuKy = Mathf.Max(0.2f, glowPulseCycle);
        float t = 0f;
        while (rt != null)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(t / chuKy * Mathf.PI * 2f) * 0.09f;
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private IEnumerator ChayNayMuiTen(RectTransform rt, Vector2 goc)
    {
        float chuKy = Mathf.Max(0.2f, arrowBounceCycle);
        float t = 0f;
        while (rt != null)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Abs(Mathf.Sin(t / chuKy * Mathf.PI));
            rt.anchoredPosition = goc + new Vector2(0f, -s * arrowBouncePixels);
            yield return null;
        }
    }

    private IEnumerator ChayBayVaTan(RectTransform rt, Image img, Vector2 vanToc, float thoiLuong)
    {
        float dur = Mathf.Max(0.1f, thoiLuong);
        const float trongLuc = -260f;
        float t = 0f;

        while (t < dur && rt != null)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            vanToc += new Vector2(0f, trongLuc * dt);
            rt.anchoredPosition += vanToc * dt;

            float r = Mathf.Clamp01(t / dur);
            if (r > 0.5f && img != null)
            {
                var c = img.color;
                c.a = Mathf.Clamp01(1f - (r - 0.5f) / 0.5f);
                img.color = c;
            }
            yield return null;
        }
        DonRac(rt != null ? rt.gameObject : null);
    }

    private void DonRac(GameObject go)
    {
        if (go == null) return;
        _dangChay.Remove(go);
        Destroy(go);
    }

    private void CanhBaoThieuArt(string ten)
    {
        if (_daCanhBaoThieuArt) return;
        _daCanhBaoThieuArt = true;
        Debug.Log($"[TutorialVfxDirector] Chưa có '{ten}' → bỏ qua hiệu ứng đó, tutorial vẫn chạy bình thường. " +
                  "Art gói B (production/art-handoff/2026-09-04_TutorialV2/B_VFX_Tutorial/) về thì kéo vào " +
                  "Inspector là xong, không cần sửa code.");
    }
}
