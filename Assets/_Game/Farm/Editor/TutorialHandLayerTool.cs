#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// LỚP TAY TUTORIAL (sortingOrder 440) — đưa bàn tay / VFX / proxy tutorial LÊN TRÊN Canvas_Popup.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// VẤN ĐỀ ĐO ĐƯỢC 05/09: <c>Tutorial_Hands</c>, <c>TutorialV2_Vfx</c> và các proxy runtime của
/// TutorialRuntimeTargetResolver đều nằm thẳng dưới <c>Tutorial_Canvas</c> (sortingOrder 250) ⇒ bị
/// vẽ DƯỚI mọi con của <c>Canvas_Popup</c> (300): khay hạt Popup_seed/Popup_hoa, Popupprocess
/// (mini-panel ô lúa có nút gem), Sickle_Bottom_Tray. Tay chỉ vào hạt/nút gem thì… khuất sau khay.
///
/// CÁCH SỬA: tạo canvas con <c>Canvas_TutorialHand</c> dưới Tutorial_Canvas với
/// <c>overrideSorting = true, sortingOrder = 440</c> (trên Canvas_Popup 300 và Canvas_TouristBoatPopup 400),
/// KHÔNG có GraphicRaycaster (tay/VFX không bao giờ được nuốt tap), rồi dời 2 object trên vào,
/// giữ đúng thứ tự anh em. Proxy sinh runtime ⇒ trỏ field serialize <c>_tutorialCanvas</c> của
/// TutorialRuntimeTargetResolver sang canvas mới để proxy sinh ra ở đó.
///
/// AN TOÀN:
///   • Code định vị tay dùng toạ độ màn hình/thế giới (TutorialActionHandGuide.PlaceHandFingertipAt,
///     TutorialDragHintAnimator :161-179) ⇒ đổi cha giữa 2 canvas Overlay KHÔNG lệch tay.
///   • KHÔNG đụng <c>Dim_Background</c> (UnmaskRaycastFilter) — phải ở lại 250; đưa lên trên 300 là nó
///     nuốt raycast của khay hạt. KHÔNG đụng <c>TutorialV2_Dialogue</c>, <c>Tutorial_GuideBoard</c>,
///     <c>NPC_Dialog_Popup</c>, <c>Cloud_Panel</c>.
///   • DRY RUN chỉ in danh sách sẽ dời. APPLY có Undo (Ctrl+Z), KHÔNG tự lưu — Sếp bấm Ctrl+S.
///   • Chạy lại nhiều lần ra cùng kết quả (canvas con dùng lại, object đã dời bỏ qua).
///
/// ⚠ SetupTutorialL1L2Tool (:589-597) khi chạy lại sẽ nối <c>_tutorialCanvas</c> về Tutorial_Canvas —
///   sau đó chỉ cần bấm lại APPLY của tool này.
/// </summary>
public static class TutorialHandLayerTool
{
    private const string MenuRoot = "Tools/Farm/Tutorial/";
    private const string MENU_DRY   = MenuRoot + "Lop tay tutorial (440) - DRY RUN";
    private const string MENU_APPLY = MenuRoot + "Lop tay tutorial (440) - APPLY";

    private const string TutorialCanvasName = "Tutorial_Canvas";
    private const string HandCanvasName     = "Canvas_TutorialHand";
    private const int    HandSortingOrder   = 440;

    /// <summary>Con TRỰC TIẾP của Tutorial_Canvas sẽ dời vào lớp tay (nếu có). Thứ tự anh em giữ theo scene.</summary>
    private static readonly string[] MoveNames = { "Tutorial_Hands", "TutorialV2_Vfx", "FX_Tutorial_Layer" };

    /// <summary>Tuyệt đối KHÔNG dời — để tool tự bảo vệ nếu ai đó sửa MoveNames sai.</summary>
    private static readonly string[] NeverMove = { "Dim_Background", "TutorialV2_Dialogue", "Tutorial_GuideBoard", "NPC_Dialog_Popup", "Cloud_Panel" };

