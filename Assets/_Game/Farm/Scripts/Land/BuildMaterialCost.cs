using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// ============================================================================
/// CHI PHI NGUYEN LIEU — dung chung cho MOI thu can vat lieu xay dung
/// ============================================================================
///
/// Dung o:
///   • Mua / don o dat mo rong   (LandRegionData)
///   • Nang cap slot kho          (WarehouseUpgradeCostTable)
///   • Xay nha dan / cong trinh   (PlaceableItemData.materialCosts)
///
/// NGUYEN LIEU LAY TU TAU LUA. Catalog hien co trong du an
/// (Assets/_Game/Farm/data/item_taulua/):
///     "go"   Go      |  "da"   Da
///     "kinh" Kinh    |  "dinh" Dinh
///     "son"  Son
/// Bo nguyen lieu cua game dung 4 mon: go, da, dinh, kinh (chot vong 14c).
/// Muon them mon moi thi chi can tao asset InventoryItemData — KHONG phai sua code o day.
/// </summary>
[Serializable]
public struct BuildMaterialCost
{
    [Tooltip("Ma nguyen lieu — phai khop itemId cua InventoryItemData. Vd: go, da, kinh, dinh")]
    public string itemId;

    [Min(0)]
    [Tooltip("So luong can.")]
    public int amount;

    public BuildMaterialCost(string id, int n) { itemId = id; amount = n; }

    public bool IsValid => !string.IsNullOrWhiteSpace(itemId) && amount > 0;
}

/// <summary>Tien ich kiem tra / tru nguyen lieu trong kho.</summary>
public static class BuildMaterials
{
    /// <summary>Ten hien thi mac dinh khi khong tra duoc catalog.</summary>
    public static string DisplayNameOf(string itemId)
    {
        switch ((itemId ?? "").Trim().ToLowerInvariant())
        {
            case "go":   return "Gỗ";
            case "da":   return "Đá";
            case "kinh": return "Kính";
            case "dinh": return "Đinh";
            case "son":  return "Sơn";
            default:     return itemId;
        }
    }

    /// <summary>So luong dang co trong kho.</summary>
    public static int Owned(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        var inv = FarmInventoryManager.Instance;
        return inv != null ? inv.GetAmount(itemId) : 0;
    }

    /// <summary>Du toan bo nguyen lieu chua.</summary>
    public static bool HasAll(IList<BuildMaterialCost> costs)
    {
        if (costs == null) return true;
        for (int i = 0; i < costs.Count; i++)
        {
            var c = costs[i];
            if (!c.IsValid) continue;
            if (Owned(c.itemId) < c.amount) return false;
        }
        return true;
    }

    /// <summary>Danh sach thu con THIEU (de hien UI "can them X go").</summary>
    public static List<BuildMaterialCost> Missing(IList<BuildMaterialCost> costs)
    {
        var missing = new List<BuildMaterialCost>();
        if (costs == null) return missing;
        for (int i = 0; i < costs.Count; i++)
        {
            var c = costs[i];
            if (!c.IsValid) continue;
            int lack = c.amount - Owned(c.itemId);
            if (lack > 0) missing.Add(new BuildMaterialCost(c.itemId, lack));
        }
        return missing;
    }

    /// <summary>
    /// Tru nguyen lieu. Chi tru khi DU HET — khong bao gio tru mot nua roi bao loi.
    /// </summary>
    public static bool TrySpend(IList<BuildMaterialCost> costs)
    {
        if (costs == null || costs.Count == 0) return true;
        if (!HasAll(costs)) return false;

        var inv = FarmInventoryManager.Instance;
        if (inv == null) return false;

        // Da kiem tra du o tren nen vong nay khong the that bai giua chung.
        for (int i = 0; i < costs.Count; i++)
        {
            var c = costs[i];
            if (!c.IsValid) continue;
            inv.RemoveItem(c.itemId, c.amount);
        }
        return true;
    }

