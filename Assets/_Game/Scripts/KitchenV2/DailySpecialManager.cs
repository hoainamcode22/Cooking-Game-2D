using System;
using System.Collections.Generic;
using System.Globalization;   // F6: NumberStyles / CultureInfo.InvariantCulture
using UnityEngine;

namespace KitchenUIv2
{
    /// <summary>
    /// MÓN HÔM NAY (bảng đen trong bếp — duyệt full logic 2026-08-26):
    /// mỗi ngày chọn cố định 3 món từ sổ công thức (seed theo ngày — mọi lần mở game
    /// trong cùng ngày ra cùng 3 món), nấu ĐẠT món trong bảng được thưởng THÊM VÀNG.
    ///
    /// ⚠ KINH TẾ: goldBonusMultiplier mặc định 1.5 (+50% vàng) — CON SỐ CHỜ SẾP DUYỆT,
    /// chỉnh trong Inspector không cần sửa code. Chỉ nhân phần VÀNG, không nhân EXP
    /// (EXP giữ nguyên để không phá đường cong level đã duyệt).
    /// </summary>
    public class DailySpecialManager : MonoBehaviour
    {
        public static DailySpecialManager Instance { get; private set; }

        [Header("Data")]
        [Tooltip("Sổ công thức — tool Setup gán (ListDishData).")]
        [SerializeField] private ListDishData dishBook;

        [Header("Kinh tế (CHỜ DUYỆT SỐ)")]
        [Tooltip("Hệ số nhân VÀNG khi nấu đạt món-hôm-nay. 1.5 = +50%. Đặt 1 = tắt bonus.")]
        [SerializeField] private float goldBonusMultiplier = 1.5f;

        [Tooltip("Số món chọn mỗi ngày")]
        [SerializeField] private int dishesPerDay = 3;

        private readonly List<DishData> _today = new List<DishData>();
        private string _cachedDayKey;

        /// <summary>3 món của hôm nay (rỗng nếu chưa có data).</summary>
        public IReadOnlyList<DishData> TodayDishes
        {
            get { RefreshIfNewDay(); return _today; }
        }

        public event Action OnTodayListChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() => RefreshIfNewDay();

        public bool IsSpecialToday(DishData dish)
        {
            if (dish == null) return false;
            RefreshIfNewDay();
            for (int i = 0; i < _today.Count; i++)
                if (_today[i] != null && _today[i].dishId == dish.dishId) return true;
            return false;
        }

        /// <summary>
        /// Hook additive từ CookingChallengeManager.HandleCookingSuccess — null-safe tuyệt đối:
        /// chưa có Instance trong scene thì trả nguyên vàng gốc, không đổi hành vi cũ.
        /// </summary>
        public static int ApplyGoldBonus(DishData dish, int baseGold)
        {
            if (Instance == null || dish == null || baseGold <= 0) return baseGold;
            if (!Instance.IsSpecialToday(dish)) return baseGold;

            int bonus = Mathf.CeilToInt(baseGold * Mathf.Max(1f, Instance.goldBonusMultiplier));
            if (bonus > baseGold)
                FarmUIManager.Instance?.ShowHint(Loc.TF("Món hôm nay! +{0} vàng thưởng thêm.", bonus - baseGold));
            return bonus;
        }

        /// <summary>Seed cố định theo ngày: cùng ngày = cùng 3 món, ngày mới tự đổi.</summary>
        private void RefreshIfNewDay()
        {
            // F6: PHAI ep InvariantCulture. Mac dinh ToString() dung lich cua may:
            // tren may dat lich Phat lich / Hijri thi "yyyy" ra 2569 / 1447, tren locale
            // chu so khong phai ASCII (ar-SA...) thi ra chu so A-rap — ca hai deu lam
            // int.Parse ben duoi nem FormatException.
            string dayKey = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            if (dayKey == _cachedDayKey && _today.Count > 0) return;

            _cachedDayKey = dayKey;
            _today.Clear();

            if (dishBook == null || dishBook.allDishes == null || dishBook.allDishes.Count == 0)
                return;

            // Chỉ chọn trong các món ĐÃ MỞ theo level người chơi — bảng không quảng cáo món chưa nấu được
            int playerLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            var pool = new List<DishData>();
            foreach (var d in dishBook.allDishes)
                if (d != null && d.unlockLevel <= playerLevel) pool.Add(d);
            if (pool.Count == 0) pool.AddRange(dishBook.allDishes);

            // F6: khong duoc int.Parse tran. Save/locale hong khong duoc lam chet ca
            // bang "Mon hom nay"; roi ve so ngay tuyet doi — van co dinh trong ngay.
            if (!int.TryParse(dayKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int daySeed))
            {
                daySeed = (int)(DateTime.UtcNow.Date - new DateTime(2020, 1, 1)).TotalDays;
                Debug.LogWarning($"[DailySpecial] Khong doc duoc ma ngay '{dayKey}' — dung so ngay du phong {daySeed}.");
            }

            var rng = new System.Random(daySeed ^ 0x5EED);
            int want = Mathf.Min(dishesPerDay, pool.Count);
            while (_today.Count < want && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                if (pool[i] != null) _today.Add(pool[i]);
                pool.RemoveAt(i);
            }

            OnTodayListChanged?.Invoke();
        }

        /// <summary>Tool Setup gán data (Editor).</summary>
        public void SetDishBook(ListDishData book) => dishBook = book;
    }
}
