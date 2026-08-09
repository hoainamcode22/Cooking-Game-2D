using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DEV-1 · V2 + V4 — ĐỒNG BỘ NHÓM 5 NHÀ DÂN (Home1…Home5).
///
/// VÌ SAO CẦN TOOL RIÊNG dù đã có `Tools/Farm/Suy Kích Thước Ô Công Trình`:
///   Tool kia SUY kích thước từ bounds prefab — nó sẽ đề nghị Home3 = 4×5 (đúng theo
///   sprite, vì mái nhà đó cao hơn) và như vậy Home3 mãi mãi lệch nhóm. Còn đây là
///   quyết định THIẾT KẾ của Edric: cùng một nhóm công trình phải cùng chiều sâu ô để
///   người chơi xếp được thành hàng đẹp (Township làm y vậy). Mái nhô ra ngoài footprint
///   là bình thường và KHÔNG phải lỗi.
///
/// TOOL NÀY LÀM 3 VIỆC, tất cả đều IDEMPOTENT (bấm bao nhiêu lần cũng ra một kết quả):
///   1. Ép `gridSize = 4×4` cho cả 5 nhà.
///   2. Kiểm tra `prefabToBuild` có giải được ra prefab thật không. Đây là chỗ đã từng
///      hỏng rất khó thấy: file .asset lưu cặp (fileID, guid); Home2/Home4 bị HOÁN guid
///      cho nhau nên guid trỏ sang prefab khác mà fileID lại của prefab đúng
///      → Unity giải ra NULL và Inspector chỉ hiện "None", trông y như chưa ai gán.
///   3. Ẩn khỏi shop những nhà KHÔNG giải được prefab bằng `unlockLevel = 999`
///      (ShopLevelLockUI phủ lớp khoá + tắt nút mua; ShopManager xếp chúng xuống cuối).
///      Chọn cách này vì nó KHÔNG cần sửa ShopManager/ShopItemUI — hai file đó không
///      thuộc quyền DEV-1, và thêm một cờ mới thì cũng vô dụng nếu không ai đọc nó.
///
/// Menu: Tools/Farm/Đồng Bộ Nhóm 5 Nhà Dân (4×4)
/// </summary>
public class HouseGroupNormalizeTool : EditorWindow
{
    /// <summary>Chiều sâu/rộng ô chốt cho cả nhóm nhà dân. Edric chốt ở §2 file TEAM.</summary>
    private static readonly Vector2Int HouseGridSize = new Vector2Int(4, 4);

    /// <summary>Cấp mở khoá dùng làm "ẩn khỏi shop". 999 = không bao giờ tới được.</summary>
    private const int HiddenUnlockLevel = 999;

    private static readonly string[] HouseAssetNames = { "Home1", "Home2", "Home3", "Home4", "Home5" };

    private class Row
    {
        public PlaceableItemData data;
        public string     path;
        public bool       prefabOk;
        public Vector2    pivotOffset;   // độ lệch pivot — chỉ còn dùng cho migration save
        public string     note = "";
    }

    private readonly List<Row> rows = new();
    private Vector2 scroll;

    [MenuItem("Tools/Farm/Đồng Bộ Nhóm 5 Nhà Dân (4×4)")]
    public static void Open()
    {
        var w = GetWindow<HouseGroupNormalizeTool>(true, "Đồng Bộ Nhóm 5 Nhà Dân");
        w.minSize = new Vector2(820f, 380f);
        w.Scan();
        w.Show();
    }

    // ── QUÉT ─────────────────────────────────────────────────────────────────

    private void Scan()
    {
        rows.Clear();

        foreach (string guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (data == null) continue;

            bool isHouse = false;
            foreach (string n in HouseAssetNames)
                if (data.name == n) { isHouse = true; break; }
            if (!isHouse) continue;

            var row = new Row
            {
                data     = data,
                path     = path,
                prefabOk = data.prefabToBuild != null
            };

            if (!row.prefabOk)
            {
                // Gợi ý sửa: đọc thẳng YAML để chỉ ra guid nào đang bị trỏ sai.
                row.note = "prefabToBuild KHÔNG giải được (guid trỏ sai prefab?) → sẽ ẩn khỏi shop";
            }
            else
            {
                row.pivotOffset = PlacementManager.PivotOffsetOf(data, 0);

                // Bằng chứng V8 sửa đúng chỗ:
                //   • Hệ CŨ (V7) snap TÂM ô ⇒ chân = (oy + M/2)·CELL. M CHẴN thì chân trùng
                //     đường kẻ; M LẺ thì chân rơi vào GIỮA ô, lệch nửa CELL so với nhóm.
                //     Đó chính là lý do Home3 (sâu 5 ô) nhô ra so với 4 nhà sâu 4 ô.
                //   • Hệ MỚI (V8) snap CHÂN ⇒ luôn là bội số CELL, lệch 0 với MỌI M.
                float lechCu = (data.gridSize.y % 2 == 0) ? 0f : PlacementManager.CELL * 0.5f;
                row.note = $"pivot ({row.pivotOffset.x:F0},{row.pivotOffset.y:F0}) · " +
                           $"chân lệch lưới: hệ cũ {lechCu:F0} unit (sâu {data.gridSize.y} ô) " +
                           $"→ hệ V8 = 0 unit";
            }

            rows.Add(row);
        }

        rows.Sort((a, b) => string.Compare(a.data.name, b.data.name, System.StringComparison.Ordinal));
    }

