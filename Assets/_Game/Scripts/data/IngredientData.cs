using UnityEngine;

public enum IngredientTier { Basic = 1, Rare = 2, Epic = 3 }
public enum IngredientKind { Ingredient, Seasoning }

[CreateAssetMenu(menuName = "Cooking/Data/Ingredient", fileName = "ING_")]
public class IngredientData : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Sprite icon;
    public IngredientKind kind;
    public IngredientTier tier;

    [Tooltip("Cấp người chơi cần để dùng nguyên liệu này trong bếp (K2 2026-08-26). 1 = mở sẵn.")]
    public int unlockLevel = 1;

    [Header("Flavor Vector")]
    public FlavorVector vector;


    public bool IsRareOrBetter => tier != IngredientTier.Basic;
}