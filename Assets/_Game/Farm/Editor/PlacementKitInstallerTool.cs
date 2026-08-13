using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GẮN BỘ KIT ĐẶT CÔNG TRÌNH VÀO TOÀN BỘ PREFAB.
///
/// ══════════════════════════════════════════════════════════════════════════
///  BA LỖI TOOL NÀY SỬA
/// ══════════════════════════════════════════════════════════════════════════
/// 1. **Phần lớn công trình không nhấc lên được.** Điều kiện duy nhất để nhấc là có
///    `EditableBuilding`. Kiểm tra thật: chỉ `House_01`, `House_02`, `Chauhoa_1`,
///    `Chauhoa_2`, `Pen_02`, `May_01..03` có. 16 prefab trang trí, `House_03..05`,
///    `Pen_01/03/04`, `Chauhoa_3/4`, `Plot_01` thì KHÔNG.
///
/// 2. **16 prefab trang trí không có collider nào.** Không collider thì `OnMouseDown`
///    không bao giờ chạy → kể cả thêm `EditableBuilding` vẫn không bấm được.
///    `[RequireComponent(typeof(Collider2D))]` có tự thêm BoxCollider2D nhưng size
///    mặc định bằng 0 — vẫn không bấm trúng.
///
/// 3. **Thảm nền gãy tham chiếu ở mọi công trình.** `EditableBuilding.footprintVisual`
///    trỏ tới một fileID không tồn tại trong `House_01.prefab`, nên
///    `SetFootprintActive()` là lệnh rỗng ở khắp nơi.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO COLLIDER ÔM VÙNG Ô, KHÔNG ÔM HÌNH VẼ
/// ══════════════════════════════════════════════════════════════════════════
/// Ôm sát hình vẽ thì cột đèn có collider ~40×180 — ngón tay trên điện thoại rộng
/// khoảng 90px, bấm 5 lần trượt 3. Ôm vùng ô (`gridSize` khai trong asset dữ liệu)
/// thì vùng bấm TRÙNG với vùng chiếm chỗ và trùng với tấm thảm hiện bên dưới:
/// thấy thảm ở đâu là bấm được ở đó, không phải đoán.
/// </summary>
public static class PlacementKitInstallerTool
{
    private const string Menu = "Tools/Farm/Bộ Kit Đặt Công Trình/";

    /// <summary>Prefab KHÔNG phải công trình world — bỏ qua.</summary>
    private static readonly string[] BoQua =
    {
        "Placement_Ghost", "KhungEwar", "KhungHatGiong", "Main Camera",
    };

    /// <summary>Thư mục chứa prefab công trình.</summary>
    private static readonly string[] ThuMuc =
    {
        "Assets/_Game/Farm/CÔNG TRÌNH",
        "Assets/_Game/Farm/Frefab_home",
    };

