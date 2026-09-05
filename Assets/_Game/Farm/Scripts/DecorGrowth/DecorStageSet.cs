using UnityEngine;

/// <summary>
/// [Decor5] Một BỘ ART 5 STAGE cho đúng MỘT item trong shop (decor / chuồng / máy).
///
/// Nguồn art: 15 sheet `Assets/Assetsgame/Buiding trang tri/*.png` — mỗi sheet 1536x1024,
/// grid 3 cột x 2 hàng, cell 512x512. Vai trò từng ô (CONTRACT §5.4):
///   (0,0)=idx0 stage1 vật liệu rời · (1,0)=idx1 stage2 xây nửa · (2,0)=idx2 stage3 HOÀN THIỆN
///   (0,1)=idx3 stage4 HỘP QUÀ      · (1,1)=idx4 stage5 hộp bung · (2,1)=idx5 TRỐNG (bỏ)
///
/// Class này CHỈ chứa dữ liệu, không có logic Unity — nó được serialize bên trong
/// <see cref="DecorGrowthConfig"/> (List&lt;DecorStageSet&gt;), không phải asset riêng.
/// DEV-D (tools) fill các field Sprite bằng Editor tool; KHÔNG ai sửa .asset bằng tay.
/// </summary>
[System.Serializable]
public class DecorStageSet
{
    [Header("Nhận diện item")]
    [Tooltip("BaseItemData.itemID — nguồn sự thật DUY NHẤT để tra bộ art. KHÔNG dùng tên prefab (bug cũ ở PlacementManager:1278).")]
    public int itemID;

    [Tooltip("Tên hiển thị trên popup tiến độ. Rỗng thì controller lấy PlaceableItemData.itemName.")]
    public string displayName;

    [Header("5 Sprite theo stage (CONTRACT §5.4)")]
    [Tooltip("Stage 1 — vật liệu / khung sườn rời (progress 0% → stage2Threshold).")]
    public Sprite stage1Parts;

    [Tooltip("Stage 2 — đang xây nửa vời (progress stage2Threshold → 100%).")]
    public Sprite stage2HalfBuilt;

    [Tooltip("Stage 3 — HOÀN THIỆN. Đây là hình cuối tồn tại vĩnh viễn trên world.")]
    public Sprite stage3Complete;

    [Tooltip("Stage 4 — HỘP QUÀ đóng, thở nhẹ, chờ người chơi click.")]
    public Sprite stage4GiftBox;

    [Tooltip("Stage 5 — hộp bung nắp, chỉ sống ~0.35s trong lúc pop scale.")]
    public Sprite stage5BoxOpen;

    [Header("Ghi đè tuỳ chọn")]
    [Tooltip("Thời gian xây riêng cho item này (giây). 0 = dùng công thức mặc định ở CONTRACT §8.")]
    public float buildSecondsOverride;

    [Tooltip("Số thợ búa quanh công trình. 0 = DEV-B tự tính theo gridSize (xem ResolveWorkerCount).")]
    public int workerCount;

    /// <summary>
    /// Bộ art dùng được hay không. stage2 và stage5 KHÔNG bắt buộc vì có đường fallback
    /// (stage2 → stage1, stage5 → stage4) — thiếu chúng chỉ mất mượt, không crash.
    /// stage1/stage3/stage4 là BẮT BUỘC: thiếu thì cả vòng đời Building→GiftBox→Completed vô nghĩa.
    /// </summary>
    public bool IsValid => stage1Parts != null && stage3Complete != null && stage4GiftBox != null;

    /// <summary>
    /// Sprite cho stage 1..5. Ngoài khoảng đó trả null (caller phải tự giữ sprite cũ).
    /// Stage 2 thiếu → mượn stage 1; stage 5 thiếu → mượn stage 4 (hộp quà đứng im, vẫn có pop scale).
    /// </summary>
    public Sprite SpriteForStage(int stage)
    {
        switch (stage)
        {
            case 1: return stage1Parts;
            case 2: return stage2HalfBuilt != null ? stage2HalfBuilt : stage1Parts;
            case 3: return stage3Complete;
            case 4: return stage4GiftBox;
            case 5: return stage5BoxOpen != null ? stage5BoxOpen : stage4GiftBox;
            default: return null;
        }
    }

    /// <summary>
    /// Số thợ búa nên đặt quanh công trình. Dùng <see cref="workerCount"/> nếu designer đã set,
    /// còn 0 thì suy theo diện tích ô lưới: 1 ô → 1 thợ · 2..8 ô → 2 thợ · từ 9 ô → 3 thợ.
    /// (DEV-B gọi hàm này; giữ ở đây để chỉ có MỘT công thức trong cả gói.)
    /// </summary>
    public int ResolveWorkerCount(Vector2Int gridSize)
    {
        if (workerCount > 0) return Mathf.Clamp(workerCount, 1, 3);
        int area = Mathf.Max(1, gridSize.x) * Mathf.Max(1, gridSize.y);
        if (area <= 1) return 1;
        if (area <= 8) return 2;
        return 3;
    }
}
