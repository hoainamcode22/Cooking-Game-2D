using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BA HIỆU ỨNG KHI GIAO HÀNG (B9) — CHẠY CÙNG LÚC, KHÔNG NỐI ĐUÔI.
///
///   1. Cụm KHÓI TRẮNG bung ra đúng vị trí phiếu vừa biến mất
///   2. SAO EXP + ĐỒNG VÀNG bay chéo lên kèm nhãn "+N"
///   3. LƯỚI DỒN LẠI lấp chỗ trống  ← do <c>OrderBoardPopupUI</c> chạy song song
///
/// VÌ SAO PHẢI CÙNG LÚC: nối đuôi ba đoạn 0.3 giây thành 0.9 giây thì mỗi lần giao đơn
/// người chơi phải ngồi chờ gần một giây mới bấm được tiếp. Bảng đơn là nguồn thu LẶP
/// LẠI, người chơi giao hàng chục lần một phiên — chờ ở đây là thuế đánh vào thao tác
/// thường xuyên nhất của cả hệ thống. Chạy chồng lên nhau thì tổng vẫn gọn trong ~0.7s
/// mà vẫn đọc được rõ "mất cái gì, được cái gì".
///
/// TOÀN BỘ hạt hiệu ứng được Editor tool dựng sẵn trong prefab rồi bật/tắt. KHÔNG
/// <c>Instantiate</c>, KHÔNG <c>new GameObject()</c> lúc chạy: cấp phát giữa lúc animation
/// đang chạy là nguồn giật khung hình kinh điển trên máy yếu, mà đây lại là hiệu ứng
/// bắn ra nhiều lần nhất trong game.
/// </summary>
public class OrderDeliverFxUI : MonoBehaviour
{
    [Header("Gốc toạ độ — cùng hệ với lưới phiếu")]
    [Tooltip("Phải là RectTransform CÙNG CHA với các phiếu, nếu không khói bung sai chỗ.")]
    [SerializeField] private RectTransform fxRoot;

    [Header("1 · Khói trắng (dựng sẵn trong prefab)")]
    [SerializeField] private Image[] smokePuffs;

    [Header("2 · Sao EXP + đồng vàng bay lên")]
    [SerializeField] private Image[] flyIcons;
    [SerializeField] private TMP_Text labelExp;
    [SerializeField] private TMP_Text labelGold;

    [Header("Nhịp")]
    // Con số này phải KHỚP với `OrderBoardPopupUI.reflowSeconds`. Ba hiệu ứng khởi động
    // cùng khung hình, nhưng nếu thời lượng lệch thì lưới dồn xong từ lâu mà khói còn bay —
    // mất hẳn cảm giác "một nhịp dứt khoát" mà video tạo ra.
    [Tooltip("Tổng thời gian. Phải khớp với reflowSeconds bên OrderBoardPopupUI.")]
    [SerializeField] private float duration = 0.42f;
    [SerializeField] private float smokeSpread = 74f;
    [SerializeField] private float flyRise = 190f;
    [SerializeField] private float labelRise = 130f;

    private Coroutine _routine;

    /// <summary>
    /// Dọn hạt khi bị tắt.
    ///
    /// 🔴 BẮT BUỘC PHẢI CÓ. Người chơi bấm GIAO HÀNG rồi đóng popup ngay trong 0,72 giây
    /// là chuyện thường xuyên. Lúc đó `SetActive(false)` popup khiến Unity **giết
    /// coroutine giữa chừng**, nên `HideAll()` ở cuối `PlayRoutine` không bao giờ chạy.
    /// Khói, xu và nhãn "+N" nằm lại với alpha dở dang, và **lần mở popup sau sẽ thấy
    /// chúng đứng im giữa lưới** — đúng cái bẫy mà chú thích ở Awake đã cảnh báo,
    /// chỉ là Awake chỉ chạy một lần nên không cứu được lần thứ hai trở đi.
    /// </summary>
    private void OnDisable()
    {
        _routine = null;   // coroutine đã bị Unity huỷ, giữ tham chiếu chỉ gây hiểu nhầm
        HideAll();
    }

    private void Awake()
    {
        // Tắt hết ngay từ đầu: prefab được lưu ở trạng thái "đang chạy dở" thì lần mở
        // popup đầu tiên sẽ thấy một cụm khói đứng im giữa lưới.
        HideAll();

        if (fxRoot == null) fxRoot = transform as RectTransform;

        // Cảnh báo sớm còn hơn để hiệu ứng bung lệch mà không ai hiểu vì sao: toạ độ
        // truyền vào Play() là toạ độ trong lưới phiếu, nên fxRoot BẮT BUỘC phải trùng
        // khít với RectTransform của lưới.
        if (fxRoot == null)
            Debug.LogWarning("[BảngĐơn] OrderDeliverFxUI thiếu fxRoot — hiệu ứng giao hàng " +
                             "sẽ bung sai vị trí.");
    }

