using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementItemUI : MonoBehaviour
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

    private AchievementData currentAch;
    private int currentTierIndex;

    public void Setup(AchievementData ach)
    {
        currentAch = ach;
        titleText.text = ach.achievementName;
        
        UpdateProgress();

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaimClicked);
    }

    private void UpdateProgress()
    {
        int progress = QuestManager.Instance.GetAchievementProgress(currentAch.achievementId);
        int claimedTier = QuestManager.Instance.GetClaimedTier(currentAch.achievementId); // Assuming 0 means none claimed, 1 means tier 1, etc.
        
        currentTierIndex = claimedTier;

        if (currentTierIndex >= currentAch.tiers.Count)
        {
            // All tiers completed
            progressText.text = "Max Tier Reached";
            progressBar.value = 1f;
            claimButton.gameObject.SetActive(false);
            completedCheckmark.SetActive(true);
            
            goldRewardText.text = "0";
            gemsRewardText.text = "0";
            return;
        }

        var currentTier = currentAch.tiers[currentTierIndex];
        goldRewardText.text = currentTier.rewardGold.ToString();
        gemsRewardText.text = currentTier.rewardGems.ToString();

        progressText.text = $"{progress} / {currentTier.threshold}";
        progressBar.value = (float)progress / currentTier.threshold;

        if (progress >= currentTier.threshold)
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
        var tierData = currentAch.tiers[currentTierIndex];
        
        // Claim logic
        QuestManager.Instance.ClaimAchievementTier(currentAch.achievementId, currentTierIndex + 1);
        
        // Play FX
        QuestClaimAnimation.Instance.PlayClaimFX(transform.position, tierData.rewardGold, tierData.rewardGems);

        UpdateProgress();
    }
}
