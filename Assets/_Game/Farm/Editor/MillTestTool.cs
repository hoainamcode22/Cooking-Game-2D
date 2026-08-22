using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TEST MÁY XAY — nạp nguyên liệu và ép mẻ xay xong ngay, để xem được các hiệu ứng.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN TOOL NÀY
/// ══════════════════════════════════════════════════════════════════════════
/// Ba hiệu ứng đắt nhất của popup máy xay chỉ xuất hiện ở những trạng thái mà người test
/// KHÔNG dễ tạo ra:
///
///   • hạt nguyên liệu bay vào phễu + máy nhún   → chỉ khi BẮT ĐẦU một mẻ
///   • khói + bong bóng phun khỏi phễu           → chỉ khi ĐANG xay (soDangXay > 0)
///   • pháo hoa "bùm bùm" + bao nảy + vòng sáng  → chỉ đúng KHOẢNH KHẮC một mẻ xong
///   • icon bay về kho                            → chỉ khi bấm THU
///
/// Kho trống ⇒ không kéo-thả được ⇒ **không thấy được cái nào cả**, và popup trông y như
/// chưa sửa gì. Công thức nhanh nhất (Cám cho gà) cũng ủ 2 phút, đứng chờ 2 phút chỉ để
/// xem một cú pháo hoa là quá lâu.
///
/// Tool này bỏ hai rào đó: nạp nguyên liệu, và ép mọi slot đang xay xong tức thì.
///
/// ══════════════════════════════════════════════════════════════════════════
///  CHỈ CHẠY TRONG PLAY MODE
/// ══════════════════════════════════════════════════════════════════════════
/// Khác `NapTienTestTool` (vàng/kim cương lưu ở PlayerPrefs nên ghi được cả ở Edit Mode),
/// nguyên liệu nằm trong `FarmInventoryManager` — một manager giữ số lượng TRONG BỘ NHỚ.
/// Ghi PlayerPrefs lúc Edit Mode là vô nghĩa: lần save kế tiếp trong Play sẽ ghi đè.
/// Vì vậy mọi lệnh ở đây yêu cầu đang Play.
///
/// ⚠ ĐÂY LÀ TOOL TEST. Nó cộng đồ miễn phí và bỏ qua thời gian ủ — đừng dùng để cân bằng
/// kinh tế, và đừng gọi nó từ code game.
/// </summary>
public static class MillTestTool
{
    private const string Menu = "Tools/Farm/Popup May Xay/";
    private const string LOG  = "[MillTest] ";

    /// <summary>
    /// Nguyên liệu của 4 công thức trong `MillConfig`. Khớp `MillRecipe_*.asset`:
    /// cam_ga = rice×3 + ngo×2 · cam_heo = carot×4 + bapcai×3 · co_tron_bo = rice×5
    /// · cam_bo_sua = rice×6.
    /// </summary>
    private static readonly string[] NguyenLieu = { "rice", "ngo", "carot", "bapcai" };

    // ─────────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(Menu + "10. TEST — Nap 50 moi nguyen lieu (Play Mode)", false, 10)]
    private static void Nap50() => NapNguyenLieu(50);

    [MenuItem(Menu + "11. TEST — Xong ngay moi slot dang xay (Play Mode)", false, 11)]
    private static void XongNgay()
    {
        if (!BaoDamDangPlay()) return;

        MillPopupUI popup = MillPopupUI.Instance;
        if (popup == null)
        {
            Bao("Không thấy MillPopupUI trong scene. Vào Play Mode và mở popup máy xay trước.");
            return;
        }

        // `_slotStates` và class `SlotState` đều private — đây là tool test nên dùng
        // reflection thay vì mở API public trên file runtime chỉ để phục vụ việc test.
        FieldInfo fSlots = typeof(MillPopupUI).GetField(
            "_slotStates", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fSlots == null)
        {
            Bao("Không tìm thấy field '_slotStates' trong MillPopupUI — file runtime đã đổi. " +
                "Sửa lại tool này cho khớp.");
            return;
        }

        var mang = fSlots.GetValue(popup) as Array;
        if (mang == null || mang.Length == 0)
        {
            Bao("Popup chưa khởi tạo slot nào. Mở popup máy xay rồi bấm lại.");
            return;
        }

        FieldInfo fEnd    = null;
        FieldInfo fRecipe = null;
        int doi = 0;

        for (int i = 0; i < mang.Length; i++)
        {
            object st = mang.GetValue(i);
            if (st == null) continue;

            // Lấy FieldInfo một lần rồi tái dùng — GetField trong vòng lặp là chậm vô ích.
            if (fEnd == null)
            {
                Type t  = st.GetType();
                fEnd    = t.GetField("endTicksUtc", BindingFlags.Public | BindingFlags.Instance);
                fRecipe = t.GetField("recipe",      BindingFlags.Public | BindingFlags.Instance);
            }

            if (fEnd == null || fRecipe == null) continue;
            if (fRecipe.GetValue(st) == null) continue;   // slot trống, không có gì để xong

            long end = (long)fEnd.GetValue(st);
            if (end <= DateTime.UtcNow.Ticks) continue;   // đã xong rồi

            // Đặt mốc xong về HIỆN TẠI. Không tự vẽ slot ở đây: `MillPopupUI.Update` là nơi
            // duy nhất được vẽ slot, frame sau nó tự chuyển sang "chờ thu" VÀ tự bắn pháo
            // hoa qua cơ chế đối chiếu số-chờ-thu-frame-trước.
            fEnd.SetValue(st, DateTime.UtcNow.Ticks);
            doi++;
        }

        if (doi == 0)
        {
            Bao("Không có slot nào đang xay. Kéo một công thức vào slot trống trước, " +
                "rồi bấm lệnh này để xem pháo hoa ngay.");
            return;
        }

        Debug.Log(LOG + "Đã ép " + doi + " slot xong ngay. Xem pháo hoa + bao nảy ở khung máy.");
    }

