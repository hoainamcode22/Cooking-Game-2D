// ============================================================================
//  WorldClearConfig — cau hinh CHAT CAY / CAT BUI / DAP DA bang dung cu shop (2026-09-25)
//  Asset: Assets/_Game/Resources/WorldClearConfig.asset (Tools > Farm Game > Dung Cu > 1).
//  Khong co asset -> dung so mac dinh ben duoi (van chay duoc).
//  Sep chot: tru 1 dung cu / lan · roi nguyen lieu + EXP · chi trong dat da mo · lau kieu Hay Day (10-30s, co nut kim cuong).
// ============================================================================
using UnityEngine;

public enum WorldClearKind { Riu = 0, Keo = 1, Bua = 2 }

[CreateAssetMenu(fileName = "WorldClearConfig", menuName = "Farm/World Clear Config")]
public class WorldClearConfig : ScriptableObject
{
    [System.Serializable]
    public class Loai
    {
        public WorldClearKind kind;
        [Tooltip("itemID dung cu trong kho (khop ToolData.itemID).")]
        public string toolItemId = "tool_axe";
        [Tooltip("Icon dung cu (tool 1 tu lay tu ToolData). Trong -> tu tim luc chay.")]
        public Sprite icon;
        [Tooltip("Thoi gian lam (giay).")]
        [Min(1)] public int giayLam = 25;
        [Tooltip("Nguyen lieu roi ra (itemID trong kho, vd go / da). Trong = khong roi.")]
        public string thuongItemId = "go";
        public Vector2Int thuongSoLuong = new Vector2Int(2, 3);
        [Min(0)] public int thuongExp = 10;
        [Tooltip("Icon nguyen lieu roi xuong dat (tu dien tu InventoryItemData theo thuongItemId).")]
        public Sprite thuongIcon;
        [Tooltip("Ten object trong scene BAT DAU bang 1 trong cac chuoi nay (khong phan biet hoa thuong) -> dung dung cu nay.")]
        public string[] tienToTen = new string[0];
    }

    public Loai[] loai = MacDinh();

    [Header("Chung")]
    [Tooltip("1 kim cuong cho moi X giay con lai khi bam Xong ngay.")]
    [Min(1)] public int giayMoiKimCuong = 10;
    [Tooltip("Tat = chi chat trong KHU DAT da mo (rung vien map khong bi chat trui). Bat = moi noi.")]
    public bool choPhepNgoaiKhuDat = false;
    [Tooltip("Ten co chua 1 trong cac chuoi nay -> bo qua (decor nguoi choi tu mua).")]
    public string[] boQuaNeuTenChua = { "Decor_", "(Clone)" };

    [Header("Rai da nho + bui ram tren dat da mo (WildDebrisSpawner, 2026-09-25)")]
    [Tooltip("Bat = moi lan vao scene rai them da nho / bui ram o cho trong tren dat nguoi choi da so huu.")]
    public bool raiVatHoang = true;
    [Tooltip("Toi da so vat rai (giu thap cho mobile).")]
    [Min(0)] public int toiDaDa = 14;
    [Min(0)] public int toiDaBui = 14;
    [Tooltip("Ti le o duoc xet (0-1). Cao = day hon (van bi chan boi toiDa).")]
    [Range(0f, 1f)] public float matDoVatHoang = 0.3f;
    [Tooltip("Can bao nhieu o trong + da mo trong vung 5x5 quanh o (cang cao cang chi rai o cho trong rong).")]
    [Range(0, 25)] public int oTrongToiThieu = 18;
    [Tooltip("% vat la da (con lai la bui).")]
    [Range(0, 100)] public int phanTramDa = 50;
    [Tooltip("Khoang cach toi thieu giua 2 vat rai (world).")]
    [Min(0f)] public float cachNhau = 260f;
    [Tooltip("So o le quanh khu da mua (dat khoi dau ve tay khong thuoc khu nao).")]
    [Min(0)] public int leDatKhoiDau = 4;
    [Tooltip("He so scale so voi prefab mau trong scene.")]
    public Vector2 tiLeDa = new Vector2(0.7f, 0.95f);
    public Vector2 tiLeBui = new Vector2(0.75f, 0.95f);
    [Tooltip("De trong = tu lay sprite tu Prefab_RocksSmall / Prefab_RockMedium trong scene.")]
    public Sprite[] spDaNho = new Sprite[0];
    [Tooltip("De trong = tu lay sprite tu Prefab_Bush / Prefab_Grass_Clump trong scene.")]
    public Sprite[] spBui = new Sprite[0];

