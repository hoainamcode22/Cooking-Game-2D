// =============================================================================
//  CloseButtonSyncTool
//  ---------------------------------------------------------------------------
//  MUC DICH: Dong bo toan bo nut "Dong X" va nut "kim cuong / toc do" trong cac
//  scene dang mo ve dung 1 bo sprite chuan (UIStandardSprites), tranh tinh trang
//  nut ve rai rac tu nhieu sprite/mau khac nhau qua tung buoi lam viec.
//
//  QUY TRINH:
//    1) "Copy sprite chuan vao Resources"  -> dam bao sprite ton tai trong build
//       that (Resources.Load can duong dan Resources/, AssetDatabase khong chay
//       trong build).
//    2) "DRY RUN"  -> chi quet & bao cao, KHONG ghi gi vao scene.
//    3) "APPLY"    -> ghi that (co Undo), roi Sep tu bam Ctrl+S de luu scene.
//
//  AN TOAN:
//    - Khong tu luu scene (EditorSceneManager.MarkSceneDirty roi nguoi dung Ctrl+S).
//    - Dung Undo.RecordObject / Undo.RegisterCreatedObjectUndo -> Ctrl+Z hoan tac duoc.
//    - Loai tru moi thu nam duoi Tutorial_Canvas co duong dan chua "Hand" hoac
//      "Mask" (ban tay huong dan / mask tutorial khong phai nut that).
// =============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class CloseButtonSyncTool
{
    private const string RESOURCES_ROOT = "Assets/Resources/UI/Standard";

    private static readonly Regex ReCloseByName = new Regex(
        @"(?i)^(btn_?close|btnclose|close_?button|btn_?dong|nut_?dong|btn_?x|btnx)$");
    private static readonly Regex ReCloseByField = new Regex(
        @"(?i)(btnclose|closebutton|_btnclose|nutdong)");

    private static readonly Regex ReGemByName = new Regex(
        @"(?i)^(btn_?pengem|btn_?speedup|btn_?gem|btn_?rutnang.*|.*gem.*btn.*)$");
    private static readonly Regex ReIconGemChild = new Regex(
        @"(?i)icon|diamond|gem|kimcuong");

    // ═════════════════════════════ MENU ITEMS ═════════════════════════════

    [MenuItem("Tools/Farm/UI/Dong bo nut dong - 1. Copy sprite chuan vao Resources", false, 700)]
    public static void CopySpritesToResources() => ChayCopy();

    [MenuItem("Tools/Farm/UI/Dong bo nut dong - 2. DRY RUN (chi bao cao)", false, 701)]
    public static void CloseDryRun() => ChayClose(false);

    [MenuItem("Tools/Farm/UI/Dong bo nut dong - 3. APPLY (ghi vao scene)", false, 702)]
    public static void CloseApply() => ChayClose(true);

    [MenuItem("Tools/Farm/UI/Dong bo nut kim cuong - DRY RUN (chi bao cao)", false, 710)]
    public static void GemDryRun() => ChayGem(false);

    [MenuItem("Tools/Farm/UI/Dong bo nut kim cuong - APPLY (ghi vao scene)", false, 711)]
    public static void GemApply() => ChayGem(true);

    // ═════════════════════════════ 1. COPY SPRITE ═════════════════════════

    private static void ChayCopy()
    {
        EnsureFolder(RESOURCES_ROOT);

        var sb = new StringBuilder();
        sb.AppendLine("================ COPY SPRITE CHUAN VAO RESOURCES ================");
        int daCopy = 0, boQua = 0, loi = 0;

        foreach (var srcPath in UIStandardSprites.AllPaths)
        {
            if (string.IsNullOrEmpty(srcPath)) continue;
            string fileName = System.IO.Path.GetFileName(srcPath);
            string dstPath = RESOURCES_ROOT + "/" + fileName;

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath) != null)
            {
                boQua++;
                sb.AppendLine($"  [BO QUA] da co: {dstPath}");
                continue;
            }

            var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (srcTex == null)
            {
                loi++;
                sb.AppendLine($"  [LOI] khong tim thay nguon: {srcPath}");
                continue;
            }

            bool ok = AssetDatabase.CopyAsset(srcPath, dstPath);
            if (!ok)
            {
                loi++;
                sb.AppendLine($"  [LOI] copy that bai: {srcPath} -> {dstPath}");
                continue;
            }

            // Dong bo import setting: Sprite, Single, giu spriteBorder tu nguon
            var srcImporter = AssetImporter.GetAtPath(srcPath) as TextureImporter;
            var dstImporter = AssetImporter.GetAtPath(dstPath) as TextureImporter;
            if (dstImporter != null)
            {
                dstImporter.textureType = TextureImporterType.Sprite;
                dstImporter.spriteImportMode = SpriteImportMode.Single;
                if (srcImporter != null)
                {
                    dstImporter.spriteBorder = srcImporter.spriteBorder;
                }
                EditorUtility.SetDirty(dstImporter);
                dstImporter.SaveAndReimport();
            }

            daCopy++;
            sb.AppendLine($"  [COPY OK] {srcPath} -> {dstPath}");
        }

        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Da copy: {daCopy}   Bo qua (da co): {boQua}   Loi: {loi}");
        sb.AppendLine("===================================================================");
        Debug.Log(sb.ToString());
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string fullPath)
    {
        // fullPath dang "Assets/Resources/UI/Standard" -> tao tung cap con thieu
        string[] parts = fullPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ═════════════════════════════ 2+3. NUT DONG ═════════════════════════

    private static void ChayClose(bool ghiThat)
    {
        if (ghiThat && UIStandardSprites.Close == null)
        {
            EditorUtility.DisplayDialog("Dong bo nut dong",
                "Khong tai duoc UIStandardSprites.Close (sprite null).\n" +
                "Hay chay menu '1. Copy sprite chuan vao Resources' truoc, roi thu lai.",
                "Da hieu");
            return;
        }

        var hits = ScanByName(ReCloseByName, ReCloseByField);

        var sb = new StringBuilder();
        sb.AppendLine("================ DONG BO NUT DONG (X) ================");
        sb.AppendLine($"Che do: {(ghiThat ? "APPLY (ghi vao scene)" : "DRY RUN (khong ghi gi)")}");
        sb.AppendLine($"Tim thay: {hits.Count} nut ung vien");
        sb.AppendLine("-------------------------------------------------------");

        int daDoi = 0;
        var scenesDaDoi = new HashSet<Scene>();

        foreach (var go in hits)
        {
            var img = go.GetComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            string duongDan = GetHierarchyPath(go.transform);
            string tenSprite = img.sprite != null ? img.sprite.name : "(none)";
            bool coChuX = ChildHasXGlyph(go.transform);

            sb.AppendLine($"  {duongDan}");
            sb.AppendLine($"      sprite hien tai  : {tenSprite}");
            sb.AppendLine($"      rect size        : {rt.sizeDelta.x:0.#} x {rt.sizeDelta.y:0.#}");
            sb.AppendLine($"      co chu 'X' con    : {(coChuX ? "co" : "KHONG")}");

            if (!ghiThat) continue;

            Undo.RecordObject(img, "Dong bo nut dong");
            Undo.RecordObject(rt, "Dong bo nut dong");

            img.sprite = UIStandardSprites.Close;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.preserveAspect = false;

            if (rt.sizeDelta.x >= 48f && rt.sizeDelta.x <= 120f &&
                rt.sizeDelta.y >= 48f && rt.sizeDelta.y <= 120f)
            {
                rt.sizeDelta = UIStandardSprites.CloseSize;
            }

            EnsureXGlyph(go.transform);

            EditorUtility.SetDirty(img);
            EditorUtility.SetDirty(rt);
            scenesDaDoi.Add(go.scene);
            daDoi++;
        }

        sb.AppendLine("-------------------------------------------------------");
        if (ghiThat)
        {
            foreach (var scene in scenesDaDoi) EditorSceneManager.MarkSceneDirty(scene);
            sb.AppendLine($"DA DOI {daDoi} nut. Nho Ctrl+S de luu scene.");
        }
        else
        {
            sb.AppendLine("KET LUAN: chay lai bang menu APPLY de ghi that.");
        }
        sb.AppendLine("=========================================================");
        Debug.Log(sb.ToString());
    }

    private static bool ChildHasXGlyph(Transform root)
    {
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string s = t.text != null ? t.text.Trim() : "";
            if (s == "X" || s == "x" || s == "×") return true;
        }
        return false;
    }

    private static void EnsureXGlyph(Transform root)
    {
        if (ChildHasXGlyph(root)) return;

        // Neu da co Image con dat ten kieu "x"/"icon" thi coi nhu da co glyph, khong tao them
        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var im in images)
        {
            if (im.transform == root) continue;
            string n = im.gameObject.name.ToLowerInvariant();
            if (n.Contains("x") || n.Contains("icon")) return;
        }

        var go = new GameObject("Txt_X", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Tao chu X nut dong");
        go.transform.SetParent(root, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "X";
        tmp.fontSize = UIStandardSprites.CloseGlyphSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    // ═════════════════════════════ NUT KIM CUONG ═════════════════════════

    private static void ChayGem(bool ghiThat)
    {
        if (ghiThat && UIStandardSprites.BtnGem == null)
        {
            EditorUtility.DisplayDialog("Dong bo nut kim cuong",
                "Khong tai duoc UIStandardSprites.BtnGem (sprite null).\n" +
                "Hay chay menu '1. Copy sprite chuan vao Resources' truoc, roi thu lai.",
                "Da hieu");
            return;
        }

        var hits = ScanByName(ReGemByName, null);

        var sb = new StringBuilder();
        sb.AppendLine("================ DONG BO NUT KIM CUONG ================");
        sb.AppendLine($"Che do: {(ghiThat ? "APPLY (ghi vao scene)" : "DRY RUN (khong ghi gi)")}");
        sb.AppendLine($"Tim thay: {hits.Count} nut ung vien");
        sb.AppendLine("---------------------------------------------------------");

        int daDoi = 0;
        var scenesDaDoi = new HashSet<Scene>();

        foreach (var go in hits)
        {
            var img = go.GetComponent<Image>();
            string duongDan = GetHierarchyPath(go.transform);
            string tenSprite = img.sprite != null ? img.sprite.name : "(none)";

            sb.AppendLine($"  {duongDan}");
            sb.AppendLine($"      sprite nen hien tai : {tenSprite}");

            if (!ghiThat) continue;

            Undo.RecordObject(img, "Dong bo nut kim cuong");
            img.sprite = UIStandardSprites.BtnGem;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            EditorUtility.SetDirty(img);

            var iconImages = go.GetComponentsInChildren<Image>(true);
            foreach (var im in iconImages)
            {
                if (im.transform == go.transform) continue;
                if (ReIconGemChild.IsMatch(im.gameObject.name))
                {
                    Undo.RecordObject(im, "Dong bo icon kim cuong");
                    im.sprite = UIStandardSprites.IconGem;
                    im.preserveAspect = true;
                    EditorUtility.SetDirty(im);
                    break; // chi doi con dau tien khop
                }
            }

            scenesDaDoi.Add(go.scene);
            daDoi++;
        }

        sb.AppendLine("---------------------------------------------------------");
        if (ghiThat)
        {
            foreach (var scene in scenesDaDoi) EditorSceneManager.MarkSceneDirty(scene);
            sb.AppendLine($"DA DOI {daDoi} nut. Nho Ctrl+S de luu scene.");
        }
        else
        {
            sb.AppendLine("KET LUAN: chay lai bang menu APPLY de ghi that.");
        }
        sb.AppendLine("===========================================================");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════ QUET DUNG CHUNG ═════════════════════════

    /// <summary>
    /// Quet toan bo scene dang mo, tim GameObject co Image ma:
    ///   - ten khop reByName, HOAC
    ///   - co MonoBehaviour nao do voi field serialize (ObjectReference) ten khop
    ///     reByField, tro toi GameObject/Component nay.
    /// Loai tru moi thu duoi Tutorial_Canvas co duong dan chua "Hand" hoac "Mask".
    /// </summary>
    private static List<GameObject> ScanByName(Regex reByName, Regex reByField)
    {
        var ket = new List<GameObject>();
        var daThay = new HashSet<GameObject>();

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var allT = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (IsExcluded(t)) continue;

                    var go = t.gameObject;
                    if (daThay.Contains(go)) continue;

                    bool khopTen = reByName != null && reByName.IsMatch(go.name);
                    bool khopField = false;

                    if (!khopTen && reByField != null)
                    {
                        khopField = CoFieldKhop(go, reByField);
                    }

                    if (!khopTen && !khopField) continue;
                    if (go.GetComponent<Image>() == null) continue;

                    daThay.Add(go);
                    ket.Add(go);
                }
            }
        }

        return ket;
    }

    private static bool CoFieldKhop(GameObject go, Regex reByField)
    {
        var behaviours = go.GetComponents<MonoBehaviour>();
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            try
            {
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (!reByField.IsMatch(it.name)) continue;
                    var val = it.objectReferenceValue;
                    if (val == null) continue;
                    if (val is GameObject g && g == go) return true;
                    if (val is Component c && c.gameObject == go) return true;
                }
            }
            catch
            {
                // Bo qua field la / khong doc duoc, khong lam sap tool
            }
        }
        return false;
    }

    private static bool IsExcluded(Transform t)
    {
        var cur = t;
        while (cur != null)
        {
            if (cur.name == "Tutorial_Canvas")
            {
                string path = GetHierarchyPath(t);
                if (path.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Mask", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            cur = cur.parent;
        }
        return false;
    }

    private static string GetHierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        var cur = t.parent;
        while (cur != null)
        {
            sb.Insert(0, cur.name + "/");
            cur = cur.parent;
        }
        return sb.ToString();
    }
}
#endif
