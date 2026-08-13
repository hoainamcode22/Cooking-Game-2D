using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPRITE THỦ TỤC CHO BỘ KIT ĐẶT CÔNG TRÌNH.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO VẼ BẰNG CODE
/// ══════════════════════════════════════════════════════════════════════════
/// Chủ dự án sẽ tự vẽ art rồi gắn đè vào sau. Cho tới lúc đó, mọi ô chờ art phải
/// có sẵn HÌNH TẠM để nhìn thấy được bố cục và chỉnh vị trí — nếu để `sprite = null`
/// thì SpriteRenderer im lặng không vẽ gì, và không ai biết khung đang đặt đúng chưa.
///
/// ══════════════════════════════════════════════════════════════════════════
///  CỐ Ý VẼ KHÁC BẢN THAM CHIẾU
/// ══════════════════════════════════════════════════════════════════════════
/// Bản tham chiếu dùng: hình thoi TÔ ĐẶC nửa trong suốt + 4 NÊM TAM GIÁC ĐẶC đặt ở
/// GIỮA CẠNH. Bộ này cố tình đi hướng khác về mặt hình học:
///
///   • Thảm nền   → hình thoi BO GÓC, RỖNG RUỘT, sáng dần ra mép (gradient viền)
///                  thay vì tô đặc phẳng.
///   • Dấu góc    → NGOẶC CHỮ L ôm 4 GÓC (kiểu khung ngắm máy ảnh),
///                  không phải nêm tam giác ở giữa cạnh.
///   • Viền       → NÉT ĐỨT chạy dọc cạnh, không phải đường liền.
///   • Bảng màu   → xanh ngọc (#5FD9A8) / san hô (#FF7A66),
///                  không phải xanh lá chanh của bản tham chiếu.
///
/// Bốn khác biệt đó nằm ở hình dạng và bảng màu — phần được bảo hộ — trong khi vẫn
/// giữ nguyên CHỨC NĂNG (báo vùng chiếm, báo hợp lệ/không hợp lệ), thứ không ai độc
/// quyền được.
///
/// Mọi sprite đều là ẢNH TRẮNG/XÁM để `SpriteRenderer.color` nhuộm được. Vẽ sẵn màu
/// vào texture thì lúc chuyển xanh↔đỏ sẽ ra màu bùn.
/// </summary>
public static class PlacementKitSpriteFactory
{
    private const int PPU = 100;
    private static readonly Dictionary<string, Sprite> _kho = new Dictionary<string, Sprite>();

    // ═════════════════════════════════════════════════════════════════════════
    //  API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Thảm hình thoi bo góc, rỗng ruột, sáng dần ra mép.</summary>
    public static Sprite ThamHinhThoi() => LayHoacVe("tham_thoi", 256, VeThamHinhThoi);

    /// <summary>Ngoặc chữ L ôm một góc. Hướng mặc định: góc trên-trái.</summary>
    public static Sprite NgoacGoc() => LayHoacVe("ngoac_goc", 128, VeNgoacGoc);

    /// <summary>Một vạch của viền nét đứt (viên thuốc bo tròn hai đầu).</summary>
    public static Sprite VachNetDut() => LayHoacVe("vach_dut", 64, VeVachNetDut);

    /// <summary>Chấm tròn mềm — dùng cho hạt nhấp nháy ở 4 góc.</summary>
    public static Sprite ChamTron() => LayHoacVe("cham_tron", 64, VeChamTron);

    /// <summary>Chip "nắm để kéo" nổi trên nóc công trình khi vào Edit Mode.</summary>
    public static Sprite ChipNamKeo() => LayHoacVe("chip_nam", 128, VeChipNamKeo);

    // ═════════════════════════════════════════════════════════════════════════
    //  VẼ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hình thoi bo góc, RỖNG RUỘT: alpha cao ở vành, tắt dần vào giữa.
    ///
    /// Dùng "khoảng cách kiểu kim cương bo góc" |x|^p + |y|^p = 1 với p ≈ 1.35.
    /// p = 1 cho hình thoi nhọn (chính là hình bản tham chiếu dùng), p = 2 cho hình
    /// tròn. Chọn 1.35 để ra hình thoi có góc bo — nhận ra ngay là khác.
    /// </summary>
    private static void VeThamHinhThoi(Texture2D tex, int n)
    {
        const float p = 1.35f;
        float giua = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x - giua) / giua;
            float v = (y - giua) / giua;
            float d = Mathf.Pow(Mathf.Abs(u), p) + Mathf.Pow(Mathf.Abs(v), p);
            d = Mathf.Pow(d, 1f / p);          // 0 ở tâm, 1 ở vành

