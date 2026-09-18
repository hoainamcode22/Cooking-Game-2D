using UnityEngine;

/// <summary>
/// [PERF P1 — F2] DAT CULLING CHO ANIMATOR LUC CHAY.
///
/// DO DUOC TRONG <c>SCN_Farm.unity</c>: 15 Animator, TAT CA deu <c>m_CullingMode: 0</c>
/// (AlwaysAnimate) — <c>Locomotive</c>, <c>Locomotive2</c>, <c>Wagon_01..Wagon_04</c> (hai bo tau),
/// cong may Animator bam tay tutorial. Doan tau nam NGOAI man hinh gan nhu suot van choi ma van
/// danh gia state machine + ghi transform MOI FRAME.
///
/// KHONG SUA DUOC TRONG SCENE (luat du an: khong dong vao .unity/.prefab), nen sua luc chay.
///
/// ⚠ VI SAO DUNG <c>CullUpdateTransforms</c> CHU KHONG PHAI <c>CullCompletely</c>:
///   • <c>CullUpdateTransforms</c> — ngung danh gia state machine khi ngoai man hinh, NHUNG van
///     cap nhat transform/IK. Ra vao khung hinh muot, khong bi dong bang sai tu the. AN TOAN.
///   • <c>CullCompletely</c>  — ngung SACH. Re hon nhung co the de nhan vat/toa tau ket cung o
///     giua mot tu the roi "nhay" khi tro lai man hinh. KHONG dung.
///
/// ⚠ ANIMATOR KHONG CO RENDERER NAO TRONG CAY CON thi KHONG co bounds de Unity so voi camera;
/// Unity coi no LUON HIEN => dat culling chang tiet kiem gi, chi them rui ro. Bo qua han.
/// Animator tren UI (co Canvas o tren) cung bo qua vi ly do tuong tu.
///
/// LOI RA: dat <see cref="NHAN_BO_QUA"/> vao TEN GameObject (vd "Locomotive [NoCull]")
/// la Animator do duoc giu nguyen AlwaysAnimate.
/// </summary>
public static class AnimatorCullingOptimizer
{
    /// <summary>Cong tac tong. Mac dinh BAT.</summary>
    public static bool Bat = true;

    /// <summary>Ten GameObject chua chuoi nay => khong dong vao.</summary>
    public const string NHAN_BO_QUA = "[NoCull]";

    /// <summary>Quet scene vua nap va dat culling. Goi tu <see cref="PerformanceGuardian"/>.</summary>
    public static void ApDung(string tenScene)
    {
        if (!Bat) return;

        Animator[] tatCa = UnityEngine.Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int doi = 0, boQuaKhongRenderer = 0, boQuaUi = 0, boQuaNhan = 0;

        for (int i = 0; i < tatCa.Length; i++)
        {
            Animator a = tatCa[i];
            if (a == null) continue;
            if (a.cullingMode == AnimatorCullingMode.CullUpdateTransforms) continue;

            if (a.gameObject.name.Contains(NHAN_BO_QUA)) { boQuaNhan++; continue; }

            // UI: Animator duoi Canvas khong co bounds the gioi => cull vo nghia.
            if (a.GetComponentInParent<Canvas>() != null) { boQuaUi++; continue; }

            // Khong co Renderer nao trong cay con => khong co bounds => Unity coi luon hien.
            if (a.GetComponentInChildren<Renderer>(true) == null) { boQuaKhongRenderer++; continue; }

            a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            doi++;
        }

        Debug.Log("[Perf] AnimatorCullingOptimizer (" + tenScene + "): tim thay " + tatCa.Length +
                  " Animator, da doi " + doi + " sang CullUpdateTransforms; bo qua " +
                  boQuaKhongRenderer + " (khong co Renderer), " + boQuaUi + " (UI), " +
                  boQuaNhan + " (co nhan " + NHAN_BO_QUA + ").");
    }
}
