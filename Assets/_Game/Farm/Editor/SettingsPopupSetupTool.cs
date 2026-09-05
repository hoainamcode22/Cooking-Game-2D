using UnityEditor;
using UnityEngine;

public static class SettingsPopupSetupTool
{
    private const string MenuPath = "Tools/Farm Game/Popups/Dựng Popup Cài Đặt (Settings)";

    [MenuItem(MenuPath, false, 25)]
    public static void SetupSettingsPopup()
    {
        SettingsPopupUI existing = Object.FindFirstObjectByType<SettingsPopupUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog("Cài Đặt", "Popup_Settings đã có sẵn trong Scene!", "OK");
            return;
        }

        Transform canvas = AvatarProfilePopupUI.FindCanvasPopup();
        if (canvas == null)
        {
            Canvas anyCanvas = Object.FindFirstObjectByType<Canvas>();
            if (anyCanvas != null) canvas = anyCanvas.transform;
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas trong scene!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Dựng Popup Cài Đặt");

        SettingsPopupUI created = SettingsPopupUI.CreateHierarchy(canvas);
        if (created != null)
        {
            Undo.RegisterCreatedObjectUndo(created.gameObject, "Dựng Popup Cài Đặt");
            created.gameObject.SetActive(false);
            Selection.activeGameObject = created.gameObject;
            EditorGUIUtility.PingObject(created.gameObject);
            EditorUtility.DisplayDialog("Thành công", "Đã dựng xong Popup_Settings (Cài Đặt) với đầy đủ slider Âm thanh, VFX, nút Ngôn ngữ và nút Đóng!", "OK");
        }
    }
}