            float a;
            if (d > 1.0f)
            {
                a = 0f;                         // ngoài hình
            }
            else if (d > 0.90f)
            {
                // Vành ngoài: dày, đây là thứ mắt nhìn thấy rõ nhất.
                a = Mathf.SmoothStep(0f, 1f, (1.0f - d) / 0.10f);
            }
            else
            {
                // Ruột: mờ dần về tâm, giữ lại chút nền để phân biệt với cỏ bên ngoài
                // nhưng KHÔNG che mất mặt đất — khác hẳn kiểu tô đặc.
                a = Mathf.Lerp(0.34f, 0.06f, Mathf.Pow(1f - d / 0.90f, 0.7f));
            }

            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    /// <summary>
    /// Ngoặc chữ L: hai thanh dày vuông góc, đầu bo tròn, chừa một khoảng hở ở đỉnh
    /// góc. Khoảng hở đó là chi tiết làm nó ra dáng "khung ngắm" chứ không phải nêm đặc.
    /// </summary>
    private static void VeNgoacGoc(Texture2D tex, int n)
    {
        float day  = n * 0.20f;      // bề dày thanh
        float dai  = n * 0.74f;      // chiều dài mỗi thanh
        float ho   = n * 0.06f;      // khoảng hở ở đỉnh góc

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;      // gốc toạ độ về góc trên-trái

            // Thanh ngang: chạy từ (ho, ho) sang phải
            float aNgang = ThanhBoTron(fx, fy, ho, ho, ho + dai, ho + day);
            // Thanh dọc: chạy từ (ho, ho) xuống dưới
            float aDoc   = ThanhBoTron(fx, fy, ho, ho, ho + day, ho + dai);

            float a = Mathf.Max(aNgang, aDoc);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    /// <summary>Vạch nét đứt: viên thuốc nằm ngang, bo tròn hai đầu.</summary>
    private static void VeVachNetDut(Texture2D tex, int n)
    {
        float day = n * 0.34f;
        float le  = n * 0.10f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;
            float a = ThanhBoTron(fx, fy, le, (n - day) * 0.5f, n - le, (n + day) * 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    private static void VeChamTron(Texture2D tex, int n)
    {
        float giua = (n - 1) * 0.5f;
        float bk   = giua * 0.86f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Mathf.Sqrt((x - giua) * (x - giua) + (y - giua) * (y - giua));
            float a = Mathf.Clamp01((bk - d) / Mathf.Max(1f, bk * 0.35f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
    }

    /// <summary>
    /// Chip nắm kéo: viên bo góc có 3 vạch ngang ở giữa (biểu tượng "kéo" quen thuộc).
    /// Đây là thứ bản tham chiếu KHÔNG có — thêm vào vì công trình đứng yên trong Edit
    /// Mode cần một dấu hiệu nói "cái này nhấc được", chứ không phải đoán.
    /// </summary>
    private static void VeChipNamKeo(Texture2D tex, int n)
    {
        float le  = n * 0.10f;
        float bk  = n * 0.22f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;
            float aNen = HinhBoGoc(fx, fy, le, le, n - le, n - le, bk);

            // Ba vạch ngang, màu đục hơn nền chip
            float aVach = 0f;
            for (int i = 0; i < 3; i++)
            {
                float cy = n * (0.34f + i * 0.16f);
                aVach = Mathf.Max(aVach,
                    ThanhBoTron(fx, fy, n * 0.28f, cy - n * 0.035f, n * 0.72f, cy + n * 0.035f));
            }

            // Vạch KHOÉT LỖ trên nền: alpha nền trừ đi alpha vạch → nhìn xuyên qua thấy
            // mặt đất, nên chip không thành một khối đặc che mất nóc nhà.
            float a = Mathf.Max(0f, aNen - aVach * 0.85f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HÌNH HỌC
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Thanh chữ nhật bo tròn hai đầu — trả alpha 0..1 có khử răng cưa.</summary>
    private static float ThanhBoTron(float px, float py, float x0, float y0, float x1, float y1)
    {
        float bk = Mathf.Min(x1 - x0, y1 - y0) * 0.5f;
        return HinhBoGoc(px, py, x0, y0, x1, y1, bk);
    }

    /// <summary>Chữ nhật bo góc bán kính bk, khử răng cưa bằng dải chuyển 1 pixel.</summary>
    private static float HinhBoGoc(float px, float py, float x0, float y0, float x1, float y1, float bk)
    {
        float cx = Mathf.Clamp(px, x0 + bk, x1 - bk);
        float cy = Mathf.Clamp(py, y0 + bk, y1 - bk);
        float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        return Mathf.Clamp01((bk - d) + 0.5f);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HẠ TẦNG
    // ═════════════════════════════════════════════════════════════════════════

    private static Sprite LayHoacVe(string ten, int kichThuoc, System.Action<Texture2D, int> ve)
    {
        // Cache theo tên: mỗi sprite chỉ vẽ MỘT LẦN cho cả phiên chơi. Không cache thì
        // mỗi công trình lại dựng một texture 256×256 riêng — 34 công trình là 8MB rác.
        if (_kho.TryGetValue(ten, out Sprite co) && co != null) return co;

        var tex = new Texture2D(kichThuoc, kichThuoc, TextureFormat.RGBA32, false)
        {
            name = "KitDat_" + ten,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        ve(tex, kichThuoc);
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, kichThuoc, kichThuoc),
                                new Vector2(0.5f, 0.5f), PPU);
        spr.name = "KitDat_" + ten;
        spr.hideFlags = HideFlags.HideAndDontSave;

        _kho[ten] = spr;
        return spr;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Dọn cache khi vào Play Mode. Texture mang `HideAndDontSave` không tự bị thu hồi
    /// giữa các lần chạy; giữ tham chiếu cũ thì sprite trỏ vào texture đã bị Unity huỷ
    /// và mọi khung đặt biến thành ô trắng.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DonCache() => _kho.Clear();
#endif
}
