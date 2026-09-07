using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ============================================================================
/// PREVIEW O CHIEM — to sang cac o ma cong trinh dang cam se dat vao
/// ============================================================================
///
/// MAU:
///   • XANH  = dat duoc
///   • DO    = vuong cong trinh khac / ngoai bien ban do
///   • VANG  = dat vao khu dat CHUA MUA
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

    [Header("Vien o")]
    public bool drawOutline = true;
    public Color outlineColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Hien thi")]
    public int sortingOrder = 950;
    public string sortingLayerName = "Default";

    [Tooltip("Thu nho moi o mot chut cho de nhin ranh gioi (0 = kin).")]
    [Range(0f, 0.25f)] public float cellInset = 0.06f;

    // ─────────────────────────────────────────────────────────────────────
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

        float k = 1f - Mathf.Clamp01(cellInset);

        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                Vector3 c = transform.InverseTransformPoint(
                                IsoGrid.CellCenterToWorld(new Vector2Int(x, y)));
                float hw = IsoGrid.CellWidth * 0.5f * k;
                float hh = IsoGrid.CellHeight * 0.5f * k;

                int b = verts.Count;
                verts.Add(c + new Vector3(0f,  hh, 0f));   // N
                verts.Add(c + new Vector3(hw,  0f, 0f));   // E
                verts.Add(c + new Vector3(0f, -hh, 0f));   // S
                verts.Add(c + new Vector3(-hw, 0f, 0f));   // W
                for (int i = 0; i < 4; i++) cols.Add(fill);

                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);

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
