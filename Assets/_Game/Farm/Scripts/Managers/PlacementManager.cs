using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Quản lý luồng đặt công trình / trang trí lên map.
/// Luồng: Shop trừ tiền → StartPlacingNewObject() → User kéo Ghost → V (xác nhận) / X (hủy + hoàn tiền).
///
/// ══════════════════════════════════════════════════════════════════════════
/// TẦNG LƯỚI (DEV-1) — xem §4 production/TEAM_PLACEMENT_CONSTRUCTION.md
/// • CELL = 100 world unit, ORIGIN = (0,0). Đây là NGUỒN SỰ THẬT DUY NHẤT.
/// • Công trình N×M ô chiếm đúng N×M ô, tâm luôn nằm chính giữa khối ô đó.
/// • Chồng lấn kiểm tra bằng HÌNH HỌC Ô LƯỚI (HashSet ô đã chiếm),
///   KHÔNG dùng Physics2D nữa — layer mask trong scene rỗng nên OverlapBox
///   luôn trả null và mọi vị trí đều "hợp lệ" (lỗi "đặt đè lên nhau").
/// ══════════════════════════════════════════════════════════════════════════
///
/// ══════════════════════════════════════════════════════════════════════════
/// HAI LOẠI TOẠ ĐỘ — ĐỌC TRƯỚC KHI SỬA BẤT KỲ DÒNG NÀO CÓ GetFootprintRect (V7)
/// • NEO   (anchor) = transform.position của Ghost / prefab. Art của dự án đặt pivot
///                    ở ĐÁY sprite nên đây là CHÂN công trình. Dùng cho: Instantiate,
///                    SnapCenter, ghi save, so khớp entry save.
/// • TÂM Ô (center) = tâm khối N×M ô. Dùng cho: GetFootprintRect, reservedRects,
///                    occupancyByObject, thảm xanh, giàn giáo, VFX.
/// • Đổi qua lại: AnchorToFootprintCenter() / PivotOffsetOf().
/// LỖI CŨ: truyền thẳng NEO vào GetFootprintRect → vùng ô tụt xuống nửa chiều cao
/// sprite (chuồng bò: 2.24 ô) → chặn oan đất trống dưới chân, bỏ sót phần mái,
/// thảm xanh lệch hẳn khỏi vùng thật.
/// ══════════════════════════════════════════════════════════════════════════
/// </summary>
public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }
    private const string PreferredBuildingSortingLayerName = "CongTrinh";
    private const string FallbackBuildingSortingLayerName = "Objects";
    private const int BuildingSortingOrder = 500;
    private static string resolvedBuildingSortingLayerName;
    private static string BuildingSortingLayerName
    {
        get
        {
            if (string.IsNullOrEmpty(resolvedBuildingSortingLayerName))
                resolvedBuildingSortingLayerName = ResolveSortingLayerName(
                    PreferredBuildingSortingLayerName,
                    FallbackBuildingSortingLayerName);
            return resolvedBuildingSortingLayerName;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 1. TOÁN LƯỚI — HẰNG SỐ CHUNG CHO CẢ ĐỘI
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cạnh một ô lưới, tính bằng world unit.
    ///
    /// VÌ SAO CHỐT 100 (không phải 50, không phải 150):
    ///   • Đo bounds 33 prefab trong CÔNG TRÌNH/: nhỏ nhất ~159×563 (cột đèn),
    ///     phổ biến 345×461 (decor) và 238–374 × 361–406 (nhà), lớn nhất ~694×446 (chuồng).
    ///     → Với CELL=100 mọi công trình rơi vào 2×6 … 7×5 ô: đúng dải Township (2×2…5×5).
    ///   • CELL=50 cho ra 7×10 ô cho một cái nhà → lưới quá mịn, snap gần như tự do,
    ///     hai công trình vẫn "ghé" sát vào nhau lệch nửa ô, và HashSet ô phình 4 lần.
    ///   • CELL=150 (bằng scale của Tilemap nền) làm tròn phí tới 45 % diện tích
    ///     (cột đèn rộng 159 → chiếm 300), và 3 tilemap nền trong SCN_Farm lệch nhau
    ///     (-290 / 0 / -28) nên KHÔNG tồn tại một lưới nền thống nhất để bám theo.
    ///   • 100 cũng là giá trị đã serialize sẵn của PlacementManager trong SCN_Farm
    ///     → đổi ObjectDragHandler (đang 50) về 100 là ít rủi ro nhất.
    ///   • Prefab dùng root scale = 100 nên "1 unit sprite = 1 ô" — số đo dễ nhẩm.
    /// </summary>
    public const float CELL = 100f;

    /// <summary>Gốc lưới. Ô (0,0) trải từ ORIGIN tới ORIGIN + (CELL, CELL).</summary>
    public static readonly Vector2 GridOrigin = Vector2.zero;

    /// <summary>Ô lưới chứa một điểm world.</summary>
    public static Vector2Int WorldToCell(Vector3 world) => new Vector2Int(
        Mathf.FloorToInt((world.x - GridOrigin.x) / CELL),
        Mathf.FloorToInt((world.y - GridOrigin.y) / CELL));

    /// <summary>Tâm world của một ô lưới.</summary>
    public static Vector3 CellCenterToWorld(Vector2Int cell) => new Vector3(
        GridOrigin.x + (cell.x + 0.5f) * CELL,
        GridOrigin.y + (cell.y + 0.5f) * CELL,
        0f);

    /// <summary>Góc dưới-trái (world) của một ô lưới.</summary>
    public static Vector3 CellCornerToWorld(int cellX, int cellY) => new Vector3(
        GridOrigin.x + cellX * CELL,
        GridOrigin.y + cellY * CELL,
        0f);

    /// <summary>
    /// SNAP TÂM Ô cho công trình N×M ô.
    ///
    /// CÔNG THỨC (đã xử lý lệch nửa ô khi cạnh CHẴN):
    ///     ox = Floor( (world.x - ORIGIN.x)/CELL - N*0.5 + 0.5 )   // ô trái nhất
    ///     oy = Floor( (world.y - ORIGIN.y)/CELL - M*0.5 + 0.5 )   // ô dưới nhất
    ///     center = ORIGIN + ( (ox + N*0.5)*CELL , (oy + M*0.5)*CELL )
    ///
    /// VÌ SAO ĐÚNG CHO CẢ CHẴN LẪN LẺ: tâm của khối N ô luôn nằm ở mốc
    /// (ox + N/2) ô. N LẺ → N/2 là số bán nguyên → tâm rơi vào TÂM một ô.
    /// N CHẴN → N/2 nguyên → tâm rơi vào ĐƯỜNG KẺ giữa hai ô. Đó chính là hành vi
    /// Township. Nếu ép mọi thứ về tâm ô (Round(x/CELL)*CELL + CELL/2) thì công
    /// trình cạnh chẵn sẽ luôn thò ra nửa ô — đây là lỗi "lệch nửa ô" hay gặp.
    ///
    /// Với N=M=1 công thức rút gọn đúng bằng Floor(x/CELL) + 0.5 ô (snap tâm ô).
    /// Dùng Floor(v + 0.5) thay cho Mathf.Round vì Round của Unity làm tròn về số
    /// chẵn ở đúng mốc .5 → nhảy ô không đều khi kéo chậm.
    /// </summary>
    public static Vector3 SnapCenter(Vector3 world, Vector2Int size)
    {
        int n = Mathf.Max(1, size.x);
        int m = Mathf.Max(1, size.y);
        int ox = Mathf.FloorToInt((world.x - GridOrigin.x) / CELL - n * 0.5f + 0.5f);
        int oy = Mathf.FloorToInt((world.y - GridOrigin.y) / CELL - m * 0.5f + 0.5f);
        return new Vector3(
            GridOrigin.x + (ox + n * 0.5f) * CELL,
            GridOrigin.y + (oy + m * 0.5f) * CELL,
            0f);
    }

    /// <summary>Vùng ô mà một công trình N×M chiếm khi tâm nằm ở centerWorld.</summary>
    public static RectInt GetFootprintRect(Vector3 centerWorld, Vector2Int size)
    {
        int n = Mathf.Max(1, size.x);
        int m = Mathf.Max(1, size.y);
        int ox = Mathf.FloorToInt((centerWorld.x - GridOrigin.x) / CELL - n * 0.5f + 0.5f);
        int oy = Mathf.FloorToInt((centerWorld.y - GridOrigin.y) / CELL - m * 0.5f + 0.5f);
        return new RectInt(ox, oy, n, m);
    }

    /// <summary>Tâm world của một vùng ô.</summary>
    public static Vector3 RectCenterWorld(RectInt rect) => new Vector3(
        GridOrigin.x + (rect.xMin + rect.width * 0.5f) * CELL,
        GridOrigin.y + (rect.yMin + rect.height * 0.5f) * CELL,
        0f);

    /// <summary>Tiện ích cho DEV-2: snap trực tiếp từ data + số bước xoay.</summary>
    public static Vector3 SnapCenterFor(PlaceableItemData data, Vector3 world, int rotationSteps)
        => SnapCenter(world, GridSizeOf(data, rotationSteps));

    /// <summary>
    /// Kích thước ô của một item sau khi xoay (bước lẻ hoán đổi X↔Y).
    ///
    /// CÓ LƯỚI AN TOÀN: nếu asset còn để mặc định 1×1 mà prefab thật to hơn 1 ô,
    /// tự đo bounds prefab rồi Ceil. Nếu không có lưới này thì trước khi Edric chạy
    /// `Tools/Farm/Suy Kích Thước Ô Công Trình`, giàn giáo của DEV-2 chỉ chiếm 1 ô
    /// (thay vì 7×5) và chỉ giữ 1 ô → đặt đè lên công trường đang xây được.
    /// Kết quả được cache để không đo lại mỗi frame.
    /// </summary>
    public static Vector2Int GridSizeOf(PlaceableItemData data, int rotationSteps)
    {
        if (data == null) return Vector2Int.one;

        Vector2Int baseSize = data.gridSize;

        if (baseSize.x <= 1 && baseSize.y <= 1 && data.prefabToBuild != null)
        {
            if (!_measuredSizeCache.TryGetValue(data, out Vector2Int measured))
            {
                measured = MeasureGridSizeFromPrefab(data.prefabToBuild);
                _measuredSizeCache[data] = measured;

                if (measured.x > 1 || measured.y > 1)
                    Debug.LogWarning($"[Placement] '{data.itemName}' chưa điền gridSize " +
                                     $"→ tạm đo từ prefab = {measured.x}×{measured.y} ô. " +
                                     "Chạy Tools ▸ Farm ▸ Suy Kích Thước Ô Công Trình để chốt.");
            }
            baseSize = measured;
        }

        baseSize.x = Mathf.Max(1, baseSize.x);
        baseSize.y = Mathf.Max(1, baseSize.y);

        // Bước xoay lẻ (90°, 270°) hoán đổi chiều
        return ((rotationSteps & 1) == 1) ? new Vector2Int(baseSize.y, baseSize.x) : baseSize;
    }

    private static readonly Dictionary<PlaceableItemData, Vector2Int> _measuredSizeCache = new();
    private static readonly Dictionary<PlaceableItemData, Vector2>    _pivotOffsetCache  = new();

    /// <summary>
    /// ĐỘ LỆCH PIVOT — khoảng cách từ điểm neo (pivot) tới TÂM hộp bao sprite.
    ///
    /// VÌ SAO CẦN: art của dự án đặt pivot ở ĐÁY sprite (đúng chuẩn — để chân nhà
    /// chạm đúng điểm đặt). Ví dụ chuồng bò cao 447 thì pivot lệch (0, 224).
    /// Nhưng GetFootprintRect coi điểm truyền vào là TÂM khối ô. Nếu truyền thẳng
    /// vị trí neo thì vùng ô bị kéo XUỐNG 2.24 ô so với thân nhà:
    ///   • chặn oan 2.5 ô đất trống phía dưới
    ///   • KHÔNG chặn phần mái phía trên → đặt đè lên nóc nhà được
    ///   • thảm xanh (vẽ ở tâm sprite) lệch hẳn với vùng ô thật
    /// Cộng bù offset này là khớp lại.
    /// </summary>
    public static Vector2 PivotOffsetOf(PlaceableItemData data, int rotationSteps)
    {
        if (data == null || data.prefabToBuild == null) return Vector2.zero;

        if (!_pivotOffsetCache.TryGetValue(data, out Vector2 off))
        {
            off = MeasurePivotOffsetFromPrefab(data.prefabToBuild);
            _pivotOffsetCache[data] = off;
        }

        // Xoay 90° thì offset cũng phải xoay theo, nếu không nhà xoay ngang sẽ lệch trục kia.
        return RotateOffset(off, rotationSteps);
    }

    /// <summary>
    /// Xoay một độ lệch theo số bước 90°, CÙNG CHIỀU với <see cref="RotationOf"/>.
    /// Tách riêng để nhánh "không tra được data" (vật do scene tự đặt, offset đo từ ghost
    /// clone) dùng CHUNG đúng một phép xoay với nhánh có data — hai công thức song song
    /// chính là nguồn gốc kinh điển của lỗi "xoay xong lệch hẳn một trục".
    /// </summary>
    public static Vector2 RotateOffset(Vector2 off, int rotationSteps) => (rotationSteps & 3) switch
    {
        1 => new Vector2( off.y, -off.x),
        2 => new Vector2(-off.x, -off.y),
        3 => new Vector2(-off.y,  off.x),
        _ => off
    };

    /// <summary>
    /// Độ lệch pivot của vật ĐANG CẦM (đã xoay theo rotationSteps).
    ///
    /// Có data → đo từ prefab (chuẩn nhất, có cache).
    /// KHÔNG có data (đang sửa một vật do scene tự đặt, không tra ngược được) → dùng
    /// độ lệch đo từ chính bản clone visual của Ghost, ghi sẵn trong CacheCloneLocalCenter().
    /// Nếu bỏ nhánh sau thì Edit Mode của vật scene sẽ lại chặn lệch đúng như bug cũ.
    /// </summary>
    private Vector2 CurrentPivotOffset()
    {
        PlaceableItemData data = currentlyEditingBuilding != null ? editingItemData : currentItem;
        if (data != null && data.prefabToBuild != null)
            return PivotOffsetOf(data, rotationSteps);

        return RotateOffset(fallbackPivotOffset, rotationSteps);
    }

    /// <summary>Tâm vùng ô của ghost đang cầm — đã cộng bù độ lệch pivot.</summary>
    private Vector3 CurrentFootprintCenter()
    {
        if (currentGhost == null) return Vector3.zero;
        Vector2 o = CurrentPivotOffset();
        Vector3 p = currentGhost.transform.position;
        return new Vector3(p.x + o.x, p.y + o.y, 0f);
    }

    /// <summary>Đổi vị trí NEO của công trình thành TÂM vùng ô mà nó thực sự chiếm.</summary>
    public static Vector3 AnchorToFootprintCenter(Vector3 anchorWorld, PlaceableItemData data,
                                                  int rotationSteps)
    {
        Vector2 o = PivotOffsetOf(data, rotationSteps);
        return new Vector3(anchorWorld.x + o.x, anchorWorld.y + o.y, anchorWorld.z);
    }

    /// <summary>
    /// Chiều NGƯỢC LẠI: từ TÂM vùng ô suy ra ĐIỂM NEO để Instantiate prefab.
    /// DEV-2 (ConstructionSite) dùng để biết đặt công trình xây xong ở đâu, sau khi
    /// giàn giáo đã được dựng quanh tâm.
    /// </summary>
    public static Vector3 FootprintCenterToAnchor(Vector3 centerWorld, PlaceableItemData data,
                                                  int rotationSteps)
    {
        Vector2 o = PivotOffsetOf(data, rotationSteps);
        return new Vector3(centerWorld.x - o.x, centerWorld.y - o.y, centerWorld.z);
    }

    /// <summary>
    /// TÂM vùng ô của một object ĐÃ nằm trên map.
    ///
    /// Ở đây KHÔNG dùng AnchorToFootprintCenter mà đo thẳng hộp bao THẬT trong scene:
    /// bounds của renderer đã bao gồm sẵn cả pivot lệch LẪN phép xoay của transform,
    /// nên đúng cho cả vật do ta Instantiate lẫn vật designer kéo tay vào scene
    /// (những vật này không tra ra PlaceableItemData nên không có prefab để đo).
    /// Không đo được (object không có sprite) thì lùi về chính vị trí neo.
    /// </summary>
    private static Vector3 FootprintCenterOfSpawned(GameObject go)
    {
        if (go == null) return Vector3.zero;

        Bounds b = MeasureWorldBounds(go);
        if (b.size.x <= 0.001f && b.size.y <= 0.001f)
            return new Vector3(go.transform.position.x, go.transform.position.y, 0f);

        return new Vector3(b.center.x, b.center.y, 0f);
    }

    /// <summary>Đo tâm hộp bao sprite trong hệ toạ độ ROOT prefab (= độ lệch so với pivot).</summary>
    private static Vector2 MeasurePivotOffsetFromPrefab(GameObject prefab)
    {
        return TryMeasurePrefabVisualBounds(prefab, out Bounds b)
             ? new Vector2(b.center.x, b.center.y)
             : Vector2.zero;
    }

    /// <summary>Đo hộp bao mọi SpriteRenderer của prefab rồi quy ra số ô (làm tròn LÊN).</summary>
    private static Vector2Int MeasureGridSizeFromPrefab(GameObject prefab)
    {
        if (!TryMeasurePrefabVisualBounds(prefab, out Bounds b)) return Vector2Int.one;

        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(b.size.x / CELL)),
            Mathf.Max(1, Mathf.CeilToInt(b.size.y / CELL)));
    }

    /// <summary>
    /// HỘP BAO VISUAL CỦA MỘT PREFAB ASSET, quy về hệ toạ độ ROOT prefab.
    /// Một hàm DUY NHẤT cho cả kích thước ô lẫn độ lệch pivot — trước đây hai hàm chép
    /// tay của nhau và đã trôi khác nhau, dẫn tới size và offset nói hai chuyện khác nhau.
    ///
    /// ⚠ HAI CHI TIẾT SỐNG CÒN:
    ///  1. Tâm mỗi mảnh phải lấy bằng `TransformPoint(sprite.bounds.center)`, KHÔNG phải
    ///     `transform.position`. Art của dự án đặt pivot ở ĐÁY sprite, nên transform.position
    ///     là CHÂN chứ không phải tâm. Dùng transform.position thì prefab một-sprite luôn
    ///     ra offset (0,0) và toàn bộ phép bù pivot trở thành vô tác dụng.
    ///  2. Phải quy về ROOT bằng InverseTransformPoint chứ không dùng localPosition —
    ///     localPosition là toạ độ so với CHA TRỰC TIẾP nên prefab lồng sâu (Pen_03/04,
    ///     May_01..03) sẽ ra hộp bao lệch.
    /// Bộ lọc tên dùng chung IsValidSourceVisualRenderer để khớp với Editor tool
    /// (Tools ▸ Farm ▸ Suy Kích Thước Ô Công Trình) — hai bên phải ra CÙNG con số.
    /// </summary>
    private static bool TryMeasurePrefabVisualBounds(GameObject prefab, out Bounds bounds)
    {
        bounds = default;
        if (prefab == null) return false;

        bool found = false;

        foreach (SpriteRenderer sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!IsValidSourceVisualRenderer(sr)) continue;

            // drawMode Sliced/Tiled lấy kích thước từ sr.size, không phải sprite gốc.
            Vector2 localSize = sr.drawMode == SpriteDrawMode.Simple
                              ? (Vector2)sr.sprite.bounds.size
                              : sr.size;

            // Prefab CHƯA Instantiate nên sr.bounds (world) không đáng tin → tự nhân lossyScale.
            Vector3 s = sr.transform.lossyScale;
            float   w = Mathf.Abs(localSize.x * s.x);
            float   h = Mathf.Abs(localSize.y * s.y);
            if (w <= 0.0001f || h <= 0.0001f) continue;

            Vector3 centerInRoot = prefab.transform.InverseTransformPoint(
                sr.transform.TransformPoint(sr.sprite.bounds.center));

            var one = new Bounds(centerInRoot, new Vector3(w, h, 0f));
            if (!found) { bounds = one; found = true; }
            else        { bounds.Encapsulate(one); }
        }

        return found;
    }

    /// <summary>Quaternion tương ứng số bước xoay 90° (0-3). Xoay theo chiều kim đồng hồ.</summary>
    public static Quaternion RotationOf(int rotationSteps)
        => Quaternion.Euler(0f, 0f, -90f * (rotationSteps & 3));

    /// <summary>Đọc ngược số bước xoay từ transform (dùng khi sửa công trình cũ).</summary>
    public static int RotationStepsOf(Transform t)
    {
        if (t == null) return 0;
        int steps = Mathf.RoundToInt(-t.eulerAngles.z / 90f);
        return ((steps % 4) + 4) % 4;
    }

    // ══════════════════════════════════════════════════════════════════════

    /// <summary>CameraController đọc flag này để block pan khi user đang bưng vật phẩm.</summary>
    public static bool IsPlacingNewObject { get; private set; }

    /// <summary>True khi đang trong luồng di chuyển công trình cũ (Edit Mode).</summary>
    public bool IsEditingBuilding => currentlyEditingBuilding != null;

    /// <summary>Số bước xoay 90° hiện tại của Ghost (0-3). DEV-2 đọc khi nhận TryStartConstruction.</summary>
    public int CurrentRotationSteps => rotationSteps;

    // Key PlayerPrefs lưu danh sách công trình — dùng chung bởi PlotController.DebugClearData()
    public const string BuildingsSaveKey = "FARM_PLACED_BUILDINGS";

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Ghost Prefab")]
    public GameObject placementGhostPrefab;

    [Header("Grid Footprint")]
    public Sprite footprintSprite;          // Sprite lưới dùng chung cho Ghost và tất cả building footprint

    [Header("Biên bản đồ (V4)")]
    [Tooltip("Bật = không cho đặt công trình ra ngoài vùng đất thật.")]
    public bool enforceMapBounds = true;
    [Tooltip("Ghi đè biên thủ công (minX, maxX, minY, maxY). Để cả 4 = 0 thì tự dò từ Tilemap.")]
    public Vector4 mapBoundsOverride = Vector4.zero;
    [Tooltip("Nới thêm/thu bớt biên, world unit. Dương = nới rộng.")]
    public float mapBoundsPadding = 0f;

    [Header("Debug")]
    [Tooltip("In log mỗi lần dò lại biên bản đồ và mỗi lần từ chối vì chồng lấn.")]
    public bool verboseGridLog = false;

    // ── Runtime state ────────────────────────────────────────────────────────

    private bool              isPlacing;
    private GameObject        currentGhost;
    private SpriteRenderer    houseRenderer;
    private Button            btnConfirm;
    private RectTransform     confirmRect;
    private RectTransform     cancelRect;
    private RectTransform     rotateRect;
    private Transform         ghostVisualCloneRoot;
    private PlacementGhostVisualController ghostVisual;
    private static Sprite     ghostActionBarSprite;
    private PlaceableItemData currentItem;      // item đang MUA (null khi đang sửa)
    private PlaceableItemData editingItemData;  // data suy ra của công trình đang SỬA (có thể null)
    private bool              isValidPos;
    private int               rotationSteps;    // 0-3, mỗi bước 90°

    // Danh sách runtime của các công trình đã đặt (đồng bộ với PlayerPrefs)
    private readonly List<BuildingEntry> placedBuildings = new();

    // ── Edit Mode state ───────────────────────────────────────────────────────
    private EditableBuilding currentlyEditingBuilding;
    private Vector3          originalEditPosition;
    private int              originalEditRotationSteps;

    // ── Ghost footprint / animation ───────────────────────────────────────────
    private Transform footprintTransform; // "Grid_Footprint" child trong Ghost
    private Coroutine pickupRoutine;
    private Vector3   pickupEndScale = Vector3.one;
    private Vector3   pickupEndPos   = Vector3.zero;
    private Vector3   cloneLocalCenter;              // tâm visual trong hệ toạ độ Ghost (để xoay quanh tâm)
    private Vector3   cloneRotationCompensation;     // bù vị trí do xoay quanh tâm
    private Vector2Int fallbackGridSize = Vector2Int.one; // cỡ ô đo từ bounds khi không tra được data
    private Vector2   fallbackPivotOffset = Vector2.zero; // độ lệch pivot (CHƯA xoay) đo từ ghost clone
    private float     lastRotateTime = -1f;          // chống xoay 2 lần trong 1 cú bấm (xem RotateGhost)

    // ── Bảng ô đã bị chiếm (V3) ───────────────────────────────────────────────
    // occupancyByObject : object đang đứng trên map → vùng ô nó chiếm
    // knownSizes        : nhớ kích thước ô của object do chính ta Instantiate
    //                     (chính xác hơn đo bounds, vì lấy thẳng từ data)
    // reservedRects     : ô do ConstructionManager (DEV-2) giữ chỗ trong lúc đang xây
    // occupiedCells     : HashSet phẳng để kiểm tra O(số ô) mỗi frame
    private readonly Dictionary<GameObject, RectInt>    occupancyByObject = new();
    private readonly Dictionary<GameObject, Vector2Int> knownSizes        = new();
    private readonly List<RectInt>                      reservedRects     = new();
    private readonly HashSet<Vector2Int>                occupiedCells     = new();

    /// <summary>Chỉ đọc — tập ô đang bị chiếm. DEV-2 / tool debug có thể dùng.</summary>
    public IReadOnlyCollection<Vector2Int> OccupiedCells => occupiedCells;

    // ── Cache biên bản đồ ─────────────────────────────────────────────────────
    private Bounds _mapBounds;
    private bool   _mapBoundsReady;

    // ── Serializable helpers ─────────────────────────────────────────────────

    [Serializable]
    private class BuildingEntry
    {
        public string itemId;
        public float  x, y;
        public int    plotId; // 0 = save cũ chưa có, >0 = ID đã gán
        // TƯƠNG THÍCH NGƯỢC: save cũ không có key "rot".
        // JsonUtility bỏ qua field thiếu và giữ giá trị mặc định của C# = 0 → không xoay.
        public int    rot;
    }

    [Serializable]
    private class BuildingsSave
    {
        public List<BuildingEntry> list = new();
    }

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        LoadBuildings();
        RefreshOccupancy();
    }

    private void Update()
    {
        if (!isPlacing || currentGhost == null) return;

        // Nút UI được ưu tiên tuyệt đối — kiểm tra trước khi xử lý drag
        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOverRect(confirmRect)) { ConfirmPlacement(); return; }
            if (IsMouseOverRect(cancelRect))  { CancelPlacement();  return; }
            if (IsMouseOverRect(rotateRect))  { RotateGhost();      return; }
        }

        // DEV/Edit: phím Delete hoặc Backspace → XÓA HẲN vật đang sửa (chỉ khi đang edit vật có sẵn).
        if (currentlyEditingBuilding != null &&
            (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            DeleteEditingBuilding();
            return;
        }

        // Phím R: xoay nhanh khi test trong Editor (nút ↻ vẫn là đường chính).
        if (Input.GetKeyDown(KeyCode.R)) { RotateGhost(); return; }

        Vector2Int size = CurrentGridSize();

        // Ghost chỉ di chuyển KHI ĐANG GIỮ chuột trái.
        // Khi thả chuột, Ghost đứng yên → user tự do rê chuột xuống bấm nút V / X / ↻.
        if (Input.GetMouseButton(0))
        {
            currentGhost.transform.position = GetSnappedMousePos(size);
        }

        // ── VALIDATION theo Ô LƯỚI (thay cho Physics2D.OverlapBox) ──
        // Hộp va chạm giờ đúng bằng footprint N×M ô, không còn 50×50 cứng.
        // PHẢI cộng bù pivot: ghost neo ở ĐÁY sprite, còn GetFootprintRect cần TÂM khối ô.
        // Không bù thì vùng chặn tụt xuống 2.24 ô → chặn oan đất trống dưới chân
        // và bỏ sót phần mái phía trên (đặt đè lên nóc nhà được).
        RectInt rect = GetFootprintRect(CurrentFootprintCenter(), size);
        bool free    = IsAreaFree(rect);
        bool inside  = IsRectInsideMap(rect);
        isValidPos   = free && inside;

        if (verboseGridLog && !isValidPos)
            Debug.Log($"[Placement] Ô {rect} KHÔNG hợp lệ — chồng lấn:{!free} ngoài biên:{!inside}");

        if (ghostVisual != null)
            ghostVisual.SetValid(isValidPos);

        if (btnConfirm != null)
            btnConfirm.interactable = isValidPos;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ ShopItemUI ngay sau khi trừ tiền thành công.
    /// Đẻ ra Ghost, gán sprite đúng vật phẩm, tự bind nút V / X / ↻.
    /// </summary>
    public void StartPlacingNewObject(PlaceableItemData itemData)
    {
        if (itemData == null || itemData.prefabToBuild == null)
        {
            return;
        }

        // Hủy ghost cũ nếu có (trường hợp gọi đè)
        if (currentGhost != null) Destroy(currentGhost);

        currentItem     = itemData;
        editingItemData = null;
        rotationSteps   = 0;
        cloneRotationCompensation = Vector3.zero;

        // Cập nhật bảng ô đã chiếm TRƯỚC khi bật ghost — nếu không, lượt đặt đầu tiên
        // sẽ thấy map trống trơn và cho đè lên công trình có sẵn trong scene.
        RefreshOccupancy();

        // Dùng GridSizeOf (có lưới an toàn đo prefab) chứ KHÔNG dùng GetGridSize trần.
        // Nếu dùng bản trần thì asset chưa điền gridSize sẽ ra 1×1, trong khi
        // ConstructionManager lại dùng GridSizeOf ra 7×5 → vùng giữ chỗ và giàn giáo
        // lệch nhau, đúng thứ gây "đè" và "lệch".
        Vector2Int size = GridSizeOf(itemData, 0);
        currentGhost = Instantiate(placementGhostPrefab, GetSnappedMousePos(size), Quaternion.identity);

        // Vô hiệu hóa toàn bộ Collider2D trên Ghost — tránh tia chuột bắn trúng Ghost
        foreach (var col in currentGhost.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        SetupGhostVisualController(showLiftArrow: false);

        // ── Tìm SpriteRenderer của ngôi nhà ──
        houseRenderer = FindBuildingVisualRenderer(currentGhost);

        // Clone toàn bộ visual sprite từ prefab thật để công trình nhiều phần vẫn hiện đủ.
        BuildGhostVisualFromSource(itemData.prefabToBuild.transform);
        if (ghostVisualCloneRoot == null || ghostVisualCloneRoot.childCount == 0)
            BuildGhostVisualFromSource(itemData.prefabToBuild.transform, relaxed: true);

        CacheCloneLocalCenter();
        SetupFootprint(size);

        ConfigureGhostCanvas();
        BindGhostButtons(bindDelete: false);

        StartCoroutine(AnimateGhostActionBar());

        isPlacing          = true;
        IsPlacingNewObject = true;  // CameraController tự khóa pan
    }

    // ── Edit Building ────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ EditableBuilding.OnMouseDown khi Edit Mode đang bật.
    /// Ẩn công trình gốc, spawn Ghost tại vị trí hiện tại, cho phép kéo thả như mua đồ mới.
    /// </summary>
    public void StartEditBuilding(EditableBuilding target)
    {
        if (target == null) return;

        // Hủy ghost cũ nếu đang có (tránh gọi đè)
        if (currentGhost != null) Destroy(currentGhost);

        currentlyEditingBuilding  = target;
        originalEditPosition      = target.transform.position;
        originalEditRotationSteps = RotationStepsOf(target.transform);
        rotationSteps             = originalEditRotationSteps;
        currentItem               = null;
        cloneRotationCompensation = Vector3.zero;

        // Suy ngược data từ tên prefab để biết kích thước ô thật.
        // Nếu không tra ra (vật do scene tự đặt) thì fallback đo bounds ở CurrentGridSize().
        editingItemData = FindItemByPrefabName(target.gameObject.name);

        // Bỏ chính nó ra khỏi bảng ô đã chiếm, nếu không sẽ tự chặn chính mình.
        RefreshOccupancy();

        currentGhost = Instantiate(placementGhostPrefab, originalEditPosition, Quaternion.identity);

        // Vô hiệu hóa toàn bộ Collider2D trên Ghost
        foreach (var col in currentGhost.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        SetupGhostVisualController(showLiftArrow: true);

        // ── Gán sprite từ công trình gốc vào Ghost ──
        houseRenderer = FindBuildingVisualRenderer(currentGhost);

        // LUÔN clone visual của ô đất/công trình gốc (đừng phụ thuộc houseRenderer).
        BuildGhostVisualFromSource(target.transform);
        // FIX biến mất: nếu clone RỖNG (sprite bị lọc tên / null) → clone NỚI LỎNG để vật vẫn hiện.
        if (ghostVisualCloneRoot == null || ghostVisualCloneRoot.childCount == 0)
            BuildGhostVisualFromSource(target.transform, relaxed: true);

        CacheCloneLocalCenter();

        // Ẩn công trình gốc — Ghost đóng vai trò "placeholder" trong khi kéo
        target.gameObject.SetActive(false);

        Vector2Int size = CurrentGridSize();
        // Snap lại ngay: công trình cũ có thể đang nằm lệch lưới (đặt từ bản build trước).
        // Snap ĐIỂM NEO (không phải tâm ô) — đúng cùng mốc lưới mà GetSnappedMousePos dùng
        // khi đặt mới, nên kéo ra kéo vào một công trình không làm nó dịch đi nửa ô.
        // Phần bù pivot được cộng riêng ở CurrentFootprintCenter(), không đụng vào đây.
        currentGhost.transform.position = SnapCenter(originalEditPosition, size);
        SetupFootprint(size);

        ConfigureGhostCanvas();
        BindGhostButtons(bindDelete: true);

        StartCoroutine(AnimateGhostActionBar());

        isPlacing          = true;
        IsPlacingNewObject = true;

        // Hiệu ứng nhấc lên: chỉ tác động visual, footprint giữ nguyên mặt đất
        Transform pickupVisual = ghostVisualCloneRoot != null ? ghostVisualCloneRoot : houseRenderer != null ? houseRenderer.transform : null;
        if (pickupVisual != null)
            pickupRoutine = StartCoroutine(AnimatePickup(pickupVisual, footprintTransform));
    }

    /// <summary>
    /// Nhấc visual lên (scale ×1.1, Y +30).
    /// footprintToFreeze được giữ nguyên localScale mỗi frame
    /// để thảm xanh không bị kéo theo khi visual là root hoặc parent của nó.
    /// </summary>
    private IEnumerator AnimatePickup(Transform visual, Transform footprintToFreeze = null)
    {
        Vector3 startScale = visual.localScale;
        Vector3 startPos   = visual.localPosition;
        Vector3 overshootScale = startScale * 1.18f;
        Vector3 endScale   = startScale * 1.1f;
        float ghostScaleY = currentGhost != null ? Mathf.Max(0.0001f, Mathf.Abs(currentGhost.transform.lossyScale.y)) : 1f;
        Vector3 liftOffset = new Vector3(0f, 30f / ghostScaleY, 0f);
        Vector3 overshootOffset = new Vector3(0f, 36f / ghostScaleY, 0f);
        Vector3 endPos     = startPos + liftOffset;
        Vector3 overshootPos = startPos + overshootOffset;

        // Ghi lại đích để RotateGhost có thể kết thúc animation ngay lập tức mà
        // không để visual kẹt ở khung hình dở dang.
        pickupEndScale = endScale;
        pickupEndPos   = endPos;

        // Ghi nhớ cả scale lẫn localPosition của footprint — đóng băng hoàn toàn trong lúc nhấc
        Vector3 frozenScale = footprintToFreeze != null ? footprintToFreeze.localScale    : Vector3.one;
        Vector3 frozenPos   = footprintToFreeze != null ? footprintToFreeze.localPosition : Vector3.zero;

        float elapsed  = 0f;
        const float upDuration = 0.16f;

        while (elapsed < upDuration)
        {
            float t      = elapsed / upDuration;
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            visual.localScale    = Vector3.LerpUnclamped(startScale, overshootScale, smooth);
            visual.localPosition = Vector3.LerpUnclamped(startPos,   overshootPos,   smooth) + cloneRotationCompensation;

            // Ép footprint về đúng vị trí và scale mỗi frame — thảm xanh không bay theo
            if (footprintToFreeze != null)
            {
                footprintToFreeze.localScale    = frozenScale;
                footprintToFreeze.localPosition = frozenPos;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        const float settleDuration = 0.12f;
        while (elapsed < settleDuration)
        {
            float t = elapsed / settleDuration;
            float smooth = 1f - (1f - t) * (1f - t);
            visual.localScale = Vector3.LerpUnclamped(overshootScale, endScale, smooth);
            visual.localPosition = Vector3.LerpUnclamped(overshootPos, endPos, smooth) + cloneRotationCompensation;

            if (footprintToFreeze != null)
            {
                footprintToFreeze.localScale    = frozenScale;
                footprintToFreeze.localPosition = frozenPos;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        visual.localScale    = endScale;
        visual.localPosition = endPos + cloneRotationCompensation;

        if (footprintToFreeze != null)
        {
            footprintToFreeze.localScale    = frozenScale;
            footprintToFreeze.localPosition = frozenPos;
        }

        pickupRoutine = null;
    }

    // ── XOAY (V5) ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn vào Btn_Rotate. Xoay 90° mỗi lần bấm, vòng 0→1→2→3→0.
    /// Bước LẺ hoán đổi kích thước ô X↔Y (data.GetGridSize lo việc này),
    /// nên phải snap lại + vẽ lại thảm ngay sau khi xoay.
    /// </summary>
    public void RotateGhost()
    {
        if (currentGhost == null) return;

        // CHỐNG XOAY ĐÚP: nút ↻ đi qua HAI đường —
        //   (1) Update() bắt GetMouseButtonDown + IsMouseOverRect (đường chính, vì Canvas
        //       world-space của Ghost không phải lúc nào cũng nhận được raycast của EventSystem)
        //   (2) Button.onClick do BindGhostButtons gắn (fire lúc nhả chuột)
        // Btn_Confirm / Btn_Cancel không bị lỗi này vì chúng huỷ Ghost ngay ở đường (1).
        // Nút xoay thì Ghost vẫn sống → nếu không chặn sẽ quay 180° mỗi lần bấm.
        if (Time.unscaledTime - lastRotateTime < 0.15f) return;
        lastRotateTime = Time.unscaledTime;

        rotationSteps = (rotationSteps + 1) & 3;

        ApplyGhostRotationVisual();

        Vector2Int size = CurrentGridSize();
        // Snap lại theo kích thước MỚI: 3×2 và 2×3 có mốc tâm khác nhau
        // (một cạnh chẵn, một cạnh lẻ) → không snap lại là lệch nửa ô ngay.
        currentGhost.transform.position = SnapCenter(currentGhost.transform.position, size);
        SetupFootprint(size);
    }

    /// <summary>
    /// Xoay riêng phần visual clone (KHÔNG xoay cả Ghost) — nếu xoay Ghost thì
    /// hàng nút ✕ ↻ ✓ và khung footprint cũng quay theo, nhìn hỏng hoàn toàn.
    /// Xoay quanh TÂM visual chứ không quanh pivot, để công trình không "văng" đi
    /// khi pivot lệch tâm.
    /// </summary>
    private void ApplyGhostRotationVisual()
    {
        Transform t = ghostVisualCloneRoot;
        if (t == null) return;

        // Kết thúc animation nhấc ngay lập tức: coroutine ghi đè localPosition mỗi frame
        // nên nếu để chạy song song thì phần bù xoay sẽ bị xoá.
        if (pickupRoutine != null)
        {
            StopCoroutine(pickupRoutine);
            pickupRoutine  = null;
            t.localScale   = pickupEndScale;
            t.localPosition = pickupEndPos;
        }

        Quaternion q = RotationOf(rotationSteps);
        Vector3 basePos = t.localPosition - cloneRotationCompensation;
        cloneRotationCompensation = cloneLocalCenter - (q * cloneLocalCenter);
        t.localRotation  = q;
        t.localPosition  = basePos + cloneRotationCompensation;
    }

    /// <summary>
    /// Ghi nhớ tâm visual (hệ toạ độ Ghost) để xoay quanh tâm, và đo sẵn cỡ ô dự phòng.
    /// PHẢI gọi KHI CLONE CHƯA XOAY: sau khi xoay thì bounds đã đổi chiều, đo lại sẽ
    /// hoán đổi X↔Y thêm một lần nữa (lỗi xoay-hai-lần).
    /// </summary>
    private void CacheCloneLocalCenter()
    {
        cloneLocalCenter    = Vector3.zero;
        fallbackGridSize    = Vector2Int.one;
        fallbackPivotOffset = Vector2.zero;
        if (currentGhost == null || ghostVisualCloneRoot == null) return;

        Bounds b = CalculateSourceVisualBounds(ghostVisualCloneRoot);
        if (b.size.x <= 0.01f && b.size.y <= 0.01f) return;

        Vector3 c = currentGhost.transform.InverseTransformPoint(b.center);
        c.z = 0f;
        cloneLocalCenter = c;

        // ĐỘ LỆCH PIVOT DỰ PHÒNG (world unit, CHƯA xoay — clone luôn được dựng ở hướng gốc).
        // Dùng khi đang SỬA một vật do scene tự đặt: không tra ra PlaceableItemData nên
        // PivotOffsetOf trả 0, mà pivot vẫn ở đáy → không có dòng này thì Edit Mode lại
        // chặn lệch xuống dưới đúng như bug cũ.
        // Sai số ~3 % do clone được phóng 1.03 lần cho dễ nhìn — nhỏ hơn nhiều so với 1 ô.
        Vector3 gp = currentGhost.transform.position;
        fallbackPivotOffset = new Vector2(b.center.x - gp.x, b.center.y - gp.y);

        RectInt r = RectFromWorldBounds(b);
        if (r.width > 0 && r.height > 0)
            fallbackGridSize = new Vector2Int(r.width, r.height);
    }

    // ── Footprint / khung xanh ────────────────────────────────────────────────

    /// <summary>
    /// Vẽ thảm xanh + khung 4 góc ĐÚNG BẰNG footprint lưới (N×M ô).
    ///
    /// SỬA SO VỚI BẢN CŨ: trước đây thảm được suy từ bounds sprite rồi Ceil lên bội số ô
    /// và nhân 1.08 → thảm to hơn vùng ô thật, người chơi tưởng chiếm chỗ nhiều hơn.
    /// Giờ thảm = đúng N*CELL × M*CELL.
    ///
    /// BÙ PIVOT (V7): thảm KHÔNG còn đặt ở gốc Ghost nữa mà đặt ĐÚNG TÂM `rect` mà
    /// Update() dùng để validate. Ghost neo ở ĐÁY sprite, nên thảm vẽ quanh gốc Ghost sẽ
    /// nằm thấp hơn vùng ô thật ~2 ô: người chơi thấy xanh nhưng vẫn bị từ chối, hoặc
    /// thấy trống mà không đặt được. Nguyên tắc: THẢM XANH = VÙNG SẼ BỊ CHẶN, không hơn
    /// không kém.
    ///
    /// Độ lệch này là HẰNG SỐ theo (size, rotation) nên đặt một lần ở đây là đủ, thảm
    /// không "trôi" khi kéo: neo luôn snap theo bước tròn 1 ô, mà rect cũng nhảy theo
    /// đúng 1 ô → hiệu số không đổi. (Chứng minh: neo = (k + N/2)·CELL với k nguyên;
    ///  rect.xMin = Floor(k + offX/CELL + 0.5) = k + hằng số.)
    /// </summary>
    private void SetupFootprint(Vector2Int size)
    {
        if (currentGhost == null) return;

        float targetW = Mathf.Max(1, size.x) * CELL;
        float targetH = Mathf.Max(1, size.y) * CELL;

        // Tâm world của ĐÚNG vùng ô đang được validate — nguồn sự thật cho cả thảm lẫn khung.
        Vector3 rectCenterWorld = RectCenterWorld(GetFootprintRect(CurrentFootprintCenter(), size));

        footprintTransform = currentGhost.transform.Find("Grid_Footprint");

        SpriteRenderer fpSR = footprintTransform != null ? footprintTransform.GetComponent<SpriteRenderer>() : null;
        Sprite footprintSourceSprite = fpSR != null ? fpSR.sprite : null;
        if (footprintSprite != null)
        {
            footprintSourceSprite = footprintSprite;
            if (fpSR != null) fpSR.sprite = footprintSprite;
        }

        if (footprintTransform != null)
        {
            float ghostScaleX = Mathf.Max(0.0001f, Mathf.Abs(currentGhost.transform.lossyScale.x));
            float ghostScaleY = Mathf.Max(0.0001f, Mathf.Abs(currentGhost.transform.lossyScale.y));
            float localTargetW = targetW / ghostScaleX;
            float localTargetH = targetH / ghostScaleY;

            Vector2 spriteSize = footprintSourceSprite != null ? (Vector2)footprintSourceSprite.bounds.size : Vector2.one;
            footprintTransform.localScale = new Vector3(
                spriteSize.x > 0f ? localTargetW / spriteSize.x : localTargetW,
                spriteSize.y > 0f ? localTargetH / spriteSize.y : localTargetH,
                1f);

            // Đặt thảm tại TÂM VÙNG Ô chứ không tại gốc Ghost (xem phần bù pivot ở doc trên).
            Vector3 fpLocal = currentGhost.transform.InverseTransformPoint(rectCenterWorld);
            fpLocal.z = 0f;
            footprintTransform.localPosition = fpLocal;
            footprintTransform.gameObject.SetActive(true);

            // Khung procedural của DEV-2 đẹp hơn sprite thảm gốc → tắt sprite, giữ transform.
            if (fpSR != null && ghostVisual != null)
                fpSR.enabled = false;
        }

        if (ghostVisual != null)
        {
            ghostVisual.SetTileSprite(footprintSourceSprite);
            // paddingMultiplier = 1 vì kích thước truyền vào ĐÃ là footprint chuẩn.
            // Tâm = rectCenterWorld (KHÔNG phải vị trí Ghost) để khung 4 góc trùng khít thảm.
            ghostVisual.ConfigureFromWorldBounds(
                new Bounds(rectCenterWorld, new Vector3(targetW, targetH, 0f)), 1f);
        }
    }

    /// <summary>
    /// EditModeManager gọi để bật/tắt thảm xanh của Ghost đang hoạt động.
    /// Dùng khi Edit Mode được toggle trong lúc Ghost đã tồn tại trên scene.
    /// </summary>
    public void SetGhostFootprintActive(bool state)
    {
        if (footprintTransform != null)
            footprintTransform.gameObject.SetActive(state);
    }

    // ── Xác nhận & Hủy ──────────────────────────────────────────────────────

    // Tên các object con vật bên trong prefab chuồng
    private static readonly string[] AnimalChildNames = { "bonam1", "ga", "heo" };

    /// <summary>
    /// Sau khi Instantiate chuồng, đảm bảo object con vật hiển thị đúng:
    /// SetActive(true) + SortingLayer và OrderInLayer khớp chuồng + offset.
    /// </summary>
    private static void FixAnimalVisibility(GameObject buildingObj)
    {
        // Lấy SpriteRenderer gốc của chuồng (bỏ qua các SR con)
        SpriteRenderer buildingSR = buildingObj.GetComponent<SpriteRenderer>();
        if (buildingSR == null)
            buildingSR = buildingObj.GetComponentInChildren<SpriteRenderer>(true);

        string sortingLayerName = buildingSR != null ? buildingSR.sortingLayerName : "Default";
        int    baseOrder        = buildingSR != null ? buildingSR.sortingOrder      : 0;

        foreach (string animalName in AnimalChildNames)
        {
            Transform t = buildingObj.transform.Find(animalName);
            if (t == null) continue;

            t.gameObject.SetActive(true);

            foreach (SpriteRenderer sr in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder     = baseOrder + 10;
            }
        }
    }

    private static void FixBuildingRenderSorting(GameObject buildingObj)
    {
        if (buildingObj == null) return;

        SpriteRenderer[] renderers = buildingObj.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            sr.sortingLayerName = BuildingSortingLayerName;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, BuildingSortingOrder);
        }
    }

    /// <summary>Gắn vào Btn_Confirm. Đặt công trình xuống map (mới hoặc edit), xóa Ghost.</summary>
    private void ConfirmPlacement()
    {
        if (!isValidPos)
        {
            if (ghostVisual != null)
                ghostVisual.SetValid(false);
            return;
        }

        // Ghost đứng yên khi user thả chuột → vị trí ghost chính là vị trí đặt công trình.
        // `pos` là ĐIỂM NEO (pivot ở ĐÁY sprite) — đây là toạ độ dùng để Instantiate và
        // để ghi save. KHÔNG phải tâm khối ô.
        Vector3 pos = currentGhost.transform.position;
        pos.z = 0f;

        Vector2Int size = CurrentGridSize();

        // Tâm khối ô tương ứng — mọi phép tính LƯỚI (reserve / occupancy) phải dùng cái này.
        Vector3 footprintCenter = CurrentFootprintCenter();

        // ── Nhánh Edit Mode: di chuyển công trình cũ sang vị trí mới ──
        if (currentlyEditingBuilding != null)
        {
            GameObject moved = currentlyEditingBuilding.gameObject;
            moved.transform.position = pos;
            moved.transform.rotation = RotationOf(rotationSteps);
            moved.SetActive(true);

            knownSizes[moved] = size;

            // Cập nhật vị trí + hướng xoay trong save data
            // (khớp theo tọa độ cũ vì grid đảm bảo không trùng)
            foreach (BuildingEntry e in placedBuildings)
            {
                if (Mathf.Approximately(e.x, originalEditPosition.x) &&
                    Mathf.Approximately(e.y, originalEditPosition.y))
                {
                    e.x   = pos.x;
                    e.y   = pos.y;
                    e.rot = rotationSteps;
                    break;
                }
            }
            SaveBuildings();

            Cleanup(refund: false);
            RefreshOccupancy();
            return;
        }

        // ── Nhánh đặt mới (luồng cũ từ Shop) ──
        int assignedPlotId = 0;

        // HỢP ĐỒNG §3 với DEV-2: nếu ConstructionManager nhận việc thì KHÔNG
        // Instantiate prefab thật ở đây — công trình sẽ hiện sau khi xây xong.
        // Gọi qua reflection vì lúc viết code này class ConstructionManager chưa tồn tại;
        // reflection giữ cho Assembly-CSharp luôn biên dịch được dù DEV-2 xong trước hay sau.
        //
        // 📐 QUY ƯỚC TOẠ ĐỘ VỚI DEV-2 (chốt V7 — ghi giống hệt ở ConstructionManager):
        //    tham số `pos` là ĐIỂM NEO (pivot ở đáy), KHÔNG phải tâm khối ô.
        //    Lý do chọn neo: đó là toạ độ Instantiate prefab thật, cũng là toạ độ hai file
        //    save (FARM_PLACED_BUILDINGS và FARM_CONSTRUCTION_SITES) đang lưu → save cũ
        //    vẫn đọc đúng, và DEV-2 không phải trừ ngược offset trước khi dựng công trình.
        //    ConstructionManager tự gọi PlacementManager.AnchorToFootprintCenter() khi cần
        //    tâm (đặt giàn giáo, giữ chỗ ô, chạy VFX).
        bool started = TryStartConstructionDev2(currentItem, pos, rotationSteps, assignedPlotId);

        if (started)
        {
            // Giữ chỗ ô trong lúc xây để không đặt đè lên giàn giáo.
            // PHẢI dùng footprintCenter: dùng `pos` (neo ở đáy) thì vùng giữ tụt xuống
            // ~2 ô so với giàn giáo → giữ oan đất trống và bỏ trống chỗ giàn giáo đứng.
            reservedRects.Add(GetFootprintRect(footprintCenter, size));
            RebuildOccupiedCells();
            Cleanup(refund: false);
            return;
        }

        GameObject spawnedObj = Instantiate(currentItem.prefabToBuild, pos, RotationOf(rotationSteps));

        FixBuildingRenderSorting(spawnedObj);
        FixAnimalVisibility(spawnedObj);

        // Tắt bất kỳ placeholder cùng tên trong scene để tránh object thừa
        DisablePlaceholderInScene(currentItem.prefabToBuild.name, spawnedObj);

        // Khởi tạo house bubble — chỉ clone này được truyền vào RegisterHouse
        var house = spawnedObj.GetComponentInChildren<Village.HouseOrderController>(true);
        if (house != null) house.Initialize();

        // Khởi tạo sạch nếu là ô đất (tránh load dữ liệu cũ trùng plotId)
        var plot = spawnedObj.GetComponentInChildren<PlotController>(true);
        if (plot != null)
        {
            plot.InitializeAsNew();
            assignedPlotId = GetNextPlotId();
            plot.SetPlotId(assignedPlotId);
        }

        // Lưu vào PlayerPrefs kèm plotId + hướng xoay
        placedBuildings.Add(new BuildingEntry
        {
            itemId = currentItem.itemID,
            x      = pos.x,
            y      = pos.y,
            plotId = assignedPlotId,
            rot    = rotationSteps
        });
        SaveBuildings();

        knownSizes[spawnedObj] = size;

        Cleanup(refund: false);
        RefreshOccupancy();
    }

    // ── Cầu nối sang DEV-2 (V6) ───────────────────────────────────────────────

    private static bool   _dev2Probed;
    private static PropertyInfo _dev2InstanceProp;
    private static MethodInfo   _dev2TryStart;

    /// <summary>
    /// Gọi ConstructionManager.Instance.TryStartConstruction(data, pos, rotSteps, plotId).
    ///
    /// VÌ SAO REFLECTION: file ConstructionManager.cs thuộc quyền DEV-2 và chưa tồn tại
    /// lúc DEV-1 viết đoạn này. Gọi trực tiếp sẽ làm CẢ Assembly-CSharp không biên dịch
    /// được → chặn toàn đội. `#if` không dùng được vì không có symbol nào để định nghĩa.
    /// Reflection tra 1 lần rồi cache MethodInfo nên chi phí ~0 (chỉ chạy khi bấm ✓).
    /// KHI DEV-2 ĐÃ MERGE: có thể thay thân hàm này bằng lời gọi trực tiếp
    ///     ConstructionManager.Instance != null &&
    ///     ConstructionManager.Instance.TryStartConstruction(data, pos, rotSteps, plotId)
    /// mà không phải sửa nơi gọi.
    /// </summary>
    private static bool TryStartConstructionDev2(PlaceableItemData data, Vector3 pos, int rotSteps, int plotId)
    {
        if (data == null) return false;

        if (!_dev2Probed)
        {
            _dev2Probed = true;
            Type t = FindTypeByName("ConstructionManager");
            if (t != null)
            {
                _dev2InstanceProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                _dev2TryStart = t.GetMethod("TryStartConstruction",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(PlaceableItemData), typeof(Vector3), typeof(int), typeof(int) },
                    null);
            }
        }

        if (_dev2InstanceProp == null || _dev2TryStart == null) return false;

        object instance = _dev2InstanceProp.GetValue(null);
        // Unity override toán tử == cho Object bị huỷ → so sánh bằng Equals(null) không đủ.
        if (instance is UnityEngine.Object uo && uo == null) return false;
        if (instance == null) return false;

        object result = _dev2TryStart.Invoke(instance, new object[] { data, pos, rotSteps, plotId });
        return result is bool b && b;
    }

    private static Type FindTypeByName(string typeName)
    {
        Type t = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
        if (t != null) return t;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(typeName, false);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>
    /// DEV-2 GỌI khi công trình XÂY XONG và đã Instantiate prefab thật.
    /// PlacementManager sẽ: sửa sorting, bật con vật, cấp plotId, ghi save, chiếm ô.
    /// Nhờ vậy quyền sở hữu file save vẫn nằm ở DEV-1, DEV-2 không phải đụng PlayerPrefs.
    /// </summary>
    public void RegisterCompletedBuilding(PlaceableItemData data, GameObject spawnedObj, int rotationStepsUsed)
    {
        if (data == null || spawnedObj == null) return;

        // `pos` = ĐIỂM NEO của prefab (pivot ở đáy) — đúng thứ cần ghi vào save,
        // vì LoadBuildings() sẽ Instantiate lại tại chính toạ độ này.
        Vector3 pos = spawnedObj.transform.position;
        pos.z = 0f;

        FixBuildingRenderSorting(spawnedObj);
        FixAnimalVisibility(spawnedObj);
        DisablePlaceholderInScene(data.prefabToBuild != null ? data.prefabToBuild.name : spawnedObj.name, spawnedObj);

        var house = spawnedObj.GetComponentInChildren<Village.HouseOrderController>(true);
        if (house != null) house.Initialize();

        int assignedPlotId = 0;
        var plot = spawnedObj.GetComponentInChildren<PlotController>(true);
        if (plot != null)
        {
            plot.InitializeAsNew();
            assignedPlotId = GetNextPlotId();
            plot.SetPlotId(assignedPlotId);
        }

        placedBuildings.Add(new BuildingEntry
        {
            itemId = data.itemID,
            x      = pos.x,
            y      = pos.y,
            plotId = assignedPlotId,
            rot    = rotationStepsUsed & 3
        });
        SaveBuildings();

        knownSizes[spawnedObj] = SizeForSpawned(data, rotationStepsUsed, spawnedObj);

        // ⚠ Nhả chỗ phải truyền TÂM VÙNG Ô, không phải neo: reservedRects được tạo từ tâm
        // (xem ConfirmPlacement), mà với pivot đáy thì ô chứa điểm neo có thể nằm NGOÀI
        // vùng giữ → không nhả được, ô bị khoá vĩnh viễn cho tới lần Clear dữ liệu.
        ReleaseConstructionCells(AnchorToFootprintCenter(pos, data, rotationStepsUsed));
        RefreshOccupancy();
    }

    /// <summary>
    /// DEV-2 gọi khi huỷ / hoàn tiền một công trình đang xây: trả lại ô cho map.
    /// <paramref name="centerWorld"/> là TÂM VÙNG Ô (ConstructionSite.CenterWorld),
    /// KHÔNG phải điểm neo — xem quy ước toạ độ ở ConfirmPlacement().
    /// </summary>
    public void ReleaseConstructionCells(Vector3 centerWorld)
    {
        Vector2Int cell = WorldToCell(centerWorld);
        for (int i = reservedRects.Count - 1; i >= 0; i--)
        {
            if (reservedRects[i].Contains(cell)) reservedRects.RemoveAt(i);
        }
        RebuildOccupiedCells();
    }

    // ── Building Persistence ─────────────────────────────────────────────────

    private void SaveBuildings()
    {
        var save = new BuildingsSave { list = placedBuildings };
        PlayerPrefs.SetString(BuildingsSaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();

        // Công trình vừa thay đổi → kích thước nội dung map đổi theo.
        // Xoá cache hộp bao để phím F1 (xem toàn bản đồ) đo lại cho đúng.
        var camCtrl = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (camCtrl != null) camCtrl.InvalidateContentBounds();
    }

    public void LoadBuildings()
    {
        if (!PlayerPrefs.HasKey(BuildingsSaveKey)) return;
        string json = PlayerPrefs.GetString(BuildingsSaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        BuildingsSave save = JsonUtility.FromJson<BuildingsSave>(json);
        if (save?.list == null) return;

        foreach (var entry in save.list)
        {
            PlaceableItemData itemData = FindItemById(entry.itemId);
            if (itemData == null || itemData.prefabToBuild == null)
            {
                continue;
            }

            // entry.x/y là ĐIỂM NEO đã ghi lúc đặt (pivot ở đáy) → Instantiate thẳng vào
            // đây là đúng chỗ cũ. KHÔNG cộng bù pivot ở bước này; vùng ô được tính lại
            // sau đó bởi RefreshOccupancy() qua FootprintCenterOfSpawned().
            Vector3 pos = new(entry.x, entry.y, 0f);
            // rot thiếu trong save cũ → JsonUtility để 0 → không xoay. Tương thích ngược.
            int rot = entry.rot & 3;
            GameObject obj = Instantiate(itemData.prefabToBuild, pos, RotationOf(rot));

            FixBuildingRenderSorting(obj);
            FixAnimalVisibility(obj);

            // Tắt placeholder cùng tên còn sót trong scene
            DisablePlaceholderInScene(itemData.prefabToBuild.name, obj);

            var house = obj.GetComponentInChildren<Village.HouseOrderController>(true);
            if (house != null) house.Initialize();

            // Restore plotId khi load — plot đã có SaveKey riêng nên chỉ cần gán lại ID
            var plot = obj.GetComponentInChildren<PlotController>(true);
            if (plot != null)
            {
                if (entry.plotId > 0)
                {
                    plot.SetPlotId(entry.plotId);
                }
                else
                {
                    // Save cũ chưa có plotId → cấp ID mới và cập nhật entry để save lại
                    int newId = GetNextPlotId();
                    plot.SetPlotId(newId);
                    entry.plotId = newId;
                }
            }

            knownSizes[obj] = SizeForSpawned(itemData, rot, obj);
            placedBuildings.Add(entry);
        }

        // Nếu có entry nào được cấp plotId mới (save cũ không có), ghi lại ngay
        SaveBuildings();
    }

    // Tìm tất cả object trong scene có tên trùng prefabName (không có "(Clone)")
    // và SetActive(false) để tránh object thừa song song với clone vừa tạo.
    private void DisablePlaceholderInScene(string prefabName, GameObject skipObj)
    {
        var allHOCs = FindObjectsByType<Village.HouseOrderController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var hoc in allHOCs)
        {
            if (hoc.gameObject == skipObj) continue;
            if (hoc.gameObject.name == prefabName)          // exact name = chưa có "(Clone)"
            {
                hoc.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Xóa toàn bộ dữ liệu nhà/công trình đã đặt khỏi PlayerPrefs.</summary>
    public void ClearBuildingData()
    {
        PlayerPrefs.DeleteKey(BuildingsSaveKey);
        PlayerPrefs.Save();
        placedBuildings.Clear();
        knownSizes.Clear();
        reservedRects.Clear();
        RefreshOccupancy();
    }

    private PlaceableItemData FindItemById(string itemId)
    {
        if (ShopManager.Instance == null) return null;

        foreach (var item in ShopManager.Instance.buildingList)
            if (item is PlaceableItemData p && p.itemID == itemId) return p;

        foreach (var item in ShopManager.Instance.decorList)
            if (item is PlaceableItemData p && p.itemID == itemId) return p;

        return null;
    }

    /// <summary>
    /// Tra data theo TÊN prefab (chấp nhận cả hậu tố "(Clone)").
    /// Dùng để biết kích thước ô của một công trình có sẵn trong scene mà ta
    /// không tự Instantiate — quy ước tên này đã được DisablePlaceholderInScene dùng sẵn.
    /// </summary>
    private PlaceableItemData FindItemByPrefabName(string objectName)
    {
        if (ShopManager.Instance == null || string.IsNullOrEmpty(objectName)) return null;

        string clean = objectName.Replace("(Clone)", "").Trim();

        PlaceableItemData Match(List<BaseItemData> list)
        {
            if (list == null) return null;
            foreach (var item in list)
                if (item is PlaceableItemData p && p.prefabToBuild != null && p.prefabToBuild.name == clean)
                    return p;
            return null;
        }

        return Match(ShopManager.Instance.buildingList) ?? Match(ShopManager.Instance.decorList);
    }

    /// <summary>Gắn vào Btn_Cancel (và có thể gọi từ ngoài, vd: phím Escape). Hoàn tiền + xóa Ghost.</summary>
    public void CancelPlacement() => Cleanup(refund: true);

    /// <summary>Xóa HẲN vật đang sửa khỏi map + khỏi save (không spawn lại lần Play sau).
    /// Gắn vào Btn_Delete trên Ghost, hoặc bấm phím Delete/Backspace khi đang edit 1 vật.</summary>
    public void DeleteEditingBuilding()
    {
        if (currentlyEditingBuilding == null)
        {
            Debug.LogWarning("[Placement] DeleteEditingBuilding: không có vật nào đang sửa — chỉ xóa được khi đang Edit 1 công trình.");
            return;
        }

        // 1) Xóa entry khớp vị trí gốc khỏi save → lần Play sau KHÔNG còn spawn lại
        int removed = placedBuildings.RemoveAll(e =>
            Mathf.Approximately(e.x, originalEditPosition.x) &&
            Mathf.Approximately(e.y, originalEditPosition.y));
        SaveBuildings();

        // 2) Hủy object gốc khỏi map; clear ref TRƯỚC để Cleanup không "hồi sinh" nó
        var go = currentlyEditingBuilding.gameObject;
        currentlyEditingBuilding = null;
        if (go != null)
        {
            knownSizes.Remove(go);
            occupancyByObject.Remove(go);
            Destroy(go);
        }

        // 3) Dọn Ghost + reset state (không refund, không restore)
        Cleanup(refund: false);
        RefreshOccupancy();
        Debug.Log($"[Placement] Đã XÓA vật thể khỏi map + {removed} entry khỏi save.");
    }

    // ── Nội bộ ──────────────────────────────────────────────────────────────

    /// <summary>Dọn dẹp sau Confirm hoặc Cancel. refund = true → hoàn tiền / trả building về cũ.</summary>
    private void Cleanup(bool refund)
    {
        if (refund)
        {
            if (currentlyEditingBuilding != null)
            {
                // Cancel Edit Mode: trả công trình về vị trí + hướng gốc và hiện lại
                currentlyEditingBuilding.transform.position = originalEditPosition;
                currentlyEditingBuilding.transform.rotation = RotationOf(originalEditRotationSteps);
                currentlyEditingBuilding.gameObject.SetActive(true);
            }
            else if (currentItem != null)
            {
                // Cancel đặt mới: hoàn tiền
                if (currentItem.diamondPrice > 0)
                    FarmEconomyManager.Instance.AddGems(currentItem.diamondPrice);
                else
                    FarmEconomyManager.Instance.AddGold(currentItem.goldPrice);

            }
        }

        // Safety net: nếu building vẫn đang bị ẩn (chưa được xử lý ở trên), phục hồi ngay
        // — bảo vệ khỏi mọi path tắt bất thường (force-quit, exception, v.v.)
        if (currentlyEditingBuilding != null && !currentlyEditingBuilding.gameObject.activeSelf)
        {
            currentlyEditingBuilding.transform.position = originalEditPosition;
            currentlyEditingBuilding.transform.rotation = RotationOf(originalEditRotationSteps);
            currentlyEditingBuilding.gameObject.SetActive(true);
        }

        if (currentGhost != null) Destroy(currentGhost);

        pickupRoutine            = null;
        currentGhost             = null;
        houseRenderer            = null;
        btnConfirm               = null;
        confirmRect              = null;
        cancelRect               = null;
        rotateRect               = null;
        currentItem              = null;
        editingItemData          = null;
        currentlyEditingBuilding = null;
        footprintTransform       = null;
        ghostVisualCloneRoot     = null;
        ghostVisual              = null;
        rotationSteps            = 0;
        cloneRotationCompensation = Vector3.zero;
        isPlacing                = false;
        IsPlacingNewObject       = false;  // Mở khóa CameraController

        RefreshOccupancy();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. BẢNG Ô ĐÃ CHIẾM — thay thế hoàn toàn Physics2D.OverlapBox (V3)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dựng lại bảng ô bị chiếm từ SCENE THẬT.
    /// Nguồn dữ liệu, theo thứ tự ưu tiên:
    ///   1. knownSizes — object do chính PlacementManager Instantiate (kích thước lấy từ data → chuẩn nhất)
    ///   2. EditableBuilding trong scene — công trình đặt sẵn tay; nếu tra được data theo tên prefab
    ///      thì dùng gridSize của data, không thì đo bounds renderer rồi Ceil lên ô.
    ///   3. reservedRects — ô do ConstructionManager giữ trong lúc xây.
    /// Công trình ĐANG SỬA bị loại ra, nếu không nó sẽ tự chặn chính nó.
    /// </summary>
    public void RefreshOccupancy()
    {
        occupancyByObject.Clear();

        // Dọn key đã bị Destroy khỏi knownSizes
        if (knownSizes.Count > 0)
        {
            var dead = new List<GameObject>();
            foreach (var kv in knownSizes)
                if (kv.Key == null) dead.Add(kv.Key);   // Unity "fake null" = object đã Destroy
            foreach (var d in dead)
                if (!ReferenceEquals(d, null)) knownSizes.Remove(d);
        }

        GameObject editing = currentlyEditingBuilding != null ? currentlyEditingBuilding.gameObject : null;

        foreach (var eb in FindObjectsByType<EditableBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (eb == null) continue;
            GameObject go = eb.gameObject;
            if (go == editing) continue;
            occupancyByObject[go] = ComputeRectFor(go);
        }

        foreach (var kv in knownSizes)
        {
            GameObject go = kv.Key;
            if (go == null || go == editing) continue;
            if (!go.activeInHierarchy) continue;
            if (occupancyByObject.ContainsKey(go)) continue;
            // FootprintCenterOfSpawned chứ không phải transform.position: pivot ở đáy.
            occupancyByObject[go] = GetFootprintRect(FootprintCenterOfSpawned(go), kv.Value);
        }

        PurgeCoveredReservations();
        RebuildOccupiedCells();
    }

    /// <summary>Bỏ chỗ giữ khi công trình thật đã mọc lên đúng vùng đó (xây xong).</summary>
    private void PurgeCoveredReservations()
    {
        if (reservedRects.Count == 0) return;

        for (int i = reservedRects.Count - 1; i >= 0; i--)
        {
            RectInt r = reservedRects[i];
            foreach (var kv in occupancyByObject)
            {
                // Phải TRÙNG KHỚP HOÀN TOÀN, không phải chỉ chồng lấn.
                // Dùng RectsOverlap thì hộp bao phình của một công trình bên cạnh
                // cũng đủ xoá mất chỗ giữ của công trường đang xây → đặt đè lên được.
                if (kv.Value.Equals(r)) { reservedRects.RemoveAt(i); break; }
            }
        }
    }

    private void RebuildOccupiedCells()
    {
        occupiedCells.Clear();
        foreach (var kv in occupancyByObject) AddRectToSet(kv.Value);
        foreach (var r in reservedRects)      AddRectToSet(r);
    }

    private void AddRectToSet(RectInt r)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
                occupiedCells.Add(new Vector2Int(x, y));
    }

    /// <summary>
    /// Vùng ô của một object đang đứng trên map.
    /// Số Ô lấy từ data/knownSizes (chuẩn), còn TÂM luôn lấy từ hộp bao thật
    /// (FootprintCenterOfSpawned) — transform.position là chân nhà, dùng nó thì vùng ô
    /// tụt xuống dưới thân nhà đúng bằng nửa chiều cao sprite.
    /// </summary>
    private RectInt ComputeRectFor(GameObject go)
    {
        Vector3 center = FootprintCenterOfSpawned(go);

        if (knownSizes.TryGetValue(go, out Vector2Int s))
            return GetFootprintRect(center, s);

        // Chưa biết → thử tra data theo tên prefab (chuẩn hơn đo bounds)
        PlaceableItemData data = FindItemByPrefabName(go.name);
        if (data != null)
        {
            Vector2Int size = SizeForSpawned(data, RotationStepsOf(go.transform), go);
            knownSizes[go] = size;    // nhớ lại cho lần sau
            return GetFootprintRect(center, size);
        }

        // Nhánh cuối đã đo thẳng hộp bao world → KHÔNG dính pivot, giữ nguyên.
        return RectFromWorldBounds(MeasureWorldBounds(go));
    }

    /// <summary>
    /// Suy vùng ô từ hộp bao world. Dùng epsilon 1 % ô để sprite chạm sát mép ô
    /// không "ăn lẹm" sang ô kế bên (viền trong suốt của sprite hay gây chuyện này).
    /// </summary>
    public static RectInt RectFromWorldBounds(Bounds b)
    {
        if (b.size.x <= 0.001f || b.size.y <= 0.001f)
            return new RectInt(0, 0, 0, 0);

        const float eps = 0.01f;
        int xMin = Mathf.FloorToInt((b.min.x - GridOrigin.x) / CELL + eps);
        int yMin = Mathf.FloorToInt((b.min.y - GridOrigin.y) / CELL + eps);
        int xMax = Mathf.CeilToInt ((b.max.x - GridOrigin.x) / CELL - eps);
        int yMax = Mathf.CeilToInt ((b.max.y - GridOrigin.y) / CELL - eps);
        return new RectInt(xMin, yMin, Mathf.Max(1, xMax - xMin), Mathf.Max(1, yMax - yMin));
    }

    private static Bounds MeasureWorldBounds(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool found = false;
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!IsValidSourceVisualRenderer(sr)) continue;
            if (!found) { b = sr.bounds; found = true; }
            else b.Encapsulate(sr.bounds);
        }
        return found ? b : new Bounds(go.transform.position, Vector3.zero);
    }

    private static bool RectsOverlap(RectInt a, RectInt b)
        => a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;

    /// <summary>Vùng ô này còn trống không? (bảng ô đã loại sẵn công trình đang sửa)</summary>
    public bool IsAreaFree(RectInt rect)
    {
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                if (occupiedCells.Contains(new Vector2Int(x, y))) return false;
        return true;
    }

    /// <summary>Như trên nhưng bỏ qua vùng ô của một object cụ thể (dùng cho ObjectDragHandler).</summary>
    public bool IsAreaFree(RectInt rect, GameObject ignore)
    {
        if (ignore == null) return IsAreaFree(rect);

        RectInt skip = occupancyByObject.TryGetValue(ignore, out RectInt r) ? r : new RectInt(0, 0, 0, 0);
        for (int x = rect.xMin; x < rect.xMax; x++)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                var c = new Vector2Int(x, y);
                if (!occupiedCells.Contains(c)) continue;
                if (skip.width > 0 && skip.Contains(c)) continue; // ô của chính nó → bỏ qua
                return false;
            }
        }
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. BIÊN BẢN ĐỒ (V4)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Xoá cache biên bản đồ (gọi nếu bạn mở rộng tilemap lúc chạy).</summary>
    public void InvalidateMapBounds() => _mapBoundsReady = false;

    /// <summary>
    /// Biên bản đồ THẬT.
    ///
    /// VÌ SAO LẤY TỪ TILEMAP CHỨ KHÔNG PHẢI CameraController.bounds:
    ///   • `CameraController.bounds` là vùng KẸP VỊ TRÍ CAMERA, không phải mép đất.
    ///     Chính comment trong CameraController.FitMapToView() đã cảnh báo điều này.
    ///   • `MapBoundary.LateUpdate()` còn tự NỚI bounds thêm 1000 unit mỗi khi camera
    ///     lại gần mép → bounds lớn dần vô hạn, dùng làm biên xây dựng thì vô nghĩa.
    ///   • Tilemap nền là vùng đất người chơi nhìn thấy và đứng lên được → đúng
    ///     ngữ nghĩa "ra ngoài bản đồ" của Township.
    /// Có `mapBoundsOverride` cho trường hợp designer muốn khoá cứng khu xây dựng.
    /// Cuối cùng luôn Encapsulate mọi công trình đang tồn tại: chống trường hợp
    /// tilemap nhỏ hơn khu đã xây làm cả map hoá "ngoài biên" và không đặt được gì.
    /// </summary>
    public bool TryGetMapBounds(out Bounds bounds)
    {
        if (_mapBoundsReady) { bounds = _mapBounds; return true; }

        bool found = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);

        if (mapBoundsOverride != Vector4.zero)
        {
            b = new Bounds(
                new Vector3((mapBoundsOverride.x + mapBoundsOverride.y) * 0.5f,
                            (mapBoundsOverride.z + mapBoundsOverride.w) * 0.5f, 0f),
                new Vector3(Mathf.Abs(mapBoundsOverride.y - mapBoundsOverride.x),
                            Mathf.Abs(mapBoundsOverride.w - mapBoundsOverride.z), 1f));
            found = true;
        }
        else
        {
            foreach (var tr in FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (tr == null || !tr.enabled) continue;
                Bounds tb = tr.bounds;
                if (tb.size.x <= 0.01f || tb.size.y <= 0.01f) continue;
                if (!found) { b = tb; found = true; }
                else b.Encapsulate(tb);
            }
        }

        if (!found)
        {
            // Fallback cuối: bounds của camera. Rộng quá nhưng còn hơn chặn nhầm.
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
            if (cam != null)
            {
                Vector4 v = cam.bounds;
                b = new Bounds(new Vector3((v.x + v.y) * 0.5f, (v.z + v.w) * 0.5f, 0f),
                               new Vector3(Mathf.Abs(v.y - v.x), Mathf.Abs(v.w - v.z), 1f));
                found = true;
            }
        }

        if (found)
        {
            // Không bao giờ để công trình đã tồn tại nằm ngoài biên → khoá cứng game.
            foreach (var kv in occupancyByObject)
            {
                RectInt r = kv.Value;
                if (r.width <= 0 || r.height <= 0) continue;
                b.Encapsulate(CellCornerToWorld(r.xMin, r.yMin));
                b.Encapsulate(CellCornerToWorld(r.xMax, r.yMax));
            }
            b.Expand(new Vector3(mapBoundsPadding * 2f, mapBoundsPadding * 2f, 0f));

            _mapBounds      = b;
            _mapBoundsReady = true;

            if (verboseGridLog)
                Debug.Log($"[Placement] Biên bản đồ = ({b.min.x:F0},{b.min.y:F0}) → ({b.max.x:F0},{b.max.y:F0})");
        }

        bounds = b;
        return found;
    }

    /// <summary>Vùng ô có nằm TRỌN trong biên bản đồ không?</summary>
    public bool IsRectInsideMap(RectInt rect)
    {
        if (!enforceMapBounds) return true;
        if (!TryGetMapBounds(out Bounds b)) return true;   // không đo được → không chặn oan

        Vector3 min = CellCornerToWorld(rect.xMin, rect.yMin);
        Vector3 max = CellCornerToWorld(rect.xMax, rect.yMax);
        const float eps = 0.5f;
        return min.x >= b.min.x - eps && max.x <= b.max.x + eps &&
               min.y >= b.min.y - eps && max.y <= b.max.y + eps;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Kích thước ô của vật đang bưng, đã tính cả xoay.</summary>
    private Vector2Int CurrentGridSize()
    {
        PlaceableItemData data = currentlyEditingBuilding != null ? editingItemData : currentItem;

        // CÓ data → luôn đi qua GridSizeOf.
        // GridSizeOf vừa tin data khi đã điền, vừa tự đo PREFAB khi chưa điền.
        // Bắt buộc dùng chung đường này với ConstructionManager: trước đây nhánh
        // "chưa điền" ở đây đo bounds của GHOST CLONE (nhân 1.03), còn DEV-2 đo PREFAB
        // → hai con số khác nhau → vùng giữ chỗ lệch với giàn giáo → sinh ra "đè" và "lệch".
        if (data != null)
        {
            WarnGridSizeMissing(data);
            return GridSizeOf(data, rotationSteps);
        }

        // KHÔNG có data (vật do scene tự đặt, không tra ngược được) → đành đo bounds
        // của ghost clone. fallbackGridSize đo LÚC CHƯA XOAY → phải hoán đổi theo bước xoay.
        return ((rotationSteps & 1) == 1)
            ? new Vector2Int(fallbackGridSize.y, fallbackGridSize.x)
            : fallbackGridSize;
    }

    /// <summary>
    /// Cỡ ô cho một object ĐÃ nằm trên map. Ưu tiên data; nếu data còn 1×1 mặc định
    /// thì đo bounds thật của object (bounds này ĐÃ xoay sẵn theo transform nên
    /// KHÔNG hoán đổi thêm lần nữa).
    ///
    /// ĐÃ XÁC MINH KHÔNG DÍNH BUG PIVOT: hàm này chỉ trả KÍCH THƯỚC (số ô rộng × cao),
    /// và cả hai nhánh đều độc lập với vị trí — data.GetGridSize không đọc toạ độ,
    /// còn MeasuredCellsOf lấy b.size của hộp bao world. Pivot chỉ ảnh hưởng tới TÂM,
    /// và tâm được xử lý riêng ở ComputeRectFor/FootprintCenterOfSpawned.
    /// </summary>
    private static Vector2Int SizeForSpawned(PlaceableItemData data, int rotSteps, GameObject go)
    {
        if (data != null && (data.gridSize.x > 1 || data.gridSize.y > 1))
            return data.GetGridSize(rotSteps);

        if (data != null) WarnGridSizeMissing(data);
        return MeasuredCellsOf(go);
    }

    private static Vector2Int MeasuredCellsOf(GameObject go)
    {
        RectInt r = RectFromWorldBounds(MeasureWorldBounds(go));
        return new Vector2Int(Mathf.Max(1, r.width), Mathf.Max(1, r.height));
    }

    // Cảnh báo MỘT LẦN cho mỗi asset — nếu không sẽ spam log mỗi frame.
    private static readonly HashSet<string> _warnedMissingGridSize = new();

    private static void WarnGridSizeMissing(PlaceableItemData data)
    {
        if (data == null) return;
        string key = data.name;
        if (!_warnedMissingGridSize.Add(key)) return;

        Debug.LogWarning(
            $"[PlacementManager] '{key}' còn gridSize = 1×1 (mặc định) — đang tạm đo bounds. " +
            "Chạy menu Tools/Farm/Suy Kích Thước Ô Công Trình để điền số ô chuẩn.", data);
    }

    /// <summary>Bind V / X / ↻ (và Delete khi đang sửa). Một chỗ duy nhất cho cả 2 luồng.</summary>
    private void BindGhostButtons(bool bindDelete)
    {
        btnConfirm  = null;
        confirmRect = null;
        cancelRect  = null;
        rotateRect  = null;

        foreach (Button btn in currentGhost.GetComponentsInChildren<Button>(true))
        {
            switch (btn.name)
            {
                case "Btn_Confirm":
                    btnConfirm  = btn;
                    confirmRect = btn.GetComponent<RectTransform>();
                    btn.onClick.AddListener(ConfirmPlacement);
                    break;
                case "Btn_Cancel":
                    cancelRect = btn.GetComponent<RectTransform>();
                    btn.onClick.AddListener(CancelPlacement);
                    break;
                case "Btn_Rotate":                       // V5 — trước đây KHÔNG bind, nút chết
                    rotateRect = btn.GetComponent<RectTransform>();
                    btn.onClick.AddListener(RotateGhost);
                    break;
                case "Btn_Delete":                       // tuỳ chọn: prefab hiện chưa có nút này
                    if (bindDelete) btn.onClick.AddListener(DeleteEditingBuilding);
                    break;
            }
        }

        if (btnConfirm == null || cancelRect == null)
            Debug.LogWarning("[PlacementManager] Ghost thiếu Btn_Confirm / Btn_Cancel — kiểm tra prefab Placement_Ghost.");
        if (rotateRect == null)
            Debug.LogWarning("[PlacementManager] Ghost thiếu Btn_Rotate — không xoay được bằng nút (phím R vẫn chạy).");
    }

    /// World Space Canvas cần truyền Camera.main để tính đúng tọa độ screen → rect.
    private static bool IsMouseOverRect(RectTransform rt)
    {
        if (rt == null || !rt.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, Camera.main);
    }

    private static string ResolveSortingLayerName(string preferred, string fallback)
    {
        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == preferred)
                return preferred;

        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == fallback)
                return fallback;

        return "Default";
    }

    /// <summary>
    /// Tìm plotId lớn nhất đang tồn tại trong scene rồi trả về maxId + 1.
    /// Đảm bảo mỗi ô đất được đặt mới có ID duy nhất, không trùng với ô scene hoặc ô đã load.
    /// </summary>
    private int GetNextPlotId()
    {
        int maxId = 0;
        foreach (var p in FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.PlotId > maxId)
                maxId = p.PlotId;
        }
        return maxId + 1;
    }

    /// <summary>
    /// Vị trí chuột trong world-space, đã snap về đúng mốc lưới cho công trình N×M ô.
    /// mapGrid (UnityEngine.Grid) đã bị BỎ: nó null trong scene nên nhánh đó chưa từng
    /// chạy, và để hai nguồn cell size song song chính là gốc của lỗi lệch lưới.
    /// Giờ chỉ còn MỘT hằng số CELL.
    /// </summary>
    private Vector3 GetSnappedMousePos(Vector2Int size)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;
        Vector3 worldPos = cam.ScreenToWorldPoint(mouse);
        worldPos.z = 0f;

        return SnapCenter(worldPos, size);
    }

    private void SetupGhostVisualController(bool showLiftArrow)
    {
        if (currentGhost == null) return;

        SortingGroup sortingGroup = currentGhost.GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = currentGhost.AddComponent<SortingGroup>();
        sortingGroup.sortingLayerName = BuildingSortingLayerName;
        sortingGroup.sortingOrder = PlacementGhostVisualController.BaseOrder;

        ghostVisual = currentGhost.GetComponent<PlacementGhostVisualController>();
        if (ghostVisual == null)
            ghostVisual = currentGhost.AddComponent<PlacementGhostVisualController>();

        ghostVisual.SetTileSprite(footprintSprite);
        ghostVisual.EnsureBuilt();
        ghostVisual.ShowLiftArrow(showLiftArrow);
        ghostVisual.PlaySpawnPop(showLiftArrow);
    }

    private void ConfigureGhostCanvas()
    {
        if (currentGhost == null) return;

        Canvas ghostCanvas = currentGhost.GetComponentInChildren<Canvas>(true);
        if (ghostCanvas == null) return;

        ghostCanvas.worldCamera = Camera.main;
        ghostCanvas.overrideSorting = true;
        ghostCanvas.sortingLayerName = BuildingSortingLayerName;
        ghostCanvas.sortingOrder = PlacementGhostVisualController.BuildingOrder + 140;
    }

    private void BuildGhostVisualFromSource(Transform sourceRoot, bool relaxed = false)
    {
        if (currentGhost == null || sourceRoot == null) return;

        if (ghostVisualCloneRoot != null)
            Destroy(ghostVisualCloneRoot.gameObject);

        if (houseRenderer != null)
            houseRenderer.enabled = false;

        GameObject cloneRoot = new GameObject("Building_Visual_Clone");
        cloneRoot.layer = currentGhost.layer;
        ghostVisualCloneRoot = cloneRoot.transform;
        ghostVisualCloneRoot.SetParent(currentGhost.transform, false);
        ghostVisualCloneRoot.localPosition = Vector3.zero;
        ghostVisualCloneRoot.localRotation = Quaternion.identity;
        // FIX: trước đây để cứng 1.03 → BỎ QUA scale gốc của prefab. Nếu prefab thật được
        // thu nhỏ cho vừa map (root scale < 1) thì ghost phình to gấp nhiều lần, che cả map.
        // Áp scale gốc của nguồn (chia cho scale ghost để không nhân kép) → ghost = đúng cỡ vật thật.
        ghostVisualCloneRoot.localScale =
            DivideVector(sourceRoot.lossyScale, currentGhost.transform.lossyScale) * 1.03f;

        SpriteRenderer[] sourceRenderers = sourceRoot.GetComponentsInChildren<SpriteRenderer>(true);
        int visualIndex = 0;
        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            SpriteRenderer source = sourceRenderers[i];
            // relaxed = clone mọi sprite (chỉ bỏ sprite null) → dùng làm fallback khi clone thường rỗng.
            bool skip = relaxed ? (source == null || source.sprite == null)
                                : !IsValidSourceVisualRenderer(source);
            if (skip) continue;

            GameObject visualGo = new GameObject($"Sprite_{visualIndex:00}_{source.gameObject.name}");
            visualGo.layer = currentGhost.layer;
            Transform visual = visualGo.transform;
            visual.SetParent(ghostVisualCloneRoot, false);
            visual.localPosition = sourceRoot.InverseTransformPoint(source.transform.position);
            visual.localRotation = Quaternion.Inverse(sourceRoot.rotation) * source.transform.rotation;
            visual.localScale = DivideVector(source.transform.lossyScale, sourceRoot.lossyScale);

            SpriteRenderer target = visualGo.AddComponent<SpriteRenderer>();
            target.sprite = source.sprite;
            target.color = source.color;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
            target.size = source.size;
            target.maskInteraction = source.maskInteraction;
            target.sortingLayerName = BuildingSortingLayerName;
            target.sortingOrder = PlacementGhostVisualController.BuildingOrder + visualIndex;
            visualIndex++;
        }
    }

    private static Vector3 DivideVector(Vector3 value, Vector3 divisor)
    {
        return new Vector3(
            Mathf.Abs(divisor.x) > 0.0001f ? value.x / divisor.x : value.x,
            Mathf.Abs(divisor.y) > 0.0001f ? value.y / divisor.y : value.y,
            Mathf.Abs(divisor.z) > 0.0001f ? value.z / divisor.z : value.z);
    }

    private static Bounds CalculateSourceVisualBounds(Transform sourceRoot)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        foreach (SpriteRenderer sr in sourceRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!IsValidSourceVisualRenderer(sr))
                continue;

            Bounds localBounds = sr.bounds;
            if (!hasBounds)
            {
                bounds = localBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localBounds);
            }
        }

        return bounds;
    }

    private static bool IsValidSourceVisualRenderer(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null)
            return false;

        string n = sr.gameObject.name;
        if (n == "Selection_Ring" ||
            n == "Grid_Footprint" ||
            n.Contains("Footprint") ||
            n.Contains("Shadow") ||
            n.StartsWith("Marker_") ||
            n.StartsWith("Arrow_") ||
            n.StartsWith("Placement_") ||
            n == "Designed_Placement_Frame" ||
            n == "Lift_Arrow_Effect")
            return false;

        return true;
    }

    private IEnumerator AnimateGhostActionBar()
    {
        if (currentGhost == null) yield break;

        Transform row = FindDeepChild(currentGhost.transform, "Button_Row");
        if (row == null)
            yield break;

        StyleGhostActionBar(row);

        Vector3 finalScale = row.localScale;
        row.localScale = finalScale * 0.45f;

        float elapsed = 0f;
        const float popDuration = 0.18f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float overshoot = BackOut(t);
            row.localScale = Vector3.LerpUnclamped(finalScale * 0.45f, finalScale * 1.08f, overshoot);
            yield return null;
        }

        elapsed = 0f;
        const float settleDuration = 0.08f;
        Vector3 start = row.localScale;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            row.localScale = Vector3.LerpUnclamped(start, finalScale, t);
            yield return null;
        }

        row.localScale = finalScale;
    }

    private static float BackOut(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static void StyleGhostActionBar(Transform row)
    {
        if (row == null) return;

        RectTransform rect = row as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(430f, 126f);
        }

        Image bg = row.GetComponent<Image>();
        if (bg == null)
            bg = row.gameObject.AddComponent<Image>();

        bg.sprite = GetGhostActionBarSprite();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.78f, 0.92f, 0.72f, 0.68f);
        bg.raycastTarget = false;

        Shadow shadow = row.GetComponent<Shadow>();
        if (shadow == null)
            shadow = row.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        shadow.effectDistance = new Vector2(0f, -5f);
    }

    private static Sprite GetGhostActionBarSprite()
    {
        if (ghostActionBarSprite != null)
            return ghostActionBarSprite;

        const int width = 128;
        const int height = 48;
        const int radius = 21;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "Placement_ActionBar_Rounded_BG";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside =
                    x >= radius && x < width - radius ||
                    IsInsideCorner(x, y, radius, radius, radius) ||
                    IsInsideCorner(x, y, width - radius - 1, radius, radius) ||
                    IsInsideCorner(x, y, radius, height - radius - 1, radius) ||
                    IsInsideCorner(x, y, width - radius - 1, height - radius - 1, radius);

                bool middleY = y >= radius && y < height - radius;
                if (middleY)
                    inside = true;

                pixels[y * width + x] = inside ? fill : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        ghostActionBarSprite = Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        ghostActionBarSprite.name = "Placement_ActionBar_Rounded_BG";
        return ghostActionBarSprite;
    }

    private static bool IsInsideCorner(int x, int y, int cx, int cy, int radius)
    {
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static SpriteRenderer FindBuildingVisualRenderer(GameObject ghost)
    {
        if (ghost == null) return null;

        Transform visual = ghost.transform.Find("Building_Visual");
        if (visual != null && visual.TryGetComponent(out SpriteRenderer directRenderer))
            return directRenderer;

        foreach (SpriteRenderer sr in ghost.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;

            string n = sr.gameObject.name;
            if (n == "Grid_Footprint" ||
                n.StartsWith("Corner_") ||
                n.StartsWith("Edge_") ||
                n.StartsWith("Marker_") ||
                n.StartsWith("Tile_") ||
                n.StartsWith("Soft_") ||
                n.StartsWith("Arrow_") ||
                n.StartsWith("Placement_") ||
                n == "Designed_Placement_Frame" ||
                n == "Lift_Arrow_Effect")
                continue;

            return sr;
        }

        return null;
    }
}
