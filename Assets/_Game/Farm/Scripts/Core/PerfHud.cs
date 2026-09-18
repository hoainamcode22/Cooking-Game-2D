using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// [PERF P0] BANG DO HIEU NANG TREN MAY THAT (ON-DEVICE PERFORMANCE HUD).
///
/// VI SAO CAN: moi con so hieu nang trong du an nay tu truoc den gio deu la UOC LUONG.
/// File nay bien chung thanh SO DO DUOC: chay tren chinh chiec dien thoai cua Sep,
/// doc thang tu bo dem Profiler cua Unity, va xuat ra CSV de gui ve phan tich.
///
/// ── QUAN HE VOI RealtimePerformanceProfiler (DA CO SAN) ─────────────────────
/// Du an DA CO <c>CookingGame.Optimization.RealtimePerformanceProfiler</c>
/// (Assets/_Game/Scripts/Optimization/). Lop do CHI lam duoc 4 thu:
/// FPS trung binh, frame time, RAM tong (Profiler.GetTotalAllocatedMemoryLong)
/// va toc do sinh rac uoc luong theo Mono heap. No KHONG co:
///   • 1% low (chinh la "giat khuc" ma Sep dang mo ta — FPS trung binh giau no di),
///   • Draw Calls / Batches / SetPass / Triangles / Vertices,
///   • GC Allocated In Frame (chi so THAT su chi diem thu pham giat),
///   • CPU main thread / render thread tach rieng,
///   • dong xac nhan targetFrameRate + vSyncCount tren may that,
///   • xuat CSV,
///   • cu cham tren mobile (no ve mot nut IMGUI DE LO tay bam va dung chuoi
///     noi suy MOI LAN OnGUI chay — ton chinh cai ma no dang do).
/// => PerfHud KHONG viet lai phan trung; no LAM PHAN CON THIEU. Hai lop chay chung
/// duoc, nhung xem muc "XUNG DOT PHIM" ben duoi.
///
/// ── XUNG DOT PHIM (DOC TRUOC KHI TEST TRONG EDITOR) ────────────────────────
/// RealtimePerformanceProfiler dang chiem F4 lam phim bat/tat cua no. PerfHud dung
/// F4 lam phim XUAT CSV. Trong Editor, bam F4 se lam CA HAI viec cung luc (xuat CSV
/// + bat/tat bang cu). Khong sap gi ca, chi hoi roi mat. Tren may that thi khong
/// lien quan vi Sep dung cu cham chu khong co ban phim.
///
/// ── CACH BAT/TAT ───────────────────────────────────────────────────────────
///   • CHAM 4 NGON cung luc            → bat/tat HUD (luon dung duoc, ke ca khi dang hien).
///   • CHAM 3 LAN vao GOC TREN BEN TRAI → dang AN thi BAT; dang HIEN thi XUAT CSV.
///   • F3 (Editor/PC)                   → bat/tat HUD.
///   • F4 (Editor/PC)                   → xuat CSV.
/// Cu cham chon kho bam nham: phai du 3 nhip trong 1.2 giay va PHAI nam gon trong o
/// 22% x 18% goc tren trai — vung ma gameplay nong trai khong dung den.
///
/// ── AN TOAN KHI PHAT HANH ──────────────────────────────────────────────────
/// TOAN BO than lop nam trong <c>#if DEVELOPMENT_BUILD || UNITY_EDITOR</c>. Ban
/// release bien dich ra mot MonoBehaviour RONG, khong tu sinh GameObject, khong
/// OnGUI, khong ProfilerRecorder. Sep KHONG can nho xoa file nay truoc khi ship.
///
/// ── QUAN TRONG: MUON THAY SO THI PHAI BUILD DEVELOPMENT ────────────────────
/// Cac bo dem Draw Calls / Batches / SetPass / Triangles / Vertices / Memory CHI
/// ton tai trong Editor va trong Development Build. Ban release binh thuong se
/// khong co chung — luc do HUD hien "n/a" chu KHONG nem loi.
/// </summary>
[DisallowMultipleComponent]
public sealed class PerfHud : MonoBehaviour
{
#if DEVELOPMENT_BUILD || UNITY_EDITOR

    // ─────────────────────────────────────────────────────────────────────────
    // HANG SO CHINH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Nhip lam moi so lieu: 4 lan/giay. KHONG tinh moi frame — HUD phai gan nhu mien phi.</summary>
    private const float NHIP_LAM_MOI = 0.25f;

