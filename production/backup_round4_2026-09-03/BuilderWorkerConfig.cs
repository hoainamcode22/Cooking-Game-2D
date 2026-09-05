using UnityEngine;

/// <summary>
/// CẤU HÌNH THỢ BÚA XÂY DỰNG — ScriptableObject duy nhất cho cả hệ.
/// ═════════════════════════════════════════════════════════════════
///
/// FEATURE FLAG (§9 CONTRACT): <see cref="enabled"/> mặc định <b>false</b>.
/// Chưa có asset này trong Resources, hoặc có mà <c>enabled == false</c> ⇒ mọi hook
/// (<see cref="BuilderWorkerCrew.AttachTo"/>, <see cref="HouseWorkerBridge"/>) return ngay,
/// game chạy y như trước khi có gói này. Đây là điều kiện an toàn để Lead gộp.
///
/// TẠO Ở ĐÂU: Sếp bấm Assets > Create > Farm > Builder Worker Config, lưu vào
/// <c>Assets/Resources/BuilderWorkerConfig.asset</c> (tên PHẢI đúng — HouseWorkerBridge
/// tra bằng <c>Resources.Load&lt;BuilderWorkerConfig&gt;("BuilderWorkerConfig")</c>).
/// Frame sẽ do Editor Tool của DEV-D đổ vào, không gán tay 24 ô.
///
/// SỐ LIỆU ĐÃ ĐO (§5.2, §5.3 CONTRACT):
///   worker_hammer_spritesheet.png    1200×896, 4 cột × 3 hàng → 12 frame, 10 fps
///   worker_celebrate_spritesheet.png 1200×896, 4 cột × 3 hàng → 12 frame, 12 fps
///   frame 8,9,10 của sheet búa = khoảnh khắc búa chạm đất (mảnh vụn bay) → bụi + SFX
///   frame 0 của sheet ăn mừng   = đứng thẳng → pose ĐỨNG IM ở giai đoạn hộp quà
///
/// [Worker]
/// </summary>
[CreateAssetMenu(fileName = "BuilderWorkerConfig", menuName = "Farm/Builder Worker Config")]
public class BuilderWorkerConfig : ScriptableObject
{
    [Header("◆ CÔNG TẮC TỔNG (mặc định TẮT — §9 CONTRACT)")]
    [Tooltip("FALSE = toàn bộ hệ thợ búa không chạy, không spawn gì, không tốn frame.\n" +
             "Chỉ bật khi frame đã được đổ đủ và Sếp muốn xem.")]
    public bool enabled = false;

    [Header("◆ 3 PREFAB THỢ (yêu cầu của Sếp — 3 người khác nhau)")]
    [Tooltip("Thợ thứ i dùng prefab [i % 3]. Ô nào để trống thì hệ tự dựng GameObject rỗng " +
             "+ SpriteRenderer + SpriteSequencePlayer bằng code (KHÔNG crash).")]
    public GameObject[] workerPrefabs = new GameObject[3];

    [Header("◆ ANIMATION ĐẬP BÚA (12 frame — tool đổ)")]
    [Tooltip("12 frame từ worker_hammer_spritesheet.png theo thứ tự 0→11.")]
    public Sprite[] hammerFrames;

    [Tooltip("10 fps = 1.2s một nhát búa (con số đề xuất trong §5.2 CONTRACT).")]
    public float hammerFps = 10f;

    [Header("◆ ANIMATION ĂN MỪNG (12 frame — tool đổ)")]
    [Tooltip("12 frame từ worker_celebrate_spritesheet.png theo thứ tự 0→11.")]
    public Sprite[] celebrateFrames;

    [Tooltip("12 fps khi nhảy ăn mừng.")]
    public float celebrateFps = 12f;

    [Tooltip("Frame ĐỨNG IM ở giai đoạn hộp quà. Sheet ăn mừng có frame 0 = đứng thẳng.")]
    public int celebrateIdleFrameIndex = 0;

    [Header("◆ FRAME BÚA CHẠM ĐẤT (bắn bụi + SFX)")]
    [Tooltip("Chỉ số frame trong hammerFrames có mảnh vụn bay. Đo được: 8, 9, 10.")]
    public int[] hammerImpactFrames = new int[] { 8, 9, 10 };

