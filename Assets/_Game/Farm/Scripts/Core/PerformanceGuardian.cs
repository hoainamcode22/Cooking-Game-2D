using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [PERF P0 — F1/F2/F3] BO CANH HIEU NANG LUON SONG.
///
/// VI SAO CAN FILE NAY (F1): <see cref="MobilePerformanceBootstrap"/> dat
/// <c>QualitySettings.vSyncCount = 0</c> + <c>Application.targetFrameRate</c> DUNG MOT LAN o
/// <c>BeforeSceneLoad</c>. Nhung <c>ProjectSettings/QualitySettings.asset</c> khai tier Android
/// (index 2 "Medium") voi <c>vSyncCount: 1</c>. Bat ky ai goi
/// <c>QualitySettings.SetQualityLevel(...)</c> ve sau (popup Cai Dat, menu Do Hoa, plugin...)
/// deu AP LAI tier do => <c>vSyncCount</c> quay ve 1 => tren Unity 6 Android
/// <c>Application.targetFrameRate</c> BI BO QUA HOAN TOAN va tran FPS chet am tham.
///
/// Component nay <c>DontDestroyOnLoad</c>, do MOT so nguyen (<c>QualitySettings.vSyncCount</c>)
/// moi <see cref="KIEM_TRA_MOI_GIAY"/> giay va sau moi lan <c>sceneLoaded</c>. Chi phi thuc te
/// bang 0; doi lai tran FPS khong bao gio bi go am tham nua.
///
/// Component cung la noi moc hai viec chay-luc-nap-scene:
///   • F2 — <see cref="AnimatorCullingOptimizer"/>: dat culling cho Animator (tau lua o ngoai man hinh).
///   • F3 — <see cref="TilemapRenderModeOptimizer"/>: doi TilemapRenderer nen sang Chunk.
///
/// KHONG keo tay vao scene nao: <see cref="MobilePerformanceBootstrap"/> tu tao.
/// </summary>
[DisallowMultipleComponent]
public class PerformanceGuardian : MonoBehaviour
{
    /// <summary>Chu ky do lai vSync, giay. 1 phep so sanh int / giay = mien phi.</summary>
    public const float KIEM_TRA_MOI_GIAY = 1f;

    /// <summary>Tat toan bo viec tai-khang-dinh FPS (chi de go loi).</summary>
    public static bool BatTaiKhangDinhFps = true;

    /// <summary>Instance duy nhat, de code khac goi <see cref="TaiKhangDinhNgay"/>.</summary>
    public static PerformanceGuardian Instance { get; private set; }

    private float  _dongHo;
    private int    _soLanCuu;
    private bool   _daApDungLanNao;  // chan luot Start() thua khi sceneLoaded da chay.

    /// <summary>So lan da phai cuu lai vSync sau khi bi doi — QA doc de chan doan.</summary>
    public int SoLanCuu => _soLanCuu;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += KhiNapScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= KhiNapScene;
    }

    private void Start()
    {
        // Luoi an toan: neu vi ly do gi do `sceneLoaded` KHONG no cho scene dau tien
        // (thu tu khoi tao cua Unity doi trong tuong lai) thi van ap dung duoc mot lan.
        // Truong hop binh thuong sceneLoaded da chay roi => bo qua, khong lam trung.
        if (_daApDungLanNao) return;
        ApDungChoSceneHienTai(SceneManager.GetActiveScene().name);
    }

    private void KhiNapScene(Scene scene, LoadSceneMode mode)
    {
        TaiKhangDinhNgay();
        ApDungChoSceneHienTai(scene.name);
    }

    private void ApDungChoSceneHienTai(string tenScene)
    {
        // LUON chay lai moi lan nap scene (ke ca additive nap lai lan 2): hai ham duoi
        // deu tu bo qua doi tuong da dung roi, va moi lan chi la MOT FindObjectsByType
        // luc nap scene — khong phai viec moi frame.
        _daApDungLanNao = true;

        AnimatorCullingOptimizer.ApDung(tenScene);
        TilemapRenderModeOptimizer.ApDung(tenScene);
    }

    private void Update()
    {
        if (!BatTaiKhangDinhFps) return;

        _dongHo += Time.unscaledDeltaTime;
        if (_dongHo < KIEM_TRA_MOI_GIAY) return;
        _dongHo = 0f;

        // MOT phep doc int. Neu ai do vua SetQualityLevel() thi no != 0 va ta cuu lai.
        if (QualitySettings.vSyncCount != 0 ||
            Application.targetFrameRate != MobilePerformanceBootstrap.FPS_MUC_TIEU)
        {
            TaiKhangDinhNgay();
        }
    }

    /// <summary>
    /// Ap lai vSync=0 + targetFrameRate. Goi ngay SAU bat ky
    /// <c>QualitySettings.SetQualityLevel(...)</c> nao neu muon khong phai cho toi 1 giay.
    /// </summary>
    public void TaiKhangDinhNgay()
    {
        bool bimat = QualitySettings.vSyncCount != 0;

        QualitySettings.vSyncCount   = 0;
        Application.targetFrameRate  = MobilePerformanceBootstrap.FPS_MUC_TIEU;
        Screen.sleepTimeout          = SleepTimeout.NeverSleep;

        if (bimat)
        {
            _soLanCuu++;
            Debug.Log("[Perf] PerformanceGuardian: phat hien vSyncCount bi dat lai != 0 " +
                      "(rat co the do SetQualityLevel) -> da ep ve 0, targetFrameRate=" +
                      MobilePerformanceBootstrap.FPS_MUC_TIEU + ". Lan thu " + _soLanCuu + ".");
        }
    }
}
