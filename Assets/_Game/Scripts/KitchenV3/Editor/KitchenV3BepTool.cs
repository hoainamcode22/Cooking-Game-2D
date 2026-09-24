// ============================================================================
//  Tools > Kitchen V3 > 12. Dung lua lo vao Hierarchy (chinh size lua)
//  Tools > Kitchen V3 > 13. Ra soat: can giua chu + icon, chu khong loi khung, o trong cung khung
//  Chay trong Unity, mo SampleScene. Xong bam Ctrl+S. (2026-09-24)
// ============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class KitchenV3BepTool
{
    // [2026-09-24] Da GO tu chay: Sep chinh tay. Tool 13 van con trong menu neu muon dung.
    private static Transform TimBep()
    {
        foreach (var ui in Object.FindObjectsByType<KitchenUIv2.KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ui != null && ui.gameObject.name == "Kitchen_UI_v3") return ui.transform;
        return null;
    }

    private static Transform TimSau(Transform cha, string ten)
    {
        if (cha == null) return null;
        for (int i = 0; i < cha.childCount; i++)
        {
            var c = cha.GetChild(i);
            if (c.name == ten) return c;
            var s = TimSau(c, ten);
            if (s != null) return s;
        }
        return null;
    }

    [MenuItem("Tools/Kitchen V3/12. Dung lua lo vao Hierarchy (chinh size lua)", false, 112)]
    private static void DungLua()
    {
        var bep = TimBep();
        var oven = bep != null ? TimSau(bep, "Oven") as RectTransform : null;
        if (oven == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Kitchen_UI_v3/Oven. Hay mo SampleScene.", "OK"); return; }

        var truoc = new HashSet<string>();
        for (int i = 0; i < oven.childCount; i++) truoc.Add(oven.GetChild(i).name);
        Undo.RegisterFullObjectHierarchyUndo(oven.gameObject, "Dung lua lo");

        var bo = KitchenJuiceFX.DamBaoLuaTrongLo(oven);
        // Sprite quang sang ve bang code khong luu duoc vao scene -> dung ban PNG (tao boi tool bubble) neu co
        var glowPng = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/VFX/Soft/soft_glow.png");
        if (bo.glow != null && glowPng != null) bo.glow.sprite = glowPng;
        var ds = new List<Image>(bo.nho) { bo.glow, bo.lua };
        foreach (var im in ds)
        {
            if (im == null) continue;
            if (!truoc.Contains(im.name)) Undo.RegisterCreatedObjectUndo(im.gameObject, "Dung lua lo");
            im.enabled = true;                                  // Edit mode: hien de Sep thay va chinh
            var c = im.color; if (c.a < 0.5f) { c.a = 1f; im.color = c; }
        }
        EditorSceneManager.MarkSceneDirty(oven.gameObject.scene);
        Selection.activeGameObject = bo.lua != null ? bo.lua.gameObject : oven.gameObject;
        Debug.Log("[Kitchen V3] Lua lo trong Hierarchy: Oven/Fx_ComboFire (lua chinh luc bam nau), Fx_ComboGlow (quang sang), " +
                  "Fx_LuaNho_0..2 (3 dom lua nho quanh). Keo/scale tuy y roi Ctrl+S - Play dung dung kich thuoc do lam goc. " +
                  "Lua luc DANG NAU la Oven/Oven_Fire (scale truc tiep).");
    }

    // [2026-09-24] Tra lai vi tri CU cua ten mon / cap / thuong (truoc lan tu can giua bi loi chong chu)
    [MenuItem("Tools/Kitchen V3/14. Tra lai vi tri cu: ten mon, cap, thuong (bang chi tiet)", false, 114)]
    private static void TraLaiThongTinMon()
    {
        var bep = TimBep();
        if (bep == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Kitchen_UI_v3. Hay mo SampleScene.", "OK"); return; }
        Transform bd = null;
        foreach (var t in bep.GetComponentsInChildren<Transform>(true))
            if (t.name == "Board_Detail" && t.Find("Card_Info") != null) { bd = t; break; }
        if (bd == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Board_Detail/Card_Info.", "OK"); return; }
        var cu = new (string ten, Vector2 pos, Vector2 size)[]
        {
            ("Txt_DishName", new Vector2(170.8f, -61.7f), new Vector2(273.016f, 30f)),
            ("Txt_DishMeta", new Vector2(185.3f, -97.5f), new Vector2(258.516f, 22f)),
            ("Txt_Rewards",  new Vector2(126f, -123f),   new Vector2(291.816f, 22f)),
        };
        int n = 0;
        foreach (var c in cu)
        {
            var rt = bd.Find(c.ten) as RectTransform;
            if (rt == null) continue;
            Undo.RecordObject(rt, "Tra lai vi tri cu");
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = c.pos;
            rt.sizeDelta = c.size;
            rt.localScale = Vector3.one;
            n++;
        }
        EditorSceneManager.MarkSceneDirty(bd.gameObject.scene);
        Selection.activeTransform = bd.Find("Txt_DishName");
        Debug.Log("[Kitchen V3] Tra lai vi tri cu cho " + n + " o chu (ten mon / cap / thuong). Gio Sep keo tay tuy y roi Ctrl+S - Play giu nguyen, code khong tu doi nua.");
    }

    // [2026-09-24] Icon mon tren dia Plating_Table dua vao Hierarchy de Sep chinh do to + can giua bang tay.
    //  Play: mon nau xong bay tu noi -> ha canh DUNG vi tri/kich thuoc/scale cua Dish_Visual nay.
    [MenuItem("Tools/Kitchen V3/15. Icon mon tren dia trinh bay (Dish_Visual) vao Hierarchy", false, 115)]
    private static void IconMonTrenDia()
    {
        var bep = TimBep();
        var dia = bep != null ? bep.Find("Plating_Table") as RectTransform : null;
        if (dia == null && bep != null) dia = TimSau(bep, "Plating_Table") as RectTransform;
        if (dia == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Kitchen_UI_v3/Plating_Table. Hay mo SampleScene.", "OK"); return; }

        var rt = dia.Find("Dish_Visual") as RectTransform;
        bool moi = rt == null;
        if (moi)
        {
            var go = new GameObject("Dish_Visual", typeof(RectTransform), typeof(Image));
            go.layer = dia.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(go, "Icon mon tren dia");
            rt = (RectTransform)go.transform;
            rt.SetParent(dia, false);
            rt.SetAsFirstSibling();                              // duoi nhan "Plating", tren mat dia
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 6f);
            rt.sizeDelta = new Vector2(72f, 72f);
            rt.localScale = Vector3.one;
        }
        else Undo.RecordObject(rt.gameObject, "Icon mon tren dia");

        var img = rt.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(rt.gameObject);
        Undo.RecordObject(img, "Icon mon tren dia");
        img.preserveAspect = true;
        img.raycastTarget = false;                              // khong chan nut cham dia
        if (img.sprite == null) img.sprite = SpriteMonMau();    // chi de xem trong Edit mode, Play tu thay bang mon that
        img.color = Color.white;
        rt.gameObject.SetActive(true);

        // Gan san cho keo-tha dia -> kho (truoc day Awake khong thay vi Dish_Visual sinh luc chay)
        var keo = dia.GetComponent<KitchenUIv3.KitchenV3PlateDrag>();
        if (keo != null) { Undo.RecordObject(keo, "Icon mon tren dia"); keo.iconMon = img; EditorUtility.SetDirty(keo); }
        foreach (var fx in bep.GetComponentsInChildren<KitchenUIv3.KitchenV3CookingFX>(true))
        { Undo.RecordObject(fx, "Icon mon tren dia"); fx.diemDen = rt; EditorUtility.SetDirty(fx); }

        EditorSceneManager.MarkSceneDirty(dia.gameObject.scene);
        Selection.activeGameObject = rt.gameObject;
        EditorGUIUtility.PingObject(rt.gameObject);
        Debug.Log("[Kitchen V3] " + (moi ? "Da tao" : "Da co san") + " Plating_Table/Dish_Visual. Keo/scale (phim T hoac Width/Height) cho vua dia roi Ctrl+S. " +
                  "Play: mon nau xong bay tu noi ha canh dung cho nay; cham/keo dia cat kho xong icon ve lai dung cho nay.");
    }

    private static Sprite SpriteMonMau()
    {
        foreach (var g in AssetDatabase.FindAssets("t:DishData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d.dishSprite != null) return d.dishSprite;
        }
        return null;
    }

    // [2026-09-24] Chu bang chi tiet mon bi phong to (scale 1.5 / 1.58 / 1.8) -> ve scale 1, co chu vua khung,
    // ten + cap + thuong xep 3 dong gon ben phai anh mon; tieu de "Ingredients needed" / "Flavor" nam gon trong the.
    [MenuItem("Tools/Kitchen V3/16. Thu nho chu bang chi tiet mon (ten, cap, thuong, tieu de)", false, 116)]
    private static void ThuNhoChuChiTiet()
    {
        var bep = TimBep();
        if (bep == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Kitchen_UI_v3. Hay mo SampleScene.", "OK"); return; }
        Transform bd = null;
        foreach (var t in bep.GetComponentsInChildren<Transform>(true))
            if (t.name == "Board_Detail" && t.Find("Card_Info") != null) { bd = t; break; }
        if (bd == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Board_Detail/Card_Info.", "OK"); return; }
        //            ten              vi tri (goc trai tren)        khung                 co max  co min
        var ds = new (string ten, Vector2 pos, Vector2 size, float coMax, float coMin)[]
        {
            ("Txt_DishName",   new Vector2(130f, -66f),  new Vector2(275f, 32f), 24f, 16f),
            ("Txt_DishMeta",   new Vector2(130f, -98f),  new Vector2(275f, 24f), 16f, 11f),
            ("Txt_Rewards",    new Vector2(130f, -122f), new Vector2(275f, 22f), 14f, 10f),
            ("Txt_NeedTitle",  new Vector2(48f, -165f),  new Vector2(360f, 26f), 18f, 12f),
            ("Txt_TasteTitle", new Vector2(48f, -282f),  new Vector2(360f, 26f), 18f, 12f),
        };
        int n = 0;
        foreach (var c in ds)
        {
            var rt = bd.Find(c.ten) as RectTransform;
            if (rt == null) continue;
            Undo.RecordObject(rt, "Thu nho chu chi tiet");
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = c.pos;
            rt.sizeDelta = c.size;
            var tx = rt.GetComponent<TMPro.TMP_Text>();
            if (tx != null)
            {
                Undo.RecordObject(tx, "Thu nho chu chi tiet");
                tx.enableAutoSizing = true;
                tx.fontSizeMax = c.coMax;
                tx.fontSizeMin = c.coMin;
                tx.fontSize = c.coMax;
                tx.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                tx.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                tx.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
                EditorUtility.SetDirty(tx);
            }
            n++;
        }
        EditorSceneManager.MarkSceneDirty(bd.gameObject.scene);
        Selection.activeTransform = bd.Find("Txt_DishName");
        Debug.Log("[Kitchen V3] Thu nho " + n + " o chu bang chi tiet (scale ve 1, co chu toi da: ten 24, cap 16, thuong 14, tieu de 18). " +
                  "Mon nao ten dai tu co nho lai vua khung. Keo tay tiep neu muon roi Ctrl+S - Play giu dung nhu Edit.");
    }

    [MenuItem("Tools/Kitchen V3/13. Ra soat: can giua chu + icon, chu khong loi khung", false, 113)]
    private static void CanGiua()
    {
        var bep = TimBep();
        if (bep == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong thay Kitchen_UI_v3. Hay mo SampleScene.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(bep.gameObject, "Can giua bep");
        int n = KitchenCanGiua.ApDung(bep, true, true);
        n += KitchenCanGiua.DongBoOTrong(bep);
        EditorSceneManager.MarkSceneDirty(bep.gameObject.scene);
        Debug.Log("[Kitchen V3] Ra soat xong: sua " + n + " muc (chu can giua, icon the can giua, chu thut vao trong khung, o trong dung khung the). " +
                  "Giu nguyen: hang Huong vi, tieu de muc, chip don, bang den. Khong vua y: Ctrl+Z. Vua y: Ctrl+S.");
    }
}
