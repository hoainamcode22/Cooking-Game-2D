using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [BOAT-TUT 2026-09-10 — Dev E] DAO DIEN man tutorial "mo khoa ben tau du lich" (level 10).
///
/// VAN DE CU: popup LEN CAP va intro ben tau cung nghe FarmLevelManager.OnLevelChanged nen
/// bung ra CUNG LUC — nguoi choi vua thay popup vua thay camera bay di.
///
/// TRINH TU MOI (Sep chot):
///   1. Popup len cap hien — tutorial ben tau CHUA chay.
///   2. Nguoi choi bam "Nhan" -> popup dong han.
///   3. LUC DO tutorial ben tau moi bat dau.
///   4. Camera zoom vao ben va GIU o do, doi tau.
///   5. Trong suot tutorial tau chay NHANH GAP <see cref="nhanTocDoTau"/> lan; xong tra ve 1x.
///   6. Tau cap ben, khach xuong -> camera THEO khach cho toi khi xep hang xong.
///   7. Xep hang xong moi tra quyen dieu khien cho nguoi choi.
///
/// CACH LAM VIEC — TOAN BO LA "DIEU PHOI", KHONG VIET LAI HE THONG NAO:
///   • Popup: nghe <see cref="LevelUpPopupUI.OnAllClosed"/> (event MOI, them canh code cu),
///     kem POLL <see cref="LevelUpPopupUI.IsActive"/> lam luoi an toan + tran thoi gian.
///   • Intro (hoi thoai + zoom + UnlockDockFree): van do TouristBoatUnlockFlow lo, minh chi
///     hoan no lai (TrihoanIntro) roi goi ChayIntroNgay() dung luc. Co TrihoanIntro duoc
///     GAC O OnEnable va HA O OnDisable, ngay canh cho khoi dong/dung coroutine dao dien —
///     "co dang bat" va "dao dien dang chay" luon la MOT dieu kien.
///   • Toc do tau: BoatDockManager.SetTutorialSpeedMultiplier() — chi chia travel, khong
///     dung toi gap/stagger. PHAI dat TRUOC khi mo ben thi chuyen dau tien moi nhanh that.
///   • Camera: CameraController.CinematicFocus(pos, size, lockInput) goi MOI FRAME khi can
///     BAM THEO khach (CameraController tu SmoothDamp nen theo rat muot). EndCinematic() tra quyen.
///   • Khoa input: FarmInputLock.RegisterPopupOpen/Close (dung cap, dung 1 lan) + co
///     lockInput cua CinematicFocus chan pan/zoom.
///
/// AN TOAN (nguyen tac: KHONG BAO GIO khoa chet nguoi choi ngoai game cua ho):
///   • MOI cho doi deu co TRAN THOI GIAN. Het tran -> log warning, di tiep buoc sau.
///   • <see cref="DonDep"/> tra toc do tau ve 1x, tra camera, mo input — va duoc goi ca trong
///     OnDisable/OnDestroy, nen doi scene giua chung cung khong de lai trang thai hong.
///   • Moi tham chieu deu kiem null; thieu component nao thi bo qua buoc do, khong nem loi.
///   • Toan bo dung thoi gian UNSCALED (popup co the dat Time.timeScale = 0).
///
/// DAT O DAU: gan len chinh object dang co TouristBoatUnlockFlow (thuong la "BoatSystem").
/// </summary>
[DisallowMultipleComponent]
public class BoatTutorialCinematic : MonoBehaviour
{
    // =========================================================================
    //  Inspector
    // =========================================================================

    [Header("Bat / Tat")]
    [Tooltip("CONG TAC TONG. Bo tick = tat han man cinematic, game quay ve luong cu " +
             "(TouristBoatUnlockFlow tu chay intro ngay khi len cap).")]
    [SerializeField] private bool batCinematic = true;

    [Tooltip("Ben duoc mo trong tutorial (0 = Ben 01, ben mien phi cua intro).")]
    [SerializeField] private int benTutorial = 0;

