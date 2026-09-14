using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FarmGame.UI;

/// <summary>
/// Quản lý trung tâm kích hoạt các hiệu ứng Game Juice và VFX có sẵn trong dự án:
///  1. RewardFlyFX: Bay tiền vàng, kim cương, EXP về Top Bar HUD.
///  2. WarehouseGainToastUI: Thanh trượt góc trên hiển thị sức chứa kho khi thu hoạch.
///  3. UIJuiceFeedback: Hiệu ứng đàn hồi nảy mẩy (Squash & Stretch) cho nút bấm UI.
///  4. Ambient VFX: Lá cây chao liệng và bụi nắng ấm áp trong không gian nông trại.
/// 
/// Tự động chạy an toàn ở AfterSceneLoad (không sửa đổi cấu trúc cứng của file Scene SCN_Farm).
/// Mọi hiệu ứng đều có cờ BẬT/TẮT trong Inspector để điều chỉnh hoặc hoàn tác dễ dàng.
/// </summary>
[DisallowMultipleComponent]
public class FarmVFXJuiceManager : MonoBehaviour
{
    public static FarmVFXJuiceManager Instance { get; private set; }

    [Header("--- Tùy chọn Bật/Tắt Hiệu Ứng ---")]
    [Tooltip("Bật hiệu ứng tiền vàng, kim cương, EXP bay vòng cung về HUD")]
    [SerializeField] private bool enableRewardFly = true;

    [Tooltip("Bật thanh toast hiển thị số lượng kho tăng khi thu hoạch")]
    [SerializeField] private bool enableWarehouseToast = true;

    [Tooltip("Bật hiệu ứng nhún nảy đàn hồi cho các nút bấm HUD")]
    [SerializeField] private bool enableButtonJuice = true;

    [Tooltip("Bật lá cây chao liệng rơi trong nông trại")]
    [SerializeField] private bool enableAmbientLeaves = true;

    [Tooltip("Bật bụi nắng vàng ấm áp lơ lửng trong không khí")]
    [SerializeField] private bool enableAmbientDust = true;

    [Header("--- Ambient VFX Prefabs (Resources/VFX) ---")]
    [SerializeField] private GameObject leavesPrefab;
    [SerializeField] private GameObject dustPrefab;

    private GameObject _ambientRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitOnSceneLoaded()
    {
        // Chỉ chạy trên Scene nông trại có hệ thống đất trồng hoặc HUD Township
        if (FindFirstObjectByType<PlotController>() == null &&
            FindFirstObjectByType<TownshipHUDController>() == null &&
            FindFirstObjectByType<FarmCropVFXSpawner>() == null)
        {
            return;
        }

        if (Instance == null)
        {
            GameObject managerGo = new GameObject("[FarmVFXJuiceManager]");
            managerGo.AddComponent<FarmVFXJuiceManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ApplyAllJuiceEffects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Kiểm tra xem scene mới có phải nông trại không
        if (FindFirstObjectByType<PlotController>() != null ||
            FindFirstObjectByType<TownshipHUDController>() != null)
        {
            ApplyAllJuiceEffects();
        }
    }

    /// <summary>
    /// Kích hoạt toàn bộ hệ thống VFX & Juice một cách an toàn, không đè nén tài nguyên cũ.
    /// </summary>
    public void ApplyAllJuiceEffects()
    {
        StartCoroutine(CoSetupJuiceRoutine());
    }

    private IEnumerator CoSetupJuiceRoutine()
    {
        // Đợi 1 frame để các Canvas và Manager trong Scene khởi tạo hoàn tất
        yield return null;

        Canvas hudCanvas = ResolveHudCanvas();

        // 1. Setup RewardFlyFX (Tiền tệ & EXP bay)
        if (enableRewardFly && hudCanvas != null)
        {
            SetupRewardFly(hudCanvas);
        }

        // 2. Setup WarehouseGainToastUI (Thanh kho trượt)
        if (enableWarehouseToast && hudCanvas != null)
        {
            SetupWarehouseToast(hudCanvas);
        }

        // 3. Setup Button Juice Feedback
        if (enableButtonJuice && hudCanvas != null)
        {
            SetupButtonJuice(hudCanvas);
        }

        // 4. Setup Ambient Environment (Lá rơi + Bụi nắng)
        SetupAmbientEffects();
    }

    private Canvas ResolveHudCanvas()
    {
        var goldObj = GameObject.Find("Gold_Container");
        if (goldObj != null)
        {
            Canvas c = goldObj.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }

        var hudCtrl = FindFirstObjectByType<TownshipHUDController>();
        if (hudCtrl != null)
        {
            Canvas c = hudCtrl.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }

        return FindFirstObjectByType<Canvas>();
    }

    private void SetupRewardFly(Canvas hudCanvas)
    {
        var flyFx = FindFirstObjectByType<RewardFlyFX>(FindObjectsInactive.Include);
        if (flyFx == null)
        {
            flyFx = hudCanvas.gameObject.GetComponent<RewardFlyFX>();
            if (flyFx == null)
            {
                flyFx = hudCanvas.gameObject.AddComponent<RewardFlyFX>();
                Debug.Log("[FarmVFXJuiceManager] ✅ Đã kích hoạt RewardFlyFX trên Canvas HUD.");
            }
        }
        else if (!flyFx.enabled)
        {
            flyFx.enabled = true;
        }
    }

    private void SetupWarehouseToast(Canvas hudCanvas)
    {
        var toast = FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include);
        if (toast == null)
        {
            GameObject toastGo = new GameObject("WarehouseGainToast", typeof(RectTransform), typeof(WarehouseGainToastUI));
            toastGo.transform.SetParent(hudCanvas.transform, false);
            var rt = toastGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Debug.Log("[FarmVFXJuiceManager] ✅ Đã kích hoạt WarehouseGainToastUI.");
        }
    }

