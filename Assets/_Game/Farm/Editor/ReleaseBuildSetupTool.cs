#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// ============================================================================
/// Tools/Map45/21. Cau Hinh Build (Dev nhanh / Release Store)
/// ============================================================================
///
/// VI SAO CO TOOL NAY
/// ------------------
/// Item 20 (GameBuildTool) lo phan BAM NUT BUILD. Tool nay lo phan TRUOC DO:
/// dat dung ~12 o thiet lap trong Player Settings. Hai muc dich dung nhau:
///
///   • DEV NHANH    — Mono + ARMv7, khong strip. Build 2-3 phut, cai thu tren
///                    may that. KHONG nop len Google Play duoc.
///   • RELEASE STORE— IL2CPP + ARM64, AAB, strip Low. Build lau (lan dau
///                    20+ phut) nhung day moi la ban Google Play nhan.
///
/// TOOL NAY KHONG BUILD. No chi ghi Player Settings roi bao cao thay doi.
/// Keystore va package name KHONG BAO GIO bi tool nay dong vao — do la thu
/// mat di la mat luon quyen cap nhat app, phai lam tay.
///
/// GHI CHU KY THUAT
/// ----------------
/// Moi lenh ghi thiet lap deu boc rieng try/catch. Ly do: mot vai API cua
/// PlayerSettings doi ten / doi chu ky giua cac ban Unity. Neu mot cai gay
/// exception ma khong bat, preset se dung giua chung -> project dinh trang
/// thai nua nay nua kia, con te hon la khong bam.
/// Target SDK ep kieu (AndroidSdkVersions)so — KHONG viet AndroidApiLevel35,
/// vi ten thanh vien enum do khac nhau tung ban Unity va se loi CS0117.
/// </summary>
public class ReleaseBuildSetupTool : EditorWindow
{
    // ── Tuy chon nguoi dung chinh duoc (serialize de nho giua cac lan mo) ──
    [SerializeField] private bool releaseKemArmv7 = true;   // ARM64 + ARMv7 de may cu van cai duoc
    [SerializeField] private int  targetSdkInt    = 35;     // ep kieu sang enum, khong goi ten thanh vien

    private Vector2 scroll;
    private string  tomTat = "";          // bang tom tat thay doi cua lan ap dung gan nhat

    // Mau chu cho bang soat
    private static readonly Color MauOk  = new Color(0.20f, 0.65f, 0.20f);
    private static readonly Color MauLoi = new Color(0.80f, 0.20f, 0.15f);
    private static readonly Color MauLuu = new Color(0.75f, 0.55f, 0.05f);

    [MenuItem("Tools/Map45/21. Cau Hinh Build (Dev nhanh / Release Store)", false, 21)]
    public static void Open()
    {
        var w = GetWindow<ReleaseBuildSetupTool>("Cau Hinh Build");
        w.minSize = new Vector2(560f, 520f);
    }

    // =====================================================================
    // DOC THIET LAP HIEN TAI — moi ham doc deu co duong lui, khong nem loi
    // ra ngoai OnGUI (nem loi trong OnGUI se lam cua so nhap nhay lien tuc).
    // =====================================================================
    private static NamedBuildTarget Nbt => NamedBuildTarget.Android;

    private static T DocAnToan<T>(System.Func<T> doc, T macDinh)
    {
        try { return doc(); } catch { return macDinh; }
    }

    private static string DocChuoiAnToan(System.Func<string> doc)
    {
        try { var s = doc(); return string.IsNullOrEmpty(s) ? "(trong)" : s; }
        catch { return "(khong doc duoc)"; }
    }

