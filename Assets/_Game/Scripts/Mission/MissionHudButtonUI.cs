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

    [Header("Điều kiện hiện HUD nhiệm vụ")]
    // Người chơi mới vào game đang phải xử lý hai luồng chỉ dẫn cùng lúc: bàn tay
    // tutorial dạy kéo hạt, VÀ bong bóng "Giao 2 đơn hàng" chắn ngay giữa ruộng. Nhiệm
    // vụ đó còn chưa làm được (chưa mở bảng đơn, chưa có hàng), nên nó chỉ tổ gây rối.
    // Giấu tới khi tutorial xong VÀ đủ cấp thì lúc hiện ra người chơi mới làm được ngay.
    [Tooltip("Cấp tối thiểu để hiện nút + bong bóng nhiệm vụ. 0 = bỏ qua điều kiện cấp.")]
    [SerializeField] private int capToiThieuHien = 3;

    [Tooltip("Bắt buộc xong tutorial mới hiện. Bỏ tick thì chỉ xét theo cấp.")]
    [SerializeField] private bool phaiXongTutorial = true;

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

        // Chưa đủ điều kiện thì ẩn NGAY trong Awake. Chờ RefreshNow ở OnEnable là muộn
        // một khung hình — đủ để bong bóng loé lên rồi tắt, nhìn như lỗi hiển thị.
        bool hien = DuocPhepHien();
        ApDungHienThi(hien);
        SetBubbleVisible(hien && bubbleInitiallyVisible, true);
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

    /// <summary>
    /// Đủ điều kiện cho người chơi thấy HUD nhiệm vụ chưa.
    /// </summary>
    private bool DuocPhepHien()
    {
        if (phaiXongTutorial && !TutorialManager.IsTutorialDone)
            return false;

        return capToiThieuHien <= 0 || GetPlayerLevel() >= capToiThieuHien;
    }

    /// <summary>
    /// Bật/tắt phần NHÌN THẤY ĐƯỢC, KHÔNG tắt chính GameObject mang script này —
    /// tắt nó thì Update ngừng chạy và HUD sẽ không bao giờ tự hiện lại khi lên cấp.
    /// </summary>
    private void ApDungHienThi(bool hien)
    {
        if (missionButton != null && missionButton.gameObject.activeSelf != hien)
            missionButton.gameObject.SetActive(hien);

        if (!hien)
        {
            // Ẩn luôn bong bóng, kể cả khi nó đang mở dở.
            if (bubbleRoot != null && bubbleRoot.gameObject.activeSelf)
                bubbleRoot.gameObject.SetActive(false);
            _bubbleVisible = false;
        }
    }

    private void RefreshNow()
    {
        if (!DuocPhepHien())
        {
            ApDungHienThi(false);
            return;
        }

        ApDungHienThi(true);

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
