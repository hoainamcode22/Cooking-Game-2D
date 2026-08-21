using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BĂNG TẢI SỌC CHẠY — cuộn texture của một <see cref="RawImage"/> theo trục U/V.
///
/// ══ TƯƠNG ỨNG 1–1 VỚI BẢN THIẾT KẾ ══
/// `full_mill_ui.html`:
///     .conveyor-stripes-anim {
///         background-image: repeating-linear-gradient(-45deg, transparent 0→15px, #2A1D15 15px→30px);
///         animation: scrollBelt 1s linear infinite;
///     }
///     @keyframes scrollBelt { to { transform: translateX(-42px); } }
/// ⇒ hoa văn lặp mỗi 30px, trôi sang TRÁI 42px mỗi giây.
///
/// CSS dịch cả DIV con rộng 200% rồi cho cha `overflow:hidden`. Unity làm cách khác:
/// giữ RawImage đứng yên và dịch CỬA SỔ UV. Kết quả trên màn hình y hệt, nhưng KHÔNG
/// cần object con, KHÔNG cần Mask (Mask thêm 1 lượt stencil + 1 draw call).
///
/// ══ VÌ SAO RawImage CHỨ KHÔNG Image ══
/// `Image` KHÔNG có `uvRect` — nó vẽ theo Sprite (và sprite trong atlas thì UV bị atlas
/// hoá, không cuộn được). `RawImage.uvRect` cho phép đặt trực tiếp offset/scale UV, đó là
/// cách duy nhất cuộn texture trong UGUI mà không cần material riêng hay shader riêng.
/// ⇒ Texture gán vào RawImage BẮT BUỘC để Wrap Mode = Repeat, nếu để Clamp thì
///   phần tràn ra bị kéo giãn thành vệt màu (đây là lỗi hay gặp nhất khi wire cái này).
///
/// ══ CHỐNG TRÀN FLOAT ══
/// `uvRect.x` bị bọc về [0,1) mỗi frame. Popup có thể mở hàng giờ; nếu cộng dồn mãi thì
/// tới cỡ 1e7 bước cộng nhỏ hơn epsilon của float ⇒ băng tải ĐỨNG YÊN dù code vẫn chạy.
/// Bọc lại là bắt buộc, không phải tối ưu cho vui.
/// </summary>
[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public class UIScrollingTexture : MonoBehaviour
{
    [Tooltip("Tốc độ cuộn, PIXEL MỖI GIÂY. Số dương = trôi sang TRÁI (đúng chiều băng tải trong video).\n" +
             "MillPopupUI ghi đè bằng MillConfig.beltScrollPxPerSec (= 42).")]
    public float pixelsPerSecond = 42f;

    [Tooltip("Chu kỳ hoa văn tính bằng pixel (HTML: 30px).\n" +
             "Chỉ dùng khi BẬT dungChuKyHoaVan. Để nguyên 30 nếu không chắc.")]
    public float stripePeriodPx = 30f;

    [Tooltip("TẮT (mặc định): quy chiếu theo chiều rộng THẬT của texture — đúng khi texture " +
             "chứa trọn một chu kỳ hoa văn.\n" +
             "BẬT: quy chiếu theo stripePeriodPx — dùng khi texture chứa NHIỀU chu kỳ " +
             "(vd ảnh 120px chứa 4 sọc) thì mới ra đúng 42px/giây trên màn hình.")]
    public bool dungChuKyHoaVan = false;

    [Tooltip("Cuộn theo trục dọc thay vì ngang. Băng tải trong video là NGANG nên để TẮT.")]
    public bool cuonTheoTrucDoc = false;

    [Tooltip("Có chạy ngay khi bật object. MillPopupUI điều khiển qua SetRunning() nên để TẮT.")]
    public bool autoStart = false;

    /// <summary>Đang cuộn hay không.</summary>
    public bool IsRunning => _running;

    private RawImage _raw;
    private bool     _running;
    private bool     _daCanhBaoThieuTexture;

    private void Awake()
    {
        _raw     = GetComponent<RawImage>();
        _running = autoStart;
    }

    private void Update()
    {
        if (!_running || _raw == null) return;

        // Số pixel dùng làm mốc "một vòng UV". Sai số này quyết định tốc độ thấy được.
        float pixelMotVongUV = LayPixelMotVongUV();
        if (pixelMotVongUV <= 0f) return;   // chưa gán texture — đã cảnh báo ở LayPixelMotVongUV

        // px/giây ÷ px/vòng = vòng/giây, nhân dt ra số vòng UV của frame này.
        float buoc = (pixelsPerSecond / pixelMotVongUV) * Time.deltaTime;

        Rect uv = _raw.uvRect;

        if (cuonTheoTrucDoc)
            uv.y = Mathf.Repeat(uv.y + buoc, 1f);
        else
            uv.x = Mathf.Repeat(uv.x + buoc, 1f);

        // Rect là struct ⇒ phải gán lại, sửa `uv` tại chỗ không ảnh hưởng RawImage.
        _raw.uvRect = uv;
    }

    /// <summary>
    /// Bật/tắt cuộn. Tắt thì hoa văn ĐỨNG YÊN tại chỗ (không reset về 0) — giống lúc
    /// máy dừng giữa ca, băng tải không "giật" về đầu.
    /// </summary>
    public void SetRunning(bool on) => _running = on;

    /// <summary>Đặt lại UV về gốc. Chỉ cần khi muốn máy khởi động lại từ đầu.</summary>
    public void ResetUV()
    {
        if (_raw == null) return;

        Rect uv = _raw.uvRect;
        uv.x = 0f;
        uv.y = 0f;
        _raw.uvRect = uv;
    }

    private float LayPixelMotVongUV()
    {
        if (dungChuKyHoaVan)
            return stripePeriodPx > 0f ? stripePeriodPx : 0f;

        Texture tex = _raw.texture;
        if (tex == null)
        {
            if (!_daCanhBaoThieuTexture)
            {
                _daCanhBaoThieuTexture = true;
                Debug.LogWarning("[MILL] UIScrollingTexture trên '" + name + "': RawImage chưa có Texture " +
                                 "⇒ băng tải không chạy. Gán ảnh sọc và đặt Wrap Mode = Repeat.", this);
            }
            return 0f;
        }

        float w = cuonTheoTrucDoc ? tex.height : tex.width;
        return w > 0f ? w : 0f;
    }

    /// <summary>Áp cấu hình từ <see cref="MillConfig"/>. Gọi từ MillPopupUI lúc Open().</summary>
    public void Configure(float pxPerSec, float chuKyPx)
    {
        pixelsPerSecond = pxPerSec;
        stripePeriodPx  = chuKyPx;
    }
}
