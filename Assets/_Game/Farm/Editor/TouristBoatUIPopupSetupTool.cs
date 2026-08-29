using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/Setup Popups (UI) — BOAT-002 §3.5 + §3.6.
/// (pattern TouristBoatSetupTool: find-or-create từng mảnh, Undo cho mọi object,
///  report cuối + checklist "CẦN BẠN LÀM")
///
/// Dựng 2 popup screen-space trong scene đang mở:
///
///   Canvas_TouristBoatPopup (canvas RIÊNG ở gốc scene — [QA B-4] CỐ Ý không dùng
///                 FarmUIManager.canvasPopupRoot: canvas đó bị EnterCookingMode()
///                 SetActive(false) ⇒ Unity giết coroutine của popup ⇒ mọi thông báo
///                 sau lần đầu vào bếp nằm chết trong hàng đợi. Component phải SỐNG
///                 để nghe event và HOÃN đúng nghĩa; phần ẩn/hiện khi ở bếp do chính
///                 popup lo — xem BoatAnnouncePopupUI.DangTrongSceneBep)
///   └─ TouristBoatPopups                      (ACTIVE — gốc gom 2 popup)
///      ├─ BoatAnnouncePopup                   (ACTIVE — giữ BoatAnnouncePopupUI, phải sống
///      │  │                                     để nghe OnNextTripScheduled dù popup đang ẩn)
///      │  └─ Root                             (INACTIVE — phần nhìn thấy)
///      │     ├─ Dim         Image đen a=0.6, full-screen, raycastTarget = chặn UI dưới
///      │     └─ Card        Image khung gỗ (sprite tìm trong project)
///      │        └─ Content  CanvasGroup (fade-in)
///      │           ├─ Title / Body  TMP
///      │           └─ Btn_DaRo (+ Label TMP "Đã rõ")
///      └─ DockPurchasePopup                   (ACTIVE — giữ DockPurchasePopupUI)
///         └─ Root                             (INACTIVE)
///            ├─ Dim · Card · Content
///            │  ├─ Title "Mở bến số X" · LevelReq · Reason (đỏ)
///            │  ├─ CostRow: Icon (vàng/gem) + CostText (vàng #FFD34D)
///            │  └─ Btn_Mua (+ Label) · Btn_Close (+ Label "X")
///
/// Tool tự làm hết: tạo hierarchy · set anchor/pivot responsive · gán font TMP có
/// thật trong project · gán sprite khung gỗ + icon vàng/gem tìm được · wire TOÀN BỘ
/// SerializeField của 2 component · wire luôn purchasePopup + lockBoardSprite cho
/// TouristBoatUnlockFlow trong scene · đặt Root inactive · ping + log từng object
/// và LIỆT KÊ RÕ sprite/font nào đang dùng TẠM để Sếp thay art sau.
///
/// IDEMPOTENT: chạy lại nhiều lần không nhân bản, không đè ref/sprite đã chỉnh tay
/// (chỉ điền chỗ đang trống); object đã có thì giữ nguyên vị trí/cỡ.
/// </summary>
public static class TouristBoatUIPopupSetupTool
{
    private const string MenuSetupPopups = "Tools/Farm Game/Tourist Boat/Setup Popups (UI)";
    private const string UndoLabel       = "Tourist Boat Popups Setup";

    private const string RootName     = "TouristBoatPopups";
    private const string AnnounceName = "BoatAnnouncePopup";
    private const string PurchaseName = "DockPurchasePopup";
    private const string CanvasRiengName = "Canvas_TouristBoatPopup";

    // Cỡ thiết kế (reference resolution 1920x1080 — cùng CanvasScaler dự án dùng)
    private static readonly Vector2 CardAnnounceSize = new Vector2(1100f, 620f);
    private static readonly Vector2 CardPurchaseSize = new Vector2(980f,  680f);

    private static readonly Color MauDim      = new Color(0f, 0f, 0f, 0.6f);   // dim đen 60% (GDD §3.5)
    private static readonly Color MauChuNau   = new Color(0.35f, 0.22f, 0.10f); // chữ trên nền gỗ
    private static readonly Color MauChuPhu   = new Color(0.45f, 0.33f, 0.20f);
    private static readonly Color MauVangGia  = new Color(1f, 0.827f, 0.302f);  // #FFD34D
    private static readonly Color MauDoLyDo   = new Color(0.898f, 0.290f, 0.290f);
    private static readonly Color MauNutMua   = new Color(0.42f, 0.72f, 0.30f);  // xanh lá "mua"
    private static readonly Color MauNutXacNhan = new Color(0.95f, 0.72f, 0.28f); // cam ấm "Đã rõ"
    private static readonly Color MauNutDong  = new Color(0.80f, 0.34f, 0.30f);

    // Ghi chú art tạm — gom lại để in cuối report
    private static readonly List<string> _ghiChuArtTam = new List<string>();
    private static readonly StringBuilder _log = new StringBuilder();

