#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MayAnim.EditorTools
{
    /// <summary>
    /// TOOL DỰNG ANIM 2 CÔNG TRÌNH MÁY SẢN XUẤT — Máy làm thức ăn gia súc · Máy xay mía.
    ///
    /// Menu:
    ///   · Tools/Farm/Setup Anim 2 May (Xay Mia · Thuc An Gia Suc)      → làm trọn gói
    ///   · Tools/Farm/Setup Anim 2 May/Kiem Tra Sheet (bao cao)         → chạy khô, KHÔNG ghi đĩa
    ///   · Tools/Farm/Setup Anim 2 May/Chi Cat Lai Sprite               → chỉ cắt sprite
    ///   · Tools/Farm/Setup Anim 2 May/Ap Lai Scale                     → chỉ áp lại localScale
    ///
    /// ══ TÁI DÙNG KHUÔN MẪU ══
    /// Cách làm việc với Unity COPY từ Assets/NV_NPC/Editor/NpcWaveSetupTool.cs (bản thân nó copy
    /// từ Assets/NV_CHEF/Editor/ChefSetupTool.cs):
    ///   · ghi sprite rect/pivot qua SpriteDataProviderFactories + ISpriteEditorDataProvider
    ///     (KHÔNG dùng TextureImporter.spritesheet đã deprecated),
    ///   · giữ ổn định fileID theo TÊN sprite bằng ISpriteNameFileIdDataProvider → cắt lại lần 2
    ///     KHÔNG làm .anim/.prefab mất tham chiếu sprite,
    ///   · tạo AnimationClip bằng AnimationUtility.SetObjectReferenceCurve trên binding
    ///     (SpriteRenderer, path "", "m_Sprite") + 1 keyframe đuôi,
    ///   · đọc pixel bằng ImageConversion.LoadImage từ byte PNG trên đĩa (KHÔNG bật isReadable,
    ///     KHÔNG làm bẩn .meta),
    ///   · CẬP NHẬT TẠI CHỖ, không xoá-rồi-tạo-lại → GUID không đổi, instance trong scene không bị
    ///     "Missing Prefab".
    ///
    /// ══ KHÁC HẲN TOOL NPC Ở 4 ĐIỂM (cố ý — công trình KHÔNG phải nhân vật) ══
    /// 1) RECT DÙNG CHUNG (union của mọi ô) thay vì bbox chặt riêng từng frame.
    ///    Đo thực tế trên 2 sheet: chân đế lệch 0px, cạnh trái lệch 1px, cạnh phải lệch 1–2px —
    ///    công trình ĐỨNG BẤT ĐỘNG, chỉ khói / bánh nước / mực nước mía đổi. Nếu cắt bbox chặt từng
    ///    frame như tool NPC thì cụm khói mọc cao thêm 12px (máy thức ăn) / 3px (xay mía) sẽ làm
    ///    bbox đổi mỗi frame → công trình nhấp nhô. Rect chung khoá cứng khung → giật = 0.
    /// 2) PIVOT = BOTTOM-CENTER (0.5, 0), KHÔNG phải tâm bàn chân. NPC cần pivot theo bàn chân vì
    ///    nhân vật trôi ngang 46–70px giữa các frame; công trình thì KHÔNG trôi (lệch 0–2px) nên
    ///    Bottom-Center là đủ và đơn giản hơn. ĐỪNG bắt chước chỗ pivot Custom của tool NPC.
    /// 3) KHÔNG gắn ChefYSort. ChefYSort tính sortingOrder theo position.y — đúng cho NHÂN VẬT đi
    ///    lại, sai cho CÔNG TRÌNH đứng một chỗ (order sẽ nhảy khi designer kéo nhà lên/xuống).
    ///    Mọi công trình có sẵn của dự án đều ghi cứng m_SortingOrder: 500 → làm y như vậy.
    /// 4) SCALE DATA-DRIVEN, CANH THEO CHIỀU CAO. Frame chỉ ~130–136px trong khi ảnh công trình
    ///    chuẩn của dự án là 500x500px ở localScale 700. Để scale 100 thì máy bé như hạt gạo.
    ///    Canh theo CHIỀU CAO (0.62x nhà), KHÔNG canh theo chiều rộng: người chơi cảm nhận độ to
    ///    qua chiều cao, và 2 sheet khác tỉ lệ khung (136x138 gần vuông vs 130x106 nằm ngang) nên
    ///    canh rộng thì chiều cao luôn lệch. Xem mục SCALE bên dưới.
    ///
    /// ══ CẠM BẪY ĐÃ GẶP THẬT, ĐỪNG LẶP LẠI ══
    /// · KHÔNG dùng GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;() — Unity trả "fake-null", toán tử ??
    ///   so sánh THAM CHIẾU nên không thêm component rồi dòng sau nổ MissingComponentException.
    ///   Dùng helper LayHoacThem&lt;T&gt;() viết bằng "== null".
    /// · KHÔNG bọc AssetDatabase.StartAssetEditing() quanh đoạn cắt sprite — nó HOÃN import nên
    ///   LoadAllAssetsAtPath() ngay sau SaveAndReimport() trả về rỗng → clip rỗng.
    /// · RectInt.xMax/yMax là EXCLUSIVE (= x + width). Vòng lặp đọc pixel phải dùng
    ///   "x &lt; rect.x + rect.width", viết "x &lt;= rect.xMax" là lố 1 pixel và vượt biên mảng.
    /// </summary>
    public static class MayAnimSetupTool
    {
        // ═════════════════════════════════════════════════════════════════════════
        // HẰNG SỐ
        // ═════════════════════════════════════════════════════════════════════════
        private const string Prefix = "[MAYANIM]";

        private const float PixelsPerUnit = 100f;   // PPU chuẩn dự án
        /// <summary>
        /// 8fps cho khói / máy móc — CHẬM HƠN NPC (10fps) là cố ý: đây là chuyển động "nặng"
        /// (khói cuộn, bánh nước quay, nước dâng). 10fps làm khói trông như giật điện.
        /// 12 frame @ 8fps = 1.5s, 16 frame @ 8fps = 2.0s — đúng nhịp máy chạy.
        /// </summary>
        private const int   Fps          = 8;
        private const string SortLayer   = "Objects";
        /// <summary>
        /// CỐ ĐỊNH 500, không tính theo y. Mọi công trình có sẵn của dự án đều ghi
        /// m_SortingOrder: 500 trong scene. Công trình đứng yên nên order động (ChefYSort) chỉ
        /// gây rủi ro chứ không được lợi gì.
        /// </summary>
        private const int   SortingOrder = 500;

        // ── Thuật toán cắt ───────────────────────────────────────────────────────
        /// <summary>alpha &gt; 8/255 = coi là có nội dung (đã đo tay trên cả 2 sheet).</summary>
        private const byte  AlphaNguong        = 8;
        /// <summary>Dải hẹp hơn mức này = nét vẽ rời / số thứ tự hoạ sĩ ghi → LOẠI.</summary>
        private const int   BeRongDaiToiThieu  = 20;
        /// <summary>Dải cao hơn TRUNG VỊ x mức này thì coi là 2 hàng DÍNH NHAU → phải tách.</summary>
        private const float TyLeCaoBatThuong   = 1.5f;
        /// <summary>Cửa sổ tìm đường cắt: +/- 15% chiều cao dải, quanh điểm giữa dải.</summary>
        private const float TyLeCuaSoCat       = 0.15f;
        /// <summary>Chặn vòng lặp tách dải chạy vô hạn nếu sheet quá dị.</summary>
        private const int   SoLanTachToiDa     = 32;

        // ── SCALE — data-driven, CANH THEO CHIỀU CAO ──────────────────────────────
        // CÔNG THỨC:  localScale = ChieuCaoMongMuon / (rectHeightPx / PixelsPerUnit)
        //
        // VÌ SAO CANH THEO CHIỀU CAO, KHÔNG PHẢI CHIỀU RỘNG (lead chốt):
        // Người chơi cảm nhận "công trình to hay nhỏ" qua CHIỀU CAO. Hai sheet lại có tỉ lệ khung
        // KHÁC NHAU — máy thức ăn 136x138 (gần vuông) vs máy xay mía 130x106 (nằm ngang) — nên canh
        // theo chiều rộng thì chiều cao luôn lệch:
        //     canh rộng 2800:  thức ăn cao 2841 (0.81x nhà)  ·  xay mía cao 2283 (0.65x nhà)
        //                      → máy thức ăn trông như "cái nhà thứ hai".
        //     canh CAO  2170:  thức ăn cao 2170 (0.62x nhà)  ·  xay mía cao 2170 (0.62x nhà)
        //                      → HAI MÁY CAO BẰNG NHAU, chiều rộng tự do theo tỉ lệ ảnh.
        // Canh theo chiều cao là cách DUY NHẤT cho 2 máy cao bằng nhau khi 2 sheet khác tỉ lệ khung.
        //
        // MẶC ĐỊNH tool ĐO chiều cao world THỰC TẾ của một công trình có sẵn trong scene đang mở
        // (ưu tiên object tên chứa "House_"), rồi lấy 0.62x con số đó.
        // Không đo được gì thì dùng công thức nhà chuẩn của dự án:
        //     ảnh nhà 500px / PPU 100 x localScale 700 = 3500 unit  →  3500 x 0.62 = 2170 unit.
        //
        // MUỐN SỬA TAY: đặt UuTienDoTrongScene = false rồi sửa ChieuCaoMongMuonThuCong.
        // Sau đó chạy menu "Ap Lai Scale" — KHÔNG cần cắt lại sprite.
        // static readonly, KHÔNG const: nếu để const thì nhánh "if (!UuTienDoTrongScene)" thành
        // hằng false lúc biên dịch và Unity spam warning CS0162 "Unreachable code detected".
        private static readonly bool UuTienDoTrongScene = true;
        /// <summary>Máy cao bằng 0.62 lần chiều cao công trình nhà tham chiếu.</summary>
        private const float TiLeCaoSoVoiNha         = 0.62f;
        private const float ChieuCaoNhaChuan        = 500f / PixelsPerUnit * 700f; // = 3500 unit
        private const float ChieuCaoMongMuonThuCong = ChieuCaoNhaChuan * TiLeCaoSoVoiNha; // = 2170 unit
        private const string TienToTenNha           = "House_";

        // ── Scene ────────────────────────────────────────────────────────────────
        private const string TenObjectCha = "MAY_ANIM";
        /// <summary>2 máy cách nhau = 1.25 x chiều rộng máy to nhất → chắc chắn không chồng nhau.</summary>
        private const float  TyLeKhoangCach = 1.25f;

        private const string ThuMucGoc  = "Assets/Assetsgame/Nhà/BUIDING_ANIM";
        private const string ThuMucAnim = ThuMucGoc + "/Animations";

        // ═════════════════════════════════════════════════════════════════════════
        // KHAI BÁO 2 MÁY
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Một clip: tên, danh sách index frame (0-based, row-major), fps, loop, ghi chú.</summary>
        private sealed class ClipSpec
        {
            public readonly string ten;
            public readonly int[]  frames;
            public readonly int    fps;
            public readonly bool   loop;
            public readonly string ghiChu;

            public ClipSpec(string ten, int[] frames, int fps, bool loop, string ghiChu)
            { this.ten = ten; this.frames = frames; this.fps = fps; this.loop = loop; this.ghiChu = ghiChu; }

            public float ThoiLuong => frames.Length / (float)fps;
        }

        /// <summary>Một máy: sheet PNG, lưới kỳ vọng, bộ clip, clip mặc định. Kết quả đo được ghi ngược vào đây.</summary>
        private sealed class MaySpec
        {
            public string     tenHienThi;
            public string     pngPath;
            public string     tenNganGon;   // MayThucAn / MayXayMia
            public int        soHang;
            public int        soCot;
            public ClipSpec[] clips;
            public string     clipMacDinh;
            public int        frameTinh;    // sprite gán sẵn cho SpriteRenderer trong prefab

            // ── tool tự điền để in report ────────────────────────────────────────
            public RectInt rectDungChung;   // rect union (px), toạ độ TƯƠNG ĐỐI trong ô
            public float   scaleTinhDuoc;
            public float   rongWorld;
            public float   caoWorld;
            public float   caoNhaThamChieu; // chiều cao công trình nhà dùng làm mốc (world unit)
            /// <summary>Chiều cao máy / chiều cao nhà tham chiếu. Phải ra ~0.62 ở cả 2 máy.</summary>
            public float TiLeCaoSoVoiNhaThucTe =>
                caoNhaThamChieu > 0.0001f ? caoWorld / caoNhaThamChieu : 0f;

            public int SoFrameKyVong => soHang * soCot;
            /// <summary>Tiền tố tên sprite = tên file PNG (vd "mayxaymia" → mayxaymia_00..15).</summary>
            public string TenGocSprite       => Path.GetFileNameWithoutExtension(pngPath);
            public string DuongDanController => $"{ThuMucGoc}/{tenNganGon}.controller";
            public string DuongDanPrefab     => $"{ThuMucGoc}/{tenNganGon}_Anim.prefab";
            public string TenPrefab          => $"{tenNganGon}_Anim";
        }

        /// <summary>
        /// Khai báo 2 máy. THỨ TỰ FRAME là row-major: hàng TRÊN CÙNG trước, trong hàng thì trái→phải.
        /// </summary>
        private static MaySpec[] TaoDanhSachMay() => new[]
        {
            // ─────────────────────────────────────────────────────────────────────
            // MÁY LÀM THỨC ĂN GIA SÚC — 4 cột x 3 hàng = 12 frame
            // Mỗi HÀNG = 1 nhịp khói (cột 1 khói nhỏ mới ra → cột 4 khói cuộn to nhất).
            // 3 hàng = 3 nhịp CÓ BIẾN THỂ (con vật trong cửa sổ đổi, lò lửa sáng/tắt) → chạy cả 12
            // frame đỡ cảm giác lặp máy móc hơn là lặp 4 frame ba lần.
            new MaySpec
            {
                tenHienThi  = "Máy làm thức ăn gia súc",
                pngPath     = ThuMucGoc + "/maylamthucan.png",
                tenNganGon  = "MayThucAn",
                soHang      = 3,
                soCot       = 4,
                frameTinh   = 0,
                clipMacDinh = "MayThucAn_Loop",
                clips = new[]
                {
                    new ClipSpec("MayThucAn_Puff", new[] { 0, 1, 2, 3 }, Fps, true,
                                 "1 nhịp khói: khói nhỏ mới ra → cuộn to nhất"),
                    new ClipSpec("MayThucAn_Loop",
                                 new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, Fps, true,
                                 "MẶC ĐỊNH: 3 nhịp khói có biến thể (con vật + lò lửa đổi) = 1.5s"),
                },
            },
            // ─────────────────────────────────────────────────────────────────────
            // MÁY XAY MÍA — 4 cột x 4 hàng = 16 frame
            // CỘT = pha bánh nước (A→B→C→D, mỗi hàng đúng 1 vòng bánh nước).
            // HÀNG = mức nước mía: hàng1 rỗng · hàng2 thấp · hàng3 lưng · hàng4 đầy.
            // Nhờ vậy tách được 3 clip trạng thái + 1 clip trọn mẻ.
            new MaySpec
            {
                tenHienThi  = "Máy xay mía",
                pngPath     = ThuMucGoc + "/mayxaymia.png",
                tenNganGon  = "MayXayMia",
                soHang      = 4,
                soCot       = 4,
                frameTinh   = 0,
                clipMacDinh = "MayXayMia_Loop",
                clips = new[]
                {
                    new ClipSpec("MayXayMia_Idle",    new[] { 0, 1, 2, 3 }, Fps, true,
                                 "bồn RỖNG, bánh nước vẫn quay (máy nghỉ)"),
                    new ClipSpec("MayXayMia_Working", new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, Fps, true,
                                 "đang ép, nước mía dâng (hàng2 thấp → hàng3 lưng)"),
                    new ClipSpec("MayXayMia_Full",    new[] { 12, 13, 14, 15 }, Fps, true,
                                 "bồn ĐẦY, chờ thu"),
                    new ClipSpec("MayXayMia_Loop",
                                 new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
                                 Fps, true,
                                 "MẶC ĐỊNH: trọn 1 mẻ rỗng → ép → đầy = 2.0s"),
                },
            },
        };

        // ═════════════════════════════════════════════════════════════════════════
        // KIỂU DỮ LIỆU PHÂN TÍCH SHEET
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Kết quả đo 1 ô trên sheet. Toạ độ theo hệ Unity: y = 0 ở ĐÁY ảnh.</summary>
        private sealed class OFrame
        {
            public int     index;        // 0..N row-major
            public int     hang, cot;    // 0-based, hang 0 = hàng TRÊN CÙNG
            public string  tenSprite;    // <base>_00 ..
            public RectInt daiO;         // (x,y) = gốc DẢI nội dung của ô (mốc toạ độ tương đối)
            public RectInt oAnToan;      // biên "ô" dùng để kẹp rect chung
            public RectInt bboxChat;     // bbox chặt TUYỆT ĐỐI của nội dung ô này
            public RectInt relBBox;      // bbox chặt đổi về TƯƠNG ĐỐI trong ô (bboxChat - daiO gốc)
            public RectInt rectCuoi;     // rect THẬT ghi vào .meta (rect chung, đã kẹp)
            public bool    biKepRong;    // rect chung bị kẹp theo CHIỀU RỘNG (nghiêm trọng)
            public bool    biKepCao;     // rect chung bị kẹp theo CHIỀU CAO (vô hại với pivot đáy)
        }

        /// <summary>Dải bị loại vì hẹp hơn BeRongDaiToiThieu.</summary>
        private sealed class DaiBiLoai
        {
            public bool theoCot;   // true = dải cột, false = dải hàng
            public int  bd, kt;    // biên dải (bao gồm 2 đầu)
            public int Be => kt - bd + 1;
        }

        private sealed class KetQuaSheet
        {
            public string pngPath;
            public string tenGoc;
            public int    texW, texH;

            public List<Vector2Int> daiCotThô   = new List<Vector2Int>(); // trái → phải
            public List<Vector2Int> daiCot      = new List<Vector2Int>(); // đã loại dải hẹp
            public List<Vector2Int> daiHangThô  = new List<Vector2Int>(); // trên → dưới
            public List<Vector2Int> daiHang     = new List<Vector2Int>(); // SAU khi tách, trên → dưới
            public List<DaiBiLoai>  biLoai      = new List<DaiBiLoai>();
            /// <summary>Đường cắt đã dùng cho các dải hàng DÍNH NHAU. Toạ độ Unity (y=0 ở đáy).</summary>
            public List<int>        duongCatUnityY = new List<int>();
            /// <summary>Số pixel đục trên đúng dòng cắt — càng nhỏ càng "cắt vào chỗ mỏng nhất".</summary>
            public List<int>        soPxTaiDuongCat = new List<int>();

            public List<OFrame> frames  = new List<OFrame>();
            public List<string> canhBao = new List<string>();
            public List<string> loi     = new List<string>();

            /// <summary>Rect union, toạ độ TƯƠNG ĐỐI trong ô. Đây là "rect dùng chung".</summary>
            public RectInt rectDungChung;
            // Độ lệch giữa các ô (px) — càng nhỏ càng chứng minh "công trình đứng bất động".
            public int lechChanDe, lechTrai, lechPhai, lechDinh;

            public bool Ok => loi.Count == 0 && frames.Count > 0;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 1 — LÀM TẤT CẢ
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Cắt sprite → tạo clip → tạo AnimatorController → tạo/cập nhật prefab → đặt vào scene.</summary>
        [MenuItem("Tools/Farm/Setup Anim 2 May (Xay Mia · Thuc An Gia Suc)", false, 200)]
        public static void LamTatCa()
        {
            var log     = new StringBuilder();
            var specs   = TaoDanhSachMay();
            var prefabs = new List<GameObject>();
            int soLoi   = 0;

            // Đo mốc scale TRƯỚC khi dựng prefab (cần scene đang mở còn nguyên vẹn).
            float caoMongMuon = LayChieuCaoMongMuon(out string nguonScale, out float caoNhaThamChieu);

            log.AppendLine("═══ SETUP ANIM 2 MÁY SẢN XUẤT ═══");
            log.AppendLine($"PPU {PixelsPerUnit:0} · clip {Fps}fps · sorting layer \"{SortLayer}\" · " +
                           $"sortingOrder {SortingOrder} (CỐ ĐỊNH, không ChefYSort) · pivot Bottom-Center (0.5, 0)");
            log.AppendLine($"CANH THEO CHIỀU CAO: chiều cao mong muốn {caoMongMuon:0.#} world unit " +
                           $"= {TiLeCaoSoVoiNha:0.00}x nhà tham chiếu ({caoNhaThamChieu:0.#} unit).");
            log.AppendLine($"  Nguồn số liệu: {nguonScale}");
            log.AppendLine("  (Canh CAO chứ không canh RỘNG để 2 máy cao BẰNG NHAU dù 2 sheet khác " +
                           "tỉ lệ khung: 136x138 gần vuông vs 130x106 nằm ngang.)");
            log.AppendLine();

            // KHÔNG bọc AssetDatabase.StartAssetEditing() quanh đoạn này: nó HOÃN mọi import tới lúc
            // StopAssetEditing, nên LoadAllAssetsAtPath() ngay sau SaveAndReimport() sẽ KHÔNG thấy
            // sprite vừa cắt → clip rỗng. Chậm hơn vài giây nhưng đúng.
            try
            {
                for (int i = 0; i < specs.Length; i++)
                {
                    var spec = specs[i];
                    EditorUtility.DisplayProgressBar("Setup anim 2 máy",
                        $"{spec.tenHienThi} ({i + 1}/{specs.Length})", (i + 0.5f) / specs.Length);

                    log.AppendLine($"───── {spec.tenHienThi} ({spec.pngPath}) ─────");
                    if (!DungMotMay(spec, caoMongMuon, caoNhaThamChieu, log)) soLoi++;
                    log.AppendLine();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var spec in specs)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(spec.DuongDanPrefab);
                if (p != null) prefabs.Add(p);
            }

            // ── Đặt vào scene ────────────────────────────────────────────────────
            float rongToNhat = 0f;
            foreach (var s in specs) if (s.rongWorld > rongToNhat) rongToNhat = s.rongWorld;
            string ketQuaScene = DatVaoScene(prefabs, rongToNhat, log);

            // ── Ghi chú bàn giao ─────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("───── GHI CHÚ NỐI GAMEPLAY (tool CỐ Ý KHÔNG tự nối) ─────");
            log.AppendLine("Controller chỉ có ĐÚNG 1 state = clip _Loop, 0 parameter, 0 trigger, 0 transition.");
            log.AppendLine("Các clip MayThucAn_Puff · MayXayMia_Idle/Working/Full VẪN được tạo thành asset " +
                           $"trong {ThuMucAnim}/ để sau này bạn nối vào gameplay.");
            log.AppendLine("ĐIỂM MÓC ĐÃ XÁC NHẬN CÓ THẬT trong dự án (lead đã đọc trực tiếp file):");
            log.AppendLine("  Assets/_Game/Farm/Scripts/FeedMill/FeedMillController.cs " +
                           "(cùng thư mục còn RotatingGear.cs)");
            log.AppendLine("    · enum State { Idle, Working }");
            log.AppendLine("    · StartWorking()   → chuyển sang Working");
            log.AppendLine("    · StopWorking()    → chuyển về Idle");
            log.AppendLine("    · SetWorking(bool) → đặt trạng thái trực tiếp");
            log.AppendLine("    · IsWorking        → đọc trạng thái hiện tại");
            log.AppendLine("Tool KHÔNG tự thêm state/parameter/Animator.Play() vào FeedMillController vì:");
            log.AppendLine("  · FeedMillController là file có sẵn của dự án, tool này không sửa file có sẵn;");
            log.AppendLine("  · mapping State → clip là quyết định gameplay (Idle dùng _Idle hay _Loop?), " +
                           "không phải việc của tool cắt sprite.");
            log.AppendLine("  Khi nối: thêm parameter bool \"working\" vào .controller, 2 state " +
                           "Idle(_Idle) ↔ Working(_Working), rồi gọi animator.SetBool(\"working\", ...) " +
                           "trong SetWorking(bool) — chỗ đó cũng là nơi StartWorking()/StopWorking() " +
                           "đi qua nên chỉ cần sửa 1 hàm. Máy xay mía có thêm _Full: dùng IsWorking " +
                           "cùng mức nước để chọn giữa _Working và _Full.");

            log.AppendLine();
            log.AppendLine("═══ CẦN BẠN LÀM ═══");
            log.AppendLine("1. Ctrl+S để LƯU SCENE (tool chỉ đánh dấu scene bẩn, không tự lưu hộ).");
            log.AppendLine($"2. Kéo object cha \"{TenObjectCha}\" (đã chọn sẵn + ping) về đúng chỗ bạn muốn " +
                           "2 máy đứng. Pivot ở ĐÁY GIỮA nên position.y CHÍNH LÀ mặt đất máy đứng.");
            log.AppendLine($"3. KIỂM TRA SORTING: Project Settings > Tags and Layers phải có sorting layer " +
                           $"\"{SortLayer}\". Nếu máy bị nhà che, đổi sortingOrder trong prefab " +
                           "(hằng số SortingOrder trong file này) — công trình dùng order CỐ ĐỊNH.");
            log.AppendLine("4. KIỂM TRA KÍCH THƯỚC: xem bảng scale ở trên. Chưa vừa mắt thì " +
                           "sửa hằng số (TiLeCaoSoVoiNha, hoặc UuTienDoTrongScene = false + " +
                           "ChieuCaoMongMuonThuCong) rồi chạy " +
                           "menu 'Ap Lai Scale' — KHÔNG cần cắt lại sprite.");
            log.AppendLine("5. Bấm Play để xem 2 máy diễn. Muốn nối vào FeedMillController thì đọc mục " +
                           "GHI CHÚ NỐI GAMEPLAY phía trên.");

            Debug.Log($"{Prefix}\n{log}");

            var tomTat = new StringBuilder();
            tomTat.AppendLine(soLoi == 0 ? "HOÀN TẤT — 2/2 máy OK." : $"XONG nhưng có {soLoi} máy LỖI.");
            tomTat.AppendLine();
            foreach (var s in specs)
                tomTat.AppendLine($"· {s.tenHienThi}: {s.clips.Length} clip, {s.TenPrefab}.prefab\n" +
                                  $"  rect {s.rectDungChung.width}x{s.rectDungChung.height}px → " +
                                  $"scale {s.scaleTinhDuoc:0.#} → rộng {s.rongWorld:0} x CAO {s.caoWorld:0} unit " +
                                  $"({s.TiLeCaoSoVoiNhaThucTe:0.00}x nhà)");
            tomTat.AppendLine();
            tomTat.AppendLine($"CANH THEO CHIỀU CAO: {caoMongMuon:0.#} unit = {TiLeCaoSoVoiNha:0.00}x nhà " +
                              $"({caoNhaThamChieu:0.#} unit) → 2 máy CAO BẰNG NHAU.");
            tomTat.AppendLine($"Nguồn: {nguonScale}");
            tomTat.AppendLine("Sửa: TiLeCaoSoVoiNha (hoặc UuTienDoTrongScene = false + " +
                              "ChieuCaoMongMuonThuCong), rồi chạy 'Ap Lai Scale'.");
            tomTat.AppendLine();
            tomTat.AppendLine(ketQuaScene);
            tomTat.AppendLine();
            tomTat.AppendLine("CẦN BẠN LÀM: Ctrl+S lưu scene · kéo MAY_ANIM về đúng chỗ · " +
                              "kiểm tra sorting layer \"Objects\" · clip Idle/Working/Full CHƯA nối vào " +
                              "FeedMillController (cố ý) · xem chi tiết trong Console.");
            EditorUtility.DisplayDialog("Setup anim 2 máy", tomTat.ToString(), "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 2 — KIỂM TRA SHEET (CHẠY KHÔ, KHÔNG GHI ĐĨA)
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Chạy khô: dò dải hàng/cột, in đường cắt đã dùng cho hàng dính nhau, bbox từng ô,
        /// rect dùng chung, độ lệch chân đế/trái/phải/đỉnh, scale + kích thước world dự kiến,
        /// danh sách clip sẽ tạo. TUYỆT ĐỐI KHÔNG ghi gì ra đĩa (không SaveAndReimport, không
        /// CreateAsset, không sửa scene).
        /// </summary>
        [MenuItem("Tools/Farm/Setup Anim 2 May/Kiem Tra Sheet (bao cao)", false, 220)]
        public static void KiemTraSheet()
        {
            var log = new StringBuilder();
            log.AppendLine("═══ BÁO CÁO KIỂM TRA 2 SHEET (CHẠY KHÔ — KHÔNG GHI ĐĨA) ═══");
            log.AppendLine($"Ngưỡng alpha > {AlphaNguong} · loại dải hẹp < {BeRongDaiToiThieu}px · " +
                           $"tách dải cao > {TyLeCaoBatThuong:0.0}x trung vị · " +
                           $"cửa sổ tìm đường cắt +/-{TyLeCuaSoCat * 100:0}% quanh giữa dải");

            float caoMongMuon = LayChieuCaoMongMuon(out string nguonScale, out float caoNhaThamChieu);
            log.AppendLine($"CANH THEO CHIỀU CAO: chiều cao mong muốn {caoMongMuon:0.#} world unit " +
                           $"= {TiLeCaoSoVoiNha:0.00}x nhà tham chiếu ({caoNhaThamChieu:0.#} unit).");
            log.AppendLine($"  Nguồn số liệu: {nguonScale}");
            log.AppendLine();

            var tomTat = new StringBuilder();
            int soLoi = 0;

            foreach (var spec in TaoDanhSachMay())
            {
                log.AppendLine($"───── {spec.tenHienThi} ({spec.pngPath}) ─────");
                var kq = PhanTichSheet(spec);
                InBaoCaoSheet(kq, spec, log, chiTiet: true);

                // Đối chiếu index frame khai báo trong clip với số ô thật sự cắt được.
                int maxIndex = -1;
                foreach (var c in spec.clips) foreach (int f in c.frames) if (f > maxIndex) maxIndex = f;
                if (kq.Ok && maxIndex >= kq.frames.Count)
                    log.AppendLine($"  LỖI: clip khai báo index tối đa #{maxIndex} nhưng chỉ cắt được " +
                                   $"{kq.frames.Count} ô (#0..#{kq.frames.Count - 1}).");

                if (kq.Ok)
                {
                    TinhScale(spec, kq.rectDungChung, caoMongMuon, caoNhaThamChieu);
                    log.AppendLine($"  SCALE DỰ KIẾN (canh theo CHIỀU CAO): {DongReportScale(spec)}");
                    log.AppendLine($"                 nhà tham chiếu: {nguonScale}");
                    log.AppendLine($"                 SỬA: đổi TiLeCaoSoVoiNha (hiện {TiLeCaoSoVoiNha:0.00}), " +
                                   "hoặc đặt UuTienDoTrongScene = false và " +
                                   "ChieuCaoMongMuonThuCong = <số unit bạn muốn> trong " +
                                   "MayAnimSetupTool.cs, rồi chạy menu 'Ap Lai Scale'.");
                }

                log.AppendLine("  Clip SẼ TẠO:");
                foreach (var c in spec.clips)
                    log.AppendLine($"    {c.ten,-20} {c.frames.Length,2} frame @ {c.fps,2}fps = " +
                                   $"{c.ThoiLuong:0.000}s  loop={c.loop}  [{string.Join(",", c.frames)}]  " +
                                   $"({c.ghiChu})" + (c.ten == spec.clipMacDinh ? "   ← STATE MẶC ĐỊNH" : ""));
                log.AppendLine($"  Controller SẼ TẠO: {spec.DuongDanController} (1 state = {spec.clipMacDinh})");
                log.AppendLine($"  Prefab SẼ TẠO:     {spec.DuongDanPrefab}");
                log.AppendLine();

                if (!kq.Ok) soLoi++;
                tomTat.AppendLine($"· {spec.tenHienThi}: {kq.frames.Count}/{spec.SoFrameKyVong} ô · " +
                                  $"{kq.daiCot.Count} dải cột · {kq.daiHang.Count} dải hàng" +
                                  (kq.duongCatUnityY.Count > 0
                                      ? $" (tách {kq.duongCatUnityY.Count} dải dính, cắt tại y=" +
                                        string.Join(",", kq.duongCatUnityY) + ")"
                                      : "") +
                                  $" · rect chung {kq.rectDungChung.width}x{kq.rectDungChung.height}px" +
                                  (kq.Ok ? $" · scale {spec.scaleTinhDuoc:0.#} → rộng {spec.rongWorld:0} x " +
                                           $"CAO {spec.caoWorld:0} unit ({spec.TiLeCaoSoVoiNhaThucTe:0.00}x nhà)" : "") +
                                  $" · {kq.loi.Count} lỗi, {kq.canhBao.Count} cảnh báo");
            }

            Debug.Log($"{Prefix}\n{log}");
            EditorUtility.DisplayDialog("Kiểm tra 2 sheet",
                (soLoi == 0 ? "2/2 sheet ĐẠT.\n\n" : $"{soLoi} sheet KHÔNG ĐẠT — xem Console.\n\n") +
                tomTat +
                "\nCẦN BẠN LÀM: đọc Console để xem dải hàng/cột, đường cắt hàng dính, bbox từng ô, " +
                "rect dùng chung, độ lệch chân đế/trái/phải/đỉnh và scale dự kiến. " +
                "Lệnh này KHÔNG ghi gì ra đĩa.", "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 3 — CHỈ CẮT LẠI SPRITE
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Chỉ chạy bước cắt sprite (rect dùng chung + pivot Bottom-Center), không tạo clip/prefab.</summary>
        [MenuItem("Tools/Farm/Setup Anim 2 May/Chi Cat Lai Sprite", false, 221)]
        public static void ChiCatLaiSprite()
        {
            var log = new StringBuilder();
            log.AppendLine("═══ CHỈ CẮT LẠI SPRITE ═══");
            int soLoi = 0, tongSprite = 0;

            foreach (var spec in TaoDanhSachMay())
            {
                log.AppendLine($"───── {spec.tenHienThi} ─────");
                var kq = PhanTichSheet(spec);
                InBaoCaoSheet(kq, spec, log, chiTiet: false);
                if (!kq.Ok) { soLoi++; log.AppendLine("→ DỪNG máy này, KHÔNG cắt."); continue; }
                if (!GhiSprite(spec, kq, log)) { soLoi++; continue; }
                tongSprite += kq.frames.Count;
            }

            AssetDatabase.Refresh();
            Debug.Log($"{Prefix}\n{log}");
            EditorUtility.DisplayDialog("Cắt lại sprite",
                (soLoi == 0 ? "HOÀN TẤT 2/2 sheet.\n\n" : $"Có {soLoi} sheet LỖI.\n\n") +
                $"Tổng {tongSprite} sprite, rect DÙNG CHUNG, pivot Bottom-Center (0.5, 0).\n\n" +
                "CẦN BẠN LÀM: nếu clip/prefab đã tồn tại thì chạy tiếp menu " +
                "'Setup Anim 2 May (...)' để clip trỏ đúng sprite mới. Chi tiết trong Console.", "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 4 — CHỈ ÁP LẠI SCALE
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Tính lại localScale từ rect sprite HIỆN CÓ trong PNG rồi áp vào prefab + các instance
        /// trong scene. KHÔNG cắt lại sprite, KHÔNG tạo lại clip/controller → nhanh và an toàn,
        /// dùng để tinh chỉnh kích thước bằng cách chạy lại nhiều lần.
        /// </summary>
        [MenuItem("Tools/Farm/Setup Anim 2 May/Ap Lai Scale", false, 222)]
        public static void ApLaiScale()
        {
            var log = new StringBuilder();
            log.AppendLine("═══ ÁP LẠI SCALE (không cắt lại sprite) ═══");

            float caoMongMuon = LayChieuCaoMongMuon(out string nguonScale, out float caoNhaThamChieu);
            log.AppendLine($"CANH THEO CHIỀU CAO: chiều cao mong muốn {caoMongMuon:0.#} world unit " +
                           $"= {TiLeCaoSoVoiNha:0.00}x nhà tham chiếu ({caoNhaThamChieu:0.#} unit).");
            log.AppendLine($"  Nguồn số liệu: {nguonScale}");
            log.AppendLine();

            var specs = TaoDanhSachMay();
            int soLoi = 0, soPrefab = 0, soInstance = 0;

            Undo.SetCurrentGroupName($"{Prefix} Áp lại scale 2 máy");
            int group = Undo.GetCurrentGroup();

            foreach (var spec in specs)
            {
                // Lấy rect từ sprite ĐÃ CẮT trong PNG (không đọc pixel lại → nhanh).
                var sprites = DocSpriteTheoIndex(spec.pngPath, spec.TenGocSprite, spec.SoFrameKyVong);
                Sprite mau = null;
                foreach (var s in sprites) if (s != null) { mau = s; break; }
                if (mau == null)
                {
                    log.AppendLine($"[{spec.tenHienThi}] LỖI: chưa có sprite nào trong {spec.pngPath}. " +
                                   "Chạy 'Chi Cat Lai Sprite' hoặc menu tổng trước.");
                    Debug.LogError($"{Prefix} {spec.tenHienThi}: chưa cắt sprite, không áp được scale.");
                    soLoi++;
                    continue;
                }

                var rect = new RectInt(0, 0, Mathf.RoundToInt(mau.rect.width), Mathf.RoundToInt(mau.rect.height));
                TinhScale(spec, rect, caoMongMuon, caoNhaThamChieu);

                log.AppendLine($"[{spec.tenHienThi}] {DongReportScale(spec)}");

                // ── Prefab ───────────────────────────────────────────────────────
                if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.DuongDanPrefab) == null)
                {
                    log.AppendLine($"  Bỏ qua prefab: {spec.DuongDanPrefab} chưa tồn tại.");
                }
                else
                {
                    var root = PrefabUtility.LoadPrefabContents(spec.DuongDanPrefab);
                    if (root == null) { log.AppendLine("  LỖI: không mở được prefab."); soLoi++; continue; }
                    try
                    {
                        root.transform.localScale = new Vector3(spec.scaleTinhDuoc, spec.scaleTinhDuoc, 1f);
                        PrefabUtility.SaveAsPrefabAsset(root, spec.DuongDanPrefab, out bool ok);
                        if (ok) { soPrefab++; log.AppendLine("  Prefab: ĐÃ CẬP NHẬT localScale (giữ GUID)."); }
                        else    { soLoi++;    log.AppendLine("  LỖI: lưu prefab thất bại."); }
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
            }

            // ── Instance trong scene ─────────────────────────────────────────────
            // Instance KHÔNG có override scale thì tự ăn theo prefab. Nhưng nếu ai đã kéo scale tay
            // (thành override) thì prefab đổi cũng không ăn → ghi đè thẳng cho chắc.
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                GameObject cha = TimTrongScene(scene, TenObjectCha);
                if (cha != null)
                {
                    foreach (var spec in specs)
                    {
                        if (spec.scaleTinhDuoc <= 0f) continue;
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.DuongDanPrefab);
                        if (prefab == null) continue;
                        var inst = TimInstance(cha.transform, prefab);
                        if (inst == null) continue;
                        Undo.RecordObject(inst.transform, "Áp scale " + spec.TenPrefab);
                        inst.transform.localScale = new Vector3(spec.scaleTinhDuoc, spec.scaleTinhDuoc, 1f);
                        soInstance++;
                        log.AppendLine($"[scene] '{inst.name}': localScale ← {spec.scaleTinhDuoc:0.###}");
                    }
                    if (soInstance > 0)
                    {
                        Selection.activeGameObject = cha;
                        EditorGUIUtility.PingObject(cha);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
                else log.AppendLine($"[scene] Không có object cha \"{TenObjectCha}\" → bỏ qua instance.");
            }
            else log.AppendLine("[scene] Không có scene nào đang mở → bỏ qua instance.");

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine("═══ CẦN BẠN LÀM ═══");
            log.AppendLine("1. Ctrl+S để LƯU SCENE nếu có instance được sửa.");
            log.AppendLine($"2. Chưa vừa mắt? Đổi TiLeCaoSoVoiNha (hiện {TiLeCaoSoVoiNha:0.00}), hoặc đặt " +
                           "UuTienDoTrongScene = false + ChieuCaoMongMuonThuCong (world unit) trong " +
                           "MayAnimSetupTool.cs rồi chạy lại menu này. KHÔNG cần cắt lại sprite.");
            Debug.Log($"{Prefix}\n{log}");

            var tt = new StringBuilder();
            tt.AppendLine(soLoi == 0 ? "HOÀN TẤT áp lại scale." : $"XONG nhưng có {soLoi} lỗi — xem Console.");
            tt.AppendLine();
            foreach (var s in specs)
                if (s.scaleTinhDuoc > 0f)
                    tt.AppendLine($"· {s.tenHienThi}: rect {s.rectDungChung.width}x{s.rectDungChung.height}px → " +
                                  $"scale {s.scaleTinhDuoc:0.#} → rộng {s.rongWorld:0} x CAO {s.caoWorld:0} unit " +
                                  $"({s.TiLeCaoSoVoiNhaThucTe:0.00}x nhà)");
            tt.AppendLine();
            tt.AppendLine($"CANH THEO CHIỀU CAO: {caoMongMuon:0.#} unit = {TiLeCaoSoVoiNha:0.00}x nhà " +
                          $"({caoNhaThamChieu:0.#} unit) → 2 máy CAO BẰNG NHAU.");
            tt.AppendLine($"Nguồn: {nguonScale}");
            tt.AppendLine($"Đã sửa {soPrefab} prefab, {soInstance} instance trong scene.");
            tt.AppendLine();
            tt.AppendLine("CẦN BẠN LÀM: Ctrl+S lưu scene · muốn đổi kích thước thì sửa TiLeCaoSoVoiNha " +
                          "(hoặc ChieuCaoMongMuonThuCong kèm UuTienDoTrongScene = false) rồi chạy lại menu này.");
            EditorUtility.DisplayDialog("Áp lại scale", tt.ToString(), "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // DỰNG 1 MÁY
        // ═════════════════════════════════════════════════════════════════════════
        private static bool DungMotMay(MaySpec spec, float caoMongMuon, float caoNhaThamChieu,
                                      StringBuilder log)
        {
            // ── 1) Phân tích ─────────────────────────────────────────────────────
            var kq = PhanTichSheet(spec);
            InBaoCaoSheet(kq, spec, log, chiTiet: false);
            if (!kq.Ok)
            {
                log.AppendLine($"[1] DỪNG {spec.tenHienThi}: phân tích sheet KHÔNG ĐẠT " +
                               $"(cần đúng {spec.SoFrameKyVong} ô = {spec.soHang} hàng x {spec.soCot} cột). " +
                               "KHÔNG cắt bừa.");
                Debug.LogError($"{Prefix} {spec.tenHienThi}: phân tích sheet lỗi — " +
                               string.Join(" | ", kq.loi.Count > 0 ? kq.loi : kq.canhBao));
                return false;
            }

            // ── 2) Cắt sprite ────────────────────────────────────────────────────
            if (!GhiSprite(spec, kq, log)) return false;

            // ── 3) Đọc lại sprite THẬT trong PNG theo đúng thứ tự index ──────────
            var sprites = DocSpriteTheoIndex(spec.pngPath, spec.TenGocSprite, kq.frames.Count);
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] == null)
                {
                    log.AppendLine($"[3] LỖI: không tìm thấy sprite '{spec.TenGocSprite}_{i:00}' trong " +
                                   $"{spec.pngPath} sau khi cắt. Sprite chưa import xong? Chạy lại menu.");
                    Debug.LogError($"{Prefix} thiếu sprite {spec.TenGocSprite}_{i:00}");
                    return false;
                }

            // ── 4) Clip ──────────────────────────────────────────────────────────
            EnsureFolder(ThuMucAnim);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var cs in spec.clips)
            {
                var clip = TaoClip(ThuMucAnim, cs, sprites, log);
                if (clip == null) return false;
                clips[cs.ten] = clip;
                log.AppendLine($"[4] {cs.ten}: {cs.frames.Length} frame @ {cs.fps}fps = {cs.ThoiLuong:0.000}s, " +
                               $"loop={cs.loop} — [{string.Join(",", cs.frames)}]");
            }

            // ── 5) Controller ────────────────────────────────────────────────────
            if (!clips.TryGetValue(spec.clipMacDinh, out var clipMacDinh))
            {
                log.AppendLine($"[5] LỖI: không có clip mặc định '{spec.clipMacDinh}'.");
                return false;
            }
            var controller = TaoController(spec.DuongDanController, clipMacDinh, log);
            if (controller == null) return false;

            // ── 6) Prefab ────────────────────────────────────────────────────────
            TinhScale(spec, kq.rectDungChung, caoMongMuon, caoNhaThamChieu);
            int idxTinh = Mathf.Clamp(spec.frameTinh, 0, sprites.Length - 1);
            return TaoHoacCapNhatPrefab(spec, controller, sprites[idxTinh], log);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 1 — PHÂN TÍCH SHEET
        // Dò dải CỘT (toàn ảnh) → dò dải HÀNG (toàn ảnh) → TÁCH dải hàng quá cao →
        // bbox từng ô → HỢP (union) thành 1 rect dùng chung → kẹp vào ô.
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Đo sheet bằng ALPHA, KHÔNG dùng grid slice.
        /// Toạ độ trả về theo hệ UNITY (y = 0 ở ĐÁY ảnh) để cắm thẳng vào SpriteRect.
        /// Số ô khác kỳ vọng → ghi vào KetQuaSheet.loi, người gọi phải DỪNG (không cắt bừa).
        /// </summary>
        private static KetQuaSheet PhanTichSheet(MaySpec spec)
        {
            string pngPath = spec.pngPath;
            var kq = new KetQuaSheet { pngPath = pngPath, tenGoc = Path.GetFileNameWithoutExtension(pngPath) };

            // Đọc pixel TỪ FILE, không qua AssetDatabase: texture đã import thường isReadable = false
            // → GetPixels32() nổ. Cách này không cần bật/tắt isReadable, không làm bẩn .meta.
            Texture2D tex = TaiPngDocDuoc(pngPath, out string err);
            if (tex == null) { kq.loi.Add(err); return kq; }

            int W = tex.width, H = tex.height;
            kq.texW = W; kq.texH = H;
            Color32[] px = tex.GetPixels32();     // hàng 0 = ĐÁY ảnh (quy ước Unity)
            UnityEngine.Object.DestroyImmediate(tex);

            var mask = new bool[W * H];
            for (int i = 0; i < px.Length; i++) mask[i] = px[i].a > AlphaNguong;

            // ── 1) DẢI CỘT ───────────────────────────────────────────────────────
            var coNoiDungTheoX = new bool[W];
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    if (mask[y * W + x]) { coNoiDungTheoX[x] = true; break; }

            var cotThô = TimDoan(coNoiDungTheoX);        // trái → phải
            kq.daiCotThô.AddRange(cotThô);
            foreach (var d in cotThô)
            {
                if (d.y - d.x + 1 >= BeRongDaiToiThieu) kq.daiCot.Add(d);
                else kq.biLoai.Add(new DaiBiLoai { theoCot = true, bd = d.x, kt = d.y });
            }
            if (kq.daiCot.Count != spec.soCot)
                kq.loi.Add($"Dò được {kq.daiCot.Count} dải CỘT (thô {cotThô.Count}, loại " +
                           $"{cotThô.Count - kq.daiCot.Count} dải hẹp < {BeRongDaiToiThieu}px), " +
                           $"KỲ VỌNG {spec.soCot}. KHÔNG cắt bừa — kiểm tra lại PNG.");

            // ── 2) DẢI HÀNG ──────────────────────────────────────────────────────
            var coNoiDungTheoY = new bool[H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (mask[y * W + x]) { coNoiDungTheoY[y] = true; break; }

            var hangThô = TimDoan(coNoiDungTheoY);       // y TĂNG = từ ĐÁY lên
            var hangGiu = new List<Vector2Int>();
            foreach (var d in hangThô)
            {
                if (d.y - d.x + 1 >= BeRongDaiToiThieu) hangGiu.Add(d);
                else kq.biLoai.Add(new DaiBiLoai { theoCot = false, bd = d.x, kt = d.y });
            }
            if (hangGiu.Count == 0)
            {
                kq.loi.Add($"Ảnh không có dải hàng nào (alpha > {AlphaNguong}). Ảnh trống?");
                return kq;
            }

            // ── 2b) TÁCH DẢI HÀNG DÍNH NHAU ──────────────────────────────────────
            TachDaiQuaCao(mask, W, hangGiu, kq);

            // Đảo lại để index 0 = hàng TRÊN CÙNG (row-major, đúng thứ tự đọc của người).
            var hangTrenTruoc = new List<Vector2Int>(hangGiu);
            hangTrenTruoc.Reverse();
            kq.daiHang.AddRange(hangTrenTruoc);
            var thôTrenTruoc = new List<Vector2Int>(hangThô);
            thôTrenTruoc.Reverse();
            kq.daiHangThô.AddRange(thôTrenTruoc);

            if (kq.daiHang.Count != spec.soHang)
                kq.loi.Add($"Dò được {kq.daiHang.Count} dải HÀNG sau khi tách (thô {hangThô.Count}), " +
                           $"KỲ VỌNG {spec.soHang}. Sheet có nét vẽ rời hoặc hàng dính nhau mà quy tắc " +
                           $"'cao > {TyLeCaoBatThuong:0.0}x trung vị' không nhận ra. KHÔNG cắt bừa.");

            if (kq.loi.Count > 0) return kq;   // sai lưới thì đừng đo tiếp, số liệu vô nghĩa

            // ── 3) Ô AN TOÀN: biên ô = trung điểm khe giữa 2 dải liền nhau ────────
            // "Ô" ở đây KHÔNG phải dải nội dung mà là vùng đất mà rect chung được phép chiếm.
            // Lấy trung điểm khe để rect chung có thể tràn nhẹ ra ngoài dải nội dung của ô hẹp
            // nhất mà KHÔNG bao giờ liếm sang nội dung của ô bên cạnh.
            int[] cotBd = new int[spec.soCot], cotKt = new int[spec.soCot];
            for (int c = 0; c < spec.soCot; c++)
            {
                cotBd[c] = c == 0 ? 0 : (kq.daiCot[c - 1].y + kq.daiCot[c].x) / 2 + 1;
                cotKt[c] = c == spec.soCot - 1 ? W - 1 : (kq.daiCot[c].y + kq.daiCot[c + 1].x) / 2;
            }
            // daiHang đang là TRÊN → DƯỚI (y giảm dần). Hàng r+1 nằm DƯỚI hàng r.
            int[] hangBd = new int[spec.soHang], hangKt = new int[spec.soHang];
            for (int r = 0; r < spec.soHang; r++)
            {
                // biên DƯỚI của ô r = trung điểm khe với hàng r+1 (nằm dưới)
                hangBd[r] = r == spec.soHang - 1 ? 0 : (kq.daiHang[r + 1].y + kq.daiHang[r].x) / 2 + 1;
                // biên TRÊN của ô r = trung điểm khe với hàng r-1 (nằm trên)
                hangKt[r] = r == 0 ? H - 1 : (kq.daiHang[r].y + kq.daiHang[r - 1].x) / 2;
            }

            // ── 4) bbox chặt từng ô + đổi về toạ độ TƯƠNG ĐỐI trong ô ────────────
            for (int r = 0; r < spec.soHang; r++)
            {
                int y0 = kq.daiHang[r].x, y1 = kq.daiHang[r].y;
                for (int c = 0; c < spec.soCot; c++)
                {
                    int x0 = kq.daiCot[c].x, x1 = kq.daiCot[c].y;
                    var bbox = TinhBBox(mask, W, x0, x1, y0, y1);
                    if (bbox.width <= 0 || bbox.height <= 0)
                    {
                        kq.loi.Add($"Ô hàng {r + 1} cột {c + 1} (x {x0}..{x1}, y {y0}..{y1}) TRỐNG. " +
                                   "Lưới dò sai. KHÔNG cắt bừa.");
                        continue;
                    }

                    int idx = kq.frames.Count;
                    kq.frames.Add(new OFrame
                    {
                        index     = idx,
                        hang      = r,
                        cot       = c,
                        tenSprite = $"{kq.tenGoc}_{idx:00}",
                        daiO      = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1),
                        oAnToan   = new RectInt(cotBd[c], hangBd[r], cotKt[c] - cotBd[c] + 1, hangKt[r] - hangBd[r] + 1),
                        bboxChat  = bbox,
                        relBBox   = new RectInt(bbox.x - x0, bbox.y - y0, bbox.width, bbox.height),
                    });
                }
            }

            if (kq.frames.Count != spec.SoFrameKyVong)
            {
                kq.loi.Add($"TỔNG ra {kq.frames.Count} ô, KỲ VỌNG {spec.SoFrameKyVong} " +
                           $"({spec.soHang} hàng x {spec.soCot} cột). DỪNG.");
                return kq;
            }

            // ── 5) HỢP (UNION) → MỘT RECT DÙNG CHUNG ─────────────────────────────
            // ĐÂY LÀ ĐIỂM KHÁC HẲN TOOL NPC. Không lấy bbox chặt riêng từng frame mà lấy hợp của
            // MỌI ô, rồi áp cùng một rect cho mọi frame. Công trình đứng bất động (chân đế lệch 0px)
            // nên rect chung + pivot Bottom-Center cho giật = 0. Nếu cắt bbox chặt từng frame thì
            // cụm khói mọc cao thêm sẽ làm bbox đổi mỗi frame → công trình nhấp nhô.
            int relX0 = int.MaxValue, relY0 = int.MaxValue, relX1 = int.MinValue, relY1 = int.MinValue;
            int traiMin = int.MaxValue, traiMax = int.MinValue;
            int dayMin  = int.MaxValue, dayMax  = int.MinValue;
            int phaiMin = int.MaxValue, phaiMax = int.MinValue;
            int dinhMin = int.MaxValue, dinhMax = int.MinValue;
            foreach (var f in kq.frames)
            {
                int rx0 = f.relBBox.x, ry0 = f.relBBox.y;
                int rx1 = f.relBBox.x + f.relBBox.width  - 1;   // xMax của RectInt là EXCLUSIVE, tự tính
                int ry1 = f.relBBox.y + f.relBBox.height - 1;
                if (rx0 < relX0) relX0 = rx0;
                if (ry0 < relY0) relY0 = ry0;
                if (rx1 > relX1) relX1 = rx1;
                if (ry1 > relY1) relY1 = ry1;

                if (rx0 < traiMin) traiMin = rx0;  if (rx0 > traiMax) traiMax = rx0;
                if (ry0 < dayMin)  dayMin  = ry0;  if (ry0 > dayMax)  dayMax  = ry0;
                if (rx1 < phaiMin) phaiMin = rx1;  if (rx1 > phaiMax) phaiMax = rx1;
                if (ry1 < dinhMin) dinhMin = ry1;  if (ry1 > dinhMax) dinhMax = ry1;
            }
            kq.rectDungChung = new RectInt(relX0, relY0, relX1 - relX0 + 1, relY1 - relY0 + 1);
            kq.lechTrai   = traiMax - traiMin;
            kq.lechChanDe = dayMax  - dayMin;
            kq.lechPhai   = phaiMax - phaiMin;
            kq.lechDinh   = dinhMax - dinhMin;

            if (kq.lechChanDe > 2)
                kq.canhBao.Add($"Chân đế lệch {kq.lechChanDe}px giữa các ô — rect chung + pivot đáy chỉ " +
                               "đứng yên tuyệt đối khi chân đế lệch 0–1px. Lệch nhiều thì art vẽ máy " +
                               "nhấp nhô sẵn, tool không sửa được.");
            if (kq.lechTrai > 3 || kq.lechPhai > 3)
                kq.canhBao.Add($"Cạnh trái lệch {kq.lechTrai}px, cạnh phải lệch {kq.lechPhai}px — " +
                               "máy hơi trôi ngang giữa các frame. Rect chung vẫn khoá khung nên " +
                               "sprite KHÔNG nhảy, chỉ là nội dung bên trong dịch nhẹ.");

            // ── 6) KẸP rect chung vào Ô AN TOÀN + biên texture ───────────────────
            // Gom cảnh báo kẹp thành 1 dòng cho mỗi loại thay vì 12–16 dòng riêng lẻ — không thì
            // Console ngập warning và người đọc bỏ qua luôn cái warning THẬT.
            var dsKepRong = new List<string>();
            var dsKepCao  = new List<string>();
            foreach (var f in kq.frames)
            {
                int x = f.daiO.x + kq.rectDungChung.x;
                int y = f.daiO.y + kq.rectDungChung.y;
                int w = kq.rectDungChung.width;
                int h = kq.rectDungChung.height;

                int gioiHanX = Mathf.Min(f.oAnToan.x + f.oAnToan.width  - 1, W - 1);
                int gioiHanY = Mathf.Min(f.oAnToan.y + f.oAnToan.height - 1, H - 1);
                int wKep = Mathf.Min(w, gioiHanX - x + 1);
                int hKep = Mathf.Min(h, gioiHanY - y + 1);

                // Bảo đảm rect vẫn trùm hết nội dung THẬT của ô này (nếu kẹp quá tay thì là lỗi lưới).
                int canRong = f.relBBox.x + f.relBBox.width  - kq.rectDungChung.x;
                int canCao  = f.relBBox.y + f.relBBox.height - kq.rectDungChung.y;

                if (wKep < w)
                {
                    f.biKepRong = true;
                    dsKepRong.Add($"#{f.index:00}({w}→{wKep})");
                    if (wKep < canRong)
                        kq.loi.Add($"#{f.index:00}: kẹp chiều rộng còn {wKep}px nhưng nội dung ô cần " +
                                   $"{canRong}px → sẽ CẮT MẤT hình. DỪNG.");
                }
                if (hKep < h)
                {
                    f.biKepCao = true;
                    dsKepCao.Add($"#{f.index:00}({h}→{hKep})");
                    if (hKep < canCao)
                        kq.loi.Add($"#{f.index:00}: kẹp chiều cao còn {hKep}px nhưng nội dung ô cần " +
                                   $"{canCao}px → sẽ CẮT MẤT hình. DỪNG.");
                }

                f.rectCuoi = new RectInt(x, y, Mathf.Max(1, wKep), Mathf.Max(1, hKep));
            }

            // NGHIÊM TRỌNG: rộng khác nhau + pivot x = 0.5 → tâm sprite dịch → máy GIẬT NGANG.
            if (dsKepRong.Count > 0)
                kq.canhBao.Add($"KẸP CHIỀU RỘNG ở {dsKepRong.Count} frame: {string.Join(" ", dsKepRong)}. " +
                               "ĐÂY LÀ VẤN ĐỀ THẬT: pivot x = 0.5 nên bề rộng khác nhau = tâm sprite dịch " +
                               "= máy GIẬT NGANG khi đổi frame. Cần nới khe giữa các cột trên PNG, " +
                               "hoặc art vẽ lại cho các cột rộng đều nhau.");

            // VÔ HẠI: pivot y = 0 (đáy) nên cạnh dưới sprite luôn ghim đúng một chỗ; rect thấp hơn
            // chỉ có nghĩa "frame này không vươn cao bằng frame khác" → KHÔNG gây giật.
            // Hơn nữa chính cái kẹp này CHẶN nội dung hàng bên trên lọt vào rect: đo trên
            // mayxaymia.png, nếu áp thẳng rect 130x106 không kẹp thì 55 pixel của hàng trên sẽ
            // lọt vào frame #04..#11 (nhiều nhất 12px ở #04) → hiện thành cục đen nhỏ lơ lửng
            // trên nóc máy, nhấp nháy theo clip. Kẹp xong: 0 pixel lạ.
            if (dsKepCao.Count > 0)
                kq.canhBao.Add($"Kẹp chiều cao ở {dsKepCao.Count} frame: {string.Join(" ", dsKepCao)}. " +
                               "VÔ HẠI — pivot y = 0 nên cạnh dưới sprite vẫn ghim đúng một chỗ, " +
                               "không giật; chỉ là frame đó không vươn cao bằng frame cao nhất. " +
                               "Kẹp này CÒN CẦN THIẾT để chặn nội dung hàng bên trên lọt vào rect " +
                               "(sheet có 2 hàng dính nhau).");

            return kq;
        }

        /// <summary>
        /// TÁCH DẢI HÀNG DÍNH NHAU (quy tắc tổng quát, không hardcode toạ độ).
        ///
        /// VÌ SAO CẦN: ở sheet mayxaymia.png, ngọn lá mía của hàng 2 CHẠM chân đế hàng 1 nên dò
        /// occupancy chỉ ra 3 dải thay vì 4 — dải đầu cao 210px trong khi 2 dải sau chỉ ~103px.
        ///
        /// QUY TẮC: tính chiều cao TRUNG VỊ của các dải; dải nào cao hơn 1.5x trung vị thì TÁCH —
        /// tìm dòng có ÍT PIXEL ĐỤC NHẤT trong khoảng +/-15% quanh điểm giữa dải, cắt tại đó.
        /// Lặp tới khi không còn dải nào quá cao.
        ///
        /// Dòng cắt được gán cho phần TRÊN (dải mới = [cắt .. y1]). Ở mayxaymia quy tắc này ra
        /// đường cắt y = 317 (Unity) = dòng 114 tính từ đỉnh ảnh, chỉ 21 pixel đục — trùng đúng
        /// chỗ đo tay. 21 pixel đó là ngọn lá mía của hàng dưới, nằm ở SÁT ĐÁY sprite hàng trên
        /// (chỗ chân đế) nên lẫn vào chân máy, không thấy được.
        /// </summary>
        /// <param name="dai">Danh sách dải theo y TĂNG (từ đáy lên). Bị SỬA TRỰC TIẾP.</param>
        private static void TachDaiQuaCao(bool[] mask, int W, List<Vector2Int> dai, KetQuaSheet kq)
        {
            int H = mask.Length / W;
            for (int lan = 0; lan < SoLanTachToiDa; lan++)
            {
                float trungVi = TrungVi(dai);
                float nguong  = trungVi * TyLeCaoBatThuong;

                // Chọn dải CAO NHẤT trong số các dải vượt ngưỡng (tách chỗ tệ nhất trước).
                int idx = -1, caoNhat = 0;
                for (int i = 0; i < dai.Count; i++)
                {
                    int h = dai[i].y - dai[i].x + 1;
                    if (h > nguong && h > caoNhat) { caoNhat = h; idx = i; }
                }
                if (idx < 0) return;   // không còn dải nào quá cao → xong

                int y0 = dai[idx].x, y1 = dai[idx].y, cao = y1 - y0 + 1;
                int giua = (y0 + y1) / 2;
                int nuaCuaSo = Mathf.RoundToInt(cao * TyLeCuaSoCat);
                int lo = Mathf.Max(y0 + 1, giua - nuaCuaSo);
                int hi = Mathf.Min(y1 - 1, giua + nuaCuaSo);
                if (lo > hi)
                {
                    kq.canhBao.Add($"Dải y {y0}..{y1} cao {cao}px (> {TyLeCaoBatThuong:0.0}x trung vị " +
                                   $"{trungVi:0.#}) nhưng cửa sổ tìm đường cắt rỗng → KHÔNG tách được.");
                    return;
                }

                // Dòng có ÍT pixel đục nhất trong cửa sổ = chỗ 2 hàng chỉ chạm nhau bằng vài pixel.
                int yCat = lo, itNhat = int.MaxValue;
                for (int y = lo; y <= hi; y++)
                {
                    int dem = 0;
                    int nen = y * W;
                    for (int x = 0; x < W; x++) if (mask[nen + x]) dem++;
                    if (dem < itNhat) { itNhat = dem; yCat = y; }
                }

                kq.duongCatUnityY.Add(yCat);
                kq.soPxTaiDuongCat.Add(itNhat);
                kq.canhBao.Add($"Dải y {y0}..{y1} cao {cao}px = {cao / Mathf.Max(1f, trungVi):0.00}x trung vị " +
                               $"{trungVi:0.#} → 2 HÀNG DÍNH NHAU. Đã TÁCH tại y = {yCat} " +
                               $"(= dòng {H - 1 - yCat} tính từ ĐỈNH ảnh), chỉ {itNhat} pixel đục — " +
                               $"chỗ mỏng nhất trong cửa sổ y {lo}..{hi}.");

                // Dòng cắt thuộc phần TRÊN.
                dai[idx] = new Vector2Int(y0, yCat - 1);
                dai.Insert(idx + 1, new Vector2Int(yCat, y1));
                dai.Sort((a, b) => a.x.CompareTo(b.x));
            }

            kq.canhBao.Add($"Đã tách {SoLanTachToiDa} lần mà vẫn còn dải quá cao → dừng để tránh " +
                           "lặp vô hạn. Kiểm tra lại PNG.");
        }

        /// <summary>Trung vị chiều cao các dải. Số dải CHẴN thì lấy trung bình 2 giá trị giữa.</summary>
        private static float TrungVi(List<Vector2Int> dai)
        {
            var h = new List<int>(dai.Count);
            foreach (var d in dai) h.Add(d.y - d.x + 1);
            h.Sort();
            int n = h.Count;
            if (n == 0) return 0f;
            return (n % 2 == 1) ? h[n / 2] : (h[n / 2 - 1] + h[n / 2]) * 0.5f;
        }

        /// <summary>bbox chặt của phần đục trong khung [x0..x1] x [y0..y1]. Toạ độ Unity (y↑).</summary>
        private static RectInt TinhBBox(bool[] mask, int W, int x0, int x1, int y0, int y1)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (mask[y * W + x])
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
            if (minX > maxX) return new RectInt(x0, y0, 0, 0);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>Tìm các đoạn liên tục true. Trả (start,end) BAO GỒM 2 đầu, index tăng dần.</summary>
        private static List<Vector2Int> TimDoan(bool[] occ)
        {
            var res = new List<Vector2Int>();
            int start = -1;
            for (int i = 0; i < occ.Length; i++)
            {
                if (occ[i] && start < 0) start = i;
                else if (!occ[i] && start >= 0) { res.Add(new Vector2Int(start, i - 1)); start = -1; }
            }
            if (start >= 0) res.Add(new Vector2Int(start, occ.Length - 1));
            return res;
        }

        private static Texture2D TaiPngDocDuoc(string assetPath, out string error)
        {
            error = null;
            string full;
            try { full = Path.GetFullPath(assetPath); }
            catch (Exception e) { error = "Đường dẫn PNG không hợp lệ: " + e.Message; return null; }
            if (!File.Exists(full)) { error = "Không tìm thấy file: " + assetPath; return null; }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(full), false))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                error = "Không giải mã được PNG: " + assetPath;
                return null;
            }
            return tex;
        }

        private static void InBaoCaoSheet(KetQuaSheet kq, MaySpec spec, StringBuilder log, bool chiTiet)
        {
            log.AppendLine($"  Ảnh {kq.texW}x{kq.texH} · dải cột {kq.daiCot.Count}/{spec.soCot} " +
                           $"(thô {kq.daiCotThô.Count}) · dải hàng {kq.daiHang.Count}/{spec.soHang} " +
                           $"(thô {kq.daiHangThô.Count}) · {kq.frames.Count}/{spec.SoFrameKyVong} ô");

            for (int i = 0; i < kq.daiCot.Count; i++)
                log.AppendLine($"    cột {i + 1}: x {kq.daiCot[i].x}..{kq.daiCot[i].y} " +
                               $"(rộng {kq.daiCot[i].y - kq.daiCot[i].x + 1}px)");
            for (int i = 0; i < kq.daiHang.Count; i++)
                log.AppendLine($"    hàng {i + 1} (từ TRÊN): y {kq.daiHang[i].x}..{kq.daiHang[i].y} " +
                               $"(cao {kq.daiHang[i].y - kq.daiHang[i].x + 1}px)");

            if (kq.duongCatUnityY.Count > 0)
            {
                var sb = new StringBuilder("    ĐƯỜNG CẮT dùng cho HÀNG DÍNH NHAU: ");
                for (int i = 0; i < kq.duongCatUnityY.Count; i++)
                    sb.Append($"y={kq.duongCatUnityY[i]} (dòng {kq.texH - 1 - kq.duongCatUnityY[i]} từ đỉnh, " +
                              $"{kq.soPxTaiDuongCat[i]} px đục)  ");
                log.AppendLine(sb.ToString().TrimEnd());
            }
            else log.AppendLine("    ĐƯỜNG CẮT: không cần — các dải hàng đều rời nhau.");

            if (kq.biLoai.Count > 0)
            {
                var sb = new StringBuilder($"    dải ĐÃ LOẠI (hẹp < {BeRongDaiToiThieu}px): ");
                foreach (var b in kq.biLoai)
                    sb.Append($"{(b.theoCot ? "cột" : "hàng")} {b.bd}..{b.kt} (be {b.Be}px)  ");
                log.AppendLine(sb.ToString().TrimEnd());
            }

            if (kq.frames.Count > 0)
            {
                log.AppendLine($"    RECT DÙNG CHUNG (tương đối trong ô) = ({kq.rectDungChung.x}, " +
                               $"{kq.rectDungChung.y}, {kq.rectDungChung.width}x{kq.rectDungChung.height})" +
                               "   ← áp cho MỌI frame, pivot Bottom-Center (0.5, 0)");
                log.AppendLine($"    ĐỘ LỆCH giữa các ô: chân đế {kq.lechChanDe}px · trái {kq.lechTrai}px · " +
                               $"phải {kq.lechPhai}px · đỉnh {kq.lechDinh}px");
                log.AppendLine($"      (chân đế + trái/phải nhỏ = công trình đứng BẤT ĐỘNG → rect chung " +
                               "cho giật 0. Đỉnh lệch nhiều là bình thường: khói/lá mọc cao thấp khác nhau, " +
                               "và CHÍNH VÌ VẬY mới không được cắt bbox chặt từng frame.)");
            }

            if (chiTiet)
                foreach (var f in kq.frames)
                    log.AppendLine($"    #{f.index:00} {f.tenSprite,-18} h{f.hang + 1}c{f.cot + 1}  " +
                                   $"bbox=({f.bboxChat.x,3},{f.bboxChat.y,3},{f.bboxChat.width,3}x{f.bboxChat.height,3}) " +
                                   $"rel=({f.relBBox.x,2},{f.relBBox.y,2})..({f.relBBox.x + f.relBBox.width - 1,3}," +
                                   $"{f.relBBox.y + f.relBBox.height - 1,3})  " +
                                   $"rectCuoi=({f.rectCuoi.x,3},{f.rectCuoi.y,3},{f.rectCuoi.width,3}x{f.rectCuoi.height,3})" +
                                   (f.biKepRong ? "  [KẸP RỘNG]" : "") + (f.biKepCao ? "  [kẹp cao]" : ""));

            foreach (var w in kq.canhBao) { log.AppendLine("    CẢNH BÁO: " + w); Debug.LogWarning($"{Prefix} {kq.pngPath}: {w}"); }
            foreach (var e in kq.loi)     { log.AppendLine("    LỖI: " + e);     Debug.LogError($"{Prefix} {kq.pngPath}: {e}"); }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // GHI SPRITE RECT + PIVOT
        // ═════════════════════════════════════════════════════════════════════════
        private static bool GhiSprite(MaySpec spec, KetQuaSheet kq, StringBuilder log)
        {
            // 2 PNG này CHƯA có .meta (Unity chưa import) → bắt Unity import trước khi lấy importer.
            var importer = AssetImporter.GetAtPath(spec.pngPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(spec.pngPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(spec.pngPath) as TextureImporter;
            }
            if (importer == null)
            {
                log.AppendLine($"[2] LỖI: {spec.pngPath} không phải texture (chưa import được?).");
                Debug.LogError($"{Prefix} {spec.pngPath} không phải texture.");
                return false;
            }

            // ── Thông số import chuẩn dự án ──────────────────────────────────────
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;                          // 100
            importer.textureCompression  = TextureImporterCompression.Uncompressed; // Compression = None
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.sRGBTexture         = true;
            importer.npotScale           = TextureImporterNPOTScale.None;
            importer.maxTextureSize      = Mathf.Max(1024, Mathf.NextPowerOfTwo(Mathf.Max(kq.texW, kq.texH)));
            // Bilinear (KHÔNG Point): art này không phải pixel-art, viền khử răng cưa mềm và prefab
            // còn phóng scale ~2000 → Point sẽ ra răng cưa cứng + sọc dải ở vùng chuyển sắc của khói.
            importer.filterMode          = FilterMode.Bilinear;

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            // FullRect: mesh = đúng khung rect. Tight sinh mesh khác nhau mỗi frame → thêm một nguồn
            // sai lệch vị trí không cần thiết, mà rect chung sinh ra chính là để KHOÁ khung lại.
            ts.spriteMeshType  = SpriteMeshType.FullRect;
            // BottomCenter, KHÔNG Custom: công trình không trôi ngang (lệch 0–2px) nên không cần
            // pivot Custom như tool NPC. Pivot đáy giữa ⇒ position.y = mặt đất máy đứng.
            ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ts.spritePivot     = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(ts);
            importer.SaveAndReimport();

            // Lấy lại importer sau reimport để data provider đọc đúng trạng thái Multiple.
            importer = (TextureImporter)AssetImporter.GetAtPath(spec.pngPath);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dp == null)
            {
                log.AppendLine("[2] LỖI: thiếu package '2D Sprite' (com.unity.2d.sprite). " +
                               "Window > Package Manager > cài '2D Sprite'.");
                Debug.LogError($"{Prefix} thiếu package com.unity.2d.sprite");
                return false;
            }
            dp.InitSpriteEditorDataProvider();

            // GIỮ fileID cũ THEO TÊN sprite → cắt lại lần 2 không làm .anim/.prefab mất tham chiếu.
            var idCu = new Dictionary<string, GUID>();
            foreach (var old in dp.GetSpriteRects())
                if (!idCu.ContainsKey(old.name)) idCu[old.name] = old.spriteID;

            var rects  = new List<SpriteRect>(kq.frames.Count);
            var pairs  = new List<SpriteNameFileIdPair>(kq.frames.Count);
            var tenMoi = new HashSet<string>();

            foreach (var f in kq.frames)
            {
                GUID id = idCu.TryGetValue(f.tenSprite, out var g) ? g : GUID.Generate();
                rects.Add(new SpriteRect
                {
                    name      = f.tenSprite,
                    spriteID  = id,
                    rect      = new Rect(f.rectCuoi.x, f.rectCuoi.y, f.rectCuoi.width, f.rectCuoi.height),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot     = new Vector2(0.5f, 0f),
                    border    = Vector4.zero,
                });
                pairs.Add(new SpriteNameFileIdPair(f.tenSprite, id));
                tenMoi.Add(f.tenSprite);
            }

            // SetSpriteRects THAY THẾ toàn bộ danh sách (không merge) → rect rác của lần cắt trước
            // tự biến mất khỏi .meta. Chạy lại KHÔNG nhân đôi sprite.
            dp.SetSpriteRects(rects.ToArray());
            var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProv != null) nameProv.SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();

            int soKepRong = 0, soKepCao = 0;
            foreach (var f in kq.frames) { if (f.biKepRong) soKepRong++; if (f.biKepCao) soKepCao++; }
            log.AppendLine($"[2] Cắt {rects.Count} sprite · RECT DÙNG CHUNG " +
                           $"{kq.rectDungChung.width}x{kq.rectDungChung.height}px " +
                           $"(kẹp rộng {soKepRong} frame, kẹp cao {soKepCao} frame) · " +
                           $"pivot Bottom-Center (0.5, 0) · PPU {PixelsPerUnit:0} · filter Bilinear · " +
                           "compression None · alphaIsTransparency · FullRect.");

            // KIỂM CHỨNG: đọc lại sprite THẬT trong PNG, còn tên lạ = .meta chưa sạch.
            var conSot = new List<string>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(spec.pngPath))
                if (o is Sprite sp && !tenMoi.Contains(sp.name)) conSot.Add(sp.name);
            if (conSot.Count > 0)
            {
                conSot.Sort();
                log.AppendLine($"[2] CẢNH BÁO: PNG còn {conSot.Count} sprite CŨ không thuộc bộ mới " +
                               $"({string.Join(", ", conSot.GetRange(0, Mathf.Min(5, conSot.Count)))}" +
                               (conSot.Count > 5 ? ", ..." : "") + "). " +
                               "Mở Sprite Editor của PNG, xoá tay các rect lạ rồi chạy lại menu.");
                Debug.LogWarning($"{Prefix} {spec.pngPath}: còn {conSot.Count} sprite cũ ngoài bộ mới.");
            }
            return true;
        }

        /// <summary>Đọc sprite trong PNG theo đúng index: phần tử [i] là "&lt;tenGoc&gt;_ii" (null nếu thiếu).</summary>
        private static Sprite[] DocSpriteTheoIndex(string pngPath, string tenGoc, int soLuong)
        {
            var res = new Sprite[soLuong];
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(pngPath))
            {
                if (!(o is Sprite s)) continue;
                if (!s.name.StartsWith(tenGoc + "_", StringComparison.Ordinal)) continue;
                if (int.TryParse(s.name.Substring(tenGoc.Length + 1), out int i) && i >= 0 && i < soLuong)
                    res[i] = s;
            }
            return res;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 2 — CLIP
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Tạo/CẬP NHẬT clip sprite. Binding: (SpriteRenderer, path "", "m_Sprite") — SpriteRenderer
        /// nằm CÙNG object với Animator (root prefab) nên path rỗng.
        /// Có 1 keyframe ĐUÔI lặp lại frame cuối để frame cuối được hiển thị đủ 1/fps giây
        /// (không có nó thì clip ngắn hơn 1 frame và frame cuối bị "nháy").
        /// Clip đã tồn tại thì GHI ĐÈ nội dung, KHÔNG xoá-tạo-lại → giữ nguyên GUID, controller và
        /// prefab không bị mất tham chiếu.
        /// </summary>
        private static AnimationClip TaoClip(string thuMuc, ClipSpec cs, Sprite[] sprites, StringBuilder log)
        {
            foreach (int i in cs.frames)
                if (i < 0 || i >= sprites.Length || sprites[i] == null)
                {
                    log.AppendLine($"[4] LỖI: clip '{cs.ten}' dùng index #{i} nhưng chỉ có " +
                                   $"{sprites.Length} sprite (#0..#{sprites.Length - 1}).");
                    Debug.LogError($"{Prefix} clip {cs.ten}: index #{i} không hợp lệ.");
                    return null;
                }

            string path = $"{thuMuc}/{cs.ten}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool taoMoi = clip == null;
            if (taoMoi) clip = new AnimationClip();

            clip.frameRate = cs.fps;

            var binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = "",
                propertyName = "m_Sprite",
            };

            // Dọn sạch curve cũ (kể cả binding lạ từ lần chạy trước) trước khi ghi curve mới.
            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, b, null);
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, b, null);

            var keys = new ObjectReferenceKeyframe[cs.frames.Length + 1];
            for (int i = 0; i < cs.frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / (float)cs.fps, value = sprites[cs.frames[i]] };
            keys[cs.frames.Length] = new ObjectReferenceKeyframe
            {
                time  = cs.frames.Length / (float)cs.fps,
                value = sprites[cs.frames[cs.frames.Length - 1]],
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Lấy settings SAU khi gán curve để startTime/stopTime đã đúng, chỉ sửa loop.
            var st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopTime  = cs.loop;
            st.loopBlend = false;   // sprite swap không nội suy được, loopBlend chỉ gây rối
            AnimationUtility.SetAnimationClipSettings(clip, st);

            if (taoMoi) AssetDatabase.CreateAsset(clip, path);
            else        EditorUtility.SetDirty(clip);
            return clip;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3a — ANIMATOR CONTROLLER: ĐÚNG 1 STATE, KHÔNG parameter/trigger
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Controller 1 state duy nhất = clip _Loop, đặt làm defaultState.
        /// KHÔNG trigger, KHÔNG parameter, KHÔNG transition — công trình trang trí chỉ cần 1 vòng
        /// lặp đều. Các clip Puff/Idle/Working/Full vẫn nằm đó dưới dạng asset để sau này nối vào
        /// FeedMillController (xem mục GHI CHÚ NỐI GAMEPLAY trong log).
        /// Controller đã có thì SỬA TẠI CHỖ (giữ GUID) chứ không xoá-tạo-lại.
        /// </summary>
        private static AnimatorController TaoController(string path, AnimationClip clipMacDinh, StringBuilder log)
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            bool taoMoi = c == null;
            if (taoMoi) c = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (c == null)
            {
                log.AppendLine($"[5] LỖI: không tạo được controller {path}.");
                Debug.LogError($"{Prefix} không tạo được controller {path}");
                return null;
            }

            if (c.layers == null || c.layers.Length == 0) c.AddLayer("Base Layer");
            var sm = c.layers[0].stateMachine;

            const string TenState = "Loop";

            // IDEMPOTENT: xoá mọi state không phải "Loop" rồi bảo đảm có đúng 1 state "Loop".
            var canXoa = new List<AnimatorState>();
            AnimatorState state = null;
            foreach (var ch in sm.states)
            {
                if (ch.state == null) continue;
                if (ch.state.name == TenState && state == null) state = ch.state;
                else canXoa.Add(ch.state);
            }
            foreach (var s in canXoa) sm.RemoveState(s);

            sm.entryPosition    = new Vector3(-240, 0);
            sm.anyStatePosition = new Vector3(-240, 90);
            sm.exitPosition     = new Vector3(300, 0);

            if (state == null) state = sm.AddState(TenState, new Vector3(20, 0));
            state.motion    = clipMacDinh;
            state.speed     = 1f;
            sm.defaultState = state;

            EditorUtility.SetDirty(c);
            log.AppendLine($"[5] Controller {path} ({(taoMoi ? "TẠO MỚI" : "CẬP NHẬT tại chỗ, giữ GUID")}): " +
                           $"1 state \"{TenState}\" = {clipMacDinh.name} (defaultState), " +
                           "0 parameter, 0 transition" +
                           (canXoa.Count > 0 ? $"; đã xoá {canXoa.Count} state rác." : "."));
            return c;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3b — SCALE
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// CHIỀU CAO world mong muốn cho MỘT máy (canh theo CHIỀU CAO, không phải chiều rộng —
        /// xem giải thích ở mục hằng số SCALE đầu file).
        /// Ưu tiên ĐO chiều cao một công trình có sẵn trong scene đang mở (tên chứa "House_"), lấy
        /// 0.62x. Không đo được thì lấy công thức nhà chuẩn của dự án
        /// (500px / PPU 100 x localScale 700 = 3500 unit) x 0.62 = 2170 unit.
        /// </summary>
        /// <param name="nguon">Câu mô tả nguồn số liệu để in vào report (đo được ở đâu / đã fallback).</param>
        /// <param name="caoNhaThamChieu">
        /// Chiều cao công trình nhà dùng làm mốc (world unit) — để report in được tỉ lệ máy/nhà.
        /// </param>
        private static float LayChieuCaoMongMuon(out string nguon, out float caoNhaThamChieu)
        {
            if (!UuTienDoTrongScene)
            {
                caoNhaThamChieu = ChieuCaoNhaChuan;
                nguon = $"HẰNG SỐ ChieuCaoMongMuonThuCong = {ChieuCaoMongMuonThuCong:0.#} unit " +
                        "(UuTienDoTrongScene = false)";
                return ChieuCaoMongMuonThuCong;
            }

            float caoMau = DoChieuCaoCongTrinhMau(out string tenMau, out bool laNha);
            if (caoMau > 0.01f)
            {
                caoNhaThamChieu = caoMau;
                nguon = $"ĐO ĐƯỢC TRONG SCENE từ object '{tenMau}' — cao {caoMau:0.#} unit " +
                        (laNha
                            ? $"(khớp tiền tố \"{TienToTenNha}\")"
                            : $"(KHÔNG có object nào tên chứa \"{TienToTenNha}\" → " +
                              "đã FALLBACK sang SpriteRenderer công trình CAO NHẤT trong scene)") +
                        $" x {TiLeCaoSoVoiNha:0.00} = {caoMau * TiLeCaoSoVoiNha:0.#} unit";
                return caoMau * TiLeCaoSoVoiNha;
            }

            caoNhaThamChieu = ChieuCaoNhaChuan;
            nguon = "KHÔNG ĐO ĐƯỢC gì trong scene → FALLBACK công thức nhà chuẩn " +
                    $"500px / PPU {PixelsPerUnit:0} x localScale 700 = {ChieuCaoNhaChuan:0.#} unit " +
                    $"x {TiLeCaoSoVoiNha:0.00} = {ChieuCaoMongMuonThuCong:0.#} unit";
            return ChieuCaoMongMuonThuCong;
        }

        /// <summary>
        /// Đo chiều CAO world THỰC TẾ của một công trình mẫu trong scene đang mở.
        /// Ưu tiên object có tên chứa "House_"; không thấy thì lấy SpriteRenderer có chiều cao
        /// world LỚN NHẤT (công trình bao giờ cũng cao hơn cây/đồ trang trí). Bỏ qua mọi thứ nằm
        /// dưới MAY_ANIM để không tự đo chính mình.
        /// Dùng sprite.rect / sprite.pixelsPerUnit x lossyScale thay vì Renderer.bounds vì bounds
        /// trả 0 khi renderer đang tắt.
        /// </summary>
        private static float DoChieuCaoCongTrinhMau(out string tenMau, out bool laNha)
        {
            tenMau = null; laNha = false;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return 0f;

            float caoNha = 0f; string tenNha = null;
            float caoToNhat = 0f; string tenToNhat = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
                if (srs == null) continue;
                foreach (var sr in srs)
                {
                    if (sr == null || sr.sprite == null) continue;
                    if (NamDuoi(sr.transform, TenObjectCha)) continue;   // đừng tự đo chính mình

                    float ppu = sr.sprite.pixelsPerUnit;
                    if (ppu <= 0f) ppu = PixelsPerUnit;
                    float cao = sr.sprite.rect.height / ppu * Mathf.Abs(sr.transform.lossyScale.y);
                    if (cao <= 0.01f) continue;

                    if (sr.gameObject.name.Contains(TienToTenNha) && cao > caoNha)
                    { caoNha = cao; tenNha = sr.gameObject.name; }
                    if (cao > caoToNhat)
                    { caoToNhat = cao; tenToNhat = sr.gameObject.name; }
                }
            }

            if (caoNha    > 0.01f) { tenMau = tenNha;    laNha = true;  return caoNha; }
            if (caoToNhat > 0.01f) { tenMau = tenToNhat; laNha = false; return caoToNhat; }
            return 0f;
        }

        private static bool NamDuoi(Transform t, string tenTo)
        {
            for (Transform p = t; p != null; p = p.parent)
                if (p.name == tenTo) return true;
            return false;
        }

        /// <summary>
        /// localScale = ChieuCaoMongMuon / (rectHeightPx / PPU) — CANH THEO CHIỀU CAO.
        /// Scale áp ĐỀU cho x và y (giữ tỉ lệ ảnh) nên chiều RỘNG world = rectWidthPx / PPU x scale
        /// và tự do theo tỉ lệ khung của từng sheet. Ghi kết quả vào spec để in report.
        /// </summary>
        private static void TinhScale(MaySpec spec, RectInt rectDungChung,
                                     float caoMongMuon, float caoNhaThamChieu)
        {
            spec.rectDungChung   = rectDungChung;
            spec.caoNhaThamChieu = caoNhaThamChieu;
            float rongUnitO1 = rectDungChung.width  / PixelsPerUnit;
            float caoUnitO1  = rectDungChung.height / PixelsPerUnit;
            // CHIA CHO CHIỀU CAO, không phải chiều rộng — đây chính là thay đổi lead yêu cầu.
            spec.scaleTinhDuoc = caoUnitO1 > 0.0001f ? caoMongMuon / caoUnitO1 : 1f;
            spec.rongWorld     = rongUnitO1 * spec.scaleTinhDuoc;
            spec.caoWorld      = caoUnitO1  * spec.scaleTinhDuoc;
        }

        /// <summary>1 dòng report scale dùng chung cho cả 4 menu — in ĐỦ mọi con số lead cần.</summary>
        private static string DongReportScale(MaySpec spec)
        {
            return $"rect chung {spec.rectDungChung.width}x{spec.rectDungChung.height}px " +
                   $"→ cao {spec.rectDungChung.height / PixelsPerUnit:0.00} unit ở scale 1 " +
                   $"→ localScale = {spec.caoWorld:0.#} / {spec.rectDungChung.height / PixelsPerUnit:0.00} " +
                   $"= {spec.scaleTinhDuoc:0.###} " +
                   $"→ world RỘNG {spec.rongWorld:0.#} x CAO {spec.caoWorld:0.#} unit " +
                   $"({spec.rongWorld / 100f:0.00} x {spec.caoWorld / 100f:0.00} ô) " +
                   $"→ CAO BẰNG {spec.TiLeCaoSoVoiNhaThucTe:0.000}x nhà tham chiếu " +
                   $"({spec.caoNhaThamChieu:0.#} unit)";
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3c — PREFAB
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Lấy component, chưa có thì thêm. BẮT BUỘC viết bằng "== null", KHÔNG dùng toán tử ??.
        ///
        /// Lý do (đã gây crash MissingComponentException thật): GetComponent trả về "fake-null" của
        /// Unity — một object C# KHÁC null nhưng con trỏ native bằng 0. Toán tử ?? so sánh THAM CHIẾU
        /// nên coi nó là có giá trị, KHÔNG thêm component, rồi dòng sau gán sr.sprite là nổ.
        /// Chỉ phép "== null" (UnityEngine.Object nạp chồng toán tử) mới nhận ra fake-null.
        /// </summary>
        private static T LayHoacThem<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>
        /// Prefab root: SpriteRenderer (frame 0) + Animator (controller). KHÔNG Rigidbody2D,
        /// KHÔNG Collider, KHÔNG ChefYSort.
        ///
        /// VÌ SAO KHÔNG ChefYSort: ChefYSort tính sortingOrder = baseOrder - position.y x hệ số —
        /// đúng cho NHÂN VẬT đi lại (đi lên thì bị công trình che), sai cho CÔNG TRÌNH đứng một chỗ
        /// (designer kéo máy lên 50 unit là order nhảy, máy đột ngột lọt sau/trước nhà). Mọi công
        /// trình có sẵn của dự án đều ghi cứng m_SortingOrder: 500 → làm y như vậy.
        ///
        /// Prefab đã có thì mở nội dung ra SỬA TẠI CHỖ rồi lưu lại → GIỮ NGUYÊN GUID, instance đã đặt
        /// trong scene KHÔNG bị "Missing Prefab".
        /// </summary>
        private static bool TaoHoacCapNhatPrefab(MaySpec spec, AnimatorController controller,
                                                 Sprite spriteTinh, StringBuilder log)
        {
            string path = spec.DuongDanPrefab;
            bool coSan  = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

            GameObject root = coSan ? PrefabUtility.LoadPrefabContents(path) : new GameObject(spec.TenPrefab);
            if (root == null)
            {
                log.AppendLine($"[6] LỖI: không mở được prefab {path}.");
                return false;
            }

            try
            {
                root.name = spec.TenPrefab;
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale    = new Vector3(spec.scaleTinhDuoc, spec.scaleTinhDuoc, 1f);

                var sr = LayHoacThem<SpriteRenderer>(root);
                sr.sprite   = spriteTinh;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.color    = Color.white;
                // Đặt sorting layer TỪ TÊN, không kế thừa: dự án có nhiều renderer trỏ sorting layer ID
                // đã bị xoá, copy là dính rác.
                if (SortingLayerTonTai(SortLayer)) sr.sortingLayerName = SortLayer;
                else
                {
                    log.AppendLine($"[6] CẢNH BÁO: không có sorting layer \"{SortLayer}\" trong " +
                                   "Project Settings > Tags and Layers → giữ layer hiện tại.");
                    Debug.LogWarning($"{Prefix} thiếu sorting layer \"{SortLayer}\".");
                }
                sr.sortingOrder = SortingOrder;   // CỐ ĐỊNH 500, khớp mọi công trình của dự án

                var anim = LayHoacThem<Animator>(root);
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
                anim.updateMode      = AnimatorUpdateMode.Normal;
                // AlwaysAnimate: máy nằm ngoài khung nhìn vẫn chạy anim → không bị "đứng hình" đúng
                // lúc camera quét tới (thấy rõ nhất với khói, vì khói đứng im trông như bug).
                anim.cullingMode     = AnimatorCullingMode.AlwaysAnimate;

                PrefabUtility.SaveAsPrefabAsset(root, path, out bool thanhCong);
                if (!thanhCong)
                {
                    log.AppendLine($"[6] LỖI: lưu prefab {path} thất bại.");
                    Debug.LogError($"{Prefix} lưu prefab {path} thất bại.");
                    return false;
                }
            }
            finally
            {
                if (coSan) PrefabUtility.UnloadPrefabContents(root);
                else       UnityEngine.Object.DestroyImmediate(root);
            }

            log.AppendLine($"[6] Prefab {path} ({(coSan ? "CẬP NHẬT tại chỗ, giữ GUID" : "TẠO MỚI")}): " +
                           $"SpriteRenderer({SortLayer}/{SortingOrder} CỐ ĐỊNH, sprite={spriteTinh?.name}) + " +
                           $"Animator({Path.GetFileName(spec.DuongDanController)}, AlwaysAnimate). " +
                           "KHÔNG ChefYSort (đó là cho nhân vật di chuyển; công trình dùng order cố định).");
            log.AppendLine($"[6] SCALE (canh theo CHIỀU CAO): {DongReportScale(spec)}. " +
                           $"SỬA: đổi TiLeCaoSoVoiNha (hiện {TiLeCaoSoVoiNha:0.00}), hoặc " +
                           "UuTienDoTrongScene = false + ChieuCaoMongMuonThuCong, rồi chạy " +
                           "menu 'Ap Lai Scale' (không cần cắt lại sprite).");
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3d — ĐẶT VÀO SCENE (idempotent + Undo được)
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Tạo object cha MAY_ANIM (nếu chưa có) rồi đặt 2 prefab vào, cách nhau
        /// 1.25 x chiều rộng máy to nhất → chắc chắn không chồng nhau.
        /// Chạy lại KHÔNG nhân đôi: instance đã có thì giữ nguyên (kể cả vị trí designer đã kéo).
        /// </summary>
        private static string DatVaoScene(List<GameObject> prefabs, float rongToNhat, StringBuilder log)
        {
            log.AppendLine("───── ĐẶT VÀO SCENE ─────");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                const string m = "Không có scene nào đang mở → BỎ QUA bước đặt vào scene. " +
                                 "Mở scene rồi chạy lại menu.";
                log.AppendLine("[7] " + m);
                Debug.LogWarning($"{Prefix} {m}");
                return m;
            }
            if (prefabs == null || prefabs.Count == 0)
            {
                const string m = "Không có prefab nào để đặt vào scene.";
                log.AppendLine("[7] " + m);
                return m;
            }

            float khoangCach = Mathf.Max(100f, rongToNhat * TyLeKhoangCach);
            log.AppendLine($"[7] Khoảng cách giữa 2 máy = {rongToNhat:0.#} (máy rộng nhất) x " +
                           $"{TyLeKhoangCach:0.00} = {khoangCach:0.#} unit → không chồng nhau.");

            Undo.SetCurrentGroupName($"{Prefix} Đặt 2 máy vào scene");
            int group = Undo.GetCurrentGroup();

            GameObject cha = TimTrongScene(scene, TenObjectCha);
            bool chaMoi = cha == null;
            if (chaMoi)
            {
                cha = new GameObject(TenObjectCha);
                Undo.RegisterCreatedObjectUndo(cha, "Tạo " + TenObjectCha);
                cha.transform.position   = Vector3.zero;
                cha.transform.localScale = Vector3.one;
                log.AppendLine($"[7] Tạo object cha \"{TenObjectCha}\".");
            }
            else log.AppendLine($"[7] Dùng lại object cha \"{TenObjectCha}\" đã có trong scene.");

            var moiTao = new List<GameObject>();
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];

                // Idempotent: đã có instance của prefab này dưới cha thì KHÔNG tạo thêm.
                GameObject daCo = TimInstance(cha.transform, prefab);
                if (daCo != null)
                {
                    log.AppendLine($"[7] '{prefab.name}' đã có trong \"{TenObjectCha}\" tại " +
                                   $"({daCo.transform.position.x:0.#}, {daCo.transform.position.y:0.#}) → " +
                                   "giữ nguyên, không tạo thêm.");
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (go == null) { log.AppendLine($"[7] LỖI: không instantiate được '{prefab.name}'."); continue; }

                Undo.RegisterCreatedObjectUndo(go, "Đặt " + prefab.name);
                Undo.SetTransformParent(go.transform, cha.transform, "Gắn vào " + TenObjectCha);
                go.transform.position = new Vector3(cha.transform.position.x + khoangCach * i,
                                                    cha.transform.position.y,
                                                    cha.transform.position.z);
                moiTao.Add(go);
                log.AppendLine($"[7] Đặt '{go.name}' tại ({go.transform.position.x:0.#}, " +
                               $"{go.transform.position.y:0.#}).");
            }

            Selection.activeGameObject = cha;
            EditorGUIUtility.PingObject(cha);
            var sv = SceneView.lastActiveSceneView;
            if (sv != null && moiTao.Count > 0) sv.FrameSelected();

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);

            string kq = moiTao.Count > 0
                ? $"Đã đặt {moiTao.Count} máy vào \"{TenObjectCha}\" (scene '{scene.name}'), " +
                  $"cách nhau {khoangCach:0.#} unit. Ctrl+S để lưu."
                : $"\"{TenObjectCha}\" đã có đủ máy — không tạo thêm gì.";
            log.AppendLine("[7] " + kq);
            return kq;
        }

        private static GameObject TimTrongScene(Scene scene, string ten)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                if (root.name == ten) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == ten) return t.gameObject;
            }
            return null;
        }

        private static GameObject TimInstance(Transform cha, GameObject prefab)
        {
            foreach (Transform t in cha)
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (src == prefab) return t.gameObject;
                // Prefab bị mất liên kết thì so tên cho chắc — vẫn tính là "đã có", tránh nhân đôi.
                if (src == null && t.name.StartsWith(prefab.name, StringComparison.Ordinal)) return t.gameObject;
            }
            return null;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // TIỆN ÍCH
        // ═════════════════════════════════════════════════════════════════════════
        private static bool SortingLayerTonTai(string n)
        {
            foreach (var l in SortingLayer.layers) if (l.name == n) return true;
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf   = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
