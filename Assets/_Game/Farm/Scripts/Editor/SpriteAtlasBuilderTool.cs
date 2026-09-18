// #define FARM_ATLAS_NO_V2
// ^^^ CUA THOAT HIEM: bo 2 dau "/" o dau dong tren (thanh "#define FARM_ATLAS_NO_V2")
//     la TAT HAN toan bo phan atlas V2. Xem muc "HUONG DAN CUU HO" ben duoi.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// ═════════════════════════════════════════════════════════════════════════
//  API DUNG TRONG FILE NAY — DOC 30 GIAY TRUOC KHI SUA
//
//  RANG BUOC LUC BIEN DICH (thieu mot trong nhung cai nay thi file KHONG bien dich
//  duoc; ma mot file trong Editor/ khong bien dich duoc thi CA PROJECT dung lai):
//    • UnityEngine.U2D.SpriteAtlas               — atlas V1, tuc file .spriteatlas
//    • UnityEditor.U2D.SpriteAtlasExtensions     — SetIncludeInBuild / SetPackingSettings /
//      SetTextureSettings / SetPlatformSettings / Add / GetPackables, ban NHAN SpriteAtlas
//    • SpriteAtlasPackingSettings  { blockOffset, enableRotation, enableTightPacking, padding }
//    • SpriteAtlasTextureSettings  { anisoLevel, filterMode, generateMipMaps, readable, sRGB }
//    • TextureImporterPlatformSettings, TextureImporterFormat (chi ten TYPE, khong ten member)
//    → Hai namespace UnityEditor.U2D / UnityEngine.U2D den tu package com.unity.2d.sprite.
//      DA KIEM TRA Packages/manifest.json cua project nay: co "com.unity.2d.sprite": "1.0.0".
//
//  QUA REFLECTION (thieu = tinh nang tu tat, KHONG BAO GIO gay loi bien dich):
//    • UnityEditor.U2D.SpriteAtlasAsset — TOAN BO duong atlas V2
//    • Load / Save / Add / GetPackables / SetIncludeInBuild / SetPackingSettings /
//      SetTextureSettings / SetPlatformSettings cua SpriteAtlasAsset
//    • TextureImporter.GetSourceTextureWidthAndHeight
//    • Ten member ASTC cua TextureImporterFormat (ASTC_6x6 hay ASTC_RGBA_6x6) — tim theo TEN
//
//  ─────────────────────────────────────────────────────────────────────
//  HUONG DAN CUU HO — LAM THEO DUNG THU TU NEU KHONG BIEN DICH DUOC
//  ─────────────────────────────────────────────────────────────────────
//
//  B1. Loi nam o dong nao co chu V2 / SpriteAtlasAsset / V2Available / TryCreateAtlasV2 /
//      TryReadV2Packables / ResolveV2 / FindV2Method / InvokeV2 / FindTypeByName ?
//        → Mo DONG 1 cua file nay, bo 2 dau "/" o dau dong, de thanh:
//              #define FARM_ATLAS_NO_V2
//        → Luu file. HET. Toan bo phan V2 bi cat khoi ban bien dich.
//        → KHONG MAT GI: 3 atlas cua project deu la V1, va mac dinh cua cong cu cung la V1.
//
//  B2. Lam B1 roi van loi, va loi van nam trong khoi "CAU NOI V2"?
//        → XOA HAN tu DONG 1118 den DONG 1339, XOA CA HAI DONG DO.
//          (De doi chieu: dong 1118 phai la  #if !FARM_ATLAS_NO_V2
//                         dong 1339 phai la  #endif )
//        → Roi van de nguyen B1 o tren.
//
//  B3. Loi noi ve SpriteAtlas / SpriteAtlasExtensions / SpriteAtlasPackingSettings /
//      SpriteAtlasTextureSettings (KHONG co chu "Asset" trong ten)?
//        → Cai nay khong va duoc bang cach xoa vai dong — no la loi cua ca cong cu.
//        → Cach nhanh nhat de project bien dich lai: XOA HAN 2 file
//              Assets/_Game/Farm/Scripts/Editor/SpriteAtlasBuilderTool.cs
//              Assets/_Game/Farm/Scripts/Editor/SpriteAtlasBuilderTool.cs.meta
//          Khong co gi khac trong project phu thuoc vao file nay — xoa la project chay lai ngay.
// ═════════════════════════════════════════════════════════════════════════

/// <summary>
/// Tools ▸ Farm Game ▸ Sprite Atlas Builder
///
/// MUC DICH: gom sprite roi le vao Sprite Atlas de giam draw call.
///
/// HIEN TRANG DA DO:
///   • ProjectSettings/EditorSettings.asset → m_SpritePackerMode: 5 (Sprite Atlas V2, Always Enabled)
///     → packer DA BAT, nhung gan nhu khong co atlas nao de packer lam viec.
///   • Chi co 3 atlas, va deu la UI:
///       Assets/_Game/Resources/Atlases/Atlas_UI_Icons.spriteatlas   (60 packable)
///       Assets/_Game/Resources/Atlases/Atlas_UI_Popups.spriteatlas  (65 packable)
///       Assets/maptitle/AssetsTitl/Sprites/map.spriteatlasv2        (2 packable)
///   • Project co 1986 file PNG → khoang 1859 sprite CHUA nam trong atlas nao.
///     Moi texture rieng = it nhat 1 draw call vi SpriteRenderer khong the batch
///     hai sprite khac texture.
///   • SCN_Farm.unity tham chieu thang 165 GUID sprite, chua ke 540 prefab instance ben trong.
///
/// NGUYEN TAC AN TOAN:
///   1. Luon SCAN (dry run) truoc. Nut "Tao atlas" bi khoa cho den khi scan xong.
///   2. Cong cu KHONG BAO GIO sua file PNG goc, KHONG sua .meta cua PNG,
///      KHONG sua scene / prefab. No CHI TAO THEM file .spriteatlas moi.
///      → Vi vay viec nay tu no da la reversible: xoa atlas di la ve nhu cu 100%.
///   3. Moi asset duoc tao deu ghi vao 1 manifest ngoai Assets/. Nut "Xoa atlas do
///      cong cu nay tao" chi xoa DUNG nhung duong dan trong manifest, khong dong gi khac.
///   4. Khong bao giu goi Scan / Apply THANG trong OnGUI (progress bar + dialog giua
///      hai lan Begin/EndHorizontal se nem "ArgumentException: GUILayout mismatched").
///      Ghi y dinh vao _pending, chay o EditorApplication.delayCall.
///
/// File nam trong Editor/ nen #if UNITY_EDITOR ve ky thuat la thua,
/// nhung giu cho dong bo voi TextureQualityRestoreTool.cs / FarmResetTool.cs cung thu muc.
/// </summary>
public class SpriteAtlasBuilderTool : EditorWindow
{
    private const string MENU_PATH = "Tools/Farm Game/Sprite Atlas Builder";
    private const string MANIFEST_PREFIX = "_AtlasBuilderManifest_";
    private const string DEFAULT_OUTPUT = "Assets/_Game/Atlases/World";

    // He so lap day thuc te cua thuat toan pack (khong bao gio dat 100% vi padding + khe ho).
    private const float PACK_EFFICIENCY = 0.80f;

    // ═════════════════════════════════════════════════════════════════════════
    //  TUY CHON
    // ═════════════════════════════════════════════════════════════════════════

