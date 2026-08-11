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
/// HAI LOẠI TOẠ ĐỘ — ĐỌC TRƯỚC KHI SỬA BẤT KỲ DÒNG NÀO CÓ RectFromAnchor (V8)
/// • NEO   (anchor) = transform.position của Ghost / prefab. Art của dự án đặt pivot
///                    ở ĐÁY sprite nên đây là CHÂN công trình. Dùng cho: Instantiate,
///                    SnapAnchor, ghi save, so khớp entry save.
/// • TÂM Ô (center) = tâm khối N×M ô. Dùng cho: thảm xanh, giàn giáo, VFX, reservedRects.
/// • Đổi qua lại: AnchorToFootprintCenter() / FootprintCenterToAnchor()
///                — từ V8 chỉ còn phụ thuộc CHIỀU SÂU Ô (M), KHÔNG còn pivot.
///
/// 🔴 V8 — NEO VÀO MÉP DƯỚI VÙNG Ô (thay cho "tâm ô − pivotOffset" của V7)
///     anchor.y = rect.yMin · CELL       (mép dưới vùng ô)
///     anchor.x = tâm ngang vùng ô       ( (rect.xMin + N/2) · CELL )
///
/// VÌ SAO ĐỔI — HAI NGUYÊN NHÂN CỘNG DỒN, cả hai đều bị V8 xoá sổ:
///  (1) V7 snap TÂM vùng ô. Mốc tâm của khối sâu M ô là (oy + M/2)·CELL, nên nhà sâu 4 ô
///      snap vào bội số CELL còn nhà sâu 5 ô snap vào GIỮA ô. Chân Home1/Home5 (4 ô) rơi
///      vào 700, chân Home3 (5 ô) rơi vào 750 → lệch NỬA Ô = 50 unit. Đúng ảnh Edric gửi:
///      mấy nhà mái nâu thẳng hàng, nhà mái xanh nhô ra.
///  (2) Phép "bù pivot" của V7 bị LỆCH ĐƠN VỊ 100 lần (xem TryMeasurePrefabVisualBounds)
///      nên gần như không làm gì: Ghost thấy vùng ô ở một chỗ, RefreshOccupancy đo bounds
///      thật lại ra chỗ khác → vừa chặn oan vừa cho đặt đè.
/// V8: chân mọi công trình = bội số nguyên của CELL, KHÔNG phụ thuộc pivot, KHÔNG phụ
/// thuộc M chẵn hay lẻ → thẳng hàng tuyệt đối. Mắt người so CHÂN nhà, giờ hệ thống cũng
/// snap CHÂN nhà. Sprite vươn cao hơn vùng ô là BÌNH THƯỜNG (mái nhô ra) — Township y vậy.
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
    /// 🔴 V8 — SNAP ĐIỂM NEO (mép dưới + giữa ngang vùng ô). ĐÂY LÀ ĐƯỜNG CHÍNH.
    ///
    /// CÔNG THỨC:
    ///     ox = Floor( (world.x − ORIGIN.x)/CELL − N*0.5 + 0.5 )   // ô trái nhất (căn giữa ngang)
    ///     oy = Floor( (world.y − ORIGIN.y)/CELL       + 0.5 )     // hàng ô chứa CHÂN công trình
    ///     anchor = ORIGIN + ( (ox + N*0.5)*CELL , oy*CELL )
    ///
    /// VÌ SAO trục Y KHÔNG có "− M*0.5": chân công trình phải nằm ĐÚNG trên một đường kẻ
    /// lưới, còn vùng ô thì mọc LÊN từ chân (M ô phía trên). Nhờ vậy anchor.y luôn là bội
    /// số nguyên của CELL → mọi công trình, dù pivot lệch bao nhiêu, dù cao 4 ô hay 5 ô,
    /// đều có chân trên cùng một lưới. Đây chính là cách sửa lỗi "méo méo không đều".
    ///
    /// BẤT BIẾN QUAN TRỌNG — HÀM NÀY LÀ IDEMPOTENT:
    ///   SnapAnchor(SnapAnchor(p)) == SnapAnchor(p).
    ///   Chứng minh: anchor.x/CELL = ox + N/2 → Floor(ox + 0.5) = ox (ox nguyên);
    ///               anchor.y/CELL = oy      → Floor(oy + 0.5) = oy.
    ///   Nhờ bất biến này, kéo một công trình ra rồi thả lại KHÔNG làm nó dịch nửa ô,
    ///   và DEV-2 có thể snap lại lần hai mà không sợ lệch (lỗi cũ ở ConstructionManager).
    ///
    /// Dùng Floor(v + 0.5) thay Mathf.Round vì Round của Unity làm tròn về số CHẴN ở đúng
    /// mốc .5 → nhảy ô không đều khi kéo chậm.
    /// </summary>
    public static Vector3 SnapAnchor(Vector3 world, Vector2Int size)
    {
        int n  = Mathf.Max(1, size.x);
        int ox = Mathf.FloorToInt((world.x - GridOrigin.x) / CELL - n * 0.5f + 0.5f);
        int oy = Mathf.FloorToInt((world.y - GridOrigin.y) / CELL + 0.5f);
        return new Vector3(
            GridOrigin.x + (ox + n * 0.5f) * CELL,
            GridOrigin.y + oy * CELL,
            0f);
    }

    /// <summary>
    /// 🔴 V8 — Vùng ô mà công trình N×M chiếm khi ĐIỂM NEO (chân) nằm ở anchorWorld.
    /// Vùng ô mọc LÊN từ chân: rect = (ox, oy, N, M).
    ///
    /// Đây là hàm thay thế GetFootprintRect trong toàn bộ luồng đặt. Không còn đường nào
    /// cần cộng bù pivot nữa — pivot chỉ còn dùng cho phép chuyển đổi save cũ (v0 → v1).
    /// </summary>
    public static RectInt RectFromAnchor(Vector3 anchorWorld, Vector2Int size)
    {
        int n  = Mathf.Max(1, size.x);
        int m  = Mathf.Max(1, size.y);
        int ox = Mathf.FloorToInt((anchorWorld.x - GridOrigin.x) / CELL - n * 0.5f + 0.5f);
        int oy = Mathf.FloorToInt((anchorWorld.y - GridOrigin.y) / CELL + 0.5f);
        return new RectInt(ox, oy, n, m);
    }

    /// <summary>ĐIỂM NEO (mép dưới + giữa ngang) của một vùng ô — chiều ngược của RectFromAnchor.</summary>
    public static Vector3 RectAnchorWorld(RectInt rect) => new Vector3(
        GridOrigin.x + (rect.xMin + rect.width * 0.5f) * CELL,
        GridOrigin.y + rect.yMin * CELL,
        0f);

    /// <summary>Tiện ích: snap NEO trực tiếp từ data + số bước xoay.</summary>
    public static Vector3 SnapAnchorFor(PlaceableItemData data, Vector3 world, int rotationSteps)
        => SnapAnchor(world, GridSizeOf(data, rotationSteps));

    /// <summary>
    /// SNAP TÂM Ô cho công trình N×M ô.
    ///
    /// ⚠ V8: KHÔNG CÒN DÙNG TRONG LUỒNG ĐẶT — hãy dùng <see cref="SnapAnchor"/>.
    /// Giữ lại vì là API public (tool/debug có thể cần một điểm TÂM đã khớp lưới) và vì
    /// xoá một hàm static public là cách nhanh nhất làm vỡ biên dịch của người khác.
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

    /// <summary>
    /// Vùng ô mà một công trình N×M chiếm khi TÂM nằm ở centerWorld.
    /// ⚠ V8: luồng đặt đã chuyển sang <see cref="RectFromAnchor"/>. Hàm này CHỈ còn dùng
    /// cho đầu vào thật sự là TÂM (DEV-2: ConstructionBridge.ReserveCells nhận
    /// ConstructionSite.CenterWorld). GIỮ NGUYÊN TÊN + CHỮ KÝ — DEV-2 đang gọi.
    /// </summary>
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
            }

            // V6: dùng CHUNG HashSet với WarnGridSizeMissing — trước đây hai chỗ có hai cổng
            // chặn riêng nên cùng một asset bị kêu hai lần với hai câu chữ khác nhau.
            if ((measured.x > 1 || measured.y > 1) && _warnedMissingGridSize.Add(data.name))
                Debug.LogWarning($"[Placement] '{data.itemName}' chưa điền gridSize " +
                                 $"→ tạm đo từ prefab = {measured.x}×{measured.y} ô. " +
                                 "Chạy Tools ▸ Farm ▸ Suy Kích Thước Ô Công Trình để chốt.", data);

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
    /// Art của dự án đặt pivot ở ĐÁY sprite (đúng chuẩn — để chân nhà chạm điểm đặt),
    /// ví dụ Home1 = (0,192), Home3 = (0,208), Home5 = (0,194), chuồng bò = (0,224).
    ///
    /// 🔴 V8 — CHỈ CÒN MỘT NGƯỜI DÙNG: phép CHUYỂN ĐỔI SAVE v0 → v1 (MigrateAnchorV0ToV1).
    /// Luồng đặt KHÔNG cộng bù pivot nữa (neo đã là mép dưới vùng ô), vì chính việc mỗi
    /// nhà lệch pivot một con số khác nhau là nguyên nhân "méo méo không đều".
    /// Vẫn để public: save cũ cần nó, và tool đo kích thước ô cũng in ra con số này.
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

    /// <summary>Vùng ô của ghost đang cầm — suy TRỰC TIẾP từ điểm neo, không cộng bù gì.</summary>
    private RectInt CurrentRectOf(Vector2Int size)
    {
        if (currentGhost == null) return new RectInt(0, 0, 0, 0);
        return RectFromAnchor(currentGhost.transform.position, size);
    }

    /// <summary>
    /// Nửa chiều sâu vùng ô, tính bằng world unit — khoảng cách NEO ↔ TÂM theo trục Y.
    /// V8: đây là TOÀN BỘ phần bù giữa hai hệ toạ độ (trục X hai hệ trùng nhau).
    /// </summary>
    private static float HalfDepthWorld(PlaceableItemData data, int rotationSteps)
        => Mathf.Max(1, GridSizeOf(data, rotationSteps).y) * CELL * 0.5f;

    /// <summary>
    /// Đổi vị trí NEO (mép dưới vùng ô) thành TÂM vùng ô.
    ///
    /// 🔴 V8 — ĐÃ BỎ pivotOffset, chỉ còn nửa chiều sâu ô:
    ///     center = anchor + (0, M·CELL/2)
    ///
    /// VÌ SAO GIỮ LẠI HÀM NÀY (thay vì xoá): DEV-2 đang gọi ở
    ///   • ConstructionManager.SpawnSite()  — đặt transform công trường vào tâm khối ô
    ///   • ConstructionSite.Initialize()    — chiều ngược, suy ra AnchorWorld
    /// Xoá là vỡ biên dịch của DEV-2. Đổi RUỘT nhưng giữ TÊN + CHỮ KÝ nên DEV-2 tự động
    /// hưởng hệ toạ độ mới mà không phải sửa một dòng nào, và cặp hàm này vẫn khứ-hồi
    /// CHÍNH XÁC TUYỆT ĐỐI: FootprintCenterToAnchor(AnchorToFootprintCenter(a)) == a.
    /// </summary>
    public static Vector3 AnchorToFootprintCenter(Vector3 anchorWorld, PlaceableItemData data,
                                                  int rotationSteps)
        => new Vector3(anchorWorld.x, anchorWorld.y + HalfDepthWorld(data, rotationSteps), anchorWorld.z);

    /// <summary>
    /// Chiều NGƯỢC LẠI: từ TÂM vùng ô suy ra ĐIỂM NEO để Instantiate prefab.
    /// DEV-2 (ConstructionSite) dùng để biết đặt công trình xây xong ở đâu, sau khi
    /// giàn giáo đã được dựng quanh tâm.
    /// V8: anchor = center − (0, M·CELL/2).
    /// </summary>
    public static Vector3 FootprintCenterToAnchor(Vector3 centerWorld, PlaceableItemData data,
                                                  int rotationSteps)
        => new Vector3(centerWorld.x, centerWorld.y - HalfDepthWorld(data, rotationSteps), centerWorld.z);

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
    /// HỘP BAO VISUAL CỦA MỘT PREFAB ASSET, tính theo WORLD UNIT, gốc đặt tại ROOT prefab.
    /// Một hàm DUY NHẤT cho cả kích thước ô lẫn độ lệch pivot — trước đây hai hàm chép
    /// tay của nhau và đã trôi khác nhau, dẫn tới size và offset nói hai chuyện khác nhau.
    ///
    /// ⚠ HAI CHI TIẾT SỐNG CÒN:
    ///  1. Tâm mỗi mảnh phải lấy bằng `TransformPoint(sprite.bounds.center)`, KHÔNG phải
    ///     `transform.position`. Art của dự án đặt pivot ở ĐÁY sprite, nên transform.position
    ///     là CHÂN chứ không phải tâm. Dùng transform.position thì prefab một-sprite luôn
    ///     ra offset (0,0) và toàn bộ phép bù pivot trở thành vô tác dụng.
    ///  2. Phải đi qua TransformPoint (chuỗi transform đầy đủ) chứ không dùng localPosition —
    ///     localPosition là toạ độ so với CHA TRỰC TIẾP nên prefab lồng sâu (Pen_03/04,
    ///     May_01..03) sẽ ra hộp bao lệch.
    ///
    /// 🔴 V8 — SỬA LỖI LỆCH ĐƠN VỊ (nguyên nhân sâu xa của "vùng ô không khớp thân nhà"):
    ///     Bản cũ lấy tâm bằng `prefab.transform.InverseTransformPoint(...)` → ra toạ độ
    ///     LOCAL (đã CHIA cho scale root), trong khi w/h lại nhân lossyScale → WORLD.
    ///     Prefab của dự án có root scale = 100, nên `size` đúng (384 unit) mà `center`
    ///     bé đi 100 lần (1.92 thay vì 192). Hậu quả:
    ///       • PivotOffsetOf trả ~2 unit → phép "bù pivot" của V7 gần như KHÔNG làm gì,
    ///         nên vùng ô của Ghost nằm thấp hơn thân nhà đúng 2 ô — người chơi thấy nhà
    ///         nổi phía trên thảm xanh của chính nó.
    ///       • Ghost và RefreshOccupancy (đo bounds THẬT trong scene) ra HAI rect khác nhau
    ///         cho cùng một công trình → vừa "chặn oan" vừa "đặt đè lên nhau".
    ///       • Với prefab NHIỀU sprite, Encapsulate trộn tâm-local với size-world nên hộp
    ///         bao tổng vô nghĩa → gridSize suy ra cũng sai.
    ///     Giờ cả center lẫn size đều là WORLD UNIT → khớp với BuildingGridSizeTool
    ///     (Tools ▸ Farm ▸ Suy Kích Thước Ô Công Trình) và khớp với các số trong §0
    ///     (Home1 = 192, Home3 = 208, Home5 = 194).
    /// Bộ lọc tên dùng chung IsValidSourceVisualRenderer để khớp với Editor tool — hai bên
    /// phải ra CÙNG con số.
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

            // WORLD UNIT, gốc tại ROOT prefab — CÙNG ĐƠN VỊ với w/h ở trên.
            // Không dùng InverseTransformPoint: nó chia cho scale root (=100) trong khi
            // w/h đã nhân scale root → lệch đúng 100 lần (xem doc ⚠ ở trên).
            Vector3 centerFromRoot = sr.transform.TransformPoint(sr.sprite.bounds.center)
                                   - prefab.transform.position;

            var one = new Bounds(centerFromRoot, new Vector3(w, h, 0f));
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

    // ══════════════════════════════════════════════════════════════════════
    // HỢP ĐỒNG API §4 VỚI DEV-2 — ✅ ĐÃ CHỐT, KHÔNG ĐỔI TÊN
    // DEV-2 (PlacementGhostVisualController) đọc 5 property này mỗi frame để vẽ
    // thanh xác nhận + 4 chevron. Đổi tên là vỡ code DEV-2 → phải bàn ở §4 trước.
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// true = đang DI CHUYỂN một vật đã có trên map → đặt MIỄN PHÍ (không trừ tiền).
    /// DEV-2 dùng để đổi chữ thanh xác nhận sang "ĐẶT MIỄN PHÍ".
    /// </summary>
    public bool IsFreeMove => currentlyEditingBuilding != null;

    /// <summary>
    /// Giá VÀNG của vật đang cầm. 0 khi đang di chuyển vật có sẵn (miễn phí) hoặc khi
    /// vật này bán bằng kim cương. DEV-2 chỉ hiện icon vàng khi số này &gt; 0.
    /// </summary>
    // F10: ô đất có giá LUỸ TIẾN nên không đọc thẳng `goldPrice` nữa — mọi nơi phải đi
    // qua PlotPurchasePricing, nếu không thì Ghost hiện một giá mà lúc trừ tiền lại là giá khác.
    public int CurrentPriceGold => (IsFreeMove || currentItem == null)
        ? 0
        : PlotPurchasePricing.EffectiveGoldPrice(currentItem);

    /// <summary>
    /// Số công trình đã ĐẶT (đã mua) mang itemID này, đọc từ save `FARM_PLACED_BUILDINGS`.
    /// PlotPurchasePricing dùng để tính giá ô đất tiếp theo.
    ///
    /// LƯU Ý: ô đang trong giai đoạn XÂY (`ConstructionManager`) chưa nằm trong danh sách
    /// này, nên mua hai ô liên tiếp trước khi ô đầu xây xong sẽ cùng một giá. Chấp nhận
    /// được: `Đất Trồng` có buildTimeSeconds = 30 nên cửa sổ đó rất hẹp, và chọn sai
    /// hướng này (rẻ hơn) an toàn hơn là tính tiền người chơi cho ô họ chưa có.
    /// </summary>
    public int CountPlacedByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        int n = 0;
        for (int i = 0; i < placedBuildings.Count; i++)
        {
            if (placedBuildings[i] != null && placedBuildings[i].itemId == itemId)
                n++;
        }
        return n;
    }

    /// <summary>Giá KIM CƯƠNG của vật đang cầm. Quy ước giống CurrentPriceGold.</summary>
    public int CurrentPriceGem => (IsFreeMove || currentItem == null) ? 0 : currentItem.diamondPrice;

    /// <summary>
    /// Vùng ô Ghost đang chiếm — DEV-2 đặt 4 chevron vào 4 GÓC CỦA RECT NÀY
    /// (không phải 4 góc sprite: mái nhà nhô ra ngoài footprint).
    /// Góc world tính bằng PlacementManager.CellCornerToWorld(rect.xMin, rect.yMin) …
    /// width/height = 0 khi không có Ghost nào đang hoạt động.
    /// </summary>
    public RectInt CurrentRect => currentRect;

    /// <summary>Vị trí hiện tại có đặt được không (trống + trong biên). DEV-2 đổi xanh/đỏ.</summary>
    public bool IsCurrentValid => isValidPos;

    // Key PlayerPrefs lưu danh sách công trình — dùng chung bởi PlotController.DebugClearData()
    public const string BuildingsSaveKey = "FARM_PLACED_BUILDINGS";

    /// <summary>
    /// PHIÊN BẢN ĐỊNH DẠNG SAVE công trình.
    ///   v0 (không có key "saveVersion") = hệ V7: anchor = tâm vùng ô − pivotOffset
    ///   v1 = hệ V8: anchor = mép dưới vùng ô
    /// LoadBuildings() tự dịch v0 → v1 rồi ghi lại. Xem MigrateAnchorV0ToV1().
    /// </summary>
    public const int CurrentSaveVersion = 1;

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
    private RectTransform     deleteRect;   // nút 🗑 (chỉ có trong Editor / Development Build)
    private bool              deleteRectLookupDone;   // chống tra lại mỗi frame
    private Transform         ghostVisualCloneRoot;
    private PlacementGhostVisualController ghostVisual;
    private static Sprite     ghostActionBarSprite;
    private PlaceableItemData currentItem;      // item đang MUA (null khi đang sửa)
    private PlaceableItemData editingItemData;  // data suy ra của công trình đang SỬA (có thể null)
    private bool              isValidPos;
    private int               rotationSteps;    // 0-3, mỗi bước 90°
    private RectInt           currentRect;      // vùng ô Ghost đang chiếm (API §4 cho DEV-2)

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
        // TƯƠNG THÍCH NGƯỢC: save cũ KHÔNG có key "saveVersion".
        // JsonUtility bỏ qua field thiếu và giữ giá trị mặc định của C# = 0
        // → mọi save do bản V7 ghi ra tự động được nhận là v0 và sẽ được dịch toạ độ.
        // (Đúng thủ thuật đã dùng thành công cho field "rot" ở BuildingEntry.)
        public int saveVersion;
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

        // Nút UI được ưu tiên tuyệt đối — kiểm tra trước khi xử lý drag.
        //
        // ⚠ BẮT BUỘC phải liệt kê MỌI nút ở đây. Nút nào thiếu thì click sẽ rơi xuống
        // nhánh "ghost đi theo chuột" bên dưới → ghost NHẢY tới con trỏ, mang luôn cái
        // nút chạy khỏi ngón tay → click không bao giờ hoàn tất, người dùng tưởng nút chết.
        // Đây chính là lỗi nút 🗑 không bấm được lúc đầu.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // LƯỚI AN TOÀN cho nút 🗑. Thứ tự thật: EnsureDeleteButton tạo nút TRƯỚC
        // BindGhostButtons, nên `deleteRect` thường đã có sẵn. Đoạn này chỉ để cứu
        // trường hợp thứ tự khởi tạo đổi trong tương lai.
        // Cờ `deleteRectLookupDone` là bắt buộc: không có nó thì mỗi frame lại
        // GetComponentsInChildren → cấp phát một mảng mới suốt lượt kéo.
        if (!deleteRectLookupDone && deleteRect == null && currentlyEditingBuilding != null)
        {
            deleteRectLookupDone = true;
            foreach (var b in currentGhost.GetComponentsInChildren<Button>(true))
                if (b.name == "Btn_Delete") { deleteRect = b.GetComponent<RectTransform>(); break; }
        }
