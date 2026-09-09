using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SỔ ĐĂNG KÝ KHO NGOÀI của quầy hàng (Vòng 16 · Hồ Câu). Static, không cần object trong scene.
///
/// Module ngoài gọi <see cref="Register"/> (thường trong
/// <c>[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]</c> để có trước Awake của quầy).
/// Không ai đăng ký → mọi hàm trả false/không thêm gì → <see cref="PlayerStallManager"/> chạy y như cũ.
///
/// Chỉ nhận giá trị <see cref="StallSourceStore"/> KHÁC hai kho farm gốc: FarmInventory và
/// SeedWarehouse đã có đường xử lý riêng trong quầy, cho đè lên là đổi hành vi đang chạy.
/// </summary>
public static class StallExternalStores
{
    private static readonly Dictionary<StallSourceStore, IStallExternalStore> Map =
        new Dictionary<StallSourceStore, IStallExternalStore>();

    // Enter Play Mode Options có thể giữ static giữa hai lần Play → xoá để module đăng ký lại sạch.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Map.Clear();
    }

    public static int Count => Map.Count;

    /// <summary>Kho farm gốc — không cho đăng ký đè.</summary>
    public static bool IsBuiltIn(StallSourceStore store)
    {
        return store == StallSourceStore.FarmInventory || store == StallSourceStore.SeedWarehouse;
    }

    /// <summary>Đăng ký (ghi đè nếu cùng store đã có — module gọi lại sau domain reload). Trả false khi bị từ chối.</summary>
    public static bool Register(StallSourceStore store, IStallExternalStore impl)
    {
        if (impl == null)
        {
            return false;
        }

        if (IsBuiltIn(store))
        {
            Debug.LogWarning("[QuầyHàng] Từ chối đăng ký kho ngoài cho " + store + " — đây là kho farm gốc.");
            return false;
        }

        Map[store] = impl;
        Debug.Log("[QuầyHàng] Đã cắm kho ngoài " + store + " (" + impl.GetType().Name + ") vào quầy hàng.");
        return true;
    }

    /// <summary>Chỉ gỡ đúng implement đang giữ (tránh cái cũ chết sau xoá mất cái mới).</summary>
    public static void Unregister(StallSourceStore store, IStallExternalStore impl)
    {
        if (Map.TryGetValue(store, out IStallExternalStore cur) && (impl == null || ReferenceEquals(cur, impl)))
        {
            Map.Remove(store);
        }
    }

    public static bool Has(StallSourceStore store)
    {
        return Map.ContainsKey(store);
    }

    public static bool TryGet(StallSourceStore store, out IStallExternalStore impl)
    {
        return Map.TryGetValue(store, out impl);
    }

    /// <summary>Kho ngoài nào BIẾT mặt hàng này (kể cả đang giữ 0). Dùng để xác định kho nguồn của itemId lạ.</summary>
    public static bool TryFindOwner(string itemId, out StallSourceStore store, out IStallExternalStore impl, out StallExternalItemInfo info)
    {
        store = StallSourceStore.FarmInventory;
        impl = null;
        info = default;

        if (string.IsNullOrEmpty(itemId) || Map.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<StallSourceStore, IStallExternalStore> kv in Map)
        {
            if (kv.Value != null && kv.Value.TryGetItemInfo(itemId, out info))
            {
                store = kv.Key;
                impl = kv.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>Gộp hàng bán được của mọi kho ngoài vào danh sách (không Clear).</summary>
    public static void EnumerateSellable(List<StallSellableItem> into)
    {
        if (into == null || Map.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<StallSourceStore, IStallExternalStore> kv in Map)
        {
            if (kv.Value == null)
            {
                continue;
            }

            int before = into.Count;
            kv.Value.EnumerateSellable(into);

            // Ép đúng khoá kho: implement quên điền `store` thì hàng hoàn về sẽ lạc kho (B8).
            for (int i = before; i < into.Count; i++)
            {
                StallSellableItem it = into[i];
                if (it.store != kv.Key)
                {
                    it.store = kv.Key;
                    into[i] = it;
                }
            }
        }
    }
}
