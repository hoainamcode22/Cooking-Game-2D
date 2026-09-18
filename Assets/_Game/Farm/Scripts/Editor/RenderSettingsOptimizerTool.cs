#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools ▸ Farm Game ▸ Render Settings Optimizer
///
/// MUC DICH: sua may thiet lap render dang dot fill-rate va dot draw call tren Android,
/// bang API chinh thong — KHONG BAO GIO sua text trong file .asset.
///
/// HIEN TRANG DA DO:
///   Assets/Settings/Renderer2D.asset
///     m_UseCameraSortingLayersTexture : 1   ← copy nguyen man hinh moi frame (dot fill-rate)
///     m_UseDepthStencilBuffer         : 1
///     m_UseNativeRenderPass           : 0   ← Vulkan khong gop duoc light pass trong tile memory
///     m_LightRenderTextureScale       : 0.5
///     4 blend style khai bao
///   Assets/Day_Night/Prefabs/DayNightWeatherSetup.prefab
///     5 Light2D, TAT CA m_ShadowsEnabled: 1 (shadow intensity 0.75) — moi cai la 1 render pass rieng
///   ProjectSettings/QualitySettings.asset  (Android tier index 2)
///     vSyncCount 1, shadows 1, pixelLightCount 1, anisotropicTextures 1, streamingMipmapsActive 0
///   ProjectSettings/ProjectSettings.asset
///     accelerometerFrequency 60, Android texture compression = ETC1, m_BuildTargetGraphicsAPIs rong (Auto)
///
/// NGUYEN TAC AN TOAN:
///   1. Luon xem PREVIEW (gia tri hien tai vs gia tri de xuat) truoc. Apply bi khoa
///      cho den khi da bam "Doc gia tri hien tai".
///   2. Moi thay doi deu ghi gia tri TRUOC vao 1 file JSON NGOAI Assets/. Co nut "Revert all".
///   3. Muc 1 (_CameraSortingLayerTexture) co the lam HONG shader. Cong cu TU GREP
///      toan bo shader trong project; neu co shader nao dung ten do thi khoa cung muc nay lai.
///   4. Muc 4 la SUA PREFAB — Sep cam. Cong cu CHI BAO CAO, khong dong vao prefab.
///   5. Khong bao giu goi Apply / Revert THANG trong OnGUI (dialog + progress bar giua
///      Begin/EndHorizontal se nem "ArgumentException: GUILayout mismatched").
///      Ghi y dinh vao _pending, chay o EditorApplication.delayCall.
/// </summary>
public class RenderSettingsOptimizerTool : EditorWindow
{
    private const string MENU_PATH = "Tools/Farm Game/Render Settings Optimizer";
    private const string BACKUP_PREFIX = "_RenderSettingsBackup_";

    private const string SORTING_LAYER_TEX = "_CameraSortingLayerTexture";

    // URP Downsampling enum, luu trong .asset duoi dang int.
    //   0 = None (copy nguyen do phan giai)  1 = _2xBilinear  2 = _4xBox  3 = _4xBilinear
    private static readonly string[] DOWNSAMPLE_NAMES = { "None", "_2xBilinear", "_4xBox", "_4xBilinear" };
    // Nhan trong dropdown: hien ca so lan ten de doi chieu thang voi file .asset.
    private static readonly string[] DOWNSAMPLE_DISPLAY =
        { "0 (None)", "1 (_2xBilinear)", "2 (_4xBox)", "3 (_4xBilinear)" };

    // Khoa cua tung muc trong manifest JSON.
    private const string K_SORTING_TEX = "Renderer2D.m_UseCameraSortingLayersTexture";
    private const string K_SORTLAYER_DOWNSAMPLE = "Renderer2D.m_CameraSortingLayerDownsamplingMethod";
    private const string K_DEPTH = "Renderer2D.m_UseDepthStencilBuffer";
    private const string K_NATIVE_RP = "Renderer2D.m_UseNativeRenderPass";
    private const string K_VSYNC = "QualitySettings.vSyncCount";
    private const string K_SHADOWS = "QualitySettings.shadows";
    private const string K_PIXELLIGHT = "QualitySettings.pixelLightCount";
    private const string K_STREAMMIP = "QualitySettings.streamingMipmapsActive";
    private const string K_ACCEL = "PlayerSettings.accelerometerFrequency";
    private const string K_ANDROID_TC = "PlayerSettings.Android.textureCompression";

    // ═════════════════════════════════════════════════════════════════════════
    //  MANIFEST (JSON, ngoai Assets/)
    // ═════════════════════════════════════════════════════════════════════════

    [Serializable]
    private class BackupEntry
    {
        public string key;
        public string before;
        public string after;
        public string method;   // API nao da duoc dung — de doc lai cho de hieu
        public string target;   // duong dan asset neu co
    }

    [Serializable]
    private class BackupManifest
    {
        public string timestamp;
        public string project;
        public string unityVersion;
        public int qualityLevelIndex;
        public List<BackupEntry> entries = new List<BackupEntry>();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TUY CHON
    // ═════════════════════════════════════════════════════════════════════════

    private string _renderer2DPath = "Assets/Settings/Renderer2D.asset";
    private string _dayNightPrefabPath = "Assets/Day_Night/Prefabs/DayNightWeatherSetup.prefab";
    private int _qualityLevelIndex = 2;   // Android tier index 2

    private bool _optSortingTex = true;
    private bool _optSortLayerDownsample = true;   // mac dinh BAT — rui ro thap, loi cao
    private int _downsampleChoice = 2;             // mac dinh _4xBox
    private bool _optDepth;               // mac dinh TAT — rui ro mask
    private bool _optNativeRP = true;
    private bool _optVSync = true;
    private bool _optShadows = true;
    private bool _optPixelLight = true;
    private bool _optStreamMip;           // mac dinh TAT — chua co mipmap thi vo nghia
    private bool _optAccel = true;
    private bool _optAndroidTC = true;

    // ═════════════════════════════════════════════════════════════════════════
    //  TRANG THAI DOC DUOC
    // ═════════════════════════════════════════════════════════════════════════

    private bool _hasRead;
    private string _readError = "";

