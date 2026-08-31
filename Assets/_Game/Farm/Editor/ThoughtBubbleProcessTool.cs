using System.IO;
using UnityEditor;
using UnityEngine;

public static class ThoughtBubbleProcessTool
{
    private const string ArtifactPath = @"C:\Users\acer\.gemini\antigravity\brain\2e949d67-4089-49e5-a8a9-990651b42430\cloud_thought_bubble_pack_1788190606998.jpg";
    private const string OutDir = "Assets/Export_Train_UI_Package/Sprites";

    [MenuItem("Tools/Farm Game/Process Thought Bubble Sprites", false, 31)]
    public static void ProcessThoughtSprites()
    {
        if (!File.Exists(ArtifactPath))
        {
            Debug.LogWarning("[ThoughtBubble] Không tìm thấy file artifact: " + ArtifactPath);
            return;
        }

        byte[] bytes = File.ReadAllBytes(ArtifactPath);
        var srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!srcTex.LoadImage(bytes)) return;

        int w = srcTex.width;
        int h = srcTex.height;
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = srcTex.GetPixels();

        // Key out pure/near white background
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float whiteness = (c.r + c.g + c.b) / 3f;
            // Nếu gần trắng và không có độ bão hòa (vùng nền) -> trong suốt
            if (whiteness > 0.94f && Mathf.Abs(c.r - c.g) < 0.04f && Mathf.Abs(c.g - c.b) < 0.04f)
            {
                c.a = 0f;
            }
            else if (whiteness > 0.88f && Mathf.Abs(c.r - c.g) < 0.05f && Mathf.Abs(c.g - c.b) < 0.05f)
            {
                c.a = Mathf.Clamp01((0.94f - whiteness) / 0.06f);
            }
            pixels[i] = c;
        }

        outTex.SetPixels(pixels);
        outTex.Apply();

        if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

        string fullOutPath = $"{OutDir}/spr_thought_cloud_full.png";
        File.WriteAllBytes(fullOutPath, outTex.EncodeToPNG());

        Object.DestroyImmediate(srcTex);
        Object.DestroyImmediate(outTex);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(fullOutPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Debug.Log("[ThoughtBubble] Đã lưu sprite đám mây suy nghĩ tại: " + fullOutPath);
    }
}
