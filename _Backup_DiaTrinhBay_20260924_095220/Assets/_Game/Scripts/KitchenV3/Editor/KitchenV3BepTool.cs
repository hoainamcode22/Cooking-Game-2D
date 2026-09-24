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
