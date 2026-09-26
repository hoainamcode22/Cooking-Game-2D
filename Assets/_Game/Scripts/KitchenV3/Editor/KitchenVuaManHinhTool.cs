// ============================================================================
//  Tools > Kitchen V3 > 18 / 19   (2026-09-25)
//  VI SAO BEP "CO LAI, DE NHAU": bep duoc can bo cuc tren Game view Free Aspect RAT RONG
//  (~2.25:1, giong dien thoai 20:9). Canvas Kitchen_UI_v3 dung CanvasScaler 1600x900, Match 0.5:
//    - man rong 20:9  -> canvas ~1810 x 800  -> du cho, dep
//    - man 16:9 (Full HD / PC / web / iPhone SE) -> canvas 1600 x 900 -> hep ngang 200 don vi,
//      cac khoi neo 2 ben (sach cong thuc, bep lo, khay) bi don vao giua -> de nhau.
//  Khong phai do tool mobile hay code thu nho (bo cuc trong scene khong doi tu 23/09).
//
//  18. Bep vua moi man hinh: CanvasScaler -> Reference 1800 x 800, Screen Match Mode = EXPAND.
//      Expand = canvas KHONG BAO GIO nho hon 1800x800 o ca 2 chieu: man rong giu y nhu anh dep,
//      man 16:9 / 4:3 thu nho deu ca bep (~11% o 16:9) chu khong don cac khoi de len nhau.
//      Khong dong vao vi tri bat ky object nao. Co Undo. Xong Ctrl+S.
//  19. Tra lai CanvasScaler cu (1600x900, Match 0.5).
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class KitchenVuaManHinhTool
{
    private const string Goc = "Tools/Kitchen V3/";
    private const string KhoaCu = "KitchenVuaManHinhTool_Cu";

    [System.Serializable] private class Cu { public Vector2 refRes; public float match; public int mode; }

    [MenuItem(Goc + "18. Bep vua moi man hinh (khong de nhau o 16:9)", false, 118)]
    private static void Lam()
    {
        var sc = Tim();
        if (sc == null) return;
        if (!EditorPrefs.HasKey(KhoaCu))
            EditorPrefs.SetString(KhoaCu, JsonUtility.ToJson(new Cu { refRes = sc.referenceResolution, match = sc.matchWidthOrHeight, mode = (int)sc.screenMatchMode }));
        Undo.RecordObject(sc, "Bep vua man hinh");
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1800f, 800f);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        EditorUtility.SetDirty(sc);
        EditorSceneManager.MarkSceneDirty(sc.gameObject.scene);
        Debug.Log("[BepVuaManHinh] Kitchen_UI_v3 CanvasScaler -> 1800x800 Expand. Ctrl+S de luu. Xem thu: Game view doi Full HD / Free Aspect / 4:3.");
        EditorUtility.DisplayDialog("Bep vua man hinh", "Xong. Bam Ctrl+S.\n\nThu doi Game view: Free Aspect (rong) phai giong anh dep, Full HD 16:9 bep nho deu lai va khong de nhau.\n\nTra lai: muc 19.", "OK");
    }

    [MenuItem(Goc + "19. Tra lai CanvasScaler bep nhu cu", false, 119)]
    private static void TraLai()
    {
        var sc = Tim();
        if (sc == null) return;
        var cu = EditorPrefs.HasKey(KhoaCu) ? JsonUtility.FromJson<Cu>(EditorPrefs.GetString(KhoaCu))
                                             : new Cu { refRes = new Vector2(1600f, 900f), match = 0.5f, mode = 0 };
        Undo.RecordObject(sc, "Tra lai CanvasScaler bep");
        sc.referenceResolution = cu.refRes;
        sc.matchWidthOrHeight = cu.match;
        sc.screenMatchMode = (CanvasScaler.ScreenMatchMode)cu.mode;
        EditorPrefs.DeleteKey(KhoaCu);
        EditorUtility.SetDirty(sc);
        EditorSceneManager.MarkSceneDirty(sc.gameObject.scene);
        Debug.Log("[BepVuaManHinh] Da tra lai CanvasScaler bep. Ctrl+S de luu.");
    }

    private static CanvasScaler Tim()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Bep vua man hinh", "Thoat Play mode truoc.", "OK"); return null; }
        foreach (var sc in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (sc != null && sc.name == "Kitchen_UI_v3" && !EditorUtility.IsPersistent(sc)) return sc;
        EditorUtility.DisplayDialog("Bep vua man hinh", "Khong thay Kitchen_UI_v3 (mo SampleScene roi bam lai).", "OK");
        return null;
    }
}
