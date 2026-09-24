using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// BO DICH CHAY NEN — dich TOAN BO chu tren man hinh sang tieng Anh ma KHONG phai sua 160 file UI.
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// VI SAO CAN: du an co ~3.000 chuoi tieng Viet nam rai rac trong code dung UI bang tay
/// (new GameObject + CreateText). Boc Loc.T() cho tung cho la sua hang tram file — khong kha thi
/// va de bo sot. Thay vao do: quet moi TMP_Text dang song, cau nao co trong bang dich thi thay.
///
/// CACH HOAT DONG (EVENT-DRIVEN CHO MOBILE)
///   • Dang tieng Viet  → KHONG LAM GI. Runner bi tat han (enabled = false) nen Unity
///     khong goi ca Update ⇒ chi phi moi khung hinh = 0.
///   • Doi sang tieng Anh → quet khi: doi ngon ngu, scene vua tai xong (+ cuoi khung hinh),
///     hoac khi UI chu dong goi <see cref="RequestRescan"/> sau luc mo popup / dung chu moi.
///   • Doi ve tieng Viet → tra lai NGUYEN VAN cau goc da nho, VA tra lai cau hinh co chu goc.
///
/// ── [FIX 2026-09-16 P0] GO BO QUET THEO CHAM ─────────────────────────────────────────
/// Ban cu goi FindObjectsByType&lt;TMP_Text&gt;(Include) NGAY tai pointer-down — dung khoanh khac
/// ngon tay cham xuong de keo ban do ⇒ giat 5-40ms MOI LAN KEO. Da bo han nhanh input do.
/// Vong poll 1 Hz cung bo, thay bang poll an toan 0,5s, tu gian ra 2s khi 3 luot quet lien tiep
/// khong doi gi, va reset ve 0,5s khi co bien dong. Ngoai ra UI CO goi RequestRescan() ngay sau
/// khi mo popup / dung list — mot dong, khong ton gi — nen thuc te chu doi ngay trong khung hinh do.
///
/// ── [MOI 2026-09-16] VUA KHUNG (AUTO-FIT) ────────────────────────────────────────────
/// Cau tieng Anh dai hon tieng Viet ~20-40%, ma 247/273 nhan trong SCN_Farm de
/// enableAutoSizing = 0 + NoWrap + Overflow ⇒ chu TRAN RA NGOAI KHUNG. Sau moi lan thay
/// VN→EN, do lai nhan do va neu tran thi bat autosize (chi cho PHEP NHO LAI, khong to len)
/// + Ellipsis neu la nhan mot dong. Cau hinh co chu GOC duoc nho lai canh vnGoc va tra lai
/// nguyen ven khi ve tieng Viet. Tat bang <see cref="AutoFitEnabled"/> neu can.
///
/// AN TOAN
///   • Nho cap (cau goc VN ⇄ cau da dich EN) theo tung object nen doi qua doi lai bao nhieu
///     lan cung khong sai, khong dich chong len chinh no.
///   • Bo qua o nhap lieu (TMP_InputField) — khong duoc dich chu nguoi choi go.
///   • Bo qua object ten co "[NoLoc]" hoac bat dau bang "~Loc".
///   • Bo qua chu qua dai (&gt; 400 ky tu).
///   • Cau chua co ban dich → ghi ra Assets/_Debug_Capture/loc_missing.txt de bo sung dan.
/// </summary>
public static class LocRuntimeInterceptor
{
    private const int   DAI_TOI_DA   = 400;
    private const int   THIEU_TOI_DA = 3000;

    /// <summary>Nhip poll an toan luc "co bien dong" (giay). Truoc day la 1.0f + moi lan cham; ban 4f qua cham.</summary>
    private const float NHIP_NHANH = 1.0f;
    /// <summary>Nhip poll khi man hinh da on dinh (giay).</summary>
    private const float NHIP_CHAM  = 8.0f;
    /// <summary>Bao nhieu luot quet khong doi gi thi ha xuong nhip cham.</summary>
    private const int   SO_LUOT_ON_DINH = 3;
    /// <summary>Trong khoang nay thi dung lai danh sach TMP_Text da tim (giay).</summary>
    private const float HAN_DUNG_CACHE = 0.5f;

