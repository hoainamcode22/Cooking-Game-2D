using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Cáº¯m GIá»Ž CÃ vÃ o QUáº¦Y HÃ€NG cá»§a farm (VÃ²ng 16): implement IStallExternalStore trÃªn FishBasket vÃ  Ä‘Äƒng kÃ½
    /// StallSourceStore.FishBasket qua StallExternalStores á»Ÿ BeforeSceneLoad (trÆ°á»›c Awake/Start cá»§a PlayerStallManager,
    /// Ä‘á»ƒ nhá»‹p TickStall Ä‘áº§u tiÃªn hoÃ n Ä‘Æ°á»£c cÃ¡ cÃ²n treo refundPending). FishBasket lÃ  plain C# lazy-load PlayerPrefs
    /// nÃªn cháº¡y Ä‘Æ°á»£c ngay á»Ÿ SCN_Farm, khÃ´ng cáº§n object nÃ o cá»§a scene cÃ¢u.
    /// Táº®T: khÃ´ng cÃ³ FishingDatabase hoáº·c config.enabled == false â†’ khÃ´ng Ä‘Äƒng kÃ½ â†’ quáº§y hÃ ng y nhÆ° cÅ©.
    /// B8: TryTake khÃ´ng Ä‘á»§ â†’ false khÃ´ng trá»«; GiveBack chá»‰ true khi giá» nháº­n TRá»ŒN sá»‘ lÆ°á»£ng (khÃ´ng dá»±a vÃ o
    /// FishBasket.TryAdd vÃ¬ hÃ m Ä‘Ã³ Káº¸P vá» MaxPerType â€” káº¹p lÃ  máº¥t cÃ¡ im láº·ng).
    /// </summary>
    public sealed class FishBasketStallStore : IStallExternalStore
    {
        /// <summary>Tiá»n tá»‘ id cÃ¡ â€” tráº£ lá»i nhanh "khÃ´ng pháº£i cá»§a tÃ´i" cho má»i id farm, khÃ´ng pháº£i quÃ©t database.</summary>
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
            if (!FarmGame.Fishing.FishingFeatureGate.IsEnabled) return;
            EnsureRegistered();
        }

        /// <summary>ÄÄƒng kÃ½ náº¿u chÆ°a (idempotent). Tráº£ false khi há»‡ Há»“ CÃ¢u táº¯t / chÆ°a cÃ³ database.</summary>
        public static bool EnsureRegistered()
        {
            if (_registered != null) { return true; }
            if (!FishingDatabase.IsEnabled)
            {
                Debug.Log(FishingIds.LogTag + " Há»‡ Há»“ CÃ¢u táº¯t/chÆ°a cÃ³ database â†’ khÃ´ng cáº¯m giá» cÃ¡ vÃ o quáº§y hÃ ng.");
                return false;
            }
            var store = new FishBasketStallStore();
            if (!StallExternalStores.Register(StallSourceStore.FishBasket, store)) { return false; }
            _registered = store;
            return true;
        }

        /// <summary>Äang cáº¯m vÃ o quáº§y chÆ°a (UI/tool há»i).</summary>
        public static bool IsRegistered { get { return _registered != null; } }

        private static FishData Resolve(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) { return null; }
            string key = FishBasket.NormalizeKey(itemId);
            if (!key.StartsWith(FishIdPrefix, System.StringComparison.Ordinal)) { return null; }
            return FishingCatchResolver.FindFishLoose(FishingDatabase.Instance, key);
        }

        // â”€â”€ IStallExternalStore â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            // FishBasket.Remove: khÃ´ng Ä‘á»§ â†’ false vÃ  khÃ´ng Ä‘á»•i gÃ¬ (Ä‘Ãºng há»£p Ä‘á»“ng B8).
            return FishBasket.Instance.Remove(itemId, amount);
        }

        public bool GiveBack(string itemId, int amount)
        {
            if (amount <= 0) { return true; }
            string key = FishBasket.NormalizeKey(itemId);
            if (string.IsNullOrEmpty(key)) { return false; }

            FishBasket basket = FishBasket.Instance;
            int having = basket.Count(key);

            // Pháº£i NHáº¬N TRá»ŒN: TryAdd káº¹p vá» MaxPerType nÃªn náº¿u Ä‘á»ƒ nÃ³ tá»± xá»­ thÃ¬ pháº§n vÆ°á»£t tráº§n biáº¿n máº¥t.
            // KhÃ´ng Ä‘á»§ chá»— â†’ false, quáº§y giá»¯ refundPending vÃ  thá»­ láº¡i nhá»‹p sau (ngÆ°á»i chÆ¡i bÃ¡n/táº·ng cÃ¡ bá»›t lÃ  hoÃ n Ä‘Æ°á»£c).
            if (having + amount > basket.MaxPerType) { return false; }
            if (having == 0 && basket.IsFull) { return false; }

            string reason;
            bool ok = basket.TryAdd(key, amount, out reason);
            if (!ok) { Debug.LogWarning(FishingIds.LogTag + " Quáº§y hoÃ n " + key + " x" + amount + " vá» giá» tháº¥t báº¡i: " + reason + " â€” sáº½ thá»­ láº¡i."); }
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
                // LoÃ i khÃ´ng cÃ²n trong database (asset bá»‹ xoÃ¡) thÃ¬ khÃ´ng lÃªn quáº§y: khÃ´ng tra Ä‘Æ°á»£c tÃªn/giÃ¡,
                // quáº§y sáº½ bÃ¡n vá»›i giÃ¡ dá»± phÃ²ng 10 â€” váº«n náº±m trong giá», bÃ¡n á»Ÿ Quáº§y CÃ¡ riÃªng náº¿u muá»‘n.
                if (Resolve(s.fishId) == null) { continue; }
                into.Add(new StallSellableItem { itemId = s.fishId, amount = s.amount, store = StallSourceStore.FishBasket });
            }
        }
    }
}
