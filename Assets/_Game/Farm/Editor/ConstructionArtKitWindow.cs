using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// EDITOR TOOL — `Tools/Farm/Bảng Ô Art Xây Dựng`.
///
/// MỘT CHỖ DUY NHẤT để xem toàn bộ ô art của hệ "đang xây": ô nào đã có art, ô nào còn
/// trống, mỗi ô mang MÀU NHẬN DẠNG nào trong Scene. Kéo thẳng sprite vào từng dòng là
/// asset kit được ghi ngay, không cần mở Inspector và dò trong 19 field.
///
/// VÌ SAO CẦN TOOL NÀY: `ConstructionManager` TỰ MỌC lúc chạy (RuntimeInitializeOnLoad)
/// nên trong scene có thể KHÔNG có object nào để kéo kit vào — kit sẽ mãi null và Edric
/// tưởng mình gán sai. Nút "Gắn kit vào scene" bên dưới xử lý đúng cái bẫy đó.
/// </summary>
public class ConstructionArtKitWindow : EditorWindow
{
    private const string KitFolder = "Assets/_Game/Farm/ScriptableObjects";

    private ConstructionArtKit _kit;
    private SerializedObject   _so;
    private Vector2            _scroll;

    // ─────────────────────────────────────────────────────────────────────────
    // BẢNG TRA: ô → tên field trong asset + mô tả ngắn
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tên field C# tương ứng mỗi ô — dùng cho SerializedProperty (có Undo sẵn).</summary>
    private static string FieldOf(ConstructionArtKit.Slot slot) => slot switch
    {
        ConstructionArtKit.Slot.GroundPatch   => "groundPatch",
        ConstructionArtKit.Slot.ScaffoldPost  => "scaffoldPost",
        ConstructionArtKit.Slot.ScaffoldRail  => "scaffoldRail",
        ConstructionArtKit.Slot.ScaffoldBrace => "scaffoldBrace",
        ConstructionArtKit.Slot.LeaningBoard  => "leaningBoard",
        ConstructionArtKit.Slot.Worker        => "worker",
        ConstructionArtKit.Slot.DustParticle  => "dustParticle",
        ConstructionArtKit.Slot.NamePlateBg   => "namePlateBg",
        ConstructionArtKit.Slot.TimerBarBg    => "timerBarBg",
        ConstructionArtKit.Slot.ClockIcon     => "clockIcon",
        ConstructionArtKit.Slot.RushButtonBg  => "rushButtonBg",
        ConstructionArtKit.Slot.CoinIcon      => "coinIcon",
        ConstructionArtKit.Slot.GemIcon       => "gemIcon",
        ConstructionArtKit.Slot.PriceBarBg    => "priceBarBg",
        ConstructionArtKit.Slot.GiftBoxSide   => "giftBoxSide",
        ConstructionArtKit.Slot.Ribbon        => "ribbon",
        ConstructionArtKit.Slot.Rosette       => "rosette",
        ConstructionArtKit.Slot.Balloon       => "balloon",
        ConstructionArtKit.Slot.HardHatDone   => "hardHatDone",
        _                                     => null
    };

