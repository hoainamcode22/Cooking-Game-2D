using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// CẦU NỐI DEV-2 → DEV-1 cho hai thứ mà `PlacementManager` chưa mở public.
///
/// VÌ SAO PHẢI DÙNG REFLECTION (và vì sao chỉ TẠM THỜI):
///   • DEV-1 sở hữu `PlacementManager.cs`, DEV-2 không được phép sửa (§2 + §9.1 doc đội).
///   • Nhưng DEV-2 cần đúng 2 việc mà API công khai hiện tại chưa có:
///       1. Đọc `currentItem` — item Ghost đang cầm — để in GIÁ lên thanh xác nhận (N4).
///       2. GIỮ CHỖ Ô LƯỚI cho công trường **khôi phục từ save**. DEV-1 chỉ thêm vào
///          `reservedRects` bên trong `ConfirmPlacement()`; khi bật lại game, các công
///          trường đang xây được ConstructionManager tự dựng lại → không đi qua
///          `ConfirmPlacement()` → ô KHÔNG được giữ → người chơi đặt đè lên giàn giáo.
///   • Đây là chiều ngược của đúng thủ thuật DEV-1 đã dùng với DEV-2
///     (`PlacementManager.TryStartConstructionDev2`), lý do giống hệt: không chặn biên dịch.
///
/// ✅ YÊU CẦU GỬI DEV-1 (ghi ở §6) — khi có 2 API dưới thì XOÁ CẢ FILE NÀY:
///     public PlaceableItemData CurrentGhostItem { get; }
///     public void ReserveConstructionCells(Vector3 centerWorld, Vector2Int size);
///
/// Mọi lời gọi đều bọc null-check: tra hỏng thì chỉ mất tính năng phụ, KHÔNG ném lỗi.
/// </summary>
public static class ConstructionBridge
{
    private static bool       _probed;
    private static FieldInfo  _fCurrentItem;
    private static FieldInfo  _fReservedRects;
    private static MethodInfo _mRebuildOccupiedCells;
    private static bool       _warnedReserve;

    private static void Probe()
    {
        if (_probed) return;
        _probed = true;

        // typeof() trực tiếp được: PlacementManager cùng assembly Assembly-CSharp,
        // và nó đã tồn tại trước khi DEV-2 viết file này nên không sợ lỗi biên dịch.
        System.Type t = typeof(PlacementManager);
        const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

        _fCurrentItem          = t.GetField("currentItem", Priv);
        _fReservedRects        = t.GetField("reservedRects", Priv);
        _mRebuildOccupiedCells = t.GetMethod("RebuildOccupiedCells", Priv);
    }

    /// <summary>
    /// Item mà Ghost đang cầm (null khi đang SỬA công trình cũ — lúc đó không hiện giá).
    /// </summary>
    public static PlaceableItemData GetGhostItem()
    {
        Probe();
        if (_fCurrentItem == null) return null;
        if (PlacementManager.Instance == null) return null;

        return _fCurrentItem.GetValue(PlacementManager.Instance) as PlaceableItemData;
    }

    /// <summary>
    /// Giữ chỗ vùng ô cho một công trường (chỉ dùng ở luồng KHÔI PHỤC TỪ SAVE —
    /// luồng đặt mới đã được `ConfirmPlacement()` của DEV-1 giữ chỗ sẵn rồi).
    /// Trả false nếu không tra được field; khi đó chỉ log cảnh báo MỘT LẦN.
    ///
    /// ⚠ <paramref name="centerWorld"/> BẮT BUỘC là TÂM KHỐI Ô (ConstructionSite.CenterWorld),
    /// KHÔNG phải điểm neo. Art của dự án đặt pivot ở ĐÁY sprite nên hai điểm lệch nhau
    /// nửa chiều cao công trình; truyền nhầm neo thì vùng giữ tụt xuống mấy ô đất trống
    /// và chỗ giàn giáo đứng lại để hở cho người chơi đặt đè lên.
    /// </summary>
    public static bool ReserveCells(Vector3 centerWorld, Vector2Int size)
    {
        Probe();

        PlacementManager pm = PlacementManager.Instance;
        if (pm == null || _fReservedRects == null || _mRebuildOccupiedCells == null)
        {
            if (!_warnedReserve)
            {
                _warnedReserve = true;
                Debug.LogWarning("[Construction] Không giữ được chỗ ô cho công trường khôi phục " +
                                 "(PlacementManager đổi tên field 'reservedRects'?). " +
                                 "Người chơi có thể đặt đè lên giàn giáo. Xem ConstructionBridge.cs.");
            }
            return false;
        }

        var rects = _fReservedRects.GetValue(pm) as List<RectInt>;
        if (rects == null) return false;

        RectInt rect = PlacementManager.GetFootprintRect(centerWorld, size);

        // Không thêm trùng: hàm này có thể bị gọi lại khi load 2 lần trong 1 phiên.
        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i].Equals(rect)) return true;
        }

        rects.Add(rect);
        _mRebuildOccupiedCells.Invoke(pm, null);
        return true;
    }
}