    /// <summary>
    /// Bắn hiệu ứng tại <paramref name="anchoredPos"/> (toạ độ trong <c>fxRoot</c>).
    /// Popup gọi hàm này ĐỒNG THỜI với lúc bắt đầu dồn lưới.
    /// </summary>
    public void Play(Vector2 anchoredPos, int exp, int gold)
    {
        if (!isActiveAndEnabled) return;

        // Giao đơn thứ hai khi hiệu ứng cũ chưa xong: cắt cái cũ rồi chạy lại từ đầu.
        // Để hai coroutine cùng ghi vào một bộ hạt thì chúng đá nhau, hạt nhấp nháy.
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(anchoredPos, exp, gold));
    }

    private IEnumerator PlayRoutine(Vector2 center, int exp, int gold)
    {
        int   smokeCount = smokePuffs != null ? smokePuffs.Length : 0;
        int   flyCount   = flyIcons   != null ? flyIcons.Length   : 0;
        float total      = Mathf.Max(0.05f, duration);

        // ── Đặt trạng thái ban đầu ───────────────────────────────────────────
        for (int i = 0; i < smokeCount; i++)
        {
            Image p = smokePuffs[i];
            if (p == null) continue;
            p.gameObject.SetActive(true);
            p.rectTransform.anchoredPosition = center;
            p.rectTransform.localScale = Vector3.one * 0.35f;
        }

        for (int i = 0; i < flyCount; i++)
        {
            Image f = flyIcons[i];
            if (f == null) continue;
            f.gameObject.SetActive(true);
            f.rectTransform.anchoredPosition = center;
            f.rectTransform.localScale = Vector3.one * 0.7f;
        }

        SetupLabel(labelExp,  center + new Vector2(-62f, 26f), exp  > 0 ? "+" + exp  : null);
        SetupLabel(labelGold, center + new Vector2( 62f, 26f), gold > 0 ? "+" + gold : null);

        // ── Một vòng lặp duy nhất điều khiển CẢ BA ───────────────────────────
        float elapsed = 0f;
        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;   // popup vẫn chạy khi game tạm dừng
            float t = Mathf.Clamp01(elapsed / total);

            // 1 · khói: toả tròn đều, phình to rồi tan
            float smokeEase = 1f - (1f - t) * (1f - t);            // ease-out
            for (int i = 0; i < smokeCount; i++)
            {
                Image p = smokePuffs[i];
                if (p == null) continue;

                float ang = (i / Mathf.Max(1f, smokeCount)) * Mathf.PI * 2f;
                float r   = smokeSpread * (0.55f + 0.45f * (i % 3) / 2f);

                p.rectTransform.anchoredPosition =
                    center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * smokeEase;
                p.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.45f, smokeEase);
                SetAlpha(p, 1f - t * t);
            }

            // 2 · sao + vàng: bay chéo lên bùng nổ, xoay tròn
            float flyEase  = 1f - Mathf.Pow(1f - t, 3f);
            float flyAlpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            
            // Hiệu ứng Bùng nổ (Punch Scale)
            float burstScale = 1.2f;
            if (t < 0.3f)
            {
                // Nảy bùng cực to lúc đầu (0 -> 0.3)
                burstScale = Mathf.Lerp(0.5f, 2.5f, Mathf.Sin(t / 0.3f * Mathf.PI / 2f));
            }
            else
            {
                // Thu nhỏ dần về kích thước bay
                burstScale = Mathf.Lerp(2.5f, 1.4f, (t - 0.3f) / 0.7f);
            }

            for (int i = 0; i < flyCount; i++)
            {
                Image f = flyIcons[i];
                if (f == null) continue;

                float side = (i % 2 == 0) ? -1f : 1f;
                float lane = 45f + (i / 2) * 35f;

                f.rectTransform.anchoredPosition =
                    center + new Vector2(side * lane * flyEase, flyRise * flyEase);
                f.rectTransform.localScale = Vector3.one * burstScale;
                
                // Thêm độ xoay (Rotate) bùng nổ
                float rot = Mathf.Lerp(0f, 360f * side, flyEase);
                f.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                
                SetAlpha(f, flyAlpha);
            }

            // nhãn "+N": bay lên chậm hơn hạt, để mắt kịp đọc con số
            float labelEase = Mathf.Sqrt(t);
            MoveLabel(labelExp,  center + new Vector2(-62f, 26f + labelRise * labelEase), flyAlpha);
            MoveLabel(labelGold, center + new Vector2( 62f, 26f + labelRise * labelEase), flyAlpha);

            yield return null;
        }

        HideAll();
        _routine = null;
    }

    private void SetupLabel(TMP_Text label, Vector2 pos, string content)
    {
        if (label == null) return;

        // content null = phần thưởng bằng 0 → không hiện nhãn. Hiện "+0" thì người chơi
        // tưởng giao hụt.
        if (string.IsNullOrEmpty(content))
        {
            label.gameObject.SetActive(false);
            return;
        }

        label.gameObject.SetActive(true);
        label.text = content;
        label.rectTransform.anchoredPosition = pos;
        SetAlpha(label, 1f);
    }

    private static void MoveLabel(TMP_Text label, Vector2 pos, float alpha)
    {
        if (label == null || !label.gameObject.activeSelf) return;
        label.rectTransform.anchoredPosition = pos;
        SetAlpha(label, alpha);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = Mathf.Clamp01(a);
        g.color = c;
    }

    private void HideAll()
    {
        if (smokePuffs != null)
            for (int i = 0; i < smokePuffs.Length; i++)
                if (smokePuffs[i] != null) smokePuffs[i].gameObject.SetActive(false);

        if (flyIcons != null)
            for (int i = 0; i < flyIcons.Length; i++)
                if (flyIcons[i] != null) flyIcons[i].gameObject.SetActive(false);

        if (labelExp  != null) labelExp.gameObject.SetActive(false);
        if (labelGold != null) labelGold.gameObject.SetActive(false);
    }
}
