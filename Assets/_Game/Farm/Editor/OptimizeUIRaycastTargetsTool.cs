#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UNITY EDITOR TOOL: Optimize UI Raycast Targets (Mobile Performance)
/// ══════════════════════════════════════════════════════════════════════════════
/// Triệt tiêu chi phí CPU GraphicRaycaster trên Mobile khi người chơi chạm màn hình:
///
/// 1. Tắt raycastTarget trên toàn bộ TMP_Text / Text (trừ trường hợp nằm trong InputField
///    hoặc có EventTrigger riêng). Giúp các cú chạm rơi thẳng xuống nút nền Image bên dưới.
/// 2. Tắt raycastTarget trên các Image / RawImage trang trí tĩnh KHÔNG có:
///    • Button, Toggle, ScrollRect, EventTrigger
///    • Slider, Scrollbar, Dropdown, InputField
///    • Thành phần xử lý sự kiện cảm ứng (IPointerClickHandler, IDragHandler...)
///    trên cùng GameObject hoặc bất kỳ GameObject cha nào.
/// 3. Bảo toàn các lớp Dimmer / Blocker toàn màn hình chặn click xuyên map (tên chứa Dim, Blocker, Overlay...).
/// 4. Hỗ trợ Undo.RecordObject đầy đủ (Ctrl+Z an toàn).
/// </summary>
public static class OptimizeUIRaycastTargetsTool
{
    [MenuItem("Tools/Farm Game/Performance/Optimize UI Raycast Targets", false, 200)]
    public static void OptimizeActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không có Scene nào đang được mở!", "OK");
            return;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        List<Graphic> allGraphics = new List<Graphic>();
        foreach (var root in rootObjects)
        {
            allGraphics.AddRange(root.GetComponentsInChildren<Graphic>(true));
        }

