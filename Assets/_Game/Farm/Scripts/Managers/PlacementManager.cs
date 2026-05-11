using System;
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

    // Key PlayerPrefs lưu danh sách công trình — dùng chung bởi PlotController.DebugClearData()
    public const string BuildingsSaveKey = "FARM_PLACED_BUILDINGS";

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Ghost Prefab")]
    public GameObject placementGhostPrefab;

    [Header("Grid Snap")]
    [SerializeField] private float gridSize = 50f;

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
                houseRenderer.sprite = prefabSR.sprite;
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

    /// <summary>Gắn vào Btn_Confirm. Đặt công trình thật xuống map, xóa Ghost.</summary>
    private void ConfirmPlacement()
    {
        if (!isValidPos) return;

        Vector3 pos = currentGhost.transform.position;
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

    /// <summary>Dọn dẹp sau Confirm hoặc Cancel. refund = true → hoàn lại tiền cho user.</summary>
    private void Cleanup(bool refund)
    {
        if (refund && currentItem != null)
        {
            if (currentItem.diamondPrice > 0)
                FarmEconomyManager.Instance.AddGems(currentItem.diamondPrice);
            else
                FarmEconomyManager.Instance.AddGold(currentItem.goldPrice);

            Debug.Log($"[PlacementManager] Hoàn tiền: " +
                      $"{(currentItem.diamondPrice > 0 ? $"{currentItem.diamondPrice} Kim Cương" : $"{currentItem.goldPrice} Vàng")}");
        }

        if (currentGhost != null) Destroy(currentGhost);

        currentGhost       = null;
        houseRenderer      = null;
        ringRenderer       = null;
        btnConfirm         = null;
        currentItem        = null;
        isPlacing          = false;
        IsPlacingNewObject = false;  // Mở khóa CameraController
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy vị trí chuột trong world-space rồi snap về lưới.
    /// Dùng legacy Input.mousePosition — hoạt động ổn định trong cả Editor lẫn Build.
    /// </summary>
    private Vector3 GetSnappedMousePos()
    {
        // Camera ở z = -10 nên depth = 10 để ra z = 0 trong world
        Vector3 mouse = Input.mousePosition;
        mouse.z = -Camera.main.transform.position.z;
        Vector3 world = Camera.main.ScreenToWorldPoint(mouse);
        world.z = 0f;

        return new Vector3(
            Mathf.Round(world.x / gridSize) * gridSize,
            Mathf.Round(world.y / gridSize) * gridSize,
            0f
        );
    }
}
