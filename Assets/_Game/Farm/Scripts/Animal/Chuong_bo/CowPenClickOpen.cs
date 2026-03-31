using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CowPenClickOpen : MonoBehaviour
{
    [SerializeField] private CowPenPopupUI cowPenPopupUI;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D targetCollider;

    private void Awake()
    {
        // tự lấy ref
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        Debug.Log("[CowPenClickOpen] Awake");
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
        {
            TryOpenCowPen(screenPos);
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

    // mở chuồng bò
    private void TryOpenCowPen(Vector2 screenPos)
    {
        if (SceneManager.GetSceneByName("SampleScene").isLoaded)
            return;

        if (IsPointerOverPopupUI(screenPos))
            return;

        if (mainCamera == null)
        {
            Debug.LogError("[CowPenClickOpen] mainCamera null");
            return;
        }

        if (targetCollider == null)
        {
            Debug.LogError("[CowPenClickOpen] targetCollider null");
            return;
        }

        if (cowPenPopupUI == null)
        {
            Debug.LogError("[CowPenClickOpen] cowPenPopupUI null");
            return;
        }

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = targetCollider.OverlapPoint(world2);
        Debug.Log("[CowPenClickOpen] World = " + world2 + " | Hit = " + hit);

        if (!hit)
            return;

        Debug.Log("[CowPenClickOpen] OPEN POPUP");
        cowPenPopupUI.OpenPopup();
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

            if (parentCanvas != null && parentCanvas.name == "Canvas_Popup")
                return true;
        }

        return false;
    }
}