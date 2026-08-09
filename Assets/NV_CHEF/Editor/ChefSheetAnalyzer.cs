#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NVChef.EditorTools
{
    /// <summary>
    /// PHÂN TÍCH SHEET ĐẦU BẾP — dò lưới frame TỪ ALPHA, không hardcode toạ độ.
    ///
    /// VÌ SAO cần file riêng này (không nhét vào tool):
    ///  - Logic dò alpha là phần dễ sai nhất và cần đọc/kiểm lại độc lập với UI.
    ///  - Edric có thể thay PNG khác (số frame khác, kích thước khác) → tool chỉ gọi lại Analyze().
    ///
    /// TOẠ ĐỘ: toàn bộ file này dùng TOẠ ĐỘ UNITY (y = 0 ở ĐÁY ảnh, y tăng lên trên),
    /// vì SpriteRect của Unity cũng dùng hệ đó. Nếu bạn so với số đo bằng PIL/Photoshop
    /// (y = 0 ở TRÊN) thì quy đổi: yUnity = (height - 1) - yPIL.
    ///
    /// VÌ SAO KHÔNG cắt bounding-box khít từng frame:
    ///   mỗi frame sẽ có rect rộng/cao khác nhau → pivot bottom-center rơi vào chỗ khác nhau
    ///   → nhân vật NHẢY GIẬT giữa các frame.
    /// VÌ SAO KHÔNG cắt lưới đều 763/8:
    ///   763 / 8 = 95.375 không chia hết → lưới trôi dần, frame cuối bị cắt mất.
    /// CÁCH LÀM: mọi frame CÙNG rect (W x H), đặt rect sao cho ĐIỂM CHÂN của frame
    ///   luôn nằm ở cùng một vị trí bên trong rect.
    /// </summary>
    public static class ChefSheetAnalyzer
    {
        // ─────────────────────────────────────────────────────────────────────────
        // THAM SỐ
        // ─────────────────────────────────────────────────────────────────────────
        [Serializable]
        public class Settings
        {
            [Tooltip("Alpha (0..255) coi là 'có hình'. 10 = ~4%: bỏ viền khử răng cưa mờ mà không mất nét.")]
            public byte alphaThreshold = 10;

            [Tooltip("Số dòng pixel dưới cùng của thân dùng để tính TÂM CHÂN. 5 dòng đủ để lấy 2 bàn chân, " +
                     "không lấn lên ống quần.")]
            public int feetSampleRows = 5;

            [Tooltip("Lề an toàn (px) chừa quanh nội dung trong rect. Chống mất nét viền khi Unity extrude sprite.")]
            public int marginPx = 2;

            [Tooltip("Lệch tâm chân (px) vượt mức này thì ÉP về vị trí lưới đã khớp tuyến tính (chống rung). " +
                     "Sheet này: Idle_0 lệch 8px do CÁN CHẢO thò ra ngang tầm chân, làm tâm chân bị kéo lệch.")]
            public float snapDriftPx = 3f;

            [Tooltip("Lệch tâm chân (px) vượt mức này thì BÁO ĐỘNG trong bảng phân tích.")]
            public float warnDriftPx = 5f;

            [Tooltip("Dải hàng/cột nhỏ hơn tỉ lệ này so với dải lớn nhất bị coi là VỤN (khói, đốm lửa bay rời) " +
                     "và được gộp vào dải chính gần nhất, không tính thành hàng/frame mới.")]
            public float smallBandRatio = 0.4f;

            [Tooltip("Vụn chỉ được gộp nếu khoảng cách tới dải chính <= tỉ lệ này x bề dày dải chính.")]
            public float mergeGapRatio = 0.5f;

            [Tooltip("BẬT = pivot Custom đặt đúng tuyệt đối lên điểm chân (sai số 0px). " +
                     "TẮT = pivot Bottom-Center theo chuẩn dự án (còn sai số làm tròn <= 0.5px, mắt không thấy).")]
            public bool pivotChinhXacTuyetDoi = false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // KẾT QUẢ
        // ─────────────────────────────────────────────────────────────────────────
        public class FrameInfo
        {
            public int index;
            public string spriteName;

            // Bao nội dung thực (toạ độ Unity, y lên).
            public int contentMinX, contentMaxX, contentMinY, contentMaxY;

            public float rawFeetX;   // tâm chân đo trực tiếp
            public float fitFeetX;   // tâm chân theo mô hình lưới (khớp tuyến tính bền vững)
            public float anchorX;    // mốc thực dùng để đặt rect
            public float driftPx;    // rawFeetX - fitFeetX
            public bool  snapped;    // đã bị ép về fitFeetX

            public int footY;        // y THẤP NHẤT có alpha = đáy chân của frame này
            public int footOffset;   // footY - groundY  (âm = chân thấp hơn mốc đất, dương = nhấc lên)

            public RectInt rect;             // rect cuối cùng
            public Vector2 pivotNormalized;  // pivot trong rect (0..1)
            public bool clamped;             // rect bị kẹp vào biên ảnh
            public bool contentClipped;      // CẢNH BÁO ĐỎ: rect không chứa hết nội dung
        }

        public class RowInfo
        {
            public string animName;
            public int yMin, yMax;      // dải hàng (Unity y)
            public float pitch;         // bước ngang giữa 2 frame
            public float intercept;     // tâm chân frame 0 theo mô hình
            public int groundY;         // MỐC ĐẤT của hàng = trung vị footY
            public int bodyHeightPx;    // cao nhất tính từ mốc đất (dùng để tính scale)
            public List<FrameInfo> frames = new List<FrameInfo>();
        }

        public class Analysis
        {
            public string pngPath;
            public int texWidth, texHeight;
            public List<RowInfo> rows = new List<RowInfo>();

            public int rectWidth, rectHeight, padBottom;
            public int totalFrames;

            // Cao thân người (bỏ lửa/khói) — lấy từ hàng đầu tiên (Idle không có lửa).
            public int bodyHeightPx;

            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();

            public bool Ok => errors.Count == 0 && totalFrames > 0;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HÀM CHÍNH
        // ─────────────────────────────────────────────────────────────────────────
        /// <param name="animNames">Tên clip cho từng hàng, tính từ hàng TRÊN xuống.</param>
        public static Analysis Analyze(string pngAssetPath, string[] animNames, Settings s)
        {
            var a = new Analysis { pngPath = pngAssetPath };
            s = s ?? new Settings();

            // ── Đọc pixel TỪ FILE trên ổ đĩa, KHÔNG qua AssetDatabase ────────────
            // VÌ SAO: Texture2D đã import thường có isReadable = false → GetPixels32() nổ.
            // Đọc thẳng byte PNG rồi LoadImage cho ra texture đọc được, mà KHÔNG phải
            // bật/tắt isReadable trong importer (tránh reimport lằng nhằng + đổi .meta thừa).
            Texture2D tex = LoadReadablePng(pngAssetPath, out string err);
            if (tex == null) { a.errors.Add(err); return a; }

            int W = tex.width, H = tex.height;
            a.texWidth = W; a.texHeight = H;

            Color32[] px = tex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tex);

            // mask[y * W + x] = true nếu pixel có hình
            bool[] mask = new bool[W * H];
            for (int i = 0; i < px.Length; i++) mask[i] = px[i].a > s.alphaThreshold;

            // ── 1) Tách HÀNG ────────────────────────────────────────────────────
            var rowOcc = new bool[H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (mask[y * W + x]) { rowOcc[y] = true; break; }

            List<Vector2Int> rowBands = MergeSmallBands(FindBands(rowOcc), s);
            if (rowBands.Count == 0) { a.errors.Add("Không tìm thấy pixel nào có alpha. Ảnh trống?"); return a; }

            // FindBands trả về theo y TĂNG DẦN = từ ĐÁY ảnh lên. Đảo lại để index 0 = hàng TRÊN CÙNG.
            rowBands.Reverse();

            // THÔNG BÁO, KHÔNG PHẢI LỖI: analyzer luôn dò HẾT số hàng có trong ảnh (cần thiết để
            // tính rect dùng chung cho đúng). Người GỌI tự quyết dùng bao nhiêu hàng đầu —
            // ChefSetupTool hiện chỉ dùng 2 hàng (Idle, Stir) và bỏ qua phần còn lại.
            if (animNames != null && rowBands.Count != animNames.Length)
                a.warnings.Add($"Dò được {rowBands.Count} hàng nhưng danh sách tên có {animNames.Length} " +
                               "-> hàng thừa được đặt tên tạm Row{n}. Nếu bên gọi CỐ Ý chỉ dùng " +
                               "một số hàng đầu thì đây là BÌNH THƯỜNG; ngược lại hãy kiểm tra lại sheet.");

            // ── 2) Tách FRAME trong từng hàng + đo chân ─────────────────────────
            for (int r = 0; r < rowBands.Count; r++)
            {
                int y0 = rowBands[r].x, y1 = rowBands[r].y;
                var row = new RowInfo
                {
                    animName = (animNames != null && r < animNames.Length) ? animNames[r] : "Row" + r,
                    yMin = y0, yMax = y1
                };

                var colOcc = new bool[W];
                for (int x = 0; x < W; x++)
                    for (int y = y0; y <= y1; y++)
                        if (mask[y * W + x]) { colOcc[x] = true; break; }

                List<Vector2Int> colBands = MergeSmallBands(FindBands(colOcc), s);
                if (colBands.Count == 0) { a.warnings.Add($"Hàng {r + 1} ({row.animName}) không có frame nào."); continue; }

                for (int c = 0; c < colBands.Count; c++)
                {
                    int x0 = colBands[c].x, x1 = colBands[c].y;

                    int cMinX = int.MaxValue, cMaxX = int.MinValue, cMinY = int.MaxValue, cMaxY = int.MinValue;
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                            if (mask[y * W + x])
                            {
                                if (x < cMinX) cMinX = x;
                                if (x > cMaxX) cMaxX = x;
                                if (y < cMinY) cMinY = y;
                                if (y > cMaxY) cMaxY = y;
                            }
                    if (cMinX > cMaxX) continue;

                    // TÂM CHÂN: chỉ xét feetSampleRows dòng pixel THẤP NHẤT của frame.
                    // VÌ SAO: nếu lấy tâm cả thân, cánh tay / chảo / lửa vung sang một bên
                    // sẽ kéo tâm đi mỗi frame một chỗ → rung ngang.
                    int fTop = Mathf.Min(cMinY + Mathf.Max(1, s.feetSampleRows) - 1, cMaxY);
                    int fMinX = int.MaxValue, fMaxX = int.MinValue;
                    for (int y = cMinY; y <= fTop; y++)
                        for (int x = x0; x <= x1; x++)
                            if (mask[y * W + x])
                            {
                                if (x < fMinX) fMinX = x;
                                if (x > fMaxX) fMaxX = x;
                            }
                    if (fMinX > fMaxX) { fMinX = cMinX; fMaxX = cMaxX; }

                    row.frames.Add(new FrameInfo
                    {
                        index = c,
                        spriteName = $"Chef_{row.animName}_{c}",
                        contentMinX = cMinX, contentMaxX = cMaxX,
                        contentMinY = cMinY, contentMaxY = cMaxY,
                        footY = cMinY,
                        rawFeetX = (fMinX + fMaxX) * 0.5f
                    });
                }

                // ── 3) MỐC ĐẤT của hàng = TRUNG VỊ footY ────────────────────────
                // VÌ SAO trung vị chứ không phải từng frame: nhân vật ĐỨNG YÊN nên đất phải
                // cố định. Frame nào chân cao hơn mốc (vd Flip_1 nhấc 3px) thì GIỮ NGUYÊN độ
                // nhấc đó -> đúng chủ ý hoạ sĩ. Nếu căn từng frame theo đáy riêng thì cái nhấc
                // 3px biến thành ĐẤT tụt 3px -> giật 1 frame.
                var feetYs = new List<float>();
                foreach (var f in row.frames) feetYs.Add(f.footY);
                row.groundY = Mathf.RoundToInt(Median(feetYs));

                // ── 4) Khớp tuyến tính BỀN VỮNG (Theil–Sen) cho tâm chân ────────
                // VÌ SAO: sheet được render trên lưới đều nên tâm chân THẬT phải nằm trên một
                // đường thẳng anchor = intercept + pitch * index. Frame nào lệch nhiều là do
                // vật thể phụ (cán chảo, lửa) chạm tầm chân làm bẩn phép đo -> ép về đường thẳng.
                // Dùng Theil–Sen (trung vị mọi hệ số góc từng cặp) thay vì bình phương tối thiểu
                // vì least-squares bị chính điểm lỗi kéo lệch cả đường.
                FitRow(row, s);

                // Cao thân tính từ mốc đất.
                int bh = 0;
                foreach (var f in row.frames) bh = Mathf.Max(bh, f.contentMaxY - row.groundY + 1);
                row.bodyHeightPx = bh;

                a.rows.Add(row);
                a.totalFrames += row.frames.Count;
            }

            if (a.totalFrames == 0) { a.errors.Add("Không tách được frame nào."); return a; }

            // ── 5) KÍCH THƯỚC RECT DÙNG CHUNG ───────────────────────────────────
            // Lấy tầm với xa nhất của nội dung so với (anchorX, groundY) trên TOÀN sheet,
            // rồi cộng lề. Rect đối xứng quanh anchorX để pivot bottom-center = 0.5 trùng mốc chân.
            float needL = 0, needR = 0, needUp = 0, needDown = 0;
            foreach (var row in a.rows)
                foreach (var f in row.frames)
                {
                    needL    = Mathf.Max(needL,    f.anchorX - f.contentMinX);
                    needR    = Mathf.Max(needR,    f.contentMaxX - f.anchorX);
                    needUp   = Mathf.Max(needUp,   f.contentMaxY - row.groundY);
                    needDown = Mathf.Max(needDown, row.groundY - f.contentMinY); // chân thấp hơn mốc đất
                }

            int half = Mathf.CeilToInt(Mathf.Max(needL, needR)) + s.marginPx;
            a.rectWidth  = half * 2;                                  // luôn CHẴN -> half nguyên
            a.padBottom  = Mathf.CeilToInt(Mathf.Max(0f, needDown)) + s.marginPx;
            a.rectHeight = a.padBottom + Mathf.CeilToInt(needUp) + 1 + s.marginPx;
            if (a.rectHeight % 2 != 0) a.rectHeight++;                // làm tròn chẵn theo yêu cầu

            if (a.rectWidth > W || a.rectHeight > H)
            {
                a.errors.Add($"Rect cần {a.rectWidth}x{a.rectHeight} nhưng ảnh chỉ {W}x{H}. " +
                             "Sheet quá chật, không thể căn chân đồng nhất.");
                return a;
            }

            // Cao thân người: dùng HÀNG ĐẦU (Idle) vì hàng này không có lửa/khói bốc lên
            // làm phồng chiều cao -> tính scale mới đúng người thật.
            a.bodyHeightPx = a.rows[0].bodyHeightPx;

            // ── 6) ĐẶT RECT TỪNG FRAME + KẸP BIÊN ───────────────────────────────
            foreach (var row in a.rows)
                foreach (var f in row.frames)
                {
                    int wantX = Mathf.RoundToInt(f.anchorX) - half;
                    int wantY = row.groundY - a.padBottom;

                    int rx = Mathf.Clamp(wantX, 0, W - a.rectWidth);
                    int ry = Mathf.Clamp(wantY, 0, H - a.rectHeight);
                    f.clamped = (rx != wantX) || (ry != wantY);
                    f.rect = new RectInt(rx, ry, a.rectWidth, a.rectHeight);

                    // Pivot: mặc định Bottom-Center (0.5, 0) theo chuẩn dự án.
                    // Nếu rect BỊ KẸP biên thì bottom-center không còn trùng mốc chân nữa
                    // -> chuyển sang pivot Custom đặt đúng lên chân để KHÔNG rung.
                    if (s.pivotChinhXacTuyetDoi || f.clamped)
                        f.pivotNormalized = new Vector2(
                            (f.anchorX - rx) / a.rectWidth,
                            (row.groundY - ry) / (float)a.rectHeight);
                    else
                        f.pivotNormalized = new Vector2(0.5f, 0f);

                    f.footOffset = f.footY - row.groundY;

                    f.contentClipped =
                        f.contentMinX < rx || f.contentMaxX > rx + a.rectWidth - 1 ||
                        f.contentMinY < ry || f.contentMaxY > ry + a.rectHeight - 1;

                    if (f.clamped)
                        a.warnings.Add($"{f.spriteName}: rect bị KẸP vào biên ảnh (muốn x={wantX},y={wantY} " +
                                       $"-> {rx},{ry}). Đã bù bằng pivot Custom nên KHÔNG rung.");
                    if (f.contentClipped)
                        a.errors.Add($"{f.spriteName}: rect CẮT MẤT nội dung. Tăng lề hoặc chừa viền cho sheet.");
                    if (Mathf.Abs(f.driftPx) > s.warnDriftPx)
                        a.warnings.Add($"{f.spriteName}: tâm chân lệch {f.driftPx:+0.0;-0.0}px so với lưới " +
                                       (f.snapped ? "-> ĐÃ ép về lưới (hết rung)." : "-> CHƯA ép, có thể rung!"));
                }

            return a;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // KHỚP LƯỚI BỀN VỮNG
        // ─────────────────────────────────────────────────────────────────────────
        private static void FitRow(RowInfo row, Settings s)
        {
            int n = row.frames.Count;
            if (n == 1)
            {
                var only = row.frames[0];
                row.pitch = 0f; row.intercept = only.rawFeetX;
                only.fitFeetX = only.rawFeetX; only.anchorX = only.rawFeetX;
                only.driftPx = 0f; only.snapped = false;
                return;
            }

            // Theil–Sen: hệ số góc = trung vị hệ số góc của MỌI cặp điểm.
            var slopes = new List<float>();
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    slopes.Add((row.frames[j].rawFeetX - row.frames[i].rawFeetX) / (j - i));
            row.pitch = Median(slopes);

            var icpts = new List<float>();
            for (int i = 0; i < n; i++) icpts.Add(row.frames[i].rawFeetX - row.pitch * i);
            row.intercept = Median(icpts);

            for (int i = 0; i < n; i++)
            {
                var f = row.frames[i];
                f.fitFeetX = row.intercept + row.pitch * i;
                f.driftPx = f.rawFeetX - f.fitFeetX;
                f.snapped = Mathf.Abs(f.driftPx) > s.snapDriftPx;
                // Lệch nhỏ thì GIỮ số đo thật (tôn trọng dao động nhỏ hoạ sĩ vẽ);
                // lệch lớn thì ép về lưới (chắc chắn là lỗi đo do vật thể phụ).
                f.anchorX = f.snapped ? f.fitFeetX : f.rawFeetX;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TIỆN ÍCH
        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>Tìm các dải liên tục có hình. Trả về (start,end) BAO GỒM cả 2 đầu, theo index tăng.</summary>
        private static List<Vector2Int> FindBands(bool[] occ)
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

        /// <summary>
        /// Gộp dải VỤN vào dải chính gần nhất.
        /// VÌ SAO cần: sheet này có đốm khói bay rời ở y(PIL) 87..89 tách hẳn khỏi thân.
        /// Nếu không gộp thì nó bị đếm thành HÀNG THỨ 5 -> sai toàn bộ mapping clip.
        /// </summary>
        private static List<Vector2Int> MergeSmallBands(List<Vector2Int> bands, Settings s)
        {
            if (bands.Count <= 1) return bands;

            int max = 0;
            foreach (var b in bands) max = Mathf.Max(max, b.y - b.x + 1);

            var main = new List<Vector2Int>();
            var small = new List<Vector2Int>();
            foreach (var b in bands)
            {
                if ((b.y - b.x + 1) >= s.smallBandRatio * max) main.Add(b);
                else small.Add(b);
            }
            if (main.Count == 0) return bands;

            foreach (var sb in small)
            {
                int best = -1, bestDist = int.MaxValue;
                for (int i = 0; i < main.Count; i++)
                {
                    var mb = main[i];
                    int d = sb.y < mb.x ? mb.x - sb.y : (sb.x > mb.y ? sb.x - mb.y : 0);
                    if (d < bestDist) { bestDist = d; best = i; }
                }
                if (best < 0) continue;
                var m = main[best];
                if (bestDist <= s.mergeGapRatio * (m.y - m.x + 1))
                    main[best] = new Vector2Int(Mathf.Min(m.x, sb.x), Mathf.Max(m.y, sb.y));
            }

            main.Sort((p, q) => p.x.CompareTo(q.x));
            return main;
        }

        private static float Median(List<float> v)
        {
            if (v == null || v.Count == 0) return 0f;
            var c = new List<float>(v);
            c.Sort();
            int m = c.Count / 2;
            return (c.Count % 2 == 1) ? c[m] : (c[m - 1] + c[m]) * 0.5f;
        }

        private static Texture2D LoadReadablePng(string assetPath, out string error)
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
    }
}
#endif
