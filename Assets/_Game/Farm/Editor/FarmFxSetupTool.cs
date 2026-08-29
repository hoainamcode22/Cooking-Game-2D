using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup 1-click cho toàn bộ cụm FX "nhận tài nguyên & bay vật phẩm":
///  - HarvestFeedbackSpawner: bay nông sản về WarehouseGainToast trên HUD, bay EXP về EXP_Bar_Container.
///  - CoinFlyFX: vàng bay về Gold_Container + nảy mẩy mẩy.
///  - GemFlyFX: kim cương bay về Diamond_Container + nảy mẩy mẩy.
///  - WarehouseGainToastUI: thanh kho [icon | fill bar | 25/30] nhảy số tăng dần từng nấc.
/// Chạy: Tools → Farm Game → FX → Setup HUD Gain FX. Sau khi chạy NHỚ SAVE SCENE.
/// </summary>
public static class FarmFxSetupTool
{
    private const string TrainSprites = "Assets/Export_Train_UI_Package/Sprites";

    [MenuItem("Tools/Farm Game/FX/Setup HUD Gain FX (gem bay + thanh kho)")]
    public static void SetupHudGainFx()
    {
        // ── 1. Tìm Canvas HUD ──
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[FarmFX] Không tìm thấy Canvas trong scene!");
            return;
        }

        // ── 2. CoinFlyFX & GemFlyFX ──
        var coinFx = Object.FindFirstObjectByType<CoinFlyFX>(FindObjectsInactive.Include);
        if (coinFx == null)
        {
            var go = new GameObject("CoinFlyFX", typeof(CoinFlyFX));
            Undo.RegisterCreatedObjectUndo(go, "Create CoinFlyFX");
            go.transform.SetParent(canvas.transform, false);
            coinFx = go.GetComponent<CoinFlyFX>();
        }

        var gemFx = coinFx.GetComponent<GemFlyFX>();
        if (gemFx == null) gemFx = Undo.AddComponent<GemFlyFX>(coinFx.gameObject);

        RectTransform goldContainer = FindContainer(canvas, "gold_container", "icon_gold");
        if (goldContainer != null)
        {
            var soCoin = new SerializedObject(coinFx);
            soCoin.FindProperty("canvas").objectReferenceValue = canvas;
            soCoin.FindProperty("targetGoldIcon").objectReferenceValue = goldContainer;
            soCoin.ApplyModifiedProperties();
            EditorUtility.SetDirty(coinFx);
        }

        RectTransform gemContainer = FindContainer(canvas, "diamond_container", "icon_diamond", "gem", "kimcuong");
        if (gemContainer != null)
        {
            var soGem = new SerializedObject(gemFx);
            soGem.FindProperty("canvas").objectReferenceValue = canvas;
            soGem.FindProperty("targetGemIcon").objectReferenceValue = gemContainer;
            soGem.ApplyModifiedProperties();
            EditorUtility.SetDirty(gemFx);
        }

        // ── 3. WarehouseGainToastUI ──
        var toast = Object.FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include);
        if (toast == null)
        {
            var go = new GameObject("WarehouseGainToast", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create WarehouseGainToast");
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            toast = go.AddComponent<WarehouseGainToastUI>();
        }

        Sprite warehouseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_warehouse_v2_1786984374562-removebg-preview.png");

        var soToast = new SerializedObject(toast);
        soToast.FindProperty("canvas").objectReferenceValue = canvas;
        if (warehouseSprite != null)
            soToast.FindProperty("iconSprite").objectReferenceValue = warehouseSprite;
        AssignSprite(soToast, "panelSprite",    TrainSprites + "/popup_panel_paper.png");
        AssignSprite(soToast, "barTrackSprite", TrainSprites + "/progress_track_bar.png");
        AssignSprite(soToast, "barFillSprite",  TrainSprites + "/progress_fill_green.png");
        soToast.ApplyModifiedProperties();

        toast.EnsureBuilt();
        EditorUtility.SetDirty(toast);

        // ── 4. HarvestFeedbackSpawner (Nông sản bay về Toast, EXP bay về EXP_Bar_Container) ──
        var spawners = Object.FindObjectsByType<HarvestFeedbackSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RectTransform expContainer = FindContainer(canvas, "exp_bar_container", "icon_exp", "topbar_exp");

        foreach (var spawner in spawners)
        {
            var soSpawner = new SerializedObject(spawner);
            soSpawner.FindProperty("warehouseTarget").objectReferenceValue = toast.PanelRect != null ? (Transform)toast.PanelRect : toast.transform;
            if (expContainer != null)
            {
                soSpawner.FindProperty("expTarget").objectReferenceValue = expContainer;
                soSpawner.FindProperty("expPulseTarget").objectReferenceValue = expContainer;
            }
            soSpawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
        }

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"[FarmFX] XONG ✔ Setup toàn bộ HUD Gain FX: Nông sản bay về WarehouseGainToast, EXP bay về EXP_Bar_Container, Vàng/Kim Cương bay về Container chuẩn. NHỚ SAVE SCENE (Ctrl+S).");
    }

    private static RectTransform FindContainer(Canvas canvas, params string[] searchNames)
    {
        var rects = canvas.GetComponentsInChildren<RectTransform>(true);
        foreach (var name in searchNames)
        {
            foreach (var rt in rects)
            {
                if (rt.gameObject.name.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    return rt;
                }
            }
        }
        return null;
    }

    private static void AssignSprite(SerializedObject so, string field, string path)
    {
        var prop = so.FindProperty(field);
        if (prop == null) return;
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) prop.objectReferenceValue = sp;
    }
}