    [Header("Toc do tau trong tutorial")]
    [Tooltip("Tau chay nhanh gap bao nhieu lan so voi binh thuong trong luc tutorial " +
             "(3 = nhanh gap 3). Xong tutorial tu tra ve 1x. De 1 = khong tang toc.")]
    [SerializeField] private float nhanTocDoTau = 3f;

    [Header("Camera")]
    [Tooltip("OrthoSize khi camera zoom vao ben (thang that cua map: mac dinh 750, " +
             "intro cu dung 460). So NHO = zoom GAN hon.")]
    [SerializeField] private float zoomTaiBen = 460f;

    [Tooltip("OrthoSize khi camera bam theo khach di bo (thuong keo ra rong hon ben " +
             "mot chut de thay ca doan khach).")]
    [SerializeField] private float zoomTheoKhach = 520f;

    [Tooltip("Giay lia camera tu vi tri hien tai vao ben (chi dung khi khong co " +
             "TouristBoatUnlockFlow lo phan nay).")]
    [SerializeField] private float giayLiaVaoBen = 1.2f;

    [Header("Tran thoi gian (giay) — luoi an toan chong ket")]
    [Tooltip("Toi da cho popup len cap dong. Het tran van chay tiep tutorial.")]
    [SerializeField] private float tranChoPopup = 120f;

    [Tooltip("Toi da cho phan intro (hoi thoai + zoom + mo ben) cua TouristBoatUnlockFlow chay xong.")]
    [SerializeField] private float tranChoIntro = 90f;

    [Tooltip("Toi da GIU camera o ben cho tau cap ben. Het tran -> bo qua, tra camera.")]
    [SerializeField] private float tranChoTauCapBen = 90f;

    [Tooltip("Toi da bam theo khach cho toi khi ho xep hang xong. Het tran -> tra camera.")]
    [SerializeField] private float tranTheoKhach = 60f;

    [Tooltip("Sau khi khach da xep hang xong, nan na them bay nhieu giay roi moi tra camera " +
             "(cho nguoi choi kip nhin thay hang khach).")]
    [SerializeField] private float nanNaSauKhiXepHang = 1.2f;

    [Header("Nhip poll")]
    [Tooltip("Bao lau kiem tra dieu kien 1 lan (giay, unscaled). Thua vua du nhay, nhe CPU.")]
    [SerializeField] private float nhipPoll = 0.1f;

    [Tooltip("Hang khach phai ON DINH (khong doi so luong, ai cung dung cho) bay nhieu giay " +
             "moi tinh la 'xep hang xong' — tranh chot som khi khach cuoi con dang di.")]
    [SerializeField] private float giayHangOnDinh = 1.0f;

    [Header("Nhat ky")]
    [Tooltip("In log tung buoc ra Console (bat khi QA, tat khi build phat hanh).")]
    [SerializeField] private bool inLog = true;

    // =========================================================================
    //  Runtime
    // =========================================================================

    private Coroutine       _dao;              // coroutine dao dien dang chay
    private bool            _dangCinematic;    // dang trong man cinematic
    private bool            _daKhoaInput;      // da goi RegisterPopupOpen chua (chong lech cap)
    private bool            _daDoiTocDoTau;    // da doi he so toc do tau chua
    private bool            _daGiuCamera;      // da chiem quyen camera chua
    private bool            _daNgheSuKienPopup;
    private bool            _dangDonDep;       // chong de quy / chong chay chong khi don dep
    private bool            _daXongHan;        // [FIX A] da chay xong / khong con viec -> khong gac co, khong chay lai

    private CameraController     _cam;
    private TouristBoatUnlockFlow _flow;
    private Vector3         _camViTriGoc;
    private float           _camZoomGoc;

    private bool            _popupBaoDaDong;   // co do event OnAllClosed bat len

    // Bo dem tai su dung khi tinh tam khach — khong cap phat moi trong vong lap.
    private readonly List<Vector3> _demViTriKhach = new List<Vector3>();

