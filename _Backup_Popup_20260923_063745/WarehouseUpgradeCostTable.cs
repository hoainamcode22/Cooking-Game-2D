using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ============================================================================
/// BANG CHI PHI NANG CAP SLOT KHO
/// ============================================================================
///
/// Truoc day WarehousePopupUI.OnUpgradeClicked() cho nang cap MIEN PHI.
/// Tu ban nay, moi cap can VANG + NGUYEN LIEU tau lua mang ve
/// (go / da / kinh / dinh) — nguoi choi buoc phai chay chuyen tau de gom du.
///
/// Cap kho: 1 -> 7 (moi cap +25 slot, xem FarmInventoryManager.SlotsPerWarehouseLevel).
/// Bang duoi la chi phi de len cap KE TIEP tu cap hien tai.
///
/// Sua so lieu: chinh truc tiep trong Table ben duoi, hoac tao asset
/// Resources/WarehouseUpgradeCost.asset de de len (xem LoadOverride).
/// </summary>
public static class WarehouseUpgradeCostTable
{
    /// <summary>Chi phi len cap ke tiep.</summary>
    public struct Entry
    {
        public int gold;
        public List<BuildMaterialCost> materials;
    }

    // level hien tai -> chi phi len level+1
    private static readonly Dictionary<int, Entry> Table = new Dictionary<int, Entry>
    {
        { 1, new Entry { gold =  1500, materials = M(("go", 10), ("da",  6)) } },
        { 2, new Entry { gold =  4000, materials = M(("go", 18), ("da", 12), ("dinh",  8)) } },
        { 3, new Entry { gold =  9000, materials = M(("go", 28), ("da", 20), ("dinh", 14), ("kinh",  6)) } },
        { 4, new Entry { gold = 18000, materials = M(("go", 40), ("da", 30), ("dinh", 22), ("kinh", 14)) } },
        { 5, new Entry { gold = 32000, materials = M(("go", 55), ("da", 45), ("dinh", 32), ("kinh", 24)) } },
        { 6, new Entry { gold = 55000, materials = M(("go", 75), ("da", 60), ("dinh", 45), ("kinh", 38)) } },
    };

    private static List<BuildMaterialCost> M(params (string id, int n)[] items)
    {
        var list = new List<BuildMaterialCost>(items.Length);
        foreach (var (id, n) in items) list.Add(new BuildMaterialCost(id, n));
        return list;
    }

    /// <summary>Cap toi da (khop FarmInventoryManager.MaxWarehouseLevel).</summary>
    public const int MaxLevel = 7;

    /// <summary>Con nang cap duoc khong.</summary>
    public static bool CanUpgradeFurther(int currentLevel) => currentLevel < MaxLevel;

    /// <summary>Chi phi len cap ke tiep. Tra ve entry rong neu da max.</summary>
    public static Entry CostFor(int currentLevel)
    {
        if (Table.TryGetValue(currentLevel, out var e)) return e;
        return new Entry { gold = 0, materials = new List<BuildMaterialCost>() };
    }

    /// <summary>Du dieu kien nang cap chua (vang + nguyen lieu).</summary>
    public static bool CanAfford(int currentLevel, out string reason)
    {
        reason = null;
        if (!CanUpgradeFurther(currentLevel)) { reason = "Kho da o cap toi da."; return false; }

        var e = CostFor(currentLevel);
        var eco = FarmEconomyManager.Instance;
        if (e.gold > 0 && (eco == null || eco.Gold < e.gold))
        {
            reason = $"Thieu vang ({e.gold:n0}).";
            return false;
        }
        if (!BuildMaterials.HasAll(e.materials))
        {
            reason = "Thieu nguyen lieu: " + BuildMaterials.Describe(e.materials, true);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Tru vang + nguyen lieu. Chi tru khi DU CA HAI — khong tru nua chung.
    /// Goi truoc khi tang WAREHOUSE_LEVEL.
    /// </summary>
    public static bool TryPay(int currentLevel)
    {
        if (!CanAfford(currentLevel, out _)) return false;

        var e = CostFor(currentLevel);
        var eco = FarmEconomyManager.Instance;

        if (e.gold > 0)
        {
            if (eco == null || !eco.SpendGold(e.gold)) return false;
        }
        if (!BuildMaterials.TrySpend(e.materials))
        {
            if (e.gold > 0 && eco != null) eco.AddGold(e.gold);   // hoan tien
            return false;
        }
        return true;
    }

    /// <summary>Mo ta chi phi de hien tren nut: "1.500 vang · 10 Go, 6 Da".</summary>
    public static string Describe(int currentLevel)
    {
        if (!CanUpgradeFurther(currentLevel)) return "Da toi da";
        var e = CostFor(currentLevel);
        string mat = BuildMaterials.Describe(e.materials);
        if (e.gold > 0 && !string.IsNullOrEmpty(mat)) return $"{e.gold:n0} vang · {mat}";
        if (e.gold > 0) return $"{e.gold:n0} vang";
        return mat;
    }
}
