using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GIẢI TÊN SORTING LAYER AN TOÀN cho hệ khách du lịch.
///
/// VÌ SAO PHẢI CÓ FILE NÀY (bug Sếp gặp lúc Play test 2026-08-29):
/// bản đầu hardcode <c>sortingLayerName = "CongTrinh"</c> — tên đó lấy từ
/// <c>LivestockAI</c> nhưng **KHÔNG TỒN TẠI trong project này**. Unity xử lý tên layer
/// sai bằng cách... IM LẶNG bỏ qua, renderer rơi về layer **Default (id 0)** ⇒ khách du
/// lịch bị cây/nhà/decor che kín, đúng triệu chứng "nhân vật bị vật thể che".
///
/// Sorting layer THẬT của project (đọc từ ProjectSettings/TagManager.asset, thứ tự từ
/// dưới lên trên):
/// <code>
///   Bottom (1161173501) · Default (0) · Objects (1471039481)
///   · ObjectsFront (3561676937) · Foreground (1304480043)
/// </code>
///
/// Quy ước dựng hình của hệ tàu khách (dưới → trên):
///   • <see cref="Gangplank"/>  = "Objects"      — tấm gỗ nằm dưới chân, khách đi ĐÈ LÊN
///   • <see cref="Visitor"/>    = "ObjectsFront" — khách nổi trên cây/nhà/decor
///   • <see cref="Overlay"/>    = "Foreground"   — bubble + mặt cười, luôn trên đầu khách
///
/// <see cref="Resolve"/> trả về tên layer ĐẦU TIÊN CÓ THẬT trong danh sách ưu tiên;
/// không tên nào tồn tại thì trả "Default" kèm CẢNH BÁO (mỗi tên chỉ cảnh báo 1 lần).
/// Không bao giờ để im lặng như bản cũ nữa.
/// </summary>
public static class TouristSortingLayers
{
    /// <summary>Ưu tiên layer cho TẤM GỖ (dưới chân khách).</summary>
    public static readonly string[] Gangplank = { "Objects", "ObjectsFront", "Default" };

    /// <summary>Ưu tiên layer cho KHÁCH DU LỊCH (cùng layer với công trình/thế giới, dưới mái tàu).</summary>
    public static readonly string[] Visitor = { "Objects", "Default" };

    /// <summary>Ưu tiên layer cho BUBBLE / MẶT CƯỜI (trên cùng).</summary>
    public static readonly string[] Overlay = { "Foreground", "ObjectsFront", "Objects", "Default" };

    private static readonly HashSet<string> _daCanhBao = new HashSet<string>();

    /// <summary>
    /// Trả tên layer đầu tiên CÓ THẬT trong <paramref name="uuTien"/>.
    /// Không có cái nào → "Default" + LogWarning (1 lần cho mỗi bộ ưu tiên).
    /// </summary>
    public static string Resolve(string[] uuTien)
    {
        if (uuTien == null || uuTien.Length == 0) return "Default";

        for (int i = 0; i < uuTien.Length; i++)
        {
            if (Exists(uuTien[i]))
            {
                // Rơi xuống lựa chọn dự phòng (không phải lựa chọn số 1) thì báo cho biết —
                // im lặng chính là thứ đã giấu bug "CongTrinh" suốt vòng QA trước.
                if (i > 0) CanhBao(uuTien[0],
                    $"Sorting layer '{uuTien[0]}' không có trong project — dùng tạm '{uuTien[i]}'. " +
                    "Tạo layer đúng tên trong Project Settings > Tags and Layers nếu muốn thứ tự chuẩn.");
                return uuTien[i];
            }
        }

        CanhBao(uuTien[0],
            $"KHÔNG có sorting layer nào trong [{string.Join(", ", uuTien)}] tồn tại — " +
            "rơi về 'Default', nhân vật/hiệu ứng có thể bị vật thể khác che. " +
            "Tạo layer 'ObjectsFront' trong Project Settings > Tags and Layers.");
        return "Default";
    }

    /// <summary>Tên layer này có thật trong project không (so sánh theo TÊN, không theo id).</summary>
    public static bool Exists(string ten)
    {
        if (string.IsNullOrEmpty(ten)) return false;

        SortingLayer[] ds = SortingLayer.layers;
        if (ds == null) return false;

        for (int i = 0; i < ds.Length; i++)
            if (ds[i].name == ten) return true;

        return false;
    }

    /// <summary>
    /// Áp layer + order cho 1 renderer theo bộ ưu tiên. Renderer null thì bỏ qua êm.
    /// Trả về tên layer đã áp (để bên gọi log/kiểm tra).
    /// </summary>
    public static string Apply(Renderer renderer, string[] uuTien, int order)
    {
        string ten = Resolve(uuTien);
        if (renderer == null) return ten;

        renderer.sortingLayerName = ten;
        renderer.sortingOrder     = order;
        return ten;
    }

    /// <summary>
    /// Nếu <paramref name="tenNguoiDungDat"/> có thật thì tôn trọng lựa chọn đó
    /// (Sếp chỉnh tay trên Inspector), không thì giải theo bộ ưu tiên.
    /// Dùng cho các field serialize đã lỡ lưu tên layer cũ trong prefab/scene.
    /// </summary>
    public static string ResolveOrOverride(string tenNguoiDungDat, string[] uuTien)
    {
        if (!string.IsNullOrEmpty(tenNguoiDungDat) && Exists(tenNguoiDungDat))
            return tenNguoiDungDat;

        if (!string.IsNullOrEmpty(tenNguoiDungDat))
            CanhBao("override:" + tenNguoiDungDat,
                $"Sorting layer '{tenNguoiDungDat}' gán trên Inspector KHÔNG tồn tại trong project " +
                "(Unity sẽ im lặng đẩy về Default) — tự chuyển sang layer dự phòng.");

        return Resolve(uuTien);
    }

    private static void CanhBao(string khoa, string thongDiep)
    {
        if (!_daCanhBao.Add(khoa)) return;   // mỗi khoá chỉ kêu 1 lần cho cả phiên
        Debug.LogWarning("[TouristVisitor] " + thongDiep);
    }
}
