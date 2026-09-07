using UnityEngine;

/// <summary>
/// Base cho các vật phẩm xây dựng và trang trí trong Shop.
/// Kế thừa BaseItemData nên tương thích hoàn toàn với ShopItemUI và ShopManager.
/// </summary>
public class PlaceableItemData : BaseItemData
{
    [Header("Placement")]
    public GameObject prefabToBuild;

    [Header("Unlock (Demo L1-L10)")]
    [Tooltip("Level người chơi cần đạt để mua công trình/trang trí này. ShopLevelLockUI đọc field này để khoá item trong shop.")]
    public int unlockLevel = 1;

    // ─────────────────────────────────────────────────────────────────────────
    // KÍCH THƯỚC THEO Ô LƯỚI — nguồn sự thật DUY NHẤT cho footprint.
    //
    // VÌ SAO cần field này thay vì đo bounds sprite lúc chạy:
    //   • Sprite thường có viền trong suốt → bounds to hơn chân công trình thật.
    //   • Bounds gồm cả mái/ống khói/con vật nhô ra → chiếm thừa ô, chặn oan chỗ trống.
    //   • Đo bounds mỗi frame tốn CPU và cho kết quả khác nhau giữa Ghost và vật thật.
    // Editor tool `Tools/Map45/8. Suy Kich Thuoc O` suy sẵn giá trị này từ bounds
    // prefab rồi cho designer chỉnh tay lại từng asset.
    // 🟢 V10: công thức ĐÚNG là IsoGrid.EstimateSizeFromWorldSize(bounds) (làm tròn GẦN
    // NHẤT theo ô iso 300 x 150). Công thức cũ Ceil(size / CELL=100) vừa sai hệ vừa luôn
    // làm tròn LÊN — đó chính là thứ đã sinh ra loạt số rác 341x342 / 413x413 / 1514x1515.
    //
    // LƯU Ý TƯƠNG THÍCH: asset .asset cũ KHÔNG có key `gridSize`. Unity chạy field
    // initializer trước khi ghi đè dữ liệu YAML nên asset cũ vẫn ra (1,1), không ra (0,0).
    // Dù vậy PlacementManager vẫn Max(1, …) để chắc chắn không bao giờ có ô = 0.
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Kích thước theo Ô LƯỚI — KHÔNG suy từ sprite nữa")]
    [Tooltip("So o luoi cong trinh chiem (rong x cao). MOT O = 300 x 150 world unit " +
             "(IsoGrid.CellWidth x IsoGrid.CellHeight), KHONG vuong. Cong trinh Township " +
             "that chi 1..7 o moi chieu; so hang tram la SO RAC, dung dien tay.\n" +
             "Dung Tools/Map45/8. Suy Kich Thuoc O de tu suy tu bounds prefab. " +
             "⛔ DUNG dung Tools/Farm/Suy Kich Thuoc O Cong Trinh: tool cu do theo luoi " +
             "VUONG 100 va luon Ceil len, chinh no sinh ra loat so rac 341x342.")]
    public Vector2Int gridSize = new Vector2Int(1, 1);

    [Header("Xây dựng (DEV-2 đọc)")]
    [Tooltip("Thời gian xây (giây). 0 = hiện ngay, bỏ qua giai đoạn ĐANG XÂY.")]
    public float buildTimeSeconds = 0f;

    [Tooltip("Giá tăng tốc cố định. 0 = ConstructionManager tự tính theo thời gian còn lại.")]
    public int rushGemCost = 0;

    /// <summary>
    /// Kích thước ô sau khi xoay <paramref name="rotationSteps"/> lần 90°.
    /// Bước lẻ (90°/270°) thì hoán đổi X↔Y; bước chẵn giữ nguyên.
    /// Luôn Max(1,…) để asset cũ / cấu hình sai không tạo footprint rỗng.
    /// </summary>
    public Vector2Int GetGridSize(int rotationSteps)
    {
        int n = Mathf.Max(1, gridSize.x);
        int m = Mathf.Max(1, gridSize.y);
        return ((rotationSteps & 1) == 1) ? new Vector2Int(m, n) : new Vector2Int(n, m);
    }
}