    private static readonly string[] DEFAULT_SOURCE_FOLDERS =
    {
        "Assets/Assetsgame",
        "Assets/maptitle",
        "Assets/Art",
        "Assets/_Game",
        "Assets/NV_NPC",
        "Assets/Lana Studio",
        "Assets/Day_Night"
    };

    /// <summary>Cach chia nhom. Xem help text trong OnGUI de biet danh doi.</summary>
    private enum GroupMode
    {
        PerTopLevelFolder = 0,   // 1 atlas / thu muc goc  (mac dinh)
        PerImmediateSubfolder = 1 // 1 atlas / thu muc con cap 1
    }

    private enum AstcBlock { ASTC_4x4 = 0, ASTC_5x5 = 1, ASTC_6x6 = 2 }

    private enum AtlasFormat
    {
        // MAC DINH. Type SpriteAtlas (V1) duoc rang buoc luc BIEN DICH — chac chan co.
        // 3 atlas Sep dang co deu la V1 (isAtlasV2: 0) nen chon V1 la dong bo nhat.
        V1_spriteatlas = 0,
        // Duong V2 di HOAN TOAN qua reflection (xem muc "CAU NOI V2" o cuoi file).
        // Khong co dong code nao tham chieu SpriteAtlasAsset luc bien dich.
        V2_spriteatlasv2 = 1
    }

    private readonly List<string> _sourceFolders = new List<string>(DEFAULT_SOURCE_FOLDERS);
    private GroupMode _groupMode = GroupMode.PerTopLevelFolder;
    private readonly List<string> _perSubfolderOverrides = new List<string>();
    private int _maxPageSize = 2048;
    private int _padding = 4;
    private AstcBlock _astcBlock = AstcBlock.ASTC_6x6; // = textureFormat 50, khop Atlas_UI_Icons
    private int _compressionQuality = 50;              // khop Atlas_UI_Icons
    private bool _alsoIOS = true;                      // Atlas_UI_Icons co ca iOS override
    private AtlasFormat _atlasFormat = AtlasFormat.V1_spriteatlas; // V1 = duong an toan, mac dinh
    private string _outputFolder = DEFAULT_OUTPUT;
    private bool _excludeTilemapPaletteSprites; // mac dinh: CHI CANH BAO, khong loai
    private bool _includeInBuild = true;

    // ═════════════════════════════════════════════════════════════════════════
    //  TRANG THAI
    // ═════════════════════════════════════════════════════════════════════════

    private sealed class SpriteEntry
    {
        public string Path;
        public int Width;
        public int Height;
        public bool InTilemapPalette;
        public long Area { get { return (long)Width * Height; } }
    }

    private sealed class AtlasGroup
    {
        public string Name;
        public readonly List<SpriteEntry> Sprites = new List<SpriteEntry>();
        public long TotalArea;
        public int PaletteCount;
        public bool TooBigForOnePage;
    }

    private readonly List<AtlasGroup> _groups = new List<AtlasGroup>();
    private readonly List<string> _skipLog = new List<string>();
    private readonly Dictionary<string, int> _skipCounts = new Dictionary<string, int>();
    private int _scannedTotal;
    private int _acceptedTotal;
    private bool _hasScanned;
    private string _optionSignatureAtScan = "";
    private Vector2 _scrollMain;
    private Vector2 _scrollTable;
    private string _lastManifest = "";
    private readonly List<string> _manifests = new List<string>();
    private int _selectedManifest;

    private enum PendingAction { None, Scan, Apply, RemoveCreated }
    private PendingAction _pending = PendingAction.None;
    private string _pendingManifest;

    // ═════════════════════════════════════════════════════════════════════════
    //  CUA VAO
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_PATH, false, 40)]
    public static void Open()
    {
        var win = GetWindow<SpriteAtlasBuilderTool>(false, "Sprite Atlas Builder", true);
        win.minSize = new Vector2(680f, 600f);
        win.RefreshManifestList();
        win.Show();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  GIAO DIEN
    // ═════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        _scrollMain = EditorGUILayout.BeginScrollView(_scrollMain);

        EditorGUILayout.HelpBox(
            "Gom sprite roi le vao Sprite Atlas de giam draw call.\n" +
            "Cong cu nay CHI TAO THEM file atlas moi — KHONG sua PNG, KHONG sua .meta cua PNG, " +
            "KHONG sua scene/prefab. Vi vay no reversible 100%: xoa atlas di la project ve nguyen trang.\n" +
            "Luon bam \"Scan (dry run)\" truoc — nut tao atlas bi khoa cho den khi scan xong.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();

        // ── 1. Thu muc nguon ────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Thu muc nguon", EditorStyles.boldLabel);
        int removeAt = -1;
        for (int i = 0; i < _sourceFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _sourceFolders[i] = EditorGUILayout.TextField(_sourceFolders[i]);
            bool valid = AssetDatabase.IsValidFolder(_sourceFolders[i]);
            GUILayout.Label(valid ? "OK" : "KHONG CO", GUILayout.Width(70f));
            if (GUILayout.Button("-", GUILayout.Width(24f))) removeAt = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeAt >= 0) _sourceFolders.RemoveAt(removeAt);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Them thu muc", GUILayout.Width(130f))) _sourceFolders.Add("Assets/");
        if (GUILayout.Button("Ve mac dinh", GUILayout.Width(110f)))
        {
            _sourceFolders.Clear();
            _sourceFolders.AddRange(DEFAULT_SOURCE_FOLDERS);
        }
        EditorGUILayout.EndHorizontal();

        // ── 2. Chia nhom ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Cach chia nhom (quan trong nhat)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "DANH DOI — doc ky truoc khi doi:\n\n" +
            "• KHONG duoc nhet tat ca vao 1 atlas. Atlas vuot qua max page size se tu dong bi Unity " +
            "cat thanh nhieu trang (page) — luc do sprite nam khac trang lai KHONG batch duoc voi nhau, " +
            "coi nhu mat sach loi ich, ma minh lai khong kiem soat duoc cai gi nam voi cai gi.\n\n" +
            "• Atlas cang to thi luc chay cang ton RAM: Unity nap CA atlas vao bo nho du man hinh chi can " +
            "1 sprite trong do. Tren Android may yeu day la nguyen nhan tut frame / bi kill.\n\n" +
            "• Nguyen tac dung: gom theo \"cai gi cung hien tren man hinh MOT LUC\". " +
            "Mac dinh la 1 atlas / thu muc goc (Assetsgame, maptitle, Art, ...) vi trong project nay " +
            "thu muc goc gan trung voi bo art dung chung canh. Neu 1 thu muc goc qua to, " +
            "chuyen sang \"1 atlas / thu muc con cap 1\" cho rieng no o o ben duoi.\n\n" +
            "• Khi mot nhom vuot qua dien tich 1 trang, cong cu tu tach thanh _1, _2, ... " +
            "Tach CHU DONG nhu vay tot hon de Unity tu cat, vi minh biet truoc sprite nao roi vao dau.",
            MessageType.None);
        _groupMode = (GroupMode)EditorGUILayout.EnumPopup("Kieu chia nhom", _groupMode);

        EditorGUILayout.LabelField(
            "Ep rieng vai thu muc goc thanh \"1 atlas / thu muc con\" (de trong = theo kieu o tren):",
            EditorStyles.miniLabel);
        int rmOv = -1;
        for (int i = 0; i < _perSubfolderOverrides.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _perSubfolderOverrides[i] = EditorGUILayout.TextField(_perSubfolderOverrides[i]);
            if (GUILayout.Button("-", GUILayout.Width(24f))) rmOv = i;
            EditorGUILayout.EndHorizontal();
        }
        if (rmOv >= 0) _perSubfolderOverrides.RemoveAt(rmOv);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Them override", GUILayout.Width(130f)))
            _perSubfolderOverrides.Add("Assets/Assetsgame");
        if (GUILayout.Button("Goi y (Assetsgame + maptitle)", GUILayout.Width(220f)))
        {
            _perSubfolderOverrides.Clear();
            _perSubfolderOverrides.Add("Assets/Assetsgame");
            _perSubfolderOverrides.Add("Assets/maptitle");
        }
        EditorGUILayout.EndHorizontal();

        // ── 3. Thong so atlas ───────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Thong so atlas", EditorStyles.boldLabel);
        _maxPageSize = EditorGUILayout.IntPopup("Max page size", _maxPageSize,
            new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 });
        _padding = EditorGUILayout.IntSlider(
            new GUIContent("Padding", "Khoang cach giua cac sprite trong atlas. 4 = gia tri Atlas_UI_Icons dang dung. Nho qua se bi ri mau canh sprite khi bilinear."),
            _padding, 2, 16);
        _astcBlock = (AstcBlock)EditorGUILayout.EnumPopup(
            new GUIContent("Android/iOS format", "Atlas_UI_Icons dang dung textureFormat 50 = ASTC 6x6. Giu nguyen de dong bo voi lua chon Sep da chot."),
            _astcBlock);
        _compressionQuality = EditorGUILayout.IntSlider("Compression quality", _compressionQuality, 0, 100);
        _alsoIOS = EditorGUILayout.ToggleLeft("Ghi luon override iOS (giong Atlas_UI_Icons)", _alsoIOS);
        _includeInBuild = EditorGUILayout.ToggleLeft("Include in build", _includeInBuild);
        _atlasFormat = (AtlasFormat)EditorGUILayout.EnumPopup(
            new GUIContent("Dinh dang atlas", "V1 (.spriteatlas) = MAC DINH, giong het 3 atlas Sep dang co (isAtlasV2: 0).\nV2 (.spriteatlasv2) chi chay duoc neu ban Unity nay co API V2 — cong cu tu do va tu lui ve V1 neu khong co."),
            _atlasFormat);
        if (_atlasFormat == AtlasFormat.V1_spriteatlas)
            EditorGUILayout.HelpBox(
                "Dang chon V1 (mac dinh, an toan nhat). 3 atlas hien tai cua project dung dung kieu nay " +
                "(isAtlasV2: 0), nen chon V1 se dong bo nhat.",
                MessageType.None);
