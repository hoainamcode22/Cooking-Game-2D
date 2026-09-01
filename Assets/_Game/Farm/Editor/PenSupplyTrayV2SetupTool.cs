#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Chuồng/★ Setup Khay V2 (1 nút)
/// ═══════════════════════════════════════════════════════════
///
/// MỘT NÚT làm trọn 2 việc để khay vật phẩm V2 (PenSupplyTrayV2) chạy trong scene:
///   1. Đảm bảo scene có host "PenSupplyTrayV2_Host" mang component PenSupplyTrayV2
///      (để Sếp chỉnh tham số dim/size/pop-in trong Inspector; không có thì runtime
///      TryShow cũng tự tạo — host chỉ là chỗ chỉnh cho tiện).
///   2. Bật toggle useSupplyTrayV2 = true trên MỌI PenMiniPanelUI trong scene
///      (kể cả đang inactive) — qua SerializedObject nên có Undo chuẩn.
///
/// IDEMPOTENT: chạy lại lần 2 báo "0 thay đổi", không tạo trùng, không đụng gì thêm.
/// UNDO: Ctrl+Z hoàn tác cả tạo host lẫn bật toggle.
/// KHÔNG auto-save scene — mọi thay đổi chỉ mark dirty qua Undo, Sếp tự Ctrl+S khi ưng.
/// </summary>
public static class PenSupplyTrayV2SetupTool
{
    private const string MENU = "Tools/Farm Game/Chuồng/★ Setup Khay V2 (1 nút)";
    private const string HOST_NAME = "PenSupplyTrayV2_Host";

    [MenuItem(MENU, false, 10)]
    public static void SetupKhayV2()
    {
        int hostTaoMoi   = 0;
        int toggleBat    = 0;
        int toggleDaBat  = 0;
        int penThieuField = 0;

        // ── 1. Host PenSupplyTrayV2 ──────────────────────────────────────────
        PenSupplyTrayV2 host = Object.FindFirstObjectByType<PenSupplyTrayV2>(FindObjectsInactive.Include);
        if (host == null)
        {
            var go = new GameObject(HOST_NAME, typeof(PenSupplyTrayV2));
            Undo.RegisterCreatedObjectUndo(go, "Setup Khay V2");
            host = go.GetComponent<PenSupplyTrayV2>();
            hostTaoMoi = 1;
        }

        // ── 2. Bật toggle useSupplyTrayV2 trên mọi chuồng ────────────────────
        var pens = Object.FindObjectsByType<PenMiniPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pen in pens)
        {
            if (pen == null) continue;

            var so   = new SerializedObject(pen);
            var prop = so.FindProperty("useSupplyTrayV2");
            if (prop == null)
            {
                // File PenMiniPanelUI.cs trong project chưa phải bản có [V2 ADD].
                penThieuField++;
                continue;
            }

            if (prop.boolValue)
            {
                toggleDaBat++;
                continue;
            }

            prop.boolValue = true;
            so.ApplyModifiedProperties(); // tự ghi Undo + mark dirty, KHÔNG save scene
            toggleBat++;
        }

        // ── 3. Report ────────────────────────────────────────────────────────
        string dongHost = hostTaoMoi == 1
            ? "TẠO MỚI \"" + HOST_NAME + "\""
            : "đã có sẵn — giữ nguyên";
        string bao =
            "KHAY V2 — SETUP XONG (idempotent, có Undo, KHÔNG auto-save scene)\n\n" +
            $"• Host PenSupplyTrayV2: {dongHost}\n" +
            $"• Chuồng tìm thấy: {pens.Length}\n" +
            $"   - Bật toggle useSupplyTrayV2 mới: {toggleBat}\n" +
            $"   - Đã bật từ trước (bỏ qua): {toggleDaBat}\n" +
            (penThieuField > 0
                ? $"   - ⚠ THIẾU FIELD useSupplyTrayV2: {penThieuField} chuồng — hãy import bản PenMiniPanelUI.cs mới ([V2 ADD]) rồi chạy lại tool.\n"
                : string.Empty) +
            (pens.Length == 0
                ? "   - ⚠ Scene hiện tại không có PenMiniPanelUI nào — mở đúng scene nông trại rồi chạy lại.\n"
                : string.Empty) +
            $"\nNhớ tự lưu scene (Ctrl+S) nếu ưng kết quả.";

        Debug.Log($"[PenSupplyTrayV2SetupTool] host mới: {hostTaoMoi} · toggle bật mới: {toggleBat} · đã bật: {toggleDaBat} · thiếu field: {penThieuField} · tổng chuồng: {pens.Length}");
        EditorUtility.DisplayDialog("★ Setup Khay V2", bao, "OK");

        if (host != null)
            Selection.activeGameObject = host.gameObject;
    }

    // Không chạy khi đang Play — tránh SerializedObject ghi đè trạng thái runtime.
    [MenuItem(MENU, true)]
    private static bool ValidateSetupKhayV2() => !EditorApplication.isPlaying;
}
#endif
