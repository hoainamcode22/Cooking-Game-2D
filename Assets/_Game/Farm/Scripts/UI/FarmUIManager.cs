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
    [SerializeField] private GameObject sickleToolRoot;   // object lưỡi liềm ngoài scene
    [SerializeField] private SickleController sickleController; // script điều khiển lưỡi liềm

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
            txtDay.text = "Day 1";

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
    }

    /// <summary>
    /// Bật lưỡi liềm tại vị trí ô đất.
    /// FarmManager gọi hàm này khi click vào ô lúa đã chín.
    /// </summary>
    public void ShowSickleTool()
    {
        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);
    }


    public void HideSickleTool()
    {
        if (sickleController != null)
        {
            sickleController.EndHarvestMode();
        }

        if (sickleToolRoot != null)
        {
            sickleToolRoot.SetActive(false);
        }
    }

    public void ShowPlantSelectForPlot(PlotController plot)
    {
        Debug.Log("[FarmUI] ShowPlantSelectForPlot CALLED");

        HideAllPopups();

        if (popupSeed == null)
        {
            Debug.LogError("[FarmUI] popupSeed is NULL");
            return;
        }

        popupSeed.SetActive(true);
        Debug.Log("[FarmUI] popupSeed activeSelf = " + popupSeed.activeSelf);
        Debug.Log("[FarmUI] popupSeed name = " + popupSeed.name);

        if (plot != null)
            ShowHint($"Kéo hạt giống để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống để trồng.");
    }

    public void HidePlantSelectPopup()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);
    }

    public void OnClick_CloseAllPopups()
    {
        HideAllPopups();
    }

    public void OnClick_GoCooking()
    {
        SceneManager.LoadScene("SCN_Cooking");
    }

    public void OnClick_OpenInventory()
    {
        ShowHint("Mở túi đồ.");
    }

    public void OnClick_OpenWarehouse()
    {
        ShowHint("Mở kho.");
    }

    public void OnClick_OpenMarket()
    {
        ShowHint("Mở chợ.");
    }

    public void OnClick_OpenRanking()
    {
        ShowHint("Mở bảng xếp hạng.");
    }
}