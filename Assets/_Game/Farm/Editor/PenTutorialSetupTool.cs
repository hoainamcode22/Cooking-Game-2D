using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CHUẨN BỊ CHUỒNG CHO TUTORIAL — bật đúng chuồng gà, tắt các chuồng mở sau.
///
/// VÌ SAO CẦN TOOL chứ không sửa tay trong Hierarchy: bốn prefab chuồng có tới BẢY bản
/// trong scene (Pen_01 ×2, Pen_02 ×2, Pen_03 ×2, Pen_04 ×1), phần lớn đang tắt và nằm rải
/// rác ngoài vùng camera. Tick tay trong Hierarchy thì rất dễ bật đúng bản nằm ở
/// (-2762, -957) — cách ruộng gần 3000 unit — rồi ngồi tự hỏi vì sao camera lia vào chỗ
/// trống. Tool này in ra toạ độ từng bản trước khi đổi, và chọn bản GẦN RUỘNG NHẤT.
///
/// LOGIC:
///   • Chuồng gà `Pen_03` mở khoá cấp 2 (100 vàng) → là chuồng đầu tiên người chơi có,
///     nên tutorial L2 phải dạy trên nó.
///   • `Pen_01` (bò, cấp 8) đang bật là sai mạch: người chơi cấp 2 nhìn thấy công trình
///     phải tới cấp 8 mới mua được.
///   • Bản `Pen_03` được bật sẽ ĐƯỢC DỜI tới đúng chỗ `Pen_01` đang đứng, để bố cục
///     ngoài map không bị trống một khoảng ở nơi người chơi vốn đã thấy có chuồng.
/// </summary>
public static class PenTutorialSetupTool
{
    private const string Menu = "Tools/Farm/Chuồng/";

    /// <summary>Tên chuồng cần BẬT — lấy từ một chỗ duy nhất, không gõ lại.</summary>
    private static string TenChuongBat => TutorialManager.TenChuongTutorial;

