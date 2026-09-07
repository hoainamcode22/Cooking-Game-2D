#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools/Farm/Khu Dat — CHIA DAT CHO CA MAP.
///
/// Sinh nhanh mot luoi cac khu dat (LandRegionData) tren luoi ISO, kem:
///   • Asset .asset cho tung khu (gia + level tang dan tu tam ra ngoai)
///   • GameObject "LandExpansion" trong scene voi component LandExpansionManager
///   • Hai Tilemap con: Tilemap_LockedOverlay (phu khu chua mua) va tro toi
///     Tilemap_IsoFence san co de ve hang rao vien khu.
///
/// Khu chinh giua duoc dat unlockedByDefault = true (dat khoi dau cua nguoi choi).
/// </summary>
public class IsoLandRegionTool : EditorWindow
{
    private const string AssetFolder = "Assets/_Game/Farm/Land";

    private Vector2Int centerCell     = new Vector2Int(0, 0);
    private Vector2Int cellsPerRegion = new Vector2Int(8, 8);
    private Vector2Int regionCount    = new Vector2Int(3, 3);   // 3x3 = 9 khu
    private int   basePrice           = 2000;
    private float priceGrowth         = 1.8f;
    private int   baseUnlockLevel     = 5;
    private int   levelStep           = 5;
    private bool  requirePrevious     = true;

    private Vector2 scroll;

