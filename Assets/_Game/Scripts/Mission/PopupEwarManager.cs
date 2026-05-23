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

    private readonly List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();
    private bool _initialized;
    private bool _popupInputLockHeld;

    // PopupManager.IsAnyPopupOpen() đọc property này để biết có đang mở không
    public bool IsOpen => popup_Ewar != null && popup_Ewar.activeSelf;

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
    }

    public void OpenPopup()
    {
        popup_Ewar.SetActive(true);
        AcquirePopupInputBlock();

        // Bật chặn raycast trên popup — ngăn click xuyên xuống các collider phía sau
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable   = true;

        // FarmInputLock.BlockMapPan tự động = true vì PopupManager.IsAnyPopupOpen()
        // sẽ trả về true → CameraController ngừng nhận input kéo map

        if (!_initialized)
        {
            SpawnMissionItems();
            _initialized = true;
        }

        RefreshAllProgress();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        // Tắt chặn raycast — cho phép click xuyên trở lại khi popup đóng
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;

        popup_Ewar.SetActive(false);

        // FarmInputLock.BlockMapPan tự động = false vì IsOpen đã là false
        // → CameraController nhận lại input kéo map bình thường
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

    private void SpawnMissionItems()
    {
        if (missionDatabase == null || missionDatabase.missions == null) return;

        foreach (Transform child in contentTransform)
            Destroy(child.gameObject);

        _spawnedItems.Clear();

        foreach (var data in missionDatabase.missions)
        {
            if (data == null) continue;

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
            int current = MissionProgressTracker.Instance != null
                ? MissionProgressTracker.Instance.GetProgress(item.Data.missionName)
                : 0;
            item.UpdateProgress(current);
        }
    }

    public void NotifyProgressChanged(string missionName, int newValue)
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null && item.Data != null && item.Data.missionName == missionName)
                item.UpdateProgress(newValue);
        }
    }
}
