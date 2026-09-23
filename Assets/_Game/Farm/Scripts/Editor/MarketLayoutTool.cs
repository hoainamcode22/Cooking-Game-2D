// ============================================================================
//  Tools > Cho > Can bo cuc Cho: khung ngang rong + tab to + thanh nut tren co nen (Edit mode)
// ----------------------------------------------------------------------------
//  Chay TRONG Unity (Unity dang mo scene => sua file tu ngoai se bi ghi de). Ctrl+S de luu.
//  So do (don vi board, tam Popup_Board):
//    Khung go     : 1920 x 900 (truoc 1880 x 840) — MarketBoardUI tu co vua man hinh khi mo.
//    Thanh tren   : TopBar_Panel (nen bo goc) tu y -84 .. -164, le trai/phai 26
//                   [Vang 260]  .....................  [Dem nguoc 340] 24 [LAM MOI 330]
//    Cot tab      : rong 172 (truoc 128), 8 tab cao 76 (truoc 68), icon 44, chu 18
//    Vung hang    : con lai ben phai, 6 cot o ~254 x 264
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MarketLayoutTool
{
    const float RONG = 1920f, CAO = 900f, LE = 26f, TREN = 176f, RAIL = 172f, KHE = 20f;
    const string TAB_PREFAB = "Assets/_Game/Prefab/ui/Market/MarketCategoryTab_Prefab.prefab";

    [MenuItem("Tools/Cho/Can bo cuc Cho: khung rong + tab to + thanh nut co nen", false, 10)]
    private static void Chay()
    {
        var ui = Object.FindFirstObjectByType<MarketBoardUI>(FindObjectsInactive.Include);
        if (ui == null) { EditorUtility.DisplayDialog("Cho", "Khong thay MarketBoardUI. Hay mo SCN_Farm.", "OK"); return; }
        var board = ui.transform as RectTransform;
        Undo.RegisterFullObjectHierarchyUndo(board.gameObject, "Can bo cuc Cho");

        board.sizeDelta = new Vector2(RONG, CAO);

        // Cot tab
        var rail = board.Find("Rail_Categories") as RectTransform;
        if (rail != null)
        {
            rail.anchorMin = new Vector2(0f, 0f); rail.anchorMax = new Vector2(0f, 1f); rail.pivot = new Vector2(0f, 0.5f);
            rail.sizeDelta = new Vector2(RAIL, -(TREN + LE));
            rail.anchoredPosition = new Vector2(LE, (LE - TREN) * 0.5f);
            var cc = rail.Find("Content_Categories");
            var vlg = cc != null ? cc.GetComponent<VerticalLayoutGroup>() : null;
            if (vlg != null)
            {
                Undo.RecordObject(vlg, "Tab cho");
                vlg.padding = new RectOffset(10, 10, 18, 18);
                vlg.spacing = 6f;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            }
        }

        // Vung hang
        float trai = LE + RAIL + KHE;
        var area = board.Find("Panel_ListingArea") as RectTransform;
        Sprite nenBoGoc = null;
        if (area != null)
        {
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.pivot = new Vector2(0.5f, 0.5f);
            area.sizeDelta = new Vector2(-(trai + LE), -(TREN + LE));
            area.anchoredPosition = new Vector2((trai - LE) * 0.5f, (LE - TREN) * 0.5f);
            var ai = area.GetComponent<Image>(); if (ai != null) nenBoGoc = ai.sprite;
        }
        var content = area != null ? area.Find("Scroll_Listings/Viewport/Content_Listings") : null;
        var gl = content != null ? content.GetComponent<GridLayoutGroup>() : null;
        if (gl != null)
        {
            Undo.RecordObject(gl, "Luoi cho");
            float trong = (RONG - trai - LE) - 16f - 44f;          // tru vien scroll + padding 22x2
            float o = Mathf.Floor((trong - 18f * 5f) / 6f);
            gl.padding = new RectOffset(22, 22, 14, 14);
            gl.cellSize = new Vector2(o, 264f);
            gl.spacing = new Vector2(18f, 16f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 6;
        }

        // Thanh nut tren: nen bo goc + 3 nut gian ra
        var bar = board.Find("TopBar_Panel") as RectTransform;
        if (bar == null)
        {
            var go = new GameObject("TopBar_Panel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "TopBar_Panel");
            go.transform.SetParent(board, false);
            bar = (RectTransform)go.transform;
        }
        bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = new Vector2(1f, 1f); bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -84f);
        bar.sizeDelta = new Vector2(-LE * 2f, 80f);
        var bi = bar.GetComponent<Image>();
        bi.sprite = nenBoGoc; bi.type = nenBoGoc != null ? Image.Type.Sliced : Image.Type.Simple;
        bi.color = nenBoGoc != null ? Color.white : new Color(0.98f, 0.93f, 0.84f, 1f); bi.raycastTarget = false;
        // nen nam DUOI 3 nut
        int minIdx = board.childCount;
        foreach (var ten in new[] { "Chip_Gold", "Chip_Timer", "Skin_Btn_Refresh", "Btn_Refresh" })
        { var t = board.Find(ten); if (t != null) minIdx = Mathf.Min(minIdx, t.GetSiblingIndex()); }
        if (minIdx < board.childCount) bar.SetSiblingIndex(Mathf.Max(0, minIdx));

        float y = -96f, h = 56f, le = LE + 16f;
        Dat(board, "Chip_Gold", new Vector2(0f, 1f), new Vector2(le, y), new Vector2(260f, h));
        var nut = board.Find("Skin_Btn_Refresh") != null ? "Skin_Btn_Refresh" : "Btn_Refresh";
        Dat(board, nut, new Vector2(1f, 1f), new Vector2(-le, y), new Vector2(330f, h));
        Dat(board, "Chip_Timer", new Vector2(1f, 1f), new Vector2(-(le + 330f + 24f), y), new Vector2(340f, h));

        // Tab prefab: cao 76, icon 44, chu 18
        string kqTab = SuaTabPrefab();

        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        Selection.activeTransform = board;
        Debug.Log("[Cho] Khung " + RONG + "x" + CAO + ", cot tab " + RAIL + ", thanh nut co nen, luoi 6 cot. " + kqTab + " Bam Ctrl+S de luu.");
    }

    private static void Dat(RectTransform cha, string ten, Vector2 neo, Vector2 pos, Vector2 size)
    {
        var t = cha.Find(ten) as RectTransform;
        if (t == null) return;
        Undo.RecordObject(t, "Can bo cuc Cho");
        t.anchorMin = t.anchorMax = neo;
        t.pivot = neo;
        t.anchoredPosition = pos;
        t.sizeDelta = size;
        t.localScale = Vector3.one;
    }

    private static string SuaTabPrefab()
    {
        var goc = PrefabUtility.LoadPrefabContents(TAB_PREFAB);
        if (goc == null) return "(khong mo duoc tab prefab)";
        try
        {
            var rt = (RectTransform)goc.transform;
            rt.sizeDelta = new Vector2(152f, 76f);
            var le = goc.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 76f; le.preferredHeight = 76f; }
            var ic = goc.transform.Find("Image_Accent") as RectTransform;
            if (ic != null) { ic.anchoredPosition = new Vector2(0f, -5f); ic.sizeDelta = new Vector2(44f, 44f); }
            var lb = goc.transform.Find("Text_Label") as RectTransform;
            if (lb != null)
            {
                lb.anchoredPosition = new Vector2(0f, 3f); lb.sizeDelta = new Vector2(-10f, 24f);
                var t = lb.GetComponent<TMP_Text>();
                if (t != null) { t.enableAutoSizing = true; t.fontSizeMin = 12f; t.fontSizeMax = 18f; t.fontSize = 18f; t.textWrappingMode = TextWrappingModes.NoWrap; }
            }
            PrefabUtility.SaveAsPrefabAsset(goc, TAB_PREFAB);
            return "Tab prefab 152x76.";
        }
        finally { PrefabUtility.UnloadPrefabContents(goc); }
    }
}
