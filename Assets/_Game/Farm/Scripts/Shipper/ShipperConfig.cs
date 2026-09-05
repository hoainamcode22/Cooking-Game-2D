using UnityEngine;

/// <summary>
/// CẤU HÌNH hệ "cô gái giỏ hoa làm shipper" (Task 1) + FEATURE FLAG (CONTRACT §9).
///
/// Đặt asset ở <c>Assets/_Game/Resources/ShipperConfig.asset</c> để
/// <see cref="ShipperManager"/> nạp được bằng <c>Resources.Load&lt;ShipperConfig&gt;("ShipperConfig")</c>.
/// KHÔNG có asset, hoặc <see cref="enabled"/> == false ⇒ mọi hook <c>return</c> ngay,
/// game chạy y như trước (default an toàn).
///
/// ── SỐ ĐO ĐÃ HIỆU CHỈNH THEO MAP THẬT (CONTRACT §2) ─────────────────────────
/// 1 ô lưới = 100 unit. Bảng đơn ở (-579, -672); khu nhà dân ở y ≈ -2000..-2600.
/// Sandbox đo trên 5 nhà thật-like cho ra đường đi 1 chiều 1948..4735 unit
/// ⇒ với <see cref="walkSpeed"/> = 420 thì mỗi chiều 4.6 – 11.3 giây. Đừng để 60-100:
/// cô gái sẽ bò mất 40 giây/chiều.
/// </summary>
[CreateAssetMenu(fileName = "ShipperConfig", menuName = "Farm/Shipper Config")]
public class ShipperConfig : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────
    //  FEATURE FLAG — CONTRACT §9: mặc định TẮT
    // ─────────────────────────────────────────────────────────────────────

    [Header("★ FEATURE FLAG (CONTRACT §9 — mặc định TẮT)")]
    [Tooltip("Bật cả hệ shipper. FALSE = ShipperManager.EnsureInstance() return ngay, " +
             "không spawn gì, không nghe event nào, game y như trước.")]
    public bool enabled = false;

    // ─────────────────────────────────────────────────────────────────────
    //  NHÂN VẬT
    // ─────────────────────────────────────────────────────────────────────

    [Header("Nhân vật")]
    [Tooltip("Prefab cô gái. ĐỂ TRỐNG cũng chạy: manager tự dựng GameObject + " +
             "SpriteRenderer + FourDirWalkAnimator + FlowerGirlShipper lúc runtime. " +
             "Prefab mẫu để clone: Assets/_Game/Farm/Prefabs/Tourists/Tourist_NV01.prefab")]
    public GameObject shipperPrefab;

    [Tooltip("12 frame đi bộ theo ĐÚNG thứ tự CONTRACT §5.1: " +
             "0-2 = down (mặt trước) · 3-5 = left · 6-8 = right · 9-11 = up (lưng). " +
             "DEV-D slice sheet flowergirl_walk_spritesheet.png (3 cột × 4 hàng) rồi gán vào đây.")]
    public Sprite[] walkFrames = new Sprite[12];

    [Tooltip("Tốc độ phát animation đi bộ (§5.1 chốt 8 fps, ping-pong 1-2-3-2).")]
    public float walkFps = 8f;

    [Tooltip("Chiều cao nhân vật trên world (unit). 170 = bằng khách du lịch, " +
             "scale được tính = worldHeight / sprite.bounds.size.y.")]
    public float worldHeight = 170f;

    // ─────────────────────────────────────────────────────────────────────
    //  DI CHUYỂN
    // ─────────────────────────────────────────────────────────────────────

    [Header("Di chuyển (map toạ độ LỚN — xem ghi chú đầu file)")]
    [Tooltip("Unit/giây. Sandbox đo đường đi thật 1948-4735 unit ⇒ 420 cho ra 4.6-11.3 giây/chiều. " +
             "Đặt 60-100 là cô gái bò mất cả phút.")]
    public float walkSpeed = 420f;

    [Tooltip("Khoảng cách coi như 'đã tới điểm' (unit). Map lớn nên không thể là 0.05.")]
    public float arriveThreshold = 12f;

    [Tooltip("Vị trí ĐỨNG CHỜ lệch so với tâm bảng đơn (-579, -672) ⇒ ra ≈ (-879, -760), " +
             "mép trái-trước bảng. Footprint bảng: X [-819, -339], Y [-837, -447].")]
    public Vector2 homeAnchorOffset = new Vector2(-300f, -88f);

    [Tooltip("Đứng trước nhà bao nhiêu giây rồi quay về bảng đơn.")]
    public float standAtHouseSeconds = 1.6f;

    [Tooltip("Lệch làn khi ĐI VỀ (unit) — để đường đi và đường về không trùng khít. " +
             "Đặt 0 để tắt.")]
    public float walkBackLaneOffset = 40f;

    [Tooltip("Giao dồn nhiều đơn thì XẾP HÀNG, KHÔNG spawn thêm cô gái. " +
             "Quá hạn mức thì bỏ đơn CŨ nhất.")]
    public int maxQueuedDeliveries = 3;

    [Tooltip("TRUE = chỉ giao tới nhà đã xây XONG (GrowthState.Completed). " +
             "FALSE = giao cả nhà đang xây.")]
    public bool onlyDeliverToCompletedHouses = true;

    [Tooltip("Điểm đứng TRƯỚC nhà: lệch xuống dưới chân nhà bấy nhiêu unit " +
             "(âm = xuống dưới, đúng góc nhìn isometric).")]
    public float houseFrontOffsetY = -120f;

    // ─────────────────────────────────────────────────────────────────────
    //  ĐƯỜNG LINE BAO QUANH KHU NHÀ (yêu cầu Sếp)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Đường line bao quanh khu nhà village")]
    [Tooltip("Vẽ đường đất bao quanh khu nhà. FALSE = vẫn tính vòng để đi, chỉ KHÔNG vẽ.")]
    public bool drawRoadRing = true;

    [Tooltip("Màu đường (dùng khi roadSprite trống, hoặc để tint sprite).")]
    public Color roadColor = new Color(0.72f, 0.56f, 0.36f, 0.85f);

    [Tooltip("Bề dày đường (unit). 90 ≈ gần 1 ô lưới.")]
    public float roadWidth = 90f;

    [Tooltip("Vòng nở ra khỏi khu nhà bao nhiêu unit. 260 = đường cách chân nhà ~2.6 ô, " +
             "đủ để cô gái đi vòng ngoài mà không xuyên qua nhà.")]
    public float roadRingPadding = 260f;

    [Tooltip("Sprite mặt đường (nên là ảnh lát ngang). ĐỂ TRỐNG = vẽ bằng màu phẳng " +
             "(texture 4×4 trắng sinh bằng code, cache static).")]
    public Sprite roadSprite;

    [Tooltip("Sorting layer của đường — phải NẰM DƯỚI mọi thứ. " +
             "Layer THẬT của project: Bottom, Default, Objects, ObjectsFront, Foreground. " +
             "KHÔNG có 'CongTrinh'.")]
    public string roadSortingLayer = "Bottom";

    [Tooltip("Sorting order của đường (âm để chắc chắn nằm dưới).")]
    public int roadSortingOrder = -50;

    // ─────────────────────────────────────────────────────────────────────
    //  MŨI TÊN CHỈ NHÀ ĐÍCH
    // ─────────────────────────────────────────────────────────────────────

    [Header("Mũi tên chỉ nhà đích")]
    [Tooltip("Sprite mũi tên CHỈ XUỐNG. ĐỂ TRỐNG = sinh bằng Texture2D vẽ code " +
             "(tam giác + thân, viền nâu #4A2B14, khử răng cưa; cache static, tạo 1 lần).")]
    public Sprite arrowSprite;

    [Tooltip("Mũi tên nổi cao hơn ĐỈNH nhà bấy nhiêu unit.")]
    public float arrowHeightAboveHouse = 150f;

    [Tooltip("Biên độ bồng bềnh lên xuống (unit).")]
    public float arrowBobPixels = 24f;

    [Tooltip("Chu kỳ bồng bềnh (giây).")]
    public float arrowBobPeriod = 1.1f;

    [Tooltip("Chiều cao mũi tên trên world (unit).")]
    public float arrowWorldSize = 120f;

    // ─────────────────────────────────────────────────────────────────────
    //  Giá trị đã kẹp an toàn — dùng cái này thay vì đọc field thô
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Tốc độ đi đã kẹp (không cho 0 hay âm ⇒ đứng chết tại chỗ).</summary>
    public float SafeWalkSpeed => Mathf.Max(20f, walkSpeed);

    /// <summary>Ngưỡng tới đích đã kẹp (quá nhỏ ⇒ rung quanh điểm mãi không "tới").</summary>
    public float SafeArriveThreshold => Mathf.Clamp(arriveThreshold, 1f, 400f);

    /// <summary>Fps animation đã kẹp.</summary>
    public float SafeWalkFps => Mathf.Clamp(walkFps, 1f, 60f);

    /// <summary>Chiều cao nhân vật đã kẹp.</summary>
    public float SafeWorldHeight => Mathf.Clamp(worldHeight, 20f, 2000f);

    /// <summary>Padding vòng đã kẹp — nhỏ hơn |houseFrontOffsetY| thì điểm đứng lọt ra ngoài vòng.</summary>
    public float SafeRingPadding =>
        Mathf.Max(Mathf.Abs(houseFrontOffsetY) + 60f, Mathf.Clamp(roadRingPadding, 40f, 4000f));

    /// <summary>Bề dày đường đã kẹp.</summary>
    public float SafeRoadWidth => Mathf.Clamp(roadWidth, 4f, 1000f);

    /// <summary>Số đơn xếp hàng tối đa đã kẹp.</summary>
    public int SafeMaxQueued => Mathf.Clamp(maxQueuedDeliveries, 0, 32);

    /// <summary>Giây đứng trước nhà đã kẹp.</summary>
    public float SafeStandSeconds => Mathf.Clamp(standAtHouseSeconds, 0f, 60f);

    /// <summary>Lệch làn đường về đã kẹp (âm ⇒ 0 = tắt).</summary>
    public float SafeLaneOffset => Mathf.Clamp(walkBackLaneOffset, 0f, 500f);

    /// <summary>Chu kỳ bồng bềnh mũi tên đã kẹp (0 ⇒ chia cho 0).</summary>
    public float SafeArrowBobPeriod => Mathf.Max(0.1f, arrowBobPeriod);

    /// <summary>Chiều cao mũi tên đã kẹp.</summary>
    public float SafeArrowWorldSize => Mathf.Clamp(arrowWorldSize, 8f, 1000f);

    /// <summary>
    /// Bộ ưu tiên sorting layer cho ĐƯỜNG — luôn kết thúc bằng "Bottom"/"Default" để
    /// <see cref="TouristSortingLayers.Resolve"/> không bao giờ rơi vào ngõ cụt.
    /// </summary>
    public string[] RoadLayerPriority
    {
        get
        {
            if (string.IsNullOrEmpty(roadSortingLayer))
                return new[] { "Bottom", "Default" };
            return new[] { roadSortingLayer, "Bottom", "Default" };
        }
    }

    /// <summary>Có đủ 12 frame hợp lệ để chạy animation không.</summary>
    public bool HasWalkFrames
    {
        get
        {
            if (walkFrames == null || walkFrames.Length < FourDirWalkAnimator.FlatFrameCount)
                return false;
            for (int i = 0; i < FourDirWalkAnimator.FlatFrameCount; i++)
                if (walkFrames[i] == null) return false;
            return true;
        }
    }
}
