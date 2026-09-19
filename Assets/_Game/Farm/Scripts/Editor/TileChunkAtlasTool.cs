// ============================================================================
//  TILE CHUNK ATLAS TOOL  —  Tools ▸ Farm Game ▸ Tile: Gom Atlas + Bat Chunk
// ----------------------------------------------------------------------------
//  VI SAO: Profiler do RenderLoop 43ms / 1.652 draw call. Nguon: ~35.000 tile tren
//  15 TilemapRenderer deu o che do INDIVIDUAL (moi tile la mot sprite roi phai sap
//  xep va ve rieng). Chunk mode gom ca lop thanh vai mesh, nhung truoc day bat Chunk
//  thi map bi "luoi": sheet tile cat sat nhau khong co khe (gutter) nen bilinear
//  ri mau pixel hang xom vao canh tile. Cach sua GOC cua nganh: dua sheet vao mot
//  SpriteAtlas co PADDING >= 4 (Unity tu keo dai pixel bien vao vung dem) roi bat Chunk.
//
//  TOOL LAM GI (moi buoc co Undo, khong dung git):
//    1. Quet moi Tilemap trong scene dang mo, lay TAT CA sprite tile dang duoc ve.
//    2. Gom texture goc cua chung, bo texture da nam trong atlas V1 khac.
//    3. Tao/ghi de Assets/_Game/Atlases/Atlas_GroundTiles.spriteatlas (V1, padding 4,
//       khong xoay, khong tight-pack, Bilinear, khong mipmap, Include in Build).
//    4. Pack atlas.
//    5. Bat CHUNK cho TilemapRenderer co >= nguong tile (mac dinh 300), danh dau scene dirty.
//  Sep chi can Ctrl+S luu scene, roi nhin Scene view: het luoi la dat.
//  HOAN TAC: bam "Tra ve Individual" trong cung cua so, hoac Ctrl+Z.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

public class TileChunkAtlasTool : EditorWindow
{
    private const string MENU = "Tools/Farm Game/Tile: Gom Atlas + Bat Chunk";
    private const string ATLAS_DIR  = "Assets/_Game/Atlases";
    private const string ATLAS_PATH = ATLAS_DIR + "/Atlas_GroundTiles.spriteatlas";

    private int  _nguongTile   = 300;
    private int  _padding      = 4;
    private int  _maxPage      = 4096;
    private bool _daQuet;
    private readonly List<TilemapRenderer> _renderers = new List<TilemapRenderer>();
    private readonly Dictionary<TilemapRenderer,int> _soTile = new Dictionary<TilemapRenderer,int>();
    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private readonly List<string> _boQua = new List<string>();
    private Vector2 _scroll;

