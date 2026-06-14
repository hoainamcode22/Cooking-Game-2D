using System;
using UnityEngine;

public class FarmEconomyManager : MonoBehaviour
{
    public static FarmEconomyManager Instance { get; private set; }

    private const string GoldKey = "FARM_ECONOMY_GOLD";
    private const string GemsKey = "FARM_ECONOMY_GEMS";

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

    private void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke(Gold, Gems);
    }

    private void LoadCurrency()
    {
        Gold = PlayerPrefs.HasKey(GoldKey) ? PlayerPrefs.GetInt(GoldKey) : startGold;
        Gems = PlayerPrefs.HasKey(GemsKey) ? PlayerPrefs.GetInt(GemsKey) : startGems;
    }

    private void SaveCurrency()
    {
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.SetInt(GemsKey, Gems);
        PlayerPrefs.Save();
    }
}