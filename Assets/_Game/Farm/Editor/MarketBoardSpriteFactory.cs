#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  SINH SPRITE THỦ TỤC CHO BẢNG TIN CHỢ
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO vẽ bằng code thay vì dùng sprite mặc định của Unity:
/// yêu cầu là "nền có màu, trang trí KHÁC video". Sprite mặc định (UISprite) có
/// bo góc cố định 8px giống hệt mọi game Unity khác; ở đây cần bo góc lớn đều +
/// dải chấm bi riêng để bảng tin có diện mạo của dự án này.
///
/// Toàn bộ hình đều là TRẮNG có alpha → tint bằng Image.color theo
/// MarketBoardPalette. Nhờ vậy chủ dự án đổi màu bằng Inspector, không phải sinh lại PNG.
///
/// Xuất ra: Assets/_Game/Farm/Art/UI_MarketBoard/
/// CHỖ CHỜ ART — thay PNG cùng tên là xong, không phải sửa code.
/// </summary>
public static class MarketBoardSpriteFactory
{
    public const string ArtFolder = "Assets/_Game/Farm/Art/UI_MarketBoard";

    // Tên sprite — dùng lại ở MarketBoardUIBuilder
    public const string PanelName  = "spr_mb_panel";   // bo góc 30, 9-slice
    public const string CardName   = "spr_mb_card";    // bo góc 20, 9-slice
    public const string InsetName  = "spr_mb_inset";   // bo góc 12, 9-slice
    public const string PillName   = "spr_mb_pill";    // viên thuốc, 9-slice
    public const string CircleName = "spr_mb_circle";  // đĩa tròn đặc
    public const string DotsName   = "spr_mb_dots";    // dải chấm bi (hoạ tiết riêng)

    [MenuItem("Tools/Farm/Chợ/1 · Sinh sprite nền bảng tin", false, 10)]
    public static void GenerateMenu()
    {
        GenerateAll(true);
        EditorUtility.DisplayDialog("Chợ",
            "Đã sinh sprite nền vào:\n" + ArtFolder,
            "OK");
    }

    public static void GenerateAll(bool force)
    {
        EnsureFolder();

        Gen(PanelName,  128, 128, (u, v, w, h) => RoundBox(u, v, w, h, 30f), new Vector4(34, 34, 34, 34), force);
        Gen(CardName,    96,  96, (u, v, w, h) => RoundBox(u, v, w, h, 20f), new Vector4(24, 24, 24, 24), force);
        Gen(InsetName,   64,  64, (u, v, w, h) => RoundBox(u, v, w, h, 12f), new Vector4(16, 16, 16, 16), force);
        Gen(PillName,    64,  64, (u, v, w, h) => RoundBox(u, v, w, h, 31f), new Vector4(31, 31, 31, 31), force);
        Gen(CircleName, 128, 128, Circle,                                     Vector4.zero,               force);
        Gen(DotsName,    64,  64, DotStrip,                                   new Vector4(0, 0, 0, 0),    force);

        AssetDatabase.Refresh();
    }

    public static Sprite Load(string spriteName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + spriteName + ".png");
    }

    /// <summary>Nạp sprite, tự sinh nếu chưa có. Builder gọi hàm này để không bao giờ ra Image trống.</summary>
    public static Sprite LoadOrGenerate(string spriteName)
    {
        Sprite sprite = Load(spriteName);
        if (sprite != null)
            return sprite;

        GenerateAll(false);
        return Load(spriteName);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HẠ TẦNG
    // ══════════════════════════════════════════════════════════════════════

    private delegate Color PixelFn(float u, float v, int w, int h);

    private static void EnsureFolder()
    {
        string abs = Abs(ArtFolder);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }
    }

    private static string Abs(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }

    private static void Gen(string spriteName, int w, int h, PixelFn fn, Vector4 border, bool force)
    {
        string path = ArtFolder + "/" + spriteName + ".png";

        if (force || !File.Exists(Abs(path)))
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f;
                    float v = (y + 0.5f) / h * 2f - 1f;
                    px[y * w + x] = fn(u, v, w, h);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Abs(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        // LUÔN áp lại import settings kể cả khi PNG đã tồn tại: nếu lần import đầu
        // lỡ ra textureType = Default thì Load<Sprite>() trả null vĩnh viễn
        // và toàn bộ popup ra ô trắng mà không có lỗi nào báo.
        ApplyImportSettings(path, border);
    }

    private static void ApplyImportSettings(string path, Vector4 border)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
            return;

        bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != border;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = false;
        imp.filterMode          = FilterMode.Bilinear;
        imp.wrapMode            = TextureWrapMode.Clamp;
        imp.spriteBorder        = border;

        // BẮT BUỘC FullRect. Mặc định của Unity là Tight — với Image type Sliced/Tiled,
        // sprite Tight sẽ đổ cảnh báo "Sprite mesh type has to be FullRect" và Unity
        // âm thầm vẽ về Simple, tức là mọi bo góc 9-slice bị kéo méo.
        TextureImporterSettings settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            dirty = true;
        }

        if (dirty)
            imp.SaveAndReimport();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CÁC HÌNH
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>SDF hình chữ nhật bo góc (Inigo Quilez). p, b tính bằng pixel.</summary>
    private static float SdRoundBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static float Aa(float sd, float softness = 1.4f)
    {
        return Mathf.Clamp01(0.5f - sd / softness);
    }

    private static Color RoundBox(float u, float v, int w, int h, float radius)
    {
        Vector2 p = new Vector2(u * w * 0.5f, v * h * 0.5f);
        Vector2 b = new Vector2(w * 0.5f - 1f, h * 0.5f - 1f);
        float sd = SdRoundBox(p, b, radius);
        float a = Aa(sd);
        return a <= 0.001f ? Color.clear : new Color(1f, 1f, 1f, a);
    }

    private static Color Circle(float u, float v, int w, int h)
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float unit = 2f / w;
        float a = Mathf.Clamp01((0.99f - d) / (unit * 1.6f));
        return a <= 0.001f ? Color.clear : new Color(1f, 1f, 1f, a);
    }

    /// <summary>
    /// Dải chấm bi — hoạ tiết trang trí RIÊNG của dự án, thay cho mái hiên sọc
    /// xanh-trắng trong video tham chiếu. Ba chấm so le trên nền trong suốt,
    /// tile ngang được nhờ chu kỳ khép kín theo trục u.
    /// </summary>
    private static Color DotStrip(float u, float v, int w, int h)
    {
        // Hai hàng chấm lệch pha nửa chu kỳ → nhìn như hoa văn thổ cẩm, không phải sọc
        float best = 1f;

        for (int row = 0; row < 2; row++)
        {
            float cy = row == 0 ? -0.36f : 0.36f;
            float phase = row == 0 ? 0f : 0.5f;

            // 2 chấm mỗi hàng trong một ô tile
            for (int i = 0; i < 2; i++)
            {
                float cx = -1f + ((i + phase + 0.5f) / 2f) * 2f;
                float d = new Vector2(u - cx, v - cy).magnitude;
                best = Mathf.Min(best, d);
            }
        }

        float unit = 2f / w;
        float a = Mathf.Clamp01((0.30f - best) / (unit * 2.2f));
        return a <= 0.001f ? Color.clear : new Color(1f, 1f, 1f, a);
    }
}
#endif
