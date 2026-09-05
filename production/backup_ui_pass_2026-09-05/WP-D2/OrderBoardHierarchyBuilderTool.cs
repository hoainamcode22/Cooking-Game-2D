#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;


public class OrderBoardHierarchyBuilderTool : EditorWindow
{
    private const string PrefabFolder    = "Assets/_Game/Prefab/ui/OrderBoard";
    private const string SystemName      = "OrderBoardSystem";
    private const string CanvasName      = "Canvas_OrderBoardPopup";
    private const string WorldObjectName = "OrderBoard_WorldObject";


    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    private const float POPUP_W = 1500f;
    private const float POPUP_H = 860f;

    // Lưới phiếu 3x3 (B4)
    private const float TICKET_W = 250f;
    private const float TICKET_H = 210f;
    private const float TICKET_GAP = 22f;
    private const float GRID_W = TICKET_W * 3f + TICKET_GAP * 2f;   // 794
    private const float GRID_H = TICKET_H * 3f + TICKET_GAP * 2f;   // 674

    // Lưới yêu cầu 3x2 (B6/B7)
    private const float REQ_W = 160f;
    private const float REQ_H = 150f;
    private const float REQ_GAP = 14f;

    private bool _regenSprites;

    [MenuItem("Tools/Farm/Bảng Đơn Hàng", false, 23)]
    public static void Open()
    {
        OrderBoardHierarchyBuilderTool w =
            GetWindow<OrderBoardHierarchyBuilderTool>(true, "Bảng Đơn Hàng — Dựng giao diện");
        w.minSize = new Vector2(440, 320);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BẢNG ĐƠN HÀNG (DEV-B)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Dựng nền CÓ MÀU cho bảng đơn hàng: popup lưới phiếu 3x3 bốn trạng thái, cột " +
            "chi tiết đơn với lưới yêu cầu 3x2 kiểu `có/cần`, hai nút hành động, bộ hiệu " +
            "ứng giao hàng, và object bảng ngoài map có phiếu ghim.\n\n" +
            "Chạy trên scene ĐANG MỞ (thường là SCN_Farm). Mọi chỗ chờ art tên là IMG_Art... / SPR_Art...",
            MessageType.Info);

        EditorGUILayout.Space();
        _regenSprites = EditorGUILayout.ToggleLeft("Vẽ lại toàn bộ sprite (ghi đè file cũ)", _regenSprites);

        EditorGUILayout.Space();

        if (GUILayout.Button("1 · Sinh sprite bảng đơn hàng", GUILayout.Height(28)))
            OrderBoardSpriteFactory.GenerateAll(_regenSprites);

        if (GUILayout.Button("2 · Dựng TẤT CẢ (sprite + hệ thống + popup + bảng ngoài map)", GUILayout.Height(40)))
            BuildEverything(_regenSprites);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Từng phần", EditorStyles.boldLabel);

        if (GUILayout.Button("Chỉ dựng hệ thống (OrderBoardSystem)"))     BuildSystem();
        if (GUILayout.Button("Chỉ dựng popup (Canvas_OrderBoardPopup)")) BuildPopup();
        if (GUILayout.Button("Chỉ dựng bảng ngoài map"))                 BuildWorldObject();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ĐIỀU PHỐI
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildEverything(bool regenSprites)
    {
        OrderBoardSpriteFactory.GenerateAll(regenSprites);
        EnsurePrefabFolder();

        BuildSystem();
        BuildPopup(); // CHỈ DỰNG POPUP UI, KHÔNG ĐỤNG VÀO CÔNG TRÌNH NGOÀI MAP

        MarkSceneDirty();
        Debug.Log("[BảngĐơn] Dựng xong Popup UI. Giữ nguyên 100% công trình trên map.");
    }

