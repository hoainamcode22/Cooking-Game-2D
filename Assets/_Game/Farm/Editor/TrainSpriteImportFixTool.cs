using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor Tool: Tools/Farm Game/Train/🔧 Sửa import sprite Train (trắng UI)
///
/// ── VẤN ĐỀ ĐANG SỬA ────────────────────────────────────────────────────────
/// 21 file PNG trong Assets/Export_Train_UI_Package/Sprites/ có file .meta do
/// script viết tay: GUID giả (aaaaaaaa…73001197), internalIDToNameTable rỗng, và
/// THIẾU key textureType ⇒ Unity import chúng thành TEXTURE chứ không phải SPRITE
/// ⇒ không sinh sub-asset Sprite ⇒ mọi Image trong Popup_Train_MasterStation.prefab
/// có m_Sprite = {fileID: 0} ⇒ popup "TÀU CHỞ HÀNG" vẽ ra hình chữ nhật TRẮNG ĐẶC.
/// (Lỗi của gói Train từ 25/08, không liên quan hệ Tourist Boat.)
///
/// ── TOOL LÀM GÌ ────────────────────────────────────────────────────────────
///   1. Quét đệ quy mọi texture trong Sprites/: textureType != Sprite thì set
///      Sprite + spriteImportMode Single + alphaIsTransparency, PPU giữ nguyên nếu
///      đã hợp lệ (mặc định 100) → SaveAndReimport().
///   2. Đặt lại border 9-slice theo BẢNG TÊN→BORDER khai báo ngay đầu file (dễ
///      chỉnh). Ảnh chưa có trong bảng và border đang 0 → để (0,0,0,0) + ghi vào
///      report "cần canh tay nếu thấy méo". KHÔNG xoá border designer đã đặt.
///   3. Quét lại prefab trong Export_Train_UI_Package/Prefabs/ và LIỆT KÊ mọi Image
///      còn sprite == null kèm đường dẫn hierarchy — CHỈ LIỆT KÊ, không tự gán bừa
///      (gán sai còn tệ hơn để trắng).
///   4. Gọi TrainPackageBuildTool dựng lại prefab qua reflection nếu có; không có
///      thì ghi vào report để Sếp tự chạy.
///   5. Ghi report ra production/session-state/TRAIN_SPRITE_FIX_REPORT.txt (UTF-8,
///      NGOÀI thư mục Assets nên Unity không import) + Console + dialog tổng kết.
///
/// ── AN TOÀN ────────────────────────────────────────────────────────────────
///  • TUYỆT ĐỐI không xoá/ghi đè file .meta bằng tay — làm vậy là đổi GUID và mất
///    mọi reference đang trỏ tới asset. Mọi thay đổi đi qua TextureImporter API,
///    GUID giữ nguyên, prefab đang trỏ đúng vẫn trỏ đúng.
///  • IDEMPOTENT: chạy lại nhiều lần chỉ sửa phần còn sai; không có gì sai thì
///    không reimport file nào.
///  • StartAssetEditing/StopAssetEditing bọc try/finally — lỗi giữa chừng cũng
///    không kẹt AssetDatabase.
/// </summary>
public static class TrainSpriteImportFixTool
{
    private const string MenuFix = "Tools/Farm Game/Train/🔧 Sửa import sprite Train (trắng UI)";

    private const string ThuMucSprite = "Assets/Export_Train_UI_Package/Sprites";
    private const string ThuMucPrefab = "Assets/Export_Train_UI_Package/Prefabs";

    // Report để NGOÀI Assets/ — trong Assets là Unity import thành TextAsset, bẩn project.
    private const string ThuMucReport = "production/session-state";
    private const string TenReport    = "TRAIN_SPRITE_FIX_REPORT.txt";

    private const float PpuMacDinh = 100f;