    // ── GIAO DIỆN ────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "V2 — Cả 5 nhà dân dùng CHUNG gridSize 4×4 để xếp thành hàng thẳng.\n" +
            "V4 — Nhà nào không giải được prefabToBuild thì ẩn khỏi shop (unlockLevel = 999),\n" +
            "     tránh để người chơi mua một thứ không đặt được.\n" +
            "Mái nhà nhô ra ngoài vùng ô là BÌNH THƯỜNG — đừng sửa gridSize theo bounds sprite.",
            MessageType.Info);

        if (GUILayout.Button("Quét lại", GUILayout.Height(22f))) Scan();

        EditorGUILayout.Space(4f);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (Row r in rows)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(r.data.name, EditorStyles.boldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField($"gridSize {r.data.gridSize.x}×{r.data.gridSize.y}", GUILayout.Width(110f));
            EditorGUILayout.LabelField($"unlockLevel {r.data.unlockLevel}", GUILayout.Width(120f));
            EditorGUILayout.LabelField(r.prefabOk ? "prefab OK" : "PREFAB NULL", GUILayout.Width(90f));
            if (GUILayout.Button("Chọn", GUILayout.Width(50f)))
                Selection.activeObject = r.data;
            EditorGUILayout.EndHorizontal();

            var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            style.normal.textColor = r.prefabOk ? Color.gray : new Color(0.85f, 0.3f, 0.25f);
            EditorGUILayout.LabelField(r.note, style);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);

        if (GUILayout.Button($"ÁP DỤNG — gridSize = {HouseGridSize.x}×{HouseGridSize.y} cho cả 5 nhà " +
                             "+ ẩn nhà thiếu prefab", GUILayout.Height(30f)))
        {
            Apply();
        }

        EditorGUILayout.Space(2f);

        if (GUILayout.Button("Mở lại shop cho nhà ĐÃ có prefab (unlockLevel 999 → 1)", GUILayout.Height(22f)))
        {
            Unhide();
        }
    }

    // ── ÁP DỤNG ──────────────────────────────────────────────────────────────

    private void Apply()
    {
        int changed = 0;

        foreach (Row r in rows)
        {
            PlaceableItemData data = r.data;

            // Undo.RecordObject PHẢI gọi TRƯỚC khi đổi giá trị, nếu không Unity ghi lại
            // trạng thái ĐÃ đổi và Ctrl+Z không trả về được gì.
            Undo.RecordObject(data, "Đồng bộ nhóm nhà dân");

            bool touched = false;

            if (data.gridSize != HouseGridSize)
            {
                data.gridSize = HouseGridSize;
                touched = true;
            }

            // V4: chỉ ẩn khi THẬT SỰ không giải được prefab. Không ẩn oan nhà đang chạy tốt.
            if (data.prefabToBuild == null && data.unlockLevel != HiddenUnlockLevel)
            {
                data.unlockLevel = HiddenUnlockLevel;
                touched = true;
            }

            if (touched)
            {
                EditorUtility.SetDirty(data);
                changed++;
                Debug.Log($"[NhàDân] '{data.name}' → gridSize {data.gridSize.x}×{data.gridSize.y}, " +
                          $"unlockLevel {data.unlockLevel}  ({r.path})", data);
            }
        }

        if (changed > 0) AssetDatabase.SaveAssets();

        Debug.Log(changed > 0
            ? $"[NhàDân] Đã cập nhật {changed}/{rows.Count} asset và lưu xuống đĩa."
            : $"[NhàDân] Cả {rows.Count} asset đã đúng — không cần sửa gì.");

        Scan();
    }

    private void Unhide()
    {
        int changed = 0;

        foreach (Row r in rows)
        {
            PlaceableItemData data = r.data;
            if (data.prefabToBuild == null) continue;              // vẫn hỏng → cứ để ẩn
            if (data.unlockLevel != HiddenUnlockLevel) continue;   // không phải do ta ẩn

            Undo.RecordObject(data, "Mở lại nhà dân trong shop");
            data.unlockLevel = 1;
            EditorUtility.SetDirty(data);
            changed++;
            Debug.Log($"[NhàDân] '{data.name}' đã có prefab → mở bán lại (unlockLevel = 1).", data);
        }

        if (changed > 0) AssetDatabase.SaveAssets();
        else Debug.Log("[NhàDân] Không có nhà nào đang bị ẩn mà đã sẵn sàng bán.");

        Scan();
    }
}
