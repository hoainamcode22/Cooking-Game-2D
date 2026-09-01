using UnityEditor;
using UnityEngine;

/// <summary>
/// [JUICE PACK — 2026-08-31] Nút demo 1-click cho Sếp xem 3 hiệu ứng mới trong PLAY MODE.
/// Menu: Tools ▸ Farm Game ▸ Demo Juice FX ▸ ...
/// Chỉ đọc + gọi API sẵn có (AddExp/AddGold/AddGems/CelebrationTapFX) — không đụng data.
/// </summary>
public static class JuiceDemoTool
{
    private const string BASE = "Tools/Farm Game/Demo Juice FX/";

    // ── 1. LÊN CẤP: cộng đúng số EXP còn thiếu → chạy TRỌN luồng thật:
    //       tiếng EXP → popup Level-Up → mascot nhảy 12 frame → 6 ô quà → confetti.
    [MenuItem(BASE + "★ Demo LÊN CẤP (popup + mascot + quà)")]
    private static void DemoLevelUp()
    {
        var p = PlayerProgressManager.Instance;
        if (p == null) { Warn("Không thấy PlayerProgressManager — Scene chưa Play?"); return; }
        if (p.Level >= PlayerProgressManager.CapToiDa)
        { Warn($"Đang ở cấp tối đa {p.Level} — dùng 'Phase 1 Test/Force Level' hạ cấp trước."); return; }
        int thieu = Mathf.Max(1, p.RequiredExpCurrentLevel - p.CurrentExp);
        p.AddExp(thieu);
        Debug.Log($"[JuiceDemo] +{thieu} EXP → lên cấp {p.Level}. Xem popup!");
    }

    // ── 2. VÀNG BAY: bung vòng tròn → hút về HUD → icon nhún ──
    [MenuItem(BASE + "Demo VÀNG bay vòng tròn (+120)")]
    private static void DemoGold()
    {
        var e = FarmEconomyManager.Instance;
        if (e == null) { Warn("Không thấy FarmEconomyManager — Scene chưa Play?"); return; }
        e.AddGold(120);
        Debug.Log("[JuiceDemo] +120 vàng — nhìn giữa màn hình → HUD.");
    }

    [MenuItem(BASE + "Demo KIM CƯƠNG bay (+5)")]
    private static void DemoGems()
    {
        var e = FarmEconomyManager.Instance;
        if (e == null) { Warn("Không thấy FarmEconomyManager — Scene chưa Play?"); return; }
        e.AddGems(5);
    }

    // ── 3. PHÁO HOA: nổ ngay giữa khung camera, to như chạm công trình mới xây ──
    [MenuItem(BASE + "Demo PHÁO HOA (giữa màn hình)")]
    private static void DemoFireworks()
    {
        Camera cam = Camera.main;
        Vector3 pos = cam != null
            ? cam.ViewportToWorldPoint(new Vector3(0.5f, 0.55f, Mathf.Abs(cam.transform.position.z)))
            : Vector3.zero;
        pos.z = 0f;
        CelebrationTapFX.Play(pos, 1.3f);
        Debug.Log("[JuiceDemo] Bùm 🎆 tại " + pos);
    }

    // ── Chỉ cho bấm khi đang Play ──
    [MenuItem(BASE + "★ Demo LÊN CẤP (popup + mascot + quà)", true)]
    [MenuItem(BASE + "Demo VÀNG bay vòng tròn (+120)", true)]
    [MenuItem(BASE + "Demo KIM CƯƠNG bay (+5)", true)]
    [MenuItem(BASE + "Demo PHÁO HOA (giữa màn hình)", true)]
    private static bool ChiKhiPlay() => Application.isPlaying;

    private static void Warn(string msg)
    {
        Debug.LogWarning("[JuiceDemo] " + msg);
        EditorUtility.DisplayDialog("Demo Juice FX", msg + "\n\n(Nút demo chỉ chạy trong Play Mode)", "OK");
    }
}
