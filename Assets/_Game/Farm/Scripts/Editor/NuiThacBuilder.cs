using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// VONG 14 — Bo cong cu Thac Nuoc.
///
/// LICH SU: ban dau tool nay dung ca ngon nui (4 tang tuong da + dinh + ho chan thac).
/// Sep xem thay XAU, chi giu lai dong suoi. Nen tool da duoc viet lai:
///   - Bo han phan dung nui.
///   - Menu 1: sinh 2 PREFAB (thac + bot) de Sep tu keo tha, tu dung con thac theo y minh.
///   - Menu 2: go phan nui cu ra khoi scene NHUNG GIU dong suoi.
///
/// KHONG tu chay khi mo Unity. Moi menu deu co Undo.
/// </summary>
public static class NuiThacBuilder
{
    private const string ROOT_NAME = "NUI_THAC_ROOT";
    private const string DIR_PREFAB = "Assets/_Game/Farm/Prefabs/VFX";
    private const string P_FALL = "Assets/maptitle/Map45Iso/Sheet_Waterfall45.png";
    private const string P_BOT = "Assets/maptitle/Map45Iso/Sheet_BotChanThac.png";

    private const float FPS_THAC = 12f;
    private const float FPS_BOT = 10f;      // lech nhip voi thac cho do lo chu ky
    private const int ORDER_THAC = 100;
    private const int ORDER_BOT = 101;      // bot ve DE LEN chan thac

    // Ten cac doi tuong thuoc phan NUI (se bi xoa). Dong suoi KHONG nam trong danh sach nay.
    private static readonly string[] PHAN_NUI =
    {
        "VachNui_Tang0", "VachNui_Tang1", "VachNui_Tang2", "VachNui_Tang3",
        "VachNui_Dinh", "Suoi_TrenDinh", "ThacNuoc"
    };

    // =====================================================================
    [MenuItem("Tools/Farm/Thac Nuoc/1. Tao prefab Thac + Bot", false, 10)]
    public static void TaoPrefab()
    {
        Sprite[] khungThac = LayKhung(P_FALL, "waterfall_frame_", 8);
        Sprite[] khungBot = LayKhung(P_BOT, "bot_frame_", 8);

        if (khungThac.Length == 0)
        {
            EditorUtility.DisplayDialog("Thieu art",
                "Khong doc duoc Sheet_Waterfall45.png.\n" +
                "Kiem tra file co trong Assets/maptitle/Map45Iso/ chua.", "OK");
            return;
        }

        if (!Directory.Exists(DIR_PREFAB)) { Directory.CreateDirectory(DIR_PREFAB); }

        string p1 = TaoMotPrefab("P_ThacNuoc", khungThac, FPS_THAC, ORDER_THAC);
        string p2 = khungBot.Length > 0
            ? TaoMotPrefab("P_BotChanThac", khungBot, FPS_BOT, ORDER_BOT)
            : null;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = "Da tao:\n\n  " + p1;
        if (p2 != null) { msg += "\n  " + p2; }
        else { msg += "\n\n(Chua tao duoc prefab bot: thieu Sheet_BotChanThac.png)"; }
        msg += "\n\nKeo tha vao scene, chinh Scale va vi tri tuy y.\n" +
               "Muon thac dai hon: keo cao Scale Y, hoac xep chong nhieu cai.";
        EditorUtility.DisplayDialog("Xong", msg, "OK");

        Object o1 = AssetDatabase.LoadAssetAtPath<Object>(p1);
        if (o1 != null) { EditorGUIUtility.PingObject(o1); Selection.activeObject = o1; }
    }

    // =====================================================================
    [MenuItem("Tools/Farm/Thac Nuoc/2. Xoa phan NUI (giu dong suoi)", false, 11)]
    public static void XoaNuiGiuSuoi()
    {
        Transform root = TimRoot();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Khong thay",
                "Khong tim thay " + ROOT_NAME + " trong scene. Co the da xoa roi.", "OK");
            return;
        }

        int daXoa = 0;
        foreach (string ten in PHAN_NUI)
        {
            Transform t = root.Find(ten);
            if (t != null) { Undo.DestroyObjectImmediate(t.gameObject); daXoa++; }
        }

        // Trong Suoi_ChanNui, go rieng cac o "ho chan thac" (o vach 43) — chinh la
        // mang 3x3 troi tren co ma Sep che xau. Giu lai cac o nuoc cua dong suoi.
        int daGoO = 0;
        Transform suoi = root.Find("Suoi_ChanNui");
        if (suoi != null)
        {
            Tilemap tm = suoi.GetComponent<Tilemap>();
            if (tm != null)
            {
                Undo.RecordObject(tm, "Go o ho chan thac");
                BoundsInt b = tm.cellBounds;
                foreach (Vector3Int c in b.allPositionsWithin)
                {
                    TileBase tb = tm.GetTile(c);
                    if (tb != null && tb.name.Contains("Cliff"))
                    {
                        tm.SetTile(c, null);
                        daGoO++;
                    }
                }
            }
        }

        // Neu root rong tron thi xoa luon cho sach cay Hierarchy.
        if (root != null && root.childCount == 0) { Undo.DestroyObjectImmediate(root.gameObject); }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ThacNuoc] Da xoa " + daXoa + " doi tuong nui va " + daGoO + " o ho da. Giu nguyen dong suoi. Nho Ctrl+S.");
    }

    // =====================================================================
    [MenuItem("Tools/Farm/Thac Nuoc/3. Xoa SACH tat ca (ke ca suoi)", false, 12)]
    public static void XoaSach()
    {
        Transform root = TimRoot();
        if (root == null) { Debug.Log("[ThacNuoc] Khong co gi de xoa."); return; }
        Undo.DestroyObjectImmediate(root.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ThacNuoc] Da xoa sach " + ROOT_NAME + ". Nho Ctrl+S.");
    }

    // =====================================================================
    private static Transform TimRoot()
    {
        foreach (Grid g in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
        {
            if (g.name != "Grid_Iso45") { continue; }
            Transform t = g.transform.Find(ROOT_NAME);
            if (t != null) { return t; }
        }
        return null;
    }

    private static string TaoMotPrefab(string ten, Sprite[] khung, float fps, int order)
    {
        string path = DIR_PREFAB + "/" + ten + ".prefab";
        GameObject go = new GameObject(ten);
        try
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = khung[0];
            sr.sortingLayerID = 0;
            sr.sortingOrder = order;

            SimpleSpriteAnimator an = go.AddComponent<SimpleSpriteAnimator>();
            an.sprites = khung;
            an.fps = fps;
            an.destroyOnEnd = false;   // BAT BUOC false — khong la chay 1 vong roi tu huy

            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        return path;
    }

    private static Sprite[] LayKhung(string pngPath, string tienTo, int soKhung)
    {
        List<Sprite> ds = new List<Sprite>();
        Object[] all = AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath);
        if (all == null) { return ds.ToArray(); }
        for (int i = 0; i < soKhung; i++)
        {
            string can = tienTo + i;
            foreach (Object o in all)
            {
                Sprite s = o as Sprite;
                if (s != null && s.name == can) { ds.Add(s); break; }
            }
        }
        return ds.ToArray();
    }
}
