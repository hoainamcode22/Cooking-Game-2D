// ============================================================================
//  Tools > VFX — dung / gan cac hieu ung nhe (2026-09-24)
//    1. Dung hieu ung moi truong vao Hierarchy  -> object [FarmAmbientFX] de chinh so luong, bat/tat
//       (khong bam van chay: game tu tao luc Play voi thong so mac dinh)
//    2. Gan NAY MO cho popup dang chon           -> chon bang chinh cua popup trong Hierarchy roi bam
//    3. Gan ANH SANG LUOT cho nut dang chon      -> chon nut roi bam
//    4. Go 2 hieu ung tren khoi object dang chon
//    5. (Play) Xem thu dom dom bat / tat
//    6. Phong to may thuc an gia suc cho vua tam sap cho (giu chan may tai cho)
//  Xong nho Ctrl+S.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VfxToolMenu
{
    [MenuItem("Tools/VFX/1. Dung hieu ung moi truong vao Hierarchy (FarmAmbientFX)", false, 10)]
    private static void DungMoiTruong()
    {
        var cu = Object.FindFirstObjectByType<FarmAmbientFX>(FindObjectsInactive.Include);
        if (cu != null) { Selection.activeGameObject = cu.gameObject; Debug.Log("[VFX] Scene da co [FarmAmbientFX] - chinh trong Inspector."); return; }
        var go = new GameObject("[FarmAmbientFX]");
        Undo.RegisterCreatedObjectUndo(go, "FarmAmbientFX");
        go.AddComponent<FarmAmbientFX>();
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("[VFX] Da dung [FarmAmbientFX]: bong may, lap lanh nuoc, buom, chim, dom dom, bui dat, sao lap lanh. Chinh trong Inspector roi Ctrl+S.");
    }

    [MenuItem("Tools/VFX/2. Gan nay mo cho popup dang chon", false, 20)]
    private static void GanNay() => Gan<UIPopBounce>("nay mo");

    [MenuItem("Tools/VFX/3. Gan anh sang luot cho nut dang chon", false, 21)]
    private static void GanShine()
    {
        // [2026-09-24] Chi gan cho NUT (co Button, khong phai Canvas, khong qua to). Truoc day chon nham
        // popup/canvas van gan duoc -> vet sang khong lo quet ngang man hinh.
        int n = 0, bo = 0;
        foreach (var go in Selection.gameObjects)
        {
            var rt = go.transform as RectTransform;
            bool laNut = rt != null && go.GetComponent<UnityEngine.UI.Selectable>() != null && go.GetComponent<Canvas>() == null
                         && rt.rect.width <= 900f && rt.rect.height <= 400f;
            if (!laNut) { bo++; Debug.LogWarning("[VFX] Bo qua '" + go.name + "': khong phai nut. Anh sang luot chi gan cho NUT (object co Button)."); continue; }
            if (go.GetComponent<UIShineSweep>() != null) continue;
            Undo.AddComponent<UIShineSweep>(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            n++;
        }
        Debug.Log("[VFX] Gan anh sang luot cho " + n + " nut" + (bo > 0 ? (", bo qua " + bo + " object khong phai nut.") : "."));
    }

    [MenuItem("Tools/VFX/4. Go nay mo + anh sang luot khoi object dang chon", false, 22)]
    private static void Go()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var c in go.GetComponents<UIPopBounce>()) { Undo.DestroyObjectImmediate(c); n++; }
            foreach (var c in go.GetComponents<UIShineSweep>()) { Undo.DestroyObjectImmediate(c); n++; }
            var m = go.transform.Find("Fx_ShineMask");
            if (m != null) { Undo.DestroyObjectImmediate(m.gameObject); n++; }
            if (n > 0) EditorSceneManager.MarkSceneDirty(go.scene);
        }
        Debug.Log("[VFX] Da go " + n + " muc.");
    }

    private static void Gan<T>(string ten) where T : Component
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (!(go.transform is RectTransform) || go.GetComponent<T>() != null) continue;
            Undo.AddComponent<T>(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            n++;
        }
        if (n == 0) EditorUtility.DisplayDialog("VFX", "Hay chon 1 hay nhieu object UI (RectTransform) trong Hierarchy truoc.", "OK");
        else Debug.Log("[VFX] Da gan " + ten + " cho " + n + " object. Ctrl+S de luu.");
    }

    // ── 6. Phong to may thuc an gia suc cho vua tam voi sap cho ──
    private const float TI_LE_MAY_SO_VOI_CHO = 0.82f;   // chieu cao may = 82% chieu cao sap cho

    [MenuItem("Tools/VFX/6. Phong to may thuc an gia suc (vua tam sap cho)", false, 30)]
    private static void PhongToMay()
    {
        var click = Object.FindFirstObjectByType<MillBuildingClick>(FindObjectsInactive.Include);
        if (click == null) { EditorUtility.DisplayDialog("VFX", "Khong thay may thuc an (MillBuildingClick). Hay mo SCN_Farm.", "OK"); return; }
        var may = click.transform;
        // leo len goc cua instance prefab (MayThucAn_Anim)
        var gocPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(may.gameObject);
        if (gocPrefab != null) may = gocPrefab.transform;

        GameObject cho = GameObject.Find("Market");                      // sap cho lon (cho.png)
        if (cho == null) cho = GameObject.Find("Stall_WorldObject");
        if (cho == null) { var st = Object.FindFirstObjectByType<StallWorldObject>(FindObjectsInactive.Include); if (st != null) cho = st.gameObject; }
        if (cho == null) { EditorUtility.DisplayDialog("VFX", "Khong thay sap cho (Market / Stall_WorldObject) de so kich thuoc.", "OK"); return; }

        if (!DoBounds(may, out Bounds bm) || !DoBounds(cho.transform, out Bounds bc))
        { EditorUtility.DisplayDialog("VFX", "Khong do duoc kich thuoc (thieu SpriteRenderer).", "OK"); return; }

        float heSo = (bc.size.y * TI_LE_MAY_SO_VOI_CHO) / Mathf.Max(1f, bm.size.y);
        if (heSo <= 1.02f) { Debug.Log("[VFX] May thuc an da du to (" + bm.size.y.ToString("0") + " / cho " + bc.size.y.ToString("0") + "). Khong doi."); Selection.activeTransform = may; return; }

        Undo.RecordObject(may, "Phong to may thuc an");
        Vector3 dayGiuaTruoc = new Vector3(bm.center.x, bm.min.y, 0f);
        may.localScale = new Vector3(may.localScale.x * heSo, may.localScale.y * heSo, may.localScale.z);
        DoBounds(may, out Bounds bm2);
        Vector3 dayGiuaSau = new Vector3(bm2.center.x, bm2.min.y, 0f);
        may.position += dayGiuaTruoc - dayGiuaSau;             // giu nguyen chan may tren mat dat
        EditorSceneManager.MarkSceneDirty(may.gameObject.scene);
        Selection.activeTransform = may;
        Debug.Log("[VFX] May thuc an x" + heSo.ToString("0.00") + " (cao " + bm.size.y.ToString("0") + " -> " + bm2.size.y.ToString("0") +
                  ", sap cho cao " + bc.size.y.ToString("0") + "). Giu chan may tai cho cu. Chinh them bang Scale neu can roi Ctrl+S.");
    }

    private static bool DoBounds(Transform t, out Bounds b)
    {
        b = default;
        bool co = false;
        foreach (var r in t.GetComponentsInChildren<SpriteRenderer>(false))
        {
            if (r.sprite == null || !r.enabled) continue;
            if (!co) { b = r.bounds; co = true; } else b.Encapsulate(r.bounds);
        }
        return co;
    }

    [MenuItem("Tools/VFX/5. (Play) Xem thu dom dom bat - tat", false, 40)]
    private static void DomDom()
    {
        var fx = FarmAmbientFX.Instance;
        if (!Application.isPlaying || fx == null) { EditorUtility.DisplayDialog("VFX", "Vao Play o SCN_Farm truoc.", "OK"); return; }
        int moi = fx.CheDoDomDom == 1 ? 0 : 1;
        fx.DatCheDoDomDom(moi);
        Debug.Log("[VFX] Dom dom: " + (moi == 1 ? "LUON BAT (xem thu)" : "theo gio may (18h-6h)"));
    }

    // [2026-09-24] Vet sang to quet ngang man hinh = UIShineSweep lo gan vao Canvas/popup toan man hinh.
    [MenuItem("Tools/VFX/7. Go vet sang luot khoi popup + canvas (chi giu tren nut)", false, 23)]
    private static void GoShineKhoiPopup()
    {
        int n = 0;
        foreach (var s in Object.FindObjectsByType<UIShineSweep>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s == null) continue;
            var rt = s.transform as RectTransform;
            bool laNut = s.GetComponent<UnityEngine.UI.Selectable>() != null && s.GetComponent<Canvas>() == null
                         && rt != null && rt.rect.width <= 900f && rt.rect.height <= 400f;
            if (laNut) continue;
            var m = s.transform.Find("Fx_ShineMask");
            if (m != null) Undo.DestroyObjectImmediate(m.gameObject);
            EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
            Debug.Log("[VFX] Go vet sang khoi: " + s.gameObject.name);
            Undo.DestroyObjectImmediate(s);
            n++;
        }
        Debug.Log("[VFX] Da go vet sang luot khoi " + n + " popup/canvas. Bam Ctrl+S.");
    }

    // [2026-09-24] Icon tren map giu co theo zoom (zoom xa to len, zoom gan nho lai)
    [MenuItem("Tools/VFX/8. Icon dang chon: giu co theo zoom (CoTheoZoom)", false, 24)]
    private static void GanCoTheoZoom()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go.GetComponent<CoTheoZoom>() != null) continue;
            Undo.AddComponent<CoTheoZoom>(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            n++;
        }
        Debug.Log("[VFX] Gan CoTheoZoom cho " + n + " object. Chinh 'Do Manh' (0 tat - 1 giu dung co man hinh) roi Ctrl+S.");
    }
}
