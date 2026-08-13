using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// THAY ÁO 3 POPUP: KHO VẬT PHẨM · HỒ SƠ AVATAR · SHOP — theo bộ thiết kế
/// `Export_Popups_Chon` (chỉ diện mạo, logic và dữ liệu giữ nguyên).
///
/// Cách hoạt động:
///   1. Tìm root của từng popup BẰNG COMPONENT (WarehousePopupUI / AvatarProfilePopupUI
///      / ShopManager) — không dò theo tên nên không sợ ai đó đổi tên object.
///   2. Quét hierarchy, PHÂN LOẠI bề mặt theo kích thước & vai trò, điền vào
///      `PopupSkinApplier` (gắn mới nếu chưa có).
///   3. In báo cáo từng popup: cái gì sẽ thành ván gỗ / giấy / thẻ / nút.
///   4. BẠN duyệt trong Inspector (gạch bỏ được từng dòng) → Play để xem áo mới.
///
/// KHÔNG sửa gì lúc Editor ngoài việc gắn component + điền danh sách — bề mặt chỉ đổi
/// lúc Play, nên Ctrl+Z / gỡ component là về nguyên trạng.
/// </summary>
public static class PopupSkinTool
{
    private const string Menu = "Tools/Farm/Thay Áo Popup/";

    [MenuItem(Menu + "1 · Gắn + phân loại cho CẢ BA popup", false, 1)]
    public static void GanCaBa()
    {
        int ok = 0;
        ok += GanCho<WarehousePopupUI>("Kho Vật Phẩm") ? 1 : 0;
        ok += GanCho<AvatarProfilePopupUI>("Hồ Sơ Avatar") ? 1 : 0;
        ok += GanCho<ShopManager>("Shop") ? 1 : 0;

        Debug.Log($"[ThayÁo] Xong {ok}/3 popup. Mở Inspector từng root để duyệt danh sách, " +
                  "rồi Play xem áo mới. Không ưng popup nào thì bỏ tick 'Bật Áo' trên nó.");
    }

    [MenuItem(Menu + "2 · Chỉ Kho Vật Phẩm", false, 10)]
    public static void GanKho() => GanCho<WarehousePopupUI>("Kho Vật Phẩm");

    [MenuItem(Menu + "3 · Chỉ Hồ Sơ Avatar", false, 11)]
    public static void GanHoSo() => GanCho<AvatarProfilePopupUI>("Hồ Sơ Avatar");

    [MenuItem(Menu + "4 · Chỉ Shop", false, 12)]
    public static void GanShop() => GanCho<ShopManager>("Shop");

    [MenuItem(Menu + "9 · Gỡ áo khỏi cả ba (về nguyên trạng)", false, 30)]
    public static void GoHet()
    {
        int n = 0;
        foreach (var a in Object.FindObjectsByType<PopupSkinApplier>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.DestroyObjectImmediate(a);
            n++;
        }
        Debug.Log($"[ThayÁo] Đã gỡ {n} component. Lớp trang trí Skin_* chỉ sinh lúc Play " +
                  "nên scene không còn dấu vết gì.");
    }

    // ═════════════════════════════════════════════════════════════════════════

    private static bool GanCho<T>(string ten) where T : MonoBehaviour
    {
        var chuNha = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (chuNha == null)
        {
            Debug.LogWarning($"[ThayÁo] Không thấy {typeof(T).Name} trong scene — bỏ qua {ten}.");
            return false;
        }

        // Root diện mạo = object mang script, hoặc panel to nhất nó tham chiếu.
        GameObject goc = chuNha.gameObject;

        var ao = goc.GetComponent<PopupSkinApplier>();
        if (ao == null)
        {
            ao = Undo.AddComponent<PopupSkinApplier>(goc);
        }

        PhanLoai(goc.transform, ao, ten);
        EditorUtility.SetDirty(ao);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(goc.scene);
        return true;
    }