    [MenuItem(Menu + "12. TEST — Xem so luong nguyen lieu (Play Mode)", false, 12)]
    private static void XemSoLuong()
    {
        if (!BaoDamDangPlay()) return;

        var sb = new StringBuilder();
        sb.Append(LOG).Append("Nguyên liệu trong kho: ");

        for (int i = 0; i < NguyenLieu.Length; i++)
        {
            if (i > 0) sb.Append(" · ");
            sb.Append(NguyenLieu[i]).Append(" = ")
              .Append(MillInventoryBridge.SoLuongTrongKho(NguyenLieu[i]));
        }

        sb.Append("   |   kim cương = ").Append(MillInventoryBridge.SoKimCuong());
        sb.Append(" · cấp = ").Append(MillInventoryBridge.CapHienTai());

        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LÕI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cộng <paramref name="soLuong"/> cho mỗi nguyên liệu của 4 công thức.</summary>
    private static void NapNguyenLieu(int soLuong)
    {
        if (!BaoDamDangPlay()) return;

        FarmInventoryManager kho = FarmInventoryManager.Instance;
        if (kho == null)
        {
            Bao("Không thấy FarmInventoryManager. Vào Play Mode ở scene SCN_Farm trước.");
            return;
        }

        var sb = new StringBuilder();
        sb.Append(LOG).Append("Đã nạp: ");
        int thanhCong = 0;

        for (int i = 0; i < NguyenLieu.Length; i++)
        {
            string id = NguyenLieu[i];

            // AddItem trả false khi túi ĐẦY và đây là loại mới — báo rõ chứ không im lặng.
            bool ok = kho.AddItem(id, soLuong);

            if (i > 0) sb.Append(" · ");
            sb.Append(id).Append(ok ? (" +" + soLuong) : " THẤT BẠI (túi đầy?)");
            if (ok) thanhCong++;
        }

        Debug.Log(sb.ToString());

        if (thanhCong == 0)
        {
            Bao("Không nạp được gì — túi nông sản đang đầy. Bán bớt hoặc nâng cấp kho.");
            return;
        }

        Debug.Log(LOG + "Giờ kéo một thẻ công thức từ cột CÔNG THỨC sang thả vào một slot trống. " +
                        "Sẽ thấy: hạt bay vào phễu, máy nhún, khói phun ra. " +
                        "Rồi chạy lệnh 11 để xem pháo hoa ngay, không phải chờ 2 phút.");
    }

    private static bool BaoDamDangPlay()
    {
        if (Application.isPlaying) return true;

        Bao("Lệnh này chỉ chạy trong PLAY MODE.\n\n" +
            "Nguyên liệu nằm trong bộ nhớ của FarmInventoryManager, không nằm ở PlayerPrefs, " +
            "nên ghi lúc Edit Mode sẽ bị lần save kế tiếp ghi đè mà không báo lỗi gì.\n\n" +
            "Bấm Play, mở popup máy xay, rồi chạy lại lệnh này.");
        return false;
    }

    private static void Bao(string noiDung)
    {
        EditorUtility.DisplayDialog("Test máy xay", noiDung, "OK");
        Debug.LogWarning(LOG + noiDung.Replace("\n", " "));
    }
}
