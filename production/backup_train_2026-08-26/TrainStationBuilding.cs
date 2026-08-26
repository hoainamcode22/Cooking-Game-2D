using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên GameObject gataulua (nhà ga, world-space).
/// Khi người chơi click vào nhà ga -> mở Popup Ga Tàu Toàn Cảnh (State 1 / 6) hoặc Popup Đang Vận Chuyển (State 4 / 5).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TrainStationBuilding : MonoBehaviour
{
    [SerializeField] private TrainProcessPopupUI processPopup;

    private BoxCollider2D _col;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        EnsurePopupsExist();
    }

    private void EnsurePopupsExist()
    {
        var master = ExportTrainUIPackage.TrainStationMasterPopupUI.Instance 
            ?? FindFirstObjectByType<ExportTrainUIPackage.TrainStationMasterPopupUI>(FindObjectsInactive.Include);

        if (master == null)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab");
            if (prefab != null)
            {
                var canvas = FindPopupCanvas();
                if (canvas != null)
                {
                    var instance = Instantiate(prefab, canvas.transform);
                    instance.name = "Popup_Train_MasterStation";
                    instance.SetActive(false);
                }
            }
#endif
        }
    }

    private Canvas FindPopupCanvas()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name.Contains("Popup") || c.name.Contains("UI"))
                return c;
        }
        return canvases.Length > 0 ? canvases[0] : null;
    }

    void Update()
    {
        bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        if (!clicked) return;
        if (FarmInputLock.BlockMapPan) return;
        if (Camera.main == null) return;

        Vector2 screenPos = InputBridge.PointerPosition;
        Vector2 worldPos  = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        if (_col == null || !_col.OverlapPoint(worldPos)) return;

        // Không mở khi Edit Mode đang bật
        if (EditModeManager.IsEditMode) return;

        // Không mở khi đang có popup khác mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        HandleClick();
    }

    private void HandleClick()
    {
        EnsurePopupsExist();

        // 1. Kiểm tra Package UI 6-State mới
        var masterPopup = ExportTrainUIPackage.TrainStationMasterPopupUI.Instance 
            ?? FindFirstObjectByType<ExportTrainUIPackage.TrainStationMasterPopupUI>(FindObjectsInactive.Include);

        if (masterPopup != null)
        {
            if (masterPopup.gameObject.activeSelf)
            {
                masterPopup.ClosePopup();
            }
            else
            {
                var procPopup = ExportTrainUIPackage.TrainProcessPopupUI.Instance
                    ?? FindFirstObjectByType<ExportTrainUIPackage.TrainProcessPopupUI>(FindObjectsInactive.Include);

                if (procPopup != null && procPopup.gameObject.activeInHierarchy && !procPopup.isArrived)
                {
                    procPopup.OpenPopup(procPopup.remainingTime);
                }
                else
                {
                    masterPopup.OpenPopup(masterPopup.currentState);
                }
            }
            return;
        }

        // 2. Fallback
        if (processPopup == null)
            processPopup = FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);

        if (processPopup != null)
        {
            if (processPopup.IsVisible)
                processPopup.Hide();
            else
            {
                float remaining = TrainManager.Instance != null ? TrainManager.Instance.TripRemainingTime : 0f;
                processPopup.Show(remaining);
            }
        }
    }
}
