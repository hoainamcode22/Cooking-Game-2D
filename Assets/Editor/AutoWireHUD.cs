using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FarmGame.UI;

public class AutoWireHUD : EditorWindow
{
    [MenuItem("Tools/Farm Game/Auto-Wire Existing HUD")]
    public static void WireHUD()
    {
        // 1. Find Canvas_HUD
        GameObject canvasHUD = GameObject.Find("Canvas_HUD");
        if (canvasHUD == null)
        {
            Debug.LogError("Could not find an object named 'Canvas_HUD' in the scene.");
            return;
        }

        // 2. Add or get HUDController
        HUDController controller = canvasHUD.GetComponent<HUDController>();
        if (controller == null)
        {
            controller = canvasHUD.AddComponent<HUDController>();
        }

        // 3. Search children for text and images
        TextMeshProUGUI[] texts = canvasHUD.GetComponentsInChildren<TextMeshProUGUI>(true);
        Image[] images = canvasHUD.GetComponentsInChildren<Image>(true);
        RectTransform[] rects = canvasHUD.GetComponentsInChildren<RectTransform>(true);

        // Map Level Text (usually has "Level" in name, or just guess)
        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLower();
            if (n.Contains("level") || n.Contains("lvl")) controller.textLevel = t;
            else if (n.Contains("exp") || n.Contains("kinh")) controller.textEXP = t;
            else if (n.Contains("gold") || n.Contains("vang") || n.Contains("coin")) controller.textGold = t;
            else if (n.Contains("diamond") || n.Contains("kimcuong") || n.Contains("gem")) controller.textDiamond = t;
        }

        // Map EXP Fill Image (usually has "fill" in name)
        foreach (var i in images)
        {
            string n = i.gameObject.name.ToLower();
            if ((n.Contains("exp") || n.Contains("kinh")) && (n.Contains("fill") || n.Contains("bar") || i.type == Image.Type.Filled))
            {
                controller.expFill = i;
            }
        }

        // Map Containers for Shake Effect
        foreach (var r in rects)
        {
            string n = r.gameObject.name.ToLower();
            if (n.Contains("gold") && (n.Contains("bg") || n.Contains("container") || n.Contains("khung"))) controller.goldContainer = r;
            else if (n.Contains("diamond") && (n.Contains("bg") || n.Contains("container") || n.Contains("khung"))) controller.diamondContainer = r;
            else if (n.Contains("exp") && (n.Contains("bg") || n.Contains("container") || n.Contains("khung"))) controller.expContainer = r;
        }

        EditorUtility.SetDirty(controller);
        Debug.Log("Successfully Auto-Wired HUDController to your existing Canvas_HUD!");
    }
}