    /// <summary>Mô tả ngắn — chép lại từ tooltip trong ConstructionArtKit.cs.</summary>
    private static string DescOf(ConstructionArtKit.Slot slot) => slot switch
    {
        ConstructionArtKit.Slot.GroundPatch   => "Thảm đất lộ ra dưới chân công trường, phủ đúng N×M ô.",
        ConstructionArtKit.Slot.ScaffoldPost  => "Cọc gỗ dựng ĐỨNG của giàn giáo. Sprite dọc, pivot giữa.",
        ConstructionArtKit.Slot.ScaffoldRail  => "Thanh gỗ NGANG nối các cọc. Sprite ngang, pivot giữa.",
        ConstructionArtKit.Slot.ScaffoldBrace => "Thanh chống CHÉO hai bên giàn giáo.",
        ConstructionArtKit.Slot.LeaningBoard  => "Tấm ván dựa nghiêng vào giàn giáo.",
        ConstructionArtKit.Slot.Worker        => "Công nhân (sprite tĩnh). Có Animator thì dùng ô Prefab bên dưới.",
        ConstructionArtKit.Slot.DustParticle  => "Hạt bụi/khói bay lên. Nên để ở texture riêng, ĐỪNG đóng atlas.",
        ConstructionArtKit.Slot.NamePlateBg   => "Nền sau TÊN công trình. Nên 9-slice. Trống = chỉ có chữ.",
        ConstructionArtKit.Slot.TimerBarBg    => "Nền thanh đếm ngược. Sprite 9-slice bo góc.",
        ConstructionArtKit.Slot.ClockIcon     => "Icon đồng hồ bên trái con số thời gian.",
        ConstructionArtKit.Slot.RushButtonBg  => "Nền nút tăng tốc. Sprite 9-slice bo góc.",
        ConstructionArtKit.Slot.CoinIcon      => "Icon xu trên nút tăng tốc (khi trừ bằng vàng).",
        ConstructionArtKit.Slot.GemIcon       => "Icon kim cương (khi trừ bằng gem).",
        ConstructionArtKit.Slot.PriceBarBg    => "Nền thanh giá phía trên 3 nút ✕ ↻ ✓ lúc đặt.",
        ConstructionArtKit.Slot.GiftBoxSide   => "Mặt hộp quà bọc công trình lúc khánh thành.",
        ConstructionArtKit.Slot.Ribbon        => "Dải ruy băng quấn quanh hộp.",
        ConstructionArtKit.Slot.Rosette       => "Hoa hồng ruy băng gắn trên đỉnh hộp.",
        ConstructionArtKit.Slot.Balloon       => "Bóng bay. Ô trống = tất cả màu đỏ; có art = rải đỏ/vàng/hồng.",
        ConstructionArtKit.Slot.HardHatDone   => "Mũ bảo hộ + tick xanh bật lên khi xây xong.",
        _                                     => ""
    };

    /// <summary>
    /// Ô nào CHƯA được nối dây. Hiện KHÔNG còn ô nào — cả 19/19 đã nối,
    /// kể cả PriceBarBg (nối ở PlacementGhostVisualController.EnsurePriceBar).
    /// Giữ hàm lại để sau này thêm ô mới thì đánh dấu được ngay.
    /// </summary>
    private static bool IsNotWiredYet(ConstructionArtKit.Slot slot) => false;

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm/Bảng Ô Art Xây Dựng")]
    public static void Open()
    {
        var win = GetWindow<ConstructionArtKitWindow>(false, "Bảng Ô Art Xây Dựng", true);
        win.minSize = new Vector2(880f, 520f);
        win.AutoFindKit();
        win.Show();
    }

    private void OnEnable() => AutoFindKit();

    /// <summary>Tự tìm asset kit đầu tiên trong project — đỡ phải kéo tay mỗi lần mở tool.</summary>
    private void AutoFindKit()
    {
        if (_kit != null) { Bind(); return; }

        string[] guids = AssetDatabase.FindAssets("t:ConstructionArtKit");
        if (guids.Length > 0)
            _kit = AssetDatabase.LoadAssetAtPath<ConstructionArtKit>(
                       AssetDatabase.GUIDToAssetPath(guids[0]));

        Bind();
    }

    private void Bind() => _so = _kit != null ? new SerializedObject(_kit) : null;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();

        if (_kit == null)
        {
            EditorGUILayout.HelpBox(
                "Chưa có asset ConstructionArtKit nào.\n" +
                "Bấm 'Tạo kit mới' bên trên — hoặc chuột phải trong Project ▸ " +
                "Create ▸ FarmGame ▸ Construction Art Kit.", MessageType.Info);
            return;
        }

        if (_so == null || _so.targetObject == null) Bind();
        if (_so == null) return;

        _so.Update();
        EditorGUI.BeginChangeCheck();

        DrawFlags();
        DrawTable();

        bool changed = EditorGUI.EndChangeCheck();
        _so.ApplyModifiedProperties();

        // Ghi thẳng vào asset ngay khi kéo sprite vào — Edric không phải nhớ Ctrl+S.
        if (changed)
        {
            EditorUtility.SetDirty(_kit);
            AssetDatabase.SaveAssets();
        }

