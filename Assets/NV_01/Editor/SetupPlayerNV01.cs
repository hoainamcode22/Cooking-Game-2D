#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Tự động dựng nhân vật 4 hướng từ 4 sprite sheet trong Assets/NV_01.
/// Bấm menu "Tools/Setup Player NV_01" → slice 5x5 + tạo 8 clip + Animator (2D blend tree) + ráp Player.prefab.
///
/// Cần package "2D Sprite" (com.unity.2d.sprite) để slice bằng ISpriteEditorDataProvider.
/// </summary>
public static class SetupPlayerNV01
{
    private const string Folder         = "Assets/NV_01";
    private const string AnimFolder     = Folder + "/Animations";
    private const string ControllerPath = Folder + "/Player.controller";
    private const string PrefabPath      = Folder + "/Player.prefab";

    private const int    Cols          = 5;
    private const int    Rows          = 5;
    private const float  PixelsPerUnit = 100f;   // chỉnh cho khớp tile nếu cần
    private const int    FrameRate     = 12;
    private const string SortLayerName = "Objects"; // layer để Y-sort chung với công trình

    // Từ khoá hướng trong tên file  →  tên clip cơ sở.
    private static readonly (string keyword, string clip)[] Dirs =
    {
        ("walk_down",  "Walk_Down"),
        ("walk_up",    "Walk_Up"),
        ("walk_left",  "Walk_Left"),
        ("walk_right", "Walk_Right"),
    };

    [MenuItem("Tools/Setup Player NV_01")]
    public static void Setup()
    {
        Debug.Log("[SetupPlayerNV01] ===== BẮT ĐẦU =====");
        EnsureFolder(AnimFolder);

        var walkClips = new Dictionary<string, AnimationClip>();
        var idleClips = new Dictionary<string, AnimationClip>();
        var firstFrame = new Dictionary<string, Sprite>();

        // 1) + 2) Slice từng sheet rồi tạo clip Walk + Idle.
        foreach (var d in Dirs)
        {
            string path = FindSheet(d.keyword);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[SetupPlayerNV01] KHÔNG tìm thấy sheet chứa '{d.keyword}' trong {Folder}. Dừng.");
                return;
            }
            Debug.Log($"[SetupPlayerNV01] [1] Sheet {d.clip}: {path}");

            SliceGrid(path, d.clip);
            Debug.Log($"[SetupPlayerNV01] [1] Đã slice 5x5 + pivot bottom-center: {d.clip}");

            List<Sprite> frames = LoadSortedSprites(path, d.clip);
            if (frames.Count < Cols * Rows)
            {
                Debug.LogError($"[SetupPlayerNV01] {d.clip}: chỉ đọc được {frames.Count}/{Cols * Rows} frame. Dừng.");
                return;
            }
            firstFrame[d.clip] = frames[0];

            walkClips[d.clip] = CreateSpriteClip(d.clip, frames, true);
            string idleName = "Idle_" + d.clip.Substring("Walk_".Length); // Idle_Down...
            idleClips[d.clip] = CreateSpriteClip(idleName, new List<Sprite> { frames[0] }, false);
            Debug.Log($"[SetupPlayerNV01] [2] Tạo clip {d.clip} (25f, loop) + {idleName} (1f).");
        }

        // 3) Animator controller + blend tree.
        AnimatorController controller = BuildController(walkClips, idleClips);
        Debug.Log($"[SetupPlayerNV01] [3] Animator controller: {ControllerPath}");