#if FARM_ATLAS_NO_V2
        else
            EditorGUILayout.HelpBox(
                "Unity nay khong ho tro atlas V2, dung V1.\n" +
                "(Phan V2 dang bi TAT bang FARM_ATLAS_NO_V2.) Cong cu se tao file .spriteatlas (V1).",
                MessageType.Warning);
#else
        else if (!V2Available())
            EditorGUILayout.HelpBox(
                "Unity nay khong ho tro atlas V2, dung V1.\n" +
                "Cong cu se tu dong tao file .spriteatlas (V1) thay vi .spriteatlasv2. Khong co loi gi xay ra.",
                MessageType.Warning);
#endif
        _outputFolder = EditorGUILayout.TextField("Thu muc xuat", _outputFolder);

        EditorGUILayout.Space(2f);
        _excludeTilemapPaletteSprites = EditorGUILayout.ToggleLeft(
            new GUIContent("Loai luon sprite dang nam trong Tilemap Palette",
                "Mac dinh TAT: cong cu chi DANH DAU chung trong bao cao chu khong loai, vi atlas hoa tile thuong van an toan. Bat len neu Sep muon chac chan khong dung toi tile."),
            _excludeTilemapPaletteSprites);

        if (EditorGUI.EndChangeCheck() && _hasScanned && BuildOptionSignature() != _optionSignatureAtScan)
            _hasScanned = false; // doi tuy chon → ket qua scan cu khong con dung

        // ── 4. Chay ─────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("4. Chay", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan (dry run)", GUILayout.Height(30f)))
            _pending = PendingAction.Scan;

        using (new EditorGUI.DisabledScope(!_hasScanned || _groups.Count == 0))
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.45f, 0.9f, 0.55f);
            if (GUILayout.Button(
                    _hasScanned ? "Tao " + _groups.Count + " atlas" : "Tao atlas (phai scan truoc)",
                    GUILayout.Height(30f)))
                _pending = PendingAction.Apply;
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndHorizontal();

        if (!_hasScanned)
            EditorGUILayout.HelpBox("Nut tao atlas bi khoa. Bam Scan (dry run) truoc.", MessageType.None);

        // ── 5. Bang ket qua scan ────────────────────────────────────────────
        if (_hasScanned)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Ket qua scan: " + _groups.Count + " atlas de xuat / " + _acceptedTotal +
                " sprite nhan / " + _scannedTotal + " texture da duyet", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ten atlas", EditorStyles.miniBoldLabel, GUILayout.Width(300f));
            EditorGUILayout.LabelField("So sprite", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField("Dien tich uoc tinh", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField("Canh bao", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            _scrollTable = EditorGUILayout.BeginScrollView(_scrollTable, GUILayout.MinHeight(200f));
            long pageBudget = PageBudget();
            foreach (var g in _groups)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(g.Name, EditorStyles.miniLabel, GUILayout.Width(300f));
                EditorGUILayout.LabelField(g.Sprites.Count.ToString(), EditorStyles.miniLabel, GUILayout.Width(70f));
                EditorGUILayout.LabelField(
                    string.Format("{0:N0} px ({1:P0} trang)", g.TotalArea, (double)g.TotalArea / pageBudget),
                    EditorStyles.miniLabel, GUILayout.Width(150f));

                var warn = new List<string>();
                if (g.TooBigForOnePage)
                    warn.Add("KHONG VUA 1 TRANG " + _maxPageSize + " — Unity se tu cat page");
                if (g.PaletteCount > 0)
                    warn.Add(g.PaletteCount + " sprite dang o Tilemap Palette");
                EditorGUILayout.LabelField(string.Join(" | ", warn.ToArray()), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (_skipCounts.Count > 0)
            {
                var sb = new StringBuilder("Da bo qua (xem chi tiet trong Console):");
                foreach (var kv in _skipCounts.OrderByDescending(k => k.Value))
                    sb.Append("\n  • ").Append(kv.Key).Append(" : ").Append(kv.Value);
                EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
            }
        }

        // ── 6. Go bo ────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("5. Xoa atlas do cong cu nay tao", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Vi cong cu khong he sua PNG goc, xoa atlas la du de ve nguyen trang.\n" +
            "Nut nay CHI xoa dung nhung duong dan ghi trong manifest duoc chon, khong dong gi khac.",
            MessageType.None);
        EditorGUILayout.BeginHorizontal();
        if (_manifests.Count == 0)
        {
            EditorGUILayout.LabelField("Chua co manifest nao.");
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshManifestList();
        }
        else
        {
            _selectedManifest = Mathf.Clamp(_selectedManifest, 0, _manifests.Count - 1);
            var names = _manifests.Select(Path.GetFileName).ToArray();
            _selectedManifest = EditorGUILayout.Popup(_selectedManifest, names);
            if (GUILayout.Button("Lam moi", GUILayout.Width(90f))) RefreshManifestList();
            if (GUILayout.Button("Xoa atlas", GUILayout.Width(100f)))
            {
                _pendingManifest = _manifests[_selectedManifest];
                _pending = PendingAction.RemoveCreated;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // ── Chay hanh dong da hen, HOAN TOAN NGOAI vong OnGUI ───────────────
        // delayCall chay o tick sau, khong con trong stack IMGUI, nen dialog /
        // progress bar / CreateAsset khong the lam vo layout.
        if (_pending != PendingAction.None)
        {
            var todo = _pending;
            var man = _pendingManifest;
            _pending = PendingAction.None;

            EditorApplication.delayCall += () =>
            {
                switch (todo)
                {
                    case PendingAction.Scan: RunScan(); break;
                    case PendingAction.Apply: RunApply(); break;
                    case PendingAction.RemoveCreated: RunRemoveCreated(man); break;
                }
                Repaint();
            };
        }
    }

    private long PageBudget()
    {
        return (long)(_maxPageSize * (long)_maxPageSize * PACK_EFFICIENCY);
    }

    private string BuildOptionSignature()
    {
        return string.Join("|", new[]
        {
            string.Join(",", _sourceFolders.ToArray()),
            string.Join(",", _perSubfolderOverrides.ToArray()),
            _groupMode.ToString(), _maxPageSize.ToString(), _padding.ToString(),
            _astcBlock.ToString(), _compressionQuality.ToString(), _alsoIOS.ToString(),
            _atlasFormat.ToString(), _outputFolder, _includeInBuild.ToString(),
            _excludeTilemapPaletteSprites.ToString()
        });
    }

    /// <summary>
    /// Tra ve TextureImporterFormat tuong ung, TIM THEO TEN chu khong viet thang
    /// TextureImporterFormat.ASTC_6x6 vao code. Ly do: ten member ASTC tung bi doi
    /// (ASTC_RGBA_6x6 cu → ASTC_6x6 moi). Viet thang ma ban Unity khac ten = LOI BIEN DICH.
    /// Tim theo ten thi sai lam cung chi la "khong tim thay → dung Automatic", khong bao gio vo build.
    /// </summary>
    private TextureImporterFormat TargetFormat()
    {
        string[] candidates;
        switch (_astcBlock)
        {
            case AstcBlock.ASTC_4x4: candidates = new[] { "ASTC_4x4", "ASTC_RGBA_4x4" }; break;
            case AstcBlock.ASTC_5x5: candidates = new[] { "ASTC_5x5", "ASTC_RGBA_5x5" }; break;
            default: candidates = new[] { "ASTC_6x6", "ASTC_RGBA_6x6" }; break;
        }

        foreach (var nm in candidates)
        {
            try
            {
                if (Enum.IsDefined(typeof(TextureImporterFormat), nm))
                    return (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), nm);
            }
            catch (Exception) { /* thu ten tiep theo */ }
        }

        Debug.LogWarning("[AtlasBuilder] Ban Unity nay khong co ten format ASTC nao trong danh sach " +
                         string.Join(", ", candidates) + " → dung Automatic.");
        return TextureImporterFormat.Automatic;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LOC / EXCLUSION
    // ═════════════════════════════════════════════════════════════════════════

    private void NoteSkip(string reason, string path)
    {
        int n;
        _skipCounts.TryGetValue(reason, out n);
        _skipCounts[reason] = n + 1;
        if (_skipLog.Count < 4000) _skipLog.Add(reason + "\t" + path);
    }

    /// <summary>Duong dan bi loai bat ke importer noi gi.</summary>
    private bool PathIsExcluded(string assetPath, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(assetPath)) { reason = "duong dan rong"; return true; }

        string p = assetPath.Replace('\\', '/');

        if (!p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        { reason = "ngoai Assets/ (Packages/ hoac khac)"; return true; }

        if (p.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase) >= 0)
        { reason = "trong Packages/"; return true; }

        if (p.IndexOf("/Library/", StringComparison.OrdinalIgnoreCase) >= 0)
        { reason = "trong Library/"; return true; }

        // Editor/ : icon cua tool, khong bao gio ra man hinh game.
        if (p.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            p.StartsWith("Assets/Editor/", StringComparison.OrdinalIgnoreCase))
        { reason = "trong Editor/"; return true; }

        // Resources/ : nap bang Resources.Load theo DUONG DAN. Dua vao atlas se doi
        // cach bind luc runtime va co the lam sprite tra ve null → bug that su.
        if (p.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            p.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase))
        { reason = "trong Resources/ (nap theo path, atlas hoa se doi binding runtime)"; return true; }

        // Khong tu an chinh thu muc xuat cua minh.
        string outNorm = _outputFolder.Replace('\\', '/').TrimEnd('/');
        if (p.StartsWith(outNorm + "/", StringComparison.OrdinalIgnoreCase))
        { reason = "nam trong thu muc xuat cua cong cu"; return true; }

        string ext = Path.GetExtension(p).ToLowerInvariant();
        if (ext == ".psd" || ext == ".psb")
        { reason = "file nguon .psd/.psb"; return true; }

        return false;
    }

    /// <summary>
    /// Doc packables cua MOI atlas dang co trong project (ca .spriteatlas va .spriteatlasv2),
    /// tra ve tap duong dan sprite da bi atlas hoa. Packable co the la ca 1 THU MUC —
    /// luc do ca cay thu muc do bi loai, neu khong se double-pack.
    /// </summary>
    private void CollectExistingAtlasContents(HashSet<string> excludedPaths, List<string> excludedFolders)
    {
        // FindAssets("t:SpriteAtlas") bat duoc atlas V1. Atlas V2 (.spriteatlasv2) co main object
        // la SpriteAtlasAsset chu khong phai SpriteAtlas nen co the bi sot → quet them theo duoi file.
        // Bo sot o day = DOUBLE PACK (sprite nam trong 2 atlas, phinh build, batch lung tung).
        var atlasPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
        {
            string ap = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(ap)) atlasPaths.Add(ap.Replace('\\', '/'));
        }
        try
        {
            string dataPath = Application.dataPath.Replace('\\', '/');
            foreach (var pattern in new[] { "*.spriteatlas", "*.spriteatlasv2" })
            {
                foreach (var f in Directory.GetFiles(dataPath, pattern, SearchOption.AllDirectories))
                {
                    string norm = f.Replace('\\', '/');
                    atlasPaths.Add("Assets" + norm.Substring(dataPath.Length));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AtlasBuilder] Khong quet duoc file atlas theo duoi: " + e.Message);
        }

        foreach (var atlasPath in atlasPaths)
        {
            if (string.IsNullOrEmpty(atlasPath)) continue;

            // Bo qua atlas do chinh cong cu nay tao trong thu muc xuat (neu chay lai lan 2
            // thi van muon coi chung la "da co" → giu, nen KHONG bo qua. De nguyen.)

            UnityEngine.Object[] packables = null;

            // V1
            var v1 = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (v1 != null)
            {
                try { packables = v1.GetPackables(); }
                catch (Exception e) { Debug.LogWarning("[AtlasBuilder] Khong doc duoc packables cua " + atlasPath + ": " + e.Message); }
            }

            // V2 — qua reflection. Neu ban Unity khong co API V2 thi tra ve null, KHONG nem.
            // Hau qua xau nhat: khong doc duoc noi dung atlas V2 → co the double-pack vai sprite.
            // Do van tot hon vo han so voi mot loi bien dich chan ca project.
#if !FARM_ATLAS_NO_V2
            if (packables == null)
                packables = TryReadV2Packables(atlasPath);
#endif

            if (packables == null) continue;

            foreach (var o in packables)
            {
                if (o == null) continue;
                string pp = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(pp)) continue;

                if (AssetDatabase.IsValidFolder(pp))
                    excludedFolders.Add(pp.Replace('\\', '/').TrimEnd('/') + "/");
                else
                    excludedPaths.Add(pp.Replace('\\', '/'));
            }
        }
    }

    /// <summary>
    /// Tim moi sprite dang duoc dung boi mot Tilemap Palette. Palette la prefab co
    /// asset GridPalette di kem, nen lay dependency cua prefab do la cach do AN TOAN.
    /// </summary>
    private HashSet<string> CollectTilemapPaletteSprites()
    {
        // OrdinalIgnoreCase: Windows khong phan biet hoa thuong, so sanh thuong se bo sot.
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] guids;
        try { guids = AssetDatabase.FindAssets("t:GridPalette"); }
        catch (Exception) { return result; }

        foreach (var g in guids)
        {
            string palPath = AssetDatabase.GUIDToAssetPath(g);
            if (string.IsNullOrEmpty(palPath)) continue;
            try
            {
                foreach (var dep in AssetDatabase.GetDependencies(palPath, true))
                {
                    string ext = Path.GetExtension(dep).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd")
                        result.Add(dep.Replace('\\', '/'));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AtlasBuilder] Khong doc duoc dependency cua palette " + palPath + ": " + e.Message);
            }
        }
        return result;
    }

    /// <summary>
    /// Lay kich thuoc anh goc. Thu method noi bo GetSourceTextureWidthAndHeight truoc
    /// (khong phai nap texture → nhanh hon nhieu voi ~1900 file); neu khong co thi nap Texture2D.
    /// </summary>
    private static MethodInfo _miSourceSize;
    private static bool _miSourceSizeResolved;

    private static void GetSize(TextureImporter ti, string path, out int w, out int h)
    {
        w = 0; h = 0;

        if (!_miSourceSizeResolved)
        {
            _miSourceSizeResolved = true;
            try
            {
                _miSourceSize = typeof(TextureImporter).GetMethod(
                    "GetSourceTextureWidthAndHeight",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch (Exception) { _miSourceSize = null; }
        }

        if (_miSourceSize != null)
        {
            try
            {
                var args = new object[] { 0, 0 };
                _miSourceSize.Invoke(ti, args);
                w = (int)args[0];
                h = (int)args[1];
                if (w > 0 && h > 0) return;
            }
            catch (Exception) { /* roi xuong fallback */ }
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) { w = tex.width; h = tex.height; }
        if (w <= 0 || h <= 0) { w = 256; h = 256; } // doan an toan de con uoc luong duoc
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SCAN (DRY RUN) — KHONG GHI GI
    // ═════════════════════════════════════════════════════════════════════════

    private void RunScan()
    {
        _groups.Clear();
        _skipLog.Clear();
        _skipCounts.Clear();
        _scannedTotal = 0;
        _acceptedTotal = 0;
        _hasScanned = false;

        var validFolders = _sourceFolders
            .Select(f => f.Replace('\\', '/').TrimEnd('/'))
            .Where(f => !string.IsNullOrEmpty(f) && AssetDatabase.IsValidFolder(f))
            .Distinct()
            .ToArray();

        if (validFolders.Length == 0)
        {
            EditorUtility.DisplayDialog("Sprite Atlas Builder",
                "Khong co thu muc nguon nao hop le.", "OK");
            return;
        }

        var alreadyAtlased = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var alreadyAtlasedFolders = new List<string>();
        var paletteSprites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var buckets = new Dictionary<string, AtlasGroup>(StringComparer.OrdinalIgnoreCase);

        try
        {
            EditorUtility.DisplayProgressBar("Scan (dry run)", "Doc atlas dang co...", 0.02f);
            CollectExistingAtlasContents(alreadyAtlased, alreadyAtlasedFolders);

            EditorUtility.DisplayProgressBar("Scan (dry run)", "Do Tilemap Palette...", 0.06f);
            paletteSprites = CollectTilemapPaletteSprites();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", validFolders);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (i % 25 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Scan (dry run)", path, 0.08f + 0.9f * i / Mathf.Max(1, guids.Length)))
                {
                    Debug.LogWarning("[AtlasBuilder] Scan bi huy giua chung — khong co gi bi sua.");
                    return;
                }

                string reason;
                if (PathIsExcluded(path, out reason)) { NoteSkip(reason, path); continue; }

                string norm = path.Replace('\\', '/');

                if (alreadyAtlased.Contains(norm))
                { NoteSkip("da nam trong atlas khac", path); continue; }

                bool inAtlasedFolder = false;
                for (int k = 0; k < alreadyAtlasedFolders.Count; k++)
                {
                    if (norm.StartsWith(alreadyAtlasedFolders[k], StringComparison.OrdinalIgnoreCase))
                    { inAtlasedFolder = true; break; }
                }
                if (inAtlasedFolder) { NoteSkip("thu muc da duoc atlas khac gom ca cum", path); continue; }

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { NoteSkip("khong phai TextureImporter", path); continue; }

                _scannedTotal++;

                if (ti.textureType != TextureImporterType.Sprite)
                { NoteSkip("textureType khong phai Sprite (" + ti.textureType + ")", path); continue; }

                if (ti.spriteImportMode == SpriteImportMode.Polygon)
                { NoteSkip("spriteImportMode = Polygon (mesh tuy bien, atlas hoa khong an toan)", path); continue; }

                if (ti.spriteImportMode == SpriteImportMode.None)
                { NoteSkip("spriteImportMode = None (khong sinh Sprite)", path); continue; }

                bool inPalette = paletteSprites.Contains(norm);
                if (inPalette && _excludeTilemapPaletteSprites)
                { NoteSkip("dang o trong Tilemap Palette (Sep chon loai)", path); continue; }

                int w, h;
                GetSize(ti, path, out w, out h);

                string groupName = GroupNameFor(norm);
                AtlasGroup grp;
                if (!buckets.TryGetValue(groupName, out grp))
                {
                    grp = new AtlasGroup { Name = groupName };
                    buckets[groupName] = grp;
                }

                grp.Sprites.Add(new SpriteEntry
                {
                    Path = norm,
                    Width = w,
                    Height = h,
                    InTilemapPalette = inPalette
                });
                if (inPalette) grp.PaletteCount++;
                grp.TotalArea += (long)w * h;
                _acceptedTotal++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // ── Tach nhom qua to thanh _1, _2, ... ──────────────────────────────
        long budget = PageBudget();
        foreach (var grp in buckets.Values.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (grp.TotalArea <= budget)
            {
                grp.TooBigForOnePage = false;
                _groups.Add(grp);
                continue;
            }

            // Xep giam dan theo dien tich roi do lan luot vao tung trang.
            var sorted = grp.Sprites.OrderByDescending(s => s.Area).ToList();
            int page = 1;
            AtlasGroup cur = null;

            foreach (var s in sorted)
            {
                if (cur == null || (cur.TotalArea + s.Area > budget && cur.Sprites.Count > 0))
                {
                    cur = new AtlasGroup { Name = grp.Name + "_" + page };
                    _groups.Add(cur);
                    page++;
                }
                cur.Sprites.Add(s);
                cur.TotalArea += s.Area;
                if (s.InTilemapPalette) cur.PaletteCount++;

                // Mot sprite don le to hon ca trang → khong the vua, phai canh bao.
                if (s.Area > budget) cur.TooBigForOnePage = true;
            }
        }

        _optionSignatureAtScan = BuildOptionSignature();
        _hasScanned = true;

        var sb = new StringBuilder();
        sb.AppendLine("═══ [AtlasBuilder] SCAN (DRY RUN) — KHONG CO GI BI SUA ═══");
        sb.AppendLine("Thu muc nguon : " + string.Join(", ", validFolders));
        sb.AppendLine("Kieu chia     : " + _groupMode + "   (override: " +
                      (_perSubfolderOverrides.Count == 0 ? "khong" : string.Join(", ", _perSubfolderOverrides.ToArray())) + ")");
        sb.AppendLine("Max page      : " + _maxPageSize + " (ngan sach dien tich 1 trang ≈ " + budget.ToString("N0") + " px)");
        sb.AppendLine("Da duyet      : " + _scannedTotal + " texture, nhan " + _acceptedTotal + " sprite");
        sb.AppendLine("Atlas de xuat : " + _groups.Count);
        sb.AppendLine();
        foreach (var g in _groups)
        {
            sb.AppendLine(string.Format("{0,-48} {1,5} sprite   {2,14:N0} px   {3:P0} trang{4}{5}",
                g.Name, g.Sprites.Count, g.TotalArea, (double)g.TotalArea / budget,
                g.TooBigForOnePage ? "   [!! KHONG VUA 1 TRANG]" : "",
                g.PaletteCount > 0 ? "   [" + g.PaletteCount + " sprite o Tilemap Palette]" : ""));
        }
        sb.AppendLine();
        sb.AppendLine("── BO QUA ──");
        foreach (var kv in _skipCounts.OrderByDescending(k => k.Value))
            sb.AppendLine(string.Format("{0,6}  {1}", kv.Value, kv.Key));
        sb.AppendLine();
        sb.AppendLine("Chi tiet bo qua (toi da 4000 dong):");
        foreach (var line in _skipLog) sb.AppendLine("  " + line);
        Debug.Log(sb.ToString());
    }

    /// <summary>Ten atlas cho mot duong dan sprite, theo kieu chia + override.</summary>
    private string GroupNameFor(string assetPath)
    {
        // assetPath dang Assets/A/B/C/x.png
        var parts = assetPath.Split('/');
        if (parts.Length < 3) return "Atlas_World_Misc";

        string topFolder = "Assets/" + parts[1];

        bool perSub = _groupMode == GroupMode.PerImmediateSubfolder;
        if (!perSub)
        {
            for (int i = 0; i < _perSubfolderOverrides.Count; i++)
            {
                string ov = _perSubfolderOverrides[i].Replace('\\', '/').TrimEnd('/');
                if (string.Equals(ov, topFolder, StringComparison.OrdinalIgnoreCase)) { perSub = true; break; }
            }
        }

        string raw;
        if (perSub && parts.Length >= 4)
            raw = parts[1] + "_" + parts[2];   // A_B
        else
            raw = parts[1];                    // A

        return "Atlas_" + Sanitize(raw);
    }

    private static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        // gom nhieu '_' lien tiep
        string outp = sb.ToString();
        while (outp.Contains("__")) outp = outp.Replace("__", "_");
        return outp.Trim('_');
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  APPLY — CHI TAO FILE MOI
    // ═════════════════════════════════════════════════════════════════════════

    private void RunApply()
    {
        if (!_hasScanned || _groups.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Atlas Builder", "Phai Scan (dry run) truoc da.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Tao atlas — XAC NHAN",
                "Se tao " + _groups.Count + " file atlas moi trong:\n" + _outputFolder +
                "\n\nTong " + _acceptedTotal + " sprite.\n\n" +
                "KHONG file PNG nao bi sua. KHONG scene / prefab nao bi sua.\n" +
                "Muon huy sau nay: dung nut \"Xoa atlas\" o muc 5.",
                "Tao", "Huy"))
            return;

        if (!EnsureFolder(_outputFolder))
        {
            EditorUtility.DisplayDialog("Sprite Atlas Builder",
                "Khong tao duoc thu muc xuat: " + _outputFolder, "OK");
            return;
        }

        var created = new List<string>();
        int failed = 0;
        bool cancelled = false;

        try
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < _groups.Count; i++)
                {
                    var g = _groups[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Sprite Atlas Builder — dang tao",
                            (i + 1) + "/" + _groups.Count + "  " + g.Name,
                            (float)(i + 1) / _groups.Count))
                    {
                        cancelled = true;
                        break;
                    }

                    try
                    {
                        string p = CreateAtlas(g);
                        if (!string.IsNullOrEmpty(p)) created.Add(p);
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Debug.LogError("[AtlasBuilder] Loi khi tao " + g.Name + ": " + e);
                    }
                }
            }
            finally
            {
                // BAT BUOC: khong goi thi AssetDatabase bi khoa den khi restart Unity.
                AssetDatabase.StopAssetEditing();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        _lastManifest = WriteManifest(created);
        RefreshManifestList();
        _hasScanned = false; // buoc scan lai truoc lan sau

        var sb = new StringBuilder();
        sb.AppendLine("═══ [AtlasBuilder] TAO ATLAS XONG ═══");
        sb.AppendLine("Da tao   : " + created.Count + " atlas");
        if (failed > 0) sb.AppendLine("Loi      : " + failed + " (xem log do o tren)");
        if (cancelled) sb.AppendLine("!! DA HUY GIUA CHUNG — chi mot phan duoc tao. Manifest van ghi dung phan da tao.");
        sb.AppendLine("Manifest : " + _lastManifest);
        sb.AppendLine();
        foreach (var p in created) sb.AppendLine("  + " + p);
        sb.AppendLine();
        sb.AppendLine("── CACH KIEM CHUNG KET QUA ──");
        sb.AppendLine("1. TRUOC KHI DOI: mo Window > Analysis > Frame Debugger, bam Enable, vao SCN_Farm,");
        sb.AppendLine("   ghi lai so draw call (so su kien trong cay ben trai) va chup man hinh lai.");
        sb.AppendLine("2. Sau khi tao atlas: doi Unity pack xong (Window > 2D > Sprite Atlas, hoac vao Play).");
        sb.AppendLine("3. Mo lai Frame Debugger, so lai. Cac lenh 'Draw Dynamic' / 'Draw Mesh' cua");
        sb.AppendLine("   SpriteRenderer phai gop lai it di han.");
        sb.AppendLine("4. Doi chieu them o Game view > Stats: 'Batches' va 'SetPass calls' phai giam.");
        sb.AppendLine("5. Neu khong giam: sprite do co the van khac atlas nhau, hoac bi chen boi");
        sb.AppendLine("   sorting order cua object khac o giua → kiem tra Sorting Layer / Order in Layer.");
        Debug.Log(sb.ToString());

        EditorUtility.DisplayDialog("Sprite Atlas Builder",
            "Da tao " + created.Count + " atlas.\n\nXem Console de biet cach kiem chung bang Frame Debugger.", "OK");
    }

    /// <summary>Tao 1 atlas. Tra ve duong dan asset, hoac null.</summary>
    private string CreateAtlas(AtlasGroup g)
    {
        var objs = new List<UnityEngine.Object>(g.Sprites.Count);
        foreach (var s in g.Sprites)
        {
            var o = AssetDatabase.LoadAssetAtPath<Texture2D>(s.Path);
            if (o != null) objs.Add(o);
        }
        if (objs.Count == 0) return null;

        var packing = new SpriteAtlasPackingSettings
        {
            blockOffset = 1,
            enableRotation = false,      // rotation lam lech gia dinh UV cua vai shader 2D
            enableTightPacking = false,  // tight packing doi mesh sprite → khong an toan khi tham chieu theo rect
            padding = _padding
        };

        var texSettings = new SpriteAtlasTextureSettings
        {
            anisoLevel = 0,
            filterMode = FilterMode.Bilinear,
            generateMipMaps = false,
            readable = false,
            sRGB = true
        };

        var androidSettings = new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = _maxPageSize,
            format = TargetFormat(),
            textureCompression = TextureImporterCompression.Compressed,
            compressionQuality = _compressionQuality
        };

        var iosSettings = new TextureImporterPlatformSettings
        {
            name = "iPhone",
            overridden = true,
            maxTextureSize = _maxPageSize,
            format = TargetFormat(),
            textureCompression = TextureImporterCompression.Compressed,
            compressionQuality = _compressionQuality
        };

        // ── Duong V2: thu qua reflection. That bai = lui ve V1, KHONG nem, KHONG mat du lieu.
