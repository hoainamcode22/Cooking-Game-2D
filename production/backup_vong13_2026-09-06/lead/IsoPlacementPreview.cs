using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ============================================================================
/// PREVIEW O CHIEM — to sang cac o ma cong trinh dang cam se dat vao
/// ============================================================================
///
/// MAU (tu V10 chi to len VIEN o, khong nhuom mat o nua):
///   • XANH  = dat duoc
///   • DO    = vuong cong trinh khac / ngoai bien ban do
///   • VANG  = dat vao khu dat CHUA MUA
///
/// 🔴 V10 — SEP YEU CAU BO LOP NHUOM MAU DE LEN MAP (Sep tu ve art luoi).
/// Mac dinh `fillCells = false` => CHI ve vien o hinh thoi (canh goc), khong to mat.
/// Bat lai bang checkbox `fillCells` tren component neu can.
/// Ha alpha cua 3 mau KHONG du de tat, vi scene da serialize alpha 0.38 roi — phai
/// co co `fillCells` moi de len duoc gia tri nam trong scene.
///
/// Ve bang mesh tam giac (o kim cuong) nen khong can sprite, khong ton draw call.
/// Tu bat khi dang dat / dang sua, tu tat khi xong — khong can lam gi them.
///
/// Tao nhanh: Tools/Map45/7. Tao Preview O Chiem
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IsoPlacementPreview : MonoBehaviour
{
    [Header("Mau")]
    public Color colorValid   = new Color(0.35f, 1f, 0.45f, 0.34f);
    public Color colorBlocked = new Color(1f, 0.32f, 0.30f, 0.38f);
    public Color colorNoLand  = new Color(1f, 0.82f, 0.25f, 0.38f);

    [Header("To mat o — TAT theo yeu cau Sep (chi con vien)")]
    [Tooltip("Bat = to kin mat o nhu cu. Tat = chi ve VIEN o, khong nhuom len map.")]
    [SerializeField] private bool fillCells = false;

    [Tooltip("Doi mau VIEN theo trang thai (xanh / do / vang) thay cho to mat o.")]
    [SerializeField] private bool tintOutlineByState = true;

    [Header("Vien o")]
    public bool drawOutline = true;
    public Color outlineColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Hien thi")]
    public int sortingOrder = 950;
    public string sortingLayerName = "Default";

    [Tooltip("Thu nho moi o mot chut cho de nhin ranh gioi (0 = kin).")]
    [Range(0f, 0.25f)] public float cellInset = 0.06f;

    /// <summary>
    /// RAO AN TOAN — so o TOI DA duoc dung mesh preview.
    /// gridSize rac (don vi ART lot vao asset, vd 341x342) tung sinh rect 116.622 o =>
    /// to kin man hinh + mesh ~466k vertex MOI BUOC KEO. Chan ngay tu goc.
    /// </summary>
    public const int MaxPreviewCells = 4096;

    // ──────────────────────────────────────────────────────────────────────
    private static bool _warnedTooBig;
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private RectInt _lastRect;
    private int _lastState = -1;

    private void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        EnsureMaterial();
        Clear();
    }

    private void LateUpdate()
    {
        var pm = PlacementManager.Instance;
        bool active = pm != null && (PlacementManager.IsPlacingNewObject || pm.IsEditingBuilding);

        if (!active) { Clear(); return; }

        RectInt rect = pm.CurrentRect;
        if (rect.width <= 0 || rect.height <= 0) { Clear(); return; }

        int state = ComputeState(pm, rect);
        if (rect.Equals(_lastRect) && state == _lastState) return;   // khong doi -> khong dung lai mesh

        _lastRect = rect;
        _lastState = state;
        Rebuild(rect, state == 0 ? colorValid : (state == 2 ? colorNoLand : colorBlocked));
    }

    /// <summary>0 = hop le, 1 = vuong/ngoai bien, 2 = ngoai dat da mua.</summary>
    private static int ComputeState(PlacementManager pm, RectInt rect)
    {
        var land = LandExpansionManager.Instance;
        if (land != null && !land.IsRectUnlocked(rect)) return 2;
        return pm.IsCurrentValid ? 0 : 1;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void EnsureMaterial()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null) return;
        var mat = _renderer.sharedMaterial;
        if (mat == null || mat.shader == null || mat.shader.name != "Sprites/Default")
        {
            mat = new Material(Shader.Find("Sprites/Default")) { name = "IsoPlacementPreview_Mat" };
            _renderer.sharedMaterial = mat;
        }
        _renderer.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName)) _renderer.sortingLayerName = sortingLayerName;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
    }

    /// <summary>Xoa preview.</summary>
    public void Clear()
    {
        if (_mesh != null) _mesh.Clear();
        _lastRect = new RectInt(0, 0, 0, 0);
        _lastState = -1;
        if (_renderer != null) _renderer.enabled = false;
    }

    private void Rebuild(RectInt rect, Color fill)
    {
        // RAO AN TOAN (Dev T yeu cau): mot gridSize rac trong tuong lai khong bao gio
        // duoc nhuom kin man hinh, cung khong dung mesh khong lo moi buoc keo.
        int cellCount = rect.width * rect.height;
        if (cellCount > MaxPreviewCells)
        {
            if (!_warnedTooBig)
            {
                _warnedTooBig = true;
                { Debug.LogWarning($"[IsoPreview] Rect {rect.width}x{rect.height} = {cellCount} o > {MaxPreviewCells} -> bo qua preview. gridSize cua asset dang la don vi ART, sua bang Tools/Map45/8."); }
            }
            Clear();
            return;
        }

        // Ca to mat lan vien deu tat => khong co gi de ve.
        if (!fillCells && !drawOutline) { Clear(); return; }

        var mf = GetComponent<MeshFilter>();
        if (mf == null) return;
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "IsoPlacementPreview" };
            _mesh.hideFlags = HideFlags.DontSave;
        }
        _mesh.Clear();

        var verts = new List<Vector3>();
        var cols  = new List<Color>();
        var tris  = new List<int>();
        var lines = new List<int>();

        // Vien co the doi mau theo trang thai de Sep van thay xanh/do/vang ma KHONG can to mat.
        Color lineCol = tintOutlineByState
            ? new Color(fill.r, fill.g, fill.b, outlineColor.a)
            : outlineColor;
        Color vertCol = fillCells ? fill : lineCol;

        float k = 1f - Mathf.Clamp01(cellInset);

        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                // 🔴 SUA V10 — LECH DON VI LOCAL/WORLD.
                // Ban cu: tam o duoc doi sang LOCAL bang InverseTransformPoint, roi cong
                // hw/hh dang la don vi WORLD vao diem LOCAL do. Object nay la con cua
                // Grid_Iso45 (localScale 1 nhung scale the gioi = 150), nen 1 don vi local
                // = 150 world => moi o kim cuong bi ve TO GAP 150 LAN. Day la nguyen nhan
                // thu hai (cung voi gridSize rac) lam preview tran kin man hinh.
                // Nay: dung 4 dinh trong WORLD roi moi doi TUNG dinh sang local.
                Vector3 wc = IsoGrid.CellCenterToWorld(new Vector2Int(x, y));
                float hw = IsoGrid.CellWidth * 0.5f * k;
                float hh = IsoGrid.CellHeight * 0.5f * k;

                int b = verts.Count;
                verts.Add(transform.InverseTransformPoint(wc + new Vector3(0f,  hh, 0f)));   // N
                verts.Add(transform.InverseTransformPoint(wc + new Vector3(hw,  0f, 0f)));   // E
                verts.Add(transform.InverseTransformPoint(wc + new Vector3(0f, -hh, 0f)));   // S
                verts.Add(transform.InverseTransformPoint(wc + new Vector3(-hw, 0f, 0f)));   // W
                for (int i = 0; i < 4; i++) cols.Add(vertCol);

                if (fillCells)
                {
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }

                if (drawOutline)
                {
                    lines.Add(b);     lines.Add(b + 1);
                    lines.Add(b + 1); lines.Add(b + 2);
                    lines.Add(b + 2); lines.Add(b + 3);
                    lines.Add(b + 3); lines.Add(b);
                }
            }
        }

        if (verts.Count == 0) { Clear(); return; }

        _mesh.indexFormat = verts.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        _mesh.SetVertices(verts);
        _mesh.SetColors(cols);
        _mesh.subMeshCount = drawOutline ? 2 : 1;
        _mesh.SetTriangles(tris, 0);
        if (drawOutline) _mesh.SetIndices(lines, MeshTopology.Lines, 1);
        _mesh.RecalculateBounds();
        mf.sharedMesh = _mesh;

        if (_renderer != null)
        {
            EnsureMaterial();
            if (drawOutline && _renderer.sharedMaterials.Length < 2)
            {
                var m = _renderer.sharedMaterial;
                _renderer.sharedMaterials = new[] { m, m };
            }
            _renderer.enabled = true;
        }
    }
}
