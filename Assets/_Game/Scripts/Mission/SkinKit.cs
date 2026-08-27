using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BỘ ĐỒ NGHỀ THAY ÁO — áp "ngôn ngữ thị giác dùng chung" của nhà thiết kế
/// (Export_Popups_Chon/README.md) lên UI CÓ SẴN mà không đụng cấu trúc.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO "THAY ÁO TẠI CHỖ" CHỨ KHÔNG DỰNG LẠI
/// ══════════════════════════════════════════════════════════════════════════
/// Kho / Hồ sơ / Shop là UI dựng sẵn trong scene-prefab với hàng chục
/// [SerializeField] trỏ chéo nhau (Kho 40 ref). Đập dựng lại là đứt hết tham chiếu
/// — đúng kiểu tai nạn đã gặp với popup Nhiệm vụ. Nên mọi hàm ở đây chỉ:
///   • đổi sprite/màu của Image ĐANG CÓ
///   • gắn thêm lớp trang trí (cạnh dưới 3D, gloss) làm CON MỚI, không xoá gì
/// Logic, dữ liệu, sự kiện onClick: nguyên vẹn.
///
/// Màu lấy từ <see cref="TaskPopupDesign"/> — cùng token với popup Nhiệm vụ vì
/// README xác nhận cả bộ popup dùng chung một ngôn ngữ.
/// </summary>
public static class SkinKit
{
    private static readonly Dictionary<string, Sprite> _kho = new Dictionary<string, Sprite>();

    // ═════════════════════════════════════════════════════════════════════════
    //  FONT CHỮ CỦA VỎ — mấu chốt "giống mock": mock dùng Baloo 2 (chữ tròn mập),
    //  Unity mặc định LiberationSans (mảnh, lạnh) nên cùng màu cùng khung vẫn
    //  "một trời một vực". Menu `0 · Tạo font chữ vỏ` build TMP asset vào
    //  Resources/Fonts/FontVo — có thì mọi vỏ tự dùng, chưa có thì giữ mặc định.
    // ═════════════════════════════════════════════════════════════════════════

    private static TMP_FontAsset _fontVo;
    private static bool _daTimFontVo;

    public static TMP_FontAsset FontVo
    {
        get
        {
            if (!_daTimFontVo || _fontVo == null)
            {
                _daTimFontVo = true;
                if (TMP_Settings.defaultFontAsset != null)
                    _fontVo = TMP_Settings.defaultFontAsset;

                if (_fontVo == null)
                    _fontVo = Resources.Load<TMP_FontAsset>("Fonts/FontVo");
#if UNITY_EDITOR
                if (_fontVo == null)
                    _fontVo = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Fonts/Baloo2 SDF.asset");
                if (_fontVo == null)
                    _fontVo = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Resources/Fonts/FontVo.asset");
#endif
            }
            return _fontVo;
        }
    }

