using System.Collections;
using UnityEngine;

public class HarvestFeedbackSpawner : MonoBehaviour
{
    public static HarvestFeedbackSpawner Instance { get; private set; }

    /// <summary>Icon/thanh kho trên HUD — cho UI khác (vd popup tàu) bay vật phẩm về đúng chỗ.</summary>
    public Transform WarehouseTarget => ResolveWarehouseTarget();

    [Header("Fly FX")]
    [SerializeField] private HarvestFlyItemFX harvestFlyPrefab;
    [SerializeField] private Transform warehouseTarget;
    [SerializeField] private WarehousePulseFX warehousePulseFX;

    [Header("EXP FX")]
    [SerializeField] private ExpFlyToAvatarFX expFlyPrefab;
    [SerializeField] private TopBarExpUI topBarExpUI;
    [SerializeField] private RectTransform expTarget;
    [SerializeField] private RectTransform expPulseTarget;

    [Header("Tuning (World)")]
    [SerializeField] private int minVisualIcons = 2;
    [SerializeField] private int maxVisualIcons = 4;
    [SerializeField] private float spawnGap = 0.06f;
    [SerializeField] private float spawnScatterRadius = 70f;

    [Header("Tuning (EXP)")]
    [SerializeField] private int minVisualExpOrbs = 1;
    [SerializeField] private int maxVisualExpOrbs = 3;
    [SerializeField] private float expSpawnGap = 0.05f;
    [SerializeField] private float expSpawnScatterRadius = 55f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (topBarExpUI == null)
            topBarExpUI = FindFirstObjectByType<TopBarExpUI>();

