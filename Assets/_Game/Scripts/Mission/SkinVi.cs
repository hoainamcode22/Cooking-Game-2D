using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ĐỒ NGHỀ CHUNG cho vỏ Kho + Hồ sơ (KhoSkin.cs / HoSoSkin.cs).
///
/// Vì sao tách file: Unity chỉ gắn được MonoBehaviour nằm trong file TRÙNG TÊN
/// class. Bản trước nhét SkinVi + KhoSkin + HoSoSkin chung `KhoHoSoSkin.cs` —
/// Unity lấy class đầu file (SkinVi, static) làm chủ file → console la
/// "abstract/ExtensionOfNativeClass" và component gắn vào scene thành missing
/// script sau khi save. Mỗi class một file là hết.
///
/// Vì sao reflection: bản vỏ đầu dò object theo TÊN, scene thật đặt tên khác nên
/// vỏ im lặng không làm gì. Đọc thẳng [SerializeField] của script game thì trúng
/// đúng object thật, bất kể tên gì trong Hierarchy.
/// </summary>
public static class SkinVi
{
    /// <summary>Đọc field private [SerializeField] của script game.</summary>
    public static T Lay<T>(object chu, string ten) where T : class
    {
        if (chu == null) return null;
        FieldInfo f = chu.GetType().GetField(ten,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return f != null ? f.GetValue(chu) as T : null;
    }

    /// <summary>Image diện tích lớn nhất dưới gốc — chính là tấm ván/khung của popup.</summary>
    public static Image TimVanGo(Transform goc)
    {
        Image tot = null; float dtMax = 0f;
        foreach (var i in goc.GetComponentsInChildren<Image>(true))
        {
            if (i == null || i.transform.name.StartsWith("Skin_")) continue;
            var r = ((RectTransform)i.transform).rect;
            float dt = Mathf.Abs(r.width * r.height);
            if (dt > dtMax) { dtMax = dt; tot = i; }
        }
        return dtMax >= 200000f ? tot : null;
    }

    /// <summary>
    /// Nút X: mặc áo đỏ; nếu nút không có chữ con nào (dấu X nằm trong sprite art cũ
    /// vừa bị thay) thì thêm chữ "X" trắng để nút không thành ô đỏ trống.
    /// </summary>
    public static void NutDong(Button nut)
    {
        if (nut == null) return;
        SkinKit.MacAoNut(nut, SkinKit.NutDo, 18f);
        if (nut.GetComponentInChildren<TMP_Text>(true) == null)
        {
            var go = new GameObject("Skin_X", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(nut.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = ((RectTransform)nut.transform).rect.size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 20f; tmp.fontSizeMax = 56f;
            tmp.raycastTarget = false;
        }
    }

    /// <summary>Đưa 1 RectTransform về neo góc/cạnh của cha (đặt lại vị trí tự do).</summary>
    public static void Neo(RectTransform rt, Vector2 anchor, Vector2 pos)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// Mặc áo giấy kem cho MODAL con (chuyển bếp / nâng cấp / thiếu đồ của Kho):
    /// nền giấy, panel con thành thẻ, nút phân màu theo TÊN (Close đỏ · Minus cam ·
    /// Plus/Confirm xanh · Max vàng), chữ ngoài nút thành nâu.
    /// </summary>
    public static void MacModal(GameObject root)
    {
        if (root == null) return;
        Transform goc = root.transform;

        var nen = goc.GetComponent<Image>();
        if (nen != null) SkinKit.MacAoGiay(nen, 26f);

        var anhNut = new System.Collections.Generic.HashSet<Transform>();
        foreach (var nut in goc.GetComponentsInChildren<Button>(true))
        {
            anhNut.Add(nut.transform);
            string t = nut.name.ToLowerInvariant();
            if      (t.Contains("close"))              NutDong(nut);
            else if (t.Contains("minus"))              SkinKit.MacAoNut(nut, SkinKit.NutCam, 26f);
            else if (t.Contains("max"))                SkinKit.MacAoNut(nut, TaskPopupDesign.NutDiLam, 18f);
            else                                       SkinKit.MacAoNut(nut, TaskPopupDesign.NutNhan, 20f);
        }

        // Panel con cỡ vừa → thẻ kem (khung icon, ô cấp, 4 ô nguyên liệu).
        foreach (var img in goc.GetComponentsInChildren<Image>(true))
        {
            if (img == null || img == nen) continue;
            if (img.transform.name.StartsWith("Skin_")) continue;
            if (img.GetComponent<Button>() != null) continue;
            bool trongNut = false;
            for (var p = img.transform.parent; p != null && p != goc; p = p.parent)
                if (anhNut.Contains(p) || p.name.StartsWith("Skin_")) { trongNut = true; break; }
            if (trongNut) continue;

            var r = ((RectTransform)img.transform).rect;
            float dt = Mathf.Abs(r.width * r.height);
            if (dt >= 6000f && dt <= 200000f && img.sprite == null)
                SkinKit.MacAoThe(img, 16f);
        }

        // Chữ ngoài nút → nâu đậm (chữ trên nút đã trắng nhờ MacAoNut).
        foreach (var tmp in goc.GetComponentsInChildren<TMP_Text>(true))
        {
            bool trongNut = false;
            for (var p = tmp.transform.parent; p != null && p != goc.parent; p = p.parent)
                if (anhNut.Contains(p) || p.name.StartsWith("Skin_")) { trongNut = true; break; }
            if (!trongNut) tmp.color = TaskPopupDesign.TenBinhThuong;
        }

        // Modal nằm ngoài popupRoot (con của Canvas_Popup) nên áp font tại đây.
        SkinKit.ApFont(goc);
    }
}
