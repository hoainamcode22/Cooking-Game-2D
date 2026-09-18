using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Giỏ cá — plain C#, KHÔNG MonoBehaviour, sống static qua scene. Đếm theo LOẠI (slot = số loài) như kho farm:
    /// loài đã có luôn cộng thêm được tới basketMaxPerType; loài mới cần còn slot. Lưu PlayerPrefs FISH_BASKET_SAVE
    /// dạng blob JSON {saveVersion, items}, gộp ghi đĩa qua LuuGopPrefs.Hen(). Khoá fishId chuẩn hoá trim + lower.
    /// </summary>
    public sealed class FishBasket
    {
        [Serializable]
        private class SaveDto
        {
            public int saveVersion;
            public List<FishStack> items = new List<FishStack>();
        }

        private static FishBasket _instance;

        /// <summary>Lazy: lần đầu gọi sẽ nạp từ PlayerPrefs.</summary>
        public static FishBasket Instance
        {
            get
            {
                if (_instance == null) { _instance = new FishBasket(); _instance.Load(); }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Enter Play Mode Options giữ static giữa các lần Play → quên instance để nạp lại từ PlayerPrefs.
            _instance = null;
        }

        // Giữ List (không Dictionary) để thứ tự thêm = thứ tự hiển thị và JsonUtility serialize thẳng.
        private readonly List<FishStack> _items = new List<FishStack>();

        private FishBasket() { }

        private static FishingConfig Cfg { get { return FishingDatabase.ConfigOrDefault; } }

        /// <summary>Số LOẠI tối đa (FishingConfig.basketSlotCapacity).</summary>
        public int SlotCapacity { get { return Mathf.Max(1, Cfg.basketSlotCapacity); } }
        /// <summary>Số con tối đa mỗi loại (FishingConfig.basketMaxPerType).</summary>
        public int MaxPerType { get { return Mathf.Max(1, Cfg.basketMaxPerType); } }
        public int UsedSlots { get { return _items.Count; } }
        public bool IsFull { get { return UsedSlots >= SlotCapacity; } }

        public int TotalCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _items.Count; i++) { n += _items[i].amount; }
                return n;
            }
        }

        public IReadOnlyList<FishStack> Items { get { return _items; } }

        public event Action OnChanged;

        /// <summary>Còn nhận được ít nhất 1 con loài này không.</summary>
        public bool CanAdd(string fishId)
        {
            string key = NormalizeKey(fishId);
            if (string.IsNullOrEmpty(key)) { return false; }
            FishStack s = Find(key);
            if (s != null) { return s.amount < MaxPerType; }
            return !IsFull;
        }

        /// <summary>
        /// Cộng cá. Loài mới khi giỏ đầy → false "Giỏ cá đầy (x/y loại)". Loài đã đủ trần → false "Đã đủ N con loại này".
        /// Cộng vượt trần thì kẹp về trần (vẫn true) — không để cá bốc hơi im lặng.
        /// </summary>
        public bool TryAdd(string fishId, int amount, out string reasonVi)
        {
            reasonVi = string.Empty;
            string key = NormalizeKey(fishId);
            if (string.IsNullOrEmpty(key) || amount <= 0) { reasonVi = Loc.T("Cá không hợp lệ"); return false; }

            FishStack s = Find(key);
            if (s == null)
            {
                if (IsFull)
                {
                    reasonVi = Loc.TF("Giỏ cá đầy ({0}/{1} loại)", UsedSlots.ToString(CultureInfo.InvariantCulture), SlotCapacity.ToString(CultureInfo.InvariantCulture));
                    return false;
                }
                s = new FishStack { fishId = key, amount = 0 };
                _items.Add(s);
            }
            else if (s.amount >= MaxPerType)
            {
                reasonVi = Loc.TF("Đã đủ {0} con loại này", MaxPerType.ToString(CultureInfo.InvariantCulture));
                return false;
            }

            s.amount = Mathf.Min(MaxPerType, s.amount + amount);
            Save();
            OnChanged?.Invoke();
            return true;
        }

        public int Count(string fishId)
        {
            FishStack s = Find(NormalizeKey(fishId));
            return s != null ? s.amount : 0;
        }

        /// <summary>Bớt cá (bán/tặng). Không đủ → false, không đổi gì. Về 0 → xoá loại khỏi giỏ (trả slot).</summary>
        public bool Remove(string fishId, int amount)
        {
            if (amount <= 0) { return false; }
            FishStack s = Find(NormalizeKey(fishId));
            if (s == null || s.amount < amount) { return false; }
            s.amount -= amount;
            if (s.amount <= 0) { _items.Remove(s); }
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Bán hết: xoá sạch giỏ.</summary>
        public void ClearAll()
        {
            if (_items.Count == 0) { return; }
            _items.Clear();
            Save();
            OnChanged?.Invoke();
        }

        public void Save()
        {
            var dto = new SaveDto { saveVersion = FishingIds.SaveVersion };
            for (int i = 0; i < _items.Count; i++)
            {
                FishStack s = _items[i];
                if (s == null || string.IsNullOrEmpty(s.fishId) || s.amount <= 0) { continue; }
                dto.items.Add(new FishStack { fishId = s.fishId, amount = s.amount });
            }
            PlayerPrefs.SetString(FishingIds.PrefsBasket, JsonUtility.ToJson(dto));
            LuuGopPrefs.Hen();
        }

        private void Load()
        {
            _items.Clear();
            bool has = PlayerPrefs.HasKey(FishingIds.PrefsBasket);
            // v1 = định dạng đầu; không có migrate. Họ FISHING dùng chung với FishingGearState — Ensure idempotent.
            SaveVersionGuard.Ensure(FishingIds.SaveFamily, FishingIds.SaveVersion, null, has);
            if (!has) { return; }

            string json = PlayerPrefs.GetString(FishingIds.PrefsBasket, string.Empty);
            if (string.IsNullOrEmpty(json)) { return; }

            SaveDto dto = null;
            try { dto = JsonUtility.FromJson<SaveDto>(json); }
            catch (Exception e) { Debug.LogWarning(FishingIds.LogTag + " FISH_BASKET_SAVE hỏng, bỏ qua: " + e.Message); }
            if (dto == null || dto.items == null) { return; }

            int max = MaxPerType;
            for (int i = 0; i < dto.items.Count; i++)
            {
                FishStack e = dto.items[i];
                if (e == null || e.amount <= 0) { continue; }
                string key = NormalizeKey(e.fishId);
                if (string.IsNullOrEmpty(key)) { continue; }
                // Gộp trùng khoá (save cũ lẫn hoa/thường) và kẹp trần — không xoá cá của người chơi ngoài phần vượt trần.
                FishStack s = Find(key);
                if (s == null) { s = new FishStack { fishId = key, amount = 0 }; _items.Add(s); }
                s.amount = Mathf.Min(max, s.amount + e.amount);
            }
            if (_items.Count > SlotCapacity)
            {
                Debug.LogWarning(FishingIds.LogTag + " Giỏ cá đang giữ " + _items.Count + " loại > " + SlotCapacity + " slot (config đổi?). Giữ nguyên, chỉ không nhận loại mới.");
            }
        }

        private FishStack Find(string key)
        {
            if (string.IsNullOrEmpty(key)) { return null; }
            for (int i = 0; i < _items.Count; i++) { if (_items[i].fishId == key) { return _items[i]; } }
            return null;
        }

        /// <summary>Khoá chuẩn hoá: trim + lower (invariant, tránh lỗi chữ i Thổ Nhĩ Kỳ).</summary>
        public static string NormalizeKey(string fishId)
        {
            return fishId == null ? string.Empty : fishId.Trim().ToLowerInvariant();
        }
    }
}
