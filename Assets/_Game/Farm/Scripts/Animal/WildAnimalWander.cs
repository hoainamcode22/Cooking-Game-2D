using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ============================================================================
/// WildAnimalWander — THU HOANG DI LAI / GAM CO QUANH CHO NO DUOC DAT
/// ============================================================================
///
/// VI SAO CO FILE NAY
/// ------------------
/// Trong SCN_Farm co 22 con thu duoc tha TU DO ra ban do (Prefab_Cow (1..8),
/// Prefab_Cow_Brown (1..4), Prefab_Chicken (1..4), Prefab_piggy (1..2) — tat ca
/// deu nam o goc Hierarchy, KHONG phai con cua Pen_01..04). Chung KHONG mang bat
/// ky script dieu khien nao nen dung im, chi chay clip Idle cua Animator.
///
/// LivestockAI (script cua thu TRONG CHUONG) KHONG dung lai duoc cho chung:
///   • No chay bang transform.LOCAL position + localBounds cua san chuong.
///   • No di tim PenMiniPanelUI o cay cha va doi trang thai chuong
///     (Idle / Processing / Ready) de quyet dinh di nhanh hay dung yen.
///   • No mo popup chuong khi bam chuot (OnMouseDown).
/// Thu hoang khong co chuong, khong co popup => can mot script rieng, doc lap.
///
/// TRIET LY: TU CHAY 100%
/// ----------------------
/// Keo script nay vao MOT GameObject bat ky co SpriteRenderer la xong, khong can
/// gan them gi:
///   • Tam di lai = vi tri luc Start (khong can Transform moc).
///   • Animator: TU DO xem controller co tham so nao (Speed / IsMoving / DirX /
///     DirY / IsEating) roi chi set dung nhung tham so CO THAT. Khong co Animator
///     thi van di chuyen binh thuong, khong nem loi.
///   • Y-sort: tu them SortingGroup neu thieu, dung dung quy uoc cua farm
///     (TouristSortingLayers + order = base - Y*heSo, co kep bien).
///
/// PHAM VI DI LAI LA HINH E-LIP, KHONG PHAI HINH TRON
/// --------------------------------------------------
/// Mot o iso cua du an la 300 x 150 world unit (xem IsoGridSizeTool.cellW = 300).
/// Neu cho thu di trong hinh TRON ban kinh R thi tren man hinh no se lang ra xa
/// theo truc Y gap doi so voi cam nhan cua nguoi choi. Nen vung di lai bi det
/// theo <see cref="tiLeDetIso"/> = 0.5 cho khop ti le 2:1 cua luoi iso.
/// Mac dinh banKinhDiLai = 300 (khoang 1 o theo be ngang, 2 o theo chieu sau) —
/// KHONG dat 2.0 nhu game toa do nho, o thang do thu se dung yen tai cho.
///
/// GIA / RE
/// --------
/// Vong lap la mot state-machine bang bien dem trong Update — KHONG coroutine
/// (moi `new WaitForSeconds` la mot lan cap phat), KHONG FindObjectsOfType,
/// KHONG cap phat moi frame. animator.parameters chi doc DUNG MOT LAN o Awake.
/// Chay tot 10-20 con tren WebGL.
///
/// LECH PHA
/// --------
/// Ca dan tha cung mot frame se buoc cung nhip neu tat ca cung bat dau dem tu 0.
/// Dung <see cref="FxEase.StablePhase01"/> (pha on dinh suy tu vi tri +
/// InstanceID — giong nhau qua moi lan chay, khac nhau giua cac con) de tre thoi
/// diem khoi dong cua tung con.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Map45/Thu Hoang Di Lai (WildAnimalWander)")]
public class WildAnimalWander : MonoBehaviour
{
    /// <summary>Cac pha trong vong doi cua con thu.</summary>
    public enum TrangThaiThu
    {
        /// <summary>Dung yen ngo nghieng.</summary>
        DungYen = 0,
        /// <summary>Dang di toi diem da chon.</summary>
        DiBo = 1,
        /// <summary>Dung tai cho cui dau gam co / an.</summary>
        GamCo = 2
    }

