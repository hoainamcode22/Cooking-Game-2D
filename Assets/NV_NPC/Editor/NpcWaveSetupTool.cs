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

namespace NVNpc.EditorTools
{
    /// <summary>
    /// TOOL DỰNG 3 NHÂN VẬT NPC ĐỨNG VẪY TAY — Bà lão hàng rong · Hải quân · Nhân viên tàu lửa.
    ///
    /// Menu:
    ///   · Tools/Farm/Setup 3 Nhan Vat (Ba Lao · Hai Quan · NV Tau Lua)   → làm trọn gói
    ///   · Tools/Farm/Setup 3 Nhan Vat/Chi Cat Lai Sprite                 → chỉ cắt sprite
    ///   · Tools/Farm/Setup 3 Nhan Vat/Kiem Tra Sheet (bao cao)           → chỉ phân tích + in báo cáo
    ///
    /// ══ TÁI DÙNG KHUÔN MẪU NV_CHEF ══
    /// Toàn bộ cách làm việc với Unity ở đây COPY từ Assets/NV_CHEF/Editor/ChefSetupTool.cs:
    ///   · ghi sprite rect/pivot qua SpriteDataProviderFactories + ISpriteEditorDataProvider
    ///     (KHÔNG dùng TextureImporter.spritesheet đã deprecated),
    ///   · giữ ổn định fileID theo TÊN sprite bằng ISpriteNameFileIdDataProvider → cắt lại lần 2
    ///     KHÔNG làm .anim/.prefab mất tham chiếu sprite,
    ///   · tạo AnimationClip bằng AnimationUtility.SetObjectReferenceCurve trên binding
    ///     (SpriteRenderer, path "", "m_Sprite") + 1 keyframe đuôi,
    ///   · đọc pixel bằng ImageConversion.LoadImage từ byte PNG trên đĩa (KHÔNG bật isReadable,
    ///     KHÔNG làm bẩn .meta).
    /// Component Y-sort thì DÙNG LẠI NGUYÊN class ChefYSort (Assets/NV_CHEF/Scripts/ChefYSort.cs,
    /// class global không namespace) — không viết lại, không sửa file trong NV_CHEF.
    ///
    /// ══ KHÁC ChefSetupTool Ở 3 ĐIỂM (cố ý) ══
    /// 1) RECT RIÊNG TỪNG FRAME (bbox chặt) thay vì 1 rect dùng chung. Ở đây pivot là TÂM BÀN CHÂN
    ///    nên chân đã bị "đóng đinh" bởi pivot; rect chung không còn tác dụng gì ngoài việc
    ///    làm sprite to hơn cần thiết.
    /// 2) PIVOT = TÂM BÀN CHÂN (SpriteAlignment.Custom), không phải Bottom-Center. Sheet vẽ nhân vật
    ///    lệch tâm nhiều (đo được tới +10.2px ở bà lão frame #10) → Bottom-Center làm nhân vật
    ///    nhảy ngang mỗi khi đổi frame. Xem GhiSprite().
    /// 3) CẬP NHẬT TẠI CHỖ, KHÔNG xoá-rồi-tạo-lại clip/controller/prefab. ChefSetupTool xoá trước khi
    ///    tạo nên GUID đổi và instance đã đặt trong scene bị "Missing Prefab" (chính README_CHEF
    ///    cũng cảnh báo điều đó). Tool này ghi đè nội dung, giữ nguyên GUID → chạy lại bao nhiêu lần
    ///    cũng không làm vỡ scene.
    /// </summary>
    public static class NpcWaveSetupTool
    {
        // ═════════════════════════════════════════════════════════════════════════
        // HẰNG SỐ DỰ ÁN — giữ y nguyên, đừng đổi lẻ một chỗ
        // ═════════════════════════════════════════════════════════════════════════
        private const string Prefix = "[NPC3]";

        private const float PixelsPerUnit = 100f;   // PPU chuẩn dự án
        private const int   FpsThuong     = 10;     // clip vẫy tay
        private const int   FpsCham       =  6;     // clip đứng/chào (đỡ rung vì các frame gần giống nhau)
        private const string SortLayer    = "Objects";
        private const int   BaseOrder     = 500;    // khớp m_SortingOrder: 500 của công trình
        private const float OrderPerUnitY = 1f;
 
        private const float PrefabScale   = 115f;

        // ── Thuật toán cắt (lead đã phân tích pixel và chốt các số này) ──────────
        private const byte AlphaNguong      = 8;    // alpha > 8/255 = có nội dung
        private const int  BeRongToiThieu   = 35;   // cụm HẸP HƠN mức này = SỐ THỨ TỰ hoạ sĩ ghi ở góc ô → LOẠI
        private const int  KhoangCachGop    = 25;   // 2 cụm cách nhau <= mức này thì thuộc CÙNG 1 nhân vật
        private const float TyLeDongChan    = 0.10f;// tâm bàn chân lấy từ 10% dòng DƯỚI CÙNG của bbox
        private const int  SoHangKyVong     = 3;
        private const int  SoCotKyVong      = 4;
        private const int  SoFrameKyVong    = SoHangKyVong * SoCotKyVong; // = 12

        // Cụm bị loại mà RỘNG hơn mức này thì rất có thể KHÔNG phải chữ số mà là một phần cơ thể.
        private const int  CanhBaoCumToPx   = 20;

        // ── Scene ────────────────────────────────────────────────────────────────
        private const string TenObjectCha = "NPC_Villagers";
        private const string TenChef      = "Chef_NPC";
        private const float  KhoangCachX  = 400f;   // 3 NPC cách nhau 400 world unit theo X

        // ═════════════════════════════════════════════════════════════════════════
        // KHAI BÁO 3 NHÂN VẬT
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Một clip: tên, danh sách index frame (0-based, row-major), fps, có loop hay không.</summary>
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

        /// <summary>Một nhân vật: sheet PNG, chỗ đặt asset, bộ clip, clip mặc định, frame đứng.</summary>
        private sealed class NpcSpec
        {
            public string     tenHienThi;   // để log cho người đọc
            public string     pngPath;      // Assets/.../xxx.png
            public string     tenNganGon;   // BaLao / HaiQuan / NvTauLua — dùng đặt tên clip, controller, prefab
            public ClipSpec[] clips;
            public string     clipMacDinh;  // tên clip làm state mặc định của Animator
            public int        frameDung;    // sprite gán sẵn cho SpriteRenderer trong prefab
            public float      caoWorldUnit; // tool tự điền sau khi tạo prefab, để in ra report

            /// <summary>Tiền tố tên sprite = tên file PNG (vd "balaohangrong" → balaohangrong_00..11).</summary>
            public string TenGocSprite => Path.GetFileNameWithoutExtension(pngPath);
            public string ThuMuc       => Path.GetDirectoryName(pngPath).Replace('\\', '/');
            public string ThuMucAnim   => ThuMuc + "/Animations";
            public string DuongDanController => $"{ThuMuc}/{tenNganGon}.controller";
            public string DuongDanPrefab     => $"{ThuMuc}/{tenNganGon}_NPC.prefab";
            public string TenPrefab          => $"{tenNganGon}_NPC";
        }

