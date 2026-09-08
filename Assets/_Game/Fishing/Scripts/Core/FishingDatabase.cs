using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Một asset gom Config + danh sách cá + cần + nhân vật, đặt ở Assets/_Game/Resources/Fishing/FishingDatabase.asset
    /// (giống RewardIconLibrary). Tool FishingDataSetupTool tạo/cập nhật. Runtime dùng FishingDatabase.Instance.
    /// </summary>
    [CreateAssetMenu(fileName = "FishingDatabase", menuName = "Farm Game/Fishing/Database")]
    public class FishingDatabase : ScriptableObject
    {
        public FishingConfig config;
        public List<FishData> fishes = new List<FishData>();
        [Tooltip("Sắp theo tier 1..4.")]
        public List<RodData> rods = new List<RodData>();
        public List<FishingCharacterDef> characters = new List<FishingCharacterDef>();

        private static FishingDatabase _instance;
        private static bool _searched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; _searched = false; _fallbackConfig = null; }

        /// <summary>Nạp 1 lần từ Resources; null nếu chưa chạy tool (mọi hook phải null-check và coi như enabled=false).</summary>
        public static FishingDatabase Instance
        {
            get
            {
                if (_instance == null && !_searched)
                {
                    _searched = true;
                    _instance = Resources.Load<FishingDatabase>(FishingIds.DatabaseResourcePath);
                    if (_instance == null) { Debug.Log(FishingIds.LogTag + " Chưa có FishingDatabase trong Resources — hệ Hồ Câu tắt. Chạy Tools/Farm Game/Hồ Câu/★ SETUP."); }
                }
                return _instance;
            }
        }

        /// <summary>Đúng khi có database + config + config.enabled.</summary>
        public static bool IsEnabled
        {
            get { var db = Instance; return db != null && db.config != null && db.config.enabled; }
        }

        /// <summary>Config an toàn: trả config thật hoặc một bản mặc định tạm (không lưu) để code không null-check khắp nơi.</summary>
        public static FishingConfig ConfigOrDefault
        {
            get
            {
                var db = Instance;
                if (db != null && db.config != null) { return db.config; }
                if (_fallbackConfig == null) { _fallbackConfig = CreateInstance<FishingConfig>(); _fallbackConfig.hideFlags = HideFlags.HideAndDontSave; }
                return _fallbackConfig;
            }
        }
        private static FishingConfig _fallbackConfig;

        public FishData FindFish(string fishId)
        {
            if (string.IsNullOrEmpty(fishId)) { return null; }
            for (int i = 0; i < fishes.Count; i++) { if (fishes[i] != null && fishes[i].fishId == fishId) { return fishes[i]; } }
            return null;
        }

        public RodData FindRod(string rodItemId)
        {
            if (string.IsNullOrEmpty(rodItemId)) { return null; }
            for (int i = 0; i < rods.Count; i++) { if (rods[i] != null && rods[i].itemID == rodItemId) { return rods[i]; } }
            return null;
        }

        public RodData FindRodByTier(int tier)
        {
            for (int i = 0; i < rods.Count; i++) { if (rods[i] != null && rods[i].tier == tier) { return rods[i]; } }
            return null;
        }

        public FishingCharacterDef FindCharacter(string characterId)
        {
            for (int i = 0; i < characters.Count; i++) { if (characters[i] != null && characters[i].characterId == characterId) { return characters[i]; } }
            return null;
        }

        /// <summary>Editor/test: quên cache để nạp lại.</summary>
        public static void ResetCache() { _instance = null; _searched = false; }
    }
}