    private const string ResolverCanvasField = "_tutorialCanvas";

    [MenuItem(MENU_DRY, false, 320)]
    public static void DryRun() => Run(apply: false);

    [MenuItem(MENU_APPLY, false, 321)]
    public static void Apply() => Run(apply: true);

    // ═══════════════════════════════════════════════════════════════════════

    private static void Run(bool apply)
    {
        string mode = apply ? "APPLY" : "DRY RUN";
        var sb = new StringBuilder();
        sb.AppendLine($"[TutorialHandLayer][{mode}]");

        Canvas tutCanvas = FindCanvasByName(TutorialCanvasName);
        if (tutCanvas == null)
        {
            sb.AppendLine($"  ✖ Không tìm thấy Canvas '{TutorialCanvasName}' trong scene đang mở (tìm cả inactive). Mở SCN_Farm.unity rồi chạy lại.");
            Debug.LogWarning(sb.ToString());
            return;
        }

        Transform tutT = tutCanvas.transform;
        sb.AppendLine($"  Tutorial_Canvas: {GetHierarchyPath(tutT)} (sortingOrder={tutCanvas.sortingOrder}, renderMode={tutCanvas.renderMode})");

        int changes = 0;

        // ── 1. Canvas con Canvas_TutorialHand ───────────────────────────────
        Transform handT = tutT.Find(HandCanvasName);
        GameObject handGo = handT != null ? handT.gameObject : null;

        if (handGo == null)
        {
            sb.AppendLine($"  + Tạo '{HandCanvasName}' (RectTransform stretch full, Canvas overrideSorting=true order={HandSortingOrder}, KHÔNG GraphicRaycaster)");
            if (apply)
            {
                handGo = new GameObject(HandCanvasName, typeof(RectTransform), typeof(Canvas));
                handGo.layer = tutT.gameObject.layer;
                handGo.transform.SetParent(tutT, false);
                Undo.RegisterCreatedObjectUndo(handGo, "Tạo Canvas_TutorialHand");
                changes++;
            }
        }
        else
        {
            sb.AppendLine($"  · Dùng lại '{HandCanvasName}' đã có");
        }

        if (handGo != null)
        {
            changes += ConfigureHandCanvas(handGo, apply, sb);
        }

        // ── 2. Dời các con trực tiếp ────────────────────────────────────────
        // Duyệt theo thứ tự anh em hiện tại của Tutorial_Canvas để giữ nguyên thứ tự khi dời.
        var toMove = new List<Transform>();
        for (int i = 0; i < tutT.childCount; i++)
        {
            Transform c = tutT.GetChild(i);
            if (System.Array.IndexOf(NeverMove, c.name) >= 0) continue;
            if (System.Array.IndexOf(MoveNames, c.name) < 0) continue;
            toMove.Add(c);
        }

        if (toMove.Count == 0)
        {
            sb.AppendLine("  · Không còn con trực tiếp nào cần dời (đã dời từ trước hoặc scene không có).");
        }

        foreach (Transform c in toMove)
        {
            sb.AppendLine($"  → Dời '{c.name}' (sibling {c.GetSiblingIndex()}, active={c.gameObject.activeSelf}) : {TutorialCanvasName} ⇒ {HandCanvasName} (worldPositionStays=false, giữ thứ tự)");
            if (apply && handGo != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(c.gameObject, "Dời vào lớp tay tutorial");
                Undo.SetTransformParent(c, handGo.transform, false, "Dời vào lớp tay tutorial");   // worldPositionStays=false
                c.SetAsLastSibling();   // dời theo đúng thứ tự duyệt ⇒ thứ tự anh em cũ được giữ
                changes++;
            }
        }

        // Báo những object đã nằm trong lớp tay từ lần chạy trước (để Sếp thấy tool idempotent).
        if (handGo != null)
        {
            for (int i = 0; i < handGo.transform.childCount; i++)
            {
                Transform c = handGo.transform.GetChild(i);
                if (!toMove.Contains(c))
                    sb.AppendLine($"  · Đã ở trong '{HandCanvasName}': {c.name}");
            }
        }

        // ── 3. raycastTarget = false cho mọi Graphic dưới object đã/sẽ dời ──
        var graphicRoots = new List<Transform>(toMove);
        if (handGo != null)
        {
            for (int i = 0; i < handGo.transform.childCount; i++)
            {
                Transform c = handGo.transform.GetChild(i);
                if (!graphicRoots.Contains(c)) graphicRoots.Add(c);
            }
        }
        changes += DisableRaycastTargets(graphicRoots, apply, sb);

        // ── 4. Resolver._tutorialCanvas → canvas mới (proxy sinh runtime dưới _tutorialCanvas) ──
        changes += RewireResolver(handGo, apply, sb);

        // ── 5. Kiểm tra lại các object KHÔNG được dời ───────────────────────
        foreach (string n in NeverMove)
        {
            Transform t = tutT.Find(n);
            if (t != null) sb.AppendLine($"  ✔ Giữ nguyên dưới {TutorialCanvasName}: {n}");
        }

        if (apply)
        {
            Scene s = tutT.gameObject.scene;
            if (s.IsValid() && s.isLoaded) EditorSceneManager.MarkSceneDirty(s);
            if (handGo != null) Selection.activeGameObject = handGo;
            sb.AppendLine($"=> ĐÃ SỬA scene: {changes} mục (có Undo Ctrl+Z, scene CHƯA lưu — nhớ Ctrl+S).");
        }
        else
        {
            sb.AppendLine("=> DRY RUN: chưa ghi gì. Chạy menu '... - APPLY' để áp dụng.");
        }

        Debug.Log(sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>RectTransform stretch full + Canvas override 440 + gỡ GraphicRaycaster nếu lỡ có.</summary>
    private static int ConfigureHandCanvas(GameObject handGo, bool apply, StringBuilder sb)
    {
        int changes = 0;

        var rt = handGo.GetComponent<RectTransform>();
        if (rt == null)
        {
            sb.AppendLine($"    ⚠ '{HandCanvasName}' không có RectTransform (không phải UI object) → bỏ qua neo.");
        }
        else
        {
            bool needRt = rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one
                       || rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero
                       || rt.localScale != Vector3.one;
            sb.AppendLine($"    RectTransform: anchors {Fmt(rt.anchorMin)}/{Fmt(rt.anchorMax)} offsets {Fmt(rt.offsetMin)}/{Fmt(rt.offsetMax)} → stretch full (0..1, offset 0){(needRt ? "" : " (đã đúng)")}");
            if (apply && needRt)
            {
                Undo.RecordObject(rt, "Neo Canvas_TutorialHand");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                changes++;
            }
        }

        var cv = handGo.GetComponent<Canvas>();
        if (cv == null)
        {
            sb.AppendLine($"    + Thêm Canvas (overrideSorting=true, sortingOrder={HandSortingOrder})");
            if (apply)
            {
                cv = Undo.AddComponent<Canvas>(handGo);
                changes++;
            }
        }
        if (cv != null)
        {
            bool needCv = !cv.overrideSorting || cv.sortingOrder != HandSortingOrder;
            sb.AppendLine($"    Canvas: overrideSorting={cv.overrideSorting} sortingOrder={cv.sortingOrder} → overrideSorting=true sortingOrder={HandSortingOrder}{(needCv ? "" : " (đã đúng)")}");
            if (apply && needCv)
            {
                Undo.RecordObject(cv, "Canvas_TutorialHand sorting");
                cv.overrideSorting = true;
                cv.sortingOrder = HandSortingOrder;
                changes++;
            }
        }

        var gr = handGo.GetComponent<GraphicRaycaster>();
        if (gr != null)
        {
            sb.AppendLine("    − Gỡ GraphicRaycaster (lớp tay KHÔNG được nhận/nuốt tap)");
            if (apply)
            {
                Undo.DestroyObjectImmediate(gr);
                changes++;
            }
        }
        else
        {
            sb.AppendLine("    ✔ Không có GraphicRaycaster (đúng)");
        }

        return changes;
    }

    /// <summary>raycastTarget=false cho mọi Graphic (Image/RawImage/TMP…) dưới các root, kể cả inactive.</summary>
    private static int DisableRaycastTargets(List<Transform> roots, bool apply, StringBuilder sb)
    {
        int changes = 0;
        int total = 0, on = 0;
        foreach (Transform root in roots)
        {
            if (root == null) continue;
            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                total++;
                if (!g.raycastTarget) continue;
                on++;
                sb.AppendLine($"    raycastTarget: {GetHierarchyPath(g.transform)} ({g.GetType().Name}) true → false");
                if (apply)
                {
                    Undo.RecordObject(g, "Tắt raycastTarget lớp tay");
                    g.raycastTarget = false;
                    MarkPrefabOverride(g);
                    changes++;
                }
            }
        }
        sb.AppendLine($"  Graphic dưới lớp tay: {total} cái, {on} cái đang raycastTarget=true{(on == 0 ? " (không cần sửa)" : "")}");
        return changes;
    }

    /// <summary>
    /// Proxy world→canvas (TutorialProxy_*) do TutorialRuntimeTargetResolver.CreateWorldProxy sinh RUNTIME
    /// dưới <c>_tutorialCanvas.transform</c> (:534-535) — không có sẵn trong scene để dời. Trỏ field
    /// serialize sang canvas mới để proxy mọc ra ở lớp 440.
    /// </summary>
    private static int RewireResolver(GameObject handGo, bool apply, StringBuilder sb)
    {
        var resolvers = Object.FindObjectsByType<TutorialRuntimeTargetResolver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (resolvers == null || resolvers.Length == 0)
        {
            sb.AppendLine("  · Không thấy TutorialRuntimeTargetResolver trong scene → bỏ qua bước nối _tutorialCanvas.");
            return 0;
        }

        Canvas target = handGo != null ? handGo.GetComponent<Canvas>() : null;
        int changes = 0;

        foreach (var r in resolvers)
        {
            var so = new SerializedObject(r);
            SerializedProperty p = so.FindProperty(ResolverCanvasField);
            if (p == null)
            {
                sb.AppendLine($"  ⚠ {r.name}: không có field '{ResolverCanvasField}' → bỏ qua (tên field đổi? xem TutorialRuntimeTargetResolver.cs:33)");
                continue;
            }

            string cur = p.objectReferenceValue != null ? (p.objectReferenceValue as Canvas)?.name ?? p.objectReferenceValue.name : "null";
            string next = target != null ? target.name : $"{HandCanvasName} (sẽ tạo khi APPLY)";
            bool same = target != null && p.objectReferenceValue == target;
            sb.AppendLine($"  Resolver '{GetHierarchyPath(r.transform)}'.{ResolverCanvasField}: {cur} → {next}{(same ? " (đã đúng)" : "")}");

            if (apply && target != null && !same)
            {
                p.objectReferenceValue = target;
                so.ApplyModifiedProperties();   // tự ghi Undo
                changes++;
            }
        }
        return changes;
    }

    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Tìm Canvas theo tên kể cả inactive (duyệt từ scene roots — như SeedPanelFixTool.FindPanelRoots).</summary>
    private static Canvas FindCanvasByName(string canvasName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Canvas c in sceneRoot.GetComponentsInChildren<Canvas>(true))
                {
                    if (c.name == canvasName) return c;
                }
            }
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
