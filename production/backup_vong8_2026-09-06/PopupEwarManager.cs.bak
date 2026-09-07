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

    // C7 — đã xoá `contentTransform` + `missionItemPrefab`: hai field này chỉ phục vụ
    // `SpawnMissionItems()`, mà hàm đó KHÔNG có nơi nào gọi và `MissionItemUI` (thứ nó
    // sinh ra) thì cộng thưởng vào `PlayerWallet` — một cái ví mồ côi, không nối với
    // top-bar vàng/kim cương nào cả. Người chơi bấm "Nhận" và tiền rơi vào hư không.
    // Danh sách nhiệm vụ thật do `UnifiedTaskPopupUI` vẽ (nó cộng vào `FarmEconomyManager`).
    // `OpenPopup()` bên dưới cũng đã chuyển hướng sang `UnifiedTaskPopupUI` từ trước.

    [Header("Data")]
    [SerializeField] private MissionDatabase missionDatabase;

    // TODO (Daily): MissionDatabase_Daily + 6 mission isDaily=true đã có data
    // (Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Daily.asset) nhưng popup
    // hiện chỉ có 1 list — cần thiết kế tab/section riêng trước khi nối daily vào UI.
    [SerializeField] private MissionDatabase dailyMissionDatabase;

    // Database thành tựu — tool "Setup Missions L1-L30" tự gán (MissionDatabase_Achievement).
    // UnifiedTaskPopupUI tab "Thành tựu" đọc qua AchievementMissionDatabaseRef.
    [SerializeField] private MissionDatabase achievementMissionDatabase;

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

    /// <summary>
    /// Tiến độ nhiệm vụ vừa đổi. Popup này KHÔNG còn tự vẽ danh sách nhiệm vụ (xem C7),
    /// nên chỉ chuyển tiếp cho <see cref="UnifiedTaskPopupUI"/> — nơi đang thật sự vẽ.
    /// Vẫn phải nghe event: bỏ hẳn thì tiến độ đổi trong lúc popup đang mở sẽ không cập nhật.
    /// </summary>
    private void HandleProgressChanged(string key, int newValue)
    {
        if (!IsOpen) return;
        UnifiedTaskPopupUI.RefreshIfOpen();
    }
}
