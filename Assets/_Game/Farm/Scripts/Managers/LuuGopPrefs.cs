using UnityEngine;

/// <summary>
/// LƯU GỘP CÓ TRỄ — thay cho việc gọi <c>PlayerPrefs.Save()</c> mỗi lần đổi một giá trị.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN
/// ══════════════════════════════════════════════════════════════════════════
/// `PlayerPrefs.SetInt/SetString` chỉ ghi vào bộ đệm trong RAM — rất nhanh.
/// `PlayerPrefs.Save()` mới là thứ đắt: nó ghi xuống ổ đĩa (trên Windows là registry),
/// ĐỒNG BỘ, chặn luồng chính. Mỗi lần khoảng 10–100ms tuỳ ổ và số key.
///
/// Dự án đang gọi `Save()` ở 41 chỗ trong code runtime. Một lần người chơi bấm "Nhận"
/// trên popup nhiệm vụ kéo theo NĂM lần flush đĩa liên tiếp:
///     ghi cờ đã-nhận · cộng vàng · cộng kim cương · cộng EXP · tăng số thành tựu
/// ⇒ ~150ms đứng hình cho một cú bấm. Thu hoạch một ô lúa cũng flush, bán một món cũng
/// flush. Đó chính là cảm giác "game lag" mà không có gì nặng đang chạy.
///
/// ══════════════════════════════════════════════════════════════════════════
///  CÁCH LÀM
/// ══════════════════════════════════════════════════════════════════════════
/// Chỗ nào đang gọi `PlayerPrefs.Save()` thì đổi thành <see cref="Hen"/>. Nó chỉ bật một
/// cờ (0ms). Bộ chạy nền flush thật sự tối đa MỘT LẦN mỗi <see cref="GianCachGiay"/>.
/// Nhiều lần Hen() dồn trong một khung hình ⇒ vẫn chỉ một lần ghi đĩa.
///
/// KHÔNG MẤT DỮ LIỆU vì flush ngay ở mọi đường thoát:
///   • `OnApplicationPause(true)`  — người chơi thu app trên điện thoại
///   • `OnApplicationFocus(false)` — chuyển sang app khác trên PC
///   • `OnApplicationQuit()`       — tắt game
///   • rời Play Mode trong Editor  — không mất tiến độ giữa các lần test
/// Ngoài ra Unity vốn tự `Save()` khi ứng dụng đóng bình thường; đây là lớp bảo hiểm
/// cho trường hợp đóng đột ngột.
///
/// Cần ghi ngay lập tức (mua vật phẩm giá trị lớn, sắp chuyển scene, tool Editor) thì
/// gọi <see cref="LuuNgay"/>.
/// </summary>
public static class LuuGopPrefs
{
    /// <summary>Giây tối thiểu giữa hai lần ghi đĩa thật.</summary>
    private const float GianCachGiay = 2f;

    private static bool  _canGhi;
    private static float _lanGhiKeTiep;

    /// <summary>Số lần ghi đĩa đã tiết kiệm được — để đo hiệu quả, xem qua <see cref="ThongKe"/>.</summary>
    private static int _soLanHen;
    private static int _soLanGhiThat;

    /// <summary>
    /// Đánh dấu "có gì đó đã đổi, cần lưu". Rẻ như gán một biến bool.
    /// Đây là hàm thay thế trực tiếp cho <c>PlayerPrefs.Save()</c> trong code runtime.
    /// </summary>
    public static void Hen()
    {
        _soLanHen++;

        // NGOÀI Play Mode thì ghi thẳng. Không có bộ chạy nền nào sống để tới hạn mà
        // flush, nên hoãn ở đây đồng nghĩa với mất dữ liệu — và mất im lặng, không lỗi.
        // Trường hợp này xảy ra thật: các khối `#if UNITY_EDITOR` trong file runtime,
        // và Editor tool gọi vào hàm lưu của manager.
        if (!Application.isPlaying)
        {
            _canGhi = false;
            _soLanGhiThat++;
            PlayerPrefs.Save();
            return;
        }

        if (_canGhi) return;

        _canGhi = true;

        // Mốc hẹn đặt tại thời điểm ĐÁNH DẤU ĐẦU TIÊN, không phải mỗi lần Hen(). Nếu dời
        // mốc mỗi lần thì lúc người chơi thao tác liên tục (thu hoạch cả ruộng) sẽ không
        // bao giờ tới hạn ghi — đúng cái bẫy "debounce vô hạn".
        _lanGhiKeTiep = Time.realtimeSinceStartup + GianCachGiay;

        BoChayNen.BaoDamTonTai();
    }

    /// <summary>Ghi xuống đĩa NGAY nếu đang có thay đổi chờ. Không có gì chờ thì không làm gì.</summary>
    public static void LuuNgay()
    {
        if (!_canGhi) return;

        _canGhi = false;
        _soLanGhiThat++;
        PlayerPrefs.Save();
    }

    /// <summary>Bộ chạy nền gọi mỗi khung hình.</summary>
    private static void KiemTraDenHan()
    {
        if (!_canGhi) return;
        if (Time.realtimeSinceStartup < _lanGhiKeTiep) return;
        LuuNgay();
    }

    public static string ThongKe()
        => $"[LưuGộp] {_soLanHen} lần yêu cầu lưu → {_soLanGhiThat} lần ghi đĩa thật " +
           $"(tiết kiệm {_soLanHen - _soLanGhiThat} lần chặn luồng chính).";

    // ═════════════════════════════════════════════════════════════════════════
    //  BỘ CHẠY NỀN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Object ẩn tự dựng, `DontDestroyOnLoad`. Tự dựng chứ không bắt phải thêm vào scene
    /// vì lớp này được gọi từ khắp nơi — kể cả scene bếp — và quên thêm vào một scene là
    /// mất save ở scene đó mà không có gì báo.
    /// </summary>
    private class BoChayNen : MonoBehaviour
    {
        private static BoChayNen _instance;

        public static void BaoDamTonTai()
        {
            if (_instance != null) return;
            if (!Application.isPlaying) return;   // Editor tool gọi Hen() thì bỏ qua

            var go = new GameObject("LuuGopPrefs(Auto)") { hideFlags = HideFlags.HideInHierarchy };
            _instance = go.AddComponent<BoChayNen>();
            DontDestroyOnLoad(go);
        }

        private void Update() => KiemTraDenHan();

        // Thu app / chuyển sang app khác: hệ điều hành có thể giết tiến trình bất cứ lúc
        // nào sau đây, nên phải ghi ngay.
        private void OnApplicationPause(bool tamDung)
        {
            if (tamDung) LuuNgay();
        }

        private void OnApplicationFocus(bool dangFocus)
        {
            if (!dangFocus) LuuNgay();
        }

        private void OnApplicationQuit() => LuuNgay();

        private void OnDestroy()
        {
            // Rời Play Mode trong Editor cũng chạy vào đây. Không ghi thì mỗi lần test
            // xong là mất tới 2 giây tiến độ cuối — đủ để mất cả lần bấm "Nhận" vừa rồi.
            LuuNgay();
            if (_instance == this) _instance = null;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Reset trạng thái static khi vào Play Mode. Bật "Enter Play Mode Options" (không
    /// reload domain) thì biến static giữ giá trị của lần chạy trước — cờ `_canGhi` còn
    /// true mà `BoChayNen` đã chết ⇒ không bao giờ ghi nữa.
    /// </summary>
    [UnityEngine.RuntimeInitializeOnLoadMethod(
        UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _canGhi = false;
        _lanGhiKeTiep = 0f;
        _soLanHen = 0;
        _soLanGhiThat = 0;
    }
#endif
}
