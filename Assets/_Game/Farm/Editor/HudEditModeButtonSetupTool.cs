using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor Tool: Tools/Farm Game/HUD/Dựng nút Sửa (Edit Mode) dưới nút nhiệm vụ
///
/// ── VÌ SAO CẦN ──────────────────────────────────────────────────────────────
/// `EditModeManager.Update()` chỉ có MỘT cửa vào Edit Mode: phím **E**. Trong
/// `SCN_Farm.unity` thì `grep Btn_EditMode` = 0 và `grep ToggleEditMode` = 0 ⇒
/// KHÔNG nút nào gọi hàm đó ⇒ trên điện thoại người chơi không bao giờ vào được
/// Edit Mode (không di chuyển / xoá được công trình đã đặt).
///
/// Tool này dựng SẴN nút trong hierarchy (không dựng runtime nữa) để Sếp kéo chỉnh
/// cho đẹp, đặt ngay dưới nút nhiệm vụ, và mặc bộ mặt của chính nút nhiệm vụ nên
/// hàng dọc HUD nhìn đều.
///
/// ── HIERARCHY SAU KHI CHẠY ──────────────────────────────────────────────────
///   Canvas_HUD
///   └─ Left_Mission_Root                 pos (-294, -280)  size (520, 200)
///      ├─ Btn_Mission_Toggle             pos (52.5,  -52.5) size (105, 105)   ← có sẵn
///      ├─ Btn_EditMode_Toggle            pos (52.5, -172.5) size (105, 105)   ← TOOL DỰNG
///      │  └─ Label (TMP "Sửa")
///      └─ Quick_Mission_Widget           pos (300,  -52.5)  size (360, 170)   ← có sẵn
///   (-172.5 = -52.5 − 105 − 15 giãn cách; anchor/pivot copy từ nút nhiệm vụ nên
///    con số này ăn đúng hệ toạ độ của nó.)
///
/// IDEMPOTENT: đã có `Btn_EditMode_Toggle` thì KHÔNG tạo lần 2 — chỉ kiểm và wire
/// lại phần còn thiếu, KHÔNG đè vị trí/cỡ Sếp đã kéo. Undo đầy đủ (Ctrl+Z).
/// **KHÔNG tự lưu scene** — Sếp tự Ctrl+S sau khi xem.
/// </summary>
public static class HudEditModeButtonSetupTool
{
    private const string MenuDung = "Tools/Farm Game/HUD/Dựng nút Sửa (Edit Mode) dưới nút nhiệm vụ";
    private const string UndoLabel = "Dựng nút Edit Mode trên HUD";

    private const string TenNutMoi     = "Btn_EditMode_Toggle";
    private const string TenNutNhiemVu = "Btn_Mission_Toggle";
    private const string TenGocHUD     = "Left_Mission_Root";

    // Số liệu lấy từ scene thật (lead parse SCN_Farm.unity 2026-08-31)
    private static readonly Vector2 ViTriNutMoi = new Vector2(52.5f, -172.5f);
    private static readonly Vector2 CoNutMoi    = new Vector2(105f, 105f);

    // Phạm vi dò sprite (giống TouristBoatUIPopupSetupTool — tránh vớ nhầm 132 PNG nhân vật)
    private static readonly string[] ThuMucUI =
    {
        "Assets/_Game/Resources",
        "Assets/Assetsgame",
        "Assets/Anh",
    };
    private static readonly string[] ThuMucCam = { "Assets/NV_NPC", "Assets/_Game/Farm/Prefabs/Tourists" };
    private static readonly Regex MauTenNhanVat = new Regex(@"^NV\d{1,2}[_-]", RegexOptions.IgnoreCase);

