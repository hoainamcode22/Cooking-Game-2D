#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TUTORIAL STUDIO — bộ 5 nút để Sếp DỌN + CHỈNH TAY cây <c>Tutorial_Canvas</c> trong SCN_Farm.unity.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// HIỆN TRẠNG ĐO ĐƯỢC (scene lưu 05/09 13:03):
///   Tutorial_Canvas (order 250)
///     ├ Dim_Background            (UnmaskRaycastFilter — KHÔNG đụng tới)
///     ├ NPC_Dialog_Popup          ← hộp thoại ông già CŨ, Sếp duyệt XOÁ HẲN
///     ├ Cloud_Panel               (giữ — dùng cho intro)
///     ├ TutorialV2_Dialogue / Tutorial_GuideBoard
///     └ Canvas_TutorialHand (order 440)
///          └ Tutorial_Hands → Tutorial_Hands → Tutorial_Hands   ← LỒNG THỪA 3 tầng
///               (Hand_Drag_Seed xuất hiện 3 bản, chỉ 1 bản được TutorialDragHintAnimator._hand trỏ tới)
///
/// 5 MỤC MENU (bấm theo đúng số thứ tự):
///   1. Báo cáo cây (DRY RUN)  — chỉ in Console, KHÔNG ghi gì.
///   2. Dọn cây + xoá hộp thoại ông già (APPLY) — 1 nhóm Undo duy nhất (Ctrl+Z là hoàn tác hết).
///   3. BẬT HẾT để chỉnh tay (EDIT MODE) — lưu trạng thái activeSelf vào EditorPrefs rồi bật hết.
///   4. TRẢ VỀ trạng thái chạy (PLAY MODE) — khôi phục đúng bản đã lưu ở mục 3.
///   5. Kiểm tra sau khi chỉnh (DRY RUN) — chạy lại mục 1 + soi object "phải tắt" mà còn bật.
///
/// AN TOÀN:
///   • KHÔNG BAO GIỜ tự lưu scene → Sếp bấm Ctrl+S. Sai thì Ctrl+Z.
///   • Trước khi xoá bất kỳ object nào: quét TOÀN BỘ MonoBehaviour trong scene, mọi
///     SerializedProperty kiểu ObjectReference (<see cref="CoAiTroToi"/>). Còn ai trỏ tới ⇒ KHÔNG xoá,
///     chỉ log cảnh báo kèm đường dẫn script đang giữ.
///   • KHÔNG đụng <c>Dim_Background</c> (UnmaskRaycastFilter — nó phải ở lại order 250, đưa lên là nuốt
///     raycast của khay hạt), KHÔNG đụng <c>Cloud_Panel</c>, KHÔNG đụng nội dung
///     <c>TutorialV2_Dialogue</c> / <c>Tutorial_GuideBoard</c>, KHÔNG đụng asset ngoài scene.
///   • KHÔNG [InitializeOnLoad], KHÔNG delayCall → chỉ chạy khi Sếp bấm menu.
/// </summary>
public static class TutorialStudioTool
{
    private const string MENU_ROOT = "Tools/Farm/Tutorial Studio/";
    private const string MENU_1 = MENU_ROOT + "1. Bao cao cay Tutorial (DRY RUN)";
    private const string MENU_2 = MENU_ROOT + "2. Don cay + Xoa hop thoai ong gia (APPLY)";
    private const string MENU_3 = MENU_ROOT + "3. BAT HET de chinh tay (EDIT MODE)";
    private const string MENU_4 = MENU_ROOT + "4. TRA VE trang thai chay (PLAY MODE)";
    private const string MENU_5 = MENU_ROOT + "5. Kiem tra sau khi chinh (DRY RUN)";

    private const string TEN_CANVAS      = "Tutorial_Canvas";
    private const string TEN_CANVAS_TAY  = "Canvas_TutorialHand";
    private const string TEN_BOC_TAY     = "Tutorial_Hands";
    private const string TEN_POPUP_ONG   = "NPC_Dialog_Popup";
    private const string TEN_HAND_DRAG   = "Hand_Drag_Seed";

    /// <summary>Field serialize của TutorialManager mà tool đọc/ghi (đã đối chiếu TutorialManager.cs:82-132).</summary>
    private const string F_STEPS      = "_steps";
    private const string F_NPC_POPUP  = "_npcDialogPopup";
    private const string F_HAND       = "_handPointer";
    private const string F_CLOUD      = "_cloudPanel";
    private const string F_V2CARD     = "_v2Card";
    private const string F_GUIDEBOARD = "_guideBoardUI";
    private const string F_DIM        = "_dimBackground";
    /// <summary>Field <c>_hand</c> của TutorialDragHintAnimator (:23) và TutorialActionHandGuide (:7).</summary>
    private const string F_HAND_ANIM  = "_hand";

    /// <summary>Object "quan trọng" — nếu KHÔNG script nào trỏ tới thì đánh dấu 🔴 (rác nghi ngờ).</summary>
    private static readonly string[] TEN_QUAN_TRONG =
    {
        TEN_POPUP_ONG, "Cloud_Panel", "TutorialV2_Dialogue", "Tutorial_GuideBoard", "Dim_Background",
    };

