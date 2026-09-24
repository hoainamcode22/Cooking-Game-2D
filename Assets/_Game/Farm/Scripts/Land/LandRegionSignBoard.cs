using TMPro;
using UnityEngine;

/// <summary>
/// ============================================================================
/// BIỂN CHỈ ĐƯỜNG Ở GIỮA LÔ ĐẤT CHƯA MUA
/// ============================================================================
///
/// VÒNG 16 — CHỮ SÁNG, CÓ DẤU, NẰM GỌN TRONG BẢNG.
/// ---------------------------------------------------------------------------
/// Vòng 15 dùng TextMesh legacy: chữ nâu tối trên bảng nâu, không dấu, không co
/// theo bảng. Vòng này đổi sang TextMeshPro 3D + font "Fonts/Baloo2 SDF" (font
/// tiếng Việt chuẩn của dự án, cùng font với KHO VẬT PHẨM / CỬA HÀNG):
///   • Màu kem sáng + viền nâu đậm → đọc được trên nền bảng gỗ.
///   • isOrthographic = true → 1 point cỡ chữ = 1 world unit (cùng cách
///     ConstructionSiteVisuals đang dùng), không phải mò tỉ lệ.
///   • Sau khi đổi chữ, đo textBounds thật rồi co cho vừa 78% bề ngang tấm bảng.
///
/// Kích thước bảng vẫn tính theo Ô LƯỚI (không theo pixel):
///   art gốc 1.26 x 2.14 unit, một ô = 300 x 150 → phải phóng ~125 lần.
///   Script + collider nằm trên ROOT scale 1, chỉ ART được scale (xem
///   LandExpansionManager.RespawnSigns).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LandRegionSignBoard : MonoBehaviour
{
    public const string FontResource = "Fonts/Baloo2 SDF";

    [Header("Tham chiếu (tự tìm nếu để trống)")]
    public SpriteRenderer boardSprite;
    public TextMeshPro label;

    [Header("Kích thước — tính theo Ô LƯỚI")]
    [Tooltip("Chiều cao tấm bảng = bao nhiêu lần chiều cao MỘT Ô lưới (150 unit).")]
    public float boardHeightInCells = 1.8f;

    [Tooltip("Tâm dòng chữ nằm ở bao nhiêu % chiều cao bảng (0 = chân cột, 1 = đỉnh).")]
    [Range(0f, 1f)] public float labelHeightRatio = 0.70f;

    [Tooltip("Chữ chiếm tối đa bao nhiêu % bề ngang tấm bảng.")]
    [Range(0.3f, 1f)] public float labelWidthRatio = 0.78f;

    [Tooltip("Chữ chiếm tối đa bao nhiêu % chiều cao tấm bảng.")]
    [Range(0.1f, 0.6f)] public float labelMaxHeightRatio = 0.30f;

    [Tooltip("Nới rộng vùng bấm so với tấm bảng (1.15 = rộng hơn 15%).")]
    public float clickPadding = 1.15f;

    [Tooltip("Bat (mac dinh) = bam bien cua lo DANG DON se mo popup tien do (khung go + " +
             "dong ho + nut kim cuong). Tat = quay ve hanh vi cu: bam khong ra gi ca.")]
    public bool moPopupTienDoKhiDangDon = true;

    [Header("Hiển thị")]
    public string sortingLayerName = "ObjectsFront";
    public int sortingOrder = 900;
    public Color boardReady  = Color.white;
    public Color boardLocked = new Color(0.90f, 0.90f, 0.90f);

    [Header("Chữ")]
    public float fontSize = 34f;                                   // world unit / dòng
    public Color textReady    = new Color(1.00f, 0.97f, 0.86f);    // kem sáng
    public Color textLocked   = new Color(0.98f, 0.90f, 0.72f);    // vàng nhạt
    public Color textClearing = new Color(0.75f, 0.92f, 1.00f);    // xanh nhạt
    public Color outlineColor = new Color(0.30f, 0.17f, 0.06f);
    [Range(0f, 0.5f)] public float outlineWidth = 0.22f;

    private LandRegionData _region;
    private LandExpansionManager _manager;
    private Transform _art;
    private float _nextRefresh;
    private string _lastText;
    private Vector3 _artBaseScale = Vector3.one;
    private float _punch;

    // ─────────────────────────────────────────────────────────────────────
    public LandRegionData Region => _region;

    public void Bind(LandRegionData region, LandExpansionManager manager)
    {
        _region = region;
        _manager = manager;
        EnsureVisual();
        Refresh();
    }

    private void OnEnable()  { LandExpansionManager.OnRegionUnlocked += HandleUnlocked; }
    private void OnDisable() { LandExpansionManager.OnRegionUnlocked -= HandleUnlocked; }
    private void HandleUnlocked(LandRegionData r) { if (r == _region) Destroy(gameObject); }

    private void Update()
    {
        if (_punch > 0f)
        {
            _punch = Mathf.Max(0f, _punch - Time.unscaledDeltaTime * 4f);
            if (_art != null)
                _art.localScale = _artBaseScale * (1f + 0.12f * Mathf.Sin(_punch * Mathf.PI));
        }

        if (Time.time < _nextRefresh) return;
        // [FIX 2026-09-21 P0 — PROFILER] 27 bang cung bat dau nen cung Refresh() trong MOT frame moi 0.5s
        // => 216 lan cap phat chuoi (8.7 KB) + 2.7ms gom vao mot frame = gai GC deu dan. Nay: nhip 1s va
        // moi bang lech pha ngau nhien de tai roi deu ra cac frame.
        if (_phaLech < 0f) _phaLech = UnityEngine.Random.value;
        _nextRefresh = Time.time + 1f + (_phaLech * 0.5f);
        _phaLech = 0f;
        Refresh();
    }
    private float _phaLech = -1f;

    // ─────────────────────────────────────────────────────────────────────
    // DỰNG HÌNH
    // ─────────────────────────────────────────────────────────────────────
    private void EnsureVisual()
    {
        // ── 1. Tấm bảng ─────────────────────────────────────────────────
        if (boardSprite == null) boardSprite = GetComponentInChildren<SpriteRenderer>(true);
        if (boardSprite == null) boardSprite = BuildFallbackBoard();
        if (boardSprite == null) return;
        _art = boardSprite.transform;

        // ── 2. Scale cho vừa ô lưới ─────────────────────────────────────
        float rawH = boardSprite.sprite != null ? boardSprite.sprite.bounds.size.y : 1f;
        if (rawH < 0.0001f) rawH = 1f;
        float targetH = IsoGrid.CellHeight * Mathf.Max(0.1f, boardHeightInCells);
        float k = targetH / rawH;

        Transform p = _art.parent;
        if (p != null && p != transform)
        {
            float pk = Mathf.Abs(p.lossyScale.y / Mathf.Max(0.0001f, transform.lossyScale.y));
            if (pk > 0.0001f) k /= pk;
        }
        _art.localScale = new Vector3(k, k, 1f);
        _artBaseScale = _art.localScale;

        // ── 3. Sorting ──────────────────────────────────────────────────
        ApplySorting(boardSprite);

        // ── 4. Chữ TMP ──────────────────────────────────────────────────
        if (label == null) label = GetComponentInChildren<TextMeshPro>(true);
        if (label == null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshPro>();
        }

        var font = Resources.Load<TMP_FontAsset>(FontResource);
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (font != null) label.font = font;

        label.isOrthographic       = true;          // 1 point = 1 world unit
        label.fontSize             = fontSize;
        label.fontStyle            = FontStyles.Bold;
        label.alignment            = TextAlignmentOptions.Center;
        label.enableWordWrapping   = false;
        label.overflowMode         = TextOverflowModes.Overflow;
        label.lineSpacing          = -8f;
        label.rectTransform.sizeDelta = new Vector2(600f, 200f);
        label.color                = textReady;
        label.outlineWidth         = outlineWidth;
        label.outlineColor         = outlineColor;

        var lmr = label.GetComponent<MeshRenderer>();
        if (lmr != null)
        {
            if (!string.IsNullOrEmpty(sortingLayerName) && LayerExists(sortingLayerName))
                lmr.sortingLayerName = sortingLayerName;
            else
                lmr.sortingLayerID = boardSprite.sortingLayerID;
            lmr.sortingOrder = sortingOrder + 2;
        }

        // ── 5. Vị trí chữ + vùng bấm ────────────────────────────────────
        PlaceLabelAndCollider();
    }

    private void PlaceLabelAndCollider()
    {
        if (boardSprite == null) return;
        Bounds wb = ComputeBoardBounds();

        if (label != null)
        {
            var world = new Vector3(wb.center.x, wb.min.y + wb.size.y * labelHeightRatio, transform.position.z);
            label.transform.position = world;
            var lp = label.transform.localPosition;
            label.transform.localPosition = new Vector3(lp.x, lp.y, -1f);
            label.transform.localScale = Vector3.one;
        }

        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Vector3 ls = transform.lossyScale;
        float sx = Mathf.Abs(ls.x) < 0.0001f ? 1f : Mathf.Abs(ls.x);
        float sy = Mathf.Abs(ls.y) < 0.0001f ? 1f : Mathf.Abs(ls.y);
        col.size = new Vector2(wb.size.x / sx, wb.size.y / sy) * Mathf.Max(1f, clickPadding);
        Vector3 lc = transform.InverseTransformPoint(wb.center);
        col.offset = new Vector2(lc.x, lc.y);
        col.isTrigger = false;
    }

    /// <summary>Đo chữ thật rồi co cho vừa tấm bảng (gọi mỗi khi đổi nội dung).</summary>
    private void FitLabel()
    {
        if (label == null || boardSprite == null) return;

        label.transform.localScale = Vector3.one;
        label.ForceMeshUpdate();
        Bounds tb = label.textBounds;                     // local, chưa nhân scale
        if (tb.size.x <= 0.001f || tb.size.y <= 0.001f) return;

        Bounds wb = ComputeBoardBounds();
        float maxW = wb.size.x * labelWidthRatio;
        float maxH = wb.size.y * labelMaxHeightRatio;
        float s = Mathf.Min(maxW / tb.size.x, maxH / tb.size.y);
        s = Mathf.Clamp(s, 0.05f, 10f);
        label.transform.localScale = new Vector3(s, s, 1f);
    }

    private Bounds ComputeBoardBounds()
    {
        var t = boardSprite.transform;
        if (boardSprite.sprite == null) return boardSprite.bounds;
        Bounds lb = boardSprite.sprite.bounds;
        Vector3 s = t.lossyScale;
        Vector3 center = t.TransformPoint(lb.center);
        return new Bounds(center, new Vector3(lb.size.x * Mathf.Abs(s.x), lb.size.y * Mathf.Abs(s.y), 1f));
    }

    private void ApplySorting(SpriteRenderer sr)
    {
        if (sr == null) return;
        bool has = !string.IsNullOrEmpty(sortingLayerName) && LayerExists(sortingLayerName);
        if (has) sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;
        foreach (var child in sr.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (child == sr) continue;
            if (has) child.sortingLayerName = sortingLayerName;
            child.sortingOrder = sortingOrder + 1;
        }
    }

    private static bool LayerExists(string name)
    {
        foreach (var l in SortingLayer.layers) if (l.name == name) return true;
        return false;
    }

    // ── Bảng vẽ bằng code khi không có prefab ─────────────────────────────
    private SpriteRenderer BuildFallbackBoard()
    {
        var go = new GameObject("BoardFallback");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FallbackSignSprite();
        return sr;
    }

    private static Sprite _fallbackSprite;
    private static Sprite FallbackSignSprite()
    {
        if (_fallbackSprite != null) return _fallbackSprite;
        const int W = 64, H = 96;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var clear = new Color(0, 0, 0, 0);
        var wood = new Color(0.45f, 0.29f, 0.15f);
        var plank = new Color(0.80f, 0.60f, 0.35f);
        var border = new Color(0.38f, 0.24f, 0.12f);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            Color c = clear;
            if (x >= W / 2 - 4 && x <= W / 2 + 3 && y < H * 0.60f) c = wood;
            if (y >= H * 0.52f && y < H * 0.95f && x >= 3 && x < W - 3)
            {
                bool edge = y <= H * 0.54f || y >= H * 0.93f || x <= 5 || x >= W - 6;
                c = edge ? border : plank;
            }
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.02f), 64f);
        return _fallbackSprite;
    }

    // ─────────────────────────────────────────────────────────────────────
    // NỘI DUNG
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Tien to "Mo o cap" cua ly do khoa theo cap (chuoi goc tieng Viet).</summary>
    private const string TIEN_TO_MO_O_CAP = "Mở ở cấp";

    /// <summary>
    /// TRUE khi `reason` la ly do "khoa theo cap". `reason` co the da duoc dich sang tieng Anh
    /// (LandExpansionManager dung Loc.TF), nen so tien to o CA HAI ngon ngu — giong LandRegionSign.
    /// </summary>
    private static bool LaLyDoKhoaTheoCap(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return false;
        reason = reason.Trim();

        if (reason.StartsWith(TIEN_TO_MO_O_CAP, System.StringComparison.Ordinal)) return true;

        string daDich = Loc.T(TIEN_TO_MO_O_CAP);
        if (!string.IsNullOrEmpty(daDich) && daDich != TIEN_TO_MO_O_CAP
            && reason.StartsWith(daDich.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    public void Refresh()
    {
        if (_region == null || _manager == null || label == null) return;

        string text;
        Color textColor;

        var site = _manager.ClearingSiteOf(_region.regionId);
        int pending = site != null ? site.RemainingSeconds
                                   : LandClearingSite.PendingRemaining(_region.regionId);
        if (pending > 0)
        {
            text = Loc.TF("ĐANG DỌN DẸP\n{0}", FormatTime(pending));
            textColor = textClearing;
            if (boardSprite != null) boardSprite.color = boardReady;
        }
        else
        {
            bool ok = _manager.CanBuy(_region, out string reason);
            bool lockedByLevel = !ok && LaLyDoKhoaTheoCap(reason);

            if (lockedByLevel)
            {
                text = Loc.TF("Mở ở cấp {0}", _region.unlockLevel);
                textColor = textLocked;
            }
            else if (_region.goldPrice > 0)
            {
                text = Loc.TF("{0}\n{1} vàng", Loc.T(_region.displayName), $"{_region.goldPrice:n0}");
                textColor = textReady;
            }
            else
            {
                text = Loc.T(_region.displayName);
                textColor = textReady;
            }
            if (boardSprite != null) boardSprite.color = ok ? boardReady : boardLocked;
        }

        label.color = textColor;
        if (text != _lastText)
        {
            label.text = text;
            _lastText = text;
            FitLabel();
        }
    }

    private static string FormatTime(int sec)
    {
        if (sec >= 3600) return $"{sec / 3600}g {(sec % 3600) / 60:00}p";
        if (sec >= 60)   return $"{sec / 60}p {sec % 60:00}s";
        return $"{sec}s";
    }

    // ─────────────────────────────────────────────────────────────────────
    // TƯƠNG TÁC
    // ─────────────────────────────────────────────────────────────────────
    private void OnMouseEnter() { if (_art != null && _punch <= 0f) _art.localScale = _artBaseScale * 1.06f; }
    private void OnMouseExit()  { if (_art != null && _punch <= 0f) _art.localScale = _artBaseScale; }

    private void OnMouseUpAsButton()
    {
        if (_region == null || _manager == null) return;
        if (EditModeManager.IsEditMode) return;
        if (PlacementManager.IsPlacingNewObject) return;

        _punch = 1f;

        if (_manager.IsClearing(_region))
        {
            // Lo dang don: KHONG mo popup mua nua (nhu cu), nhung gio cho xem tien do.
            // Site null (vd save con do ma site chua kip dung lai) => y het ban cu: khong lam gi.
            if (moPopupTienDoKhiDangDon)
            {
                var site = _manager.ClearingSiteOf(_region.regionId);
                if (site != null && site.IsRunning)
                    BuildingProcessPopupUI.GetOrCreate().Open(site);
            }
            return;
        }

        // [2026-09-24] Chua du cap: chi hien dong chu "This land unlocks at Level N" roi mo dan
        if (LockedHintFX.ChanTheoCap("Vùng đất này", _region.unlockLevel)) return;

        // Lo chua mua: nguyen ven duong cu, khong dong gi vao.
        LandPurchasePopupUI.Show(_region, _manager);
    }
}
