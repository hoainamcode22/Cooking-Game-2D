#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using Assetsgame.Animals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PenAnimalSetupTool
{
    private const string ChickenPrefabPath = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Chicken/Prefab_Chicken.prefab";
    private const string CowBrownPrefabPath = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Prefab_Cow_Brown.prefab";
    private const string CowDairyPrefabPath = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Prefab_Cow.prefab";
    private const string PigPrefabPath = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Piggy/Prefab_piggy.prefab";

    private const string ChickenAudio1 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Chicken/Audio/Chicken-001.wav";
    private const string ChickenAudio2 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Chicken/Audio/Chicken-002.wav";
    private const string CowAudio1 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Audio/Cow-001.wav";
    private const string CowAudio2 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Cow/Audio/Cow-002.wav";
    private const string PigAudio1 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Piggy/Audio/Pig-001.wav";
    private const string PigAudio2 = "Assets/Assetsgame/Bò/HappyHarvest_Copy/Art/Animals/Piggy/Audio/Pig-002.wav";

    [MenuItem("Tools/Farm/Setup Toàn Bộ 4 Chuồng Gia Súc (Gà, Bò thịt, Bò sữa, Heo)")]
    public static void SetupAllPens()
    {
        // Load Animal Prefabs
        GameObject chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChickenPrefabPath);
        GameObject cowBrownPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CowBrownPrefabPath);
        GameObject cowDairyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CowDairyPrefabPath);
        GameObject pigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PigPrefabPath);

        // Load Audio Clips
        AudioClip chick1 = AssetDatabase.LoadAssetAtPath<AudioClip>(ChickenAudio1);
        AudioClip chick2 = AssetDatabase.LoadAssetAtPath<AudioClip>(ChickenAudio2);
        AudioClip[] chickenSounds = (chick1 != null && chick2 != null) ? new[] { chick1, chick2 } : (chick1 != null ? new[] { chick1 } : null);

        AudioClip cow1 = AssetDatabase.LoadAssetAtPath<AudioClip>(CowAudio1);
        AudioClip cow2 = AssetDatabase.LoadAssetAtPath<AudioClip>(CowAudio2);
        AudioClip[] cowSounds = (cow1 != null && cow2 != null) ? new[] { cow1, cow2 } : (cow1 != null ? new[] { cow1 } : null);

        AudioClip pig1 = AssetDatabase.LoadAssetAtPath<AudioClip>(PigAudio1);
        AudioClip pig2 = AssetDatabase.LoadAssetAtPath<AudioClip>(PigAudio2);
        AudioClip[] pigSounds = (pig1 != null && pig2 != null) ? new[] { pig1, pig2 } : (pig1 != null ? new[] { pig1 } : null);

        // 1. Setup Pen_01 (Bò thịt)
        ConfigurePenPrefab("Pen_01.prefab", cowBrownPrefab, "bonam1", "HappyHarvest_Cow_Brown",
            new Vector3(0f, 0f, 0f), new Vector3(0.85f, 0.85f, 0.85f), 2, 1.2f, new Vector2(-1.15f, -0.6f), new Vector2(1.15f, 0.45f), cowSounds);

        // 2. Setup Pen_02 (Heo)
        ConfigurePenPrefab("Pen_02.prefab", pigPrefab, "heonam1_0", "HappyHarvest_Pig",
            new Vector3(0f, 0f, 0f), new Vector3(0.85f, 0.85f, 0.85f), 2, 1.2f, new Vector2(-1.15f, -0.6f), new Vector2(1.15f, 0.45f), pigSounds);

        // 3. Setup Pen_03 (Gà)
        ConfigurePenPrefab("Pen_03.prefab", chickenPrefab, "ganam1_0", "HappyHarvest_Chicken",
            new Vector3(0f, 0f, 0f), new Vector3(0.85f, 0.85f, 0.85f), 3, 1.1f, new Vector2(-1.15f, -0.6f), new Vector2(1.15f, 0.45f), chickenSounds);

        // 4. Setup Pen_04 (Bò sữa)
        ConfigurePenPrefab("Pen_04.prefab", cowDairyPrefab, "ganam1_0", "HappyHarvest_Cow",
            new Vector3(0f, 0f, 0f), new Vector3(0.85f, 0.85f, 0.85f), 2, 1.2f, new Vector2(-1.15f, -0.6f), new Vector2(1.15f, 0.45f), cowSounds);

        AssetDatabase.SaveAssets();

        // 5. Cập nhật giao diện Process UI cho cả Ruộng và Chuồng Trại
        BuildingProcessUIBuilderTool.BuildAndApplyAllProcessUI();

        // 6. Cập nhật các instance trong Scene hiện tại
        var allSpawners = Object.FindObjectsByType<HappyHarvestAnimalVisualSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var spawner in allSpawners)
        {
            EditorUtility.SetDirty(spawner.gameObject);
        }

        if (allSpawners.Length > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[PenSetup] Đã hoàn tất setup toàn bộ 4 chuồng (Gà, Bò thịt, Bò sữa, Heo) với đầy đủ bounds, walk anim, AI, âm thanh, sorting tứ chi & Process UI mượt mà!");
    }

    private static void ConfigurePenPrefab(string prefabName, GameObject animalPrefab, string legacyChildName, string spawnedChildName,
        Vector3 localPos, Vector3 localScale, int count, float spacing, Vector2 minBounds, Vector2 maxBounds, AudioClip[] sounds)
    {
        string targetPath = $"Assets/_Game/Farm/CÔNG TRÌNH/{prefabName}";
        if (!File.Exists(targetPath))
        {
            string[] guids = AssetDatabase.FindAssets(prefabName.Replace(".prefab", "") + " t:Prefab");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(prefabName))
                {
                    targetPath = p;
                    break;
                }
            }
        }
        if (!File.Exists(targetPath)) return;

        try
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(targetPath);
            if (prefabRoot == null) return;

            HappyHarvestAnimalVisualSpawner spawner = prefabRoot.GetComponentInChildren<HappyHarvestAnimalVisualSpawner>(true);
            if (spawner == null)
            {
                spawner = prefabRoot.AddComponent<HappyHarvestAnimalVisualSpawner>();
            }

            SetPrivateField(spawner, "animalPrefab", animalPrefab);
            SetPrivateField(spawner, "legacyChildName", legacyChildName);
            SetPrivateField(spawner, "spawnedChildName", spawnedChildName);
            SetPrivateField(spawner, "localPosition", localPos);
            SetPrivateField(spawner, "localScale", localScale);
            SetPrivateField(spawner, "animalCount", count);
            SetPrivateField(spawner, "horizontalSpacing", spacing);
            SetPrivateField(spawner, "walkBoundsMin", minBounds);
            SetPrivateField(spawner, "walkBoundsMax", maxBounds);
            SetPrivateField(spawner, "soundClips", sounds);
            SetPrivateField(spawner, "sortingOrderOffset", 50);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, targetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PenSetup] Bỏ qua config prefab {targetPath}: {ex.Message}");
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null)
        {
            f.SetValue(target, value);
        }
    }
}
#endif