        // 4) + 5) Ráp prefab.
        BuildPrefab(controller, firstFrame["Walk_Down"]);
        Debug.Log($"[SetupPlayerNV01] [5] Prefab: {PrefabPath}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupPlayerNV01] ===== HOÀN TẤT =====");
        EditorUtility.DisplayDialog("Setup Player NV_01",
            "Hoàn tất!\n\nPrefab: " + PrefabPath +
            "\n\nKéo Player.prefab vào scene → Play → đi bằng WASD / mũi tên.",
            "OK");
    }

    // ── Tìm sheet theo từ khoá hướng ─────────────────────────────────────────────
    private static string FindSheet(string keyword)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(p).ToLowerInvariant().Contains(keyword))
                return p;
        }
        return null;
    }

    // ── Import + slice lưới 5x5 (pivot bottom-center) bằng ISpriteEditorDataProvider ─
    private static void SliceGrid(string path, string clipBase)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType        = TextureImporterType.Sprite;
        importer.spriteImportMode   = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode         = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.sRGBTexture        = true;
        importer.mipmapEnabled      = false;

        var ts = new TextureImporterSettings();
        importer.ReadTextureSettings(ts);
        ts.spriteMeshType  = SpriteMeshType.FullRect;
        ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importer.SetTextureSettings(ts);
        importer.SaveAndReimport();

        // Lấy lại importer sau reimport để data provider đọc đúng trạng thái Multiple.
        importer = (TextureImporter)AssetImporter.GetAtPath(path);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cw = tex.width  / Cols;
        int ch = tex.height / Rows;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        var pairs = new List<SpriteNameFileIdPair>();
        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Cols; col++)
        {
            int index = row * Cols + col;                 // 0..24, trái→phải, trên→xuống
            float x = col * cw;
            float y = (Rows - 1 - row) * ch;              // hàng trên cùng = index nhỏ
            var id = GUID.Generate();
            var sr = new SpriteRect
            {
                name      = $"{clipBase}_{index}",
                spriteID  = id,
                rect      = new Rect(x, y, cw, ch),
                alignment = SpriteAlignment.BottomCenter, // chân chạm đất → Y-sort đúng
                pivot     = new Vector2(0.5f, 0f),
                border    = Vector4.zero,
            };
            rects.Add(sr);
            pairs.Add(new SpriteNameFileIdPair(sr.name, id));
        }

        dp.SetSpriteRects(rects.ToArray());
        var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProv != null) nameProv.SetNameFileIdPairs(pairs);

        dp.Apply();
        importer.SaveAndReimport();
    }

    // ── Đọc sprite con, SORT theo số index ở đuôi tên (không sort chuỗi thô) ───────
    private static List<Sprite> LoadSortedSprites(string path, string clipBase)
    {
        var list = new List<Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s && s.name.StartsWith(clipBase + "_"))
                list.Add(s);

        list.Sort((a, b) => IndexSuffix(a.name).CompareTo(IndexSuffix(b.name)));
        return list;
    }

    private static int IndexSuffix(string name)
    {
        int u = name.LastIndexOf('_');
        return (u >= 0 && int.TryParse(name.Substring(u + 1), out int v)) ? v : 0;
    }

    // ── Tạo 1 clip sprite (bind vào SpriteRenderer.m_Sprite) ──────────────────────
    private static AnimationClip CreateSpriteClip(string name, List<Sprite> frames, bool loop)
    {
        string p = $"{AnimFolder}/{name}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(p) != null)
            AssetDatabase.DeleteAsset(p);

        var clip = new AnimationClip { frameRate = FrameRate };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",                 // SpriteRenderer ở cùng object với Animator (root)
            propertyName = "m_Sprite",
        };

        var keys = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)FrameRate, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, p);
        return clip;
    }

    // ── Animator controller: Idle/Walk = 2D Simple Directional blend tree ─────────
    private static AnimatorController BuildController(
        Dictionary<string, AnimationClip> walk, Dictionary<string, AnimationClip> idle)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        c.AddParameter("MoveX",    AnimatorControllerParameterType.Float);
        c.AddParameter("MoveY",    AnimatorControllerParameterType.Float);
        c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        var sm = c.layers[0].stateMachine;

        AnimatorState idleState = c.CreateBlendTreeInController("Idle", out BlendTree idleTree, 0);
        FillDirTree(idleTree, idle);

        AnimatorState walkState = c.CreateBlendTreeInController("Walk", out BlendTree walkTree, 0);
        FillDirTree(walkTree, walk);

        sm.defaultState = idleState;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false; toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false; toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    private static void FillDirTree(BlendTree tree, Dictionary<string, AnimationClip> clips)
    {
        tree.blendType       = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter  = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.AddChild(clips["Walk_Down"],  new Vector2(0f, -1f));
        tree.AddChild(clips["Walk_Up"],    new Vector2(0f,  1f));
        tree.AddChild(clips["Walk_Left"],  new Vector2(-1f, 0f));
        tree.AddChild(clips["Walk_Right"], new Vector2(1f,  0f));
    }

    // ── Ráp prefab Player ─────────────────────────────────────────────────────────
    private static void BuildPrefab(AnimatorController controller, Sprite defaultSprite)
    {
        var go = new GameObject("Player");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = defaultSprite;
        if (SortingLayerExists(SortLayerName)) sr.sortingLayerName = SortLayerName;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation; // khoá xoay Z
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider nhỏ ở CHÂN (pivot bottom-center → chân ở local y≈0).
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size      = new Vector2(0.45f, 0.28f);
        col.offset    = new Vector2(0f, 0.18f);

        go.AddComponent<PlayerMovement>();
        go.AddComponent<YSortIso>();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────
    private static bool SortingLayerExists(string n)
    {
        foreach (var l in SortingLayer.layers) if (l.name == n) return true;
        return false;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf   = System.IO.Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
