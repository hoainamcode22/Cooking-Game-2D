using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SOI POPUP NHIỆM VỤ ĐANG CHẠY.
///
/// Dựng bằng code nên không mở Inspector ra xem được như prefab. Khi hiển thị sai —
/// chữ trống, nền trắng, thiếu hàng — không có cách nào biết là do màu, do sprite, do
/// kích thước, hay do object không tồn tại.
///
/// Tool này in ra cây thật kèm màu, kích thước, tên sprite, nội dung chữ. Đủ để chỉ
/// đúng thủ phạm thay vì đoán.
/// </summary>
public static class TaskPopupInspectTool
{
    private const string Menu = "Tools/Farm/Popup Nhiệm Vụ/";

    [MenuItem(Menu + "9 · Soi popup đang chạy (cần Play Mode)", false, 30)]
    public static void Soi()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Soi popup",
                "Cần đang ở Play Mode và popup đang MỞ.\n\n" +
                "Bấm Play → mở popup Nhiệm vụ → chạy lại tool này.", "OK");
            return;
        }

        var popup = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            Debug.LogError("[SoiPopup] Không thấy UnifiedTaskPopupUI trong scene.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("═══════ CÂY POPUP NHIỆM VỤ ═══════");
        InRa(popup.transform, sb, 0);

        // Chia log ra nhiều mảnh: Console Unity cắt chuỗi quá ~15.000 ký tự, và cắt
        // đúng chỗ quan trọng thì soi cũng bằng không.
        string all = sb.ToString();
        const int mau = 12000;
        for (int i = 0; i < all.Length; i += mau)
            Debug.Log($"[SoiPopup {i / mau + 1}/{(all.Length - 1) / mau + 1}]\n" +
                      all.Substring(i, Mathf.Min(mau, all.Length - i)));
    }

    [MenuItem(Menu + "10 · Đếm hàng thật sự đang hiện", false, 31)]
    public static void DemHang()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Đếm hàng", "Cần Play Mode.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("═══ ĐẾM HÀNG TRONG VÙNG CUỘN ═══\n");

        foreach (var sr in Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
        {
            if (sr == null || sr.content == null) continue;
            if (!sr.name.Contains("Mission") && !sr.name.Contains("Achievement")) continue;

            int tong = sr.content.childCount, bat = 0;
            var caoHang = new StringBuilder();

            for (int i = 0; i < tong; i++)
            {
                Transform c = sr.content.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                bat++;

                if (bat <= 4)
                {
                    var rt = c as RectTransform;
                    var le = c.GetComponent<LayoutElement>();
                    caoHang.AppendLine($"        [{i}] {c.name}  y={rt.anchoredPosition.y:0}  " +
                                       $"cao={rt.rect.height:0}  " +
                                       $"LayoutElement={(le != null ? le.preferredHeight.ToString("0") : "KHÔNG")}  " +
                                       $"chữ đầu=\"{ChuDauTien(c)}\"");
                }
            }

            var content = sr.content;
            sb.AppendLine($"  {sr.name}");
            sb.AppendLine($"     viewport cao : {(sr.viewport != null ? sr.viewport.rect.height : 0):0}");
            sb.AppendLine($"     content cao  : {content.rect.height:0}");
            sb.AppendLine($"     LayoutGroup  : {(content.GetComponent<VerticalLayoutGroup>() != null ? "có" : "KHÔNG")}");
            sb.AppendLine($"     SizeFitter   : {(content.GetComponent<ContentSizeFitter>() != null ? "có" : "KHÔNG ← content không tự cao theo số hàng")}");
            sb.AppendLine($"     con: {tong} object, ĐANG BẬT: {bat}");
            sb.Append(caoHang);
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static string ChuDauTien(Transform t)
    {
        foreach (var tx in t.GetComponentsInChildren<TMP_Text>(true))
            if (tx != null && !string.IsNullOrEmpty(tx.text)) return tx.text;
        return "(TRỐNG)";
    }

    private static void InRa(Transform t, StringBuilder sb, int sau)
    {
        if (sau > 6) return;

        string le = new string(' ', sau * 2);
        string tat = t.gameObject.activeSelf ? "" : " [TẮT]";

        var rt = t as RectTransform;
        string kt = rt != null ? $" {rt.rect.width:0}×{rt.rect.height:0}@({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})" : "";

        string them = "";

        var img = t.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            them += $"  Image[{Hex(c)} a={c.a:0.00} {img.type}" +
                    $" spr={(img.sprite != null ? img.sprite.name : "NULL")}]";
        }

        var tx = t.GetComponent<TMP_Text>();
        if (tx != null)
        {
            them += $"  Text[\"{(string.IsNullOrEmpty(tx.text) ? "TRỐNG" : tx.text)}\" cỡ={tx.fontSize:0}" +
                    $" {Hex(tx.color)} a={tx.color.a:0.00}" +
                    $" font={(tx.font != null ? tx.font.name : "NULL ← không có font!")}]";
        }

        var cg = t.GetComponent<CanvasGroup>();
        if (cg != null) them += $"  CanvasGroup[alpha={cg.alpha:0.00}]";

        sb.AppendLine($"{le}{t.name}{tat}{kt}{them}");

        for (int i = 0; i < t.childCount; i++)
            InRa(t.GetChild(i), sb, sau + 1);
    }

    private static string Hex(Color c)
        => $"#{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
}
