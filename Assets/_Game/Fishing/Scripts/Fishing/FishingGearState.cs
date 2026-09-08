using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Cần câu đang sở hữu + độ bền + cần đang cầm — plain C#, static sống qua scene, lưu PlayerPrefs FISHING_GEAR_SAVE
    /// blob JSON {saveVersion, owned, equippedRodItemId}. Mua ở Quầy Cá (Dev C gọi TryBuy), trừ độ bền mỗi lần QUĂNG (Dev B).
    /// RodData tra qua FishingDatabase.Instance.FindRod — không có database thì coi như không có cần dùng được.
    /// </summary>
    public sealed class FishingGearState
    {
        [Serializable]
        private class SaveDto
        {
            public int saveVersion;
            public List<OwnedRod> owned = new List<OwnedRod>();
            public string equippedRodItemId;
        }

        private static FishingGearState _instance;

        public static FishingGearState Instance
        {
            get
            {
                if (_instance == null) { _instance = new FishingGearState(); _instance.Load(); }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }

        private readonly List<OwnedRod> _owned = new List<OwnedRod>();
        private string _equippedId;

        private FishingGearState() { }

        /// <summary>Cần đang cầm; null = chưa có / database chưa nạp / id lạ.</summary>
        public RodData EquippedRod
        {
            get
            {
                if (string.IsNullOrEmpty(_equippedId)) { return null; }
                FishingDatabase db = FishingDatabase.Instance;
                return db != null ? db.FindRod(_equippedId) : null;
            }
        }

        public int EquippedDurabilityLeft
        {
            get { OwnedRod o = FindOwned(_equippedId); return o != null ? o.durabilityLeft : 0; }
        }

        public bool HasUsableRod { get { return EquippedRod != null && EquippedDurabilityLeft > 0; } }

        public IReadOnlyList<OwnedRod> Owned { get { return _owned; } }

        public event Action OnChanged;

        public bool CanAfford(RodData rod)
        {
            if (rod == null) { return false; }
            FarmEconomyManager eco = FarmEconomyManager.Instance;
            if (eco == null) { return false; }
            return rod.IsGemRod ? eco.Gems >= rod.diamondPrice : eco.Gold >= rod.goldPrice;
        }

        /// <summary>
        /// Mua cần: kiểm cấp → trừ tiền (gem nếu IsGemRod, không thì vàng) → thêm/cộng độ bền → trang bị → lưu.
        /// Đã sở hữu cùng loại còn dùng được → cộng độ bền vào entry cũ (không tạo trùng).
        /// Tiếng "mua bán" do FarmEconomyManager.SpendGold/SpendGems tự phát, KHÔNG phát thêm ở đây (tránh kêu 2 lần).
        /// </summary>
        public bool TryBuy(RodData rod, out string reasonVi)
        {
            reasonVi = string.Empty;
            if (rod == null || string.IsNullOrEmpty(rod.itemID)) { reasonVi = "Cần câu không hợp lệ"; return false; }

            int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            if (rod.unlockLevel > level)
            {
                reasonVi = "Cần mở ở cấp " + rod.unlockLevel.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            FarmEconomyManager eco = FarmEconomyManager.Instance;
            if (eco == null) { reasonVi = "Chưa có hệ tiền"; return false; }

            bool paid = rod.IsGemRod ? eco.SpendGems(rod.diamondPrice) : eco.SpendGold(rod.goldPrice);
            if (!paid)
            {
                reasonVi = rod.IsGemRod ? "Không đủ kim cương" : "Không đủ vàng";
                return false;
            }

            int casts = Mathf.Max(1, rod.durabilityCasts);
            OwnedRod existing = FindOwned(rod.itemID);
            if (existing != null && existing.durabilityLeft > 0)
            {
                existing.durabilityLeft += casts;
            }
            else
            {
                if (existing != null) { _owned.Remove(existing); }
                _owned.Add(new OwnedRod { rodItemId = rod.itemID, durabilityLeft = casts });
            }
            _equippedId = rod.itemID;

            Debug.Log(FishingIds.LogTag + " Mua cần " + rod.itemID + " (tier " + rod.tier + ", +" + casts + " lần quăng), giá " + (rod.IsGemRod ? rod.diamondPrice + " gem" : rod.goldPrice + " vàng"));
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Cầm cần khác đang sở hữu và còn độ bền.</summary>
        public bool Equip(string rodItemId)
        {
            OwnedRod o = FindOwned(rodItemId);
            if (o == null || o.durabilityLeft <= 0) { return false; }
            if (_equippedId == o.rodItemId) { return true; }
            _equippedId = o.rodItemId;
            Save();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Trừ 1 độ bền cho lần QUĂNG. Về 0 → xoá cần khỏi Owned, tự cầm cần khác còn dùng được (nếu có) và trả FALSE
        /// (= vừa hỏng, controller không quăng). Không có cần → false.
        /// </summary>
        public bool ConsumeCast()
        {
            OwnedRod o = FindOwned(_equippedId);
            if (o == null || o.durabilityLeft <= 0) { return false; }

            o.durabilityLeft -= 1;
            bool stillUsable = o.durabilityLeft > 0;
            if (!stillUsable)
            {
                _owned.Remove(o);
                _equippedId = null;
                for (int i = 0; i < _owned.Count; i++)
                {
                    if (_owned[i].durabilityLeft > 0) { _equippedId = _owned[i].rodItemId; break; }
                }
                Debug.Log(FishingIds.LogTag + " Cần " + o.rodItemId + " đã hỏng" + (_equippedId != null ? ", tự cầm " + _equippedId : "."));
            }
            Save();
            OnChanged?.Invoke();
            return stillUsable;
        }

        public void Save()
        {
            var dto = new SaveDto { saveVersion = FishingIds.SaveVersion, equippedRodItemId = _equippedId ?? string.Empty };
            for (int i = 0; i < _owned.Count; i++)
            {
                OwnedRod o = _owned[i];
                if (o == null || string.IsNullOrEmpty(o.rodItemId) || o.durabilityLeft <= 0) { continue; }
                dto.owned.Add(new OwnedRod { rodItemId = o.rodItemId, durabilityLeft = o.durabilityLeft });
            }
            PlayerPrefs.SetString(FishingIds.PrefsGear, JsonUtility.ToJson(dto));
            LuuGopPrefs.Hen();
        }

        private void Load()
        {
            _owned.Clear();
            _equippedId = null;
            bool has = PlayerPrefs.HasKey(FishingIds.PrefsGear);
            SaveVersionGuard.Ensure(FishingIds.SaveFamily, FishingIds.SaveVersion, null, has);
            if (!has) { return; }

            string json = PlayerPrefs.GetString(FishingIds.PrefsGear, string.Empty);
            if (string.IsNullOrEmpty(json)) { return; }

            SaveDto dto = null;
            try { dto = JsonUtility.FromJson<SaveDto>(json); }
            catch (Exception e) { Debug.LogWarning(FishingIds.LogTag + " FISHING_GEAR_SAVE hỏng, bỏ qua: " + e.Message); }
            if (dto == null) { return; }

            if (dto.owned != null)
            {
                for (int i = 0; i < dto.owned.Count; i++)
                {
                    OwnedRod o = dto.owned[i];
                    if (o == null || string.IsNullOrEmpty(o.rodItemId) || o.durabilityLeft <= 0) { continue; }
                    // Gộp entry trùng id (save cũ) thành 1.
                    OwnedRod cur = FindOwned(o.rodItemId);
                    if (cur != null) { cur.durabilityLeft += o.durabilityLeft; }
                    else { _owned.Add(new OwnedRod { rodItemId = o.rodItemId, durabilityLeft = o.durabilityLeft }); }
                }
            }

            OwnedRod eq = FindOwned(dto.equippedRodItemId);
            if (eq != null) { _equippedId = eq.rodItemId; }
            else if (_owned.Count > 0) { _equippedId = _owned[0].rodItemId; }
        }

        private OwnedRod FindOwned(string rodItemId)
        {
            if (string.IsNullOrEmpty(rodItemId)) { return null; }
            for (int i = 0; i < _owned.Count; i++) { if (_owned[i].rodItemId == rodItemId) { return _owned[i]; } }
            return null;
        }
    }
}
