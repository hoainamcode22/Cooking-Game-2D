#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Map45/24. Nap Am Thanh Moi
///
/// VI SAO CAN CONG CU NAY (doc ky truoc khi sua):
///   AudioManager.AutoInit() la [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]. No tao MOT
///   GameObject "AudioManager" hoan toan moi TRUOC khi bat ky Scene nao load, roi
///   DontDestroyOnLoad. Moi AudioManager ma Sep da dat san trong Scene se tu huy o Awake vi
///   guard `_instance != this`. HE QUA: TAT CA cac o [SerializeField] AudioClip keo tay trong
///   Inspector deu KHONG BAO GIO duoc dung luc chay game. Duong duy nhat de mot file am thanh
///   thuc su keu la di qua LoadDefaultClipsIfMissing() -> Resources.Load<AudioClip>("Audio/...").
///
///   Vi vay cong cu nay CHEP (khong di chuyen) 8 file am thanh moi cua Sep tu
///   Assets/ÂM THANH GAME/ sang Assets/Resources/Audio/ voi TEN ASCII chuan (khong dau,
///   khong khoang trang) dung bang ten ma AudioManager dang tim.
///
/// AN TOAN: chi CHEP, khong bao gio xoa/di chuyen ban goc cua Sep. Neu file dich da co san
/// thi BO QUA (khong ghi de). Moi thao tac rui ro nam trong try/catch rieng.
/// </summary>
public class AudioImportTool : EditorWindow
{
    private const string ThuMucNguon = "Assets/ÂM THANH GAME";
    private const string ThuMucDich  = "Assets/Resources/Audio";

    // Nguong quyet dinh cach nen. Duoi nguong: Decompress On Load + preload (SFX ngan, keu ngay).
    // Tren nguong: Compressed In Memory (do ton RAM). TUYET DOI KHONG dung PCM — mot dot ra soat
    // truoc day tim ra 3 file PCM ngon ~100 MB trong ban build.
    private const long NguongByte = 200 * 1024;

    /// <summary>Mot dong anh xa: file goc -> truong trong AudioManager -> ten chuan trong Resources.</summary>
    private class DongAnhXa
    {
        public string TenFileGoc;
        public string TruongAudioManager;
        public string TenChuan;
        public string MoTa;
    }

    // BANG ANH XA CHOT. Ten chuan phai KHOP TUNG KY TU voi chuoi trong
    // AudioManager.LoadDefaultClipsIfMissing() ("Audio/<TenChuan>").
    private static readonly DongAnhXa[] BangAnhXa = new DongAnhXa[]
    {
        new DongAnhXa { TenFileGoc = "Âm thanh công nhân xây dựng.mp3", TruongAudioManager = "buildingHammer", TenChuan = "sfx_builder_hammer",  MoTa = "Tho xay dap bua" },
        new DongAnhXa { TenFileGoc = "buble.mp3",                       TruongAudioManager = "bubblePop",      TenChuan = "sfx_bubble_pop",      MoTa = "Bong bong no" },
        new DongAnhXa { TenFileGoc = "đặt công trình.mp3",              TruongAudioManager = "buildingPlace",  TenChuan = "sfx_building_place",  MoTa = "Dat cong trinh xuong" },
        new DongAnhXa { TenFileGoc = "kim cương.mp3",                   TruongAudioManager = "gemSparkle",     TenChuan = "sfx_gem",             MoTa = "Kim cuong lap lanh" },
        new DongAnhXa { TenFileGoc = "lên cấp.mp3",                     TruongAudioManager = "fanfareLevelUp", TenChuan = "sfx_levelup",         MoTa = "Fanfare len cap" },
        new DongAnhXa { TenFileGoc = "tàu hỏa.mp3",                     TruongAudioManager = "trainWhistle",   TenChuan = "sfx_train_whistle",   MoTa = "Coi tau hoa" },
        new DongAnhXa { TenFileGoc = "tàu thủy.mp3",                    TruongAudioManager = "boatHorn",       TenChuan = "sfx_boat_horn",       MoTa = "Coi tau thuy" },
        new DongAnhXa { TenFileGoc = "vàng.mp3",                        TruongAudioManager = "coinTing",       TenChuan = "sfx_coin",            MoTa = "Tieng vang leng keng" },
    };

    private Vector2 _cuonBang;
    private Vector2 _cuonNhatKy;
    private readonly List<string> _nhatKy = new List<string>();
    private bool _daQuet;
    private string[] _fileLaTrongThuMucNguon = new string[0];

