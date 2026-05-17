using System;

[Serializable]
public class CookingScoreResult
{
    public FlavorVector ingredientVector;
    public FlavorVector seasoningVector;
    public FlavorVector totalVector;

    public int ingredientScore;
    public int seasoningScore;

    public int baseScore;
    public int rareBonus;
    public int techniqueBonus;
    public int finalScore;

    public int goldReward;
    public int gemReward;
    public int rankPointReward;
}