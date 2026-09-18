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
/// Tools ▸ Farm Game ▸ Restore Texture Quality
///
/// MUC DICH: tra lai do net cho texture sau hai dot "toi uu" tu dong da lam hong anh,
/// dong thoi van giu/tang hieu nang tren mobile.
///
/// HIEN TRANG DA KIEM TRA TU .meta (vong 2):
///   • DefaultTexturePlatform.maxTextureSize = 1024 (Art/, Day_Night/) → anh bi thu nho 1/2
///   • Android + iOS override textureFormat 50 = ASTC 6x6 (3.56 bpp) → be khoi (block mush)
///     tren gradient mem va canh cheo 45 do cua isometric
///   • enableMipMap = 0 tren MOI texture → shimmer/crawl khi zoom xa, mat nhu giat khi pan
///   • filterMode lech nhau: 0 (Point) o maptitle/plotdat, tile_dirt, raythang; 1 (Bilinear) cho con lai
///   • aniso = 1, Android quality tier anisotropicTextures = 0
///   • spritePixelsToUnits co 6 gia tri khac nhau: 64, 90, 100, 128, 132, 256
///   • crunchedCompression = 0 o khap noi → crunch KHONG phai thu pham
///
/// NGUYEN TAC AN TOAN CUA CONG CU NAY:
///   1. Luon SCAN (dry run) truoc. Nut Apply bi khoa cho den khi da scan xong.
///   2. Backup toan bo .meta bi anh huong ra ngoai Assets/ truoc khi ghi, kem manifest.
///      Co nut Restore de tra ve nguyen trang.
///   3. CHI dung TextureImporter API. KHONG bao gio sua text trong file .meta.
///   4. KHONG dung toi spritePixelsToUnits (doi PPU = doi kich thuoc moi sprite trong scene
///      → vo bo cuc). Cong cu chi BAO CAO su lech PPU de Sep tu quyet dinh sau.
///   5. KHONG luu scene, KHONG dung toi .unity / .prefab / .asset.
///
/// File nay nam trong thu muc Editor/ nen #if UNITY_EDITOR ve ky thuat la thua,
/// nhung giu lai cho dong bo voi FarmResetTool.cs cung thu muc.
/// </summary>
public class TextureQualityRestoreTool : EditorWindow
{
    private const string MENU_PATH = "Tools/Farm Game/Restore Texture Quality";
    private const string BACKUP_PREFIX = "_MetaBackup_";
    private const string MANIFEST_NAME = "_manifest.txt";
    private const int BATCH_SIZE = 100;

    // Do da profile duoc tren may that (xem ROUND2_TEXTURE_NOTES.md):
    //   Texture memory 608 MB / RAM tong 2864 MB / CPU 90.05 ms / Render 5.75 ms
    // → game dang NGHEN CPU, khong nghen GPU. Do phan giai + format texture gan nhu
    //   KHONG anh huong fps. Cai that su nguy hiem la RAM: may Android 4 GB bi OOM-kill
    //   se bi Google Play ghi nhan la crash.
    private const float MEASURED_TEXTURE_MB = 608f;   // texture memory da do duoc
    private const float SAFE_TEXTURE_MB     = 780f;   // tran an toan cho may 4 GB
    private const float DANGER_TEXTURE_MB   = 950f;   // tren muc nay: rui ro OOM that su

    // ═════════════════════════════════════════════════════════════════════════
    //  TUY CHON
    // ═════════════════════════════════════════════════════════════════════════

    private static readonly string[] FOLDER_PRESETS =
    {
        "Assets",
        "Assets/Art",
        "Assets/maptitle",
        "Assets/_Game",
        "Assets/Day_Night",
        "Assets/NV_NPC",
        "Assets/Lana Studio"
    };

    // ASTC luon 128 bit / block → bpp = 128 / (bx * by).
    // LUU Y: Unity KHONG expose ASTC 6x8 trong TextureImporterFormat
    // (chi co 4x4, 5x5, 6x6, 8x8, 10x10, 12x12), nen o day khong co 6x8.
    private enum AstcBlock { ASTC_4x4 = 0, ASTC_5x5 = 1, ASTC_6x6 = 2, ASTC_8x8 = 3 }

    // Preset = bo gia tri da tinh san cho tung nhom folder, de Sep khoi phai doan.
    private enum Preset { Custom = 0, NetVua = 1, NetToiDa = 2, ChiIconBiThuNho = 3, TileSheet = 4, VfxRe = 5 }
    private Preset _preset = Preset.Custom;

    private string _scopeFolder = "Assets";
    private AstcBlock _astcBlock = AstcBlock.ASTC_5x5;
    private int _maxTextureSize = 2048;
    private bool _enableMipMaps = true;
    private FilterMode _filterMode = FilterMode.Bilinear;
    private int _anisoLevel = 2;

    // Bat/tat tung nhom thay doi
    private bool _catMaxSize = true;       // maxTextureSize top-level + DefaultTexturePlatform
    private bool _catMipMaps = true;       // mipmapEnabled
    private bool _catFilterMode = true;    // filterMode
    private bool _catAniso = true;         // anisoLevel
    private bool _catAndroid = true;       // override Android
    private bool _catIOS = true;           // override iPhone

    private bool _skipNormalMaps = true;
    private bool _skipLightmaps = true;

    // ═════════════════════════════════════════════════════════════════════════
    //  TRANG THAI
    // ═════════════════════════════════════════════════════════════════════════

    private sealed class PlannedChange
    {
        public string AssetPath;
        public readonly List<string> Diffs = new List<string>();
        public float Ppu;
    }

    private readonly List<PlannedChange> _planned = new List<PlannedChange>();
    private int _scannedTotal;
    private int _skippedTotal;
    private string _ppuReport = "";

    // Uoc luong bo nho texture cua pham vi dang quet (byte tren GPU/RAM, khong phai
    // dung luong file PNG tren o cung).
    private double _memBeforeBytes;
    private double _memAfterBytes;
    private int _memUnknownSource;   // so texture khong doc duoc kich thuoc goc
    private string _optionSignatureAtScan = "";
    private bool _hasScanned;
    private Vector2 _scrollOptions;
    private Vector2 _scrollReport;

