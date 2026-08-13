using System.Text;
using UnityEditor;
using UnityEngine;


public static class DonMissingScriptTool
{
    private const string Menu = "Tools/Farm/Dọn Script Mất/";

    /// <summary>Chỉ dọn trong các nhánh này — không quét mù cả scene.</summary>
    private static readonly string[] NhanhChoPhep =
        { "PigPenPopup", "ChickenPenPopup", "CowPenPopup" };

    [MenuItem(Menu + "1 · Đếm lại (chỉ đọc)", false, 1)]
    public static void DemLai()
    {
        var sb = new StringBuilder("═══ COMPONENT MẤT SCRIPT TRONG 3 POPUP CHUỒNG CŨ ═══\n");
        int tong = 0;

        foreach (GameObject goc in TimGocPopup())
        {
            foreach (Transform t in goc.GetComponentsInChildren<Transform>(true))
            {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (n <= 0) continue;
                tong += n;
                sb.AppendLine($"  • {DuongDan(t)}  ({n})");
            }
        }

        sb.AppendLine($"\n  Tổng: {tong}. Chạy mục 2 để gỡ (Ctrl+Z được).");
        Debug.Log(sb.ToString());
    }

    [MenuItem(Menu + "2 · Gỡ component mất script (giữ nguyên object)", false, 2)]
    public static void Don()
    {
        int tong = 0, soObj = 0;
        var sb = new StringBuilder();

        foreach (GameObject goc in TimGocPopup())
        {
            foreach (Transform t in goc.GetComponentsInChildren<Transform>(true))
            {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (n <= 0) continue;

                Undo.RegisterCompleteObjectUndo(t.gameObject, "Gỡ script mất");
                int daGo = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                tong += daGo; soObj++;
                sb.AppendLine($"  − {DuongDan(t)}  ({daGo})");
            }
        }

        if (tong > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(tong > 0
            ? $"[DọnScript] ✅ Gỡ {tong} component trên {soObj} object.\n{sb}→ Ctrl+S để lưu scene."
            : "[DọnScript] Không còn gì để gỡ — đã sạch từ trước.");
    }

    private static System.Collections.Generic.List<GameObject> TimGocPopup()
    {
        var ket = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || EditorUtility.IsPersistent(t.gameObject)) continue;
            foreach (string ten in NhanhChoPhep)
                if (t.name == ten) { ket.Add(t.gameObject); break; }
        }
        return ket;
    }

    private static string DuongDan(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