#if !FARM_ATLAS_NO_V2
        if (_atlasFormat == AtlasFormat.V2_spriteatlasv2 && V2Available())
        {
            string v2Path = _outputFolder.TrimEnd('/') + "/" + g.Name + ".spriteatlasv2";
            v2Path = AssetDatabase.GenerateUniqueAssetPath(v2Path);

            string err;
            if (TryCreateAtlasV2(v2Path, packing, texSettings, androidSettings, iosSettings, objs.ToArray(), out err))
            {
                AssetDatabase.ImportAsset(v2Path, ImportAssetOptions.ForceUpdate);
                return v2Path;
            }

            Debug.LogWarning("[AtlasBuilder] Tao atlas V2 that bai (" + err + ") → lui ve V1 cho " + g.Name + ".");
        }
#endif

        // ── Duong V1: rang buoc luc bien dich, luon chay duoc.
        string path = _outputFolder.TrimEnd('/') + "/" + g.Name + ".spriteatlas";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var atlas = new SpriteAtlas();
        atlas.SetIncludeInBuild(_includeInBuild);
        atlas.SetPackingSettings(packing);
        atlas.SetTextureSettings(texSettings);
        atlas.SetPlatformSettings(androidSettings);
        if (_alsoIOS) atlas.SetPlatformSettings(iosSettings);
        atlas.Add(objs.ToArray());

        AssetDatabase.CreateAsset(atlas, path);
        return path;
    }

    private static bool EnsureFolder(string folder)
    {
        folder = folder.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folder)) return true;

        var parts = folder.Split('/');
        if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.OrdinalIgnoreCase))
            return false;

        string cur = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
        return AssetDatabase.IsValidFolder(folder);
    }