    /// <summary>Các chuồng mở khoá muộn hơn — tắt hết để không lộ ra ở cấp thấp.</summary>
    private static readonly string[] ChuongTatBot = { "Pen_01", "Pen_02", "Pen_04" };

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(Menu + "1 · Xem trạng thái các chuồng", false, 1)]
    public static void XemTrangThai()
    {
        var all = TimTatCaChuong();
        if (all.Count == 0)
        {
            Debug.LogWarning("[Chuồng] Không thấy chuồng nào trong scene đang mở. " +
                             "Mở SCN_Farm rồi chạy lại.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ CHUỒNG TRONG SCENE ═══");
        sb.AppendLine($"  Tutorial đang trỏ vào: {TenChuongBat}");
        sb.AppendLine();

        foreach (var g in all)
        {
            Vector3 p = g.transform.position;
            sb.AppendLine($"  {(g.activeSelf ? "BẬT " : " tắt")}  {g.name,-8} " +
                          $"tại ({p.x:F0}, {p.y:F0})   cha: {TenCha(g)}");
        }

        sb.AppendLine();
        sb.AppendLine("  Thứ tự mở khoá: gà Pen_03 (L2) → heo Pen_02 (L4) → " +
                      "bò Pen_01 (L8) → bò sữa Pen_04 (L13)");
        Debug.Log(sb.ToString());
    }

    [MenuItem(Menu + "2 · Bật chuồng tutorial, tắt các chuồng mở sau", false, 2)]
    public static void BatChuongTutorial()
    {
        var all = TimTatCaChuong();
        if (all.Count == 0)
        {
            EditorUtility.DisplayDialog("Chuồng",
                "Không thấy chuồng nào trong scene.\nMở SCN_Farm rồi chạy lại.", "OK");
            return;
        }

        // Ghi nhớ chỗ chuồng ĐANG BẬT đứng, để dời chuồng gà vào đúng đó. Lấy bản đang
        // bật ĐẦU TIÊN trong nhóm cần tắt — đó là cái người chơi thật sự đang nhìn thấy.
        Vector3? choDangThay = null;
        foreach (var g in all)
        {
            if (!g.activeSelf) continue;
            if (g.name != TenChuongBat && LaChuongCanTat(g.name))
            {
                choDangThay = g.transform.position;
                break;
            }
        }

        Undo.SetCurrentGroupName("Bật chuồng tutorial");
        int nhom = Undo.GetCurrentGroup();

        // ── Tắt các chuồng mở sau ────────────────────────────────────────────
        int daTat = 0;
        foreach (var g in all)
        {
            if (g.name == TenChuongBat) continue;
            if (!LaChuongCanTat(g.name)) continue;
            if (!g.activeSelf) continue;

            Undo.RecordObject(g, "Tắt chuồng");
            g.SetActive(false);
            Debug.Log($"[Chuồng] Tắt {g.name} tại ({g.transform.position.x:F0}, " +
                      $"{g.transform.position.y:F0}).");
            daTat++;
        }

        // ── Bật chuồng tutorial ──────────────────────────────────────────────
        GameObject chon = ChonBanGanRuongNhat(all, TenChuongBat, choDangThay);
        if (chon == null)
        {
            Undo.CollapseUndoOperations(nhom);
            EditorUtility.DisplayDialog("Chuồng",
                $"Không tìm thấy '{TenChuongBat}' trong scene.\n\n" +
                "Kéo prefab Assets/_Game/Farm/CÔNG TRÌNH/Pen_03.prefab vào scene rồi chạy lại.",
                "OK");
            return;
        }

        Undo.RecordObject(chon, "Bật chuồng tutorial");
        Undo.RecordObject(chon.transform, "Dời chuồng tutorial");

        chon.SetActive(true);

        if (choDangThay.HasValue)
        {
            // Giữ nguyên Z: chuồng có thể đang dùng Z để xếp lớp vẽ, ghi đè là chui
            // xuống dưới đất hoặc đè lên nhà.
            Vector3 dich = choDangThay.Value;
            dich.z = chon.transform.position.z;
            chon.transform.position = dich;
            Debug.Log($"[Chuồng] Dời {chon.name} tới ({dich.x:F0}, {dich.y:F0}) — " +
                      "đúng chỗ chuồng cũ đang đứng.");
        }

        // Tắt các bản TRÙNG TÊN còn lại, không thì GameObject.Find có thể bắt bản khác.
        int trung = 0;
        foreach (var g in all)
        {
            if (g == chon || g.name != TenChuongBat || !g.activeSelf) continue;
            Undo.RecordObject(g, "Tắt bản trùng");
            g.SetActive(false);
            trung++;
        }

        Undo.CollapseUndoOperations(nhom);
        Selection.activeGameObject = chon;
        EditorGUIUtility.PingObject(chon);
        DanhDauSceneCanLuu();

        Debug.Log($"[Chuồng] ✅ Xong. Bật {chon.name}, tắt {daTat} chuồng mở sau" +
                  (trung > 0 ? $" và {trung} bản trùng tên" : "") + ".\n" +
                  "→ Ctrl+S để lưu scene, rồi Play để thử bước tutorial cho gà ăn.");
    }

    [MenuItem(Menu + "3 · Kiểm tra tutorial có tìm được chuồng không", false, 3)]
    public static void KiemTraTutorial()
    {
        // Đúng cách `TutorialRuntimeTargetResolver` dò: GameObject.Find, KHÔNG thấy
        // object đang tắt. Kiểm bằng chính hàm đó mới phản ánh được thực tế lúc chạy.
        GameObject tim = GameObject.Find(TenChuongBat);
        if (tim == null) tim = GameObject.Find(TenChuongBat + "(Clone)");

        if (tim != null)
        {
            Vector3 p = tim.transform.position;
            Debug.Log($"[Chuồng] ✅ Tutorial sẽ tìm thấy '{tim.name}' tại ({p.x:F0}, {p.y:F0}).\n" +
                      "Bàn tay và camera sẽ trỏ đúng vào đây.");
            Selection.activeGameObject = tim;
            return;
        }

        var tatCa = TimTatCaChuong();
        var datTen = new List<string>();
        foreach (var g in tatCa)
            if (g.name == TenChuongBat)
                datTen.Add($"({g.transform.position.x:F0}, {g.transform.position.y:F0}) — đang TẮT");

        Debug.LogError($"[Chuồng] ❌ Tutorial KHÔNG tìm được '{TenChuongBat}'.\n" +
                       "→ Bước L2_07_FocusPen sẽ TREO: camera không lia, bàn tay không hiện.\n" +
                       (datTen.Count > 0
                           ? "Có " + datTen.Count + " bản trong scene nhưng đều tắt:\n    " +
                             string.Join("\n    ", datTen) + "\n→ Chạy mục 2 ở trên."
                           : "Không có bản nào trong scene. Kéo Pen_03.prefab vào scene."));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    private static bool LaChuongCanTat(string ten)
    {
        foreach (string t in ChuongTatBot)
            if (ten == t || ten == t + "(Clone)") return true;
        return false;
    }

    /// <summary>
    /// Quét mọi chuồng KỂ CẢ ĐANG TẮT. `GameObject.Find`/`FindObjectsByType` mặc định bỏ
    /// qua object tắt — mà chính những bản tắt mới là thứ cần tìm ở đây.
    /// </summary>
    private static List<GameObject> TimTatCaChuong()
    {
        var ket = new List<GameObject>();
        var canTim = new List<string>(ChuongTatBot) { TenChuongBat };

        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;

            // Bỏ qua object trong prefab preview scene / asset, chỉ lấy trong scene đang mở.
            if (EditorUtility.IsPersistent(t.gameObject)) continue;

            foreach (string ten in canTim)
            {
                if (t.name != ten && t.name != ten + "(Clone)") continue;
                ket.Add(t.gameObject);
                break;
            }
        }

        ket.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return ket;
    }

    /// <summary>
    /// Trong nhiều bản trùng tên, chọn bản GẦN mốc nhất (mốc = chỗ chuồng cũ đứng).
    /// Không có mốc thì chọn bản gần cụm ô đất — chuồng ở (-2762, -957) mà bật lên là
    /// camera lia ra vùng trống ngoài bản đồ.
    /// </summary>
    private static GameObject ChonBanGanRuongNhat(List<GameObject> all, string ten, Vector3? moc)
    {
        // Tâm cụm 8 ô đất, lấy từ RICE_ORDER trong TutorialRuntimeTargetResolver.
        Vector3 tamRuong = moc ?? new Vector3(2337f, -1038f, 0f);

        GameObject tot = null;
        float gan = float.MaxValue;

        foreach (var g in all)
        {
            if (g.name != ten && g.name != ten + "(Clone)") continue;

            float d = Vector2.SqrMagnitude(
                (Vector2)g.transform.position - (Vector2)tamRuong);
            if (d >= gan) continue;

            gan = d;
            tot = g;
        }

        return tot;
    }

    private static string TenCha(GameObject g)
        => g.transform.parent != null ? g.transform.parent.name : "(gốc scene)";

    private static void DanhDauSceneCanLuu()
    {
        if (Application.isPlaying) return;
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
