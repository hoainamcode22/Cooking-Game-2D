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
/// WP-C3 — Sửa chữ "MIỄN PHÍ" bị tràn trong nút kim cương (Btn_SpeedUp) của các mini-panel tiến trình.
/// Quét mọi TMP_Text tên <c>Txt_GemCost</c> trong các scene đang mở:
///  - DRY RUN: chỉ in đường dẫn, rect và font hiện tại + giá trị sẽ đổi (không ghi gì).
///  - APPLY: rect 84x56 tại x=+6, NoWrap, auto-size 12–22, Ellipsis, căn giữa; icon anh em (Image, width ≤ 40) dời sang x=-26.
///    Có Undo (Ctrl+Z), đánh dấu scene dirty, KHÔNG tự lưu scene → bạn tự Ctrl+S.
/// Cùng thông số với <see cref="BuildingProcessUIBuilderTool"/> để panel mới/cũ đồng nhất.
/// </summary>
public static class GemCostTextFixTool
{
    private const string MenuRoot = "Tools/Farm/UI/";
    private const string TargetName = "Txt_GemCost";

    // Thông số đích (đồng bộ với BuildingProcessUIBuilderTool)
    private static readonly Vector2 TextSize = new Vector2(84f, 56f);
    private static readonly Vector2 TextPos = new Vector2(6f, 0f);
    private const float FontMin = 12f;
    private const float FontMax = 22f;
    private const float IconX = -26f;
    private const float IconMaxWidth = 40f;

    [MenuItem(MenuRoot + "Sua chu MIEN PHI nut gem - DRY RUN (chi bao cao)", false, 300)]
    public static void DryRun() => Run(apply: false);

    [MenuItem(MenuRoot + "Sua chu MIEN PHI nut gem - APPLY (ghi vao scene)", false, 301)]
    public static void Apply() => Run(apply: true);

    private static void Run(bool apply)
    {
        List<TMP_Text> targets = FindTargets();
        string mode = apply ? "APPLY" : "DRY RUN";

        if (targets.Count == 0)
        {
            Debug.LogWarning($"[GemCostTextFix][{mode}] Không tìm thấy TMP_Text tên '{TargetName}' trong scene đang mở. Hãy mở scene Farm rồi chạy lại.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[GemCostTextFix][{mode}] Tìm thấy {targets.Count} '{TargetName}':");

        var dirtyScenes = new HashSet<Scene>();
        int changed = 0;

        foreach (TMP_Text txt in targets)
        {
            RectTransform rt = txt.rectTransform;
            Image icon = FindIconSibling(rt);
            string path = GetHierarchyPath(txt.transform);

            sb.AppendLine($"- {path}  (text hiện tại: \"{txt.text}\")");
            sb.AppendLine($"    rect : size {Fmt(rt.sizeDelta)} pos {Fmt(rt.anchoredPosition)}  →  size {Fmt(TextSize)} pos {Fmt(TextPos)}");
            sb.AppendLine($"    font : size {txt.fontSize} auto={txt.enableAutoSizing} [{txt.fontSizeMin}-{txt.fontSizeMax}] wrap={txt.textWrappingMode} overflow={txt.overflowMode} align={txt.alignment}");
            sb.AppendLine($"           →  auto=true [{FontMin}-{FontMax}] wrap=NoWrap overflow=Ellipsis align=Center");
            if (icon != null)
                sb.AppendLine($"    icon : '{icon.name}' x={icon.rectTransform.anchoredPosition.x}  →  x={IconX}");
            else
                sb.AppendLine($"    icon : không có Image anh em width ≤ {IconMaxWidth} → bỏ qua");

            if (!apply) continue;

            ApplyTo(txt, rt, icon);
            dirtyScenes.Add(txt.gameObject.scene);
            changed++;
        }

        if (apply)
        {
            foreach (Scene scene in dirtyScenes)
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
            }
            sb.AppendLine($"=> ĐÃ SỬA {changed} text trong {dirtyScenes.Count} scene. Có Undo (Ctrl+Z). Scene chưa lưu — Nho Ctrl+S.");
        }
        else
        {
            sb.AppendLine("=> DRY RUN: chưa ghi gì. Chạy menu '... - APPLY (ghi vao scene)' để áp dụng.");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>Áp thông số rect + TMP lên 1 text và dời icon anh em (nếu có), có Undo.</summary>
    private static void ApplyTo(TMP_Text txt, RectTransform rt, Image icon)
    {
        Undo.RecordObject(rt, "Fix Txt_GemCost rect");
        rt.sizeDelta = TextSize;
        rt.anchoredPosition = TextPos;
        MarkPrefabOverride(rt);

        Undo.RecordObject(txt, "Fix Txt_GemCost TMP");
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = FontMin;
        txt.fontSizeMax = FontMax;
        txt.alignment = TextAlignmentOptions.Center;
        MarkPrefabOverride(txt);

        if (icon != null)
        {
            RectTransform iconRt = icon.rectTransform;
            Undo.RecordObject(iconRt, "Move Icon_Diamond");
            iconRt.anchoredPosition = new Vector2(IconX, iconRt.anchoredPosition.y);
            MarkPrefabOverride(iconRt);
        }
    }

    /// <summary>Nếu object là instance prefab trong scene → ghi nhận override để không bị prefab revert.</summary>
    private static void MarkPrefabOverride(Component c)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(c))
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    /// <summary>Gom mọi TMP_Text tên Txt_GemCost (kể cả object inactive) trong tất cả scene đang load.</summary>
    private static List<TMP_Text> FindTargets()
    {
        var result = new List<TMP_Text>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t != null && t.name == TargetName) result.Add(t);
                }
            }
        }
        return result;
    }

    /// <summary>Tìm Image anh em (cùng cha) nhỏ (width ≤ 40) — chính là icon kim cương.</summary>
    private static Image FindIconSibling(RectTransform textRt)
    {
        Transform parent = textRt.parent;
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == textRt) continue;
            if (child.GetComponent<TMP_Text>() != null) continue;
            Image img = child.GetComponent<Image>();
            if (img == null) continue;
            if (img.rectTransform.sizeDelta.x <= IconMaxWidth) return img;
        }
        return null;
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
