using UnityEditor;
using UnityEngine;

/// <summary>Gắn vỏ mock B/A cho Kho + Hồ sơ — cùng dòng menu với vỏ Shop.</summary>
public static class VoKhoHoSoTool
{
    [MenuItem("Tools/Farm/Thay Áo Popup/6 · Kho — vỏ mock B", false, 14)]
    public static void GanKho() => Gan<WarehousePopupUI, KhoSkin>("Kho Vật Phẩm");

    [MenuItem("Tools/Farm/Thay Áo Popup/7 · Hồ sơ — vỏ mock A", false, 15)]
    public static void GanHoSo() => Gan<AvatarProfilePopupUI, HoSoSkin>("Hồ Sơ Avatar");

    private static void Gan<TChu, TAo>(string ten)
        where TChu : MonoBehaviour where TAo : MonoBehaviour
    {
        var chu = Object.FindFirstObjectByType<TChu>(FindObjectsInactive.Include);
        if (chu == null)
        {
            EditorUtility.DisplayDialog("Vỏ popup", $"Không thấy {typeof(TChu).Name} trong scene.", "OK");
            return;
        }

        // Gỡ applier màu đại trà nếu còn — hai lớp vỏ chồng nhau chỉ tổ rối.
        var cu = chu.GetComponent<PopupSkinApplier>();
        if (cu != null) Undo.DestroyObjectImmediate(cu);

        // Dọn XÁC VỎ BẢN 1: KhoSkin/HoSoSkin từng nằm chung file `KhoHoSoSkin.cs`
        // (sai luật Unity: MonoBehaviour phải ở file trùng tên class) nên component
        // đã gắn hoá "missing script" sau khi tách file. Chỉ dọn TRÊN ĐÚNG object
        // này — không quét mù cả scene.
        int xac = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(chu.gameObject);
        if (xac > 0) Debug.Log($"[Vỏ] Dọn {xac} missing script (xác vỏ cũ) trên '{chu.gameObject.name}'.");

        if (chu.GetComponent<TAo>() == null) Undo.AddComponent<TAo>(chu.gameObject);

        EditorUtility.SetDirty(chu);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(chu.gameObject.scene);
        Debug.Log($"[Vỏ] ✅ Gắn {typeof(TAo).Name} lên '{chu.gameObject.name}' ({ten}). " +
                  "Ctrl+S → Play → mở popup xem. Bỏ tick 'Bật Áo' là về vỏ cũ.");
    }
}
