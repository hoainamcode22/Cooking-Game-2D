using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class CreateEXPBarUI : EditorWindow
{
    [MenuItem("Tools/Farm Game/Tạo Thanh EXP Đẹp (Bo góc)")]
    public static void CreateBar()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Chưa có Canvas trong Scene!");
            return;
        }

        // Lấy Sprite bo góc mặc định của Unity
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // 1. Viền ngoài (Bo góc, nền đậm)
        GameObject bgObj = new GameObject("Thanh_EXP_Moi");
        bgObj.transform.SetParent(canvas.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(250, 36);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = defaultSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.2f, 0.1f, 0.05f, 1f); // Nâu đậm làm viền

        // 2. Lớp nền bên trong (để lúc chưa có EXP thì có màu tối)
        GameObject innerBgObj = new GameObject("Nen_EXP");
        innerBgObj.transform.SetParent(bgObj.transform, false);
        RectTransform innerBgRect = innerBgObj.AddComponent<RectTransform>();
        innerBgRect.anchorMin = new Vector2(0, 0);
        innerBgRect.anchorMax = new Vector2(1, 1);
        innerBgRect.offsetMin = new Vector2(3, 3); // Cách viền 3px
        innerBgRect.offsetMax = new Vector2(-3, -3);
        Image innerBgImg = innerBgObj.AddComponent<Image>();
        innerBgImg.sprite = defaultSprite;
        innerBgImg.type = Image.Type.Sliced;
        innerBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 3. Lớp Fill (Màu xanh chảy)
        GameObject fillObj = new GameObject("Fill_Xanh");
        fillObj.transform.SetParent(innerBgObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = new Vector2(0, 0); 
        fillRect.offsetMax = new Vector2(0, 0);
        
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = defaultSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.65f; // Chạy thử 65%
        fillImg.color = new Color(0.1f, 0.7f, 1f, 1f); // Xanh dương tươi

        // 4. Đường line thẳng (Hiệu ứng bóng bóng đẹp)
        GameObject glossObj = new GameObject("Duong_Line_Bong");
        glossObj.transform.SetParent(fillObj.transform, false);
        RectTransform glossRect = glossObj.AddComponent<RectTransform>();
        glossRect.anchorMin = new Vector2(0, 0.5f); // Nửa trên
        glossRect.anchorMax = new Vector2(1, 1);
        glossRect.offsetMin = new Vector2(2, 2);
        glossRect.offsetMax = new Vector2(-2, -2);
        Image glossImg = glossObj.AddComponent<Image>();
        glossImg.sprite = defaultSprite;
        glossImg.type = Image.Type.Sliced;
        glossImg.color = new Color(1f, 1f, 1f, 0.3f); // Trắng trong suốt tạo bóng

        Selection.activeGameObject = bgObj;
        Debug.Log("Đã tạo xong Thanh EXP xịn xò!");
    }
}
