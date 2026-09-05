#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ⛔ [VÒNG 13 — 04/09/2026] ĐÃ TẮT TỰ CHẠY THEO LỆNH LEAD.
// Trước đây attribute [InitializeOnLoad] khiến static constructor chạy MỖI LẦN Unity biên dịch
// lại, kéo theo EditorApplication.delayCall → tool tự sửa scene rồi TỰ LƯU. Hậu quả: mọi thứ
// Sếp kéo tay trong scene (vị trí prefab tàu, nút HUD, reference nhân vật popup) đều bị ghi đè
// âm thầm sau mỗi lần compile — đây chính là nguyên nhân của chuỗi lỗi "tự nhiên hỏng".
// Menu trong Tools/... VẪN CÒN — muốn chạy thì bấm tay, chủ động và kiểm soát được.
// Muốn bật lại: bỏ dấu // ở dòng dưới.
// [InitializeOnLoad]
public static class BakePopupsIntoFarmScene
{
    private const string MenuPath = "Tools/Farm Game/Popups/Bake Cài Đặt & Hồ Sơ Vào SCN_Farm";

    static BakePopupsIntoFarmScene()
    {
        // ⛔ [VÒNG 14] ĐÃ TẮT — dòng dưới từng khiến tool tự chạy + tự lưu scene mỗi lần compile.
        // Comment [InitializeOnLoad] ở vòng 13 là CHƯA ĐỦ: chỉ cần code khác chạm vào bất kỳ
        // member nào của class là static constructor vẫn chạy, và dòng này vẫn đăng ký.
        // Muốn chạy: bấm menu trong Tools/... (chủ động, kiểm soát được).
        // EditorApplication.delayCall += AutoCheckAndBake;
    }

    private static void AutoCheckAndBake()
    {
        // Tự động kiểm tra khi load Editor
        var openScene = EditorSceneManager.GetActiveScene();
        if (openScene.name == "SCN_Farm")
        {
            EnsurePopupsInCurrentScene(false);
        }
    }

    [MenuItem(MenuPath, false, 10)]
    public static void BakeNow()
    {
        string scenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        EnsurePopupsInCurrentScene(true);
    }

