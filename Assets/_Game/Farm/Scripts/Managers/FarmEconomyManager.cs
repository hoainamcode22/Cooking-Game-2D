using System;
using UnityEngine;

public class FarmEconomyManager : MonoBehaviour
{
    public static FarmEconomyManager Instance { get; private set; }

    private const string GoldKey = "FARM_ECONOMY_GOLD";
    private const string GemsKey = "FARM_ECONOMY_GEMS";

    // B4 — họ save + phiên bản. Hai khoá ghi thẳng số nguyên nên dấu phiên bản nằm ở
    // khoá phụ `SAVE_VER_FARM_ECONOMY`.
    //
    // v1 = vàng/kim cương tính theo bảng giá `MarketPriceTable` hiện hành.
    // TĂNG SỐ NÀY nếu đổi đơn vị tiền hoặc nhân/chia toàn bộ bảng giá: người chơi đang có
    // 5.000 vàng theo giá cũ sẽ giàu (hoặc nghèo) gấp mấy lần nếu bảng giá đổi mà số dư không đổi.
    private const string SaveFamily  = "FARM_ECONOMY";
    private const int    SaveVersion = 1;

    [SerializeField] private int startGold = 400;
    [SerializeField] private int startGems = 15;

    public int Gold { get; private set; }
    public int Gems { get; private set; }

    public event Action<int, int> OnCurrencyChanged;

    // FX: bắn khi cộng vàng để UI chạy hiệu ứng "coin bay về ví" (xem CoinFlyFX)
    public static event Action<int> OnGoldAddedFx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // Tách ra root để DontDestroyOnLoad hoạt động (fix warning)
        DontDestroyOnLoad(gameObject);

        LoadCurrency();
    }

    private void Start()
    {
        NotifyCurrencyChanged();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveCurrency();
    }

    private void OnApplicationQuit()
    {
        SaveCurrency();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
        SaveCurrency();
        NotifyCurrencyChanged();
        return true;
    }

    public bool SpendGems(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gems < amount)
            return false;

        Gems -= amount;
        SaveCurrency();
        NotifyCurrencyChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        SaveCurrency();
        NotifyCurrencyChanged();
        OnGoldAddedFx?.Invoke(amount);
    }

    public void AddGems(int amount)
    {
        if (amount <= 0)
            return;

        Gems += amount;
        SaveCurrency();
        NotifyCurrencyChanged();
    }

    /// <summary>
    /// Reset tiền & kim cương về mặc định (startGold/startGems).
    /// Dùng cho tool/nút "Reset Level 1" — vì manager là DontDestroyOnLoad nên xoá PlayerPrefs
    /// thôi KHÔNG đủ (instance còn sống sẽ ghi đè lại giá trị cũ). Phải reset cả bộ nhớ.
    /// </summary>
    public void ResetCurrency()
    {
        Gold = startGold;
        Gems = startGems;
        SaveCurrency();
        NotifyCurrencyChanged();
        Debug.Log($"[FarmEconomyManager] Reset tiền/kim cương về mặc định: {startGold} vàng, {startGems} gem.");
    }

    /// <summary>Đặt thẳng số vàng/gem (dùng cho tool test khi đang dựng game).</summary>
    public void SetCurrency(int gold, int gems)
    {
        Gold = Mathf.Max(0, gold);
        Gems = Mathf.Max(0, gems);
        SaveCurrency();
        NotifyCurrencyChanged();
        Debug.Log($"[FarmEconomyManager] Set currency: {Gold} vàng, {Gems} gem.");
    }

    private void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke(Gold, Gems);
    }

    private void LoadCurrency()
    {
        // B4 — đóng dấu phiên bản trước khi đọc. Chưa có nhánh migrate nào vì v0 → v1
        // không đổi định dạng (vẫn hai số nguyên cùng ý nghĩa); truyền null để hàm chỉ
        // đóng dấu. Khi nào cần đổi thật thì thêm hàm migrate vào ĐÚNG chỗ này.
        SaveVersionGuard.Ensure(SaveFamily, SaveVersion, null,
                                PlayerPrefs.HasKey(GoldKey) || PlayerPrefs.HasKey(GemsKey));

        Gold = PlayerPrefs.HasKey(GoldKey) ? PlayerPrefs.GetInt(GoldKey) : startGold;
        Gems = PlayerPrefs.HasKey(GemsKey) ? PlayerPrefs.GetInt(GemsKey) : startGems;
    }

    private void SaveCurrency()
    {
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.SetInt(GemsKey, Gems);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }
}