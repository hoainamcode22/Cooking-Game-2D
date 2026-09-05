using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// TOOL 1 — CẮT (SLICE) 3 SPRITESHEET NHÂN VẬT.
///
/// Menu:
///   Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (DRY-RUN)
///   Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY)
///   Tools/Farm Game/Characters/Kiểm tra sprite con đã slice
///
/// LÀM GÌ
///   • Đặt TextureImporter cho 7 file PNG nhân vật: Sprite · Multiple · PPU 100 ·
///     pivot Bottom-Center · alphaIsTransparency · mipmap OFF · filter Bilinear.
///   • Cắt lưới đúng chỉ số §5 CONTRACT rồi đặt TÊN CỐ ĐỊNH cho từng ô. Kích thước
///     KHÔNG hardcode — tool tự đọc PNG thật rồi chia cols×rows (xem BuildSpecs()):
///       flowergirl_walk_spritesheet.png    3 cột × 4 hàng
///           hàng 0 → fg_down_1..3 · hàng 1 → fg_left_1..3
///           hàng 2 → fg_right_1..3 · hàng 3 → fg_up_1..3
///       worker_hammer_spritesheet.png      4 cột × 3 hàng → hammer_01..12
///       worker_celebrate_spritesheet.png   4 cột × 3 hàng → celebrate_01..12
///       worker02_hammer_spritesheet.png    4 cột × 3 hàng → w02_hammer_01..12
///       worker02_celebrate_spritesheet.png 4 cột × 3 hàng → w02_celebrate_01..12
///       worker03_hammer_spritesheet.png    4 cột × 3 hàng → w03_hammer_01..12
///       worker03_celebrate_spritesheet.png 4 cột × 3 hàng → w03_celebrate_01..12
///     (thứ tự đọc: trái → phải, trên → dưới)
///   • MẶC ĐỊNH cắt TRỌN Ô LƯỚI (SheetSpec.dungOLuoiDayDu = true) — mọi frame cùng
///     kích thước nên animation không giật ngang. Tight-crop cũ vẫn còn (set field
///     này = false cho sheet muốn dùng lại).
///
/// VÌ SAO CHIA RANH GIỚI BẰNG RoundToInt(index * size / count)
///   Cell không chia hết số nguyên (848/3 = 282.667 · 896/3 = 298.667). Nếu lấy
///   cellW = 282 rồi nhân dần thì cột cuối lệch 2px và sai số DỒN. Cách này tính
///   ranh giới tuyệt đối cho từng mép nên tổng bề rộng luôn đúng bằng ảnh, ô cuối
///   tự nhận phần dư. (Áp dụng khi dungOLuoiDayDu = false — chế độ tight-crop cũ.)
///
/// MẶC ĐỊNH (dungOLuoiDayDu = true): cắt TRỌN Ô LƯỚI cellW = w/cols, cellH = h/rows.
///   Bắt buộc w % cols == 0 và h % rows == 0, KHÔNG THÌ CHẶN CỨNG (lỗi) — lưới không
///   chia hết sẽ khiến RoundToInt lệch dần, mỗi frame một kích thước khác nhau ⇒
///   nhân vật giật ngang khi chạy animation (đo được lệch tới hơn 100px).
///
/// VÌ SAO DÙNG SpriteDataProviderFactories CHỨ KHÔNG TextureImporter.spritesheet
///   `TextureImporter.spritesheet` đã deprecated. Dự án đã có tiền lệ chạy tốt với
///   API mới (Assets/NV_CHEF/Editor/ChefSetupTool.cs, Assets/NV_01/Editor/SetupPlayerNV01.cs)
///   nên chắc chắn package com.unity.2d.sprite có mặt. Quan trọng hơn:
///   ISpriteNameFileIdDataProvider cho phép GIỮ NGUYÊN fileID theo TÊN sprite ⇒
///   chạy tool lần 2 KHÔNG làm prefab/asset đang trỏ vào sprite bị "missing".
///
/// IDEMPOTENT: SetSpriteRects THAY THẾ toàn bộ danh sách (không merge) và tên sprite
/// là cố định ⇒ chạy 10 lần vẫn đúng 12 sprite con, không nhân đôi.
/// </summary>
public static class CharacterSheetSliceTool
{
    // ─── Menu ────────────────────────────────────────────────────────────
    private const string MenuRoot  = "Tools/Farm Game/Characters/";
    private const string MenuDry   = MenuRoot + "★ Slice 3 spritesheet nhân vật (DRY-RUN)";
    private const string MenuApply = MenuRoot + "★ Slice 3 spritesheet nhân vật (APPLY)";
    private const string MenuCheck = MenuRoot + "Kiểm tra sprite con đã slice";

