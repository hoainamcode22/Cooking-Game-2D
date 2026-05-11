using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Village
{
    public class HouseOrderController : MonoBehaviour, IPointerClickHandler
    {
        // ── Public State ──────────────────────────────────────────────────────

        public int               HouseId      { get; private set; }
        public HouseOrderRuntime CurrentOrder { get; private set; }
        public OrderState        CurrentState { get; private set; } = OrderState.Idle;

        // ── Private ───────────────────────────────────────────────────────────

        private HouseOrderBubble myBubble;
        private Coroutine        cooldownRoutine;
        private Action<HouseOrderController> onCooldownComplete;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            myBubble = GetComponentInChildren<HouseOrderBubble>(true);

            if (myBubble == null)
                Debug.LogError($"[HOC] Awake — Không tìm thấy HouseOrderBubble trong children của '{gameObject.name}'. " +
                               "Đảm bảo OrderPopup2 (có HouseOrderBubble) là child của OrderAnchor bên trong house.");
        }

        // ── Initialization ────────────────────────────────────────────────────

        // Gọi từ PlacementManager sau khi Instantiate.
        // KHÔNG gọi Initialize(int) ở đây — RegisterHouse sẽ gọi nó với id đúng.
        // Chỉ đảm bảo myBubble được tìm trước khi RegisterHouse dùng nó.
        public void Initialize()
        {
            if (myBubble == null)
                myBubble = GetComponentInChildren<HouseOrderBubble>(true);

            if (myBubble != null && myBubble.transform.parent != null
                && myBubble.transform.parent != transform)
                myBubble.transform.parent.gameObject.SetActive(true);

            if (VillageOrderManager.Instance != null)
                VillageOrderManager.Instance.RegisterHouse(this);
            else
            {
                // Fallback: VillageOrderManager chưa có trong scene
                Initialize(GetInstanceID());
                if (myBubble != null) myBubble.Hide();
            }
        }

        public void Initialize(int id)
        {
            HouseId      = id;
            CurrentState = OrderState.Idle;

            // Tìm lại nếu Awake chưa kịp gán (trường hợp Instantiate runtime)
            if (myBubble == null)
                myBubble = GetComponentInChildren<HouseOrderBubble>(true);

            // Đảm bảo parent container của bubble đang active để Show() hoạt động
            if (myBubble != null && myBubble.transform.parent != null
                && myBubble.transform.parent != transform)
                myBubble.transform.parent.gameObject.SetActive(true);

            if (myBubble != null) myBubble.Hide();
            else Debug.LogWarning($"[HOC] Initialize — myBubble vẫn null trên '{gameObject.name}'.");
        }

        // ── Order Operations ──────────────────────────────────────────────────

        public void AssignOrder(HouseOrderRuntime order)
        {
            if (order == null || order.item1 == null)
            {
                Debug.LogWarning($"[HOC] AssignOrder SKIPPED '{gameObject.name}' — order or item1 null.");
                return;
            }

            CurrentOrder = order;
            CurrentState = OrderState.Active;

            Sprite icon2 = (order.HasSecondItem && order.item2 != null) ? order.item2.icon : null;
            if (myBubble != null) myBubble.Show(order.item1.icon, icon2);

            string items = order.HasSecondItem
                ? $"{order.item1.requiredAmount}x {order.item1.displayName} + {order.item2.requiredAmount}x {order.item2.displayName}"
                : $"{order.item1.requiredAmount}x {order.item1.displayName}";
            Debug.Log($"[HOC] Order active '{gameObject.name}': {items} → {order.rewardGold}g {order.rewardExp}xp");
        }

        public void StartCooldown(float duration, Action<HouseOrderController> onComplete)
        {
            CurrentOrder       = null;
            CurrentState       = OrderState.Cooldown;
            onCooldownComplete = onComplete;

            if (myBubble != null) myBubble.Hide();

            if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
            cooldownRoutine = StartCoroutine(CooldownRoutine(duration));
        }

        public void ClearOrder()
        {
            CurrentOrder = null;
            CurrentState = OrderState.Idle;
            if (myBubble != null) myBubble.Hide();
        }

        // ── Input — Path A: Physics (Collider/Collider2D) ─────────────────────

        private void OnMouseDown()
        {
            HandleClick("OnMouseDown");
        }

        // ── Input — Path B: EventSystem (Physics2DRaycaster + EventSystem) ────

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick("OnPointerClick");
        }

        // ── Shared Click Logic ────────────────────────────────────────────────

        private void HandleClick(string source)
        {
            if (HouseOrderPopupUI.IsOpen) return;

            if (CurrentState != OrderState.Active)
            {
                Debug.Log($"[HOC] ({source}) Ignored — State={CurrentState} (need Active)");
                return;
            }
            if (CurrentOrder == null)
            {
                Debug.Log($"[HOC] ({source}) Ignored — CurrentOrder is null");
                return;
            }

            if (FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle)
            {
                Debug.Log($"[HOC] ({source}) Ignored — InputLock " +
                          $"(seed={FarmInputLock.IsDraggingSeed} sickle={FarmInputLock.IsDraggingSickle})");
                return;
            }

            if (HouseOrderPopupUI.Instance == null)
            {
                Debug.LogError($"[HOC] ({source}) HouseOrderPopupUI.Instance is null.");
                return;
            }

            HouseOrderPopupUI.Instance.Open(CurrentOrder, this);
        }

        // ── Cooldown Coroutine ────────────────────────────────────────────────

        private IEnumerator CooldownRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            CurrentState    = OrderState.Idle;
            cooldownRoutine = null;
            Debug.Log($"[HOC] Cooldown finished — '{gameObject.name}'");
            onCooldownComplete?.Invoke(this);
        }
    }
}
