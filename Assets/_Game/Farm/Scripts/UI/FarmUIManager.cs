using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FarmUIManager : MonoBehaviour
{
    public static FarmUIManager Instance { get; private set; }

    [Header("Top Bar")]
    [SerializeField] private TMP_Text txtDay;
    [SerializeField] private TMP_Text txtGold;
    [SerializeField] private TMP_Text txtGem;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtHint;

    [Header("Popup Root")]
    [SerializeField] private GameObject popupSeed;

    [Header("Harvest Tool")]
    [SerializeField] private GameObject sickleToolRoot;
    [SerializeField] private SickleController sickleController;

    [Header("Scene Names")]
    [SerializeField] private string cookingSceneName = "SampleScene";

    [Header("Cooking Mode - Hide/Disable")]
    [SerializeField] private GameObject canvasHudRoot;
    [SerializeField] private GameObject canvasPopupRoot;
    [SerializeField] private GameObject[] popupObjectsToForceClose;
    [SerializeField] private Behaviour[] behavioursToDisableInCooking;
    [SerializeField] private AudioListener farmAudioListener;

    private bool isCookingMode;

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
        HideAllPopups();
        HideSickleTool();
        RefreshTopBar();
    }

    private void HandleCurrencyChanged(int gold, int gems)
    {
        RefreshTopBar();
    }

    private void HandleLevelChanged(int level)
    {
        RefreshTopBar();
    }

    public void RefreshTopBar()
    {
        if (txtDay != null)
            txtDay.text = "Ngày 1";

        if (txtGold != null)
        {
            txtGold.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gold.ToString()
                : "0";
        }

        if (txtGem != null)
        {
            txtGem.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gems.ToString()
                : "0";
        }

        if (txtLevel != null)
        {
            txtLevel.text = FarmLevelManager.Instance != null
                ? $"Lv.{FarmLevelManager.Instance.CurrentLevel}"
                : "Lv.1";
        }
    }

    public void ShowHint(string message)
    {
        if (txtHint != null)
            txtHint.text = message;
    }

    public void HideAllPopups()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);

        // Clear seed-related input locks whenever all popups close.
        FarmInputLock.IsSeedPopupOpen = false;
        FarmInputLock.IsDraggingSeed  = false;

        if (popupObjectsToForceClose != null)
        {
            for (int i = 0; i < popupObjectsToForceClose.Length; i++)
            {
                if (popupObjectsToForceClose[i] != null)
                    popupObjectsToForceClose[i].SetActive(false);
            }
        }
    }

    public void ShowSickleTool()
    {
        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);
    }

    public void HideSickleTool()
    {
        if (sickleController != null)
            sickleController.EndHarvestMode();

        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(false);
    }

    /// <summary>
    /// Open seed popup near the clicked plot world position.
    /// Converts world → screen → canvas local once, then popup stays fixed.
    /// </summary>
    public void ShowPlantSelectForPlot(PlotController plot)
    {
        if (isCookingMode)
            return;

        Debug.Log("[FarmUI] ShowPlantSelectForPlot CALLED");

        HideAllPopups();

        if (popupSeed == null)
        {
            Debug.LogError("[FarmUI] popupSeed is NULL");
            return;
        }

        // Open at fixed UI position — do not reposition near the plot.
        popupSeed.SetActive(true);
        FarmInputLock.IsSeedPopupOpen = true;

        if (plot != null)
            ShowHint($"Kéo hạt giống để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống để trồng.");
    }

    /// <summary>
    /// Position the popup near the clicked plot using a candidate-position system.
    /// Tries below → right → left → above. Uses first candidate fully inside canvas.
    /// Falls back to clamping if none fit perfectly.
    /// </summary>
    private void PositionPopupNearPlot(GameObject popup, Vector3 worldPos)
    {
        Camera cam = Camera.main;
        Canvas canvas = canvasPopupRoot != null
            ? canvasPopupRoot.GetComponent<Canvas>()
            : popup.GetComponentInParent<Canvas>();

        if (cam == null || canvas == null) return;

        RectTransform popupRect  = popup.GetComponent<RectTransform>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (popupRect == null || canvasRect == null) return;

        // World → canvas-local position of the plot (no offset yet).
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, canvas.worldCamera, out Vector2 plotLocal))
            return;

        Vector2 popupHalf  = popupRect.rect.size * 0.5f;
        Vector2 canvasHalf = canvasRect.rect.size * 0.5f;
        float   gap        = 20f; // pixels between plot edge and popup edge

        // Candidate offsets in priority order: below, right, left, above.
        Vector2[] candidates = new Vector2[]
        {
            new Vector2(0f,                       -(popupHalf.y + gap)),  // below
            new Vector2( popupHalf.x + gap,        0f),                   // right
            new Vector2(-(popupHalf.x + gap),      0f),                   // left
            new Vector2(0f,                         popupHalf.y + gap),   // above
        };

        // Try each candidate; use the first one that fits fully inside the canvas.
        Vector2 chosen = plotLocal + candidates[0]; // default = below
        foreach (Vector2 offset in candidates)
        {
            Vector2 candidate = plotLocal + offset;
            if (PopupFitsInCanvas(candidate, popupHalf, canvasHalf))
            {
                chosen = candidate;
                break;
            }
        }

        // Clamp the chosen position so popup never goes outside canvas.
        chosen = ClampToCanvas(chosen, popupHalf, canvasHalf);

        popupRect.anchoredPosition = chosen;
    }

    /// <summary>Returns true if a popup centered at localPos fits fully inside the canvas.</summary>
    private bool PopupFitsInCanvas(Vector2 localPos, Vector2 popupHalf, Vector2 canvasHalf)
    {
        return localPos.x - popupHalf.x >= -canvasHalf.x
            && localPos.x + popupHalf.x <=  canvasHalf.x
            && localPos.y - popupHalf.y >= -canvasHalf.y
            && localPos.y + popupHalf.y <=  canvasHalf.y;
    }

    /// <summary>Clamps popup anchoredPosition so all four edges stay inside the canvas.</summary>
    private Vector2 ClampToCanvas(Vector2 localPos, Vector2 popupHalf, Vector2 canvasHalf)
    {
        localPos.x = Mathf.Clamp(localPos.x, -canvasHalf.x + popupHalf.x, canvasHalf.x - popupHalf.x);
        localPos.y = Mathf.Clamp(localPos.y, -canvasHalf.y + popupHalf.y, canvasHalf.y - popupHalf.y);
        return localPos;
    }

    /// <summary>Close seed popup and clear input locks.</summary>
    public void HidePlantSelectPopup()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);

        FarmInputLock.IsSeedPopupOpen = false;
        FarmInputLock.IsDraggingSeed  = false;
    }

    public void OnClick_CloseAllPopups()
    {
        HideAllPopups();
    }

    public void OnClick_GoCooking()
    {
        if (!SceneManager.GetSceneByName(cookingSceneName).isLoaded)
            SceneManager.LoadScene(cookingSceneName, LoadSceneMode.Additive);
    }

    public void OnClick_OpenInventory()
    {
        if (isCookingMode) return;
        ShowHint("Mở túi đồ.");
    }

    public void OnClick_OpenWarehouse()
    {
        if (isCookingMode) return;
        ShowHint("Mở kho.");
    }

    public void OnClick_OpenMarket()
    {
        if (isCookingMode) return;
        ShowHint("Mở chợ.");
    }

    public void OnClick_OpenRanking()
    {
        if (isCookingMode) return;
        ShowHint("Mở bảng xếp hạng.");
    }

    public void EnterCookingMode()
    {
        if (isCookingMode)
            return;

        isCookingMode = true;

        HideAllPopups();
        HideSickleTool();

        if (canvasHudRoot != null)
            canvasHudRoot.SetActive(false);

        if (canvasPopupRoot != null)
            canvasPopupRoot.SetActive(false);

        if (behavioursToDisableInCooking != null)
        {
            for (int i = 0; i < behavioursToDisableInCooking.Length; i++)
            {
                if (behavioursToDisableInCooking[i] != null)
                    behavioursToDisableInCooking[i].enabled = false;
            }
        }

        if (farmAudioListener != null)
            farmAudioListener.enabled = false;
    }

    public void ExitCookingMode()
    {
        if (!isCookingMode)
            return;

        isCookingMode = false;

        if (canvasHudRoot != null)
            canvasHudRoot.SetActive(true);

        if (canvasPopupRoot != null)
            canvasPopupRoot.SetActive(true);

        if (behavioursToDisableInCooking != null)
        {
            for (int i = 0; i < behavioursToDisableInCooking.Length; i++)
            {
                if (behavioursToDisableInCooking[i] != null)
                    behavioursToDisableInCooking[i].enabled = true;
            }
        }

        if (farmAudioListener != null)
            farmAudioListener.enabled = true;

        HideAllPopups();
        HideSickleTool();
        RefreshTopBar();
    }
}

