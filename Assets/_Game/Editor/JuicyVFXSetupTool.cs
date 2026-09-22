using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class JuicyVFXSetupTool
{
    [MenuItem("Tools/Farm Game/VFX & Juice/1-Click Setup All Juicy Button Feedback")]
    public static void SetupAllButtons()
    {
        var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var feedback = btn.GetComponent<JuicyButtonFeedback>();
            if (feedback == null)
            {
                btn.gameObject.AddComponent<JuicyButtonFeedback>();
                EditorUtility.SetDirty(btn.gameObject);
                added++;
            }
        }

        Debug.Log($"<color=green>[JuicyVFX] Successfully added JuicyButtonFeedback to {added} buttons in scene (Total: {buttons.Length})!</color>");
    }

    [MenuItem("Tools/Farm Game/VFX & Juice/Test Highlight & Glow on Storage HUD")]
    public static void TestGlowOnStorageHUD()
    {
        var toastGo = GameObject.Find("WarehouseGainToast");
        if (toastGo != null)
        {
            var panel = toastGo.transform.Find("Panel_WarehouseToast") as RectTransform;
            if (panel != null)
            {
                UIGlowPulseFX.Attach(panel, new Color(1f, 0.85f, 0.2f, 0.9f), 2.5f);
                Debug.Log("<color=green>[JuicyVFX] Attached glowing halo to Storage HUD panel!</color>");
            }
        }
    }
}
