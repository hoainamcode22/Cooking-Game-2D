using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class HUDGenerator : EditorWindow
{
    [MenuItem("Tools/Farm Game/Generate HUD")]
    public static void GenerateHUD()
    {
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Event System if not exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Get default UI Sprite
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // 3. TopLeft Anchor Panel
        GameObject topLeftPanel = new GameObject("TopLeftPanel");
        topLeftPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform tlRect = topLeftPanel.AddComponent<RectTransform>();
        tlRect.anchorMin = new Vector2(0, 1);
        tlRect.anchorMax = new Vector2(0, 1);
        tlRect.pivot = new Vector2(0, 1);
        tlRect.anchoredPosition = new Vector2(20, -20);
        tlRect.sizeDelta = new Vector2(400, 150);

        // Avatar Frame
        GameObject avatarFrame = CreateUIElement("AvatarFrame", topLeftPanel.transform, uiSprite);
        RectTransform avatarRect = avatarFrame.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0, 0.5f);
        avatarRect.anchorMax = new Vector2(0, 0.5f);
        avatarRect.pivot = new Vector2(0, 0.5f);
        avatarRect.anchoredPosition = new Vector2(10, 0);
        avatarRect.sizeDelta = new Vector2(120, 120);

        // Level Star (overlap)
        GameObject levelStar = CreateUIElement("LevelStar", avatarFrame.transform, uiSprite);
        RectTransform starRect = levelStar.GetComponent<RectTransform>();
        starRect.anchorMin = new Vector2(1, 0);
        starRect.anchorMax = new Vector2(1, 0);
        starRect.pivot = new Vector2(0.5f, 0.5f);
        starRect.anchoredPosition = new Vector2(-10, 10); // bottom right overlap
        starRect.sizeDelta = new Vector2(40, 40);
        levelStar.GetComponent<Image>().color = new Color(1f, 0.9f, 0.1f, 1f); // Yellowish star
        
        CreateTextElement("LevelText", levelStar.transform, "1");

        // EXP Bar Background
        GameObject expBarBg = CreateUIElement("ExpBarBg", topLeftPanel.transform, backgroundSprite);
        RectTransform expBgRect = expBarBg.GetComponent<RectTransform>();
        expBgRect.anchorMin = new Vector2(0, 0.5f);
        expBgRect.anchorMax = new Vector2(0, 0.5f);
        expBgRect.pivot = new Vector2(0, 0.5f);
        expBgRect.anchoredPosition = new Vector2(140, 0);
        expBgRect.sizeDelta = new Vector2(200, 30);
        expBarBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // EXP Bar Fill
        GameObject expBarFill = CreateUIElement("ExpBarFill", expBarBg.transform, uiSprite);
        RectTransform expFillRect = expBarFill.GetComponent<RectTransform>();
        expFillRect.anchorMin = new Vector2(0, 0);
        expFillRect.anchorMax = new Vector2(1, 1);
        expFillRect.pivot = new Vector2(0, 0.5f);
        expFillRect.offsetMin = new Vector2(2, 2);
        expFillRect.offsetMax = new Vector2(-2, -2);
        expBarFill.GetComponent<Image>().color = new Color(0.1f, 0.8f, 0.1f, 1f); // Greenish
        expBarFill.GetComponent<Image>().type = Image.Type.Filled;
        expBarFill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        expBarFill.GetComponent<Image>().fillAmount = 0.5f;

        // EXP Text
        CreateTextElement("ExpText", expBarBg.transform, "50 / 100");

        // 4. TopRight Anchor Panel
        GameObject topRightPanel = new GameObject("TopRightPanel");
        topRightPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform trRect = topRightPanel.AddComponent<RectTransform>();
        trRect.anchorMin = new Vector2(1, 1);
        trRect.anchorMax = new Vector2(1, 1);
        trRect.pivot = new Vector2(1, 1);
        trRect.anchoredPosition = new Vector2(-20, -20);
        trRect.sizeDelta = new Vector2(500, 150);

        // Settings Gear Icon
        GameObject settingsIcon = CreateUIElement("SettingsIcon", topRightPanel.transform, uiSprite);
        RectTransform settingsRect = settingsIcon.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(1, 0.5f);
        settingsRect.anchorMax = new Vector2(1, 0.5f);
        settingsRect.pivot = new Vector2(1, 0.5f);
        settingsRect.anchoredPosition = new Vector2(0, 0);
        settingsRect.sizeDelta = new Vector2(60, 60);

        // Diamond Frame
        GameObject diamondFrame = CreateUIElement("DiamondFrame", topRightPanel.transform, backgroundSprite);
        RectTransform diamondRect = diamondFrame.GetComponent<RectTransform>();
        diamondRect.anchorMin = new Vector2(1, 0.5f);
        diamondRect.anchorMax = new Vector2(1, 0.5f);
        diamondRect.pivot = new Vector2(1, 0.5f);
        diamondRect.anchoredPosition = new Vector2(-80, 0);
        diamondRect.sizeDelta = new Vector2(160, 40);

        GameObject diamondIcon = CreateUIElement("DiamondIcon", diamondFrame.transform, uiSprite);
        RectTransform diamondIconRect = diamondIcon.GetComponent<RectTransform>();
        diamondIconRect.anchorMin = new Vector2(0, 0.5f);
        diamondIconRect.anchorMax = new Vector2(0, 0.5f);
        diamondIconRect.pivot = new Vector2(0.5f, 0.5f);
        diamondIconRect.anchoredPosition = new Vector2(0, 0);
        diamondIconRect.sizeDelta = new Vector2(50, 50);
        diamondIcon.GetComponent<Image>().color = new Color(0.2f, 0.9f, 1f, 1f); // Cyan diamond

        CreateTextElement("DiamondText", diamondFrame.transform, "1,000");

        GameObject diamondAddBtn = CreateUIElement("DiamondAddButton", diamondFrame.transform, uiSprite);
        RectTransform diamondAddRect = diamondAddBtn.GetComponent<RectTransform>();
        diamondAddRect.anchorMin = new Vector2(1, 0.5f);
        diamondAddRect.anchorMax = new Vector2(1, 0.5f);
        diamondAddRect.pivot = new Vector2(0.5f, 0.5f);
        diamondAddRect.anchoredPosition = new Vector2(0, 0);
        diamondAddRect.sizeDelta = new Vector2(30, 30);
        diamondAddBtn.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green add button
        
        GameObject diamondPlus = CreateTextElement("PlusText", diamondAddBtn.transform, "+");
        diamondPlus.GetComponent<TextMeshProUGUI>().color = Color.white;

        // Gold Frame
        GameObject goldFrame = CreateUIElement("GoldFrame", topRightPanel.transform, backgroundSprite);
        RectTransform goldRect = goldFrame.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(1, 0.5f);
        goldRect.anchorMax = new Vector2(1, 0.5f);
        goldRect.pivot = new Vector2(1, 0.5f);
        goldRect.anchoredPosition = new Vector2(-260, 0);
        goldRect.sizeDelta = new Vector2(160, 40);

        GameObject goldIcon = CreateUIElement("GoldIcon", goldFrame.transform, uiSprite);
        RectTransform goldIconRect = goldIcon.GetComponent<RectTransform>();
        goldIconRect.anchorMin = new Vector2(0, 0.5f);
        goldIconRect.anchorMax = new Vector2(0, 0.5f);
        goldIconRect.pivot = new Vector2(0.5f, 0.5f);
        goldIconRect.anchoredPosition = new Vector2(0, 0);
        goldIconRect.sizeDelta = new Vector2(50, 50);
        goldIcon.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f, 1f); // Gold coin

        CreateTextElement("GoldText", goldFrame.transform, "50,000");

        // Select the canvas
        Selection.activeGameObject = canvasObj;
        Debug.Log("HUD Generated successfully!");
    }

    private static GameObject CreateUIElement(string name, Transform parent, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        return obj;
    }

    private static GameObject CreateTextElement(string name, Transform parent, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14;
        tmp.fontSizeMax = 24;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // By default fill the parent but add some padding
        rect.offsetMin = new Vector2(10, 2);
        rect.offsetMax = new Vector2(-10, -2);

        return obj;
    }
}