        /// <summary>
        /// THỨ TỰ FRAME do lead chốt bằng ma trận khác biệt pixel. Index 0-based, row-major
        /// (hàng TRÊN trước, trong hàng thì trái→phải).
        ///
        /// ══ THƯỚC ĐO ĐỘ MƯỢT ══
        /// Mọi số % dưới đây là XOR SILHOUETTE của 2 frame LIỀN NHAU sau khi đã căn theo tâm bàn chân:
        /// (số pixel khác nhau) / (số pixel hợp) x 100. Càng nhỏ càng mượt. Đo trên chính 3 PNG này.
        ///
        /// ══ ĐÃ SỬA THỨ TỰ FRAME — lead DUYỆT sau khi kiểm lại ảnh ở zoom 1.9x ══
        ///
        /// · BÀ LÃO: #1..#5 vẫy tay BÊN PHẢI (theo người xem), #10/#11 vẫy tay BÊN TRÁI.
        ///   Bản đầu ghép thẳng 10→1 = 21.3% — CẶP TỆ NHẤT TOÀN MA TRẬN — cánh tay "nhảy" sang bên
        ///   kia người trong 1 frame. Đã tách thành 2 clip theo 2 bên tay, và trong _Loop thì
        ///   HẠ TAY VỀ XUÔI TRƯỚC KHI ĐỔI BÊN.
        ///   VÌ SAO PHẢI HẠ TAY: sheet KHÔNG có frame nào tay ở tầm ngang giữa hai bên. Cầu nối rẻ
        ///   nhất giữa nhóm tay xuôi (#6..#9) và nhóm tay giơ là 6↔4 = 15.9%; mọi cặp khác đều đắt
        ///   hơn (1↔6 = 18.3%, 5↔7 = 16.4%). Nên 15.9% là SÀN, không thể thấp hơn bằng cách xếp lại
        ///   frame — muốn mượt hơn nữa thì phải VẼ THÊM frame tay ngang, không phải việc của tool.
        ///   Kết quả: _Loop từ max 21.3% xuống max 15.9%.
        ///
        /// · NV TÀU LỬA: thứ tự [1,3,2,6,5,7,4,9,8,11] rất mượt trong lòng (max 10.4%) nhưng chỗ
        ///   QUẤN VÒNG 11→1 tốn 15.1%. Đã đổi sang PING-PONG (đi rồi về theo đúng thứ tự đó) nên
        ///   không còn bước quấn vòng nào: max 10.4%. Trong _Loop, ra/vào frame đứng #0 đi qua #4
        ///   (0↔4 = 11.9%) thay vì qua #8 (8→0 = 16.7%): max 16.7% xuống 12.0%.
        ///
        /// · HẢI QUÂN: GIỮ NGUYÊN. #7 (đứng nghiêm) là cầu nối RẺ NHẤT giữa nhóm chào (#0..#6) và
        ///   nhóm vẫy (#8..#11); mọi đường khác đều >= 19%. Salute đạt 3.0% so với mức tối ưu tuyệt
        ///   đối 2.7% (đã brute-force chu trình bottleneck). Nit duy nhất là đuôi "...,11,9,7" →
        ///   "...,11,8,7" rẻ hơn 0.3% (17.3% → 17.0%) — lead quyết KHÔNG đổi, không đáng.
        /// </summary>
        private static NpcSpec[] TaoDanhSachNpc() => new[]
        {
            // ─────────────────────────────────────────────────────────────────────
            new NpcSpec
            {
                tenHienThi  = "Bà lão hàng rong",
                pngPath     = "Assets/BAOLAOHANGRONG/balaohangrong.png",
                tenNganGon  = "BaLao",
                frameDung   = 7,          // tay xuôi, hé miệng
                clipMacDinh = "BaLao_Loop",
                clips = new[]
                {
                    // Tay xuôi. 4 frame lệch nhau 2.2–4.7% nên trông như thở nhẹ.
                    // KHÔNG dùng #0 (hai tay xoè, lệch 10.8–13.3% khỏi nhóm đứng → pop).
                    new ClipSpec("BaLao_Idle", new[] { 7, 9, 8, 6 }, FpsCham, true,
                                 "tay xuôi, như thở nhẹ; #6 hé miệng rao hàng"),
                    // Vẫy tay BÊN PHẢI (theo người xem). Ping-pong tay lên #1→#3 rồi hạ #5→#4→#1.
                    // CHỈ dùng #1..#5 — TUYỆT ĐỐI không trộn #10/#11 vào đây (tay bên kia, 21.3%).
                    new ClipSpec("BaLao_Wave", new[] { 1, 2, 3, 5, 4, 3, 2, 1 }, FpsThuong, true,
                                 "vẫy tay BÊN PHẢI (người xem), ping-pong lên rồi xuống"),
                    // Vẫy tay BÊN TRÁI — clip riêng vì sheet chỉ có 2 frame cho bên này (#10, #11).
                    new ClipSpec("BaLao_Wave2", new[] { 10, 11, 10, 11 }, FpsThuong, true,
                                 "vẫy tay BÊN TRÁI (người xem) — sheet chỉ có 2 frame bên này"),
                    // STATE MẶC ĐỊNH: đứng thở → vẫy tay phải → HẠ TAY (6,7) → vẫy tay trái → về đứng.
                    // Hai frame 6,7 ở giữa KHÔNG phải để cho đẹp: đó là chỗ hạ tay bắt buộc trước khi
                    // đổi bên, nếu bỏ đi thì cánh tay nhảy ngang người (21.3%).
                    new ClipSpec("BaLao_Loop",
                                 new[] { 7, 7, 9, 9, 8, 8, 6, 6,
                                         4, 1, 2, 3, 5, 3, 2, 1, 4,
                                         6, 7,
                                         11, 10, 11, 10,
                                         9, 8 },
                                 FpsThuong, true,
                                 "MẶC ĐỊNH: đứng thở → vẫy tay phải → hạ tay → vẫy tay trái → lặp"),
                },
            },
            // ─────────────────────────────────────────────────────────────────────
            new NpcSpec
            {
                tenHienThi  = "Hải quân",
                pngPath     = "Assets/Haiquan/haiquan.png",
                tenNganGon  = "HaiQuan",
                frameDung   = 7,          // hạ tay, đứng nghiêm
                clipMacDinh = "HaiQuan_Loop",
                clips = new[]
                {
                    // Chào tay lên trán. 7 frame lệch nhau 1.0–3.0% → rất mượt, để 6fps cho đỡ "rung".
                    new ClipSpec("HaiQuan_Salute", new[] { 0, 2, 3, 1, 4, 5, 6 }, FpsCham, true,
                                 "chào tay lên trán"),
                    new ClipSpec("HaiQuan_Wave", new[] { 8, 9, 11, 9 }, FpsThuong, true,
                                 "vẫy 1 tay"),
                    // STATE MẶC ĐỊNH: chào → hạ tay (#7) → vẫy → reo hò 2 tay (#10) → hạ tay → lặp.
                    new ClipSpec("HaiQuan_Loop",
                                 new[] { 0, 0, 2, 2, 3, 3, 1, 1, 4, 4, 5, 5, 6, 6,
                                         7,
                                         8, 9, 11, 9, 8, 9, 11,
                                         10, 10,
                                         11, 9,
                                         7 },
                                 FpsThuong, true, "MẶC ĐỊNH: chào → vẫy → reo hò → lặp lại đều"),
                },
            },
            // ─────────────────────────────────────────────────────────────────────
            new NpcSpec
            {
                tenHienThi  = "Nhân viên tàu lửa",
                pngPath     = "Assets/Nhanvientaulua/nhanvientaulua.png",
                tenNganGon  = "NvTauLua",
                frameDung   = 0,          // frame duy nhất tay xuôi
                clipMacDinh = "NvTauLua_Loop",
                clips = new[]
                {
                    // Sheet CHỈ có 1 frame tay xuôi (#0) → clip 1 frame. Đây là giới hạn của art,
                    // không phải lỗi tool.
                    new ClipSpec("NvTauLua_Idle", new[] { 0 }, FpsCham, true,
                                 "đứng yên (sheet chỉ có 1 frame tay xuôi)"),
                    // PING-PONG: đi 1→...→11 rồi VỀ theo đúng đường cũ. Không có bước quấn vòng nào
                    // nên không còn cú pop 11→1 = 15.1% của bản đầu. Đắt nhất chỉ còn 5↔7 = 10.4%.
                    new ClipSpec("NvTauLua_Wave",
                                 new[] { 1, 3, 2, 6, 5, 7, 4, 9, 8, 11,
                                         8, 9, 4, 7, 5, 6, 2, 3 }, FpsThuong, true,
                                 "vẫy tay, ping-pong (đi rồi về cùng đường)"),
                    // STATE MẶC ĐỊNH: ra/vào frame đứng #0 ĐI QUA #4 (0↔4 = 11.9%) — #4 là frame giơ
                    // tay GIỐNG frame đứng nhất. Bản đầu vào #1 (13.4%) và ra #8 (16.7%, chỗ xấu nhất).
                    new ClipSpec("NvTauLua_Loop",
                                 new[] { 0, 0, 0, 0,
                                         4, 9, 8, 11,
                                         10, 10,
                                         11, 8, 9, 4,
                                         7, 3, 1, 2, 5, 6, 2, 3, 1, 4 },
                                 FpsThuong, true, "MẶC ĐỊNH: đứng rồi vẫy tay, lặp đều"),
                },
            },
        };