    // =========================================================================
    //  Vong doi
    // =========================================================================

    // [FIX A] KHONG CON Awake().
    //
    // Ban truoc dat TrihoanIntro = true trong Awake. Hai canh cua lam co do ket true
    // ca session (=> TryStartIntro thoat som mai mai => ben tau khong bao gio mo):
    //   1. Component duoc AUTHOR SAN O TRANG THAI TAT tren mot GameObject dang bat:
    //      Unity VAN chay Awake, nhung OnEnable/Start/OnDisable thi khong bao gio chay
    //      -> co bat len ma khong co ai chay, khong co ai ha xuong.
    //   2. Tat roi bat lai: OnDisable ha co va null _dao, OnEnable dung lai co, nhung
    //      Start KHONG chay lai -> lai co co ma khong co dao dien.
    //
    // Nay "co dang bat" va "dao dien dang chay" duoc dat CHUNG MOT CHO (OnEnable) va
    // ha CHUNG MOT CHO (OnDisable), nen theo cau truc chung khong the lech nhau nua.
    //
    // Bo Awake KHONG lam mat thoi diem: moi Awake/OnEnable cua scene deu xong TRUOC bat
    // ky Start nao, ma TryStartIntro chi den duoc tu BootRoutine — coroutine do
    // TouristBoatUnlockFlow.Start() khoi dong. Nen co van luon duoc gac kip.
    private void OnEnable()
    {
        if (!batCinematic) return;

        // Da chay xong han (hoac da xac dinh khong co gi de lam) -> KHONG gac co nua va
        // khong chay lai. Neu van gac co o day thi lai chinh la loi "co ket true".
        if (_daXongHan) return;

        TouristBoatUnlockFlow.TrihoanIntro = true;

        if (!_daNgheSuKienPopup)
        {
            LevelUpPopupUI.OnAllClosed += HandlePopupDaDong;
            _daNgheSuKienPopup = true;
        }

        // Cung mot dieu kien voi co ben tren, dat lien ke — bat lai object thi dao dien
        // cung chay lai. StartCoroutine tu OnEnable la hop le (component da enabled).
        if (_dao == null)
            _dao = StartCoroutine(DaoDienRoutine());
    }

    private void OnDisable()
    {
        if (_daNgheSuKienPopup)
        {
            LevelUpPopupUI.OnAllClosed -= HandlePopupDaDong;
            _daNgheSuKienPopup = false;
        }

        if (_dao != null) { StopCoroutine(_dao); _dao = null; }

        // [FIX BLOCKER 2] Nha co hoan intro NGAY khi bi tat: minh khong con chay nua thi
        // phai tra quyen mo intro lai cho TouristBoatUnlockFlow, khong duoc giu con tin.
        TouristBoatUnlockFlow.TrihoanIntro = false;

        // Doi scene / tat object giua chung: tuyet doi khong duoc de lai tau chay sai toc do,
        // camera bi khoa hay input bi chan.
        DonDep("OnDisable");
    }

    private void OnDestroy()
    {
        DonDep("OnDestroy");
        TouristBoatUnlockFlow.TrihoanIntro = false;
    }

    /// <summary>Event tu LevelUpPopupUI: hang doi popup da rong han (nguoi choi bam "Nhan").</summary>
    private void HandlePopupDaDong() => _popupBaoDaDong = true;

    /// <summary>
    /// [FIX A] Chot HAN: khong con viec gi nua trong session nay. Ha co hoan intro VA
    /// danh dau de OnEnable sau nay khong gac co len lai (bat/tat object khong lam
    /// khoa lai tien do). CO Y khong goi trong finally cua coroutine: bi StopCoroutine
    /// giua chung thi phai con chay lai duoc khi object bat lai.
    /// </summary>
    private void KetThucHan()
    {
        _daXongHan = true;
        TouristBoatUnlockFlow.TrihoanIntro = false;
    }

