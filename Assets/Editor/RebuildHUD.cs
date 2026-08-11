using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FarmGame.UI;

public class RebuildHUD : EditorWindow
{
    [MenuItem("Tools/Farm Game/Rebuild HUD Layout")]
    public static void RebuildLayout()
    {
        // 1. Find existing Canvas_HUD
        GameObject canvasHUD = GameObject.Find("Canvas_HUD");
        if (canvasHUD == null)
        {
            Debug.LogError("Không tìm thấy 'Canvas_HUD' trong Scene. Hãy chắc chắn bạn đang mở đúng Scene!");
            return;
        }

        // 2. Add or get HUDController
        HUDController controller = canvasHUD.GetComponent<HUDController>();
        if (controller == null)
        {
            controller = canvasHUD.AddComponent<HUDController>();
        }

        // 3. Build Top Left Anchor (Avatar & EXP)
        GameObject topLeft = new GameObject("TopLeft_Anchor");
        topLeft.transform.SetParent(canvasHUD.transform, false);
        RectTransform tlRect = topLeft.AddComponent<RectTransform>();
        tlRect.anchorMin = new Vector2(0, 1);
        tlRect.anchorMax = new Vector2(0, 1);
        tlRect.pivot = new Vector2(0, 1);
        tlRect.anchoredPosition = new Vector2(50, -50);
        tlRect.sizeDelta = new Vector2(500, 150);

        // Avatar Frame
        GameObject avatarObj = CreateImageObject("Avatar_Frame", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 120), new Vector2(0, 0));
        // EXP Background Pill
        GameObject expBg = CreateImageObject("EXP_Background", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 40), new Vector2(100, 20));
        expBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f);
        controller.expContainer = expBg.GetComponent<RectTransform>();
        // EXP Fill
        GameObject expFill = CreateImageObject("EXP_Fill", expBg.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(0, 0));
        expFill.GetComponent<Image>().color = new Color(0.1f, 0.6f, 1f, 1f);
        controller.expFill = expFill.GetComponent<Image>();
        // Level Star
        GameObject levelStar = CreateImageObject("Level_Star", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80, 80), new Vector2(100, 40));
        levelStar.GetComponent<Image>().color = new Color(0.1f, 0.6f, 1f, 1f);
        GameObject levelText = CreateTextObject("Text_Level", levelStar.transform, "32", 36, Color.white);
        controller.textLevel = levelText.GetComponent<TextMeshProUGUI>();
        controller.textEXP = CreateTextObject("Text_EXP", expBg.transform, "4680/6200", 24, Color.white).GetComponent<TextMeshProUGUI>();
        
        // 4. Build Top Right Anchor (Gold, Diamonds, Settings)
        GameObject topRight = new GameObject("TopRight_Anchor");
        topRight.transform.SetParent(canvasHUD.transform, false);
        RectTransform trRect = topRight.AddComponent<RectTransform>();
        trRect.anchorMin = new Vector2(1, 1);
        trRect.anchorMax = new Vector2(1, 1);
        trRect.pivot = new Vector2(1, 1);
        trRect.anchoredPosition = new Vector2(-50, -50);
        trRect.sizeDelta = new Vector2(600, 100);

        // Settings Icon
        GameObject settingsIcon = CreateImageObject("Settings_Icon", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(80, 80), new Vector2(0, 0));
        
        // Diamond Pill
        GameObject diamondBg = CreateImageObject("Diamond_Background", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(200, 60), new Vector2(-100, 0));
        diamondBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f);
        controller.diamondContainer = diamondBg.GetComponent<RectTransform>();
        CreateImageObject("Diamond_Icon", diamondBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70, 70), new Vector2(0, 0));
        controller.textDiamond = CreateTextObject("Text_Diamond", diamondBg.transform, "320", 30, Color.white).GetComponent<TextMeshProUGUI>();
        
        // Gold Pill
        GameObject goldBg = CreateImageObject("Gold_Background", topRight.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(250, 60), new Vector2(-320, 0));
        goldBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.8f);
        controller.goldContainer = goldBg.GetComponent<RectTransform>();
        CreateImageObject("Gold_Icon", goldBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70, 70), new Vector2(0, 0));
        controller.textGold = CreateTextObject("Text_Gold", goldBg.transform, "12 450", 30, Color.white).GetComponent<TextMeshProUGUI>();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvasHUD);

        Selection.activeGameObject = topLeft; // Select it so user sees it
        Debug.Log("Dựng xong bộ khung UI mới ngay bên trong Canvas_HUD cũ!");
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
        tmp.enableWordWrapping = false;
        
        return obj;
    }
}