    [Header("◆ TỈ LỆ & VỊ TRÍ (1 ô lưới = 100 world unit — §2 CONTRACT)")]
    [Tooltip("Chiều cao thợ tính bằng world unit. 170 = cao bằng khách du lịch để cùng tỉ lệ.")]
    public float workerWorldHeight = 170f;

    [Tooltip("Thợ đứng cách MÉP công trình bao nhiêu world unit. 40 ≈ 0.4 ô.")]
    public float placementRadiusPadding = 40f;

    [Tooltip("Số thợ tối thiểu / tối đa quanh một công trình.")]
    public int minWorkers = 1;
    public int maxWorkers = 3;

    [Header("◆ FADE (dùng Time.unscaledDeltaTime — §0.6 CONTRACT)")]
    public float fadeInSeconds  = 0.25f;
    public float fadeOutSeconds = 0.35f;

    [Tooltip("Lệch nhịp giữa các thợ (giây). 0.4s ⇒ với 10 fps là lệch 4/12 vòng.\n" +
             "Đặt 0 thì 3 thợ đập ĐỒNG LOẠT — mắt đọc thành một khối, mất cảm giác 'sống'.")]
    public float phaseSpreadSeconds = 0.4f;

    [Header("◆ VFX & SFX (đều NULLABLE — thiếu thì bỏ qua, KHÔNG crash)")]
    [Tooltip("Prefab hạt bụi bắn ở chân thợ lúc búa chạm đất. Để trống = không có bụi.")]
    public GameObject dustVfxPrefab;

    [Tooltip("Tiếng búa. Để trống = im lặng. Toàn crew bị chặn tối đa 1 tiếng / 0.25s.")]
    public AudioClip hammerSfx;

    [Range(0f, 1f)]
    public float hammerSfxVolume = 0.6f;

    /// <summary>
    /// SỐ THỢ theo diện tích chân công trình (world unit, 1 ô = 100).
    ///
    /// Quy tắc:
    ///   • chiều dài nhất ≥ 600 (từ 6 ô trở lên)      ⇒ 3 thợ  (chuồng 7×5, công trình lớn)
    ///   • cả hai chiều &lt; 300 (decor nhỏ 2×2)      ⇒ 1 thợ
    ///   • còn lại (trung bình, decor/nhà 4×4)        ⇒ 2 thợ
    /// Kết quả luôn được clamp vào [<see cref="minWorkers"/>, <see cref="maxWorkers"/>].
    /// </summary>
    public int WorkerCountForFootprint(Vector2 worldSize)
    {
        float w = Mathf.Abs(worldSize.x);
        float h = Mathf.Abs(worldSize.y);
        float longest = Mathf.Max(w, h);

        int count;
        if (longest >= 600f)            count = 3;
        else if (w < 300f && h < 300f)  count = 1;
        else                            count = 2;

        int lo = Mathf.Max(0, Mathf.Min(minWorkers, maxWorkers));
        int hi = Mathf.Max(minWorkers, maxWorkers);
        return Mathf.Clamp(count, lo, hi);
    }

    private void OnValidate()
    {
        hammerFps    = Mathf.Max(0.01f, hammerFps);
        celebrateFps = Mathf.Max(0.01f, celebrateFps);

        minWorkers = Mathf.Max(0, minWorkers);
        maxWorkers = Mathf.Max(minWorkers, maxWorkers);

        workerWorldHeight      = Mathf.Max(1f, workerWorldHeight);
        placementRadiusPadding = Mathf.Max(0f, placementRadiusPadding);

        fadeInSeconds      = Mathf.Max(0f, fadeInSeconds);
        fadeOutSeconds     = Mathf.Max(0f, fadeOutSeconds);
        phaseSpreadSeconds = Mathf.Max(0f, phaseSpreadSeconds);

        celebrateIdleFrameIndex = Mathf.Max(0, celebrateIdleFrameIndex);

        if (workerPrefabs == null || workerPrefabs.Length == 0)
            workerPrefabs = new GameObject[3];
    }
}