    /// <summary>
    /// BẢNG BORDER 9-SLICE (chỉnh ở đây, không phải trong code xử lý).
    /// Key = tên file KHÔNG đuôi (so sánh không phân biệt hoa/thường).
    /// Value = border (left, bottom, right, top) — đúng thứ tự Unity dùng cho
    /// TextureImporter.spriteBorder / Sprite.border.
    ///
    /// Chỉ điền ảnh ĐÃ BIẾT CHẮC. Ảnh khung mà đặt border sai còn khó thấy hơn
    /// border = 0 (méo tinh vi ở góc), nên thà để trống và báo cáo.
    /// Mốc đã xác nhận: WoodBoard_Frame 512x512 PPU 100 → border 64 mỗi cạnh.
    /// </summary>
    private static readonly Dictionary<string, Vector4> BangBorder =
        new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
        {
            { "WoodBoard_Frame", new Vector4(64f, 64f, 64f, 64f) },
        };

    // Tên prefab cần soi sau khi reimport (theo báo cáo điều tra).
    private static readonly string[] PrefabCanSoi =
    {
        "Popup_train",
        "Popup_item_Train",
        "Popup_Train_MasterStation",
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MenuFix, false, 40)]
    public static void SuaImportSpriteTrain()
    {
        SuaImportSpriteTrain(false);
    }

    /// <summary>
    /// quiet = true: không bật dialog, trả report dạng chuỗi (cho tool tổng gọi
    /// sang — cùng quy ước với TouristBoatUIPopupSetupTool.SetupPopups(bool)).
    /// </summary>
    public static string SuaImportSpriteTrain(bool quiet)
    {
        var report = new StringBuilder();
        report.AppendLine("TRAIN SPRITE IMPORT FIX — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Thư mục sprite: " + ThuMucSprite);
        report.AppendLine(new string('=', 72));
        report.AppendLine();

        if (!AssetDatabase.IsValidFolder(ThuMucSprite))
        {
            string loi = "LỖI: không thấy thư mục " + ThuMucSprite +
                         " trong project — gói Train chưa được copy vào? Không làm gì cả.";
            report.AppendLine(loi);
            KetThuc(report, quiet, "Không tìm thấy thư mục sprite Train", loi);
            return report.ToString();
        }

        // ── BƯỚC 1 + 2: import lại texture thành Sprite + đặt border ────────
        int daSua = 0, daDung = 0, loiSua = 0;
        var canCanhTay = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ThuMucSprite });
        report.AppendLine($"── BƯỚC 1-2: quét {guids.Length} texture ──");

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null)
                {
                    report.AppendLine($"  ? {path} — không lấy được TextureImporter, bỏ qua.");
                    continue;
                }

                var doi = new List<string>(); // liệt kê thay đổi của riêng file này

                if (ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    doi.Add("textureType → Sprite");
                }
                if (ti.spriteImportMode != SpriteImportMode.Single)
                {
                    ti.spriteImportMode = SpriteImportMode.Single;
                    doi.Add("spriteImportMode → Single");
                }
                if (!ti.alphaIsTransparency)
                {
                    ti.alphaIsTransparency = true;
                    doi.Add("alphaIsTransparency → true");
                }
                // PPU: chỉ đụng khi giá trị vô lý (0 / âm) — giữ nguyên số designer đã chỉnh
                if (ti.spritePixelsPerUnit <= 0.01f)
                {
                    ti.spritePixelsPerUnit = PpuMacDinh;
                    doi.Add($"PPU → {PpuMacDinh:0}");
                }

                // Border 9-slice
                string ten = Path.GetFileNameWithoutExtension(path);
                Vector4 borderMoi;
                if (BangBorder.TryGetValue(ten, out borderMoi))
                {
                    if (ti.spriteBorder != borderMoi)
                    {
                        ti.spriteBorder = borderMoi;
                        doi.Add($"border → ({borderMoi.x:0},{borderMoi.y:0},{borderMoi.z:0},{borderMoi.w:0})");
                    }
                }
                else if (ti.spriteBorder == Vector4.zero)
                {
                    // Không biết border, cũng không có sẵn → để 0, báo cáo cho Sếp
                    canCanhTay.Add(path);
                }

                if (doi.Count == 0)
                {
                    daDung++;
                    continue;
                }

                try
                {
                    ti.SaveAndReimport();
                    daSua++;
                    report.AppendLine($"  ✔ {path}\n      {string.Join(" · ", doi.ToArray())}");
                }
                catch (Exception e)
                {
                    loiSua++;
                    report.AppendLine($"  ✖ {path} — reimport lỗi: {e.Message}");
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            // LUÔN đóng cặp, kể cả khi ném giữa chừng — không thì AssetDatabase kẹt
            // ở trạng thái "đang edit" và Unity ngừng import mọi thứ.
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        report.AppendLine();
        report.AppendLine($"  Tổng: sửa {daSua} · đã đúng sẵn {daDung} · lỗi {loiSua}");
        if (canCanhTay.Count > 0)
        {
            report.AppendLine();
            report.AppendLine($"  ⚠ {canCanhTay.Count} ảnh CHƯA BIẾT BORDER 9-slice (đang để 0,0,0,0).");
            report.AppendLine("    Nếu ảnh nào là khung/nút bo góc mà thấy MÉO khi phóng to:");
            report.AppendLine("    mở Sprite Editor canh border, hoặc thêm tên vào BangBorder trong tool này.");
            foreach (string p in canCanhTay) report.AppendLine("      - " + p);
        }
        report.AppendLine();

        // ── BƯỚC 3: gọi TrainPackageBuildTool dựng lại prefab (nếu có) ──────
        report.AppendLine("── BƯỚC 3: dựng lại prefab qua TrainPackageBuildTool ──");
        report.AppendLine("  " + GoiTrainPackageBuildTool());
        report.AppendLine();

        // ── BƯỚC 4: soi prefab, liệt kê Image còn thiếu sprite ──────────────
        report.AppendLine("── BƯỚC 4: Image còn thiếu sprite trong prefab Train ──");
        int tongThieu = SoiPrefab(report);
        report.AppendLine();

        // ── BƯỚC 5: việc còn lại của Sếp ────────────────────────────────────
        report.AppendLine("── CẦN SẾP LÀM TRONG UNITY ──");
        report.AppendLine("1) Nhìn Project window: các PNG trong " + ThuMucSprite);
        report.AppendLine("   giờ phải BUNG RA được (mũi tên ►) và có sub-asset Sprite bên trong.");
        report.AppendLine("   Không bung được = file .meta còn hỏng nặng hơn → báo lại, ĐỪNG xoá .meta tay.");
        report.AppendLine("2) Mở prefab Popup_Train_MasterStation, xem popup đã hết trắng chưa.");
        if (tongThieu > 0)
            report.AppendLine("3) Gán tay sprite cho " + tongThieu + " Image liệt kê ở BƯỚC 4 (tool cố ý KHÔNG đoán).");
        else
            report.AppendLine("3) Không còn Image nào thiếu sprite — không phải gán tay gì.");
        report.AppendLine("4) Ctrl+S lưu scene/prefab nếu Unity hỏi.");
        report.AppendLine();
        report.AppendLine("GHI CHÚ AN TOÀN: tool KHÔNG đụng vào file .meta (giữ nguyên GUID," );
        report.AppendLine("mọi reference cũ còn nguyên) — chỉ đi qua TextureImporter API.");

        string duongDanReport = GhiReport(report.ToString());

        string tomTat =
            $"Đã sửa import: {daSua} ảnh (đúng sẵn {daDung}, lỗi {loiSua}).\n" +
            $"Image còn thiếu sprite trong prefab: {tongThieu}.\n" +
            (canCanhTay.Count > 0 ? $"Cần canh border tay: {canCanhTay.Count} ảnh.\n" : "") +
            "\nReport đầy đủ:\n" + (duongDanReport ?? "(không ghi được file — xem Console)");

        KetThuc(report, quiet, "Train — Sửa Import Sprite", tomTat);
        return report.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Soi prefab
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Liệt kê mọi Image có sprite == null trong các prefab Train. CHỈ ĐỌC — không
    /// sửa prefab: gán nhầm sprite còn khó phát hiện hơn là để trắng.
    /// </summary>
    private static int SoiPrefab(StringBuilder report)
    {
        if (!AssetDatabase.IsValidFolder(ThuMucPrefab))
        {
            report.AppendLine("  ? Không thấy thư mục " + ThuMucPrefab + " — bỏ qua bước soi prefab.");
            return 0;
        }

        int tong = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ThuMucPrefab });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Ưu tiên 3 prefab trong danh sách, nhưng vẫn soi hết cho chắc
            bool trongDanhSach = Array.IndexOf(PrefabCanSoi, prefab.name) >= 0;

            var thieu = new List<string>();
            foreach (var img in prefab.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img.sprite != null) continue;
                thieu.Add(DuongDanHierarchy(img.transform, prefab.transform));
            }

            if (thieu.Count == 0)
            {
                report.AppendLine($"  ✔ {prefab.name}: mọi Image đều có sprite." +
                                  (trongDanhSach ? "" : "  (ngoài danh sách trọng điểm)"));
                continue;
            }

            tong += thieu.Count;
            report.AppendLine($"  ✖ {prefab.name} ({path}): {thieu.Count} Image thiếu sprite");
            foreach (string h in thieu) report.AppendLine("       " + h);
        }

        if (guids.Length == 0)
            report.AppendLine("  ? Không có prefab nào trong " + ThuMucPrefab);

        return tong;
    }

    /// <summary>Đường dẫn hierarchy từ gốc prefab tới object (Root/Panel/Icon).</summary>
    private static string DuongDanHierarchy(Transform t, Transform goc)
    {
        string path = t.name;
        while (t.parent != null && t != goc)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gọi TrainPackageBuildTool qua reflection
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm TrainPackageBuildTool trong mọi assembly đã nạp và gọi hàm dựng lại
    /// prefab (public static, KHÔNG tham số) nếu có. Không có tool/không có hàm phù
    /// hợp → trả câu hướng dẫn để Sếp tự chạy, KHÔNG coi là lỗi.
    /// </summary>
    private static string GoiTrainPackageBuildTool()
    {
        Type t = null;
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { t = asm.GetType("TrainPackageBuildTool"); }
            catch { t = null; }
            if (t != null) break;
        }

        if (t == null)
            return "· Không thấy class TrainPackageBuildTool trong project — bỏ qua. " +
                   "Nếu prefab vẫn thiếu sprite, chạy tool dựng gói Train của bạn rồi bấm lại menu này.";

        string[] tenHam = { "RebuildPrefabs", "BuildPrefabs", "RebuildAll", "BuildAll", "Rebuild", "Build" };
        foreach (string ten in tenHam)
        {
            MethodInfo m;
            try { m = t.GetMethod(ten, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null); }
            catch { m = null; }
            if (m == null) continue;

            try
            {
                m.Invoke(null, null);
                AssetDatabase.Refresh();
                return $"✔ Đã gọi TrainPackageBuildTool.{ten}() dựng lại prefab.";
            }
            catch (Exception e)
            {
                Exception that = e.InnerException ?? e;
                Debug.LogException(that);
                return $"✖ TrainPackageBuildTool.{ten}() ném lỗi: {that.Message} — " +
                       "prefab có thể chưa được dựng lại, chạy tool đó bằng tay rồi bấm lại menu này.";
            }
        }

        return "· Có TrainPackageBuildTool nhưng không thấy hàm public static không-tham-số nào " +
               "trong (" + string.Join(", ", tenHam) + ") — Sếp chạy tool dựng gói Train bằng menu của nó.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Report
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi report ra &lt;ProjectRoot&gt;/production/session-state/ — NGOÀI Assets/ nên
    /// Unity không import thành TextAsset. Trả đường dẫn đầy đủ, null nếu ghi hỏng.
    /// </summary>
    private static string GhiReport(string noiDung)
    {
        try
        {
            string goc = Path.GetDirectoryName(Application.dataPath); // .../ProjectRoot
            if (string.IsNullOrEmpty(goc)) return null;

            string thuMuc = Path.Combine(goc, ThuMucReport.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(thuMuc);

            string duongDan = Path.Combine(thuMuc, TenReport);
            File.WriteAllText(duongDan, noiDung, new UTF8Encoding(false));
            return duongDan;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Train] Không ghi được report: " + e.Message);
            return null;
        }
    }

    private static void KetThuc(StringBuilder report, bool quiet, string tieuDe, string tomTat)
    {
        Debug.Log("[Train] " + tieuDe + "\n" + report);
        if (!quiet) EditorUtility.DisplayDialog(tieuDe, tomTat, "OK");
    }
}
