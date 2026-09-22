using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CozyStorageHUDSetupTool
{
    private const string SpriteFolder = "Assets/_Game/Farm/Sprites/StorageHUD";

    [MenuItem("Tools/Farm Game/Setup Cozy Storage HUD")]
    public static void SetupCozyStorageHUD()
    {
        // 1. Process assets with FloodFill alpha masking
        StorageHUDExtractorTool.ProcessAssets();

        // 2. Find Canvas in current active scene
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CozyStorageHUD] No Canvas found in the current active scene!");
            return;
        }

        // 3. Find or create WarehouseGainToast root
        var toastGo = GameObject.Find("WarehouseGainToast");
        if (toastGo == null)
        {
            toastGo = new GameObject("WarehouseGainToast", typeof(RectTransform));
            toastGo.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)toastGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var toastUI = toastGo.GetComponent<WarehouseGainToastUI>();
        if (toastUI == null) toastUI = toastGo.AddComponent<WarehouseGainToastUI>();

        // Load Sprites
        var mainFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/storage_main_frame.png");
        var barnSprite      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/storage_barn_house.png");
        var headerSprite    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/storage_header_badge.png");
        var crateSprite     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/storage_crate_icon.png");
        var trackSprite     = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/progress_track.png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_track_bar.png");
        var fillSprite      = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/progress_fill.png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_fill_green.png");

        // 4. Pixel-Perfect Hierarchy matching Image 2
        // Panel Root (Main Wooden Plaque)
        var panelTr = toastGo.transform.Find("Panel_WarehouseToast") as RectTransform;
        if (panelTr == null)
        {
            var go = new GameObject("Panel_WarehouseToast", typeof(RectTransform));
            go.transform.SetParent(toastGo.transform, false);
            panelTr = (RectTransform)go.transform;
        }

        panelTr.anchorMin = panelTr.anchorMax = new Vector2(0.5f, 1f);
        panelTr.pivot = new Vector2(0.5f, 1f);
        panelTr.anchoredPosition = new Vector2(120f, -50f);
        panelTr.sizeDelta = new Vector2(400f, 120f);

        var imgPanel = panelTr.GetComponent<Image>();
        if (imgPanel == null) imgPanel = panelTr.gameObject.AddComponent<Image>();
        imgPanel.sprite = mainFrameSprite;
        imgPanel.type = Image.Type.Simple;
        imgPanel.preserveAspect = true;
        imgPanel.color = Color.white;
        imgPanel.raycastTarget = false;

        var cg = panelTr.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelTr.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // A. Barn House (3D Red Barn House Diorama on the left)
        var barnTr = FindOrCreate(panelTr, "Img_BarnHouse");
        var imgBarn = barnTr.GetComponent<Image>();
        if (imgBarn == null) imgBarn = barnTr.gameObject.AddComponent<Image>();
        imgBarn.sprite = barnSprite;
        imgBarn.preserveAspect = true;
        imgBarn.color = Color.white;
        imgBarn.raycastTarget = false;
        var bRt = (RectTransform)barnTr;
        bRt.anchorMin = bRt.anchorMax = new Vector2(0f, 0.5f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(-15f, 6f);
        bRt.sizeDelta = new Vector2(145f, 145f);

        // B. Header Badge ("Storage" Wooden Badge)
        var headerTr = FindOrCreate(panelTr, "Badge_Header");
        var imgHeader = headerTr.GetComponent<Image>();
        if (imgHeader == null) imgHeader = headerTr.gameObject.AddComponent<Image>();
        imgHeader.sprite = headerSprite;
        imgHeader.preserveAspect = true;
        imgHeader.color = Color.white;
        imgHeader.raycastTarget = false;
        var hRt = (RectTransform)headerTr;
        hRt.anchorMin = hRt.anchorMax = new Vector2(0f, 1f);
        hRt.pivot = new Vector2(0f, 1f);
        hRt.anchoredPosition = new Vector2(135f, -16f);
        hRt.sizeDelta = new Vector2(175f, 40f);

        // Header Text: "Storage"
        var txtHeaderTr = FindOrCreate(headerTr as RectTransform, "Txt_Title");
        var txtHeader = txtHeaderTr.GetComponent<TextMeshProUGUI>();
        if (txtHeader == null) txtHeader = txtHeaderTr.gameObject.AddComponent<TextMeshProUGUI>();
        txtHeader.text = "Storage";
        txtHeader.fontSize = 20;
        txtHeader.fontStyle = FontStyles.Bold;
        txtHeader.alignment = TextAlignmentOptions.Center;
        txtHeader.color = Color.white;
        txtHeader.outlineColor = new Color32(0x38, 0x1F, 0x0C, 0xFF);
        txtHeader.outlineWidth = 0.22f;
        txtHeader.raycastTarget = false;
        var thRt = (RectTransform)txtHeaderTr;
        thRt.anchorMin = Vector2.zero;
        thRt.anchorMax = Vector2.one;
        thRt.offsetMin = new Vector2(36f, 0f);
        thRt.offsetMax = new Vector2(-8f, 0f);

        // C. Progress Container (Capsule Track, Fill, Crate Icon Badge, Count Text)
        var progressContainerTr = FindOrCreate(panelTr, "Progress_Container");
        var pcRt = (RectTransform)progressContainerTr;
        pcRt.anchorMin = pcRt.anchorMax = new Vector2(0f, 1f);
        pcRt.pivot = new Vector2(0f, 1f);
        pcRt.anchoredPosition = new Vector2(135f, -64f);
        pcRt.sizeDelta = new Vector2(240f, 36f);

        // Track
        var trackTr = FindOrCreate(pcRt, "Bar_Track");
        var imgTrack = trackTr.GetComponent<Image>();
        if (imgTrack == null) imgTrack = trackTr.gameObject.AddComponent<Image>();
        imgTrack.sprite = trackSprite;
        imgTrack.type = Image.Type.Sliced;
        imgTrack.color = new Color32(0x38, 0x1F, 0x0C, 0xFF);
        imgTrack.raycastTarget = false;
        var tRt = (RectTransform)trackTr;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(16f, 2f);
        tRt.offsetMax = new Vector2(-4f, -2f);

        // Fill
        var fillTr = FindOrCreate(tRt, "Bar_Fill");
        var imgFill = fillTr.GetComponent<Image>();
        if (imgFill == null) imgFill = fillTr.gameObject.AddComponent<Image>();
        imgFill.sprite = fillSprite;
        imgFill.type = Image.Type.Filled;
        imgFill.fillMethod = Image.FillMethod.Horizontal;
        imgFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        imgFill.color = new Color32(0x4C, 0xCE, 0x15, 0xFF);
        imgFill.raycastTarget = false;
        var fRt = (RectTransform)fillTr;
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(3f, 3f);
        fRt.offsetMax = new Vector2(-3f, -3f);

        // Circular Crate Badge (Left of the progress bar)
        var crateTr = FindOrCreate(pcRt, "Img_CrateBadge");
        var imgCrate = crateTr.GetComponent<Image>();
        if (imgCrate == null) imgCrate = crateTr.gameObject.AddComponent<Image>();
        imgCrate.sprite = crateSprite;
        imgCrate.preserveAspect = true;
        imgCrate.color = Color.white;
        imgCrate.raycastTarget = false;
        var cRt = (RectTransform)crateTr;
        cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(16f, 0f);
        cRt.sizeDelta = new Vector2(46f, 46f);

        // Count Text: "18/25"
        var txtCountTr = FindOrCreate(tRt, "Txt_Count");
        var txtCount = txtCountTr.GetComponent<TextMeshProUGUI>();
        if (txtCount == null) txtCount = txtCountTr.gameObject.AddComponent<TextMeshProUGUI>();
        txtCount.text = "18/25";
        txtCount.fontSize = 19;
        txtCount.fontStyle = FontStyles.Bold;
        txtCount.alignment = TextAlignmentOptions.Center;
        txtCount.color = Color.white;
        txtCount.outlineColor = new Color32(0x1A, 0x49, 0x06, 0xFF);
        txtCount.outlineWidth = 0.22f;
        txtCount.raycastTarget = false;
        var tcRt = (RectTransform)txtCountTr;
        tcRt.anchorMin = Vector2.zero;
        tcRt.anchorMax = Vector2.one;
        tcRt.offsetMin = new Vector2(14f, 0f);
        tcRt.offsetMax = Vector2.zero;

        // Apply references to script serialized fields
        SerializedObject so = new SerializedObject(toastUI);
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("panelSprite").objectReferenceValue = mainFrameSprite;
        so.FindProperty("iconSprite").objectReferenceValue = barnSprite;
        so.FindProperty("barTrackSprite").objectReferenceValue = trackSprite;
        so.FindProperty("barFillSprite").objectReferenceValue = fillSprite;
        so.FindProperty("headerBadgeSprite").objectReferenceValue = headerSprite;
        so.FindProperty("crateBadgeSprite").objectReferenceValue = crateSprite;
        so.FindProperty("anchoredPos").vector2Value = new Vector2(120f, -50f);
        so.FindProperty("panelSize").vector2Value = new Vector2(400f, 120f);
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(toastGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("<color=green>[CozyStorageHUD] Cozy Storage HUD successfully built and wired in scene with pixel-perfect coordinates!</color>");
    }

    private static Transform FindOrCreate(RectTransform parent, string name)
    {
        var tr = parent.Find(name);
        if (tr == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            tr = go.transform;
        }
        return tr;
    }
}
