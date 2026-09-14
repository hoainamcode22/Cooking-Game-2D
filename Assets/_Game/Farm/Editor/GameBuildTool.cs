#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// ============================================================================
/// Tools/Map45/20. Build Game (APK cho Android + WebGL cho moi dien thoai)
/// ============================================================================
///
/// VI SAO CO TOOL NAY
/// ------------------
/// Build tay phai nho ~12 o thiet lap rai rac trong Player Settings, quen mot o
/// la build hong hoac ra file khong cai duoc. Tool nay dat het mot lan roi build.
///
/// HAI BAN, HAI MUC DICH KHAC NHAU
/// -------------------------------
///   • APK    — chi Android. Copy vao may, bam cai, chay muot nhat.
///   • WebGL  — up len itch.io roi gui LINK. iPhone lan Android deu mo bang
///              trinh duyet, khong phai cai gi. Day la cach DUY NHAT de nguoi
///              dung iPhone choi thu duoc, vi build iOS bat buoc phai co may Mac.
///
/// TOOL NAY KHONG SUA FILE NAO TU BEN NGOAI UNITY.
/// No goi thang API PlayerSettings nen khong dinh bay "processed bytes does not
/// match file size" tung lam mat TouristFerry.
/// </summary>
public class GameBuildTool : EditorWindow
{
    // ── Thong tin game ───────────────────────────────────────────────────
    private string productName = "Nong Trai Vui Ve";
    private string companyName = "Astronex";
    private string packageId   = "com.astronex.cookinggame";
    private string version     = "1.0";

    // ── Tuy chon Android ─────────────────────────────────────────────────
    private bool nhanhHon = false;      // Mono + ARMv7: build 3 phut thay vi 25
    private bool developmentBuild = false;

    // ── Scene ────────────────────────────────────────────────────────────
    // 🔴 VONG 19 — BO DANH SACH "SCENE RAC" TU DOAN.
    // Ban truoc hard-code bo SampleScene vi tuong do la scene mac dinh cua Unity.
    // Sai: do la scene chuc nang cua Sep. Khong duoc tu quyet dinh thay Sep nua —
    // gio liet ke HET scene ra cho Sep tu tick.
    private readonly Dictionary<string, bool> sceneChon = new Dictionary<string, bool>();
    private bool daNapScene;

    private string status = "";
    private Vector2 scroll;

    [MenuItem("Tools/Map45/20. Build Game (APK + WebGL)", false, 20)]
    public static void Open() => GetWindow<GameBuildTool>("Build Game");

