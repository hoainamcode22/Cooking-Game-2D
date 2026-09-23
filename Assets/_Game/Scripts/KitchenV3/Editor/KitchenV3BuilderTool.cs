// ============================================================================
//  KITCHEN V3 BUILDER  —  Tools > Kitchen V3 > ...
// ----------------------------------------------------------------------------
//  Dung Kitchen_UI_v3 thanh HIERARCHY THAT (GameObject that, sprite gan san,
//  Sep keo tha tu do, Ctrl+S la vinh vien). KHONG dung UI bang code luc chay.
//
//  KIEN TRUC: V3 = "bo nao" V2 + skin V3 + layout V3.
//    - Gan lai chinh KitchenSceneV2UI (khoaLayout = true) len canvas V3. Ten node dung
//      chuan BindExistingHierarchy() nen toan bo logic nau / khay / danh sach mon /
//      don khach chay ngay, khong viet lai gameplay.
//    - Vi code runtime luon to lai the/dong mon bang skin.*, V3 co ban skin RIENG tro
//      vao art V3 -> art cua Sep khong bi to de lai luc Play.
//    - Tat PopupSkinUnifier cho V3 (boQuaDongBoSkinShop) de nut/khung khong bi doi ve
//      bo cua Shop.
//    - Them 3 component nho: UISpriteFrameAnimator (dong ho, noi), KitchenV3CookingFX
//      (mon bay noi->dia, thanh thoi gian), KitchenV3PlateDrag (keo dia vao kho).
//
//  AN TOAN: luu + sao luu file scene truoc; Undo day du; V2 chi bi TAT, khong xoa.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using KitchenUIv2;
using KitchenUIv3;

public static class KitchenV3BuilderTool
{
    private const string ROOT_V2 = "Kitchen_UI_v2";
    private const string ROOT_V3 = "Kitchen_UI_v3";
    private const string CUT  = "Assets/Art/UI/KitchenCozyV3/Cut/";
    private const string PROC = "Assets/Art/UI/KitchenCozyV3/Processed/";

    private static readonly Color Cream = new Color(0.99f, 0.96f, 0.88f);
    private static readonly Color Brown = new Color(0.36f, 0.20f, 0.09f);
    private static readonly Color BrownSoft = new Color(0.55f, 0.40f, 0.25f);
    private static readonly Color[] FlavorDot = {
        new Color(0.93f, 0.45f, 0.65f), new Color(0.88f, 0.28f, 0.22f), new Color(0.45f, 0.75f, 0.30f),
        new Color(0.35f, 0.60f, 0.88f), new Color(0.65f, 0.45f, 0.28f) };

    // ═══════════════════════════ MENU ═══════════════════════════════════════

