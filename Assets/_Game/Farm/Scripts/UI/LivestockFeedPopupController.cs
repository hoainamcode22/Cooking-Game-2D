using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LivestockFeedPopupController : MonoBehaviour
{
    [Header("UI Container")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject feedItemPrefab;

    [Header("Item Styling")]
    [SerializeField] private float itemWidth = 130f;
    [SerializeField] private float itemHeight = 160f;

    private PenMiniPanelUI _currentPen;
    private bool _popupInputLockHeld;
    private readonly List<LivestockFeedDragItem> _activeItems = new List<LivestockFeedDragItem>();

    public PenMiniPanelUI CurrentPen => _currentPen;

    private void OnEnable()
    {
        FarmInputLock.IsSeedPopupOpen = true;
        AcquireInputLock();

        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshAllStocks;
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshAllStocks;
        }

        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.OnWarehouseChanged -= RefreshAllStocks;
            WarehouseManager.Instance.OnWarehouseChanged += RefreshAllStocks;
        }
    }

    private void OnDisable()
    {
        FarmInputLock.IsSeedPopupOpen = false;
        ReleaseInputLock();

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshAllStocks;

        if (WarehouseManager.Instance != null)
            WarehouseManager.Instance.OnWarehouseChanged -= RefreshAllStocks;
    }

    private void Update()
    {
        if (FarmInputLock.IsDraggingSeed) return;
        if (!InputBridge.IsPointerDownThisFrame) return;

        if (!IsPointerOnPopup())
        {
            FarmUIManager.Instance?.HideLivestockFeedPopup();
        }
    }

    private bool IsPointerOnPopup()
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = InputBridge.PointerPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject != null && results[i].gameObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private void AcquireInputLock()
    {
        if (_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _popupInputLockHeld = true;
    }

    private void ReleaseInputLock()
    {
        if (!_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _popupInputLockHeld = false;
    }

    public void Open(PenMiniPanelUI pen)
    {
        _currentPen = pen;
        if (pen == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        PopulateFeedItems(pen);
    }

    public void PopulateFeedItems(PenMiniPanelUI pen)
    {
        if (pen == null || pen.Config == null) return;
        var cfg = pen.Config;

        EnsureContent();

        // Ẩn tất cả item cũ
        for (int i = 0; i < _activeItems.Count; i++)
        {
            if (_activeItems[i] != null) _activeItems[i].gameObject.SetActive(false);
        }

        int itemIdx = 0;

        // 1. Thức ăn 1 (Ví dụ: Cám Bò, Cám Heo, Cám Gà)
        if (!string.IsNullOrEmpty(cfg.food1ItemId))
        {
            Sprite icon = cfg.food1Icon != null ? cfg.food1Icon : GetDefaultFeedSprite(cfg.food1ItemId);
            string name = GetFoodDisplayName(cfg.food1ItemId);
            GetOrCreateItem(itemIdx++).Setup(cfg.food1ItemId, icon, name, pen);
        }

        // 2. Thức ăn 2 (Nông sản phụ trợ: Bắp cải / Ngô / Lúa mì)
        if (!string.IsNullOrEmpty(cfg.food2ItemId))
        {
            Sprite icon = cfg.food2Icon != null ? cfg.food2Icon : GetDefaultFeedSprite(cfg.food2ItemId);
            string name = GetFoodDisplayName(cfg.food2ItemId);
            GetOrCreateItem(itemIdx++).Setup(cfg.food2ItemId, icon, name, pen);
        }

        // 3. Thức ăn cao cấp (Premium Food)
        if (!string.IsNullOrEmpty(cfg.premiumFoodItemId) && cfg.premiumFoodItemId != cfg.food1ItemId)
        {
            Sprite icon = cfg.premiumFoodIcon != null ? cfg.premiumFoodIcon : GetDefaultFeedSprite(cfg.premiumFoodItemId);
            string name = GetFoodDisplayName(cfg.premiumFoodItemId);
            GetOrCreateItem(itemIdx++).Setup(cfg.premiumFoodItemId, icon, name, pen);
        }

        RefreshAllStocks();
    }

    private void EnsureContent()
    {
        if (content != null) return;

        // Tự tìm hoặc tạo child "Content"
        Transform c = transform.Find("Content") ?? transform.Find("Panel/Content") ?? transform.Find("Background/Content");
        if (c != null)
        {
            content = c;
            return;
        }

        var go = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 180f);
        rt.anchoredPosition = Vector2.zero;

        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        content = go.transform;
    }

    private LivestockFeedDragItem GetOrCreateItem(int index)
    {
        while (_activeItems.Count <= index)
        {
            GameObject itemObj = null;
            if (feedItemPrefab != null)
            {
                itemObj = Instantiate(feedItemPrefab, content);
            }
            else
            {
                itemObj = CreateDefaultFeedItemObj(content);
            }

            var dragItem = itemObj.GetComponent<LivestockFeedDragItem>();
            if (dragItem == null) dragItem = itemObj.AddComponent<LivestockFeedDragItem>();
            _activeItems.Add(dragItem);
        }

        var item = _activeItems[index];
        item.gameObject.SetActive(true);
        return item;
    }

    private GameObject CreateDefaultFeedItemObj(Transform parent)
    {
        var root = new GameObject("FeedItem", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LivestockFeedDragItem));
        root.transform.SetParent(parent, false);

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(itemWidth, itemHeight);

        var bg = root.GetComponent<Image>();
        bg.color = new Color(1f, 0.98f, 0.94f, 0.95f);

        // Icon
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.6f);
        iconRt.anchorMax = new Vector2(0.5f, 0.6f);
        iconRt.sizeDelta = new Vector2(85f, 85f);
        iconRt.anchoredPosition = Vector2.zero;

        // Tên
        var nameGo = new GameObject("TxtName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(root.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.82f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.anchoredPosition = Vector2.zero;
        nameRt.offsetMin = new Vector2(5f, 0f);
        nameRt.offsetMax = new Vector2(-5f, 0f);
        var txtN = nameGo.GetComponent<TextMeshProUGUI>();
        txtN.fontSize = 18f;
        txtN.alignment = TextAlignmentOptions.Center;
        txtN.color = new Color(0.28f, 0.16f, 0.08f, 1f);

        // Số lượng
        var stockGo = new GameObject("TxtStock", typeof(RectTransform), typeof(Image), typeof(TextMeshProUGUI));
        stockGo.transform.SetParent(root.transform, false);
        var stockRt = stockGo.GetComponent<RectTransform>();
        stockRt.anchorMin = new Vector2(0.5f, 0.15f);
        stockRt.anchorMax = new Vector2(0.5f, 0.15f);
        stockRt.sizeDelta = new Vector2(70f, 26f);
        stockRt.anchoredPosition = Vector2.zero;
        var txtS = stockGo.GetComponent<TextMeshProUGUI>();
        txtS.fontSize = 18f;
        txtS.alignment = TextAlignmentOptions.Center;
        txtS.color = Color.white;

        return root;
    }

    private void RefreshAllStocks()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            if (_activeItems[i] != null && _activeItems[i].gameObject.activeSelf)
                _activeItems[i].RefreshStock();
        }
    }

    private Sprite GetDefaultFeedSprite(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        var spr = Resources.Load<Sprite>($"Icons/{itemId}") ?? Resources.Load<Sprite>($"Sprites/{itemId}");
        return spr;
    }

    private string GetFoodDisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return string.Empty;
        if (itemId.Contains("cow") || itemId.Contains("bo")) return "Cám Bò";
        if (itemId.Contains("pig") || itemId.Contains("heo")) return "Cám Heo";
        if (itemId.Contains("chicken") || itemId.Contains("ga")) return "Cám Gà";
        if (itemId.Contains("bapcai")) return "Bắp Cải";
        if (itemId.Contains("cachua")) return "Cà Chua";
        if (itemId.Contains("ngo") || itemId.Contains("corn")) return "Bắp Ngô";
        if (itemId.Contains("lua") || itemId.Contains("rice")) return "Lúa Mì";
        return itemId;
    }
}
