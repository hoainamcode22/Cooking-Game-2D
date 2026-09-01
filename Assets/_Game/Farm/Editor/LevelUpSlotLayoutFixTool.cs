#if UNITY_EDITOR
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V3.3] MENU: Tools/Farm Game/Level Up Popup/★ HOÀN THIỆN NHÂN VẬT (vị trí + art, 1 nút)
///
/// Một nút làm 4 việc theo chỉ đạo Sếp (idempotent — bấm lại bao nhiêu lần cũng ra đúng chuẩn):
///   1. NỀN DIM XÁM: thêm Image "V3_DimBackground" phủ toàn màn hình, sibling 0 của popupRoot
///      (nằm SAU mọi nội dung popup, che mờ cảnh game) — màu xám đen alpha 0.62.
///   2. CHỮ THƯỞNG TRẮNG-TO-RÕ: mọi TMP trong goldRewardRow/gemRewardRow + hintText +
///      unlockDescText → màu trắng, đậm, cỡ chữ tối thiểu 44 (dòng phụ 30).
///   3. 4 SLOT NHÂN VẬT: phóng 110/150 → 230px, neo thành 2 cặp ôm hai bên banner
///      "LÊN CẤP!" (so le cao-thấp), không đè HUD.
///   4. GẮN ART MASTER + CHẾ ĐỘ THỞ: gọi LevelUpPuppetArtTool.WirePuppetArt().
///
/// Có Undo. KHÔNG auto-save scene — Sếp Ctrl+S sau khi ưng.
/// </summary>
public static class LevelUpSlotLayoutFixTool
{
    private const string MENU = "Tools/Farm Game/Level Up Popup/★ HOÀN THIỆN NHÂN VẬT (vị trí + art, 1 nút)";

    private static readonly Vector2[] SlotAnchors =
    {
        new Vector2(0.195f, 0.705f),   // trái-cao
        new Vector2(0.285f, 0.565f),   // trái-thấp (gần banner hơn)
        new Vector2(0.805f, 0.705f),   // phải-cao
        new Vector2(0.715f, 0.565f),   // phải-thấp
    };
    private const float SlotSize      = 230f;   // Sếp chê 150 nhỏ → 230
    private const float MainFontMin   = 44f;    // "Phần thưởng: +300 +3"
    private const float SubFontMin    = 30f;    // hint / dòng mở khóa
    private static readonly Color DimColor = new Color(0.07f, 0.07f, 0.09f, 0.62f);

    [MenuItem(MENU)]
    public static void FixLayoutAndWireArt()
    {
        var report = new StringBuilder();
        report.AppendLine("═══════ HOÀN THIỆN POPUP LEVEL-UP V3.3 (dim + chữ + BO GÓC + vị trí + art) ═══════");

        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Hoàn thiện popup",
                "Khong tim thay LevelUpPopupUI trong scene.\nMo scene SCN_Farm roi chay lai.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Hoan thien popup Level Up V3.3");

        var so = new SerializedObject(popup);

        // ── 1) NỀN DIM XÁM ───────────────────────────────────────────────────
        var rootProp = so.FindProperty("popupRoot");
        GameObject popupRoot = rootProp != null ? rootProp.objectReferenceValue as GameObject : null;
        if (popupRoot != null)
        {
            Transform dimTr = popupRoot.transform.Find("V3_DimBackground");
            GameObject dim;
            if (dimTr == null)
            {
                dim = new GameObject("V3_DimBackground", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(dim, "Tao dim background");
                dim.transform.SetParent(popupRoot.transform, false);
                report.AppendLine("  + Tạo V3_DimBackground (nền dim xám)");
            }
            else { dim = dimTr.gameObject; report.AppendLine("  = V3_DimBackground đã có → chuẩn hoá lại"); }

            var dimRt = (RectTransform)dim.transform;
            Undo.RecordObject(dimRt, "Dim rect");
            dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
            // Phủ lố 2000px mỗi phía — popupRoot có thể không phủ hết màn hình
            dimRt.offsetMin = new Vector2(-2000f, -2000f);
            dimRt.offsetMax = new Vector2( 2000f,  2000f);
            var dimImg = dim.GetComponent<Image>();
            Undo.RecordObject(dimImg, "Dim mau");
            dimImg.color = DimColor;
            dimImg.raycastTarget = false;              // tap xuyên qua → LevelUpTapToClose vẫn nhận
            dim.transform.SetSiblingIndex(0);          // nằm SAU mọi nội dung popup
            EditorUtility.SetDirty(dim);
        }
        else report.AppendLine("[WARN] popupRoot null → bỏ qua bước dim.");

        // ── 2) CHỮ TRẮNG TO RÕ ───────────────────────────────────────────────
        int textFixed = 0;
        textFixed += WhitenRowTexts(so, "goldRewardRow", MainFontMin, report);
        textFixed += WhitenRowTexts(so, "gemRewardRow",  MainFontMin, report);
        textFixed += WhitenSingleText(so, "hintText",       SubFontMin, report);
        textFixed += WhitenSingleText(so, "unlockDescText", SubFontMin, report);
        report.AppendLine($"  ✓ Chỉnh {textFixed} text → trắng/đậm/to.");

        // ── 2.5) BO GÓC các panel phẳng (dải quà / dải mở khóa / content) ─────
        Sprite bo = TaoSpriteBoGoc(report);
        if (bo != null)
        {
            int boCount = 0;
            boCount += ApDungBoGoc(so, "contentPanel",       bo, report);
            boCount += ApDungBoGoc(so, "unlockStripRoot",    bo, report);
            boCount += ApDungBoGoc(so, "giftItemsContainer", bo, report);
            report.AppendLine($"  ✓ Bo góc {boCount} panel (sprite 9-slice bo tròn 22px).");
        }

        // ── 3) VỊ TRÍ + CỠ 4 SLOT NHÂN VẬT ──────────────────────────────────
        var slotsProp = so.FindProperty("celebrationSlots");
        int fixedCount = 0;
        if (slotsProp != null)
        {
            for (int i = 0; i < slotsProp.arraySize && i < SlotAnchors.Length; i++)
            {
                var slot = slotsProp.GetArrayElementAtIndex(i).objectReferenceValue as CelebrationCharacterSlot;
                if (slot == null) { report.AppendLine($"[WARN] celebrationSlots[{i}] null → bỏ qua."); continue; }
                var rt = slot.transform as RectTransform;
                if (rt == null) continue;

                Undo.RecordObject(rt, "Dat lai vi tri slot");
                rt.anchorMin = rt.anchorMax = SlotAnchors[i];
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(SlotSize, SlotSize);
                rt.localScale = Vector3.one;
                EditorUtility.SetDirty(rt);
                report.AppendLine($"  ✓ {slot.gameObject.name}: anchor=({SlotAnchors[i].x:0.###}, {SlotAnchors[i].y:0.###}), size={SlotSize}px");
                fixedCount++;
            }
        }
        else report.AppendLine("[ERR] Chưa có celebrationSlots — chạy '★ Nâng cấp V2 (1 nút)' trước.");

        Undo.CollapseUndoOperations(group);
        report.AppendLine($"─── Dim ✓ · chữ ✓ · {fixedCount}/4 slot 230px ôm banner ✓. Bước cuối: gắn art master... ───");
        Debug.Log(report.ToString());

        // ── 4) GẮN ART MASTER + THỞ ──────────────────────────────────────────
        LevelUpPuppetArtTool.WirePuppetArt();
    }

