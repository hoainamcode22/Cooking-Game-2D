using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [JUICE T2 — 2026-08-31] Nhạc trưởng tiền tệ: nghe FarmEconomyManager.OnGoldAddedFx /
/// OnGemAddedFx và phát hiệu ứng MỚI CurrencyBurstFlyFX (bung vòng tròn → bay HUD → nhún)
/// tại vị trí con trỏ/ngón tay — thay bộ bay cũ CoinFlyFX/GemFlyFX (tắt êm bằng enabled=false,
/// OnDisable của chúng tự huỷ đăng ký event; muốn HOÀN TÁC chỉ việc xoá file này).
/// Tự khởi động, không cần kéo vào scene, không sửa file nào đang chạy.
/// </summary>
public class CurrencyFXDirector : MonoBehaviour
{
    /// <summary>Số icon theo lượng nhận: 20 vàng ~ 5 icon, 300 vàng ~ 14 icon (kẹp MaxIcons).</summary>
    private static int IconCount(int amount) => Mathf.Clamp(4 + amount / 25, 4, CurrencyBurstFlyFX.MaxIcons);

    private static CurrencyFXDirector _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("CurrencyFXDirector");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<CurrencyFXDirector>();
    }

    private void OnEnable()
    {
        FarmEconomyManager.OnGoldAddedFx += HandleGold;
        FarmEconomyManager.OnGemAddedFx  += HandleGem;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        DisableLegacy();
    }

    private void OnDisable()
    {
        FarmEconomyManager.OnGoldAddedFx -= HandleGold;
        FarmEconomyManager.OnGemAddedFx  -= HandleGem;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode m) => DisableLegacy();

    /// <summary>Tắt bộ bay cũ để không nổ ĐÔI hiệu ứng — OnDisable của chúng tự gỡ event.</summary>
    private static void DisableLegacy()
    {
        foreach (var c in FindObjectsByType<CoinFlyFX>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.enabled) c.enabled = false;
        foreach (var g in FindObjectsByType<GemFlyFX>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (g.enabled) g.enabled = false;
    }

    private static Vector2 Origin()
    {
        Vector2 p = Input.mousePosition;
        if (p.x <= 1f && p.y <= 1f)   // không có input (thưởng offline/tự động) → giữa màn hình
            p = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
        return p;
    }

    private void HandleGold(int amount) => CurrencyBurstFlyFX.PlayCoinsScreen(Origin(), IconCount(amount));
    private void HandleGem (int amount) => CurrencyBurstFlyFX.PlayGemsScreen (Origin(), IconCount(amount * 8));
}
