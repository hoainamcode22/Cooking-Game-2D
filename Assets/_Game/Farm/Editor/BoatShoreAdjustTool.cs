using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/Dịch bến sát bờ  (BOAT-002 §3.7)
///
/// DỊCH điểm đậu (Berth) của cả 3 bến vào GẦN BỜ hơn — V2 muốn tàu đậu sát bờ để
/// tấm gỗ (gangplank) bắc tới đất liền cho khách xuống.
///
/// Vì sao cần tool: Berth là con của Dock_XX, kéo tay từng cái trong Scene view vừa
/// lâu vừa dễ lệch nhau; tool dịch cả 3 bến CÙNG một offset nên hàng bến vẫn thẳng
/// đều như bố cục Sếp đã canh.
///
/// ── DÙNG ĐƯỢC 2 KIỂU ─────────────────────────────────────────────────────
/// 1. Cửa sổ tool (menu ở trên) — Sếp chỉnh tay, xem trước, hoàn tác.
/// 2. GỌI CODE, không mở cửa sổ — cho tool "1 nút" của Dev B
///    (<c>TouristBoatOneClickSetup.cs</c>):
/// <code>
///     int soBen = BoatShoreAdjustTool.ApplyShoreOffset(new Vector2(0f, 200f));
///     Vector2 huong = BoatShoreAdjustTool.GuessShoreDirection();
/// </code>
///
/// ── HƯỚNG VÀO BỜ ─────────────────────────────────────────────────────────
/// Toạ độ thật trong SCN_Farm.unity: 3 Berth nằm ở y ≈ -4285…-4839, cổng nhà hàng
/// cooking ở y ≈ -2367 — tức ĐẤT LIỀN Ở PHÍA TRÊN, nên **+Y là hướng vào bờ** và
/// offset mặc định là <c>(0, +200)</c>.
/// <see cref="GuessShoreDirection"/> suy hướng theo thứ tự: (1) từ tâm 3 Berth hướng
/// tới mốc đất liền (CookingGate / object tên chứa "cooking") — chính xác nhất;
/// (2) fallback vector BlindPoint → Berth (biển → bờ) — với layout hiện tại BlindPoint
/// nằm xa phía TÂY-NAM (-9818,-7819) nên vector này nghiêng nhiều về +X, chỉ nên coi
/// là ước lượng thô; (3) không suy được thì trả <c>Vector2.up</c>.
///
/// Mọi thay đổi qua <c>Undo.RecordObject</c> → Ctrl+Z hoàn tác được; cửa sổ còn có
/// nút "Hoàn tác lần dịch vừa rồi" (dịch ngược đúng offset đã áp).
/// Tool KHÔNG đụng dữ liệu runtime, KHÔNG sửa config — chỉ transform trong scene.
/// </summary>
public class BoatShoreAdjustTool : EditorWindow
{
    private const string MenuPath  = "Tools/Farm Game/Tourist Boat/Dịch bến sát bờ";
    private const string RootName  = "BoatSystem";
    private const string UndoLabel = "Tourist Boat — Dịch bến sát bờ";

    /// <summary>
    /// Offset mặc định (unit world) kéo tàu vào gần bờ — chốt theo toạ độ thật của
    /// scene: bến ở dưới (y ≈ -4285…-4839), đất liền ở trên ⇒ +Y là vào bờ.
    /// Đây cũng là giá trị tool "1 nút" của Dev B dùng.
    /// </summary>
    public static readonly Vector2 DefaultShoreOffset = new Vector2(0f, 200f);

    /// <summary>Số waypoint CUỐI mặc định được dịch theo Berth (1 là đủ với path 3 WP do tool sinh).</summary>
    public const int DefaultTailWaypointCount = 1;

    /// <summary>Số bến của hệ — bằng BoatDockManager.DockCount, để riêng cho code Editor khỏi phụ thuộc thứ tự compile.</summary>
    private const int DockTotal = 3;

    // ═════════════════════════════════════════════════════════════════════
    //  API PUBLIC — Dev B gọi từ TouristBoatOneClickSetup, không mở cửa sổ
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dịch Berth (và 1 waypoint cuối của mỗi bến) của CẢ 3 BẾN theo offset world.
    /// Trả về SỐ BẾN đã dịch được (0 = không tìm thấy BoatSystem / không bến nào có Berth).
    ///
    /// Ghi log chi tiết ra Console: bến nào dịch từ đâu tới đâu, bến nào bị bỏ qua.
    /// recordUndo = true (mặc định): mọi thay đổi đi qua Undo.RecordObject và gom
    /// thành 1 nhóm Ctrl+Z. Đặt false khi caller tự quản lý nhóm Undo của mình
    /// (vd tool "1 nút" gom cả chục bước vào một Undo group duy nhất).
    /// </summary>
    /// <param name="offsetWorld">Độ dịch (x, y) tính bằng unit world. +Y = vào bờ theo layout scene hiện tại.</param>
    /// <param name="recordUndo">Ghi Undo cho từng transform (mặc định true).</param>
    public static int ApplyShoreOffset(Vector2 offsetWorld, bool recordUndo = true)
    {
        return ApplyShoreOffset(offsetWorld, recordUndo, true, DefaultTailWaypointCount, null);
    }