    [MenuItem("Tools/Map45/24. Nap Am Thanh Moi", false, 24)]
    public static void MoCuaSo()
    {
        AudioImportTool w = GetWindow<AudioImportTool>(true, "24. Nạp Âm Thanh Mới", true);
        w.minSize = new Vector2(880f, 560f);
        w.QuetThuMuc();
        w.Show();
    }

    // ── QUET ────────────────────────────────────────────────────────────────────────────
    private void QuetThuMuc()
    {
        _daQuet = true;
        _fileLaTrongThuMucNguon = new string[0];

        try
        {
            string duongDanTuyetDoi = ToAbsolute(ThuMucNguon);
            if (!Directory.Exists(duongDanTuyetDoi))
            {
                Ghi("KHONG THAY thu muc nguon: " + ThuMucNguon);
                return;
            }

            List<string> la = new List<string>();
            string[] tatCa = Directory.GetFiles(duongDanTuyetDoi);
            for (int i = 0; i < tatCa.Length; i++)
            {
                string ten = Path.GetFileName(tatCa[i]);
                if (ten.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (TimDongTheoTenGoc(ten) == null) la.Add(ten);
            }
            _fileLaTrongThuMucNguon = la.ToArray();
        }
        catch (Exception e)
        {
            Ghi("LOI khi quet thu muc nguon: " + e.Message);
        }
    }

    private static DongAnhXa TimDongTheoTenGoc(string ten)
    {
        for (int i = 0; i < BangAnhXa.Length; i++)
            if (string.Equals(BangAnhXa[i].TenFileGoc, ten, StringComparison.Ordinal))
                return BangAnhXa[i];
        return null;
    }

    private static string ToAbsolute(string duongDanAssets)
    {
        // Application.dataPath tro toi <project>/Assets, con duongDanAssets bat dau bang "Assets/".
        return Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + duongDanAssets;
    }

    private static string DuongDanNguon(DongAnhXa d) { return ThuMucNguon + "/" + d.TenFileGoc; }
    private static string DuongDanDich(DongAnhXa d)  { return ThuMucDich  + "/" + d.TenChuan + Path.GetExtension(d.TenFileGoc); }

    private static bool NguonTonTai(DongAnhXa d)
    {
        try { return File.Exists(ToAbsolute(DuongDanNguon(d))); } catch { return false; }
    }

    private static bool DichTonTai(DongAnhXa d)
    {
        try { return File.Exists(ToAbsolute(DuongDanDich(d))); } catch { return false; }
    }

    private static long KichThuocNguon(DongAnhXa d)
    {
        try
        {
            FileInfo fi = new FileInfo(ToAbsolute(DuongDanNguon(d)));
            return fi.Exists ? fi.Length : 0L;
        }
        catch { return 0L; }
    }

    // ── GIAO DIEN ───────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!_daQuet) QuetThuMuc();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("NẠP 8 ÂM THANH MỚI VÀO RESOURCES", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Vì sao phải chép sang Resources?\n" +
            "AudioManager tự sinh một bản mới trước khi Scene load (RuntimeInitializeOnLoadMethod), " +
            "nên MỌI ô AudioClip kéo tay trong Inspector đều CHẾT lúc chạy game. Đường duy nhất để " +
            "một file kêu được là Resources.Load(\"Audio/<tên chuẩn>\").\n\n" +
            "Công cụ này CHỈ CHÉP — bản gốc trong \"" + ThuMucNguon + "\" của Sếp giữ nguyên. " +
            "File đích đã có sẵn thì BỎ QUA, không ghi đè.",
            MessageType.Info);

        EditorGUILayout.LabelField("Nguồn:", ThuMucNguon);
        EditorGUILayout.LabelField("Đích:",  ThuMucDich);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("BẢNG ÁNH XẠ (chỉ đọc)", EditorStyles.boldLabel);

        _cuonBang = EditorGUILayout.BeginScrollView(_cuonBang, GUILayout.MinHeight(200f));
        VeDongTieuDe();
        for (int i = 0; i < BangAnhXa.Length; i++) VeMotDong(BangAnhXa[i]);
        EditorGUILayout.EndScrollView();

