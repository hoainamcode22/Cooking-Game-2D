using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FarmGame.UI;

public class FixHUDLayoutWindow : EditorWindow
{
    private RectTransform oldAvatarGroup;
    private RectTransform oldGoldGroup;
    private RectTransform oldDiamondGroup;

    private TextMeshProUGUI textLevel;
    private TextMeshProUGUI textEXP;
    private Image expFill;
    private TextMeshProUGUI textGold;
    private TextMeshProUGUI textDiamond;

    [MenuItem("Tools/Farm Game/Fix Old HUD Layout")]
    public static void ShowWindow()
    {
        GetWindow<FixHUDLayoutWindow>("Fix Old HUD Layout");
    }

    private void OnGUI()
    {
        GUILayout.Label("Công cụ Nối Code Khung Gỗ cũ (Giữ nguyên vị trí!)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Kéo thả các UI cũ (khung gỗ) của bạn vào đây. Tool sẽ KHÔNG làm dịch chuyển vị trí của sếp nữa, mà CHỈ TỰ ĐỘNG NỐI CODE để nó chạy mượt.", MessageType.Info);

        GUILayout.Space(10);
        GUILayout.Label("1. Cụm UI cũ của sếp", EditorStyles.boldLabel);
        oldAvatarGroup = (RectTransform)EditorGUILayout.ObjectField("Khung Avatar/EXP cũ", oldAvatarGroup, typeof(RectTransform), true);
        oldGoldGroup = (RectTransform)EditorGUILayout.ObjectField("Khung Vàng cũ", oldGoldGroup, typeof(RectTransform), true);
        oldDiamondGroup = (RectTransform)EditorGUILayout.ObjectField("Khung Kim Cương cũ", oldDiamondGroup, typeof(RectTransform), true);

        GUILayout.Space(10);
        GUILayout.Label("2. Thành phần bên trong (để nối code)", EditorStyles.boldLabel);
        textLevel = (TextMeshProUGUI)EditorGUILayout.ObjectField("Text Level (số 32)", textLevel, typeof(TextMeshProUGUI), true);
        textEXP = (TextMeshProUGUI)EditorGUILayout.ObjectField("Text EXP (4680/6200)", textEXP, typeof(TextMeshProUGUI), true);
        expFill = (Image)EditorGUILayout.ObjectField("Thanh EXP (màu xanh)", expFill, typeof(Image), true);
        textGold = (TextMeshProUGUI)EditorGUILayout.ObjectField("Text Vàng (24556)", textGold, typeof(TextMeshProUGUI), true);
        textDiamond = (TextMeshProUGUI)EditorGUILayout.ObjectField("Text Kim Cương (613)", textDiamond, typeof(TextMeshProUGUI), true);

        GUILayout.Space(20);
        if (GUILayout.Button("Chỉ Nối Code (Giữ nguyên vị trí UI)!", GUILayout.Height(40)))
        {
            FixLayout();
        }
    }

    private void FixLayout()
    {
        if (oldAvatarGroup == null || oldGoldGroup == null || oldDiamondGroup == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Sếp chưa kéo đủ 3 cụm UI chính (Avatar, Vàng, Kim Cương) vào ô trống!", "OK");
            return;
        }

        // Attach and wire up logic
        Canvas canvas = oldAvatarGroup.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            HUDController controller = canvas.GetComponent<HUDController>();
            if (controller == null) controller = canvas.gameObject.AddComponent<HUDController>();

            controller.textLevel = textLevel;
            controller.textEXP = textEXP;
            controller.expFill = expFill;
            controller.textGold = textGold;
            controller.textDiamond = textDiamond;
            
            controller.expContainer = oldAvatarGroup;
            controller.goldContainer = oldGoldGroup;
            controller.diamondContainer = oldDiamondGroup;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(canvas.gameObject);
        }

        Debug.Log("Đã nối code thành công! Vị trí UI vẫn được giữ nguyên!");
    }
}