    private void SetupButtonJuice(Canvas hudCanvas)
    {
        // Gắn vào các nút quan trọng trên HUD
        Button[] buttons = hudCanvas.GetComponentsInChildren<Button>(true);
        int added = 0;
        foreach (var btn in buttons)
        {
            if (btn.GetComponent<UIJuiceFeedback>() == null)
            {
                btn.gameObject.AddComponent<UIJuiceFeedback>();
                added++;
            }
        }
        if (added > 0)
        {
            Debug.Log($"[FarmVFXJuiceManager] ✅ Đã thêm UIJuiceFeedback vào {added} nút bấm HUD.");
        }
    }

    private void SetupAmbientEffects()
    {
        if (!enableAmbientLeaves && !enableAmbientDust) return;

        if (_ambientRoot == null)
        {
            _ambientRoot = GameObject.Find("_Environment_VFX");
            if (_ambientRoot == null)
            {
                _ambientRoot = new GameObject("_Environment_VFX");
            }
        }

        Camera mainCam = Camera.main;
        Vector3 centerPos = mainCam != null ? new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, 0f) : Vector3.zero;

        // Lá rơi
        if (enableAmbientLeaves && _ambientRoot.transform.Find("Ambient_Leaves") == null)
        {
            if (leavesPrefab == null)
                leavesPrefab = Resources.Load<GameObject>("VFX/VFX_Leaves");

            if (leavesPrefab != null)
            {
                GameObject leaves = Instantiate(leavesPrefab, centerPos + new Vector3(0f, 2f, 0f), Quaternion.identity, _ambientRoot.transform);
                leaves.name = "Ambient_Leaves";
                leaves.transform.localScale = Vector3.one * 1.5f;
            }
        }

        // Bụi nắng
        if (enableAmbientDust && _ambientRoot.transform.Find("Ambient_Dust") == null)
        {
            if (dustPrefab == null)
                dustPrefab = Resources.Load<GameObject>("VFX/VFX_DustParticles");

            if (dustPrefab != null)
            {
                GameObject dust = Instantiate(dustPrefab, centerPos, Quaternion.identity, _ambientRoot.transform);
                dust.name = "Ambient_Dust";
                dust.transform.localScale = Vector3.one * 10f;
            }
        }
    }
}
