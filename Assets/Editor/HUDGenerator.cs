using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HUDGenerator : EditorWindow
{
    [MenuItem("Tools/Farm Game/Generate HUD")]
    public static void GenerateHUD()
    {
        // Find existing canvas or create one
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        
        if (canvas != null)
        {
            canvasObj = canvas.gameObject;
        }
        else
        {
            canvasObj = new GameObject("Canvas_HUD");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Event System
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 3. Top Left Anchor (Avatar & EXP)
        GameObject topLeft = new GameObject("TopLeft_Anchor");
        topLeft.transform.SetParent(canvasObj.transform, false);
        RectTransform tlRect = topLeft.AddComponent<RectTransform>();
        tlRect.anchorMin = new Vector2(0, 1);
        tlRect.anchorMax = new Vector2(0, 1);
        tlRect.pivot = new Vector2(0, 1);
        tlRect.anchoredPosition = new Vector2(50, -50);
        tlRect.sizeDelta = new Vector2(500, 150);
        tlRect.SetAsLastSibling(); // Ensure it renders on top

        // Avatar Frame
        GameObject avatarObj = CreateImageObject("Avatar_Frame", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 120), new Vector2(0, 0));
        // EXP Background Pill
        GameObject expBg = CreateImageObject("EXP_Background", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 40), new Vector2(100, 20));
        expBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f); // Dark brown
        // EXP Fill
        GameObject expFill = CreateImageObject("EXP_Fill", expBg.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(0, 0));
        expFill.GetComponent<Image>().color = new Color(0.1f, 0.6f, 1f, 1f); // Blue
        // Level Star
        GameObject levelStar = CreateImageObject("Level_Star", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80, 80), new Vector2(100, 40));
        levelStar.GetComponent<Image>().color = new Color(0.1f, 0.6f, 1f, 1f);
        GameObject levelText = CreateTextObject("Text_Level", levelStar.transform, "32", 36, Color.white);
        
        // 4. Top Right Anchor (Gold, Diamonds, Settings)
        GameObject topRight = new GameObject("TopRight_Anchor");
        topRight.transform.SetParent(canvasObj.transform, false);
        RectTransform trRect = topRight.AddComponent<RectTransform>();
        trRect.anchorMin = new Vector2(1, 1);
        trRect.anchorMax = new Vector2(1, 1);
        trRect.pivot = new Vector2(1, 1);
        trRect.anchoredPosition = new Vector2(-50, -50);
        trRect.sizeDelta = new Vector2(600, 100);
        trRect.SetAsLastSibling(); // Ensure it renders on top

        // Settings Icon
        GameObject settingsIcon = CreateImageObject("Settings_Icon", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(80, 80), new Vector2(0, 0));
        // Diamond Pill
        GameObject diamondBg = CreateImageObject("Diamond_Background", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(200, 60), new Vector2(-100, 0));
        diamondBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f);
        CreateImageObject("Diamond_Icon", diamondBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70, 70), new Vector2(0, 0));
        CreateTextObject("Text_Diamond", diamondBg.transform, "320", 30, Color.white);
        
        // Gold Pill
        GameObject goldBg = CreateImageObject("Gold_Background", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(250, 60), new Vector2(-320, 0));
        goldBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f);
        CreateImageObject("Gold_Icon", goldBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70, 70), new Vector2(0, 0));
        CreateTextObject("Text_Gold", goldBg.transform, "12 450", 30, Color.white);

        Selection.activeGameObject = canvasObj;
        Debug.Log("HUD Generated successfully inside existing Canvas!");
    }

    private static GameObject CreateImageObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        obj.AddComponent<Image>(); 
        return obj;
    }

    private static GameObject CreateTextObject(string name, Transform parent, string textStr, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = textStr;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        
        return obj;
    }
}
