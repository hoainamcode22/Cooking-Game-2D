using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PigPenPopupUI : MonoBehaviour
{
    public static PigPenPopupUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Slots — kéo các PigSlotUI vào")]
    [SerializeField] private List<PigSlotUI> pigSlots = new List<PigSlotUI>();

    [Header("Feed Items — kéo DraggablePigFeedItem vào")]
    [SerializeField] private List<DraggablePigFeedItem> feedItems = new List<DraggablePigFeedItem>();

    [SerializeField] private string productItemId = "pork";

    public bool IsOpen => popupRoot.activeSelf;
    private bool popupInputLockHeld;
    private bool _startOpen;

    private void Awake()
    {
        Instance = this;
        _startOpen = popupRoot != null && popupRoot.activeSelf;
    }

    private void Start()
    {
        if (!_startOpen) popupRoot.SetActive(false);

        for (int i = 0; i < pigSlots.Count; i++)
        {
            PigSlotUI slot = pigSlots[i];
            if (slot == null) continue;

            // Gán click event cho vật phẩm thịt heo
            GameObject vatPhamThitObj = slot.GetVatPhamThit();
            if (vatPhamThitObj == null) continue;

            EventTrigger trigger = vatPhamThitObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = vatPhamThitObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;

            PigSlotUI capturedSlot = slot;
            entry.callback.AddListener(_ => capturedSlot.OnHarvestClick());
            trigger.triggers.Add(entry);

            slot.OnHarvested += OnHarvestSlot;
        }

        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);
    }

    public void OpenPopup()
    {
        // [LEGACY - disabled, replaced by PenMiniPanelUI + PenClickDetector]
        return;

        popupRoot.SetActive(true);
        AcquirePopupInputBlock();
        RefreshFeedUI();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        popupRoot.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    public void RefreshFeedUI()
    {
        if (FarmInventoryManager.Instance == null) return;

        for (int i = 0; i < feedItems.Count; i++)
        {
            DraggablePigFeedItem item = feedItems[i];
            if (item == null) continue;

            int amount = FarmInventoryManager.Instance.GetAmount(item.feedItemId);

            if (item.txtFeedAmount != null)
                item.txtFeedAmount.text = "x" + amount;

            item.gameObject.SetActive(amount > 0);
        }
    }

    // Gọi khi user click thu hoạch thịt heo → cộng pork vào kho
    private void OnHarvestSlot()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.AddItem(productItemId, 1);
    }
}