    [MenuItem(MENU, false, 41)]
    public static void Open() => GetWindow<TileChunkAtlasTool>("Tile Chunk Atlas");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Muc tieu: 1.652 draw call -> vai chuc. Buoc 1 quet, doc ket qua, roi buoc 2 ap dung.\n" +
            "Sau khi ap dung: nhin Scene view (KHONG can Play). Het luoi = dat. Con luoi = bam 'Tra ve Individual' va bao lai.",
            MessageType.Info);

        _nguongTile = EditorGUILayout.IntField(new GUIContent("Nguong tile de bat Chunk", "Lop co it tile hon so nay giu Individual (khong dang ke)."), _nguongTile);
        _padding    = EditorGUILayout.IntSlider(new GUIContent("Padding atlas", "4 la du de het ri mau canh. Tang len 8 neu van thay luoi mo."), _padding, 2, 16);
        _maxPage    = EditorGUILayout.IntPopup("Kich thuoc trang atlas", _maxPage, new[]{"2048","4096"}, new[]{2048,4096});

        GUILayout.Space(6);
        if (GUILayout.Button("1) QUET tilemap trong scene dang mo", GUILayout.Height(28))) Quet();

        if (_daQuet)
        {
            GUILayout.Space(4);
            EditorGUILayout.LabelField($"TilemapRenderer: {_renderers.Count}   |   texture sheet can gom: {_textures.Count}   |   bo qua (da o atlas khac): {_boQua.Count}", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            foreach (var r in _renderers)
            {
                int n = _soTile[r];
                string mode = r.mode == TilemapRenderer.Mode.Chunk ? "Chunk" : "Individual";
                string act  = n >= _nguongTile ? "-> CHUNK" : "(giu)";
                EditorGUILayout.LabelField($"  {r.gameObject.name,-28} tiles={n,6}  hien tai={mode,-10} {act}");
            }
            GUILayout.Space(4);
            foreach (var t in _textures) EditorGUILayout.LabelField("  + " + AssetDatabase.GetAssetPath(t));
            foreach (var s in _boQua)    EditorGUILayout.LabelField("  ~ bo qua: " + s);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            GUI.enabled = _textures.Count > 0 || _renderers.Count > 0;
            if (GUILayout.Button("2) AP DUNG: tao atlas + pack + bat Chunk", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Xac nhan",
                    $"Tao {ATLAS_PATH} voi {_textures.Count} texture, padding {_padding}, roi bat Chunk cho lop >= {_nguongTile} tile.\nCo Undo. Tiep tuc?", "Lam", "Huy"))
                    ApDung();
            }
            GUI.enabled = true;
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("Chunk lam lo khe giua cac o co (mesh Tight + o vua khit). Nut duoi: lop NHIN THAY ve Individual (dep nhu cu), lop AN (Mong, Water, Underwater) giu Chunk => van giu ~60% loi ich draw call.", MessageType.None);
            if (GUILayout.Button("3) LOP NHIN THAY -> Individual, lop an giu Chunk  (khuyen dung)", GUILayout.Height(28))) LopNhinThayVeIndividual();
            GUILayout.Space(4);
            if (GUILayout.Button("Tra ve Individual TAT CA (hoan tac che do, giu atlas)")) DatMode(TilemapRenderer.Mode.Individual);
        }
    }

    private void Quet()
    {
        _renderers.Clear(); _soTile.Clear(); _textures.Clear(); _boQua.Clear();
        var daCo = new HashSet<Texture2D>();
        var trongAtlasKhac = LayTextureDaOAtlasKhac();

        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var r = tm.GetComponent<TilemapRenderer>();
            if (r == null) continue;
            tm.CompressBounds();
            var b = tm.cellBounds; int n = 0;
            foreach (var pos in b.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                n++;
                var sp = tm.GetSprite(pos);
                if (sp == null || sp.texture == null) continue;
                var tex = sp.texture;
                if (daCo.Contains(tex)) continue;
                daCo.Add(tex);
                string p = AssetDatabase.GetAssetPath(tex);
                if (trongAtlasKhac.Contains(tex)) { _boQua.Add(p); continue; }
                _textures.Add(tex);
            }
            _renderers.Add(r); _soTile[r] = n;
        }
        _renderers.Sort((a, c) => _soTile[c].CompareTo(_soTile[a]));
        _daQuet = true;
        Debug.Log($"[TileChunkAtlas] quet xong: {_renderers.Count} renderer, {_textures.Count} texture, bo qua {_boQua.Count}.");
    }

    private static HashSet<Texture2D> LayTextureDaOAtlasKhac()
    {
        var set = new HashSet<Texture2D>();
        foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p == ATLAS_PATH) continue;                      // atlas cua chinh tool nay: se ghi de
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(p);
            if (atlas == null) continue;
            foreach (var o in atlas.GetPackables())
            {
                if (o is Texture2D t) set.Add(t);
                else if (o is Sprite s && s.texture != null) set.Add(s.texture);
                else if (o is DefaultAsset folder)
                {
                    string fp = AssetDatabase.GetAssetPath(folder);
                    foreach (var g2 in AssetDatabase.FindAssets("t:Texture2D", new[] { fp }))
                    {
                        var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g2));
                        if (t2 != null) set.Add(t2);
                    }
                }
            }
        }
        return set;
    }

    private void ApDung()
    {
        // ── 1. Atlas ──
        if (_textures.Count > 0)
        {
            if (!Directory.Exists(ATLAS_DIR)) Directory.CreateDirectory(ATLAS_DIR);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ATLAS_PATH);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, ATLAS_PATH);
            }
            else
            {
                var cu = atlas.GetPackables();
                if (cu != null && cu.Length > 0) atlas.Remove(cu);
            }

            var pack = atlas.GetPackingSettings();
            pack.padding            = _padding;
            pack.enableRotation     = false;   // tile khong duoc xoay
            pack.enableTightPacking = false;   // tight-pack lam sai UV cua tile
            pack.blockOffset        = 1;
            atlas.SetPackingSettings(pack);

            var tex = atlas.GetTextureSettings();
            tex.filterMode     = FilterMode.Bilinear;
            tex.generateMipMaps = false;
            tex.sRGB           = true;
            tex.readable       = false;
            atlas.SetTextureSettings(tex);

            var def = atlas.GetPlatformSettings("DefaultTexturePlatform");
            def.maxTextureSize = _maxPage; def.overridden = true;
            def.format = TextureImporterFormat.Automatic; def.textureCompression = TextureImporterCompression.Compressed;
            atlas.SetPlatformSettings(def);

            var and = atlas.GetPlatformSettings("Android");
            and.overridden = true; and.maxTextureSize = _maxPage;
            and.format = TextureImporterFormat.ASTC_5x5; and.compressionQuality = 100;
            atlas.SetPlatformSettings(and);

            var ios = atlas.GetPlatformSettings("iPhone");
            ios.overridden = true; ios.maxTextureSize = _maxPage;
            ios.format = TextureImporterFormat.ASTC_5x5; ios.compressionQuality = 100;
            atlas.SetPlatformSettings(ios);

            atlas.SetIncludeInBuild(true);
            var arr = new Object[_textures.Count];
            for (int i = 0; i < _textures.Count; i++) arr[i] = _textures[i];
            atlas.Add(arr);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();

            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"[TileChunkAtlas] da tao + pack {ATLAS_PATH} ({_textures.Count} texture, padding {_padding}).");
        }

        // ── 2. Chunk mode ──
        DatMode(TilemapRenderer.Mode.Chunk);
        Debug.Log("[TileChunkAtlas] XONG. Ctrl+S luu scene. Nhin Scene view: het luoi = dat; con luoi -> tang padding 8, pack lai.");
    }

    // Lop nam DUOI moi thu / o ria map: giu Chunk vi khe (neu co) khong nhin thay.
    private static readonly HashSet<string> LopAnGiuChunk = new HashSet<string>
    {
        "M\u00F3ng", "Mong", "Water_Tilemap", "Underwater_Tilemap", "Dat_Nen", "Dat_Nen (1)"
    };

    private void LopNhinThayVeIndividual()
    {
        int doi = 0;
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            bool giuChunk = LopAnGiuChunk.Contains(r.gameObject.name);
            var muon = giuChunk ? TilemapRenderer.Mode.Chunk : TilemapRenderer.Mode.Individual;
            if (r.mode == muon) continue;
            Undo.RecordObject(r, "Tilemap mode (lop nhin thay)");
            r.mode = muon; EditorUtility.SetDirty(r); doi++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[TileChunkAtlas] lop nhin thay -> Individual, lop an giu Chunk. Doi {doi} renderer. Ctrl+S de luu.");
    }

    private void DatMode(TilemapRenderer.Mode mode)
    {
        int doi = 0;
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            bool duLon = _soTile.TryGetValue(r, out int n) && n >= _nguongTile;
            var muon = mode == TilemapRenderer.Mode.Chunk ? (duLon ? TilemapRenderer.Mode.Chunk : r.mode) : TilemapRenderer.Mode.Individual;
            if (r.mode == muon) continue;
            Undo.RecordObject(r, "Tilemap mode");
            r.mode = muon;
            EditorUtility.SetDirty(r);
            doi++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[TileChunkAtlas] doi che do {doi} TilemapRenderer -> {mode}.");
    }
}