    // =========================================================================
    //  Dao dien chinh
    // =========================================================================

    private IEnumerator DaoDienRoutine()
    {
        // ── 0. Doi he thong san sang ────────────────────────────────────────
        float doi = 0f;
        while ((BoatDockManager.Instance == null || !BoatDockManager.Instance.IsReady ||
                FarmLevelManager.Instance == null) && doi < 15f)
        {
            doi += Time.unscaledDeltaTime;
            yield return null;
        }

        var mgr = BoatDockManager.Instance;
        if (mgr == null || !mgr.IsReady || mgr.Config == null || FarmLevelManager.Instance == null)
        {
            Debug.LogWarning("[BoatTutCinematic] He thong ben tau/level chua san sang sau 15s — " +
                             "tra quyen intro ve TouristBoatUnlockFlow (luong cu).");
            KetThucHan();
            yield break;
        }

        if (mgr.IsIntroDone)
        {
            Log("Intro ben tau da chay tu truoc (save cu) — cinematic khong lam gi.");
            KetThucHan();
            yield break;
        }

        _flow = GetComponent<TouristBoatUnlockFlow>();
        if (_flow == null) _flow = FindFirstObjectByType<TouristBoatUnlockFlow>();

        int capMo = mgr.Config.unlockLevel;

        // ── 1. Doi nguoi choi dat level mo khoa ─────────────────────────────
        // KHONG dat tran o day: day la dieu kien kich hoat, khong phai buoc cho trong cutscene.
        while (FarmLevelManager.Instance != null && !FarmLevelManager.Instance.HasReached(capMo))
            yield return new WaitForSecondsRealtime(nhipPoll);

        Log($"Da dat level {capMo} — cho popup len cap dong roi moi mo man.");

        // ── 2. Doi popup len cap dong HAN (bam "Nhan") ──────────────────────
        yield return ChoPopupLenCapDong();

        // ── 3. Bat dau cinematic ────────────────────────────────────────────
        // [FIX HEAVY 4] Tu day tro di minh DA CHIEM input + camera + toc do tau. Unity
        // GIET IM LANG mot coroutine nem exception (khong co OnDisable/OnDestroy nao chay
        // vi object van song va van enabled) -> se de lai input khoa cung, camera khoa
        // cung, tau chay 3x vinh vien. try/finally bao dam DonDep() luon chay: khi hoan
        // tat, khi yield break giua chung, khi StopCoroutine, VA khi nem exception.
        // Luu y C#: yield return duoc phep trong try CO finally (chi cam khi co catch).
        _dangCinematic = true;
        bool xongTron = false;

        try
        {
            KhoaInput();
            ApTocDoTau(nhanTocDoTau);   // PHAI truoc UnlockDockFree thi chuyen dau moi nhanh

            LuuCameraGoc();

            // ── 4. Hoi thoai + zoom + mo ben: giao lai cho UnlockFlow (giu camera) ──
            if (_flow != null)
            {
                _flow.GiuCameraSauIntro = true;

                // CO Y chay tren MonoBehaviour CUA FLOW (khong phai cua minh): neu object nay
                // bi tat giua chung, intro van tu chay het va tu don overlay chan UI cua no —
                // nguoi choi khong bao gio bi ket sau tam chan mo.
                _flow.StartCoroutine(_flow.ChayIntroNgay());

                float ti = 0f;
                // Doi flow bat dau (mot vai frame) roi doi no chay xong, co tran.
                while (!_flow.IntroDangChay && !_flow.IntroDaXong && ti < 2f)
                {
                    ti += Time.unscaledDeltaTime;
                    yield return null;
                }
                ti = 0f;
                while (_flow.IntroDangChay && ti < tranChoIntro)
                {
                    ti += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (_flow.IntroDangChay)
                {
                    // [FIX HEAVY 3] TRUOC DAY chay tiep — SAI NANG. Hoi thoai intro cho tap
                    // vo han (ShowLineRoutine: autoClose = Infinity o moi cau khong phai cau
                    // cuoi), nen "het tran 90s" gan nhu chi co nghia la NGUOI CHOI DAT MAY
                    // XUONG, khong phai he thong hong. Chay tiep se giat camera toi mot ben
                    // CHUA MO, dang co bang hoi thoai song de len tren, roi tra quyen dieu
                    // khien vao giua doan hoi thoai. Dung cach la BO CUOC sach se: finally
                    // se tra camera/input/toc do tau, con intro cu tu chay not tren
                    // MonoBehaviour cua no va tu mo ben nhu luong cu.
                    Debug.LogWarning($"[BoatTutCinematic] Het tran {tranChoIntro:0}s ma intro (hoi thoai) " +
                                     "chua xong (nguoi choi chua bam het hoi thoai?) — HUY phan cinematic, " +
                                     "de TouristBoatUnlockFlow tu chay not theo luong cu.");
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[BoatTutCinematic] Khong tim thay TouristBoatUnlockFlow — " +
                                 "tu lia camera va mo ben (bo qua phan hoi thoai).");
                yield return LiaCameraVaoBen(mgr);
                mgr.UnlockDockFree(benTutorial);
                mgr.MarkIntroDone();
            }

            // ── 5. GIU camera o ben, doi tau cap ben ────────────────────────────
            GiuCameraTaiBen(mgr, zoomTaiBen);
            bool tauDaCap = false;
            yield return ChoTauCapBen(mgr, ok => tauDaCap = ok);

            if (!tauDaCap)
            {
                Debug.LogWarning($"[BoatTutCinematic] Het tran {tranChoTauCapBen:0}s ma tau chua cap ben — " +
                                 "ket thuc cinematic som de khong ket nguoi choi.");
                yield break;   // finally se don dep
            }

            // ── 6. Bam theo khach toi khi xep hang xong ─────────────────────────
            yield return TheoKhachXepHang();

            if (nanNaSauKhiXepHang > 0f)
                yield return new WaitForSecondsRealtime(nanNaSauKhiXepHang);

            xongTron = true;
            KetThucHan();   // [FIX A] chay tron ven -> nha co han, khong chay lai
            Log("Khach da xep hang xong — tra quyen dieu khien cho nguoi choi.");
        }
        finally
        {
            // ── 7. Tra quyen cho nguoi choi (LUON LUON chay) ────────────────────
            DonDep(xongTron ? "hoan tat" : "ket thuc som / bi ngat");
        }
    }

    // =========================================================================
    //  Buoc 2 — cho popup len cap
    // =========================================================================

    /// <summary>
    /// Cho popup len cap dong han. Uu tien EVENT (OnAllClosed), poll IsActive lam luoi
    /// an toan (event co the bi bo lo neu popup dong truoc khi minh kip nghe).
    /// </summary>
    private IEnumerator ChoPopupLenCapDong()
    {
        // Popup chua kip bat: cho mot nhip ngan de no co co hoi hien ra, neu khong minh
        // se "thay khong co popup" va lao vao cutscene ngay giua luc popup dang bung.
        float chomo = 0f;
        while (!LevelUpPopupUI.IsActive && chomo < 1.5f)
        {
            chomo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!LevelUpPopupUI.IsActive && !_popupBaoDaDong)
        {
            Log("Khong thay popup len cap nao mo — vao thang cinematic.");
            yield break;
        }

        _popupBaoDaDong = false;
        float t = 0f;
        while (LevelUpPopupUI.IsActive && !_popupBaoDaDong && t < tranChoPopup)
        {
            // [FIX BLOCKER 1] Vong nay ngu nhipPoll (0.1s) moi lan lap, KHONG phai 1 frame.
            // Cong Time.unscaledDeltaTime (~0.016s) se lam tran 120s keo dai thanh ~720s.
            // Phai cong dung luong thoi gian vua ngu. Lay max de framerate tut cung dung.
            t += Mathf.Max(nhipPoll, Time.unscaledDeltaTime);
            yield return new WaitForSecondsRealtime(nhipPoll);
        }

        if (LevelUpPopupUI.IsActive)
        {
            Debug.LogWarning($"[BoatTutCinematic] Het tran {tranChoPopup:0}s ma popup len cap van mo — " +
                             "chay tiep de tutorial khong ket vinh vien.");
        }
        else
        {
            Log($"Popup len cap da dong sau {t:0.0}s — mo man tutorial ben tau.");
        }

        // Nhip tho cho anim dong cua popup chay not.
        yield return new WaitForSecondsRealtime(0.25f);

        // [FIX LIGHT 8] Len 2 cap mot luc -> popup thu 2 duoc day ra ngay trong 0.25s vua
        // roi. Neu khong kiem lai thi cinematic van dam vao giua popup — dung CAI BUG ma
        // file nay sinh ra de sua. Lap lai cho toi khi san khau that su trong, van trong
        // tran tong tranChoPopup (t da tich luy o vong tren).
        while (LevelUpPopupUI.IsActive && t < tranChoPopup)
        {
            Log("Co them popup len cap vua mo (len nhieu cap) — cho tiep.");
            _popupBaoDaDong = false;
            while (LevelUpPopupUI.IsActive && !_popupBaoDaDong && t < tranChoPopup)
            {
                t += Mathf.Max(nhipPoll, Time.unscaledDeltaTime);
                yield return new WaitForSecondsRealtime(nhipPoll);
            }
            yield return new WaitForSecondsRealtime(0.25f);
            t += 0.25f;   // [FIX C] nhip tho cung phai tinh vao tran, khong thi popup
                          // mo di mo lai co the doi them ~300s ngoai con so Inspector
        }
    }

    // =========================================================================
    //  Buoc 5 — cho tau cap ben
    // =========================================================================

    private IEnumerator ChoTauCapBen(BoatDockManager mgr, System.Action<bool> ketQua)
    {
        float t = 0f;
        while (t < tranChoTauCapBen)
        {
            if (mgr == null) break;

            BoatPhaseInfo info;
            if (mgr.TryGetPhaseInfo(benTutorial, out info) && info.State == BoatState.Docked)
            {
                Log($"Tau da cap ben sau {t:0.0}s.");
                ketQua?.Invoke(true);
                yield break;
            }

            // Giu camera bam ben suot thoi gian doi (CinematicFocus phai goi lai deu
            // vi noi khac cung co the ghi targetPosition).
            GiuCameraTaiBen(mgr, zoomTaiBen);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ketQua?.Invoke(false);
    }

    // =========================================================================
    //  Buoc 6 — bam theo khach
    // =========================================================================

    /// <summary>
    /// Camera bam theo TAM cua doan khach cho toi khi tat ca deu dung cho (WaitingServe)
    /// va so luong hang khong doi trong <see cref="giayHangOnDinh"/> giay.
    /// </summary>
    private IEnumerator TheoKhachXepHang()
    {
        TouristQueue hang = LayHangCho();
        if (hang == null)
        {
            Debug.LogWarning("[BoatTutCinematic] Khong tim thay TouristQueue — bo qua doan bam theo khach.");
            yield break;
        }

        float t         = 0f;
        float onDinh    = 0f;
        int   soTruoc   = -1;

        while (t < tranTheoKhach)
        {
            Vector3 tam;
            int soKhach = TinhTamKhach(hang, out tam);

            if (soKhach > 0)
                GiuCameraTaiDiem(tam, zoomTheoKhach);

            // "Xep hang xong" = co it nhat 1 khach, tat ca deu WaitingServe, va so luong
            // khong doi trong giayHangOnDinh giay (khach cuoi con dang di thi chua tinh).
            bool tatCaDungCho = soKhach > 0 && TatCaDaVaoCho(hang);
            if (tatCaDungCho && soKhach == soTruoc)
            {
                onDinh += Time.unscaledDeltaTime;
                if (onDinh >= giayHangOnDinh)
                {
                    Log($"{soKhach} khach da xep hang on dinh sau {t:0.0}s.");
                    yield break;
                }
            }
            else
            {
                onDinh = 0f;
            }

            soTruoc = soKhach;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning($"[BoatTutCinematic] Het tran {tranTheoKhach:0}s ma khach chua xep hang xong — " +
                         "tra camera cho nguoi choi.");
    }

    /// <summary>Tam (trung binh vi tri) cua doan khach; tra ve so khach dem duoc.</summary>
    private int TinhTamKhach(TouristQueue hang, out Vector3 tam)
    {
        tam = Vector3.zero;
        _demViTriKhach.Clear();

        IReadOnlyList<TouristAgent> ds = hang != null ? hang.Agents : null;
        if (ds == null) return 0;

        for (int i = 0; i < ds.Count; i++)
        {
            TouristAgent a = ds[i];
            if (a == null) continue;
            _demViTriKhach.Add(a.transform.position);
        }

        if (_demViTriKhach.Count == 0) return 0;

        Vector3 tong = Vector3.zero;
        for (int i = 0; i < _demViTriKhach.Count; i++) tong += _demViTriKhach[i];
        tam = tong / _demViTriKhach.Count;
        return _demViTriKhach.Count;
    }

    /// <summary>Moi khach trong hang deu da vao cho dung (WaitingServe) chua.</summary>
    private static bool TatCaDaVaoCho(TouristQueue hang)
    {
        IReadOnlyList<TouristAgent> ds = hang != null ? hang.Agents : null;
        if (ds == null || ds.Count == 0) return false;

        for (int i = 0; i < ds.Count; i++)
        {
            TouristAgent a = ds[i];
            if (a == null) continue;
            if (a.State != TouristAgent.AgentState.WaitingServe) return false;
        }
        return true;
    }

    private static TouristQueue LayHangCho()
    {
        var vm = TouristVisitorManager.Instance;
        if (vm != null && vm.Queue != null) return vm.Queue;
        return FindFirstObjectByType<TouristQueue>();
    }

    // =========================================================================
    //  Camera
    // =========================================================================

    private CameraController LayCam()
    {
        if (_cam == null) _cam = FindFirstObjectByType<CameraController>();
        return _cam;
    }

    private void LuuCameraGoc()
    {
        var cc = LayCam();
        if (cc == null || _daGiuCamera) return;

        _camViTriGoc = cc.CurrentPosition;
        _camZoomGoc  = cc.CurrentSize;
        _daGiuCamera = true;
    }

    private void GiuCameraTaiBen(BoatDockManager mgr, float zoom)
    {
        if (mgr == null) return;
        Transform berth = mgr.GetDockBerth(benTutorial);
        if (berth == null) return;
        GiuCameraTaiDiem(berth.position, zoom);
    }

    private void GiuCameraTaiDiem(Vector3 diem, float zoom)
    {
        var cc = LayCam();
        if (cc == null) return;

        LuuCameraGoc();
        // lockInput = true: CameraController tu chan pan/zoom cua nguoi choi toi EndCinematic().
        cc.CinematicFocus(diem, zoom, lockInput: true);
    }

    private IEnumerator LiaCameraVaoBen(BoatDockManager mgr)
    {
        GiuCameraTaiBen(mgr, zoomTaiBen);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, giayLiaVaoBen));
    }

    private void TraCameraVe()
    {
        // [FIX LIGHT 9] CHI dung toi camera khi CHINH MINH dang giu no. DonDep chay tu
        // ca OnDisable lan OnDestroy (va tu finally), nen lan chay thu hai truoc day se
        // goi EndCinematic() vo dieu kien va CAT NGANG mot cinematic cua he thong KHAC
        // vua bat dau (vd intro cua TouristBoatUnlockFlow, hay mot buoc tutorial khac).
        if (!_daGiuCamera) return;

        var cc = LayCam();
        // [FIX B] Chua tra duoc camera thi CHUA duoc nha quyen so huu. Ban truoc ha
        // _daGiuCamera TRUOC cai null-check nay: neu CameraController tam thoi chua tra
        // ra duoc (dang doi scene, vua bi destroy roi tao lai...), quyen so huu bi vut di
        // ma EndCinematic() chua he duoc goi, va guard o tren chan luon moi lan thu sau
        // -> camera khoa vinh vien.
        if (cc == null) return;

        _daGiuCamera = false;

        // Lia ve cho cu roi mo khoa — CinematicFocus voi lockInput:false de
        // CameraController van SmoothDamp ve nhung nguoi choi da co quyen ngay.
        cc.CinematicFocus(_camViTriGoc, _camZoomGoc, lockInput: false);
        cc.EndCinematic();
    }

    // =========================================================================
    //  Toc do tau / khoa input / don dep
    // =========================================================================

    private void ApTocDoTau(float heSo)
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null) return;