    private readonly List<string> _backupFolders = new List<string>();
    private int _selectedBackup;

    // Khong bao gio goi Scan/Apply/Restore THANG trong OnGUI: dialog + progress bar
    // giua hai lan Begin/EndHorizontal se lam vo layout ("GUILayout mismatched").
    // Ghi y dinh vao day, chay sau khi moi group da dong.
    private enum PendingAction { None, Scan, Apply, Restore }
    private PendingAction _pending = PendingAction.None;
    private string _pendingRestoreFolder;

    // ═════════════════════════════════════════════════════════════════════════
    //  CUA VAO
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_PATH, false, 20)]
    public static void Open()
    {
        var win = GetWindow<TextureQualityRestoreTool>(false, "Restore Texture Quality", true);
        win.minSize = new Vector2(620f, 560f);
        win.RefreshBackupList();
        win.Show();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  GIAO DIEN
    // ═════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        _scrollOptions = EditorGUILayout.BeginScrollView(_scrollOptions);

        EditorGUILayout.HelpBox(
            "Tra lai do net cho texture. LUON bam \"Scan (dry run)\" truoc — Apply bi khoa cho den khi scan xong.\n" +
            "Cong cu KHONG doi spritePixelsToUnits, KHONG luu scene, KHONG dung toi .unity/.prefab/.asset.",
            MessageType.Info);


        // ──────────────────────────────────────────────────────────────────
        // 0. PRESET ─ bam mot cai la xong, khoi doan.
        // Chi GAN gia tri vao field, KHONG mo dialog / KHONG ghi asset → an toan trong OnGUI.
        // ──────────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("0. Preset (bam mot cai roi Scan)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Net vua, an toan RAM",
                "Assets/Art: max 1024, ASTC 5x5, mipmap TAT. Do net tang ro o vung player nhin gan, RAM tang vua phai."),
                GUILayout.Height(26f)))
            ApplyPreset(Preset.NetVua);
        if (GUILayout.Button(new GUIContent("Net toi da (rui ro RAM)",
                "Toan bo Assets: max 2048, ASTC 4x4, mipmap TAT. CANH BAO: bo nho texture tang khoang 2.2 lan. Chi dung de so sanh, dung ship thang."),
                GUILayout.Height(26f)))
            ApplyPreset(Preset.NetToiDa);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Chi icon bi thu nho (512 -> 1024)",
                "Assets/Assetsgame: nhom Icon dang bi ep xuong 512 tu anh goc ~2700px (thu nho 5.3 lan). Day la cho MO NHAT ca project ma lai re nhat de sua."),
                GUILayout.Height(26f)))
            ApplyPreset(Preset.ChiIconBiThuNho);
        if (GUILayout.Button(new GUIContent("Tile sheet (BAT BUOC tat mipmap)",
                "Assets/maptitle: sheet cat sat nhau khong co gutter, spriteExtrude = 1 → bat mipmap se LO DUONG KE giua cac o."),
                GUILayout.Height(26f)))
            ApplyPreset(Preset.TileSheet);
        if (GUILayout.Button(new GUIContent("VFX / particle (re RAM)",
                "Assets/Lana Studio: hat particle nho, mo cung khong ai thay → ASTC 8x8, max 512."),
                GUILayout.Height(26f)))
            ApplyPreset(Preset.VfxRe);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Preset dang chon: " + PresetLabel(_preset), EditorStyles.miniLabel);

        // ── Pham vi ─────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Pham vi quet (folder scope)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _scopeFolder = EditorGUILayout.TextField("Folder", _scopeFolder);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < FOLDER_PRESETS.Length; i++)
        {
            if (i == 4) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
            bool on = string.Equals(_scopeFolder, FOLDER_PRESETS[i], StringComparison.OrdinalIgnoreCase);
            if (GUILayout.Toggle(on, FOLDER_PRESETS[i], EditorStyles.miniButton) && !on)
                _scopeFolder = FOLDER_PRESETS[i];
        }
        EditorGUILayout.EndHorizontal();
        if (!AssetDatabase.IsValidFolder(_scopeFolder))
            EditorGUILayout.HelpBox("Folder khong ton tai trong project: " + _scopeFolder, MessageType.Warning);

        // ── Gia tri dich ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Gia tri dich", EditorStyles.boldLabel);
        _astcBlock = (AstcBlock)EditorGUILayout.EnumPopup(
            new GUIContent("ASTC block size", "ASTC luon 128 bit/block:\n4x4 = 16 texel = 8.00 bpp (net nhat, nang nhat)\n5x5 = 25 texel = 5.12 bpp (khuyen dung)\n6x6 = 36 texel = 3.56 bpp (dang dung, bi be khoi)"),
            _astcBlock);
        _maxTextureSize = EditorGUILayout.IntPopup("Max texture size", _maxTextureSize,
            new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 });
        _enableMipMaps = EditorGUILayout.Toggle(
            new GUIContent("Bat mipmap", "Tat mipmap gay shimmer/crawl khi zoom xa — mat nhin ra nhu giat khi pan."),
            _enableMipMaps);
        _filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter mode", _filterMode);
        _anisoLevel = EditorGUILayout.IntSlider(
            new GUIContent("Aniso level", "Chi co tac dung khi mipmap bat VA Quality tier bat Anisotropic Textures."),
            _anisoLevel, 0, 16);

        // ── Nhom thay doi ───────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Nhom thay doi (bat/tat tung cai)", EditorStyles.boldLabel);
        _catMaxSize    = EditorGUILayout.ToggleLeft("Max texture size (top-level + DefaultTexturePlatform)", _catMaxSize);
        _catMipMaps    = EditorGUILayout.ToggleLeft("Mipmap", _catMipMaps);
        _catFilterMode = EditorGUILayout.ToggleLeft("Filter mode", _catFilterMode);
        _catAniso      = EditorGUILayout.ToggleLeft("Aniso level", _catAniso);
        _catAndroid    = EditorGUILayout.ToggleLeft("Override Android (format ASTC + max size + quality 100)", _catAndroid);
        _catIOS        = EditorGUILayout.ToggleLeft("Override iOS / iPhone (format ASTC + max size + quality 100)", _catIOS);

        EditorGUILayout.Space(4f);
        _skipNormalMaps = EditorGUILayout.ToggleLeft("Bo qua normal map", _skipNormalMaps);
        _skipLightmaps  = EditorGUILayout.ToggleLeft("Bo qua lightmap", _skipLightmaps);

        if (EditorGUI.EndChangeCheck())
        {
            _preset = Preset.Custom; // sua tay → khong con la preset nua
            if (_hasScanned && BuildOptionSignature() != _optionSignatureAtScan)
                _hasScanned = false; // doi tuy chon → ket qua scan cu khong con dung
        }

        if (!AnyCategoryEnabled())
            EditorGUILayout.HelpBox("Chua bat nhom thay doi nao — scan se khong ra gi.", MessageType.Warning);

        // ── Scan / Apply ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4. Chay", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan (dry run)", GUILayout.Height(30f)))
            _pending = PendingAction.Scan;

        using (new EditorGUI.DisabledScope(!_hasScanned || _planned.Count == 0))
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.65f, 0.35f);
            if (GUILayout.Button(
                    _hasScanned ? "Apply (" + _planned.Count + " asset)" : "Apply (phai scan truoc)",
                    GUILayout.Height(30f)))
                _pending = PendingAction.Apply;
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndHorizontal();

        if (!_hasScanned)
            EditorGUILayout.HelpBox("Apply bi khoa. Bam Scan (dry run) truoc.", MessageType.None);

        // ── Bao cao ─────────────────────────────────────────────────────────
        if (_hasScanned)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Ket qua scan: " + _planned.Count + " asset se doi / " + _scannedTotal + " texture da quet / " +
                _skippedTotal + " bo qua", EditorStyles.boldLabel);

            // Uoc luong bo nho ─ cho Sep thay GIA phai tra TRUOC KHI bam Apply.
            float beforeMb = (float)(_memBeforeBytes / (1024.0 * 1024.0));
            float afterMb  = (float)(_memAfterBytes  / (1024.0 * 1024.0));
            float deltaMb  = afterMb - beforeMb;
            float projMb   = MEASURED_TEXTURE_MB + deltaMb;
            EditorGUILayout.HelpBox(BuildMemoryReport(beforeMb, afterMb, deltaMb, projMb),
                projMb >= DANGER_TEXTURE_MB ? MessageType.Error
                    : (projMb >= SAFE_TEXTURE_MB ? MessageType.Warning : MessageType.Info));

            _scrollReport = EditorGUILayout.BeginScrollView(_scrollReport, GUILayout.MinHeight(180f));
            int shown = 0;
            foreach (var c in _planned)
            {
                if (shown++ >= 400)
                {
                    EditorGUILayout.LabelField("... con " + (_planned.Count - 400) +
                                               " asset nua (xem day du trong Console).");
                    break;
                }
                EditorGUILayout.LabelField(c.AssetPath, EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("    " + string.Join("  |  ", c.Diffs.ToArray()), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_ppuReport))
                EditorGUILayout.HelpBox(_ppuReport, MessageType.Warning);
        }

        // ── Restore ─────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("5. Restore from backup", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (_backupFolders.Count == 0)
        {
            EditorGUILayout.LabelField("Chua co backup nao.");
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshBackupList();
        }
        else
        {
            _selectedBackup = Mathf.Clamp(_selectedBackup, 0, _backupFolders.Count - 1);
            var names = _backupFolders.Select(Path.GetFileName).ToArray();
            _selectedBackup = EditorGUILayout.Popup(_selectedBackup, names);
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshBackupList();
            if (GUILayout.Button("Restore", GUILayout.Width(90f)))
            {
                _pendingRestoreFolder = _backupFolders[_selectedBackup];
                _pending = PendingAction.Restore;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // ── Chay hanh dong da hen, HAN TOAN NGOAI vong OnGUI ──────────────
        // delayCall chay o tick tiep theo cua Editor, khong con trong stack IMGUI,
        // nen dialog / progress bar / reimport khong the lam vo layout.
        if (_pending != PendingAction.None)
        {
            var todo = _pending;
            var folder = _pendingRestoreFolder;
            _pending = PendingAction.None;

            EditorApplication.delayCall += () =>
            {
                switch (todo)
                {
                    case PendingAction.Scan:    RunScan(); break;
                    case PendingAction.Apply:   RunApply(); break;
                    case PendingAction.Restore: RunRestore(folder); break;
                }
                Repaint();
            };
        }
    }

    private bool AnyCategoryEnabled()
    {
        return _catMaxSize || _catMipMaps || _catFilterMode || _catAniso || _catAndroid || _catIOS;
    }

    private string BuildOptionSignature()
    {
        return string.Join("|", new[]
        {
            _scopeFolder, _astcBlock.ToString(), _maxTextureSize.ToString(),
            _enableMipMaps.ToString(), _filterMode.ToString(), _anisoLevel.ToString(),
            _catMaxSize.ToString(), _catMipMaps.ToString(), _catFilterMode.ToString(),
            _catAniso.ToString(), _catAndroid.ToString(), _catIOS.ToString(),
            _skipNormalMaps.ToString(), _skipLightmaps.ToString()
        });
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  PRESET + UOC LUONG BO NHO
    // ═════════════════════════════════════════════════════════════════════════

    private static string PresetLabel(Preset p)
    {
        switch (p)
        {
            case Preset.NetVua:          return "Net vua, an toan RAM";
            case Preset.NetToiDa:        return "Net toi da (RUI RO RAM)";
            case Preset.ChiIconBiThuNho: return "Chi icon bi thu nho (512 -> 1024)";
            case Preset.TileSheet:       return "Tile sheet (tat mipmap)";
            case Preset.VfxRe:           return "VFX / particle (re RAM)";
            default:                     return "Custom (sua tay)";
        }
    }

    /// <summary>
    /// Gan san toan bo tuy chon theo preset. KHONG ghi asset, KHONG mo dialog
    /// → goi thang trong OnGUI van an toan.
    /// Ly do tung con so nam trong ROUND2_TEXTURE_NOTES.md.
    /// </summary>
    private void ApplyPreset(Preset p)
    {
        _preset = p;

        // Mac dinh chung cho moi preset: KHONG dung toi PPU, khong dung toi filter Point
        // cua pixel-art (Sep tu doi neu muon), mipmap MAC DINH TAT.
        _catMaxSize    = true;
        _catMipMaps    = true;
        _catFilterMode = false;   // filterMode lech nhau la CO Y o vai texture → khong dong loat
        _catAniso      = false;   // aniso vo nghia khi mipmap tat + tier Android tat anisotropic
        _catAndroid    = true;
        _catIOS        = true;
        _enableMipMaps = false;
        _filterMode    = FilterMode.Bilinear;
        _anisoLevel    = 1;

        switch (p)
        {
            case Preset.NetVua:
                // Anh trong Art/ hau het <= 1024 roi, cai loi chinh la ASTC 6x6 be khoi.
                _scopeFolder    = "Assets/Art";
                _maxTextureSize = 1024;
                _astcBlock      = AstcBlock.ASTC_5x5;
                break;

            case Preset.NetToiDa:
                // CANH BAO: 2048 + 4x4 tren toan project la duong OOM. Chi de do thu.
                _scopeFolder    = "Assets";
                _maxTextureSize = 2048;
                _astcBlock      = AstcBlock.ASTC_4x4;
                break;

            case Preset.ChiIconBiThuNho:
                // Nhom Assetsgame/Icon* dang bi ep 512 tu anh goc ~2700px.
                _scopeFolder    = "Assets/Assetsgame";
                _maxTextureSize = 1024;
                _astcBlock      = AstcBlock.ASTC_5x5;
                break;

            case Preset.TileSheet:
                // maptitle/Map45Iso/Sheet_*.png cat sat nhau, KHONG co gutter.
                // Bat mipmap o day = lo duong ke giua cac o → tuyet doi de mipmap OFF.
                _scopeFolder    = "Assets/maptitle";
                _maxTextureSize = 2048;
                _astcBlock      = AstcBlock.ASTC_5x5;
                _enableMipMaps  = false;
                break;

            case Preset.VfxRe:
                _scopeFolder    = "Assets/Lana Studio";
                _maxTextureSize = 512;
                _astcBlock      = AstcBlock.ASTC_8x8;
                break;
        }

        _hasScanned = false;   // doi tuy chon → phai scan lai
        GUI.FocusControl(null);
    }

    /// <summary>Bit tren moi pixel cua mot block ASTC (luon 128 bit / block).</summary>
    private static double BitsPerPixel(AstcBlock b)
    {
        switch (b)
        {
            case AstcBlock.ASTC_4x4: return 128.0 / 16.0;   // 8.00
            case AstcBlock.ASTC_5x5: return 128.0 / 25.0;   // 5.12
            case AstcBlock.ASTC_8x8: return 128.0 / 64.0;   // 2.00
            default:                 return 128.0 / 36.0;   // 6x6 = 3.56
        }
    }

    private static double BitsPerPixelForFormat(TextureImporterFormat f)
    {
        switch (f)
        {
            case TextureImporterFormat.ASTC_4x4:   return 8.00;
            case TextureImporterFormat.ASTC_5x5:   return 5.12;
            case TextureImporterFormat.ASTC_6x6:   return 128.0 / 36.0;
            case TextureImporterFormat.ASTC_8x8:   return 2.00;
            case TextureImporterFormat.ASTC_10x10: return 1.28;
            case TextureImporterFormat.ASTC_12x12: return 128.0 / 144.0;
            case TextureImporterFormat.RGBA32:     return 32.0;
            case TextureImporterFormat.ARGB32:     return 32.0;
            case TextureImporterFormat.RGB24:      return 24.0;
            case TextureImporterFormat.ETC2_RGBA8: return 8.00;
            case TextureImporterFormat.ETC2_RGB4:  return 4.00;
            case TextureImporterFormat.ETC_RGB4:   return 4.00;
            default:                               return 8.00;  // doan an toan (cao)
        }
    }

    // TextureImporter.GetWidthAndHeight(ref int, ref int) la method non-public,
    // co mat tu Unity 5 den nay. Dung reflection + cache, neu Unity doi ten thi
    // _sourceSizeMethod = null va ta im lang bo qua (chi mat phan uoc luong, KHONG loi build).
    private static MethodInfo _sourceSizeMethod;
    private static bool _sourceSizeMethodResolved;

    private static bool TryGetSourceSize(TextureImporter ti, out int w, out int h)
    {
        w = 0; h = 0;
        if (ti == null) return false;

        if (!_sourceSizeMethodResolved)
        {
            _sourceSizeMethodResolved = true;
            try
            {
                _sourceSizeMethod = typeof(TextureImporter).GetMethod(
                    "GetWidthAndHeight",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            catch { _sourceSizeMethod = null; }
        }
        if (_sourceSizeMethod == null) return false;

        try
        {
            var args = new object[] { 0, 0 };
            _sourceSizeMethod.Invoke(ti, args);
            w = (int)args[0];
            h = (int)args[1];
            return w > 0 && h > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Bo nho mot texture chiem tren may, theo cong thuc:
    ///   byte = w * h * bpp / 8, nhan them 4/3 neu co mipmap.
    /// Unity ep canh dai nhat xuong con <paramref name="cap"/>, giu ty le.
    /// </summary>
    private static double EstimateBytes(int srcW, int srcH, int cap, double bpp, bool mip)
    {
        if (srcW <= 0 || srcH <= 0 || cap <= 0) return 0.0;

        double w = srcW;
        double h = srcH;
        double longest = Math.Max(w, h);
        if (longest > cap)
        {
            double k = cap / longest;
            w = Math.Max(1.0, Math.Round(w * k));
            h = Math.Max(1.0, Math.Round(h * k));
        }
        double bytes = w * h * bpp / 8.0;
        if (mip) bytes *= 4.0 / 3.0;   // chuoi mipmap day du = +33%
        return bytes;
    }

    private string BuildMemoryReport(float beforeMb, float afterMb, float deltaMb, float projMb)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UOC LUONG BO NHO TEXTURE (Android, format ASTC)");
        sb.AppendLine("  Trong pham vi \"" + _scopeFolder + "\":  truoc " + beforeMb.ToString("F1") +
                      " MB  →  sau " + afterMb.ToString("F1") + " MB   (" +
                      (deltaMb >= 0f ? "+" : "") + deltaMb.ToString("F1") + " MB)");
        sb.AppendLine("  Da do tren may that: " + MEASURED_TEXTURE_MB.ToString("F0") +
                      " MB texture / 2864 MB RAM tong.");
        sb.AppendLine("  Du bao sau khi Apply: ~" + projMb.ToString("F0") + " MB texture.");
        if (_memUnknownSource > 0)
            sb.AppendLine("  (" + _memUnknownSource + " texture khong doc duoc kich thuoc goc → con so that co the CAO HON.)");
        sb.AppendLine();
        if (projMb >= DANGER_TEXTURE_MB)
            sb.Append("NGUY HIEM: vuot " + DANGER_TEXTURE_MB.ToString("F0") +
                      " MB. May Android 4 GB rat de bi OOM-kill, ma Google Play tinh OOM la CRASH. Ha maxTextureSize hoac dung ASTC block to hon.");
        else if (projMb >= SAFE_TEXTURE_MB)
            sb.Append("CAN THAN: vuot tran an toan " + SAFE_TEXTURE_MB.ToString("F0") +
                      " MB. Nen thu nghiem tren may 4 GB that truoc khi ship.");
        else
            sb.Append("OK: van trong tran an toan " + SAFE_TEXTURE_MB.ToString("F0") + " MB cho may 4 GB.");
        sb.AppendLine();
        sb.Append("Nho: game dang NGHEN CPU (90.05 ms) chu khong nghen GPU (5.75 ms) → doi do net gan nhu KHONG doi fps, chi doi RAM.");
        return sb.ToString();
    }

    private TextureImporterFormat TargetFormat()
    {
        switch (_astcBlock)
        {
            case AstcBlock.ASTC_4x4: return TextureImporterFormat.ASTC_4x4;
            case AstcBlock.ASTC_6x6: return TextureImporterFormat.ASTC_6x6;
            case AstcBlock.ASTC_8x8: return TextureImporterFormat.ASTC_8x8;
            default:                 return TextureImporterFormat.ASTC_5x5;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LOC ASSET
    // ═════════════════════════════════════════════════════════════════════════

    private static bool PathIsExcluded(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return true;
        string p = assetPath.Replace('\\', '/');
        if (!p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (p.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.IndexOf("/Library/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (p.IndexOf(BACKUP_PREFIX, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private bool ShouldProcess(TextureImporter ti)
    {
        if (ti == null) return false;

        if (_skipNormalMaps && (ti.textureType == TextureImporterType.NormalMap || ti.convertToNormalmap))
            return false;
        if (_skipLightmaps && ti.textureType == TextureImporterType.Lightmap)
            return false;

        // Chi xu ly Sprite va Texture2D thuong (Default). Cursor/Cookie/GUI/SingleChannel... bo qua.
        return ti.textureType == TextureImporterType.Sprite || ti.textureType == TextureImporterType.Default;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SCAN (DRY RUN) — KHONG GHI GI
    // ═════════════════════════════════════════════════════════════════════════

    private void RunScan()
    {
        _planned.Clear();
        _scannedTotal = 0;
        _skippedTotal = 0;
        _ppuReport = "";
        _hasScanned = false;
        _memBeforeBytes = 0.0;
        _memAfterBytes = 0.0;
        _memUnknownSource = 0;

        if (!AssetDatabase.IsValidFolder(_scopeFolder))
        {
            EditorUtility.DisplayDialog("Restore Texture Quality",
                "Folder khong hop le: " + _scopeFolder, "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _scopeFolder });
        var ppuBuckets = new Dictionary<float, List<string>>();

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (i % 25 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Scan (dry run)", path, (float)i / Mathf.Max(1, guids.Length)))
                {
                    Debug.LogWarning("[TextureQualityRestore] Scan bi huy giua chung.");
                    return;
                }

                if (PathIsExcluded(path)) { _skippedTotal++; continue; }

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!ShouldProcess(ti)) { _skippedTotal++; continue; }

                _scannedTotal++;
                AccumulateMemory(ti);

                // Thu thap PPU (chi BAO CAO, khong bao gio sua)
                if (ti.textureType == TextureImporterType.Sprite)
                {
                    float ppu = ti.spritePixelsPerUnit;
                    if (!ppuBuckets.TryGetValue(ppu, out var list))
                    {
                        list = new List<string>();
                        ppuBuckets[ppu] = list;
                    }
                    list.Add(path);
                }

                var change = BuildPlan(ti, path);
                if (change != null) _planned.Add(change);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        _ppuReport = BuildPpuReport(ppuBuckets);
        _optionSignatureAtScan = BuildOptionSignature();
        _hasScanned = true;

        var sb = new StringBuilder();
        sb.AppendLine("═══ [TextureQualityRestore] SCAN (DRY RUN) — KHONG CO GI BI SUA ═══");
        sb.AppendLine("Pham vi        : " + _scopeFolder);
        sb.AppendLine("Da quet        : " + _scannedTotal + " texture (bo qua " + _skippedTotal + ")");
        sb.AppendLine("Se thay doi    : " + _planned.Count + " asset");
        sb.AppendLine("Dich           : maxSize " + _maxTextureSize + ", mipmap " + (_enableMipMaps ? "ON" : "OFF") +
                      ", filter " + _filterMode + ", aniso " + _anisoLevel + ", " + TargetFormat());
        sb.AppendLine();
        sb.AppendLine(BuildMemoryReport(
            (float)(_memBeforeBytes / (1024.0 * 1024.0)),
            (float)(_memAfterBytes / (1024.0 * 1024.0)),
            (float)((_memAfterBytes - _memBeforeBytes) / (1024.0 * 1024.0)),
            MEASURED_TEXTURE_MB + (float)((_memAfterBytes - _memBeforeBytes) / (1024.0 * 1024.0))));
        sb.AppendLine();
        foreach (var c in _planned)
        {
            sb.AppendLine(c.AssetPath);
            sb.AppendLine("    " + string.Join("  |  ", c.Diffs.ToArray()));
        }
        sb.AppendLine();
        sb.AppendLine(_ppuReport);
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Cong bo nho cua MOT texture vao tong "truoc" va "sau".
    /// Tinh cho CA texture khong phai doi gi, vi chung van chiem RAM.
    /// Chi DOC importer, khong ghi gi.
    /// </summary>
    private void AccumulateMemory(TextureImporter ti)
    {
        int srcW, srcH;
        if (!TryGetSourceSize(ti, out srcW, out srcH))
        {
            _memUnknownSource++;
            return;
        }

        // ── TRUOC: doc dung setting Android dang co ─────────────────────────
        int capBefore = ti.maxTextureSize;
        double bppBefore = 8.0;
        var psAndroid = ti.GetPlatformTextureSettings("Android");
        if (psAndroid != null && psAndroid.overridden)
        {
            capBefore = psAndroid.maxTextureSize;
            bppBefore = BitsPerPixelForFormat(psAndroid.format);
        }
        else
        {
            var def = ti.GetDefaultPlatformTextureSettings();
            if (def != null) capBefore = def.maxTextureSize;
            // Khong override → build lay theo Player Settings.
            // Android dang de ETC1 (khong co alpha!) → 4 bpp.
            bppBefore = 4.0;
        }
        _memBeforeBytes += EstimateBytes(srcW, srcH, capBefore, bppBefore, ti.mipmapEnabled);

        // ── SAU: gia tri dich, chi tinh nhung nhom that su duoc bat ─────────
        int capAfter = _catMaxSize ? _maxTextureSize : capBefore;
        double bppAfter = _catAndroid ? BitsPerPixel(_astcBlock) : bppBefore;
        bool mipAfter = _catMipMaps ? _enableMipMaps : ti.mipmapEnabled;
        if (_catAndroid && _catMaxSize) capAfter = _maxTextureSize;
        _memAfterBytes += EstimateBytes(srcW, srcH, capAfter, bppAfter, mipAfter);
    }

    /// <summary>
    /// So sanh trang thai hien tai voi gia tri dich. Tra ve null neu khong co gi phai doi.
    /// KHONG ghi gi vao importer.
    /// </summary>
    private PlannedChange BuildPlan(TextureImporter ti, string path)
    {
        var c = new PlannedChange { AssetPath = path, Ppu = ti.spritePixelsPerUnit };

        if (_catMaxSize && ti.maxTextureSize != _maxTextureSize)
            c.Diffs.Add("maxTextureSize " + ti.maxTextureSize + " → " + _maxTextureSize);

        if (_catMaxSize)
        {
            var def = ti.GetDefaultPlatformTextureSettings();
            if (def != null && def.maxTextureSize != _maxTextureSize)
                c.Diffs.Add("Default platform maxSize " + def.maxTextureSize + " → " + _maxTextureSize);
        }

        if (_catMipMaps && ti.mipmapEnabled != _enableMipMaps)
            c.Diffs.Add("mipmap " + (ti.mipmapEnabled ? "ON" : "OFF") + " → " + (_enableMipMaps ? "ON" : "OFF"));

        if (_catFilterMode && ti.filterMode != _filterMode)
            c.Diffs.Add("filterMode " + ti.filterMode + " → " + _filterMode);

        if (_catAniso && ti.anisoLevel != _anisoLevel)
            c.Diffs.Add("aniso " + ti.anisoLevel + " → " + _anisoLevel);

        if (_catAndroid) AddPlatformDiff(ti, "Android", c);
        if (_catIOS) AddPlatformDiff(ti, "iPhone", c);

        return c.Diffs.Count > 0 ? c : null;
    }

    private void AddPlatformDiff(TextureImporter ti, string platform, PlannedChange c)
    {
        var ps = ti.GetPlatformTextureSettings(platform);
        if (ps == null) return;

        var want = TargetFormat();
        if (!ps.overridden)
            c.Diffs.Add(platform + ": overridden OFF → ON");
        if (ps.format != want)
            c.Diffs.Add(platform + ": format " + ps.format + " → " + want);
        if (ps.maxTextureSize != _maxTextureSize)
            c.Diffs.Add(platform + ": maxSize " + ps.maxTextureSize + " → " + _maxTextureSize);
        if (ps.compressionQuality != 100)
            c.Diffs.Add(platform + ": quality " + ps.compressionQuality + " → 100");
    }

    /// <summary>
    /// YEU CAU #6: chi BAO CAO su lech spritePixelsToUnits, TUYET DOI khong sua.
    /// Doi PPU = doi kich thuoc world cua moi sprite → vo toan bo bo cuc scene.
    /// </summary>
    private static string BuildPpuReport(Dictionary<float, List<string>> buckets)
    {
        if (buckets.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("── BAO CAO spritePixelsToUnits (CONG CU KHONG SUA MUC NAY) ──");
        if (buckets.Count == 1)
        {
            var only = buckets.First();
            sb.AppendLine("Dong nhat: PPU " + only.Key + " tren " + only.Value.Count + " sprite. Khong co gi phai lo.");
            return sb.ToString();
        }

        sb.AppendLine("Project dang dung " + buckets.Count + " gia tri PPU khac nhau. Sep tu quyet dinh co gom lai hay khong:");
        foreach (var kv in buckets.OrderByDescending(k => k.Value.Count))
        {
            var folders = kv.Value
                .Select(p =>
                {
                    int last = p.LastIndexOf('/');
                    return last > 0 ? p.Substring(0, last) : p;
                })
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            sb.AppendLine("  • PPU " + kv.Key + "  →  " + kv.Value.Count + " sprite, trong " + folders.Count + " folder:");
            foreach (var f in folders.Take(12)) sb.AppendLine("        " + f);
            if (folders.Count > 12) sb.AppendLine("        ... va " + (folders.Count - 12) + " folder nua");
        }
        sb.AppendLine("LY DO KHONG SUA: doi PPU lam moi sprite doi kich thuoc trong world → lech het bo cuc scene,");
        sb.AppendLine("collider, vi tri prefab. Phai sua thu cong tung cum, kem chinh lai scene.");
        return sb.ToString();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  APPLY
    // ═════════════════════════════════════════════════════════════════════════

    private void RunApply()
    {
        if (!_hasScanned || _planned.Count == 0) return;

        if (!EditorUtility.DisplayDialog(
                "Restore Texture Quality — XAC NHAN",
                "Se sua " + _planned.Count + " asset trong " + _scopeFolder + ".\n\n" +
                "Dich: maxSize " + _maxTextureSize + " | mipmap " + (_enableMipMaps ? "ON" : "OFF") +
                " | filter " + _filterMode + " | aniso " + _anisoLevel + " | " + TargetFormat() + "\n\n" +
                "Toan bo .meta bi anh huong se duoc backup ra ngoai Assets/ truoc khi ghi.\n" +
                "KHONG dung toi scene, prefab, .asset hay spritePixelsToUnits.\n\n" +
                "Reimport co the mat vai phut.",
                "Apply", "Huy"))
            return;

        string backupFolder = CreateBackup(_planned.Select(p => p.AssetPath).ToList());
        if (string.IsNullOrEmpty(backupFolder))
        {
            Debug.LogError("[TextureQualityRestore] Backup that bai — DA DUNG, khong sua gi ca.");
            return;
        }

        int changed = 0;
        int failed = 0;
        bool cancelled = false;

        try
        {
            for (int start = 0; start < _planned.Count; start += BATCH_SIZE)
            {
                if (cancelled) break;
                int end = Mathf.Min(start + BATCH_SIZE, _planned.Count);

                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = start; i < end; i++)
                    {
                        string path = _planned[i].AssetPath;

                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Restore Texture Quality — dang ap dung",
                                (i + 1) + "/" + _planned.Count + "  " + path,
                                (float)(i + 1) / _planned.Count))
                        {
                            cancelled = true;
                            break;
                        }

                        try
                        {
                            if (ApplyToAsset(path)) changed++;
                        }
                        catch (Exception e)
                        {
                            failed++;
                            Debug.LogError("[TextureQualityRestore] Loi khi sua " + path + ": " + e.Message);
                        }
                    }
                }
                finally
                {
                    // BAT BUOC: neu khong goi, AssetDatabase bi khoa vinh vien cho den khi restart Unity.
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        int skipped = _scannedTotal - changed;

        var sb = new StringBuilder();
        sb.AppendLine("═══ [TextureQualityRestore] APPLY XONG ═══");
        sb.AppendLine("Da sua        : " + changed + " asset");
        sb.AppendLine("Bo qua        : " + skipped + " (khong nam trong ke hoach / khong can doi)");
        if (failed > 0) sb.AppendLine("Loi           : " + failed + " asset (xem log do o tren)");
        if (cancelled) sb.AppendLine("!! DA HUY GIUA CHUNG — chi mot phan duoc ap dung. Dung nut Restore neu muon tra het ve cu.");
        sb.AppendLine("Backup .meta  : " + backupFolder);
        sb.AppendLine();
        sb.AppendLine("Close and reopen Unity is not required, but a Library reimport will take several minutes.");
        sb.AppendLine();
        sb.AppendLine("VIEC THU CONG CON LAI (cong cu khong tu lam duoc, xem ROUND2_TEXTURE_NOTES.md):");
        sb.AppendLine("  Edit > Project Settings > Player > Android > Texture compression format: doi ETC1 → ASTC");
        sb.AppendLine("  (ETC1 khong co kenh alpha — day la nguyen nhan goc lam sprite trong bi hong.)");
        Debug.Log(sb.ToString());

        _hasScanned = false; // buoc phai scan lai truoc lan Apply sau
        RefreshBackupList();
    }

    /// <summary>
    /// YEU CAU #5: chi dung TextureImporter API. Tra ve true neu co gi that su doi.
    /// </summary>
    private bool ApplyToAsset(string path)
    {
        if (PathIsExcluded(path)) return false;

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!ShouldProcess(ti)) return false;

        bool dirty = false;

        if (_catMaxSize && ti.maxTextureSize != _maxTextureSize)
        {
            ti.maxTextureSize = _maxTextureSize;
            dirty = true;
        }

        if (_catMaxSize)
        {
            var def = ti.GetDefaultPlatformTextureSettings();
            if (def != null && def.maxTextureSize != _maxTextureSize)
            {
                def.maxTextureSize = _maxTextureSize;
                ti.SetPlatformTextureSettings(def);
                dirty = true;
            }
        }

        if (_catMipMaps && ti.mipmapEnabled != _enableMipMaps)
        {
            ti.mipmapEnabled = _enableMipMaps;
            dirty = true;
        }

        if (_catFilterMode && ti.filterMode != _filterMode)
        {
            ti.filterMode = _filterMode;
            dirty = true;
        }

        if (_catAniso && ti.anisoLevel != _anisoLevel)
        {
            ti.anisoLevel = _anisoLevel;
            dirty = true;
        }

        if (_catAndroid && ApplyPlatform(ti, "Android")) dirty = true;
        if (_catIOS && ApplyPlatform(ti, "iPhone")) dirty = true;

        // LUU Y: KHONG dung toi ti.spritePixelsPerUnit — xem BuildPpuReport().

        if (dirty) ti.SaveAndReimport();
        return dirty;
    }

    private bool ApplyPlatform(TextureImporter ti, string platform)
    {
        var ps = ti.GetPlatformTextureSettings(platform);
        if (ps == null) return false;

        var want = TargetFormat();
        bool dirty = false;

        if (!ps.overridden)                        { ps.overridden = true;                  dirty = true; }
        if (ps.format != want)                     { ps.format = want;                      dirty = true; }
        if (ps.maxTextureSize != _maxTextureSize)  { ps.maxTextureSize = _maxTextureSize;   dirty = true; }
        if (ps.compressionQuality != 100)          { ps.compressionQuality = 100;           dirty = true; }

        if (dirty)
        {
            ps.name = platform; // dam bao dung platform khi ghi nguoc lai
            ti.SetPlatformTextureSettings(ps);
        }
        return dirty;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  BACKUP / RESTORE
    // ═════════════════════════════════════════════════════════════════════════

    private static string ProjectRoot()
    {
        // Application.dataPath = <project>/Assets
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    /// <summary>
    /// Chep moi .meta bi anh huong ra &lt;project&gt;/_MetaBackup_yyyyMMdd_HHmmss/, giu nguyen duong dan tuong doi.
    /// Ghi kem manifest .txt. Tra ve duong dan folder backup, hoac null neu that bai.
    /// </summary>
    private string CreateBackup(List<string> assetPaths)
    {
        string root = ProjectRoot();
        string folder = Path.Combine(root, BACKUP_PREFIX + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        try
        {
            Directory.CreateDirectory(folder);

            var manifest = new StringBuilder();
            manifest.AppendLine("# TextureQualityRestoreTool — meta backup");
            manifest.AppendLine("# Thoi diem : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            manifest.AppendLine("# Project   : " + root);
            manifest.AppendLine("# Pham vi   : " + _scopeFolder);
            manifest.AppendLine("# Dich      : maxSize " + _maxTextureSize + " | mipmap " + (_enableMipMaps ? "ON" : "OFF") +
                                " | filter " + _filterMode + " | aniso " + _anisoLevel + " | " + TargetFormat());
            manifest.AppendLine("# Cach khoi phuc: mo lai cong cu → muc \"5. Restore from backup\" → chon folder nay → Restore");
            manifest.AppendLine();

            int copied = 0;
            for (int i = 0; i < assetPaths.Count; i++)
            {
                string metaRel = assetPaths[i] + ".meta";              // vd: Assets/Art/a.png.meta
                string src = Path.Combine(root, metaRel);
                if (!File.Exists(src))
                {
                    manifest.AppendLine("MISSING\t" + metaRel);
                    continue;
                }

                string dst = Path.Combine(folder, metaRel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(src, dst, true);
                manifest.AppendLine("OK\t" + metaRel);
                copied++;

                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Backup .meta", metaRel, (float)i / Mathf.Max(1, assetPaths.Count));
            }

            manifest.AppendLine();
            manifest.AppendLine("# Tong cong da backup: " + copied + "/" + assetPaths.Count + " file .meta");
            File.WriteAllText(Path.Combine(folder, MANIFEST_NAME), manifest.ToString(), Encoding.UTF8);

            Debug.Log("[TextureQualityRestore] Da backup " + copied + " file .meta vao:\n" + folder);
            return folder;
        }
        catch (Exception e)
        {
            Debug.LogError("[TextureQualityRestore] Backup that bai: " + e);
            return null;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void RefreshBackupList()
    {
        _backupFolders.Clear();
        try
        {
            string root = ProjectRoot();
            var dirs = Directory.GetDirectories(root, BACKUP_PREFIX + "*", SearchOption.TopDirectoryOnly);
            _backupFolders.AddRange(dirs.OrderByDescending(d => d));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TextureQualityRestore] Khong doc duoc danh sach backup: " + e.Message);
        }
        _selectedBackup = 0;
    }

    private void RunRestore(string backupFolder)
    {
        if (string.IsNullOrEmpty(backupFolder) || !Directory.Exists(backupFolder))
        {
            EditorUtility.DisplayDialog("Restore", "Folder backup khong ton tai.", "OK");
            return;
        }

        string root = ProjectRoot();
        string assetsInBackup = Path.Combine(backupFolder, "Assets");
        if (!Directory.Exists(assetsInBackup))
        {
            EditorUtility.DisplayDialog("Restore", "Folder backup khong co thu muc Assets/ ben trong.", "OK");
            return;
        }

        var files = Directory.GetFiles(assetsInBackup, "*.meta", SearchOption.AllDirectories);
        if (!EditorUtility.DisplayDialog(
                "Restore from backup — XAC NHAN",
                "Se ghi de " + files.Length + " file .meta tu:\n" + Path.GetFileName(backupFolder) +
                "\n\nMoi thay doi texture sau lan backup nay se mat. Unity se reimport lai.",
                "Restore", "Huy"))
            return;

        int restored = 0;
        int failed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string rel = files[i].Substring(backupFolder.Length).TrimStart('/', '\\');
                    EditorUtility.DisplayProgressBar("Restore .meta", rel, (float)(i + 1) / files.Length);
                    try
                    {
                        string dst = Path.Combine(root, rel);
                        string dstDir = Path.GetDirectoryName(dst);
                        if (!Directory.Exists(dstDir)) { failed++; continue; }
                        File.Copy(files[i], dst, true);
                        restored++;
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Debug.LogError("[TextureQualityRestore] Restore loi " + rel + ": " + e.Message);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        _hasScanned = false;
        Debug.Log("═══ [TextureQualityRestore] RESTORE XONG ═══\n" +
                  "Da khoi phuc : " + restored + " file .meta\n" +
                  (failed > 0 ? "Loi          : " + failed + "\n" : "") +
                  "Tu folder    : " + backupFolder + "\n\n" +
                  "Close and reopen Unity is not required, but a Library reimport will take several minutes.");
    }
}
#endif
