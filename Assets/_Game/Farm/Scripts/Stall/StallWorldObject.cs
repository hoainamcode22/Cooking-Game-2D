using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// OBJECT QUẦY HÀNG NGOÀI MAP (B2) — bấm vào để mở popup.
///
/// Bám đúng khuôn mẫu đang dùng trong dự án (`MarketClickOpen`, `WarehouseClickOpen`,
/// `KitchenClickOpen`): New Input System + <c>Collider2D.OverlapPoint</c>, KHÔNG dùng
/// <c>OnMouseDown</c>. Lý do phải theo: <c>OnMouseDown</c> không thấy chạm trên mobile
/// khi có nhiều camera, và nó bỏ qua hết các chốt chặn (edit mode, popup đang mở) mà
/// bốn màn hình kia đều tôn trọng — lệch khuôn là quầy hàng mở được xuyên qua popup khác.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StallWorldObject : MonoBehaviour
{
    [Header("Liên kết")]
    [SerializeField] private StallPopupUI popupUI;
    [SerializeField] private Camera       mainCamera;
    [SerializeField] private Collider2D   targetCollider;

    [Header("Điều kiện mở")]
    [Tooltip("Cấp tối thiểu để dùng quầy. 0 = mở ngay từ đầu.")]
    [SerializeField] private int requiredLevel = 0;

    [Tooltip("Tên các Canvas popup — bấm trúng chúng thì KHÔNG tính là bấm vào quầy.")]
    [SerializeField]
    private string[] popupCanvasNames = { "Canvas_Popup", "Canvas_MarketPopup", "Canvas_StallPopup" };

    private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>();

    private void Awake()
    {
        if (mainCamera == null)     mainCamera = Camera.main;
        if (targetCollider == null) targetCollider = GetComponent<Collider2D>();

        if (popupUI == null)
            popupUI = StallPopupUI.Instance ?? FindAnyObjectByType<StallPopupUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (TryGetPointerScreenPosition(out Vector2 screenPos))
            TryOpenStall(screenPos);
    }

    private void OnMouseUpAsButton()
    {
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        Vector2 pos;
#if ENABLE_INPUT_SYSTEM
        pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        pos = (Vector2)Input.mousePosition;
#endif
        TryOpenStall(pos);
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
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

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = (Vector2)Input.mousePosition;
            return true;
        }

        return false;
    }

    private void TryOpenStall(Vector2 screenPos)
    {
        if (FarmInputLock.IsCookingMode) return;
        // Minigame nấu ăn nạp chồng lên scene farm — lúc đó click thuộc về minigame.
        if (SceneManager.GetSceneByName("SampleScene").isLoaded) return;

        if (EditModeManager.IsEditMode) return;

        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return;

        if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            mainCamera = Camera.main;

        if (popupUI == null)
            popupUI = StallPopupUI.Instance ?? FindAnyObjectByType<StallPopupUI>(FindObjectsInactive.Include);

        if (popupUI == null || mainCamera == null) return;
        if (popupUI.IsOpen) return;

        if (IsPointerOverPopupUI(screenPos)) return;

        Vector3 world3 = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world3.x, world3.y);

        bool hit = false;
        var colliders = GetComponentsInChildren<Collider2D>();
        if (colliders != null && colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled && colliders[i].OverlapPoint(world2))
                {
                    hit = true;
                    break;
                }
            }
        }
        else if (targetCollider != null && targetCollider.OverlapPoint(world2))
        {
            hit = true;
        }

        if (!hit) return;

        if (requiredLevel > 0 && GetPlayerLevel() < requiredLevel)
        {
            Debug.Log($"[QuầyHàng] Cần đạt cấp {requiredLevel} mới dùng được quầy hàng.");
            return;
        }

        popupUI.OpenPopup();
    }

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        // Nếu không có popup nào đang mở thì không chặn
        if (PopupManager.Instance != null && !PopupManager.Instance.IsAnyPopupOpen())
            return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };

        RaycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastBuffer);

        for (int i = 0; i < RaycastBuffer.Count; i++)
        {
            Canvas parentCanvas = RaycastBuffer[i].gameObject.GetComponentInParent<Canvas>();
            if (parentCanvas == null) continue;

            if (popupCanvasNames == null) continue;

            for (int n = 0; n < popupCanvasNames.Length; n++)
            {
                if (parentCanvas.name == popupCanvasNames[n]) return true;
            }
        }

        return false;
    }
}
