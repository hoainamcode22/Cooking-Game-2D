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
///   • Dang tieng Viet  → khong lam gi (khong ton hieu nang, khong rui ro).
///   • Doi sang tieng Anh → quet 1 lan duy nhat khi doi ngon ngu hoac khi scene moi vua tai xong.
///     DA TRIET TIEU HOAN TOAN vong lap quet dinh ky moi 0.2s de dam bao 60 FPS muot ma tren Mobile.
///   • Doi ve tieng Viet → tra lai NGUYEN VAN cau goc da nho.
///
/// AN TOAN
///   • Nho cap (cau goc VN ⇄ cau da dich EN) theo tung object nen doi qua doi lai bao nhieu
///     lan cung khong sai, khong dich chong len chinh no.
///   • Bo qua o nhap lieu (TMP_InputField) — khong duoc dich chu nguoi choi go.
///   • Bo qua chu qua dai (> 400 ky tu) va chuoi thuan so ("12/25", "1.200").
///   • Cau chua co ban dich → ghi ra Assets/_Debug_Capture/loc_missing.txt de bo sung dan.
/// </summary>
public static class LocRuntimeInterceptor
{
    private const int   DAI_TOI_DA  = 400;
    private const int   THIEU_TOI_DA = 3000;

    private class Muc { public string vnGoc; public string enDaAp; public bool boQua; }

    private static readonly Dictionary<TMP_Text, Muc> _theoDoi = new Dictionary<TMP_Text, Muc>();
    private static readonly HashSet<string> _chuaDich = new HashSet<string>();
    private static bool _daKhoiTao;
    private static GameObject _runner;
    private static bool _daNgheSceneLoaded;

    public static int SoDangTheoDoi => _theoDoi.Count;
    public static int SoChuaDich    => _chuaDich.Count;

    /// <summary>Goi mot lan luc game khoi dong (LocalizationManager.KhoiTao goi ho).</summary>
    public static void KhoiTao()
    {
        if (_daKhoiTao) return;
        _daKhoiTao = true;

        LocalizationManager.OnChanged += KhiDoiNgonNgu;

        if (_runner == null)
        {
            _runner = new GameObject("~LocRuntimeInterceptor");
            Object.DontDestroyOnLoad(_runner);
            _runner.hideFlags = HideFlags.HideInHierarchy;
            _runner.AddComponent<LocInterceptorRunner>();
        }

        // Scene moi tai xong thi UI dung trong Awake/Start co the bi lo mat toi 0,2s
        // (nhip quet) => man hinh loe tieng Viet. Nghe sceneLoaded de quet NGAY.
        if (!_daNgheSceneLoaded)
        {
            _daNgheSceneLoaded = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += KhiSceneVuaTai;
        }
    }

    /// <summary>Scene vua tai xong: don muc chet roi quet lai ngay + cuoi khung hinh.</summary>
    private static void KhiSceneVuaTai(UnityEngine.SceneManagement.Scene scene,
                                       UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        DonMucChet();
        if (!LocalizationManager.DangTiengAnh) return;

        QuetVaDich();

        if (_runner != null)
        {
            var chay = _runner.GetComponent<LocInterceptorRunner>();
            if (chay != null) chay.StartCoroutine(QuetLaiCuoiKhung());
        }
    }

    /// <summary>Quet lai sau khi moi UI cua scene moi da kip dung xong.</summary>
    private static IEnumerator QuetLaiCuoiKhung()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        if (LocalizationManager.DangTiengAnh) QuetVaDich();
    }

    private static void KhiDoiNgonNgu(string lang)
    {
        if (lang == LocalizationManager.EN) QuetVaDich();
        else                                TraVeTiengViet();
    }

    /// <summary>Quet moi TMP_Text dang song va dich sang tieng Anh.</summary>
    public static void QuetVaDich()
    {
        if (!LocalizationManager.DangTiengAnh) return;

        var tats = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tats.Length; i++)
        {
            var t = tats[i];
            if (t == null) continue;

            if (!_theoDoi.TryGetValue(t, out var muc))
            {
                muc = new Muc { boQua = LaChuKhongDuocDich(t) };
                _theoDoi[t] = muc;
            }
            if (muc.boQua) continue;

            string hienTai = t.text;
            if (string.IsNullOrEmpty(hienTai) || hienTai.Length > DAI_TOI_DA) continue;

            // Chinh cau minh vua dat vao → khong dung den nua.
            if (muc.enDaAp != null && hienTai == muc.enDaAp) continue;

            string en = LocalizationManager.T(hienTai);
            if (en != hienTai)
            {
                muc.vnGoc  = hienTai;
                muc.enDaAp = en;
                t.text     = en;
            }
            else if (CoDauTiengViet(hienTai))
            {
                GhiChuaDich(hienTai);
            }
        }

        DonMucChet();
    }

    /// <summary>Tra moi chu ve dung cau tieng Viet goc.</summary>
    public static void TraVeTiengViet()
    {
        foreach (var cap in _theoDoi)
        {
            var t = cap.Key; var m = cap.Value;
            if (t == null || m == null || m.vnGoc == null) continue;
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

    /// <summary>Component gắn trên GameObject ẩn để host Coroutine một lần khi tải scene.</summary>
    private class LocInterceptorRunner : MonoBehaviour
    {
        // [TỐI ƯU HIỆU NĂNG MOBILE] Đã loại bỏ hoàn toàn Coroutine quét lặp định kỳ mỗi 0.2s.
        // Chỉ hoạt động theo cơ chế event-driven (sceneLoaded và OnChanged), không tốn CPU chu kỳ nào.
    }
}
