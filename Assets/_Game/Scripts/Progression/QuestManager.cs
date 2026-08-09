using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StringIntPair
{
    public string key;
    public int value;
    public StringIntPair(string k, int v) { key = k; value = v; }
}

[System.Serializable]
public class QuestSaveData
{
    public List<StringIntPair> questProgress = new List<StringIntPair>();
    public List<string> completedQuests = new List<string>();
    public List<StringIntPair> achievementProgress = new List<StringIntPair>();
    public List<StringIntPair> claimedAchievementTiers = new List<StringIntPair>();

    public int GetValue(List<StringIntPair> list, string key)
    {
        var pair = list.Find(p => p.key == key);
        return pair != null ? pair.value : 0;
    }

    public void SetValue(List<StringIntPair> list, string key, int val)
    {
        var pair = list.Find(p => p.key == key);
        if (pair != null)
            pair.value = val;
        else
            list.Add(new StringIntPair(key, val));
    }
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    private const string SAVE_KEY = "QUEST_SAVE_V1";
    private QuestSaveData saveData = new QuestSaveData();

    public List<QuestData> allQuests = new List<QuestData>();
    public List<AchievementData> allAchievements = new List<AchievementData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadProgress()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            saveData = JsonUtility.FromJson<QuestSaveData>(json) ?? new QuestSaveData();
        }
    }

    private void SaveProgress()
    {
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    // --- PUBLIC GETTERS FOR UI ---

    public int GetQuestProgress(string questId, int conditionIndex)
    {
        return saveData.GetValue(saveData.questProgress, $"{questId}_{conditionIndex}");
    }

    public bool IsQuestCompleted(string questId)
    {
        return saveData.completedQuests.Contains(questId);
    }

    public int GetAchievementProgress(string achievementId)
    {
        return saveData.GetValue(saveData.achievementProgress, achievementId);
    }

    public int GetClaimedTier(string achievementId)
    {
        return saveData.GetValue(saveData.claimedAchievementTiers, achievementId);
    }

    public void ClaimAchievementTier(string achievementId, int tier)
    {
        saveData.SetValue(saveData.claimedAchievementTiers, achievementId, tier);
        SaveProgress();
    }

    // --- EVENT HOOKS ---

    public void OnItemHarvested(string itemId, int amount = 1)
    {
        ProcessEvent(MissionEventType.HarvestItem, itemId, amount);
    }

    public void OnItemCooked(string dishId, int amount = 1)
    {
        ProcessEvent(MissionEventType.CookDish, dishId, amount);
    }

    public void OnOrderDelivered(string orderId = "", int amount = 1)
    {
        ProcessEvent(MissionEventType.DeliverOrder, orderId, amount);
    }

    private void ProcessEvent(MissionEventType eventType, string itemId, int amount)
    {
        bool changed = false;

        foreach (var quest in allQuests)
        {
            if (saveData.completedQuests.Contains(quest.questId)) continue;

            for (int i = 0; i < quest.conditions.Count; i++)
            {
                var cond = quest.conditions[i];
                if (cond.eventType == eventType && (string.IsNullOrEmpty(cond.targetItemId) || cond.targetItemId == itemId))
                {
                    string key = $"{quest.questId}_{i}";
                    int currentProgress = saveData.GetValue(saveData.questProgress, key);
                    
                    int newProgress = currentProgress + amount;
                    newProgress = Mathf.Min(newProgress, cond.targetAmount);
                    
                    if (newProgress != currentProgress)
                    {
                        saveData.SetValue(saveData.questProgress, key, newProgress);
                        changed = true;
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }

        foreach (var achv in allAchievements)
        {
            if (achv.achievementId.Contains("harvest") && eventType == MissionEventType.HarvestItem)
            {
                int currentProgress = saveData.GetValue(saveData.achievementProgress, achv.achievementId);
                saveData.SetValue(saveData.achievementProgress, achv.achievementId, currentProgress + amount);
                changed = true;
            }
            // Can add more event to achievement mappings later
        }

        if (changed)
        {
            SaveProgress();
        }
    }

    private void CheckQuestCompletion(QuestData quest)
    {
        bool allDone = true;
        for (int i = 0; i < quest.conditions.Count; i++)
        {
            string key = $"{quest.questId}_{i}";
            int progress = saveData.GetValue(saveData.questProgress, key);
            if (progress < quest.conditions[i].targetAmount)
            {
                allDone = false;
                break;
            }
        }

        if (allDone && !saveData.completedQuests.Contains(quest.questId))
        {
            saveData.completedQuests.Add(quest.questId);
            Debug.Log($"Quest {quest.questName} completed!");
            // TODO: Give rewards using PlayerWallet or PlayerProgressManager
        }
    }
}