    [MenuItem("Tools/Map45/9. Khu Dat — mo rong ban do", false, 9)]
    public static void Open() => GetWindow<IsoLandRegionTool>("Khu Dat");

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Chia ban do thanh luoi cac KHU DAT tren luoi ISO.\n" +
            "Khu giua = dat khoi dau (mien phi). Cang ra ngoai gia va cap yeu cau cang cao.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hinh hoc", EditorStyles.boldLabel);
        centerCell     = EditorGUILayout.Vector2IntField("O trung tam ban do", centerCell);
        cellsPerRegion = EditorGUILayout.Vector2IntField("So o moi khu (N x M)", cellsPerRegion);
        regionCount    = EditorGUILayout.Vector2IntField("So khu (ngang x doc)", regionCount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Kinh te", EditorStyles.boldLabel);
        basePrice       = EditorGUILayout.IntField("Gia khu dau tien", basePrice);
        priceGrowth     = EditorGUILayout.FloatField("He so tang gia moi vong", priceGrowth);
        baseUnlockLevel = EditorGUILayout.IntField("Cap mo vong 1", baseUnlockLevel);
        levelStep       = EditorGUILayout.IntField("Cap tang moi vong", levelStep);
        requirePrevious = EditorGUILayout.Toggle("Bat buoc mua khu ke truoc", requirePrevious);

        EditorGUILayout.Space();
        int total = Mathf.Max(1, regionCount.x) * Mathf.Max(1, regionCount.y);
        EditorGUILayout.LabelField($"Se tao {total} khu, moi khu {cellsPerRegion.x}x{cellsPerRegion.y} o " +
                                   $"(tong {total * cellsPerRegion.x * cellsPerRegion.y} o).");

        EditorGUILayout.Space();
        if (GUILayout.Button("1. Sinh asset khu dat", GUILayout.Height(30))) GenerateRegions();
        if (GUILayout.Button("2. Dung LandExpansion vao Scene", GUILayout.Height(30))) BuildSceneObject();
        EditorGUILayout.Space();
        if (GUILayout.Button("Xoa tien do mua dat (PlayerPrefs)"))
        {
            PlayerPrefs.DeleteKey("FARM_UNLOCKED_REGIONS");
            PlayerPrefs.Save();
            Debug.Log("[Land] Da xoa FARM_UNLOCKED_REGIONS.");
        }

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    private void GenerateRegions()
    {
        if (!Directory.Exists(AssetFolder)) Directory.CreateDirectory(AssetFolder);

        int cx = Mathf.Max(1, regionCount.x), cy = Mathf.Max(1, regionCount.y);
        int nx = Mathf.Max(1, cellsPerRegion.x), ny = Mathf.Max(1, cellsPerRegion.y);
        int midX = cx / 2, midY = cy / 2;

        var created = new List<LandRegionData>();
        var byIndex = new Dictionary<Vector2Int, LandRegionData>();

        for (int i = 0; i < cx; i++)
        {
            for (int j = 0; j < cy; j++)
            {
                int ring = Mathf.Max(Mathf.Abs(i - midX), Mathf.Abs(j - midY));
                string id = $"region_{i}_{j}";
                string path = $"{AssetFolder}/Land_{i}_{j}.asset";

                var data = AssetDatabase.LoadAssetAtPath<LandRegionData>(path);
                if (data == null)
                {
                    data = CreateInstance<LandRegionData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.regionId    = id;
                data.displayName = ring == 0 ? "Dat Khoi Dau" : $"Khu {i}-{j}";
                data.cellRects   = new List<RectInt> {
                    new RectInt(centerCell.x + (i - midX) * nx,
                                centerCell.y + (j - midY) * ny, nx, ny)
                };
                data.unlockedByDefault = (ring == 0);
                data.goldPrice   = ring == 0 ? 0 : Mathf.RoundToInt(basePrice * Mathf.Pow(priceGrowth, ring - 1) / 10f) * 10;
                data.gemPrice    = 0;
                data.unlockLevel = ring == 0 ? 0 : baseUnlockLevel + (ring - 1) * levelStep;
                data.requiredRegionIds = new List<string>();

                EditorUtility.SetDirty(data);
                created.Add(data);
                byIndex[new Vector2Int(i, j)] = data;
            }
        }

        // rang buoc: phai mua khu ke ben huong ve tam truoc
        if (requirePrevious)
        {
            foreach (var kv in byIndex)
            {
                var idx = kv.Key; var data = kv.Value;
                if (data.unlockedByDefault) continue;
                int stepX = idx.x == midX ? 0 : (idx.x > midX ? -1 : 1);
                int stepY = idx.y == midY ? 0 : (idx.y > midY ? -1 : 1);
                var prev = new Vector2Int(idx.x + stepX, idx.y + stepY);
                if (byIndex.TryGetValue(prev, out var p) && p != data)
                {
                    data.requiredRegionIds.Add(p.regionId);
                    EditorUtility.SetDirty(data);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Land] Da sinh {created.Count} khu dat trong {AssetFolder}.");
        EditorUtility.DisplayDialog("Khu Dat",
            $"Da sinh {created.Count} khu vao {AssetFolder}.\n\nBuoc tiep: bam \"2. Dung LandExpansion vao Scene\".", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    private void BuildSceneObject()
    {
        var existing = Object.FindFirstObjectByType<LandExpansionManager>();
        GameObject root = existing != null ? existing.gameObject : new GameObject("LandExpansion");
        var mgr = existing != null ? existing : root.AddComponent<LandExpansionManager>();

        // nap toan bo asset khu
        mgr.regions.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:LandRegionData"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var d = AssetDatabase.LoadAssetAtPath<LandRegionData>(p);
            if (d != null) mgr.regions.Add(d);
        }

        // tilemap phu khu chua mua — dat trong Grid_Iso45 de dung he toa do o
        var gridGo = GameObject.Find(IsoGrid.IsoGridObjectName);
        if (gridGo != null)
        {
            var overlay = gridGo.transform.Find("Tilemap_LockedOverlay");
            if (overlay == null)
            {
                var go = new GameObject("Tilemap_LockedOverlay", typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(gridGo.transform, false);
                var r = go.GetComponent<TilemapRenderer>();
                r.sortingOrder = 7;                       // tren moi tile nen, duoi UI
                r.mode = TilemapRenderer.Mode.Individual;
                overlay = go.transform;
            }
            mgr.lockedOverlayTilemap = overlay.GetComponent<Tilemap>();

            var fence = gridGo.transform.Find("Tilemap_IsoFence");
            if (fence != null) mgr.fenceTilemap = fence.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning($"[Land] Khong tim thay {IsoGrid.IsoGridObjectName} trong scene — " +
                             "hay keo tay 2 tilemap vao LandExpansionManager.");
        }

        // rule tile hang rao
        if (mgr.fenceTile == null)
        {
            var fenceRule = AssetDatabase.LoadAssetAtPath<TileBase>(
                "Assets/maptitle/Map45Iso/RuleTile_IsoFence45.asset");
            if (fenceRule != null) mgr.fenceTile = fenceRule;
        }

        EditorUtility.SetDirty(mgr);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Khu Dat",
            $"Da dung LandExpansion voi {mgr.regions.Count} khu.\n\n" +
            "Con lai (lam tay):\n" +
            "• Keo tile 'co dai / lop toi' vao o Locked Overlay Tile\n" +
            "• Keo prefab bien bao vao o Sign Prefab (neu muon bam mua tren map)\n" +
            "• Nho Save Scene (Ctrl+S)", "OK");
    }
}
#endif
