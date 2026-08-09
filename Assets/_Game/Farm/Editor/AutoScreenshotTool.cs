using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AutoScreenshotTool : EditorWindow
{
    [MenuItem("Tools/Farm/Capture Screenshot & Fixes")]
    public static void RunFixAndCapture()
    {
        // 1. Gắn RainSplashManager vào WeatherSetup
        string[] guids = AssetDatabase.FindAssets("DayNightWeatherSetup t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponentInChildren<RainSplashManager>() == null)
            {
                prefab.AddComponent<RainSplashManager>();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }
        
        // 2. Chup man hinh (Play mode neu dang chay, khong thi chup Scene)
        string screenshotPath = @"C:\Users\acer\.gemini\antigravity\brain\26f6f275-94b1-4a49-83f2-1b6e04b9186d\screenshot_farm.png";
        ScreenCapture.CaptureScreenshot(screenshotPath);
        Debug.Log("Da luu anh chup tai: " + screenshotPath);
    }
}