    /// <summary>
    /// Bản đầy đủ của <see cref="ApplyShoreOffset(Vector2, bool)"/> — thêm quyền
    /// kiểm soát waypoint đuôi và nhận StringBuilder để caller gom log riêng.
    /// </summary>
    /// <param name="offsetWorld">Độ dịch (x, y) unit world.</param>
    /// <param name="recordUndo">Ghi Undo cho từng transform.</param>
    /// <param name="moveTailWaypoints">Có dịch theo N waypoint cuối của path không (tránh path gãy khúc ở đoạn cuối).</param>
    /// <param name="tailWaypointCount">Số waypoint cuối được dịch theo (kẹp trong [1, số WP thực có]).</param>
    /// <param name="log">Nơi ghi chi tiết; null thì tool tự dựng buffer để in ra Console.</param>
    /// <returns>Số bến đã dịch được Berth.</returns>
    public static int ApplyShoreOffset(Vector2 offsetWorld, bool recordUndo,
                                       bool moveTailWaypoints, int tailWaypointCount, StringBuilder log)
    {
        GameObject root = FindBoatSystem();
        if (root == null)
        {
            Debug.LogWarning("[TouristBoat] Dịch bến sát bờ: không tìm thấy " + RootName + " trong scene — chưa dịch được gì.");
            return 0;
        }

        bool ownLog = log == null;
        if (ownLog) log = new StringBuilder();

        if (recordUndo)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
        }

        Vector3 delta = new Vector3(offsetWorld.x, offsetWorld.y, 0f);
        int movedBerths = 0, movedWps = 0;

        for (int i = 0; i < DockTotal; i++)
        {
            Transform dock = root.transform.Find(string.Format("Dock_{0:00}", i + 1));
            if (dock == null)
            {
                log.AppendLine($"- Dock_{i + 1:00}: không có trong scene — bỏ qua.");
                continue;
            }

            Transform berth = dock.Find("Berth");
            if (berth == null)
            {
                log.AppendLine($"- Dock_{i + 1:00}: thiếu con \"Berth\" — bỏ qua.");
                continue;
            }

            Vector3 from = berth.position;
            if (recordUndo) Undo.RecordObject(berth, UndoLabel);
            berth.position = from + delta;
            EditorUtility.SetDirty(berth);
            movedBerths++;
            log.AppendLine($"- Dock_{i + 1:00}/Berth: ({from.x:0}, {from.y:0}) → ({berth.position.x:0}, {berth.position.y:0})");

            if (!moveTailWaypoints) continue;

            Transform path = dock.Find("Path");
            if (path == null || path.childCount == 0)
            {
                log.AppendLine($"    (Dock_{i + 1:00} chưa có waypoint — chỉ dịch Berth)");
                continue;
            }

            // WP cuối = con cuối cùng của Path (tool sinh theo thứ tự WP_01..WP_n).
            int take = Mathf.Clamp(tailWaypointCount, 1, path.childCount);
            for (int k = path.childCount - take; k < path.childCount; k++)
            {
                Transform wp = path.GetChild(k);
                if (wp == null) continue;

                Vector3 wpFrom = wp.position;
                if (recordUndo) Undo.RecordObject(wp, UndoLabel);
                wp.position = wpFrom + delta;
                EditorUtility.SetDirty(wp);
                movedWps++;
                log.AppendLine($"    + {wp.name}: ({wpFrom.x:0}, {wpFrom.y:0}) → ({wp.position.x:0}, {wp.position.y:0})");
            }
        }

        if (recordUndo)
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        if (ownLog)
        {
            if (movedBerths == 0)
                Debug.LogWarning("[TouristBoat] Dịch bến sát bờ: không dịch được bến nào.\n" + log);
            else
                Debug.Log($"[TouristBoat] Dịch bến sát bờ — offset ({offsetWorld.x:0}, {offsetWorld.y:0}), " +
                          $"{movedBerths} Berth + {movedWps} WP:\n{log}");
        }

