using System.Xml.Linq;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using static UnityEngine.InputManagerEntry;
using static UnityEngine.UI.Image;

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

        popupSeed.SetActive(true);

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

