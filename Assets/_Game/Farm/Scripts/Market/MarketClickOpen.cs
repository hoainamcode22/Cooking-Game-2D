using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MarketClickOpen : MonoBehaviour
{
    [SerializeField] private MarketPopupUI marketPopupUI;
    [SerializeField] private MarketManager marketManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D targetCollider;

    private void Awake()
    {
        // tự lấy ref
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        if (marketManager == null)
            marketManager = MarketManager.Instance;
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
        {
            TryOpenMarket(screenPos);
        }
    }

    // lấy vị trí click / touch
    private bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
        screenPos = default;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }

    // mở chợ
    private void TryOpenMarket(Vector2 screenPos)
    {
        if (SceneManager.GetSceneByName("SampleScene").isLoaded)
            return;

        // Không mở khi Edit Mode đang bật
        if (EditModeManager.IsEditMode) return;

        if (FarmInputLock.BlockMapPan) return;

        // Không mở khi đang có popup khác mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        if (IsPointerOverPopupUI(screenPos))
            return;

        if (mainCamera == null)
        {
            Debug.LogError("[MarketClickOpen] mainCamera null");
            return;
        }

        if (targetCollider == null)
        {
            Debug.LogError("[MarketClickOpen] targetCollider null");
            return;
        }

        if (marketManager == null)
            marketManager = MarketManager.Instance;

        if (marketManager == null && marketPopupUI == null)
        {
            Debug.LogError("[MarketClickOpen] marketManager and marketPopupUI are null");
            return;
        }

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = targetCollider.OverlapPoint(world2);
        Debug.Log("[MarketClickOpen] World = " + world2 + " | Hit = " + hit);

        if (!hit)
            return;

        Debug.Log("[MarketClickOpen] OPEN POPUP");
        if (marketManager != null)
            marketManager.OpenMarketPopup();
        else
            marketPopupUI.OpenPopup();
    }

    // check có bấm vào UI popup không
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

            if (parentCanvas != null && (parentCanvas.name == "Canvas_Popup" || parentCanvas.name == "Canvas_MarketPopup"))
                return true;
        }

        return false;
    }
}
