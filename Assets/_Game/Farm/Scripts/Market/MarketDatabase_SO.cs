using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MarketItemDef
{
    public string ItemID;
    public int BuyPrice = 10;
    public int MinQuantity = 1;
    public int MaxQuantity = 5;
}

[CreateAssetMenu(fileName = "MarketDatabase", menuName = "Farm/Market Database")]
public class MarketDatabase_SO : ScriptableObject
{
    [TextArea(5, 10)]
    [SerializeField]
    private string setupNotes =
        "Fill ItemID from the game's master item database.\n" +
        "Old data targets: 10 seed IDs, 10 flower seed IDs, cattle meat IDs (cow/chicken/pig), egg ID, 20 dish IDs.\n" +
        "New data targets to add later to the master DB: fish sauce, salt, MSG, vegetables.\n" +
        "Market only owns BuyPrice and Min/MaxQuantity for each ItemID.";

    [SerializeField] private List<MarketItemDef> items = new List<MarketItemDef>();

    public IReadOnlyList<MarketItemDef> Items => items;
    public string SetupNotes => setupNotes;

    [ContextMenu("Reset To Default Placeholder Rows")]
    public void ResetToDefaultPlaceholderRows()
    {
        items = CreateDefaultPlaceholderRows();
    }

    public static List<MarketItemDef> CreateDefaultPlaceholderRows()
    {
        List<MarketItemDef> rows = new List<MarketItemDef>();

        AddRows(rows, "TODO_SEED_ID_", 10, 25, 1, 3);
        AddRows(rows, "TODO_FLOWER_SEED_ID_", 10, 30, 1, 3);

        rows.Add(CreateRow("TODO_MEAT_COW_ID", 120, 1, 2));
        rows.Add(CreateRow("TODO_MEAT_CHICKEN_ID", 90, 1, 2));
        rows.Add(CreateRow("TODO_MEAT_PIG_ID", 110, 1, 2));
        rows.Add(CreateRow("TODO_EGG_ID", 35, 1, 4));

        AddRows(rows, "TODO_DISH_ID_", 20, 180, 1, 2);

        rows.Add(CreateRow("TODO_FISH_SAUCE_ID", 45, 1, 3));
        rows.Add(CreateRow("TODO_SALT_ID", 20, 1, 5));
        rows.Add(CreateRow("TODO_MSG_ID", 35, 1, 4));
        rows.Add(CreateRow("TODO_VEGETABLE_ID", 40, 1, 4));

        return rows;
    }

    private static void AddRows(List<MarketItemDef> rows, string idPrefix, int count, int price, int minQuantity, int maxQuantity)
    {
        for (int i = 1; i <= count; i++)
        {
            rows.Add(CreateRow(idPrefix + i.ToString("00"), price, minQuantity, maxQuantity));
        }
    }

    private static MarketItemDef CreateRow(string itemID, int price, int minQuantity, int maxQuantity)
    {
        return new MarketItemDef
        {
            ItemID = itemID,
            BuyPrice = price,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity
        };
    }
}
