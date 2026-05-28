#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// FarmTools → Pen Animal Setup
/// 1 click:
///   • Gắn HappyHarvestAnimalVisualSpawner đúng con vật vào Pen_01–04
///   • Fix sorting pen (CongTrinh/500)
///   • Fix sorting animal prefab — giữ relative order giữa thân/đầu/mắt/chân
/// </summary>
public class PenAnimalSetupTool : EditorWindow
{
    // ── Config từng chuồng ──────────────────────────────────────────────────────
    private class PenConfig
    {
        public string label;
        public string penPath;
        public string animalPath;
        public string legacyChildName;
        public string spawnedChildName;
        public Vector3 localPosition;
        public Vector3 localScale;
        public int sortingOrderOffset = 10;
    }

    private static readonly PenConfig[] Pens =
    {
        new PenConfig
        {
            label             = "Pen_01 — Bò Nâu",
            penPath           = "Assets/_Game/Farm/CÔNG TRÌNH/Pen_01.prefab",
            animalPath        = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Prefab_Cow_Brown.prefab",
            legacyChildName   = "bonam1",
            spawnedChildName  = "HappyHarvest_Cow_Brown",
            localPosition     = new Vector3(0.02f, -0.07f, 0f),
            localScale        = new Vector3(0.13f, 0.13f, 0.13f),
            sortingOrderOffset = 10
        },
        new PenConfig
        {
            label             = "Pen_02 — Heo",
            penPath           = "Assets/_Game/Farm/CÔNG TRÌNH/Pen_02.prefab",
            animalPath        = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Piggy/Prefab_piggy.prefab",
            legacyChildName   = "heonam1_0",
            spawnedChildName  = "HappyHarvest_Pig",
            localPosition     = new Vector3(0f, 0f, 0f),
            localScale        = new Vector3(0.13f, 0.13f, 0.13f),
            sortingOrderOffset = 10
        },
        new PenConfig
        {
            label             = "Pen_03 — Gà",
            penPath           = "Assets/_Game/Farm/CÔNG TRÌNH/Pen_03.prefab",
            animalPath        = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Chicken/Prefab_Chicken.prefab",
            legacyChildName   = "ganam1_0",
            spawnedChildName  = "HappyHarvest_Chicken",
            localPosition     = new Vector3(0.01f, 0.1f, 0f),
            localScale        = new Vector3(0.10f, 0.10f, 0.10f),
            sortingOrderOffset = 10
        },
        new PenConfig
        {
            label             = "Pen_04 — Bò Sữa",
            penPath           = "Assets/_Game/Farm/CÔNG TRÌNH/Pen_04.prefab",
            animalPath        = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Prefab_Cow.prefab",
            legacyChildName   = "ganam1_0",
            spawnedChildName  = "HappyHarvest_Cow",
            localPosition     = new Vector3(0.02f, -0.07f, 0f),
            localScale        = new Vector3(0.13f, 0.13f, 0.13f),
            sortingOrderOffset = 10
        }
    };

    // ── Sorting constants ───────────────────────────────────────────────────────
    private const string SortingLayer   = "CongTrinh";
    private const int    BuildingOrder  = 500;   // pen/chuồng
    private const int    AnimalBaseOrder = 510;  // animal base (= BuildingOrder + offset 10)

    // ── GUI state ───────────────────────────────────────────────────────────────
    private Vector2 scrollPos;
    private readonly List<string> log = new List<string>();

    // ── Menu entry ──────────────────────────────────────────────────────────────
    [MenuItem("FarmTools/Pen Animal Setup")]
    public static void ShowWindow()
    {
        var win = GetWindow<PenAnimalSetupTool>("Pen Animal Setup");
        win.minSize = new Vector2(500, 560);
    }