        int textCount = 0;
        int imgCount = 0;
        int preservedCount = 0;
        StringBuilder logReport = new StringBuilder();
        logReport.AppendLine($"═════════ [OPTIMIZE UI RAYCAST TARGETS - SCENE '{activeScene.name}'] ═════════");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Optimize UI Raycast Targets");
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < allGraphics.Count; i++)
        {
            Graphic g = allGraphics[i];
            if (g == null || !g.raycastTarget) continue;

            if (g is TMP_Text || g is Text)
            {
                if (CanDisableTextRaycast(g))
                {
                    Undo.RecordObject(g, "Disable Text Raycast Target");
                    g.raycastTarget = false;
                    EditorUtility.SetDirty(g);
                    textCount++;
                    logReport.AppendLine($"  • [Text] {GetHierarchyPath(g.transform)}");
                }
                else
                {
                    preservedCount++;
                }
            }
            else if (g is Image || g is RawImage)
            {
                if (CanDisableImageRaycast(g))
                {
                    Undo.RecordObject(g, "Disable Image Raycast Target");
                    g.raycastTarget = false;
                    EditorUtility.SetDirty(g);
                    imgCount++;
                    logReport.AppendLine($"  • [Image] {GetHierarchyPath(g.transform)}");
                }
                else
                {
                    preservedCount++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        int totalOptimized = textCount + imgCount;
        if (totalOptimized > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            logReport.AppendLine($"══════════════════════════════════════════════════════════════════════");
            logReport.AppendLine($"Tổng kết: Đã tắt raycastTarget cho {totalOptimized} thành phần UI:");
            logReport.AppendLine($"  - TMP_Text / Text: {textCount}");
            logReport.AppendLine($"  - Image / RawImage trang trí tĩnh: {imgCount}");
            logReport.AppendLine($"  - Giữ lại {preservedCount} Graphic tương tác / nút bấm / dimmer.");
            Debug.Log(logReport.ToString());

            EditorUtility.DisplayDialog(
                "Tối ưu Raycast Targets thành công!",
                $"Đã tắt raycastTarget cho {totalOptimized} thành phần UI trong Scene '{activeScene.name}':\n\n" +
                $"• Text / TMP_Text: {textCount}\n" +
                $"• Image tĩnh không tương tác: {imgCount}\n" +
                $"• Giữ nguyên {preservedCount} Graphic tương tác (Button, Toggle, Scroll, Dimmer...)\n\n" +
                $"Chi tiết xem tại Console. Nhấn Ctrl+S để lưu Scene.",
                "OK");
        }
        else
        {
            Debug.Log($"[OptimizeUIRaycast] Scene '{activeScene.name}' đã tối ưu sẵn (không có Text hay Image tĩnh nào còn bật raycastTarget).");
            EditorUtility.DisplayDialog(
                "Thông báo",
                $"Toàn bộ UI trong Scene '{activeScene.name}' đã được tối ưu sẵn!\nKhông có raycastTarget dư thừa nào cần tắt.",
                "OK");
        }
    }

    [MenuItem("Tools/Farm Game/Performance/Optimize UI Raycast Targets in All Prefabs", false, 201)]
    public static void OptimizeAllPrefabs()
    {
        if (!EditorUtility.DisplayDialog(
            "Xác nhận tối ưu Prefabs",
            "Tool sẽ quét toàn bộ UI Prefabs trong Assets/_Game và tắt raycastTarget trên các Text / Image trang trí tĩnh không có tương tác.\n\nBạn có muốn tiếp tục?",
            "Tiếp tục", "Huỷ"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" });
        int totalPrefabsModified = 0;
        int totalTexts = 0;
        int totalImages = 0;
        StringBuilder logReport = new StringBuilder();
        logReport.AppendLine("═════════ [OPTIMIZE UI RAYCAST TARGETS - PREFABS] ═════════");

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Optimize UI Raycast Targets", $"Đang xử lý: {path}", (float)i / guids.Length);

                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                Graphic[] graphics = prefab.GetComponentsInChildren<Graphic>(true);
                int pTexts = 0;
                int pImages = 0;

                foreach (var g in graphics)
                {
                    if (g == null || !g.raycastTarget) continue;

                    if (g is TMP_Text || g is Text)
                    {
                        if (CanDisableTextRaycast(g))
                        {
                            g.raycastTarget = false;
                            pTexts++;
                        }
                    }
                    else if (g is Image || g is RawImage)
                    {
                        if (CanDisableImageRaycast(g))
                        {
                            g.raycastTarget = false;
                            pImages++;
                        }
                    }
                }

                if (pTexts + pImages > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    totalPrefabsModified++;
                    totalTexts += pTexts;
                    totalImages += pImages;
                    logReport.AppendLine($"  ✓ [{path}] Tắt {pTexts} Text, {pImages} Image.");
                }

                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        int grandTotal = totalTexts + totalImages;
        logReport.AppendLine($"══════════════════════════════════════════════════════════════════════");
        logReport.AppendLine($"Tổng kết Prefab: Đã tối ưu {grandTotal} Graphic trong {totalPrefabsModified} Prefabs (Text: {totalTexts}, Image: {totalImages}).");
        Debug.Log(logReport.ToString());

        EditorUtility.DisplayDialog(
            "Tối ưu Prefabs hoàn tất",
            $"Đã tối ưu thành công {grandTotal} Graphic trong {totalPrefabsModified} Prefabs!\n" +
            $"• Text / TMP_Text: {totalTexts}\n" +
            $"• Image trang trí tĩnh: {totalImages}\n\n" +
            $"Chi tiết xem tại Unity Console.",
            "OK");
    }

    /// <summary>
    /// Kiểm tra xem TMP_Text/Text có thể tắt raycastTarget an toàn không.
    /// Bỏ qua nếu là InputField hoặc có EventTrigger / handler trên cùng object.
    /// </summary>
    private static bool CanDisableTextRaycast(Graphic g)
    {
        // 1. Ô nhập liệu người chơi gõ phím -> Cần bắt chạm
        if (g.GetComponentInParent<TMP_InputField>() != null || g.GetComponentInParent<InputField>() != null)
            return false;

        // 2. Có thành phần xử lý sự kiện tương tác trực tiếp trên chính nó
        if (g.GetComponent<EventTrigger>() != null || g.GetComponent<IPointerClickHandler>() != null)
            return false;

        // 3. Là targetGraphic duy nhất của một Button không có Image nền
        Selectable sel = g.GetComponentInParent<Selectable>();
        if (sel != null && sel.targetGraphic == g)
        {
            Image bgImg = sel.GetComponent<Image>();
            if (bgImg == null) return false; // Nút dạng Text-only không có nền Image
        }

        return true;
    }

    /// <summary>
    /// Kiểm tra xem Image/RawImage có phải là hình trang trí tĩnh (không có Button/Toggle/ScrollRect/EventTrigger trên nó hoặc cha)
    /// </summary>
    private static bool CanDisableImageRaycast(Graphic g)
    {
        // 1. Kiểm tra các thành phần tương tác trên cùng GameObject hoặc cha
        if (HasInteractiveParentOrSelf(g.gameObject))
            return false;

        // 2. Bảo toàn các tấm Dimmer / Blocker nền mờ chặn click xuyên thấu
        if (IsIntentionalRaycastBlocker(g.gameObject))
            return false;

        // 3. Là targetGraphic của Selectable
        Selectable sel = g.GetComponentInParent<Selectable>();
        if (sel != null && sel.targetGraphic == g)
            return false;

        // Đây là Image trang trí tĩnh không có bất kỳ tương tác nào -> Tắt an toàn!
        return true;
    }

    /// <summary>
    /// Kiểm tra xem có Button, Toggle, ScrollRect, EventTrigger hoặc Selectable trên GameObject hoặc cha không.
    /// </summary>
    private static bool HasInteractiveParentOrSelf(GameObject go)
    {
        Transform curr = go.transform;
        while (curr != null)
        {
            // Kiểm tra các component tương tác phổ biến
            if (curr.GetComponent<Button>() != null) return true;
            if (curr.GetComponent<Toggle>() != null) return true;
            if (curr.GetComponent<ScrollRect>() != null) return true;
            if (curr.GetComponent<EventTrigger>() != null) return true;
            if (curr.GetComponent<Slider>() != null) return true;
            if (curr.GetComponent<Scrollbar>() != null) return true;
            if (curr.GetComponent<Dropdown>() != null) return true;
            if (curr.GetComponent<TMP_Dropdown>() != null) return true;
            if (curr.GetComponent<InputField>() != null) return true;
            if (curr.GetComponent<TMP_InputField>() != null) return true;

            // Kiểm tra bất kỳ component nào implements IEventSystemHandler (IPointerClickHandler, IDragHandler...)
            var handlers = curr.GetComponents<IEventSystemHandler>();
            if (handlers != null && handlers.Length > 0) return true;

            curr = curr.parent;
        }

        return false;
    }

    /// <summary>
    /// Nhận diện các tấm nền làm nhiệm vụ chặn click / Dimmer / Blocker popup.
    /// </summary>
    private static bool IsIntentionalRaycastBlocker(GameObject go)
    {
        string name = go.name.ToLowerInvariant();
        if (name.Contains("dim") ||
            name.Contains("blocker") ||
            name.Contains("overlay") ||
            name.Contains("raycastblock") ||
            name.Contains("touchblock") ||
            name.Contains("shield") ||
            name.Contains("inputblock") ||
            name.Contains("curtain") ||
            name.Contains("fadepanel"))
        {
            return true;
        }

        return false;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
