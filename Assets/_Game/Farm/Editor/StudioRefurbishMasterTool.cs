#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// STUDIO MASTER REFURBISH TOOL — Hoàn thiện toàn diện 3 Task:
/// 1. Tối ưu Tutorial: Sửa 8 hạt giống, nâng cao khay hạt & panel hoa, sửa nút kim cương và chống kẹt.
/// 2. Popup Lên Cấp: Đồng bộ quà L2-L30, gắn nút Bắt đầu nào xịn, nối 4 nhân vật ăn mừng, pháo hoa bắn liên tục.
/// 3. Avatar Profile & Đồng bộ nút Đóng (btn_close) tròn toàn bộ các popup trong game.
/// </summary>
public static class StudioRefurbishMasterTool
{
    private const string MENU_ROOT = "Tools/Farm Game/Studio Refurbish/";

    // ─────────────────────────────────────────────────────────────────────────
    // TASK 1: NÂNG KHAY HẠT GIỐNG & PANEL HOA
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_ROOT + "1. Nâng khay hạt giống & Panel hoa (APPLY)", false, 10)]
    public static void ApplySeedPanelFix()
    {
        SeedPanelFixTool.Apply();
        Debug.Log("[StudioRefurbish] ✔ Đã nâng khay hạt giống và panel hoa thành công.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TASK 2: HOÀN THIỆN POPUP LÊN CẤP
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_ROOT + "2. Hoàn thiện Popup Lên Cấp (APPLY)", false, 20)]
    public static void ApplyLevelUpPopupFix()
    {
        // 1. Sinh sprite chuẩn
        PopupSpriteFactory.GenerateAll(force: true);

        // 2. Tìm popup trong scene
        var popup = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (popup == null)
        {
            Debug.LogError("[StudioRefurbish] Không tìm thấy LevelUpPopupUI trong scene.");
            return;
        }

        Undo.RecordObject(popup.gameObject, "Hoàn thiện Popup Lên Cấp");

        // 3. Gán sprite nút "Bắt đầu nào"
        Transform btnTiepTucTr = popup.transform.Find("Root_HienThi/Content/Btn_TiepTuc")
                             ?? popup.transform.Find("Content/Btn_TiepTuc")
                             ?? popup.transform.Find("Btn_TiepTuc");
        if (btnTiepTucTr != null)
        {
            var btnImg = btnTiepTucTr.GetComponent<Image>();
            if (btnImg != null)
            {
                Undo.RecordObject(btnImg, "Gán sprite nút Bắt đầu nào");
                Sprite sprBtn = PopupSpriteFactory.Load("spr_btn_green") ?? UIStandardSprites.BtnGreen;
                if (sprBtn != null)
                {
                    btnImg.sprite = sprBtn;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = Color.white;
                    EditorUtility.SetDirty(btnImg);
                }
            }
        }

        // 4. Gắn pháo hoa sprites
        var soPopup = new SerializedObject(popup);
        var propUseUI = soPopup.FindProperty("useUIFireworks");
        if (propUseUI != null) propUseUI.boolValue = true;

        var propSprites = soPopup.FindProperty("fireworkSprites");
        if (propSprites != null)
        {
            string[] fireworkNames = { "confetti_01", "confetti_02", "confetti_03", "confetti_04", "confetti_05", "confetti_06", "spark_star" };
            var list = new List<Sprite>();
            foreach (var fn in fireworkNames)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/LevelUpV2/fireworks/{fn}.png");
                if (s != null) list.Add(s);
            }
            if (list.Count > 0)
            {
                propSprites.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                    propSprites.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }
        soPopup.ApplyModifiedProperties();

        // 5. Nối lại 4 nhân vật ăn mừng
        LevelUpPopupRewireTool.Apply();

        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
        Debug.Log("[StudioRefurbish] ✔ Đã hoàn thiện Popup Lên Cấp thành công.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TASK 3: ĐỒNG BỘ NÚT ĐÓNG TOÀN BỘ POPUP & AVATAR PROFILE
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(MENU_ROOT + "3. Đồng bộ Nút Đóng tròn toàn Game & Avatar (APPLY)", false, 30)]
    public static void ApplyCloseButtonUnification()
    {
        Sprite closeSprite = UIStandardSprites.Close;
        if (closeSprite == null)
        {
            closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Kitchen_UI_Package/Sprites/btn_red_small.png")
                       ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/btn_close.png");
        }

        if (closeSprite == null)
        {
            Debug.LogError("[StudioRefurbish] Không tìm thấy sprite nút đóng chuẩn.");
            return;
        }

        int count = 0;
        var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var modifiedScenes = new HashSet<Scene>();

        foreach (var btn in allButtons)
        {
            if (btn == null) continue;
            string n = btn.name.ToLowerInvariant();
            if (n.Contains("close") || n.Contains("btn_close") || n.Contains("button_close") || n == "btn_dong")
            {
                // Bỏ qua nút chữ "ĐÓNG" to ở đáy (như btnBottomClose)
                if (n.Contains("bottom")) continue;

                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, "Đồng bộ nút đóng");
                    img.sprite = closeSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);

                    var rt = btn.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Undo.RecordObject(rt, "Đồng bộ size nút đóng");
                        rt.sizeDelta = UIStandardSprites.CloseSize;
                        EditorUtility.SetDirty(rt);
                    }

                    // Đảm bảo chữ X rõ ràng nếu có
                    var txt = btn.GetComponentInChildren<TMP_Text>(true);
                    if (txt != null && (txt.text == "X" || txt.text == "x" || string.IsNullOrWhiteSpace(txt.text)))
                    {
                        Undo.RecordObject(txt, "Đồng bộ chữ X");
                        txt.text = "X";
                        txt.fontSize = UIStandardSprites.CloseGlyphSize;
                        txt.fontStyle = FontStyles.Bold;
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.color = Color.white;
                        EditorUtility.SetDirty(txt);
                    }

                    modifiedScenes.Add(btn.gameObject.scene);
                    count++;
                }
            }
        }

        // Tái tạo / format Avatar Profile Popup
        var avatarPopup = Object.FindObjectsByType<AvatarProfilePopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (avatarPopup != null)
        {
            Undo.RecordObject(avatarPopup.gameObject, "Update Avatar Profile");
            EditorUtility.SetDirty(avatarPopup);
            modifiedScenes.Add(avatarPopup.gameObject.scene);
        }

        foreach (var sc in modifiedScenes)
        {
            if (sc.IsValid() && sc.isLoaded)
                EditorSceneManager.MarkSceneDirty(sc);
        }

        Debug.Log($"[StudioRefurbish] ✔ Đã đồng bộ {count} nút đóng trên {modifiedScenes.Count} scene.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TỔNG HỢP: CHẠY TẤT CẢ 3 TASK TRONG 1 NÚT BẤM
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Farm Game/★ SETUP TOÀN BỘ TUTORIAL L1-L2 (1 BẤM LÀ XONG)", false, -100)]
    [MenuItem(MENU_ROOT + "★ CHẠY TẤT CẢ 3 TASK (1 Nút)", false, 0)]
    public static void RunAll()
    {
        TutorialStudioTool.DonCay();
        TutorialV2SetupTool.Dung();
        SetupTutorialL1L2Tool.RunSetupSilent();
        ApplySeedPanelFix();
        ApplyLevelUpPopupFix();
        ApplyCloseButtonUnification();
        Debug.Log("[StudioRefurbish] 🚀 HOÀN TẤT SETUP TUTORIAL! Nhớ bấm Ctrl+S để lưu Scene.");
    }
}
#endif
