using UnityEngine;

/// <summary>
/// Cấu hình hiệu ứng bóng mờ lưu ảnh (afterimage) cho nhân vật/xe cộ di chuyển
/// + ghost-pulse cho công trình (nhà village / decor) lúc đổi stage.
/// Asset đặt tại <c>Assets/_Game/Resources/AfterimageConfig.asset</c> — do
/// AfterimageSetupTool (menu ★ SETUP) tạo. Runtime chỉ Resources.Load;
/// KHÔNG có asset hoặc <see cref="enabled"/> == false ⇒ toàn hệ tắt (feature gate §9).
/// </summary>
[CreateAssetMenu(fileName = "AfterimageConfig", menuName = "Farm/Afterimage Config")]
public class AfterimageConfig : ScriptableObject
{
    /// <summary>1 mục tiêu nhận afterimage — data-driven theo TÊN CLASS (GetType().Name), không reference cứng type.</summary>
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Tên class (GetType().Name) của MonoBehaviour trên nhân vật/xe.")]
        public string typeName;

        [Tooltip("true cho xe nhiều SpriteRenderer con (tàu lửa: đầu + toa; tàu thủy: thân + buồm). " +
                 "Mỗi nhịp nhả ghost cho tối đa 6 SR đang nhìn thấy.")]
        public bool includeChildRenderers;

        [Tooltip("true: dùng tintOverride thay cho tint chung.")]
        public bool useTintOverride;

        [Tooltip("Tông màu riêng cho entry này (vd xe cộ dùng trắng-xanh nhạt hơn).")]
        public Color tintOverride = Color.white;
    }

    [Header("Feature gate")]
    [Tooltip("Tắt là toàn hệ ngừng: không quét, không gắn emitter/pulse, không nhả ghost.")]
    public bool enabled = true;

    [Header("Điều kiện nhả ghost")]
    [Tooltip("Tốc độ world tối thiểu (unit/giây) để nhả ghost. 1 ô lưới = 100 unit. " +
             "Dưới ngưỡng (vd thợ đứng đập búa tại chỗ) KHÔNG nhả; đi bộ (shipper 420 u/s) mới nhả.")]
    public float minSpeed = 60f;

    [Tooltip("Khoảng cách thời gian (giây) giữa 2 nhịp ghost liên tiếp của 1 mục tiêu.")]
    public float spawnInterval = 0.07f;

    [Header("Vòng đời ghost")]
    [Tooltip("Ghost sống bao lâu (giây) trước khi mờ hẳn và trả về pool.")]
    public float ghostLife = 0.35f;

    [Tooltip("Alpha khởi điểm của ghost (fade tuyến tính về 0 trong ghostLife).")]
    [Range(0f, 1f)] public float startAlpha = 0.45f;

    [Header("Màu & scale")]
    [Tooltip("Tông màu ghost mặc định — bóng lạnh nhẹ kiểu speed-ghost.")]
    public Color tint = new Color(0.75f, 0.85f, 1f, 1f);

    [Tooltip("true: nhân tint với màu gốc của SpriteRenderer; false: thay hẳn bằng tint.")]
    public bool multiplyTint = true;

    [Tooltip("Ghost co nhỏ dần trong lúc mờ đi.")]
    public bool shrink = true;

    [Tooltip("Hệ số scale ở cuối đời ghost (so với scale lúc sinh).")]
    public float endScaleMul = 0.92f;

    [Header("Pool & sorting")]
    [Tooltip("Số ghost tối đa tồn tại đồng thời (pool cap). Vượt cap thì bỏ qua lượt nhả.")]
    public int poolCap = 64;

    [Tooltip("Chênh sortingOrder so với nguồn lúc chụp — mặc định -1: ghost nằm NGAY SAU, cùng sorting layer.")]
    public int sortingOrderOffset = -1;

    [Header("Mục tiêu nhận hiệu ứng (data-driven — so GetType().Name, không reference cứng type)")]
    [Tooltip("Danh sách chính. Nhân vật đơn SR để includeChildRenderers=false; xe cộ nhiều SR con để true.")]
    public Entry[] targetEntries =
    {
        new Entry { typeName = "FlowerGirlShipper" },
        new Entry { typeName = "BuilderWorker" },
        new Entry { typeName = "TouristAgent" },
        new Entry { typeName = "DeliveryCharacterMover" },
        new Entry { typeName = "TrainPathFollower",     includeChildRenderers = true, useTintOverride = true, tintOverride = new Color(0.88f, 0.94f, 1f, 1f) },
        new Entry { typeName = "TouristBoatController", includeChildRenderers = true, useTintOverride = true, tintOverride = new Color(0.88f, 0.94f, 1f, 1f) },
        new Entry { typeName = "FerryController",       includeChildRenderers = true, useTintOverride = true, tintOverride = new Color(0.88f, 0.94f, 1f, 1f) },
    };

    [Tooltip("LEGACY (giữ tương thích asset cũ): tên class trần, coi như Entry mặc định (đơn SR, tint chung). " +
             "Asset mới dùng targetEntries.")]
    public string[] targetTypeNames = new string[0];

    [Header("Ghost-pulse công trình (nhà village + decor — không di chuyển, phun bóng lúc ĐỔI SPRITE stage)")]
    [Tooltip("Bật/tắt riêng nhánh pulse công trình.")]
    public bool buildingPulse = true;

    [Tooltip("Tên class controller công trình (so chuỗi, không reference cứng).")]
    public string[] buildingTypeNames = { "DecorGrowthController", "HouseGrowthController" };

    [Tooltip("Pulse phóng to tới hệ số này (1.0 → 1.12) trong pulseLife.")]
    public float pulseScaleMul = 1.12f;

    [Tooltip("Đời sống 1 pulse (giây).")]
    public float pulseLife = 0.45f;

    [Tooltip("Alpha khởi điểm của pulse (fade về 0).")]
    [Range(0f, 1f)] public float pulseAlpha = 0.5f;

    [Header("Quét scene")]
    [Tooltip("Chu kỳ (giây) quét lại scene tìm mục tiêu spawn muộn (shipper/thợ/tàu sinh lúc runtime).")]
    public float rescanInterval = 10f;
}