    // ─────────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Menu bấm tay. [QA m-11] Priority 30: Dev A = 12, Dev B = 20/21/22, Dev C = 30 —
    /// thứ tự menu trong nhánh Tourist Boat cố định, Sếp không bấm nhầm tool sau mỗi
    /// lần Unity reload domain.
    ///
    /// PRIVATE có chủ ý (không phải sơ suất): tool tổng của Dev B gọi sang đây bằng
    /// reflection `GetMethod("SetupPopups", Public | Static)` — kiểu tra CHỈ THEO TÊN
    /// này ném AmbiguousMatchException nếu có 2 overload public cùng tên. Để wrapper
    /// menu ở mức private thì phía public chỉ còn ĐÚNG MỘT hàm `SetupPopups(bool)`,
    /// reflection luôn resolve đúng. Menu vẫn chạy bình thường — Unity không đòi
    /// MenuItem phải public.
    /// </summary>
    [MenuItem(MenuSetupPopups, false, 30)]
    private static void SetupPopupsMenu()
    {
        SetupPopups(false); // menu bấm tay — vẫn hiện dialog như cũ
    }

    /// <summary>
    /// Dựng + wire 2 popup. Trả về NỘI DUNG REPORT dạng chuỗi (gồm cả mục
    /// "ART ĐANG DÙNG TẠM" và checklist "CẦN BẠN LÀM").
    ///
    /// quiet = false: hiện EditorUtility.DisplayDialog như khi bấm menu.
    /// quiet = true : KHÔNG hiện dialog nào — dành cho tool tổng
    ///                (`TouristBoatOneClickSetup` của Dev B) gộp report của cả 3 Dev
    ///                vào MỘT bảng tổng kết duy nhất, không chen ngang giữa chừng.
    ///                Console log vẫn ghi (rẻ, và là nơi tra chi tiết từng object).
    ///
    /// Thất bại (không tạo được canvas) cũng trả chuỗi mô tả lý do — tool tổng in
    /// nguyên văn, không nuốt mất thông tin.
    /// </summary>
    public static string SetupPopups(bool quiet)
    {
        _ghiChuArtTam.Clear();
        _log.Length = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        // ── Tài nguyên dùng chung: font + sprite ────────────────────────────
        TMP_FontAsset font  = TimFontTMP();
        Sprite khungGo      = TimSpriteKhungGo();
        Sprite iconVang     = TimIconTien(laVang: true);
        Sprite iconGem      = TimIconTien(laVang: false);
        Sprite spriteTron   = SpriteBuiltin("UI/Skin/Knob.psd");
        Sprite spriteNut    = SpriteBuiltin("UI/Skin/UISprite.psd"); // 9-slice bo góc sẵn

        // ── Canvas RIÊNG (không bị EnterCookingMode tắt — QA B-4) ───────────
        Transform canvas = TimHoacTaoCanvasRieng(out string canvasNguon);
        if (canvas == null)
        {
            const string loi = "LỖI: không tạo được Canvas popup trong scene đang mở.\n" +
                               "Mở scene farm (SCN_Farm) rồi chạy lại.";
            Debug.LogError("[TouristBoat] Setup Popups (UI) — " + loi);
            if (!quiet) EditorUtility.DisplayDialog("Tourist Boat — Popups", loi, "OK");
            return loi;
        }
        Ghi($"Canvas dùng: {DuongDanScene(canvas)}  ({canvasNguon})");

        // Scene đã chạy bản tool cũ (popup nằm dưới canvasPopupRoot) → chuyển sang canvas riêng
        ChuyenPopupCuSangCanvasRieng(canvas);

        // ── Gốc gom 2 popup ─────────────────────────────────────────────────
        Transform root = TimHoacTaoUI(canvas, RootName, out bool rootMoi);
        StretchFull(root as RectTransform);
        root.gameObject.SetActive(true);
        if (rootMoi) Ghi($"+ {RootName} (gốc gom popup — ACTIVE)");

        // ── Popup 1: báo tàu ────────────────────────────────────────────────
        DungPopupBaoTau(root, font, khungGo, spriteNut);

        // ── Popup 2: mua slot bến ───────────────────────────────────────────
        DungPopupMuaBen(root, font, khungGo, spriteNut, spriteTron, iconVang, iconGem);

        // ── Wire ngược sang TouristBoatUnlockFlow (bảng khóa + popup mua) ───
        WireUnlockFlow(khungGo);

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        EditorUtility.SetDirty(root.gameObject);

        // Tool tổng tự chọn/ping object của nó ở bước cuối — đừng giành Selection
        if (!quiet)
        {
            Selection.activeGameObject = root.gameObject;
            EditorGUIUtility.PingObject(root.gameObject);
        }

        // ── REPORT ──────────────────────────────────────────────────────────
        var bao = new StringBuilder();
        bao.AppendLine("Đã dựng/kiểm tra xong 2 popup của hệ Tourist Boat V2.");
        bao.AppendLine();
        bao.AppendLine("ART ĐANG DÙNG TẠM (thay là xong, KHÔNG cần sửa code):");
        if (_ghiChuArtTam.Count == 0)
            bao.AppendLine("• (không có — mọi sprite/font đều lấy được asset thật trong project)");
        else
            foreach (string s in _ghiChuArtTam) bao.AppendLine("• " + s);
        bao.AppendLine();
        bao.AppendLine("CẦN BẠN LÀM TRONG UNITY:");
        bao.AppendLine("1) Ctrl+S lưu scene (tool chỉ sửa scene, không tạo prefab).");
        bao.AppendLine("2) Thay sprite khung gỗ thật vào Card của 2 popup khi art xong,");
        bao.AppendLine("   và vào field 'Lock Board Sprite' của BoatSystem/TouristBoatUnlockFlow.");
        bao.AppendLine("3) Kiểm font tiếng Việt: mở Root của popup, bật tạm lên xem chữ có dấu");
        bao.AppendLine("   ('Tàu số 01 sắp cập bến!') có hiện đủ dấu không — thiếu thì đổi font TMP.");
        bao.AppendLine("4) Play test: Lv12 + 2.000 vàng → tap bảng khóa bến 2 → popup mua.");
        bao.AppendLine("5) Test hồi quy QA B-4: vào bếp rồi ra, đợi tàu rời bến →");
        bao.AppendLine("   popup báo tàu VẪN hiện (canvas riêng, không bị EnterCookingMode tắt).");
        bao.AppendLine();
        bao.Append("Chi tiết từng object đã in ra Console. (Ctrl+Z hoàn tác toàn bộ.)");

        Debug.Log("[TouristBoat] Setup Popups (UI):\n" + _log);

        string report = bao.ToString();
        if (!quiet)
            EditorUtility.DisplayDialog("Tourist Boat — Popups UI", report, "OK");
        return report;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POPUP 1 — BÁO TÀU (§3.5)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DungPopupBaoTau(Transform parent, TMP_FontAsset font,
                                        Sprite khungGo, Sprite spriteNut)
    {
        Transform holder = TimHoacTaoUI(parent, AnnounceName, out bool moi);
        StretchFull(holder as RectTransform);
        holder.gameObject.SetActive(true); // PHẢI active: component nghe event khi popup ẩn
        if (moi) Ghi($"+ {AnnounceName} (ACTIVE — giữ component nghe OnNextTripScheduled)");

        var ui = holder.GetComponent<BoatAnnouncePopupUI>();
        if (ui == null)
        {
            ui = Undo.AddComponent<BoatAnnouncePopupUI>(holder.gameObject);
            Ghi($"  + component BoatAnnouncePopupUI");
        }

        // Root (inactive) → Dim + Card
        Transform rootVisual = TimHoacTaoUI(holder, "Root", out bool rootMoi);
        StretchFull(rootVisual as RectTransform);
        if (rootMoi) Ghi("  + Root (phần nhìn thấy — để INACTIVE)");

        Image dim = EnsureImage(rootVisual, "Dim", out bool dimMoi);
        StretchFull(dim.rectTransform);
        if (dimMoi)
        {
            dim.color         = MauDim;
            dim.raycastTarget = true; // chặn mọi thao tác dưới popup
            Ghi("    + Dim (đen 60%, chặn raycast)");
        }

        Image card = EnsureImage(rootVisual, "Card", out bool cardMoi);
        if (cardMoi)
        {
            CanhGiuaMH(card.rectTransform, CardAnnounceSize);
            ApSpriteKhung(card, khungGo);
            Ghi($"    + Card (khung gỗ {CardAnnounceSize.x:0}x{CardAnnounceSize.y:0})");
        }
        else if (card.sprite == null)
        {
            ApSpriteKhung(card, khungGo);
        }

        Transform content = TimHoacTaoUI(card.transform, "Content", out bool ctMoi);
        StretchFull(content as RectTransform);
        var group = content.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(content.gameObject);
        if (ctMoi) Ghi("      + Content (CanvasGroup — fade-in chữ)");

        // Tiêu đề
        TextMeshProUGUI title = EnsureText(content, "Title", font, 62f, MauChuNau,
                                           TextAlignmentOptions.Center, out bool tMoi);
        if (tMoi)
        {
            NeoTren(title.rectTransform, new Vector2(CardAnnounceSize.x - 140f, 110f), -70f);
            title.fontStyle = FontStyles.Bold;
            title.text      = "Tàu số 01 sắp cập bến!";
            Ghi("      + Title (TMP — text thật set lúc runtime)");
        }

        // Nội dung
        TextMeshProUGUI body = EnsureText(content, "Body", font, 42f, MauChuPhu,
                                          TextAlignmentOptions.Center, out bool bMoi);
        if (bMoi)
        {
            NeoGiua(body.rectTransform, new Vector2(CardAnnounceSize.x - 180f, 240f), 10f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.text = "Tàu số 01 sẽ cập bến sau 5 phút! Chuẩn bị nguyên liệu, " +
                        "nấu món ngon tiếp đãi khách nhé!";
            Ghi("      + Body (TMP nhiều dòng)");
        }

        // Nút "Đã rõ"
        Button btn = EnsureButton(content, "Btn_DaRo", spriteNut, MauNutXacNhan,
                                  new Vector2(360f, 110f), out TextMeshProUGUI label, out bool nMoi);
        if (nMoi)
        {
            NeoDuoi(btn.GetComponent<RectTransform>(), new Vector2(360f, 110f), 80f);
            ApFont(label, font);
            label.text     = "Đã rõ";
            label.fontSize = 46f;
            label.color    = Color.white;
            Ghi("      + Btn_DaRo (+ Label \"Đã rõ\")");
        }

        // ── Wire SerializeField (chỉ điền chỗ trống — không đè chỉnh tay) ───
        var so = new SerializedObject(ui);
        SetRefIfEmpty(so, "popupRoot",     rootVisual.gameObject, moi);
        SetRefIfEmpty(so, "dimImage",      dim,                   moi);
        SetRefIfEmpty(so, "cardRect",      card.rectTransform,    moi);
        SetRefIfEmpty(so, "contentGroup",  group,                 moi);
        SetRefIfEmpty(so, "titleText",     title,                 moi);
        SetRefIfEmpty(so, "bodyText",      body,                  moi);
        SetRefIfEmpty(so, "confirmButton", btn,                   moi);
        so.ApplyModifiedProperties();
        Ghi("  ✓ Wire xong SerializeField của BoatAnnouncePopupUI");

        rootVisual.gameObject.SetActive(false); // trạng thái mặc định
        EditorGUIUtility.PingObject(holder.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POPUP 2 — MUA SLOT BẾN (§3.6)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DungPopupMuaBen(Transform parent, TMP_FontAsset font, Sprite khungGo,
                                        Sprite spriteNut, Sprite spriteTron,
                                        Sprite iconVang, Sprite iconGem)
    {
        Transform holder = TimHoacTaoUI(parent, PurchaseName, out bool moi);
        StretchFull(holder as RectTransform);
        holder.gameObject.SetActive(true);
        if (moi) Ghi($"+ {PurchaseName} (ACTIVE)");

        var ui = holder.GetComponent<DockPurchasePopupUI>();
        if (ui == null)
        {
            ui = Undo.AddComponent<DockPurchasePopupUI>(holder.gameObject);
            Ghi("  + component DockPurchasePopupUI");
        }

        Transform rootVisual = TimHoacTaoUI(holder, "Root", out bool rootMoi);
        StretchFull(rootVisual as RectTransform);
        if (rootMoi) Ghi("  + Root (INACTIVE)");

        Image dim = EnsureImage(rootVisual, "Dim", out bool dimMoi);
        StretchFull(dim.rectTransform);
        if (dimMoi)
        {
            dim.color         = MauDim;
            dim.raycastTarget = true;
            Ghi("    + Dim (đen 60%)");
        }

        Image card = EnsureImage(rootVisual, "Card", out bool cardMoi);
        if (cardMoi)
        {
            CanhGiuaMH(card.rectTransform, CardPurchaseSize);
            ApSpriteKhung(card, khungGo);
            Ghi($"    + Card (khung gỗ {CardPurchaseSize.x:0}x{CardPurchaseSize.y:0})");
        }
        else if (card.sprite == null)
        {
            ApSpriteKhung(card, khungGo);
        }

        Transform content = TimHoacTaoUI(card.transform, "Content", out bool ctMoi);
        StretchFull(content as RectTransform);
        var group = content.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(content.gameObject);
        if (ctMoi) Ghi("      + Content (CanvasGroup)");

        // Tiêu đề
        TextMeshProUGUI title = EnsureText(content, "Title", font, 58f, MauChuNau,
                                           TextAlignmentOptions.Center, out bool tMoi);
        if (tMoi)
        {
            NeoTren(title.rectTransform, new Vector2(CardPurchaseSize.x - 160f, 100f), -70f);
            title.fontStyle = FontStyles.Bold;
            title.text      = "Mở bến số 2";
            Ghi("      + Title");
        }

        // Dòng yêu cầu level
        TextMeshProUGUI levelReq = EnsureText(content, "LevelReq", font, 38f, MauChuPhu,
                                              TextAlignmentOptions.Center, out bool lMoi);
        if (lMoi)
        {
            NeoTren(levelReq.rectTransform, new Vector2(CardPurchaseSize.x - 160f, 60f), -190f);
            levelReq.text = "Yêu cầu: đạt Lv12 (bạn đang Lv10)";
            Ghi("      + LevelReq");
        }

        // Hàng giá: icon + số (HorizontalLayoutGroup cho icon-số luôn dính nhau)
        Transform costRow = TimHoacTaoUI(content, "CostRow", out bool crMoi);
        var costRect = costRow as RectTransform;
        if (crMoi)
        {
            NeoGiua(costRect, new Vector2(CardPurchaseSize.x - 240f, 120f), 20f);
            var layout = Undo.AddComponent<HorizontalLayoutGroup>(costRow.gameObject);
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.spacing                = 18f;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = false;
            layout.childControlHeight     = false;
            Ghi("      + CostRow (HorizontalLayoutGroup — icon + số giá)");
        }

        Image costIcon = EnsureImage(costRow, "CostIcon", out bool ciMoi);
        if (ciMoi)
        {
            costIcon.rectTransform.sizeDelta = new Vector2(88f, 88f);
            costIcon.raycastTarget = false;
            costIcon.preserveAspect = true;
            if (iconVang != null) costIcon.sprite = iconVang;
            else                  costIcon.sprite = spriteTron; // placeholder tròn
            Ghi("        + CostIcon (script tự đổi vàng/gem theo bến)");
        }

        TextMeshProUGUI costText = EnsureText(costRow, "CostText", font, 64f, MauVangGia,
                                              TextAlignmentOptions.MidlineLeft, out bool cMoi);
        if (cMoi)
        {
            costText.rectTransform.sizeDelta = new Vector2(340f, 90f);
            costText.fontStyle        = FontStyles.Bold;
            costText.textWrappingMode = TextWrappingModes.NoWrap;
            costText.text             = "2.000";
            Ghi("        + CostText (vàng #FFD34D)");
        }
        costText.color = MauVangGia; // ép đúng tông vàng kể cả khi object có sẵn

        // Dòng lý do (đỏ)
        TextMeshProUGUI reason = EnsureText(content, "Reason", font, 34f, MauDoLyDo,
                                            TextAlignmentOptions.Center, out bool rMoi);
        if (rMoi)
        {
            NeoDuoi(reason.rectTransform, new Vector2(CardPurchaseSize.x - 160f, 50f), 210f);
            reason.text = "Không đủ vàng";
            Ghi("      + Reason (đỏ — chỉ hiện khi nút MUA disable)");
        }
        reason.color = MauDoLyDo;

        // Nút MUA
        Button btnMua = EnsureButton(content, "Btn_Mua", spriteNut, MauNutMua,
                                     new Vector2(400f, 118f), out TextMeshProUGUI labelMua, out bool nMoi);
        if (nMoi)
        {
            NeoDuoi(btnMua.GetComponent<RectTransform>(), new Vector2(400f, 118f), 80f);
            ApFont(labelMua, font);
            labelMua.text     = "MUA";
            labelMua.fontSize = 48f;
            labelMua.color    = Color.white;
            Ghi("      + Btn_Mua (+ Label \"MUA\")");
        }

        // Nút X đóng — góc phải trên của Card
        Button btnDong = EnsureButton(content, "Btn_Close", spriteTron, MauNutDong,
                                      new Vector2(84f, 84f), out TextMeshProUGUI labelDong, out bool dMoi);
        if (dMoi)
        {
            var rt = btnDong.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.sizeDelta        = new Vector2(84f, 84f);
            rt.anchoredPosition = new Vector2(-24f, -24f);
            ApFont(labelDong, font);
            labelDong.text     = "X";
            labelDong.fontSize = 44f;
            labelDong.color    = Color.white;
            Ghi("      + Btn_Close (góc phải trên)");
        }

        // ── Wire SerializeField ─────────────────────────────────────────────
        var so = new SerializedObject(ui);
        SetRefIfEmpty(so, "popupRoot",      rootVisual.gameObject, moi);
        SetRefIfEmpty(so, "dimImage",       dim,                   moi);
        SetRefIfEmpty(so, "cardRect",       card.rectTransform,    moi);
        SetRefIfEmpty(so, "contentGroup",   group,                 moi);
        SetRefIfEmpty(so, "titleText",      title,                 moi);
        SetRefIfEmpty(so, "levelReqText",   levelReq,              moi);
        SetRefIfEmpty(so, "costIcon",       costIcon,              moi);
        SetRefIfEmpty(so, "costText",       costText,              moi);
        SetRefIfEmpty(so, "reasonText",     reason,                moi);
        SetRefIfEmpty(so, "buyButton",      btnMua,                moi);
        SetRefIfEmpty(so, "buyLabel",       labelMua,              moi);
        SetRefIfEmpty(so, "closeButton",    btnDong,               moi);
        SetRefIfEmpty(so, "goldIconSprite", iconVang,              false);
        SetRefIfEmpty(so, "gemIconSprite",  iconGem,               false);
        so.ApplyModifiedProperties();
        Ghi("  ✓ Wire xong SerializeField của DockPurchasePopupUI");

        rootVisual.gameObject.SetActive(false);
        EditorGUIUtility.PingObject(holder.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Wire ngược sang TouristBoatUnlockFlow (bảng khóa world-space)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gán purchasePopup + lockBoardSprite (placeholder khung gỗ) cho
    /// TouristBoatUnlockFlow trong scene, nếu field đang trống. Không có
    /// BoatSystem trong scene thì chỉ ghi log — không phải lỗi.
    /// </summary>
    private static void WireUnlockFlow(Sprite khungGo)
    {
        var flow = Object.FindFirstObjectByType<TouristBoatUnlockFlow>(FindObjectsInactive.Include);
        if (flow == null)
        {
            Ghi("! Không thấy TouristBoatUnlockFlow trong scene — chạy tool " +
                "\"1. Setup All (Scene + Config)\" trước nếu chưa dựng BoatSystem.");
            return;
        }

        var popup = Object.FindFirstObjectByType<DockPurchasePopupUI>(FindObjectsInactive.Include);

        var so = new SerializedObject(flow);
        bool coGan = false;
        coGan |= SetRefIfEmpty(so, "purchasePopup",   popup,   false);
        coGan |= SetRefIfEmpty(so, "lockBoardSprite", khungGo, false);
        so.ApplyModifiedProperties();

        Ghi(coGan
            ? "✓ Wire TouristBoatUnlockFlow: purchasePopup + lockBoardSprite (khung gỗ tạm)"
            : "· TouristBoatUnlockFlow đã có sẵn ref — giữ nguyên, không đè.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Tìm tài nguyên: canvas · font · sprite khung · icon tiền
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Canvas RIÊNG cho popup boat: tìm object tên Canvas_TouristBoatPopup (kể cả
    /// đang tắt) → không có thì tạo mới ở GỐC scene, Overlay, sortingOrder 400
    /// (dưới overlay intro 500, trên HUD farm), ScaleWithScreenSize 1920x1080.
    ///
    /// [QA B-4] KHÔNG đặt dưới FarmUIManager.canvasPopupRoot nữa: `EnterCookingMode()`
    /// gọi `canvasPopupRoot.SetActive(false)`, mà Unity GIẾT toàn bộ coroutine của
    /// MonoBehaviour bị deactivate và KHÔNG chạy lại khi bật lên ⇒ vòng rút hàng đợi
    /// chết vĩnh viễn sau lần đầu vào bếp (GDD §5 edge 6 đòi HOÃN rồi HIỆN LẠI).
    /// Đổi lại, popup phải tự ẩn khi ở scene bếp — đã có trong cả 2 component.
    /// </summary>
    private static Transform TimHoacTaoCanvasRieng(out string nguon)
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c != null && c.name == CanvasRiengName)
            {
                nguon = "canvas riêng đã có từ lần chạy trước";
                return c.transform;
            }
        }

        var canvasGo = new GameObject(CanvasRiengName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGo, UndoLabel);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400; // dưới overlay intro (500), trên HUD farm
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        nguon = "TẠO MỚI (canvas riêng, không bị tắt khi vào bếp)";
        return canvasGo.transform;
    }

    /// <summary>
    /// [QA B-4] Scene đã chạy BẢN CŨ của tool sẽ có TouristBoatPopups nằm dưới
    /// canvasPopupRoot — chuyển sang canvas riêng, giữ nguyên mọi con + ref đã wire.
    /// Không tìm thấy hoặc đã đúng chỗ → không làm gì.
    /// </summary>
    private static void ChuyenPopupCuSangCanvasRieng(Transform canvasRieng)
    {
        Transform cu = null;
        var announce = Object.FindFirstObjectByType<BoatAnnouncePopupUI>(FindObjectsInactive.Include);
        if (announce != null && announce.transform.parent != null)
            cu = announce.transform.parent; // BoatAnnouncePopup → cha là TouristBoatPopups

        if (cu == null || cu.name != RootName) return;
        if (cu.parent == canvasRieng) return;

        Undo.SetTransformParent(cu, canvasRieng, "Chuyển popup boat sang canvas riêng");
        StretchFull(cu as RectTransform);
        Ghi($"↻ Chuyển '{RootName}' từ '{(cu.parent != null ? cu.parent.name : "(gốc scene)")}' " +
            "sang canvas riêng — sửa lỗi popup chết sau khi vào bếp (QA B-4).");
    }

    /// <summary>
    /// Font TMP CÓ THẬT trong project: ưu tiên font trong Assets/ (art dự án, khả
    /// năng có dấu tiếng Việt), fallback font mặc định của TMP. Trả null nếu
    /// project chưa import TMP Essentials — lúc đó TMP tự lấy font mặc định.
    /// </summary>
    private static TMP_FontAsset TimFontTMP()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        TMP_FontAsset trongAssets = null;
        TMP_FontAsset batKy       = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) continue;

            if (batKy == null) batKy = f;
            if (path.StartsWith("Assets/") && !path.Contains("TextMesh Pro/Resources"))
            {
                trongAssets = f;
                break; // font riêng của dự án — ưu tiên tuyệt đối
            }
        }

        TMP_FontAsset chon = trongAssets ?? batKy;
        if (chon == null)
        {
            _ghiChuArtTam.Add("KHÔNG thấy TMP_FontAsset nào — TMP sẽ dùng font mặc định. " +
                              "Nếu chữ tiếng Việt mất dấu: Window > TextMeshPro > Import TMP Essential Resources, " +
                              "rồi tạo font asset từ font có dấu và gán vào các TMP của 2 popup.");
        }
        else if (trongAssets == null)
        {
            _ghiChuArtTam.Add($"Font TMP đang dùng: '{chon.name}' (font mặc định của TMP, không phải font dự án) — " +
                              "kiểm tra chữ có dấu tiếng Việt, thiếu thì thay font.");
        }
        else
        {
            Ghi($"Font TMP: {chon.name} (font trong Assets/ của dự án).");
        }
        return chon;
    }

    /// <summary>
    /// Sprite khung gỗ: dò AssetDatabase theo tên ưu tiên "khunggo" → "WoodBoard_Frame"
    /// → "khung" → "wood"/"frame"/"board"/"panel". Không thấy → sprite built-in
    /// UISprite (9-slice bo góc) làm placeholder + ghi chú cho Sếp.
    /// </summary>
    private static Sprite TimSpriteKhungGo()
    {
        string[] tuKhoaUuTien = { "khunggo", "khung_go", "woodboard_frame", "woodboard", "khung", "wood_frame" };
        string[] tuKhoaPhu    = { "wood", "frame", "board", "panel", "popup" };

        Sprite s = TimSpriteTheoTuKhoa(tuKhoaUuTien, out string path);
        if (s == null) s = TimSpriteTheoTuKhoa(tuKhoaPhu, out path);

        if (s != null)
        {
            Ghi($"Sprite khung: {s.name}  ({path})");
            _ghiChuArtTam.Add($"Khung card 2 popup đang dùng sprite '{s.name}' ({path}) — " +
                              "đúng khung gỗ art request thì giữ, không thì thay ở field Source Image của Card.");
            return s;
        }

        _ghiChuArtTam.Add("KHÔNG tìm thấy sprite khung gỗ nào trong project (đã tìm 'khunggo', " +
                          "'WoodBoard_Frame', 'khung', 'wood', 'frame'...) — Card đang dùng UISprite built-in " +
                          "(hộp xám bo góc). Art xong thì kéo vào Source Image của Card ở CẢ 2 popup.");
        return SpriteBuiltin("UI/Skin/UISprite.psd");
    }

    /// <summary>
    /// Icon tiền: ưu tiên lấy ĐÚNG icon HUD đang dùng (Image anh em của txtGold/
    /// txtGem trong FarmUIManager), fallback dò asset theo tên.
    /// </summary>
    private static Sprite TimIconTien(bool laVang)
    {
        // 1. Icon thật trên HUD
        var uiMgr = Object.FindFirstObjectByType<FarmUIManager>(FindObjectsInactive.Include);
        if (uiMgr != null)
        {
            var so = new SerializedObject(uiMgr);
            SerializedProperty p = so.FindProperty(laVang ? "txtGold" : "txtGem");
            if (p != null && p.objectReferenceValue is TMP_Text txt && txt != null)
            {
                Transform cha = txt.transform.parent;
                if (cha != null)
                {
                    // Image anh em (không tính chính ô chữ) = icon tiền của HUD
                    foreach (var img in cha.GetComponentsInChildren<Image>(true))
                    {
                        if (img == null || img.sprite == null) continue;
                        Ghi($"Icon {(laVang ? "vàng" : "gem")}: lấy từ HUD ({img.sprite.name}).");
                        return img.sprite;
                    }
                }
            }
        }

        // 2. Dò theo tên asset
        string[] tuKhoa = laVang
            ? new[] { "icon_gold", "gold", "coin", "vang", "xu" }
            : new[] { "icon_gem", "gem", "diamond", "kimcuong", "kim_cuong" };

        Sprite s = TimSpriteTheoTuKhoa(tuKhoa, out string path);
        if (s != null)
        {
            Ghi($"Icon {(laVang ? "vàng" : "gem")}: {s.name} ({path})");
            return s;
        }

        _ghiChuArtTam.Add($"KHÔNG thấy icon {(laVang ? "VÀNG" : "GEM")} trong project — " +
                          $"hàng giá của popup mua sẽ dùng chấm tròn placeholder. Kéo icon HUD thật vào field " +
                          $"'{(laVang ? "Gold Icon Sprite" : "Gem Icon Sprite")}' của DockPurchasePopupUI.");
        return null;
    }

    /// <summary>
    /// Quét mọi Sprite trong Assets/, trả sprite đầu tiên có tên chứa 1 trong các
    /// từ khóa (so sánh không phân biệt hoa/thường, ưu tiên theo THỨ TỰ từ khóa).
    /// Bỏ qua asset trong Packages/ và thư mục Editor.
    /// </summary>
    private static Sprite TimSpriteTheoTuKhoa(string[] tuKhoa, out string duongDan)
    {
        duongDan = null;
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets" });

        for (int k = 0; k < tuKhoa.Length; k++)
        {
            string key = tuKhoa[k].ToLowerInvariant();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.Contains("/Editor/")) continue;

                // Sprite có thể là sub-asset của texture (sprite sheet) — quét hết
                foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    var sp = obj as Sprite;
                    if (sp == null) continue;
                    if (!sp.name.ToLowerInvariant().Contains(key)) continue;

                    duongDan = path;
                    return sp;
                }
            }
        }
        return null;
    }

