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

    // ── [FIX QA P2] Chong quet toan scene moi lan cham vao ga ──────────────────
    // EnsurePopupsExist() chay ca trong HandleClick() nen MOI cu cham deu keo theo
    // 2 lan FindFirstObjectByType(FindObjectsInactive.Include) = 2 lan duyet toan scene.
    // Giai xong mot lan thi thoi; nhung van lam lai neu popup da giai bi huy (doi scene),
    // de doi scene khong lam hong ga vinh vien.
    private bool      _daTaoPopup;
    private Component _masterPopupDaGiai;

    [Header("World Bubble — báo 'Tàu đã về' trên nóc ga")]
    [Tooltip("Sprite world_bubble_train_arrived.png — gán bằng Tools/Farm Game/Train/Setup Train World Assets")]
    [SerializeField] private Sprite arrivedBubbleSprite;
    [Tooltip("Độ cao bubble so với gốc ga (world unit)")]
    [SerializeField] private float bubbleHeight = 2.2f;

    // ── [BUILD-FIX] Prefab popup cho BAN BUILD ──────────────────────────────────
    // EnsurePopupsExist() truoc day nam tron trong #if UNITY_EDITOR va dung AssetDatabase,
    // nen trong ban build KHONG popup nao duoc tao => bam vao ga khong ra gi.
    // AssetDatabase khong ton tai trong build, va cac prefab nay KHONG nam trong thu muc
    // Resources/, nen duong duy nhat an toan la tham chieu truc tiep qua [SerializeField]
    // (Sep keo 3 prefab vao day tren Inspector cua ga), co fallback Resources.Load neu sau
    // nay prefab duoc chep vao Resources/Train/.
    [Header("Popup prefabs — BAT BUOC gan de ban BUILD tao duoc popup")]
    [Tooltip("Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab")]
    [SerializeField] private GameObject prefabMasterPopup;
    [Tooltip("Assets/Export_Train_UI_Package/Prefabs/Popup_item_Train.prefab")]
    [SerializeField] private GameObject prefabItemPopup;
    [Tooltip("Assets/Export_Train_UI_Package/Prefabs/Popup_train.prefab")]
    [SerializeField] private GameObject prefabProcessPopup;

    private SpriteRenderer _arrivedBubble;

    private Collider2D _col;

    // [PERF F4.3 2026-09-17] Camera.main trong Update: no phai tim GameObject co tag
    // "MainCamera" dang bat, khong phai mot phep doc field. Cache lai, tu tim lai khi
    // tham chieu chet (doi scene / camera bi thay). Hanh vi khong doi.
    private Camera _cam;

    /// <summary>Camera chinh da cache; tu tim lai neu tham chieu da chet.</summary>
    private Camera Cam
    {
        get
        {
            if (_cam == null) _cam = Camera.main;
            return _cam;
        }
    }

    void Awake()
    {
        _cam = Camera.main;
        _col = GetComponent<Collider2D>();
        if (_col == null) _col = GetComponentInChildren<Collider2D>();
        if (_col == null) _col = gameObject.AddComponent<BoxCollider2D>();

        if (_col is BoxCollider2D boxCol && (boxCol.size.x < 0.2f || boxCol.size.y < 0.2f))
        {
            var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                boxCol.size = sr.sprite.bounds.size;
                boxCol.offset = sr.sprite.bounds.center;
            }
            else
            {
                boxCol.size = new Vector2(3f, 3f);
            }
        }
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
        // Da giai xong va popup van con song => khong quet lai.
        if (_daTaoPopup && _masterPopupDaGiai != null) return;

        // [VONG 6 - 06/09] Phai dung LayPopupThat(): prefab dang co 4 component MasterPopupUI di lac
        // tren Wagon_1..Wagon_4, neu chi dung Instance/FindFirstObjectByType thi bien 'master' co the
        // tro vao mot BAN DI LAC (mot toa tau) chu khong phai popup that => tuong "da co roi" va bo qua.
        // [BUILD-FIX] Bo #if UNITY_EDITOR: than chay ca trong build. AssetDatabase chi con la
        // duong du phong TRONG EDITOR (duoi #if), con build dung [SerializeField] / Resources.Load.
        var canvas = FindPopupCanvas();
        if (canvas == null) return;

        var master = ExportTrainUIPackage.TrainStationMasterPopupUI.LayPopupThat();
        if (master == null)
            TaoPopupNeuThieu(canvas, prefabMasterPopup,
                             "Train/Popup_Train_MasterStation",
                             "Assets/Export_Train_UI_Package/Prefabs/Popup_Train_MasterStation.prefab",
                             "Popup_Train_MasterStation");

        var itemPopup = ExportTrainUIPackage.TrainLoadPopupUI.Instance
            ?? FindFirstObjectByType<ExportTrainUIPackage.TrainLoadPopupUI>(FindObjectsInactive.Include);
        if (itemPopup == null)
            TaoPopupNeuThieu(canvas, prefabItemPopup,
                             "Train/Popup_item_Train",
                             "Assets/Export_Train_UI_Package/Prefabs/Popup_item_Train.prefab",
                             "Popup_item_Train");

        var procPopup = ExportTrainUIPackage.TrainProcessPopupUI.Instance
            ?? FindFirstObjectByType<ExportTrainUIPackage.TrainProcessPopupUI>(FindObjectsInactive.Include);
        if (procPopup == null)
            TaoPopupNeuThieu(canvas, prefabProcessPopup,
                             "Train/Popup_train",
                             "Assets/Export_Train_UI_Package/Prefabs/Popup_train.prefab",
                             "Popup_train");

        // Ghi nhan ket qua de lan cham sau khong phai quet lai. Neu popup bi huy (doi scene)
        // thi _masterPopupDaGiai ve null va cua thoat nhanh o dau ham tu dong mo lai.
        _masterPopupDaGiai = ExportTrainUIPackage.TrainStationMasterPopupUI.LayPopupThat();
        _daTaoPopup        = true;
    }

    /// <summary>
    /// [BUILD-FIX] Tao 1 popup tu prefab, theo thu tu an toan cho BUILD:
    /// ① tham chieu [SerializeField] → ② Resources.Load (neu prefab duoc chep vao Resources/)
    /// → ③ AssetDatabase (CHI trong Editor). Khong tim thay thi canh bao, khong nem loi.
    /// </summary>
    private void TaoPopupNeuThieu(Canvas canvas, GameObject prefabGan, string duongDanResources,
                                  string duongDanAsset, string tenObject)
    {
        GameObject prefab = prefabGan;

        if (prefab == null && !string.IsNullOrEmpty(duongDanResources))
            prefab = Resources.Load<GameObject>(duongDanResources);

#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(duongDanAsset);
#endif

        if (prefab == null)
        {
            Debug.LogWarning("[Train] Thieu prefab '" + tenObject + "'. Gan no vao TrainStationBuilding " +
                             "tren Inspector (hoac chep vao Resources/" + duongDanResources + ") " +
                             "de ban BUILD tao duoc popup.");
            return;
        }

        var instance = Instantiate(prefab, canvas.transform);
        instance.name = tenObject;
        instance.SetActive(false);
    }

    private Canvas FindPopupCanvas()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var c in canvases)
            if (c.name == "Canvas_Popup") return c;

        foreach (var c in canvases)
            if (c.name.Contains("Popup") && !c.name.Contains("Stall")) return c;

        foreach (var c in canvases)
            if (c.name.Contains("Popup") || c.name.Contains("UI")) return c;

        return canvases.Length > 0 ? canvases[0] : null;
    }

    void Update()
    {
        bool clicked = InputBridge.IsPointerDownThisFrame
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    || Input.GetMouseButtonDown(0);
        if (!clicked) return;

        // Chỉ chặn khi chuột/ngón tay đang bấm trên UI Canvas thực sự
        if (FarmInputLock.ConTroTrenUiThat()) return;
        if (FarmInputLock.BlockWorldInteraction) return;
        if (EditModeManager.IsEditMode) return;
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return;

        // [PERF F4.3] doc qua cache thay vi Camera.main.
        var cam = Cam;
        if (cam == null) return;

        Vector2 screenPos = InputBridge.PointerPosition;
        if (screenPos == Vector2.zero)
        {
            if (Mouse.current != null) screenPos = Mouse.current.position.ReadValue();
            else screenPos = (Vector2)Input.mousePosition;
        }

        Vector3 world3 = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        Vector2 worldPos = new Vector2(world3.x, world3.y);

        // [PERF F4.3] _col DA duoc cache trong Awake; nhanh nay chi la luoi an toan khi
        // tham chieu chet. Dung if long thay toan tu `??` — `??` tren UnityEngine.Object
        // BO QUA phep so sanh null cua Unity nen co the tra ve mot collider da bi Destroy.
        if (_col == null)
        {
            _col = GetComponent<Collider2D>();
            if (_col == null) _col = GetComponentInChildren<Collider2D>();
        }
        if (_col == null || !_col.OverlapPoint(worldPos)) return;

        HandleClick();
    }

    private void OnMouseDown()
    {
        // [VONG 3 - 06/09] Log chan doan '[Train]': chi in khi con tro THUC SU cham collider
        // cua nha ga (OnMouseDown chi no khi do), nen khong spam Console.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup)
        {
            Debug.Log($"[Train] Click GA bi chan tai cong BlockWorldClickBySceneOrPopup. Popup dang mo = '{PopupManager.TenPopupDangMo()}'");
            return;
        }
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            Debug.Log($"[Train] Click GA bi chan tai cong IsAnyPopupOpen. Popup dang mo = '{PopupManager.TenPopupDangMo()}'");
            return;
        }
        if (EditModeManager.IsEditMode)
        {
            Debug.Log("[Train] Click GA bi chan: dang bat Edit Mode.");
            return;
        }

        Debug.Log("[Train] Click GA HOP LE (OnMouseDown) -> goi HandleClick()");
        HandleClick();
    }

    private void HandleClick()
    {
        EnsurePopupsExist();

        var state = TrainManager.Instance != null ? TrainManager.Instance.State : TrainState.WaitingForLoad;

        // 1. Package UI 6-state (Export_Train_UI_Package) — view đọc TrainManager
        // [VONG 6 - 06/09] LayPopupThat() luon tra ve popup NGOAI CUNG, khong bao gio tra ve mot
        // component di lac tren toa tau (thu pham cua vu "3 popup de nhau").
        var masterPopup = ExportTrainUIPackage.TrainStationMasterPopupUI.LayPopupThat();

        Debug.Log($"[Train] HandleClick: TrainState={state} | masterPopup={(masterPopup != null ? masterPopup.name : "NULL (khong tim thay Popup_Train_MasterStation trong scene!)")}");

        if (masterPopup != null)
        {
            if (masterPopup.gameObject.activeSelf)
            {
                Debug.Log("[Train] Popup master DANG MO san -> click nay dong popup lai (toggle).");
                masterPopup.ClosePopup();
                return;
            }

            // [VONG 8 - 06/09] CONG CAP 3 dat SAU nhanh toggle-dong o tren: neu popup dang mo thi
            // click nay la de DONG popup, chan cho do se lam popup mo roi khong dong duoc nua.
            // TrainGateAccess tu hien thong bao "Tau hoa mo o cap 3 nhe!" khi chan.
            if (!TrainGateAccess.CanOpenOrWarn()) return;

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
                    Debug.Log("[Train] Nhanh MAC DINH -> mo popup ga tau (WaitingForLoad).");
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
                // [VONG 8 - 06/09] Cong cap 3 cho CA duong fallback popup cu, neu khong nguoi choi
                // cap 1 van vao duoc tau bang duong nay khi popup package chua co trong scene.
                if (!TrainGateAccess.CanOpenOrWarn()) return;

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
