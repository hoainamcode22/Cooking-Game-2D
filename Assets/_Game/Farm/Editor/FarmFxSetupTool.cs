using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup 1-click cho cụm FX "nhận tài nguyên" (yêu cầu Sếp 2026-08-26, tham khảo video Township):
///  - GemFlyFX: kim cương bay về icon gem trên HUD (gắn cạnh CoinFlyFX có sẵn).
///  - WarehouseGainToastUI: thanh kho [icon | fill bar | 25/30] hiện khi nhận vật phẩm.
/// Chạy: Tools → Farm Game → FX → Setup HUD Gain FX. Sau khi chạy NHỚ SAVE SCENE.
/// </summary>
public static class FarmFxSetupTool
{
    private const string TrainSprites = "Assets/Export_Train_UI_Package/Sprites";

    [MenuItem("Tools/Farm Game/FX/Setup HUD Gain FX (gem bay + thanh kho)")]
    public static void SetupHudGainFx()
    {
        // ── 1. Điểm neo: CoinFlyFX đã được Setup All wire sẵn trên HUD canvas ──
        var coinFx = Object.FindFirstObjectByType<CoinFlyFX>(FindObjectsInactive.Include);
        if (coinFx == null)
        {
            Debug.LogError("[FarmFX] Không thấy CoinFlyFX trong scene — chạy 'Setup All' trước rồi chạy lại tool này.");
            return;
        }

        var canvas = coinFx.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[FarmFX] CoinFlyFX không nằm trong Canvas nào — kiểm tra hierarchy HUD.");
            return;
        }

        // ── 2. GemFlyFX cạnh CoinFlyFX ──
        var gemFx = coinFx.GetComponent<GemFlyFX>();
        if (gemFx == null) gemFx = Undo.AddComponent<GemFlyFX>(coinFx.gameObject);

        RectTransform gemIcon = FindGemIcon(canvas);
        var soGem = new SerializedObject(gemFx);
        soGem.FindProperty("canvas").objectReferenceValue = canvas;
        if (gemIcon != null)
        {
            soGem.FindProperty("targetGemIcon").objectReferenceValue = gemIcon;
            var img = gemIcon.GetComponent<Image>();
            if (img != null && img.sprite != null)
                soGem.FindProperty("gemSprite").objectReferenceValue = img.sprite;
        }
        soGem.ApplyModifiedProperties();
        EditorUtility.SetDirty(gemFx);

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

        // Icon nhà kho: đọc từ HarvestFeedbackSpawner.warehouseTarget (field private → SerializedObject)
        Sprite warehouseSprite = null;
        var spawner = Object.FindFirstObjectByType<HarvestFeedbackSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            var soSpawner = new SerializedObject(spawner);
            var targetProp = soSpawner.FindProperty("warehouseTarget");
            var target = targetProp != null ? targetProp.objectReferenceValue as Transform : null;
            if (target != null)
            {
                var img = target.GetComponent<Image>();
                if (img != null) warehouseSprite = img.sprite;
            }
        }

        var soToast = new SerializedObject(toast);
        soToast.FindProperty("canvas").objectReferenceValue = canvas;
        if (warehouseSprite != null)
            soToast.FindProperty("iconSprite").objectReferenceValue = warehouseSprite;
        AssignSprite(soToast, "panelSprite",    TrainSprites + "/popup_panel_paper.png");
        AssignSprite(soToast, "barTrackSprite", TrainSprites + "/progress_track_bar.png");
        AssignSprite(soToast, "barFillSprite",  TrainSprites + "/progress_fill_green.png");
        soToast.ApplyModifiedProperties();

        // Bake hierarchy ngay trong Editor để sprite serialize vào scene (an toàn khi build)
        toast.EnsureBuilt();
        EditorUtility.SetDirty(toast);
        EditorSceneManager.MarkSceneDirty(toast.gameObject.scene);

        EditorGUIUtility.PingObject(toast);
        Debug.Log($"[FarmFX] XONG ✔ GemFlyFX (icon gem: {(gemIcon != null ? gemIcon.name : "TỰ TÌM lúc chạy")}) + " +
                  $"WarehouseGainToast (icon kho: {(warehouseSprite != null ? "OK" : "tự lấy lúc chạy")}). NHỚ SAVE SCENE (Ctrl+S).");
    }

    private static RectTransform FindGemIcon(Canvas canvas)
    {
        RectTransform best = null;
        foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            string n = rt.gameObject.name.ToLowerInvariant();
            if ((n.Contains("kimcuong") || n.Contains("gem") || n.Contains("diamond")) && rt.GetComponent<Image>() != null)
            {
                best = rt;
                break;
            }
        }
        return best;
    }

    private static void AssignSprite(SerializedObject so, string field, string path)
    {
        var prop = so.FindProperty(field);
        if (prop == null) return;
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) prop.objectReferenceValue = sp;
    }
}