    private static void EnsurePrefabFolder()
    {
        string abs = Path.Combine(Directory.GetCurrentDirectory(), PrefabFolder);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }
    }


    private static void MarkSceneDirty()
        => EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());


    private static void BuildSystem()
    {
        GameObject old = TimObjectGocKeCaDangTat(SystemName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(SystemName);
        Undo.RegisterCreatedObjectUndo(root, "Tạo hệ bảng đơn hàng");
        root.AddComponent<OrderBoardManager>();

        Debug.Log($"[BảngĐơn] Đã tạo '{SystemName}' mang OrderBoardManager.");
        MarkSceneDirty();
    }


    private static GameObject TimObjectGocKeCaDangTat(string ten)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == ten) return t.gameObject;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POPUP
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildPopup()
    {
        OrderBoardSpriteFactory.GenerateAll(false);   // bù sprite còn thiếu, không ghi đè
        EnsurePrefabFolder();

        // Quét cả object đang TẮT — xem TimObjectGocKeCaDangTat để biết vì sao.
        GameObject old = TimObjectGocKeCaDangTat(CanvasName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        OrderTicketUI      ticketPrefab = BuildTicketPrefab();
        OrderRequireCellUI reqPrefab    = BuildRequireCellPrefab();

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGo = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Tạo Canvas_OrderBoardPopup");

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 121;   // trên HUD, cùng bậc với popup quầy hàng

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.matchWidthOrHeight  = 0.5f;

        OrderBoardPopupUI popup = canvasGo.AddComponent<OrderBoardPopupUI>();

        // ── Nền mờ (chính là popupRoot) ──────────────────────────────────────
        RectTransform dim = CreateUI("Panel_Dim", canvasGo.transform);
        Stretch(dim, 0, 0, 0, 0);
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.62f);
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.transition    = Selectable.Transition.None;

        // ── Thân popup (Khung ván gỗ đồng bộ 100% Kho & Shop) ─────────────────
        RectTransform main = CreateUI("Popup_Main", dim);
        Center(main, Vector2.zero, new Vector2(POPUP_W, POPUP_H));

        // 3a. Viền gỗ ngoài #4A2508
        RectTransform boardBorder = CreateUI("Board_Border", main);
        Stretch(boardBorder, -8, -8, -8, -8);
        boardBorder.gameObject.AddComponent<Image>().color = TaskPopupDesign.VanGoVien;

        // 3b. Thân ván gỗ đáy #7C4E22
        RectTransform boardFill = CreateUI("Board_Fill_Bottom", main);
        Stretch(boardFill, 0, 0, 0, 0);
        boardFill.gameObject.AddComponent<Image>().color = TaskPopupDesign.VanGoDuoi;

        // 3c. Lớp phủ gradient #A9743C
        RectTransform boardTop = CreateUI("Board_Fill_Top", main);
        Stretch(boardTop, 0, 0, 0, 0);
        boardTop.gameObject.AddComponent<Image>().color = new Color(TaskPopupDesign.VanGoTren.r, TaskPopupDesign.VanGoTren.g, TaskPopupDesign.VanGoTren.b, 0.45f);

        // 3d. Thớ ván ngang
        for (int i = 1; i <= 6; i++)
        {
            float yPos = 400f - i * 125f;
            RectTransform grainRect = CreateUI($"Board_Grain_{i}", main);
            Center(grainRect, new Vector2(0f, yPos), new Vector2(1460f, 5f));
            grainRect.gameObject.AddComponent<Image>().color = TaskPopupDesign.VanGoTho;
        }

        // 3e. 4 Đinh sắt góc
        Vector2[] studPositions = {
            new Vector2(-700f, 385f), new Vector2(700f, 385f),
            new Vector2(-700f, -385f), new Vector2(700f, -385f)
        };
        for (int i = 0; i < studPositions.Length; i++)
        {
            Vector2 pos = studPositions[i];
            RectTransform sRim = CreateUI($"Stud_{i}_Rim", main);
            Center(sRim, pos, new Vector2(30f, 30f));
            sRim.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatVien;

            RectTransform sBase = CreateUI($"Stud_{i}_Base", main);
            Center(sBase, pos, new Vector2(26f, 26f));
            sBase.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatToi;

            RectTransform sShine = CreateUI($"Stud_{i}_Shine", main);
            Center(sShine, pos + new Vector2(-2f, 2f), new Vector2(13f, 13f));
            sShine.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatSang;
        }

        // 4. RIBBON TIÊU ĐỀ ("BẢNG ĐƠN HÀNG" 3D 100% SVG ASSET)
        Sprite bannerRibbonSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_banner_ribbon.png");
        RectTransform bannerRect = CreateUI("Header_Banner", main);
        Center(bannerRect, new Vector2(0f, 415f), new Vector2(620f, 126f));
        Image bannerImg = bannerRect.gameObject.AddComponent<Image>();
        bannerImg.sprite = bannerRibbonSpr;
        bannerImg.type = Image.Type.Sliced;
        bannerImg.raycastTarget = false;

        TextMeshProUGUI title = AddText(bannerRect, "Text_Title", "BẢNG ĐƠN HÀNG", 46,
                                        TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, 40, 6, 40, 6);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        title.textWrappingMode = TextWrappingModes.NoWrap;

        // 5. NÚT ĐÓNG [X] (btnX.png 90x90)
        Sprite btnCloseSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/btnX.png");
        RectTransform close = CreateUI("BtnClose", main);
        Center(close, new Vector2(735f, 415f), new Vector2(90f, 90f));
        Image closeImg = close.gameObject.AddComponent<Image>();
        closeImg.sprite = btnCloseSpr ?? OrderBoardSpriteFactory.Load("ob_circle");
        closeImg.preserveAspect = true;
        Button closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;

        // ── B4 · Cột trái: lưới phiếu 3x3 ────────────────────────────────────
        RectTransform gridBack = CreateUI("Panel_TicketArea", main);
        Center(gridBack, new Vector2(-313f, -14f), new Vector2(GRID_W + 34f, GRID_H + 34f));
        Sliced(gridBack, "ob_inset", Color.white);

        RectTransform grid = CreateUI("TicketGrid", main);
        Center(grid, new Vector2(-313f, -14f), new Vector2(GRID_W, GRID_H));
        GridLayoutGroup gl = grid.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize        = new Vector2(TICKET_W, TICKET_H);
        gl.spacing         = new Vector2(TICKET_GAP, TICKET_GAP);
        gl.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 3;
        gl.childAlignment  = TextAnchor.UpperLeft;

        // ── B6 · Cột phải: chi tiết đơn ──────────────────────────────────────
        DetailRefs detail = BuildDetailColumn(main, reqPrefab);

        // ── B8 · Hai nút hành động ───────────────────────────────────────────
        // Thùng rác ĐỎ bên trái, GIAO HÀNG XANH DƯƠNG bên phải — thứ tự này theo video và
        // cũng đúng nguyên tắc: hành động phá huỷ nằm xa ngón cái hơn hành động chính.
        RectTransform discard = CreateUI("Btn_Discard", main);
        Center(discard, new Vector2(200f, -360f), new Vector2(112f, 100f));
        Sliced(discard, "ob_btn", OrderBoardSpriteFactory.Brick);
        Button discardBtn = discard.gameObject.AddComponent<Button>();
        discardBtn.targetGraphic = discard.GetComponent<Image>();

        RectTransform trashIcon = CreateUI("IMG_ArtTrashIcon", discard);
        Anchor(trashIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
        Simple(trashIcon, "ob_trash", Color.white);

        RectTransform deliver = CreateUI("Btn_Deliver", main);
        Center(deliver, new Vector2(490f, -360f), new Vector2(390f, 100f));
        Image deliverImg = Sliced(deliver, "ob_btn", OrderBoardSpriteFactory.Ocean);
        Button deliverBtn = deliver.gameObject.AddComponent<Button>();
        deliverBtn.targetGraphic = deliverImg;

        // Transition.None vì màu nút do OrderBoardPopupUI.SetDeliverState điều khiển tay
        // (xanh dương khi đủ hàng / xám khi chưa). Để ColorTint thì Unity ghi đè màu đó
        // mỗi lần con trỏ đi qua và tín hiệu "chưa đủ hàng" biến mất.
        deliverBtn.transition = Selectable.Transition.None;

        TextMeshProUGUI deliverTxt = AddText(deliver, "Text_Deliver", "GIAO HÀNG", 36,
                                             Color.white, TextAlignmentOptions.Center);
        Stretch(deliverTxt.rectTransform, 14, 6, 14, 6);
        deliverTxt.fontStyle = FontStyles.Bold;

        // ── B9 · Bộ hiệu ứng giao hàng ───────────────────────────────────────
        FxRefs fx = BuildDeliverFx(main);

        // ── Thông báo ────────────────────────────────────────────────────────
        RectTransform toast = CreateUI("Message_Toast", main);
        Center(toast, new Vector2(-313f, -384f), new Vector2(700f, 62f));
        Sliced(toast, "ob_btn", OrderBoardSpriteFactory.Hex("#1B2A20"));
        TextMeshProUGUI toastTxt = AddText(toast, "Text_Message", "", 26,
                                           OrderBoardSpriteFactory.Cream, TextAlignmentOptions.Center);
        Stretch(toastTxt.rectTransform, 16, 4, 16, 4);
        toast.gameObject.SetActive(false);

        // ── Nối dây ──────────────────────────────────────────────────────────
        new Wiring(popup)
            .Obj("popupRoot",              dim.gameObject)
            .Obj("buttonClose",            closeBtn)
            .Obj("buttonDimBackground",    dimBtn)
            .Obj("textTitle",              title)
            .Obj("ticketGridContent",      grid)
            .Obj("ticketGridLayout",       gl)
            .Obj("ticketPrefab",           ticketPrefab)
            .Obj("detailEmptyRoot",        detail.EmptyRoot)
            .Obj("detailContentRoot",      detail.ContentRoot)
            .Obj("imageArtCustomerAvatar", detail.Avatar)
            .Obj("textOrderTitle",         detail.OrderTitle)
            .Obj("textRewardExp",          detail.RewardExp)
            .Obj("textRewardGold",         detail.RewardGold)
            .ObjList("requireCells",       detail.Cells)
            .Obj("buttonDiscard",          discardBtn)
            .Obj("buttonDeliver",          deliverBtn)
            .Obj("textDeliverLabel",       deliverTxt)
            .Obj("imageDeliverBackground", deliverImg)
            .Obj("deliverFx",              fx.Component)
            .Obj("messageRoot",            toast.gameObject)
            .Obj("textMessage",            toastTxt)
            .Apply();

        dim.gameObject.SetActive(false);

        // Nối với prefab để chủ dự án mở Prefab Mode sửa được bằng chuột.
        string canvasPrefabPath = $"{PrefabFolder}/{CanvasName}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGo, canvasPrefabPath, InteractionMode.AutomatedAction);

        Debug.Log($"[BảngĐơn] Đã dựng popup + lưu prefab: {canvasPrefabPath}");
        MarkSceneDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CỘT PHẢI — CHI TIẾT ĐƠN (B6 + B7)
    // ─────────────────────────────────────────────────────────────────────────

    private class DetailRefs
    {
        public GameObject EmptyRoot, ContentRoot;
        public Image      Avatar;
        public TextMeshProUGUI OrderTitle, RewardExp, RewardGold;
        public List<Object> Cells = new List<Object>();
    }

    private static DetailRefs BuildDetailColumn(RectTransform main, OrderRequireCellUI reqPrefab)
    {
        var r = new DetailRefs();

        RectTransform col = CreateUI("Col_Detail", main);
        Center(col, new Vector2(417f, 23f), new Vector2(586f, 600f));
        Sliced(col, "ob_inset", Color.white);

        // ── Chưa chọn đơn nào ────────────────────────────────────────────────
        RectTransform empty = CreateUI("Detail_Empty", col);
        Stretch(empty, 24, 24, 24, 24);
        TextMeshProUGUI emptyTxt = AddText(empty, "Text_DetailHint",
            "Chọn một đơn bên trái\nđể xem cần những gì", 28,
            new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center);
        Stretch(emptyTxt.rectTransform, 0, 0, 0, 0);
        emptyTxt.textWrappingMode = TextWrappingModes.Normal;
        r.EmptyRoot = empty.gameObject;

        // ── Đã chọn đơn ──────────────────────────────────────────────────────
        RectTransform content = CreateUI("Detail_Content", col);
        Stretch(content, 0, 0, 0, 0);
        r.ContentRoot = content.gameObject;

        // Avatar khách hàng — góc trên trái, to (theo video)
        RectTransform avatarFrame = CreateUI("Frame_Avatar", content);
        Anchor(avatarFrame, new Vector2(0f, 1f), new Vector2(110f, -102f), new Vector2(152f, 152f));
        Sliced(avatarFrame, "ob_btn", OrderBoardSpriteFactory.Hex("#3A5844"));

        RectTransform avatar = CreateUI("IMG_ArtCustomerAvatar", avatarFrame);
        Anchor(avatar, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(122f, 122f));
        r.Avatar = Simple(avatar, "ob_circle", new Color(0.75f, 0.80f, 0.72f, 1f));

        // Ô thưởng — góc trên phải
        RectTransform reward = CreateUI("Box_Reward", content);
        Anchor(reward, new Vector2(1f, 1f), new Vector2(-162f, -102f), new Vector2(276f, 152f));
        Sliced(reward, "ob_btn", OrderBoardSpriteFactory.Hex("#1B2A20"));

        r.RewardExp  = BuildRewardRow(reward, "Row_Exp",  -44f, "ob_star",
                                      OrderBoardSpriteFactory.Hex("#7FB5F0"));
        r.RewardGold = BuildRewardRow(reward, "Row_Gold", -106f, "ob_coin", Color.white);

        // Tên đơn — kho tên 300+ của DEV-A phải có chỗ để hiện, nếu không thì cả mục 5.2
        // file TEAM thành công cốc.
        r.OrderTitle = AddText(content, "Text_OrderTitle", "", 30,
                               OrderBoardSpriteFactory.Cream, TextAlignmentOptions.Center);
        Anchor(r.OrderTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f),
               new Vector2(520f, 44f));
        r.OrderTitle.textWrappingMode = TextWrappingModes.NoWrap;
        r.OrderTitle.overflowMode     = TextOverflowModes.Ellipsis;

        // Gạch NÉT ĐỨT chia hai phần (theo video)
        RectTransform divider = CreateUI("IMG_DashDivider", content);
        Anchor(divider, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(510f, 10f));
        Image divImg = divider.gameObject.AddComponent<Image>();
        divImg.sprite = OrderBoardSpriteFactory.Load("ob_dashline");
        divImg.type   = Image.Type.Tiled;
        divImg.color  = new Color(1f, 1f, 1f, 0.30f);
        divImg.raycastTarget = false;

        // Lưới yêu cầu 3x2
        float reqGridW = REQ_W * 3f + REQ_GAP * 2f;
        float reqGridH = REQ_H * 2f + REQ_GAP;

        RectTransform reqGrid = CreateUI("RequireGrid", content);
        Anchor(reqGrid, new Vector2(0.5f, 1f), new Vector2(0f, -262f - reqGridH * 0.5f),
               new Vector2(reqGridW, reqGridH));
        GridLayoutGroup rg = reqGrid.gameObject.AddComponent<GridLayoutGroup>();
        rg.cellSize        = new Vector2(REQ_W, REQ_H);
        rg.spacing         = new Vector2(REQ_GAP, REQ_GAP);
        rg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        rg.constraintCount = 3;
        rg.childAlignment  = TextAnchor.UpperLeft;

        // 6 ô dựng SẴN trong Editor chứ không Instantiate lúc chạy: số ô là hằng số
        // (3x2), không có lý do gì phải cấp phát lúc chơi. Dựng sẵn thì chủ dự án còn
        // sửa được từng ô bằng chuột.
        if (reqPrefab == null)
        {
            // Lưu prefab hỏng thì dừng ở đây kèm lỗi rõ ràng, thay vì ném NullReference
            // giữa chừng và để lại một popup dựng dở trong scene.
            Debug.LogError("[BảngĐơn] Không tạo được prefab PF_OrderRequireCell → cột phải " +
                           "sẽ không có lưới yêu cầu. Kiểm tra quyền ghi thư mục " + PrefabFolder);
        }
        else
        {
            for (int i = 0; i < OrderBoardManagerBase.MaxRequirementSlots; i++)
            {
                var cellGo = (GameObject)PrefabUtility.InstantiatePrefab(reqPrefab.gameObject, reqGrid);
                if (cellGo == null) continue;

                cellGo.name = $"RequireCell_{i}";
                r.Cells.Add(cellGo.GetComponent<OrderRequireCellUI>());
            }
        }

        content.gameObject.SetActive(false);
        return r;
    }

    /// <summary>Một dòng thưởng: [icon] [số] — dùng cho cả sao EXP lẫn đồng vàng.</summary>
    private static TextMeshProUGUI BuildRewardRow(RectTransform parent, string name, float y,
                                                  string iconSprite, Color iconColor)
    {
        RectTransform row = CreateUI(name, parent);
        Anchor(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(230f, 56f));

        RectTransform icon = CreateUI("IMG_ArtRewardIcon", row);
        Anchor(icon, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(46f, 46f));
        Simple(icon, iconSprite, iconColor);

        TextMeshProUGUI txt = AddText(row, "Text_Value", "0", 34, OrderBoardSpriteFactory.Cream,
                                      TextAlignmentOptions.MidlineLeft);
        Stretch(txt.rectTransform, 76, 4, 10, 4);
        txt.fontStyle = FontStyles.Bold;
        return txt;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  B9 · BỘ HIỆU ỨNG GIAO HÀNG
    // ─────────────────────────────────────────────────────────────────────────

    private class FxRefs
    {
        public OrderDeliverFxUI Component;
    }

    /// <summary>
    /// Dựng SẴN toàn bộ hạt hiệu ứng trong hierarchy.
    ///
    /// `FX_Root` được đặt TRÙNG KHÍT với `TicketGrid` (cùng tâm, cùng kích thước) và các
    /// hạt dùng cùng kiểu neo (0,1) mà GridLayoutGroup gán cho phiếu. Nhờ vậy toạ độ
    /// `HomePosition` của phiếu dùng thẳng được cho khói — không phải đổi hệ toạ độ,
    /// mà đổi hệ toạ độ giữa hai RectTransform chính là chỗ hiệu ứng hay bung sai chỗ nhất.
    /// </summary>
    private static FxRefs BuildDeliverFx(RectTransform main)
    {
        var r = new FxRefs();

        RectTransform root = CreateUI("FX_DeliverRoot", main);
        Center(root, new Vector2(-313f, -14f), new Vector2(GRID_W, GRID_H));

        var smokes = new List<Object>();
        for (int i = 0; i < 8; i++)
        {
            RectTransform s = CreateUI($"Smoke_{i}", root);
            Anchor(s, new Vector2(0f, 1f), Vector2.zero, new Vector2(96f, 96f));
            Image img = Simple(s, "ob_smoke", Color.white);
            s.gameObject.SetActive(false);
            smokes.Add(img);
        }

        var flies = new List<Object>();
        for (int i = 0; i < 6; i++)
        {
            RectTransform f = CreateUI($"Fly_{i}", root);
            Anchor(f, new Vector2(0f, 1f), Vector2.zero, new Vector2(54f, 54f));

            // Xen kẽ sao EXP và đồng vàng: hai loại phần thưởng bay lên cùng lúc thì
            // người chơi đọc ra ngay là "được cả hai", không phải đoán.
            bool isStar = (i % 2 == 0);
            Image img = Simple(f, isStar ? "ob_star" : "ob_coin",
                               isStar ? OrderBoardSpriteFactory.Hex("#7FB5F0") : Color.white);
            f.gameObject.SetActive(false);
            flies.Add(img);
        }

        TextMeshProUGUI labelExp = AddText(root, "Label_Exp", "+0", 36,
                                           OrderBoardSpriteFactory.Hex("#9CC9F5"), TextAlignmentOptions.Center);
        Anchor(labelExp.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(180f, 52f));
        labelExp.fontStyle = FontStyles.Bold;
        labelExp.gameObject.SetActive(false);

        TextMeshProUGUI labelGold = AddText(root, "Label_Gold", "+0", 36,
                                            OrderBoardSpriteFactory.Amber, TextAlignmentOptions.Center);
        Anchor(labelGold.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(180f, 52f));
        labelGold.fontStyle = FontStyles.Bold;
        labelGold.gameObject.SetActive(false);

        OrderDeliverFxUI fx = root.gameObject.AddComponent<OrderDeliverFxUI>();
        new Wiring(fx)
            .Obj("fxRoot",        root)
            .ObjList("smokePuffs", smokes)
            .ObjList("flyIcons",   flies)
            .Obj("labelExp",      labelExp)
            .Obj("labelGold",     labelGold)
            .Apply();

        r.Component = fx;
        return r;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREFAB PHIẾU (B4 + B5)
    // ─────────────────────────────────────────────────────────────────────────

    private static OrderTicketUI BuildTicketPrefab()
    {
        EnsurePrefabFolder();

        var root = new GameObject("PF_OrderTicket", typeof(RectTransform));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(TICKET_W, TICKET_H);

        // Vùng bấm phủ cả phiếu — Image gần như trong suốt chỉ để nhận raycast.
        Image hit = root.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0.001f);
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = hit;
        btn.transition    = Selectable.Transition.None;   // trạng thái do code vẽ, không để Unity ghi đè

        // ── Trạng thái 1 & 2: CÓ ĐƠN ─────────────────────────────────────────
        RectTransform filled = CreateUI("State_Filled", rt);
        Stretch(filled, 0, 0, 0, 0);

        RectTransform paper = CreateUI("IMG_ArtTicketPaper", filled);
        Stretch(paper, 0, 0, 0, 0);
        Image paperImg = Simple(paper, "ob_ticket", new Color(0.96f, 0.94f, 0.86f, 1f));
        paperImg.preserveAspect = false;   // tờ giấy phải lấp kín ô, không giữ tỉ lệ

        RectTransform pin = CreateUI("IMG_ArtPin", filled);
        Anchor(pin, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(42f, 42f));
        Simple(pin, "ob_pin", OrderBoardSpriteFactory.Amber);

        // ⚠ CHỈ hiện PHẦN THƯỞNG. Không thêm dòng yêu cầu vào đây — xem chú thích đầu
        // file `OrderTicketUI.cs`.
        TextMeshProUGUI expTxt  = BuildTicketRewardRow(filled, "Row_Exp",  26f, "ob_star",
                                                       OrderBoardSpriteFactory.Hex("#3B82D9"));
        TextMeshProUGUI goldTxt = BuildTicketRewardRow(filled, "Row_Gold", -36f, "ob_coin", Color.white);

        // Dấu tích to góc trên phải — trạng thái 2
        RectTransform check = CreateUI("Check_Badge", filled);
        Anchor(check, new Vector2(1f, 1f), new Vector2(-34f, -32f), new Vector2(58f, 58f));
        Simple(check, "ob_circle", OrderBoardSpriteFactory.Hex("#2E8B3F"));
        RectTransform checkMark = CreateUI("IMG_Check", check);
        Anchor(checkMark, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
        Simple(checkMark, "ob_check", Color.white);
        check.gameObject.SetActive(false);

        // ── Trạng thái 4: Ô TRỐNG ────────────────────────────────────────────
        RectTransform emptyState = CreateUI("State_Empty", rt);
        Stretch(emptyState, 6, 6, 6, 6);
        Image emptyImg = emptyState.gameObject.AddComponent<Image>();
        emptyImg.sprite = OrderBoardSpriteFactory.Load("ob_dashed");
        emptyImg.type   = Image.Type.Simple;
        emptyImg.color  = new Color(1f, 1f, 1f, 0.55f);
        emptyImg.raycastTarget = false;
        emptyState.gameObject.SetActive(false);

        // ── Trạng thái 3: KHUNG SÁNG VÀNG (chồng lên 1 hoặc 2) ───────────────
        RectTransform glow = CreateUI("Frame_SelectedGlow", rt);
        Stretch(glow, -14, -14, -14, -14);   // lồi ra ngoài mép phiếu để quầng sáng thấy rõ
        Image glowImg = glow.gameObject.AddComponent<Image>();
        glowImg.sprite = OrderBoardSpriteFactory.Load("ob_glow");
        glowImg.type   = Image.Type.Sliced;
        glowImg.color  = Color.white;
        glowImg.raycastTarget = false;
        glow.gameObject.SetActive(false);

        OrderTicketUI ticket = root.AddComponent<OrderTicketUI>();
        new Wiring(ticket)
            .Obj("stateFilledRoot", filled.gameObject)
            .Obj("stateEmptyRoot",  emptyState.gameObject)
            .Obj("imageArtPaper",   paperImg)
            .Obj("imageArtPin",     pin.GetComponent<Image>())
            .Obj("textExp",         expTxt)
            .Obj("textGold",        goldTxt)
            .Obj("checkBadge",      check.gameObject)
            .Obj("selectedGlow",    glow.gameObject)
            .Obj("button",          btn)
            .Apply();

        string path = $"{PrefabFolder}/PF_OrderTicket.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return saved != null ? saved.GetComponent<OrderTicketUI>() : null;
    }

    private static TextMeshProUGUI BuildTicketRewardRow(RectTransform parent, string name, float y,
                                                        string iconSprite, Color iconColor)
    {
        RectTransform row = CreateUI(name, parent);
        Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(178f, 52f));

        RectTransform icon = CreateUI("IMG_ArtRewardIcon", row);
        Anchor(icon, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(44f, 44f));
        Simple(icon, iconSprite, iconColor);

        TextMeshProUGUI txt = AddText(row, "Text_Value", "0", 34,
                                      new Color(0.24f, 0.21f, 0.16f, 1f), TextAlignmentOptions.MidlineLeft);
        Stretch(txt.rectTransform, 66, 4, 8, 4);
        txt.fontStyle = FontStyles.Bold;
        return txt;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREFAB Ô YÊU CẦU (B7)
    // ─────────────────────────────────────────────────────────────────────────

    private static OrderRequireCellUI BuildRequireCellPrefab()
    {
        EnsurePrefabFolder();

        var root = new GameObject("PF_OrderRequireCell", typeof(RectTransform));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(REQ_W, REQ_H);

        Image bgImg = root.AddComponent<Image>();
        bgImg.sprite = OrderBoardSpriteFactory.Load("ob_inset");
        bgImg.type   = Image.Type.Sliced;
        bgImg.color  = new Color(0.18f, 0.25f, 0.19f, 1f);
        bgImg.raycastTarget = false;

        RectTransform filled = CreateUI("State_Filled", rt);
        Stretch(filled, 0, 0, 0, 0);

        RectTransform icon = CreateUI("IMG_ArtItemIcon", filled);
        Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(68f, 68f));
        Image iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        // ⚠ `có/cần` — vế TRÁI là kho, vế PHẢI là yêu cầu. Không đổi thứ tự.
        TextMeshProUGUI amount = AddText(filled, "Text_Amount", "0/0", 30,
                                         OrderBoardSpriteFactory.Cream, TextAlignmentOptions.Center);
        Anchor(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(150f, 36f));
        amount.fontStyle = FontStyles.Bold;

        TextMeshProUGUI nameTxt = AddText(filled, "Text_Name", "", 17,
                                          new Color(1f, 1f, 1f, 0.62f), TextAlignmentOptions.Center);
        Anchor(nameTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(152f, 26f));
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform check = CreateUI("Check_Badge", filled);
        Anchor(check, new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(38f, 38f));
        Simple(check, "ob_circle", OrderBoardSpriteFactory.Hex("#2E8B3F"));
        RectTransform checkMark = CreateUI("IMG_Check", check);
        Anchor(checkMark, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
        Simple(checkMark, "ob_check", Color.white);
        check.gameObject.SetActive(false);

        RectTransform emptyState = CreateUI("State_Empty", rt);
        Stretch(emptyState, 6, 6, 6, 6);
        Image emptyImg = emptyState.gameObject.AddComponent<Image>();
        emptyImg.sprite = OrderBoardSpriteFactory.Load("ob_dashed");
        emptyImg.type   = Image.Type.Simple;
        emptyImg.color  = new Color(1f, 1f, 1f, 0.35f);
        emptyImg.raycastTarget = false;

        OrderRequireCellUI cell = root.AddComponent<OrderRequireCellUI>();
        new Wiring(cell)
            .Obj("stateFilledRoot",        filled.gameObject)
            .Obj("stateEmptyRoot",         emptyState.gameObject)
            .Obj("imageArtItemIcon",       iconImg)
            .Obj("textAmount",             amount)
            .Obj("textName",               nameTxt)
            .Obj("checkBadge",             check.gameObject)
            .Obj("imageArtCellBackground", bgImg)
            .Apply();

        string path = $"{PrefabFolder}/PF_OrderRequireCell.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return saved != null ? saved.GetComponent<OrderRequireCellUI>() : null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BẢNG NGOÀI MAP (B1 + B2)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chép sorting layer + order từ một công trình ĐANG HIỂN THỊ ĐÚNG trong scene.
    ///
    /// VÌ SAO KHÔNG GÁN SỐ CỨNG: dự án đang dùng sorting layer id <c>1669604809</c> cho
    /// 208 sprite (gồm `Market`, `CookingGate`), nhưng id đó **không có trong TagManager**.
    /// Đây là "layer chết" đã biết của dự án. Gán id lạ bằng code thì Unity từ chối, im
    /// lặng rơi về Default, và bảng chìm xuống dưới mọi công trình khác — lỗi cực khó đoán
    /// vì Inspector vẫn hiện đúng tên layer.
    ///
    /// Chép trực tiếp từ SpriteRenderer thật thì luôn khớp, kể cả sau này ai đó dọn lại
    /// bảng sorting layer.
    /// </summary>
    private static void ApDungSortingTheoCongTrinhCoSan(SpriteRenderer dich)
    {
        if (dich == null) return;

        // Ưu tiên theo thứ tự: quầy hàng mới → chợ → cổng bếp
        string[] mau = { "Stall_WorldObject", "Market", "CookingGate" };
        foreach (string ten in mau)
        {
            GameObject g = GameObject.Find(ten);
            SpriteRenderer nguon = g != null ? g.GetComponent<SpriteRenderer>() : null;
            if (nguon == null) continue;

            dich.sortingLayerID = nguon.sortingLayerID;
            dich.sortingOrder   = nguon.sortingOrder;
            return;
        }

        Debug.LogWarning(
            "[BảngĐơn] Không tìm thấy công trình mẫu ('Stall_WorldObject' / 'Market' / " +
            "'CookingGate') để chép sorting. Bảng sẽ nằm ở layer Default order 0 và nhiều " +
            "khả năng bị chìm dưới các công trình khác. Hãy tự chỉnh Sorting Layer + Order " +
            "cho khớp một công trình bất kỳ trong scene.");
    }

    private static void BuildWorldObject()
    {
        // Sinh sprite trước (bỏ qua file đã có). Bấm thẳng nút này mà chưa chạy bước 1 thì
        // mọi Load() trả null và bảng ra một cục vô hình giữa bản đồ — rất khó đoán.
        OrderBoardSpriteFactory.GenerateAll(false);

        // GameObject.Find KHÔNG thấy object đang TẮT → chạy tool lúc bảng đang tắt sẽ sinh
        // ra cái thứ hai, bản đồ có hai bảng chồng nhau. Phải quét cả object tắt.
        GameObject old = null;
        foreach (var t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == WorldObjectName) { old = t.gameObject; break; }
        }

        // GIỮ LẠI VỊ TRÍ người dùng đã kéo. Destroy rồi tạo mới ở (0,0) thì mỗi lần chạy
        // lại tool là bảng nhảy về giữa bản đồ, phải kéo lại từ đầu.
        Vector3 viTriCu = old != null ? old.transform.position : Vector3.zero;
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(WorldObjectName);
        Undo.RegisterCreatedObjectUndo(root, "Tạo bảng đơn hàng ngoài map");
        root.transform.position = viTriCu;

        // ── B1 · THÂN CÔNG TRÌNH: MỘT SpriteRenderer duy nhất, ngay trên gốc ──
        // Cố ý KHÔNG tách thành bảng + mái hiên + chóp vòm như video: chủ dự án tự vẽ cả
        // công trình thành một ảnh, một ô để gắn là dễ nhất. Đây cũng đúng quy ước sẵn có
        // của dự án — `Market` và `CookingGate` đều là 1 SpriteRenderer trên gốc.
        SpriteRenderer bodySr = root.AddComponent<SpriteRenderer>();
        bodySr.sprite = OrderBoardSpriteFactory.Load("ob_panel");   // ảnh tạm, thay bằng art thật
        bodySr.color  = OrderBoardSpriteFactory.Hex("#31503B");
        bodySr.drawMode = SpriteDrawMode.Sliced;
        bodySr.size     = new Vector2(3.2f, 2.4f);
        ApDungSortingTheoCongTrinhCoSan(bodySr);

        // ── B2 · PHIẾU GHIM PHẢN CHIẾU TRẠNG THÁI ────────────────────────────
        var marksRoot = new GameObject("OrderMarks");
        marksRoot.transform.SetParent(root.transform, false);
        marksRoot.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        var marks = new List<Object>();
        for (int i = 0; i < 5; i++)
        {
            var m = new GameObject($"SPR_ArtOrderMark_{i}");
            m.transform.SetParent(marksRoot.transform, false);

            // Xếp so le hai hàng cho giống mấy tờ giấy ghim vội, không thẳng hàng như bảng biểu.
            float x = -0.92f + (i % 3) * 0.46f + (i >= 3 ? 0.23f : 0f);
            float y = (i < 3) ? 0.22f : -0.30f;
            m.transform.localPosition = new Vector3(x, y, 0f);
            m.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0) ? 4f : -5f);
            m.transform.localScale    = new Vector3(0.16f, 0.16f, 1f);

            SpriteRenderer sr = m.AddComponent<SpriteRenderer>();
            sr.sprite = OrderBoardSpriteFactory.Load("ob_ticket");
            sr.color  = new Color(0.96f, 0.94f, 0.86f, 1f);
            ApDungSortingTheoCongTrinhCoSan(sr);
            sr.sortingOrder += 3;    // nổi trên mặt bảng
            marks.Add(sr);
        }

        // Vùng bấm
        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(3.2f, 2.6f);
        col.offset    = new Vector2(0f, 0.2f);
        col.isTrigger = true;

        OrderBoardWorldObject world = root.AddComponent<OrderBoardWorldObject>();

        OrderBoardPopupUI popup =
            Object.FindFirstObjectByType<OrderBoardPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
            Debug.LogWarning("[BảngĐơn] Chưa có Canvas_OrderBoardPopup trong scene → bảng ngoài " +
                             "map chưa nối được popup. Chạy 'Dựng TẤT CẢ' hoặc dựng popup trước.");

        new Wiring(world)
            .Obj("popupUI",        popup)
            .Obj("mainCamera",     Camera.main)
            .Obj("targetCollider", col)
            .Obj("spriteArtBoard", bodySr)
            .Obj("orderMarksRoot", marksRoot)
            .ObjList("orderMarks", marks)
            .Apply();

        Debug.Log("[BảngĐơn] Đã tạo bảng ngoài map — kéo tới vị trí mong muốn trên bản đồ, " +
                  "rồi thay sprite trên gốc bằng art thật.");
        MarkSceneDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH DỰNG UI
    // ─────────────────────────────────────────────────────────────────────────

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    private static void Stretch(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    private static Image Sliced(RectTransform rt, string spriteName, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = OrderBoardSpriteFactory.Load(spriteName);
        img.type   = Image.Type.Sliced;
        img.color  = color;
        return img;
    }

    private static Image Simple(RectTransform rt, string spriteName, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = OrderBoardSpriteFactory.Load(spriteName);
        img.type   = Image.Type.Simple;
        img.color  = color;
        img.raycastTarget  = false;
        img.preserveAspect = true;
        return img;
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string name, string content,
                                           float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = CreateUI(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text          = content;
        t.fontSize      = size;
        t.color         = color;
        t.alignment     = align;
        t.raycastTarget = false;
        // API mới của TMP trong Unity 6 (`enableWordWrapping` đã bị đánh dấu lỗi thời).
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode     = TextOverflowModes.Overflow;
        return t;
    }


    private class Wiring
    {
        private readonly SerializedObject _so;
        private readonly string           _name;

        public Wiring(Object target)
        {
            _so   = new SerializedObject(target);
            _name = target != null ? target.GetType().Name : "(null)";
        }

        public Wiring Obj(string propertyName, Object value)
        {
            SerializedProperty p = _so.FindProperty(propertyName);
            if (p == null) { Debug.LogError($"[BảngĐơn] {_name}: không có field '{propertyName}'."); return this; }
            p.objectReferenceValue = value;
            return this;
        }

        public Wiring ObjList(string propertyName, List<Object> values)
        {
            SerializedProperty p = _so.FindProperty(propertyName);
            if (p == null) { Debug.LogError($"[BảngĐơn] {_name}: không có field '{propertyName}'."); return this; }

            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            return this;
        }

        public void Apply() => _so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