    public static Loai[] MacDinh()
    {
        return new[]
        {
            new Loai { kind = WorldClearKind.Riu, toolItemId = "tool_axe", giayLam = 25, thuongItemId = "go",
                thuongSoLuong = new Vector2Int(2, 3), thuongExp = 10,
                tienToTen = new[] { "prefab_pinetree", "prefab_fruittree", "caythong", "cayrung", "cayganduognra", "caydaithu", "caychetkho", "log_horizontal" } },
            new Loai { kind = WorldClearKind.Keo, toolItemId = "tool_scissors", giayLam = 12,
                thuongItemId = "herbs", thuongSoLuong = new Vector2Int(1, 2), thuongExp = 5,
                tienToTen = new[] { "prefab_bush", "prefab_grass_clump", "wildbush" } },
            new Loai { kind = WorldClearKind.Bua, toolItemId = "tool_hammer", giayLam = 18, thuongItemId = "da",
                thuongSoLuong = new Vector2Int(2, 3), thuongExp = 8,
                tienToTen = new[] { "prefab_rocksbig", "prefab_rockmedium", "prefab_rockssmall", "wildrock" } },
        };
    }

    private static WorldClearConfig _cache;

    public static WorldClearConfig Load()
    {
        if (_cache != null) return _cache;
        _cache = Resources.Load<WorldClearConfig>("WorldClearConfig");
        if (_cache == null) _cache = CreateInstance<WorldClearConfig>();
        return _cache;
    }

    /// <summary>Ma + so luong thuong that (bui cay: config cu de trong -> Rau thom 1-2).</summary>
    public void Thuong(WorldClearKind k, out string id, out Vector2Int soLuong)
    {
        var l = Tim(k);
        id = l.thuongItemId; soLuong = l.thuongSoLuong;
        if (k == WorldClearKind.Keo && string.IsNullOrEmpty(id)) { id = "herbs"; soLuong = new Vector2Int(1, 2); }
    }

    /// <summary>Icon nguyen lieu roi: config -> BuildMaterials -> moi InventoryItemData dang nap. Editor: tu ghi vao asset.</summary>
    public Sprite IconThuong(WorldClearKind k)
    {
        var l = Tim(k);
        string id; Vector2Int sl; Thuong(k, out id, out sl);
        if (string.IsNullOrEmpty(id)) return null;
        if (l.thuongIcon != null && l.thuongItemId == id) return l.thuongIcon;
        Sprite sp = BuildMaterials.IconOf(id);
        if (sp == null)
            foreach (var d in Resources.FindObjectsOfTypeAll<InventoryItemData>())
                if (d != null && d.icon != null && string.Equals(d.itemId, id, System.StringComparison.OrdinalIgnoreCase)) { sp = d.icon; break; }
#if UNITY_EDITOR
        if (sp == null)
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:InventoryItemData"))
            {
                var d = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
                if (d != null && d.icon != null && string.Equals(d.itemId, id, System.StringComparison.OrdinalIgnoreCase)) { sp = d.icon; break; }
            }
        if (sp != null && UnityEditor.EditorUtility.IsPersistent(this))
        {
            l.thuongItemId = id; l.thuongSoLuong = sl; l.thuongIcon = sp;
            UnityEditor.EditorUtility.SetDirty(this);             // ghi vao asset -> ban build co icon
        }
#endif
        if (sp != null) { l.thuongItemId = id; l.thuongIcon = sp; }
        return sp;
    }

    public Loai Tim(WorldClearKind k)
    {
        if (loai != null)
            for (int i = 0; i < loai.Length; i++)
                if (loai[i] != null && loai[i].kind == k) return loai[i];
        var md = MacDinh();
        return md[(int)k];
    }

    /// <summary>Icon dung cu: config -> ToolData dang nap trong bo nho (shop tham chieu) theo itemID.</summary>
    public Sprite IconCua(WorldClearKind k)
    {
        var l = Tim(k);
        if (l.icon != null) return l.icon;
        foreach (var td in Resources.FindObjectsOfTypeAll<ToolData>())
            if (td != null && td.itemID == l.toolItemId && td.itemIcon != null) { l.icon = td.itemIcon; break; }
        return l.icon;
    }
}