    // Từ khoá sprite "sửa" — ưu tiên khớp chính xác trước, rồi khớp chứa
    private static readonly string[] TenSpriteChinhXac = { "btn_edit_mode_forge_1788194641095_transparent", "btn_edit_mode_icon", "btn_edit_mode", "icon_sua", "Icon_Sua", "edit_mode", "edit", "bua", "hammer", "setting" };
    private static readonly string[] TuKhoaSpriteChua  = { "btn_edit_mode", "edit_mode", "edit", "bua", "hammer", "wrench" };

    [MenuItem(MenuDung, false, 40)]
    private static void DungNutMenu()
    {
        DungNut(false);
    }

    /// <summary>
    /// quiet = true: không bật dialog, trả report dạng chuỗi (cùng quy ước với
    /// TouristBoatUIPopupSetupTool.SetupPopups(bool) để tool tổng gộp báo cáo).
    /// </summary>
    public static string DungNut(bool quiet)
    {
        var log = new StringBuilder();

        // ── 1. Tìm nút nhiệm vụ + gốc HUD ───────────────────────────────────
        Button nutNhiemVu = TimNutNhiemVu(out Transform gocHUD);
        if (nutNhiemVu == null || gocHUD == null)
        {
            string loi = $"LỖI: không thấy '{TenNutNhiemVu}' (hoặc '{TenGocHUD}') trong scene đang mở.\n" +
                         "Mở scene farm (SCN_Farm) rồi chạy lại menu này.";
            Debug.LogError("[HUD EditMode] " + loi);
            if (!quiet) EditorUtility.DisplayDialog("HUD — Nút Sửa", loi, "OK");
            return loi;
        }
        log.AppendLine($"Nút nhiệm vụ: {DuongDan(nutNhiemVu.transform)}");
        log.AppendLine($"Gốc HUD:      {DuongDan(gocHUD)}");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        // ── 2. Tìm hoặc tạo nút mới trực tiếp trên Canvas_HUD (để không bị trượt mất khi đóng mission) ──
        Transform canvasHUD = gocHUD.parent != null && gocHUD.parent.name.Contains("Canvas") ? gocHUD.parent : gocHUD;
        Transform cu = canvasHUD.Find(TenNutMoi);
        if (cu == null) cu = gocHUD.Find(TenNutMoi);

        bool taoMoi = cu == null;
        GameObject go;
        if (taoMoi)
        {
            go = new GameObject(TenNutMoi, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(canvasHUD, false);
            go.layer = 5; // Layer UI

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.localScale = Vector3.one;
            rt.sizeDelta        = new Vector2(105f, 105f);
            rt.anchoredPosition = new Vector2(30f, -230f);

            log.AppendLine($"+ Tạo {TenNutMoi} tại Top-Left pos (30, -230) size (105, 105) trên Canvas_HUD");
        }
        else
        {
            go = cu.gameObject;
            go.layer = 5;
            go.transform.SetParent(canvasHUD, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.localScale = Vector3.one;
            rt.sizeDelta        = new Vector2(105f, 105f);
            rt.anchoredPosition = new Vector2(30f, -230f);
            log.AppendLine($"· {TenNutMoi} đã có — chuyển lên Canvas_HUD và ghim vị trí (30, -230).");
        }

        // ── 3. Background Card: Luôn dùng sprite khung thẻ bo góc chuẩn của Game ─────
        var img = go.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(go);

        Sprite cardBgSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites/hud_bottom_tab_base.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/UI_ChuyenCanh/WoodBoard_Frame.png")
                        ?? SkinKit.BoGoc(22f);

        img.sprite = cardBgSpr;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = true;
        log.AppendLine($"  ✓ Image Background: {cardBgSpr.name} (Sliced 9-slice)");

        // ── 4. Icon Búa / Forge Sửa Chữa (Kích thước 68x68 ở chính giữa) ─────────────
        Transform iconTf = go.transform.Find("Icon_Forge");
        if (iconTf == null)
        {
            GameObject iconGo = new GameObject("Icon_Forge", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
            iconGo.transform.SetParent(go.transform, false);
            iconTf = iconGo.transform;
        }

        var iconRt = (RectTransform)iconTf;
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(68f, 68f);
        iconRt.anchoredPosition = new Vector2(0f, 4f);

        var iconImg = iconTf.GetComponent<Image>();
        if (iconImg == null) iconImg = iconTf.gameObject.AddComponent<Image>();

        Sprite hammerSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/btn_EditMode/btn_edit_mode_forge_1788194641095_transparent.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/btn_EditMode/btn_edit_mode_icon.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/iconbuabua.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/btn_edit_mode_icon.png");

        iconImg.sprite = hammerSpr;
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        log.AppendLine($"  ✓ Icon Sửa: {(hammerSpr != null ? hammerSpr.name : "null")}");

        // ── 5. Button: copy cảm giác bấm, KHÔNG copy onClick ────────────────
        var btn = go.GetComponent<Button>();
        if (btn == null)
        {
            btn = Undo.AddComponent<Button>(go);
            btn.targetGraphic = img;

            btn.transition  = nutNhiemVu.transition;
            btn.colors      = nutNhiemVu.colors;
            btn.spriteState = nutNhiemVu.spriteState;
            log.AppendLine("  + Button (copy transition/colors/spriteState, KHÔNG copy onClick)");
        }
        else
        {
            btn.targetGraphic = img;
            log.AppendLine("  · Button đã có.");
        }

        // ── 6. Label TMP "Sửa" ──────────────────────────────────────────────
        Transform labelCu = go.transform.Find("Label");
        if (labelCu == null)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            labelGo.transform.SetParent(go.transform, false);
            labelCu = labelGo.transform;
        }

        var lrt = (RectTransform)labelCu;
        lrt.anchorMin = new Vector2(0.5f, 0f);
        lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(90f, 26f);
        lrt.anchoredPosition = new Vector2(0f, 16f);

        var tmp = labelCu.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = labelCu.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text          = "SỬA";
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = 16f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = new Color32(92, 52, 18, 255); // Nâu gỗ đồng bộ
        tmp.raycastTarget = false;

        if (SkinKit.FontVo != null) tmp.font = SkinKit.FontVo;
        log.AppendLine("  ✓ Label \"SỬA\" (Font đẹp, màu nâu gỗ sắc nét)");

        // ── 6. MobileEditModeButton + wire nutCoSan ─────────────────────────
        var comp = go.GetComponent<MobileEditModeButton>();
        if (comp == null)
        {
            comp = Undo.AddComponent<MobileEditModeButton>(go);
            log.AppendLine("  + component MobileEditModeButton");
        }

        var so = new SerializedObject(comp);
        SerializedProperty pNut = so.FindProperty("nutCoSan");
        if (pNut != null)
        {
            if (pNut.objectReferenceValue == null)
            {
                pNut.objectReferenceValue = btn;
                so.ApplyModifiedProperties();
                log.AppendLine("  ✓ Wire nutCoSan = Button vừa dựng (component KHÔNG tự dựng nút runtime nữa)");
            }
            else
            {
                log.AppendLine("  · nutCoSan đã được gán — giữ nguyên.");
            }
        }
        else
        {
            log.AppendLine("  ! Không thấy field 'nutCoSan' trên MobileEditModeButton — kéo tay trong Inspector.");
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        EditorUtility.SetDirty(go);

        if (!quiet)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        string duongDanDayDu = DuongDan(go.transform);
        log.AppendLine();
        log.AppendLine("ĐƯỜNG DẪN HIERARCHY: " + duongDanDayDu);

        string tomTat =
            (taoMoi ? "Đã dựng nút Sửa (Edit Mode).\n\n" : "Nút Sửa đã có sẵn — chỉ kiểm/wire lại.\n\n") +
            "Tìm ở đây trong Hierarchy:\n" + duongDanDayDu + "\n\n" +
            "Vị trí: ngay DƯỚI Btn_Mission_Toggle, cùng cỡ 105x105.\n" +
            "Kéo chỉnh cho đẹp thoải mái — chạy lại menu này sẽ KHÔNG đè vị trí bạn đã kéo.\n\n" +
            "CẦN BẠN LÀM:\n" +
            "1) Ctrl+S lưu scene (tool KHÔNG tự lưu).\n" +
            "2) Bấm Play → bấm nút Sửa → lưới Edit Mode hiện, nhãn đổi thành \"Xong\".\n" +
            "3) Muốn icon riêng: thay Source Image của nút.\n\n" +
            "Chi tiết đã in ra Console. (Ctrl+Z hoàn tác.)";

        Debug.Log("[HUD EditMode] Dựng nút Sửa:\n" + log);
        if (!quiet) EditorUtility.DisplayDialog("HUD — Nút Sửa (Edit Mode)", tomTat, "OK");

        return log.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm Btn_Mission_Toggle (kể cả object đang tắt) + gốc HUD chứa nó.
    /// Ưu tiên nút nằm dưới Left_Mission_Root; không thấy thì lấy nút đầu tiên
    /// đúng tên và dùng chính cha của nó làm gốc.
    /// </summary>
    private static Button TimNutNhiemVu(out Transform gocHUD)
    {
        gocHUD = null;
        Button duPhong = null;
        Transform chaDuPhong = null;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.name != TenNutNhiemVu) continue;

            var b = t.GetComponent<Button>();
            if (b == null) continue;

            if (t.parent != null && t.parent.name == TenGocHUD)
            {
                gocHUD = t.parent;
                return b;
            }

            if (duPhong == null) { duPhong = b; chaDuPhong = t.parent; }
        }

        gocHUD = chaDuPhong;
        return duPhong;
    }

    /// <summary>Sprite cho nút "Sửa": khớp tên chính xác trước, rồi khớp chứa; chỉ trong thư mục UI.</summary>
    private static Sprite TimSpriteSua(out string duongDan)
    {
        duongDan = null;

        var thuMuc = new List<string>();
        foreach (string f in ThuMucUI)
            if (AssetDatabase.IsValidFolder(f)) thuMuc.Add(f);
        if (thuMuc.Count == 0) return null;

        var ungVien = new List<KeyValuePair<Sprite, string>>();
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", thuMuc.ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.Contains("/Editor/")) continue;
            if (BiCam(path)) continue;

            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sp = obj as Sprite;
                if (sp == null) continue;
                if (MauTenNhanVat.IsMatch(sp.name)) continue;
                ungVien.Add(new KeyValuePair<Sprite, string>(sp, path));
            }
        }

        for (int k = 0; k < TenSpriteChinhXac.Length; k++)
            foreach (var uv in ungVien)
                if (string.Equals(uv.Key.name, TenSpriteChinhXac[k], System.StringComparison.OrdinalIgnoreCase))
                { duongDan = uv.Value; return uv.Key; }

        for (int k = 0; k < TuKhoaSpriteChua.Length; k++)
        {
            string key = TuKhoaSpriteChua[k].ToLowerInvariant();
            foreach (var uv in ungVien)
                if (uv.Key.name.ToLowerInvariant().Contains(key))
                { duongDan = uv.Value; return uv.Key; }
        }

        return null;
    }

    private static bool BiCam(string path)
    {
        for (int i = 0; i < ThuMucCam.Length; i++)
            if (path.StartsWith(ThuMucCam[i], System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Font TMP của dự án (ưu tiên font trong Assets/, không phải font mặc định TMP).</summary>
    private static TMP_FontAsset TimFontTMP()
    {
        TMP_FontAsset batKy = null;
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (BiCam(path)) continue;
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) continue;
            if (batKy == null) batKy = f;
            if (path.StartsWith("Assets/") && !path.Contains("TextMesh Pro/Resources")) return f;
        }
        return batKy;
    }

    private static string DuongDan(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