    /// <summary>Cua so thong ke: 5 giay gan nhat (dung cho FPS trung binh + 1% low).</summary>
    private const float CUA_SO_GIAY = 5f;

    /// <summary>So frame toi da giu trong vong dem. 512 frame ~ 8.5 giay o 60fps — du che 5 giay.</summary>
    private const int SUC_CHUA_FRAME = 512;

    /// <summary>So mau CSV giu lai (1 mau/giay) — khoang 60 giay gan nhat.</summary>
    private const int SUC_CHUA_MAU = 64;

    /// <summary>Ten file CSV trong Application.persistentDataPath.</summary>
    private const string TEN_FILE_CSV = "perf_log.csv";

    /// <summary>Gia tri bao "khong doc duoc bo dem nay" — hien thi thanh "n/a".</summary>
    private const long KHONG_CO = -1L;

    // ─────────────────────────────────────────────────────────────────────────
    // MAU CSV — struct LONG NHAU, khong tao them file/class (luat 1 class 1 file)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mot mau do trong 1 giay. Dung struct long nhau de khong pha luat "1 class 1 file",
    /// va de mang mau nam lien khoi trong bo nho (khong sinh rac).
    ///
    /// LUU Y: <see cref="GiayUnix"/> la <c>long</c>, KHONG phai float — float 32-bit
    /// khong du do chinh xac de giu so giay Unix (hien tai ~1.7 ty) va se lam tron
    /// sai hang tram giay.
    /// </summary>
    private struct Mau
    {
        public long GiayUnix;
        public float Fps;
        public float FpsMotPhanTram;
        public float FpsTrungBinh5s;
        public float FrameMs;
        public float MainMs;
        public float RenderMs;
        public long DrawCalls;
        public long SetPass;
        public long Batches;
        public long Triangles;
        public long Vertices;
        public long BoNhoDungByte;
        public long GcDungByte;
        public long TextureByte;
        public long GcMoiFrameByte;
        public long Audio;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TRANG THAI
    // ─────────────────────────────────────────────────────────────────────────

    private static PerfHud _thucThe;

    private bool _dangHien;              // MAC DINH TAT.
    private bool _daKhoiDongBoDem;

    // Vong dem thoi gian frame (giay).
    private readonly float[] _dt = new float[SUC_CHUA_FRAME];
    private readonly float[] _dtSapXep = new float[SUC_CHUA_FRAME];
    private int _dtDau;
    private int _dtSo;

    // Vong dem mau CSV.
    private readonly Mau[] _mau = new Mau[SUC_CHUA_MAU];
    private int _mauDau;
    private int _mauSo;

    // Bo dem nhip.
    private float _dongHoLamMoi;
    private float _dongHoMau;
    private int _frameTuLanLamMoi;
    private float _thoiGianTuLanLamMoi;

    // So lieu da tinh.
    private float _fpsHienTai;
    private float _fpsMotPhanTram;
    private float _fpsTrungBinh5s;
    private float _frameMs;
    private float _mainMs = -1f;
    private float _renderMs = -1f;

    // Chuoi dung san — CHI dung lai khi lam moi so lieu (4 lan/giay), KHONG dung trong OnGUI.
    private readonly StringBuilder _sb = new StringBuilder(1024);
    private string _chuoiHud = string.Empty;

    // GUI cache.
    private GUIStyle _kieu;
    private Texture2D _nen;
    private Rect _khung;
    private int _wCu;
    private int _hCu;

    // Cu cham.
    private float _lanChamGocCuoi;
    private int _soLanChamGoc;
    private float _khoaCuCham;
    private bool _bonNgonDangCham;

    // Ten cac muc quality — QualitySettings.names CAP PHAT mang moi lan goi,
    // nen doc DUNG MOT LAN roi giu lai.
    private string[] _tenQuality;

    // ─────────────────────────────────────────────────────────────────────────
    // BO DEM PROFILER
    // Chi co du lieu trong Editor / Development Build. Ngoai ra Valid == false
    // va HUD hien "n/a".
    // ─────────────────────────────────────────────────────────────────────────

    private ProfilerRecorder _rDrawCalls;
    private ProfilerRecorder _rSetPass;
    private ProfilerRecorder _rBatches;
    private ProfilerRecorder _rTriangles;
    private ProfilerRecorder _rVertices;

    private ProfilerRecorder _rTongDung;
    private ProfilerRecorder _rTongDatTruoc;
    private ProfilerRecorder _rGcDung;
    private ProfilerRecorder _rTexture;
    private ProfilerRecorder _rGcMoiFrame;