        if (_fileLaTrongThuMucNguon.Length > 0)
        {
            EditorGUILayout.HelpBox(
                "Có " + _fileLaTrongThuMucNguon.Length + " file trong thư mục nguồn KHÔNG nằm trong bảng ánh xạ " +
                "(công cụ sẽ bỏ qua):\n  • " + string.Join("\n  • ", _fileLaTrongThuMucNguon),
                MessageType.Warning);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Quét lại", GUILayout.Height(30f), GUILayout.Width(120f)))
                QuetThuMuc();

            if (GUILayout.Button("Chạy thử (KHÔNG đổi gì)", GUILayout.Height(30f)))
                ChayThu();

            GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f);
            if (GUILayout.Button("CHÉP THẬT vào Resources/Audio", GUILayout.Height(30f)))
                XacNhanRoiChep();
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("NHẬT KÝ", EditorStyles.boldLabel);
        _cuonNhatKy = EditorGUILayout.BeginScrollView(_cuonNhatKy, GUILayout.MinHeight(150f));
        for (int i = 0; i < _nhatKy.Count; i++) EditorGUILayout.LabelField("• " + _nhatKy[i]);
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Xoá nhật ký", GUILayout.Width(120f))) _nhatKy.Clear();
    }

    private void VeDongTieuDe()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("File gốc",       EditorStyles.miniBoldLabel, GUILayout.Width(250f));
        EditorGUILayout.LabelField("Trường",         EditorStyles.miniBoldLabel, GUILayout.Width(120f));
        EditorGUILayout.LabelField("Tên chuẩn",      EditorStyles.miniBoldLabel, GUILayout.Width(170f));
        EditorGUILayout.LabelField("Cỡ",             EditorStyles.miniBoldLabel, GUILayout.Width(70f));
        EditorGUILayout.LabelField("Nén dự kiến",    EditorStyles.miniBoldLabel, GUILayout.Width(150f));
        EditorGUILayout.LabelField("Trạng thái",     EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void VeMotDong(DongAnhXa d)
    {
        bool coNguon = NguonTonTai(d);
        bool coDich  = DichTonTai(d);
        long co      = KichThuocNguon(d);

        string trangThai = !coNguon ? "THIẾU FILE GỐC"
                         : coDich   ? "Đã có ở đích → bỏ qua"
                                    : "Sẽ chép";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(d.TenFileGoc, GUILayout.Width(250f));
        EditorGUILayout.LabelField(d.TruongAudioManager, GUILayout.Width(120f));
        EditorGUILayout.LabelField(d.TenChuan, GUILayout.Width(170f));
        EditorGUILayout.LabelField(coNguon ? (co / 1024L) + " KB" : "-", GUILayout.Width(70f));
        EditorGUILayout.LabelField(co <= NguongByte ? "Vorbis + DecompressOnLoad" : "Vorbis + CompressedInMemory", GUILayout.Width(150f));

        Color cu = GUI.color;
        if (!coNguon) GUI.color = new Color(1f, 0.5f, 0.5f);
        else if (coDich) GUI.color = new Color(0.75f, 0.75f, 0.75f);
        EditorGUILayout.LabelField(trangThai);
        GUI.color = cu;
        EditorGUILayout.EndHorizontal();
    }

    // ── CHAY THU ────────────────────────────────────────────────────────────────────────
    private void ChayThu()
    {
        _nhatKy.Clear();
        Ghi("=== CHẠY THỬ — không có gì bị thay đổi ===");

        int seChep = 0, boQua = 0, thieu = 0;
        for (int i = 0; i < BangAnhXa.Length; i++)
        {
            DongAnhXa d = BangAnhXa[i];
            if (!NguonTonTai(d))      { Ghi("THIẾU GỐC : " + DuongDanNguon(d)); thieu++; continue; }
            if (DichTonTai(d))        { Ghi("BỎ QUA    : " + DuongDanDich(d) + " (đã có)"); boQua++; continue; }
            long co = KichThuocNguon(d);
            Ghi("SẼ CHÉP   : " + DuongDanNguon(d) + "  →  " + DuongDanDich(d) +
                "   [" + (co / 1024L) + " KB, " + (co <= NguongByte ? "DecompressOnLoad + preload" : "CompressedInMemory") + "]");
            seChep++;
        }
        Ghi("=== Tổng: sẽ chép " + seChep + ", bỏ qua " + boQua + ", thiếu gốc " + thieu + " ===");
    }

    // ── CHEP THAT ───────────────────────────────────────────────────────────────────────
    private void XacNhanRoiChep()
    {
        bool dongY = EditorUtility.DisplayDialog(
            "Chép âm thanh vào Resources?",
            "Công cụ sẽ CHÉP (không di chuyển, không xoá) các file từ\n" + ThuMucNguon +
            "\nsang\n" + ThuMucDich +
            "\n\nBản gốc của Sếp giữ nguyên. File đích đã tồn tại sẽ được bỏ qua.\n\nTiếp tục?",
            "Chép ngay", "Huỷ");

        if (!dongY) { Ghi("Người dùng đã huỷ."); return; }
        ChepThat();
    }

    private void ChepThat()
    {
        _nhatKy.Clear();
        Ghi("=== CHÉP THẬT ===");

        // Buoc 1: bao dam thu muc dich ton tai.
        try
        {
            string tuyetDoi = ToAbsolute(ThuMucDich);
            if (!Directory.Exists(tuyetDoi))
            {
                Directory.CreateDirectory(tuyetDoi);
                AssetDatabase.Refresh();
                Ghi("Đã tạo thư mục đích: " + ThuMucDich);
            }
        }
        catch (Exception e)
        {
            Ghi("LỖI tạo thư mục đích: " + e.Message);
            return;
        }

        List<string> canDatImport = new List<string>();
        int daChep = 0, boQua = 0, thieu = 0, loi = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < BangAnhXa.Length; i++)
            {
                DongAnhXa d = BangAnhXa[i];
                string nguon = DuongDanNguon(d);
                string dich  = DuongDanDich(d);

                if (!NguonTonTai(d)) { Ghi("THIẾU GỐC : " + nguon); thieu++; continue; }
                if (DichTonTai(d))   { Ghi("BỎ QUA    : " + dich + " (đã có, không ghi đè)"); boQua++; canDatImport.Add(dich); continue; }

                // Moi lan chep la mot thao tac rui ro rieng — hong mot file khong duoc keo do ca me.
                try
                {
                    if (AssetDatabase.CopyAsset(nguon, dich))
                    {
                        Ghi("ĐÃ CHÉP   : " + nguon + "  →  " + dich);
                        canDatImport.Add(dich);
                        daChep++;
                    }
                    else
                    {
                        Ghi("LỖI CHÉP  : " + nguon + " (CopyAsset trả về false)");
                        loi++;
                    }
                }
                catch (Exception e)
                {
                    Ghi("LỖI CHÉP  : " + nguon + " — " + e.Message);
                    loi++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        try { AssetDatabase.Refresh(); }
        catch (Exception e) { Ghi("LỖI Refresh: " + e.Message); }

        // Buoc 2: dat thiet lap import cho tung ban sao.
        for (int i = 0; i < canDatImport.Count; i++)
            DatThietLapImport(canDatImport[i]);

        try { AssetDatabase.SaveAssets(); }
        catch (Exception e) { Ghi("LỖI SaveAssets: " + e.Message); }

        Ghi("=== Tổng: chép " + daChep + ", bỏ qua " + boQua + ", thiếu gốc " + thieu + ", lỗi " + loi + " ===");
        Ghi("Bước tiếp theo: chạy game. AudioManager.LoadDefaultClipsIfMissing() sẽ tự nạp qua Resources.Load.");
        QuetThuMuc();
    }

    /// <summary>
    /// SFX ngan (&lt;= 200 KB): Vorbis + Decompress On Load + preload -> keu tuc thi, khong giat.
    /// Dai hon: Vorbis + Compressed In Memory -> do ton RAM.
    /// TUYET DOI KHONG dung PCM (mot dot ra soat truoc day tim ra 3 file PCM ngon ~100 MB build).
    /// </summary>
    private void DatThietLapImport(string duongDanAsset)
    {
        try
        {
            AudioImporter imp = AssetImporter.GetAtPath(duongDanAsset) as AudioImporter;
            if (imp == null) { Ghi("BỎ QUA IMPORT: " + duongDanAsset + " (không phải AudioImporter)"); return; }

            long co = 0L;
            try { co = new FileInfo(ToAbsolute(duongDanAsset)).Length; } catch { co = 0L; }

            bool ngan = co <= NguongByte;

            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            s.loadType         = ngan ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;   // KHONG BAO GIO PCM
            s.quality          = 0.7f;
            s.preloadAudioData = ngan;     // preload cho SFX ngan de bam la keu (da chuyen sang SampleSettings)
            imp.defaultSampleSettings = s; // gan SAU khi da set du moi truong

            imp.loadInBackground = !ngan;  // van nam tren AudioImporter, KHONG deprecate
            imp.forceToMono      = true;   // SFX 2D — mono giam nua dung luong, khong mat gi

            EditorUtility.SetDirty(imp);
            imp.SaveAndReimport();

            Ghi("IMPORT OK : " + duongDanAsset + " → Vorbis, " +
                (ngan ? "DecompressOnLoad + preload" : "CompressedInMemory") + ", mono");
        }
        catch (Exception e)
        {
            Ghi("LỖI IMPORT: " + duongDanAsset + " — " + e.Message);
        }
    }

    private void Ghi(string dong)
    {
        _nhatKy.Add(dong);
        Debug.Log("[NapAmThanh] " + dong);
    }
}
#endif
