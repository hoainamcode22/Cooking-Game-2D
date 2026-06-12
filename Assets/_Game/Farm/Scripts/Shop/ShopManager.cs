using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI")]
    public GameObject     shopPanel;     // Panel nền popup Shop
    public Transform      contentParent; // Vùng chứa item
    public GameObject     itemPrefab;    // Prefab của 1 item trong shop
    public TMP_InputField searchBar;     // Thanh tìm kiếm

    [Header("Dữ liệu theo Tab")]
    public List<BaseItemData> seedList;     // Tab 0 - Hạt giống
    public List<BaseItemData> buildingList; // Tab 1 - Công trình
    public List<BaseItemData> decorList;    // Tab 2 - Trang trí

    private List<BaseItemData> currentActiveList;
    private bool popupInputLockHeld;

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        shopPanel.SetActive(false);
        searchBar.onValueChanged.AddListener(OnSearchTextChanged);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Nếu click KHÔNG trúng UI nào trong Canvas_Popup → đóng Shop
        if (!IsPointerOverPopupUI(Input.mousePosition))
            CloseShop();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        AcquirePopupInputBlock();
        if (searchBar != null) searchBar.text = "";
        ShowTab(0);
    }

    public void CloseShop()
    {
        ReleasePopupInputBlock();
        shopPanel.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(shopPanel, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(shopPanel, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    public void ShowTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0: currentActiveList = seedList;     break;
            case 1: currentActiveList = buildingList; break;
            case 2: currentActiveList = decorList;    break;
            default:
                return;
        }

        if (searchBar != null)
            OnSearchTextChanged(searchBar.text);
        else
            OnSearchTextChanged("");
    }

    // ── Tìm kiếm & Render ────────────────────────────────────────────────────

    private void OnSearchTextChanged(string keyword)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (currentActiveList == null) return;

        string keyLower = keyword.ToLower();

        foreach (BaseItemData item in currentActiveList)
        {
            bool match = string.IsNullOrEmpty(keyword)
                      || item.itemName.ToLower().Contains(keyLower);

            if (!match) continue;

            GameObject go = Instantiate(itemPrefab, contentParent);
            go.GetComponent<ShopItemUI>().Setup(item);
        }
    }

    // ── UI Raycast (y hệt PigPenClickOpen) ───────────────────────────────────

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            Transform t = results[i].gameObject.transform;
            Canvas parentCanvas = t.GetComponentInParent<Canvas>();

            if (parentCanvas != null && parentCanvas.name == "Canvas_Popup")
                return true;
        }

        return false;
    }
}
