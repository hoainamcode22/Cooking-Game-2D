using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [2026-09-21] Kẹp popup cho LỌT MÀN HÌNH lúc mở — dùng chung cho Shop / Bảng đơn hàng.
///
/// Đo DẤU CHÂN THẬT của bảng (mọi RectTransform con đang bật, kể cả ruy-băng tiêu đề
/// Header_Banner thò lên trên mép bảng và nút X thò sang phải; DỪNG tại node có
/// Mask/RectMask2D để không đếm nội dung ScrollRect bị xén) quy về ĐƠN VỊ CANVAS GỐC,
/// rồi so với rect canvas (ưu tiên rect lớp "~SafeArea" nếu popup nằm trong đó) trừ lề:
///   1. Ưu tiên DỊCH (anchoredPosition) cho mép bị cắt lọt vào trong.
///   2. Chỉ khi dấu chân TO HƠN khung mới CO (localScale, ≤ 1, không bao giờ phóng to),
///      co xong dịch lại lần nữa.
///
/// Người gọi nhớ scale/vị trí GỐC ở lần mở đầu và truyền vào mỗi lần — hàm luôn đặt lại
/// gốc trước khi đo nên mở đi mở lại không dồn nén / không trôi.
/// </summary>
public static class PopupFitClamp
{
    private static readonly Vector3[] _goc = new Vector3[4];

    /// <summary>Lề an toàn mặc định (đơn vị canvas).</summary>
    public const float LE_MAC_DINH = 16f;

    /// <param name="rtBang">Tấm bảng (KHÔNG phải nền mờ phủ kín màn).</param>
    /// <param name="scaleGoc">localScale gốc lưu ở lần mở đầu.</param>
    /// <param name="viTriGoc">anchoredPosition gốc lưu ở lần mở đầu.</param>
    /// <param name="le">Lề chừa mỗi mép canvas.</param>
    /// <returns>true nếu đã phải dịch hoặc co.</returns>
    public static bool VuaKhung(RectTransform rtBang, Vector3 scaleGoc, Vector2 viTriGoc, float le)
    {
        if (rtBang == null) return false;

        RectTransform rtKhung = rtBang.parent as RectTransform;
        if (rtKhung == null) return false;

        Canvas cv = rtBang.GetComponentInParent<Canvas>();
        if (cv == null) return false;
        RectTransform rtCanvas = (cv.rootCanvas != null ? cv.rootCanvas : cv).transform as RectTransform;
        if (rtCanvas == null) return false;

        // [2026-09-21] SafeAreaBootstrap bọc mọi con của Canvas vào lớp "~SafeArea" (SafeAreaFitter).
        // Popup nằm TRONG lớp bọc ⇒ khung để kẹp phải là rect vùng an toàn (cha gần nhất có
        // SafeAreaFitter), không phải rect Canvas gốc — nếu không popup vẫn có thể lọt xuống tai thỏ.
        // Lớp bọc scale 1 so với Canvas nên đơn vị đo không đổi.
        SafeAreaFitter saf = rtBang.GetComponentInParent<SafeAreaFitter>();
        if (saf != null)
        {
            RectTransform rtSafe = saf.transform as RectTransform;
            if (rtSafe != null && rtSafe != rtBang && rtSafe.rect.width > 1f && rtSafe.rect.height > 1f)
                rtCanvas = rtSafe;
        }

        // Về nguyên bản trước khi đo.
        rtBang.localScale       = scaleGoc;
        rtBang.anchoredPosition = viTriGoc;

        // Ép layout chạy: ô/thẻ vừa Instantiate còn ở rect prefab, GetWorldCorners trả số cũ.
        Canvas.ForceUpdateCanvases();

        Vector2 min, max;
        if (!DoDauChan(rtBang, rtCanvas, out min, out max)) return false;

        float rong = max.x - min.x;
        float cao  = max.y - min.y;
        if (rong < 1f || cao < 1f) return false;

        Rect kh = rtCanvas.rect;
        float xMin = kh.xMin + le, xMax = kh.xMax - le;
        float yMin = kh.yMin + le, yMax = kh.yMax - le;
        if (xMax - xMin < 1f || yMax - yMin < 1f) return false;

        // Bước CO (chỉ khi dấu chân to hơn khung — dịch kiểu gì cũng không lọt).
        float heSo = Mathf.Min(1f, Mathf.Min((xMax - xMin) / rong, (yMax - yMin) / cao));
        bool daCo = heSo < 0.999f;
        if (daCo)
        {
            // Co quanh pivot của bảng ⇒ dấu chân co về phía pivot theo cùng hệ số.
            Vector2 pivotC = (Vector2)rtCanvas.InverseTransformPoint(rtBang.position);
            min = pivotC + (min - pivotC) * heSo;
            max = pivotC + (max - pivotC) * heSo;
            rtBang.localScale = scaleGoc * heSo;
        }

        // Bước DỊCH: mép trên/phải bị cắt ⇒ kéo vào; ngược lại mép dưới/trái.
        float dx = 0f, dy = 0f;
        if      (max.y > yMax) dy = yMax - max.y;
        else if (min.y < yMin) dy = yMin - min.y;
        if      (max.x > xMax) dx = xMax - max.x;
        else if (min.x < xMin) dx = xMin - min.x;

        bool daDich = Mathf.Abs(dx) > 0.5f || Mathf.Abs(dy) > 0.5f;
        if (!daDich) return daCo;

        // 1 đơn vị anchoredPosition của bảng = (lossyKhung / lossyCanvas) đơn vị canvas.
        Vector3 lsKhung  = rtKhung.lossyScale;
        Vector3 lsCanvas = rtCanvas.lossyScale;
        if (Mathf.Abs(lsCanvas.x) < 1e-5f || Mathf.Abs(lsCanvas.y) < 1e-5f) return daCo;
        float kx = lsKhung.x / lsCanvas.x;
        float ky = lsKhung.y / lsCanvas.y;
        if (Mathf.Abs(kx) < 1e-5f || Mathf.Abs(ky) < 1e-5f) return daCo;

        rtBang.anchoredPosition = rtBang.anchoredPosition + new Vector2(dx / kx, dy / ky);
        return true;
    }