#if !FARM_ATLAS_NO_V2
    // ══════════════════════════════════════════════════════════════════════════
    //  CAU NOI V2 — TOAN BO BANG REFLECTION
    //
    //  KHONG co dong nao duoi day viet ten SpriteAtlasAsset nhu mot TYPE trong code.
    //  Tat ca chi la chuoi + MethodInfo.Invoke. Nen du ban Unity nay KHONG co API V2
    //  thi file van BIEN DICH BINH THUONG, chi la tinh nang V2 tu tat di.
    //  Cach lam nay giong het GetSourceTextureWidthAndHeight va PlayerSettings.Android
    //  trong RenderSettingsOptimizerTool.cs.
    // ══════════════════════════════════════════════════════════════════════════

    private const string V2_ASSET_TYPE = "UnityEditor.U2D.SpriteAtlasAsset";
    private const string V2_EXT_TYPE = "UnityEditor.U2D.SpriteAtlasExtensions";

    private static bool _v2Resolved;
    private static Type _tV2Asset;
    private static Type _tV2Ext;
    private static MethodInfo _miV2Load;
    private static MethodInfo _miV2Save;
    private static MethodInfo _miV2GetPackables;
    private static MethodInfo _miV2SetIncludeInBuild;
    private static MethodInfo _miV2SetPackingSettings;
    private static MethodInfo _miV2SetTextureSettings;
    private static MethodInfo _miV2SetPlatformSettings;
    private static MethodInfo _miV2Add;

    /// <summary>Tim type theo ten day du, quet het assembly da nap. Khong bao gio nem.</summary>
    private static Type FindTypeByName(string fullName)
    {
        try
        {
            var t = Type.GetType(fullName, false);
            if (t != null) return t;
        }
        catch (Exception) { /* bo qua */ }

        try
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    var t = asms[i].GetType(fullName, false);
                    if (t != null) return t;
                }
                catch (Exception) { /* assembly loi → bo qua */ }
            }
        }
        catch (Exception) { /* bo qua */ }

        return null;
    }

    /// <summary>
    /// Tim mot thao tac V2. Thu 2 cho:
    ///   1. method instance ngay tren SpriteAtlasAsset;
    ///   2. extension method static tren SpriteAtlasExtensions, tham so dau la SpriteAtlasAsset.
    /// Unity tung doi qua lai giua hai cach nay nen phai thu ca hai.
    /// </summary>
    private static MethodInfo FindV2Method(string name, Type[] tailArgs)
    {
        if (tailArgs == null) tailArgs = Type.EmptyTypes;

        if (_tV2Asset != null)
        {
            try
            {
                var m = _tV2Asset.GetMethod(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, tailArgs, null);
                if (m != null) return m;
            }
            catch (Exception) { /* bo qua */ }
        }

        if (_tV2Ext != null && _tV2Asset != null)
        {
            try
            {
                var full = new Type[tailArgs.Length + 1];
                full[0] = _tV2Asset;
                Array.Copy(tailArgs, 0, full, 1, tailArgs.Length);

                var m = _tV2Ext.GetMethod(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null, full, null);
                if (m != null) return m;
            }
            catch (Exception) { /* bo qua */ }
        }

        return null;
    }

    private static void ResolveV2()
    {
        if (_v2Resolved) return;
        _v2Resolved = true;

        _tV2Asset = FindTypeByName(V2_ASSET_TYPE);
        _tV2Ext = FindTypeByName(V2_EXT_TYPE);
        if (_tV2Asset == null) return;

        try
        {
            _miV2Load = _tV2Asset.GetMethod("Load",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        }
        catch (Exception) { _miV2Load = null; }

        try
        {
            _miV2Save = _tV2Asset.GetMethod("Save",
                BindingFlags.Public | BindingFlags.Static, null, new[] { _tV2Asset, typeof(string) }, null);
        }
        catch (Exception) { _miV2Save = null; }

        _miV2GetPackables = FindV2Method("GetPackables", Type.EmptyTypes);
        _miV2SetIncludeInBuild = FindV2Method("SetIncludeInBuild", new[] { typeof(bool) });
        _miV2SetPackingSettings = FindV2Method("SetPackingSettings", new[] { typeof(SpriteAtlasPackingSettings) });
        _miV2SetTextureSettings = FindV2Method("SetTextureSettings", new[] { typeof(SpriteAtlasTextureSettings) });
        _miV2SetPlatformSettings = FindV2Method("SetPlatformSettings", new[] { typeof(TextureImporterPlatformSettings) });
        _miV2Add = FindV2Method("Add", new[] { typeof(UnityEngine.Object[]) });
    }

    /// <summary>Goi method V2 du no la instance method hay extension method static.</summary>
    private static object InvokeV2(MethodInfo m, object target, params object[] args)
    {
        if (m == null) return null;
        if (args == null) args = new object[0];

        if (m.IsStatic)
        {
            var full = new object[args.Length + 1];
            full[0] = target;
            Array.Copy(args, 0, full, 1, args.Length);
            return m.Invoke(null, full);
        }
        return m.Invoke(target, args);
    }

    /// <summary>true = ban Unity nay that su co du API de tao atlas V2.</summary>
    private static bool V2Available()
    {
        ResolveV2();
        return _tV2Asset != null && _miV2Save != null && _miV2Add != null;
    }

    /// <summary>
    /// Doc packables cua mot file atlas V2. Tra ve null neu khong doc duoc
    /// (khong co API, hoac file khong phai V2). KHONG BAO GIO NEM.
    /// </summary>
    private static UnityEngine.Object[] TryReadV2Packables(string atlasPath)
    {
        ResolveV2();
        if (_miV2Load == null || _miV2GetPackables == null) return null;

        try
        {
            var asset = _miV2Load.Invoke(null, new object[] { atlasPath });
            if (asset == null) return null;
            return InvokeV2(_miV2GetPackables, asset) as UnityEngine.Object[];
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AtlasBuilder] Khong doc duoc packables V2 cua " + atlasPath + ": " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Tao 1 atlas V2 hoan toan qua reflection. Tra ve false kem ly do neu that bai —
    /// luc do ben goi tu lui ve V1. KHONG BAO GIO NEM ra ngoai.
    /// </summary>
    private bool TryCreateAtlasV2(
        string path,
        SpriteAtlasPackingSettings packing,
        SpriteAtlasTextureSettings texSettings,
        TextureImporterPlatformSettings androidSettings,
        TextureImporterPlatformSettings iosSettings,
        UnityEngine.Object[] objs,
        out string err)
    {
        err = null;
        ResolveV2();

        if (_tV2Asset == null) { err = "khong co type " + V2_ASSET_TYPE; return false; }
        if (_miV2Save == null) { err = "khong co SpriteAtlasAsset.Save"; return false; }
        if (_miV2Add == null) { err = "khong co Add(Object[])"; return false; }

        try
        {
            object asset = Activator.CreateInstance(_tV2Asset);
            if (asset == null) { err = "khong tao duoc instance"; return false; }

            // Cac setter duoi day chi la "co thi tot": thieu mot cai khong lam hong atlas,
            // chi la atlas dung gia tri mac dinh cua Unity cho muc do.
            if (_miV2SetIncludeInBuild != null) InvokeV2(_miV2SetIncludeInBuild, asset, _includeInBuild);
            if (_miV2SetPackingSettings != null) InvokeV2(_miV2SetPackingSettings, asset, packing);
            if (_miV2SetTextureSettings != null) InvokeV2(_miV2SetTextureSettings, asset, texSettings);
            if (_miV2SetPlatformSettings != null)
            {
                InvokeV2(_miV2SetPlatformSettings, asset, androidSettings);
                if (_alsoIOS) InvokeV2(_miV2SetPlatformSettings, asset, iosSettings);
            }

            // Add thi BAT BUOC phai co: khong co no thi atlas rong, vo nghia.
            InvokeV2(_miV2Add, asset, (object)objs);

            _miV2Save.Invoke(null, new object[] { asset, path });
            return true;
        }
        catch (Exception e)
        {
            // TargetInvocationException boc loi that ben trong → lay InnerException cho de doc.
            var inner = e.InnerException != null ? e.InnerException : e;
            err = inner.Message;
            return false;
        }
    }
#endif

    // ═════════════════════════════════════════════════════════════════════════
    //  MANIFEST / GO BO
    // ═════════════════════════════════════════════════════════════════════════

    private static string ProjectRoot()
    {
        // Application.dataPath = <project>/Assets
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    private string WriteManifest(List<string> createdPaths)
    {
        string file = Path.Combine(ProjectRoot(),
            MANIFEST_PREFIX + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# SpriteAtlasBuilderTool — danh sach asset DA TAO");
            sb.AppendLine("# Thoi diem : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("# Project   : " + ProjectRoot());
            sb.AppendLine("# Kieu chia : " + _groupMode + " | max page " + _maxPageSize + " | padding " + _padding);
            sb.AppendLine("# Go bo     : mo lai cong cu → muc 5 → chon file nay → Xoa atlas");
            sb.AppendLine("# Cong cu KHONG sua PNG goc, nen xoa nhung duong dan duoi day la ve nguyen trang.");
            sb.AppendLine();
            foreach (var p in createdPaths) sb.AppendLine(p);
            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            Debug.Log("[AtlasBuilder] Da ghi manifest: " + file);
            return file;
        }
        catch (Exception e)
        {
            Debug.LogError("[AtlasBuilder] Khong ghi duoc manifest: " + e);
            return "";
        }
    }

    private void RefreshManifestList()
    {
        _manifests.Clear();
        try
        {
            var files = Directory.GetFiles(ProjectRoot(), MANIFEST_PREFIX + "*.txt", SearchOption.TopDirectoryOnly);
            _manifests.AddRange(files.OrderByDescending(f => f));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AtlasBuilder] Khong doc duoc danh sach manifest: " + e.Message);
        }
        _selectedManifest = 0;
    }

    private void RunRemoveCreated(string manifestFile)
    {
        if (string.IsNullOrEmpty(manifestFile) || !File.Exists(manifestFile))
        {
            EditorUtility.DisplayDialog("Xoa atlas", "Khong tim thay manifest.", "OK");
            return;
        }

        List<string> paths;
        try
        {
            paths = File.ReadAllLines(manifestFile)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#"))
                .ToList();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Xoa atlas", "Khong doc duoc manifest: " + e.Message, "OK");
            return;
        }

        // CHI xoa file atlas, khong bao gio xoa thu khac — chot an toan kep.
        var safe = paths.Where(p =>
        {
            string ext = Path.GetExtension(p).ToLowerInvariant();
            return ext == ".spriteatlas" || ext == ".spriteatlasv2";
        }).ToList();

        int refused = paths.Count - safe.Count;

        if (!EditorUtility.DisplayDialog(
                "Xoa atlas — XAC NHAN",
                "Se xoa " + safe.Count + " file atlas ghi trong:\n" + Path.GetFileName(manifestFile) +
                (refused > 0 ? "\n\n(" + refused + " dong bi tu choi vi khong phai file atlas.)" : "") +
                "\n\nPNG goc KHONG bi dong toi.",
                "Xoa", "Huy"))
            return;

        int deleted = 0, missing = 0;
        // CO Y KHONG dung StartAssetEditing/StopAssetEditing o day: Unity khuyen cao
        // DeleteAsset khong dang tin cay khi nam trong batch edit. So atlas chi vai chuc
        // file nen xoa truc tiep vua nhanh vua chac an hon.
        try
        {
            string root = ProjectRoot();
            for (int i = 0; i < safe.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Xoa atlas", safe[i], (float)(i + 1) / safe.Count);

                // Kiem tra tren o dia thay vi LoadMainAssetAtPath: khong phu thuoc trang thai AssetDatabase.
                if (!File.Exists(Path.Combine(root, safe[i]))) { missing++; continue; }

                if (AssetDatabase.DeleteAsset(safe[i])) deleted++;
                else Debug.LogWarning("[AtlasBuilder] Khong xoa duoc: " + safe[i]);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log("[AtlasBuilder] Da xoa " + deleted + " atlas (" + missing + " file da khong con). Manifest: " + manifestFile);
        EditorUtility.DisplayDialog("Xoa atlas",
            "Da xoa " + deleted + " atlas.\n" + missing + " file da khong con ton tai.", "OK");
        RefreshManifestList();
    }
}
#endif
