using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Cắm GIỎ CÁ vào QUẦY HÀNG của farm (Vòng 16): implement IStallExternalStore trên FishBasket và đăng ký
    /// StallSourceStore.FishBasket qua StallExternalStores ở BeforeSceneLoad (trước Awake/Start của PlayerStallManager,
    /// để nhịp TickStall đầu tiên hoàn được cá còn treo refundPending). FishBasket là plain C# lazy-load PlayerPrefs
    /// nên chạy được ngay ở SCN_Farm, không cần object nào của scene câu.
    /// TẮT: không có FishingDatabase hoặc config.enabled == false → không đăng ký → quầy hàng y như cũ.
    /// B8: TryTake không đủ → false không trừ; GiveBack chỉ true khi giỏ nhận TRỌN số lượng (không dựa vào
    /// FishBasket.TryAdd vì hàm đó KẸP về MaxPerType — kẹp là mất cá im lặng).
    /// </summary>
    public sealed class FishBasketStallStore : IStallExternalStore
    {
        /// <summary>Tiền tố id cá — trả lời nhanh "không phải của tôi" cho mọi id farm, không phải quét database.</summary>
        private const string FishIdPrefix = "fish_";

        private static FishBasketStallStore _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _registered = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            EnsureRegistered();
        }

        /// <summary>Đăng ký nếu chưa (idempotent). Trả false khi hệ Hồ Câu tắt / chưa có database.</summary>
        public static bool EnsureRegistered()
        {
            if (_registered != null) { return true; }
            if (!FishingDatabase.IsEnabled)
            {
                Debug.Log(FishingIds.LogTag + " Hệ Hồ Câu tắt/chưa có database → không cắm giỏ cá vào quầy hàng.");
                return false;
            }
            var store = new FishBasketStallStore();
            if (!StallExternalStores.Register(StallSourceStore.FishBasket, store)) { return false; }
            _registered = store;
            return true;
        }

        /// <summary>Đang cắm vào quầy chưa (UI/tool hỏi).</summary>
        public static bool IsRegistered { get { return _registered != null; } }

        private static FishData Resolve(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) { return null; }
            string key = FishBasket.NormalizeKey(itemId);
            if (!key.StartsWith(FishIdPrefix, System.StringComparison.Ordinal)) { return null; }
            return FishingCatchResolver.FindFishLoose(FishingDatabase.Instance, key);
        }

        // ── IStallExternalStore ──────────────────────────────────────────────

        public bool TryGetItemInfo(string itemId, out StallExternalItemInfo info)
        {
            info = default;
            FishData fish = Resolve(itemId);
            if (fish == null) { return false; }
            info.icon = fish.icon;
            info.displayName = string.IsNullOrEmpty(fish.displayName) ? fish.fishId : fish.displayName;
            info.category = StallItemCategory.Ca;
            info.baseSellGold = Mathf.Max(0, fish.sellPrice);
            return true;
        }

        public int GetAvailable(string itemId)
        {
            if (Resolve(itemId) == null) { return 0; }
            return FishBasket.Instance.Count(itemId);
        }

        public bool TryTake(string itemId, int amount)
        {
            if (amount <= 0 || Resolve(itemId) == null) { return false; }
            // FishBasket.Remove: không đủ → false và không đổi gì (đúng hợp đồng B8).
            return FishBasket.Instance.Remove(itemId, amount);
        }

        public bool GiveBack(string itemId, int amount)
        {
            if (amount <= 0) { return true; }
            string key = FishBasket.NormalizeKey(itemId);
            if (string.IsNullOrEmpty(key)) { return false; }

            FishBasket basket = FishBasket.Instance;
            int having = basket.Count(key);

            // Phải NHẬN TRỌN: TryAdd kẹp về MaxPerType nên nếu để nó tự xử thì phần vượt trần biến mất.
            // Không đủ chỗ → false, quầy giữ refundPending và thử lại nhịp sau (người chơi bán/tặng cá bớt là hoàn được).
            if (having + amount > basket.MaxPerType) { return false; }
            if (having == 0 && basket.IsFull) { return false; }

            string reason;
            bool ok = basket.TryAdd(key, amount, out reason);
            if (!ok) { Debug.LogWarning(FishingIds.LogTag + " Quầy hoàn " + key + " x" + amount + " về giỏ thất bại: " + reason + " — sẽ thử lại."); }
            return ok;
        }

        public void EnumerateSellable(List<StallSellableItem> into)
        {
            if (into == null) { return; }
            IReadOnlyList<FishStack> items = FishBasket.Instance.Items;
            for (int i = 0; i < items.Count; i++)
            {
                FishStack s = items[i];
                if (s == null || s.amount <= 0) { continue; }
                // Loài không còn trong database (asset bị xoá) thì không lên quầy: không tra được tên/giá,
                // quầy sẽ bán với giá dự phòng 10 — vẫn nằm trong giỏ, bán ở Quầy Cá riêng nếu muốn.
                if (Resolve(s.fishId) == null) { continue; }
                into.Add(new StallSellableItem { itemId = s.fishId, amount = s.amount, store = StallSourceStore.FishBasket });
            }
        }
    }
}