    // ── Pham vi ──────────────────────────────────────────────────────────
    [Header("Pham vi di lai (quanh cho duoc dat)")]
    [Tooltip("Ban kinh di lai theo truc X, tinh bang world unit. Mot o iso = 300 x 150 " +
             "unit nen 300 ~ 1 o ngang. Con vat KHONG BAO GIO ra khoi vung nay.")]
    public float banKinhDiLai = 300f;

    [Tooltip("He so det cua vung di lai theo truc Y (luoi iso 2:1 nen 0.5 la dung ti le). " +
             "De 1 neu muon vung di lai la hinh tron.")]
    [Range(0.1f, 1f)]
    public float tiLeDetIso = 0.5f;

    [Tooltip("Khoang cach coi nhu 'da toi diem' (world unit). Ban do dung toa do rat lon " +
             "nen KHONG the de 0.05 nhu game toa do nho.")]
    public float nguongToiNoi = 6f;

    // ── Toc do ───────────────────────────────────────────────────────────
    [Header("Toc do")]
    [Tooltip("Toc do di bo goc (world unit / giay). 40 ~ di het 1 o iso trong khoang 7 giay.")]
    public float tocDoDiBo = 40f;

    [Tooltip("Bien thien toc do rieng cho tung con (ti le). 0.2 = moi con nhanh/cham lech " +
             "nhau toi 20% so voi toc do goc, de ca dan khong buoc cung nhip.")]
    [Range(0f, 0.8f)]
    public float bienThienTocDo = 0.2f;

    // ── Thoi luong ───────────────────────────────────────────────────────
    [Header("Thoi luong cac pha (giay)")]
    [Tooltip("Thoi gian dung yen NGAN NHAT truoc khi chon diem di moi.")]
    public float dungYenMin = 1.5f;

    [Tooltip("Thoi gian dung yen DAI NHAT truoc khi chon diem di moi.")]
    public float dungYenMax = 4.5f;

    [Tooltip("Thoi gian gam co NGAN NHAT sau khi toi noi.")]
    public float gamCoMin = 2.5f;

    [Tooltip("Thoi gian gam co DAI NHAT sau khi toi noi.")]
    public float gamCoMax = 6.5f;

    [Tooltip("Xac suat con vat gam co sau khi di toi noi (0 = khong bao gio gam, " +
             "1 = luc nao cung gam). Con lai la di thang sang pha dung yen.")]
    [Range(0f, 1f)]
    public float xacSuatGamCo = 0.55f;

    [Tooltip("Do tre khoi dong TOI DA (giay). Moi con nhan mot phan tre rieng suy tu " +
             "FxEase.StablePhase01 nen ca dan tha cung mot frame se khong dong loat buoc.")]
    public float doTreKhoiDongToiDa = 3f;

    // ── Animator ─────────────────────────────────────────────────────────
    [Header("Animator (bo trong tham so nao KHONG muon dung)")]
    [Tooltip("Ten tham so float 'toc do'. Day la quy uoc cua bo controller thu trong du an " +
             "(Controller_cow / Controller_piggy / Controller_Prefab_Chicken — LivestockAI " +
             "cung dang set tham so nay). Tham so khong co trong controller se bi BO QUA.")]
    public string thamSoSpeed = "Speed";

    [Tooltip("Ten tham so bool 'dang di' (quy uoc TouristAgent). Bo qua neu controller khong co.")]
    public string thamSoIsMoving = "IsMoving";

    [Tooltip("Ten tham so float huong X (quy uoc TouristAgent). Bo qua neu controller khong co.")]
    public string thamSoDirX = "DirX";

    [Tooltip("Ten tham so float huong Y (quy uoc TouristAgent). Bo qua neu controller khong co.")]
    public string thamSoDirY = "DirY";

