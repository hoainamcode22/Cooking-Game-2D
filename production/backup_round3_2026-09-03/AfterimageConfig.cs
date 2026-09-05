using UnityEngine;

/// <summary>
/// Cấu hình hiệu ứng bóng mờ lưu ảnh (afterimage) cho nhân vật di chuyển.
/// Asset đặt tại <c>Assets/_Game/Resources/AfterimageConfig.asset</c> — do
/// AfterimageSetupTool (menu ★ SETUP) tạo. Runtime chỉ Resources.Load;
/// KHÔNG có asset hoặc <see cref="enabled"/> == false ⇒ toàn hệ tắt (feature gate §9).
/// </summary>
[CreateAssetMenu(fileName = "AfterimageConfig", menuName = "Farm/Afterimage Config")]
public class AfterimageConfig : ScriptableObject
{
    [Header("Feature gate")]
    [Tooltip("Tắt là toàn hệ ngừng: không quét, không gắn emitter, không nhả ghost.")]
    public bool enabled = true;

    [Header("Điều kiện nhả ghost")]
    [Tooltip("Tốc độ world tối thiểu (unit/giây) để nhả ghost. 1 ô lưới = 100 unit. " +
             "Dưới ngưỡng (vd thợ đứng đập búa tại chỗ) KHÔNG nhả; đi bộ (shipper 420 u/s) mới nhả.")]
    public float minSpeed = 60f;

    [Tooltip("Khoảng cách thời gian (giây) giữa 2 ghost liên tiếp của 1 nhân vật.")]
    public float spawnInterval = 0.07f;

    [Header("Vòng đời ghost")]
    [Tooltip("Ghost sống bao lâu (giây) trước khi mờ hẳn và trả về pool.")]
    public float ghostLife = 0.35f;

    [Tooltip("Alpha khởi điểm của ghost (fade tuyến tính về 0 trong ghostLife).")]
    [Range(0f, 1f)] public float startAlpha = 0.45f;

    [Header("Màu & scale")]
    [Tooltip("Tông màu ghost — bóng lạnh nhẹ kiểu speed-ghost.")]
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

    [Tooltip("Chênh sortingOrder so với nhân vật lúc chụp — mặc định -1: ghost nằm NGAY SAU nhân vật, cùng sorting layer.")]
    public int sortingOrderOffset = -1;

    [Header("Nhân vật nhận hiệu ứng (so theo GetType().Name — data-driven, không reference cứng type)")]
    public string[] targetTypeNames = { "FlowerGirlShipper", "BuilderWorker", "TouristAgent" };

    [Tooltip("Chu kỳ (giây) quét lại scene tìm nhân vật spawn muộn (shipper/thợ sinh lúc runtime).")]
    public float rescanInterval = 2f;
}