    /// <summary>Đổi font MỌI chữ dưới gốc sang font vỏ (nếu đã tạo). Chỉ đổi font — nội dung, cỡ, màu giữ nguyên.</summary>
    public static void ApFont(Component goc)
    {
        var f = FontVo;
        if (f == null || goc == null) return;
        foreach (var t in goc.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.font != f)
            {
                t.font = f;
                if (f.material != null) t.fontSharedMaterial = f.material;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SPRITE — bản đã kiểm chứng từ popup Nhiệm vụ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Chữ nhật bo góc alpha đặc, 9-slice được.</summary>
    public static Sprite BoGoc(float banKinh)
    {
        string khoa = $"skin_bogoc_{banKinh:0.#}";
        if (_kho.TryGetValue(khoa, out Sprite co) && co != null) return co;

        int r = Mathf.Max(2, Mathf.RoundToInt(banKinh));
        int n = r * 4 + 8;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { name = khoa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
          hideFlags = HideFlags.HideAndDontSave };

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = x < r ? r - x : (x >= n - r ? x - (n - r - 1) : 0f);
            float dy = y < r ? r - y : (y >= n - r ? y - (n - r - 1) : 0f);
            float a = (dx <= 0f || dy <= 0f) ? 1f : Mathf.Clamp01(r - Mathf.Sqrt(dx*dx+dy*dy) + 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                                SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
        spr.name = khoa; spr.hideFlags = HideFlags.HideAndDontSave;
        _kho[khoa] = spr;
        return spr;
    }

    /// <summary>
    /// Hình TRÒN đầy — vẽ SIMPLE trên rect vuông. Đừng giả tròn bằng BoGoc(r) Sliced:
    /// bán kính sprite vượt nửa cạnh rect là 9-slice vỡ, ra "vuông bo tí góc"
    /// (đĩa icon Shop trong ảnh 13/08).
    /// </summary>
    public static Sprite HinhTron()
    {
        const string khoa = "skin_tron";
        if (_kho.TryGetValue(khoa, out Sprite co) && co != null) return co;

        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { name = khoa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
          hideFlags = HideFlags.HideAndDontSave };

        float tam = (n - 1) * 0.5f, r = tam - 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Mathf.Sqrt((x - tam) * (x - tam) + (y - tam) * (y - tam));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        spr.name = khoa; spr.hideFlags = HideFlags.HideAndDontSave;
        _kho[khoa] = spr;
        return spr;
    }

    /// <summary>Dải gradient dọc 1×64 — vẽ Simple, KHÔNG Sliced (bài học ván gỗ trắng).</summary>
    public static Sprite DaiGradient()
    {
        const string khoa = "skin_gradient";
        if (_kho.TryGetValue(khoa, out Sprite co) && co != null) return co;

        const int n = 64;
        var tex = new Texture2D(1, n, TextureFormat.RGBA32, false)
        { name = khoa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
          hideFlags = HideFlags.HideAndDontSave };
        for (int y = 0; y < n; y++)
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (float)y / (n - 1))));
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, 1, n), new Vector2(0.5f, 0.5f), 100f);
        spr.name = khoa; spr.hideFlags = HideFlags.HideAndDontSave;
        _kho[khoa] = spr;
        return spr;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  KIỂU NÚT — bảng trong README dùng chung
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Kim cương (mua bằng gem): #7cc9f0→#3486c2, viền #2e6fa3 — README có, TaskPopupDesign chưa.</summary>
    public static readonly TaskPopupDesign.KieuNut NutKimCuong =
        new TaskPopupDesign.KieuNut("#7cc9f0", "#3486c2", "#2e6fa3", "#ffffff", "");

    /// <summary>Huỷ / Gỡ bán: #e8a19a→#c9645c, viền #a4453e.</summary>
    public static readonly TaskPopupDesign.KieuNut NutDo =
        new TaskPopupDesign.KieuNut("#e8a19a", "#c9645c", "#a4453e", "#ffffff", "");

    /// <summary>Vô hiệu / khoá: #b8ae95.</summary>
    public static readonly TaskPopupDesign.KieuNut NutXam =
        new TaskPopupDesign.KieuNut("#b8ae95", "#b8ae95", "#9c927c", "#ffffff", "");

    /// <summary>Nút − (giảm số lượng) trong mock Shop: quả trám CAM, không phải vàng.</summary>
    public static readonly TaskPopupDesign.KieuNut NutCam =
        new TaskPopupDesign.KieuNut("#ef9950", "#dd7a28", "#b55e19", "#ffffff", "");

    /// <summary>
    /// Mặc áo 3D cho một nút ĐANG CÓ: nền bo góc + gradient + cạnh dưới dày + chữ trắng
    /// đổ bóng. Không đụng onClick, không đổi kích thước, không đổi vị trí.
    /// </summary>
    public static void MacAoNut(Button nut, TaskPopupDesign.KieuNut kieu, float boGoc = 16f)
    {
        if (nut == null) return;

        Image nen = nut.image != null ? nut.image : nut.GetComponent<Image>();
        if (nen == null) return;

        var rt = (RectTransform)nut.transform;
        Vector2 kt = rt.rect.size;

        // Nền chính = màu ĐÁY của gradient; sprite gốc (art cũ) được thay bằng bo góc.
        nen.sprite = BoGoc(boGoc);
        nen.type = Image.Type.Sliced;
        nen.color = kieu.nenDuoi;

        // Bóng đổ mềm dưới nút — mock có ở mọi nút, thiếu nó nút "dán" vào nền.
        if (nen.GetComponent<Shadow>() == null)
        {
            var bong = nen.gameObject.AddComponent<Shadow>();
            bong.effectColor = new Color(0.35f, 0.22f, 0.05f, 0.28f);
            bong.effectDistance = new Vector2(0f, -5f);
        }

        // ⚠ QUY TẮC SỐNG CÒN: mọi lớp Skin_* phải CHÈN LÊN ĐẦU danh sách con
        // (index 0-1-2). Trong uGUI, con vẽ theo thứ tự — gắn vào CUỐI là lớp trang
        // trí phủ kín chữ và icon CÓ SẴN của nút. Chính lỗi này làm cả ba popup
        // "trống trơn" ở lần chạy đầu: nội dung vẫn đó, nằm dưới tấm áo.
        int lop = 0;
        if (nut.transform.Find("Skin_EdgeBottom") == null)
        {
            var canh = TaoConImage(nut.transform, "Skin_EdgeBottom", BoGoc(boGoc),
                TaskPopupDesign.NutDoCanh, new Vector2(0f, -5f), kt);
            canh.SetSiblingIndex(lop++);
        }
        else lop++;

        if (nut.transform.Find("Skin_Border") == null)
        {
            var vien = TaoConImage(nut.transform, "Skin_Border", BoGoc(boGoc),
                kieu.vien, Vector2.zero, kt + new Vector2(6f, 6f));
            vien.SetSiblingIndex(lop++);
        }
        else lop++;

        if (nut.transform.Find("Skin_FillTop") == null)
        {
            var top = TaoConImage(nut.transform, "Skin_FillTop", DaiGradient(),
                kieu.nen, Vector2.zero, new Vector2(kt.x - boGoc * 1.2f, kt.y - 6f));
            top.GetComponent<Image>().type = Image.Type.Simple;
            top.SetSiblingIndex(lop++);
        }

        // Chữ trên nút: trắng/bảng màu + bóng — KHÔNG đổi nội dung, cỡ, font.
        foreach (var tmp in nut.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.parent != nut.transform &&
                !tmp.transform.parent.name.StartsWith("Skin_")) { }
            tmp.color = kieu.chu;
            if (tmp.GetComponent<Shadow>() == null)
            {
                var sh = tmp.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.25f);
                sh.effectDistance = new Vector2(0f, -2f);
            }
        }
    }

    /// <summary>Tấm giấy kem: đổi Image có sẵn sang nền giấy #fdf3da→#fbeccb viền nâu.</summary>
    public static void MacAoGiay(Image anh, float boGoc = 22f)
    {
        if (anh == null) return;
        anh.sprite = BoGoc(boGoc);
        anh.type = Image.Type.Sliced;
        anh.color = TaskPopupDesign.GiayDuoi;

        var rt = (RectTransform)anh.transform;
        int lop = 0;
        if (anh.transform.Find("Skin_Border") == null)
        {
            var vien = TaoConImage(anh.transform, "Skin_Border", BoGoc(boGoc),
                TaskPopupDesign.GiayVien, Vector2.zero, rt.rect.size + new Vector2(8f, 8f));
            vien.SetSiblingIndex(lop++);
            // ⚠ KHOÉT RUỘT — con vẽ SAU cha: viền đặc là tấm phủ kín thân giấy
            // (ảnh 13/08: panel Hồ sơ thành slab nâu). Chỉ giữ vành 8px.
            vien.GetComponent<Image>().fillCenter = false;
        }
        else lop++;

        // CHỈ vành trong mảnh — KHÔNG phủ tấm ruột nữa. Bản đầu có "Skin_InnerFill"
        // to bằng cả panel gắn cuối danh sách con: nó là chính tấm giấy trống che
        // sạch item/chữ trong ảnh chụp. Nền cha đã đúng màu giấy, ruột là thừa.
        if (anh.transform.Find("Skin_InnerRing") == null)
        {
            var ring = TaoConImage(anh.transform, "Skin_InnerRing", BoGoc(boGoc - 3f),
                TaskPopupDesign.GiayVienTrong, Vector2.zero, rt.rect.size - new Vector2(6f, 6f));
            ring.SetSiblingIndex(lop++);

            // Ring đặc sẽ che nội dung → khoét ruột bằng cách thu nó thành KHUNG:
            // vẽ 4 cạnh mảnh thay vì tấm đặc.
            var img = ring.GetComponent<Image>();
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
        }
    }

    /// <summary>
    /// Ván gỗ: đổi Image nền ngoài cùng sang gỗ nâu + viền đậm.
    /// `themGradient:false` cho popup CAO — dải gradient kéo trên cả tấm cao sẽ
    /// loang thành mảng sáng lệch góc (ảnh Kho 13/08), tấm phẳng nhìn đúng mock hơn.
    /// </summary>
    public static void MacAoVanGo(Image anh, float boGoc = 40f, bool themGradient = true)
    {
        if (anh == null) return;
        anh.sprite = BoGoc(boGoc);
        anh.type = Image.Type.Sliced;
        anh.color = TaskPopupDesign.VanGoDuoi;

        var rt = (RectTransform)anh.transform;
        int lop = 0;
        if (anh.transform.Find("Skin_Border") == null)
        {
            var vien = TaoConImage(anh.transform, "Skin_Border", BoGoc(boGoc),
                TaskPopupDesign.VanGoVien, Vector2.zero, rt.rect.size + new Vector2(14f, 14f));
            vien.SetSiblingIndex(lop++);
        }
        else lop++;

        if (themGradient && anh.transform.Find("Skin_FillTop") == null)
        {
            var top = TaoConImage(anh.transform, "Skin_FillTop", DaiGradient(),
                TaskPopupDesign.VanGoTren, Vector2.zero,
                new Vector2(rt.rect.size.x - boGoc * 1.2f, rt.rect.size.y - 10f));
            top.GetComponent<Image>().type = Image.Type.Simple;
            top.SetSiblingIndex(lop++);   // dưới toàn bộ nội dung có sẵn
        }
    }

    /// <summary>Thẻ/hàng nội dung: nền kem sáng #fffdf4→#fdf6e3, viền #ecd09c. `bongDo` cho thẻ nổi (card), để false cho ô lõm (search).</summary>
    public static void MacAoThe(Image anh, float boGoc = 18f, bool bongDo = false)
    {
        if (anh == null) return;
        anh.sprite = BoGoc(boGoc);
        anh.type = Image.Type.Sliced;
        anh.color = TaskPopupDesign.HangDuoi;

        var rt = (RectTransform)anh.transform;
        if (anh.transform.Find("Skin_Border") == null)
        {
            var vien = TaoConImage(anh.transform, "Skin_Border", BoGoc(boGoc),
                TaskPopupDesign.HangVien, Vector2.zero, rt.rect.size + new Vector2(6f, 6f));
            vien.SetSiblingIndex(0);   // dưới nội dung có sẵn của thẻ
            // ⚠ KHOÉT RUỘT — viền đặc phủ kín thân thẻ (ô Kho trắng mà nhìn toàn
            // màu viền beige, ảnh 13/08). Chỉ giữ vành 6px.
            vien.GetComponent<Image>().fillCenter = false;

            if (bongDo)
            {
                var bong = vien.gameObject.AddComponent<Shadow>();
                bong.effectColor = new Color(0.35f, 0.22f, 0.05f, 0.2f);
                bong.effectDistance = new Vector2(0f, -6f);
            }
        }
    }

    /// <summary>
    /// RUY BĂNG TIÊU ĐỀ vàng — chi tiết nhận diện của cả bộ mock (CỬA HÀNG / KHO VẬT
    /// PHẨM / HỒ SƠ): viền nâu #a35c14 → thân cam #f0a32f → gradient sáng #ffd257 →
    /// chữ trắng đậm đổ bóng. Thuần trang trí: mọi raycast TẮT, idempotent theo tên
    /// "Skin_Ribbon", không đụng object có sẵn. `chu` để trống nếu popup đã có chữ
    /// tiêu đề riêng (đặt ruy băng ngay dưới chữ đó bằng SetSiblingIndex).
    /// </summary>
    public static RectTransform LamRuyBang(Transform cha, string chu, Vector2 anchor,
                                           Vector2 viTri, Vector2 kt)
    {
        var cu = cha.Find("Skin_Ribbon");
        if (cu != null) return (RectTransform)cu;

        var goc = new GameObject("Skin_Ribbon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)goc.transform;
        rt.SetParent(cha, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = viTri;
        rt.sizeDelta = kt;
        var vien = goc.GetComponent<Image>();
        vien.sprite = BoGoc(26f);
        vien.type = Image.Type.Sliced;
        vien.color = TaskPopupDesign.RibbonVien;
        vien.raycastTarget = false;
        var bong = goc.AddComponent<Shadow>();
        bong.effectColor = new Color(0.2f, 0.1f, 0f, 0.3f);
        bong.effectDistance = new Vector2(0f, -6f);

        TaoConImage(rt, "Skin_RibbonFill", BoGoc(22f), TaskPopupDesign.RibbonDuoi,
                    Vector2.zero, kt - new Vector2(10f, 10f));

        // Gradient sáng: dải 1×64 vẽ SIMPLE — Sliced sẽ nghiền gradient thành 1 hàng
        // pixel (bài học ván gỗ trắng của popup Nhiệm vụ).
        var grad = TaoConImage(rt, "Skin_RibbonGrad", DaiGradient(), TaskPopupDesign.RibbonTren,
                    new Vector2(0f, 2f), kt - new Vector2(30f, 18f));
        grad.GetComponent<Image>().type = Image.Type.Simple;

        if (!string.IsNullOrEmpty(chu))
        {
            var chuGo = new GameObject("Skin_RibbonText", typeof(RectTransform));
            var crt = (RectTransform)chuGo.transform;
            crt.SetParent(rt, false);
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 2f);
            crt.sizeDelta = kt - new Vector2(60f, 30f);
            var tmp = chuGo.AddComponent<TextMeshProUGUI>();
            tmp.text = chu;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 24f;
            tmp.fontSizeMax = 62f;
            tmp.raycastTarget = false;
            var sh = chuGo.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.3f);
            sh.effectDistance = new Vector2(0f, -3f);
        }
        return rt;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static RectTransform TaoConImage(Transform cha, string ten, Sprite spr,
                                             Color mau, Vector2 viTri, Vector2 kt)
    {
        var go = new GameObject(ten, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(cha, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = viTri;
        rt.sizeDelta = kt;

        var img = go.GetComponent<Image>();
        img.sprite = spr;
        img.type = Image.Type.Sliced;
        img.color = mau;
        img.raycastTarget = false;
        return rt;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DonKho()
    {
        _kho.Clear();
        _fontVo = null;
        _daTimFontVo = false;
    }
#endif
}
