using UnityEngine;

/// <summary>
/// [PERF P0] KHOI TAO HIEU NANG TOAN GAME — chay TRUOC khi scene dau tien duoc nap.
///
/// VI SAO CAN: truoc file nay, du an KHONG he dat <c>Application.targetFrameRate</c> o bat ky dau.
/// Tren Android/iOS, Unity mac dinh <c>targetFrameRate = 30</c> => game bi khoa 30fps du may thua
/// suc chay 60fps, va cam giac keo map / cuon list luc nao cung "nang tay".
///
/// LAM 3 VIEC, dung 1 lan, khong ton them GameObject nao:
///   ① <c>Application.targetFrameRate = 60</c> — muc tieu 60fps.
///   ② <c>QualitySettings.vSyncCount = 0</c>   — BAT BUOC: khi vSyncCount &gt; 0 thi Unity BO QUA
///      targetFrameRate hoan toan (nhip do man hinh quyet dinh). Tren mobile vSync khong co y
///      nghia nen tat di la an toan.
///   ③ <c>Screen.sleepTimeout = NeverSleep</c> — khong tat man hinh giua luc dang choi.
///
/// KHONG dung MonoBehaviour: <c>[RuntimeInitializeOnLoadMethod]</c> chay o moi build va moi scene
/// dau vao, nen khong phai keo component vao scene nao (va khong the quen keo).
///
/// Muon doi so: sua hang so <see cref="FPS_MUC_TIEU"/> roi build lai.
/// </summary>
public static class MobilePerformanceBootstrap
{
    /// <summary>FPS muc tieu. 60 hop cho ca mobile lan Editor/PC.</summary>
    public const int FPS_MUC_TIEU = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void KhoiTao()
    {
        // vSync phai TAT truoc, neu khong targetFrameRate bi bo qua.
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = FPS_MUC_TIEU;

        // Chi co y nghia tren thiet bi cam tay; tren PC la no-op vo hai.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("[Perf] MobilePerformanceBootstrap: targetFrameRate=" + FPS_MUC_TIEU +
                  ", vSyncCount=0, sleepTimeout=NeverSleep");

        TaoBoCanh();
    }

    /// <summary>
    /// [F1 2026-09-17] Dat mot lan la KHONG DU. QualitySettings.asset khai tier Android
    /// (index 2 "Medium") voi vSyncCount: 1; bat ky ai goi QualitySettings.SetQualityLevel(...)
    /// ve sau deu ap lai tier do => vSyncCount quay ve 1 => tren Unity 6 Android
    /// Application.targetFrameRate BI BO QUA va tran 60fps chet am tham.
    ///
    /// Vi vay tao them mot GameObject DontDestroyOnLoad mang <see cref="PerformanceGuardian"/>:
    /// no doc lai MOT so nguyen moi giay va sau moi lan sceneLoaded, va tra vSync ve 0 neu bi doi.
    /// GameObject nay cung la noi moc AnimatorCullingOptimizer (F2) + TilemapRenderModeOptimizer (F3).
    /// </summary>
    private static void TaoBoCanh()
    {
        if (PerformanceGuardian.Instance != null) return;

        var go = new GameObject("~PerformanceGuardian");
        go.AddComponent<PerformanceGuardian>();   // tu goi DontDestroyOnLoad trong Awake.
    }
}
