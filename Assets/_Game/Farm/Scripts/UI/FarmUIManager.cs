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

    private void Awake()
    {
        // Singleton chuẩn cho UI manager trong scene farm.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Ẩn popup seed ngay khi vào scene để tránh hiện sẵn.
        HideAllPopups();
        RefreshTopBar();
    }

    private void HandleCurrencyChanged(int gold, int gems)
    {
        // Event hook nếu sau này cần update top bar theo economy.
        RefreshTopBar();
    }

    private void HandleLevelChanged(int level)
    {
        // Event hook nếu sau này cần update top bar theo level.
        RefreshTopBar();
    }

    public void RefreshTopBar()
    {
        // Đồng bộ top UI cơ bản từ các manager runtime.
        if (txtDay != null)
            txtDay.text = "Day 1";

        if (txtGold != null)
            txtGold.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gold.ToString()
                : "0";

        if (txtGem != null)
            txtGem.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gems.ToString()
                : "0";

        if (txtLevel != null)
            txtLevel.text = FarmLevelManager.Instance != null
                ? $"Lv.{FarmLevelManager.Instance.CurrentLevel}"
                : "Lv.1";
    }

    public void ShowHint(string message)
    {
        // Hiển thị message ngắn ở vùng hint để debug / feedback gameplay.
        if (txtHint != null)
            txtHint.text = message;
    }

    public void HideAllPopups()
    {
        // Hiện tại chỉ còn 1 popup seed, nên đóng duy nhất popup này.
        if (popupSeed != null)
            popupSeed.SetActive(false);
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
        // Đóng popup seed sau khi trồng thành công.
        if (popupSeed != null)
            popupSeed.SetActive(false);
    }

    public void OnClick_CloseAllPopups()
    {
        // API đóng popup dùng cho nút X hoặc background.
        HideAllPopups();
    }

    public void OnClick_GoCooking()
    {
        // Điều hướng sang scene cooking nếu cần từ UI farm.
        SceneManager.LoadScene("SCN_Cooking");
    }

    public void OnClick_OpenInventory()
    {
        // Placeholder cho inventory UI.
        ShowHint("Mở túi đồ.");
    }

    public void OnClick_OpenWarehouse()
    {
        // Placeholder cho warehouse UI.
        ShowHint("Mở kho.");
    }

    public void OnClick_OpenMarket()
    {
        // Placeholder cho market UI.
        ShowHint("Mở chợ.");
    }

    public void OnClick_OpenRanking()
    {
        // Placeholder cho ranking UI.
        ShowHint("Mở bảng xếp hạng.");
    }
}