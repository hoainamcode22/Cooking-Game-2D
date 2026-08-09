using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một dòng hàng có thể xuất hiện ở Bảng Tin Chợ.
///
/// A2 — thêm Category / UnlockLevel / Weight. Ba trường này để chỗ nào?
/// Chúng CÓ trong <see cref="MarketPriceTable"/> rồi, nhưng vẫn phải serialize xuống
/// asset: nếu để provider tra ngược bảng static thì người cân bằng game không thể
/// chỉnh riêng một dòng trong Inspector mà không đụng code. Asset là bản chụp có thể
/// tinh chỉnh tay; bảng static là nguồn để SINH RA bản chụp đó.
/// </summary>
[Serializable]
public class MarketItemDef
{
    public string ItemID;
    public int    BuyPrice    = 10;
    public int    MinQuantity = 1;
    public int    MaxQuantity = 5;

    [Header("A2 — bộ lọc & bốc ngẫu nhiên")]
    [Tooltip("Danh mục dùng cho dải tab lọc bên trái bảng tin.")]
    public MarketCategory Category = MarketCategory.NongSan;

    [Tooltip("Cấp người chơi tối thiểu để dòng này lọt vào rổ bốc. Cấp thấp thì bảng tin toàn hàng cơ bản.")]
    public int UnlockLevel = 1;

    [Tooltip("Trọng số bốc ngẫu nhiên. 0 = không bao giờ xuất hiện. Càng cao càng hay gặp.")]
    public int Weight = 50;
}

[CreateAssetMenu(fileName = "MarketDatabase", menuName = "Farm/Market Database")]
public class MarketDatabase_SO : ScriptableObject
{
    [TextArea(4, 10)]
    [SerializeField]
    private string setupNotes =
        "KHÔNG gõ tay file này.\n" +
        "Chạy menu: Tools/Farm/Chợ/Sinh lại MarketDatabase từ bảng giá.\n" +
        "Nguồn dữ liệu là MarketPriceTable.cs — sửa giá ở đó rồi sinh lại.";

    [SerializeField] private List<MarketItemDef> items = new List<MarketItemDef>();

    public IReadOnlyList<MarketItemDef> Items => items;
    public string SetupNotes => setupNotes;

#if UNITY_EDITOR
    /// <summary>
    /// Chỉ Editor tool được ghi đè danh sách. Để public sẽ có ngày ai đó gọi lúc runtime
    /// rồi mất dữ liệu asset mà không hiểu tại sao.
    /// </summary>
    public void EditorReplaceItems(List<MarketItemDef> newItems, string notes)
    {
        items = newItems ?? new List<MarketItemDef>();
        if (!string.IsNullOrEmpty(notes))
            setupNotes = notes;
    }
#endif
}