    /// <summary>Nhóm "PHẢI TẮT lúc chạy" — active=1 trong scene là bất thường (tay/hộp thoại lộ ra khi vào game).</summary>
    private static readonly string[] PHAI_TAT_LUC_CHAY =
    {
        "Dim_Background", "TutorialV2_Dialogue", "Tutorial_GuideBoard", TEN_POPUP_ONG,
    };

    /// <summary>Bộ mặc định an toàn cho mục 4 khi CHƯA có bản lưu EditorPrefs.</summary>
    private static readonly string[] MAC_DINH_BAT = { "Cloud_Panel", TEN_CANVAS_TAY, TEN_BOC_TAY };

    private const string KHOA_PREFS = "FarmTutorialStudio.TrangThaiActive.";

    // ═══════════════════════════════════════════════════════════════════════════
    //  MENU
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_1, false, 400)]
    [MenuItem("Tools/Farm Game/Tutorial Studio/1. Bao cao cay Tutorial (DRY RUN)", false, 400)]
    public static void Menu1_BaoCao() => BaoCaoCay(soiKyActive: false);

    [MenuItem(MENU_2, false, 401)]
    [MenuItem("Tools/Farm Game/Tutorial Studio/2. Don cay + Xoa hop thoai ong gia (APPLY)", false, 401)]
    public static void Menu2_DonCay()
    {
        if (!EditorUtility.DisplayDialog(
                "Tutorial Studio — dọn cây (APPLY)",
                "Tool sẽ GHI VÀO SCENE:\n" +
                "  • XOÁ hẳn 'NPC_Dialog_Popup' (hộp thoại ông già cũ) và gỡ field _npcDialogPopup về null.\n" +
                "  • Dẹp các lớp 'Tutorial_Hands' lồng thừa, dời tay thật lên 1 lớp duy nhất.\n" +
                "  • XOÁ các bản 'Hand_Drag_Seed' thừa (bản KHÔNG được script trỏ tới).\n\n" +
                "Object nào còn script khác trỏ tới thì tool KHÔNG xoá, chỉ cảnh báo.\n" +
                "Có Undo (Ctrl+Z 1 lần là về như cũ). Tool KHÔNG tự lưu scene.",
                "Chạy đi", "Thôi"))
            return;

        DonCay();
    }

    [MenuItem("Tools/Farm Game/★ BẬT HẾT ĐỂ CHỈNH TAY (EDIT MODE)", false, -98)]
    [MenuItem(MENU_3, false, 402)]
    [MenuItem("Tools/Farm Game/Tutorial Studio/3. BAT HET de chinh tay (EDIT MODE)", false, 402)]
    public static void Menu3_BatHet() => BatHetDeChinh();

    [MenuItem("Tools/Farm Game/★ TẮT HẾT TRẢ VỀ BAN ĐẦU (PLAY MODE)", false, -97)]
    [MenuItem(MENU_4, false, 403)]
    [MenuItem("Tools/Farm Game/Tutorial Studio/4. TRA VE trang thai chay (PLAY MODE)", false, 403)]
    public static void Menu4_TraVe()
    {
        if (!EditorUtility.DisplayDialog(
                "Tutorial Studio — trả về trạng thái chạy",
                "Tool sẽ đặt lại activeSelf của mọi object dưới 'Tutorial_Canvas' theo bản đã lưu ở mục 3.\n" +
                "Nếu chưa có bản lưu, tool áp BỘ MẶC ĐỊNH AN TOÀN (tắt Dim/Dialogue/GuideBoard/Hand_*, " +
                "bật Cloud_Panel + lớp tay).\n\nCó Undo (Ctrl+Z). Tool KHÔNG tự lưu scene.",
                "Trả về", "Thôi"))
            return;

        TraVeTrangThaiChay();
    }

    [MenuItem(MENU_5, false, 404)]
    [MenuItem("Tools/Farm Game/Tutorial Studio/5. Kiem tra sau khi chinh (DRY RUN)", false, 404)]
    public static void Menu5_KiemTra() => BaoCaoCay(soiKyActive: true);

    // ═══════════════════════════════════════════════════════════════════════════
    //  MỤC 1 + 5 — BÁO CÁO CÂY
    // ═══════════════════════════════════════════════════════════════════════════

    private static void BaoCaoCay(bool soiKyActive)
    {
        GameObject canvas = TimTheoTenToanScene(TEN_CANVAS);
        if (canvas == null)
        {
            Debug.LogError($"[TutorialStudio] Không tìm thấy '{TEN_CANVAS}' trong scene đang mở (đã tìm cả object inactive). " +
                           "Hãy mở Assets/_Game/Scenes/SCN_Farm.unity rồi chạy lại.");
            return;
        }

        HashSet<GameObject> duocTroToi = ThuThapObjectDuocTroToi(out List<string> nhatKyTro);

        var sb = new StringBuilder();
        sb.AppendLine("═════════════ TUTORIAL STUDIO — BÁO CÁO CÂY ═════════════");
        sb.AppendLine(soiKyActive
            ? "Chế độ : 5. KIỂM TRA SAU KHI CHỈNH (DRY RUN — không ghi gì)"
            : "Chế độ : 1. BÁO CÁO (DRY RUN — không ghi gì)");
        sb.AppendLine($"Scene  : {canvas.scene.name}");
        sb.AppendLine("─── Field script đang trỏ tới (đọc qua SerializedObject) ───");
        foreach (string d in nhatKyTro) sb.AppendLine("  " + d);
        sb.AppendLine("─── Cây Tutorial_Canvas ───");
        sb.AppendLine("  (tên · active · component · order · anchoredPosition/sizeDelta)");

        int soCanhBao = 0;
        var conBatSai = new List<string>();
        InCay(canvas.transform, 0, sb, duocTroToi, ref soCanhBao, conBatSai);

        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine($"Tổng cộng {soCanhBao} dấu 🔴 bất thường.");

        if (soiKyActive)
        {
            sb.AppendLine("─── SOI KỸ: object thuộc nhóm 'phải tắt lúc chạy' mà còn active=1 ───");
            if (conBatSai.Count == 0)
            {
                sb.AppendLine("  ✔ Sạch — không còn object nào phải tắt mà đang bật.");
            }
            else
            {
                foreach (string s in conBatSai) sb.AppendLine("  🔴 " + s);
                sb.AppendLine($"  ⇒ {conBatSai.Count} object cần TẮT. Bấm mục '4. TRA VE trang thai chay (PLAY MODE)'.");
            }
        }

        sb.AppendLine("Ghi chú: DRY RUN không ghi bất cứ thứ gì vào scene.");
        sb.AppendLine("══════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    /// <summary>In đệ quy 1 nhánh cây + gắn dấu 🔴 cho các bất thường.</summary>
    private static void InCay(Transform t, int sau, StringBuilder sb, HashSet<GameObject> duocTroToi,
                              ref int soCanhBao, List<string> conBatSai)
    {
        GameObject go = t.gameObject;
        string thut = new string(' ', 2 + sau * 3);

        // Component chính (bỏ Transform/RectTransform vì đã in riêng phần rect)
        var tenComp = new List<string>();
        bool thieuScript = false;
        foreach (Component c in go.GetComponents<Component>())
        {
            if (c == null) { thieuScript = true; continue; }
            if (c is Transform) continue;
            tenComp.Add(c.GetType().Name);
        }
        string moTaComp = tenComp.Count > 0 ? string.Join(", ", tenComp) : "—";

        // Canvas order
        var cv = go.GetComponent<Canvas>();
        string moTaOrder = cv != null ? $" · order={cv.sortingOrder}{(cv.overrideSorting ? " (override)" : "")}" : "";

        // RectTransform
        var rt = t as RectTransform;
        string moTaRect = rt != null ? $" · pos{Fmt(rt.anchoredPosition)} size{Fmt(rt.sizeDelta)}" : "";

        // ── Bất thường ──
        var co = new List<string>();
        if (thieuScript) { co.Add("MISSING SCRIPT"); soCanhBao++; }

        if (TrungTenVoiToTien(t)) { co.Add($"TRÙNG TÊN LỒNG NHAU (tổ tiên cũng tên '{t.name}')"); soCanhBao++; }

        bool quanTrong = System.Array.IndexOf(TEN_QUAN_TRONG, t.name) >= 0 || t.name.StartsWith("Hand_");
        if (quanTrong && !duocTroToi.Contains(go))
        {
            co.Add("KHÔNG script nào trỏ tới (nghi rác / bản thừa)");
            soCanhBao++;
        }

        bool phaiTat = System.Array.IndexOf(PHAI_TAT_LUC_CHAY, t.name) >= 0 || t.name.StartsWith("Hand_");
        if (phaiTat && go.activeSelf)
        {
            co.Add("ĐANG BẬT nhưng phải TẮT lúc chạy");
            soCanhBao++;
            conBatSai.Add($"{DuongDan(t)} (active=1)");
        }

        string moTaCanhBao = co.Count > 0 ? "   🔴 " + string.Join(" | ", co) : "";

        sb.AppendLine($"{thut}{(sau == 0 ? "" : "└ ")}{t.name} · active={(go.activeSelf ? 1 : 0)} · {moTaComp}{moTaOrder}{moTaRect}{moTaCanhBao}");

        for (int i = 0; i < t.childCount; i++)
            InCay(t.GetChild(i), sau + 1, sb, duocTroToi, ref soCanhBao, conBatSai);
    }

    /// <summary>Đọc 8 field serialize (TutorialManager ×6, 2 animator ×1) → tập GameObject "được trỏ tới".</summary>
    private static HashSet<GameObject> ThuThapObjectDuocTroToi(out List<string> nhatKy)
    {
        var ket = new HashSet<GameObject>();
        nhatKy = new List<string>();

        var mgr = UnityEngine.Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            nhatKy.Add("⚠ Không thấy TutorialManager trong scene → không đối chiếu được field nào.");
        }
        else
        {
            var so = new SerializedObject(mgr);
            string[] fields = { F_NPC_POPUP, F_HAND, F_CLOUD, F_V2CARD, F_GUIDEBOARD, F_DIM };
            foreach (string f in fields)
            {
                SerializedProperty p = so.FindProperty(f);
                if (p == null) { nhatKy.Add($"⚠ TutorialManager KHÔNG có field '{f}' (tên field đã đổi?)"); continue; }
                GameObject go = LayGameObject(p.objectReferenceValue);
                if (go != null) ket.Add(go);
                nhatKy.Add($"TutorialManager.{f} = {(go != null ? DuongDan(go.transform) : "null")}");
            }

            SerializedProperty pSteps = so.FindProperty(F_STEPS);
            nhatKy.Add(pSteps != null && pSteps.isArray
                ? $"TutorialManager.{F_STEPS} = {pSteps.arraySize} bước"
                : $"⚠ TutorialManager KHÔNG đọc được field '{F_STEPS}'");
        }

        foreach (var a in UnityEngine.Object.FindObjectsByType<TutorialDragHintAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject go = DocFieldHand(a, nhatKy, "TutorialDragHintAnimator");
            if (go != null) ket.Add(go);
        }
        foreach (var a in UnityEngine.Object.FindObjectsByType<TutorialActionHandGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject go = DocFieldHand(a, nhatKy, "TutorialActionHandGuide");
            if (go != null) ket.Add(go);
        }

        return ket;
    }

    private static GameObject DocFieldHand(Component comp, List<string> nhatKy, string nhan)
    {
        if (comp == null) return null;
        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(F_HAND_ANIM);
        if (p == null)
        {
            nhatKy.Add($"⚠ {nhan} KHÔNG có field '{F_HAND_ANIM}'");
            return null;
        }
        GameObject go = LayGameObject(p.objectReferenceValue);
        nhatKy.Add($"{nhan}.{F_HAND_ANIM} = {(go != null ? DuongDan(go.transform) : "null")}");
        return go;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MỤC 2 — DỌN CÂY + XOÁ HỘP THOẠI ÔNG GIÀ
    // ═══════════════════════════════════════════════════════════════════════════

    public static void DonCay()
    {
        GameObject canvas = TimTheoTenToanScene(TEN_CANVAS);
        if (canvas == null)
        {
            Debug.LogError($"[TutorialStudio] Không tìm thấy '{TEN_CANVAS}' trong scene đang mở. Mở SCN_Farm.unity rồi chạy lại.");
            return;
        }

        XayBangThamChieu();

        var sb = new StringBuilder();
        sb.AppendLine("═════════════ TUTORIAL STUDIO — DỌN CÂY (APPLY) ═════════════");
        sb.AppendLine($"Scene : {canvas.scene.name}");

        int nhom = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tutorial Studio - don cay");

        int daXoa = 0, daDoi = 0;

        daXoa += XoaHopThoaiOngGia(canvas, sb);
        DepLongTay(canvas, sb, ref daXoa, ref daDoi);

        Undo.CollapseUndoOperations(nhom);

        Scene s = canvas.scene;
        if (s.IsValid() && s.isLoaded) EditorSceneManager.MarkSceneDirty(s);

        sb.AppendLine("─────────────────────────────────────────────────────────────");
        sb.AppendLine($"TỔNG KẾT: đã xoá {daXoa}, đã dời {daDoi}.");
        sb.AppendLine(">>> Scene CHƯA lưu — nhớ Ctrl+S. Sai thì Ctrl+Z (1 lần là hoàn tác cả nhóm).");
        sb.AppendLine("═════════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());

        _bangThamChieu = null;
    }

    /// <summary>Gỡ field _npcDialogPopup về null rồi XOÁ hẳn NPC_Dialog_Popup. Trả về số object đã xoá.</summary>
    private static int XoaHopThoaiOngGia(GameObject canvas, StringBuilder sb)
    {
        sb.AppendLine("─── 1. Hộp thoại ông già cũ ───");

        GameObject popup = TimConTheoTen(canvas.transform, TEN_POPUP_ONG);
        if (popup == null)
        {
            sb.AppendLine($"  · Không còn '{TEN_POPUP_ONG}' dưới {TEN_CANVAS} → bỏ qua (chắc đã xoá lần trước).");
            return 0;
        }

        // Gỡ mọi TutorialManager._npcDialogPopup đang trỏ vào nó → tránh missing reference sau khi xoá.
        foreach (var mgr in UnityEngine.Object.FindObjectsByType<TutorialManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(mgr);
            SerializedProperty p = so.FindProperty(F_NPC_POPUP);
            if (p == null)
            {
                sb.AppendLine($"  ⚠ {mgr.name}: không có field '{F_NPC_POPUP}' → bỏ qua.");
                continue;
            }
            if (LayGameObject(p.objectReferenceValue) != popup) continue;

            p.objectReferenceValue = null;
            so.ApplyModifiedProperties();   // tự ghi Undo
            sb.AppendLine($"  ✔ Đã set TutorialManager.{F_NPC_POPUP} = null (trước khi xoá).");
        }

        // Bảng tham chiếu được dựng TRƯỚC khi gỡ field ⇒ dựng lại cho đúng hiện trạng.
        XayBangThamChieu(batBuocDungLai: true);

        if (CoAiTroToi(popup, out string lyDo))
        {
            sb.AppendLine($"  🔴 KHÔNG XOÁ '{TEN_POPUP_ONG}': còn script khác trỏ tới → {lyDo}");
            sb.AppendLine("     Hãy gỡ tham chiếu đó trước rồi chạy lại mục 2 (xoá lúc còn tham chiếu sẽ sinh Missing).");
            return 0;
        }

        sb.AppendLine($"  − XOÁ '{DuongDan(popup.transform)}' (kèm NPC_Background / NPC_Portrait / NPC_Text).");
        Undo.DestroyObjectImmediate(popup);
        return 1;
    }

    /// <summary>Dẹp các lớp Tutorial_Hands lồng nhau: dời tay thật lên lớp gốc, xoá wrapper rỗng + bản Hand_Drag_Seed thừa.</summary>
    private static void DepLongTay(GameObject canvas, StringBuilder sb, ref int daXoa, ref int daDoi)
    {
        sb.AppendLine("─── 2. Dẹp lớp 'Tutorial_Hands' lồng thừa ───");

        GameObject canvasTay = TimConTheoTen(canvas.transform, TEN_CANVAS_TAY);
        if (canvasTay == null)
        {
            sb.AppendLine($"  ⚠ Không thấy '{TEN_CANVAS_TAY}' dưới {TEN_CANVAS} → bỏ qua toàn bộ phần tay.");
            return;
        }

        Transform goc = null;
        for (int i = 0; i < canvasTay.transform.childCount; i++)
        {
            Transform c = canvasTay.transform.GetChild(i);
            if (c.name == TEN_BOC_TAY) { goc = c; break; }
        }
        if (goc == null)
        {
            sb.AppendLine($"  ⚠ '{TEN_CANVAS_TAY}' không có con trực tiếp tên '{TEN_BOC_TAY}' → bỏ qua phần tay.");
            return;
        }
        sb.AppendLine($"  Lớp GIỮ LẠI: {DuongDan(goc)}");

        // ── 2a. Xoá các bản Hand_Drag_Seed THỪA (bản KHÔNG được TutorialDragHintAnimator._hand trỏ tới) ──
        GameObject dragDuocTro = null;
        int soAnimator = 0;
        foreach (var a in UnityEngine.Object.FindObjectsByType<TutorialDragHintAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            soAnimator++;
            var so = new SerializedObject(a);
            SerializedProperty p = so.FindProperty(F_HAND_ANIM);
            GameObject go = p != null ? LayGameObject(p.objectReferenceValue) : null;
            if (go != null && go.name == TEN_HAND_DRAG) dragDuocTro = go;
        }

        if (dragDuocTro == null)
        {
            sb.AppendLine($"  ⚠ TutorialDragHintAnimator._hand đang NULL (thấy {soAnimator} animator) ⇒ không biết bản " +
                          $"'{TEN_HAND_DRAG}' nào là bản thật → KHÔNG xoá bản nào cho an toàn.");
        }
        else
        {
            sb.AppendLine($"  Bản '{TEN_HAND_DRAG}' ĐƯỢC TRỎ TỚI (giữ): {DuongDan(dragDuocTro.transform)}");

            var duThua = new List<GameObject>();
            foreach (Transform t in canvasTay.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != TEN_HAND_DRAG) continue;
                if (t.gameObject == dragDuocTro) continue;
                duThua.Add(t.gameObject);
            }

            foreach (GameObject go in duThua)
            {
                if (go == null) continue;
                if (CoAiTroToi(go, out string lyDo))
                {
                    sb.AppendLine($"  🔴 KHÔNG XOÁ '{DuongDan(go.transform)}': còn script trỏ tới → {lyDo}");
                    continue;
                }
                sb.AppendLine($"  − XOÁ bản thừa '{DuongDan(go.transform)}'");
                Undo.DestroyObjectImmediate(go);
                daXoa++;
            }

            // Hand_Drag_Seed nằm NGOÀI lớp tay (ví dụ dưới TutorialV2_Dialogue) — chỉ báo, TUYỆT ĐỐI không đụng.
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name != TEN_HAND_DRAG) continue;
                if (t.gameObject == dragDuocTro) continue;
                if (t.IsChildOf(canvasTay.transform)) continue;
                sb.AppendLine($"  · Có '{TEN_HAND_DRAG}' NGOÀI lớp tay: {DuongDan(t)} → tool KHÔNG đụng (ngoài phạm vi).");
            }
        }

        // ── 2b. Dời con thật từ các wrapper lồng lên lớp gốc ──
        var wrapper = new List<Transform>();
        foreach (Transform t in goc.GetComponentsInChildren<Transform>(true))
        {
            if (t == goc) continue;
            if (t.name == TEN_BOC_TAY) wrapper.Add(t);
        }
        // Sâu nhất trước ⇒ dời từ trong ra ngoài, không mất con.
        wrapper.Sort((a, b) => DoSau(b).CompareTo(DoSau(a)));

        if (wrapper.Count == 0)
            sb.AppendLine("  · Không còn lớp lồng nào — cây tay đã phẳng.");

        foreach (Transform w in wrapper)
        {
            if (w == null) continue;

            var conThat = new List<Transform>();
            for (int i = 0; i < w.childCount; i++)
            {
                Transform c = w.GetChild(i);
                if (c.name == TEN_BOC_TAY) continue;   // wrapper con — xử ở vòng khác
                conThat.Add(c);
            }

            foreach (Transform c in conThat)
            {
                if (c == null) continue;

                var rt = c as RectTransform;
                Vector2 posCu  = rt != null ? rt.anchoredPosition : Vector2.zero;
                Vector2 sizeCu = rt != null ? rt.sizeDelta : Vector2.zero;
                Vector3 scaleCu = c.localScale;

                sb.AppendLine($"  → DỜI '{c.name}' (active={(c.gameObject.activeSelf ? 1 : 0)}): {DuongDan(w)} ⇒ {DuongDan(goc)}");

                Undo.RegisterFullObjectHierarchyUndo(c.gameObject, "Tutorial Studio - doi tay");
                Undo.SetTransformParent(c, goc, false, "Tutorial Studio - doi tay");   // worldPositionStays = false
                c.SetAsLastSibling();

                // Kiểm tra lại — lệch thì gán về đúng giá trị đã đọc trước khi dời.
                if (rt != null)
                {
                    if (rt.anchoredPosition != posCu || rt.sizeDelta != sizeCu)
                    {
                        Undo.RecordObject(rt, "Tutorial Studio - giu toa do tay");
                        rt.anchoredPosition = posCu;
                        rt.sizeDelta = sizeCu;
                        sb.AppendLine($"      (đã gán lại pos{Fmt(posCu)} size{Fmt(sizeCu)} vì SetParent làm lệch)");
                    }
                }
                if (c.localScale != scaleCu)
                {
                    Undo.RecordObject(c, "Tutorial Studio - giu scale tay");
                    c.localScale = scaleCu;
                    sb.AppendLine($"      (đã gán lại scale {scaleCu})");
                }

                daDoi++;
            }
        }

        // ── 2c. Xoá wrapper rỗng (sâu nhất trước) ──
        foreach (Transform w in wrapper)
        {
            if (w == null) continue;
            if (w.childCount > 0)
            {
                sb.AppendLine($"  ⚠ Giữ lại '{DuongDan(w)}': vẫn còn {w.childCount} con (không phải wrapper rỗng).");
                continue;
            }
            if (CoAiTroToi(w.gameObject, out string lyDo))
            {
                sb.AppendLine($"  🔴 KHÔNG XOÁ wrapper '{DuongDan(w)}': còn script trỏ tới → {lyDo}");
                continue;
            }
            sb.AppendLine($"  − XOÁ wrapper rỗng '{DuongDan(w)}'");
            Undo.DestroyObjectImmediate(w.gameObject);
            daXoa++;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MỤC 3 — BẬT HẾT ĐỂ CHỈNH TAY
    // ═══════════════════════════════════════════════════════════════════════════

    [System.Serializable]
    private class MucTrangThai
    {
        public string duongDan;
        public bool bat;
    }

    [System.Serializable]
    private class BanLuuTrangThai
    {
        public string scene;
        public List<MucTrangThai> muc = new List<MucTrangThai>();
    }

    private static void BatHetDeChinh()
    {
        GameObject canvas = TimTheoTenToanScene(TEN_CANVAS);
        if (canvas == null)
        {
            Debug.LogError($"[TutorialStudio] Không tìm thấy '{TEN_CANVAS}' trong scene đang mở. Mở SCN_Farm.unity rồi chạy lại.");
            return;
        }

        string khoa = KhoaPrefs(canvas.scene);
        var sb = new StringBuilder();
        sb.AppendLine("═════════ TUTORIAL STUDIO — BẬT HẾT ĐỂ CHỈNH TAY ═════════");
        sb.AppendLine($"Scene : {canvas.scene.name}   ·   EditorPrefs key: {khoa}");

        // Đã có bản lưu ⇒ hỏi, mặc định GIỮ bản cũ (bấm nhầm mục 3 lần 2 sẽ không đè mất trạng thái gốc).
        bool ghiBanLuu = true;
        if (EditorPrefs.HasKey(khoa))
        {
            ghiBanLuu = !EditorUtility.DisplayDialog(
                "Đã có bản lưu trạng thái",
                "Scene này đã có bản lưu activeSelf từ lần bấm mục 3 trước.\n\n" +
                "• GIỮ bản lưu cũ (khuyên dùng — mục 4 vẫn trả về đúng trạng thái chạy gốc)\n" +
                "• Ghi đè bằng trạng thái HIỆN TẠI (chỉ chọn khi Sếp chắc chắn)",
                "Giữ bản lưu cũ", "Ghi đè");
            sb.AppendLine(ghiBanLuu ? "  · Sếp chọn GHI ĐÈ bản lưu." : "  · Giữ bản lưu cũ (không ghi đè).");
        }

        var ban = new BanLuuTrangThai { scene = canvas.scene.name };
        var dsCon = new List<Transform>();
        ThuThapCon(canvas.transform, canvas.transform, dsCon, ban.muc);

        if (ghiBanLuu)
        {
            EditorPrefs.SetString(khoa, JsonUtility.ToJson(ban));
            sb.AppendLine($"  ✔ Đã lưu activeSelf của {ban.muc.Count} object vào EditorPrefs.");
        }

        int doi = 0;
        foreach (Transform t in dsCon)
        {
            if (t == null || t.gameObject.activeSelf) continue;
            Undo.RecordObject(t.gameObject, "Tutorial Studio - bat het");
            t.gameObject.SetActive(true);
            sb.AppendLine($"  BẬT : {DuongDan(t)}");
            doi++;
        }
        if (!canvas.activeSelf)
        {
            Undo.RecordObject(canvas, "Tutorial Studio - bat het");
            canvas.SetActive(true);
            sb.AppendLine($"  BẬT : {DuongDan(canvas.transform)} (chính Tutorial_Canvas)");
            doi++;
        }

        var cg = canvas.GetComponent<CanvasGroup>();
        if (cg != null && !Mathf.Approximately(cg.alpha, 1f))
        {
            Undo.RecordObject(cg, "Tutorial Studio - alpha 1");
            cg.alpha = 1f;
            sb.AppendLine("  ✔ CanvasGroup của Tutorial_Canvas: alpha → 1");
            doi++;
        }

        Scene s = canvas.scene;
        if (s.IsValid() && s.isLoaded) EditorSceneManager.MarkSceneDirty(s);

        Selection.activeGameObject = canvas;
        SceneView.FrameLastActiveSceneView();

        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine($"Đã bật {doi} mục. Giờ Sếp kéo-thả tay trong Scene view thoải mái.");
        sb.AppendLine(">>> CHỈNH XONG BẤM MỤC 4 ĐỂ TRẢ LẠI (Tools ▸ Farm ▸ Tutorial Studio ▸ 4. TRA VE trang thai chay).");
        sb.AppendLine(">>> Scene CHƯA lưu — Ctrl+S sau khi đã bấm mục 4.");
        sb.AppendLine("══════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    /// <summary>Duyệt mọi con (mọi độ sâu, kể cả inactive) → list Transform + list bản ghi activeSelf.</summary>
    private static void ThuThapCon(Transform goc, Transform t, List<Transform> ra, List<MucTrangThai> banGhi)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i);
            ra.Add(c);
            banGhi.Add(new MucTrangThai { duongDan = DuongDanTuongDoi(goc, c), bat = c.gameObject.activeSelf });
            ThuThapCon(goc, c, ra, banGhi);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MỤC 4 — TRẢ VỀ TRẠNG THÁI CHẠY
    // ═══════════════════════════════════════════════════════════════════════════

    private static void TraVeTrangThaiChay()
    {
        GameObject canvas = TimTheoTenToanScene(TEN_CANVAS);
        if (canvas == null)
        {
            Debug.LogError($"[TutorialStudio] Không tìm thấy '{TEN_CANVAS}' trong scene đang mở. Mở SCN_Farm.unity rồi chạy lại.");
            return;
        }

        string khoa = KhoaPrefs(canvas.scene);
        var sb = new StringBuilder();
        sb.AppendLine("════════ TUTORIAL STUDIO — TRẢ VỀ TRẠNG THÁI CHẠY ════════");
        sb.AppendLine($"Scene : {canvas.scene.name}   ·   EditorPrefs key: {khoa}");

        var dsCon = new List<Transform>();
        var bqua = new List<MucTrangThai>();
        ThuThapCon(canvas.transform, canvas.transform, dsCon, bqua);

        var mongMuon = new Dictionary<string, bool>();
        bool coBanLuu = EditorPrefs.HasKey(khoa);

        if (coBanLuu)
        {
            var ban = JsonUtility.FromJson<BanLuuTrangThai>(EditorPrefs.GetString(khoa, ""));
            if (ban == null || ban.muc == null || ban.muc.Count == 0)
            {
                coBanLuu = false;
                sb.AppendLine("  ⚠ Bản lưu hỏng / rỗng → dùng BỘ MẶC ĐỊNH AN TOÀN.");
            }
            else
            {
                foreach (MucTrangThai m in ban.muc) mongMuon[m.duongDan] = m.bat;
                sb.AppendLine($"  ✔ Đọc bản lưu: {ban.muc.Count} object (scene lưu: {ban.scene}).");
            }
        }
        else
        {
            sb.AppendLine("  ⚠ CHƯA có bản lưu (Sếp chưa bấm mục 3) → dùng BỘ MẶC ĐỊNH AN TOÀN:");
            sb.AppendLine("     TẮT: Dim_Background, TutorialV2_Dialogue, Tutorial_GuideBoard, mọi Hand_*");
            sb.AppendLine("     BẬT: Cloud_Panel, Canvas_TutorialHand, Tutorial_Hands");
        }

        int doi = 0, giu = 0, thieu = 0;
        foreach (Transform t in dsCon)
        {
            if (t == null) continue;
            string duongDan = DuongDanTuongDoi(canvas.transform, t);

            bool muon;
            if (coBanLuu && mongMuon.TryGetValue(duongDan, out bool luu))
            {
                muon = luu;
            }
            else
            {
                if (coBanLuu) { thieu++; sb.AppendLine($"  · Không có trong bản lưu (object mới?): {duongDan} → áp mặc định."); }
                muon = MacDinhAnToan(t.name, t.gameObject.activeSelf);
            }

            if (t.gameObject.activeSelf == muon) { giu++; continue; }

            Undo.RecordObject(t.gameObject, "Tutorial Studio - tra ve trang thai chay");
            t.gameObject.SetActive(muon);
            sb.AppendLine($"  {(muon ? "BẬT " : "TẮT ")}: {duongDan}   (trước: {(!muon ? "bật" : "tắt")})");
            doi++;
        }

        Scene s = canvas.scene;
        if (s.IsValid() && s.isLoaded) EditorSceneManager.MarkSceneDirty(s);

        sb.AppendLine("──────────────────────────────────────────────────────────");
        sb.AppendLine($"Đã đổi {doi} object, giữ nguyên {giu}{(thieu > 0 ? $", {thieu} object không có trong bản lưu" : "")}.");
        sb.AppendLine(">>> Kiểm lại bằng mục 5, rồi Ctrl+S. Sai thì Ctrl+Z.");
        sb.AppendLine("══════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    /// <summary>Bộ mặc định khi không có bản lưu: nhóm phải tắt → false; Cloud/lớp tay → true; còn lại giữ nguyên.</summary>
    private static bool MacDinhAnToan(string ten, bool hienTai)
    {
        if (System.Array.IndexOf(PHAI_TAT_LUC_CHAY, ten) >= 0) return false;
        if (ten.StartsWith("Hand_")) return false;
        if (System.Array.IndexOf(MAC_DINH_BAT, ten) >= 0) return true;
        return hienTai;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  AN TOÀN XOÁ — quét mọi SerializedProperty ObjectReference trong scene
    // ═══════════════════════════════════════════════════════════════════════════

    private struct ThamChieu
    {
        public MonoBehaviour nguon;
        public string moTa;
    }

    private static Dictionary<UnityEngine.Object, List<ThamChieu>> _bangThamChieu;

    /// <summary>Dựng bảng "object nào đang bị ai trỏ tới" — quét 1 lần cho cả lượt APPLY.</summary>
    private static void XayBangThamChieu(bool batBuocDungLai = false)
    {
        if (_bangThamChieu != null && !batBuocDungLai) return;

        _bangThamChieu = new Dictionary<UnityEngine.Object, List<ThamChieu>>();

        var tatCa = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in tatCa)
        {
            if (mb == null) continue;

            var so = new SerializedObject(mb);
            SerializedProperty p = so.GetIterator();
            bool vaoCon = true;
            while (p.Next(vaoCon))
            {
                vaoCon = true;
                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;

                UnityEngine.Object v = p.objectReferenceValue;
                if (v == null) continue;

                if (!_bangThamChieu.TryGetValue(v, out List<ThamChieu> ds))
                {
                    ds = new List<ThamChieu>();
                    _bangThamChieu[v] = ds;
                }
                ds.Add(new ThamChieu
                {
                    nguon = mb,
                    moTa  = $"{DuongDan(mb.transform)} ({mb.GetType().Name}).{p.propertyPath}",
                });
            }
        }
    }

    /// <summary>
    /// TRUE nếu còn MonoBehaviour NGOÀI cây <paramref name="go"/> trỏ tới chính nó, hoặc tới bất kỳ
    /// GameObject/Component nào trong cây con của nó (xoá sẽ sinh Missing reference).
    /// Tham chiếu phát ra từ CHÍNH cây sắp bị xoá thì bỏ qua (chúng cũng biến mất cùng).
    /// </summary>
    private static bool CoAiTroToi(GameObject go) => CoAiTroToi(go, out _);

    private static bool CoAiTroToi(GameObject go, out string lyDo)
    {
        lyDo = null;
        if (go == null) return false;

        XayBangThamChieu();

        // Toàn bộ GameObject + Component trong cây con — xoá go là mất hết chúng.
        var trongCay = new List<UnityEngine.Object>();
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            trongCay.Add(t.gameObject);
            foreach (Component c in t.GetComponents<Component>())
                if (c != null) trongCay.Add(c);
        }

        foreach (UnityEngine.Object o in trongCay)
        {
            if (!_bangThamChieu.TryGetValue(o, out List<ThamChieu> ds)) continue;
            foreach (ThamChieu tc in ds)
            {
                if (tc.nguon == null) continue;
                if (tc.nguon.transform.IsChildOf(go.transform)) continue;   // nguồn cũng nằm trong cây sắp xoá
                lyDo = $"{tc.moTa} → {o.name}";
                return true;
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Tìm GameObject theo tên trong mọi scene đang mở, kể cả inactive (như SeedPanelFixTool.FindPanelRoots).</summary>
    private static GameObject TimTheoTenToanScene(string ten)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == ten) return t.gameObject;
                }
            }
        }
        return null;
    }

    /// <summary>Tìm con (mọi độ sâu, kể cả inactive) theo tên.</summary>
    private static GameObject TimConTheoTen(Transform goc, string ten)
    {
        foreach (Transform t in goc.GetComponentsInChildren<Transform>(true))
        {
            if (t != goc && t.name == ten) return t.gameObject;
        }
        return null;
    }

    private static GameObject LayGameObject(UnityEngine.Object o)
    {
        if (o == null) return null;
        if (o is GameObject go) return go;
        if (o is Component c) return c.gameObject;
        return null;
    }

    private static bool TrungTenVoiToTien(Transform t)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.name == t.name) return true;
            p = p.parent;
        }
        return false;
    }

    private static int DoSau(Transform t)
    {
        int d = 0;
        while (t.parent != null) { d++; t = t.parent; }
        return d;
    }

    private static string DuongDan(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return $"[{t.gameObject.scene.name}] {sb}";
    }

    /// <summary>Đường dẫn tương đối từ gốc, KÈM chỉ số anh em để phân biệt các object trùng tên lồng nhau.</summary>
    private static string DuongDanTuongDoi(Transform goc, Transform t)
    {
        var doan = new List<string>();
        Transform cur = t;
        while (cur != null && cur != goc)
        {
            doan.Insert(0, $"{cur.name}[{cur.GetSiblingIndex()}]");
            cur = cur.parent;
        }
        return string.Join("/", doan);
    }

    private static string KhoaPrefs(Scene s) => KHOA_PREFS + (string.IsNullOrEmpty(s.name) ? "Unknown" : s.name);

    private static string Fmt(Vector2 v) => $"({v.x:0.#},{v.y:0.#})";
}
#endif
