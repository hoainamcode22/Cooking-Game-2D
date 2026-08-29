using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup hiển thị tiến trình xây nhà (tương tự CropProcessPopupUI và PenProcessPopupUI).
/// Tự động gắn kết với HouseGrowthController, hiển thị tên, thanh fill bar, thời gian và nút Kim Cương.
/// </summary>
public class BuildingProcessPopupUI : MonoBehaviour
{
    public static BuildingProcessPopupUI Instance { get; private set; }

    private HouseGrowthController _house;
    private Canvas                _canvas;
    private GameObject            _root;
    private Image                 _rootImg;
    private Image                 _trackImg;
    private Image                 _fillImg;
    private Image                 _btnImg;
    private Image                 _diamondIconImg;
    private TMP_Text              _txtName;
    private TMP_Text              _txtTime;
    private TMP_Text              _txtGemCost;
    private Button                _btnGem;
    private int                   _openedAtFrame = -999;

    // Sprites loaded
    private Sprite                _frameBgSpr;
    private Sprite                _trackBgSpr;
    private Sprite                _fillGreenSpr;
    private Sprite                _btnBlueSpr;
    private Sprite                _diamondIconSpr;
    private TMP_FontAsset         _fontVo;

    public bool IsOpen => _root != null && _root.activeSelf;

    public static BuildingProcessPopupUI GetOrCreate()
    {
        if (Instance == null)
        {
            var go = new GameObject("BuildingProcessPopupUI");
            Instance = go.AddComponent<BuildingProcessPopupUI>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDesignAssets();
        BuildUI();
    }

    private void Update()
    {
        if (_root == null || !_root.activeSelf) return;

        if (_house == null || _house.State != HouseGrowthController.GrowthState.Building)
        {
            Close();
            return;
        }

        RefreshDisplay();
        UpdateScreenPosition();

        // Đóng khi click ra ngoài (bảo vệ 2 frame đầu)
        if (Time.frameCount > _openedAtFrame + 2 && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverRoot())
                Close();
        }
    }

    public void Open(HouseGrowthController house)
    {
        if (house == null || house.State != HouseGrowthController.GrowthState.Building) return;
        _house = house;
        _openedAtFrame = Time.frameCount;

        if (_frameBgSpr == null || _fillGreenSpr == null)
        {
            LoadDesignAssets();
        }

        ApplyLoadedSprites();
        RefreshDisplay();
        UpdateScreenPosition();
        _root.SetActive(true);
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
        _house = null;
    }

    private void RefreshDisplay()
    {
        if (_house == null) return;

        if (_txtName != null)
            _txtName.text = _house.HouseName;

        float rem = _house.RemainingSeconds;
        int min = Mathf.FloorToInt(rem / 60f);
        int sec = Mathf.FloorToInt(rem % 60f);

        if (_txtTime != null)
            _txtTime.text = $"{min:00}:{sec:00}";

        if (_fillImg != null)
            _fillImg.fillAmount = _house.Progress;

        if (_txtGemCost != null)
            _txtGemCost.text = _house.SpeedUpGemCost.ToString();
    }

