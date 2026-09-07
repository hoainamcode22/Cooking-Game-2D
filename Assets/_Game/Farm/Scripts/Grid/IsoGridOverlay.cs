using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ============================================================================
/// LUOI NEN ISO — ve khung o kim cuong de CAN KHI VE TILE / DAT CONG TRINH
/// ============================================================================
///
/// Hien ca trong SCENE VIEW (khong can bam Play) lan GAME VIEW.
/// Ve bang Mesh line nen khong ton draw call, khong can shader rieng.
///
/// CACH DUNG
///   • Tools/Map45/6. Tao Luoi Nen  -> tu dung vao scene.
///   • Phim G (doi duoc) de bat/tat luc dang choi.
///   • Chinh 'halfSize' de luoi phu het vung ban do.
///
/// Luoi bam DUNG Grid_Iso45 qua IsoGrid nen o ve ra trung khop tung pixel voi
/// tile ban ve — cu nhin khung nay ma to tile la khop.
///
/// 🔴 V10 — SEP YEU CAU BO LUOI DE LEN MAP (Sep tu ve art luoi).
///   • `masterEnable` MAC DINH = false => luoi TAT hoan toan. Day la field MOI nen
///     Unity lay gia tri mac dinh tu code cho ca object da nam trong scene => tat
///     duoc ma KHONG phai sua file .unity.
///   • `halfSize` trong scene dang la 341 (683x683 o). Doi mac dinh trong code
///     KHONG de len duoc gia tri da serialize, nen phai KEP lai luc chay bang
///     MaxHalfSize (xem EffectiveHalfSize).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IsoGridOverlay : MonoBehaviour
{
    [Header("CONG TAC CHINH — mac dinh TAT theo yeu cau Sep")]
    [Tooltip("Tat = khong ve luoi gi ca (Sep tu ve art luoi). Bat lai neu can can tile.")]
    [SerializeField] private bool masterEnable = false;

    /// <summary>Tran halfSize luc CHAY — de len gia tri rac da nam trong scene (341).</summary>
    public const int MaxHalfSize = 40;

    [Header("Pham vi luoi (tinh bang O)")]
    [Tooltip("So o ve ra moi huong tinh tu o goc. 40 = luoi 80x80 o.")]
    public int halfSize = 40;

    [Tooltip("O trung tam cua luoi.")]
    public Vector2Int centerCell = Vector2Int.zero;

    [Header("Hien thi")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.16f);

    [Tooltip("Ke dam them moi N o cho de dem. 0 = tat.")]
    public int majorEvery = 5;
    public Color majorColor = new Color(1f, 0.9f, 0.35f, 0.34f);

    [Tooltip("Sorting order cua luoi. De cao hon tile nen, thap hon UI.")]
    public int sortingOrder = 900;
    public string sortingLayerName = "Default";

    [Header("Bat / tat")]
    [Tooltip("Phim bat tat luoi luc dang choi.")]
    public KeyCode toggleKey = KeyCode.G;

    [Tooltip("Bat = tu dong hien khi vao Edit Mode, an khi thoat.")]
    public bool autoShowInEditMode = true;

    [Tooltip("Hien luoi ngay ca khi khong o Edit Mode.")]
    public bool alwaysVisible = false;

    // ─────────────────────────────────────────────────────────────────────
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private int _builtHalf = -1;
    private Vector2Int _builtCenter;
    private float _builtW, _builtH;
    private bool _manualOn = true;
    private static bool _warnedClamp;

    /// <summary>
    /// halfSize thuc su dung de dung mesh — luon bi kep trong [1, MaxHalfSize].
    /// Scene dang luu 341 (683x683 o = 466k vertex); kep o day la cach DUY NHAT de
    /// code de len gia tri do ma khong sua file .unity.
    /// </summary>
    private int EffectiveHalfSize
    {
        get
        {
            if (halfSize > MaxHalfSize && !_warnedClamp)
            {
                _warnedClamp = true;
                { Debug.LogWarning($"[IsoGridOverlay] halfSize = {halfSize} trong scene qua lon -> kep con {MaxHalfSize}. Sua field halfSize tren object 'IsoGridOverlay' (con cua Grid_Iso45) neu muon khac."); }
            }
            return Mathf.Clamp(halfSize, 1, MaxHalfSize);
        }
    }

    private void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        EnsureMaterial();
        Rebuild();
    }

    private void OnValidate()
    {
        halfSize = Mathf.Clamp(halfSize, 1, MaxHalfSize);
        if (isActiveAndEnabled) { EnsureMaterial(); Rebuild(); }
    }

    private void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(toggleKey)) _manualOn = !_manualOn;

        bool show = masterEnable && _manualOn &&
                    (alwaysVisible || !Application.isPlaying ||
                     (autoShowInEditMode && EditModeManager.IsEditMode));
        if (_renderer != null && _renderer.enabled != show) _renderer.enabled = show;

        // luoi doi khi Grid_Iso45 bi doi scale / vi tri
        if (!Mathf.Approximately(_builtW, IsoGrid.CellWidth) ||
            !Mathf.Approximately(_builtH, IsoGrid.CellHeight) ||
            _builtHalf != EffectiveHalfSize || _builtCenter != centerCell)
            Rebuild();
    }

    private void EnsureMaterial()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null) return;
        var mat = _renderer.sharedMaterial;
        if (mat == null || mat.shader == null || mat.shader.name != "Sprites/Default")
        {
            mat = new Material(Shader.Find("Sprites/Default")) { name = "IsoGridOverlay_Mat" };
            _renderer.sharedMaterial = mat;
        }
        _renderer.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName)) _renderer.sortingLayerName = sortingLayerName;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
    }

    /// <summary>
    /// Tu tinh halfSize sao cho luoi phu het ban do.
    /// Do bien tu TilemapRenderer (moi tilemap trong scene), fallback Camera bounds.
    /// </summary>
    public void FitToMap(float paddingCells = 4f)
    {
        Bounds b = default; bool has = false;
        foreach (var r in Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (r == null || !r.enabled) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr == null || sr.sprite == null) continue;
            if (sr.drawMode != SpriteDrawMode.Tiled) continue;   // nen trai (GroundBase)
            if (!has) { b = sr.bounds; has = true; } else b.Encapsulate(sr.bounds);
        }
        if (!has) { Debug.LogWarning("[IsoGridOverlay] Khong do duoc bien ban do."); return; }

        // 4 goc hop bao -> toa do o, lay bien lon nhat
        float maxAbs = 0f;
        foreach (var p in new[] {
            new Vector3(b.min.x, b.min.y), new Vector3(b.max.x, b.min.y),
            new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y) })
        {
            Vector2 c = IsoGrid.WorldToCellFloat(p);
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(c.x - centerCell.x), Mathf.Abs(c.y - centerCell.y));
        }
        halfSize = Mathf.Clamp(Mathf.CeilToInt(maxAbs + paddingCells), 1, MaxHalfSize);
        Rebuild();
        Debug.Log($"[IsoGridOverlay] Phu ban do: halfSize = {halfSize} o " +
                  $"(bounds {b.size.x:0} x {b.size.y:0} world).");
    }

    /// <summary>Dung lai mesh luoi.</summary>
    public void Rebuild()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "IsoGridOverlay" };
            _mesh.hideFlags = HideFlags.DontSave;
        }
        _mesh.Clear();

        int h = EffectiveHalfSize;
        int minX = centerCell.x - h, maxX = centerCell.x + h;
        int minY = centerCell.y - h, maxY = centerCell.y + h;

        var verts = new List<Vector3>();
        var cols  = new List<Color>();
        var idx   = new List<int>();

        // Duong theo huong +cx (chay len-phai): giu cy co dinh
        for (int cy = minY; cy <= maxY; cy++)
        {
            Vector3 a = LocalOf(new Vector2(minX - 0.5f, cy - 0.5f));
            Vector3 b = LocalOf(new Vector2(maxX + 0.5f, cy - 0.5f));
            AddLine(verts, cols, idx, a, b, IsMajor(cy) ? majorColor : lineColor);
        }
        // vien cuoi
        {
            Vector3 a = LocalOf(new Vector2(minX - 0.5f, maxY + 0.5f));
            Vector3 b = LocalOf(new Vector2(maxX + 0.5f, maxY + 0.5f));
            AddLine(verts, cols, idx, a, b, IsMajor(maxY + 1) ? majorColor : lineColor);
        }

        // Duong theo huong +cy (chay len-trai): giu cx co dinh
        for (int cx = minX; cx <= maxX; cx++)
        {
            Vector3 a = LocalOf(new Vector2(cx - 0.5f, minY - 0.5f));
            Vector3 b = LocalOf(new Vector2(cx - 0.5f, maxY + 0.5f));
            AddLine(verts, cols, idx, a, b, IsMajor(cx) ? majorColor : lineColor);
        }
        {
            Vector3 a = LocalOf(new Vector2(maxX + 0.5f, minY - 0.5f));
            Vector3 b = LocalOf(new Vector2(maxX + 0.5f, maxY + 0.5f));
            AddLine(verts, cols, idx, a, b, IsMajor(maxX + 1) ? majorColor : lineColor);
        }

        _mesh.indexFormat = verts.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        _mesh.SetVertices(verts);
        _mesh.SetColors(cols);
        _mesh.SetIndices(idx, MeshTopology.Lines, 0);
        _mesh.RecalculateBounds();
        mf.sharedMesh = _mesh;

        _builtHalf = h;
        _builtCenter = centerCell;
        _builtW = IsoGrid.CellWidth;
        _builtH = IsoGrid.CellHeight;
    }

    private bool IsMajor(int i) => majorEvery > 0 && (i % majorEvery == 0);

    /// <summary>Toa do o (thuc) -> toa do LOCAL cua object nay.</summary>
    private Vector3 LocalOf(Vector2 cellFloat)
        => transform.InverseTransformPoint(IsoGrid.CellFloatToWorld(cellFloat));

    private static void AddLine(List<Vector3> v, List<Color> c, List<int> i,
                                Vector3 a, Vector3 b, Color col)
    {
        i.Add(v.Count); v.Add(a); c.Add(col);
        i.Add(v.Count); v.Add(b); c.Add(col);
    }
}