    [Tooltip("Ten tham so bool 'dang an co'. Bo qua neu controller khong co (hau het " +
             "controller thu hien nay chi co Idle + Walk nen se tu dong bo qua).")]
    public string thamSoAnCo = "IsEating";

    // ── Lat mat ──────────────────────────────────────────────────────────
    [Header("Lat mat theo huong di")]
    [Tooltip("Lat sprite theo huong di (doi dau localScale.x — dung cach LivestockAI dang lam).")]
    public bool latTheoHuongDi = true;

    [Tooltip("Bat neu art goc VE CON VAT QUAY SANG PHAI. Tat neu art goc quay sang trai.")]
    public bool artGocQuayPhai = true;

    // ── Y-sort ───────────────────────────────────────────────────────────
    [Header("Y-sort (quy uoc farm: SortingGroup + order theo Y)")]
    [Tooltip("Tat neu doi tuong da co he sap xep rieng. Bat = tu them SortingGroup neu thieu " +
             "va cap nhat order theo Y moi khi con vat di chuyen du xa.")]
    public bool dungYSort = true;

    [Tooltip("Sorting layer. Mac dinh 'Default' de chim duoi cay/decor the gioi (layer Objects).")]
    public string sortingLayerName = "Default";

    [Tooltip("Order goc truoc khi cong phan tinh theo Y. De 0 va layer Default de bi cay/decor the gioi che phu hop.")]
    public int baseSortingOrder = 0;

    [Tooltip("He so doi Y world -> sortingOrder. Ban do dung toa do rat lon (hang nghin) nen " +
             "he so phai nho, khong thi tran gioi han sorting order cua Unity (+-32767).")]
    public float heSoYSort = 1f;

    [Tooltip("Bien kep phan Y-sort — giu tong order trong khoang an toan cua Unity.")]
    public int kepYSort = 8000;

    // ── Tranh chuong ngai vat ────────────────────────────────────────────
    [Header("Tranh nuoc / cong trinh (kiem tra RE, KHONG phai tim duong)")]
    [Tooltip("Cac layer vat ly coi la CAM DAT CHAN (nuoc, cong trinh...). DE TRONG (Nothing) " +
             "= tat kiem tra, con vat chi bi gioi han boi ban kinh. Khi co gia tri, moi diem " +
             "den duoc thu bang DUNG MOT lan Physics2D.OverlapPoint (khong cap phat); diem nao " +
             "trung chuong ngai vat thi boc lai. KHONG co pathfinding — con vat chi TRANH " +
             "CHON diem xau, khong biet di vong.")]
    public LayerMask lopChuongNgaiVat = 0;

    [Tooltip("So lan boc lai diem den khi diem vua boc trung chuong ngai vat. Het luot ma van " +
             "trung thi con vat dung yen them mot nhip roi thu lai.")]
    [Range(1, 12)]
    public int soLanBocLaiDiem = 6;

    // ── Trang thai runtime ───────────────────────────────────────────────
    private TrangThaiThu _trangThai;
    private float _hetGio;
    private Vector3 _tamDiLai;
    private Vector3 _diemDen;
    private float _tocDoRieng;
    private float _scaleXGoc;
    private bool _daKhoiDong;

    private Animator _animator;
    private bool _coSpeed, _coIsMoving, _coDirX, _coDirY, _coAnCo;
    private int _hashSpeed, _hashIsMoving, _hashDirX, _hashDirY, _hashAnCo;

    private SortingGroup _sortingGroup;
    private SpriteRenderer _spriteRenderer;
    private string _layerDaGiai;
    private float _yDaSap = float.NaN;

    // ── Be mat cong khai ─────────────────────────────────────────────────

    /// <summary>Pha hien tai cua con thu (chi doc).</summary>
    public TrangThaiThu TrangThai { get { return _trangThai; } }

    /// <summary>Con thu dang di chuyen hay khong (chi doc).</summary>
    public bool DangDiChuyen { get { return _trangThai == TrangThaiThu.DiBo; } }

