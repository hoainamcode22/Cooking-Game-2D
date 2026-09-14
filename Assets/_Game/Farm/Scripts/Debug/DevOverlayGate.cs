using UnityEngine;

/// <summary>
/// CONG TAC CHUNG cho MOI overlay/hotkey danh cho dev.
///
/// Vi sao khong dung #if: mot so script debug KHONG duoc boc #if nen van bien dich
/// vao ban release. Class nay PHAI bien dich vo dieu kien de moi script deu goi duoc,
/// roi tu quyet dinh bat/tat luc chay.
///
/// LUAT:
///   - Trong Editor        : mac dinh BAT, doc PlayerPrefs "DEV_OVERLAYS_ON" (mac dinh 1).
///   - Ban player thuong   : LUON TAT (khong phai debug build).
///   - Ban development     : TAT tren mobile; may khac doc cung key nhung mac dinh 0.
///
/// Gia tri duoc cache trong static int nen khong doc PlayerPrefs moi frame.
/// </summary>
public static class DevOverlayGate
{
    /// <summary>Ten key PlayerPrefs de bat/tat thu cong khi can test.</summary>
    public const string PrefKey = "DEV_OVERLAYS_ON";

    // -1 = chua doc lan nao; 0 = tat; 1 = bat.
    private static int _cached = -1;

    /// <summary>TRUE thi overlay/hotkey dev moi duoc phep chay.</summary>
    public static bool Enabled
    {
        get
        {
            if (_cached >= 0) return _cached == 1;

#if UNITY_EDITOR
            // Editor: mac dinh bat cho tien lam viec.
            _cached = PlayerPrefs.GetInt(PrefKey, 1);
#else
            // Ban phat hanh khong phai debug build: chan thang, khong doc pref.
            if (!Debug.isDebugBuild)
            {
                _cached = 0;
            }
            else if (Application.isMobilePlatform)
            {
                // Mobile: khong bao gio hien overlay IMGUI (de cham nham, che man hinh).
                _cached = 0;
            }
            else
            {
                // Development build tren PC: phai bat tay bang pref.
                _cached = PlayerPrefs.GetInt(PrefKey, 0);
            }
#endif
            return _cached == 1;
        }
    }

    /// <summary>Bat/tat thu cong luc chay — ghi pref va cap nhat cache ngay.</summary>
    public static void Set(bool on)
    {
        _cached = on ? 1 : 0;
        PlayerPrefs.SetInt(PrefKey, _cached);
        PlayerPrefs.Save();
    }
}
