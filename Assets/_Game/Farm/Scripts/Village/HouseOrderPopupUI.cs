using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Village
{

    public class HouseOrderPopupUI : MonoBehaviour
    {
        public static HouseOrderPopupUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Item row 1")]
        [SerializeField] private Image    img_item_1;
        [SerializeField] private TMP_Text txt_itemName_1;
        [SerializeField] private TMP_Text txt_amount_1;

        [Header("Item row 2  (row2Root hides row for single-item orders)")]
        [SerializeField] private GameObject row2Root;
        [SerializeField] private Image      img_item_2;
        [SerializeField] private TMP_Text   txt_itemName_2;
        [SerializeField] private TMP_Text   txt_amount_2;

        [Header("Rewards")]
        [SerializeField] private TMP_Text txt_rewardGold;
        [SerializeField] private TMP_Text txt_rewardExp;
        [SerializeField] private Image    icon_rewardGold;
        [SerializeField] private Image    icon_rewardExp;

        [Header("Deliver button")]
        [SerializeField] private Button   btn_deliver;
        [SerializeField] private TMP_Text txt_deliver;

        [Header("Close button")]
        [SerializeField] private Button btn_close;

        [Header("Deliver button colours")]
        [SerializeField] private Color colorCanDeliver    = new Color(0.22f, 0.78f, 0.35f);
        [SerializeField] private Color colorCannotDeliver = new Color(0.80f, 0.25f, 0.25f);

        [Header("Amount text colours")]
        [SerializeField] private Color colorAmountOk      = Color.white;
        [SerializeField] private Color colorAmountShort   = new Color(0.90f, 0.25f, 0.25f);

        // ── Runtime ───────────────────────────────────────────────────────────

        private HouseOrderRuntime    currentOrder;
        private HouseOrderController currentHouse;
        private bool popupInputLockHeld;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            bool startOpen = gameObject.activeSelf;
            Instance = this;
            IsOpen   = startOpen;

            // Panel phải start ACTIVE trong scene để Awake chạy và đăng ký Instance,
            // sau đó ẩn ngay để không lộ ra trước khi Open() được gọi.
            if (!startOpen) gameObject.SetActive(false);
        }

        private void Start()
        {
            if (btn_deliver != null)
            {
                btn_deliver.onClick.RemoveAllListeners();
                btn_deliver.onClick.AddListener(OnDeliverClicked);
                Debug.Log("[OrderPopup] Đã gắn sự kiện click cho btn_deliver trong Start!");
            }
            else
            {
                Debug.LogError("[OrderPopup] Start — btn_deliver là NULL! Kéo Button vào Inspector.");
            }
        }

        private void OnEnable()
        {
            if (btn_deliver != null)
            {
                btn_deliver.onClick.RemoveAllListeners();
                btn_deliver.onClick.AddListener(OnDeliverClicked);
                Debug.Log("[OrderPopup] btn_deliver listener registered (OnEnable).");
            }

            if (btn_close != null)
            {
                btn_close.onClick.RemoveAllListeners();
                btn_close.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            ReleasePopupInputBlock();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open(HouseOrderRuntime order, HouseOrderController house)
        {
            Debug.Log($"[OrderPopup] Open() — " +
                      $"house={(house != null ? house.gameObject.name : "NULL")}  " +
                      $"order={(order?.item1?.itemId ?? "NULL")}");

            if (order == null || house == null || order.item1 == null)
            {
                Debug.LogError("[OrderPopup] Open() aborted — order, house, or item1 is null.");
                return;
            }

            currentOrder = order;
            currentHouse = house;

            IsOpen = true;
            gameObject.SetActive(true);
            AcquirePopupInputBlock();

            DiagnoseButtonInput();
            Refresh();
        }

        public void Close()
        {
            Debug.Log("[OrderPopup] Close()");
            IsOpen = false;
            ReleasePopupInputBlock();
            gameObject.SetActive(false);
            currentOrder = null;
            currentHouse = null;
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        private void DiagnoseButtonInput()
        {
            bool hasEventSystem = EventSystem.current != null;
            if (!hasEventSystem)
                Debug.LogError("[OrderPopup] ★ KHÔNG CÓ EventSystem trong scene! " +
                               "UI buttons sẽ không bao giờ hoạt động. " +
                               "GameObject > UI > Event System để thêm vào.");

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var gr = canvas.GetComponent<GraphicRaycaster>();
                if (gr == null)
                    Debug.LogError($"[OrderPopup] ★ Canvas '{canvas.gameObject.name}' thiếu GraphicRaycaster! " +
                                   "Add Component → GraphicRaycaster vào Canvas đó.");
                else
                    Debug.Log($"[OrderPopup] GraphicRaycaster OK trên '{canvas.gameObject.name}'.");
            }
            else
            {
                Debug.LogError("[OrderPopup] ★ Popup không nằm trong Canvas nào!");
            }

            var groups = GetComponentsInParent<CanvasGroup>();
            foreach (var cg in groups)
            {
                if (!cg.interactable)
                    Debug.LogError($"[OrderPopup] ★ CanvasGroup '{cg.gameObject.name}' có interactable=FALSE — " +
                                   "buttons bên trong không click được!");
                if (!cg.blocksRaycasts)
                    Debug.LogWarning($"[OrderPopup] CanvasGroup '{cg.gameObject.name}' có blocksRaycasts=FALSE.");
            }

            if (btn_deliver == null)
            {
                Debug.LogError("[OrderPopup] ★ btn_deliver là NULL — chưa kéo Button vào Inspector!");
                return;
            }

            int persistentCount = btn_deliver.onClick.GetPersistentEventCount();
            Debug.Log($"[OrderPopup] btn_deliver — " +
                      $"interactable={btn_deliver.interactable}  " +
                      $"persistent_listeners={persistentCount}  " +
                      $"active={btn_deliver.gameObject.activeInHierarchy}  " +
                      $"enabled={btn_deliver.enabled}");

            if (!btn_deliver.gameObject.activeInHierarchy)
                Debug.LogError("[OrderPopup] ★ btn_deliver.gameObject không active trong hierarchy!");
            if (!btn_deliver.enabled)
                Debug.LogError("[OrderPopup] ★ btn_deliver component bị disabled!");

            var btnRect = btn_deliver.GetComponent<RectTransform>();
            if (btnRect != null)
                Debug.Log($"[OrderPopup] btn_deliver RectTransform — " +
                          $"position={btnRect.position}  size={btnRect.rect.size}");

            Debug.Log("[OrderPopup] Diagnostics done.");
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (currentOrder == null) return;

            var mgr = VillageOrderManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[OrderPopup] Refresh: VillageOrderManager.Instance null.");
                return;
            }

            ClearAllFields();

            int  owned1    = mgr.GetPlayerItemAmount(currentOrder.item1.itemId);
            int  required1 = currentOrder.item1.requiredAmount;
            bool enough1   = owned1 >= required1;

            SetImage(img_item_1,     currentOrder.item1.icon);
            SetText(txt_itemName_1,  currentOrder.item1.displayName);
            SetText(txt_amount_1,    $"{owned1} / {required1}");
            SetTextColor(txt_amount_1, enough1 ? colorAmountOk : colorAmountShort);

            bool hasTwo = currentOrder.HasSecondItem;

            if (row2Root != null)
                row2Root.SetActive(hasTwo);
            else
            {
                SetActive(img_item_2,     hasTwo);
                SetActive(txt_itemName_2, hasTwo);
                SetActive(txt_amount_2,   hasTwo);
            }

            if (hasTwo && currentOrder.item2 != null)
            {
                int  owned2    = mgr.GetPlayerItemAmount(currentOrder.item2.itemId);
                int  required2 = currentOrder.item2.requiredAmount;
                bool enough2   = owned2 >= required2;

                SetImage(img_item_2,    currentOrder.item2.icon);
                SetText(txt_itemName_2, currentOrder.item2.displayName);
                SetText(txt_amount_2,   $"{owned2} / {required2}");
                SetTextColor(txt_amount_2, enough2 ? colorAmountOk : colorAmountShort);
            }

            SetText(txt_rewardGold, currentOrder.rewardGold.ToString());
            SetText(txt_rewardExp,  currentOrder.rewardExp.ToString());

            bool canDeliver = mgr.HasEnoughForOrder(currentOrder);

            if (btn_deliver != null)
            {
                btn_deliver.interactable = true;
                var btnImg = btn_deliver.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = canDeliver ? colorCanDeliver : colorCannotDeliver;
            }

            SetText(txt_deliver, canDeliver ? "Giao" : "Thiếu hàng");

            Debug.Log($"[OrderPopup] Refresh — '{currentOrder.item1.itemId}' {owned1}/{required1}  canDeliver={canDeliver}");
        }

        // ── Deliver Handler ───────────────────────────────────────────────────

        private void OnDeliverClicked()
        {
            

            if (currentOrder == null)
            {
                Debug.LogError("[OrderPopup] OnDeliverClicked: currentOrder NULL.");
                return;
            }
            if (currentHouse == null)
            {
                Debug.LogError("[OrderPopup] OnDeliverClicked: currentHouse NULL.");
                return;
            }

            var mgr = VillageOrderManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[OrderPopup] OnDeliverClicked: VillageOrderManager NULL.");
                return;
            }

            // Bước 1: Kiểm tra an toàn – double-check kho còn đủ hàng không (tránh spam click)
            bool hasEnough = mgr.HasEnoughForOrder(currentOrder);
            Debug.Log($"[OrderPopup] Kiểm tra kho: HasEnough={hasEnough}");

            if (!hasEnough)
            {
                Refresh();   // Cập nhật lại màu nút / số lượng
                return;
            }

          
            Debug.Log($"Đã cộng {currentOrder.rewardGold} Vàng và {currentOrder.rewardExp} Exp");

            mgr.DeliverOrder(currentHouse);

            // Bước 5: Đóng popup
            Close();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        

        private void ClearAllFields()
        {
            SetText(txt_itemName_1, string.Empty);
            SetText(txt_amount_1,   string.Empty);
            SetText(txt_itemName_2, string.Empty);
            SetText(txt_amount_2,   string.Empty);
            SetText(txt_rewardGold, string.Empty);
            SetText(txt_rewardExp,  string.Empty);
            SetText(txt_deliver,    string.Empty);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private static void SetTextColor(TMP_Text label, Color color)
        {
            if (label != null) label.color = color;
        }

        private static void SetImage(Image img, Sprite sprite)
        {
            if (img == null) return;
            img.sprite  = sprite;
            img.enabled = sprite != null;
        }

        private static void SetActive(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        private void AcquirePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, true);

            if (popupInputLockHeld) return;
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }

        private void ReleasePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, false);

            if (!popupInputLockHeld) return;
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }

        
    }
    
}