    private void UpdateScreenPosition()
    {
        if (_house == null || _root == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = _house.transform.position + new Vector3(0f, 3.2f, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        RectTransform rootRect = _root.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.position = screenPos;
        }
    }

    private void OnSpeedUpClicked()
    {
        if (_house != null)
        {
            _house.TrySpeedUpWithGem();
        }
    }

    private bool IsPointerOverRoot()
    {
        if (_root == null) return false;
        RectTransform rt = _root.GetComponent<RectTransform>();
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
    }

    private void LoadDesignAssets()
    {
        // 1. Tận dụng sprite từ CropProcessPopupUI nếu có
        var cropPopup = FindFirstObjectByType<CropProcessPopupUI>();
        if (cropPopup != null)
        {
            if (cropPopup.progressFill != null && cropPopup.progressFill.sprite != null)
                _fillGreenSpr = cropPopup.progressFill.sprite;
            if (cropPopup.btnSpeedUp != null && cropPopup.btnSpeedUp.image != null)
                _btnBlueSpr = cropPopup.btnSpeedUp.image.sprite;
            if (cropPopup.imgDiamondIcon != null && cropPopup.imgDiamondIcon.sprite != null)
                _diamondIconSpr = cropPopup.imgDiamondIcon.sprite;
        }

        if (_fontVo == null)
            _fontVo = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("Canvas_BuildingProcessPopup", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _root = new GameObject("Root_BuildingProcess", typeof(RectTransform), typeof(Image));
        _root.transform.SetParent(canvasGO.transform, false);
        _rootImg = _root.GetComponent<Image>();
        _rootImg.color = new Color(0.96f, 0.92f, 0.84f, 0.95f); // Nền màu be kem ấm

        RectTransform rootRT = _root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(360f, 130f);

        // Tên nhà
        RectTransform nameRT = CreateRect(_root.transform, "Txt_Name", new Vector2(320f, 32f), new Vector2(0f, 36f));
        _txtName = CreateText(nameRT, "Nhà Dân", 24f, new Color(0.35f, 0.20f, 0.10f), _fontVo, TextAlignmentOptions.Center);

        // Thanh Track Background
        RectTransform trackRT = CreateRect(_root.transform, "Track_BG", new Vector2(200f, 26f), new Vector2(-50f, -12f));
        _trackImg = trackRT.gameObject.AddComponent<Image>();
        _trackImg.color = new Color(0.3f, 0.2f, 0.15f, 0.5f);

        // Thanh Fill Green
        RectTransform fillRT = CreateRect(trackRT, "Fill_Green", new Vector2(196f, 22f), Vector2.zero);
        _fillImg = fillRT.gameObject.AddComponent<Image>();
        _fillImg.type = Image.Type.Filled;
        _fillImg.fillMethod = Image.FillMethod.Horizontal;
        _fillImg.fillOrigin = 0;
        _fillImg.color = new Color(0.35f, 0.85f, 0.25f, 1f);

        // Text Thời gian
        RectTransform timeRT = CreateRect(trackRT, "Txt_Time", new Vector2(180f, 24f), Vector2.zero);
        _txtTime = CreateText(timeRT, "00:30", 18f, Color.white, _fontVo, TextAlignmentOptions.Center);

        // Nút Kim Cương (Speed Up)
        RectTransform btnRT = CreateRect(_root.transform, "Btn_SpeedUp", new Vector2(88f, 54f), new Vector2(115f, -12f));
        _btnImg = btnRT.gameObject.AddComponent<Image>();
        _btnImg.color = new Color(0.2f, 0.65f, 0.95f, 1f);

        _btnGem = btnRT.gameObject.AddComponent<Button>();
        _btnGem.targetGraphic = _btnImg;
        _btnGem.onClick.AddListener(OnSpeedUpClicked);

        // Icon Kim Cương
        RectTransform diaRT = CreateRect(btnRT, "Icon_Diamond", new Vector2(28f, 28f), new Vector2(-16f, 0f));
        _diamondIconImg = diaRT.gameObject.AddComponent<Image>();
        _diamondIconImg.color = Color.cyan;

        // Số Gem
        RectTransform costRT = CreateRect(btnRT, "Txt_GemCost", new Vector2(36f, 28f), new Vector2(18f, 0f));
        _txtGemCost = CreateText(costRT, "1", 20f, Color.white, _fontVo, TextAlignmentOptions.Center);

        ApplyLoadedSprites();
        _root.SetActive(false);
    }

    private void ApplyLoadedSprites()
    {
        if (_fillImg != null && _fillGreenSpr != null)
        {
            _fillImg.sprite = _fillGreenSpr;
            _fillImg.color = Color.white;
        }
        if (_btnImg != null && _btnBlueSpr != null)
        {
            _btnImg.sprite = _btnBlueSpr;
            _btnImg.type = Image.Type.Sliced;
            _btnImg.color = Color.white;
        }
        if (_diamondIconImg != null && _diamondIconSpr != null)
        {
            _diamondIconImg.sprite = _diamondIconSpr;
            _diamondIconImg.color = Color.white;
        }
    }

    private RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private TMP_Text CreateText(RectTransform parent, string initialText, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;
        txt.text = initialText;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = align;
        if (font != null) txt.font = font;
        return txt;
    }
}
