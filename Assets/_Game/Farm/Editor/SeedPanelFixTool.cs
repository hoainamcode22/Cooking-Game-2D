#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// WP-C3 — Panel hạt giống (<c>Popup_seed</c>) và hoa (<c>Popup_hoa</c>) không cắt chữ nữa + hiện tên hạt.
/// Phần SCENE (có Undo, không tự lưu → Ctrl+S):
///  - Root: sizeDelta.y 190 → 230 (icon vẫn 90).
///  - HorizontalLayoutGroup con: padding top -100 → 0, bottom 0 → 8, childAlignment = MiddleLeft. Giữ RectMask2D của Viewport.
///  - SeedPopupController: field serialize <c>itemPreferredHeight</c> → 170 (qua SerializedObject).
/// Phần PREFAB <c>Assets/Assetsgame/hatgiong/Iteam_1.prefab</c> (LoadPrefabContents → SaveAsPrefabAsset, KHÔNG có Undo):
///  - Root 120x170; Icon_item neo top (0.5,1) y=-8, 90x90; txt_name bật lên, neo bottom (0.5,0) y=34, 112x26, font 18 auto 12–18, NoWrap, Ellipsis, giữa;
///    txt_soluong neo bottom (0.5,0) y=4, 112x28, font 24 auto 14–24.
/// DRY RUN liệt kê mọi thay đổi dự kiến (scene + prefab) mà không ghi gì.
/// </summary>
public static class SeedPanelFixTool
{
    private const string MenuRoot = "Tools/Farm/UI/";
    private static readonly string[] RootNames = { "Popup_seed", "Popup_hoa" };
    private const string PrefabPath = "Assets/Assetsgame/hatgiong/Iteam_1.prefab";

    // Scene
    private const float RootHeightNew = 240f;
    private const int PaddingTopNew = 0;
    private const int PaddingBottomNew = 20;
    private const float ItemPreferredHeightNew = 180f;
    private const string ItemHeightFieldName = "itemPreferredHeight";

    // Prefab
    private static readonly Vector2 TileSize = new Vector2(120f, 180f);
    private static readonly Vector2 IconSize = new Vector2(90f, 90f);
    private const float IconY = -4f;
    private static readonly Vector2 NameSize = new Vector2(112f, 26f);
    private const float NameY = 44f;
    private const float NameFont = 18f, NameFontMin = 12f, NameFontMax = 18f;
    private static readonly Vector2 QtySize = new Vector2(112f, 28f);
    private const float QtyY = 16f;
    private const float QtyFont = 24f, QtyFontMin = 14f, QtyFontMax = 24f;

    [MenuItem(MenuRoot + "Sua panel hat giong + hoa - DRY RUN (chi bao cao)", false, 310)]
    public static void DryRun() => Run(apply: false);

    [MenuItem(MenuRoot + "Sua panel hat giong + hoa - APPLY (ghi vao scene)", false, 311)]
    public static void Apply() => Run(apply: true);

    private static void Run(bool apply)
    {
        string mode = apply ? "APPLY" : "DRY RUN";
        var sb = new StringBuilder();
        sb.AppendLine($"[SeedPanelFix][{mode}]");

        int sceneChanges = FixSceneRoots(apply, sb);
        int prefabChanges = FixPrefab(apply, sb);

        if (apply)
            sb.AppendLine($"=> ĐÃ SỬA scene: {sceneChanges} mục (có Undo Ctrl+Z, scene chưa lưu — Nho Ctrl+S); prefab: {prefabChanges} mục (đã ghi file .prefab, không Undo).");
        else
            sb.AppendLine("=> DRY RUN: chưa ghi gì. Chạy menu '... - APPLY (ghi vao scene)' để áp dụng.");

        Debug.Log(sb.ToString());
    }

    // ── SCENE ────────────────────────────────────────────────────────────────

