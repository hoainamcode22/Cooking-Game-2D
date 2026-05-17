using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý luồng đặt công trình / trang trí lên map.
/// Luồng: Shop trừ tiền → StartPlacingNewObject() → User kéo Ghost → V (xác nhận) / X (hủy + hoàn tiền).
/// </summary>
public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    /// <summary>CameraController đọc flag này để block pan khi user đang bưng vật phẩm.</summary>
    public static bool IsPlacingNewObject { get; private set; }

    /// <summary>True khi đang trong luồng di chuyển công trình cũ (Edit Mode).</summary>
    public bool IsEditingBuilding => currentlyEditingBuilding != null;

    // Key PlayerPrefs lưu danh sách công trình — dùng chung bởi PlotController.DebugClearData()
    public const string BuildingsSaveKey = "FARM_PLACED_BUILDINGS";

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Ghost Prefab")]
    public GameObject placementGhostPrefab;

    [Header("Grid Snap")]
    public UnityEngine.Grid mapGrid;
    [SerializeField] private float gridSize = 50f; // fallback khi mapGrid chưa gán

    [Header("Grid Footprint")]
    public float  footprintPadding = 1.3f;  // nhân thêm vào diện tích thảm để dễ phát hiện chồng lấn
    public Sprite footprintSprite;          // Sprite lưới dùng chung cho Ghost và tất cả building footprint

    [Header("Validation")]
    public  LayerMask obstacleLayer;
    [SerializeField] private Vector2 collisionCheckSize = new(45f, 45f);

    // ── Runtime state ────────────────────────────────────────────────────────

    private bool              isPlacing;
    private GameObject        currentGhost;
    private SpriteRenderer    houseRenderer;
    private SpriteRenderer    ringRenderer;
    private Button            btnConfirm;
    private PlaceableItemData currentItem;
    private bool              isValidPos;

    // Danh sách runtime của các công trình đã đặt (đồng bộ với PlayerPrefs)
    private readonly List<BuildingEntry> placedBuildings = new();

    // ── Edit Mode state ───────────────────────────────────────────────────────
    private EditableBuilding currentlyEditingBuilding;
    private Vector3          originalEditPosition;

    // ── Ghost footprint ───────────────────────────────────────────────────────
    private Transform footprintTransform; // "Grid_Footprint" child trong Ghost

    // ── Serializable helpers ─────────────────────────────────────────────────

    [Serializable]
    private class BuildingEntry
    {
        public string itemId;
        public float  x, y;
    }

    [Serializable]
    private class BuildingsSave
    {
        public List<BuildingEntry> list = new();
    }

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        LoadBuildings();
    }

    private void Update()
    {
        if (!isPlacing || currentGhost == null) return;

        // Chỉ cập nhật vị trí Ghost khi con trỏ KHÔNG đang đè lên UI (nút V / X)
        // → Tránh Ghost trượt đi khi user bấm xác nhận
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            currentGhost.transform.position = GetSnappedMousePos();
        }

        // Kiểm tra va chạm liên tục
        isValidPos = !Physics2D.OverlapBox(
            currentGhost.transform.position,
            collisionCheckSize,
            0f,
            obstacleLayer
        );

        // Đổi màu Selection_Ring theo kết quả validation
        if (ringRenderer != null)
            ringRenderer.color = isValidPos
                ? new Color(0f, 1f, 0f, 0.5f)   // Xanh = chỗ trống
                : new Color(1f, 0f, 0f, 0.5f);  // Đỏ   = bị chặn

        // Chỉ cho bấm V khi vị trí hợp lệ
        if (btnConfirm != null)
            btnConfirm.interactable = isValidPos;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ ShopItemUI ngay sau khi trừ tiền thành công.
    /// Đẻ ra Ghost, gán sprite đúng vật phẩm, tự bind nút V / X.
    /// </summary>
    public void StartPlacingNewObject(PlaceableItemData itemData)
    {
        if (itemData == null || itemData.prefabToBuild == null)
        {
            Debug.LogWarning("[PlacementManager] itemData hoặc prefabToBuild bị null.");
            return;
        }

        // Hủy ghost cũ nếu có (trường hợp gọi đè)
        if (currentGhost != null) Destroy(currentGhost);

        currentItem  = itemData;
        currentGhost = Instantiate(placementGhostPrefab, GetSnappedMousePos(), Quaternion.identity);

        // ── Tìm SpriteRenderer của ngôi nhà (bỏ qua Selection_Ring) ──
        foreach (SpriteRenderer sr in currentGhost.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name == "Selection_Ring") continue;
            houseRenderer = sr;
            break;
        }

        // Gán sprite từ prefab thật để Ghost trông đúng món vừa mua
        if (houseRenderer != null)
        {
            SpriteRenderer prefabSR = itemData.prefabToBuild.GetComponentInChildren<SpriteRenderer>();
            if (prefabSR != null)
            {
                houseRenderer.sprite = prefabSR.sprite;
                // Điều chỉnh thảm xanh khớp footprint công trình
                SetupFootprint(prefabSR.sprite);
            }
        }

        // ── Tìm Selection_Ring ──
        Transform ringT = currentGhost.transform.Find("Selection_Ring");
        if (ringT != null)
            ringRenderer = ringT.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning("[PlacementManager] Không tìm thấy 'Selection_Ring' trong Ghost prefab.");

        // ── Tự động bind nút V (Confirm) và X (Cancel) ──
        foreach (Button btn in currentGhost.GetComponentsInChildren<Button>(true))
        {
            if (btn.name == "Btn_Confirm")
            {
                btnConfirm = btn;
                btn.onClick.AddListener(ConfirmPlacement);
            }
            else if (btn.name == "Btn_Cancel")
            {
                btn.onClick.AddListener(CancelPlacement);
            }
        }

        isPlacing          = true;
        IsPlacingNewObject = true;  // CameraController tự khóa pan
        Debug.Log($"[PlacementManager] Bắt đầu đặt: {itemData.itemName}");
    }

    // ── Edit Building ────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ EditableBuilding.OnMouseDown khi Edit Mode đang bật.
    /// Ẩn công trình gốc, spawn Ghost tại vị trí hiện tại, cho phép kéo thả như mua đồ mới.
    /// </summary>
    public void StartEditBuilding(EditableBuilding target)
    {
        if (target == null) return;

        // Hủy ghost cũ nếu đang có (tránh gọi đè)
        if (currentGhost != null) Destroy(currentGhost);

        currentlyEditingBuilding = target;
        originalEditPosition     = target.transform.position;

        // Ẩn công trình gốc — Ghost đóng vai trò "placeholder" trong khi kéo
        target.gameObject.SetActive(false);

        // Spawn Ghost tại đúng vị trí công trình
        currentGhost = Instantiate(placementGhostPrefab, originalEditPosition, Quaternion.identity);

        // ── Gán sprite từ công trình gốc vào Ghost ──
        foreach (SpriteRenderer sr in currentGhost.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name == "Selection_Ring") continue;
            houseRenderer = sr;
            break;
        }

        if (houseRenderer != null)
        {
            SpriteRenderer targetSR = target.GetComponentInChildren<SpriteRenderer>(true);
            if (targetSR != null)
            {
                houseRenderer.sprite = targetSR.sprite;
                // Điều chỉnh thảm xanh khớp footprint công trình
                SetupFootprint(targetSR.sprite);
            }
        }

        // ── Tìm Selection_Ring ──
        Transform ringT = currentGhost.transform.Find("Selection_Ring");
        if (ringT != null)
            ringRenderer = ringT.GetComponent<SpriteRenderer>();

        // ── Bind nút V / X ──
        foreach (Button btn in currentGhost.GetComponentsInChildren<Button>(true))
        {
            if (btn.name == "Btn_Confirm")
            {
                btnConfirm = btn;
                btn.onClick.AddListener(ConfirmPlacement);
            }
            else if (btn.name == "Btn_Cancel")
            {
                btn.onClick.AddListener(CancelPlacement);
            }
        }

        isPlacing          = true;
        IsPlacingNewObject = true;

        // Hiệu ứng nhấc lên: chỉ tác động visual, footprint giữ nguyên mặt đất
        if (houseRenderer != null)
            StartCoroutine(AnimatePickup(houseRenderer.transform, footprintTransform));

        Debug.Log($"[PlacementManager] Edit: bắt đầu di chuyển '{target.name}'");
    }

    /// <summary>
    /// Nhấc visual lên (scale ×1.1, Y +30).
    /// footprintToFreeze được giữ nguyên localScale mỗi frame
    /// để thảm xanh không bị kéo theo khi visual là root hoặc parent của nó.
    /// </summary>
    private IEnumerator AnimatePickup(Transform visual, Transform footprintToFreeze = null)
    {
        Vector3 startScale = visual.localScale;
        Vector3 startPos   = visual.localPosition;
        Vector3 endScale   = startScale * 1.1f;
        Vector3 endPos     = startPos + new Vector3(0f, 30f, 0f);

        // Ghi nhớ cả scale lẫn localPosition của footprint — đóng băng hoàn toàn trong lúc nhấc
        Vector3 frozenScale = footprintToFreeze != null ? footprintToFreeze.localScale    : Vector3.one;
        Vector3 frozenPos   = footprintToFreeze != null ? footprintToFreeze.localPosition : Vector3.zero;

        float duration = 0.15f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            float t      = elapsed / duration;
            float smooth = 1f - (1f - t) * (1f - t); // ease-out quad
            visual.localScale    = Vector3.LerpUnclamped(startScale, endScale, smooth);
            visual.localPosition = Vector3.LerpUnclamped(startPos,   endPos,   smooth);

            // Ép footprint về đúng vị trí và scale mỗi frame — thảm xanh không bay theo
            if (footprintToFreeze != null)
            {
                footprintToFreeze.localScale    = frozenScale;
                footprintToFreeze.localPosition = frozenPos;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        visual.localScale    = endScale;
        visual.localPosition = endPos;

        if (footprintToFreeze != null)
        {
            footprintToFreeze.localScale    = frozenScale;
            footprintToFreeze.localPosition = frozenPos;
        }
    }

    /// <summary>
    /// Tìm "Grid_Footprint" trong Ghost hiện tại và scale nó khớp với số ô lưới mà sprite chiếm.
    /// Dùng mapGrid.cellSize nếu đã gán, fallback về gridSize.
    /// </summary>
    private void SetupFootprint(Sprite sprite)
    {
        footprintTransform = currentGhost.transform.Find("Grid_Footprint");
        if (footprintTransform == null || sprite == null) return;

        float cellW = (mapGrid != null && mapGrid.cellSize.x > 0f) ? mapGrid.cellSize.x : gridSize;
        float cellH = (mapGrid != null && mapGrid.cellSize.y > 0f) ? mapGrid.cellSize.y : gridSize;

        // sprite.bounds.size đã là world units (= pixel / PPU)
        Vector2 spriteSize = sprite.bounds.size;
        float scaleX = Mathf.Max(1f, Mathf.Round(spriteSize.x / cellW)) * footprintPadding;
        float scaleY = Mathf.Max(1f, Mathf.Round(spriteSize.y / cellH)) * footprintPadding;

        footprintTransform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Gán sprite lưới chung để Ghost footprint trông giống hệt building footprint
        if (footprintSprite != null)
        {
            SpriteRenderer fpSR = footprintTransform.GetComponent<SpriteRenderer>();
            if (fpSR != null) fpSR.sprite = footprintSprite;
        }

        // Luôn hiện footprint ngay sau khi setup — prefab có thể để inactive mặc định
        footprintTransform.gameObject.SetActive(true);
    }

    /// <summary>
    /// EditModeManager gọi để bật/tắt thảm xanh của Ghost đang hoạt động.
    /// Dùng khi Edit Mode được toggle trong lúc Ghost đã tồn tại trên scene.
    /// </summary>
    public void SetGhostFootprintActive(bool state)
    {
        if (footprintTransform != null)
            footprintTransform.gameObject.SetActive(state);
    }

    // ── Xác nhận & Hủy ──────────────────────────────────────────────────────

    // Tên các object con vật bên trong prefab chuồng
    private static readonly string[] AnimalChildNames = { "bonam1", "ga", "heo" };

    /// <summary>
    /// Sau khi Instantiate chuồng, đảm bảo object con vật hiển thị đúng:
    /// SetActive(true) + SortingLayer và OrderInLayer khớp chuồng + offset.
    /// </summary>
    private static void FixAnimalVisibility(GameObject buildingObj)
    {
        // Lấy SpriteRenderer gốc của chuồng (bỏ qua các SR con)
        SpriteRenderer buildingSR = buildingObj.GetComponent<SpriteRenderer>();
        if (buildingSR == null)
            buildingSR = buildingObj.GetComponentInChildren<SpriteRenderer>(true);

        string sortingLayerName = buildingSR != null ? buildingSR.sortingLayerName : "Default";
        int    baseOrder        = buildingSR != null ? buildingSR.sortingOrder      : 0;

        foreach (string animalName in AnimalChildNames)
        {
            Transform t = buildingObj.transform.Find(animalName);
            if (t == null) continue;

            t.gameObject.SetActive(true);

            foreach (SpriteRenderer sr in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder     = baseOrder + 10;
            }
        }
    }

    /// <summary>Gắn vào Btn_Confirm. Đặt công trình xuống map (mới hoặc edit), xóa Ghost.</summary>
    private void ConfirmPlacement()
    {
        if (!isValidPos) return;

        Vector3 pos = currentGhost.transform.position;

        // ── Nhánh Edit Mode: di chuyển công trình cũ sang vị trí mới ──
        if (currentlyEditingBuilding != null)
        {
            currentlyEditingBuilding.transform.position = pos;
            currentlyEditingBuilding.gameObject.SetActive(true);

            // Cập nhật vị trí trong save data (khớp theo tọa độ cũ vì grid đảm bảo không trùng)
            foreach (BuildingEntry e in placedBuildings)
            {
                if (Mathf.Approximately(e.x, originalEditPosition.x) &&
                    Mathf.Approximately(e.y, originalEditPosition.y))
                {
                    e.x = pos.x;
                    e.y = pos.y;
                    break;
                }
            }
            SaveBuildings();

            Debug.Log($"[PlacementManager] Di chuyển '{currentlyEditingBuilding.name}' → {pos}");
            Cleanup(refund: false);
            return;
        }

        // ── Nhánh đặt mới (luồng cũ từ Shop) ──
        GameObject spawnedObj = Instantiate(currentItem.prefabToBuild, pos, Quaternion.identity);
        Debug.Log($"[PlacementManager] Đặt thành công: {currentItem.itemName} tại {pos}");

        FixAnimalVisibility(spawnedObj);

        // Tắt bất kỳ placeholder cùng tên trong scene để tránh object thừa
        DisablePlaceholderInScene(currentItem.prefabToBuild.name, spawnedObj);

        // Khởi tạo house bubble — chỉ clone này được truyền vào RegisterHouse
        var house = spawnedObj.GetComponentInChildren<Village.HouseOrderController>(true);
        if (house != null) house.Initialize();

        // Khởi tạo sạch nếu là ô đất (tránh load dữ liệu cũ trùng plotId)
        var plot = spawnedObj.GetComponentInChildren<PlotController>(true);
        if (plot != null) plot.InitializeAsNew();

        // Lưu vào PlayerPrefs
        placedBuildings.Add(new BuildingEntry { itemId = currentItem.itemID, x = pos.x, y = pos.y });
        SaveBuildings();

        Cleanup(refund: false);
    }

    // ── Building Persistence ─────────────────────────────────────────────────

    private void SaveBuildings()
    {
        var save = new BuildingsSave { list = placedBuildings };
        PlayerPrefs.SetString(BuildingsSaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void LoadBuildings()
    {
        if (!PlayerPrefs.HasKey(BuildingsSaveKey)) return;
        string json = PlayerPrefs.GetString(BuildingsSaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        BuildingsSave save = JsonUtility.FromJson<BuildingsSave>(json);
        if (save?.list == null) return;

        foreach (var entry in save.list)
        {
            PlaceableItemData itemData = FindItemById(entry.itemId);
            if (itemData == null || itemData.prefabToBuild == null)
            {
                Debug.LogWarning($"[PlacementManager] LoadBuildings: không tìm thấy itemId='{entry.itemId}' trong ShopManager.");
                continue;
            }

            Vector3 pos = new(entry.x, entry.y, 0f);
            GameObject obj = Instantiate(itemData.prefabToBuild, pos, Quaternion.identity);

            FixAnimalVisibility(obj);

            // Tắt placeholder cùng tên còn sót trong scene
            DisablePlaceholderInScene(itemData.prefabToBuild.name, obj);

            var house = obj.GetComponentInChildren<Village.HouseOrderController>(true);
            if (house != null) house.Initialize();

            placedBuildings.Add(entry);
            Debug.Log($"[PlacementManager] Loaded '{entry.itemId}' tại {pos}");
        }
    }

    // Tìm tất cả object trong scene có tên trùng prefabName (không có "(Clone)")
    // và SetActive(false) để tránh object thừa song song với clone vừa tạo.
    private void DisablePlaceholderInScene(string prefabName, GameObject skipObj)
    {
        var allHOCs = FindObjectsByType<Village.HouseOrderController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var hoc in allHOCs)
        {
            if (hoc.gameObject == skipObj) continue;
            if (hoc.gameObject.name == prefabName)          // exact name = chưa có "(Clone)"
            {
                hoc.gameObject.SetActive(false);
                Debug.Log($"[PlacementManager] Đã tắt scene placeholder: '{prefabName}'");
            }
        }
    }

    /// <summary>Xóa toàn bộ dữ liệu nhà/công trình đã đặt khỏi PlayerPrefs.</summary>
    public void ClearBuildingData()
    {
        PlayerPrefs.DeleteKey(BuildingsSaveKey);
        PlayerPrefs.Save();
        placedBuildings.Clear();
        Debug.Log("[PlacementManager] ClearBuildingData: đã xóa toàn bộ dữ liệu nhà đã đặt.");
    }

    private PlaceableItemData FindItemById(string itemId)
    {
        if (ShopManager.Instance == null) return null;

        foreach (var item in ShopManager.Instance.buildingList)
            if (item is PlaceableItemData p && p.itemID == itemId) return p;

        foreach (var item in ShopManager.Instance.decorList)
            if (item is PlaceableItemData p && p.itemID == itemId) return p;

        return null;
    }

    /// <summary>Gắn vào Btn_Cancel (và có thể gọi từ ngoài, vd: phím Escape). Hoàn tiền + xóa Ghost.</summary>
    public void CancelPlacement() => Cleanup(refund: true);

    // ── Nội bộ ──────────────────────────────────────────────────────────────

    /// <summary>Dọn dẹp sau Confirm hoặc Cancel. refund = true → hoàn tiền / trả building về cũ.</summary>
    private void Cleanup(bool refund)
    {
        if (refund)
        {
            if (currentlyEditingBuilding != null)
            {
                // Cancel Edit Mode: trả công trình về vị trí gốc và hiện lại
                currentlyEditingBuilding.transform.position = originalEditPosition;
                currentlyEditingBuilding.gameObject.SetActive(true);
                Debug.Log($"[PlacementManager] Hủy edit, trả '{currentlyEditingBuilding.name}' về {originalEditPosition}");
            }
            else if (currentItem != null)
            {
                // Cancel đặt mới: hoàn tiền
                if (currentItem.diamondPrice > 0)
                    FarmEconomyManager.Instance.AddGems(currentItem.diamondPrice);
                else
                    FarmEconomyManager.Instance.AddGold(currentItem.goldPrice);

                Debug.Log($"[PlacementManager] Hoàn tiền: " +
                          $"{(currentItem.diamondPrice > 0 ? $"{currentItem.diamondPrice} Kim Cương" : $"{currentItem.goldPrice} Vàng")}");
            }
        }

        // Safety net: nếu building vẫn đang bị ẩn (chưa được xử lý ở trên), phục hồi ngay
        // — bảo vệ khỏi mọi path tắt bất thường (force-quit, exception, v.v.)
        if (currentlyEditingBuilding != null && !currentlyEditingBuilding.gameObject.activeSelf)
        {
            currentlyEditingBuilding.transform.position = originalEditPosition;
            currentlyEditingBuilding.gameObject.SetActive(true);
            Debug.LogWarning($"[PlacementManager] Safety restore: '{currentlyEditingBuilding.name}' về {originalEditPosition}");
        }

        if (currentGhost != null) Destroy(currentGhost);

        currentGhost             = null;
        houseRenderer            = null;
        ringRenderer             = null;
        btnConfirm               = null;
        currentItem              = null;
        currentlyEditingBuilding = null;
        footprintTransform       = null;
        isPlacing                = false;
        IsPlacingNewObject       = false;  // Mở khóa CameraController
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy vị trí chuột trong world-space rồi snap về tâm ô lưới gần nhất.
    /// Ưu tiên dùng UnityEngine.Grid (mapGrid) nếu đã gán trong Inspector;
    /// fallback về Mathf.Round thủ công nếu chưa gán.
    /// </summary>
    private Vector3 GetSnappedMousePos()
    {
        // Bước 1 – World pos của chuột / ngón tay
        Vector3 mouse = Input.mousePosition;
        mouse.z = -Camera.main.transform.position.z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouse);
        worldPos.z = 0f;

        // Bước 2-3 – Snap qua Unity Grid (chính xác hơn, tôn trọng offset & cell size)
        if (mapGrid != null)
        {
            Vector3Int cellPos   = mapGrid.WorldToCell(worldPos);
            Vector3    snapped   = mapGrid.GetCellCenterWorld(cellPos);
            snapped.z = 0f;
            return snapped;
        }

        // Fallback – snap thủ công khi mapGrid chưa được gán
        return new Vector3(
            Mathf.Round(worldPos.x / gridSize) * gridSize,
            Mathf.Round(worldPos.y / gridSize) * gridSize,
            0f
        );
    }
}