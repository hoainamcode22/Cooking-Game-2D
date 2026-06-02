using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the sweep-drag-to-plant flow.
/// SeedDragItem.OnBeginDrag calls StartPlantDrag; OnEndDrag calls EndPlantDrag.
/// Update() runs the linecast sweep and acts as a failsafe mouse-up detector.
/// </summary>
public class PlantDragController : MonoBehaviour
{
    public static PlantDragController Instance { get; private set; }

    [Header("Detection")]
    [SerializeField] private LayerMask plotLayerMask = ~0;

    [Header("Seed Rain FX")]
    [SerializeField] private int   seedRainCount    = 5;
    [SerializeField] private float seedRainDurMin   = 0.18f;
    [SerializeField] private float seedRainDurMax   = 0.28f;
    [SerializeField] private float seedRainSpread   = 1.8f;
    [SerializeField] private float seedRainSpawnY   = 1.8f;
    [SerializeField] private float seedRainScale    = 0.6f;   // debug: 0.6f  final: 0.22-0.35f
    [SerializeField] private Sprite debugSeedSprite;

    private bool             isPlantDragging;
    private CropData         currentDragCrop;
    private Vector2          prevMouseWorld;
    private readonly HashSet<PlotController> plantedThisDrag = new HashSet<PlotController>();
    private bool             plantedAnyThisDrag;
    private Camera           mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartPlantDrag(CropData crop)
    {
        if (crop == null) return;

        currentDragCrop    = crop;
        isPlantDragging    = true;
        plantedAnyThisDrag = false;
        plantedThisDrag.Clear();
        prevMouseWorld = GetMouseWorld();

        FarmInputLock.IsDraggingSeed = true;
        // Không SetActive(false) popup ở đây — SeedDragItem vẫn đang active bên trong popup.
        // SetActive(false) sẽ kill OnDrag/OnEndDrag ngay lập tức.
        // Popup sẽ được đóng bởi EndPlantDrag sau khi drag kết thúc.
        FarmUIManager.Instance?.ShowFloatingDragIcon(crop.icon);

        Debug.Log($"[PlantDrag] BEGIN | crop={crop.cropId} | IsDraggingSeed={FarmInputLock.IsDraggingSeed}");
    }

    public void EndPlantDrag()
    {
        Debug.Log($"[PlantDrag] END requested | isPlantDragging={isPlantDragging}" +
                  $" | plantedAny={plantedAnyThisDrag} | count={plantedThisDrag.Count}");

        if (!isPlantDragging) return;

        bool   didPlant  = plantedAnyThisDrag;
        int    count     = plantedThisDrag.Count;
        string cropName  = currentDragCrop?.displayName ?? "?";

        CleanupPlantDragState();

        if (didPlant)
        {
            FarmUIManager.Instance?.HidePlantSelectPopup();
            FarmUIManager.Instance?.ShowHint($"Đã trồng {count} ô {cropName}");
        }
        else
        {
            // Drag cancelled without planting — popup already closed, nothing to reopen.
            Debug.Log("[PlantDrag] No plots planted this drag.");
        }
    }

    /// <summary>
    /// Resets all drag state unconditionally. Called from every exit path.
    /// </summary>
    private void CleanupPlantDragState()
    {
        isPlantDragging    = false;
        plantedAnyThisDrag = false;
        currentDragCrop    = null;
        plantedThisDrag.Clear();

        FarmInputLock.IsDraggingSeed  = false;
        FarmInputLock.IsSeedPopupOpen = false;

        FarmUIManager.Instance?.HideFloatingDragIcon();

        Debug.Log($"[PlantDrag] CLEANUP DONE" +
                  $" | isPlantDragging={isPlantDragging}" +
                  $" | currentDragCrop={currentDragCrop}" +
                  $" | IsDraggingSeed={FarmInputLock.IsDraggingSeed}" +
                  $" | IsSeedPopupOpen={FarmInputLock.IsSeedPopupOpen}");
    }

    // ── Update sweep ──────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isPlantDragging) return;

        Vector2 cur = GetMouseWorld();
        SweepForPlots(prevMouseWorld, cur);
        prevMouseWorld = cur;

