using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

    // ── Phạm vi dò sprite (thu hẹp — bài học từ Play test) ──────────────────
    //
    // TRƯỚC: FindAssets("t:Sprite") quét TOÀN project rồi lấy match tên đầu tiên.
    // Sau khi thêm 132 PNG nhân vật (Assets/NV_NPC/NVGAME/Processed/NV01..NV11/
    // NVxx_*.png), phép dò đó có thể vớ nhầm ảnh nhân vật làm khung card / icon tiền.
    // NAY: chỉ tìm trong các thư mục UI thật sự, loại trừ tuyệt đối thư mục nhân vật,
    // và khớp tên CHÍNH XÁC trước rồi mới tới khớp chứa.
    private static readonly string[] ThuMucUI =
    {
        "Assets/_Game/Resources",
        "Assets/Assetsgame",
        "Assets/Anh",
        "Assets/Export_Train_UI_Package/Sprites",
    };

    // Không bao giờ lấy sprite từ những nơi này (kể cả khi phải fallback quét Assets/)
    private static readonly string[] ThuMucCam =
    {
        "Assets/NV_NPC",
        "Assets/_Game/Farm/Prefabs/Tourists",
    };

    // Ảnh nhân vật đặt tên kiểu NV01_down_1 / NV11_left_3 — chặn theo mẫu tên luôn,
    // phòng khi sheet nhân vật được copy sang thư mục khác.
    private static readonly Regex MauTenNhanVat = new Regex(@"^NV\d{1,2}[_-]", RegexOptions.IgnoreCase);

    // Mốc chắc chắn do lead xác nhận: khung gỗ THẬT của project (512x512, PPU 100,
    // border 64 mỗi cạnh → Image PHẢI để Type = Sliced, không thì méo).
    private const string DuongDanKhungGoChuan = "Assets/_Game/Resources/UI_ChuyenCanh/WoodBoard_Frame.png";

    private const string RootName     = "TouristBoatPopups";
    private const string AnnounceName = "BoatAnnouncePopup";
    private const string PurchaseName = "DockPurchasePopup";
    private const string CanvasRiengName = "Canvas_TouristBoatPopup";

    // Cỡ thiết kế (reference resolution 1920x1080 — cùng CanvasScaler dự án dùng)
    private static readonly Vector2 CardAnnounceSize = new Vector2(500f, 140f);
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
        _log.Length     = 0;
        _ungVienSprite  = null; // dựng lại danh sách ứng viên mỗi lần chạy (asset có thể vừa thêm/xoá)

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
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(1f, 1f);
            card.rectTransform.pivot = new Vector2(1f, 1f);
            card.rectTransform.sizeDelta = CardAnnounceSize;
            card.rectTransform.anchoredPosition = new Vector2(-30f, -135f);
            ApSpriteKhung(card, khungGo);
            Ghi($"    + Card (Toast Báo Tàu {CardAnnounceSize.x:0}x{CardAnnounceSize.y:0})");
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
        TextMeshProUGUI title = EnsureText(content, "Title", font, 24f, new Color(1f, 0.95f, 0.82f),
                                           TextAlignmentOptions.Left, out bool tMoi);
        if (tMoi)
        {
            RectTransform trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(25f, -15f);
            trt.sizeDelta = new Vector2(-50f, 32f);
            title.fontStyle = FontStyles.Bold;
            title.text      = "⚓ Tàu Du Lịch Sắp Cập Bến!";
            Ghi("      + Title (TMP vàng sáng)");
        }

        // Nội dung
        TextMeshProUGUI body = EnsureText(content, "Body", font, 18f, new Color(1f, 0.98f, 0.92f),
                                          TextAlignmentOptions.Left, out bool bMoi);
        if (bMoi)
        {
            RectTransform brt = body.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.offsetMin = new Vector2(25f, 12f);
            brt.offsetMax = new Vector2(-130f, -48f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.text = "Sẽ cập bến sau 5 phút. Hãy chuẩn bị món ăn đón khách nhé!";
            Ghi("      + Body (TMP kem sáng)");
        }

        // Nút "Đã rõ"
        Button btn = EnsureButton(content, "Btn_DaRo", spriteNut, MauNutXacNhan,
                                  new Vector2(100f, 38f), out TextMeshProUGUI label, out bool nMoi);
        if (nMoi || btn != null)
        {
            RectTransform btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.anchoredPosition = new Vector2(-20f, 18f);
            btn.gameObject.SetActive(true);

            if (label != null)
            {
                label.text = "ĐÃ RÕ";
                label.fontSize = 16f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.28f, 0.16f, 0.05f); // Nâu đậm trên nền nút vàng/cam
            }
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
            if (BiCam(path)) continue; // đồng bộ luật loại trừ với phần dò sprite
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
    /// Sprite khung gỗ cho card popup, theo thứ tự chắc chắn dần:
    ///   1. ĐƯỜNG DẪN CHUẨN do lead xác nhận (WoodBoard_Frame.png trong UI_ChuyenCanh)
    ///      — không phụ thuộc phép dò nào cả.
    ///   2. Khớp tên CHÍNH XÁC trong các thư mục UI ("khunggo", "WoodBoard_Frame"...).
    ///   3. Khớp tên CHỨA từ khóa (wood/frame/board/panel) trong các thư mục UI.
    ///   4. Không có gì → UISprite built-in (hộp xám bo góc) + ghi chú cho Sếp.
    /// Mọi bước đều loại trừ thư mục nhân vật và tên kiểu NVxx_.
    /// </summary>
    private static Sprite TimSpriteKhungGo()
    {
        // 1. Mốc chắc chắn
        var chuan = AssetDatabase.LoadAssetAtPath<Sprite>(DuongDanKhungGoChuan);
        if (chuan != null)
        {
            Ghi($"Sprite khung: {chuan.name}  ({DuongDanKhungGoChuan})  [đường dẫn chuẩn của project]");
            CanhBaoBorderKhung(chuan, DuongDanKhungGoChuan);
            return chuan;
        }
        Ghi($"· Không thấy khung gỗ ở đường dẫn chuẩn ({DuongDanKhungGoChuan}) — chuyển sang dò theo tên.");

        // 2 + 3. Dò theo tên trong thư mục UI
        string[] tenChinhXac = { "WoodBoard_Frame", "khunggo", "khung_go", "khung", "WoodBoard", "Frame_Wood" };
        string[] tuKhoaChua  = { "khunggo", "khung", "woodboard", "wood", "frame", "board", "panel", "popup" };

        string path;
        Sprite s = TimSpriteTheoTen(tenChinhXac, tuKhoaChua, out path);
        if (s != null)
        {
            Ghi($"Sprite khung: {s.name}  ({path})");
            CanhBaoBorderKhung(s, path);
            _ghiChuArtTam.Add($"Khung card 2 popup đang dùng sprite '{s.name}' ({path}) — " +
                              "đúng khung gỗ art request thì giữ, không thì thay ở field Source Image của Card.");
            return s;
        }

        _ghiChuArtTam.Add("KHÔNG tìm thấy sprite khung gỗ trong các thư mục UI " +
                          $"({string.Join(", ", ThuMucUI)}) — Card đang dùng UISprite built-in (hộp xám bo góc). " +
                          "Art xong thì kéo vào Source Image của Card ở CẢ 2 popup.");
        Ghi("! Sprite khung: KHÔNG tìm được — dùng UI/Skin/UISprite.psd built-in.");
        return SpriteBuiltin("UI/Skin/UISprite.psd");
    }

    /// <summary>
    /// Khung 9-slice mà border = 0 thì Image Sliced vô nghĩa và ảnh sẽ bị kéo méo.
    /// Ghi chú lại để Sếp mở Sprite Editor canh border (khung chuẩn của project là 64
    /// mỗi cạnh). Không tự sửa importer ở đây — tool này chỉ dựng UI, không đụng asset.
    /// </summary>
    private static void CanhBaoBorderKhung(Sprite khung, string path)
    {
        if (khung == null || khung.border != Vector4.zero) return;

        _ghiChuArtTam.Add($"Sprite khung '{khung.name}' ({path}) đang có border 9-slice = 0 → " +
                          "Image phải vẽ Simple, phóng to sẽ méo góc. Mở Sprite Editor đặt border " +
                          "(khung gỗ chuẩn của project là 64 mỗi cạnh) rồi chạy lại tool.");
    }

    /// <summary>
    /// Icon tiền: ưu tiên lấy ĐÚNG icon HUD đang dùng (Image anh em của txtGold/
    /// txtGem trong FarmUIManager), fallback dò tên trong các thư mục UI.
    /// </summary>
    private static Sprite TimIconTien(bool laVang)
    {
        // 1. Icon thật trên HUD — chắc chắn nhất, không dính chuyện dò tên
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
                        if (MauTenNhanVat.IsMatch(img.sprite.name)) continue; // an toàn kép

                        string duongDanHud = AssetDatabase.GetAssetPath(img.sprite);
                        Ghi($"Icon {(laVang ? "vàng" : "gem")}: {img.sprite.name}  " +
                            $"({(string.IsNullOrEmpty(duongDanHud) ? "sprite trong scene/prefab" : duongDanHud)})  [lấy từ HUD]");
                        return img.sprite;
                    }
                }
            }
        }

        // 2. Dò theo tên trong thư mục UI
        string[] tenChinhXac = laVang
            ? new[] { "icon_gold", "Icon_Vang", "gold", "coin", "vang" }
            : new[] { "icon_gem", "Icon_KimCuong", "gem", "diamond", "kimcuong" };
        string[] tuKhoaChua = laVang
            ? new[] { "icon_gold", "gold", "coin", "vang", "xu" }
            : new[] { "icon_gem", "gem", "diamond", "kimcuong", "kim_cuong" };

        string path;
        Sprite s = TimSpriteTheoTen(tenChinhXac, tuKhoaChua, out path);
        if (s != null)
        {
            Ghi($"Icon {(laVang ? "vàng" : "gem")}: {s.name}  ({path})");
            return s;
        }

        _ghiChuArtTam.Add($"KHÔNG thấy icon {(laVang ? "VÀNG" : "GEM")} trong các thư mục UI — " +
                          "hàng giá của popup mua sẽ dùng chấm tròn placeholder. Kéo icon HUD thật vào field " +
                          $"'{(laVang ? "Gold Icon Sprite" : "Gem Icon Sprite")}' của DockPurchasePopupUI.");
        Ghi($"! Icon {(laVang ? "vàng" : "gem")}: KHÔNG tìm được — dùng Knob built-in.");
        return null;
    }

    /// <summary>
    /// Dò sprite trong CÁC THƯ MỤC UI (không phải toàn project):
    ///   • pass 1 — tên khớp CHÍNH XÁC, theo đúng thứ tự ưu tiên của tenChinhXac;
    ///   • pass 2 — tên CHỨA từ khóa, theo thứ tự tuKhoaChua.
    /// Loại bỏ: thư mục cấm (Assets/NV_NPC, prefab khách du lịch), thư mục Editor,
    /// và mọi ứng viên có tên kiểu NVxx_ (ảnh nhân vật).
    /// </summary>
    private static Sprite TimSpriteTheoTen(string[] tenChinhXac, string[] tuKhoaChua, out string duongDan)
    {
        duongDan = null;
        List<KeyValuePair<Sprite, string>> ungVien = LayUngVienSprite();

        // pass 1 — khớp chính xác
        for (int k = 0; k < tenChinhXac.Length; k++)
        {
            string ten = tenChinhXac[k];
            foreach (var uv in ungVien)
            {
                if (!string.Equals(uv.Key.name, ten, System.StringComparison.OrdinalIgnoreCase)) continue;
                duongDan = uv.Value;
                return uv.Key;
            }
        }

        // pass 2 — khớp chứa
        for (int k = 0; k < tuKhoaChua.Length; k++)
        {
            string key = tuKhoaChua[k].ToLowerInvariant();
            foreach (var uv in ungVien)
            {
                if (uv.Key.name.ToLowerInvariant().IndexOf(key, System.StringComparison.Ordinal) < 0) continue;
                duongDan = uv.Value;
                return uv.Key;
            }
        }

        return null;
    }

    // Danh sách ứng viên dựng 1 LẦN mỗi lần chạy tool (3 ô khung/vàng/gem dùng chung) —
    // quét AssetDatabase 3 lần là phí, và bảo đảm 3 ô cùng nhìn một tập ứng viên.
    private static List<KeyValuePair<Sprite, string>> _ungVienSprite;

    private static List<KeyValuePair<Sprite, string>> LayUngVienSprite()
    {
        if (_ungVienSprite != null) return _ungVienSprite;
        _ungVienSprite = new List<KeyValuePair<Sprite, string>>();

        // Chỉ quét thư mục UI CÓ THẬT; không có cái nào thì đành quét Assets/ nhưng
        // vẫn lọc thư mục cấm + tên nhân vật ở dưới.
        var thuMuc = new List<string>();
        foreach (string f in ThuMucUI)
            if (AssetDatabase.IsValidFolder(f)) thuMuc.Add(f);

        bool quetToanBo = thuMuc.Count == 0;
        if (quetToanBo)
        {
            thuMuc.Add("Assets");
            Ghi("! Không thấy thư mục UI nào trong danh sách ThuMucUI — phải quét cả Assets/ " +
                "(vẫn loại trừ Assets/NV_NPC và ảnh tên NVxx_). Nên cập nhật ThuMucUI trong tool.");
        }
        else
        {
            Ghi("Phạm vi dò sprite: " + string.Join(" · ", thuMuc));
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", thuMuc.ToArray());
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.Contains("/Editor/")) continue;
            if (BiCam(path)) continue;

            // Sprite có thể là sub-asset của texture (sprite sheet) — quét hết
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sp = obj as Sprite;
                if (sp == null) continue;
                if (MauTenNhanVat.IsMatch(sp.name)) continue; // ảnh nhân vật NVxx_

                _ungVienSprite.Add(new KeyValuePair<Sprite, string>(sp, path));
            }
        }

        Ghi($"Số sprite ứng viên sau khi lọc: {_ungVienSprite.Count}");
        return _ungVienSprite;
    }

    /// <summary>Đường dẫn nằm trong thư mục cấm (ảnh nhân vật / prefab khách du lịch)?</summary>
    private static bool BiCam(string path)
    {
        for (int i = 0; i < ThuMucCam.Length; i++)
        {
            if (path.StartsWith(ThuMucCam[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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

    /// <summary>
    /// Áp sprite khung cho Card. Sprite CÓ border → Type = Sliced (bắt buộc: khung gỗ
    /// chuẩn của project có border 64 mỗi cạnh, để Simple là 4 góc bị kéo méo).
    /// Sprite không border → Simple (Sliced khi border = 0 chẳng khác gì Simple).
    /// </summary>
    private static void ApSpriteKhung(Image card, Sprite khung)
    {
        card.sprite        = khung;
        card.color         = Color.white;
        card.raycastTarget = true; // card cũng chặn click xuyên

        bool coBorder = khung != null && khung.border != Vector4.zero;
        card.type = coBorder ? Image.Type.Sliced : Image.Type.Simple;
        // fillCenter để mặc định (true) — khung gỗ cần phần ruột được vẽ.
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