    private ProfilerRecorder _rMainThread;
    private ProfilerRecorder _rRenderThread;
    private ProfilerRecorder _rAudio;

    // ─────────────────────────────────────────────────────────────────────────
    // TU SINH RA — Sep khong phai keo component vao scene nao
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tu tao GameObject DontDestroyOnLoad sau khi scene dau tien nap xong.
    /// Chan tao trung: neu <see cref="_thucThe"/> da co thi thoi.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiTao()
    {
        if (_thucThe != null) return;

        var go = new GameObject("~PerfHud");
        DontDestroyOnLoad(go);
        _thucThe = go.AddComponent<PerfHud>();
    }

    private void Awake()
    {
        // Chan tao trung lan hai: neu ai do keo tay component nay vao scene.
        if (_thucThe != null && _thucThe != this)
        {
            Destroy(this);
            return;
        }

        _thucThe = this;
        _tenQuality = QualitySettings.names;   // doc 1 lan, khong doc lai moi frame.
        _chuoiHud = "[PerfHud] dang do...";
    }

    private void OnEnable()
    {
        KhoiDongBoDem();
    }

    private void OnDisable()
    {
        GiaiPhongBoDem();
    }

    private void OnDestroy()
    {
        GiaiPhongBoDem();           // goi hai lan van an toan (co co _daKhoiDongBoDem).

        if (_nen != null)
        {
            Destroy(_nen);
            _nen = null;
        }

        if (_thucThe == this) _thucThe = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BO DEM: TAO CO KIEM TRA + GIAI PHONG DU
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tao mot ProfilerRecorder mot cach AN TOAN. Neu ten bo dem khong ton tai tren
    /// nen tang / cau hinh build nay, tra ve <c>default</c> (Valid == false) thay vi nem loi.
    /// </summary>
    private static ProfilerRecorder Tao(ProfilerCategory nhom, string ten, List<string> thieu)
    {
        try
        {
            ProfilerRecorder r = ProfilerRecorder.StartNew(nhom, ten);
            if (r.Valid) return r;
            r.Dispose();
        }
        catch (Exception)
        {
            // Nuot loi co y: HUD la cong cu do, KHONG bao gio duoc phep lam sap game.
        }

        if (thieu != null) thieu.Add(ten);
        return default;
    }

    /// <summary>
    /// Thu lan luot nhieu ten bo dem, lay cai dau tien hop le. Dung cho nhung bo dem
    /// ma ten co the khac nhau giua cac phien ban Unity (render thread, audio).
    /// </summary>
    private static ProfilerRecorder TaoThuNhieuTen(ProfilerCategory nhom, string[] cacTen, List<string> thieu)
    {
        for (int i = 0; i < cacTen.Length; i++)
        {
            try
            {
                ProfilerRecorder r = ProfilerRecorder.StartNew(nhom, cacTen[i]);
                if (r.Valid) return r;
                r.Dispose();
            }
            catch (Exception)
            {
                // bo qua, thu ten tiep theo.
            }
        }

        if (thieu != null && cacTen.Length > 0) thieu.Add(cacTen[0]);
        return default;
    }

    private void KhoiDongBoDem()
    {
        if (_daKhoiDongBoDem) return;
        _daKhoiDongBoDem = true;

        var thieu = new List<string>();

        _rDrawCalls = Tao(ProfilerCategory.Render, "Draw Calls Count", thieu);
        _rSetPass   = Tao(ProfilerCategory.Render, "SetPass Calls Count", thieu);
        _rBatches   = Tao(ProfilerCategory.Render, "Batches Count", thieu);
        _rTriangles = Tao(ProfilerCategory.Render, "Triangles Count", thieu);
        _rVertices  = Tao(ProfilerCategory.Render, "Vertices Count", thieu);

        _rTongDung     = Tao(ProfilerCategory.Memory, "Total Used Memory", thieu);
        _rTongDatTruoc = Tao(ProfilerCategory.Memory, "Total Reserved Memory", thieu);
        _rGcDung       = Tao(ProfilerCategory.Memory, "GC Used Memory", thieu);
        _rTexture      = Tao(ProfilerCategory.Memory, "Texture Memory", thieu);
        _rGcMoiFrame   = Tao(ProfilerCategory.Memory, "GC Allocated In Frame", thieu);

        // "Main Thread" nam trong nhom Internal (theo tai lieu ProfilerRecorder cua Unity).
        _rMainThread = Tao(ProfilerCategory.Internal, "Main Thread", thieu);

        // Render thread: ten KHONG dong nhat giua cac phien ban Unity => thu lan luot.
        _rRenderThread = TaoThuNhieuTen(
            ProfilerCategory.Render,
            new[] { "Render Thread", "CPU Render Thread Frame Time", "Render Thread Frame Time" },
            thieu);

        // Audio: chi lay neu re. Ten cung khong dong nhat => thu lan luot, khong co thi thoi.
        _rAudio = TaoThuNhieuTen(
            ProfilerCategory.Audio,
            new[] { "Playing Audio Sources", "Audio Voices", "Total Audio Voices" },
            thieu);

        if (thieu.Count > 0)
        {
            _sb.Length = 0;
            _sb.Append("[PerfHud] Khong gan duoc ").Append(thieu.Count.ToString(CultureInfo.InvariantCulture)).Append(" bo dem (se hien 'n/a'): ");
            for (int i = 0; i < thieu.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                _sb.Append(thieu[i]);
            }
            _sb.Append(" | Nho: cac bo dem nay CHI co trong Editor va Development Build.");
            Debug.Log(_sb.ToString());
            _sb.Length = 0;
        }
    }

    /// <summary>
    /// Giai phong TAT CA ProfilerRecorder. Bo dem bi bo quen la ro ri bo nho THAT.
    /// Goi duoc nhieu lan (idempotent).
    /// </summary>
    private void GiaiPhongBoDem()
    {
        if (!_daKhoiDongBoDem) return;
        _daKhoiDongBoDem = false;

        Huy(ref _rDrawCalls);
        Huy(ref _rSetPass);
        Huy(ref _rBatches);
        Huy(ref _rTriangles);
        Huy(ref _rVertices);

        Huy(ref _rTongDung);
        Huy(ref _rTongDatTruoc);
        Huy(ref _rGcDung);
        Huy(ref _rTexture);
        Huy(ref _rGcMoiFrame);

        Huy(ref _rMainThread);
        Huy(ref _rRenderThread);
        Huy(ref _rAudio);
    }

    private static void Huy(ref ProfilerRecorder r)
    {
        try
        {
            if (r.Valid) r.Dispose();
        }
        catch (Exception)
        {
            // khong bao gio duoc phep nem tu duong dispose.
        }

        r = default;
    }

    /// <summary>Doc mot bo dem. Tra ve <see cref="KHONG_CO"/> neu bo dem khong ton tai.</summary>
    private static long Doc(ref ProfilerRecorder r)
    {
        if (!r.Valid) return KHONG_CO;

        try
        {
            return r.LastValue;
        }
        catch (Exception)
        {
            return KHONG_CO;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE — duong chay MOI FRAME phai TUYET DOI khong cap phat
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // 1) Ghi frame time vao vong dem (ghi de, khong cap phat).
        _dt[_dtDau] = dt;
        _dtDau = (_dtDau + 1) % SUC_CHUA_FRAME;
        if (_dtSo < SUC_CHUA_FRAME) _dtSo++;

        _frameTuLanLamMoi++;
        _thoiGianTuLanLamMoi += dt;

        // 2) Cu cham / phim tat.
        DocCuCham(dt);

        // 3) Lam moi so lieu 4 lan/giay. Chi o day moi tinh toan + dung chuoi.
        _dongHoLamMoi += dt;
        if (_dongHoLamMoi >= NHIP_LAM_MOI)
        {
            _dongHoLamMoi = 0f;
            TinhSoLieu();

            // Dang AN thi KHONG dung chuoi: tat han duong cap phat duy nhat cua HUD.
            if (_dangHien) DungChuoiHud();
            _frameTuLanLamMoi = 0;
            _thoiGianTuLanLamMoi = 0f;
        }

        // 4) Luu mau CSV moi giay.
        _dongHoMau += dt;
        if (_dongHoMau >= 1f)
        {
            _dongHoMau = 0f;
            LuuMau();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CU CHAM + PHIM TAT
    // ─────────────────────────────────────────────────────────────────────────

    private void DocCuCham(float dt)
    {
        if (_khoaCuCham > 0f) _khoaCuCham -= dt;

        // ── Phim tat (Editor / PC) ──
        if (PhimVuaBam(true))  { BatTat(); return; }
        if (PhimVuaBam(false)) { XuatCsv(); return; }

        // ── Cham 4 ngon: canh len (0..3 ngon -> >=4 ngon) ──
        int soNgon = DemNgon();
        if (soNgon >= 4)
        {
            if (!_bonNgonDangCham && _khoaCuCham <= 0f)
            {
                _bonNgonDangCham = true;
                _khoaCuCham = 1f;
                BatTat();
                return;
            }
        }
        else if (soNgon == 0)
        {
            _bonNgonDangCham = false;
        }

        // ── Cham 3 lan goc tren trai ──
        if (soNgon != 1) return;
        if (!ChamXuongTrongFrameNay(out Vector2 diem)) return;
        if (_khoaCuCham > 0f) return;

        // Goc TREN BEN TRAI. Toa do cham: y tinh tu DAY man hinh len.
        bool trongGoc = diem.x <= Screen.width * 0.22f &&
                        diem.y >= Screen.height * 0.82f;
        if (!trongGoc)
        {
            _soLanChamGoc = 0;
            return;
        }

        float bayGio = Time.unscaledTime;
        if (bayGio - _lanChamGocCuoi > 1.2f) _soLanChamGoc = 0;

        _lanChamGocCuoi = bayGio;
        _soLanChamGoc++;

        if (_soLanChamGoc < 3) return;

        _soLanChamGoc = 0;
        _khoaCuCham = 1f;

        // Dang AN thi BAT len. Dang HIEN thi XUAT CSV (van con cham 4 ngon de tat).
        if (_dangHien) XuatCsv();
        else BatTat();
    }

    private void BatTat()
    {
        _dangHien = !_dangHien;
        if (_dangHien)
        {
            TinhSoLieu();
            DungChuoiHud();
        }
    }

    /// <summary><c>true</c> = phim F3 (bat/tat); <c>false</c> = phim F4 (xuat CSV).</summary>
    private static bool PhimVuaBam(bool laF3)
    {
#if ENABLE_INPUT_SYSTEM
        var ban = UnityEngine.InputSystem.Keyboard.current;
        if (ban != null)
        {
            if (laF3) { if (ban.f3Key.wasPressedThisFrame) return true; }
            else      { if (ban.f4Key.wasPressedThisFrame) return true; }
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(laF3 ? KeyCode.F3 : KeyCode.F4)) return true;
#endif
        return false;
    }

    private static int DemNgon()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0) return Input.touchCount;
#endif
#if ENABLE_INPUT_SYSTEM
        var man = UnityEngine.InputSystem.Touchscreen.current;
        if (man != null)
        {
            int dem = 0;
            var cacNgon = man.touches;
            for (int i = 0; i < cacNgon.Count; i++)
            {
                if (cacNgon[i].press.isPressed) dem++;
            }
            return dem;
        }
#endif
        return 0;
    }

    private static bool ChamXuongTrongFrameNay(out Vector2 diem)
    {
        diem = Vector2.zero;

#if ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == UnityEngine.TouchPhase.Began)
            {
                diem = t.position;
                return true;
            }
        }
#endif
#if ENABLE_INPUT_SYSTEM
        var man = UnityEngine.InputSystem.Touchscreen.current;
        if (man != null && man.primaryTouch.press.wasPressedThisFrame)
        {
            diem = man.primaryTouch.position.ReadValue();
            return true;
        }
#endif
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TINH SO LIEU (4 lan/giay)
    // ─────────────────────────────────────────────────────────────────────────

    private void TinhSoLieu()
    {
        // FPS hien tai: tinh tren dung nhip vua roi, on dinh hon 1/dt cua 1 frame le.
        _fpsHienTai = _thoiGianTuLanLamMoi > 0f && _frameTuLanLamMoi > 0
            ? _frameTuLanLamMoi / _thoiGianTuLanLamMoi
            : 0f;

        // Lay cac frame nam trong 5 giay gan nhat, di NGUOC tu frame moi nhat.
        int n = 0;
        float tong = 0f;
        int viTri = _dtDau;
        for (int i = 0; i < _dtSo; i++)
        {
            viTri--;
            if (viTri < 0) viTri = SUC_CHUA_FRAME - 1;

            float d = _dt[viTri];
            if (tong + d > CUA_SO_GIAY && n > 0) break;

            _dtSapXep[n] = d;
            n++;
            tong += d;
        }

        if (n <= 0)
        {
            _fpsTrungBinh5s = 0f;
            _fpsMotPhanTram = 0f;
            _frameMs = 0f;
        }
        else
        {
            _fpsTrungBinh5s = n / tong;
            _frameMs = (tong / n) * 1000f;

            // 1% LOW = FPS trung binh cua 1% FRAME TE NHAT. Day moi la "giat khuc";
            // FPS trung binh che no di hoan toan.
            // Array.Sort tai cho tren mang da cap phat san => KHONG sinh rac.
            Array.Sort(_dtSapXep, 0, n);

            int k = n / 100;
            if (k < 1) k = 1;

            float tongTe = 0f;
            for (int i = n - k; i < n; i++) tongTe += _dtSapXep[i];

            float dtTe = tongTe / k;
            _fpsMotPhanTram = dtTe > 0f ? 1f / dtTe : 0f;
        }

        // Main / render thread: bo dem tra ve NANO giay.
        long main = Doc(ref _rMainThread);
        _mainMs = main >= 0 ? main * 1e-6f : -1f;

        long render = Doc(ref _rRenderThread);
        _renderMs = render >= 0 ? render * 1e-6f : -1f;
    }

    private void LuuMau()
    {
        Mau m;
        m.GiayUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();   // long, KHONG dung float.
        m.Fps = _fpsHienTai;
        m.FpsMotPhanTram = _fpsMotPhanTram;
        m.FpsTrungBinh5s = _fpsTrungBinh5s;
        m.FrameMs = _frameMs;
        m.MainMs = _mainMs;
        m.RenderMs = _renderMs;
        m.DrawCalls = Doc(ref _rDrawCalls);
        m.SetPass = Doc(ref _rSetPass);
        m.Batches = Doc(ref _rBatches);
        m.Triangles = Doc(ref _rTriangles);
        m.Vertices = Doc(ref _rVertices);
        m.BoNhoDungByte = Doc(ref _rTongDung);
        m.GcDungByte = Doc(ref _rGcDung);
        m.TextureByte = Doc(ref _rTexture);
        m.GcMoiFrameByte = Doc(ref _rGcMoiFrame);
        m.Audio = Doc(ref _rAudio);

        _mau[_mauDau] = m;
        _mauDau = (_mauDau + 1) % SUC_CHUA_MAU;
        if (_mauSo < SUC_CHUA_MAU) _mauSo++;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DUNG CHUOI HUD (4 lan/giay, KHONG phai moi OnGUI)
    // ─────────────────────────────────────────────────────────────────────────

    private static int FpsMucTieu()
    {
        int t = Application.targetFrameRate;
        return t > 0 ? t : 60;
    }

    private static string MauTheoFps(float fps, int mucTieu)
    {
        if (fps >= mucTieu) return "#4ade80";                 // xanh la: dat muc tieu.
        if (fps >= mucTieu * 2f / 3f) return "#facc15";       // vang: duoi muc tieu.
        return "#f87171";                                     // do: duoi 2/3 muc tieu.
    }

    private void So(float v, string dinhDang)
    {
        _sb.Append(v.ToString(dinhDang, CultureInfo.InvariantCulture));
    }

    /// <summary>Ghi mot bo dem dang so nguyen, hoac "n/a" neu khong co.</summary>
    private void SoNguyen(long v)
    {
        if (v < 0) _sb.Append("n/a");
        else _sb.Append(v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Ghi mot bo dem dang byte thanh MB, hoac "n/a".</summary>
    private void Mb(long byteSo)
    {
        if (byteSo < 0) { _sb.Append("n/a"); return; }
        So(byteSo / (1024f * 1024f), "F1");
        _sb.Append(" MB");
    }

    private void Ms(float ms)
    {
        if (ms < 0f) { _sb.Append("n/a"); return; }
        So(ms, "F2");
        _sb.Append(" ms");
    }

    private void DungChuoiHud()
    {
        int mucTieu = FpsMucTieu();

        _sb.Length = 0;

        // ── Dong 1: FPS (co mau) ──
        _sb.Append("<b><color=").Append(MauTheoFps(_fpsHienTai, mucTieu)).Append(">FPS ");
        So(_fpsHienTai, "F1");
        _sb.Append("</color></b>   1% low <color=").Append(MauTheoFps(_fpsMotPhanTram, mucTieu)).Append('>');
        So(_fpsMotPhanTram, "F1");
        _sb.Append("</color>   tb5s <color=").Append(MauTheoFps(_fpsTrungBinh5s, mucTieu)).Append('>');
        So(_fpsTrungBinh5s, "F1");
        _sb.Append("</color>   (muc tieu ").Append(mucTieu.ToString(CultureInfo.InvariantCulture)).Append(")\n");

        // ── Dong 2: thoi gian frame ──
        _sb.Append("Frame "); Ms(_frameMs);
        _sb.Append("   CPU main "); Ms(_mainMs);
        _sb.Append("   render "); Ms(_renderMs);
        _sb.Append('\n');

        // ── Dong 3: ve ──
        _sb.Append("Draw "); SoNguyen(Doc(ref _rDrawCalls));
        _sb.Append("   SetPass "); SoNguyen(Doc(ref _rSetPass));
        _sb.Append("   Batches "); SoNguyen(Doc(ref _rBatches));
        _sb.Append('\n');

        _sb.Append("Tris "); SoNguyen(Doc(ref _rTriangles));
        _sb.Append("   Verts "); SoNguyen(Doc(ref _rVertices));
        _sb.Append('\n');

        // ── Dong 4: bo nho ──
        _sb.Append("RAM dung "); Mb(Doc(ref _rTongDung));
        _sb.Append(" / dat truoc "); Mb(Doc(ref _rTongDatTruoc));
        _sb.Append('\n');

        _sb.Append("GC heap "); Mb(Doc(ref _rGcDung));
        _sb.Append("   Texture "); Mb(Doc(ref _rTexture));
        _sb.Append('\n');

        // ── Dong 5: GC MOI FRAME — chi so quan trong nhat de truy "giat khuc" ──
        long gcFrame = Doc(ref _rGcMoiFrame);
        _sb.Append("<b>GC/frame </b><color=");
        if (gcFrame < 0) _sb.Append("#9ca3af");
        else if (gcFrame == 0) _sb.Append("#4ade80");        // xanh la: 0 byte = hoan hao.
        else if (gcFrame < 1024) _sb.Append("#facc15");
        else _sb.Append("#f87171");
        _sb.Append('>');
        if (gcFrame < 0) _sb.Append("n/a");
        else { _sb.Append(gcFrame.ToString(CultureInfo.InvariantCulture)); _sb.Append(" B"); }
        _sb.Append("</color>");

        long audio = Doc(ref _rAudio);
        _sb.Append("   Audio "); SoNguyen(audio);
        _sb.Append('\n');

        // ── Dong 6: XAC NHAN CAU HINH 60FPS DA AN TREN MAY THAT ──
        // Day la dong quan trong nhat cho bug da biet: QualitySettings.asset khai
        // tier Android voi vSyncCount: 1 => Application.targetFrameRate bi BO QUA
        // va tran 60fps chet am tham. vSync phai la 0.
        int vsync = QualitySettings.vSyncCount;
        _sb.Append(Screen.width.ToString(CultureInfo.InvariantCulture)).Append('x')
           .Append(Screen.height.ToString(CultureInfo.InvariantCulture));
        _sb.Append("  dpi "); So(Screen.dpi, "F0");
        _sb.Append("  target ").Append(Application.targetFrameRate.ToString(CultureInfo.InvariantCulture));
        _sb.Append("  vSync <color=").Append(vsync == 0 ? "#4ade80" : "#f87171").Append('>')
           .Append(vsync.ToString(CultureInfo.InvariantCulture)).Append("</color>");
        _sb.Append("  Q ").Append(TenQualityHienTai());
        _sb.Append('\n');

        _sb.Append("<color=#9ca3af>4 ngon = bat/tat | 3 cham goc tren trai = xuat CSV | F3 / F4</color>");

        _chuoiHud = _sb.ToString();
        _sb.Length = 0;
    }

    private string TenQualityHienTai()
    {
        int i = QualitySettings.GetQualityLevel();
        if (_tenQuality != null && i >= 0 && i < _tenQuality.Length) return _tenQuality[i];
        return i.ToString(CultureInfo.InvariantCulture);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VE — CHI lam viec o EventType.Repaint
    // OnGUI co the chay VAI LAN moi frame (Layout, Repaint, cac su kien input).
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!_dangHien) return;
        if (Event.current == null || Event.current.type != EventType.Repaint) return;

        ChuanBiKieu();

        GUI.depth = -1000;                   // ve de len tren moi thu.
        GUI.Label(_khung, _chuoiHud, _kieu);
    }

    private void ChuanBiKieu()
    {
        bool doiKichThuoc = _wCu != Screen.width || _hCu != Screen.height;
        if (_kieu != null && !doiKichThuoc) return;

        _wCu = Screen.width;
        _hCu = Screen.height;

        if (_nen == null)
        {
            _nen = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _nen.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
            _nen.Apply();
            _nen.hideFlags = HideFlags.HideAndDontSave;
        }

        // Co chu theo DPI de doc duoc tren dien thoai. Screen.dpi co the tra 0
        // tren mot so may / trong Editor => lay 96 lam mac dinh.
        float dpi = Screen.dpi;
        if (dpi < 1f) dpi = 96f;

        int coChu = Mathf.RoundToInt(13f * (dpi / 96f));
        coChu = Mathf.Clamp(coChu, 12, 44);

        if (_kieu == null) _kieu = new GUIStyle();

        _kieu.normal.background = _nen;
        _kieu.normal.textColor = Color.white;
        _kieu.richText = true;
        _kieu.wordWrap = false;
        _kieu.alignment = TextAnchor.UpperLeft;
        _kieu.fontSize = coChu;
        _kieu.padding = new RectOffset(10, 10, 8, 8);

        float rong = Mathf.Min(Screen.width - 16f, coChu * 30f);
        float cao = coChu * 14f;
        _khung = new Rect(8f, 8f, rong, cao);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // XUAT CSV
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi ~60 giay mau gan nhat ra <c>Application.persistentDataPath/perf_log.csv</c>.
    /// MOI thao tac file boc try/catch — cong cu do KHONG bao gio duoc phep lam sap game.
    /// Moi so deu dinh dang bang <see cref="CultureInfo.InvariantCulture"/> de file mo
    /// duoc tren may nao cung dung (locale VN dung dau phay lam dau thap phan => vo CSV).
    /// </summary>
    private void XuatCsv()
    {
        string duongDan = string.Empty;

        try
        {
            duongDan = Path.Combine(Application.persistentDataPath, TEN_FILE_CSV);

            var ghi = new StringBuilder(4096);
            ghi.Append("unix_seconds,fps,fps_1pct_low,fps_avg_5s,frame_ms,cpu_main_ms,render_ms,")
               .Append("draw_calls,setpass_calls,batches,triangles,vertices,")
               .Append("total_used_bytes,gc_used_bytes,texture_bytes,gc_alloc_per_frame_bytes,audio\n");

            // Doc vong dem theo dung thu tu thoi gian (cu -> moi).
            int batDau = _mauSo < SUC_CHUA_MAU ? 0 : _mauDau;
            for (int i = 0; i < _mauSo; i++)
            {
                Mau m = _mau[(batDau + i) % SUC_CHUA_MAU];

                ghi.Append(m.GiayUnix.ToString(CultureInfo.InvariantCulture)).Append(',');
                ghi.Append(m.Fps.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
                ghi.Append(m.FpsMotPhanTram.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
                ghi.Append(m.FpsTrungBinh5s.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
                ghi.Append(m.FrameMs.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
                GhiO(ghi, m.MainMs);
                GhiO(ghi, m.RenderMs);
                GhiO(ghi, m.DrawCalls);
                GhiO(ghi, m.SetPass);
                GhiO(ghi, m.Batches);
                GhiO(ghi, m.Triangles);
                GhiO(ghi, m.Vertices);
                GhiO(ghi, m.BoNhoDungByte);
                GhiO(ghi, m.GcDungByte);
                GhiO(ghi, m.TextureByte);
                GhiO(ghi, m.GcMoiFrameByte);

                // Cot cuoi: KHONG co dau phay theo sau.
                if (m.Audio >= 0L) ghi.Append(m.Audio.ToString(CultureInfo.InvariantCulture));
                ghi.Append('\n');
            }

            File.WriteAllText(duongDan, ghi.ToString());

            Debug.Log("[PerfHud] DA GHI CSV (" + _mauSo.ToString(CultureInfo.InvariantCulture) +
                      " mau) => " + duongDan);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PerfHud] Ghi CSV that bai (" + duongDan + "): " + e.Message);
        }
    }

    /// <summary>O CSV kieu so thuc: de TRONG neu khong do duoc (thay vi ghi -1 gay hieu nham).</summary>
    private static void GhiO(StringBuilder ghi, float v)
    {
        if (v >= 0f) ghi.Append(v.ToString("F3", CultureInfo.InvariantCulture));
        ghi.Append(',');
    }

    /// <summary>O CSV kieu so nguyen: de TRONG neu khong do duoc.</summary>
    private static void GhiO(StringBuilder ghi, long v)
    {
        if (v >= 0L) ghi.Append(v.ToString(CultureInfo.InvariantCulture));
        ghi.Append(',');
    }

#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
}
