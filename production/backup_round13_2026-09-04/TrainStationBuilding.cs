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

    [Header("World Bubble — báo 'Tàu đã về' trên nóc ga")]
    [Tooltip("Sprite world_bubble_train_arrived.png — gán bằng Tools/Farm Game/Train/Setup Train World Assets")]
    [SerializeField] private Sprite arrivedBubbleSprite;
    [Tooltip("Độ cao bubble so với gốc ga (world unit)")]
    [SerializeField] private float bubbleHeight = 2.2f;

    private SpriteRenderer _arrivedBubble;

    private BoxCollider2D _col;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        EnsurePopupsExist();
    }

    void Start()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnStateChanged += HandleTrainStateChanged;
            SyncArrivedBubble(TrainManager.Instance.State);
        }
    }

    void OnDestroy()
    {
        if (TrainManager.Instance != null)
            TrainManager.Instance.OnStateChanged -= HandleTrainStateChanged;
    }

    private void HandleTrainStateChanged(TrainState s) => SyncArrivedBubble(s);

    /// <summary>Bubble 'Tàu đã về' chỉ hiện khi tàu thưởng đang vào ga / chờ thu.</summary>
    private void SyncArrivedBubble(TrainState s)
    {
        bool show = s == TrainState.RewardArriving || s == TrainState.RewardReadyToCollect;

        if (!show)
        {
            if (_arrivedBubble != null) _arrivedBubble.gameObject.SetActive(false);
            return;
        }

        if (!EnsureArrivedBubble()) return;
        _arrivedBubble.gameObject.SetActive(true);
    }

    private bool EnsureArrivedBubble()
    {
        if (_arrivedBubble != null) return true;

        Sprite sprite = arrivedBubbleSprite;
#if UNITY_EDITOR
        if (sprite == null)
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Export_Train_UI_Package/Sprites/world_bubble_train_arrived.png");
#endif
        if (sprite == null)
        {
            Debug.LogWarning("[Train] Chưa gán arrivedBubbleSprite — chạy Tools/Farm Game/Train/Setup Train World Assets rồi save scene.");
            return false;
        }

        var go = new GameObject("Bubble_TrainArrived");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, bubbleHeight, 0f);

        _arrivedBubble = go.AddComponent<SpriteRenderer>();
        _arrivedBubble.sprite = sprite;

        // Cùng sorting layer với ga, vẽ đè lên trên
        var stationSr = GetComponent<SpriteRenderer>();
        if (stationSr == null) stationSr = GetComponentInChildren<SpriteRenderer>();
        if (stationSr != null)
        {
            _arrivedBubble.sortingLayerID = stationSr.sortingLayerID;
            _arrivedBubble.sortingOrder   = stationSr.sortingOrder + 5;
        }
        else
        {
            _arrivedBubble.sortingOrder = 50;
        }

        // Chuẩn hoá bề rộng bubble ~1.4 world unit bất kể ảnh gốc 256 hay 1024px
        float w = sprite.bounds.size.x;
        if (w > 0.01f) go.transform.localScale = Vector3.one * (1.4f / w);

        go.AddComponent<TrainArrivedBubbleBob>();
        return true;
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

        if (FarmInputLock.BlockWorldInteraction) return;
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

        var state = TrainManager.Instance != null ? TrainManager.Instance.State : TrainState.WaitingForLoad;

        // 1. Package UI 6-state (Export_Train_UI_Package) — view đọc TrainManager
        var masterPopup = ExportTrainUIPackage.TrainStationMasterPopupUI.Instance
            ?? FindFirstObjectByType<ExportTrainUIPackage.TrainStationMasterPopupUI>(FindObjectsInactive.Include);

        if (masterPopup != null)
        {
            if (masterPopup.gameObject.activeSelf)
            {
                masterPopup.ClosePopup();
                return;
            }

            var procPopup = ExportTrainUIPackage.TrainProcessPopupUI.Instance
                ?? FindFirstObjectByType<ExportTrainUIPackage.TrainProcessPopupUI>(FindObjectsInactive.Include);

            switch (state)
            {
                case TrainState.ShipDeparting:
                case TrainState.Processing:
                    // Đang vận chuyển → popup timer + tăng tốc
                    if (procPopup != null) procPopup.OpenPopup();
                    else masterPopup.OpenPopup(ExportTrainUIPackage.TrainState.Processing);
                    break;

                case TrainState.RewardArriving:
                case TrainState.RewardReadyToCollect:
                    // Tàu đã về → popup nhận thưởng
                    if (procPopup != null && procPopup.gameObject.activeSelf) procPopup.ClosePopup();
                    masterPopup.OpenPopup(ExportTrainUIPackage.TrainState.RewardReadyToCollect);
                    break;

                default: // WaitingForLoad, RewardDeparting
                    masterPopup.OpenPopup(ExportTrainUIPackage.TrainState.WaitingForLoad);
                    break;
            }
            return;
        }

        // 2. Fallback popup cũ
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

/// <summary>Hiệu ứng nhấp nhô cho bubble world-space (tự gắn runtime, không cần prefab).</summary>
public class TrainArrivedBubbleBob : MonoBehaviour
{
    private Vector3 _basePos;
    private float   _seed;

    void OnEnable()
    {
        _basePos = transform.localPosition;
        _seed    = Random.Range(0f, 10f);
    }

    void Update()
    {
        transform.localPosition = _basePos + new Vector3(0f, Mathf.Sin((Time.time + _seed) * 3f) * 0.08f, 0f);
    }

    void OnDisable()
    {
        transform.localPosition = _basePos;
    }
}
