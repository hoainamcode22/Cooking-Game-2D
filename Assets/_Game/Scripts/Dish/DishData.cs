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

    [Header("Target Flavor")]
    public FlavorVector targetFlavor;

    [Header("Scoring - Required Ingredients")]
    public List<IngredientData> requiredIngredients;

}