// ============================================================================
//  Tools > Khach du lich > 1. Dung bubble don hang (Hierarchy + prefab dung chung)
//  Tools > Khach du lich > 2. Dung lai bubble tu dau (xoa ban cu trong scene)
// ----------------------------------------------------------------------------
//  Chay TRONG Unity, mo SCN_Farm. Tool:
//    - Dat "TouristOrderBubble" vao cuoi Canvas_HUD (tren HUD, duoi cac popup).
//    - Da co prefab PF_TouristOrderBubble -> chi tha prefab vao (giu ban Sep da chinh).
//      Chua co -> dung du cay con, dien san 1 mon mau de xem truoc, luu thanh prefab + noi.
//    - Bubble_Root de BAT o Edit mode de Sep keo tha; vao Play tu an, cham khach moi hien.
//  Xong bam Ctrl+S. Sua prefab (Open Prefab) -> moi khach dung chung ban moi.
// ============================================================================
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TouristOrderBubbleTool
{
    const string THU_MUC_PREFAB = "Assets/_Game/Prefab/ui/Tourist";
    const string PREFAB = THU_MUC_PREFAB + "/PF_TouristOrderBubble.prefab";
    const string THU_MUC_ANH = "Assets/_Game/Art/VFX/Soft";
    const string STD = "Assets/Resources/UI/Standard/";

    static readonly Color NauDam  = new Color(0.36f, 0.20f, 0.09f, 1f);
    static readonly Color NauNhat = new Color(0.55f, 0.40f, 0.25f, 1f);
    static readonly Color Vang    = new Color(0.72f, 0.50f, 0.06f, 1f);
    static readonly Color XanhExp = new Color(0.18f, 0.45f, 0.85f, 1f);
    static readonly Color Kem     = new Color(1f, 0.97f, 0.89f, 1f);

    static TMP_FontAsset _font;

    [MenuItem("Tools/Khach du lich/1. Dung bubble don hang (Hierarchy + prefab)", false, 10)]
    private static void Dung() => Chay(false);

    [MenuItem("Tools/Khach du lich/2. Dung lai bubble tu dau (xoa ban cu trong scene)", false, 11)]
    private static void DungLai()
    {
        if (!EditorUtility.DisplayDialog("Bubble don hang",
            "Xoa TouristOrderBubble trong scene va dung lai tu dau?\n(Prefab cu se bi GHI DE bang ban moi.)", "Dung lai", "Huy")) return;
        Chay(true);
    }

    private static void Chay(bool lamLai)
    {
        var canvas = TimCanvasHud();
        if (canvas == null) { EditorUtility.DisplayDialog("Bubble don hang", "Khong thay Canvas_HUD. Hay mo SCN_Farm.", "OK"); return; }

        var cu = Object.FindFirstObjectByType<TouristOrderBubbleUI>(FindObjectsInactive.Include);
        if (cu != null && !lamLai)
        {
            Selection.activeGameObject = cu.gameObject;
            EditorGUIUtility.PingObject(cu.gameObject);
            Debug.Log("[Bubble don hang] Scene DA CO '" + cu.name + "' - khong dung them. Muon dung lai: Tools > Khach du lich > 2.");
            return;
        }
        if (cu != null) Undo.DestroyObjectImmediate(cu.gameObject);

        DamBaoAnhMem();
        _font = TimFont(canvas.transform);

        GameObject go;
        bool coPrefab = !lamLai && File.Exists(PREFAB);
        if (coPrefab)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            go = (GameObject)PrefabUtility.InstantiatePrefab(pf, canvas.transform);
            Undo.RegisterCreatedObjectUndo(go, "Bubble don hang");
        }
        else
        {
            go = DungCay(canvas.transform);
            Undo.RegisterCreatedObjectUndo(go, "Bubble don hang");
            if (!AssetDatabase.IsValidFolder(THU_MUC_PREFAB)) TaoThuMuc(THU_MUC_PREFAB);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, PREFAB, InteractionMode.UserAction);
        }
        go.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(go.scene);
        var root = go.transform.Find("Bubble_Root");
        Selection.activeTransform = root != null ? root : go.transform;
        Debug.Log("[Bubble don hang] " + (coPrefab ? "Tha prefab co san" : "Dung moi + luu prefab") + " -> " + PREFAB +
                  ". Keo tha Bubble_Root/Img_Frame tuy y roi bam Ctrl+S (va Overrides > Apply All neu muon luu vao prefab).");
    }

    // ---------------------------------------------------------------------
    //  Dung cay
    // ---------------------------------------------------------------------

    private static GameObject DungCay(Transform canvas)
    {
        var goc = Tao("TouristOrderBubble", canvas, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        goc.gameObject.AddComponent<TouristOrderBubbleUI>();

        Sprite khung   = Lay(STD + "inner_panel.png");
        Sprite ruyBang = Lay(STD + "ribbon_banner_gold.png");
        Sprite o       = Lay(STD + "slot_normal.png");
        Sprite nutDo   = Lay(STD + "btn_red_small.png");
        Sprite nutXanh = Lay(STD + "btn_green_3d.png");
        Sprite glow    = Lay(THU_MUC_ANH + "/soft_glow.png");
        Sprite cham    = Lay(THU_MUC_ANH + "/bubble_dot.png");
        var lib = RewardIconLibrary.Instance;
        Sprite vang = lib != null && lib.goldSprite != null ? lib.goldSprite : Lay(STD + "icon_gold.png");
        Sprite exp  = lib != null && lib.expSprite  != null ? lib.expSprite  : Lay("Assets/_Game/Farm/Art/UI_OrderBoard/ob_star.png");

        var c = new Vector2(0.5f, 0.5f);
        var duoiGiua = new Vector2(0.5f, 0f);
        var trenTrai = new Vector2(0f, 1f);

        // Bubble_Root: pivot day-giua = diem mui bubble cham dau khach
        var root = Tao("Bubble_Root", goc, c, c, duoiGiua, new Vector2(0f, -220f), new Vector2(600f, 540f));
        root.gameObject.AddComponent<CanvasGroup>();

        var hq = Tao("Img_Glow", root, duoiGiua, duoiGiua, duoiGiua, new Vector2(0f, 30f), new Vector2(780f, 580f));
        Anh(hq, glow, new Color(1f, 0.95f, 0.75f, 0.3f));

        var d0 = Tao("Tail_Dot_0", root, duoiGiua, duoiGiua, c, new Vector2(-6f, 16f), new Vector2(26f, 26f));
        Anh(d0, cham, Color.white);
        var d1 = Tao("Tail_Dot_1", root, duoiGiua, duoiGiua, c, new Vector2(14f, 48f), new Vector2(42f, 42f));
        Anh(d1, cham, Color.white);

        var frame = Tao("Img_Frame", root, duoiGiua, duoiGiua, duoiGiua, new Vector2(0f, 70f), new Vector2(600f, 470f));
        Anh(frame, khung, Kem, true, true);

        var rb = Tao("Header_Ribbon", frame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), c, new Vector2(0f, 4f), new Vector2(330f, 64f));
        Anh(rb, ruyBang, Color.white);
        var tt = Tao("Txt_Title", rb, Vector2.zero, Vector2.one, c, new Vector2(0f, 4f), new Vector2(-40f, -14f));
        Chu(tt, "Tourist Order", 26f, new Color(0.4f, 0.2f, 0.05f), TextAlignmentOptions.Center, true);

        var close = Tao("Btn_Close", frame, Vector2.one, Vector2.one, c, new Vector2(-8f, -8f), new Vector2(58f, 58f));
        Anh(close, nutDo, Color.white, false, true);
        close.gameObject.AddComponent<Button>();
        var cx = Tao("Txt_X", close, Vector2.zero, Vector2.one, c, new Vector2(0f, 2f), Vector2.zero);
        Chu(cx, "X", 30f, Color.white, TextAlignmentOptions.Center, true);

        var box = Tao("Dish_Box", frame, trenTrai, trenTrai, trenTrai, new Vector2(30f, -56f), new Vector2(150f, 150f));
        Anh(box, o, Color.white);
        var dish = Tao("Img_Dish", box, Vector2.zero, Vector2.one, c, Vector2.zero, new Vector2(-24f, -24f));
        var imDish = Anh(dish, null, Color.white); imDish.preserveAspect = true;

        var ten = Tao("Txt_DishName", frame, trenTrai, trenTrai, trenTrai, new Vector2(198f, -58f), new Vector2(372f, 44f));
        var tTen = Chu(ten, "Dish name", 32f, NauDam, TextAlignmentOptions.Left, true); tTen.fontSizeMin = 20f;
        var kho = Tao("Txt_Stock", frame, trenTrai, trenTrai, trenTrai, new Vector2(198f, -104f), new Vector2(372f, 30f));
        Chu(kho, "In storage: 0", 22f, NauNhat, TextAlignmentOptions.Left, false);

        DungThuong("Reward_Gold", frame, new Vector2(198f, -146f), new Vector2(170f, 56f), vang, "+0", Vang);
        DungThuong("Reward_Exp",  frame, new Vector2(380f, -146f), new Vector2(190f, 56f), exp, "+0 EXP", XanhExp);

        var need = Tao("Txt_NeedTitle", frame, trenTrai, trenTrai, trenTrai, new Vector2(30f, -222f), new Vector2(540f, 30f));
        Chu(need, "Ingredients needed:", 22f, NauDam, TextAlignmentOptions.Left, true);

        var row = Tao("Row_Ingredients", frame, trenTrai, trenTrai, trenTrai, new Vector2(30f, -256f), new Vector2(540f, 100f));
        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 12f; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        for (int i = 0; i < 6; i++)
        {
            var s = Tao("Ing_Slot_" + i, row, c, c, c, Vector2.zero, new Vector2(80f, 100f));
            var bg = Tao("Img_Bg", s, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(80f, 80f));
            Anh(bg, o, Color.white);
            var ic = Tao("Img_Icon", bg, Vector2.zero, Vector2.one, c, Vector2.zero, new Vector2(-18f, -18f));
            Anh(ic, null, Color.white).preserveAspect = true;
            var nm = Tao("Txt_Name", s, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(92f, 20f));
            var tn = Chu(nm, "", 15f, NauDam, TextAlignmentOptions.Center, false); tn.fontSizeMin = 10f;
        }

        // Hao quang nut: dat TRUOC nut de ve phia sau nut
        var bgl = Tao("Img_BtnGlow", frame, duoiGiua, duoiGiua, c, new Vector2(0f, 65f), new Vector2(430f, 150f));
        Anh(bgl, glow, new Color(1f, 0.86f, 0.35f, 0.5f));
        var btn = Tao("Btn_Action", frame, duoiGiua, duoiGiua, c, new Vector2(0f, 65f), new Vector2(320f, 86f));
        Anh(btn, nutXanh, Color.white, false, true);
        btn.gameObject.AddComponent<Button>();
        var ta = Tao("Txt_Action", btn, Vector2.zero, Vector2.one, c, new Vector2(0f, 4f), new Vector2(-30f, -16f));
        Chu(ta, "Deliver", 34f, Color.white, TextAlignmentOptions.Center, true);

        // Mau chu "vui ve" bay len khi giao xong (tat san - code clone ra)
        var fx = Tao("Fx_Happy", goc, c, c, c, new Vector2(0f, 120f), new Vector2(430f, 84f));
        Anh(fx, khung, Kem, true);
        var ft = Tao("Txt_Happy", fx, Vector2.zero, Vector2.one, c, Vector2.zero, new Vector2(-30f, -12f));
        Chu(ft, "Delicious! Thank you!", 36f, new Color(0.85f, 0.32f, 0.2f), TextAlignmentOptions.Center, true);
        fx.gameObject.SetActive(false);

        DienMonMau(dish.GetComponent<Image>(), tTen, row);
        return goc.gameObject;
    }

    private static void DungThuong(string ten, RectTransform cha, Vector2 pos, Vector2 size, Sprite icon, string chu, Color mau)
    {
        var tl = new Vector2(0f, 1f);
        var r = Tao(ten, cha, tl, tl, tl, pos, size);
        var ic = Tao("Img_Icon", r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
        Anh(ic, icon, Color.white).preserveAspect = true;
        var t = Tao("Txt_Value", r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(size.x - 56f, 44f));
        var tx = Chu(t, chu, 30f, mau, TextAlignmentOptions.Left, true); tx.fontSizeMin = 18f;
    }

    /// <summary>Dien 1 mon that de Sep nhin thay bubble day du o Edit mode (Play se thay bang mon cua khach).</summary>
    private static void DienMonMau(Image imDish, TMP_Text tTen, RectTransform row)
    {
        DishData mau = null;
        foreach (var g in AssetDatabase.FindAssets("t:DishData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d.dishSprite != null && d.requiredIngredients != null && d.requiredIngredients.Count >= 3) { mau = d; break; }
        }
        if (mau == null) return;
        imDish.sprite = mau.dishSprite;
        tTen.text = !string.IsNullOrEmpty(mau.dishName) ? mau.dishName : mau.dishId;
        for (int i = 0; i < row.childCount; i++)
        {
            var s = row.GetChild(i);
            var ing = i < mau.requiredIngredients.Count ? mau.requiredIngredients[i] : null;
            var ic = s.Find("Img_Bg/Img_Icon");
            if (ic != null && ing != null) ic.GetComponent<Image>().sprite = ing.icon;
            var nm = s.Find("Txt_Name");
            if (nm != null && ing != null) nm.GetComponent<TMP_Text>().text = ing.displayName;
        }
    }

    // ---------------------------------------------------------------------
    //  Anh mem (PNG) cho Edit mode
    // ---------------------------------------------------------------------

    private static void DamBaoAnhMem()
    {
        if (!AssetDatabase.IsValidFolder(THU_MUC_ANH)) TaoThuMuc(THU_MUC_ANH);
        GhiPNG(THU_MUC_ANH + "/soft_glow.png", SoftFxSprites.VeGlow(128));
        GhiPNG(THU_MUC_ANH + "/soft_circle.png", SoftFxSprites.VeCircle(64));
        GhiPNG(THU_MUC_ANH + "/soft_sparkle.png", SoftFxSprites.VeSparkle(64));
        GhiPNG(THU_MUC_ANH + "/bubble_dot.png", VeChamBubble(96));
    }

    private static void GhiPNG(string path, Texture2D tex)
    {
        if (File.Exists(path)) { Object.DestroyImmediate(tex); return; }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
    }

    /// <summary>Cham tron kem vien nau (duoi bubble suy nghi).</summary>
    private static Texture2D VeChamBubble(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f, R = c - 1f, vien = n * 0.07f;
        Color kem = Kem, nau = new Color(0.45f, 0.27f, 0.12f, 1f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(R - d + 0.5f);
                float kVien = Mathf.Clamp01(d - (R - vien) + 0.5f);
                Color col = Color.Lerp(kem, nau, kVien); col.a = a;
                px[y * n + x] = col;
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    // ---------------------------------------------------------------------
    //  Tien ich
    // ---------------------------------------------------------------------

    private static Canvas TimCanvasHud()
    {
        Canvas thay = null;
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!cv.isRootCanvas) continue;
            if (cv.name == "Canvas_HUD") return cv;
            if (thay == null && cv.renderMode == RenderMode.ScreenSpaceOverlay && cv.name.Contains("HUD")) thay = cv;
        }
        return thay;
    }

    private static TMP_FontAsset TimFont(Transform canvas)
    {
        foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (t.font != null) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void TaoThuMuc(string path)
    {
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static Sprite Lay(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static RectTransform Tao(string ten, Transform cha, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(ten, typeof(RectTransform));
        go.layer = cha.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(cha, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image Anh(RectTransform rt, Sprite sp, Color mau, bool sliced = false, bool ray = false)
    {
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = sp;
        im.color = mau;
        im.raycastTarget = ray;
        if (sliced && sp != null && sp.border != Vector4.zero) im.type = Image.Type.Sliced;
        return im;
    }

    private static TextMeshProUGUI Chu(RectTransform rt, string s, float co, Color mau, TextAlignmentOptions canh, bool dam)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = s;
        t.fontSize = co;
        t.enableAutoSizing = true; t.fontSizeMax = co; t.fontSizeMin = Mathf.Max(10f, co * 0.6f);
        t.color = mau;
        t.alignment = canh;
        t.fontStyle = dam ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }
}
