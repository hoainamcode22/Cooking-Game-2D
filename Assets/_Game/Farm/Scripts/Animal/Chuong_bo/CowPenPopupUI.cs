using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CowPenPopupUI : MonoBehaviour
{
    public static CowPenPopupUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Slots — kéo 4 CowSlotUI vào")]
    [SerializeField] private List<CowSlotUI> cowSlots = new List<CowSlotUI>();

    [Header("Feed Items — kéo 3 DraggableFeedItem vào")]
    [SerializeField] private List<DraggableFeedItem> feedItems = new List<DraggableFeedItem>();

    [SerializeField] private string beefItemId = "beef";

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

        for (int i = 0; i < cowSlots.Count; i++)
        {
            CowSlotUI slot = cowSlots[i];
            if (slot == null) continue;

            GameObject vatPhamThitObj = slot.GetVatPhamThit();
            if (vatPhamThitObj == null) continue;

            EventTrigger trigger = vatPhamThitObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = vatPhamThitObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;

            CowSlotUI capturedSlot = slot;
            entry.callback.AddListener(_ => capturedSlot.OnHarvestClick());
            trigger.triggers.Add(entry);

            slot.OnHarvested += OnHarvestSlot;
        }

        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);
    }

    public void OpenPopup()
    {
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
            DraggableFeedItem item = feedItems[i];
            if (item == null) continue;

            int amount = FarmInventoryManager.Instance.GetAmount(item.feedItemId);

            if (item.txtFeedAmount != null)
                item.txtFeedAmount.text = "x" + amount;

            item.gameObject.SetActive(amount > 0);
        }
    }

    private void OnHarvestSlot()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.AddItem(beefItemId, 1);
    }
}