        // Failsafe: use new input system (same as FarmPlotInput) so it always fires.
        bool mouseUp = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        if (mouseUp)
        {
            Debug.Log("[PlantDrag] failsafe mouse-up detected in Update");
            EndPlantDrag();
        }
    }

    private void SweepForPlots(Vector2 from, Vector2 to)
    {
        if ((to - from).sqrMagnitude > 0.0001f)
        {
            RaycastHit2D[] hits = Physics2D.LinecastAll(from, to, plotLayerMask);
            foreach (RaycastHit2D hit in hits)
                TryPlantAt(hit.collider);
        }
        else
        {
            Collider2D col = Physics2D.OverlapPoint(to, plotLayerMask);
            if (col != null) TryPlantAt(col);
        }
    }

    private void TryPlantAt(Collider2D col)
    {
        if (col == null || currentDragCrop == null) return;

        PlotController plot = col.GetComponentInParent<PlotController>();
        if (plot == null) return;
        if (plantedThisDrag.Contains(plot)) return;

        // Kiểm tra kho còn đủ hạt giống không trước khi trồng
        string seedId    = currentDragCrop.seedItemId;
        int    seedStock = WarehouseManager.Instance != null
                           ? WarehouseManager.Instance.GetAmount(seedId)
                           : 0;

        if (seedStock <= 0)
        {
            Debug.Log($"[PlantDrag] Hết hạt giống '{currentDragCrop.displayName}' (seedId={seedId}) — dừng trồng.");
            EndPlantDrag();
            return;
        }

        bool planted = FarmManager.Instance.TryPlantToSpecificPlot(plot, currentDragCrop);
        if (!planted) return;

        // Trừ 1 hạt giống khỏi kho sau khi trồng thành công
        WarehouseManager.Instance?.RemoveItem(seedId, 1);

        plantedThisDrag.Add(plot);
        plantedAnyThisDrag = true;

        SpawnSeedRain(plot.transform.position);

        Debug.Log($"[PlantDrag] Trồng {currentDragCrop.cropId} → {plot.name} | Kho còn {seedStock - 1} hạt");
    }

    // ── Seed Rain FX ──────────────────────────────────────────────────────────

    // Creates a solid 16×16 red/magenta texture sprite — no asset dependency.
    private static Sprite _proceduralDebugSprite;
    private static Sprite GetProceduralDebugSprite()
    {
        if (_proceduralDebugSprite != null) return _proceduralDebugSprite;
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] px = new Color[16 * 16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.magenta;
        tex.SetPixels(px);
        tex.Apply();
        _proceduralDebugSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        return _proceduralDebugSprite;
    }

    private void SpawnSeedRain(Vector3 plotWorldPos)
    {
        const int   COUNT    = 8;
        const float SCALE    = 200f;  // world units
        const float DUR_MIN  = 0.35f;
        const float DUR_MAX  = 0.55f;

        Sprite finalSprite = currentDragCrop?.plantSeedFxIcon != null ? currentDragCrop.plantSeedFxIcon :
                             currentDragCrop?.icon             != null ? currentDragCrop.icon :
                             GetProceduralDebugSprite();

        for (int i = 0; i < COUNT; i++)
        {
            Vector3 startPos = plotWorldPos + new Vector3(
                Random.Range(-200f, 200f),   // trải rộng theo chiều ngang
                Random.Range(100f, 250f),    // rơi từ trên cao xuống
                -5f);

            Vector3 endPos = plotWorldPos + new Vector3(
                Random.Range(-150f, 150f),
                Random.Range(0f, 30f),
                -5f);

            // Spawn at scene root — no parent, world scale stays independent
            GameObject go = new GameObject($"SeedRain_{i}");
            go.transform.position   = startPos;
            go.transform.localScale = Vector3.one * SCALE;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = finalSprite;
            sr.color            = Color.white;
            sr.sortingLayerName = "FX";
            sr.sortingOrder     = 8000;

            Debug.Log($"[SeedRain] #{i}" +
                      $" | sprite='{(sr.sprite != null ? sr.sprite.name : "NULL")}'" +
                      $" | localScale={go.transform.localScale}" +
                      $" | start={startPos} end={endPos}" +
                      $" | layer='{sr.sortingLayerName}' ID={sr.sortingLayerID} order={sr.sortingOrder}");

            StartCoroutine(CoFallNoFade(go, endPos, Random.Range(DUR_MIN, DUR_MAX)));
        }
    }

    // DEBUG: no fade, no scale change — object stays fully opaque for entire duration.
    private IEnumerator CoFallNoFade(GameObject go, Vector3 endPos, float duration)
    {
        if (go == null) yield break;

        Vector3 startPos = go.transform.position;
        float   t        = 0f;

        while (t < 1f)
        {
            if (go == null) yield break;
            t += Time.deltaTime / duration;
            go.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 GetMouseWorld()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return Vector2.zero;
        return mainCam.ScreenToWorldPoint(Input.mousePosition);
    }
}