    /// <summary>
    /// Bat/tat buoc do-va-thu-nho chu sau khi dich. Sep tat duoc neu thay no lam hong layout nao do:
    /// <c>LocRuntimeInterceptor.AutoFitEnabled = false;</c>
    /// </summary>
    public static bool AutoFitEnabled = true;

    /// <summary>Mot nhan dang theo doi: cau goc, cau da ap, va cau hinh CO CHU GOC de hoan tra.</summary>
    private class Muc
    {
        public string vnGoc;
        public string enDaAp;
        public bool   boQua;

        // ── cau hinh co chu NGUYEN BAN (chup mot lan, truoc khi dung den autosize) ──
        public bool  daLuuCoChu;
        public bool  autoSizeGoc;
        public float fontSizeGoc;
        public float fontSizeMinGoc;
        public float fontSizeMaxGoc;
        public TextOverflowModes overflowGoc;
        public bool  dangApVuaKhung;
    }

    private static readonly Dictionary<TMP_Text, Muc> _theoDoi = new Dictionary<TMP_Text, Muc>();
    private static readonly HashSet<string> _chuaDich = new HashSet<string>();
    private static bool _daKhoiTao;
    private static GameObject _runner;
    private static LocInterceptorRunner _chay;
    private static bool _daNgheSceneLoaded;

    // ── cache danh sach TMP_Text ────────────────────────────────────────────
    private static TMP_Text[] _dsCache;
    private static float      _lucCache = -999f;

    public static int SoDangTheoDoi => _theoDoi.Count;
    public static int SoChuaDich    => _chuaDich.Count;

    /// <summary>Goi mot lan luc game khoi dong (LocalizationManager.KhoiTao goi ho).</summary>
    /// <summary>LocalizationManager goi khi vao Play (Domain Reload co the dang bi tat).</summary>
    public static void DatLaiCoKhoiTao() { _daKhoiTao = false; }