    private const string RoundedPath = "Assets/Art/UI/Generated/panel_rounded_64.png";

    /// <summary>Tạo (1 lần) sprite trắng bo góc 22px, 9-slice border 24 — tô màu gì cũng được.</summary>
    private static Sprite TaoSpriteBoGoc(StringBuilder report)
    {
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
        if (spr != null) return spr;

        const int N = 64; const float R = 22f;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float dx = Mathf.Max(Mathf.Max(R - x, x - (N - 1 - R)), 0f);
            float dy = Mathf.Max(Mathf.Max(R - y, y - (N - 1 - R)), 0f);
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float aP = Mathf.Clamp01(R - d + 0.5f);           // mép mềm 1px
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, aP));
        }
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(RoundedPath));
        File.WriteAllBytes(RoundedPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(RoundedPath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(RoundedPath);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteBorder = new Vector4(24, 24, 24, 24);        // 9-slice
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
        var kq = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
        report.AppendLine(kq != null ? $"  + Tạo sprite bo góc {RoundedPath}" : "[WARN] Không tạo được sprite bo góc.");
        return kq;
    }

    /// <summary>Gắn sprite bo góc (Sliced) cho Image của field — CHỈ khi Image đang là
    /// màu bệt (sprite null) hoặc đã là sprite bo góc cũ; sprite art thật thì KHÔNG đụng.</summary>
    private static int ApDungBoGoc(SerializedObject so, string field, Sprite bo, StringBuilder report)
    {
        var prop = so.FindProperty(field);
        Object o = prop != null ? prop.objectReferenceValue : null;
        GameObject go = o as GameObject;
        if (go == null && o is Component c) go = c.gameObject;
        if (go == null) return 0;

        int n = 0;
        var img = go.GetComponent<Image>();
        if (img == null && go.transform.parent != null) img = go.transform.parent.GetComponent<Image>();
        if (img != null && (img.sprite == null || img.sprite.name.StartsWith("panel_rounded") ||
                            img.sprite.name == "Background" || img.sprite.name == "UISprite"))
        {
            Undo.RecordObject(img, "Bo goc panel");
            img.sprite = bo;
            img.type = Image.Type.Sliced;
            EditorUtility.SetDirty(img);
            report.AppendLine($"    · bo góc: {img.gameObject.name} ({field})");
            n++;
        }
        return n;
    }

    /// <summary>Mọi TMP con (kể cả inactive) trong row → trắng, đậm, cỡ ≥ minSize.</summary>
    private static int WhitenRowTexts(SerializedObject so, string rowField, float minSize, StringBuilder report)
    {
        var prop = so.FindProperty(rowField);
        var row  = prop != null ? prop.objectReferenceValue as GameObject : null;
        if (row == null) { report.AppendLine($"[WARN] {rowField} null → bỏ qua."); return 0; }

        int n = 0;
        foreach (var tmp in row.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(tmp, "Chinh chu thuong");
            tmp.color = Color.white;
            tmp.fontStyle |= FontStyles.Bold;
            if (tmp.fontSize < minSize) tmp.fontSize = minSize;
            tmp.enableAutoSizing = false;
            EditorUtility.SetDirty(tmp);
            n++;
        }
        return n;
    }

    private static int WhitenSingleText(SerializedObject so, string field, float minSize, StringBuilder report)
    {
        var prop = so.FindProperty(field);
        var tmp  = prop != null ? prop.objectReferenceValue as TextMeshProUGUI : null;
        if (tmp == null) return 0;
        Undo.RecordObject(tmp, "Chinh chu phu");
        tmp.color = Color.white;
        if (tmp.fontSize < minSize) tmp.fontSize = minSize;
        EditorUtility.SetDirty(tmp);
        return 1;
    }
}
#endif