    [MenuItem("Tools/Kitchen V3/1. Cau hinh import sprite V3", false, 1)]
    public static void CauHinhImport()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CUT.TrimEnd('/') }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (CauHinhMotTexture(p)) n++;
        }
        if (CauHinhMotTexture(PROC + "kitchen_empty_room_background.png")) n++;
        if (CauHinhMotTexture(PROC + "kitchen_background_main.png")) n++;
        AssetDatabase.Refresh();
        Debug.Log($"[KitchenV3] Da cau hinh import {n} texture (Sprite, no mipmap, 9-slice cho khung).");
    }

    [MenuItem("Tools/Kitchen V3/2. DUNG Kitchen_UI_v3 (an V2, khong xoa)", false, 2)]
    public static void Dung()
    {
        if (Application.isPlaying) { Bao("Thoat Play roi bam lai."); return; }

        var v2Ui = TimV2();
        if (v2Ui == null) { Bao("Khong thay Kitchen_UI_v2 (co KitchenSceneV2UI) trong scene dang mo.\nMo SampleScene roi bam lai."); return; }
        var scene = v2Ui.gameObject.scene;

        var v3Cu = GameObject.Find(ROOT_V3) ?? TimTheoTen(scene, ROOT_V3);
        if (v3Cu != null)
        {
            int c = EditorUtility.DisplayDialogComplex("Kitchen V3",
                "Da co Kitchen_UI_v3 trong scene. Dung lai se XOA ban cu (ke ca chinh sua tay).",
                "Xoa ban cu va dung lai", "Huy", "Giu ban cu, chi bat no len");
            if (c == 1) return;
            if (c == 2) { ChuyenSang(true); return; }
        }

        if (!LuuVaSaoLuu(scene)) return;
        CauHinhImport();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Dung Kitchen_UI_v3");
        int nhom = Undo.GetCurrentGroup();

        if (v3Cu != null) Undo.DestroyObjectImmediate(v3Cu);

        GameObject root = null;
        try
        {
            root = DungHierarchy(v2Ui);
            Undo.RegisterCreatedObjectUndo(root, "Kitchen_UI_v3");
            Undo.RecordObject(v2Ui.gameObject, "An V2");
            v2Ui.gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Bao("Dung V3 THAT BAI — xem Console.\nScene chua luu; Ctrl+Z hoac File > Open Scene > Don't Save.\n\n" + e.Message);
            return;
        }
        Undo.CollapseUndoOperations(nhom);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Bao("Da dung Kitchen_UI_v3 va TAT Kitchen_UI_v2 (khong xoa).\n\n" +
            "BAY GIO Ctrl+S.\n" +
            "Keo tha tu do trong Hierarchy; Play se giu nguyen (khoaLayout = true).\n" +
            "Muon quay ve V2: Tools > Kitchen V3 > 3.");
    }

    [MenuItem("Tools/Kitchen V3/3. Chuyen: bat V2, an V3", false, 20)]
    public static void BatV2() => ChuyenSang(false);

    [MenuItem("Tools/Kitchen V3/4. Chuyen: bat V3, an V2", false, 21)]
    public static void BatV3() => ChuyenSang(true);

    [MenuItem("Tools/Kitchen V3/5. XOA Kitchen_UI_v3 (giu V2)", false, 40)]
    public static void XoaV3()
    {
        var v3 = GameObject.Find(ROOT_V3);
        if (v3 == null) { Bao("Khong co Kitchen_UI_v3 trong scene."); return; }
        if (!EditorUtility.DisplayDialog("Kitchen V3", "Xoa Kitchen_UI_v3 va bat lai Kitchen_UI_v2?\n(Co Undo. Chua Ctrl+S thi chua mat gi.)", "Xoa", "Huy")) return;
        if (!LuuVaSaoLuu(v3.scene)) return;
        var v2 = TimV2(true);
        Undo.DestroyObjectImmediate(v3);
        if (v2 != null) { Undo.RecordObject(v2.gameObject, "Bat V2"); v2.gameObject.SetActive(true); }
        EditorSceneManager.MarkSceneDirty(SceneManager_Active());
    }

    // ═══════════════════════════ CORE ═══════════════════════════════════════

    // ------------------------------------------------------------------------
    //  TOA DO: do truc tiep tren anh mau 1731x907 (pixel), quy ve canvas 1600x900
    //  (x *0.924, y *0.992). Ghi kem toa do mau de doi chieu: [mau x0-x1, y0-y1].
    // ------------------------------------------------------------------------
    private static GameObject DungHierarchy(KitchenSceneV2UI v2Ui)
    {
        var v2Go = v2Ui.gameObject;
        var root = new GameObject(ROOT_V3, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(v2Go.transform.parent, false);
        root.transform.SetSiblingIndex(v2Go.transform.GetSiblingIndex() + 1);

        var cv2 = v2Go.GetComponent<Canvas>(); var cv3 = root.GetComponent<Canvas>();
        cv3.renderMode = RenderMode.ScreenSpaceOverlay;
        cv3.sortingOrder = cv2 != null ? cv2.sortingOrder : 5;
        cv3.additionalShaderChannels = cv2 != null ? cv2.additionalShaderChannels : AdditionalCanvasShaderChannels.TexCoord1;
        var sc3 = root.GetComponent<CanvasScaler>();
        sc3.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc3.referenceResolution = new Vector2(1600f, 900f);
        sc3.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc3.matchWidthOrHeight = 0.5f;

        var R = (RectTransform)root.transform;
        var v2Skin = DocSkin(v2Ui);

        // ── Nen ──
        var bg = Panel(R, "BG_Room", Sp(PROC + "kitchen_empty_room_background.png"), Image.Type.Simple, false);
        Stretch(bg, Vector2.zero, Vector2.zero); bg.GetComponent<Image>().raycastTarget = false;

        // ── BACK TO FARM  [mau 45-275, 25-95] ──
        var back = Nut(R, "Btn_BackFarm", Sp(CUT + "btn_back_farm_plank.png"), Image.Type.Simple, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -20), new Vector2(200, 88));
        var backLbl = Chu(back.transform, "Txt_Label", "BACK TO FARM", 17, Cream, true, TextAlignmentOptions.Center);
        StretchTiLe(backLbl.rectTransform, new Vector2(0.36f, 0.10f), new Vector2(0.98f, 0.90f));

        // ── COOKING: day dai co non  [mau strap 490-1290, 15-105; chu 810-1010] ──
        var title = Panel(R, "Title_Cooking", Sp(CUT + "banner_strap_cooking_hat.png"), Image.Type.Simple, true);
        Dat(title, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(740, 122)); title.GetComponent<Image>().raycastTarget = false;
        var titleTxt = Chu(title.transform, "Txt_Title", "COOKING", 40, Cream, true, TextAlignmentOptions.Center);
        StretchTiLe(titleTxt.rectTransform, new Vector2(0.34f, 0.18f), new Vector2(0.82f, 0.82f));

        // ── TODAY'S SPECIAL  [mau 1300-1700, 95-340] — bang den ngang co khung go ──
        var chalk = Panel(R, "Chalkboard", Sp(CUT + "frame_chalkboard.png"), Image.Type.Simple, true);   // bang treo day nhu mau
        Dat(chalk, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -60), new Vector2(340, 346)); chalk.GetComponent<Image>().raycastTarget = false;
        var chalkTitle = Chu(chalk.transform, "Txt_Title", "TODAY'S SPECIAL", 22, Cream, true, TextAlignmentOptions.Center);
        StretchTiLe(chalkTitle.rectTransform, new Vector2(0.16f, 0.60f), new Vector2(0.84f, 0.72f));   // vung den cua bang treo: y 0.12-0.75
        chalkTitle.gameObject.AddComponent<TMPCurvedText>().doCong = 14f;   // chu cong len theo bang
        var avBoard = Anh(chalk.transform, "Img_Avatar_Board", null, true); Dat(avBoard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(92, 92));
        var chalkTxt = Chu(chalk.transform, "Txt_Chalk", "", 15, Cream, false, TextAlignmentOptions.Center);
        StretchTiLe(chalkTxt.rectTransform, new Vector2(0.16f, 0.13f), new Vector2(0.84f, 0.28f));

        // ── RECIPE BOOK  [mau khung 20-480, 120-830] ──
        var board = Panel(R, "Recipe_Board", Sp(CUT + "frame_recipe_book.png"), Image.Type.Sliced, false);
        Dat(board, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -119), new Vector2(440, 704));
        {
            // pill tieu de  [mau 90-410, 135-205]
            var rib = Panel(board.transform, "Ribbon", Sp(CUT + "banner_recipe_curved_badge.png"), Image.Type.Simple, true);   // banner cong cua Sep
            Dat(rib, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 34), new Vector2(330, 176)); rib.GetComponent<Image>().raycastTarget = false;
            var hdr = Chu(rib.transform, "Txt_Header", "RECIPE BOOK", 26, Brown, true, TextAlignmentOptions.Center);
            StretchTiLe(hdr.rectTransform, new Vector2(0.30f, 0.32f), new Vector2(0.92f, 0.74f));
            hdr.gameObject.AddComponent<TMPCurvedText>().doCong = -9f;   // uon theo mep giay cong xuong

            // Board_List: vung giay trong khung  [mau 62-450, 228-780] -> nen kem
            var list = Panel(board.transform, "Board_List", Sp(CUT + "card_rounded_white.png"), Image.Type.Sliced, false);
            StretchTiLe(list, new Vector2(0.09f, 0.06f), new Vector2(0.94f, 0.82f)); list.GetComponent<Image>().raycastTarget = false;
            string[] tabLbl = { "All", "Easy", "Medium", "Hard" };
            for (int i = 0; i < 4; i++)   // [mau tab 40 cao, 4 tab deu nhau]
            {
                var tab = Nut(list, "Tab_" + i, SkinKit.BoGoc(21f), Image.Type.Sliced, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8 + i * 92, -16), new Vector2(82, 42));   // 4 tab, khe 10px, khong de nhau
                var tabSh = tab.gameObject.AddComponent<Shadow>(); tabSh.effectColor = new Color(0.3f, 0.18f, 0.08f, 0.35f); tabSh.effectDistance = new Vector2(0, -3);
                tab.GetComponent<Image>().color = i == 0 ? new Color(0.93f, 0.55f, 0.20f) : new Color(0.90f, 0.82f, 0.68f);
                var l = Chu(tab.transform, "Txt_Label", tabLbl[i], 17, i == 0 ? Color.white : Brown, true, TextAlignmentOptions.Center); Stretch(l.rectTransform, Vector2.zero, Vector2.zero);
            }
            var scroll = new GameObject("Dish_Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scroll.transform.SetParent(list, false);
            var srt = (RectTransform)scroll.transform; Stretch(srt, new Vector2(8, 10), new Vector2(-8, -70));
            scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scroll.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vl = content.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.padding = new RectOffset(4, 4, 4, 4);
            vl.childControlWidth = true; vl.childForceExpandWidth = true; vl.childControlHeight = false; vl.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = scroll.GetComponent<ScrollRect>(); sr.content = crt; sr.viewport = srt; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 24;

            // Board_Detail: to giay thong tin mon (an mac dinh)
            var detail = Panel(board.transform, "Board_Detail", Sp(CUT + "paper_sheet_portrait.png"), Image.Type.Simple, false);
            StretchTiLe(detail, new Vector2(0.09f, 0.06f), new Vector2(0.94f, 0.82f));
            var D = detail.transform;
            var other = Nut(D, "Btn_OtherDish", SkinKit.BoGoc(14f), Image.Type.Sliced, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(110, 34));
            other.GetComponent<Image>().color = new Color(0.93f, 0.55f, 0.20f);
            var ol = Chu(other.transform, "Txt_Label", "◀ Back", 15, Color.white, true, TextAlignmentOptions.Center); Stretch(ol.rectTransform, Vector2.zero, Vector2.zero);
            // Nhip doc theo V2: FlavorRow.SetY() luon ep Flavor_Row_i ve y = -230 - 34*i (khong bi KhoaLayout chan),
            // nen cac muc phia tren phai nam gon truoc -226 nhu bo cuc V2.
            var imgDish = Anh(D, "Img_Dish", null, true); Dat(imgDish, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(46, -50), new Vector2(60, 60));
            ChuDat(D, "Txt_DishName", "Dish name", 21, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(86, -46), new Vector2(230, 28));
            ChuDat(D, "Txt_DishMeta", "Easy · Lv 1", 14, BrownSoft, false, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(86, -78), new Vector2(230, 20));
            ChuDat(D, "Txt_NeedTitle", "Ingredients needed", 14, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -110), new Vector2(300, 20));
            var chips = new GameObject("Need_Chips", typeof(RectTransform), typeof(GridLayoutGroup)); chips.transform.SetParent(D, false);
            Dat((RectTransform)chips.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -132), new Vector2(300, 66));
            var gl = chips.GetComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(66, 64); gl.spacing = new Vector2(6, 6);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4; gl.childAlignment = TextAnchor.UpperLeft;
            ChuDat(D, "Txt_TasteTitle", "Flavor", 14, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -204), new Vector2(300, 20));
            for (int i = 0; i < 5; i++)
            {
                var row = Rong(D, "Flavor_Row_" + i); Dat(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -230 - i * 34), new Vector2(300, 24));
                var dot = Anh(row, "Dot", null, false); Dat(dot, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(12, 12)); dot.GetComponent<Image>().color = FlavorDot[i];
                ChuDat(row, "Label", "Taste", 14, Brown, false, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(60, 22));
                var track = Panel(row, "Track", v2Skin.tasteTrack, Image.Type.Sliced, false); Dat(track, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(86, 0), new Vector2(150, 14)); track.GetComponent<Image>().raycastTarget = false;
                var fill = Anh(track.transform, "Fill", v2Skin.tasteFill, false); Stretch(fill, new Vector2(2, 2), new Vector2(-2, -2));
                var fi = fill.GetComponent<Image>(); fi.type = Image.Type.Filled; fi.fillMethod = Image.FillMethod.Horizontal; fi.fillOrigin = 0; fi.fillAmount = 0.5f; fi.color = new Color(0.95f, 0.65f, 0.2f);
                ChuDat(row, "Value", "0/0", 13, Brown, false, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(244, 0), new Vector2(50, 22));
            }
            ChuDat(D, "Txt_Rewards", "", 15, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 46), new Vector2(300, 22));
            ChuDat(D, "Txt_Projection", "", 17, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 16), new Vector2(300, 26));
            detail.gameObject.SetActive(false);
        }

        // ── DON KHACH  [mau 515-1280, 130-270] ──
        RectTransform avKhach = null;   // Img_Avatar cua the don, dung cho mirror len bang den
        // ── DON KHACH  [mau 515-1280, 130-270]: khung mon trai, ten tren, hang chip nguyen lieu + dong ho duoi ──
        var banner = Panel(R, "Order_Banner", Sp(CUT + "plank_paper_order_card.png"), Image.Type.Sliced, false);
        Dat(banner, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(30, -128), new Vector2(707, 150)); banner.GetComponent<Image>().raycastTarget = false;
        {
            var card = new GameObject("Order_Card", typeof(RectTransform), typeof(Image), typeof(Button)); card.transform.SetParent(banner.transform, false);
            var crt = (RectTransform)card.transform; Stretch(crt, new Vector2(34, 24), new Vector2(-34, -24));
            card.GetComponent<Image>().color = new Color(1, 1, 1, 0f); card.GetComponent<Button>().transition = Selectable.Transition.None;

            // khung mon (o kem bo goc) + icon mon  [mau 570-680]
            var dishFrame = Panel(crt, "Dish_Frame", Sp(CUT + "card_rounded_white.png"), Image.Type.Sliced, false);
            Dat(dishFrame, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(98, 98)); dishFrame.GetComponent<Image>().raycastTarget = false;
            var od = Anh(crt, "Img_Dish", null, true); Dat(od, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(49, 0), new Vector2(84, 84));
            // avatar khach: goc phai tren the (mau khong co; Sep yeu cau) — mirror len bang den
            var av = Anh(crt, "Img_Avatar", null, true); avKhach = av; Dat(av, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(64, 64));
            var tenKhach = ChuDat(crt, "Txt_Name", "No customer waiting", 22, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, 0), new Vector2(380, 30));
            ChuDat(crt, "Txt_Rewards", "", 13, new Color(0.72f, 0.52f, 0.08f), true, TextAlignmentOptions.Right, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-72, -4), new Vector2(150, 20));
            // hang chip nguyen lieu (KitchenV3OrderChips tu sinh/tai su dung chip)
            var needRow = Rong(crt, "Need_Row"); Dat(needRow, new Vector2(0, 0), new Vector2(0, 0), new Vector2(116, 0), new Vector2(480, 46));
            var chips = needRow.gameObject.AddComponent<KitchenV3OrderChips>(); chips.nenChip = Sp(CUT + "card_rounded_white.png"); chips.iconDongHo = Sp(CUT + "anim_clock_01.png");
            // Chip_0..4 cua V2 (cham vi): tao san nhung TAT — V2 tim thay nen khong tu sinh, ma khong hien
            for (int i = 0; i < 5; i++)
            {
                var chip = Panel(crt, "Chip_" + i, null, Image.Type.Sliced, false); Dat(chip, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-i * 30, 0), new Vector2(28, 20));
                var dot = Anh(chip, "Dot", null, false); Dat(dot, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(10, 10)); dot.GetComponent<Image>().color = FlavorDot[i];
                ChuDat(chip, "Txt_Val", "0", 11, Brown, true, TextAlignmentOptions.Left, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(14, 16));
                chip.gameObject.SetActive(false);
            }
            var pop = od.gameObject.AddComponent<KitchenV3OrderPop>(); pop.nhayTheo = new[] { av, tenKhach.rectTransform, needRow };   // nau xong -> khach ke nhay vao
        }

        // ── Decor (mau khong co toi/hanh treo: nen da co day den) -> tao san nhung TAT ──
        foreach (var (n, sp, pos, size) in new (string, Sprite, Vector2, Vector2)[] {
            ("Deco_Garlic_R", v2Skin.decorGarlic, new Vector2(330, -4),  new Vector2(48, 82)),
            ("Deco_Onion_R",  v2Skin.decorOnion,  new Vector2(392, -6),  new Vector2(50, 86)),
            ("Deco_Herbs_R",  v2Skin.decorHerbs,  new Vector2(456, -4),  new Vector2(52, 78)),
            ("Deco_Herbs_L",  v2Skin.decorHerbs,  new Vector2(-540, -4), new Vector2(52, 78)),
            ("Deco_Garlic_L", v2Skin.decorGarlic, new Vector2(-478, -6), new Vector2(48, 82)),
            ("Deco_Lights_L", v2Skin.decorLights, new Vector2(-360, 0),  new Vector2(280, 40)),
            ("Deco_Lights_R", v2Skin.decorLights, new Vector2(520, 0),   new Vector2(300, 40)) })
        {
            var d = Anh(R, n, sp, true); Dat(d, new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, size); d.gameObject.SetActive(false);
        }
        // ke dung cu  [mau 1050-1270, 280-410]
        var rack = Panel(R, "Rack_Utensils", Sp(CUT + "rack_utensils_shelf_plant.png"), Image.Type.Simple, true);
        Dat(rack, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(273, 108), new Vector2(205, 128)); rack.GetComponent<Image>().raycastTarget = false;
        // ban  [mau mat ban 555-1240, 430-590; chan ban khuat sau khay]
        var table = Panel(R, "Table_Cook", Sp(CUT + "table_counter_flat.png"), Image.Type.Simple, true);   // ban phang giong mau (Sep chon)
        Dat(table, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(30, 42), new Vector2(660, 397)); table.GetComponent<Image>().raycastTarget = false;
        // meo dau bep  [mau 505-630, 300-440]
        var catFrames = Frames(CUT + "cat_chef_frame_{0:00}.png", 12);
        // Ten "Cat_Chef_Idle" (khong phai "Cat_Chef") de V2 KHONG gan KitchenCatWalker vao -> meo dung yen,
        // chi nhay nhot bang 12 frame. skin.catChefWalk de rong nen V2 cung khong tu tao Cat_Chef.
        var cat = Panel(R, "Cat_Chef_Idle", catFrames[0], Image.Type.Simple, true);
        Dat(cat, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-275, 83), new Vector2(120, 140)); cat.GetComponent<Image>().raycastTarget = false;
        var catAnim = cat.gameObject.AddComponent<UISpriteFrameAnimator>(); catAnim.frames = catFrames; catAnim.fps = 7; catAnim.lap = true; catAnim.chayKhiBat = true;

        // ── NOI (8 frame)  [mau chao 650-870, 400-520 -> noi dat tren mat ban] ──
        var potFrames = Frames(CUT + "anim_pot_{0:00}.png", 8);
        var pot = Panel(R, "Pot_Anim", potFrames[0], Image.Type.Simple, true);
        Dat(pot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-97, 14), new Vector2(160, 190)); pot.GetComponent<Image>().raycastTarget = false;
        var potAnim = pot.gameObject.AddComponent<UISpriteFrameAnimator>(); potAnim.frames = potFrames; potAnim.fps = 8; potAnim.frameNghi = 0;

        // ── DIA  [mau 915-1105, 435-530] ──
        var plate = Nut(R, "Plating_Table", v2Skin.platingTable, Image.Type.Simple, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(134, -22), new Vector2(180, 100));
        {
            var pill = Panel(plate.transform, "Label_Pill", SkinKit.BoGoc(12f), Image.Type.Sliced, false);
            Dat(pill, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(120, 26)); pill.GetComponent<Image>().raycastTarget = false;
            pill.GetComponent<Image>().color = new Color(0.98f, 0.94f, 0.86f, 0.9f);
            var pl = Chu(pill.transform, "Txt_Label", "Plating", 13, Brown, true, TextAlignmentOptions.Center); Stretch(pl.rectTransform, Vector2.zero, Vector2.zero);
            plate.gameObject.AddComponent<KitchenV3PlateDrag>();
        }

        // ── DONG HO + THANH THOI GIAN  [mau dong ho 600-650, 525-575; thanh 655-1100, 538-562] ──
        var clockFrames = Frames(CUT + "anim_clock_{0:00}.png", 8);
        var clock = Panel(R, "Clock_Anim", clockFrames[0], Image.Type.Simple, true);
        Dat(clock, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-222, -100), new Vector2(54, 62)); clock.GetComponent<Image>().raycastTarget = false;
        var clockAnim = clock.gameObject.AddComponent<UISpriteFrameAnimator>(); clockAnim.frames = clockFrames; clockAnim.fps = 8; clockAnim.frameNghi = 0;
        var bar = Panel(R, "Cook_Timer_Bar", SkinKit.BoGoc(12f), Image.Type.Sliced, false);
        Dat(bar, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(-188, -100), new Vector2(406, 28));
        bar.GetComponent<Image>().color = new Color(0.20f, 0.14f, 0.10f); bar.GetComponent<Image>().raycastTarget = false;
        var barFill = Anh(bar.transform, "Fill", SkinKit.BoGoc(9f), false); Stretch(barFill, new Vector2(3, 3), new Vector2(-3, -3));
        var bf = barFill.GetComponent<Image>(); bf.type = Image.Type.Filled; bf.fillMethod = Image.FillMethod.Horizontal; bf.fillOrigin = 0; bf.fillAmount = 0; bf.color = new Color(0.55f, 0.80f, 0.30f);
        var barTxt = Chu(bar.transform, "Txt_Time", "", 14, Cream, true, TextAlignmentOptions.Right); Stretch(barTxt.rectTransform, new Vector2(8, 0), new Vector2(-10, 0));

        // ── LO  [mau 1385-1700, 345-650] ──
        var oven = Panel(R, "Oven", Sp(CUT + "oven_stone.png"), Image.Type.Simple, true);
        Dat(oven, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, -43), new Vector2(291, 302)); oven.GetComponent<Image>().raycastTarget = false;
        {
            var mouth = Rong(oven.transform, "Oven_Mouth"); Dat(mouth, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(120, 90));
            var glow = Anh(oven.transform, "Oven_Glow", v2Skin.ovenGlow, true); Dat(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(150, 110)); glow.GetComponent<Image>().enabled = false;
            var fire = Anh(oven.transform, "Oven_Fire", v2Skin.ovenFire != null && v2Skin.ovenFire.Length > 0 ? v2Skin.ovenFire[0] : null, true);
            Dat(fire, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(100, 86)); fire.GetComponent<Image>().enabled = false;
            // "OVEN NOT LIT"  [mau 1450-1630, 585-625]
            var state = Panel(oven.transform, "Oven_StateBar", SkinKit.BoGoc(12f), Image.Type.Sliced, false);
            Dat(state, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(166, 40));
            state.GetComponent<Image>().color = new Color(0.20f, 0.14f, 0.10f); state.GetComponent<Image>().raycastTarget = false;
            var sf = Anh(state.transform, "Fill", SkinKit.BoGoc(8f), false); Stretch(sf, new Vector2(4, 4), new Vector2(-4, -4));
            var sfi = sf.GetComponent<Image>(); sfi.type = Image.Type.Filled; sfi.fillMethod = Image.FillMethod.Horizontal; sfi.fillOrigin = 0; sfi.fillAmount = 0; sfi.color = new Color(0.95f, 0.65f, 0.2f, 0.35f);
            var st = Chu(state.transform, "Txt_State", "OVEN NOT LIT", 13, Cream, true, TextAlignmentOptions.Center); Stretch(st.rectTransform, new Vector2(6, 0), new Vector2(-6, 0));
        }

        // ── HOP KHO  [mau 1270-1420, 420-580] ──
        var wh = Panel(R, "Warehouse_Box", Sp(CUT + "storage_box_logs.png"), Image.Type.Simple, true);
        Dat(wh, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-312, -50), new Vector2(140, 190));
        ChuDat(wh.transform, "Txt_Wh", "TO STORAGE", 13, Cream, true, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(136, 22));
        ChuDat(wh.transform, "Txt_Sent", "0 dishes sent", 12, Cream, false, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(136, 20));

        ChuDat(R, "Txt_PrepToast", "", 16, Cream, true, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30, 190), new Vector2(520, 26));

        // ── KHAY  [mau khung 510-1240, 650-830; tab 520-730, 615-660] ──
        // Khay = 2 lop bo goc: go ngoai + kem trong (dung nhu mau, khong phu thuoc 9-slice cua anh)
        var tray = Panel(R, "Tray", SkinKit.BoGoc(26f), Image.Type.Sliced, false);
        Dat(tray, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(9, 26), new Vector2(690, 240));
        tray.GetComponent<Image>().color = new Color(0.62f, 0.40f, 0.20f);
        var trayIn = Panel(tray, "Tray_Inner", SkinKit.BoGoc(20f), Image.Type.Sliced, false); Stretch(trayIn, new Vector2(14, 14), new Vector2(-14, -14));
        trayIn.GetComponent<Image>().color = new Color(0.99f, 0.95f, 0.86f); trayIn.GetComponent<Image>().raycastTarget = false;
        {
            var T = tray.transform;
            // Tab = the kem bo goc gan TREN mep khung, nua trong nua ngoai — dung nhu mau [mau 520-730, 615-660]
            var tabI = Nut(T, "Tab_Ingredients", Sp(CUT + "card_rounded_white.png"), Image.Type.Sliced, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(18, 8), new Vector2(222, 56));
            var tl = Chu(tabI.transform, "Txt_Label", "Ingredients 0/4", 18, Brown, true, TextAlignmentOptions.Center); Stretch(tl.rectTransform, new Vector2(10, 4), new Vector2(-10, -4));
            var tabS = Nut(T, "Tab_Seasonings", Sp(CUT + "card_rounded_white.png"), Image.Type.Sliced, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(252, 8), new Vector2(222, 56));
            var ts = Chu(tabS.transform, "Txt_Label", "Seasonings 0/3", 18, Brown, true, TextAlignmentOptions.Center); Stretch(ts.rectTransform, new Vector2(10, 4), new Vector2(-10, -4));
            var clr = Nut(T, "Btn_ClearAll", v2Skin.btnRedSmall, Image.Type.Sliced, new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-18, 8), new Vector2(100, 44));
            var cl = Chu(clr.transform, "Txt_Label", "Clear", 14, Color.white, true, TextAlignmentOptions.Center); Stretch(cl.rectTransform, Vector2.zero, Vector2.zero);
            LuoiKhay(T, "Grid_Ingredients", true);
            LuoiKhay(T, "Grid_Seasonings", false);
        }

        // ── START COOKING  [mau 1355-1705, 705-805] — anh nguyen khoi, KHONG Sliced ──
        var act = Nut(R, "Btn_Action", Sp(CUT + "btn_cook_pill.png"), Image.Type.Simple, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-25, 92), new Vector2(350, 142));
        var al = Chu(act.transform, "Txt_Label", "START COOKING", 28, Cream, true, TextAlignmentOptions.Center); StretchTiLe(al.rectTransform, new Vector2(0.30f, 0.22f), new Vector2(0.95f, 0.78f));
        ChuDat(act.transform, "Txt_Sub", "", 12, Cream, false, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(40, 14), new Vector2(220, 18));

        // ── Bo nao: KitchenSceneV2UI (copy toan bo tham chieu tu V2) + skin V3 ──
        var v3Ui = root.AddComponent<KitchenSceneV2UI>();
        UnityEditorInternal.ComponentUtility.CopyComponent(v2Ui);
        UnityEditorInternal.ComponentUtility.PasteComponentValues(v3Ui);
        var so = new SerializedObject(v3Ui);
        so.FindProperty("khoaLayout").boolValue = true;
        so.FindProperty("boQuaDongBoSkinShop").boolValue = true;
        var sk = so.FindProperty("skin");
        DatSprite(sk, "panelBoard",      Sp(CUT + "frame_dish_card.png"));
        DatSprite(sk, "panelPaper",      Sp(CUT + "card_rounded_white.png"));
        DatSprite(sk, "cardIngredient",  Sp(CUT + "card_rounded_white.png"));
        DatSprite(sk, "cardLocked",      Sp(CUT + "card_rounded_white.png"));
        DatSprite(sk, "btnGreen",        Sp(CUT + "btn_cook_pill.png"));
        DatSprite(sk, "btnGray",         Sp(CUT + "btn_cook_pill.png"));
        DatSprite(sk, "btnBackFarm",     Sp(CUT + "btn_back_farm_plank.png"));
        DatSprite(sk, "chalkboard",      Sp(CUT + "chalkboard_plain.png"));
        DatSprite(sk, "ovenBody",        Sp(CUT + "oven_stone.png"));
        DatSprite(sk, "warehouseHatch",  Sp(CUT + "storage_box_logs.png"));
        DatSprite(sk, "prepTable",       Sp(CUT + "table_counter_flat.png"));
        DatSprite(sk, "cookPot",         potFrames[0]);
        DatSprite(sk, "ribbon",          Sp(CUT + "banner_recipe_book_pill.png"));
        DatSprite(sk, "plaqueOvenState", SkinKit.BoGoc(12f));
        sk.FindPropertyRelative("tabOn").objectReferenceValue  = null;
        sk.FindPropertyRelative("tabOff").objectReferenceValue = null;
        sk.FindPropertyRelative("catChefWalk").arraySize = 0;   // meo V3 tu chay animation, V2 khong duoc tao Cat_Chef di lai
        DatSprite(sk, "cardDishRow", Sp(CUT + "card_rounded_white.png"));
        so.ApplyModifiedPropertiesWithoutUndo();

        var ds2 = v2Go.GetComponent<DailySpecialManager>();
        if (ds2 != null)
        {
            var ds3 = root.AddComponent<DailySpecialManager>();
            UnityEditorInternal.ComponentUtility.CopyComponent(ds2);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(ds3);
        }

        var fx = root.AddComponent<KitchenV3CookingFX>();
        fx.noi = potAnim; fx.dongHo = clockAnim; fx.thanhFill = bf; fx.txtThoiGian = barTxt;
        fx.diemXuatPhat = pot; fx.diaTrinhBay = plate;
        plate.GetComponent<KitchenV3PlateDrag>().hopKho = wh;
        var mirror = avBoard.gameObject.AddComponent<KitchenV3AvatarMirror>(); mirror.nguon = avKhach != null ? avKhach.GetComponent<Image>() : null;   // avatar khach dong bo len bang

        try { Debug.Log("[KitchenV3] " + v3Ui.VaPhanThieuChoEditor()); }
        catch (System.Exception e) { Debug.LogWarning("[KitchenV3] Do day the/danh sach mon that bai (Play van tu sinh): " + e.Message); }
        HauXuLyThe(root.transform);
        return root;
    }

    /// <summary>Luoi khay: Scroll_<name>/Viewport/<name> — 4 o/hang, o 148x120 (khay 690 rong), keo doc.</summary>
    private static void LuoiKhay(Transform tray, string name, bool active)
    {
        var scroll = new GameObject("Scroll_" + name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scroll.transform.SetParent(tray, false);
        var srt = (RectTransform)scroll.transform; Stretch(srt, new Vector2(26, 22), new Vector2(-26, -44));
        scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); vp.transform.SetParent(scroll.transform, false);
        Stretch((RectTransform)vp.transform, Vector2.zero, Vector2.zero);
        var grid = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); grid.transform.SetParent(vp.transform, false);
        var grt = (RectTransform)grid.transform; grt.anchorMin = new Vector2(0, 1); grt.anchorMax = new Vector2(1, 1); grt.pivot = new Vector2(0.5f, 1); grt.offsetMin = Vector2.zero; grt.offsetMax = Vector2.zero;
        var gl = grid.GetComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(150, 128); gl.spacing = new Vector2(10, 12);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4; gl.childAlignment = TextAnchor.UpperCenter; gl.padding = new RectOffset(4, 4, 4, 4);
        grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.GetComponent<ScrollRect>(); sr.viewport = (RectTransform)vp.transform; sr.content = grt; sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 24;
        scroll.SetActive(active);
    }

    /// <summary>Chinh the khay theo o 148x120 (icon to, ten duoi, so luong = huy hieu goc phai) va dong mon cao 64 nhu mau.</summary>
    private static void HauXuLyThe(Transform root)
    {
        foreach (var grid in new[] { "Tray/Scroll_Grid_Ingredients/Viewport/Grid_Ingredients", "Tray/Scroll_Grid_Seasonings/Viewport/Grid_Seasonings" })
        {
            var g = root.Find(grid); if (g == null) continue;
            foreach (Transform card in g)
            {
                var ico = card.Find("Img_MainIcon") as RectTransform; if (ico != null) { ico.anchorMin = ico.anchorMax = new Vector2(0.5f, 0.5f); ico.pivot = new Vector2(0.5f, 0.5f); ico.anchoredPosition = new Vector2(0, 8); ico.sizeDelta = new Vector2(88, 88); }
                var nm  = card.Find("Txt_Name") as RectTransform;     if (nm  != null) { nm.gameObject.SetActive(false); }   // mau: chi icon + so luong
                var q   = card.Find("Txt_Quantity") as RectTransform; if (q != null)
                {
                    // huy hieu so luong goc duoi-phai nhu mau ("x1")
                    if (card.Find("Qty_Badge") == null)
                    {
                        var bd = Anh(card, "Qty_Badge", SkinKit.BoGoc(10f), false); bd.GetComponent<Image>().type = Image.Type.Sliced; bd.GetComponent<Image>().color = new Color(0.36f, 0.22f, 0.10f, 0.92f);
                        Dat(bd, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 6), new Vector2(50, 26)); bd.SetSiblingIndex(q.GetSiblingIndex());
                    }
                    q.anchorMin = q.anchorMax = new Vector2(1, 0); q.pivot = new Vector2(1, 0); q.anchoredPosition = new Vector2(-6, 6); q.sizeDelta = new Vector2(50, 26);
                    var qt = q.GetComponent<TMP_Text>(); if (qt) { qt.fontSize = 15; qt.color = Color.white; qt.alignment = TextAlignmentOptions.Center; }
                }
            }
        }
        var content = root.Find("Recipe_Board/Board_List/Dish_Scroll/Content");
        if (content != null)
            foreach (Transform row in content)
            {
                var rrt = row as RectTransform; if (rrt != null) rrt.sizeDelta = new Vector2(0, 64);
                var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 64; le.minHeight = 64;
                var ico = row.Find("Img_Icon") as RectTransform; if (ico != null) { ico.sizeDelta = new Vector2(50, 50); ico.anchoredPosition = new Vector2(36, 0); }
                var nm  = row.Find("Txt_Name") as RectTransform; if (nm != null) { nm.anchoredPosition = new Vector2(70, -8); nm.sizeDelta = new Vector2(230, 24); var t = nm.GetComponent<TMP_Text>(); if (t) t.fontSize = 18; }
                var mt  = row.Find("Txt_Meta") as RectTransform; if (mt != null) { mt.anchoredPosition = new Vector2(70, 8); mt.sizeDelta = new Vector2(230, 16); var t = mt.GetComponent<TMP_Text>(); if (t) t.fontSize = 11; }
                if (row.Find("Txt_Chevron") == null)   // mui ten ">" ben phai nhu mau
                    ChuDat(row, "Txt_Chevron", "›", 26, new Color(0.62f, 0.45f, 0.28f), true, TextAlignmentOptions.Center, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 2), new Vector2(22, 30));
            }
    }

    // ═══════════════════════════ HELPERS ═════════════════════════════════════

    private static KitchenSceneV2UI TimV2(bool keCaTat = true)
    {
        foreach (var ui in Object.FindObjectsByType<KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ui.gameObject.name == ROOT_V2) return ui;
        var any = Object.FindObjectsByType<KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return any.Length > 0 ? any[0] : null;
    }

    private static GameObject TimTheoTen(UnityEngine.SceneManagement.Scene s, string ten)
    {
        foreach (var go in s.GetRootGameObjects()) { if (go.name == ten) return go; var t = go.transform.Find(ten); if (t != null) return t.gameObject; }
        return null;
    }

    private static UnityEngine.SceneManagement.Scene SceneManager_Active() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();

    private static void ChuyenSang(bool v3)
    {
        var v3Go = GameObject.Find(ROOT_V3) ?? TimTheoTen(SceneManager_Active(), ROOT_V3);
        var v2 = TimV2(true);
        if (v3Go == null) { Bao("Chua co Kitchen_UI_v3. Bam menu 2 de dung."); return; }
        if (v2 != null) { Undo.RecordObject(v2.gameObject, "Chuyen V2/V3"); v2.gameObject.SetActive(!v3); }
        Undo.RecordObject(v3Go, "Chuyen V2/V3"); v3Go.SetActive(v3);
        EditorSceneManager.MarkSceneDirty(v3Go.scene);
        Debug.Log("[KitchenV3] Dang bat: " + (v3 ? "Kitchen_UI_v3" : "Kitchen_UI_v2") + " — Ctrl+S de luu.");
    }

    private static bool LuuVaSaoLuu(UnityEngine.SceneManagement.Scene scene)
    {
        if (scene.isDirty)
        {
            int c = EditorUtility.DisplayDialogComplex("Kitchen V3", "Scene co thay doi CHUA LUU. Luu truoc de sao luu file scene?", "Luu roi chay", "Huy", "Chay luon (khong sao luu)");
            if (c == 1) return false;
            if (c == 0) EditorSceneManager.SaveScene(scene);
        }
        if (string.IsNullOrEmpty(scene.path)) return true;
        try
        {
            Directory.CreateDirectory("_Backup_Scene_TruocKhiVa");
            string dich = Path.Combine("_Backup_Scene_TruocKhiVa", Path.GetFileNameWithoutExtension(scene.path) + "_V3_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
            File.Copy(scene.path, dich, true);
            Debug.Log("[KitchenV3] Da sao luu scene -> " + dich);
        }
        catch (System.Exception e) { if (!EditorUtility.DisplayDialog("Kitchen V3", "Khong sao luu duoc scene: " + e.Message + "\nVan chay?", "Chay", "Huy")) return false; }
        return true;
    }

    private static void Bao(string msg) => EditorUtility.DisplayDialog("Kitchen V3", msg, "OK");

    private static Sprite Sp(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning("[KitchenV3] Thieu sprite: " + path + " (object van duoc tao, Sep gan anh sau).");
        return s;
    }

    private static Sprite[] Frames(string fmt, int n)
    {
        var arr = new Sprite[n];
        for (int i = 0; i < n; i++) arr[i] = Sp(string.Format(fmt, i + 1));
        return arr;
    }

    private static bool CauHinhMotTexture(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) return false;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = path.Contains("/anim_") || path.Contains("cat_chef_frame") ? 512 : 1024;
        ti.spritePixelsPerUnit = 100;
        string f = Path.GetFileNameWithoutExtension(path);
        Vector4 b = Vector4.zero; // x=left y=bottom z=right w=top
        switch (f)
        {
            case "frame_recipe_book": b = new Vector4(90, 90, 90, 90); break;
            case "frame_dish_card":   b = new Vector4(90, 90, 90, 90); break;
            case "card_rounded_white": b = new Vector4(14, 14, 14, 14); break;   // vien nau that chi ~12px
            case "plank_paper_order_card": b = new Vector4(70, 40, 70, 40); break;
            case "plank_white_paper_wide": b = new Vector4(60, 50, 60, 50); break;
            case "chalkboard_plain":  b = new Vector4(70, 70, 70, 70); break;
            case "dish_card_row_bg":  b = new Vector4(300, 120, 200, 120); break;
            case "btn_cook_pill":     b = Vector4.zero; break; // dung Simple + preserveAspect, khong slice
        }
        ti.spriteBorder = b;
        ti.SaveAndReimport();
        return true;
    }

    private static RectTransform Rong(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
    }

    private static RectTransform Anh(Transform parent, string name, Sprite sp, bool preserve)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sp; img.preserveAspect = preserve; img.raycastTarget = false;
        if (sp == null) img.color = new Color(1, 1, 1, 0.001f);
        return (RectTransform)go.transform;
    }

    private static RectTransform Panel(Transform parent, string name, Sprite sp, Image.Type type, bool preserve)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sp; img.type = type; img.preserveAspect = preserve; img.color = Color.white;
        if (sp == null) img.color = new Color(1, 1, 1, 0.25f); // khung trong mo: Sep nhin thay de gan anh
        return (RectTransform)go.transform;
    }

    private static RectTransform Nut(Transform parent, string name, Sprite sp, Image.Type type, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = Panel(parent, name, sp, type, type == Image.Type.Simple);
        rt.gameObject.AddComponent<Button>();
        Dat(rt, anchor, pivot, pos, size);
        return rt;
    }

    private static void Decor(Transform parent, string name, Sprite sp, Vector2 pos, Vector2 size)
    {
        var rt = Anh(parent, name, sp, true); Dat(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, size);
    }

    private static TextMeshProUGUI Chu(Transform parent, string name, string text, float size, Color color, bool bold, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
        try { var f = SkinKit.FontVo; if (f != null) t.font = f; } catch { }
        return t;
    }

    private static TextMeshProUGUI ChuDat(Transform parent, string name, string text, float size, Color color, bool bold, TextAlignmentOptions align, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sz)
    {
        var t = Chu(parent, name, text, size, color, bold, align); Dat(t.rectTransform, anchor, pivot, pos, sz); return t;
    }

    private static void Dat(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = offMin; rt.offsetMax = offMax;
    }

    private static void StretchTiLe(RectTransform rt, Vector2 aMin, Vector2 aMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void DatSprite(SerializedProperty skin, string field, Sprite sp)
    {
        if (sp == null) return;
        var p = skin.FindPropertyRelative(field); if (p != null) p.objectReferenceValue = sp;
    }

    /// <summary>Doc skin V2 (class [Serializable] private) qua SerializedObject -> ban sao de lay sprite dung chung.</summary>
    private sealed class SkinDoc
    {
        public Sprite tabOn, tabOff, btnRedSmall, tasteTrack, tasteFill, chipTaste, ovenGlow, platingTable, decorGarlic, decorOnion, decorHerbs, decorLights;
        public Sprite[] ovenFire;
    }
    private static SkinDoc DocSkin(KitchenSceneV2UI ui)
    {
        var so = new SerializedObject(ui); var sk = so.FindProperty("skin"); var d = new SkinDoc();
        Sprite G(string n) { var p = sk.FindPropertyRelative(n); return p != null ? p.objectReferenceValue as Sprite : null; }
        d.tabOn = G("tabOn"); d.tabOff = G("tabOff"); d.btnRedSmall = G("btnRedSmall"); d.tasteTrack = G("tasteTrack"); d.tasteFill = G("tasteFill");
        d.chipTaste = G("chipTaste"); d.ovenGlow = G("ovenGlow"); d.platingTable = G("platingTable");
        d.decorGarlic = G("decorGarlic"); d.decorOnion = G("decorOnion"); d.decorHerbs = G("decorHerbs"); d.decorLights = G("decorLights");
        var fa = sk.FindPropertyRelative("ovenFire");
        if (fa != null) { d.ovenFire = new Sprite[fa.arraySize]; for (int i = 0; i < fa.arraySize; i++) d.ovenFire[i] = fa.GetArrayElementAtIndex(i).objectReferenceValue as Sprite; }
        return d;
    }
}