    // ═════════════════════════════════════════════════════════════════════════
    //  1 · KIỂM TRA (chỉ đọc)
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "1 · Kiểm tra — sẽ đổi những gì", false, 1)]
    public static void KiemTra()
    {
        List<GameObject> ds = TimPrefabCongTrinh();
        Dictionary<GameObject, PlaceableItemData> theoPrefab = TraDuLieuTheoPrefab();

        var sb = new StringBuilder();
        sb.AppendLine("═══ KIỂM TRA BỘ KIT ĐẶT CÔNG TRÌNH ═══");
        sb.AppendLine($"  Tìm thấy {ds.Count} prefab công trình\n");
        sb.AppendLine($"  {"prefab",-26}{"ô lưới",-9}{"nhấc?",-8}{"collider",-11}kit");
        sb.AppendLine("  " + new string('─', 68));

        int thieuEdit = 0, thieuCol = 0, thieuKit = 0, khongCoData = 0;

        foreach (GameObject p in ds)
        {
            bool coEdit = p.GetComponent<EditableBuilding>() != null;
            bool coKit  = p.GetComponent<BuildingFootprintKit>() != null;
            bool coCol  = CoColliderDungDuoc(p);

            theoPrefab.TryGetValue(p, out PlaceableItemData data);
            Vector2Int o = data != null ? OCuaAsset(data) : Vector2Int.zero;
            string chuO = data == null ? "—" : $"{o.x}×{o.y}";
            if (data == null) khongCoData++;

            if (!coEdit) thieuEdit++;
            if (!coCol)  thieuCol++;
            if (!coKit)  thieuKit++;

            sb.AppendLine($"  {p.name,-26}{chuO,-9}{(coEdit ? "có" : "THIẾU"),-8}" +
                          $"{(coCol ? "có" : "THIẾU"),-11}{(coKit ? "có" : "THIẾU")}");
        }

        sb.AppendLine("  " + new string('─', 68));
        sb.AppendLine($"  Thiếu EditableBuilding : {thieuEdit}");
        sb.AppendLine($"  Thiếu collider dùng được: {thieuCol}");
        sb.AppendLine($"  Thiếu kit nền           : {thieuKit}");
        if (khongCoData > 0)
            sb.AppendLine($"  ⚠ {khongCoData} prefab không có asset dữ liệu trỏ tới " +
                          "→ kích thước ô sẽ phải tự đo từ hình vẽ.");

        sb.AppendLine("\n  Chạy mục 2 để gắn. Ctrl+Z hoàn tác được.");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  2 · GẮN
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "2 · Gắn kit + cho phép nhấc vào TẤT CẢ", false, 2)]
    public static void GanTatCa()
    {
        List<GameObject> ds = TimPrefabCongTrinh();
        if (ds.Count == 0)
        {
            EditorUtility.DisplayDialog("Bộ kit", "Không tìm thấy prefab công trình nào.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Gắn bộ kit",
                $"Sẽ sửa {ds.Count} prefab công trình:\n\n" +
                "  • Thêm EditableBuilding (để nhấc được trong Edit Mode)\n" +
                "  • Thêm/sửa BoxCollider2D ôm đúng vùng ô lưới\n" +
                "  • Thêm BuildingFootprintKit (thảm + ngoặc góc + chip kéo)\n" +
                "  • Gỡ tham chiếu footprintVisual gãy\n\n" +
                "Ctrl+Z hoàn tác được.",
                "Gắn hết", "Huỷ"))
            return;

        Dictionary<GameObject, PlaceableItemData> theoPrefab = TraDuLieuTheoPrefab();

        int daSua = 0, themEdit = 0, themCol = 0, themKit = 0, goGay = 0;
        var sb = new StringBuilder();

        try
        {
            for (int i = 0; i < ds.Count; i++)
            {
                GameObject p = ds[i];
                EditorUtility.DisplayProgressBar("Gắn bộ kit", p.name, (float)i / ds.Count);

                string duongDan = AssetDatabase.GetAssetPath(p);
                GameObject goc = PrefabUtility.LoadPrefabContents(duongDan);
                bool doi = false;

                theoPrefab.TryGetValue(p, out PlaceableItemData data);
                Vector2Int o = data != null ? OCuaAsset(data) : DoOTuHinhVe(goc);

                // ── a · collider ôm vùng ô ───────────────────────────────────
                if (ThemHoacSuaCollider(goc, o)) { themCol++; doi = true; }

                // ── b · cho phép nhấc ────────────────────────────────────────
                var eb = goc.GetComponent<EditableBuilding>();
                if (eb == null) { eb = goc.AddComponent<EditableBuilding>(); themEdit++; doi = true; }

                // Gỡ tham chiếu gãy: `BuildingFootprintKit` lo phần thảm, để lại ô này
                // trỏ vào một fileID không tồn tại chỉ tổ gây hiểu nhầm khi mở Inspector.
                if (eb.footprintVisual != null || TrucTiepCoThamGay(eb))
                {
                    eb.footprintVisual = null;
                    goGay++; doi = true;
                }

                // ── c · kit nền ──────────────────────────────────────────────
                var kit = goc.GetComponent<BuildingFootprintKit>();
                if (kit == null) { kit = goc.AddComponent<BuildingFootprintKit>(); themKit++; doi = true; }
                if (kit.SoO != o) { kit.SoO = o; doi = true; }

                if (doi)
                {
                    PrefabUtility.SaveAsPrefabAsset(goc, duongDan);
                    daSua++;
                    sb.AppendLine($"  {p.name,-26} ô {o.x}×{o.y}");
                }

                PrefabUtility.UnloadPrefabContents(goc);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BộKit] ✅ Sửa {daSua}/{ds.Count} prefab.\n" +
                  $"   + EditableBuilding : {themEdit}\n" +
                  $"   + collider vùng ô  : {themCol}\n" +
                  $"   + kit nền          : {themKit}\n" +
                  $"   gỡ thảm gãy        : {goGay}\n\n" + sb);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  3 · SỬA DỮ LIỆU LỆCH
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "3 · Sửa gridSize và prefab bị lệch trong asset", false, 3)]
    public static void SuaDuLieuLech()
    {
        var sb = new StringBuilder();
        int sua = 0;

        // ── a · gridSize 1×1 nhưng công trình to hơn ─────────────────────────
        foreach (PlaceableItemData d in TatCaAssetDuLieu())
        {
            if (d == null || d.prefabToBuild == null) continue;
            if (d.gridSize.x > 1 || d.gridSize.y > 1) continue;

            Vector2Int doDuoc = DoOTuHinhVe(d.prefabToBuild);
            if (doDuoc.x <= 1 && doDuoc.y <= 1) continue;

            Undo.RecordObject(d, "Sửa gridSize");
            sb.AppendLine($"  {d.name,-20} gridSize 1×1 → {doDuoc.x}×{doDuoc.y}");
            d.gridSize = doDuoc;
            EditorUtility.SetDirty(d);
            sua++;
        }

        // ── b · tên asset và tên prefab lệch nhau ────────────────────────────
        // "Chậu Hoa3" trỏ vào `Chauhoa_4.prefab`, "Chậu Hoa4" trỏ vào `Chauhoa_3.prefab`.
        // Không phải lỗi chạy (cả hai đều là chậu hoa hợp lệ) nhưng người sửa shop sau này
        // đổi giá cho "Chậu Hoa3" lại thấy chậu khác đổi giá — rất khó lần ra.
        foreach (PlaceableItemData d in TatCaAssetDuLieu())
        {
            if (d == null || d.prefabToBuild == null) continue;

            string so = SoCuoi(d.name);
            string soPrefab = SoCuoi(d.prefabToBuild.name);
            if (so.Length == 0 || soPrefab.Length == 0 || so == soPrefab) continue;
            if (!d.name.ToLowerInvariant().Contains("hoa")) continue;

            sb.AppendLine($"  ⚠ {d.name,-20} → prefab '{d.prefabToBuild.name}' (số cuối lệch: {so} ≠ {soPrefab})");
        }

        AssetDatabase.SaveAssets();
        Debug.Log(sua > 0 || sb.Length > 0
            ? $"[BộKit] Sửa {sua} asset.\n{sb}"
            : "[BộKit] Không có asset nào cần sửa.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  4 · BÁO CÁO SAU KHI GẮN
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem(Menu + "4 · Báo cáo — công trình trong scene có nhấc được không", false, 4)]
    public static void BaoCaoScene()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ CÔNG TRÌNH TRONG SCENE ═══\n");

        var thay = new List<Transform>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || EditorUtility.IsPersistent(t.gameObject)) continue;
            if (t.GetComponent<SpriteRenderer>() == null) continue;
            if (t.GetComponent<EditableBuilding>() != null ||
                t.GetComponent<ObjectDragHandler>() != null ||
                LaGocPrefabCongTrinh(t.gameObject))
                thay.Add(t);
        }

        int nhacDuoc = 0, khongNhac = 0;
        sb.AppendLine($"  {"object",-28}{"nhấc?",-10}{"collider",-11}kit");
        sb.AppendLine("  " + new string('─', 64));

        foreach (Transform t in thay)
        {
            GameObject g = t.gameObject;
            bool eb  = g.GetComponent<EditableBuilding>() != null;
            bool odh = g.GetComponent<ObjectDragHandler>() != null;
            bool col = CoColliderDungDuoc(g);
            bool kit = g.GetComponent<BuildingFootprintKit>() != null;

            bool ok = (eb || odh) && col;
            if (ok) nhacDuoc++; else khongNhac++;

            string cach = eb ? "giữ 0,3s" : odh ? "kéo thẳng" : "KHÔNG";
            sb.AppendLine($"  {g.name,-28}{cach,-10}{(col ? "có" : "THIẾU"),-11}{(kit ? "có" : "—")}");
        }

        sb.AppendLine("  " + new string('─', 64));
        sb.AppendLine($"  Nhấc được: {nhacDuoc}   ·   Không nhấc được: {khongNhac}");
        if (khongNhac > 0)
            sb.AppendLine("\n  → Các object trong scene được đặt tay từ trước không tự nhận\n" +
                          "    thay đổi của prefab nếu chúng đã bị 'Unpack'. Kiểm mục nào\n" +
                          "    ghi THIẾU rồi gắn tay, hoặc xoá đi đặt lại từ Shop.");

        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ═════════════════════════════════════════════════════════════════════════

    private static List<GameObject> TimPrefabCongTrinh()
    {
        var ket = new List<GameObject>();
        var daCo = new HashSet<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", ThuMuc))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (!daCo.Add(p)) continue;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) continue;

            if (System.Array.IndexOf(BoQua, go.name) >= 0) continue;

            // Prefab UI (RectTransform) không phải công trình world.
            if (go.GetComponent<RectTransform>() != null) continue;

            // Phải có ít nhất một hình vẽ, không thì chẳng có gì để đặt xuống map.
            if (go.GetComponentInChildren<SpriteRenderer>(true) == null) continue;

            ket.Add(go);
        }

        ket.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return ket;
    }

    private static List<PlaceableItemData> TatCaAssetDuLieu()
    {
        var ket = new List<PlaceableItemData>();
        var daXet = new HashSet<string>();

        // Quét cả 3 tên lớp: KHÔNG asset nào dùng `PlaceableItemData` trần, tất cả đều
        // là `BuildingData` hoặc `DecorData`. Lọc "t:PlaceableItemData" trả về 0 kết quả.
        foreach (string lop in new[] { "PlaceableItemData", "BuildingData", "DecorData" })
        foreach (string guid in AssetDatabase.FindAssets("t:" + lop))
        {
            if (!daXet.Add(guid)) continue;
            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null) ket.Add(d);
        }

        return ket;
    }

    private static Dictionary<GameObject, PlaceableItemData> TraDuLieuTheoPrefab()
    {
        var map = new Dictionary<GameObject, PlaceableItemData>();
        foreach (PlaceableItemData d in TatCaAssetDuLieu())
            if (d.prefabToBuild != null && !map.ContainsKey(d.prefabToBuild))
                map[d.prefabToBuild] = d;
        return map;
    }

    private static Vector2Int OCuaAsset(PlaceableItemData d)
    {
        Vector2Int o = d.gridSize;
        if (o.x > 1 || o.y > 1) return o;
        return DoOTuHinhVe(d.prefabToBuild);
    }

    /// <summary>Đo số ô từ hình vẽ, làm tròn LÊN.</summary>
    private static Vector2Int DoOTuHinhVe(GameObject prefab)
    {
        if (prefab == null) return Vector2Int.one;

        Bounds? gop = null;
        foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            if (sr.transform.name == "Kit_Nen" || sr.transform.IsChildOf(TimKit(prefab))) continue;

            Bounds b = sr.sprite.bounds;
            Vector3 s = sr.transform.lossyScale;
            var world = new Bounds(sr.transform.position,
                new Vector3(b.size.x * Mathf.Abs(s.x), b.size.y * Mathf.Abs(s.y), 1f));

            gop = gop.HasValue ? Gop(gop.Value, world) : world;
        }

        if (!gop.HasValue) return Vector2Int.one;

        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(gop.Value.size.x / PlacementManager.CELL)),
            Mathf.Max(1, Mathf.CeilToInt(gop.Value.size.y / PlacementManager.CELL)));
    }

    private static Transform TimKit(GameObject g)
    {
        Transform t = g.transform.Find("Kit_Nen");
        return t != null ? t : g.transform;
    }

    private static Bounds Gop(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    /// <summary>Collider không phải trigger, có diện tích thật.</summary>
    private static bool CoColliderDungDuoc(GameObject g)
    {
        foreach (var c in g.GetComponents<Collider2D>())
        {
            if (c == null || c.isTrigger) continue;
            if (c is BoxCollider2D b && (b.size.x < 1f || b.size.y < 1f)) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Thêm hoặc chỉnh BoxCollider2D ôm đúng vùng ô. Vùng ô mọc LÊN từ chân công trình
    /// (quy ước "V8" của PlacementManager), nên tâm collider nằm cao hơn gốc h/2.
    /// </summary>
    private static bool ThemHoacSuaCollider(GameObject goc, Vector2Int o)
    {
        // Kích thước tính theo hệ toạ độ CỦA ROOT. Prefab dùng root scale 100 nên
        // 1 đơn vị cục bộ = 100 world unit = đúng 1 ô lưới.
        float sx = Mathf.Max(0.0001f, Mathf.Abs(goc.transform.localScale.x));
        float donVi = PlacementManager.CELL / sx;

        float w = o.x * donVi;
        float h = o.y * donVi;

        BoxCollider2D dich = null;
        foreach (var c in goc.GetComponents<BoxCollider2D>())
        {
            if (c.isTrigger) continue;
            dich = c; break;
        }

        bool moi = dich == null;
        if (moi) dich = goc.AddComponent<BoxCollider2D>();

        var sizeMoi = new Vector2(w, h);
        var offMoi  = new Vector2(0f, h * 0.5f);

        // Collider đã ôm đúng rồi thì đừng động vào — ghi lại prefab không cần thiết
        // làm bẩn lịch sử git và có thể phá offset ai đó chỉnh tay có chủ đích.
        if (!moi &&
            (dich.size - sizeMoi).sqrMagnitude < 0.0001f &&
            (dich.offset - offMoi).sqrMagnitude < 0.0001f)
            return false;

        dich.size = sizeMoi;
        dich.offset = offMoi;
        dich.isTrigger = false;
        return true;
    }

    /// <summary>Ô `footprintVisual` đang trỏ vào một object không tồn tại?</summary>
    private static bool TrucTiepCoThamGay(EditableBuilding eb)
    {
        var so = new SerializedObject(eb);
        SerializedProperty p = so.FindProperty("footprintVisual");
        // Unity biểu diễn tham chiếu gãy bằng instanceID khác 0 nhưng object == null.
        return p != null && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0;
    }

    private static bool LaGocPrefabCongTrinh(GameObject g)
    {
        GameObject nguon = PrefabUtility.GetCorrespondingObjectFromSource(g);
        if (nguon == null) return false;
        string p = AssetDatabase.GetAssetPath(nguon);
        foreach (string tm in ThuMuc)
            if (p.StartsWith(tm)) return true;
        return false;
    }

    private static string SoCuoi(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i = s.Length - 1;
        while (i >= 0 && char.IsDigit(s[i])) i--;
        return s.Substring(i + 1);
    }
}
