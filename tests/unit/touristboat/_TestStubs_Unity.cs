// ═══════════════════════════════════════════════════════════════════════════
//  STUB CHỈ DÙNG CHO TEST CONSOLE — KHÔNG PHẢI CODE GAME.
//
//  File này nằm NGOÀI thư mục Assets/ nên Unity KHÔNG BAO GIỜ biên dịch nó
//  (Unity chỉ compile file .cs dưới Assets/). Nó tồn tại để `mcs` biên dịch
//  được TouristRewardCalculator.cs + TouristBoatConfig.cs THẬT ngoài Unity,
//  đúng cách BoatScheduleCoreTests đang làm với lõi thuần C#.
//
//  Nguyên tắc: stub chỉ khai đúng chữ ký cần thiết, KHÔNG mô phỏng logic —
//  logic phải là của file thật, nếu không thì test chẳng chứng minh điều gì.
//  BasePriceBook cố tình luôn trả false (không tra được giá) để test đi vào
//  đúng nhánh fallback bi quan nhất.
// ═══════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { }
    public class Sprite : Object { }

    public static class Mathf
    {
        public static float Max(float a, float b) => a > b ? a : b;
        public static int   Max(int a, int b)     => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int   RoundToInt(float f)   => (int)Math.Round((double)f, MidpointRounding.ToEven);
    }

    /// <summary>Gom log cảnh báo để test kiểm "chỉ log 1 lần mỗi món".</summary>
    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void Log(object o) { }
        public static void LogWarning(object o) { Warnings.Add(o != null ? o.ToString() : ""); }
        public static void LogError(object o) { }
    }

    public class HeaderAttribute   : Attribute { public HeaderAttribute(string s) { } }
    public class TooltipAttribute  : Attribute { public TooltipAttribute(string s) { } }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute(int a, int b) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
}

// ─── Type dự án mà calculator phụ thuộc ─────────────────────────────────
public struct FlavorVector { public float sweet, salty, sour, spicy; }
public enum IngredientTier { Basic = 1, Rare = 2, Epic = 3 }
public enum IngredientKind { Ingredient, Seasoning }

public class IngredientData : UnityEngine.ScriptableObject
{
    public string id;
    public IngredientKind kind;
    public IngredientTier tier;
}

public enum DishDifficulty { Easy, Normal, Hard }

public class DishData : UnityEngine.ScriptableObject
{
    public string dishId;
    public DishDifficulty difficulty = DishDifficulty.Normal;
    public int unlockLevel = 5;
    public List<IngredientData> requiredIngredients;
    public int rewardExp = 20;
    public int rewardGold = 0;
    public int sellPrice = 0;
}

/// <summary>Stub: luôn "không tra được giá" → calculator phải đi nhánh DefaultBasePrice.</summary>
public static class BasePriceBook
{
    public const int DefaultBasePrice = 10;
    public static bool HasProvider => false;
    public static bool TryGetBasePrice(string id, out int basePrice)
    {
        basePrice = DefaultBasePrice;
        return false;
    }
}

/// <summary>Stub tối giản: chỉ cần Instance/Config cho 2 overload tương thích V2.0.</summary>
public class BoatDockManager
{
    public static BoatDockManager Instance;
    public TouristBoatConfig Config;
}

/// <summary>Stub: DockUnlockRequirement do BoatScheduleCore khai, test này không nạp lõi.</summary>
public struct DockUnlockRequirement
{
    public int RequiredLevel;
    public int GoldCost;
    public int GemCost;
}