    private static int FixSceneRoots(bool apply, StringBuilder sb)
    {
        List<RectTransform> roots = FindPanelRoots();
        if (roots.Count == 0)
        {
            sb.AppendLine($"  [scene] Không tìm thấy '{string.Join("' / '", RootNames)}' trong scene đang mở (tìm cả object inactive). Hãy mở scene Farm.");
            return 0;
        }

        int changes = 0;
        var dirtyScenes = new HashSet<Scene>();

        foreach (RectTransform root in roots)
        {
            sb.AppendLine($"  [scene] {GetHierarchyPath(root)} (active={root.gameObject.activeInHierarchy})");

            // 1. Root height
            sb.AppendLine($"    root sizeDelta: {Fmt(root.sizeDelta)} → ({root.sizeDelta.x:0.#},{RootHeightNew})");
            if (apply)
            {
                Undo.RecordObject(root, "SeedPanel root height");
                root.sizeDelta = new Vector2(root.sizeDelta.x, RootHeightNew);
                MarkPrefabOverride(root);
                changes++;
            }

            // 2. HorizontalLayoutGroup (Content)
            HorizontalLayoutGroup hlg = root.GetComponentInChildren<HorizontalLayoutGroup>(true);
            if (hlg == null)
            {
                sb.AppendLine("    HorizontalLayoutGroup: KHÔNG tìm thấy → bỏ qua");
            }
            else
            {
                RectOffset p = hlg.padding;
                sb.AppendLine($"    HLG '{hlg.name}': padding T{p.top} B{p.bottom} L{p.left} R{p.right} align={hlg.childAlignment} → T{PaddingTopNew} B{PaddingBottomNew} (giữ L/R) align=MiddleLeft");
                if (apply)
                {
                    Undo.RecordObject(hlg, "SeedPanel layout padding");
                    hlg.padding = new RectOffset(p.left, p.right, PaddingTopNew, PaddingBottomNew);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    MarkPrefabOverride(hlg);
                    LayoutRebuilder.MarkLayoutForRebuild(hlg.GetComponent<RectTransform>());
                    changes++;
                }
            }

            // 3. SeedPopupController.itemPreferredHeight → 170 (qua SerializedObject để có Undo + ghi override đúng)
            SeedPopupController ctrl = root.GetComponentInChildren<SeedPopupController>(true);
            if (ctrl == null)
            {
                sb.AppendLine("    SeedPopupController: KHÔNG tìm thấy → bỏ qua");
            }
            else
            {
                var so = new SerializedObject(ctrl);
                SerializedProperty prop = so.FindProperty(ItemHeightFieldName);
                if (prop == null)
                {
                    sb.AppendLine($"    SeedPopupController: không có field '{ItemHeightFieldName}' → bỏ qua");
                }
                else
                {
                    sb.AppendLine($"    SeedPopupController '{ctrl.name}'.{ItemHeightFieldName}: {prop.floatValue} → {ItemPreferredHeightNew}");
                    if (apply && !Mathf.Approximately(prop.floatValue, ItemPreferredHeightNew))
                    {
                        prop.floatValue = ItemPreferredHeightNew;
                        so.ApplyModifiedProperties(); // tự ghi Undo
                        changes++;
                    }
                }
            }

            if (apply) dirtyScenes.Add(root.gameObject.scene);
        }

        if (apply)
        {
            foreach (Scene s in dirtyScenes)
                if (s.IsValid() && s.isLoaded) EditorSceneManager.MarkSceneDirty(s);
        }
        return changes;
    }

