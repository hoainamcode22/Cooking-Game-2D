using UnityEngine;
using UnityEditor;

public class QuestGeneratorTool : EditorWindow
{
    private const string QuestPath = "Assets/_Game/Data/Quests";
    private const string AchievementPath = "Assets/_Game/Data/Achievements";

    [MenuItem("Tools/Quest & Achievement Generator")]
    public static void ShowWindow()
    {
        GetWindow<QuestGeneratorTool>("Quest Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generator Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Base Achievements"))
        {
            GenerateBaseAchievements();
        }

        if (GUILayout.Button("Generate Sample Quests L1-L10"))
        {
            GenerateSampleQuests();
        }
    }

    private void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Game"))
        {
            AssetDatabase.CreateFolder("Assets", "_Game");
        }
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_Game", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Quests"))
        {
            AssetDatabase.CreateFolder("Assets/_Game/Data", "Quests");
        }
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Achievements"))
        {
            AssetDatabase.CreateFolder("Assets/_Game/Data", "Achievements");
        }
    }

    private void GenerateBaseAchievements()
    {
        EnsureDirectories();

        AchievementData achievement = ScriptableObject.CreateInstance<AchievementData>();
        achievement.achievementId = "achv_harvest_king";
        achievement.achievementName = "Harvest King";
        
        for (int i = 1; i <= 10; i++)
        {
            achievement.tiers.Add(new AchievementTier
            {
                threshold = i * 100,
                rewardGold = i * 50,
                rewardGems = i * 5,
                rewardExp = i * 20
            });
        }

        string path = $"{AchievementPath}/{achievement.achievementId}.asset";
        AssetDatabase.CreateAsset(achievement, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Generated Base Achievements at {path}");
    }

    private void GenerateSampleQuests()
    {
        EnsureDirectories();

        string[] sampleItems = { "Crop_Rice", "khoai_tay_chien", "Crop_Tomato" };
        
        for (int i = 1; i <= 10; i++)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = $"quest_main_L{i}";
            quest.questName = $"Level {i} Challenge";
            quest.kind = QuestKind.Main;
            quest.requiredLevel = i;
            quest.rewardGold = i * 100;
            quest.rewardGems = i % 3 == 0 ? 5 : 0;
            quest.rewardExp = i * 50;

            quest.conditions.Add(new QuestCondition
            {
                eventType = MissionEventType.HarvestItem,
                targetItemId = sampleItems[i % sampleItems.Length],
                targetAmount = i * 5
            });

            string path = $"{QuestPath}/{quest.questId}.asset";
            AssetDatabase.CreateAsset(quest, path);
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Generated 10 Sample Quests in {QuestPath}");
    }
}
