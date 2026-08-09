using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestPopupController : MonoBehaviour
{
    public static QuestPopupController Instance { get; private set; }

    public enum TabType
    {
        MainQuests,
        Daily,
        Achievements,
        Events
    }

    [Header("UI References")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    
    [Header("Tab Buttons")]
    [SerializeField] private Button btnMainQuests;
    [SerializeField] private Button btnDaily;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnEvents;

    [Header("Prefabs")]
    [SerializeField] private QuestItemUI questItemPrefab;
    [SerializeField] private AchievementItemUI achievementItemPrefab;

    private TabType currentTab = TabType.MainQuests;
    private List<GameObject> activeItems = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Bind buttons
        btnMainQuests.onClick.AddListener(() => SwitchTab(TabType.MainQuests));
        btnDaily.onClick.AddListener(() => SwitchTab(TabType.Daily));
        btnAchievements.onClick.AddListener(() => SwitchTab(TabType.Achievements));
        btnEvents.onClick.AddListener(() => SwitchTab(TabType.Events));
    }

    public void Show()
    {
        popupContainer.SetActive(true);
        SwitchTab(TabType.MainQuests);
    }

    public void Hide()
    {
        popupContainer.SetActive(false);
    }

    private void SwitchTab(TabType tab)
    {
        currentTab = tab;
        UpdateTabVisuals();
        RefreshContent();
    }

    private void UpdateTabVisuals()
    {
        // Highlight active tab logic here
        btnMainQuests.interactable = currentTab != TabType.MainQuests;
        btnDaily.interactable = currentTab != TabType.Daily;
        btnAchievements.interactable = currentTab != TabType.Achievements;
        btnEvents.interactable = currentTab != TabType.Events;
    }

    public void RefreshContent()
    {
        // Clear old items
        foreach (var item in activeItems)
        {
            Destroy(item);
        }
        activeItems.Clear();

        switch (currentTab)
        {
            case TabType.MainQuests:
                PopulateQuests(QuestKind.Main);
                break;
            case TabType.Daily:
                PopulateQuests(QuestKind.Daily);
                break;
            case TabType.Achievements:
                PopulateAchievements();
                break;
            case TabType.Events:
                PopulateEvents();
                break;
        }

        // Reset scroll position
        scrollRect.normalizedPosition = new Vector2(0, 1);
    }

    private void PopulateQuests(QuestKind kind)
    {
        if (QuestManager.Instance == null) return;

        foreach (var quest in QuestManager.Instance.allQuests)
        {
            if (quest.kind == kind)
            {
                var item = Instantiate(questItemPrefab, contentContainer);
                item.Setup(quest);
                activeItems.Add(item.gameObject);
            }
        }
    }

    private void PopulateAchievements()
    {
        if (QuestManager.Instance == null) return;

        foreach (var ach in QuestManager.Instance.allAchievements)
        {
            var item = Instantiate(achievementItemPrefab, contentContainer);
            item.Setup(ach);
            activeItems.Add(item.gameObject);
        }
    }

    private void PopulateEvents()
    {
        // TODO: Implement Events system if available
    }
}
