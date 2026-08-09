using UnityEngine;
using System;

namespace Core
{
    public class PlayerProfile : MonoBehaviour
    {
        [Header("Player Data")]
        public int Level = 1;
        public int CurrentEXP = 0;
        public int MaxEXP = 100;
        public int Gold = 0;
        public int Diamond = 0;
        
        // Actions to notify listeners (optional but useful for a decoupled architecture)
        public Action<int> OnEXPGained;
        public Action<int> OnGoldGained;
        public Action<int> OnDiamondGained;
        public Action<int> OnLevelUp;

        public void AddEXP(int amount)
        {
            CurrentEXP += amount;
            
            while (CurrentEXP >= MaxEXP)
            {
                CurrentEXP -= MaxEXP;
                Level++;
                MaxEXP = CalculateMaxEXPForLevel(Level);
                OnLevelUp?.Invoke(Level);
            }
            
            OnEXPGained?.Invoke(amount);
        }
        
        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldGained?.Invoke(amount);
        }
        
        public void AddDiamond(int amount)
        {
            Diamond += amount;
            OnDiamondGained?.Invoke(amount);
        }

        public int CalculateMaxEXPForLevel(int level)
        {
            // Simple logic for next level's max EXP
            return level * 100;
        }
    }
}
