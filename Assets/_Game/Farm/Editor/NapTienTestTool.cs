using UnityEditor;
using UnityEngine;

/// <summary>
/// NẠP TIỀN TEST — cộng thêm vàng / kim cương để thử nghiệm.
///
/// KHÁC với tool cũ `Give Test Currency` ở chỗ nó **CỘNG THÊM** chứ không **ĐẶT BẰNG**.
/// Tool cũ gọi `SetCurrency(1000, 1000)`: đang có 25.000 vàng mà bấm là tụt xuống còn
/// 1.000 — đúng nghĩa mất tiền chứ không phải nạp tiền.
///
/// Chạy được ở CẢ HAI chế độ:
///   • Play Mode  → gọi thẳng `FarmEconomyManager` đang sống, HUD cập nhật ngay lập tức.
///   • Edit Mode  → ghi thẳng PlayerPrefs, lần Play sau load lên là có.
///
/// Vì sao không dùng chung một đường: manager là `DontDestroyOnLoad` và giữ số dư TRONG
/// BỘ NHỚ. Đang Play mà chỉ ghi PlayerPrefs thì lần `SaveCurrency()` kế tiếp (bán một
/// món hàng bất kỳ) ghi đè lại số cũ — tiền nạp vào bốc hơi mà không có lỗi nào báo.
/// </summary>
public static class NapTienTestTool
{
    private const string Menu = "Tools/Farm/Nạp Tiền Test/";

    private const string KeyGold = "FARM_ECONOMY_GOLD";
    private const string KeyGems = "FARM_ECONOMY_GEMS";

    // ─────────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(Menu + "+1.000 vàng  &  +1.000 kim cương", false, 1)]
    private static void Nap1000Ca2() => Nap(1000, 1000);

    [MenuItem(Menu + "+1.000 vàng", false, 2)]
    private static void Nap1000Vang() => Nap(1000, 0);

    [MenuItem(Menu + "+1.000 kim cương", false, 3)]
    private static void Nap1000Gem() => Nap(0, 1000);

    [MenuItem(Menu + "+10.000 vàng  &  +10.000 kim cương", false, 20)]
    private static void Nap10000Ca2() => Nap(10000, 10000);

    [MenuItem(Menu + "Nhập số tuỳ ý…", false, 40)]
    private static void NhapTuyY() => CuaSoNapTien.Mo();

    [MenuItem(Menu + "Xem số dư hiện tại", false, 60)]
    private static void XemSoDu()
    {
        DocSoDu(out int vang, out int gem, out string nguon);
        Debug.Log($"[NạpTiền] Số dư ({nguon}): {vang:N0} vàng · {gem:N0} kim cương");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LÕI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cộng thêm tiền. Truyền số âm để trừ bớt (dùng khi test cảnh thiếu tiền).</summary>
    public static void Nap(int themVang, int themGem)
    {
        if (themVang == 0 && themGem == 0) return;

        DocSoDu(out int vangCu, out int gemCu, out _);

        // Chặn xuống dưới 0 ở đây luôn, vì `AddGold` bỏ qua số âm còn PlayerPrefs thì
        // nhận tuốt — hai đường đi sẽ cho kết quả khác nhau nếu không kẹp trước.
        int vangMoi = Mathf.Max(0, vangCu + themVang);
        int gemMoi  = Mathf.Max(0, gemCu  + themGem);

        var eco = Application.isPlaying ? FarmEconomyManager.Instance : null;
        if (eco != null)
        {
            // Đường Play Mode: `SetCurrency` đã tự `SaveCurrency()` + bắn sự kiện đổi số
            // dư, nên HUD vàng/gem góc màn hình cập nhật ngay, không phải chờ Play lại.
            eco.SetCurrency(vangMoi, gemMoi);
        }
        else
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[NạpTiền] Đang Play nhưng chưa có FarmEconomyManager — " +
                                 "ghi tạm vào PlayerPrefs, số dư trên HUD sẽ không đổi cho " +
                                 "tới lần Play sau.");
            }

            PlayerPrefs.SetInt(KeyGold, vangMoi);
            PlayerPrefs.SetInt(KeyGems, gemMoi);
            PlayerPrefs.Save();
        }

        string dauV = themVang >= 0 ? "+" : "";
        string dauG = themGem  >= 0 ? "+" : "";
        Debug.Log($"[NạpTiền] Vàng {vangCu:N0} → {vangMoi:N0} ({dauV}{themVang:N0})  ·  " +
                  $"Kim cương {gemCu:N0} → {gemMoi:N0} ({dauG}{themGem:N0})" +
                  (eco != null ? "" : "   — bấm Play để thấy trong game."));
    }

    private static void DocSoDu(out int vang, out int gem, out string nguon)
    {
        var eco = Application.isPlaying ? FarmEconomyManager.Instance : null;
        if (eco != null)
        {
            vang = eco.Gold; gem = eco.Gems; nguon = "đang chạy";
            return;
        }

        vang = PlayerPrefs.GetInt(KeyGold, 0);
        gem  = PlayerPrefs.GetInt(KeyGems, 0);
        nguon = "PlayerPrefs";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CỬA SỔ NHẬP SỐ TUỲ Ý
    // ─────────────────────────────────────────────────────────────────────────

    private class CuaSoNapTien : EditorWindow
    {
        private int _vang = 1000;
        private int _gem  = 1000;

        public static void Mo()
        {
            var w = GetWindow<CuaSoNapTien>(true, "Nạp tiền test", true);
            w.minSize = new Vector2(340f, 190f);
            w.maxSize = new Vector2(340f, 190f);
            w.Show();
        }

        private void OnGUI()
        {
            DocSoDu(out int vangCu, out int gemCu, out string nguon);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Hiện có ({nguon}):", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"{vangCu:N0} vàng   ·   {gemCu:N0} kim cương",
                                       EditorStyles.boldLabel);

            EditorGUILayout.Space(10f);
            _vang = EditorGUILayout.IntField("Cộng thêm vàng", _vang);
            _gem  = EditorGUILayout.IntField("Cộng thêm kim cương", _gem);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Số âm = trừ bớt (test cảnh thiếu tiền).",
                                       EditorStyles.miniLabel);

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nạp", GUILayout.Height(28f)))
                {
                    Nap(_vang, _gem);
                    Repaint();
                }

                if (GUILayout.Button("Đóng", GUILayout.Height(28f), GUILayout.Width(70f)))
                    Close();
            }
        }

        // Đang Play mà số dư đổi (bán hàng, giao đơn) thì con số trên cửa sổ này phải
        // đổi theo, không thì người dùng cộng dồn lên một số dư đã cũ.
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }
    }
}