        // ═════════════════════════════════════════════════════════════════════════
        // KIỂU DỮ LIỆU PHÂN TÍCH SHEET
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Kết quả đo 1 frame trên sheet. Toạ độ theo hệ Unity: y = 0 ở ĐÁY ảnh.</summary>
        private sealed class FrameInfo
        {
            public int      index;          // 0..11 row-major
            public int      hang, cot;      // 0-based
            public string   tenSprite;      // <base>_00 ..
            public RectInt  rect;           // bbox chặt
            public float    tamChanX;       // toạ độ x TUYỆT ĐỐI trên sheet của tâm bàn chân
            public int      soDongLayChan;  // số dòng dưới cùng đã dùng để tính tâm chân
            public Vector2  pivot;          // normalized trong rect: ((tamChanX - rect.x) / rect.width, 0)
        }

        /// <summary>Cụm bị LOẠI vì hẹp hơn BeRongToiThieu (thường là số thứ tự hoạ sĩ ghi ở góc ô).</summary>
        private sealed class CumBiLoai
        {
            public int hang;          // 0-based
            public RectInt rect;      // bbox chặt của cụm bị loại
        }

        private sealed class KetQuaSheet
        {
            public string pngPath;
            public string tenGoc;
            public int texW, texH;
            public List<FrameInfo> frames  = new List<FrameInfo>();
            public List<CumBiLoai> biLoai  = new List<CumBiLoai>();
            public List<string> canhBao    = new List<string>();
            public List<string> loi        = new List<string>();
            public List<Vector2Int> daiHang = new List<Vector2Int>(); // (yMin,yMax) hàng trên → dưới
            public bool Ok => loi.Count == 0 && frames.Count == SoFrameKyVong;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 1 — LÀM TẤT CẢ
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Cắt sprite → tạo clip → tạo AnimatorController → tạo/cập nhật prefab → đặt vào scene.</summary>
        [MenuItem("Tools/Farm/Setup 3 Nhan Vat (Ba Lao · Hai Quan · NV Tau Lua)", false, 100)]
        public static void LamTatCa()
        {
            var log  = new StringBuilder();
            var specs = TaoDanhSachNpc();
            var prefabs = new List<GameObject>();
            int soLoi = 0;

            log.AppendLine("═══ SETUP 3 NHÂN VẬT NPC ═══");
            log.AppendLine($"PPU {PixelsPerUnit:0} · clip {FpsThuong}fps (clip chậm {FpsCham}fps) · " +
                           $"sorting layer \"{SortLayer}\" · baseOrder {BaseOrder} · scale prefab {PrefabScale:0}");
            log.AppendLine();

            // KHÔNG bọc AssetDatabase.StartAssetEditing() quanh đoạn này: nó HOÃN mọi import tới
            // lúc StopAssetEditing, nên LoadAllAssetsAtPath() ngay sau SaveAndReimport() sẽ KHÔNG
            // thấy sprite vừa cắt → clip rỗng. Chậm hơn vài giây nhưng đúng.
            try
            {
                for (int i = 0; i < specs.Length; i++)
                {
                    var spec = specs[i];
                    EditorUtility.DisplayProgressBar("Setup 3 nhân vật",
                        $"{spec.tenHienThi} ({i + 1}/{specs.Length})", (i + 0.5f) / specs.Length);

                    log.AppendLine($"───── {spec.tenHienThi} ({spec.pngPath}) ─────");
                    if (!DungMotNhanVat(spec, log)) soLoi++;
                    log.AppendLine();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Sau khi Refresh mới load lại prefab (chắc chắn đã có trên đĩa).
            foreach (var spec in specs)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(spec.DuongDanPrefab);
                if (p != null) prefabs.Add(p);
            }

            // ── Đặt vào scene ────────────────────────────────────────────────────
            string ketQuaScene = DatVaoScene(prefabs, log);

            log.AppendLine();
            log.AppendLine("═══ CẦN BẠN LÀM ═══");
            log.AppendLine("1. Ctrl+S để LƯU SCENE (tool chỉ đánh dấu scene bẩn, không tự lưu hộ).");
            log.AppendLine($"2. Kéo object cha \"{TenObjectCha}\" (đã được chọn sẵn + ping trong Project/Hierarchy) " +
                           "về đúng chỗ bạn muốn 3 NPC đứng.");
            log.AppendLine($"3. KIỂM TRA SORTING: Project Settings > Tags and Layers phải có sorting layer " +
                           $"\"{SortLayer}\". SCN_Farm hiện có 218 SpriteRenderer trỏ sorting layer ID đã bị XOÁ " +
                           "(nằm DƯỚI \"Objects\") — tới khi 218 cái đó chưa được trỏ lại, NPC sẽ luôn vẽ ĐÈ công trình " +
                           "bất kể order. Đây là vấn đề có sẵn của dự án, tool này KHÔNG tự sửa " +
                           "(nằm ngoài phạm vi 3 nhân vật).");
            log.AppendLine($"4. KIỂM TRA KÍCH THƯỚC: scale {PrefabScale:0} được chọn để 3 NPC CAO NGANG " +
                           "Chef_NPC trên màn hình (Chef: 74px x scale 200 = 148 world unit ~ 1.48 ô). " +
                           "Chiều cao world THỰC TẾ của từng nhân vật đã in ở dòng [6] phía trên — so lại " +
                           "với 148 rồi tinh chỉnh hằng số PrefabScale trong NpcWaveSetupTool.cs nếu cần " +
                           $"(mỗi 1 đơn vị scale ~ 1.25 world unit chiều cao), sau đó chạy lại menu này.");
            log.AppendLine("5. Bấm Play để xem 3 NPC diễn. Muốn dùng lại clip Idle/Wave/Salute (đã tạo sẵn thành asset) " +
                           "thì tự thêm state + trigger vào .controller — tool cố ý chỉ dựng 1 state mặc định.");

            string all = log.ToString();
            Debug.Log($"{Prefix}\n{all}");

            var tomTat = new StringBuilder();
            tomTat.AppendLine(soLoi == 0 ? "HOÀN TẤT — 3/3 nhân vật OK." : $"XONG nhưng có {soLoi} nhân vật LỖI.");
            tomTat.AppendLine();
            foreach (var spec in specs)
                tomTat.AppendLine($"· {spec.tenHienThi}: {spec.clips.Length} clip, {spec.TenPrefab}.prefab, " +
                                  $"cao {spec.caoWorldUnit:0.#} world unit ({spec.caoWorldUnit / 100f:0.00} ô)");
            tomTat.AppendLine();
            tomTat.AppendLine($"Scale {PrefabScale:0} — mốc so sánh: Chef_NPC cao 148 unit (1.48 ô). " +
                              "Lệch nhiều thì sửa hằng số PrefabScale rồi chạy lại " +
                              "(1 đơn vị scale ~ 1.25 unit chiều cao).");
            tomTat.AppendLine();
            tomTat.AppendLine(ketQuaScene);
            tomTat.AppendLine();
            tomTat.AppendLine("CẦN BẠN LÀM: Ctrl+S lưu scene · kéo NPC_Villagers về đúng chỗ · " +
                              "kiểm tra sorting layer \"Objects\" · xem chi tiết trong Console.");
            EditorUtility.DisplayDialog("Setup 3 nhân vật NPC", tomTat.ToString(), "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 2 — CHỈ CẮT SPRITE
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Chỉ chạy bước cắt lại sprite (rect + pivot tâm bàn chân), không tạo clip/prefab.</summary>
        [MenuItem("Tools/Farm/Setup 3 Nhan Vat/Chi Cat Lai Sprite", false, 120)]
        public static void ChiCatLaiSprite()
        {
            var log = new StringBuilder();
            log.AppendLine("═══ CHỈ CẮT LẠI SPRITE ═══");
            int soLoi = 0, tongSprite = 0, tongLoai = 0;

            foreach (var spec in TaoDanhSachNpc())
            {
                log.AppendLine($"───── {spec.tenHienThi} ─────");
                var kq = PhanTichSheet(spec.pngPath);
                InBaoCaoSheet(kq, log, chiTiet: false);
                if (!kq.Ok) { soLoi++; log.AppendLine("→ DỪNG nhân vật này, KHÔNG cắt."); continue; }
                if (!GhiSprite(spec, kq, log)) { soLoi++; continue; }
                tongSprite += kq.frames.Count;
                tongLoai   += kq.biLoai.Count;
            }

            AssetDatabase.Refresh();
            Debug.Log($"{Prefix}\n{log}");
            EditorUtility.DisplayDialog("Cắt lại sprite",
                (soLoi == 0 ? "HOÀN TẤT 3/3 sheet.\n\n" : $"Có {soLoi} sheet LỖI.\n\n") +
                $"Tổng: {tongSprite} sprite, đã loại {tongLoai} cụm nhỏ (số thứ tự hoạ sĩ ghi trên sheet).\n\n" +
                "CẦN BẠN LÀM: nếu clip/prefab đã tồn tại thì chạy tiếp menu " +
                "'Setup 3 Nhan Vat (...)' để clip trỏ đúng sprite mới. Chi tiết trong Console.", "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MENU 3 — CHỈ BÁO CÁO
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>Chỉ phân tích 3 sheet và in báo cáo: rect, pivot, tâm bàn chân, cụm nhỏ bị loại.</summary>
        [MenuItem("Tools/Farm/Setup 3 Nhan Vat/Kiem Tra Sheet (bao cao)", false, 121)]
        public static void KiemTraSheet()
        {
            var log = new StringBuilder();
            log.AppendLine("═══ BÁO CÁO KIỂM TRA 3 SHEET ═══");
            log.AppendLine($"Ngưỡng alpha > {AlphaNguong} · loại cụm hẹp < {BeRongToiThieu}px · " +
                           $"gộp cụm cách <= {KhoangCachGop}px · tâm chân lấy {TyLeDongChan * 100:0}% dòng dưới cùng");
            log.AppendLine();

            var tomTat = new StringBuilder();
            int soLoi = 0;
            foreach (var spec in TaoDanhSachNpc())
            {
                log.AppendLine($"───── {spec.tenHienThi} ({spec.pngPath}) ─────");
                var kq = PhanTichSheet(spec.pngPath);
                InBaoCaoSheet(kq, log, chiTiet: true);

                // Đối chiếu index frame khai báo trong clip với số frame thật sự cắt được.
                int maxIndex = -1;
                foreach (var c in spec.clips) foreach (int f in c.frames) if (f > maxIndex) maxIndex = f;
                if (kq.Ok && maxIndex >= kq.frames.Count)
                    log.AppendLine($"  LỖI: clip khai báo index tối đa #{maxIndex} nhưng chỉ cắt được " +
                                   $"{kq.frames.Count} frame (#0..#{kq.frames.Count - 1}).");

                if (kq.Ok && spec.frameDung >= 0 && spec.frameDung < kq.frames.Count)
                {
                    int hPx = kq.frames[spec.frameDung].rect.height;
                    float cao = hPx / PixelsPerUnit * PrefabScale;
                    log.AppendLine($"  Chiều cao world DỰ KIẾN: frame đứng #{spec.frameDung} cao {hPx}px " +
                                   $"x scale {PrefabScale:0} / PPU {PixelsPerUnit:0} = {cao:0.#} unit " +
                                   $"({cao / 100f:0.00} ô)   [mốc: Chef_NPC = 148 unit / 1.48 ô]");
                }

                log.AppendLine("  Clip sẽ tạo:");
                foreach (var c in spec.clips)
                    log.AppendLine($"    {c.ten,-18} {c.frames.Length,2} frame @ {c.fps,2}fps = " +
                                   $"{c.ThoiLuong:0.000}s  loop={c.loop}  ({c.ghiChu})" +
                                   (c.ten == spec.clipMacDinh ? "   ← STATE MẶC ĐỊNH" : ""));
                log.AppendLine();

                if (!kq.Ok) soLoi++;
                tomTat.AppendLine($"· {spec.tenHienThi}: {kq.frames.Count}/{SoFrameKyVong} frame, " +
                                  $"loại {kq.biLoai.Count} cụm nhỏ, {kq.loi.Count} lỗi, {kq.canhBao.Count} cảnh báo");
            }

            Debug.Log($"{Prefix}\n{log}");
            EditorUtility.DisplayDialog("Kiểm tra 3 sheet",
                (soLoi == 0 ? "3/3 sheet ĐẠT (đúng 12 frame mỗi sheet).\n\n"
                            : $"{soLoi} sheet KHÔNG ĐẠT — xem Console.\n\n") +
                tomTat +
                "\nCẦN BẠN LÀM: đọc Console để xem rect / pivot / tâm bàn chân từng frame và danh sách " +
                "cụm nhỏ đã bị loại. Không có gì bị ghi ra đĩa ở lệnh này.", "OK");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // DỰNG 1 NHÂN VẬT
        // ═════════════════════════════════════════════════════════════════════════
        private static bool DungMotNhanVat(NpcSpec spec, StringBuilder log)
        {
            // ── 1) Phân tích ─────────────────────────────────────────────────────
            var kq = PhanTichSheet(spec.pngPath);
            InBaoCaoSheet(kq, log, chiTiet: false);
            if (!kq.Ok)
            {
                log.AppendLine($"[1] DỪNG {spec.tenHienThi}: phân tích sheet KHÔNG ĐẠT (cần đúng {SoFrameKyVong} cụm).");
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
                    log.AppendLine($"[3] LỖI: không tìm thấy sprite '{spec.TenGocSprite}_{i:00}' trong {spec.pngPath} " +
                                   "sau khi cắt. Sprite chưa import xong? Thử chạy lại menu.");
                    Debug.LogError($"{Prefix} thiếu sprite {spec.TenGocSprite}_{i:00}");
                    return false;
                }

            // ── 4) Clip ──────────────────────────────────────────────────────────
            EnsureFolder(spec.ThuMucAnim);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var cs in spec.clips)
            {
                var clip = TaoClip(spec.ThuMucAnim, cs, sprites, log);
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
            int idxDung = Mathf.Clamp(spec.frameDung, 0, sprites.Length - 1);
            if (idxDung != spec.frameDung)
                log.AppendLine($"[6] Cảnh báo: frame đứng #{spec.frameDung} ngoài phạm vi, dùng #{idxDung}.");
            return TaoHoacCapNhatPrefab(spec, controller, sprites[idxDung], log);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 1 — PHÂN TÍCH SHEET (dò dải hàng → dò đoạn cột → loại số → gộp → bbox → tâm chân)
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Đo sheet bằng ALPHA, không dùng grid slice.
        /// Trả về đúng 12 frame (3 hàng × 4 nhân vật) kèm rect chặt + pivot tâm bàn chân,
        /// hoặc ghi lỗi vào KetQuaSheet.loi nếu số cụm khác 12.
        /// Toạ độ trả về theo hệ UNITY (y = 0 ở ĐÁY ảnh) để cắm thẳng vào SpriteRect.
        /// </summary>
        private static KetQuaSheet PhanTichSheet(string pngPath)
        {
            var kq = new KetQuaSheet { pngPath = pngPath, tenGoc = Path.GetFileNameWithoutExtension(pngPath) };

            // Đọc pixel TỪ FILE, không qua AssetDatabase: texture đã import thường isReadable = false
            // → GetPixels32() nổ. Cách này không cần bật/tắt isReadable, không làm bẩn .meta.
            // (Y HỆT ChefSheetAnalyzer.LoadReadablePng)
            Texture2D tex = TaiPngDocDuoc(pngPath, out string err);
            if (tex == null) { kq.loi.Add(err); return kq; }

            int W = tex.width, H = tex.height;
            kq.texW = W; kq.texH = H;
            Color32[] px = tex.GetPixels32();     // hàng 0 = ĐÁY ảnh (quy ước Unity)
            UnityEngine.Object.DestroyImmediate(tex);

            var mask = new bool[W * H];
            for (int i = 0; i < px.Length; i++) mask[i] = px[i].a > AlphaNguong;

            // ── 2) DẢI HÀNG: các đoạn liên tục theo Y có nội dung ────────────────
            var coNoiDungTheoY = new bool[H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (mask[y * W + x]) { coNoiDungTheoY[y] = true; break; }

            var daiHang = TimDoan(coNoiDungTheoY);
            if (daiHang.Count == 0) { kq.loi.Add("Ảnh không có pixel nào đục (alpha > " + AlphaNguong + "). Ảnh trống?"); return kq; }

            // TimDoan trả theo y TĂNG = từ ĐÁY lên. Đảo lại để index 0 = hàng TRÊN CÙNG (row-major).
            daiHang.Reverse();
            kq.daiHang.AddRange(daiHang);

            if (daiHang.Count != SoHangKyVong)
                kq.loi.Add($"Dò được {daiHang.Count} dải hàng, KỲ VỌNG {SoHangKyVong}. " +
                           "Sheet có thêm nét vẽ rời (khói/bóng) hoặc 2 hàng bị chạm nhau. " +
                           "KHÔNG cắt bừa — kiểm tra lại PNG.");

            // ── 3) Trong mỗi dải hàng: dò đoạn cột, loại số thứ tự, gộp thành nhân vật ──
            for (int r = 0; r < daiHang.Count; r++)
            {
                int y0 = daiHang[r].x, y1 = daiHang[r].y;

                var coNoiDungTheoX = new bool[W];
                for (int x = 0; x < W; x++)
                    for (int y = y0; y <= y1; y++)
                        if (mask[y * W + x]) { coNoiDungTheoX[x] = true; break; }

                var doanCot = TimDoan(coNoiDungTheoX);

                // 3a) LOẠI đoạn hẹp hơn BeRongToiThieu = SỐ THỨ TỰ hoạ sĩ ghi ở góc mỗi ô.
                //     Sheet nhân viên tàu lửa có 13 chữ số như vậy (rộng 6–12px). Không loại thì
                //     con số sẽ hiện trong game.
                var giuLai = new List<Vector2Int>();
                foreach (var d in doanCot)
                {
                    int rong = d.y - d.x + 1;
                    if (rong >= BeRongToiThieu) { giuLai.Add(d); continue; }

                    var bboxNho = TinhBBox(mask, W, d.x, d.y, y0, y1);
                    kq.biLoai.Add(new CumBiLoai { hang = r, rect = bboxNho });

                    // Cụm bị loại mà khá to thì rất có thể là MỘT PHẦN CƠ THỂ bị tách rời,
                    // không phải chữ số → phải nói ra, đừng im lặng cắt mất.
                    if (bboxNho.width > CanhBaoCumToPx || bboxNho.height > (y1 - y0 + 1) * 0.5f)
                        kq.canhBao.Add($"Hàng {r + 1}: cụm bị loại RỘNG/CAO bất thường " +
                                       $"({bboxNho.width}x{bboxNho.height} tại x={bboxNho.x},y={bboxNho.y}) — " +
                                       "kiểm tra xem có phải một phần cơ thể (bàn tay rời) chứ không phải chữ số. " +
                                       $"Nếu đúng là cơ thể, tăng BeRongToiThieu hoặc giảm xuống dưới {bboxNho.width}.");
                }

                // 3b) Gộp các đoạn còn lại cách nhau <= KhoangCachGop thành 1 nhân vật.
                var cum = new List<Vector2Int>();
                foreach (var d in giuLai)
                {
                    if (cum.Count > 0 && d.x - cum[cum.Count - 1].y - 1 <= KhoangCachGop)
                        cum[cum.Count - 1] = new Vector2Int(cum[cum.Count - 1].x, Mathf.Max(cum[cum.Count - 1].y, d.y));
                    else
                        cum.Add(d);
                }

                if (cum.Count != SoCotKyVong)
                    kq.loi.Add($"Hàng {r + 1} (y {y0}..{y1}): ra {cum.Count} nhân vật, KỲ VỌNG {SoCotKyVong}. " +
                               $"Đoạn cột thô = {doanCot.Count} (đã loại {doanCot.Count - giuLai.Count} cụm hẹp). " +
                               "KHÔNG cắt bừa.");

                // 3c) Mỗi cụm → bbox chặt + tâm bàn chân + pivot
                for (int c = 0; c < cum.Count; c++)
                {
                    var bbox = TinhBBox(mask, W, cum[c].x, cum[c].y, y0, y1);
                    if (bbox.width <= 0 || bbox.height <= 0) continue;

                    // ── PIVOT = TÂM BÀN CHÂN — mấu chốt chống giật ────────────────
                    // Lấy trung bình x của các pixel đục trong 10% dòng DƯỚI CÙNG của bbox.
                    // VÌ SAO không dùng Center: sheet đặt nhân vật lệch tâm nhiều (đo được bà lão
                    // frame #10 lệch +10.2px so với tâm hình học vì cánh tay giơ về một bên nới rộng
                    // bbox sang bên đó). Pivot Center → nhân vật nhảy ngang khi đổi frame.
                    // Pivot theo bàn chân thì bàn chân dính đúng một chỗ, cánh tay muốn vươn đâu cũng được.
                    // Pivot y = 0 (đáy bbox = pixel thấp nhất = gót chân) cũng khớp yêu cầu ChefYSort:
                    // "transform.position.y chính là chỗ chân đứng".
                    int soDong = Mathf.Max(1, Mathf.RoundToInt(bbox.height * TyLeDongChan));
                    double tong = 0; int dem = 0;
                    // CHÚ Ý: dùng x/y/width/height chứ KHÔNG dùng RectInt.xMax — xMax của RectInt là
                    // x + width (KHÔNG bao gồm pixel cuối), viết "x <= xMax" là đọc lố 1 pixel và
                    // có thể vượt mảng ở góc phải-trên ảnh.
                    for (int y = bbox.y; y < bbox.y + soDong; y++)
                        for (int x = bbox.x; x < bbox.x + bbox.width; x++)
                            if (mask[y * W + x]) { tong += x; dem++; }

                    float tamChanX = dem > 0 ? (float)(tong / dem) : bbox.x + bbox.width * 0.5f;
                    if (dem == 0)
                        kq.canhBao.Add($"Frame #{kq.frames.Count}: {soDong} dòng dưới cùng không có pixel đục " +
                                       "(không thể xảy ra với bbox chặt) → tạm dùng tâm hình học.");

                    float pivotX = (tamChanX - bbox.x) / bbox.width;
                    // Kẹp cho chắc: pivot ngoài [0,1] là sprite hỏng, Unity sẽ vẽ lệch hẳn.
                    if (pivotX < 0f || pivotX > 1f)
                    {
                        kq.canhBao.Add($"Frame #{kq.frames.Count}: pivotX {pivotX:0.000} ngoài [0,1] → kẹp lại.");
                        pivotX = Mathf.Clamp01(pivotX);
                    }

                    int idx = kq.frames.Count;
                    kq.frames.Add(new FrameInfo
                    {
                        index         = idx,
                        hang          = r,
                        cot           = c,
                        tenSprite     = $"{kq.tenGoc}_{idx:00}",
                        rect          = bbox,
                        tamChanX      = tamChanX,
                        soDongLayChan = soDong,
                        pivot         = new Vector2(pivotX, 0f),
                    });
                }
            }

            if (kq.frames.Count != SoFrameKyVong)
                kq.loi.Add($"TỔNG ra {kq.frames.Count} nhân vật, KỲ VỌNG {SoFrameKyVong} (3 hàng × 4). DỪNG.");

            return kq;
        }

        /// <summary>bbox chặt của phần đục trong khung [x0..x1] × [y0..y1]. Toạ độ Unity (y↑).</summary>
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

        private static void InBaoCaoSheet(KetQuaSheet kq, StringBuilder log, bool chiTiet)
        {
            log.AppendLine($"  Ảnh {kq.texW}x{kq.texH} · {kq.daiHang.Count} dải hàng · " +
                           $"{kq.frames.Count} nhân vật · loại {kq.biLoai.Count} cụm nhỏ (< {BeRongToiThieu}px)");
            for (int i = 0; i < kq.daiHang.Count; i++)
                log.AppendLine($"    hàng {i + 1}: y {kq.daiHang[i].x}..{kq.daiHang[i].y} " +
                               $"(cao {kq.daiHang[i].y - kq.daiHang[i].x + 1}px)");

            if (kq.biLoai.Count > 0)
            {
                var sb = new StringBuilder("    cụm nhỏ ĐÃ LOẠI (số thứ tự hoạ sĩ ghi trên sheet): ");
                foreach (var b in kq.biLoai) sb.Append($"h{b.hang + 1}({b.rect.x},{b.rect.y},{b.rect.width}x{b.rect.height}) ");
                log.AppendLine(sb.ToString().TrimEnd());
            }

            if (chiTiet)
                foreach (var f in kq.frames)
                    log.AppendLine($"    #{f.index:00} {f.tenSprite,-22} rect=({f.rect.x,3},{f.rect.y,3}, " +
                                   $"{f.rect.width,3}x{f.rect.height,3})  tâmChânX={f.tamChanX,6:0.0} " +
                                   $"(tuyệt đối trên sheet)  pivot=({f.pivot.x:0.0000}, 0)  " +
                                   $"lệch tâm hình học={f.tamChanX - (f.rect.x + f.rect.width * 0.5f):+0.0;-0.0}px  " +
                                   $"lấy {f.soDongLayChan} dòng dưới");

            foreach (var w in kq.canhBao) { log.AppendLine("    CẢNH BÁO: " + w); Debug.LogWarning($"{Prefix} {kq.pngPath}: {w}"); }
            foreach (var e in kq.loi)     { log.AppendLine("    LỖI: " + e);     Debug.LogError($"{Prefix} {kq.pngPath}: {e}"); }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // GHI SPRITE RECT + PIVOT
        // ═════════════════════════════════════════════════════════════════════════
        private static bool GhiSprite(NpcSpec spec, KetQuaSheet kq, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(spec.pngPath) as TextureImporter;
            if (importer == null)
            {
                log.AppendLine($"[2] LỖI: {spec.pngPath} không phải texture (chưa import?).");
                Debug.LogError($"{Prefix} {spec.pngPath} không phải texture.");
                return false;
            }

            // ── Thông số import chuẩn dự án ──────────────────────────────────────
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;                        // 100
            importer.textureCompression  = TextureImporterCompression.Uncompressed; // Compression = None
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.sRGBTexture         = true;
            importer.npotScale           = TextureImporterNPOTScale.None;
            importer.maxTextureSize      = Mathf.Max(1024, Mathf.NextPowerOfTwo(Mathf.Max(kq.texW, kq.texH)));
            // Bilinear (KHÔNG Point): art này không phải pixel-art, viền khử răng cưa mềm và
            // prefab còn phóng scale 200 → Point sẽ ra răng cưa cứng + sọc dải vùng chuyển sắc.
            importer.filterMode          = FilterMode.Bilinear;

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            // FullRect: mesh = đúng khung rect. Tight sẽ sinh mesh khác nhau mỗi frame → thêm một
            // nguồn sai lệch vị trí không cần thiết, mà sprite bé nên chẳng tiết kiệm được gì.
            ts.spriteMeshType  = SpriteMeshType.FullRect;
            ts.spriteAlignment = (int)SpriteAlignment.Custom;   // pivot thật đặt riêng từng SpriteRect
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

            var rects = new List<SpriteRect>(kq.frames.Count);
            var pairs = new List<SpriteNameFileIdPair>(kq.frames.Count);
            var tenMoi = new HashSet<string>();

            foreach (var f in kq.frames)
            {
                GUID id = idCu.TryGetValue(f.tenSprite, out var g) ? g : GUID.Generate();
                rects.Add(new SpriteRect
                {
                    name      = f.tenSprite,
                    spriteID  = id,
                    rect      = new Rect(f.rect.x, f.rect.y, f.rect.width, f.rect.height),
                    alignment = SpriteAlignment.Custom,   // BẮT BUỘC để Unity dùng pivot bên dưới
                    pivot     = f.pivot,                  // (tâm bàn chân, đáy)
                    border    = Vector4.zero,
                });
                pairs.Add(new SpriteNameFileIdPair(f.tenSprite, id));
                tenMoi.Add(f.tenSprite);
            }

            // SetSpriteRects THAY THẾ toàn bộ danh sách (không merge) → rect rác của lần cắt trước
            // (grid slice, tên khác) tự biến mất khỏi .meta. Chạy lại KHÔNG nhân đôi sprite.
            dp.SetSpriteRects(rects.ToArray());
            var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProv != null) nameProv.SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();

            log.AppendLine($"[2] Cắt {rects.Count} sprite · rect chặt riêng từng frame · " +
                           $"pivot Custom = (tâm bàn chân, đáy) · PPU {PixelsPerUnit:0} · " +
                           "filter Bilinear · compression None · alphaIsTransparency · FullRect.");

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

        /// <summary>Đọc sprite trong PNG theo đúng index: phần tử [i] là "<tenGoc>_ii" (null nếu thiếu).</summary>
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
            // Đối chiếu index trước khi làm gì cả — sai index thì clip sẽ trống lặng lẽ.
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
            st.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, st);

            if (taoMoi) AssetDatabase.CreateAsset(clip, path);
            else        EditorUtility.SetDirty(clip);
            return clip;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3a — ANIMATOR CONTROLLER: ĐÚNG 1 STATE, KHÔNG parameter/transition
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Controller 1 state duy nhất = clip _Loop, đặt làm defaultState.
        /// KHÔNG trigger, KHÔNG parameter, KHÔNG transition: chủ dự án muốn "1 hành động lặp đi lặp
        /// lại đều" — càng ít mảnh càng ít vỡ. Clip Idle/Wave/Salute vẫn nằm đó dưới dạng asset để
        /// sau này ghép state thêm.
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

            // IDEMPOTENT: xoá mọi state không phải "Loop" (rác của lần chạy trước / người dùng thêm tay)
            // rồi bảo đảm có đúng 1 state "Loop".
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
            sm.anyStatePosition  = new Vector3(-240, 90);
            sm.exitPosition      = new Vector3(300, 0);

            if (state == null) state = sm.AddState(TenState, new Vector3(20, 0));
            state.motion      = clipMacDinh;
            state.speed       = 1f;
            sm.defaultState   = state;

            EditorUtility.SetDirty(c);
            log.AppendLine($"[5] Controller {path}: 1 state \"{TenState}\" = {clipMacDinh.name} " +
                           $"(defaultState), 0 parameter, 0 transition" +
                           (canXoa.Count > 0 ? $"; đã xoá {canXoa.Count} state rác." : "."));
            return c;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3b — PREFAB
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Lấy component, chưa có thì thêm. BẮT BUỘC viết bằng "== null" chứ KHÔNG dùng toán tử ??.
        ///
        /// Lý do (đã gây crash MissingComponentException ở lần chạy đầu): GetComponent trả về
        /// "fake-null" của Unity — một object C# KHÁC null nhưng con trỏ native bằng 0. Toán tử ??
        /// so sánh THAM CHIẾU nên coi nó là có giá trị, không thêm component, rồi dòng sau gán
        /// sr.sprite là nổ. Chỉ phép "== null" (UnityEngine.Object nạp chồng) mới nhận ra fake-null.
        /// </summary>
        private static T LayHoacThem<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>
        /// Prefab root: SpriteRenderer (frame đứng) + Animator (controller) + ChefYSort (tái dùng).
        /// KHÔNG Rigidbody2D, KHÔNG Collider, KHÔNG script input — NPC trang trí đứng yên.
        /// Prefab đã có thì mở nội dung ra SỬA TẠI CHỖ rồi lưu lại → GIỮ NGUYÊN GUID, instance đã đặt
        /// trong scene KHÔNG bị "Missing Prefab" (khác ChefSetupTool, xem README_CHEF mục cảnh báo).
        /// </summary>
        private static bool TaoHoacCapNhatPrefab(NpcSpec spec, AnimatorController controller,
                                                 Sprite spriteDung, StringBuilder log)
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
                root.transform.localScale    = new Vector3(PrefabScale, PrefabScale, 1f);

                var sr = LayHoacThem<SpriteRenderer>(root);
                sr.sprite   = spriteDung;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.color    = Color.white;
                // Đặt sorting layer TỪ TÊN, không kế thừa: dự án có 218 renderer trỏ sorting layer ID
                // đã bị xoá, copy là dính rác.
                if (SortingLayerTonTai(SortLayer)) sr.sortingLayerName = SortLayer;
                else
                {
                    log.AppendLine($"[6] CẢNH BÁO: không có sorting layer \"{SortLayer}\" trong " +
                                   "Project Settings > Tags and Layers → giữ layer hiện tại.");
                    Debug.LogWarning($"{Prefix} thiếu sorting layer \"{SortLayer}\".");
                }
                sr.sortingOrder = BaseOrder;   // giá trị thấy trước khi ChefYSort tính lại

                var anim = LayHoacThem<Animator>(root);
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
                anim.updateMode      = AnimatorUpdateMode.Normal;
                // AlwaysAnimate: NPC đứng ngoài khung nhìn camera vẫn chạy anim → không bị "đứng hình"
                // đúng lúc camera quét tới.
                anim.cullingMode     = AnimatorCullingMode.AlwaysAnimate;

                // TÁI DÙNG class ChefYSort (Assets/NV_CHEF/Scripts/ChefYSort.cs, global namespace).
                // KHÔNG viết lại component sorting, KHÔNG sửa file trong NV_CHEF.
                var ys = LayHoacThem<ChefYSort>(root);
                ys.sortingLayerName = SortLayer;
                ys.baseOrder        = BaseOrder;
                ys.orderPerUnitY    = OrderPerUnitY;
                ys.sortPoint        = null;      // pivot đã ở bàn chân → position.y CHÍNH LÀ mặt đất
                ys.luonCapNhat      = false;     // NPC đứng yên: chỉ tính lại khi y đổi thật

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

            float caoUnit = 0f;
            if (spriteDung != null) caoUnit = spriteDung.rect.height / PixelsPerUnit * PrefabScale;
            spec.caoWorldUnit = caoUnit;   // để hộp thoại tóm tắt in ra cho chủ dự án đối chiếu
            log.AppendLine($"[6] Prefab {path} ({(coSan ? "CẬP NHẬT tại chỗ, giữ GUID" : "TẠO MỚI")}): " +
                           $"SpriteRenderer({SortLayer}/{BaseOrder}, sprite={spriteDung?.name}) + " +
                           $"Animator({Path.GetFileName(spec.DuongDanController)}) + " +
                           $"ChefYSort(base {BaseOrder}, {OrderPerUnitY}/unitY), scale {PrefabScale:0} " +
                           $"→ cao ~{caoUnit:0} world unit (~{caoUnit / 100f:0.0} ô).");
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHẦN 3c — ĐẶT VÀO SCENE (idempotent + Undo được)
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Tạo object cha NPC_Villagers (nếu chưa có) rồi đặt 3 prefab vào, cách nhau 400 unit theo X.
        /// Mốc: vị trí Chef_NPC nếu tìm được trong scene, không thì (0,0).
        /// Chạy lại KHÔNG nhân đôi: instance đã có thì giữ nguyên (kể cả vị trí designer đã kéo).
        /// </summary>
        private static string DatVaoScene(List<GameObject> prefabs, StringBuilder log)
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

            Undo.SetCurrentGroupName($"{Prefix} Đặt 3 NPC vào scene");
            int group = Undo.GetCurrentGroup();

            // ── Object cha ───────────────────────────────────────────────────────
            GameObject cha = TimTrongScene(scene, TenObjectCha);
            bool chaMoi = cha == null;
            if (chaMoi)
            {
                cha = new GameObject(TenObjectCha);
                Undo.RegisterCreatedObjectUndo(cha, "Tạo " + TenObjectCha);
                // Cha để ở gốc, con giữ toạ độ world → dễ đọc số trong Inspector.
                cha.transform.position   = Vector3.zero;
                cha.transform.localScale = Vector3.one;
                log.AppendLine($"[7] Tạo object cha \"{TenObjectCha}\".");
            }
            else log.AppendLine($"[7] Dùng lại object cha \"{TenObjectCha}\" đã có trong scene.");

            // ── Mốc vị trí ───────────────────────────────────────────────────────
            var chef = TimTrongScene(scene, TenChef) ?? TimTrongSceneTheoTienTo(scene, TenChef);
            Vector3 moc = chef != null ? chef.transform.position : Vector3.zero;
            // Có đầu bếp thì NPC đầu tiên nằm cách đầu bếp 1 bước (khỏi trùng chỗ); không có thì bắt đầu từ mốc.
            int buocDau = chef != null ? 1 : 0;
            log.AppendLine(chef != null
                ? $"[7] Mốc = vị trí '{chef.name}' ({moc.x:0.#}, {moc.y:0.#}); NPC đặt cách đầu bếp " +
                  $"{KhoangCachX:0}, {KhoangCachX * 2:0}, {KhoangCachX * 3:0} unit theo X."
                : $"[7] KHÔNG tìm thấy '{TenChef}' trong scene → mốc = (0, 0).");

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
                go.transform.position = new Vector3(moc.x + KhoangCachX * (buocDau + i), moc.y, moc.z);
                moiTao.Add(go);
                log.AppendLine($"[7] Đặt '{go.name}' tại ({go.transform.position.x:0.#}, {go.transform.position.y:0.#}).");
            }

            Selection.activeGameObject = cha;
            EditorGUIUtility.PingObject(cha);
            var sv = SceneView.lastActiveSceneView;
            if (sv != null && moiTao.Count > 0) sv.FrameSelected();

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);

            string kq = moiTao.Count > 0
                ? $"Đã đặt {moiTao.Count} NPC vào \"{TenObjectCha}\" (scene '{scene.name}'). Ctrl+S để lưu."
                : $"\"{TenObjectCha}\" đã có đủ 3 NPC — không tạo thêm gì.";
            log.AppendLine("[7] " + kq);
            return kq;
        }

        private static GameObject TimTrongScene(Scene scene, string ten)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == ten) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == ten) return t.gameObject;
            }
            return null;
        }

        /// <summary>Dự phòng: instance trong scene thường bị đổi tên thành "Chef_NPC (1)".</summary>
        private static GameObject TimTrongSceneTheoTienTo(Scene scene, string tienTo)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith(tienTo, StringComparison.Ordinal)) return t.gameObject;
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
