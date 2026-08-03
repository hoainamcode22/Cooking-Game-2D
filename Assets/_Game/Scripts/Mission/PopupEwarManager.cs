using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupEwarManager : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject  popup_Ewar;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Controls")]
    [SerializeField] private Button      btnClose;

    [Header("ScrollView")]
    [SerializeField] private Transform   contentTransform;
    [SerializeField] private GameObject  missionItemPrefab;

    [Header("Data")]
    [SerializeField] private MissionDatabase missionDatabase;

    // TODO (Daily): MissionDatabase_Daily + 6 mission isDaily=true đã có data
    // (Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Daily.asset) nhưng popup
    // hiện chỉ có 1 list — cần thiết kế tab/section riêng trước khi nối daily vào UI.
    [SerializeField] private MissionDatabase dailyMissionDatabase;

    // Database thành tựu — tool "Setup Missions L1-L30" tự gán (MissionDatabase_Achievement).
    // UnifiedTaskPopupUI tab "Thành tựu" đọc qua AchievementMissionDatabaseRef.
    [SerializeField] private MissionDatabase achievementMissionDatabase;

    private readonly List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();
    private bool _initialized;
    private bool _popupInputLockHeld;

    // PopupManager.IsAnyPopupOpen() đọc property này để biết có đang mở không
    public bool IsOpen => popup_Ewar != null && popup_Ewar.activeSelf;

    // Cho UnifiedTaskPopupUI lấy database mà không phải gán lại tay trong Inspector.
    public MissionDatabase MissionDatabaseRef => missionDatabase;
    public MissionDatabase DailyMissionDatabaseRef => dailyMissionDatabase;
    public MissionDatabase AchievementMissionDatabaseRef => achievementMissionDatabase;

    private void Awake()
    {
        btnClose.onClick.AddListener(ClosePopup);

        // Đảm bảo CanvasGroup chặn click xuyên xuống map
        if (canvasGroup == null)
            canvasGroup = popup_Ewar.GetComponent<CanvasGroup>() ?? popup_Ewar.AddComponent<CanvasGroup>();

        if (popup_Ewar.GetComponent<UIRaycastBlocker>() == null)
            popup_Ewar.AddComponent<UIRaycastBlocker>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;

        popup_Ewar.SetActive(false);

        // UI cập nhật realtime khi tiến độ đổi (hết code chết NotifyProgressChanged)
        MissionProgressTracker.OnProgressChanged += HandleProgressChanged;
    }

    private void OnDestroy()
    {
        MissionProgressTracker.OnProgressChanged -= HandleProgressChanged;
    }


    public void OpenPopup()
    {
        UnifiedTaskPopupUI.OpenAchievement();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;

        popup_Ewar.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popup_Ewar, true);

        if (!_popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            _popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popup_Ewar, false);

        if (_popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            _popupInputLockHeld = false;
        }
    }

    private void SpawnMissionItems(int playerLevel)
    {
        if (missionDatabase == null || missionDatabase.missions == null) return;

        foreach (Transform child in contentTransform)
            Destroy(child.gameObject);

        _spawnedItems.Clear();

        foreach (var data in missionDatabase.missions)
        {
            if (data == null) continue;

            // List chính: chỉ mission đã mở khoá theo level, không lấy mission daily
            if (data.isDaily) continue;
            if (data.requiredLevel > playerLevel) continue;

            var go   = Instantiate(missionItemPrefab, contentTransform);
            var item = go.GetComponent<MissionItemUI>();
            if (item == null) continue;

            item.Setup(data);
            _spawnedItems.Add(item);
        }
    }

    private void RefreshAllProgress()
    {
        foreach (var item in _spawnedItems)
        {
            if (item == null || item.Data == null) continue;
            item.UpdateProgress(MissionProgressTracker.GetProgressFor(item.Data));
        }
    }

    private void HandleProgressChanged(string key, int newValue)
    {
        if (!IsOpen) return;
        NotifyProgressChanged();
    }

    /// <summary>Refresh tiến độ mọi item đang hiển thị (gọi từ event tracker).</summary>
    public void NotifyProgressChanged()
    {
        RefreshAllProgress();
    }

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }
}
