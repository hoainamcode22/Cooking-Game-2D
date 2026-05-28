using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChickenPenPopupUI : MonoBehaviour
{
    public static ChickenPenPopupUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Slots — kéo các ChickenSlotUI vào")]
    [SerializeField] private List<ChickenSlotUI> chickenSlots = new List<ChickenSlotUI>();

    [Header("Feed Items — kéo DraggableChickenFeedItem vào")]
    [SerializeField] private List<DraggableChickenFeedItem> feedItems = new List<DraggableChickenFeedItem>();

    [SerializeField] private string chickenItemId = "chicken_meat";
    [SerializeField] private string eggItemId = "egg";

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

        for (int i = 0; i < chickenSlots.Count; i++)
        {
            ChickenSlotUI slot = chickenSlots[i];
            if (slot == null) continue;

            // Gán click event cho thịt gà
            GameObject vatPhamThitObj = slot.GetVatPhamThit();
            if (vatPhamThitObj != null)
            {
                EventTrigger triggerMeat = vatPhamThitObj.GetComponent<EventTrigger>();
                if (triggerMeat == null)
                    triggerMeat = vatPhamThitObj.AddComponent<EventTrigger>();

                EventTrigger.Entry entryMeat = new EventTrigger.Entry();
                entryMeat.eventID = EventTriggerType.PointerClick;

                ChickenSlotUI capturedSlot = slot;
                entryMeat.callback.AddListener(_ => capturedSlot.OnHarvestMeat());
                triggerMeat.triggers.Add(entryMeat);
            }

            // Gán click event cho trứng
            GameObject vatPhamTrungObj = slot.GetVatPhamTrung();
            if (vatPhamTrungObj != null)
            {
                EventTrigger triggerEgg = vatPhamTrungObj.GetComponent<EventTrigger>();
                if (triggerEgg == null)
                    triggerEgg = vatPhamTrungObj.AddComponent<EventTrigger>();

                EventTrigger.Entry entryEgg = new EventTrigger.Entry();
                entryEgg.eventID = EventTriggerType.PointerClick;

                ChickenSlotUI capturedSlotEgg = slot;
                entryEgg.callback.AddListener(_ => capturedSlotEgg.OnHarvestEgg());
                triggerEgg.triggers.Add(entryEgg);
            }

            // Subscribe event hoàn tất thu hoạch (cả hai đã click)
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
            DraggableChickenFeedItem item = feedItems[i];
            if (item == null) continue;

            int amount = FarmInventoryManager.Instance.GetAmount(item.feedItemId);

            if (item.txtFeedAmount != null)
                item.txtFeedAmount.text = "x" + amount;

            item.gameObject.SetActive(amount > 0);
        }
    }

    // Gọi khi user đã click CẢ HAI thịt và trứng → cộng cả hai vào kho
    private void OnHarvestSlot(ChickenSlotUI slot)
    {
        if (FarmInventoryManager.Instance == null) return;
        FarmInventoryManager.Instance.AddItem(chickenItemId, 1);
        FarmInventoryManager.Instance.AddItem(eggItemId, 1);
    }
}
