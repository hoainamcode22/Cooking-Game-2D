using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class MasterTutorialBeautifier : EditorWindow
{
    [MenuItem("Tools/Farm/Master Beautify Tutorial & Mission UI")]
    public static void RunBeautify()
    {
        // [B3 KHOÁ TOOL PHÁ — 2026-09-05] Tool này ép sprite builtin Background + màu #8CC63F lên nút,
        // và đưa nền mờ Bg_NenToi về alpha 0 → nút "Bắt đầu nào" phẳng, popup lên cấp mất nền mờ.
        // Chặn bằng dialog, nút MẶC ĐỊNH (focus) = Huỷ. Muốn phục hồi: chạy '★ Nối lại dây popup (APPLY)'.
        bool huy = EditorUtility.DisplayDialog(
            "⚠ Tool này GHI ĐÈ style lên toàn bộ UI tutorial/mission trong scene",
            "MasterTutorialBeautifier sẽ ép sprite builtin 'Background' + màu #8CC63F lên mọi nút,\n" +
            "đổi font, và có thể làm phẳng nút 'Bắt đầu nào' / mất nền mờ popup lên cấp.\n\n" +
            "Chỉ chạy khi bạn chủ động muốn style lại tutorial. Có Undo (Ctrl+Z) nhưng KHÔNG tự lưu.\n\n" +
            "Bạn có chắc muốn chạy?",
            "Huỷ (an toàn)", "Vẫn chạy");
        if (huy) { Debug.Log("[MasterTutorialBeautifier] Đã huỷ theo yêu cầu — chưa đụng gì."); return; }

        // Colors
        Color pureWhite = Color.white;
        
        Color darkBrown = Color.black;
        ColorUtility.TryParseHtmlString("#5C4033", out darkBrown);
        
        Color woodColor = Color.black;
        ColorUtility.TryParseHtmlString("#C19A6B", out woodColor); // Light Pine Wood
        
        Color freshGreen = Color.white;
        ColorUtility.TryParseHtmlString("#8CC63F", out freshGreen);

        Color dimBlack = new Color(0, 0, 0, 0.7f);

        // Font
        TMP_FontAsset bangers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Fonts/Bangers SDF.asset");
        if (bangers == null) bangers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/LiberationSans SDF.asset");

        Sprite defaultBgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // PROCESS SCENE OBJECTS
        TutorialGuideBoardUI[] boards = FindObjectsByType<TutorialGuideBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in boards) ProcessGuideBoard(b.gameObject, pureWhite, woodColor, darkBrown, freshGreen, dimBlack, bangers, defaultBgSprite);

        TutorialManager[] tutManagers = FindObjectsByType<TutorialManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tm in tutManagers) ProcessTutorialManager(tm, pureWhite, woodColor, darkBrown, freshGreen, bangers, defaultBgSprite);

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            foreach (Transform child in c.transform)
            {
                if (child.name.Contains("Mission") || child.name.Contains("Quest") || child.name.Contains("Nhiệm Vụ") || child.name.Contains("LevelUp") || child.GetComponent("LevelUpPopupUI") != null)
                {
                    ApplyWhiteWoodStyle(child, pureWhite, woodColor, darkBrown, freshGreen, bangers, defaultBgSprite);
                    EditorUtility.SetDirty(child.gameObject);
                }
            }
        }

        // PROCESS PREFABS
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Farm")) continue; // Only Farm related

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject prefabRoot = editingScope.prefabContentsRoot;
                bool changed = false;

                TutorialGuideBoardUI tg = prefabRoot.GetComponentInChildren<TutorialGuideBoardUI>(true);
                if (tg != null)
                {
                    ProcessGuideBoard(tg.gameObject, pureWhite, woodColor, darkBrown, freshGreen, dimBlack, bangers, defaultBgSprite);
                    changed = true;
                }

                TutorialManager tm = prefabRoot.GetComponentInChildren<TutorialManager>(true);
                if (tm != null)
                {
                    ProcessTutorialManager(tm, pureWhite, woodColor, darkBrown, freshGreen, bangers, defaultBgSprite);
                    changed = true;
                }

                if (prefabRoot.name.Contains("LevelUp") || prefabRoot.name.Contains("Mission") || prefabRoot.GetComponent("LevelUpPopupUI") != null)
                {
                    ApplyWhiteWoodStyle(prefabRoot.transform, pureWhite, woodColor, darkBrown, freshGreen, bangers, defaultBgSprite);
                    changed = true;
                }

                if (changed)
                    EditorUtility.SetDirty(prefabRoot);
            }
        }

        Debug.Log("Master Tutorial & Mission UI Beautified! White background with Wood frame applied!");
    }

    private static void ProcessGuideBoard(GameObject go, Color pureWhite, Color woodColor, Color darkBrown, Color freshGreen, Color dimBlack, TMP_FontAsset font, Sprite bgSprite)
    {
        Transform rootPanel = go.transform.Find("Root") ?? go.transform.Find("Panel");
        if (rootPanel != null)
        {
            var rootImg = rootPanel.GetComponent<Image>();
            if (rootImg != null) rootImg.color = dimBlack;
            
            Transform board = rootPanel.Find("Board") ?? rootPanel.Find("Popup");
            if (board != null)
            {
                ApplyWhiteWoodStyle(board, pureWhite, woodColor, darkBrown, freshGreen, font, bgSprite);
            }
        }
        EditorUtility.SetDirty(go);
    }

    private static void ProcessTutorialManager(TutorialManager tm, Color pureWhite, Color woodColor, Color darkBrown, Color freshGreen, TMP_FontAsset font, Sprite bgSprite)
    {
        var type = typeof(TutorialManager);
        var popupField = type.GetField("_npcDialogPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (popupField != null)
        {
            GameObject popup = popupField.GetValue(tm) as GameObject;
            if (popup != null)
            {
                var rect = popup.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.sizeDelta = new Vector2(800, 200);
                    rect.anchoredPosition = new Vector2(0, 50); // Floating
                }
                ApplyWhiteWoodStyle(popup.transform, pureWhite, woodColor, darkBrown, freshGreen, font, bgSprite);
                EditorUtility.SetDirty(popup);
            }
        }
    }

    private static void ApplyWhiteWoodStyle(Transform board, Color bg, Color border, Color textCol, Color btnCol, TMP_FontAsset font, Sprite bgSprite)
    {
        var boardImg = board.GetComponent<Image>();
        if (boardImg != null)
        {
            if (bgSprite != null) boardImg.sprite = bgSprite;
            boardImg.type = Image.Type.Sliced;
            boardImg.color = bg;

            var outline = boardImg.GetComponent<Outline>();
            if (outline == null) outline = boardImg.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(6, -6);
            
            var shadow = boardImg.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = boardImg.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.3f);
                shadow.effectDistance = new Vector2(8, -8);
            }
        }

        Image[] innerImgs = board.GetComponentsInChildren<Image>(true);
        foreach (var img in innerImgs)
        {
            if (img == boardImg) continue;
            
            if (img.GetComponent<Button>() != null)
            {
                if (bgSprite != null) img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
                img.color = btnCol; 
                var outl = img.GetComponent<Outline>();
                if (outl != null) DestroyImmediate(outl);
            }
            else if (img.gameObject.name.ToLower().Contains("panel") || img.gameObject.name.ToLower().Contains("background") || img.gameObject.name.ToLower().Contains("bg"))
            {
                img.color = Color.clear;
            }
            else
            {
                // Leave icons alone
                img.color = Color.white;
            }
        }
        
        TextMeshProUGUI[] txts = board.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in txts)
        {
            if (font != null) txt.font = font;
            
            if (txt.transform.parent != null && txt.transform.parent.GetComponent<Button>() != null)
            {
                txt.color = Color.white;
            }
            else
            {
                txt.color = textCol;
            }

            var txtOutline = txt.GetComponent<Outline>();
            if (txtOutline != null) DestroyImmediate(txtOutline);
        }
    }
}