    // ── GUI ─────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Pen Animal Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1 click thực hiện 3 việc:\n" +
            "  1. Gắn HappyHarvestAnimalVisualSpawner (đúng prefab) vào Pen_01–04\n" +
            "  2. Fix sorting pen → CongTrinh / 500\n" +
            "  3. Fix sorting animal prefab → CongTrinh / 510+ (giữ relative order mắt/thân/chân)\n\n" +
            "Không chạm Tilemap.",
            MessageType.Info);
        GUILayout.Space(6);

        // Bảng trạng thái
        DrawHeader();
        foreach (var cfg in Pens)
            DrawPenRow(cfg);

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f);
        if (GUILayout.Button("▶  Setup All Pens  (1 Click)", GUILayout.Height(38)))
        {
            log.Clear();
            SetupAll();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        if (log.Count > 0)
        {
            EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var line in log)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Pen", EditorStyles.boldLabel, GUILayout.Width(160));
            EditorGUILayout.LabelField("Pen OK", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Animal OK", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Animal Prefab", EditorStyles.boldLabel);
        }
    }

    private void DrawPenRow(PenConfig cfg)
    {
        bool penOk    = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.penPath)    != null;
        bool animalOk = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.animalPath) != null;

        using (new EditorGUILayout.HorizontalScope("box"))
        {
            EditorGUILayout.LabelField(cfg.label, GUILayout.Width(160));

            GUI.color = penOk    ? Color.green : Color.red;
            EditorGUILayout.LabelField(penOk    ? "✓" : "✗", GUILayout.Width(60));

            GUI.color = animalOk ? Color.green : Color.red;
            EditorGUILayout.LabelField(animalOk ? "✓" : "✗", GUILayout.Width(70));

            GUI.color = Color.white;
            EditorGUILayout.LabelField(System.IO.Path.GetFileName(cfg.animalPath));
        }
    }

    // ── Setup All ───────────────────────────────────────────────────────────────

    private void SetupAll()
    {
        // Bước 1 & 2: Gán spawner + fix pen sorting
        log.Add("=== BƯỚC 1-2: Setup spawner + pen sorting ===");
        int penOk = 0;
        foreach (var cfg in Pens)
            if (SetupPen(cfg)) penOk++;

        // Bước 3: Fix animal prefab sorting (giữ relative order mắt/thân/chân)
        log.Add("=== BƯỚC 3: Fix animal prefab sorting ===");
        int animalOk = 0;
        var done = new HashSet<string>();
        foreach (var cfg in Pens)
        {
            if (done.Contains(cfg.animalPath)) continue;
            done.Add(cfg.animalPath);
            if (FixAnimalPrefabSorting(cfg.animalPath, cfg.label, cfg.sortingOrderOffset))
                animalOk++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"[PenSetup] Xong: pen {penOk}/{Pens.Length} | animal {animalOk}/{done.Count}";
        log.Add(summary);
        Debug.Log(summary);
        Repaint();
    }

    // ── Setup 1 pen ─────────────────────────────────────────────────────────────

    private bool SetupPen(PenConfig cfg)
    {
        GameObject penAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.penPath);
        if (penAsset == null)
        {
            Warn($"  ✗ {cfg.label}: pen prefab không tìm thấy → {cfg.penPath}"); return false;
        }

        GameObject animalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.animalPath);
        if (animalAsset == null)
        {
            Warn($"  ✗ {cfg.label}: animal prefab không tìm thấy → {cfg.animalPath}"); return false;
        }

        string path = AssetDatabase.GetAssetPath(penAsset);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            bool changed = false;

            // Tìm hoặc thêm spawner
            var spawner = root.GetComponent<Assetsgame.Animals.HappyHarvestAnimalVisualSpawner>();
            if (spawner == null)
            {
                spawner = root.AddComponent<Assetsgame.Animals.HappyHarvestAnimalVisualSpawner>();
                log.Add($"  + {cfg.label}: thêm mới spawner");
                changed = true;
            }

            // Gán fields
            var so = new SerializedObject(spawner);
            changed |= SetProp(so, "animalPrefab",       animalAsset);
            changed |= SetProp(so, "legacyChildName",    cfg.legacyChildName);
            changed |= SetProp(so, "spawnedChildName",   cfg.spawnedChildName);
            changed |= SetProp(so, "localPosition",      cfg.localPosition);
            changed |= SetProp(so, "localScale",         cfg.localScale);
            changed |= SetProp(so, "sortingOrderOffset", cfg.sortingOrderOffset);
            so.ApplyModifiedProperties();

            // Fix sorting pen (flat — pen chỉ có BarnSprite)
            changed |= FixPenSorting(root, cfg.label);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.Add($"  ✓ {cfg.label}{(changed ? " (updated)" : " (no change)")}");
            return true;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── Fix pen SpriteRenderers ─────────────────────────────────────────────────
    // Pen chỉ có sprite chuồng → set CongTrinh/500, không cần giữ relative order

    private bool FixPenSorting(GameObject root, string label)
    {
        bool changed = false;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sortingLayerName != SortingLayer) { sr.sortingLayerName = SortingLayer; changed = true; }
            if (sr.sortingOrder < BuildingOrder)     { sr.sortingOrder = BuildingOrder;    changed = true; }
        }
        if (changed) log.Add($"    → pen sorting: {SortingLayer}/{BuildingOrder}+");
        return changed;
    }

    // ── Fix animal prefab SpriteRenderers ───────────────────────────────────────
    // Giữ nguyên relative order giữa bộ phận (thân=0, đầu=2, mắt=4 → 510, 512, 514)

    private bool FixAnimalPrefabSorting(string animalPath, string label, int sortingOrderOffset)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(animalPath);
        if (asset == null)
        {
            Warn($"  ✗ Animal không tìm thấy: {animalPath}"); return false;
        }

        string path = AssetDatabase.GetAssetPath(asset);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
            {
                log.Add($"  ! {label}: animal không có SpriteRenderer");
                return false;
            }

            // Tìm order nhỏ nhất gốc → dùng làm pivot để tính relative offset
            int minOrder = int.MaxValue;
            foreach (var sr in renderers)
                if (sr.sortingOrder < minOrder) minOrder = sr.sortingOrder;

            int baseOrder = AnimalBaseOrder; // 510

            bool changed = false;
            foreach (var sr in renderers)
            {
                string targetLayer = SortingLayer;
                int    targetOrder = baseOrder + (sr.sortingOrder - minOrder);
                // Ví dụ gốc: thân=0→510, đầu=2→512, mắt=4→514

                if (sr.sortingLayerName != targetLayer || sr.sortingOrder != targetOrder)
                {
                    sr.sortingLayerName = targetLayer;
                    sr.sortingOrder     = targetOrder;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add($"  ✓ Animal [{System.IO.Path.GetFileName(animalPath)}]: " +
                        $"minSrc={minOrder} → base={baseOrder}, {renderers.Length} renderers fixed");
            }
            else
            {
                log.Add($"  – Animal [{System.IO.Path.GetFileName(animalPath)}]: no change");
            }
            return true;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static bool SetProp(SerializedObject so, string name, object value)
    {
        var prop = so.FindProperty(name);
        if (prop == null) return false;
        switch (value)
        {
            case Object   o: if (prop.objectReferenceValue == o)           return false; prop.objectReferenceValue = o; return true;
            case string   s: if (prop.stringValue == s)                    return false; prop.stringValue = s;          return true;
            case Vector3  v: if (prop.vector3Value == v)                   return false; prop.vector3Value = v;         return true;
            case int      i: if (prop.intValue == i)                       return false; prop.intValue = i;             return true;
            case float    f: if (Mathf.Approximately(prop.floatValue, f))  return false; prop.floatValue = f;           return true;
            default: return false;
        }
    }

    private void Warn(string msg) { log.Add(msg); Debug.LogWarning(msg); }
}
#endif