    private const string Tag = "[Tool]";

    // ─── Hằng số CHỈNH ĐƯỢC ──────────────────────────────────────────────
    private const float PixelsPerUnit = 100f;

    // ─── Đường dẫn 7 sheet (public để tool khác dùng lại, khỏi gõ lại chuỗi) ──
    public const string PathFlowerGirl  = "Assets/Art/Characters/FlowerGirl/flowergirl_walk_spritesheet.png";
    public const string PathHammer      = "Assets/Art/Characters/Worker/worker_hammer_spritesheet.png";
    public const string PathCelebrate   = "Assets/Art/Characters/Worker/worker_celebrate_spritesheet.png";
    public const string PathHammer02    = "Assets/Art/Characters/Worker/worker02_hammer_spritesheet.png";
    public const string PathCelebrate02 = "Assets/Art/Characters/Worker/worker02_celebrate_spritesheet.png";
    public const string PathHammer03    = "Assets/Art/Characters/Worker/worker03_hammer_spritesheet.png";
    public const string PathCelebrate03 = "Assets/Art/Characters/Worker/worker03_celebrate_spritesheet.png";

    /// <summary>Số sprite con mong đợi ở MỖI sheet.</summary>
    public const int ExpectedSpritesPerSheet = 12;

    // ─────────────────────────────────────────────────────────────────────
    //  MÔ TẢ 1 SHEET
    // ─────────────────────────────────────────────────────────────────────
    private sealed class SheetSpec
    {
        public string   path;
        public string   nhan;        // tên ngắn để in report
        public int      cols;
        public int      rows;
        public int      expectedW;   // TUỲ CHỌN — 0 = bỏ qua kiểm. Khác 0 mà không khớp
        public int      expectedH;   // kích thước thật ⇒ chỉ CẢNH BÁO, KHÔNG chặn.
        public bool     dungOLuoiDayDu = true; // true = cắt TRỌN Ô LƯỚI (mặc định, hết giật
                                                // hình); false = tight-crop cũ (revert được).
        public string[] names;       // cols*rows tên, đọc trái→phải, trên→dưới
    }

    private static SheetSpec[] BuildSpecs()
    {
        // FlowerGirl: 3 cột × 4 hàng, mỗi hàng 1 hướng.
        var fgNames = new string[12];
        string[] huong = { "down", "left", "right", "up" };
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
                fgNames[r * 3 + c] = "fg_" + huong[r] + "_" + (c + 1);

        // Worker 01: 4 cột × 3 hàng, đánh số phẳng 01..12 (giữ tên cũ — đang có tham chiếu).
        var hammerNames    = TenPhangSheet("hammer_");
        var celebrateNames = TenPhangSheet("celebrate_");

        // Worker 02 / 03: cùng grid 4×3, thêm tiền tố w02_/w03_ để không đụng Worker 01.
        var hammer02Names    = TenPhangSheet("w02_hammer_");
        var celebrate02Names = TenPhangSheet("w02_celebrate_");
        var hammer03Names    = TenPhangSheet("w03_hammer_");
        var celebrate03Names = TenPhangSheet("w03_celebrate_");

        return new[]
        {
            // expectedW/expectedH chỉ để CẢNH BÁO khi lệch — 0 = bỏ qua kiểm. Tool luôn
            // TỰ ĐO kích thước thật của PNG, không hardcode giá trị này để cắt.
            new SheetSpec { path = PathFlowerGirl,  nhan = "FlowerGirl walk",
                            cols = 3, rows = 4, expectedW = 900,  expectedH = 1264, names = fgNames },
            new SheetSpec { path = PathHammer,      nhan = "Worker hammer",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = hammerNames },
            new SheetSpec { path = PathCelebrate,   nhan = "Worker celebrate",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = celebrateNames },
            new SheetSpec { path = PathHammer02,    nhan = "Worker02 hammer",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = hammer02Names },
            new SheetSpec { path = PathCelebrate02, nhan = "Worker02 celebrate",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = celebrate02Names },
            new SheetSpec { path = PathHammer03,    nhan = "Worker03 hammer",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = hammer03Names },
            new SheetSpec { path = PathCelebrate03, nhan = "Worker03 celebrate",
                            cols = 4, rows = 3, expectedW = 1200, expectedH = 900,  names = celebrate03Names },
        };
    }

