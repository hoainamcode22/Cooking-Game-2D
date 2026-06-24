using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionHudButtonUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MissionDatabase missionDatabase;
    [SerializeField] private PopupEwarManager popupEwarManager;
    [SerializeField] private bool bubbleInitiallyVisible = true;

    [Header("Circle Button")]
    [SerializeField] private Button missionButton;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Image buttonProgressFill;
    [SerializeField] private TMP_Text buttonProgressText;

    [Header("Bubble")]
    [SerializeField] private RectTransform bubbleRoot;
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image missionIcon;
    [SerializeField] private TMP_Text missionNameText;
    [SerializeField] private Image missionProgressFill;
    [SerializeField] private TMP_Text missionProgressText;
    [SerializeField] private Button goButton;
    [SerializeField] private TMP_Text goButtonText;

    private const string DefaultTitle = "Nhi\u1ec7m V\u1ee5 M\u1edbi";
    private const string DefaultAction = "\u0110\u1ebfn";
    private const string EmptyMission = "Ch\u01b0a c\u00f3 nhi\u1ec7m v\u1ee5";

    // Cho UnifiedTaskPopupUI lấy database nếu cần.
    public MissionDatabase MissionDatabaseRef => missionDatabase;

    private MissionData _currentMission;
    private bool _bubbleVisible;
    private Coroutine _bubbleRoutine;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (missionButton != null)
            missionButton.onClick.AddListener(ToggleBubble);

        if (goButton != null)
            goButton.onClick.AddListener(OpenMissionPopup);

        if (titleText != null)
            titleText.text = DefaultTitle;
        if (goButtonText != null)
            goButtonText.text = DefaultAction;

        SetBubbleVisible(bubbleInitiallyVisible, true);
    }

    private void OnEnable()
    {
        MissionProgressTracker.OnProgressChanged += HandleProgressChanged;

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.OnLevelChanged += HandleLevelChanged;

        RefreshNow();
    }

    private void OnDisable()
    {
        MissionProgressTracker.OnProgressChanged -= HandleProgressChanged;

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        // 0.3s: sau khi claim 1 nhiệm vụ (không đổi tiến độ nên OnProgressChanged không bắn),
        // thanh nhảy sang nhiệm vụ kế nhanh → cảm giác bám sát game.
        _nextRefreshTime = Time.unscaledTime + 0.3f;
        RefreshNow();
    }

    private void HandleProgressChanged(string key, int newValue)
    {
        RefreshNow();
    }

    private void HandleLevelChanged(int level)
    {
        RefreshNow();
    }

    private void ToggleBubble()
    {
        SetBubbleVisible(!_bubbleVisible, false);
    }

    private void OpenMissionPopup()
    {
        // Btn_GoMission ("Đi" trên bong bóng nhiệm vụ) → mở POPUP GỘP, tab Nhiệm vụ.
        SetBubbleVisible(false, false);
        UnifiedTaskPopupUI.OpenMission();
    }

    private void RefreshNow()
    {
        if (missionDatabase == null)
            return;

        _currentMission = PickMission();
        if (_currentMission == null)
        {
            ApplyEmptyState();
            return;
        }

        int current = Mathf.Clamp(
            MissionProgressTracker.GetProgressFor(_currentMission),
            0,
            Mathf.Max(1, _currentMission.targetAmount));
        int target = Mathf.Max(1, _currentMission.targetAmount);
        float progress01 = Mathf.Clamp01((float)current / target);

        if (buttonIcon != null && _currentMission.missionIcon != null)
            buttonIcon.sprite = _currentMission.missionIcon;
        if (missionIcon != null && _currentMission.missionIcon != null)
            missionIcon.sprite = _currentMission.missionIcon;

        if (missionNameText != null)
            missionNameText.text = _currentMission.missionName;
        if (buttonProgressFill != null)
            buttonProgressFill.fillAmount = progress01;
        if (missionProgressFill != null)
            missionProgressFill.fillAmount = progress01;

        string progressText = $"{current}/{target}";
        if (buttonProgressText != null)
            buttonProgressText.text = progressText;
        if (missionProgressText != null)
            missionProgressText.text = progressText;
    }

    private MissionData PickMission()
    {
        if (missionDatabase == null || missionDatabase.missions == null)
            return null;

        int playerLevel = GetPlayerLevel();
        MissionData completedUnclaimed = null;
        MissionData firstInProgress = null;

        foreach (MissionData mission in missionDatabase.missions)
        {
            if (!IsVisibleMainMission(mission, playerLevel))
                continue;
            if (IsClaimed(mission))
                continue;

            int current = MissionProgressTracker.GetProgressFor(mission);
            if (current >= Mathf.Max(1, mission.targetAmount))
            {
                if (completedUnclaimed == null) completedUnclaimed = mission;
            }
            else if (firstInProgress == null)
            {
                firstInProgress = mission;
            }
        }

        // ƯU TIÊN nhiệm vụ HOÀN THÀNH CHỜ NHẬN → thanh hiện nó (icon đồng bộ) để user bấm Nhận ngay;
        // chưa có cái nào xong thì hiện nhiệm vụ đang làm đầu tiên.
        return completedUnclaimed != null ? completedUnclaimed : firstInProgress;
    }

    private static bool IsVisibleMainMission(MissionData mission, int playerLevel)
    {
        return mission != null &&
               !mission.isDaily &&
               mission.requiredLevel <= playerLevel;
    }

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null)
            return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)
            return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }

    private static bool IsClaimed(MissionData mission)
    {
        if (mission == null)
            return true;

        string id = mission.MissionId;
        string key = mission.isDaily
            ? $"MISSION_CLAIMED_DAILY_{System.DateTime.Now:yyyyMMdd}_{id}"
            : $"MISSION_CLAIMED_{id}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void ApplyEmptyState()
    {
        if (missionNameText != null)
            missionNameText.text = EmptyMission;
        if (buttonProgressText != null)
            buttonProgressText.text = "0/0";
        if (missionProgressText != null)
            missionProgressText.text = "0/0";
        if (buttonProgressFill != null)
            buttonProgressFill.fillAmount = 0f;
        if (missionProgressFill != null)
            missionProgressFill.fillAmount = 0f;
    }

    private void SetBubbleVisible(bool visible, bool instant)
    {
        _bubbleVisible = visible;

        if (bubbleRoot == null)
            return;

        if (bubbleCanvasGroup == null)
            bubbleCanvasGroup = bubbleRoot.GetComponent<CanvasGroup>();

        if (_bubbleRoutine != null)
            StopCoroutine(_bubbleRoutine);

        if (instant)
        {
            bubbleRoot.gameObject.SetActive(visible);
            bubbleRoot.localScale = visible ? Vector3.one : new Vector3(0.82f, 0.82f, 1f);
            if (bubbleCanvasGroup != null)
            {
                bubbleCanvasGroup.alpha = visible ? 1f : 0f;
                bubbleCanvasGroup.interactable = visible;
                bubbleCanvasGroup.blocksRaycasts = visible;
            }
            return;
        }

        _bubbleRoutine = StartCoroutine(AnimateBubble(visible));
    }

    private IEnumerator AnimateBubble(bool visible)
    {
        bubbleRoot.gameObject.SetActive(true);
        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.interactable = false;
            bubbleCanvasGroup.blocksRaycasts = false;
        }

        Vector3 fromScale = bubbleRoot.localScale;
        Vector3 toScale = visible ? Vector3.one : new Vector3(0.82f, 0.82f, 1f);
        float fromAlpha = bubbleCanvasGroup != null ? bubbleCanvasGroup.alpha : (visible ? 0f : 1f);
        float toAlpha = visible ? 1f : 0f;

        const float duration = 0.16f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            bubbleRoot.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }

        bubbleRoot.localScale = toScale;
        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.alpha = toAlpha;
            bubbleCanvasGroup.interactable = visible;
            bubbleCanvasGroup.blocksRaycasts = visible;
        }
        bubbleRoot.gameObject.SetActive(visible);
        _bubbleRoutine = null;
    }
}
