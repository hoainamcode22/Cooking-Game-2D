using System;
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

    [Header("Hints - Required")]
    public HintIngredientSlotData required1;
    public HintIngredientSlotData required2;

    [Header("Hints - Optional")]
    public HintIngredientSlotData optional1;
    public HintIngredientSlotData optional2;
    public HintIngredientSlotData optional3;

    [Header("Hints - Seasoning Tips")]
    public SeasoningTipData tip1;
    public SeasoningTipData tip2;
    public SeasoningTipData tip3;
    public SeasoningTipData tip4;

    [Header("Hints - Bonus Combo")]
    [TextArea(2, 3)]
    public string bonusComboText;

    [Header("Hints - Judge Button")]
    public string whatJudgeLikesText = "Giám kh?o thích gì?";
}