    /// <summary>Tìm root Popup_seed / Popup_hoa kể cả khi inactive (duyệt từ scene roots).</summary>
    private static List<RectTransform> FindPanelRoots()
    {
        var result = new List<RectTransform>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Array.IndexOf(RootNames, t.name) < 0) continue;
                    var rt = t as RectTransform;
                    if (rt != null) result.Add(rt);
                }
            }
        }
        return result;
    }

    // ── PREFAB ───────────────────────────────────────────────────────────────

    private static int FixPrefab(bool apply, StringBuilder sb)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            sb.AppendLine($"  [prefab] Không tìm thấy '{PrefabPath}' → bỏ qua phần prefab.");
            return 0;
        }

        int changes = 0;
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            sb.AppendLine($"  [prefab] {PrefabPath}");

            // Root tile
            RectTransform rootRt = prefabRoot.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                sb.AppendLine($"    root sizeDelta: {Fmt(rootRt.sizeDelta)} → {Fmt(TileSize)}");
                if (apply) { rootRt.sizeDelta = TileSize; changes++; }
            }
            else sb.AppendLine("    root: không có RectTransform → bỏ qua");

            // Icon_item: neo top-center, y=-8, 90x90
            RectTransform icon = FindChildRect(prefabRoot.transform, "Icon_item");
            if (icon != null)
            {
                sb.AppendLine($"    Icon_item: anchor {Fmt(icon.anchorMin)}/{Fmt(icon.anchorMax)} pivot {Fmt(icon.pivot)} pos {Fmt(icon.anchoredPosition)} size {Fmt(icon.sizeDelta)} → anchor/pivot (0.5,1) pos (0,{IconY}) size {Fmt(IconSize)}");
                if (apply)
                {
                    SetAnchors(icon, new Vector2(0.5f, 1f));
                    icon.anchoredPosition = new Vector2(0f, IconY);
                    icon.sizeDelta = IconSize;
                    changes++;
                }
            }
            else sb.AppendLine("    Icon_item: KHÔNG tìm thấy → bỏ qua");

            // txt_name: bật lên, neo bottom-center, y=34, 112x26, font 18 auto 12–18
            RectTransform nameRt = FindChildRect(prefabRoot.transform, "txt_name");
            if (nameRt != null)
            {
                sb.AppendLine($"    txt_name: active={nameRt.gameObject.activeSelf} anchor {Fmt(nameRt.anchorMin)} pos {Fmt(nameRt.anchoredPosition)} size {Fmt(nameRt.sizeDelta)} → active=true anchor/pivot (0.5,0) pos (0,{NameY}) size {Fmt(NameSize)}");
                sb.AppendLine($"      text: {DescribeText(nameRt)} → font {NameFont} auto [{NameFontMin}-{NameFontMax}] NoWrap Ellipsis Center");
                if (apply)
                {
                    nameRt.gameObject.SetActive(true);
                    SetAnchors(nameRt, new Vector2(0.5f, 0f));
                    nameRt.anchoredPosition = new Vector2(0f, NameY);
                    nameRt.sizeDelta = NameSize;
                    ApplyTextSettings(nameRt, NameFont, NameFontMin, NameFontMax);
                    changes++;
                }
            }
            else sb.AppendLine("    txt_name: KHÔNG tìm thấy → bỏ qua (tên hạt sẽ không hiện)");

            // txt_soluong: neo bottom-center, y=4, 112x28, font 24 auto 14–24
            RectTransform qtyRt = FindChildRect(prefabRoot.transform, "txt_soluong");
            if (qtyRt != null)
            {
                sb.AppendLine($"    txt_soluong: anchor {Fmt(qtyRt.anchorMin)} pos {Fmt(qtyRt.anchoredPosition)} size {Fmt(qtyRt.sizeDelta)} → anchor/pivot (0.5,0) pos (0,{QtyY}) size {Fmt(QtySize)}");
                sb.AppendLine($"      text: {DescribeText(qtyRt)} → font {QtyFont} auto [{QtyFontMin}-{QtyFontMax}] NoWrap Ellipsis Center");
                if (apply)
                {
                    SetAnchors(qtyRt, new Vector2(0.5f, 0f));
                    qtyRt.anchoredPosition = new Vector2(0f, QtyY);
                    qtyRt.sizeDelta = QtySize;
                    ApplyTextSettings(qtyRt, QtyFont, QtyFontMin, QtyFontMax);
                    changes++;
                }
            }
            else sb.AppendLine("    txt_soluong: KHÔNG tìm thấy → bỏ qua");

            if (apply && changes > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath, out bool ok);
                sb.AppendLine(ok ? "    → Đã lưu prefab." : "    → LỖI: SaveAsPrefabAsset thất bại!");
                if (!ok) changes = 0;
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        return changes;
    }

    /// <summary>Đặt anchorMin = anchorMax = pivot (neo điểm).</summary>
    private static void SetAnchors(RectTransform rt, Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
    }

    /// <summary>Áp font/auto-size/NoWrap/Ellipsis/Center cho TMP_Text; fallback UGUI Text (best-fit) nếu prefab còn dùng Text cũ.</summary>
    private static void ApplyTextSettings(RectTransform rt, float fontSize, float fontMin, float fontMax)
    {
        TMP_Text tmp = rt.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = fontMin;
            tmp.fontSizeMax = fontMax;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = TextAlignmentOptions.Center;
            return;
        }
        Text legacy = rt.GetComponent<Text>();
        if (legacy != null)
        {
            legacy.fontSize = (int)fontSize;
            legacy.resizeTextForBestFit = true;
            legacy.resizeTextMinSize = (int)fontMin;
            legacy.resizeTextMaxSize = (int)fontMax;
            legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
            legacy.verticalOverflow = VerticalWrapMode.Truncate;
            legacy.alignment = TextAnchor.MiddleCenter;
        }
    }

    private static string DescribeText(RectTransform rt)
    {
        TMP_Text tmp = rt.GetComponent<TMP_Text>();
        if (tmp != null)
            return $"TMP font {tmp.fontSize} auto={tmp.enableAutoSizing} [{tmp.fontSizeMin}-{tmp.fontSizeMax}] wrap={tmp.textWrappingMode} overflow={tmp.overflowMode} align={tmp.alignment}";
        Text legacy = rt.GetComponent<Text>();
        if (legacy != null)
            return $"UGUI Text font {legacy.fontSize} bestFit={legacy.resizeTextForBestFit}";
        return "không có component text";
    }

    /// <summary>Tìm con (mọi cấp, kể cả inactive) theo tên, trả RectTransform.</summary>
    private static RectTransform FindChildRect(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != root && t.name == childName) return t as RectTransform;
        }
        return null;
    }

    private static void MarkPrefabOverride(Component c)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(c))
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    private static string GetHierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return $"[{t.gameObject.scene.name}] {sb}";
    }

    private static string Fmt(Vector2 v) => $"({v.x:0.#},{v.y:0.#})";
}
#endif
