using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    private const string PrefCoins    = "WALLET_COINS";
    private const string PrefDiamonds = "WALLET_DIAMONDS";

    public int Coins    { get; private set; }
    public int Diamonds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void AddCoin(int amount)
    {
        Coins += amount;
        Save();
        Debug.Log($"[Wallet] +{amount} Coin | Tổng: {Coins}");
    }

    public void AddDiamond(int amount)
    {
        Diamonds += amount;
        Save();
        Debug.Log($"[Wallet] +{amount} Diamond | Tổng: {Diamonds}");
    }

    public bool SpendCoin(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        Save();
        return true;
    }

    public bool SpendDiamond(int amount)
    {
        if (Diamonds < amount) return false;
        Diamonds -= amount;
        Save();
        return true;
    }

    private void Load()
    {
        Coins    = PlayerPrefs.GetInt(PrefCoins, 0);
        Diamonds = PlayerPrefs.GetInt(PrefDiamonds, 0);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(PrefCoins, Coins);
        PlayerPrefs.SetInt(PrefDiamonds, Diamonds);
        PlayerPrefs.Save();
    }
}