        // [FIX HEAVY 5] Mathf.Clamp(NaN, a, b) tra ve NaN chu KHONG kep ve a. NaN chay
        // tiep vao EffectiveTravelSeconds -> SecondsToTicks -> luu vao PlayerPrefs thanh
        // lich ben HONG vinh vien. So sanh "!(x > 0f)" bat duoc ca NaN lan so am/0.
        if (!(heSo > 0f))
        {
            Debug.LogWarning($"[BoatTutCinematic] nhanTocDoTau khong hop le ({heSo}) — dung 1x.");
            heSo = 1f;
        }

        float m = Mathf.Clamp(heSo, 0.05f, 50f);
        mgr.SetTutorialSpeedMultiplier(m);
        _daDoiTocDoTau = true;
        Log($"Tang toc tau trong tutorial: {m:0.##}x.");
    }

    private void TraTocDoTauVeBinhThuong()
    {
        if (!_daDoiTocDoTau) return;
        _daDoiTocDoTau = false;

        var mgr = BoatDockManager.Instance;
        if (mgr != null) mgr.SetTutorialSpeedMultiplier(1f);
    }

    private void KhoaInput()
    {
        if (_daKhoaInput) return;
        _daKhoaInput = true;
        FarmInputLock.RegisterPopupOpen();
    }

    private void MoInput()
    {
        if (!_daKhoaInput) return;
        _daKhoaInput = false;
        FarmInputLock.RegisterPopupClose();
    }

    /// <summary>
    /// TRA LAI MOI THU. An toan khi goi nhieu lan (moi buoc deu co co rieng).
    /// Duoc goi ca khi hoan tat, khi het tran, va trong OnDisable/OnDestroy.
    /// </summary>
    private void DonDep(string lyDo)
    {
        // [FIX HEAVY 4] BAT BUOC idempotent: ham nay chay tu finally cua coroutine, tu
        // OnDisable VA tu OnDestroy — thuong la 2-3 lan lien tiep cho cung mot lan ket
        // thuc. Moi buoc ben trong deu tu gac bang co rieng (_daDoiTocDoTau, _daKhoaInput,
        // _daGiuCamera) nen goi lai bao nhieu lan cung khong tra thua input, khong cat
        // nham cinematic cua he thong khac.
        if (_dangDonDep) return;
        _dangDonDep = true;
        try
        {
            TraTocDoTauVeBinhThuong();
            MoInput();

            if (_flow != null) _flow.GiuCameraSauIntro = false;

            TraCameraVe();

            if (_dangCinematic)
            {
                _dangCinematic = false;
                Log($"Ket thuc cinematic ({lyDo}) — toc do tau, camera va input da tra ve binh thuong.");
            }
        }
        finally
        {
            _dangDonDep = false;
        }
    }

    private void Log(string s)
    {
        if (inLog) Debug.Log($"[BoatTutCinematic] {s}");
    }
}