    /// <summary>Dấu chân của <paramref name="bang"/> trong KHÔNG GIAN CỤC BỘ CỦA CANVAS GỐC.</summary>
    private static bool DoDauChan(RectTransform bang, RectTransform rtCanvas, out Vector2 min, out Vector2 max)
    {
        Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 mx = new Vector3(float.MinValue, float.MinValue, 0f);
        Gom(rtCanvas, bang, ref mn, ref mx);
        min = mn; max = mx;
        return mn.x <= mx.x && mn.y <= mx.y;
    }

    private static void Gom(RectTransform rtCanvas, RectTransform node, ref Vector3 min, ref Vector3 max)
    {
        // So tường minh `== null`: component đã Destroy trả "fake-null".
        if (node == null || !node.gameObject.activeSelf) return;

        node.GetWorldCorners(_goc);
        for (int i = 0; i < 4; i++)
        {
            Vector3 c = rtCanvas.InverseTransformPoint(_goc[i]);
            if (c.x < min.x) min.x = c.x;
            if (c.y < min.y) min.y = c.y;
            if (c.x > max.x) max.x = c.x;
            if (c.y > max.y) max.y = c.y;
        }

        // Node CẮT ⇒ con của nó bị xén, không tính.
        if (node.GetComponent<RectMask2D>() != null) return;
        if (node.GetComponent<Mask>() != null) return;

        for (int i = 0; i < node.childCount; i++)
            Gom(rtCanvas, node.GetChild(i) as RectTransform, ref min, ref max);
    }
}
