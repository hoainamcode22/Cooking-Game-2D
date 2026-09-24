// ============================================================================
//  KitchenOrderRequest — "phieu dat mon" chuyen tu Farm sang Bep.
//  Bubble khach du lich bam "Nau ngay" -> Dat(dishId) -> vao bep, KitchenSceneV2UI.PickDefaultDish
//  goi LayVaXoa() de chon san dung mon khach doi. Phieu het han sau 120 giay (gio thuc) de
//  lan vao bep sau (khong qua bubble) khong bi chon nham mon cu.
// ============================================================================
using UnityEngine;

public static class KitchenOrderRequest
{
    private const float HAN_GIAY = 120f;
    private static string _dishId;
    private static float _luc = -999f;

    public static void Dat(string dishId)
    {
        _dishId = dishId;
        _luc = Time.realtimeSinceStartup;
    }

    /// <summary>Lay ma mon dang dat (con han) va xoa phieu. Khong co -> null.</summary>
    public static string LayVaXoa()
    {
        string id = _dishId;
        bool conHan = Time.realtimeSinceStartup - _luc <= HAN_GIAY;
        _dishId = null;
        return conHan && !string.IsNullOrEmpty(id) ? id : null;
    }
}
