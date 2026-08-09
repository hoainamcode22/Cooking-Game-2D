using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject completedCheckmark;
    
    [Header("Rewards")]
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private TextMeshProUGUI gemsRewardText;

    private QuestData currentQuest;

    public void Setup(QuestData quest)
    {
        currentQuest = quest;
        titleText.text = quest.questName;
        
        goldRewardText.text = quest.rewardGold.ToString();
        gemsRewardText.text = quest.rewardGems.ToString();

        UpdateProgress();

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaimClicked);
    }

    private void UpdateProgress()
    {
        bool isCompleted = QuestManager.Instance.IsQuestCompleted(currentQuest.questId);
        
        if (isCompleted)
        {
            progressText.text = "Completed";
            progressBar.value = 1f;
            claimButton.gameObject.SetActive(false);
            completedCheckmark.SetActive(true);
            return;
        }

        bool allConditionsMet = true;
        int totalProgress = 0;
        int totalTarget = 0;

        for (int i = 0; i < currentQuest.conditions.Count; i++)
        {
            var cond = currentQuest.conditions[i];
            int prog = QuestManager.Instance.GetQuestProgress(currentQuest.questId, i);
            totalProgress += prog;
            totalTarget += cond.targetAmount;
            
            if (prog < cond.targetAmount)
                allConditionsMet = false;
        }

        progressText.text = $"{totalProgress} / {totalTarget}";
        if (totalTarget > 0)
            progressBar.value = (float)totalProgress / totalTarget;

        if (allConditionsMet)
        {
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = true;
            completedCheckmark.SetActive(false);
        }
        else
        {
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = false;
            completedCheckmark.SetActive(false);
        }
    }

    private void OnClaimClicked()
    {
        // Claim logic
        // QuestManager.Instance.ClaimQuestReward(currentQuest.questId);
        
        // Play FX
        QuestClaimAnimation.Instance.PlayClaimFX(transform.position, currentQuest.rewardGold, currentQuest.rewardGems);

        UpdateProgress();
        // Trigger popup refresh if necessary
        // QuestPopupController.Instance.RefreshContent();
    }
}