#endif

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOverRect(deleteRect))  { DeleteEditingBuilding(); return; }
            if (IsMouseOverRect(confirmRect)) { ConfirmPlacement();      return; }
            if (IsMouseOverRect(cancelRect))  { CancelPlacement();       return; }
            if (IsMouseOverRect(rotateRect))  { RotateGhost();           return; }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // DEV/Edit: phím Delete hoặc Backspace → XÓA HẲN vật đang sửa.
        //
        // ⚠ PHẢI bọc #if. Không bọc thì ở bản release PC/WebGL người chơi bấm Delete
        // trong Edit Mode là MẤT CÔNG TRÌNH VĨNH VIỄN, KHÔNG hoàn tiền — đúng thứ mà
        // #if ở EnsureDeleteButton đang cố ngăn. Chặn nút mà để hở bàn phím là vô nghĩa.
        if (currentlyEditingBuilding != null &&
            (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            DeleteEditingBuilding();
            return;
        }
#endif

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
        // Hộp va chạm đúng bằng footprint N×M ô, không còn 50×50 cứng.
        // V8: vùng ô suy TRỰC TIẾP từ điểm neo (chân công trình) và mọc LÊN M ô.
        // KHÔNG còn cộng bù pivot — chính phép bù đó (mỗi nhà một con số) là nguyên nhân lệch.
        RectInt rect = CurrentRectOf(size);
        currentRect  = rect;                 // API §4: DEV-2 đọc để đặt 4 chevron
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
        // V8: SnapAnchor là IDEMPOTENT → công trình đã đặt bằng hệ mới thì kéo ra kéo vào
        // KHÔNG dịch một pixel nào; công trình từ save cũ thì được kéo về đúng lưới.
        currentGhost.transform.position = SnapAnchor(originalEditPosition, size);
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
        // Snap lại theo kích thước MỚI: 3×2 và 2×3 có mốc căn giữa NGANG khác nhau
        // (một cạnh chẵn, một cạnh lẻ) → không snap lại là lệch nửa ô ngay.
        // Trục Y không đổi mốc (neo luôn ở mép dưới) nên xoay không làm nhà "tụt".
        currentGhost.transform.position = SnapAnchor(currentGhost.transform.position, size);
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
        cloneLocalCenter = Vector3.zero;
        fallbackGridSize = Vector2Int.one;
        if (currentGhost == null || ghostVisualCloneRoot == null) return;

        Bounds b = CalculateSourceVisualBounds(ghostVisualCloneRoot);
        if (b.size.x <= 0.01f && b.size.y <= 0.01f) return;

        Vector3 c = currentGhost.transform.InverseTransformPoint(b.center);
        c.z = 0f;
        cloneLocalCenter = c;

        // V8 ĐÃ XOÁ `fallbackPivotOffset`: vùng ô không còn suy từ TÂM sprite nữa mà suy
        // từ ĐIỂM NEO, nên độ lệch pivot của bản clone không còn ý nghĩa gì. Giữ lại sẽ là
        // code chết và mời gọi người sau cộng bù hai lần.

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
    /// V8: thảm đặt ĐÚNG TÂM `rect` mà Update() dùng để validate, và `rect` giờ suy trực
    /// tiếp từ điểm neo (mép dưới = chân công trình, mọc lên M ô). Nguyên tắc bất di bất
    /// dịch: THẢM XANH = VÙNG SẼ BỊ CHẶN, không hơn không kém.
    /// Thảm không "trôi" khi kéo vì neo snap theo bước tròn 1 ô và rect nhảy theo đúng 1 ô.
    /// </summary>
    private void SetupFootprint(Vector2Int size)
    {
        if (currentGhost == null) return;

        float targetW = Mathf.Max(1, size.x) * CELL;
        float targetH = Mathf.Max(1, size.y) * CELL;

        // Vùng ô đang được validate — nguồn sự thật cho cả thảm, khung 4 góc lẫn API §4.
        currentRect = CurrentRectOf(size);
        Vector3 rectCenterWorld = RectCenterWorld(currentRect);

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
        // `pos` là ĐIỂM NEO = MÉP DƯỚI + GIỮA NGANG vùng ô (V8), và vì art đặt pivot ở đáy
        // thì đây đúng là CHÂN công trình → toạ độ để Instantiate và để ghi save.
        Vector3 pos = currentGhost.transform.position;
        pos.z = 0f;

        Vector2Int size = CurrentGridSize();

        // Vùng ô CHỐT LẠI — mọi phép tính LƯỚI (reserve / occupancy) dùng đúng rect này.
        RectInt placedRect = RectFromAnchor(pos, size);

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
        // 📐 QUY ƯỚC TOẠ ĐỘ VỚI DEV-2 (V8 — HỢP ĐỒNG KHÔNG ĐỔI, chỉ đổi ĐỊNH NGHĨA của neo):
        //    tham số `pos` VẪN là ĐIỂM NEO đã snap, KHÔNG phải tâm khối ô → DEV-2 không
        //    phải sửa một dòng nào. Chỉ khác: từ V8 neo = MÉP DƯỚI vùng ô (bội số của CELL)
        //    thay vì "tâm vùng ô − pivotOffset".
        //    ConstructionManager vẫn gọi PlacementManager.AnchorToFootprintCenter() khi cần
        //    tâm (giàn giáo, giữ ô, VFX) — hàm đó đã được cập nhật sang công thức mới.
        bool started = TryStartConstructionDev2(currentItem, pos, rotationSteps, assignedPlotId);

        if (started)
        {
            // Giữ chỗ ô trong lúc xây để không đặt đè lên giàn giáo.
            // placedRect suy thẳng từ neo nên TRÙNG KHÍT vùng ô mà Ghost vừa hiện cho
            // người chơi thấy, và cũng trùng vùng ô mà DEV-2 dựng giàn giáo lên.
            reservedRects.Add(placedRect);
            RebuildOccupiedCells();
            Cleanup(refund: false);
            return;
        }

        GameObject spawnedObj = Instantiate(currentItem.prefabToBuild, pos, RotationOf(rotationSteps));

        FixBuildingRenderSorting(spawnedObj);
        FixAnimalVisibility(spawnedObj);

        // Tắt bất kỳ placeholder cùng tên trong scene để tránh object thừa
        DisablePlaceholderInScene(currentItem.prefabToBuild.name, spawnedObj);

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

        // `pos` = ĐIỂM NEO của prefab — đúng thứ cần ghi vào save, vì LoadBuildings() sẽ
        // Instantiate lại tại chính toạ độ này.
        // V8: DEV-2 dựng prefab tại ConstructionSite.AnchorWorld, mà AnchorWorld đã được
        // FootprintCenterToAnchor() tính theo hệ mới → `pos` chắc chắn là mép dưới vùng ô.
        // Vẫn snap lại cho chắc: nếu prefab có script nào tự dịch transform lúc Awake thì
        // save sẽ bị lệch lưới vĩnh viễn. SnapAnchor idempotent nên không bao giờ hại.
        float keepZ  = spawnedObj.transform.position.z;
        Vector3 pos  = SnapAnchor(spawnedObj.transform.position, GridSizeOf(data, rotationStepsUsed));
        pos.z        = 0f;
        // Giữ nguyên z của DEV-2 trên transform (họ có thể dùng z để xếp lớp), chỉ chuẩn
        // hoá x/y. Toạ độ ghi save luôn z = 0 như mọi entry khác.
        spawnedObj.transform.position = new Vector3(pos.x, pos.y, keepZ);

        FixBuildingRenderSorting(spawnedObj);
        FixAnimalVisibility(spawnedObj);
        DisablePlaceholderInScene(data.prefabToBuild != null ? data.prefabToBuild.name : spawnedObj.name, spawnedObj);

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

        // ⚠ Nhả chỗ phải truyền TÂM VÙNG Ô, không phải neo: ReleaseConstructionCells tìm
        // rect CHỨA ô của điểm truyền vào, mà neo nằm ở MÉP DƯỚI nên ô chứa nó là ô ngoài
        // cùng — sai một chút là không nhả được, ô bị khoá vĩnh viễn tới lần Clear dữ liệu.
        // AnchorToFootprintCenter đã là công thức V8 (+ M·CELL/2) nên trả về đúng tâm rect.
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
        // LUÔN ghi saveVersion = 1: mọi toạ độ trong placedBuildings đã ở hệ V8
        // (đặt mới thì sinh ra ở hệ mới, load save cũ thì đã được dịch ở LoadBuildings).
        var save = new BuildingsSave { saveVersion = CurrentSaveVersion, list = placedBuildings };
        PlayerPrefs.SetString(BuildingsSaveKey, JsonUtility.ToJson(save));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs

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

        // v0 = save do bản V7 ghi ra (không có key saveVersion → JsonUtility để 0).
        bool needMigrate = save.saveVersion < CurrentSaveVersion;
        int  migratedCount = 0;

        foreach (var entry in save.list)
        {
            PlaceableItemData itemData = FindItemById(entry.itemId);
            if (itemData == null || itemData.prefabToBuild == null)
            {
                continue;
            }

            // rot thiếu trong save cũ → JsonUtility để 0 → không xoay. Tương thích ngược.
            int rot = entry.rot & 3;

            // ── CHUYỂN ĐỔI SAVE v0 → v1 (V3) ────────────────────────────────────
            // Sửa TRỰC TIẾP vào `entry`; entry được add vào placedBuildings ở cuối vòng lặp
            // và SaveBuildings() ở cuối hàm ghi lại kèm saveVersion = 1 → chỉ dịch MỘT LẦN.
            if (needMigrate)
            {
                Vector3 migrated = MigrateAnchorV0ToV1(new Vector3(entry.x, entry.y, 0f), itemData, rot);
                entry.x = migrated.x;
                entry.y = migrated.y;
                migratedCount++;
            }

            // entry.x/y là ĐIỂM NEO = mép dưới vùng ô (V8) → Instantiate thẳng vào đây.
            // Vùng ô được tính lại sau đó bởi RefreshOccupancy() qua RectFromAnchor().
            Vector3 pos = new(entry.x, entry.y, 0f);
            GameObject obj = Instantiate(itemData.prefabToBuild, pos, RotationOf(rot));

            FixBuildingRenderSorting(obj);
            FixAnimalVisibility(obj);

            // Tắt placeholder cùng tên còn sót trong scene
            DisablePlaceholderInScene(itemData.prefabToBuild.name, obj);

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

        // Ghi lại ngay: (a) entry được cấp plotId mới, (b) toạ độ vừa dịch sang hệ V8,
        // (c) đóng dấu saveVersion = 1 để lần mở sau KHÔNG dịch lần hai (dịch hai lần là
        // công trình bay lên nửa chiều sâu ô — đúng loại bug rất khó truy).
        SaveBuildings();

        if (migratedCount > 0)
            Debug.Log($"[Placement] Đã chuyển {migratedCount} công trình từ save v{save.saveVersion} " +
                      $"sang v{CurrentSaveVersion} (neo: tâm vùng ô − pivot → mép dưới vùng ô).");
    }

    /// <summary>
    /// 🔴 V3 — DỊCH TOẠ ĐỘ NEO TỪ HỆ V7 (v0) SANG HỆ V8 (v1).
    ///
    /// Hệ cũ (V7): neo = kết quả SnapCenter, và vùng ô được suy bằng neo + pivotOffset
    ///             ⇒ theo mô hình của V7: <c>neoCũ = footprintCenter − pivotOffset</c>
    /// Hệ mới (V8): <c>neoMới = mép dưới vùng ô = footprintCenter − (0, M·CELL/2)</c>
    /// ⇒ <c>neoMới = neoCũ + pivotOffset − (0, M·CELL/2)</c>   (M = chiều sâu ô)
    ///
    /// Ý NGHĨA THỰC TẾ CỦA CÔNG THỨC: vì pivot ở đáy sprite nên pivotOffset.y ≈ nửa chiều
    /// cao sprite ≈ M·CELL/2, hai số gần như triệt tiêu nhau. Nói cách khác phép dịch này
    /// chính là "giữ nguyên CHÂN nhà, chỉ kéo nó về đúng đường kẻ lưới gần nhất" — đúng
    /// yêu cầu "công trình đứng đúng chỗ như trước, không nhảy".
    ///
    /// Sau phép dịch còn SnapAnchor một lần nữa vì:
    ///   • Phần dư (pivotOffset.y − M·CELL/2) là vài chục unit, chưa nằm trên lưới.
    ///   • Save rất cũ có thể tích nhiễu số thực; SnapAnchor idempotent nên vô hại.
    ///
    /// KIỂM CHỨNG BẰNG SỐ — lấy đúng đầu ra của V7 (neo = SnapCenter = (oy + M/2)·CELL),
    /// với oy = 5 cho cả ba nhà:
    ///   Home1 4×4 pivot 192 : neoCũ = (5+2)·100   = 700 → 700+192−200 = 692 → snap 700 ✔ dịch 0
    ///   Home5 4×4 pivot 194 : neoCũ = (5+2)·100   = 700 → 700+194−200 = 694 → snap 700 ✔ dịch 0
    ///   Home3 4×5 pivot 208 : neoCũ = (5+2.5)·100 = 750 → 750+208−200 = 758 → snap 800 ✔ dịch +50
    /// TRƯỚC: chân ở 700 / 700 / 750 → Home3 lệch NỬA Ô so với hai nhà kia, chỉ vì nó sâu
    ///        5 ô (số LẺ) nên mốc tâm của nó rơi vào giữa ô. Đây đúng là "nhà mái xanh nhô ra".
    /// SAU  : chân ở 700 / 700 / 800 → tất cả là bội số của CELL → thẳng hàng tuyệt đối.
    /// Chỉ Home3 dịch 50 unit (≈26 px) vì gridSize của nó cũng đổi 4×5 → 4×4; 4 nhà còn lại
    /// KHÔNG dịch một pixel nào.
    /// </summary>
    private static Vector3 MigrateAnchorV0ToV1(Vector3 anchorOld, PlaceableItemData data, int rotSteps)
    {
        Vector2    pivot = PivotOffsetOf(data, rotSteps);
        Vector2Int size  = GridSizeOf(data, rotSteps);

        Vector3 converted = new Vector3(
            anchorOld.x + pivot.x,
            anchorOld.y + pivot.y - Mathf.Max(1, size.y) * CELL * 0.5f,
            0f);

        return SnapAnchor(converted, size);
    }

    // Tìm tất cả object trong scene có tên trùng prefabName (không có "(Clone)")
    // và SetActive(false) để tránh object thừa song song với clone vừa tạo.
    //
    // VÌ SAO quét theo Transform chứ không theo một component đánh dấu:
    // bản cũ neo vào `HouseOrderController` — component vừa bị xoá cùng hệ đơn hàng nhà dân.
    // Ứng viên thay thế đầu tiên là `EditableBuilding`, nhưng KIỂM TRA THẬT thì chỉ
    // House_01 và House_02 có component đó; House_03/04/05 KHÔNG có. Neo vào nó là ba
    // placeholder kia không bao giờ bị tắt, và người chơi thấy hai căn nhà chồng lên nhau.
    //
    // Quét toàn bộ Transform là cách duy nhất không phụ thuộc vào việc prefab nào lỡ thiếu
    // component nào. Đắt hơn, nhưng hàm này chỉ chạy đúng một lần mỗi lần đặt công trình.
    private void DisablePlaceholderInScene(string prefabName, GameObject skipObj)
    {
        if (string.IsNullOrEmpty(prefabName)) return;

        var all = FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var t in all)
        {
            if (t == null) continue;

            GameObject go = t.gameObject;
            if (go == skipObj) continue;
            if (go.name != prefabName) continue;            // exact name = chưa có "(Clone)"

            // Bỏ qua mọi thứ nằm BÊN TRONG object vừa spawn: prefab có thể chứa một con
            // trùng tên với chính nó, tắt nhầm là công trình mới hiện thiếu một mảnh.
            if (skipObj != null && t.IsChildOf(skipObj.transform)) continue;

            go.SetActive(false);
        }
    }

    /// <summary>Xóa toàn bộ dữ liệu nhà/công trình đã đặt khỏi PlayerPrefs.</summary>
    public void ClearBuildingData()
    {
        PlayerPrefs.DeleteKey(BuildingsSaveKey);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
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
        //
        // ⚠ TRƯỚC ĐÂY DÙNG Mathf.Approximately — VÀ ĐÓ LÀ LỖI.
        // Approximately có epsilon cực chặt (~1e-6 tương đối). Toạ độ trong save đi qua
        // JSON (chuỗi thập phân) rồi parse lại, còn `originalEditPosition` là float đã qua
        // snap — hai bên lệch nhau ở vài bit cuối là chuyện thường. Khi đó RemoveAll không
        // khớp gì, `removed = 0`, object bị huỷ khỏi map NHƯNG entry vẫn nằm trong save
        // → Play lại NÓ QUAY VỀ. Đúng hiện tượng "xoá rồi mà chậu hoa vẫn còn".
        //
        // Giờ tìm entry GẦN NHẤT trong bán kính nửa ô. Chỉ xoá đúng một entry — dùng
        // RemoveAll với bán kính sẽ xoá oan công trình kề bên nếu chúng sát nhau.
        int removed = 0;
        {
            float nguongBinhPhuong = (CELL * 0.5f) * (CELL * 0.5f);
            int   iGan = -1;
            float dGan  = float.MaxValue;

            for (int i = 0; i < placedBuildings.Count; i++)
            {
                var e  = placedBuildings[i];
                float dx = e.x - originalEditPosition.x;
                float dy = e.y - originalEditPosition.y;
                float d2 = dx * dx + dy * dy;
                if (d2 < dGan) { dGan = d2; iGan = i; }
            }

            if (iGan >= 0 && dGan <= nguongBinhPhuong)
            {
                placedBuildings.RemoveAt(iGan);
                removed = 1;
            }
            else if (iGan >= 0)
            {
                Debug.LogWarning($"[Placement] Không có entry nào trong save nằm gần " +
                                 $"({originalEditPosition.x:0},{originalEditPosition.y:0}). " +
                                 $"Gần nhất cách {Mathf.Sqrt(dGan):0} unit — quá xa nửa ô ({CELL * 0.5f:0}). " +
                                 "Dùng Tools ▸ Farm ▸ Dọn Dẹp Dữ Liệu Đã Lưu để xoá tay.");
            }
        }
        SaveBuildings();

        // 2) Hủy object gốc khỏi map; clear ref TRƯỚC để Cleanup không "hồi sinh" nó
        var go = currentlyEditingBuilding.gameObject;
        currentlyEditingBuilding = null;
        if (go != null)
        {
            knownSizes.Remove(go);
            occupancyByObject.Remove(go);

            // SetActive(false) TRƯỚC Destroy — bắt buộc.
            // Destroy chỉ huỷ thật ở CUỐI frame, nên RefreshOccupancy() ngay bên dưới
            // (dùng FindObjectsInactive.Exclude) VẪN THẤY object này và đăng ký lại
            // vùng ô của nó → ô không được nhả, đặt cái mới vào đó báo "đã có nhà".
            go.SetActive(false);
            Destroy(go);
        }

        // 3) Dọn Ghost + reset state (không refund, không restore)
        Cleanup(refund: false);
        RefreshOccupancy();

        if (removed == 0)
            Debug.LogWarning("[Placement] Đã xoá khỏi map nhưng KHÔNG có entry nào trong save — " +
                             "đây là vật do designer kéo tay vào scene. Lần Play sau NÓ SẼ QUAY LẠI. " +
                             "Muốn xoá hẳn thì xoá trong Scene (thoát Play Mode) rồi Ctrl+S.");
        else
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
                // Cancel đặt mới: hoàn tiền.
                // Hoàn ĐÚNG số vừa trừ: với ô đất, giá phụ thuộc số ô đã có, mà lúc này
                // ô chưa được ghi vào save nên PlotPurchasePricing trả lại đúng con số cũ.
                if (currentItem.diamondPrice > 0)
                    FarmEconomyManager.Instance.AddGems(currentItem.diamondPrice);
                else
                    FarmEconomyManager.Instance.AddGold(PlotPurchasePricing.EffectiveGoldPrice(currentItem));

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
        deleteRect               = null;   // đừng để lại tham chiếu treo tới Ghost đã huỷ
        deleteRectLookupDone     = false;
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
        // API §4: rect rỗng = "không có Ghost nào" → DEV-2 tự ẩn 4 chevron.
        currentRect              = new RectInt(0, 0, 0, 0);
        isValidPos               = false;

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
            // V8: object trong knownSizes là object do CHÍNH TA Instantiate → transform.position
            // chắc chắn là ĐIỂM NEO của hệ mới (mép dưới vùng ô) → suy rect thẳng từ đó.
            // Chính xác TUYỆT ĐỐI, không còn phụ thuộc phép đo bounds (vốn phình theo mái
            // nhà, viền trong suốt của sprite và cả con vật nhô ra khỏi chuồng).
            occupancyByObject[go] = RectFromAnchor(go.transform.position, kv.Value);
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
    /// Vùng ô của một object đang đứng trên map. Ba nhánh, theo độ tin cậy giảm dần:
    ///
    ///  1. CÓ trong knownSizes = do chính ta Instantiate ⇒ transform.position là ĐIỂM NEO
    ///     của hệ V8 ⇒ RectFromAnchor cho kết quả CHÍNH XÁC TUYỆT ĐỐI.
    ///  2. Tra được data theo tên prefab nhưng KHÔNG do ta đặt (designer kéo tay vào scene,
    ///     có thể nằm lệch lưới và pivot không chắc ở đáy) ⇒ số Ô lấy từ data, còn VỊ TRÍ
    ///     đo từ hộp bao thật. Đo bounds là cách DUY NHẤT không phải đoán pivot.
    ///  3. Không tra được gì ⇒ suy cả rect từ hộp bao world.
    ///
    /// VÌ SAO KHÔNG dùng RectFromAnchor cho nhánh 2 và 3: vật do scene tự đặt có thể có
    /// pivot ở GIỮA sprite (decor, ô đất). Với pivot giữa, "neo" không phải chân vật, nên
    /// suy rect từ neo sẽ đẩy vùng ô lên cao nửa chiều sâu — chặn oan trời và bỏ trống đất.
    /// </summary>
    private RectInt ComputeRectFor(GameObject go)
    {
        if (knownSizes.TryGetValue(go, out Vector2Int s))
            return RectFromAnchor(go.transform.position, s);

        Vector3 center = FootprintCenterOfSpawned(go);

        // Chưa biết → thử tra data theo tên prefab (chuẩn hơn đo bounds)
        PlaceableItemData data = FindItemByPrefabName(go.name);
        if (data != null)
        {
            Vector2Int size = SizeForSpawned(data, RotationStepsOf(go.transform), go);
            // KHÔNG ghi vào knownSizes: knownSizes là dấu hiệu "do ta đặt, neo đúng hệ V8"
            // (nhánh 1 ở trên tin vào đó). Ghi vào đây thì lần Refresh sau vật scene lệch
            // lưới sẽ bị tính rect theo neo → vùng ô nhảy một phát mấy ô.
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
    // Dùng CHUNG với nhánh cảnh báo trong GridSizeOf() để hai chỗ không kêu hai lần
    // về cùng một asset.
    private static readonly HashSet<string> _warnedMissingGridSize = new();

    /// <summary>
    /// 🔴 V6 — CẢNH BÁO CHỈ KHI THẬT SỰ SAI.
    ///
    /// BUG CŨ: hàm này in cảnh báo "còn gridSize = 1×1" cho MỌI data được truyền vào,
    /// KHÔNG hề kiểm tra gridSize. Vì CurrentGridSize() gọi nó mỗi lần cầm một vật,
    /// console báo `'Home3' còn gridSize = 1×1` trong khi asset đã là 4×5 → Edric mất
    /// thời gian đi sửa một thứ vốn đã đúng. Cảnh báo sai còn tệ hơn không cảnh báo.
    ///
    /// BA ĐIỀU KIỆN PHẢI ĐỦ CẢ BA:
    ///   1. gridSize THẬT SỰ đang là 1×1 (chưa ai điền)
    ///   2. CÓ prefabToBuild — không có thì không đo được, cảnh báo vô nghĩa
    ///      (Home2/Home4 từng rơi vào đây)
    ///   3. Prefab đo ra LỚN HƠN 1 ô — vật bé thật (chậu hoa 1×1) thì 1×1 là ĐÚNG
    /// </summary>
    private static void WarnGridSizeMissing(PlaceableItemData data)
    {
        if (data == null) return;
        if (data.gridSize.x > 1 || data.gridSize.y > 1) return;   // (1) đã điền rồi
        if (data.prefabToBuild == null) return;                   // (2) không đo được

        // (3) đo prefab — dùng CHUNG cache với GridSizeOf để không đo lại mỗi frame.
        if (!_measuredSizeCache.TryGetValue(data, out Vector2Int measured))
        {
            measured = MeasureGridSizeFromPrefab(data.prefabToBuild);
            _measuredSizeCache[data] = measured;
        }
        if (measured.x <= 1 && measured.y <= 1) return;            // 1×1 là đúng, im lặng

        if (!_warnedMissingGridSize.Add(data.name)) return;

        Debug.LogWarning(
            $"[PlacementManager] '{data.name}' còn gridSize = 1×1 nhưng prefab đo ra " +
            $"{measured.x}×{measured.y} ô — đang tạm dùng số đo. " +
            "Chạy menu Tools/Farm/Suy Kích Thước Ô Công Trình để chốt số ô.", data);
    }

    /// <summary>Bind V / X / ↻ (và Delete khi đang sửa). Một chỗ duy nhất cho cả 2 luồng.</summary>
    private void BindGhostButtons(bool bindDelete)
    {
        btnConfirm  = null;
        confirmRect = null;
        cancelRect  = null;
        rotateRect  = null;
        deleteRect  = null;
        deleteRectLookupDone = false;

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
                case "Btn_Delete":
                    // KHÔNG nối onClick ở đây — PlacementGhostVisualController.EnsureDeleteButton
                    // đã tự nối lúc tạo nút. Nối thêm sẽ thành 2 listener, xoá gọi 2 lần.
                    //
                    // NHƯNG PHẢI ghi lại rect: nhánh kiểm tra chuột trong Update() cần nó,
                    // nếu không thì ghost nhảy tới con trỏ và nút chạy khỏi ngón tay.
                    if (bindDelete) deleteRect = btn.GetComponent<RectTransform>();
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

        // V8: snap ĐIỂM NEO (chân công trình) — vùng ô mọc lên từ đó.
        return SnapAnchor(worldPos, size);
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
