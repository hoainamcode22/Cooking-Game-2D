using UnityEngine;

/// <summary>
/// Quản lý visual cây trồng trong 1 plot.
/// Script gắn lên CropGroup (parent của CropPoint_1..4).
///
/// Hierarchy:
///   CropGroup  ← script này
///     CropPoint_1  (Transform rỗng, localScale = 1,1,1)
///       Visual     (SpriteRenderer, tạo tự động)
///     CropPoint_2 ...
///
/// Scale cuối: normalizedFromHeight * cropDataStageScale * globalVisualMultiplier
/// localPosition của Visual luôn = (0,0,0).
/// </summary>
public class PlotCropVisual : MonoBehaviour
{
    [Header("Points — tự tìm, hoặc gán tay")]
    [SerializeField] private Transform[] cropPoints = new Transform[0];

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Crop";
    [SerializeField] private int    sortingOrder     = 20;

    [Header("Ripe Wind Sway")]
    [SerializeField] private bool  enableReadySway = true;
    [SerializeField] private float swayAngle       = 4.5f;
    [SerializeField] private float swaySpeed       = 1.4f;
    [SerializeField] private float swayPhaseRange  = 2.0f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private CropData         currentCrop;
    private SpriteRenderer[] slotRenderers;
    private Transform[]      slotVisuals;
    private float[]          slotPhase;

    private float swayTimer;
    private bool  isReadySwayActive;
    private bool  isSetupDone;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureSetup();
        Debug.Log($"CropPoints found: {cropPoints.Length}");
    }
    private void OnValidate() => AutoFindPoints();

    private void Update()
    {
        if (!enableReadySway || !isReadySwayActive || slotVisuals == null)
            return;

        swayTimer += Time.deltaTime;
        for (int i = 0; i < slotVisuals.Length; i++)
        {
            Transform      v  = slotVisuals[i];
            SpriteRenderer sr = slotRenderers[i];
            if (v == null || sr == null || !sr.enabled) continue;

            float angle = Mathf.Sin((swayTimer + slotPhase[i]) * swaySpeed) * swayAngle;
            v.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Hiển thị crop theo progress (0..1).</summary>
    public void ShowCrop(CropData crop, float progress01)
    {
        EnsureSetup();
        if (crop == null) { ClearAll(); return; }

        currentCrop = crop;
        progress01  = Mathf.Clamp01(progress01);
        int  stage   = progress01 >= 1f ? 2 : (progress01 < 0.5f ? 0 : 1);
        bool isReady = stage == 2;

        SetReadySwayActive(isReady);
        UpdateVisual(stage);

        if (!isReady)
            foreach (var v in slotVisuals)
                if (v != null) v.localRotation = Quaternion.identity;

        for (int i = 0; i < cropPoints.Length; i++)
            if (cropPoints[i] != null)
                cropPoints[i].gameObject.SetActive(i < crop.displayCount);
    }

    /// <summary>Cập nhật sprite và scale cho stage hiện tại (0=Sprout, 1=Growing, 2=Ready).</summary>
    public void UpdateVisual(int stage)
    {
        if (currentCrop == null || slotRenderers == null) return;

        Vector3 targetScale = stage == 0 ? currentCrop.sproutScale
                            : stage == 1 ? currentCrop.growingScale
                            : currentCrop.readyScale;

        float offsetY = (targetScale.y - currentCrop.sproutScale.y) * -0.3f;

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null) continue;
            sr.sprite  = currentCrop.GetSprite(stage);
            sr.enabled = true;

            Transform visual = slotVisuals[i];
            if (visual == null) continue;
            visual.localScale    = targetScale;
            visual.localPosition = new Vector3(0f, offsetY, 0f);
        }
    }

    /// <summary>Tắt toàn bộ visual.</summary>
    public void ClearAll()
    {
        EnsureSetup();
        SetReadySwayActive(false);

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null) continue;

            sr.enabled = false;
            sr.sprite  = null;

            if (slotVisuals[i] != null)
            {
                slotVisuals[i].localPosition = Vector3.zero;
                slotVisuals[i].localRotation = Quaternion.identity;
                slotVisuals[i].localScale    = Vector3.one;
            }
        }

        for (int i = 0; i < cropPoints.Length; i++)
        {
            if (cropPoints[i] != null)
                cropPoints[i].gameObject.SetActive(true);
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    [ContextMenu("Auto Find Points")]
    public void AutoFindPoints()
    {
        var found = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.name.StartsWith("CropPoint_"))
                found.Add(t);
        }
        cropPoints = found.ToArray();
    }

    private void EnsureSetup()
    {
        AutoFindPoints();

        bool needRebuild = !isSetupDone
                        || slotRenderers == null
                        || slotRenderers.Length != cropPoints.Length;

        if (!needRebuild) return;

        slotRenderers = new SpriteRenderer[cropPoints.Length];
        slotVisuals   = new Transform[cropPoints.Length];
        slotPhase     = new float[cropPoints.Length];

        for (int i = 0; i < cropPoints.Length; i++)
        {
            Transform point = cropPoints[i];
            if (point == null) continue;

            // Đảm bảo CropPoint không có scale lạ ảnh hưởng child
            point.localScale = Vector3.one;

            // Tìm hoặc tạo child "Visual"
            Transform visualTf = point.Find("Visual");
            GameObject go;
            if (visualTf != null)
            {
                go = visualTf.gameObject;
            }
            else
            {
                go = new GameObject("Visual");
                go.transform.SetParent(point, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;
            }

            // Đảm bảo có SpriteRenderer — dùng explicit null check vì Unity ?? không reliable
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();

            // Chỉ set properties sau khi sr đã không null
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder;
            sr.sprite           = null;
            sr.color            = Color.white;
            sr.enabled          = false;

            slotRenderers[i] = sr;
            slotVisuals[i]   = go.transform;
            slotPhase[i]     = Random.Range(-swayPhaseRange, swayPhaseRange);
        }

        swayTimer         = Random.Range(0f, 10f);
        isReadySwayActive = false;
        isSetupDone       = true;
    }


    private void SetReadySwayActive(bool active)
    {
        if (isReadySwayActive == active) return;
        isReadySwayActive = active;

        if (!isReadySwayActive && slotVisuals != null)
            foreach (var v in slotVisuals)
                if (v != null) v.localRotation = Quaternion.identity;
    }
}
