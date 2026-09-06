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
    private DecorGrowthController _decor;
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
    // [FIX 2026-09-06 B3] Cờ chống double-register với FarmInputLock — cùng pattern
    // TrainStationMasterPopupUI đang dùng: mở 1 lần tăng popupLockCount, đóng 1 lần giảm,
    // không được tăng/giảm lệch nhau (lệch = tái tạo đúng bug "khoá UI luôn" Sếp báo).
    private bool                  _inputLockHeld;

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

        bool isHouseActive = _house != null && _house.State == HouseGrowthController.GrowthState.Building;
        bool isDecorActive = _decor != null && _decor.State == DecorGrowthController.DecorState.Building;

        if (!isHouseActive && !isDecorActive)
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
        _decor = null;
        _openedAtFrame = Time.frameCount;

        if (_frameBgSpr == null || _fillGreenSpr == null)
        {
            LoadDesignAssets();
        }

        ApplyLoadedSprites();
        RefreshDisplay();
        UpdateScreenPosition();
        _root.SetActive(true);
        // [ROLLBACK 2026-09-06] KHONG khoa input o popup nay.
        // FarmInputLock.RegisterPopupOpen() lam popupLockCount>0 => BlockMapPan chan
        // TOAN BO map va moi click world. Popup nay neo o world, khong che man hinh,
        // nen khong can khoa. Close() van goi Release de go bat ky khoa ket nao.
    }

    public void Open(DecorGrowthController decor)
    {
        if (decor == null || decor.State != DecorGrowthController.DecorState.Building) return;
        _decor = decor;
        _house = null;
        _openedAtFrame = Time.frameCount;

        if (_frameBgSpr == null || _fillGreenSpr == null)
        {
            LoadDesignAssets();
        }

        ApplyLoadedSprites();
        RefreshDisplay();
        UpdateScreenPosition();
        _root.SetActive(true);
        // [ROLLBACK 2026-09-06] KHONG khoa input o popup nay.
        // FarmInputLock.RegisterPopupOpen() lam popupLockCount>0 => BlockMapPan chan
        // TOAN BO map va moi click world. Popup nay neo o world, khong che man hinh,
        // nen khong can khoa. Close() van goi Release de go bat ky khoa ket nao.
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
        _house = null;
        _decor = null;
        ReleasePopupInputBlock();
    }

    private void RefreshDisplay()
    {
        if (_house != null)
        {
            if (_txtName != null) _txtName.text = _house.HouseName;

            float rem = _house.RemainingSeconds;
            int min = Mathf.FloorToInt(rem / 60f);
            int sec = Mathf.FloorToInt(rem % 60f);

            if (_txtTime != null) _txtTime.text = $"{min:00}:{sec:00}";
            if (_fillImg != null) _fillImg.fillAmount = _house.Progress;
            if (_txtGemCost != null) _txtGemCost.text = _house.SpeedUpGemCost.ToString();
        }
        else if (_decor != null)
        {
            if (_txtName != null) _txtName.text = _decor.DisplayName;

            float rem = _decor.RemainingSeconds;
            int min = Mathf.FloorToInt(rem / 60f);
            int sec = Mathf.FloorToInt(rem % 60f);

            if (_txtTime != null) _txtTime.text = $"{min:00}:{sec:00}";
            if (_fillImg != null) _fillImg.fillAmount = _decor.Progress;
            if (_txtGemCost != null) _txtGemCost.text = _decor.SpeedUpGemCost.ToString();
        }
    }

    private void UpdateScreenPosition()
    {
        Transform targetTf = _house != null ? _house.transform : (_decor != null ? _decor.transform : null);
        if (targetTf == null || _root == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = targetTf.position + new Vector3(0f, 3.2f, 0f);
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
        else if (_decor != null)
        {
            _decor.TrySpeedUpWithGem();
        }
    }

    private bool IsPointerOverRoot()
    {
        if (_root == null) return false;
        RectTransform rt = _root.GetComponent<RectTransform>();
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
    }

    // ── [FIX 2026-09-06 B3] Đăng ký với hệ khoá input chung của game ─────────
    // Trước đây popup này VÔ HÌNH với FarmInputLock/PopupManager: mở popup không tăng
    // popupLockCount, PopupManager.IsAnyPopupOpen() cũng không biết tới nó. Chỉ gọi
    // RegisterPopupOpen/RegisterPopupClose (KHÔNG gọi SetPopupRaycastBlock) — cố tình,
    // vì SetPopupRaycastBlock ép raycastTarget=true lên chính Image của _root, ngược lại
    // hoàn toàn với yêu cầu B5 (giảm vùng chặn raycast xuống chỉ còn đúng nút kim cương).
    private void AcquirePopupInputBlock()
    {
        if (_inputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _inputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        if (!_inputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _inputLockHeld = false;
    }

    private void LoadDesignAssets()
    {
        // [FIX 2026-09-06 B2] 1. Mượn sprite từ CropProcessPopupUI — PHẢI có
        // FindObjectsInactive.Include: CropProcessPopupUI.Awake() luôn gameObject.SetActive(false)
        // khi đóng, nên bản cũ dùng FindFirstObjectByType() mặc định (Exclude) luôn ra null →
        // 3 sprite dưới đây không bao giờ được gán → vẽ bằng màu phẳng, mất khung/nền.
        // Duyệt TẤT CẢ instance (không chỉ cái đầu) để tăng cơ hội gặp bản đã bind đủ field,
        // và còn lấy thêm 2 sprite mà CropProcessPopupUI không public field hoá:
        //   _frameBgSpr  = Image ngay trên GameObject gốc của CropProcessPopupUI (khung/card bo góc)
        //   _trackBgSpr  = Image trên child "Track_Bar" (máng thanh tiến độ)
        // (đúng pattern PenProcessPopupUI.LoadDesignAssets() đã dùng cho fallback runtime của nó).
        if (_frameBgSpr == null || _trackBgSpr == null || _fillGreenSpr == null
            || _btnBlueSpr == null || _diamondIconSpr == null)
        {
            var cropPopups = FindObjectsByType<CropProcessPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cropPopup in cropPopups)
            {
                if (cropPopup == null) continue;
                cropPopup.AutoBindComponents();

                if (_fillGreenSpr == null && cropPopup.progressFill != null && cropPopup.progressFill.sprite != null)
                    _fillGreenSpr = cropPopup.progressFill.sprite;
                if (_btnBlueSpr == null && cropPopup.btnSpeedUp != null && cropPopup.btnSpeedUp.image != null
                    && cropPopup.btnSpeedUp.image.sprite != null)
                    _btnBlueSpr = cropPopup.btnSpeedUp.image.sprite;
                if (_diamondIconSpr == null && cropPopup.imgDiamondIcon != null && cropPopup.imgDiamondIcon.sprite != null)
                    _diamondIconSpr = cropPopup.imgDiamondIcon.sprite;
                if (_frameBgSpr == null)
                {
                    var cropImg = cropPopup.GetComponent<Image>();
                    if (cropImg != null && cropImg.sprite != null) _frameBgSpr = cropImg.sprite;
                }
                if (_trackBgSpr == null)
                {
                    var trackTr = cropPopup.transform.Find("Track_Bar");
                    if (trackTr != null)
                    {
                        var tImg = trackTr.GetComponent<Image>();
                        if (tImg != null && tImg.sprite != null) _trackBgSpr = tImg.sprite;
                    }
                }

                if (_frameBgSpr != null && _trackBgSpr != null && _fillGreenSpr != null
                    && _btnBlueSpr != null && _diamondIconSpr != null)
                    break;
            }
        }

        // [FIX 2026-09-06 B2] 2. Dự phòng runtime an toàn cho build thật (KHÔNG AssetDatabase —
        // đó là lỗi tiềm ẩn của PenProcessPopupUI, API đó chỉ chạy trong Editor và luôn null
        // trên Android). UIStandardSprites.Load() thử Resources.Load("UI/Standard/<tên>") trước
        // tiên — 5 file cần dùng đều đã có sẵn trong Assets/Resources/UI/Standard/ (đã kiểm),
        // nên chạy được cả trong build thật, không chỉ trong Editor.
        if (_frameBgSpr == null) _frameBgSpr = UIStandardSprites.PanelPaper;
        if (_trackBgSpr == null) _trackBgSpr = UIStandardSprites.BarTrack;
        if (_fillGreenSpr == null) _fillGreenSpr = UIStandardSprites.BarFill;
        if (_btnBlueSpr == null) _btnBlueSpr = UIStandardSprites.BtnGem;
        if (_diamondIconSpr == null) _diamondIconSpr = UIStandardSprites.IconGem;

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
        // [FIX 2026-09-06 B5] Khung/nền KHÔNG được ăn raycast — chỉ nút kim cương mới cần.
        // Đây là nguyên nhân chính khiến EventSystem báo "UI dưới con trỏ" và chặn kéo map
        // (UiBlockerProbe) dù popup chỉ rộng 360x130, không phải full-screen.
        _rootImg.raycastTarget = false;

        RectTransform rootRT = _root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(360f, 130f);

        // Tên nhà
        RectTransform nameRT = CreateRect(_root.transform, "Txt_Name", new Vector2(320f, 32f), new Vector2(0f, 36f));
        _txtName = CreateText(nameRT, "Nhà Dân", 24f, new Color(0.35f, 0.20f, 0.10f), _fontVo, TextAlignmentOptions.Center);

        // Thanh Track Background
        RectTransform trackRT = CreateRect(_root.transform, "Track_BG", new Vector2(200f, 26f), new Vector2(-50f, -12f));
        _trackImg = trackRT.gameObject.AddComponent<Image>();
        _trackImg.color = new Color(0.3f, 0.2f, 0.15f, 0.5f);
        _trackImg.raycastTarget = false; // [FIX 2026-09-06 B5]

        // Thanh Fill Green
        RectTransform fillRT = CreateRect(trackRT, "Fill_Green", new Vector2(196f, 22f), Vector2.zero);
        _fillImg = fillRT.gameObject.AddComponent<Image>();
        _fillImg.type = Image.Type.Filled;
        _fillImg.fillMethod = Image.FillMethod.Horizontal;
        _fillImg.fillOrigin = 0;
        _fillImg.color = new Color(0.35f, 0.85f, 0.25f, 1f);
        _fillImg.raycastTarget = false; // [FIX 2026-09-06 B5]

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
        _diamondIconImg.raycastTarget = false; // [FIX 2026-09-06 B5] icon trang trí, nút cha đã ăn raycast rồi

        // Số Gem
        RectTransform costRT = CreateRect(btnRT, "Txt_GemCost", new Vector2(36f, 28f), new Vector2(18f, 0f));
        _txtGemCost = CreateText(costRT, "1", 20f, Color.white, _fontVo, TextAlignmentOptions.Center);

        ApplyLoadedSprites();
        _root.SetActive(false);
    }

    private void ApplyLoadedSprites()
    {
        // [FIX 2026-09-06 B2/B6] _frameBgSpr/_trackBgSpr TỪNG LÀ FIELD CHẾT: khai báo nhưng
        // không hề được gán (LoadDesignAssets cũ) lẫn không hề được áp (ở đây) — đây chính là
        // lý do "mất nền, card bo góc, khung" dù popup vẫn mở đúng. Áp giống 3 sprite còn lại.
        if (_rootImg != null && _frameBgSpr != null)
        {
            _rootImg.sprite = _frameBgSpr;
            _rootImg.type = Image.Type.Sliced;
            _rootImg.color = Color.white;
        }
        if (_trackImg != null && _trackBgSpr != null)
        {
            _trackImg.sprite = _trackBgSpr;
            _trackImg.type = Image.Type.Sliced;
            _trackImg.color = Color.white;
        }
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
        txt.raycastTarget = false; // [FIX 2026-09-06 B5] nhãn chữ, không phải nút bấm
        return txt;
    }
}