    private string _curSortingTex = "?";
    private string _curDownsample = "?";
    private bool _hasDownsampleProp;
    private string _curDepth = "?";
    private string _curNativeRP = "?";
    private string _curVSync = "?";
    private string _curShadows = "?";
    private string _curPixelLight = "?";
    private string _curStreamMip = "?";
    private string _curAccel = "?";
    private string _curAndroidTC = "?";
    private string _androidTcMethod = "?";

    private readonly List<string> _shadersUsingSortingTex = new List<string>();
    private readonly List<string> _light2DReport = new List<string>();
    private int _light2DWithShadows;

    private Vector2 _scroll;
    private readonly List<string> _backups = new List<string>();
    private int _selectedBackup;

    private enum PendingAction { None, Read, Apply, Revert, PrintPrefabInstructions }
    private PendingAction _pending = PendingAction.None;
    private string _pendingBackup;

    // ═════════════════════════════════════════════════════════════════════════
    //  CUA VAO
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_PATH, false, 41)]
    public static void Open()
    {
        var win = GetWindow<RenderSettingsOptimizerTool>(false, "Render Settings Optimizer", true);
        win.minSize = new Vector2(760f, 640f);
        win.RefreshBackupList();
        win.Show();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  GIAO DIEN
    // ═════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.HelpBox(
            "Sua thiet lap render bang API chinh thong — KHONG sua text trong file .asset.\n" +
            "Bam \"Doc gia tri hien tai\" truoc. Apply bi khoa cho den khi doc xong.\n" +
            "Moi thay doi deu ghi gia tri CU vao file JSON ngoai Assets/, co nut Revert all.",
            MessageType.Info);

        // ── Duong dan ───────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("0. Duong dan / muc tieu", EditorStyles.boldLabel);
        _renderer2DPath = EditorGUILayout.TextField("Renderer2D asset", _renderer2DPath);
        _dayNightPrefabPath = EditorGUILayout.TextField("DayNight prefab", _dayNightPrefabPath);

        string[] qNames;
        try { qNames = QualitySettings.names; } catch (Exception) { qNames = new string[0]; }
        if (qNames.Length > 0)
        {
            _qualityLevelIndex = Mathf.Clamp(_qualityLevelIndex, 0, qNames.Length - 1);
            _qualityLevelIndex = EditorGUILayout.Popup("Quality level (Android tier)", _qualityLevelIndex, qNames);
        }
        else
        {
            _qualityLevelIndex = EditorGUILayout.IntField("Quality level index", _qualityLevelIndex);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Doc gia tri hien tai (preview, khong sua gi)", GUILayout.Height(28f)))
            _pending = PendingAction.Read;

        if (!string.IsNullOrEmpty(_readError))
            EditorGUILayout.HelpBox(_readError, MessageType.Error);

        if (!_hasRead)
        {
            EditorGUILayout.HelpBox("Chua doc gia tri. Bam nut o tren.", MessageType.None);
            EditorGUILayout.EndScrollView();
            DispatchPending();
            return;
        }

        // ── Bang so sanh ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Renderer2D.asset", EditorStyles.boldLabel);

        // Muc 1 — co chot an toan grep shader
        bool sortingTexBlocked = _shadersUsingSortingTex.Count > 0;
        if (sortingTexBlocked)
        {
            var sb = new StringBuilder();
            sb.AppendLine("KHOA MUC 1 LAI. Co " + _shadersUsingSortingTex.Count +
                          " shader dang doc " + SORTING_LAYER_TEX + ":");
            foreach (var s in _shadersUsingSortingTex.Take(20)) sb.AppendLine("  • " + s);
            if (_shadersUsingSortingTex.Count > 20) sb.AppendLine("  ... va con nua, xem Console.");
            sb.AppendLine();
            sb.AppendLine("Tat m_UseCameraSortingLayersTexture se lam nhung shader nay sampling vao texture rong " +
                          "→ anh den / trong suot / lem mau. Phai sua shader truoc, roi doc lai.");
            sb.AppendLine();
            sb.AppendLine("khong tat duoc, nhung hay bat muc m_CameraSortingLayerDownsamplingMethod ben duoi " +
                          "de giam 4x chi phi copy.");
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.Error);
            _optSortingTex = false;
        }

        using (new EditorGUI.DisabledScope(sortingTexBlocked))
        {
            _optSortingTex = Row(_optSortingTex,
                "m_UseCameraSortingLayersTexture", _curSortingTex, "0 (tat)",
                "Bat = URP copy nguyen mot ban mau cua man hinh moi frame de shader doc duoc lop duoi. " +
                "Day la fill-rate thuan tuy, tren Android rat dat. RUI RO: shader nao dang sampling " +
                SORTING_LAYER_TEX + " se hong — cong cu da grep toan bo shader va " +
                (sortingTexBlocked ? "PHAT HIEN CO." : "khong thay cai nao."));
        }

        if (!_hasDownsampleProp) _optSortLayerDownsample = false;
        _optSortLayerDownsample = RowDownsampling(_optSortLayerDownsample,
            "m_CameraSortingLayerDownsamplingMethod",
            _hasDownsampleProp ? _curDownsample : "khong tim thay",
            "KHONG tat duoc m_UseCameraSortingLayersTexture: co 3 shadergraph nuoc dang sample " +
            SORTING_LAYER_TEX + ", va cai dang chay that " +
            "(Assets/Test nuoc/ImportedWater/Materials/Material_Water.mat) nam tren object Water_Tilemap " +
            "trong ca SCN_Farm lan SCN_Fishing, voi 9438 o nuoc da to. Tat di thi nuoc se thanh mot mang " +
            "phang bac mau, khong con nhin thay mat dat ben duoi nua. NHUNG ban copy do KHONG bat buoc phai " +
            "full resolution. Muc nay chinh do phan giai cua chinh ban copy full-screen moi frame do. " +
            "_4xBox cat bang thong cua no xuong con khoang 1/4. Vi khuc xa nuoc von la mot vet meo mo dang " +
            "dong, mat rat kho thay khac biet. RUI RO THAP — tac dung duy nhat la khuc xa hoi mem hon mot chut. " +
            "Nen A/B test tren may that truoc khi chot.",
            !_hasDownsampleProp);

        _optDepth = Row(_optDepth,
            "m_UseDepthStencilBuffer", _curDepth, "0 (tat)",
            "Tat depth/stencil buffer tiet kiem bang thong bo nho. RUI RO: game 2D nao dung " +
            "SpriteMask / stencil mask / sorting theo depth se hong hien thi. Mac dinh cong cu de TAT muc nay — " +
            "chi bat khi Sep da kiem tra chac trong scene khong co mask.");

        _optNativeRP = Row(_optNativeRP,
            "m_UseNativeRenderPass", _curNativeRP, "1 (bat)",
            "Cho Vulkan gop cac pass anh sang 2D lai trong tile memory cua GPU thay vi ghi ra RAM roi doc lai. " +
            "RUI RO THAP — day la thay doi duong ong noi bo, khong doi hinh anh. Chi co loi khi chay Vulkan " +
            "(project dang de Graphics API = Auto, tuc Vulkan truoc).");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. QualitySettings — level: " +
            (qNames.Length > _qualityLevelIndex ? qNames[_qualityLevelIndex] : _qualityLevelIndex.ToString()),
            EditorStyles.boldLabel);

        _optVSync = Row(_optVSync,
            "vSyncCount", _curVSync, "0 (tat)",
            "Tren Android vSync cua Unity gan nhu vo nghia (he dieu hanh tu dong bo), nhung no khoa " +
            "frame rate va lam Application.targetFrameRate bi bo qua. RUI RO THAP, co the sinh tearing tren PC editor.");

        _optShadows = Row(_optShadows,
            "shadows", _curShadows, "Disable",
            "Day la shadow cua he thong anh sang 3D — game 2D isometric nay khong dung toi, " +
            "nhung van ton pass. RUI RO: neu co object 3D nao trong scene can do bong thi se mat bong.");

        _optPixelLight = Row(_optPixelLight,
            "pixelLightCount", _curPixelLight, "0",
            "So den pixel-light 3D duoc phep. Game 2D dung Light2D (he khac), nen dat 0 la an toan. " +
            "RUI RO: chi anh huong neu co Light 3D thuc su trong scene.");

        _optStreamMip = Row(_optStreamMip,
            "streamingMipmapsActive", _curStreamMip, "true",
            "Mipmap streaming chi nap muc mipmap dang thuc su can → tiet kiem RAM texture rat nhieu. " +
            "LUU Y QUAN TRONG: hien tai texture trong project dang TAT mipmap (enableMipMap = 0), nen bat cai nay " +
            "BAY GIO KHONG co tac dung gi. Chi bat sau khi da dung Restore Texture Quality de sinh mipmap. " +
            "Vi vay mac dinh cong cu de TAT muc nay.");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. PlayerSettings", EditorStyles.boldLabel);

        _optAccel = Row(_optAccel,
            "accelerometerFrequency", _curAccel, "0 (tat)",
            "60 = Unity doc cam bien gia toc 60 lan/giay du game khong he dung. Ton CPU va PIN vo ich. " +
            "RUI RO: chi hong neu co code doc Input.acceleration — game nong trai nay thi khong.");

        _optAndroidTC = Row(_optAndroidTC,
            "Android texture compression", _curAndroidTC, "ASTC",
            "ETC1 KHONG CO KENH ALPHA — day la nguyen nhan goc lam sprite trong bi hong vien. " +
            "ASTC vua co alpha vua net hon o cung dung luong. RUI RO: may Android rat cu (truoc 2016, " +
            "khong co GLES3.1/Vulkan) se khong doc duoc ASTC. API dung: " + _androidTcMethod);

        // ── Muc 4: prefab — CHI BAO CAO ─────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4. Light2D trong DayNightWeatherSetup.prefab — CHI BAO CAO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Sep cam cong cu sua prefab, nen cong cu KHONG dong vao file nay. Duoi day la bao cao chinh xac " +
            "de Sep tu sua bang tay. Moi Light2D bat shadow la MOT render pass rieng — 5 cai bat shadow " +
            "tren Android la rat nang.",
            MessageType.Warning);

        if (_light2DReport.Count == 0)
        {
            EditorGUILayout.LabelField("Khong doc duoc Light2D nao (kiem tra lai duong dan prefab).");
        }
        else
        {
            EditorGUILayout.LabelField("Tim thay " + _light2DReport.Count + " Light2D, trong do " +
                                       _light2DWithShadows + " cai dang bat shadow:", EditorStyles.miniBoldLabel);
            foreach (var line in _light2DReport)
                EditorGUILayout.LabelField("   " + line, EditorStyles.miniLabel);
        }

        if (GUILayout.Button("In huong dan sua tay ra Console (khong sua gi)"))
            _pending = PendingAction.PrintPrefabInstructions;

        // ── Apply ───────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("5. Chay", EditorStyles.boldLabel);
        int n = CountSelected();
        using (new EditorGUI.DisabledScope(n == 0))
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.65f, 0.35f);
            if (GUILayout.Button("Apply " + n + " muc da tick", GUILayout.Height(30f)))
                _pending = PendingAction.Apply;
            GUI.backgroundColor = old;
        }
        if (n == 0) EditorGUILayout.HelpBox("Chua tick muc nao.", MessageType.None);

        // ── Revert ──────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("6. Revert all (tu file JSON)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (_backups.Count == 0)
        {
            EditorGUILayout.LabelField("Chua co backup nao.");
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshBackupList();
        }
        else
        {
            _selectedBackup = Mathf.Clamp(_selectedBackup, 0, _backups.Count - 1);
            var names = _backups.Select(Path.GetFileName).ToArray();
            _selectedBackup = EditorGUILayout.Popup(_selectedBackup, names);
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshBackupList();
            if (GUILayout.Button("Revert all", GUILayout.Width(100f)))
            {
                _pendingBackup = _backups[_selectedBackup];
                _pending = PendingAction.Revert;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        DispatchPending();
    }

    /// <summary>Mot dong: checkbox | ten | hien tai → de xuat | giai thich.</summary>
    private bool Row(bool on, string label, string current, string proposed, string explain)
    {
        EditorGUILayout.Space(3f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        on = EditorGUILayout.Toggle(on, GUILayout.Width(18f));
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(280f));
        EditorGUILayout.LabelField("hien tai: " + current, GUILayout.Width(180f));
        EditorGUILayout.LabelField("→  de xuat: " + proposed);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(explain, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
        return on;
    }

    /// <summary>
    /// Nhu Row() nhung gia tri de xuat la dropdown — Sep co the doi _4xBox sang _2xBilinear
    /// neu thay nuoc bi mem qua. missing = khong tim thay property tren asset → khoa checkbox.
    /// </summary>
    private bool RowDownsampling(bool on, string label, string current, string explain, bool missing)
    {
        EditorGUILayout.Space(3f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(missing))
            on = EditorGUILayout.Toggle(on, GUILayout.Width(18f));
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(280f));
        EditorGUILayout.LabelField("hien tai: " + current, GUILayout.Width(180f));
        EditorGUILayout.LabelField("→  de xuat:", GUILayout.Width(70f));
        using (new EditorGUI.DisabledScope(missing))
            _downsampleChoice = EditorGUILayout.Popup(_downsampleChoice, DOWNSAMPLE_DISPLAY, GUILayout.Width(150f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(explain, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
        return on;
    }

    /// <summary>"2 (_4xBox)" — hien so kem ten enum cho de doi chieu voi file .asset.</summary>
    private static string DownsampleLabel(int v)
    {
        string nm = (v >= 0 && v < DOWNSAMPLE_NAMES.Length) ? DOWNSAMPLE_NAMES[v] : "khong ro";
        return v + " (" + nm + ")";
    }

    private int CountSelected()
    {
        int n = 0;
        if (_optSortingTex) n++;
        if (_optSortLayerDownsample) n++;
        if (_optDepth) n++;
        if (_optNativeRP) n++;
        if (_optVSync) n++;
        if (_optShadows) n++;
        if (_optPixelLight) n++;
        if (_optStreamMip) n++;
        if (_optAccel) n++;
        if (_optAndroidTC) n++;
        return n;
    }

    /// <summary>
    /// Chay hanh dong da hen, HOAN TOAN NGOAI vong OnGUI. delayCall chay o tick sau,
    /// khong con trong stack IMGUI, nen dialog / progress bar khong the lam vo layout.
    /// </summary>
    private void DispatchPending()
    {
        if (_pending == PendingAction.None) return;

        var todo = _pending;
        var bk = _pendingBackup;
        _pending = PendingAction.None;

        EditorApplication.delayCall += () =>
        {
            switch (todo)
            {
                case PendingAction.Read: RunRead(); break;
                case PendingAction.Apply: RunApply(); break;
                case PendingAction.Revert: RunRevert(bk); break;
                case PendingAction.PrintPrefabInstructions: PrintPrefabInstructions(); break;
            }
            Repaint();
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  DOC (PREVIEW) — KHONG GHI GI
    // ═════════════════════════════════════════════════════════════════════════

    private void RunRead()
    {
        _hasRead = false;
        _readError = "";
        _hasDownsampleProp = false;
        _shadersUsingSortingTex.Clear();
        _light2DReport.Clear();
        _light2DWithShadows = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Doc gia tri hien tai", "Renderer2D.asset...", 0.1f);

            var so = LoadRenderer2DSerialized();
            if (so == null)
            {
                _readError = "Khong nap duoc " + _renderer2DPath +
                             " (kiem tra lai duong dan). Cac muc QualitySettings/PlayerSettings van doc duoc.";
                _curSortingTex = _curDepth = _curNativeRP = "KHONG DOC DUOC";
                _curDownsample = "KHONG DOC DUOC";
                _hasDownsampleProp = false;
            }
            else
            {
                _curSortingTex = ReadBoolProp(so, "m_UseCameraSortingLayersTexture");
                _curDepth = ReadBoolProp(so, "m_UseDepthStencilBuffer");
                _curNativeRP = ReadBoolProp(so, "m_UseNativeRenderPass");
                _curDownsample = ReadDownsampleProp(so, out _hasDownsampleProp);
            }

            EditorUtility.DisplayProgressBar("Doc gia tri hien tai", "Grep shader...", 0.35f);
            GrepShadersForSortingLayerTexture();

            EditorUtility.DisplayProgressBar("Doc gia tri hien tai", "QualitySettings...", 0.6f);
            ReadQuality();

            EditorUtility.DisplayProgressBar("Doc gia tri hien tai", "PlayerSettings...", 0.8f);
            _curAccel = PlayerSettings.accelerometerFrequency.ToString();
            _curAndroidTC = ReadAndroidTextureCompression(out _androidTcMethod);

            EditorUtility.DisplayProgressBar("Doc gia tri hien tai", "Light2D trong prefab...", 0.9f);
            ReadLight2DReport();
        }
        catch (Exception e)
        {
            _readError = "Loi khi doc: " + e.Message;
            Debug.LogError("[RenderOptimizer] " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        _hasRead = true;

        var sb = new StringBuilder();
        sb.AppendLine("═══ [RenderOptimizer] DOC GIA TRI HIEN TAI — KHONG CO GI BI SUA ═══");
        sb.AppendLine("Renderer2D : " + _renderer2DPath);
        sb.AppendLine("  m_UseCameraSortingLayersTexture = " + _curSortingTex);
        sb.AppendLine("  m_UseDepthStencilBuffer         = " + _curDepth);
        sb.AppendLine("  m_UseNativeRenderPass           = " + _curNativeRP);
        sb.AppendLine("  m_CameraSortingLayerDownsamplingMethod = " +
                      (_hasDownsampleProp ? _curDownsample : "khong tim thay"));
        sb.AppendLine("Shader dung " + SORTING_LAYER_TEX + " : " + _shadersUsingSortingTex.Count);
        foreach (var s in _shadersUsingSortingTex) sb.AppendLine("    • " + s);
        sb.AppendLine("QualitySettings (level " + _qualityLevelIndex + ")");
        sb.AppendLine("  vSyncCount             = " + _curVSync);
        sb.AppendLine("  shadows                = " + _curShadows);
        sb.AppendLine("  pixelLightCount        = " + _curPixelLight);
        sb.AppendLine("  streamingMipmapsActive = " + _curStreamMip);
        sb.AppendLine("PlayerSettings");
        sb.AppendLine("  accelerometerFrequency = " + _curAccel);
        sb.AppendLine("  Android texture compr. = " + _curAndroidTC + "   (API: " + _androidTcMethod + ")");
        sb.AppendLine("Light2D trong " + _dayNightPrefabPath + " : " + _light2DReport.Count +
                      " (bat shadow: " + _light2DWithShadows + ")");
        foreach (var l in _light2DReport) sb.AppendLine("    " + l);
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Nap Renderer2D asset nhu ScriptableObject thuong. Co tinh KHONG ep kieu ve
    /// Renderer2DData cua URP: cac field can sua deu la private serialized, SerializedObject
    /// doc duoc het ma khong can phu thuoc assembly URP luc bien dich.
    /// </summary>
    private SerializedObject LoadRenderer2DSerialized()
    {
        var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_renderer2DPath);
        if (obj == null) return null;
        return new SerializedObject(obj);
    }

    private static string ReadBoolProp(SerializedObject so, string name)
    {
        var p = so.FindProperty(name);
        if (p == null) return "KHONG CO PROPERTY";
        if (p.propertyType == SerializedPropertyType.Boolean) return p.boolValue ? "1 (bat)" : "0 (tat)";
        if (p.propertyType == SerializedPropertyType.Integer) return p.intValue.ToString();
        return p.propertyType.ToString();
    }

    /// <summary>
    /// m_CameraSortingLayerDownsamplingMethod la enum Downsampling nhung serialize thanh int.
    /// Doc bang intValue (an toan hon enumValueIndex vi khong phu thuoc thu tu popup cua Unity).
    /// Kiem tra kieu truoc khi doc, giong cach ReadLight2DReport kiem tra floatValue.
    /// </summary>
    private static string ReadDownsampleProp(SerializedObject so, out bool found)
    {
        found = false;
        var p = so.FindProperty("m_CameraSortingLayerDownsamplingMethod");
        if (p == null) return "khong tim thay";
        if (p.propertyType != SerializedPropertyType.Integer &&
            p.propertyType != SerializedPropertyType.Enum) return "kieu la: " + p.propertyType;
        found = true;
        return DownsampleLabel(p.intValue);
    }

    /// <summary>
    /// Grep MOI file shader trong Assets/ tim ten _CameraSortingLayerTexture.
    /// Doc bang File.ReadAllText cho nhanh va bat duoc ca .hlsl/.cginc/.shadergraph
    /// (shadergraph la JSON, ten property van nam duoi dang chuoi trong do).
    /// </summary>
    private void GrepShadersForSortingLayerTexture()
    {
        string root = Application.dataPath;
        string[] exts = { "*.shader", "*.hlsl", "*.cginc", "*.compute", "*.shadergraph", "*.shadersubgraph" };

        foreach (var pattern in exts)
        {
            string[] files;
            try { files = Directory.GetFiles(root, pattern, SearchOption.AllDirectories); }
            catch (Exception) { continue; }

            foreach (var f in files)
            {
                try
                {
                    string txt = File.ReadAllText(f);
                    if (txt.IndexOf(SORTING_LAYER_TEX, StringComparison.Ordinal) >= 0)
                    {
                        string rel = "Assets" + f.Replace('\\', '/').Substring(root.Replace('\\', '/').Length);
                        _shadersUsingSortingTex.Add(rel);
                    }
                }
                catch (Exception) { /* file khoa / khong doc duoc → bo qua */ }
            }
        }
    }

    private void ReadQuality()
    {
        int prev = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(_qualityLevelIndex, false);
            _curVSync = QualitySettings.vSyncCount.ToString();
            _curShadows = QualitySettings.shadows.ToString();
            _curPixelLight = QualitySettings.pixelLightCount.ToString();
            _curStreamMip = QualitySettings.streamingMipmapsActive.ToString();
        }
        finally
        {
            QualitySettings.SetQualityLevel(prev, false);
        }
    }

    /// <summary>
    /// Unity 6 co hai duong: PlayerSettings.Android.textureCompressionFormats (moi)
    /// va EditorUserBuildSettings.androidBuildSubtarget (cu, van con). Dung reflection de
    /// chon cai nao that su ton tai thay vi doan — neu doan sai thi khong bien dich duoc.
    /// </summary>
    private static PropertyInfo AndroidFormatsProp()
    {
        try
        {
            // LUU Y: GetNestedType KHONG nhan BindingFlags.Static — dua Static vao se luon tra ve null.
            var t = typeof(PlayerSettings).GetNestedType("Android", BindingFlags.Public | BindingFlags.NonPublic);
            if (t == null) return null;
            return t.GetProperty("textureCompressionFormats", BindingFlags.Public | BindingFlags.Static);
        }
        catch (Exception) { return null; }
    }

    private static PropertyInfo AndroidSubtargetProp()
    {
        try
        {
            return typeof(EditorUserBuildSettings).GetProperty(
                "androidBuildSubtarget", BindingFlags.Public | BindingFlags.Static);
        }
        catch (Exception) { return null; }
    }

    private static string ReadAndroidTextureCompression(out string method)
    {
        var pFormats = AndroidFormatsProp();
        if (pFormats != null)
        {
            method = "PlayerSettings.Android.textureCompressionFormats";
            try
            {
                var arr = pFormats.GetValue(null, null) as Array;
                if (arr == null || arr.Length == 0) return "(rong)";
                var names = new List<string>();
                foreach (var v in arr) names.Add(v == null ? "null" : v.ToString());
                return string.Join(",", names.ToArray());
            }
            catch (Exception e) { return "loi doc: " + e.Message; }
        }

        var pSub = AndroidSubtargetProp();
        if (pSub != null)
        {
            method = "EditorUserBuildSettings.androidBuildSubtarget";
            try { return Convert.ToString(pSub.GetValue(null, null)); }
            catch (Exception e) { return "loi doc: " + e.Message; }
        }

        method = "KHONG TIM THAY API NAO";
        return "?";
    }

    /// <summary>
    /// Doc Light2D trong prefab CHI DE BAO CAO. Khong ep kieu ve Light2D cua URP
    /// (tranh phu thuoc assembly) — do bang ten type, doc m_ShadowsEnabled bang SerializedObject.
    /// </summary>
    private void ReadLight2DReport()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(_dayNightPrefabPath);
        if (go == null)
        {
            _light2DReport.Add("KHONG NAP DUOC PREFAB: " + _dayNightPrefabPath);
            return;
        }

        var comps = go.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            string typeName = c.GetType().Name;
            if (typeName != "Light2D") continue;

            var so = new SerializedObject(c);
            var pShadow = so.FindProperty("m_ShadowsEnabled");
            var pIntensity = so.FindProperty("m_ShadowIntensity");

            string state = pShadow == null
                ? "khong co m_ShadowsEnabled"
                : (pShadow.boolValue ? "SHADOW BAT" : "shadow tat");
            if (pShadow != null && pShadow.boolValue) _light2DWithShadows++;

            // Doc floatValue tren property khong phai float se nem — kiem tra kieu truoc.
            string inten = (pIntensity != null && pIntensity.propertyType == SerializedPropertyType.Float)
                ? "  intensity " + pIntensity.floatValue.ToString("0.##")
                : "";
            _light2DReport.Add(GetHierarchyPath(c.transform) + "  →  Light2D.m_ShadowsEnabled = " + state + inten);
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        var p = t.parent;
        while (p != null)
        {
            sb.Insert(0, p.name + "/");
            p = p.parent;
        }
        return sb.ToString();
    }

    private void PrintPrefabInstructions()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ [RenderOptimizer] HUONG DAN SUA TAY — CONG CU KHONG DONG VAO PREFAB ═══");
        sb.AppendLine("File   : " + _dayNightPrefabPath);
        sb.AppendLine("Ly do  : moi Light2D bat shadow la MOT render pass rieng cua URP 2D.");
        sb.AppendLine("         5 den bat shadow tren Android tier thap = 5 pass thua moi frame.");
        sb.AppendLine("         Game nong trai isometric nay khong can bong dong tu den moi truong.");
        sb.AppendLine();
        sb.AppendLine("CACH LAM:");
        sb.AppendLine("  1. Project window → mo " + _dayNightPrefabPath + " (double click de vao Prefab Mode).");
        sb.AppendLine("  2. Voi TUNG object duoi day, chon no, trong Inspector tim component 'Light 2D',");
        sb.AppendLine("     mo muc 'Shadows', BO TICH o 'Enabled' (property noi bo: m_ShadowsEnabled).");
        sb.AppendLine("  3. Ctrl+S de luu prefab, roi thoat Prefab Mode.");
        sb.AppendLine("  4. Vao Play, nhin ky canh dem: neu mat chieu sau thi bat lai 1 den duy nhat");
        sb.AppendLine("     (den chinh cua mat trang), khong bat lai ca 5.");
        sb.AppendLine();
        sb.AppendLine("DANH SACH CHINH XAC:");
        if (_light2DReport.Count == 0)
            sb.AppendLine("  (chua doc duoc — bam 'Doc gia tri hien tai' truoc)");
        foreach (var l in _light2DReport) sb.AppendLine("  " + l);
        sb.AppendLine();
        sb.AppendLine("Component : Light2D   |   Property : m_ShadowsEnabled   |   1 → 0");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  APPLY
    // ═════════════════════════════════════════════════════════════════════════

    private void RunApply()
    {
        if (!_hasRead)
        {
            EditorUtility.DisplayDialog("Render Settings Optimizer", "Phai bam 'Doc gia tri hien tai' truoc.", "OK");
            return;
        }

        if (_optSortingTex && _shadersUsingSortingTex.Count > 0)
        {
            EditorUtility.DisplayDialog("Render Settings Optimizer",
                "TU CHOI: co shader dang dung " + SORTING_LAYER_TEX + ". Khong the tat muc 1.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply — XAC NHAN",
                "Se sua " + CountSelected() + " muc.\n\n" +
                "Gia tri cu duoc ghi vao file JSON ngoai Assets/ — co nut Revert all.\n" +
                "Cong cu KHONG dong vao prefab, KHONG sua text trong .asset.",
                "Apply", "Huy"))
            return;

        var man = new BackupManifest
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            project = ProjectRoot(),
            unityVersion = Application.unityVersion,
            qualityLevelIndex = _qualityLevelIndex
        };

        int applied = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Apply", "Renderer2D.asset...", 0.2f);

            if (_optSortingTex || _optSortLayerDownsample || _optDepth || _optNativeRP)
            {
                var so = LoadRenderer2DSerialized();
                if (so == null)
                {
                    Debug.LogError("[RenderOptimizer] Khong nap duoc " + _renderer2DPath + " — bo qua cac muc Renderer2D.");
                }
                else
                {
                    if (_optSortingTex && SetBoolProp(so, "m_UseCameraSortingLayersTexture", false, man, K_SORTING_TEX)) applied++;
                    if (_optSortLayerDownsample && SetIntProp(so, "m_CameraSortingLayerDownsamplingMethod",
                            _downsampleChoice, man, K_SORTLAYER_DOWNSAMPLE)) applied++;
                    if (_optDepth && SetBoolProp(so, "m_UseDepthStencilBuffer", false, man, K_DEPTH)) applied++;
                    if (_optNativeRP && SetBoolProp(so, "m_UseNativeRenderPass", true, man, K_NATIVE_RP)) applied++;

                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(so.targetObject);
                }
            }

            EditorUtility.DisplayProgressBar("Apply", "QualitySettings...", 0.5f);

            if (_optVSync || _optShadows || _optPixelLight || _optStreamMip)
            {
                int prev = QualitySettings.GetQualityLevel();
                try
                {
                    QualitySettings.SetQualityLevel(_qualityLevelIndex, false);

                    if (_optVSync)
                    {
                        Record(man, K_VSYNC, QualitySettings.vSyncCount.ToString(), "0", "QualitySettings.vSyncCount", "");
                        QualitySettings.vSyncCount = 0;
                        applied++;
                    }
                    if (_optShadows)
                    {
                        Record(man, K_SHADOWS, QualitySettings.shadows.ToString(), ShadowQuality.Disable.ToString(),
                            "QualitySettings.shadows", "");
                        QualitySettings.shadows = ShadowQuality.Disable;
                        applied++;
                    }
                    if (_optPixelLight)
                    {
                        Record(man, K_PIXELLIGHT, QualitySettings.pixelLightCount.ToString(), "0",
                            "QualitySettings.pixelLightCount", "");
                        QualitySettings.pixelLightCount = 0;
                        applied++;
                    }
                    if (_optStreamMip)
                    {
                        Record(man, K_STREAMMIP, QualitySettings.streamingMipmapsActive.ToString(), "True",
                            "QualitySettings.streamingMipmapsActive", "");
                        QualitySettings.streamingMipmapsActive = true;
                        applied++;
                    }
                }
                finally
                {
                    QualitySettings.SetQualityLevel(prev, false);
                }
            }

            EditorUtility.DisplayProgressBar("Apply", "PlayerSettings...", 0.8f);

            if (_optAccel)
            {
                Record(man, K_ACCEL, PlayerSettings.accelerometerFrequency.ToString(), "0",
                    "PlayerSettings.accelerometerFrequency", "");
                PlayerSettings.accelerometerFrequency = 0;
                applied++;
            }

            if (_optAndroidTC)
            {
                string method;
                string before = ReadAndroidTextureCompression(out method);
                if (SetAndroidTextureCompressionAstc())
                {
                    string after = ReadAndroidTextureCompression(out method);
                    Record(man, K_ANDROID_TC, before, after, method, "");
                    applied++;
                }
                else
                {
                    Debug.LogError("[RenderOptimizer] Khong doi duoc Android texture compression — " +
                                   "phai lam tay: Edit > Project Settings > Player > Android > Texture compression format = ASTC");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[RenderOptimizer] Loi khi apply: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string file = WriteManifest(man);
        RefreshBackupList();

        var sb = new StringBuilder();
        sb.AppendLine("═══ [RenderOptimizer] APPLY XONG ═══");
        sb.AppendLine("Da sua   : " + applied + " muc");
        sb.AppendLine("Backup   : " + file);
        sb.AppendLine();
        foreach (var e in man.entries)
            sb.AppendLine(string.Format("  {0,-50} {1}  →  {2}   [{3}]", e.key, e.before, e.after, e.method));
        sb.AppendLine();
        sb.AppendLine("CHUA LAM (cong cu co tinh khong lam — xem muc 4 trong cua so):");
        sb.AppendLine("  • 5 Light2D trong " + _dayNightPrefabPath + " van dang bat m_ShadowsEnabled.");
        sb.AppendLine();
        sb.AppendLine("CACH KIEM CHUNG:");
        sb.AppendLine("  Window > Analysis > Frame Debugger → so so pass truoc/sau.");
        sb.AppendLine("  Muc 1 (sorting layer texture) se lam BIEN MAT han mot lenh copy full-screen moi frame.");
        Debug.Log(sb.ToString());

        _hasRead = false; // buoc doc lai truoc lan sau
    }

    private bool SetBoolProp(SerializedObject so, string name, bool value, BackupManifest man, string key)
    {
        var p = so.FindProperty(name);
        if (p == null)
        {
            Debug.LogWarning("[RenderOptimizer] Khong tim thay property " + name + " tren " + _renderer2DPath);
            return false;
        }
        if (p.propertyType != SerializedPropertyType.Boolean)
        {
            Debug.LogWarning("[RenderOptimizer] Property " + name + " khong phai bool (" + p.propertyType + ") — bo qua.");
            return false;
        }
        Record(man, key, p.boolValue.ToString(), value.ToString(),
            "SerializedObject.FindProperty(\"" + name + "\")", _renderer2DPath);
        p.boolValue = value;
        return true;
    }

    /// <summary>Ghi enum-luu-thanh-int (vd m_CameraSortingLayerDownsamplingMethod) bang intValue.</summary>
    private bool SetIntProp(SerializedObject so, string name, int value, BackupManifest man, string key)
    {
        var p = so.FindProperty(name);
        if (p == null)
        {
            Debug.LogWarning("[RenderOptimizer] Khong tim thay property " + name + " tren " + _renderer2DPath);
            return false;
        }
        if (p.propertyType != SerializedPropertyType.Integer && p.propertyType != SerializedPropertyType.Enum)
        {
            Debug.LogWarning("[RenderOptimizer] Property " + name + " khong phai int/enum (" + p.propertyType + ") — bo qua.");
            return false;
        }
        Record(man, key, p.intValue.ToString(), value.ToString(),
            "SerializedObject.FindProperty(\"" + name + "\").intValue", _renderer2DPath);
        p.intValue = value;
        return true;
    }

    private static void Record(BackupManifest man, string key, string before, string after, string method, string target)
    {
        man.entries.Add(new BackupEntry { key = key, before = before, after = after, method = method, target = target });
    }

    private bool SetAndroidTextureCompressionAstc()
    {
        var pFormats = AndroidFormatsProp();
        if (pFormats != null)
        {
            try
            {
                Type arrType = pFormats.PropertyType;       // vd TextureCompressionFormat[]
                Type elem = arrType.GetElementType();
                if (elem == null || !elem.IsEnum) return false;

                object astc = null;
                foreach (var nm in Enum.GetNames(elem))
                {
                    if (string.Equals(nm, "ASTC", StringComparison.OrdinalIgnoreCase))
                    { astc = Enum.Parse(elem, nm); break; }
                }
                if (astc == null) return false;

                var arr = Array.CreateInstance(elem, 1);
                arr.SetValue(astc, 0);
                pFormats.SetValue(null, arr, null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RenderOptimizer] textureCompressionFormats that bai: " + e.Message);
            }
        }

        var pSub = AndroidSubtargetProp();
        if (pSub != null)
        {
            try
            {
                Type t = pSub.PropertyType;                 // MobileTextureSubtarget
                if (!t.IsEnum) return false;
                object astc = Enum.Parse(t, "ASTC");
                pSub.SetValue(null, astc, null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RenderOptimizer] androidBuildSubtarget that bai: " + e.Message);
            }
        }

        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  MANIFEST / REVERT
    // ═════════════════════════════════════════════════════════════════════════

    private static string ProjectRoot()
    {
        // Application.dataPath = <project>/Assets
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    private string WriteManifest(BackupManifest man)
    {
        string file = Path.Combine(ProjectRoot(),
            BACKUP_PREFIX + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
        try
        {
            File.WriteAllText(file, JsonUtility.ToJson(man, true), Encoding.UTF8);
            Debug.Log("[RenderOptimizer] Da ghi backup: " + file);
            return file;
        }
        catch (Exception e)
        {
            Debug.LogError("[RenderOptimizer] Khong ghi duoc backup: " + e);
            return "";
        }
    }

    private void RefreshBackupList()
    {
        _backups.Clear();
        try
        {
            var files = Directory.GetFiles(ProjectRoot(), BACKUP_PREFIX + "*.json", SearchOption.TopDirectoryOnly);
            _backups.AddRange(files.OrderByDescending(f => f));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[RenderOptimizer] Khong doc duoc danh sach backup: " + e.Message);
        }
        _selectedBackup = 0;
    }

    private void RunRevert(string file)
    {
        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            EditorUtility.DisplayDialog("Revert all", "Khong tim thay file backup.", "OK");
            return;
        }

        BackupManifest man;
        try
        {
            man = JsonUtility.FromJson<BackupManifest>(File.ReadAllText(file));
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Revert all", "Khong doc duoc backup: " + e.Message, "OK");
            return;
        }

        if (man == null || man.entries == null || man.entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Revert all", "File backup rong.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Revert all — XAC NHAN",
                "Se tra " + man.entries.Count + " muc ve gia tri truoc khi apply, theo:\n" +
                Path.GetFileName(file) + "\n(luu luc " + man.timestamp + ")",
                "Revert", "Huy"))
            return;

        int done = 0, failed = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Revert all", "Renderer2D.asset...", 0.3f);

            var so = LoadRenderer2DSerialized();
            bool soDirty = false;

            foreach (var e in man.entries)
            {
                try
                {
                    switch (e.key)
                    {
                        case K_SORTING_TEX:
                        case K_DEPTH:
                        case K_NATIVE_RP:
                            if (so == null) { failed++; break; }
                            string prop = e.key.Substring(e.key.IndexOf('.') + 1);
                            var p = so.FindProperty(prop);
                            if (p == null || p.propertyType != SerializedPropertyType.Boolean) { failed++; break; }
                            p.boolValue = ParseBool(e.before);
                            soDirty = true;
                            done++;
                            break;

                        case K_SORTLAYER_DOWNSAMPLE:
                            if (so == null) { failed++; break; }
                            var pDs = so.FindProperty("m_CameraSortingLayerDownsamplingMethod");
                            if (pDs == null ||
                                (pDs.propertyType != SerializedPropertyType.Integer &&
                                 pDs.propertyType != SerializedPropertyType.Enum)) { failed++; break; }
                            pDs.intValue = int.Parse(e.before);
                            soDirty = true;
                            done++;
                            break;

                        case K_ACCEL:
                            PlayerSettings.accelerometerFrequency = int.Parse(e.before);
                            done++;
                            break;

                        case K_ANDROID_TC:
                            if (RevertAndroidTextureCompression(e.before)) done++; else failed++;
                            break;

                        case K_VSYNC:
                        case K_SHADOWS:
                        case K_PIXELLIGHT:
                        case K_STREAMMIP:
                            // xu ly theo cum o duoi (phai doi quality level 1 lan thoi)
                            break;

                        default:
                            Debug.LogWarning("[RenderOptimizer] Khoa la trong backup, bo qua: " + e.key);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.LogError("[RenderOptimizer] Revert loi o " + e.key + ": " + ex.Message);
                }
            }

            if (so != null && soDirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so.targetObject);
            }

            EditorUtility.DisplayProgressBar("Revert all", "QualitySettings...", 0.7f);

            var qEntries = man.entries.Where(e =>
                e.key == K_VSYNC || e.key == K_SHADOWS || e.key == K_PIXELLIGHT || e.key == K_STREAMMIP).ToList();

            if (qEntries.Count > 0)
            {
                int prev = QualitySettings.GetQualityLevel();
                try
                {
                    QualitySettings.SetQualityLevel(man.qualityLevelIndex, false);
                    foreach (var e in qEntries)
                    {
                        try
                        {
                            if (e.key == K_VSYNC) QualitySettings.vSyncCount = int.Parse(e.before);
                            else if (e.key == K_PIXELLIGHT) QualitySettings.pixelLightCount = int.Parse(e.before);
                            else if (e.key == K_STREAMMIP) QualitySettings.streamingMipmapsActive = ParseBool(e.before);
                            else if (e.key == K_SHADOWS)
                                QualitySettings.shadows = (ShadowQuality)Enum.Parse(typeof(ShadowQuality), e.before);
                            done++;
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            Debug.LogError("[RenderOptimizer] Revert loi o " + e.key + ": " + ex.Message);
                        }
                    }
                }
                finally
                {
                    QualitySettings.SetQualityLevel(prev, false);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("[RenderOptimizer] Revert xong: " + done + " muc tra ve cu, " + failed + " loi. Nguon: " + file);
        EditorUtility.DisplayDialog("Revert all",
            "Da tra " + done + " muc ve gia tri cu." + (failed > 0 ? "\n" + failed + " muc loi, xem Console." : ""), "OK");

        _hasRead = false;
    }

    private static bool ParseBool(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim();
        if (s == "1") return true;
        if (s == "0") return false;
        bool b;
        if (bool.TryParse(s, out b)) return b;
        return s.IndexOf("bat", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool RevertAndroidTextureCompression(string before)
    {
        if (string.IsNullOrEmpty(before)) return false;

        var pFormats = AndroidFormatsProp();
        if (pFormats != null)
        {
            try
            {
                Type elem = pFormats.PropertyType.GetElementType();
                if (elem == null || !elem.IsEnum) return false;
                var names = before.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                var arr = Array.CreateInstance(elem, names.Length);
                for (int i = 0; i < names.Length; i++)
                    arr.SetValue(Enum.Parse(elem, names[i], true), i);
                pFormats.SetValue(null, arr, null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RenderOptimizer] Revert textureCompressionFormats that bai: " + e.Message);
                return false;
            }
        }

        var pSub = AndroidSubtargetProp();
        if (pSub != null)
        {
            try
            {
                pSub.SetValue(null, Enum.Parse(pSub.PropertyType, before.Trim(), true), null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RenderOptimizer] Revert androidBuildSubtarget that bai: " + e.Message);
                return false;
            }
        }

        return false;
    }
}
#endif
