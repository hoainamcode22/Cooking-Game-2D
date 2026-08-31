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
    private static readonly string[] TenSpriteChinhXac = { "btn_edit_mode_icon", "btn_edit_mode_forge_1788194641095_transparent", "btn_edit_mode", "icon_sua", "Icon_Sua", "sua", "edit", "bua", "hammer", "setting" };
    private static readonly string[] TuKhoaSpriteChua  = { "btn_edit_mode", "edit_mode", "sua", "edit", "bua", "hammer", "setting", "wrench" };

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

        // ── 2. Tìm hoặc tạo nút mới (idempotent) ────────────────────────────
        Transform cu = gocHUD.Find(TenNutMoi);
        bool taoMoi = cu == null;

        GameObject go;
        if (taoMoi)
        {
            go = new GameObject(TenNutMoi, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(gocHUD, false);

            // Đặt ngay SAU nút nhiệm vụ trong hierarchy cho dễ nhìn
            int idx = nutNhiemVu.transform.GetSiblingIndex();
            go.transform.SetSiblingIndex(Mathf.Min(idx + 1, gocHUD.childCount - 1));

            var rtSrc = nutNhiemVu.GetComponent<RectTransform>();
            var rt    = (RectTransform)go.transform;
            // Copy anchor/pivot của nút nhiệm vụ TRƯỚC khi đặt toạ độ — nếu không,
            // con số (52.5, -172.5) sẽ ăn theo hệ toạ độ khác và nút bay đi đâu mất.
            if (rtSrc != null)
            {
                rt.anchorMin = rtSrc.anchorMin;
                rt.anchorMax = rtSrc.anchorMax;
                rt.pivot     = rtSrc.pivot;
                rt.localScale = rtSrc.localScale;
            }
            rt.sizeDelta        = CoNutMoi;
            rt.anchoredPosition = ViTriNutMoi;

            log.AppendLine($"+ Tạo {TenNutMoi} tại pos {ViTriNutMoi} size {CoNutMoi} " +
                           "(anchor/pivot copy từ nút nhiệm vụ)");
        }
        else
        {
            go = cu.gameObject;
            log.AppendLine($"· {TenNutMoi} đã có — GIỮ NGUYÊN vị trí/cỡ bạn đã kéo, chỉ kiểm phần thiếu.");
        }

        // ── 3. Image: mặc bộ mặt của nút nhiệm vụ, ưu tiên sprite "sửa" ─────
        var imgSrc = nutNhiemVu.GetComponent<Image>();
        var img    = go.GetComponent<Image>();
        if (img == null)
        {
            img = Undo.AddComponent<Image>(go);

            if (imgSrc != null)
            {
                // CopySerialized lấy TRỌN sprite + type + color + material +
                // pixelsPerUnitMultiplier... của nút nhiệm vụ (không kéo theo listener
                // nào vì đây là Image, không phải Button).
                EditorUtility.CopySerialized(imgSrc, img);
                log.AppendLine("  + Image: copy nguyên bộ mặt từ nút nhiệm vụ " +
                               $"(sprite '{(imgSrc.sprite != null ? imgSrc.sprite.name : "null")}', type {imgSrc.type})");
            }
            else
            {
                img.color = new Color(0.42f, 0.72f, 0.30f, 0.95f);
                log.AppendLine("  ! Nút nhiệm vụ không có Image — dùng màu xanh lá đặc.");
            }

            // Sprite riêng cho "Sửa" nếu tìm được (nổi bật hơn là dùng lại icon nhiệm vụ)
            Sprite riengCuaSua = TimSpriteSua(out string duongDanSprite);
            if (riengCuaSua != null)
            {
                img.sprite = riengCuaSua;
                if (img.sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
                log.AppendLine($"  + Sprite riêng cho nút Sửa: {riengCuaSua.name}  ({duongDanSprite})");
            }
            else
            {
                log.AppendLine("  · Không tìm được sprite tên chứa sua/edit/bua/hammer/setting " +
                               "trong thư mục UI — dùng lại sprite của nút nhiệm vụ (art xong thì thay ở Source Image).");
            }
            img.raycastTarget = true;
        }
        else
        {
            log.AppendLine("  · Image đã có — không đè.");
        }

        // ── 4. Button: copy cảm giác bấm, KHÔNG copy onClick ────────────────
        var btn = go.GetComponent<Button>();
        if (btn == null)
        {
            btn = Undo.AddComponent<Button>(go);
            btn.targetGraphic = img;

            // Chỉ copy 3 thứ về "cảm giác bấm". CỐ Ý không EditorUtility.CopySerialized
            // trên Button: nó sẽ copy luôn danh sách onClick của nút nhiệm vụ ⇒ bấm
            // nút Sửa lại mở bảng nhiệm vụ.
            btn.transition  = nutNhiemVu.transition;
            btn.colors      = nutNhiemVu.colors;
            btn.spriteState = nutNhiemVu.spriteState;
            log.AppendLine("  + Button (copy transition/colors/spriteState, KHÔNG copy onClick)");
        }
        else
        {
            log.AppendLine("  · Button đã có — không đè.");
        }

        // ── 5. Label TMP "Sửa" ──────────────────────────────────────────────
        Transform labelCu = go.transform.Find("Label");
        if (labelCu == null)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            labelGo.transform.SetParent(go.transform, false);

            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Sửa";
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // chữ không được nuốt click của nút

            TMP_Text nhanNguon = nutNhiemVu.GetComponentInChildren<TMP_Text>(true);
            if (nhanNguon != null)
            {
                tmp.font      = nhanNguon.font;
                tmp.fontSize  = nhanNguon.fontSize;
                tmp.color     = nhanNguon.color;
                tmp.fontStyle = nhanNguon.fontStyle;
                log.AppendLine($"  + Label \"Sửa\" (font/cỡ/màu copy từ label nút nhiệm vụ: {nhanNguon.name})");
            }
            else
            {
                TMP_FontAsset font = TimFontTMP();
                if (font != null) tmp.font = font;
                tmp.fontSize = CoNutMoi.y * 0.30f; // 105 → ~32
                tmp.color    = Color.white;
                tmp.fontStyle = FontStyles.Bold;
                log.AppendLine("  + Label \"Sửa\" (nút nhiệm vụ không có label TMP — dùng font TMP của dự án" +
                               (font != null ? $": {font.name})" : ", không thấy font nào)"));
            }
        }
        else
        {
            var tmpCu = labelCu.GetComponent<TMP_Text>();
            if (tmpCu != null && string.IsNullOrEmpty(tmpCu.text)) tmpCu.text = "Sửa";
            log.AppendLine("  · Label đã có — không đè.");
        }

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
