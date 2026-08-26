using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Setup 1-click Kitchen UI v2 (Sprint K1 — 2026-08-26).
/// Mở SampleScene rồi chạy: Tools → Farm Game → Kitchen → Setup Kitchen UI v2.
/// Tạo GO "Kitchen_UI_v2" + gán data (21 IngredientData, ListDishData) + DailySpecialManager.
/// KHÔNG xoá/tắt UI cũ — canvas v2 che phủ; minigame/popup cũ được UI v2 tự nâng lên trên.
/// </summary>
public static class KitchenV2SetupTool
{
    [MenuItem("Tools/Farm Game/Kitchen/Setup Kitchen UI v2")]
    public static void SetupKitchenV2()
    {
        var challenge = Object.FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
        if (challenge == null)
        {
            Debug.LogError("[KitchenV2] Không thấy CookingChallengeManager — mở đúng SampleScene (scene nấu ăn) rồi chạy lại.");
            return;
        }
        var selection = Object.FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

        // 1. Root GO
        var ui = Object.FindFirstObjectByType<KitchenUIv2.KitchenSceneV2UI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            var go = new GameObject("Kitchen_UI_v2", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Kitchen_UI_v2");
            ui = go.AddComponent<KitchenUIv2.KitchenSceneV2UI>();
        }

        var daily = ui.GetComponent<KitchenUIv2.DailySpecialManager>();
        if (daily == null) daily = Undo.AddComponent<KitchenUIv2.DailySpecialManager>(ui.gameObject);

        // 2. Data: 21 IngredientData + ListDishData
        var ingredients = new System.Collections.Generic.List<IngredientData>();
        foreach (var g in AssetDatabase.FindAssets("t:IngredientData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
            if (a != null) ingredients.Add(a);
        }
        ingredients.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        ListDishData book = null;
        foreach (var g in AssetDatabase.FindAssets("t:ListDishData"))
        {
            book = AssetDatabase.LoadAssetAtPath<ListDishData>(AssetDatabase.GUIDToAssetPath(g));
            if (book != null) break;
        }

        // 3. Gán qua SerializedObject (field private)
        var so = new SerializedObject(ui);
        var arr = so.FindProperty("allIngredients");
        arr.arraySize = ingredients.Count;
        for (int i = 0; i < ingredients.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = ingredients[i];
        so.FindProperty("dishBook").objectReferenceValue = book;
        so.FindProperty("challenge").objectReferenceValue = challenge;
        so.FindProperty("selection").objectReferenceValue = selection;
        so.ApplyModifiedProperties();

        var soDaily = new SerializedObject(daily);
        soDaily.FindProperty("dishBook").objectReferenceValue = book;
        soDaily.ApplyModifiedProperties();

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorGUIUtility.PingObject(ui);

        Debug.Log($"[KitchenV2] XONG ✔ nguyên liệu: {ingredients.Count} · sổ món: " +
                  $"{(book != null ? book.allDishes.Count.ToString() : "KHÔNG THẤY ListDishData!")} · " +
                  "NHỚ SAVE SCENE (Ctrl+S) rồi vào Play xem UI mới.");
    }
}