    public static void EnsurePopupsInCurrentScene(bool showDialog)
    {
        GameObject canvasPopup = GameObject.Find("Canvas_Popup");
        if (canvasPopup == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (anyCanvas != null) canvasPopup = anyCanvas.gameObject;
        }

        if (canvasPopup == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas_Popup trong scene SCN_Farm!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Bake Cài Đặt & Hồ Sơ Vào Scene");

        // ═════════════════════════════════════════════════════════════════════
        // 1. REBUILD POPUP_AVATARPROFILE (DẤU TÍCH V & EXP XANH BIỂN & 4 ICONS)
        // ═════════════════════════════════════════════════════════════════════
        Transform oldProfile = canvasPopup.transform.Find("Popup_AvatarProfile");
        if (oldProfile != null)
        {
            Undo.DestroyObjectImmediate(oldProfile.gameObject);
        }

        AvatarProfilePopupUI newProfile = AvatarProfilePopupUI.CreateHierarchy(canvasPopup.transform);
        if (newProfile != null)
        {
            newProfile.gameObject.name = "Popup_AvatarProfile";
            newProfile.gameObject.SetActive(false);
            EditorUtility.SetDirty(newProfile);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 2. BUILD POPUP_SETTINGS (CÀI ĐẶT ÂM THANH GAME, VFX, NGÔN NGỮ, CLOSE)
        // ═════════════════════════════════════════════════════════════════════
        Transform oldSettings = canvasPopup.transform.Find("Popup_Settings");
        if (oldSettings != null)
        {
            Undo.DestroyObjectImmediate(oldSettings.gameObject);
        }

        SettingsPopupUI newSettings = SettingsPopupUI.CreateHierarchy(canvasPopup.transform);
        if (newSettings != null)
        {
            newSettings.gameObject.name = "Popup_Settings";
            newSettings.gameObject.SetActive(false);
            EditorUtility.SetDirty(newSettings);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 3. WIRE VÀO TOWNSHIP HUD CONTROLLER & ĐẶT LAYER UI
        // ═════════════════════════════════════════════════════════════════════
        var hud = Object.FindFirstObjectByType<FarmGame.UI.TownshipHUDController>(FindObjectsInactive.Include);
        if (hud != null)
        {
            if (hud.btnSettings == null)
            {
                var allButtons = hud.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < allButtons.Length; i++)
                {
                    if (allButtons[i].name.Contains("Setting") || allButtons[i].name.Contains("CaiDat"))
                    {
                        hud.btnSettings = allButtons[i];
                        break;
                    }
                }
            }

            if (hud.btnSettings != null)
            {
                hud.btnSettings.gameObject.layer = 5; // Layer UI
                EditorUtility.SetDirty(hud.btnSettings.gameObject);
            }

            EditorUtility.SetDirty(hud);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 4. XOÁ BỎ HOÀN TOÀN VÒNG TRÒN VÀNG (LOCKICON KNOB) & CĂN CHỈNH BẢNG KHÓA
        // ═════════════════════════════════════════════════════════════════════
        var allDockSlots = Object.FindObjectsByType<BoatDockSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allDockSlots.Length; i++)
        {
            var slot = allDockSlots[i];
            var dockTf = slot.transform;

            // Xoá hoặc tắt LockIcon tròn vàng placeholder
            Transform lockIcon = dockTf.Find("LockUI/LockIcon");
            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(false);
                lockIcon.localScale = Vector3.zero;
                EditorUtility.SetDirty(lockIcon.gameObject);
            }

            // Sửa BoxCollider2D trên Dock hoặc LockUI cho vừa khít bảng gỗ 180x90 ở vị trí (0,0)
            var col = dockTf.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(180f, 90f);
                col.offset = new Vector2(0f, 35f); // Khớp vị trí LockUI
                EditorUtility.SetDirty(col);
            }

            Transform lockUi = dockTf.Find("LockUI");
            if (lockUi != null)
            {
                var lockCol = lockUi.GetComponent<BoxCollider2D>();
                if (lockCol == null) lockCol = lockUi.gameObject.AddComponent<BoxCollider2D>();
                lockCol.size = new Vector2(180f, 90f);
                lockCol.offset = Vector2.zero;
                EditorUtility.SetDirty(lockCol);

                Transform tt = lockUi.Find("TeaserText");
                if (tt != null)
                {
                    var tmp = tt.GetComponent<TextMeshPro>();
                    if (tmp != null)
                    {
                        tmp.fontSize = 18f;
                        tmp.alignment = TextAlignmentOptions.Center;
                        var rt = tmp.rectTransform;
                        if (rt != null) rt.sizeDelta = new Vector2(160f, 60f);
                        tt.localPosition = new Vector3(0f, -5f, 0f);
                        EditorUtility.SetDirty(tmp);
                    }
                }
            }

            EditorUtility.SetDirty(slot);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 5. TĂNG SIZE TÀU DU LỊCH ĐẸP MẮT (680 UNIT WORLD)
        // ═════════════════════════════════════════════════════════════════════
        var allBoats = Object.FindObjectsByType<TouristBoatController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allBoats.Length; i++)
        {
            var soBoat = new SerializedObject(allBoats[i]);
            var propWidth = soBoat.FindProperty("boatWorldWidth");
            if (propWidth != null)
            {
                propWidth.floatValue = 680f;
                soBoat.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(allBoats[i]);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 6. DỰNG VÀ GHIM NÚT EDIT MODE (SỬA) LUÔN HIỆN TRÊN HUD
        // ═════════════════════════════════════════════════════════════════════
        try
        {
            HudEditModeButtonSetupTool.DungNut(true);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BakePopupsIntoFarmScene] DungNut EditMode exception: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(canvasPopup.scene);
        EditorSceneManager.SaveScene(canvasPopup.scene);

        Debug.Log("[BakePopupsIntoFarmScene] Đã dựng và lưu thành công:\n1. Popup_AvatarProfile (Dấu tích V)\n2. Popup_Settings (3D bo góc & Cờ VN/EN)\n3. Bảng khóa bến tàu nhỏ gọn 180x90\n4. Tăng size tàu du lịch lên 680 unit\n5. Ghim nút Sửa (Edit Mode Khung thẻ + Búa) cố định trên HUD!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công", "Đã cập nhật và lưu thẳng vào SCN_Farm.unity:\n1. Nút Sửa (Edit Mode Khung thẻ + Icon Búa) ghim cố định trên HUD (Top-Left)\n2. Thu nhỏ Bảng khóa bến tàu & chữ 'Mở ở Lv12' gọn gàng vừa vặn\n3. Tăng kích thước Tàu Du Lịch lên 680 unit rõ nét\n4. Popup Cài Đặt 3D (Cờ VN/EN, không ô vuông trắng, không trôi map)\n5. Popup Hồ Sơ (Dấu tích chữ V, EXP xanh biển, 4 Icon)", "Tuyệt vời!");
        }
    }
}
#endif