    /// <summary>Tam vung di lai — mac dinh la vi tri luc Start (chi doc).</summary>
    public Vector3 TamDiLai { get { return _tamDiLai; } }

    /// <summary>
    /// Doi tam vung di lai sang mot cho khac (vi du sau khi Sep keo con vat di noi khac
    /// luc dang Play). Con thu se lap tuc lay diem den moi quanh tam moi.
    /// </summary>
    public void DatTamDiLai(Vector3 tamMoi)
    {
        _tamDiLai = tamMoi;
        VaoPhaDungYen(0.1f);
    }

    /// <summary>Lay tam vung di lai lam ngay tai vi tri hien tai cua con thu.</summary>
    public void DatLaiTamVeViTriHienTai()
    {
        DatTamDiLai(transform.position);
    }

    // ── Vong doi Unity ───────────────────────────────────────────────────

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        _animator = GetComponentInChildren<Animator>(true);

        DocThamSoAnimatorMotLan();

        _scaleXGoc = Mathf.Abs(transform.localScale.x);
        if (_scaleXGoc < 0.0001f) _scaleXGoc = 1f;

        if (dungYSort) ChuanBiYSort();
    }

    private void Start()
    {
        _tamDiLai = transform.position;
        _diemDen = _tamDiLai;

        // Toc do rieng cho tung con: lech +- bienThienTocDo quanh toc do goc.
        _tocDoRieng = tocDoDiBo * (1f + Random.Range(-bienThienTocDo, bienThienTocDo));
        if (_tocDoRieng < 0.01f) _tocDoRieng = 0.01f;

        // Lech pha ON DINH: cung mot con luon ra cung mot do tre qua moi lan Play,
        // nhung hai con khac nhau thi tre khac nhau.
        float pha = FxEase.StablePhase01(transform);
        VaoPhaDungYen(pha * Mathf.Max(0f, doTreKhoiDongToiDa));

        _daKhoiDong = true;

        if (dungYSort) CapNhatYSort(true);
    }

    private void Update()
    {
        if (!_daKhoiDong) return;

        float dt = Time.deltaTime;
        _hetGio -= dt;

        switch (_trangThai)
        {
            case TrangThaiThu.DungYen:
                if (_hetGio <= 0f) BatDauDiBo();
                break;

            case TrangThaiThu.DiBo:
                TienToiDiemDen(dt);
                break;

            case TrangThaiThu.GamCo:
                if (_hetGio <= 0f) VaoPhaDungYen(Random.Range(dungYenMin, dungYenMax));
                break;
        }

        if (dungYSort) CapNhatYSort(false);
    }

    // ── State machine ────────────────────────────────────────────────────

    private void VaoPhaDungYen(float giay)
    {
        _trangThai = TrangThaiThu.DungYen;
        _hetGio = giay;
        DatAnimator(false, false);
    }

    private void VaoPhaGamCo()
    {
        _trangThai = TrangThaiThu.GamCo;
        _hetGio = Random.Range(gamCoMin, gamCoMax);
        DatAnimator(false, true);
    }

    private void BatDauDiBo()
    {
        Vector3 diem;
        if (!BocDiemDen(out diem))
        {
            // Khong boc duoc diem sach (bi nuoc/cong trinh vay quanh) — nghi them
            // mot nhip ngan roi thu lai. Khong bao gio ket cung.
            VaoPhaDungYen(Random.Range(dungYenMin, dungYenMax));
            return;
        }

        _diemDen = diem;
        _trangThai = TrangThaiThu.DiBo;
        _hetGio = 0f;
        DatAnimator(true, false);

        CapNhatHuongNhin(_diemDen.x - transform.position.x, _diemDen.y - transform.position.y);
    }

    private void TienToiDiemDen(float dt)
    {
        Vector3 pos = transform.position;
        float dx = _diemDen.x - pos.x;
        float dy = _diemDen.y - pos.y;

        if ((dx * dx + dy * dy) <= (nguongToiNoi * nguongToiNoi))
        {
            transform.position = _diemDen;
            if (Random.value < xacSuatGamCo) VaoPhaGamCo();
            else VaoPhaDungYen(Random.Range(dungYenMin, dungYenMax));
            return;
        }

        Vector3 moi = Vector3.MoveTowards(pos, _diemDen, _tocDoRieng * dt);

        // LUOI AN TOAN: du co ai day/keo con vat, no van bi ep ve trong e-lip.
        moi = EpVaoTrongPhamVi(moi);
        transform.position = moi;

        CapNhatHuongNhin(dx, dy);
        if (_coDirX || _coDirY) DatHuongAnimator(dx, dy);
    }

    // ── Chon diem / gioi han pham vi ─────────────────────────────────────

    /// <summary>
    /// Boc ngau nhien mot diem trong e-lip quanh tam. Tra ve false neu het luot boc
    /// ma diem nao cung trung chuong ngai vat.
    /// </summary>
    private bool BocDiemDen(out Vector3 diem)
    {
        int soLuot = (lopChuongNgaiVat.value == 0) ? 1 : soLanBocLaiDiem;

        for (int i = 0; i < soLuot; i++)
        {
            // Phan bo DEU tren dia: sqrt(random) chong don cuc o giua.
            float goc = Random.value * Mathf.PI * 2f;
            float ban = Mathf.Sqrt(Random.value) * banKinhDiLai;

            Vector3 p = _tamDiLai;
            p.x += Mathf.Cos(goc) * ban;
            p.y += Mathf.Sin(goc) * ban * tiLeDetIso;
            p.z = transform.position.z;

            if (lopChuongNgaiVat.value == 0 || Physics2D.OverlapPoint(p, lopChuongNgaiVat) == null)
            {
                diem = p;
                return true;
            }
        }

        diem = transform.position;
        return false;
    }

    /// <summary>Ep mot vi tri ve trong e-lip di lai (khong bao gio ra ngoai ban kinh).</summary>
    private Vector3 EpVaoTrongPhamVi(Vector3 p)
    {
        if (banKinhDiLai <= 0.0001f) return _tamDiLai;

        float rx = banKinhDiLai;
        float ry = Mathf.Max(0.0001f, banKinhDiLai * tiLeDetIso);

        float nx = (p.x - _tamDiLai.x) / rx;
        float ny = (p.y - _tamDiLai.y) / ry;
        float d2 = nx * nx + ny * ny;

        if (d2 <= 1f) return p;

        float k = 1f / Mathf.Sqrt(d2);
        p.x = _tamDiLai.x + nx * k * rx;
        p.y = _tamDiLai.y + ny * k * ry;
        return p;
    }

    // ── Animator ─────────────────────────────────────────────────────────

    /// <summary>
    /// Doc DUNG MOT LAN danh sach tham so cua controller de biet tham so nao CO THAT.
    /// Nho vay khi chay chi goi SetFloat/SetBool cho tham so ton tai — khong Animator
    /// hoac controller thieu tham so deu khong sinh loi/canh bao.
    /// </summary>
    private void DocThamSoAnimatorMotLan()
    {
        _coSpeed = _coIsMoving = _coDirX = _coDirY = _coAnCo = false;
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        AnimatorControllerParameter[] ds = _animator.parameters;
        if (ds == null) return;

        for (int i = 0; i < ds.Length; i++)
        {
            AnimatorControllerParameter p = ds[i];
            if (p == null || string.IsNullOrEmpty(p.name)) continue;

            if (!_coSpeed && p.type == AnimatorControllerParameterType.Float && p.name == thamSoSpeed)
            {
                _coSpeed = true; _hashSpeed = p.nameHash;
            }
            else if (!_coIsMoving && p.type == AnimatorControllerParameterType.Bool && p.name == thamSoIsMoving)
            {
                _coIsMoving = true; _hashIsMoving = p.nameHash;
            }
            else if (!_coDirX && p.type == AnimatorControllerParameterType.Float && p.name == thamSoDirX)
            {
                _coDirX = true; _hashDirX = p.nameHash;
            }
            else if (!_coDirY && p.type == AnimatorControllerParameterType.Float && p.name == thamSoDirY)
            {
                _coDirY = true; _hashDirY = p.nameHash;
            }
            else if (!_coAnCo && p.type == AnimatorControllerParameterType.Bool && p.name == thamSoAnCo)
            {
                _coAnCo = true; _hashAnCo = p.nameHash;
            }
        }
    }

    private void DatAnimator(bool dangDi, bool dangAn)
    {
        if (_animator == null) return;

        if (_coSpeed) _animator.SetFloat(_hashSpeed, dangDi ? 1f : 0f);
        if (_coIsMoving) _animator.SetBool(_hashIsMoving, dangDi);
        if (_coAnCo) _animator.SetBool(_hashAnCo, dangAn);
    }

    private void DatHuongAnimator(float dx, float dy)
    {
        if (_animator == null) return;

        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d < 0.0001f) return;

        if (_coDirX) _animator.SetFloat(_hashDirX, dx / d);
        if (_coDirY) _animator.SetFloat(_hashDirY, dy / d);
    }

    private void CapNhatHuongNhin(float dx, float dy)
    {
        if (!latTheoHuongDi) return;
        if (Mathf.Abs(dx) < 0.5f) return;   // gan nhu di thang dung — giu nguyen mat

        bool quayPhai = dx > 0f;
        float dau = (quayPhai == artGocQuayPhai) ? 1f : -1f;

        Vector3 s = transform.localScale;
        float mong = _scaleXGoc * dau;
        if (Mathf.Approximately(s.x, mong)) return;   // khong ghi lai neu khong doi

        s.x = mong;
        transform.localScale = s;
    }

    // ── Y-sort (quy uoc farm) ────────────────────────────────────────────

    private void ChuanBiYSort()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        if (_sortingGroup == null) _sortingGroup = gameObject.AddComponent<SortingGroup>();

        // Layer Default de chim duoi cay, bui co va cong trinh the gioi (layer Objects)
        _layerDaGiai = TouristSortingLayers.ResolveOrOverride(string.IsNullOrEmpty(sortingLayerName) ? "Default" : sortingLayerName, new[] { "Default", "Objects" });
        _sortingGroup.sortingLayerName = _layerDaGiai;
        _sortingGroup.sortingOrder = baseSortingOrder;
    }

    private void CapNhatYSort(bool epBuoc)
    {
        if (_sortingGroup == null) return;

        float y = transform.position.y;

        // Dung yen thi khong tinh lai (Y-sort chi doi khi con vat NHUC NHICH).
        if (!epBuoc && !float.IsNaN(_yDaSap) && Mathf.Abs(y - _yDaSap) < 1f) return;
        _yDaSap = y;

        int dong = Mathf.Clamp(Mathf.RoundToInt(-y * heSoYSort), -kepYSort, kepYSort);
        _sortingGroup.sortingOrder = baseSortingOrder + dong;
    }

    // ── Gizmo ────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 tam = Application.isPlaying ? _tamDiLai : transform.position;
        float rx = banKinhDiLai;
        float ry = banKinhDiLai * tiLeDetIso;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);

        const int buoc = 40;
        Vector3 truoc = tam + new Vector3(rx, 0f, 0f);
        for (int i = 1; i <= buoc; i++)
        {
            float a = (i / (float)buoc) * Mathf.PI * 2f;
            Vector3 nay = tam + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
            Gizmos.DrawLine(truoc, nay);
            truoc = nay;
        }

        if (Application.isPlaying && _trangThai == TrangThaiThu.DiBo)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawLine(transform.position, _diemDen);
        }
    }
}