    /// <summary>
    /// Phân loại bề mặt theo DIỆN TÍCH và vai trò:
    ///   • Image to nhất (và ≥ 55% màn) → ván gỗ
    ///   • Image ≥ 40% ván gỗ           → giấy
    ///   • Image có Button              → vào danh sách nút (áo nút lo riêng)
    ///   • Image 15k–200k px², KHÔNG phải icon (không giữ sprite art tỉ lệ lạ) → thẻ
    /// Icon và art thật (sprite từ Assetsgame) được NHẬN DIỆN và bỏ qua — thay áo
    /// không được đè lên hình vẽ tay của chủ dự án.
    /// </summary>
    private static void PhanLoai(Transform goc, PopupSkinApplier ao, string ten)
    {
        ao.vanGo.Clear(); ao.giay.Clear(); ao.the.Clear(); ao.nut.Clear();

        var tatCaAnh = new List<Image>(goc.GetComponentsInChildren<Image>(true));
        var tatCaNut = goc.GetComponentsInChildren<Button>(true);

        // Nút: nhận hết, phân màu lúc chạy. Image của nút loại khỏi danh sách bề mặt.
        var anhCuaNut = new HashSet<Image>();
        foreach (var b in tatCaNut)
        {
            ao.nut.Add(b);
            if (b.image != null) anhCuaNut.Add(b.image);
        }

        // Tìm ván gỗ: Image diện tích lớn nhất.
        Image lonNhat = null; float dtLonNhat = 0f;
        foreach (var i in tatCaAnh)
        {
            if (i == null || anhCuaNut.Contains(i)) continue;
            var r = ((RectTransform)i.transform).rect;
            float dt = Mathf.Abs(r.width * r.height);
            if (dt > dtLonNhat) { dtLonNhat = dt; lonNhat = i; }
        }

        var sb = new StringBuilder($"═══ THAY ÁO · {ten} ═══\n");

        if (lonNhat != null && dtLonNhat >= 300000f)
        {
            ao.vanGo.Add(lonNhat);
            sb.AppendLine($"  ván gỗ : {DuongDan(lonNhat.transform, goc)}  ({dtLonNhat:0} px²)");
        }

        foreach (var i in tatCaAnh)
        {
            if (i == null || i == lonNhat || anhCuaNut.Contains(i)) continue;
            if (i.transform.name.StartsWith("Skin_")) continue;

            var r = ((RectTransform)i.transform).rect;
            float dt = Mathf.Abs(r.width * r.height);

            // Icon/art thật: sprite có tên (không phải sprite built-in trắng) VÀ
            // preserveAspect hoặc tỉ lệ gần vuông nhỏ → giữ nguyên, không đè.
            bool laArt = i.sprite != null && i.sprite.name != "Background" &&
                         i.sprite.name != "UISprite" && i.sprite.name != "" &&
                         (i.preserveAspect || dt < 15000f);
            if (laArt) continue;

            if (dtLonNhat > 0f && dt >= dtLonNhat * 0.4f)
            {
                ao.giay.Add(i);
                sb.AppendLine($"  giấy   : {DuongDan(i.transform, goc)}");
            }
            else if (dt >= 15000f && dt <= 200000f)
            {
                ao.the.Add(i);
                sb.AppendLine($"  thẻ    : {DuongDan(i.transform, goc)}");
            }
        }

        sb.AppendLine($"  nút    : {ao.nut.Count} cái (phân màu theo màu hiện có lúc Play)");
        sb.AppendLine($"\n  → Duyệt/gạch bỏ trong Inspector của '{goc.name}', rồi Play.");
        Debug.Log(sb.ToString());
    }

    private static string DuongDan(Transform t, Transform goc)
    {
        string s = t.name;
        while (t.parent != null && t.parent != goc) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}

/// <summary>
/// XUẤT CÂY 3 POPUP — bước bắt buộc trước khi dựng lại vỏ cho giống mock 100%.
///
/// Lần thay áo tại chỗ thất bại vì BỐ CỤC chính là bản thiết kế: giữ bố cục cũ thì
/// đổi màu mấy cũng không giống. Muốn dựng lại vỏ mà không đứt logic, phải biết chính
/// xác object nào đang tồn tại, tên gì, to bao nhiêu, mang chữ/ảnh gì — tool này in
/// ra đúng bản đồ đó để đội dev làm việc trên sự thật thay vì đoán.
/// </summary>
public static class PopupHierarchyDumpTool
{
    [MenuItem("Tools/Farm/Thay Áo Popup/8 · Xuất cây 3 popup (gửi cho dev)", false, 20)]
    public static void XuatCay()
    {
        Xuat<WarehousePopupUI>("KHO VẬT PHẨM");
        Xuat<AvatarProfilePopupUI>("HỒ SƠ AVATAR");
        Xuat<ShopManager>("SHOP");
        Debug.Log("[XuấtCây] Xong — copy 3 log trên gửi cho đội dựng lại vỏ.");
    }

    private static void Xuat<T>(string ten) where T : MonoBehaviour
    {
        var c = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (c == null) { Debug.LogWarning($"[XuấtCây] Không thấy {typeof(T).Name}"); return; }

        var sb = new StringBuilder($"═══ CÂY · {ten} ({typeof(T).Name}) ═══\n");
        In(c.transform, sb, 0);

        string all = sb.ToString();
        const int mau = 12000;   // Console cắt chuỗi dài — chia mảnh
        for (int i = 0; i < all.Length; i += mau)
            Debug.Log($"[XuấtCây {ten} {i / mau + 1}]\n" + all.Substring(i, Mathf.Min(mau, all.Length - i)));
    }

    private static void In(Transform t, StringBuilder sb, int sau)
    {
        if (sau > 7) return;
        if (t.name.StartsWith("Skin_")) return;   // bỏ lớp áo tạm

        var rt = t as RectTransform;
        string kt = rt != null ? $" {rt.rect.width:0}×{rt.rect.height:0}" : "";

        string them = "";
        var img = t.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            them += $" Img[{(img.sprite != null ? img.sprite.name : "null")}]";
        var tx = t.GetComponent<TMPro.TMP_Text>();
        if (tx != null)
            them += $" Txt[\"{(string.IsNullOrEmpty(tx.text) ? "" : tx.text.Substring(0, Mathf.Min(24, tx.text.Length)))}\"]";
        if (t.GetComponent<UnityEngine.UI.Button>() != null) them += " [BUTTON]";
        if (!t.gameObject.activeSelf) them += " [TẮT]";

        sb.AppendLine($"{new string(' ', sau * 2)}{t.name}{kt}{them}");
        for (int i = 0; i < t.childCount; i++) In(t.GetChild(i), sb, sau + 1);
    }
}
