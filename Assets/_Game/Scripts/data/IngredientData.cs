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

    [Header("Flavor Vector")]
    public FlavorVector vector;


    public bool IsRareOrBetter => tier != IngredientTier.Basic;
}