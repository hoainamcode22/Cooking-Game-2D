using System;
using UnityEngine;

public class FarmEconomyManager : MonoBehaviour
{
    public static FarmEconomyManager Instance { get; private set; }

    [SerializeField] private int startGold = 1250;
    [SerializeField] private int startGems = 10;

    public int Gold { get; private set; }
    public int Gems { get; private set; }

    public event Action<int, int> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = startGold;
        Gems = startGems;
    }

    private void Start()
    {
        NotifyCurrencyChanged();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
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
        NotifyCurrencyChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        NotifyCurrencyChanged();
    }

    public void AddGems(int amount)
    {
        if (amount <= 0)
            return;

        Gems += amount;
        NotifyCurrencyChanged();
    }

    private void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke(Gold, Gems);
    }
}