    /// <summary>Sinh 12 tên "prefix01".."prefix12" theo thứ tự đọc phẳng trái→phải, trên→dưới.</summary>
    private static string[] TenPhangSheet(string prefix)
    {
        var res = new string[12];
        for (int i = 0; i < 12; i++) res[i] = prefix + (i + 1).ToString("00");
        return res;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MENU: DRY-RUN
    // ─────────────────────────────────────────────────────────────────────
    [MenuItem(MenuDry, false, 10)]
    public static void DryRun()
    {
        var specs = BuildSpecs();
        var sb    = new StringBuilder();
        sb.AppendLine(Tag + " CharacterSheetSliceTool — DRY-RUN (không ghi gì lên đĩa)");
        sb.AppendLine("─────────────────────────────────────────────────────────────");

        int okFile = 0, thieuFile = 0, loiChiaHet = 0, canhBao = 0;

        for (int i = 0; i < specs.Length; i++)
        {
            SheetSpec s = specs[i];
            sb.AppendLine();
            sb.AppendLine($"[{i + 1}/{specs.Length}] {s.nhan}");
            sb.AppendLine("   file      : " + s.path);

            if (!File.Exists(s.path))
            {
                sb.AppendLine("   ⚠ BỎ QUA (chưa có file) — đội vẽ chưa giao art này.");
                thieuFile++;
                continue;
            }

            int w, h;
            if (!TryReadPngSize(s.path, out w, out h))
            {
                sb.AppendLine("   ✖ file tồn tại nhưng không đọc được (không phải PNG hợp lệ).");
                sb.AppendLine("     CẦN LÀM: kiểm tra lại file art rồi chạy lại menu này.");
                thieuFile++;
                continue;
            }
            okFile++;

            sb.AppendLine($"   kích thước thật   : {w} × {h} px");
            sb.AppendLine($"   grid              : {s.cols} cột × {s.rows} hàng");

            bool chiaHetCot  = w % s.cols == 0;
            bool chiaHetHang = h % s.rows == 0;
            sb.AppendLine($"   chia hết          : cột {(chiaHetCot ? "OK" : "KHÔNG")} · hàng {(chiaHetHang ? "OK" : "KHÔNG")}");

            string loiChia;
            if (!KiemTraChiaHet(w, h, s.cols, s.rows, out loiChia))
            {
                sb.AppendLine("   ✖ LỖI: " + loiChia);
                loiChiaHet++;
                continue;
            }

            if (s.expectedW != 0 || s.expectedH != 0)
            {
                bool khopExpected = (s.expectedW == 0 || w == s.expectedW) && (s.expectedH == 0 || h == s.expectedH);
                sb.AppendLine($"   kích thước ghi trong tool: {s.expectedW} × {s.expectedH} px" +
                              (khopExpected ? " (khớp)" : " (KHÁC — chỉ cảnh báo, không chặn)"));
                if (!khopExpected)
                {
                    canhBao++;
                    sb.AppendLine("   ⚠ CẢNH BÁO: kích thước THẬT khác kích thước ghi trong tool — " +
                                  "tool vẫn tự đo và cắt theo kích thước THẬT.");
                }
            }

            int cellW = w / s.cols;
            int cellH = h / s.rows;
            sb.AppendLine($"   cell              : {cellW} × {cellH} px" +
                          (s.dungOLuoiDayDu ? " (cắt TRỌN Ô LƯỚI)" : " (tight-crop)"));

            Rect[] rects = TinhRects(s, w, h);
            sb.AppendLine($"   {s.names.Length} tên sprite con + rect:");
            for (int k = 0; k < s.names.Length; k++)
            {
                Rect r = rects[k];
                sb.AppendLine($"      {(k + 1),2}. {s.names[k],-16} rect=({r.x:0},{r.y:0},{r.width:0},{r.height:0})");
            }

            CellDo[] doNoiDung = DoNoiDungTheoO(s.path, w, h, s.cols, s.rows);
            int soTranBien = 0;
            int baselineMin = int.MaxValue, baselineMax = int.MinValue;
            var baselineList = new List<string>();
            for (int k = 0; k < doNoiDung.Length; k++)
            {
                CellDo o = doNoiDung[k];
                if (o.tranBien) soTranBien++;
                if (o.coNoiDung)
                {
                    baselineList.Add(o.baseline.ToString());
                    if (o.baseline < baselineMin) baselineMin = o.baseline;
                    if (o.baseline > baselineMax) baselineMax = o.baseline;
                }
                else
                {
                    baselineList.Add("-");
                }
            }
            sb.AppendLine("   baseline (đáy nội dung→đáy ô, px): [" + string.Join(", ", baselineList) + "]");
            sb.AppendLine(baselineMax >= baselineMin
                ? $"   độ lệch baseline max: {baselineMax - baselineMin}px"
                : "   độ lệch baseline max: (không ô nào có nội dung để đo)");
            if (soTranBien > 0)
            {
                canhBao++;
                sb.AppendLine($"   ⚠ TRÀN BIÊN ô ({soTranBien}/{doNoiDung.Length} ô) — TRÀN BIÊN ô, cắt sẽ dính frame bên cạnh.");
            }

            int dangCo = DemSpriteCon(s.path);
            sb.AppendLine($"   sprite con hiện có trên đĩa: {dangCo}" +
                          (dangCo == s.names.Length ? " (đã slice trước đó — APPLY sẽ ghi đè, KHÔNG nhân đôi)"
                                                    : " (APPLY sẽ tạo mới)"));
        }

        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────");
        sb.AppendLine($"{Tag} TỔNG KẾT DRY-RUN: đọc được {okFile}/{specs.Length} file · " +
                      $"thiếu {thieuFile} · lỗi lưới không chia hết {loiChiaHet} · cảnh báo {canhBao}. " +
                      "Chưa ghi gì — bấm menu (APPLY) để cắt thật.");
        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MENU: APPLY
    // ─────────────────────────────────────────────────────────────────────
    [MenuItem(MenuApply, false, 11)]
    public static void Apply()
    {
        var specs = BuildSpecs();
        var sb    = new StringBuilder();
        sb.AppendLine(Tag + " CharacterSheetSliceTool — APPLY");
        sb.AppendLine("─────────────────────────────────────────────────────────────");

        int daCat = 0, boQua = 0, tongSprite = 0;

        for (int i = 0; i < specs.Length; i++)
        {
            SheetSpec s = specs[i];
            sb.AppendLine();
            sb.AppendLine($"[{i + 1}/{specs.Length}] {s.nhan} — {s.path}");

            if (!File.Exists(s.path))
            {
                sb.AppendLine("   ⚠ BỎ QUA (chưa có file) — đội vẽ chưa giao art này.");
                boQua++;
                continue;
            }

            int w, h;
            if (!TryReadPngSize(s.path, out w, out h))
            {
                sb.AppendLine("   ✖ BỎ QUA: file tồn tại nhưng không đọc được (không phải PNG hợp lệ).");
                boQua++;
                continue;
            }

            string loiChiaApply;
            if (!KiemTraChiaHet(w, h, s.cols, s.rows, out loiChiaApply))
            {
                sb.AppendLine("   ✖ BỎ QUA: " + loiChiaApply);
                boQua++;
                continue;
            }

            bool khopExpectedApply = (s.expectedW == 0 || w == s.expectedW) && (s.expectedH == 0 || h == s.expectedH);
            if (!khopExpectedApply)
                sb.AppendLine($"   ⚠ kích thước THẬT {w}×{h} khác kích thước ghi trong tool ({s.expectedW}×{s.expectedH}) — " +
                              "vẫn cắt theo kích thước THẬT (không chặn).");

            string loi;
            int soSprite;
            if (!CatMotSheet(s, w, h, out soSprite, out loi))
            {
                sb.AppendLine("   ✖ BỎ QUA: " + loi);
                boQua++;
                continue;
            }

            daCat++;
            tongSprite += soSprite;
            sb.AppendLine($"   ✔ đã cắt {soSprite} sprite con · pivot Bottom-Center · " +
                          $"PPU {PixelsPerUnit:0} · alphaIsTransparency ON · mipmap OFF · filter Bilinear.");

            // Xác minh NGAY: đọc lại sprite thật trong PNG, đối chiếu tên.
            List<string> thieuTen, laTen;
            KiemTenSprite(s, out thieuTen, out laTen);
            if (thieuTen.Count > 0)
                sb.AppendLine("   ⚠ thiếu sprite sau khi cắt: " + string.Join(", ", thieuTen));
            if (laTen.Count > 0)
                sb.AppendLine("   ⚠ còn sprite LẠ (tên cũ) trong file: " + string.Join(", ", laTen) +
                              " — mở Sprite Editor xoá tay nếu thấy khó chịu, không ảnh hưởng tool.");
            if (thieuTen.Count == 0 && laTen.Count == 0)
                sb.AppendLine("   ✔ xác minh tên sprite: khớp 100%.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────");
        sb.AppendLine($"{Tag} TỔNG KẾT APPLY: cắt xong {daCat}/{specs.Length} sheet · " +
                      $"bỏ qua {boQua} · tổng {tongSprite} sprite con. " +
                      (boQua == 0
                          ? "Bước tiếp: Tools/Farm Game/Worker/★ SETUP thợ búa và Tools/Farm Game/Shipper/★ SETUP cô gái giỏ hoa."
                          : "Sửa các dòng ✖ ở trên rồi chạy lại menu này."));
        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MENU: KIỂM TRA
    // ─────────────────────────────────────────────────────────────────────
    [MenuItem(MenuCheck, false, 12)]
    public static void KiemTra()
    {
        var specs = BuildSpecs();
        var sb    = new StringBuilder();
        sb.AppendLine(Tag + " CharacterSheetSliceTool — KIỂM TRA sprite con đã slice");
        sb.AppendLine("─────────────────────────────────────────────────────────────");

        int du = 0;
        for (int i = 0; i < specs.Length; i++)
        {
            SheetSpec s = specs[i];
            var ten = new List<string>();
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(s.path);
            if (all != null)
            {
                for (int k = 0; k < all.Length; k++)
                {
                    var sp = all[k] as Sprite;
                    if (sp != null) ten.Add(sp.name);
                }
            }
            ten.Sort(System.StringComparer.Ordinal);

            bool okSo = ten.Count == s.names.Length;
            if (okSo) du++;

            sb.AppendLine();
            sb.AppendLine($"{(okSo ? "✅" : "❌")} {s.nhan} — {ten.Count}/{s.names.Length} sprite con");
            sb.AppendLine("   " + s.path);
            sb.AppendLine("   " + (ten.Count == 0 ? "(chưa slice — chạy menu APPLY)" : string.Join(", ", ten)));
        }

        sb.AppendLine();
        sb.AppendLine("─────────────────────────────────────────────────────────────");
        sb.AppendLine($"{Tag} TỔNG KẾT KIỂM TRA: {du}/{specs.Length} sheet đủ {ExpectedSpritesPerSheet} sprite con.");
        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  LÕI CẮT 1 SHEET
    // ─────────────────────────────────────────────────────────────────────
    private static bool CatMotSheet(SheetSpec s, int w, int h, out int soSprite, out string loi)
    {
        soSprite = 0;
        loi      = null;

        var importer = AssetImporter.GetAtPath(s.path) as TextureImporter;
        if (importer == null)
        {
            loi = s.path + " không phải texture (TextureImporter = null).";
            return false;
        }

        Undo.RecordObject(importer, "Slice character spritesheet");

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Bilinear;
        importer.sRGBTexture         = true;
        importer.npotScale           = TextureImporterNPOTScale.None;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        // maxTextureSize phải >= cạnh lớn nhất, không thì Unity co ảnh xuống và
        // rect cắt theo kích thước gốc sẽ lệch.
        importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));

        var ts = new TextureImporterSettings();
        importer.ReadTextureSettings(ts);
        ts.spriteMeshType  = SpriteMeshType.FullRect;                  // mesh = đúng rect
        ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;        // §2: chân chạm đất
        importer.SetTextureSettings(ts);
        importer.SaveAndReimport();

        // Lấy lại importer SAU reimport để data provider đọc đúng trạng thái Multiple.
        importer = AssetImporter.GetAtPath(s.path) as TextureImporter;
        if (importer == null)
        {
            loi = "mất TextureImporter sau SaveAndReimport (" + s.path + ").";
            return false;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dp == null)
        {
            loi = "thiếu package '2D Sprite' (com.unity.2d.sprite) — không lấy được ISpriteEditorDataProvider.";
            return false;
        }
        dp.InitSpriteEditorDataProvider();

        // Giữ fileID cũ theo TÊN ⇒ cắt lại không làm prefab/asset mất tham chiếu sprite.
        var guidCu = new Dictionary<string, GUID>();
        SpriteRect[] cu = dp.GetSpriteRects();
        if (cu != null)
        {
            for (int i = 0; i < cu.Length; i++)
                if (cu[i] != null && !guidCu.ContainsKey(cu[i].name))
                    guidCu[cu[i].name] = cu[i].spriteID;
        }

        Rect[] rects = TinhRects(s, w, h);
        var moi   = new List<SpriteRect>(rects.Length);
        var pairs = new List<SpriteNameFileIdPair>(rects.Length);

        for (int i = 0; i < rects.Length && i < s.names.Length; i++)
        {
            string ten = s.names[i];
            GUID id;
            if (!guidCu.TryGetValue(ten, out id) || id.Empty()) id = GUID.Generate();

            var sr = new SpriteRect
            {
                name      = ten,
                spriteID  = id,
                rect      = rects[i],
                alignment = SpriteAlignment.BottomCenter,
                pivot     = new Vector2(0.5f, 0f),
                border    = Vector4.zero,
            };
            moi.Add(sr);
            pairs.Add(new SpriteNameFileIdPair(ten, id));
        }

        dp.SetSpriteRects(moi.ToArray());
        var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProv != null) nameProv.SetNameFileIdPairs(pairs);
        dp.Apply();
        importer.SaveAndReimport();

        soSprite = moi.Count;
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH CẮT SÁT BIÊN ALPHA (KHÔNG DÍNH VIỀN / LAYOUT CELL BÊN CẠNH)
    // ─────────────────────────────────────────────────────────────────────

    private static Rect[] ComputeTightRects(string path, int w, int h, int cols, int rows)
    {
        cols = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rows);

        var xs = new int[cols + 1];
        for (int c = 0; c <= cols; c++) xs[c] = Mathf.RoundToInt(c * (float)w / cols);
        xs[0] = 0; xs[cols] = w;

        var yTop = new int[rows + 1];
        for (int r = 0; r <= rows; r++) yTop[r] = Mathf.RoundToInt(r * (float)h / rows);
        yTop[0] = 0; yTop[rows] = h;

        Texture2D tex = null;
        try
        {
            if (File.Exists(path))
            {
                byte[] raw = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2);
                tex.LoadImage(raw);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[CharacterSheetSliceTool] Không load được raw texture để tight-crop: " + ex.Message);
        }

        var res = new Rect[cols * rows];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int cellX = xs[c];
                int cellW = Mathf.Max(1, xs[c + 1] - xs[c]);
                int cellY = h - yTop[r + 1];
                int cellH = Mathf.Max(1, yTop[r + 1] - yTop[r]);

                if (tex != null && tex.width == w && tex.height == h)
                {
                    // Quét tìm hộp giới hạn pixel có alpha > 0.05
                    int minX = cellX + cellW, maxX = cellX;
                    int minY = cellY + cellH, maxY = cellY;
                    bool hasPixel = false;

                    // Chỉ quét bên trong cell (chừa mép an toàn 3px bên trong để không chạm cell cạnh)
                    for (int py = cellY + 3; py < cellY + cellH - 3; py++)
                    {
                        for (int px = cellX + 3; px < cellX + cellW - 3; px++)
                        {
                            Color clr = tex.GetPixel(px, py);
                            if (clr.a > 0.15f)
                            {
                                if (px < minX) minX = px;
                                if (px > maxX) maxX = px;
                                if (py < minY) minY = py;
                                if (py > maxY) maxY = py;
                                hasPixel = true;
                            }
                        }
                    }

                    if (hasPixel)
                    {
                        // Thêm 1px margin an toàn nhưng kẹp chặt trong ô cách mép ít nhất 3px
                        minX = Mathf.Max(cellX + 4, minX - 1);
                        maxX = Mathf.Min(cellX + cellW - 5, maxX + 1);
                        minY = Mathf.Max(cellY + 4, minY - 1);
                        maxY = Mathf.Min(cellY + cellH - 5, maxY + 1);

                        int tightW = Mathf.Max(4, maxX - minX + 1);
                        int tightH = Mathf.Max(4, maxY - minY + 1);
                        res[r * cols + c] = new Rect(minX, minY, tightW, tightH);
                        continue;
                    }
                }

                // Fallback: cắt thụt vào 5px mỗi cạnh để triệt tiêu 100% hiện tượng lem viền
                int insetX = cellX + 5;
                int insetY = cellY + 5;
                int insetW = Mathf.Max(1, cellW - 10);
                int insetH = Mathf.Max(1, cellH - 10);
                res[r * cols + c] = new Rect(insetX, insetY, insetW, insetH);
            }
        }

        if (tex != null) Object.DestroyImmediate(tex);
        return res;
    }

