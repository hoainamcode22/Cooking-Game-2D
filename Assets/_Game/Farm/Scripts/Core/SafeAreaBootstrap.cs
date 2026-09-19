using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tu dong boc moi Canvas Overlay vao mot lop "~SafeArea" luc chay game.
///
/// VI SAO LAM BANG CODE: du an dang bat androidRenderOutsideSafeArea = 1 (Unity ve
/// TRAN xuong duoi tai tho) ma khong co object nao gan SafeAreaFitter, nen HUD bi
/// tai tho va thanh cu chi che. Sua tay tung file .unity / .prefab thi phai dung vao
/// hang chuc file (dang bi chan quyen), nen o day chen lop boc luc runtime.
///
/// CACH LAM: voi moi Canvas co renderMode == ScreenSpaceOverlay, tao mot GameObject
/// con ten "~SafeArea" trai kin canvas, mang SafeAreaFitter, roi chuyen TOAN BO con
/// truc tiep dang co cua Canvas vao trong no. Vi lop boc trai kin canvas va viec
/// chuyen cha dung worldPositionStays = false, moi con giu nguyen localScale,
/// anchoredPosition va thu tu sibling.
///
/// IDEMPOTENT: chay lai bao nhieu lan cung chi co DUNG MOT lop "~SafeArea" moi canvas,
/// khong long nhieu lop.
/// </summary>
public static class SafeAreaBootstrap
{
    /// <summary>Ten cua object boc. Dau "~" de no luon nam cuoi khi sap xep theo ten.</summary>
    public const string TEN_LOP_BOC = "~SafeArea";

    /// <summary>
    /// Canvas co ten nam trong danh sach nay thi KHONG boc: man chuyen canh, man fade
    /// toan man hinh... vo n phai phu kin ca tai tho, boc vao se ho ra vien den.
    /// Co the them tu code khac truoc khi scene nap: SafeAreaBootstrap.CanvasBoQua.Add("Ten");
    /// </summary>
    public static readonly HashSet<string> CanvasBoQua = new HashSet<string>
    {
        "TransitionCanvas",
        "Canvas_Transition",
        "Canvas_Fade",
        "FadeCanvas",
        "Canvas_Loading",
        "LoadingCanvas",
        "Canvas_Splash",
    };

    /// <summary>Bat/tat toan bo co che (de debug nhanh tren may that).</summary>
    public static bool BatCoChe = false; // [2026-09-18] TAM TAT de kiem chung loi "popup khong click duoc". Bat lai = true sau khi xac nhan khong lien quan.

    /// <summary>In log moi lan boc mot canvas.</summary>
    public static bool GhiLog = false;

    private static bool _daDangKy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiDong()
    {
        if (!_daDangKy)
        {
            SceneManager.sceneLoaded += KhiSceneNap;
            _daDangKy = true;
        }

        // Scene dau tien da nap xong truoc khi su kien duoc dang ky, nen quet ngay mot lan.
        QuetVaBoc();
    }

    private static void KhiSceneNap(Scene scene, LoadSceneMode mode)
    {
        QuetVaBoc();
    }

    /// <summary>Quet moi Canvas Overlay dang co va boc chung. Goi lai luc nao cung an toan.</summary>
    public static void QuetVaBoc()
    {
        if (!BatCoChe) return;

        // Unity 6: FindObjectsByType (FindObjectsOfType da bi bo).
        Canvas[] ds = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < ds.Length; i++)
            BocMotCanvas(ds[i]);
    }

    /// <summary>Boc mot canvas. Tra ve lop boc, hoac null neu canvas nay khong can boc.</summary>
    public static RectTransform BocMotCanvas(Canvas canvas)
    {
        if (canvas == null) return null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) return null;
        if (CanvasBoQua.Contains(canvas.name)) return null;

        RectTransform goc = canvas.transform as RectTransform;
        if (goc == null) return null;

        // Canvas long trong Canvas khac: canvas cha da lo phan vung an toan roi.
        if (goc.parent != null && goc.parent.GetComponentInParent<Canvas>() != null) return null;

        RectTransform lopBoc = LayHoacTaoLopBoc(goc);
        if (lopBoc == null) return null;

        ChuyenConVaoLopBoc(goc, lopBoc);
        return lopBoc;
    }

    /// <summary>
    /// Tim lop boc dang co (idempotent) hoac tao moi. Lop boc luon trai kin canvas:
    /// anchor 0-1, offset 0, pivot giua, scale 1 - nho vay con ben trong khong xe dich.
    /// </summary>
    private static RectTransform LayHoacTaoLopBoc(RectTransform goc)
    {
        // Da co lop boc thi dung lai, KHONG tao them lop moi.
        for (int i = 0; i < goc.childCount; i++)
        {
            Transform con = goc.GetChild(i);
            if (con == null) continue;
            if (con.name != TEN_LOP_BOC) continue;

            RectTransform rt = con as RectTransform;
            if (rt == null) continue;
            if (rt.GetComponent<SafeAreaFitter>() == null) rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        GameObject go = new GameObject(TEN_LOP_BOC, typeof(RectTransform));
        RectTransform lop = go.GetComponent<RectTransform>();
        lop.SetParent(goc, false);
        lop.anchorMin        = Vector2.zero;
        lop.anchorMax        = Vector2.one;
        lop.pivot            = new Vector2(0.5f, 0.5f);
        lop.offsetMin        = Vector2.zero;
        lop.offsetMax        = Vector2.zero;
        lop.localScale       = Vector3.one;
        lop.localRotation    = Quaternion.identity;
        lop.anchoredPosition = Vector2.zero;
        lop.SetAsFirstSibling();

        go.AddComponent<SafeAreaFitter>();

        if (GhiLog) Debug.Log("[SafeAreaBootstrap] Da tao lop boc cho canvas '" + goc.name + "'.");
        return lop;
    }

    /// <summary>
    /// Chuyen moi con truc tiep con lai cua canvas vao lop boc, GIU DUNG thu tu sibling.
    /// worldPositionStays = false nen localScale / anchoredPosition / localRotation
    /// duoc giu nguyen y het gia tri trong scene.
    /// </summary>
    private static void ChuyenConVaoLopBoc(RectTransform goc, RectTransform lopBoc)
    {
        // Gom truoc roi moi chuyen: doi childCount ngay trong vong lap la bo sot con.
        List<Transform> canChuyen = new List<Transform>(goc.childCount);
        for (int i = 0; i < goc.childCount; i++)
        {
            Transform con = goc.GetChild(i);
            if (con == null) continue;
            if (con == lopBoc) continue;
            canChuyen.Add(con);
        }

        if (canChuyen.Count == 0) return;

        for (int i = 0; i < canChuyen.Count; i++)
        {
            Transform con = canChuyen[i];

            // Chup lai truoc khi doi cha de con nao khong phai RectTransform cung an toan.
            Vector3    scaleCu = con.localScale;
            Quaternion xoayCu  = con.localRotation;
            RectTransform rt   = con as RectTransform;
            Vector2 viTriCu    = rt != null ? rt.anchoredPosition : Vector2.zero;

            con.SetParent(lopBoc, false);
            con.SetSiblingIndex(lopBoc.childCount - 1);   // noi tiep vao cuoi => giu dung thu tu cu

            con.localScale    = scaleCu;
            con.localRotation = xoayCu;
            if (rt != null) rt.anchoredPosition = viTriCu;
        }

        if (GhiLog)
            Debug.Log("[SafeAreaBootstrap] Canvas '" + goc.name + "': da chuyen " +
                      canChuyen.Count + " con vao " + TEN_LOP_BOC + ".");
    }
}
