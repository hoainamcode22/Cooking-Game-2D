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

    // C9 — đã xoá `rareBonus` và `techniqueBonus`: `CookingScoreCalculator.Evaluate` gán
    // cứng cả hai = 0 và `finalScore` không hề cộng chúng vào, cũng không có UI nào đọc.
    // Hai field 0 vĩnh viễn nằm trong kết quả chấm điểm là bẫy: ai đọc cũng tưởng có
    // thưởng nguyên liệu hiếm / thưởng kỹ thuật và đi tìm chỗ tính, mà không có chỗ nào.
    public int finalScore;

    public int goldReward;
    public int gemReward;
    public int rankPointReward;
}