    private static Sprite SpriteBuiltin(string ten)
        => AssetDatabase.GetBuiltinExtraResource<Sprite>(ten);

    // ─────────────────────────────────────────────────────────────────────────
    //  Helper dựng UI
    // ─────────────────────────────────────────────────────────────────────────

    private static Transform TimHoacTaoUI(Transform parent, string ten, out bool moiTao)
    {
        Transform t = parent.Find(ten);
        moiTao = t == null;
        if (t != null) return t;

        var go = new GameObject(ten, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoLabel);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Image EnsureImage(Transform parent, string ten, out bool moiTao)
    {
        Transform t = TimHoacTaoUI(parent, ten, out moiTao);
        var img = t.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(t.gameObject);
        return img;
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string ten, TMP_FontAsset font,
                                              float size, Color color, TextAlignmentOptions align,
                                              out bool moiTao)
    {
        Transform t = TimHoacTaoUI(parent, ten, out moiTao);
        var txt = t.GetComponent<TextMeshProUGUI>();
        if (txt == null) txt = Undo.AddComponent<TextMeshProUGUI>(t.gameObject);

        if (moiTao)
        {
            txt.fontSize      = size;
            txt.color         = color;
            txt.alignment     = align;
            txt.raycastTarget = false; // chữ không nuốt click của nút
        }
        ApFont(txt, font);
        return txt;
    }

    private static Button EnsureButton(Transform parent, string ten, Sprite sprite, Color mau,
                                       Vector2 size, out TextMeshProUGUI label, out bool moiTao)
    {
        Transform t = TimHoacTaoUI(parent, ten, out moiTao);

        var img = t.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(t.gameObject);
        if (moiTao)
        {
            img.sprite = sprite;
            img.color  = mau;
            img.type   = sprite != null && sprite.border != Vector4.zero
                ? Image.Type.Sliced : Image.Type.Simple;
            ((RectTransform)t).sizeDelta = size;
        }

        var btn = t.GetComponent<Button>();
        if (btn == null) btn = Undo.AddComponent<Button>(t.gameObject);
        btn.targetGraphic = img;

        Transform lt = TimHoacTaoUI(t, "Label", out bool labelMoi);
        label = lt.GetComponent<TextMeshProUGUI>();
        if (label == null) label = Undo.AddComponent<TextMeshProUGUI>(lt.gameObject);
        if (labelMoi)
        {
            StretchFull(label.rectTransform);
            label.alignment     = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
        return btn;
    }

    private static void ApFont(TMP_Text txt, TMP_FontAsset font)
    {
        if (txt == null || font == null) return;
        if (txt.font == null || txt.font != font) txt.font = font;
    }

    /// <summary>Card: neo giữa màn hình, cỡ cố định — CanvasScaler lo phần co giãn theo thiết bị.</summary>
    private static void CanhGiuaMH(RectTransform rt, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void NeoTren(RectTransform rt, Vector2 size, float y)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static void NeoGiua(RectTransform rt, Vector2 size, float y)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static void NeoDuoi(RectTransform rt, Vector2 size, float y)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Áp sprite khung cho Card (9-slice nếu sprite có border — bo góc không méo).</summary>
    private static void ApSpriteKhung(Image card, Sprite khung)
    {
        card.sprite        = khung;
        card.color         = Color.white;
        card.raycastTarget = true; // card cũng chặn click xuyên
        card.type          = khung != null && khung.border != Vector4.zero
            ? Image.Type.Sliced : Image.Type.Simple;
    }

    /// <summary>
    /// Điền field object CHỈ KHI đang trống (force=true cho object tool vừa tạo).
    /// Trả true nếu lần gọi này có gán. Giữ nguyên mọi ref Sếp đã kéo tay.
    /// </summary>
    private static bool SetRefIfEmpty(SerializedObject so, string propName, Object value, bool force)
    {
        if (value == null) return false;
        SerializedProperty p = so.FindProperty(propName);
        if (p == null)
        {
            Ghi($"! Không thấy field '{propName}' trên {so.targetObject.GetType().Name} — bỏ qua (script đổi tên field?).");
            return false;
        }
        if (!force && p.objectReferenceValue != null) return false;

        p.objectReferenceValue = value;
        return true;
    }

    private static void Ghi(string dong)
    {
        _log.AppendLine(dong);
    }

    private static string DuongDanScene(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
