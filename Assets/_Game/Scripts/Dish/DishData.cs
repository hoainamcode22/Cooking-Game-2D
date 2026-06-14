using System;
using System.Collections.Generic;
using UnityEngine;

public enum DishDifficulty
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class HintIngredientSlotData
{
    public string displayName;
    public Sprite icon;
}

[Serializable]
public class SeasoningTipData
{
    public string displayName;
    public Sprite icon;
    public string effectText;
}

[CreateAssetMenu(fileName = "Dish_", menuName = "Cooking/Dish Data")]
public class DishData : ScriptableObject
{
    [Header("Basic Info")]
    public string dishId;
    public string dishName;
    public string dishSubTitle;
    public Sprite dishSprite;
    public DishDifficulty difficulty = DishDifficulty.Normal;

    [Tooltip("Player level required to cook this dish (Demo L1-L10)")]
    public int unlockLevel = 5;

    [Header("Target Flavor")]
    public FlavorVector targetFlavor;

    [Header("Scoring - Required Ingredients")]
    public List<IngredientData> requiredIngredients;

}