        ResolveExpTarget();
        ResolveWarehouseTarget();
    }

    public void SpawnHarvestFly(Sprite icon, Vector3 worldPosition, int amount)
    {
        if (harvestFlyPrefab == null || icon == null)
            return;

        StartCoroutine(CoSpawnFly(icon, worldPosition, amount));
    }

    public void SpawnExpFly(Vector3 worldPosition, int expAmount, bool addExpOnArrival = true)
    {
        if (expAmount <= 0 || expFlyPrefab == null)
            return;

        StartCoroutine(CoSpawnExp(worldPosition, expAmount, addExpOnArrival));
    }

    private IEnumerator CoSpawnFly(Sprite icon, Vector3 worldPosition, int amount)
    {
        int visualCount = Mathf.Clamp(amount, minVisualIcons, maxVisualIcons);

        for (int i = 0; i < visualCount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * spawnScatterRadius;
            Vector3 spawnPos = worldPosition + new Vector3(scatter.x, scatter.y, 0f);

            HarvestFlyItemFX fx = Instantiate(harvestFlyPrefab, spawnPos, Quaternion.identity);
            if (fx == null) continue;

            fx.ClearIconImmediate();

            Vector3 worldTarget = GetWarehouseTargetWorldPosition(spawnPos);

            fx.Play(icon, spawnPos, worldTarget, () =>
            {
                // Khi từng icon chạm đích: gọi kho tăng dần + hiệu ứng mẩy mẩy
                if (WarehouseGainToastUI.Instance != null)
                {
                    WarehouseGainToastUI.Instance.OnHarvestItemArrived(icon);
                }
                else if (warehousePulseFX != null)
                {
                    warehousePulseFX.PlayPulse();
                }
            });

            if (spawnGap > 0f)
                yield return new WaitForSeconds(spawnGap);
        }
    }

    private IEnumerator CoSpawnExp(Vector3 worldPosition, int expAmount, bool addExpOnArrival)
    {
        int visualCount = Mathf.Clamp(expAmount, minVisualExpOrbs, maxVisualExpOrbs);
        int perOrbExp = Mathf.Max(1, expAmount / visualCount);
        int remainingExp = expAmount;

        for (int i = 0; i < visualCount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * expSpawnScatterRadius;
            Vector3 spawnPos = worldPosition + new Vector3(scatter.x, scatter.y, 0f);

            ExpFlyToAvatarFX fx = Instantiate(expFlyPrefab, spawnPos, Quaternion.identity);
            if (fx == null) continue;

            Vector3 expWorldTarget = GetExpTargetWorldPosition(spawnPos);
            int thisOrbExp = (i == visualCount - 1) ? remainingExp : perOrbExp;
            remainingExp -= thisOrbExp;

            fx.Play(spawnPos, expWorldTarget, () =>
            {
                // Khi viên EXP chạm vào thanh EXP_Bar_Container: nảy mẩy mẩy + cộng EXP
                PlayExpTargetPulse();

                // [FIX 2026-09-03] EXP đã cộng ở dòng 372; orb chỉ chạy FX, không cộng lần 2 (bug cộng đôi).
                if (addExpOnArrival && PlayerProgressManager.Instance != null)
                    PlayerProgressManager.Instance.AddExp(thisOrbExp);
            });

            if (expSpawnGap > 0f)
                yield return new WaitForSeconds(expSpawnGap);
        }
    }

    private Transform ResolveWarehouseTarget()
    {
        if (WarehouseGainToastUI.Instance != null && WarehouseGainToastUI.Instance.PanelRect != null)
        {
            warehouseTarget = WarehouseGainToastUI.Instance.PanelRect;
            return warehouseTarget;
        }

        if (warehouseTarget != null)
            return warehouseTarget;

        var toastGO = GameObject.Find("WarehouseGainToast");
        if (toastGO != null)
        {
            warehouseTarget = toastGO.transform;
            return warehouseTarget;
        }

        return transform;
    }

    private Vector3 GetWarehouseTargetWorldPosition(Vector3 spawnWorldPosition)
    {
        Transform target = ResolveWarehouseTarget();
        if (target == null)
            return spawnWorldPosition;

        if (target is RectTransform rt)
        {
            Camera worldCamera = Camera.main;
            if (worldCamera == null) return target.position;

            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            float depth = Mathf.Abs(worldCamera.transform.position.z - spawnWorldPosition.z);
            Vector3 worldTarget = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            worldTarget.z = spawnWorldPosition.z;
            return worldTarget;
        }

        return target.position;
    }

    private RectTransform ResolveExpTarget()
    {
        if (expTarget != null)
            return expTarget;

        // 1. Tìm container thanh EXP mới trên HUD: EXP_Bar_Container
        GameObject expBar = GameObject.Find("EXP_Bar_Container");
        if (expBar != null)
        {
            expTarget = expBar.GetComponent<RectTransform>();
            expPulseTarget = expTarget;
            return expTarget;
        }

        // 2. Tìm qua TopBarExpUI
        if (topBarExpUI == null)
            topBarExpUI = FindFirstObjectByType<TopBarExpUI>();

        if (topBarExpUI != null && topBarExpUI.IconExp != null)
        {
            expTarget = topBarExpUI.IconExp;
            expPulseTarget = expTarget;
            return expTarget;
        }

        // 3. Tìm icon_exp
        GameObject iconExp = GameObject.Find("icon_exp");
        if (iconExp != null)
        {
            expTarget = iconExp.GetComponent<RectTransform>();
            expPulseTarget = expTarget;
            return expTarget;
        }

        // 4. Tìm TopLeft_Township_HUD
        GameObject topLeft = GameObject.Find("TopLeft_Township_HUD");
        if (topLeft != null)
        {
            expTarget = topLeft.GetComponent<RectTransform>();
            expPulseTarget = expTarget;
            return expTarget;
        }

        return expTarget;
    }

    private Vector3 GetExpTargetWorldPosition(Vector3 spawnWorldPosition)
    {
        RectTransform target = ResolveExpTarget();
        Camera worldCamera = Camera.main;

        if (target == null)
        {
            // Fallback: góc trên-trái màn hình (nơi đặt avatar & exp)
            if (worldCamera != null)
            {
                float depth = Mathf.Abs(worldCamera.transform.position.z - spawnWorldPosition.z);
                Vector3 fallbackWorld = worldCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.15f, Screen.height * 0.92f, depth));
                fallbackWorld.z = spawnWorldPosition.z;
                return fallbackWorld;
            }
            return spawnWorldPosition + new Vector3(-100f, 200f, 0f);
        }

        if (worldCamera == null)
            return target.position;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        float d = Mathf.Abs(worldCamera.transform.position.z - spawnWorldPosition.z);
        Vector3 worldTarget = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, d));
        worldTarget.z = spawnWorldPosition.z;
        return worldTarget;
    }

    private void PlayExpTargetPulse()
    {
        RectTransform target = expPulseTarget != null ? expPulseTarget : ResolveExpTarget();
        if (target == null)
            return;

        JuicyPulseFX.Play(target, 1.22f, 0.25f);
    }
}
