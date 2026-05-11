using UnityEngine;
using UnityEngine.UI;

public class SeedPopupController : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject seedItemPrefab;

    [Header("Container")]
    [SerializeField] private Transform content;

    [Header("Data List")]
    [SerializeField] private CropData[] cropDataList;

    [Header("Item Size")]
    [SerializeField] private float itemPreferredWidth  = 120f;
    [SerializeField] private float itemPreferredHeight = 150f;

    [Header("Click Outside để đóng")]
    // Kéo RectTransform của khung bảng hạt giống vào đây trong Inspector
    [SerializeField] private RectTransform popupRect;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        SpawnAllItems();
    }

    private void Update()
    {
        // Không xử lý click-outside trong lúc đang kéo hạt giống
        if (FarmInputLock.IsDraggingSeed) return;

        if (!Input.GetMouseButtonDown(0)) return;

        if (popupRect == null) return;

        // Nếu click NGOÀI vùng popup → đóng bảng
        bool isInsidePopup = RectTransformUtility.RectangleContainsScreenPoint(
            popupRect, Input.mousePosition, null);

        if (!isInsidePopup)
        {
            // Gọi đúng hàm chuẩn của FarmUIManager thay vì SetActive(false) trực tiếp
            // HidePlantSelectPopup() sẽ tắt cả 2 popup VÀ clear FarmInputLock.IsSeedPopupOpen
            // → Map không còn bị khóa sau khi đóng
            FarmUIManager.Instance?.HidePlantSelectPopup();
        }
    }

    // ── Spawn items ───────────────────────────────────────────────────────────

    public void SpawnAllItems()
    {
        if (content == null)
        {
            Debug.LogError("[SeedPopup] content chưa gán!");
            return;
        }

        // DestroyImmediate: xóa item cũ đồng bộ trước khi spawn mới.
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        if (seedItemPrefab == null)
        {
            Debug.LogError("[SeedPopup] seedItemPrefab chưa gán!");
            return;
        }

        if (cropDataList == null || cropDataList.Length == 0)
        {
            Debug.LogWarning("[SeedPopup] cropDataList rỗng — gán CropData trong inspector SeedPopupController.");
            return;
        }

        Debug.Log($"[SeedPopup] Spawn {cropDataList.Length} items...");

        int count = 0;
        foreach (var data in cropDataList)
        {
            if (data == null) { Debug.LogWarning("[SeedPopup] Slot null trong cropDataList."); continue; }

            GameObject go = Instantiate(seedItemPrefab, content);

            // Inject LayoutElement để HorizontalLayoutGroup không set item width = 0.
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = itemPreferredWidth;
            le.preferredHeight = itemPreferredHeight;

            SeedDragItem item = go.GetComponent<SeedDragItem>();
            if (item != null)
            {
                item.SetData(data);
                count++;
                Debug.Log($"[SeedPopup] #{count} crop={data.cropId} | icon={(data.icon != null ? data.icon.name : "NULL")}");
            }
            else
            {
                Debug.LogError($"[SeedPopup] Prefab thiếu SeedDragItem!");
            }
        }

        Debug.Log($"[SeedPopup] Xong: {count}/{cropDataList.Length} items.");
    }
}
