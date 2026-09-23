using UnityEngine;

/// <summary>
/// Keo RectTransform dang gan vao lot gon trong vung an toan cua man hinh
/// (Screen.safeArea) de tai tho / lo camera / thanh cu chi khong che HUD.
///
/// GAN VAO DAU: gan vao GOC cua mot nhom UI (thuong la con truc tiep cua Canvas),
/// khong gan vao tung nut. SafeAreaBootstrap tu chen mot object "~SafeArea" mang
/// component nay duoi moi Canvas Overlay nen thuong khong phai gan tay.
///
/// CACH LAM: quy safeArea (pixel) ve anchorMin/anchorMax (0-1) roi zero hoa offset.
/// Vi lam qua anchor nen moi con ben trong giu nguyen ti le, khong phai sua gi them.
///
/// LE TOI THIEU: [2026-09-21] moi canh luon lui vao it nhat LE_TOI_THIEU_PX (nhan theo
/// Screen.dpi/160, kep [1,3]) ke ca may KHONG khuyet, de HUD khong dan sat mep man hinh.
/// Vung ap dung = giao cua Screen.safeArea va man hinh da lui le.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Ap dung theo chieu nao")]
    [Tooltip("Ap vung an toan theo chieu NGANG (trai + phai). Game nam ngang thi tai tho nam hai ben nen day la chieu quan trong nhat.")]
    public bool apDungNgang = true;

    [Tooltip("Ap vung an toan theo chieu DOC (tren + duoi). Chu yeu de tranh thanh cu chi Android / iPhone o canh duoi.")]
    public bool apDungDoc = true;

    [Tooltip("Bo le toi thieu 12px (chi con tranh tai tho THAT). Dung cho man da can tay tung pixel " +
             "nhu bep: tren PC/Editor khong co tai tho => Play giong het Edit mode.")]
    public bool boLeToiThieu = false;

    [Header("Debug")]
    [Tooltip("In log moi lan ap lai vung an toan. Chi bat khi test tren may that.")]
    public bool ghiLog = false;

    /// <summary>Le toi thieu (pixel o dpi 160) ap cho MOI canh, ke ca may khong notch.</summary>
    public const float LE_TOI_THIEU_PX = 12f;

    /// <summary>He so DPI: Screen.dpi/160 kep [1,3]; dpi = 0 (Editor/khong ro) coi nhu 160.</summary>
    public static float HeSoDpi()
    {
        float dpi = Screen.dpi;
        if (dpi <= 1f) dpi = 160f;
        return Mathf.Clamp(dpi / 160f, 1f, 3f);
    }

    /// <summary>Le toi thieu THUC (pixel man hinh) sau khi nhan he so DPI.</summary>
    public static float LeToiThieuPixel() => LE_TOI_THIEU_PX * HeSoDpi();

    private RectTransform _rect;

    // Cache de chi tinh lai khi THUC SU doi, khong tinh moi frame.
    private Rect              _safeAreaCuoi;
    private int               _rongCuoi = -1;
    private int               _caoCuoi  = -1;
    private ScreenOrientation _huongCuoi;

    // Anchor GOC luc chua bi component nay dung toi. Moi phep tinh deu dua tren
    // day chu khong dua tren anchor da bi sua, nen chay bao nhieu lan cung khong don lech.
    private Vector2 _anchorMinGoc;
    private Vector2 _anchorMaxGoc;
    private bool    _daLuuAnchorGoc;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        LuuAnchorGoc();
    }

    private void OnEnable()
    {
        // Dat moc "khong hop le" de lan dau chac chan tinh lai.
        _rongCuoi = -1;
        _caoCuoi  = -1;
        ApDung();
    }

    private void Update()
    {
        // Poll rat nhe: chi so 4 moc. Giong nhau thi thoat ngay o dong dau.
        if (_rongCuoi == Screen.width &&
            _caoCuoi  == Screen.height &&
            _huongCuoi == Screen.orientation &&
            CungMotVung(Screen.safeArea, _safeAreaCuoi)) return;

        ApDung();
    }

    /// <summary>So 2 vung an toan theo tung canh, nguong 1 pixel la du min.</summary>
    private static bool CungMotVung(Rect a, Rect b)
    {
        return Mathf.Abs(a.x - b.x) < 1f && Mathf.Abs(a.y - b.y) < 1f &&
               Mathf.Abs(a.width  - b.width)  < 1f &&
               Mathf.Abs(a.height - b.height) < 1f;
    }

    private void LuuAnchorGoc()
    {
        if (_daLuuAnchorGoc || _rect == null) return;
        _anchorMinGoc   = _rect.anchorMin;
        _anchorMaxGoc   = _rect.anchorMax;
        _daLuuAnchorGoc = true;
    }

    /// <summary>Tinh lai anchor theo Screen.safeArea hien tai.</summary>
    public void ApDung()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_rect == null) return;
        LuuAnchorGoc();

        int w = Screen.width;
        int h = Screen.height;
        Rect safe = Screen.safeArea;

        // Ghi cache TRUOC moi nhanh thoat som, neu khong se tinh lai moi frame.
        _rongCuoi     = w;
        _caoCuoi      = h;
        _huongCuoi    = Screen.orientation;
        _safeAreaCuoi = safe;

        // CHAN CHIA CHO 0: man hinh bang 0 (nhip dau tren vai thiet bi) thi bo qua,
        // va ep tinh lai o nhip sau bang cach xoa moc.
        if (w <= 0 || h <= 0)
        {
            _rongCuoi = -1;
            _caoCuoi  = -1;
            return;
        }

        // safeArea bang 0 (he dieu hanh chua bao kip) => khong dung gi.
        if (safe.width <= 0f || safe.height <= 0f)
        {
            _rongCuoi = -1;
            _caoCuoi  = -1;
            return;
        }

        // Rect cha bang 0 => moi phep quy doi deu vo nghia, bo qua va thu lai sau.
        RectTransform cha = _rect.parent as RectTransform;
        if (cha != null && (cha.rect.width <= 0f || cha.rect.height <= 0f))
        {
            _rongCuoi = -1;
            _caoCuoi  = -1;
            return;
        }

        // Le toi thieu moi canh (ke ca may khong khuyet): vung dung = giao(safeArea, man hinh lui le).
        float le = boLeToiThieu ? 0f : LeToiThieuPixel();
        // Le khong duoc "an" qua 1/4 moi chieu (man hinh rat nho / dpi bao sai).
        le = Mathf.Min(le, Mathf.Min(w, h) * 0.25f);

        float xMinPx = Mathf.Max(safe.xMin, le);
        float yMinPx = Mathf.Max(safe.yMin, le);
        float xMaxPx = Mathf.Min(safe.xMax, w - le);
        float yMaxPx = Mathf.Min(safe.yMax, h - le);

        // Khong lui gi ca (le = 0 va khong khuyet) => tra ve dung anchor goc, giu nguyen layout thiet ke.
        bool khongDoi = xMinPx <= 0.5f && yMinPx <= 0.5f &&
                        xMaxPx >= w - 0.5f && yMaxPx >= h - 0.5f;
        if (khongDoi)
        {
            _rect.anchorMin = _anchorMinGoc;
            _rect.anchorMax = _anchorMaxGoc;
            ZeroOffset();
            if (ghiLog) Debug.Log("[SafeArea] " + name + ": khong khuyet va khong le, giu nguyen layout goc.");
            return;
        }

        // pixel -> ti le 0-1
        Vector2 min = new Vector2(xMinPx / w, yMinPx / h);
        Vector2 max = new Vector2(xMaxPx / w, yMaxPx / h);

        // Chieu nao khong ap thi tra ve anchor goc cua chieu do.
        if (!apDungNgang) { min.x = _anchorMinGoc.x; max.x = _anchorMaxGoc.x; }
        if (!apDungDoc)   { min.y = _anchorMinGoc.y; max.y = _anchorMaxGoc.y; }

        // Kep 0-1 va giu min < max: thiet bi bao so la cung khong lam rect am.
        min.x = Mathf.Clamp01(min.x); min.y = Mathf.Clamp01(min.y);
        max.x = Mathf.Clamp01(max.x); max.y = Mathf.Clamp01(max.y);
        if (max.x - min.x < 0.01f) { min.x = _anchorMinGoc.x; max.x = _anchorMaxGoc.x; }
        if (max.y - min.y < 0.01f) { min.y = _anchorMinGoc.y; max.y = _anchorMaxGoc.y; }

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        ZeroOffset();

        if (ghiLog)
            Debug.Log("[SafeArea] " + name + ": safeArea=" + safe + " man hinh=" + w + "x" + h +
                      " le=" + le.ToString("F1") + "px (dpi " + Screen.dpi + ")" +
                      " -> anchorMin=" + min + " anchorMax=" + max);
    }

    /// <summary>
    /// Offset phai ve 0 thi anchor moi co hieu luc that, con offset cu thi rect van
    /// bi keo ra ngoai vung an toan dung bang so offset do.
    /// </summary>
    private void ZeroOffset()
    {
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    /// <summary>Doi co trong Inspector luc dang Play thi thay ket qua ngay.</summary>
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        _rongCuoi = -1;
        _caoCuoi  = -1;
    }
#endif
}
