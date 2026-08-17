using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FarmGame.UI;

public class GeneratePerfectHUD : EditorWindow
{
    [MenuItem("Tools/Farm Game/Tạo Bố Cục UI Y Hệt Ảnh Mẫu")]
    public static void CreatePerfectHUD()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas_HUD_Moi");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // --- CỤM BÊN TRÁI (AVATAR & EXP) ---
        GameObject leftAnchor = new GameObject("TopLeft_Anchor");
        leftAnchor.transform.SetParent(canvas.transform, false);
        RectTransform leftRect = leftAnchor.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 1);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 1);
        leftRect.anchoredPosition = new Vector2(20, -20);
        
        // Nền EXP (Viên thuốc nâu sẫm)
        GameObject expBg = CreateSlicedImage("Nen_EXP", leftAnchor.transform, roundedSprite, new Vector2(100, -50), new Vector2(250, 44), new Color(0.2f, 0.1f, 0.05f, 0.9f));
        expBg.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        
        // Thanh chảy EXP (Xanh dương)
        GameObject expFill = CreateSlicedImage("Fill_EXP", expBg.transform, roundedSprite, new Vector2(0, 0), new Vector2(244, 38), new Color(0.1f, 0.6f, 1f, 1f));
        expFill.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        expFill.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        expFill.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        Image fillImg = expFill.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.75f;
        
        // Text EXP
        TextMeshProUGUI txtExp = CreateText("Txt_EXP", expBg.transform, "4680/6200", 22, Color.white);

        // Khung Avatar (Vuông bo góc)
        GameObject avatarFrame = CreateSlicedImage("Avatar_Frame", leftAnchor.transform, roundedSprite, new Vector2(70, -50), new Vector2(100, 100), new Color(0.9f, 0.8f, 0.6f, 1f));
        CreateSlicedImage("Avatar_Mask", avatarFrame.transform, roundedSprite, new Vector2(0, 0), new Vector2(92, 92), Color.gray);

        // Sao Level (Tròn/Ngôi sao xanh) đè lên góc Avatar và EXP
        GameObject levelStar = CreateSlicedImage("Level_Star", leftAnchor.transform, knobSprite, new Vector2(110, -30), new Vector2(50, 50), new Color(0.1f, 0.6f, 1f, 1f));
        TextMeshProUGUI txtLevel = CreateText("Txt_Level", levelStar.transform, "32", 24, Color.white);


        // --- CỤM BÊN PHẢI (VÀNG & KIM CƯƠNG) ---
        GameObject rightAnchor = new GameObject("TopRight_Anchor");
        rightAnchor.transform.SetParent(canvas.transform, false);
        RectTransform rightRect = rightAnchor.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 1);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 1);
        rightRect.anchoredPosition = new Vector2(-20, -30);

        // Nền Kim Cương (Nâu sẫm)
        GameObject gemBg = CreateSlicedImage("Nen_KimCuong", rightAnchor.transform, roundedSprite, new Vector2(-150, -20), new Vector2(130, 44), new Color(0.2f, 0.1f, 0.05f, 0.9f));
        TextMeshProUGUI txtGem = CreateText("Txt_Gem", gemBg.transform, "320", 24, Color.white);
        txtGem.rectTransform.anchoredPosition = new Vector2(15, 0);
        // Icon Kim Cương
        CreateSlicedImage("Icon_Gem", gemBg.transform, knobSprite, new Vector2(-65, 0), new Vector2(50, 50), new Color(0.2f, 0.8f, 1f, 1f));

        // Nền Vàng (Nâu sẫm)
        GameObject goldBg = CreateSlicedImage("Nen_Vang", rightAnchor.transform, roundedSprite, new Vector2(-320, -20), new Vector2(150, 44), new Color(0.2f, 0.1f, 0.05f, 0.9f));
        TextMeshProUGUI txtGold = CreateText("Txt_Gold", goldBg.transform, "12 450", 24, Color.white);
        txtGold.rectTransform.anchoredPosition = new Vector2(15, 0);
        // Icon Vàng
        CreateSlicedImage("Icon_Gold", goldBg.transform, knobSprite, new Vector2(-75, 0), new Vector2(50, 50), new Color(1f, 0.8f, 0f, 1f));

        // Nút Cài đặt
        CreateSlicedImage("Btn_Settings", rightAnchor.transform, roundedSprite, new Vector2(-40, -20), new Vector2(50, 50), new Color(0.8f, 0.8f, 0.8f, 1f));


        // --- NỐI CODE ---
        HUDController controller = canvas.GetComponent<HUDController>();
        if (controller == null) controller = canvas.gameObject.AddComponent<HUDController>();

        controller.textLevel = txtLevel;
        controller.textEXP = txtExp;
        controller.expFill = fillImg;
        controller.textGold = txtGold;
        controller.textDiamond = txtGem;
        
        controller.expContainer = expBg.GetComponent<RectTransform>();
        controller.goldContainer = goldBg.GetComponent<RectTransform>();
        controller.diamondContainer = gemBg.GetComponent<RectTransform>();

        EditorUtility.SetDirty(controller);
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Dựng xong Layout UI chuẩn 100% y hệt ảnh mẫu (có bo góc) và đã tự nối code!");
    }

    private static GameObject CreateSlicedImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return obj;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string textStr, int fontSize, Color color)
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
        
        return tmp;
    }
}