        DrawFooter();
    }

    // ── Trên cùng ────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Asset kit", GUILayout.Width(70f));

            var picked = (ConstructionArtKit)EditorGUILayout.ObjectField(
                _kit, typeof(ConstructionArtKit), false);

            if (picked != _kit)
            {
                _kit = picked;
                Bind();
            }

            if (GUILayout.Button("Tạo kit mới", GUILayout.Width(110f)))
                CreateKit();

            using (new EditorGUI.DisabledScope(_kit == null))
            {
                if (GUILayout.Button(new GUIContent("Gắn kit vào scene",
                        "Tìm (hoặc tạo) ConstructionManager trong scene đang mở rồi gán kit vào đó. " +
                        "Bắt buộc phải làm một lần, vì manager tự mọc lúc chạy sẽ KHÔNG có kit."),
                        GUILayout.Width(150f)))
                {
                    AttachKitToScene();
                }
            }
        }

        EditorGUILayout.Space(2f);
        DrawSeparator();
    }

    private void DrawFlags()
    {
        SerializedProperty labels = _so.FindProperty("showSlotLabels");
        SerializedProperty force  = _so.FindProperty("forcePlaceholderColors");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (labels != null)
            {
                labels.boolValue = EditorGUILayout.ToggleLeft(
                    new GUIContent("Hiện nhãn tên ô trong Scene  (showSlotLabels)",
                        "Bật để mỗi mảnh placeholder mang một nhãn chữ ghi tên ô. NHỚ TẮT trước khi build."),
                    labels.boolValue, GUILayout.Width(330f));
            }

            if (force != null)
            {
                force.boolValue = EditorGUILayout.ToggleLeft(
                    new GUIContent("Ép màu nhận dạng cả khi đã có art  (forcePlaceholderColors)",
                        "Dùng lúc căn chỉnh vị trí: art thật vẫn bị tô màu ô."),
                    force.boolValue);
            }
        }

        EditorGUILayout.Space(4f);
    }

    // ── Bảng ─────────────────────────────────────────────────────────────────

    private void DrawTable()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField("Màu",      EditorStyles.miniBoldLabel, GUILayout.Width(38f));
            EditorGUILayout.LabelField("Tên ô",    EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            EditorGUILayout.LabelField("Mô tả",    EditorStyles.miniBoldLabel, GUILayout.MinWidth(240f));
            EditorGUILayout.LabelField("Sprite",   EditorStyles.miniBoldLabel, GUILayout.Width(200f));
            EditorGUILayout.LabelField("Trạng thái", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (ConstructionArtKit.Slot slot in System.Enum.GetValues(typeof(ConstructionArtKit.Slot)))
        {
            DrawRow(slot);

            // Ô "Prefab công nhân" không phải Sprite nên không nằm trong enum Slot,
            // nhưng nó THAY THẾ ô Worker nên phải đứng ngay dưới cho khỏi lạc.
            if (slot == ConstructionArtKit.Slot.Worker) DrawWorkerPrefabRow();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRow(ConstructionArtKit.Slot slot)
    {
        string field = FieldOf(slot);
        SerializedProperty prop = field != null ? _so.FindProperty(field) : null;
        bool assigned = prop != null && prop.objectReferenceValue != null;

        using (new EditorGUILayout.HorizontalScope())
        {
            // Ô vuông màu nhận dạng — ép alpha = 1 để nhìn đúng màu, không lẫn nền cửa sổ.
            Rect swatch = GUILayoutUtility.GetRect(30f, 16f, GUILayout.Width(30f), GUILayout.Height(16f));
            Color c = ConstructionArtKit.ColorOf(slot);
            EditorGUI.DrawRect(swatch, new Color(c.r, c.g, c.b, 1f));
            GUILayout.Space(8f);

            EditorGUILayout.LabelField(ConstructionArtKit.LabelOf(slot),
                                       EditorStyles.boldLabel, GUILayout.Width(120f));

            string desc = DescOf(slot);
            if (IsNotWiredYet(slot)) desc += "   ⚠ CHƯA NỐI DÂY";
            EditorGUILayout.LabelField(new GUIContent(desc, desc), GUILayout.MinWidth(240f));

            if (prop != null)
                EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(200f));
            else
                EditorGUILayout.LabelField("(không có field)", GUILayout.Width(200f));

            var status = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = assigned ? new Color(0.25f, 0.7f, 0.25f) : new Color(0.8f, 0.45f, 0.15f) }
            };
            EditorGUILayout.LabelField(assigned ? "✔ đã gán" : "✘ còn trống", status, GUILayout.Width(90f));
        }
    }

    private void DrawWorkerPrefabRow()
    {
        SerializedProperty prop = _so.FindProperty("workerPrefab");
        if (prop == null) return;

        bool assigned = prop.objectReferenceValue != null;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(38f);
            EditorGUILayout.LabelField("↳ Prefab công nhân", GUILayout.Width(120f));
            EditorGUILayout.LabelField(
                new GUIContent("Thay CẢ công nhân bằng prefab (Animator, hiệu ứng búa…). Gán thì bỏ qua ô Worker.",
                               "Tuỳ chọn — không tính vào tiến độ 19 ô."),
                GUILayout.MinWidth(240f));
            EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(200f));
            EditorGUILayout.LabelField(assigned ? "✔ đã gán" : "— tuỳ chọn", GUILayout.Width(90f));
        }
    }

    // ── Dưới cùng ────────────────────────────────────────────────────────────

    private void DrawFooter()
    {
        DrawSeparator();

        int done = 0, total = 0;
        var missing = new List<string>();

        foreach (ConstructionArtKit.Slot slot in System.Enum.GetValues(typeof(ConstructionArtKit.Slot)))
        {
            total++;
            if (_kit.GetSprite(slot) != null) done++;
            else missing.Add(ConstructionArtKit.LabelOf(slot));
        }

        Rect bar = GUILayoutUtility.GetRect(18f, 20f, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(bar, total > 0 ? done / (float)total : 0f,
                              $"Đã gán {done}/{total} ô art");

        if (missing.Count > 0)
        {
            EditorGUILayout.LabelField("Còn trống: " + string.Join(" · ", missing),
                                       EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Xong hết — nhớ TẮT 'Hiện nhãn tên ô' trước khi build.",
                                       EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(4f);
    }

    private static void DrawSeparator()
    {
        Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.25f));
        EditorGUILayout.Space(2f);
    }

    // ── Hành động ────────────────────────────────────────────────────────────

    private void CreateKit()
    {
        if (!AssetDatabase.IsValidFolder(KitFolder))
            AssetDatabase.CreateFolder("Assets/_Game/Farm", "ScriptableObjects");

        string path = AssetDatabase.GenerateUniqueAssetPath(KitFolder + "/ConstructionArtKit.asset");

        var kit = CreateInstance<ConstructionArtKit>();
        AssetDatabase.CreateAsset(kit, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _kit = kit;
        Bind();
        EditorGUIUtility.PingObject(kit);
        Debug.Log($"[ArtKit] Đã tạo bộ ô art mới tại {path}. Bấm 'Gắn kit vào scene' để dùng được.");
    }

    /// <summary>
    /// Gán kit vào ConstructionManager của scene đang mở, tạo mới nếu chưa có.
    /// ⚠ Đây là bước DỄ QUÊN NHẤT: manager tự mọc lúc chạy (RuntimeInitializeOnLoadMethod)
    /// là một object rỗng, `artKit` của nó luôn null → art gán bao nhiêu cũng không hiện.
    /// </summary>
    private void AttachKitToScene()
    {
        var mgr = Object.FindFirstObjectByType<ConstructionManager>(FindObjectsInactive.Include);

        if (mgr == null)
        {
            var go = new GameObject("ConstructionManager");
            Undo.RegisterCreatedObjectUndo(go, "Tạo ConstructionManager");
            mgr = go.AddComponent<ConstructionManager>();
        }

        var so = new SerializedObject(mgr);
        SerializedProperty p = so.FindProperty("artKit");
        if (p == null)
        {
            Debug.LogError("[ArtKit] Không tìm thấy field 'artKit' trên ConstructionManager.");
            return;
        }

        p.objectReferenceValue = _kit;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        Selection.activeGameObject = mgr.gameObject;

        Debug.Log($"[ArtKit] Đã gán '{_kit.name}' vào ConstructionManager " +
                  $"trong scene '{mgr.gameObject.scene.name}'. NHỚ LƯU SCENE (Ctrl+S).");
    }
}
