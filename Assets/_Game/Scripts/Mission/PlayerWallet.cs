using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public int Coins    { get; private set; }
    public int Diamonds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCoin(int amount)
    {
        Coins += amount;
        Debug.Log($"[Wallet] +{amount} Coin | Tổng: {Coins}");
    }

    public void AddDiamond(int amount)
    {
        Diamonds += amount;
        Debug.Log($"[Wallet] +{amount} Diamond | Tổng: {Diamonds}");
    }

    public bool SpendCoin(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }

    public bool SpendDiamond(int amount)
    {
        if (Diamonds < amount) return false;
        Diamonds -= amount;
        return true;
    }
}
