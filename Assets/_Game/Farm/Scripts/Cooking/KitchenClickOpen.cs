using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class KitchenClickOpen : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D targetCollider;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
        {
            TryOpenCooking(screenPos);
        }
    }

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

    private void TryOpenCooking(Vector2 screenPos)
    {
        if (FarmInputLock.BlockWorldInteraction) return;
        if (EditModeManager.IsEditMode) return;
        if (FarmInputLock.BlockMapPan) return;

        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        // Cháº·n khi Ä‘ang báº¥m lÃªn UI popup farm (Canvas_Popup)
        if (IsPointerOverFarmPopupUI(screenPos))
            return;

        if (mainCamera == null || targetCollider == null)
            return;

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = targetCollider.OverlapPoint(world2);

        if (!hit)
            return;

        // Hit detected â€” BuildingInteractable.OnMouseDown() handles scene transition.
    }

    private bool IsPointerOverFarmPopupUI(Vector2 screenPos)
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
