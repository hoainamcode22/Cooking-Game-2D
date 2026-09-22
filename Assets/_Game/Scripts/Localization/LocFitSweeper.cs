using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [Localization 2026-09-21] QUET TOAN CUC "chu to khung nho": sau khi scene tai / doi ngon ngu /
/// popup lon vua mo, do TUNG TMP_Text mot lan; chu nao tran khung thi bat autosize qua <see cref="LocFit.Fit"/>.
/// Chi phi MOT LAN moi luot (FindObjectsByType + GetPreferredValues), KHONG chay moi frame.
/// Tat toan bo: <c>LocFitSweeper.Bat = false;</c>
/// </summary>
public static class LocFitSweeper
{
    /// <summary>Cong tac tong. false => Sweep() khong lam gi.</summary>
    public static bool Bat = true;

    /// <summary>Ti le co chu nho nhat so voi co goc (0.6 = duoc bop toi 60%).</summary>
    public static float MinRatio = 0.6f;

    private const float LECH_CHO_PHEP = 2f;

    private static bool _daMoc;
    private static Runner _runner;
    private static bool _dangCho;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiTao()
    {
        if (_daMoc) return;
        _daMoc = true;
        SceneManager.sceneLoaded += KhiSceneVuaTai;
        try { LocalizationManager.OnChanged += KhiDoiNgonNgu; } catch { }
        Sweep();
    }

    private static void KhiSceneVuaTai(Scene scene, LoadSceneMode mode) => Sweep();
    private static void KhiDoiNgonNgu(string lang) => Sweep();

    /// <summary>
    /// Xin quet: doi 1 frame cho layout on dinh roi do. Goi nhieu lan trong cung frame chi quet 1 lan.
    /// </summary>
    public static void Sweep()
    {
        if (!Bat) return;
        if (!Application.isPlaying) return;
        if (_dangCho) return;
        var r = LayRunner();
        if (r == null) return;
        _dangCho = true;
        r.StartCoroutine(CoSweep());
    }

    private static IEnumerator CoSweep()
    {
        yield return null;                 // 1 frame: LayoutGroup / ContentSizeFitter tinh xong
        _dangCho = false;
        SweepNow();
    }

    /// <summary>Quet NGAY (dong bo). Tra ve so TMP da bi siet nho.</summary>
    public static int SweepNow()
    {
        if (!Bat) return 0;
        TMP_Text[] tats;
        try
        {
            tats = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
        catch { return 0; }

        int soFit = 0;
        for (int i = 0; i < tats.Length; i++)
        {
            var t = tats[i];
            if (t == null) continue;
            try
            {
                if (XuLyMot(t)) soFit++;
            }
            catch
            {
                // Cach ly tung nhan: mot nhan hong khong lam dung ca luot.
            }
        }
        return soFit;
    }

    private static bool XuLyMot(TMP_Text t)
    {
        if (t.enableAutoSizing) return false;          // TMP tu lo roi
        if (!t.gameObject.activeInHierarchy) return false;
        if (t.font == null) return false;
        if (t.GetComponentInParent<TMP_InputField>() != null) return false;
        if (LaBoQuaTheoTen(t.transform)) return false;

        string chu = t.text;
        if (string.IsNullOrEmpty(chu)) return false;

        var rt = t.rectTransform;
        if (rt == null) return false;
        Vector2 khung = rt.rect.size;
        if (khung.x <= 1f || khung.y <= 1f) return false;   // chua layout

        Vector2 uocLuong = t.GetPreferredValues(chu, khung.x, 0f);
        bool khongWrap = t.textWrappingMode == TextWrappingModes.NoWrap;

        bool tran = uocLuong.y > khung.y + LECH_CHO_PHEP
                 || (khongWrap && uocLuong.x > khung.x + LECH_CHO_PHEP);
        if (!tran) return false;

        LocFit.Fit(t, MinRatio);
        return true;
    }

    private static bool LaBoQuaTheoTen(Transform tr)
    {
        while (tr != null)
        {
            string n = tr.name;
            if (n.Contains("[NoLoc]") || n.StartsWith("~Loc")) return true;
            tr = tr.parent;
        }
        return false;
    }

    private static Runner LayRunner()
    {
        if (_runner != null) return _runner;
        try
        {
            var go = new GameObject("~LocFitSweeperRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }
        catch { _runner = null; }
        return _runner;
    }

    private sealed class Runner : MonoBehaviour { }
}