        return movedBerths;
    }

    /// <summary>
    /// Suy VECTOR ĐƠN VỊ hướng "từ bến vào bờ" từ scene đang mở, theo thứ tự ưu tiên:
    ///   1. Từ tâm 3 Berth hướng tới mốc đất liền (object tên "CookingGate", hoặc tên
    ///      chứa "cooking") — chính xác nhất vì nhà hàng chắc chắn nằm trên đất liền.
    ///   2. Vector BlindPoint → Berth (biển → bờ). Với layout hiện tại BlindPoint ở xa
    ///      phía tây-nam nên vector này nghiêng nhiều về +X — chỉ là ước lượng thô.
    ///   3. Không suy được → <c>Vector2.up</c> (đúng layout scene thật: đất liền ở trên).
    /// Luôn trả về vector đã chuẩn hóa, không bao giờ trả zero.
    /// </summary>
    public static Vector2 GuessShoreDirection()
    {
        GameObject root = FindBoatSystem();
        if (root == null) return Vector2.up;

        // Tâm các Berth tìm được.
        Vector2 tongBerth = Vector2.zero;
        int soBerth = 0;
        for (int i = 0; i < DockTotal; i++)
        {
            Transform berth = FindBerth(root.transform, i);
            if (berth == null) continue;
            tongBerth += new Vector2(berth.position.x, berth.position.y);
            soBerth++;
        }
        if (soBerth == 0) return Vector2.up;

        Vector2 tamBerth = tongBerth / soBerth;

        // (1) Mốc đất liền: cổng nhà hàng cooking.
        Transform datLien = TimMocDatLien();
        if (datLien != null)
        {
            Vector2 huong = new Vector2(datLien.position.x, datLien.position.y) - tamBerth;
            if (huong.sqrMagnitude > 0.0001f)
                return huong.normalized;
        }

        // (2) Fallback: biển (BlindPoint) → bờ (Berth).
        Transform blind = root.transform.Find("BlindPoint");
        if (blind != null)
        {
            Vector2 huong = tamBerth - new Vector2(blind.position.x, blind.position.y);
            if (huong.sqrMagnitude > 0.0001f)
                return huong.normalized;
        }

        // (3) Chốt hạ theo layout scene thật: đất liền ở phía trên.
        return Vector2.up;
    }

    /// <summary>Tìm mốc đất liền để suy hướng bờ: ưu tiên tên đúng "CookingGate", sau đó tên chứa "cooking".</summary>
    private static Transform TimMocDatLien()
    {
        Transform gan = null;
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.name == "CookingGate") return t;
            if (gan == null && t.name.ToLowerInvariant().Contains("cooking")) gan = t;
        }
        return gan;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Cửa sổ tool (Sếp bấm tay)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Độ dịch của Berth theo world (x, y).</summary>
    private Vector2 _offset = DefaultShoreOffset;

    /// <summary>Khoảng cách dùng cho nút tự suy hướng bờ.</summary>
    private float _shoreDistance = 200f;

    /// <summary>Có dịch theo cả N waypoint CUỐI của path không (tránh path gãy khúc ở đoạn cuối).</summary>
    private bool _moveTailWaypoints = true;

    /// <summary>Số waypoint cuối được dịch theo.</summary>
    private int _tailWaypointCount = DefaultTailWaypointCount;

    /// <summary>Offset đã áp gần nhất — cho nút hoàn tác nhanh trong cửa sổ.</summary>
    private Vector2 _lastApplied = Vector2.zero;
    private bool    _hasApplied;

    private Vector2 _scroll;
    private string  _status = "Chưa chạy lần nào.";

    [MenuItem(MenuPath, false, 12)]
    public static void Open()
    {
        var win = GetWindow<BoatShoreAdjustTool>(true, "Dịch bến sát bờ", true);
        win.minSize = new Vector2(430f, 460f);
        win.Show();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Dịch điểm đậu (Berth) của cả 3 bến", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Dùng để kéo tàu ĐẬU SÁT BỜ hơn (V2: khách phải bước từ tàu qua tấm gỗ lên bờ).\n\n" +
            "Tool dịch object Berth của Dock_01/02/03 cùng một offset. Tàu bám theo Berth nên " +
            "khi Play tàu sẽ đậu ở vị trí mới.\n\n" +
            "Ctrl+Z hoàn tác được. Nhớ Ctrl+S lưu scene sau khi ưng ý.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1) Chọn độ dịch", EditorStyles.boldLabel);
        _offset = EditorGUILayout.Vector2Field("Offset (unit world)", _offset);

        EditorGUILayout.HelpBox(
            "Mặc định (0, +200): trong scene thật bến nằm dưới (y ≈ -4285…-4839), đất liền/nhà hàng " +
            "ở trên (y ≈ -2367) nên +Y chính là hướng vào bờ. Đây cũng là số tool \"1 nút\" đang dùng.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        _shoreDistance = EditorGUILayout.FloatField("Khoảng dịch vào bờ", _shoreDistance);
        if (GUILayout.Button("Tự suy hướng bờ", GUILayout.Width(140f)))
            SuggestOffsetFromScene();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "\"Tự suy hướng bờ\" = lấy hướng từ tâm 3 Berth tới cổng nhà hàng cooking (mốc đất liền chắc chắn), " +
            "không có thì tạm dùng hướng BlindPoint → Berth, cuối cùng mới mặc định +Y. " +
            "Kết quả nhân với khoảng dịch ở trên và ghi vào ô Offset — vẫn sửa tay được trước khi áp.",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2) Path có dịch theo không", EditorStyles.boldLabel);
        _moveTailWaypoints = EditorGUILayout.Toggle("Dịch cả WP cuối", _moveTailWaypoints);
        using (new EditorGUI.DisabledScope(!_moveTailWaypoints))
        {
            _tailWaypointCount = Mathf.Max(1, EditorGUILayout.IntField("Số WP cuối dịch theo", _tailWaypointCount));
        }
        EditorGUILayout.HelpBox(
            "Berth dịch mà WP cuối đứng yên thì đoạn cuối đường tàu bị gãy nhẹ. Bật tùy chọn này " +
            "để WP cuối đi theo Berth (mặc định 1 WP là đủ với path 3 WP do tool sinh).",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3) Áp dụng", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(_offset.sqrMagnitude < 0.0001f))
        {
            if (GUILayout.Button("ÁP DỤNG cho 3 bến", GUILayout.Height(30f)))
                Apply(_offset, isUndo: false);
        }

        using (new EditorGUI.DisabledScope(!_hasApplied))
        {
            if (GUILayout.Button($"Hoàn tác lần dịch vừa rồi ({_lastApplied.x:0}, {_lastApplied.y:0})"))
                Apply(-_lastApplied, isUndo: true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Kết quả", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(_status, MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "SAU KHI DỊCH:\n" +
            "• Bấm menu \"10. Canh Tau Vao O Dau\" để tàu snap về chỗ đậu mới trong Edit Mode.\n" +
            "• Nhìn scene chỉnh tay lần cuối cho khớp mép bờ (bước REVIEW của Sếp).\n" +
            "• Gangplank (tấm gỗ) của Dev B đặt theo Berth nên tự đi theo.",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>Ghi offset gợi ý = hướng vào bờ suy từ scene × khoảng dịch đang nhập.</summary>
    private void SuggestOffsetFromScene()
    {
        if (FindBoatSystem() == null)
        {
            _status = "Không tìm thấy " + RootName + " trong scene — hãy tự nhập Offset bằng tay.";
            EditorUtility.DisplayDialog("Dịch bến sát bờ", _status, "OK");
            return;
        }

        Vector2 huong = GuessShoreDirection();
        _offset = huong * Mathf.Abs(_shoreDistance);
        _status = $"Hướng vào bờ suy được: ({huong.x:0.00}, {huong.y:0.00}) — offset gợi ý ({_offset.x:0}, {_offset.y:0}).";
        Repaint();
    }

    /// <summary>Cửa sổ gọi vào API public, rồi cập nhật dòng trạng thái + nút hoàn tác.</summary>
    private void Apply(Vector2 offset, bool isUndo)
    {
        var log = new StringBuilder();
        int movedBerths = ApplyShoreOffset(offset, true, _moveTailWaypoints, _tailWaypointCount, log);

        if (movedBerths == 0)
        {
            _status = "Không dịch được bến nào (xem Console).";
            Debug.LogWarning("[TouristBoat] Dịch bến sát bờ: không dịch được bến nào.\n" + log);
            EditorUtility.DisplayDialog("Dịch bến sát bờ", _status + "\n\nChi tiết ở Console.", "OK");
            return;
        }

        if (isUndo)
        {
            _hasApplied = false;
            _status = $"Đã hoàn tác: dịch ngược ({offset.x:0}, {offset.y:0}) cho {movedBerths} bến.";
        }
        else
        {
            _lastApplied = offset;
            _hasApplied  = true;
            _status = $"Đã dịch {movedBerths} bến theo ({offset.x:0}, {offset.y:0}).";
        }

        Debug.Log($"[TouristBoat] Dịch bến sát bờ — offset ({offset.x:0}, {offset.y:0}), {movedBerths} bến:\n{log}");
        Repaint();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static Transform FindBerth(Transform root, int dockIndex)
    {
        Transform dock = root.Find(string.Format("Dock_{0:00}", dockIndex + 1));
        return dock != null ? dock.Find("Berth") : null;
    }

    /// <summary>Tìm BoatSystem trong scene đang mở — ưu tiên theo component, fallback theo tên (kể cả đang tắt).</summary>
    private static GameObject FindBoatSystem()
    {
        var mgr = Object.FindFirstObjectByType<BoatDockManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr.gameObject;

        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == RootName)
                return t.gameObject;
        }
        return null;
    }
}
