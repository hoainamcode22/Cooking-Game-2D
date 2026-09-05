using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
        if (FarmInputLock.BlockWorldInteraction) return;
        if (SceneManager.GetSceneByName("SampleScene").isLoaded)
            return;

        // Không mở khi Edit Mode đang bật
        if (EditModeManager.IsEditMode) return;

        // Không mở khi đang có popup khác mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        // chỉ chặn khi bấm vào UI popup
        if (IsPointerOverPopupUI(screenPos))
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        if (warehousePopupUI == null)
            warehousePopupUI = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);

        if (mainCamera == null || targetCollider == null || warehousePopupUI == null)
            return;

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = targetCollider.OverlapPoint(world2);
        if (!hit)
            return;

        warehousePopupUI.OpenPopup();
    }

    // check có đang bấm vào UI popup không
    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            var hitGO = results[i].gameObject;
            if (hitGO == null) continue;

            // Nếu không có popup nào đang mở thì không chặn
            if (PopupManager.Instance != null && !PopupManager.Instance.IsAnyPopupOpen())
                continue;

            Canvas parentCanvas = hitGO.GetComponentInParent<Canvas>();
            if (parentCanvas != null && (parentCanvas.name == "Canvas_Popup" || parentCanvas.name == "Canvas_MarketPopup" || parentCanvas.name == "Canvas_StallPopup"))
            {
                return true;
            }
        }

        return false;
    }
}