    public static void KhoiTao()
    {
        if (_daKhoiTao) return;

        // [FIX 2026-09-22] EDIT MODE: DontDestroyOnLoad nem InvalidOperationException khi
        // khong o Play mode. Bat cu tool Editor nao goi Loc.T() (vd tool va UI bep) se chet
        // ngay tai day. Bang dich la Dictionary C# TINH nen Loc.T van tra cuu binh thuong
        // ma khong can runner; runner chi lam viec quet dinh ky luc chay.
        // CHU Y: khong dat _daKhoiTao = true o nhanh nay, de khi vao Play van khoi tao du.
        if (!Application.isPlaying) return;

        _daKhoiTao = true;

        LocalizationManager.OnChanged += KhiDoiNgonNgu;

        if (_runner == null)
        {
            _runner = new GameObject("~LocRuntimeInterceptor");
            Object.DontDestroyOnLoad(_runner);
            _runner.hideFlags = HideFlags.HideInHierarchy;
            _chay = _runner.AddComponent<LocInterceptorRunner>();
            // Dang tieng Viet thi TAT HAN component: Unity khong goi Update ⇒ 0 chi phi/khung hinh.
            _chay.enabled = LocalizationManager.DangTiengAnh;
        }

        // Scene moi tai xong thi UI dung trong Awake/Start co the bi lo mat
        // => man hinh loe tieng Viet. Nghe sceneLoaded de quet NGAY.
        if (!_daNgheSceneLoaded)
        {
            _daNgheSceneLoaded = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += KhiSceneVuaTai;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CUA GOI TU BEN NGOAI
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Quet lai NGAY LAP TUC (dong bo). Goi sau khi vua dung xong mot dong chu moi ma
    /// can thay doi ngay trong khung hinh nay. Dang tieng Viet thi khong lam gi.
    /// </summary>
    public static void RescanNow()
    {
        // [FIX P0 2026-09-17] TUYET DOI KHONG NEM RA NGOAI.
        // Ham nay duoc goi TU TRONG THAN cac OpenPopup(). Neu no nem, phan con lai cua
        // OpenPopup KHONG chay nua => popup hien ra nhung chua duoc noi day (nut chua gan
        // listener, CanvasGroup chua bat lai) => dung ca "mo duoc ma khong bam duoc".
        // Dich chu la trang tri; no khong bao gio duoc phep lam vo mot popup.
        try
        {
            if (!LocalizationManager.DangTiengAnh) return;
            QuetVaDich(true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Loc] RescanNow bo qua loi (khong anh huong popup): " + e.Message);
        }
    }

    /// <summary>
    /// Xin quet lai o khung hinh ke tiep (re hon <see cref="RescanNow"/>: goi 50 lan trong
    /// mot khung hinh cung chi quet 1 lan). Day la ham UI nen goi sau khi mo popup / build list.
    /// </summary>
    public static void RequestRescan()
    {
        // [FIX P0 2026-09-17] Xem chu thich o RescanNow(): 20 cho trong cac OpenPopup() dang goi
        // ham nay. Duong `else QuetVaDich(true)` chay DONG BO ngay tren stack cua OpenPopup
        // (xay ra khi runner chua kip dung, hoac vua bi Destroy) — do la duong co the nem.
        // Boc toan bo lai: hong ban dich thi thoi, popup van phai song.
        try
        {
            if (!LocalizationManager.DangTiengAnh) return;
            if (_chay != null) _chay.XinQuet();
            else QuetVaDich(true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Loc] RequestRescan bo qua loi (khong anh huong popup): " + e.Message);
        }
    }

    /// <summary>Scene vua tai xong: don muc chet roi quet lai ngay + cuoi khung hinh.</summary>
    private static void KhiSceneVuaTai(UnityEngine.SceneManagement.Scene scene,
                                       UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _dsCache = null;                      // scene khac ⇒ danh sach cu vo nghia
        DonMucChet();
        if (!LocalizationManager.DangTiengAnh) return;

        QuetVaDich(true);

        if (_runner != null)
        {
            var c = _runner.GetComponent<LocInterceptorRunner>();
            if (c != null) c.StartCoroutine(QuetLaiCuoiKhung());
        }
    }

    /// <summary>Quet lai sau khi moi UI cua scene moi da kip dung xong.</summary>
    private static IEnumerator QuetLaiCuoiKhung()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        if (LocalizationManager.DangTiengAnh) QuetVaDich(true);
    }

    private static void KhiDoiNgonNgu(string lang)
    {
        _dsCache = null;
        if (_chay != null)
        {
            _chay.enabled = (lang == LocalizationManager.EN);
            _chay.DatLaiNhip();
        }

        if (lang == LocalizationManager.EN) QuetVaDich(true);
        else                                TraVeTiengViet();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUET
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Quet moi TMP_Text dang song va dich sang tieng Anh (lam moi danh sach).</summary>
    public static void QuetVaDich()
    {
        QuetVaDich(true);
    }

    /// <summary>
    /// Quet va dich. <paramref name="lamMoiDanhSach"/> = false thi dung lai danh sach TMP_Text
    /// da tim lan truoc neu no con moi (&lt; 0,5s) — tranh goi FindObjectsByType lien tiep
    /// trong cung mot cum (vi du quet luc sceneLoaded roi lai quet cuoi khung hinh).
    /// </summary>
    /// <returns>So nhan thuc su bi doi chu trong luot nay.</returns>
    private static int QuetVaDich(bool lamMoiDanhSach) => QuetVaDich(lamMoiDanhSach, true);

    /// <summary>
    /// [FIX 2026-09-18 P0] <paramref name="gomCaObjectTat"/> = false thi CHI quet object dang bat.
    /// FindObjectsByType(Include) phai duyet TOAN BO cay scene (1.860 GameObject o SCN_Farm) ke ca
    /// prefab popup dang tat ⇒ spike 10-40ms. Luoi an toan chay nen khong can dieu do: popup luc mo
    /// da goi RequestRescan() roi. Chi cac moc su kien (doi ngon ngu / scene vua tai) moi quet ca do tat.
    /// </summary>
    private static int QuetVaDich(bool lamMoiDanhSach, bool gomCaObjectTat)
    {
        if (!LocalizationManager.DangTiengAnh) return 0;

        float gio = Time.unscaledTime;
        if (lamMoiDanhSach || _dsCache == null || gio - _lucCache > HAN_DUNG_CACHE)
        {
            _dsCache  = Object.FindObjectsByType<TMP_Text>(
                gomCaObjectTat ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            _lucCache = gio;
        }

        var tats = _dsCache;
        int soDoi = 0;

        for (int i = 0; i < tats.Length; i++)
        {
            var t = tats[i];
            if (t == null) continue;

            // [FIX P0 2026-09-17] Cach ly TUNG nhan. Mot nhan hong (font null, prefab dang bi
            // Destroy, TMP nem trong luc do khung...) truoc day lam DUNG ca luot quet — va neu
            // luot quet do dang chay dong bo trong OpenPopup thi keo do luon ca popup.
            try
            {
                if (!_theoDoi.TryGetValue(t, out var muc))
                {
                    muc = new Muc { boQua = LaChuKhongDuocDich(t) };
                    _theoDoi[t] = muc;
                }
                if (muc.boQua) continue;

                string hienTai = t.text;
                if (string.IsNullOrEmpty(hienTai) || hienTai.Length > DAI_TOI_DA) continue;

                // Chinh cau minh vua dat vao → khong dung den nua. Day la cua early-out chinh:
                // nhan da dich va chua bi code doi chu thi luot qua het, khong do dac gi ca.
                if (muc.enDaAp != null && hienTai == muc.enDaAp) continue;

                string en = LocalizationManager.T(hienTai);
                if (en != hienTai)
                {
                    muc.vnGoc  = hienTai;
                    muc.enDaAp = en;
                    t.text     = en;
                    soDoi++;

                    // [MOI] Chu tieng Anh dai hon ⇒ do lai va thu nho neu tran khung.
                    ApVuaKhung(t, muc);
                }
                else if (CoDauTiengViet(hienTai))
                {
                    GhiChuaDich(hienTai);
                }
            }
            catch
            {
                // Bo qua rieng nhan nay, quet tiep nhan sau.
            }
        }

        DonMucChet();
        return soDoi;
    }

    /// <summary>Tra moi chu ve dung cau tieng Viet goc VA tra lai cau hinh co chu nguyen ban.</summary>
    public static void TraVeTiengViet()
    {
        foreach (var cap in _theoDoi)
        {
            var t = cap.Key; var m = cap.Value;
            if (t == null || m == null) continue;

            // Tra co chu TRUOC khi tra chu: layout chi phai tinh lai mot lan.
            HoanVuaKhung(t, m);

            if (m.vnGoc == null) continue;
            if (t.text == m.enDaAp) t.text = m.vnGoc;
            m.enDaAp = null;
        }
        DonMucChet();
    }

    private static void DonMucChet()
    {
        List<TMP_Text> chet = null;
        foreach (var cap in _theoDoi)
            if (cap.Key == null) (chet ??= new List<TMP_Text>()).Add(cap.Key);
        if (chet == null) return;
        for (int i = 0; i < chet.Count; i++) _theoDoi.Remove(chet[i]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VUA KHUNG (AUTO-FIT)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Do nhan <paramref name="t"/> voi chu HIEN TAI; neu tran ra ngoai rect thi bat autosize
    /// (tran max = co chu goc ⇒ chi co the NHO LAI, khong bao gio to len) va dat Ellipsis cho
    /// nhan mot dong. Goi duoc tu ngoai (LocalizedText goi sau khi tu dat chu tieng Anh).
    /// </summary>
    public static void ApVuaKhung(TMP_Text t) => ApVuaKhung(t, TI_LE_MIN_MAC_DINH);

    /// <summary>
    /// Nhu <see cref="ApVuaKhung(TMP_Text)"/> nhung cho chon san duoi co chu (ti le so voi co goc).
    /// <see cref="LocFit.Fit(TMP_Text, float)"/> goi ham nay sau khi code tu gan Loc.T() vao .text.
    /// </summary>
    public static void ApVuaKhung(TMP_Text t, float tiLeMin)
    {
        if (t == null) return;
        if (!_theoDoi.TryGetValue(t, out var muc))
        {
            muc = new Muc { boQua = LaChuKhongDuocDich(t) };
            _theoDoi[t] = muc;
        }
        if (muc.boQua) return;
        ApVuaKhung(t, muc, tiLeMin);
    }

    /// <summary>San duoi mac dinh khi interceptor tu thu nho: 72% co goc.</summary>
    private const float TI_LE_MIN_MAC_DINH = 0.72f;

    /// <summary>Tra lai co chu / overflow nguyen ban cho mot nhan (dung khi ve tieng Viet).</summary>
    public static void HoanVuaKhung(TMP_Text t)
    {
        if (t == null) return;
        if (_theoDoi.TryGetValue(t, out var muc)) HoanVuaKhung(t, muc);
    }

    private static void ApVuaKhung(TMP_Text t, Muc m) => ApVuaKhung(t, m, TI_LE_MIN_MAC_DINH);

    private static void ApVuaKhung(TMP_Text t, Muc m, float tiLeMin)
    {
        if (!AutoFitEnabled || t == null || m == null || m.boQua) return;
        tiLeMin = Mathf.Clamp(tiLeMin, 0.3f, 1f);

        var rt = t.rectTransform;
        if (rt == null) return;

        // Nhan nam trong LayoutGroup chua kip tinh ⇒ rect = 0. Do luc nay se ra so rac va
        // bat autosize bay ba. Bo qua, luot quet sau (hoac RequestRescan) se lam lai.
        Vector2 khung = rt.rect.size;
        if (khung.x <= 1f || khung.y <= 1f) return;

        string chu = t.text;
        if (string.IsNullOrEmpty(chu)) return;

        // Chup cau hinh GOC dung mot lan — chup sau khi da bat autosize thi fontSize da bi
        // TMP sua, luu vao la mat vinh vien co chu that cua Sep.
        if (!m.daLuuCoChu)
        {
            m.daLuuCoChu     = true;
            m.autoSizeGoc    = t.enableAutoSizing;
            m.fontSizeGoc    = t.fontSize;
            m.fontSizeMinGoc = t.fontSizeMin;
            m.fontSizeMaxGoc = t.fontSizeMax;
            m.overflowGoc    = t.overflowMode;
        }

        // Nhan von DA bat autosize san trong thiet ke ⇒ TMP tu lo vua khung roi, dung vao chi
        // lam hong: fontSize doc duoc luc do la co DA BI BOP, lay no lam fontSizeMax se khoa
        // nhan o co nho vinh vien.
        if (m.autoSizeGoc) return;

        // Do o co chu GOC, khong phai co chu dang bi autosize keo xuong tu luot truoc.
        float coGoc = m.fontSizeGoc;
        if (coGoc <= 0f) return;

        bool phaiHoan = t.enableAutoSizing != m.autoSizeGoc || !Mathf.Approximately(t.fontSize, coGoc);
        if (phaiHoan)
        {
            t.enableAutoSizing = m.autoSizeGoc;
            t.fontSize         = coGoc;
        }

        if (t.font == null) return;

        Vector2 tuDo, epNgang;
        try
        {
            tuDo    = t.GetPreferredValues(chu);                 // khong rang buoc
            epNgang = t.GetPreferredValues(chu, khung.x, 0f);    // ep theo be ngang khung
        }
        catch
        {
            return;
        }

        bool tran    = tuDo.x > khung.x + 0.5f || epNgang.y > khung.y + 0.5f;
        bool motDong = epNgang.y <= tuDo.y + 0.5f;   // ep be ngang ma khong cao them ⇒ khong xuong dong

        if (!tran)
        {
            // Vua khung: neu luot truoc da bop nho thi tra lai nguyen ban.
            if (m.dangApVuaKhung) HoanVuaKhung(t, m);
            return;
        }

        t.enableAutoSizing = true;
        t.fontSizeMax      = coGoc;                             // KHONG cho to hon thiet ke goc
        t.fontSizeMin      = Mathf.Max(8f, coGoc * tiLeMin);    // san duoi: duoi 8pt la khong doc noi
        if (motDong) t.overflowMode = TextOverflowModes.Ellipsis;
        m.dangApVuaKhung = true;
    }

    private static void HoanVuaKhung(TMP_Text t, Muc m)
    {
        if (t == null || m == null) return;
        if (!m.daLuuCoChu || !m.dangApVuaKhung) return;

        t.enableAutoSizing = m.autoSizeGoc;
        t.fontSize         = m.fontSizeGoc;
        t.fontSizeMin      = m.fontSizeMinGoc;
        t.fontSizeMax      = m.fontSizeMaxGoc;
        t.overflowMode     = m.overflowGoc;
        m.dangApVuaKhung   = false;
    }

    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>O nhap lieu thi tuyet doi khong dich (chu nguoi choi tu go).</summary>
    private static bool LaChuKhongDuocDich(TMP_Text t)
    {
        if (t.GetComponentInParent<TMP_InputField>() != null) return true;
        var tr = t.transform;
        while (tr != null)
        {
            if (tr.name.Contains("[NoLoc]") || tr.name.StartsWith("~Loc")) return true;
            tr = tr.parent;
        }
        return false;
    }

    private static bool CoDauTiengViet(string s)
    {
        const string dau = "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ"
                         + "ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ";
        for (int i = 0; i < s.Length; i++)
            if (dau.IndexOf(s[i]) >= 0) return true;
        return false;
    }

    private static void GhiChuaDich(string vi)
    {
        if (_chuaDich.Count >= THIEU_TOI_DA) return;
        _chuaDich.Add(vi);
    }

    /// <summary>Ghi danh sach cau chua dich ra file de Lead bo sung vao bang.</summary>
    public static void XuatCauChuaDich()
    {
#if UNITY_EDITOR
        if (_chuaDich.Count == 0) { Debug.Log("[Loc] Khong con cau nao chua dich."); return; }
        string thuMuc = System.IO.Path.Combine(Application.dataPath, "_Debug_Capture");
        System.IO.Directory.CreateDirectory(thuMuc);
        string f = System.IO.Path.Combine(thuMuc, "loc_missing.txt");
        var dong = new List<string>(_chuaDich);
        dong.Sort();
        System.IO.File.WriteAllLines(f, dong, System.Text.Encoding.UTF8);
        Debug.Log($"[Loc] Da ghi {dong.Count} cau CHUA DICH vao {f}");
#endif
    }

    /// <summary>
    /// Component gan tren GameObject an, chi lam MOT viec: dieu phoi nhip quet.
    /// [FIX 2026-09-16] DA BO nhanh Input.GetMouseButtonDown / touch Began — chinh no la thu phat
    /// gay giat 5-40ms moi lan ngon tay cham xuong de keo ban do.
    /// </summary>
    private class LocInterceptorRunner : MonoBehaviour
    {
        private float _lanQuetCuoi;
        private bool  _xinQuet;
        private int   _soLuotKhongDoi;

        /// <summary>UI goi qua RequestRescan(): danh dau de quet o Update ke tiep.</summary>
        public void XinQuet()
        {
            _xinQuet        = true;
            _soLuotKhongDoi = 0;   // co bien dong ⇒ ve lai nhip nhanh
        }

        /// <summary>Doi ngon ngu: xoa trang bo dem nhip.</summary>
        public void DatLaiNhip()
        {
            _xinQuet        = false;
            _lanQuetCuoi    = Time.unscaledTime;
            _soLuotKhongDoi = 0;
        }

        /// <summary>
        /// [2026-09-22] CHONG NHAN BAN. Object "~LocRuntimeInterceptor" duoc tao luc chay voi
        /// DontDestroyOnLoad + HideInHierarchy, nhung neu scene bi LUU trong luc dang Play
        /// (tool dong bang UI da tung lam vay) thi no bi ghi thang vao file scene. SampleScene
        /// dang chua 3 ban sao nhu vay, cong them 1 ban tu sinh luc chay = 4 Update cung quet
        /// toan bo TMP_Text moi nhip — dung 4 lan chi phi ma khong duoc gi.
        /// Ban nao khong phai ban chinh thi tu huy ngay.
        /// </summary>
        private void Awake()
        {
            if (_chay != null && _chay != this)
            {
                Debug.LogWarning("[Loc] Xoa ban sao thua cua ~LocRuntimeInterceptor (bi luu nham vao scene).", gameObject);
                Destroy(gameObject);
                return;
            }
            _chay = this;
        }

        private void OnEnable()
        {
            DatLaiNhip();
        }

        private void Update()
        {
            // Chan cung: dang tieng Viet thi khong co gi de dich. (KhiDoiNgonNgu con tat han
            // component nay nen thuc te Update khong duoc goi — day chi la day an toan.)
            if (!LocalizationManager.DangTiengAnh) return;

            float gio = Time.unscaledTime;

            if (_xinQuet)
            {
                _xinQuet     = false;
                _lanQuetCuoi = gio;
                int doi = LocRuntimeInterceptor.QuetVaDich(true);
                _soLuotKhongDoi = doi > 0 ? 0 : _soLuotKhongDoi + 1;
                return;
            }

            // [FIX 2026-09-18 P0] DANG CHAM MAN HINH thi TUYET DOI khong quet.
            // Day chinh la luc Sep keo ban do; mot spike FindObjectsByType o day = giat tay.
            if (Input.touchCount > 0 || Input.GetMouseButton(0)) { _lanQuetCuoi = gio; return; }

            // Luoi an toan: bat nhung chu do code dung ra ngoai scene-load ma quen RequestRescan().
            float nhip = _soLuotKhongDoi >= SO_LUOT_ON_DINH ? NHIP_CHAM : NHIP_NHANH;
            if (gio - _lanQuetCuoi < nhip) return;

            _lanQuetCuoi = gio;
            // Luoi an toan chi quet object DANG BAT ⇒ re hon nhieu lan.
            int soDoi = LocRuntimeInterceptor.QuetVaDich(true, false);
            _soLuotKhongDoi = soDoi > 0 ? 0 : _soLuotKhongDoi + 1;
        }
    }
}

/// <summary>
/// [2026-09-21] VUA KHUNG GOI TAY. Interceptor chi tu thu nho nhan ma CHINH NO vua dich (VN→EN).
/// Nhan do code gan thang bang <c>Loc.T()</c> / <c>Loc.TF()</c> thi interceptor khong dong den
/// ⇒ chu tieng Anh dai hon van tran khung. Sau moi lan gan .text nhu vay, goi
/// <c>LocFit.Fit(tmp)</c>. Ham khong bao gio nem; nhan chua co rect (LayoutGroup chua tinh) thi bo qua.
/// </summary>
public static class LocFit
{
    /// <summary>
    /// Do lai nhan TMP voi chu HIEN TAI; tran khung thi bat autosize (max = co goc, min = co goc × minRatio)
    /// + Ellipsis neu la nhan mot dong. Vua khung thi tra lai co chu goc. An toan goi nhieu lan.
    /// </summary>
    public static void Fit(TMP_Text t, float minRatio = 0.6f)
    {
        if (t == null) return;
        try { LocRuntimeInterceptor.ApVuaKhung(t, minRatio); }
        catch (System.Exception e) { Debug.LogWarning("[LocFit] bo qua: " + e.Message); }
    }

    /// <summary>
    /// Ban cho <c>UnityEngine.UI.Text</c> legacy (interceptor KHONG quet loai nay): bat BestFit,
    /// co nho nhat = co hien tai × minRatio, cho phep xuong dong theo be ngang.
    /// </summary>
    public static void Fit(UnityEngine.UI.Text t, float minRatio = 0.6f)
    {
        if (t == null) return;
        try
        {
            int co = t.fontSize > 0 ? t.fontSize : 14;
            if (!t.resizeTextForBestFit || t.resizeTextMaxSize < co) t.resizeTextMaxSize = co;
            t.resizeTextMinSize      = Mathf.Max(8, Mathf.RoundToInt(co * Mathf.Clamp(minRatio, 0.3f, 1f)));
            t.resizeTextForBestFit   = true;
            t.horizontalOverflow     = HorizontalWrapMode.Wrap;
        }
        catch (System.Exception e) { Debug.LogWarning("[LocFit] bo qua (legacy Text): " + e.Message); }
    }
}