    // =====================================================================
    // GIAO DIEN
    // =====================================================================
    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Tool này chỉ SỬA Player Settings, không build.\n" +
            "Sau khi chọn preset xong thì sang Tools/Map45/20 để bấm build.",
            MessageType.Info);

        VeBangSoat();
        EditorGUILayout.Space(12f);
        VePhanDevNhanh();
        EditorGUILayout.Space(12f);
        VePhanReleaseStore();
        EditorGUILayout.Space(12f);
        VePhanTomTat();

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    // PHAN 1 — BANG SOAT (chi doc, ve lai moi OnGUI, khong ghi gi het)
    // ─────────────────────────────────────────────────────────────────────
    private void VeBangSoat()
    {
        EditorGUILayout.LabelField("1. BẢNG SOÁT — hiện trạng so với yêu cầu của Google Play",
                                   EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // --- doc het mot luot ---
        var backend   = DocAnToan(() => PlayerSettings.GetScriptingBackend(Nbt),
                                  ScriptingImplementation.Mono2x);
        var kienTruc  = DocAnToan(() => PlayerSettings.Android.targetArchitectures,
                                  AndroidArchitecture.None);
        bool devBuild = DocAnToan(() => EditorUserBuildSettings.development, false);
        int  minSdk   = DocAnToan(() => (int)PlayerSettings.Android.minSdkVersion, 0);
        int  tarSdk   = DocAnToan(() => (int)PlayerSettings.Android.targetSdkVersion, 0);
        var  strip    = DocAnToan(() => PlayerSettings.GetManagedStrippingLevel(Nbt),
                                  ManagedStrippingLevel.Disabled);
        bool stripEng = DocAnToan(() => PlayerSettings.stripEngineCode, false);
        var  il2cpp   = DocAnToan(() => PlayerSettings.GetIl2CppCompilerConfiguration(Nbt),
                                  Il2CppCompilerConfiguration.Debug);
        bool aab      = DocAnToan(() => EditorUserBuildSettings.buildAppBundle, false);
        bool keystore = DocAnToan(() => PlayerSettings.Android.useCustomKeystore, false);
        string ksTen  = DocChuoiAnToan(() => PlayerSettings.Android.keystoreName);
        string ver    = DocChuoiAnToan(() => PlayerSettings.bundleVersion);
        int  verCode  = DocAnToan(() => PlayerSettings.Android.bundleVersionCode, 0);
        string appId  = DocChuoiAnToan(() => PlayerSettings.GetApplicationIdentifier(Nbt));

        // --- cac dieu kien chan ban Store ---
        bool co64  = (kienTruc & AndroidArchitecture.ARM64) != 0;
        bool ilOk  = backend == ScriptingImplementation.IL2CPP;

        VeTieuDeCot();
        Dong("Scripting backend", backend.ToString(), "IL2CPP",
             ilOk ? TrangThai.Ok : TrangThai.Loi,
             ilOk ? "" : "Mono không build được ARM64");
        Dong("Target architectures", kienTruc.ToString(), "có ARM64",
             co64 ? TrangThai.Ok : TrangThai.Loi,
             co64 ? "" : "Play bắt buộc 64-bit");
        Dong("Development build", devBuild ? "BẬT" : "TẮT", "TẮT",
             devBuild ? TrangThai.Loi : TrangThai.Ok,
             devBuild ? "Play từ chối bản development" : "");
        Dong("Min SDK", minSdk.ToString(), "≥ 23",
             minSdk >= 23 ? TrangThai.Ok : TrangThai.Luu, "");
        Dong("Target SDK", tarSdk == 0 ? "0 (Automatic)" : tarSdk.ToString(),
             "ghim ≥ 35", tarSdk >= 35 ? TrangThai.Ok : TrangThai.Luu,
             tarSdk >= 35 ? "" : "Automatic có thể lệch mốc Play bắt buộc");
        Dong("Managed stripping", strip.ToString(), "Low (bản Store)",
             strip == ManagedStrippingLevel.Low ? TrangThai.Ok : TrangThai.Luu, "");
        Dong("Strip engine code", stripEng ? "BẬT" : "TẮT", "BẬT",
             stripEng ? TrangThai.Ok : TrangThai.Luu, "");
        Dong("IL2CPP compiler config", il2cpp.ToString(), "Release",
             il2cpp == Il2CppCompilerConfiguration.Release ? TrangThai.Ok : TrangThai.Luu, "");
        Dong("Định dạng gói", aab ? "AAB (App Bundle)" : "APK", "AAB",
             aab ? TrangThai.Ok : TrangThai.Loi,
             aab ? "" : "Play chỉ nhận .aab cho app mới");
        Dong("Custom keystore", keystore ? "CÓ — " + ksTen : "KHÔNG", "CÓ",
             keystore ? TrangThai.Ok : TrangThai.Loi,
             keystore ? "" : "Bản ký debug không upload được");
        Dong("Bundle version", ver, "tự đặt", TrangThai.Ok, "");
        Dong("Version code", verCode.ToString(), "> lần nộp trước", TrangThai.Luu,
             "Mỗi lần nộp phải lớn hơn lần trước");
        Dong("Package / bundle id", appId, "cố định vĩnh viễn", TrangThai.Ok,
             "Đổi id = thành app khác, mất toàn bộ người dùng");

        // --- hai ket luan bang cau van xuoi ---
        EditorGUILayout.Space(6f);
        bool nopDuoc = ilOk && co64 && !devBuild && aab && keystore;

        var cu = GUI.color;
        GUI.color = nopDuoc ? MauOk : MauLoi;
        EditorGUILayout.LabelField(
            nopDuoc
                ? "KẾT LUẬN 1: Cấu hình hiện tại ĐỦ điều kiện tải lên Google Play."
                : "KẾT LUẬN 1: Cấu hình hiện tại KHÔNG tải lên Google Play được.",
            EditorStyles.boldLabel);
        GUI.color = cu;

        var lyDo = new List<string>();
        if (!ilOk)     lyDo.Add("scripting backend đang là Mono nên không sinh được thư viện 64-bit");
        if (!co64)     lyDo.Add("target architectures chưa có ARM64 (Google Play bắt buộc 64-bit từ 2019)");
        if (devBuild)  lyDo.Add("development build đang bật, Play từ chối gói development");
        if (!aab)      lyDo.Add("đang xuất APK, Play chỉ nhận Android App Bundle (.aab) cho app mới");
        if (!keystore) lyDo.Add("chưa cấu hình custom keystore nên gói sẽ ký bằng khoá debug");

        EditorGUILayout.LabelField(
            lyDo.Count == 0
                ? "KẾT LUẬN 2: Không còn hạng mục nào chặn việc nộp bản build."
                : "KẾT LUẬN 2: Lý do bị chặn — " + string.Join("; ", lyDo) + ".",
            new GUIStyle(EditorStyles.label) { wordWrap = true });

        EditorGUILayout.EndVertical();
    }

    private enum TrangThai { Ok, Luu, Loi }

    private static void VeTieuDeCot()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hạng mục", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
        EditorGUILayout.LabelField("Hiện tại", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
        EditorGUILayout.LabelField("Store cần", EditorStyles.miniBoldLabel, GUILayout.Width(110f));
        EditorGUILayout.LabelField("Ghi chú", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private static void Dong(string ten, string hienTai, string canCo, TrangThai tt, string ghiChu)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ten, GUILayout.Width(160f));

        var cu = GUI.color;
        GUI.color = tt == TrangThai.Ok ? MauOk : (tt == TrangThai.Loi ? MauLoi : MauLuu);
        string dau = tt == TrangThai.Ok ? "OK  " : (tt == TrangThai.Loi ? "LỖI " : "LƯU Ý ");
        EditorGUILayout.LabelField(dau + hienTai, EditorStyles.boldLabel, GUILayout.Width(150f));
        GUI.color = cu;

        EditorGUILayout.LabelField(canCo, GUILayout.Width(110f));
        EditorGUILayout.LabelField(ghiChu, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────────────────
    // PHAN 2 — DEV NHANH
    // ─────────────────────────────────────────────────────────────────────
    private void VePhanDevNhanh()
    {
        EditorGUILayout.LabelField("2. DEV NHANH — cấu hình build thử hằng ngày", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Mono + ARMv7 + APK + không strip. Build khoảng 2-3 phút.\n" +
            "Không đụng tới keystore, package name, version code.",
            new GUIStyle(EditorStyles.label) { wordWrap = true });

        GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        if (GUILayout.Button("DEV NHANH — áp dụng cấu hình build thử", GUILayout.Height(34f)))
        {
            if (EditorUtility.DisplayDialog(
                    "Áp dụng preset DEV NHANH?",
                    "Sẽ ghi đè Player Settings của cả project:\n\n" +
                    "  • Scripting backend  → Mono2x\n" +
                    "  • Architectures      → ARMv7\n" +
                    "  • Development build  → TẮT\n" +
                    "  • Build App Bundle   → TẮT (xuất APK)\n" +
                    "  • Managed stripping  → Disabled\n" +
                    "  • Strip engine code  → TẮT\n\n" +
                    "KHÔNG đụng tới keystore, package name, version code.\n\n" +
                    "Bản này KHÔNG nộp Google Play được.",
                    "Áp dụng", "Huỷ"))
            {
                ApDungDevNhanh();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void ApDungDevNhanh()
    {
        var log = new List<string>();

        Ghi(log, "Scripting backend",
            () => PlayerSettings.GetScriptingBackend(Nbt).ToString(),
            () => PlayerSettings.SetScriptingBackend(Nbt, ScriptingImplementation.Mono2x));

        Ghi(log, "Target architectures",
            () => PlayerSettings.Android.targetArchitectures.ToString(),
            () => PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7);

        Ghi(log, "Development build",
            () => EditorUserBuildSettings.development.ToString(),
            () => EditorUserBuildSettings.development = false);

        Ghi(log, "Build App Bundle (AAB)",
            () => EditorUserBuildSettings.buildAppBundle.ToString(),
            () => EditorUserBuildSettings.buildAppBundle = false);

        Ghi(log, "Managed stripping level",
            () => PlayerSettings.GetManagedStrippingLevel(Nbt).ToString(),
            () => PlayerSettings.SetManagedStrippingLevel(Nbt, ManagedStrippingLevel.Disabled));

        // Strip engine code tat luon: bat len thi build cham va de mat code
        // bi goi qua reflection — dev thu khong can tiet kiem dung luong.
        Ghi(log, "Strip engine code",
            () => PlayerSettings.stripEngineCode.ToString(),
            () => PlayerSettings.stripEngineCode = false);

        KetThuc("DEV NHANH", log);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PHAN 3 — RELEASE STORE
    // ─────────────────────────────────────────────────────────────────────
    private void VePhanReleaseStore()
    {
        EditorGUILayout.LabelField("3. RELEASE STORE — cấu hình nộp Google Play", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "CẢNH BÁO: IL2CPP build LÂU HƠN RẤT NHIỀU so với Mono.\n" +
            "Máy phải cài sẵn Android SDK + NDK + JDK qua Unity Hub\n" +
            "(Installs → bánh răng → Add modules → Android Build Support,\n" +
            " tick cả OpenJDK và Android SDK & NDK Tools).\n" +
            "Lần build IL2CPP đầu tiên có thể mất 20+ phút — đó là bình thường,\n" +
            "đừng tắt Unity giữa chừng.",
            MessageType.Warning);

        releaseKemArmv7 = EditorGUILayout.ToggleLeft(
            "Kèm cả ARMv7 cùng ARM64 (bật: máy cũ 32-bit vẫn cài được, gói nặng hơn)",
            releaseKemArmv7);

        targetSdkInt = EditorGUILayout.IntField(
            new GUIContent("Target SDK (số API)",
                           "Ghi thẳng con số, tool ép kiểu sang AndroidSdkVersions."),
            targetSdkInt);
        if (targetSdkInt < 23) targetSdkInt = 23;

        EditorGUILayout.LabelField(
            "Không đụng tới keystore và package name. Không tự tăng version code.",
            EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(1f, 0.75f, 0.55f);
        if (GUILayout.Button("RELEASE STORE — áp dụng cấu hình nộp Play", GUILayout.Height(34f)))
        {
            if (EditorUtility.DisplayDialog(
                    "Áp dụng preset RELEASE STORE?",
                    "Sẽ ghi đè Player Settings của cả project:\n\n" +
                    "  • Scripting backend  → IL2CPP\n" +
                    "  • Architectures      → " + (releaseKemArmv7 ? "ARM64 + ARMv7" : "ARM64") + "\n" +
                    "  • Development build  → TẮT\n" +
                    "  • Build App Bundle   → BẬT (xuất .aab)\n" +
                    "  • IL2CPP config      → Release\n" +
                    "  • Managed stripping  → Low\n" +
                    "  • Strip engine code  → BẬT\n" +
                    "  • Target SDK         → " + targetSdkInt + "\n\n" +
                    "KHÔNG đụng keystore, package name; KHÔNG tự tăng version code.\n\n" +
                    "Build sau đó sẽ lâu hơn nhiều.",
                    "Áp dụng", "Huỷ"))
            {
                ApDungReleaseStore();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Tăng Version Code +1 (bấm thủ công trước mỗi lần nộp)",
                             GUILayout.Height(24f)))
        {
            TangVersionCode();
        }

        EditorGUILayout.EndVertical();
    }

    private void ApDungReleaseStore()
    {
        var log = new List<string>();

        Ghi(log, "Scripting backend",
            () => PlayerSettings.GetScriptingBackend(Nbt).ToString(),
            () => PlayerSettings.SetScriptingBackend(Nbt, ScriptingImplementation.IL2CPP));

        var kt = releaseKemArmv7
               ? (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7)
               : AndroidArchitecture.ARM64;
        Ghi(log, "Target architectures",
            () => PlayerSettings.Android.targetArchitectures.ToString(),
            () => PlayerSettings.Android.targetArchitectures = kt);

        Ghi(log, "Development build",
            () => EditorUserBuildSettings.development.ToString(),
            () => EditorUserBuildSettings.development = false);

        Ghi(log, "Build App Bundle (AAB)",
            () => EditorUserBuildSettings.buildAppBundle.ToString(),
            () => EditorUserBuildSettings.buildAppBundle = true);

        Ghi(log, "IL2CPP compiler configuration",
            () => PlayerSettings.GetIl2CppCompilerConfiguration(Nbt).ToString(),
            () => PlayerSettings.SetIl2CppCompilerConfiguration(Nbt, Il2CppCompilerConfiguration.Release));

        Ghi(log, "Managed stripping level",
            () => PlayerSettings.GetManagedStrippingLevel(Nbt).ToString(),
            () => PlayerSettings.SetManagedStrippingLevel(Nbt, ManagedStrippingLevel.Low));

        Ghi(log, "Strip engine code",
            () => PlayerSettings.stripEngineCode.ToString(),
            () => PlayerSettings.stripEngineCode = true);

        // ÉP KIEU chu KHONG goi ten thanh vien enum: ten AndroidApiLevelXX doi
        // theo tung ban Unity, viet thang ten se loi bien dich CS0117.
        int sdk = targetSdkInt;
        Ghi(log, "Target SDK",
            () => ((int)PlayerSettings.Android.targetSdkVersion).ToString(),
            () => PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)sdk);

        KetThuc("RELEASE STORE", log);
    }

    private void TangVersionCode()
    {
        int cu = DocAnToan(() => PlayerSettings.Android.bundleVersionCode, -1);
        if (cu < 0)
        {
            tomTat = "Không đọc được bundleVersionCode.";
            Debug.LogWarning("[CauHinhBuild] Khong doc duoc bundleVersionCode.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Tăng Version Code?",
                "Version code: " + cu + " → " + (cu + 1) +
                "\n\nMỗi bản nộp lên Play phải có version code lớn hơn bản trước.",
                "Tăng", "Huỷ"))
            return;

        var log = new List<string>();
        Ghi(log, "Android bundleVersionCode",
            () => PlayerSettings.Android.bundleVersionCode.ToString(),
            () => PlayerSettings.Android.bundleVersionCode = cu + 1);
        KetThuc("TANG VERSION CODE", log);
    }

    // =====================================================================
    // HA TANG GHI THIET LAP
    // Moi setting mot try/catch rieng: mot API khong ho tro tren ban Unity
    // nay chi lam hong DUNG dong do, cac dong con lai van chay het.
    // =====================================================================
    private static void Ghi(List<string> log, string ten,
                            System.Func<string> docGiaTri, System.Action ghiGiaTri)
    {
        string truoc;
        try { truoc = docGiaTri(); }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CauHinhBuild] Khong DOC duoc '" + ten + "' — bo qua. " + e.Message);
            log.Add("BO QUA  " + ten + " (khong doc duoc)");
            return;
        }

        try
        {
            ghiGiaTri();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CauHinhBuild] Khong GHI duoc '" + ten + "' — bo qua, cac muc khac van ap dung. "
                             + e.Message);
            log.Add("LOI     " + ten + " (khong ghi duoc: " + e.GetType().Name + ")");
            return;
        }

        string sau;
        try { sau = docGiaTri(); }
        catch { sau = "(khong doc lai duoc)"; }

        log.Add(truoc == sau
            ? "giu nguyen " + ten + ": " + sau
            : "DOI     " + ten + ": " + truoc + "  ->  " + sau);
    }

    private void KetThuc(string tenPreset, List<string> log)
    {
        try { AssetDatabase.SaveAssets(); }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CauHinhBuild] SaveAssets that bai: " + e.Message);
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== " + tenPreset + " — tom tat thay doi ===");
        foreach (var d in log) sb.AppendLine("  " + d);
        sb.AppendLine("(Da goi AssetDatabase.SaveAssets)");

        tomTat = sb.ToString();
        Debug.Log("[CauHinhBuild]\n" + tomTat);
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────
    private void VePhanTomTat()
    {
        EditorGUILayout.LabelField("Tóm tắt lần áp dụng gần nhất", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            string.IsNullOrEmpty(tomTat) ? "(chưa áp dụng preset nào trong phiên này)" : tomTat,
            new GUIStyle(EditorStyles.label) { wordWrap = true, richText = false });
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "VIỆC PHẢI LÀM TAY (tool cố ý không tự động):\n" +
            "  1. Keystore: Player Settings → Android → Publishing Settings →\n" +
            "     tick Custom Keystore → Keystore Manager → tạo file .keystore,\n" +
            "     đặt mật khẩu, tạo alias. LƯU FILE + MẬT KHẨU RA CHỖ AN TOÀN.\n" +
            "     Mất keystore = vĩnh viễn không cập nhật được app trên Play.\n" +
            "  2. Package name (com.xxx.yyy): đặt một lần, không đổi về sau.\n" +
            "  3. Bấm 'Tăng Version Code +1' trước mỗi lần nộp bản mới.",
            MessageType.Info);
    }
}
#endif
