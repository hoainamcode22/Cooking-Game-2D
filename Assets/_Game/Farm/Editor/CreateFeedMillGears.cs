#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tool dựng cụm bánh răng "FeedMillGears" cho máy làm thức ăn gia súc.
///
/// Hierarchy dựng ra:
///   FeedMillGears (root + SortingGroup + FeedMillController)
///    ├── Gear_Big   (SpriteRenderer + RotatingGear)
///    └── Gear_Small (SpriteRenderer + RotatingGear, scale 0.7, lệch chéo ăn khớp)
///
/// 2 menu:
///  • "Create FeedMill Gears Prefab"          → chỉ tạo/cập nhật prefab asset.
///  • "Add FeedMill Gears To Scene (selection)"→ tạo prefab (nếu chưa có) RỒI thả 1 bản
///    vào scene đang mở, gắn làm CON của object đang chọn (vd nhà). Bấm 1 phát là xong,
///    việc còn lại chỉ nhích 2 bánh vào hốc.
///
/// Sprite bánh răng TỰ TÌM theo tên ('gear_clean' ưu tiên, sau đó bất kỳ 'gear') — không hardcode path.
/// </summary>
public static class CreateFeedMillGears
{
    private const string MENU_PREFAB = "Tools/Farm Game/Create FeedMill Gears Prefab";
    private const string MENU_SCENE  = "Tools/Farm Game/Add FeedMill Gears To Scene (selection)";

    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath   = PrefabFolder + "/FeedMillGears.prefab";

    // Gears nằm TRÊN thân nhà, DƯỚI prop tiền cảnh.
    private const string SortingLayer = "Objects";
    private const int    SortingOrder = 10;

    // ── Menu 1: chỉ tạo prefab ──────────────────────────────────────────────────
    [MenuItem(MENU_PREFAB)]
    public static void CreatePrefab()
    {
        var prefab = EnsurePrefab(out string msg);
        if (prefab == null) { EditorUtility.DisplayDialog("FeedMill Gears", msg, "OK"); return; }

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        EditorUtility.DisplayDialog("FeedMill Gears",
            "Đã tạo/cập nhật prefab:\n" + PrefabPath + "\n\n" + msg +
            "\n\nDùng menu 'Add FeedMill Gears To Scene (selection)' để thả thẳng vào nhà.",
            "OK");
    }

    // ── Menu 2: tạo prefab (nếu cần) + thả vào scene dưới object đang chọn ─────────
    [MenuItem(MENU_SCENE)]
    public static void AddToScene()
    {
        var prefab = EnsurePrefab(out string msg);
        if (prefab == null) { EditorUtility.DisplayDialog("FeedMill Gears", msg, "OK"); return; }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
        {
            EditorUtility.DisplayDialog("FeedMill Gears", "Không tạo được bản trong scene.", "OK");
            return;
        }

        // Gắn làm CON của object đang chọn (nếu chọn 1 object trong scene); không thì để ở gốc scene.
        GameObject parent = Selection.activeGameObject;
        bool parented = parent != null && parent.scene.IsValid();
        if (parented)
            instance.transform.SetParent(parent.transform, false);
        instance.transform.localPosition = Vector3.zero;

        Undo.RegisterCreatedObjectUndo(instance, "Add FeedMillGears");
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        EditorUtility.DisplayDialog("FeedMill Gears",
            "Đã thả FeedMillGears vào scene" +
            (parented ? " làm CON của '" + parent.name + "'." : " (ở gốc scene — chưa chọn nhà nên để rời).") +
            "\n\nViệc còn lại: nhích 2 bánh vào hốc bánh răng + chỉnh localScale root cho vừa.\n" +
            "Bật máy: gọi FeedMillController.StartWorking() hoặc Play → chuột phải component → Test Start.",
            "OK");
    }

    // ── Tạo/đảm bảo prefab tồn tại; trả prefab asset ─────────────────────────────
    private static GameObject EnsurePrefab(out string msg)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) { msg = "Sprite/đã dựng sẵn — dùng prefab hiện có."; return existing; }

        Sprite gear = FindGearSprite();
        if (gear == null)
        {
            msg = "Không tìm thấy sprite bánh răng. Import gear_clean.png (Single) hoặc spritesheet (Multiple) rồi chạy lại.";
            return null;
        }

        GameObject root = BuildGearsRoot(gear);

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
        Object.DestroyImmediate(root);

        if (!ok || prefab == null) { msg = "Lưu prefab THẤT BẠI."; return null; }

        AssetDatabase.SaveAssets();
        msg = "Sprite dùng: " + gear.name + "  |  Sorting: " + SortingLayer + " / order " + SortingOrder + ".";
        return prefab;
    }

    // ── Dựng hierarchy bánh răng (đã wire đầy đủ) ────────────────────────────────
    private static GameObject BuildGearsRoot(Sprite gear)
    {
        var root = new GameObject("FeedMillGears");
        var sg = root.AddComponent<SortingGroup>();
        sg.sortingLayerName = SortingLayer;
        sg.sortingOrder     = SortingOrder;
        var controller = root.AddComponent<FeedMillController>();

        // Gear_Big
        var big   = new GameObject("Gear_Big", typeof(SpriteRenderer), typeof(RotatingGear));
        big.transform.SetParent(root.transform, false);
        var bigSR = big.GetComponent<SpriteRenderer>();
        bigSR.sprite = gear; bigSR.sortingLayerName = SortingLayer; bigSR.sortingOrder = 0;
        var bigGear = big.GetComponent<RotatingGear>();

        // Gear_Small (0.7, lệch chéo dưới-phải để mép răng chạm bánh lớn)
        var small   = new GameObject("Gear_Small", typeof(SpriteRenderer), typeof(RotatingGear));
        small.transform.SetParent(root.transform, false);
        small.transform.localScale = Vector3.one * 0.7f;
        var smallSR = small.GetComponent<SpriteRenderer>();
        smallSR.sprite = gear; smallSR.sortingLayerName = SortingLayer; smallSR.sortingOrder = 1;
        var smallGear = small.GetComponent<RotatingGear>();

        float r = bigSR.sprite != null ? bigSR.sprite.bounds.extents.x : 1f;
        small.transform.localPosition = new Vector3(r * 1.45f, -r * 0.95f, 0f);

        // Wire bigGear/smallGear (field private → SerializedObject)
        var so = new SerializedObject(controller);
        var pBig = so.FindProperty("bigGear");   if (pBig   != null) pBig.objectReferenceValue   = bigGear;
        var pSml = so.FindProperty("smallGear"); if (pSml != null) pSml.objectReferenceValue = smallGear;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // ── Tìm sprite ───────────────────────────────────────────────────────────────
    private static Sprite FindGearSprite()
    {
        Sprite found = FindFirstSprite("gear_clean");
        return found != null ? found : FindFirstSprite("gear");
    }

    private static Sprite FindFirstSprite(string keyword)
    {
        foreach (string guid in AssetDatabase.FindAssets(keyword + " t:Sprite"))
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sp != null) return sp;
        }
        foreach (string guid in AssetDatabase.FindAssets(keyword + " t:Texture2D"))
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                if (obj is Sprite sp2) return sp2;
        }
        return null;
    }
}
#endif
