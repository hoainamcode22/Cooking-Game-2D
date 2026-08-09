using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestHUDController : MonoBehaviour
{
    public enum HUDState
    {
        Hidden,
        InProgress,
        Completed
    }

    [Header("UI References")]
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    
    [Header("Completed State Visuals")]
    [SerializeField] private GameObject redDotIndicator;
    [SerializeField] private GameObject glowEffect;
    [SerializeField] private RectTransform iconTransform;

    private HUDState currentState = HUDState.Hidden;
    private QuestData trackedQuest;
    private Coroutine bounceCoroutine;
    private Coroutine glowCoroutine;

    private void Start()
    {
        SetState(HUDState.Hidden);
    }

    public void TrackQuest(QuestData quest)
    {
        trackedQuest = quest;
        questTitleText.text = quest.questName;
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        if (trackedQuest == null) return;

        bool allCompleted = true;
        int totalProgress = 0;
        int totalTarget = 0;

        for (int i = 0; i < trackedQuest.conditions.Count; i++)
        {
            var condition = trackedQuest.conditions[i];
            int progress = QuestManager.Instance.GetQuestProgress(trackedQuest.questId, i);
            
            totalProgress += progress;
            totalTarget += condition.targetAmount;

            if (progress < condition.targetAmount)
            {
                allCompleted = false;
            }
        }

        progressText.text = $"{totalProgress} / {totalTarget}";
        if (totalTarget > 0)
        {
            progressBar.value = (float)totalProgress / totalTarget;
        }

        if (QuestManager.Instance.IsQuestCompleted(trackedQuest.questId) || allCompleted)
        {
            SetState(HUDState.Completed);
        }
        else
        {
            SetState(HUDState.InProgress);
        }
    }

    private void SetState(HUDState state)
    {
        currentState = state;

        // Reset animations
        if (bounceCoroutine != null) { StopCoroutine(bounceCoroutine); bounceCoroutine = null; }
        if (glowCoroutine != null) { StopCoroutine(glowCoroutine); glowCoroutine = null; }
        iconTransform.localScale = Vector3.one;
        glowEffect.SetActive(false);

        switch (state)
        {
            case HUDState.Hidden:
                contentPanel.SetActive(false);
                break;

            case HUDState.InProgress:
                contentPanel.SetActive(true);
                redDotIndicator.SetActive(false);
                break;

            case HUDState.Completed:
                contentPanel.SetActive(true);
                redDotIndicator.SetActive(true);
                glowEffect.SetActive(true);
                
                // Play animations
                PlayCompletedAnimations();
                break;
        }
    }

    private void PlayCompletedAnimations()
    {
        bounceCoroutine = StartCoroutine(BounceRoutine());
        if (glowEffect.TryGetComponent<Image>(out var glowImage))
        {
            glowImage.color = new Color(1f, 1f, 1f, 0.5f);
            glowCoroutine = StartCoroutine(GlowRoutine(glowImage));
        }
    }

    private IEnumerator BounceRoutine()
    {
        float speed = 1f / 0.4f;
        while (true)
        {
            float pingPong = Mathf.PingPong(Time.time * speed, 1f);
            float t = pingPong * pingPong * (3f - 2f * pingPong);
            float scale = Mathf.Lerp(1f, 1.15f, t);
            iconTransform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
    }

    private IEnumerator GlowRoutine(Image glowImage)
    {
        float speed = 1f / 0.6f;
        while (true)
        {
            float pingPong = Mathf.PingPong(Time.time * speed, 1f);
            float alpha = Mathf.Lerp(0.5f, 1f, pingPong);
            Color c = glowImage.color;
            c.a = alpha;
            glowImage.color = c;
            yield return null;
        }
    }

    public void OnHUDClicked()
    {
        if (currentState == HUDState.Completed)
        {
            // Open popup or claim directly
            // QuestPopupController.Instance.Show();
        }
    }

    private void OnDestroy()
    {
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        if (glowCoroutine != null) StopCoroutine(glowCoroutine);
    }
}