    /// <summary>Chuoi mo ta ngan: "12 Go, 8 Da" — dung cho toast / log.</summary>
    public static string Describe(IList<BuildMaterialCost> costs, bool onlyMissing = false)
    {
        if (costs == null || costs.Count == 0) return "";
        var list = onlyMissing ? Missing(costs) : costs;
        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].IsValid) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(list[i].amount).Append(' ').Append(Loc.T(DisplayNameOf(list[i].itemId)));
        }
        return sb.ToString();
    }

    /// <summary>Icon cua nguyen lieu, tra tu catalog InventoryItemData trong Resources (neu co).</summary>
    public static Sprite IconOf(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        if (_iconCache.TryGetValue(itemId, out var cached)) return cached;

        // 🔴 VONG 14b — VI SAO PHAI CO _registry:
        // Ham nay von chi doc Resources.LoadAll, nhung 5 asset nguyen lieu
        // (Go/Da/Kinh/Dinh/Son) nam o Assets/_Game/Farm/data/item_taulua/ — KHONG
        // nam trong bat ky thu muc Resources/ nao. Nen IconOf() luon tra null va
        // moi khung nguyen lieu deu ve o trong. Gio cho phep man hinh nao dang giu
        // san danh sach InventoryItemData (vd WarehousePopupUI.extraItemDatabase)
        // nap vao day, khoi phai di chuyen asset.
        Sprite found = null;
        if (_registry.TryGetValue(itemId, out var reg) && reg != null) { found = reg.icon; }

        // [FIX 2026-09-21] Tu nap luoi danh muc (1 lan, cache static) thay vi cho man hinh khac
        // goi NapDanhMuc — WarehousePopupUI chi nap trong Awake, popup kho tat trong scene thi
        // Awake chua chay => popup mua dat mo truoc se mat het icon nguyen lieu.
        if (found == null)
        {
            string ten = DisplayNameOf(itemId);
            foreach (var d in DanhMucTuNap())
            {
                if (d == null || d.icon == null) continue;
                if (string.Equals(d.itemId, itemId, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(d.displayName) &&
                        (string.Equals(d.displayName, itemId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(d.displayName, ten, StringComparison.OrdinalIgnoreCase)))
                    || string.Equals(d.name, ten, StringComparison.OrdinalIgnoreCase))
                { found = d.icon; break; }
            }
        }
        // Fallback cuoi: sprite roi trong Resources theo ma nguyen lieu.
        if (found == null) found = Resources.Load<Sprite>("Icons/" + itemId);
        if (found == null) found = Resources.Load<Sprite>("UI/Standard/" + itemId);

        _iconCache[itemId] = found;
        return found;
    }

    private static InventoryItemData[] _danhMucTuNap;

    /// <summary>
    /// Danh muc InventoryItemData nap 1 lan: Resources.LoadAll (build) + AssetDatabase (Editor,
    /// vi 5 asset nguyen lieu nam o Assets/_Game/Farm/data/item_taulua/, ngoai Resources).
    /// </summary>
    private static InventoryItemData[] DanhMucTuNap()
    {
        if (_danhMucTuNap != null) return _danhMucTuNap;
        var list = new List<InventoryItemData>(Resources.LoadAll<InventoryItemData>(""));
#if UNITY_EDITOR
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:InventoryItemData"))
        {
            var d = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (d != null && !list.Contains(d)) list.Add(d);
        }
#endif
        _danhMucTuNap = list.ToArray();
        return _danhMucTuNap;
    }

    private static readonly Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();

    /// <summary>Danh muc nguyen lieu do man hinh khac nap vao (xem IconOf).</summary>
    private static readonly Dictionary<string, InventoryItemData> _registry =
        new Dictionary<string, InventoryItemData>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Nap danh sach InventoryItemData de IconOf() tra duoc icon that.
    /// Goi tu bat ky man hinh nao dang giu san danh sach (WarehousePopupUI, Shop...).
    /// Goi bao nhieu lan cung duoc, chi ghi de khi entry cu rong.
    /// </summary>
    public static void NapDanhMuc(IEnumerable<InventoryItemData> ds)
    {
        if (ds == null) { return; }
        bool coMoi = false;
        foreach (var d in ds)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.itemId)) { continue; }
            if (_registry.TryGetValue(d.itemId, out var cu) && cu != null && cu.icon != null) { continue; }
            _registry[d.itemId] = d;
            coMoi = true;
        }
        if (coMoi) { _iconCache.Clear(); }
    }

    /// <summary>Xoa cache icon (goi khi doi scene / reload catalog).</summary>
    public static void ClearIconCache() { _iconCache.Clear(); _danhMucTuNap = null; }
}