    /// <summary>Chọn chế độ cắt theo SheetSpec.dungOLuoiDayDu.</summary>
    private static Rect[] TinhRects(SheetSpec s, int w, int h)
    {
        return s.dungOLuoiDayDu
            ? ComputeGridRects(w, h, s.cols, s.rows)
            : ComputeTightRects(s.path, w, h, s.cols, s.rows);
    }

    /// <summary>
    /// MẶC ĐỊNH: mỗi sprite = TRỌN Ô LƯỚI cellW×cellH (không tight-crop). Mọi frame
    /// cùng kích thước ⇒ pivot Bottom-Center không trôi ⇒ animation không giật ngang.
    /// Chỉ gọi được khi w % cols == 0 và h % rows == 0 (KiemTraChiaHet đã lọc trước).
    /// </summary>
    private static Rect[] ComputeGridRects(int w, int h, int cols, int rows)
    {
        cols = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rows);
        int cellW = w / cols;
        int cellH = h / rows;

        var res = new Rect[cols * rows];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int cellX = c * cellW;
                int cellY = h - (r + 1) * cellH; // Rect gốc trái-dưới; hàng 0 = trên cùng ảnh
                res[r * cols + c] = new Rect(cellX, cellY, cellW, cellH);
            }
        }
        return res;
    }

    /// <summary>
    /// CHẶN CỨNG khi lưới không chia hết số nguyên — cắt kiểu ô lưới đầy đủ sẽ lệch
    /// dần (RoundToInt) khiến mỗi frame một kích thước khác nhau. Trả false + thông
    /// báo rõ để đội vẽ sửa canvas, KHÔNG ném exception.
    /// </summary>
    private static bool KiemTraChiaHet(int w, int h, int cols, int rows, out string loi)
    {
        loi = null;
        if (cols <= 0 || rows <= 0)
        {
            loi = $"cols/rows không hợp lệ ({cols}×{rows}).";
            return false;
        }
        if (w % cols != 0)
        {
            loi = $"canvas {w}×{h} không chia hết cho {cols} cột ({(float)w / cols:0.##}px) — yêu cầu đội vẽ sửa canvas.";
            return false;
        }
        if (h % rows != 0)
        {
            loi = $"canvas {w}×{h} không chia hết cho {rows} hàng ({(float)h / rows:0.##}px) — yêu cầu đội vẽ sửa canvas.";
            return false;
        }
        return true;
    }

    /// <summary>Số đo nghiệm thu 1 ô lưới cho report DRY-RUN.</summary>
    private struct CellDo
    {
        public bool coNoiDung; // ô có pixel alpha > ngưỡng không
        public bool tranBien;  // có pixel alpha > ngưỡng NẰM NGAY trên mép ô (nghi dính frame cạnh)
        public int  baseline;  // khoảng cách đáy nội dung → đáy ô (px); -1 nếu ô trống
    }

    /// <summary>
    /// Quét pixel alpha (ngưỡng 32/255) theo TỪNG Ô LƯỚI để đo baseline và phát hiện
    /// nội dung tràn ra sát mép ô — dùng GetPixels32 một lần cho cả sheet để đủ nhanh
    /// chạy trong DRY-RUN. Không load được ảnh ⇒ trả mảng rỗng (coNoiDung=false hết),
    /// KHÔNG ném exception.
    /// </summary>
    private static CellDo[] DoNoiDungTheoO(string path, int w, int h, int cols, int rows)
    {
        var res = new CellDo[cols * rows];
        Texture2D tex = null;
        Color32[] px  = null;
        try
        {
            if (File.Exists(path))
            {
                byte[] raw = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2);
                tex.LoadImage(raw);
                if (tex.width == w && tex.height == h) px = tex.GetPixels32();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[CharacterSheetSliceTool] Không load được raw texture để đo nội dung: " + ex.Message);
        }
        if (tex != null) Object.DestroyImmediate(tex);
        if (px == null) return res;

        int cellW = w / cols;
        int cellH = h / rows;
        const byte nguong = 32;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int cellX = c * cellW;
                int cellYBottom = h - (r + 1) * cellH;
                int minY = int.MaxValue;
                bool co = false, tran = false;

                for (int py = cellYBottom; py < cellYBottom + cellH; py++)
                {
                    bool hangBien = py == cellYBottom || py == cellYBottom + cellH - 1;
                    int rowBase = py * w;
                    for (int pxi = cellX; pxi < cellX + cellW; pxi++)
                    {
                        if (px[rowBase + pxi].a <= nguong) continue;
                        co = true;
                        if (py < minY) minY = py;
                        bool cotBien = pxi == cellX || pxi == cellX + cellW - 1;
                        if (hangBien || cotBien) tran = true;
                    }
                }

                res[r * cols + c] = new CellDo
                {
                    coNoiDung = co,
                    tranBien  = tran,
                    baseline  = co ? (minY - cellYBottom) : -1,
                };
            }
        }
        return res;
    }

    /// <summary>
    /// Đọc kích thước PNG từ IHDR (byte 16..23, big-endian) — KHÔNG qua Texture2D.
    /// Vì sao: Texture2D đã import có thể bị maxTextureSize co nhỏ ⇒ đọc ra số SAI
    /// và rect cắt sẽ lệch. Đọc header là số THẬT của file art.
    /// </summary>
    private static bool TryReadPngSize(string assetPath, out int w, out int h)
    {
        w = 0; h = 0;
        if (string.IsNullOrEmpty(assetPath)) return false;

        try
        {
            if (!File.Exists(assetPath)) return false;
            using (var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read))
            {
                var b = new byte[24];
                if (fs.Read(b, 0, 24) < 24) return false;
                if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
                w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return w > 0 && h > 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(Tag + " không đọc được header PNG " + assetPath + " — " + e.Message);
            return false;
        }
    }

    private static int DemSpriteCon(string assetPath)
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (all == null) return 0;
        int n = 0;
        for (int i = 0; i < all.Length; i++) if (all[i] is Sprite) n++;
        return n;
    }

    private static void KiemTenSprite(SheetSpec s, out List<string> thieu, out List<string> la)
    {
        thieu = new List<string>();
        la    = new List<string>();

        var coTrenDia = new HashSet<string>();
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(s.path);
        if (all != null)
        {
            for (int i = 0; i < all.Length; i++)
            {
                var sp = all[i] as Sprite;
                if (sp != null) coTrenDia.Add(sp.name);
            }
        }

        var mongDoi = new HashSet<string>(s.names);
        for (int i = 0; i < s.names.Length; i++)
            if (!coTrenDia.Contains(s.names[i])) thieu.Add(s.names[i]);

        foreach (string t in coTrenDia)
            if (!mongDoi.Contains(t)) la.Add(t);

        la.Sort(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Tool khác (BuilderWorkerSetupTool / ShipperSetupTool) gọi hàm này để lấy
    /// sprite con theo TÊN, khỏi phải tự lặp LoadAllAssetsAtPath.
    /// Thiếu bất kỳ tên nào ⇒ trả null (KHÔNG trả mảng lỗ để tool sau khỏi crash).
    /// </summary>
    public static Sprite[] LoadFramesByName(string assetPath, string[] names)
    {
        if (string.IsNullOrEmpty(assetPath) || names == null || names.Length == 0) return null;

        var map = new Dictionary<string, Sprite>();
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (all == null) return null;
        for (int i = 0; i < all.Length; i++)
        {
            var sp = all[i] as Sprite;
            if (sp != null && !map.ContainsKey(sp.name)) map[sp.name] = sp;
        }

        var res = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            Sprite sp;
            if (!map.TryGetValue(names[i], out sp) || sp == null) return null;
            res[i] = sp;
        }
        return res;
    }

    /// <summary>12 tên frame theo thứ tự §5.1 mà FourDirWalkAnimator.SetupFromFlat mong đợi.</summary>
    public static string[] FlowerGirlFrameNames()
    {
        var res = new string[12];
        string[] huong = { "down", "left", "right", "up" };
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
                res[r * 3 + c] = "fg_" + huong[r] + "_" + (c + 1);
        return res;
    }

    /// <summary>hammer_01..12 (hoặc celebrate_01..12) theo thứ tự đọc phẳng.</summary>
    public static string[] WorkerFrameNames(string prefix)
    {
        var res = new string[12];
        for (int i = 0; i < 12; i++) res[i] = prefix + (i + 1).ToString("00");
        return res;
    }
}