    // ─────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "APK   = chi may Android, cai thang vao may.\n" +
            "WebGL = up len itch.io roi gui link — iPhone va Android deu mo duoc\n" +
            "        bang trinh duyet, khong can cai. Khong build duoc iOS tren\n" +
            "        Windows: Apple bat buoc phai co may Mac + Xcode.",
            MessageType.Info);

        // ── Trang thai module: nhin phat biet ngay cai nao thieu ─────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Module da cai", EditorStyles.boldLabel);
        bool coAndroid = CoModule(BuildTarget.Android, BuildTargetGroup.Android);
        bool coWebGL   = CoModule(BuildTarget.WebGL,   BuildTargetGroup.WebGL);
        VeDongModule("Android Build Support", coAndroid);
        VeDongModule("WebGL Build Support",   coWebGL);
        EditorGUILayout.LabelField(
            $"   Nen tang dang bat: {EditorUserBuildSettings.activeBuildTarget}",
            EditorStyles.miniLabel);

        if (!coAndroid || !coWebGL)
            EditorGUILayout.HelpBox(
                "Module con thieu. Unity Hub > Installs > banh rang o ban 6000.3.10f1 >\n" +
                "Add modules. Android can them: OpenJDK va Android SDK & NDK Tools.\n" +
                "Cai xong phai TAT Unity mo lai thi moi nhan.",
                MessageType.Warning);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Thong tin game", EditorStyles.boldLabel);
        productName = EditorGUILayout.TextField("Ten hien tren may", productName);
        companyName = EditorGUILayout.TextField("Ten cong ty", companyName);
        packageId   = EditorGUILayout.TextField("Package name", packageId);
        version     = EditorGUILayout.TextField("Phien ban", version);

        if (!PackageIdHopLe(packageId))
            EditorGUILayout.HelpBox(
                "Package name phai co dang com.tencongty.tengame — chi chu thuong, so va " +
                "dau cham, moi doan phai bat dau bang chu cai, khong dau, khong khoang trang.",
                MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene dua vao ban build", EditorStyles.boldLabel);
        NapSceneNeuCan();

        var duongDanScene = new List<string>(sceneChon.Keys);
        for (int i = 0; i < duongDanScene.Count; i++)
        {
            string sp = duongDanScene[i];
            string nhan = Path.GetFileNameWithoutExtension(sp);
            if (i == 0) nhan += "   <- scene MO DAU khi bat game";
            sceneChon[sp] = EditorGUILayout.ToggleLeft("   " + nhan, sceneChon[sp]);
        }
        EditorGUILayout.LabelField(
            "   Thu tu lay theo File > Build Profiles. Scene tren cung la scene mo dau.",
            EditorStyles.miniLabel);

        // ── ANDROID ──────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Ban APK cho Android", EditorStyles.boldLabel);

        nhanhHon = EditorGUILayout.ToggleLeft(
            "Build nhanh de test (Mono + ARMv7, ~3 phut thay vi ~25 phut)", nhanhHon);
        if (nhanhHon)
            EditorGUILayout.HelpBox(
                "Ban nhanh KHONG cai duoc tren vai may doi rat moi chi chay 64-bit " +
                "(Pixel 7 tro len...). May bao 'khong cai duoc app' thi bo tick nay build lai.",
                MessageType.Warning);

        developmentBuild = EditorGUILayout.ToggleLeft(
            "Development Build (hien log loi ngay tren man hinh dien thoai)", developmentBuild);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        // KHONG goi BuildPlayer thang trong OnGUI: build chiem luon luong ve, chong
        // layout cua IMGUI bi vo -> "EndLayoutGroup: BeginLayoutGroup must be called first".
        // delayCall cho build chay SAU khi ve xong khung hinh nay.
        if (GUILayout.Button("BUILD APK", GUILayout.Height(34)))
            EditorApplication.delayCall += BuildAndroid;
        GUI.backgroundColor = Color.white;

        // ── WEBGL ────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Ban WebGL cho moi dien thoai", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Build xong duoc mot THU MUC. Nen ca thu muc thanh .zip roi keo len itch.io.",
            EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
        if (GUILayout.Button("BUILD WEBGL", GUILayout.Height(34)))
            EditorApplication.delayCall += BuildWebGL;
        GUI.backgroundColor = Color.white;

        // ── Tien ich ─────────────────────────────────────────────────────
        EditorGUILayout.Space();
        if (GUILayout.Button("Chi dat thiet lap, khong build"))
        {
            ApDungThietLapChung();
            status = "Da dat xong thiet lap. Mo Player Settings ra xem lai neu muon.";
        }
        if (GUILayout.Button("Mo thu muc Build"))
        {
            string d = ThuMucBuild();
            Directory.CreateDirectory(d);
            EditorUtility.RevealInFinder(d);
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    // THIET LAP
    // ─────────────────────────────────────────────────────────────────────
    private static bool PackageIdHopLe(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        var doan = id.Split('.');
        if (doan.Length < 2) return false;
        foreach (var d in doan)
        {
            if (d.Length == 0) return false;
            if (!char.IsLetter(d[0])) return false;
            foreach (char c in d)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }

    private static void VeDongModule(string ten, bool coRoi)
    {
        var mau = GUI.color;
        GUI.color = coRoi ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.55f, 0.5f);
        EditorGUILayout.LabelField($"   {(coRoi ? "[co]" : "[THIEU]")}  {ten}");
        GUI.color = mau;
    }

    private static string ThuMucBuild()
        => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build");

    /// <summary>Doc danh sach scene tu Build Profiles, mac dinh tick het cai dang bat.</summary>
    private void NapSceneNeuCan()
    {
        if (daNapScene && sceneChon.Count > 0) return;
        sceneChon.Clear();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(s.path)) continue;
            if (!File.Exists(s.path)) continue;              // scene da bi xoa
            sceneChon[s.path] = s.enabled;
        }
        daNapScene = true;
    }

    private string[] LayDanhSachScene()
    {
        NapSceneNeuCan();
        var list = new List<string>();
        foreach (var kv in sceneChon) if (kv.Value) list.Add(kv.Key);
        return list.ToArray();
    }

    /// <summary>Thiet lap dung cho ca hai ban build.</summary>
    private void ApDungThietLapChung()
    {
        PlayerSettings.productName  = productName;
        PlayerSettings.companyName  = companyName;
        PlayerSettings.bundleVersion = version;

        // Game nay ve theo chieu ngang. De Auto Rotation thi cam doc se vo bo cuc.
        PlayerSettings.defaultInterfaceOrientation      = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait      = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft  = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        AssetDatabase.SaveAssets();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUILD ANDROID
    // ─────────────────────────────────────────────────────────────────────
    private void BuildAndroid()
    {
        if (!PackageIdHopLe(packageId))
        {
            status = "Package name chua hop le — sua roi build lai.";
            return;
        }

        ApDungThietLapChung();

        var nbt = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(nbt, packageId);

        // ARM64 + IL2CPP la cau hinh chuan, cai duoc moi may doi moi.
        // Mono chi chay ARMv7 — nhanh hon nhieu nhung may 64-bit-only se tu choi cai.
        if (nhanhHon)
        {
            PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        }
        else
        {
            PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        // Unity 6.3 da BO AndroidApiLevel24 khoi enum (san thap nhat gio la 25 = Android 7.1).
        // Viet 24 vao day la loi CS0117, hong ca Assembly-CSharp-Editor.
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode++;      // moi ban tang 1, de cai de len ban cu

        // APK mot file, KHONG phai .aab (aab chi dung de nop len Google Play,
        // khong cai thang vao may duoc).
        EditorUserBuildSettings.buildAppBundle = false;

        string thuMuc = Path.Combine(ThuMucBuild(), "Android");
        Directory.CreateDirectory(thuMuc);
        string file = Path.Combine(thuMuc, DatTenFile() + ".apk");

        var opt = new BuildPlayerOptions
        {
            scenes           = LayDanhSachScene(),
            locationPathName = file,
            target           = BuildTarget.Android,
            targetGroup      = BuildTargetGroup.Android,
            options          = developmentBuild ? BuildOptions.Development : BuildOptions.None
        };

        ChayBuild(opt, "APK", file, nbt);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BUILD WEBGL
    // ─────────────────────────────────────────────────────────────────────
    private void BuildWebGL()
    {
        ApDungThietLapChung();

        var nbt = NamedBuildTarget.WebGL;
        PlayerSettings.SetApplicationIdentifier(nbt, packageId);

        // Gzip la chuan nhat tren itch.io va tat ca trinh duyet di dong.
        // decompressionFallback = false de trinh duyet (C++) tu giai nen tren luong mang,
        // TUYET DOI KHONG dung JS decompressor de tranh nhan doi RAM gay crash 90% tren iOS/Safari/Messenger.
        PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching           = false;
        PlayerSettings.WebGL.initialMemorySize     = 64;

        // Tat bat loi ngoai le -> ban build nho hon va chay nhanh hon han tren
        // dien thoai. Khi nao can soi loi thi doi lai ExplicitlyThrownExceptionsOnly.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

        // Cat bot code khong dung — quan trong voi mobile vi Safari gioi han RAM.
        PlayerSettings.SetManagedStrippingLevel(nbt, ManagedStrippingLevel.High);

        string thuMuc = Path.Combine(ThuMucBuild(), "WebGL");
        Directory.CreateDirectory(thuMuc);

        var opt = new BuildPlayerOptions
        {
            scenes           = LayDanhSachScene(),
            locationPathName = thuMuc,
            target           = BuildTarget.WebGL,
            targetGroup      = BuildTargetGroup.WebGL,
            options          = BuildOptions.None
        };

        ChayBuild(opt, "WebGL", thuMuc, nbt);
    }

    // ─────────────────────────────────────────────────────────────────────
    private string DatTenFile()
    {
        string s = productName.ToLowerInvariant().Replace(' ', '_');
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
        string ten = sb.ToString();
        return string.IsNullOrEmpty(ten) ? "game" : ten;
    }

    /// <summary>
    /// Module nen tang da duoc cai chua. Kiem tra TRUOC khi build de bao dung benh:
    /// BuildPipeline chi nem ra "build target was unsupported" rat kho hieu.
    /// </summary>
    private static bool CoModule(BuildTarget target, BuildTargetGroup group)
        => BuildPipeline.IsBuildTargetSupported(group, target);

    /// <summary>
    /// File / thu muc ket qua co THAT SU ra doi khong.
    ///
    /// 🔴 VONG 18 — KHONG DUOC TIN MOI BuildReport.
    /// Da gap that: BuildPlayer chay 17,5 phut, nem UnityException "Build target
    /// 'WebGL' not supported" o buoc Postprocess, NHUNG summary.result van tra ve
    /// Succeeded va tool bao "XONG 612,9 MB" trong khi thu muc RONG TUENH.
    /// Nen phai so file that truoc khi dam bao voi Sep la xong.
    /// </summary>
    private static bool CoKetQua(BuildTarget target, string duongDan)
    {
        if (target == BuildTarget.Android)
            return File.Exists(duongDan) && new FileInfo(duongDan).Length > 1024;

        return Directory.Exists(duongDan) &&
               File.Exists(Path.Combine(duongDan, "index.html"));
    }

    private void ChayBuild(BuildPlayerOptions opt, string ten, string duongDan, NamedBuildTarget nbt)
    {
        if (opt.scenes == null || opt.scenes.Length == 0)
        {
            status = "Khong co scene nao trong Build Settings. Mo File > Build Profiles de them.";
            return;
        }

        if (!CoModule(opt.target, opt.targetGroup))
        {
            status =
                $"CHUA CAI MODULE {ten.ToUpperInvariant()} — day la ly do build hong,\n" +
                "khong phai loi code.\n\n" +
                "Unity Hub > Installs > banh rang o ban 6000.3.10f1 > Add modules,\n" +
                (opt.target == BuildTarget.Android
                    ? "tick: Android Build Support + OpenJDK + Android SDK & NDK Tools."
                    : "tick: WebGL Build Support.") + "\n\n" +
                "NEU KHONG THAY nut 'Add modules': ban Unity nay duoc them vao Hub bang\n" +
                "'Locate existing installation' nen Hub khong quan ly duoc. Cach chua:\n" +
                "go ban do khoi Hub roi cai lai bang Hub (Installs > Install Editor >\n" +
                "Archive), luc cai tick san cac module tren. Du an khong bi anh huong.";
            Debug.LogError($"[Build] {status}");
            Repaint();
            return;
        }

        // ── PHAI CHUYEN NEN TANG TRUOC KHI BUILD ────────────────────────
        // 🔴 VONG 18 — day la ly do that cua "Build target 'WebGL' not supported".
        // Module da cai (IsBuildTargetSupported tra true) nhung nen tang dang bat
        // van la Windows. BuildPlayer dung du lieu dung nen tang dich, den buoc
        // Postprocess moi goi module cua nen tang DANG BAT -> khong khop -> nem loi,
        // ma bao cao van ghi Succeeded. Chuyen nen tang truoc la het.
        if (EditorUserBuildSettings.activeBuildTarget != opt.target)
        {
            Debug.Log($"[Build] Dang chuyen nen tang sang {ten}...");
            bool doiDuoc = EditorUserBuildSettings.SwitchActiveBuildTarget(nbt, opt.target);

            if (!doiDuoc)
            {
                status = $"KHONG CHUYEN DUOC SANG {ten.ToUpperInvariant()}.\n" +
                         "Module bao la co nhung Unity khong nap duoc.\n" +
                         "Tat Unity, mo lai, roi thu lai. Van hong thi cai lai module trong Hub.";
                Debug.LogError($"[Build] {status}");
                Repaint();
                return;
            }

            // 🔴 VONG 19 — TRUOC DAY CHO `return` O DAY. DO LA LOI.
            // SwitchActiveBuildTarget la ham DONG BO: no chi tra ve sau khi da nen
            // xong toan bo anh cho nen tang moi. Nen cho return la thua, ma con hai:
            // Sep bam APK -> doi sang Android roi thoat; bam WebGL -> doi sang WebGL
            // roi thoat; bam APK lai -> doi ve Android roi thoat... quay vong mai ma
            // KHONG LAN NAO BUILD THAT. Do la ly do thu muc Build luon rong.
            // Doi xong thi chay tiep xuong build luon.
            Debug.Log($"[Build] Doi nen tang xong, build tiep.");
        }

        Debug.Log($"[Build] Bat dau build {ten} -> {duongDan}");
        BuildReport report = BuildPipeline.BuildPlayer(opt);
        BuildSummary s = report.summary;

        bool raFile = CoKetQua(opt.target, duongDan);

        if (s.result == BuildResult.Succeeded && raFile)
        {
            status = $"BUILD {ten} XONG.\n" +
                     $"Duong dan: {duongDan}\n" +
                     $"Dung luong: {s.totalSize / 1048576f:0.0} MB\n" +
                     $"Mat: {s.totalTime.TotalMinutes:0.0} phut";
            Debug.Log($"[Build] {status}");
            EditorUtility.RevealInFinder(duongDan);
        }
        else if (s.result == BuildResult.Succeeded && !raFile)
        {
            status = $"BAO CAO NOI XONG NHUNG KHONG CO FILE NAO O:\n{duongDan}\n\n" +
                     "Dung la truong hop Unity nem loi o buoc dong goi cuoi ma van ghi\n" +
                     "ket qua 'Succeeded'. Mo Console tim dong 'UnityException' hoac\n" +
                     "'not supported' — do moi la nguyen nhan that.";
            Debug.LogError($"[Build] {status}");
        }
        else
        {
            status = $"BUILD {ten} THAT BAI ({s.result}). {s.totalErrors} loi.\n" +
                     "Mo Console, doc dong co chu 'error' DAU TIEN — dong do moi la nguyen nhan,\n" +
                     "cac dong sau chi la he qua.";
            Debug.LogError($"[Build] {status}");
        }

        // build chay qua delayCall nen khung hinh da ve xong tu truoc -> phai ve lai
        // thi o ket qua moi hien chu, khong thi Sep tuong tool treo.
        Repaint();
    }
}
#endif
