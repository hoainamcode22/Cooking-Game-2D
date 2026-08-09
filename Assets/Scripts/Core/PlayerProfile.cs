using UnityEngine;

namespace FarmGame.Core
{
    public class PlayerProfile : MonoBehaviour
    {
        public static PlayerProfile Instance;

        public int Level = 32;
        public int CurrentEXP = 4680;
        public int Gold = 12450;
        public int Diamond = 320;

        public int MaxEXP => Level * 100 + 3000; // Formula for max EXP

        public delegate void OnResourceChanged();
        public event OnResourceChanged ResourceChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void AddEXP(int amount)
        {
            CurrentEXP += amount;
            while (CurrentEXP >= MaxEXP)
            {
                CurrentEXP -= MaxEXP;
                Level++;
            }
            ResourceChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            ResourceChanged?.Invoke();
        }

        public void AddDiamond(int amount)
        {
            Diamond += amount;
            ResourceChanged?.Invoke();
        }
    }
}
