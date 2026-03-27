using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WarehouseClickOpen : MonoBehaviour
{
    [SerializeField] private WarehousePopupUI warehousePopupUI;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D targetCollider;

    private void Awake()
    {
        // tự lấy ref
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        Debug.Log("[WarehouseClickOpen] Awake");
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
        {
            TryOpenWarehouse(screenPos);
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

    // mở kho
    private void TryOpenWarehouse(Vector2 screenPos)
    {
        // chỉ chặn khi bấm vào UI popup
        if (IsPointerOverPopupUI(screenPos))
            return;

        if (mainCamera == null)
        {
            Debug.LogError("[WarehouseClickOpen] mainCamera null");
            return;
        }

        if (targetCollider == null)
        {
            Debug.LogError("[WarehouseClickOpen] targetCollider null");
            return;
        }

        if (warehousePopupUI == null)
        {
            Debug.LogError("[WarehouseClickOpen] warehousePopupUI null");
            return;
        }

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = targetCollider.OverlapPoint(world2);
        Debug.Log("[WarehouseClickOpen] World = " + world2 + " | Hit = " + hit);

        if (!hit)
            return;

        Debug.Log("[WarehouseClickOpen] OPEN POPUP");
        warehousePopupUI.OpenPopup();
    }

    // check có đang bấm vào UI popup không
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

            if (t.GetComponentInParent<Canvas>() != null)
            {
                Canvas parentCanvas = t.GetComponentInParent<Canvas>();

                if (parentCanvas != null && parentCanvas.name == "Canvas_Popup")
                    return true;
            }
        }

        return false;
    }
}