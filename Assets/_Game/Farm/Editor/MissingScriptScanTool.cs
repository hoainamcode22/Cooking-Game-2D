#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TRUY NGUỒN "The referenced script (Unknown) on this Behaviour is missing!"
///
/// Dòng log đó của Unity KHÔNG nói object nào — thấy 15 dòng là phải mò tay từng object.
/// Tool này quét MỌI GameObject trong scene đang mở (kể cả đang tắt) + mọi prefab trong
/// Assets, in đường dẫn đầy đủ của từng object có script chết.
///
/// Script chết = component trỏ tới file .cs đã bị XOÁ hoặc ĐỔI GUID (xoá .meta, di chuyển
/// file ngoài Unity). Nó vô hại lúc chạy nhưng spam console và làm SerializedObject của
/// Inspector nổ NullReference khi list vẽ lại.
///
/// Lệnh 2 chỉ GỠ trong scene — prefab liệt kê để sửa tay (gỡ máy móc trong prefab dễ
/// mất component thật nếu prefab đang chờ script được restore).
/// </summary>
public static class MissingScriptScanTool
{
    [MenuItem("Tools/Farm/Missing Script/1. Quet (liet ke, khong sua)")]
    public static void Quet()
    {
        var sb = new StringBuilder("═══ QUÉT MISSING SCRIPT ═══\n");
        int soObj = 0, soComp = 0;

        foreach (Transform tr in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
            if (n <= 0) continue;
            soObj++; soComp += n;
            sb.Append("  SCENE  ").Append(DuongDan(tr)).Append("  — ").Append(n).Append(" script chết\n");
        }

        int soPrefab = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            int tong = 0;
            foreach (Transform tr in go.GetComponentsInChildren<Transform>(true))
                tong += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);

            if (tong <= 0) continue;
            soPrefab++;
            sb.Append("  PREFAB ").Append(path).Append("  — ").Append(tong).Append(" script chết\n");
        }

        sb.Append("Tổng: ").Append(soComp).Append(" script chết trên ").Append(soObj)
          .Append(" object trong scene, ").Append(soPrefab).Append(" prefab dính.\n");
        sb.Append(soComp + soPrefab > 0
            ? "Chạy lệnh 2 để gỡ trong SCENE. Prefab thì mở từng cái, gỡ tay."
            : "Sạch — 15 dòng log kia đến từ scene/prefab KHÁC (mở scene đó rồi quét lại).");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Farm/Missing Script/2. Go trong scene dang mo")]
    public static void Go()
    {
        int soObj = 0, soComp = 0;

        foreach (Transform tr in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject go = tr.gameObject;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) <= 0) continue;

            Undo.RegisterCompleteObjectUndo(go, "Gỡ missing script");
            soComp += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            soObj++;
            EditorUtility.SetDirty(go);
        }

        if (soObj > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("═══ ĐÃ GỠ " + soComp + " script chết trên " + soObj +
                      " object và LƯU SCENE. Ctrl+Z hoàn tác được (nhớ Ctrl+S lại nếu hoàn tác). ═══");
        }
        else Debug.Log("═══ Scene đang mở không có script chết nào. ═══");
    }

    private static string DuongDan(Transform tr)
    {
        string s = tr.name;
        while (tr.parent != null) { tr = tr.parent; s = tr.name + "/" + s; }
        return s;
    }
}
#endif
