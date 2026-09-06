using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Popup process riêng cho Chuồng Nuôi — điêu khắc 100% ĐỒNG BỘ với CropProcessPopupUI (của ruộng).
/// Tự động load đúng Sprites (proc_frame_bg, proc_track_bg, proc_fill_green, proc_btn_blue, kimcuong) 
/// và Font (FontVo) từ hệ thống UI mới.
/// </summary>
public class PenProcessPopupUI : MonoBehaviour
{
    public static PenProcessPopupUI Instance { get; private set; }

    private PenMiniPanelUI _pen;
    private Canvas         _canvas;
    private GameObject     _root;
    private Image          _rootImg;
    private Image          _trackImg;
    private Image          _fillImg;
    private Image          _btnImg;
    private Image          _diamondIconImg;
    private TMP_Text       _txtName;
    private TMP_Text       _txtTime;
    private TMP_Text       _txtGemCost;
    private Button         _btnGem;
    private int            _openedAtFrame = -999;
    private bool           _inputLockHeld;

    // Assets loaded
    private Sprite         _frameBgSpr;
    private Sprite         _trackBgSpr;
    private Sprite         _fillGreenSpr;
    private Sprite         _btnBlueSpr;
    private Sprite         _diamondIconSpr;
    private TMP_FontAsset  _fontVo;

    // ── Lifecycle ────────────────────────────────────────────────────────────

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
        if (_pen == null || _pen.CurrentState != PenMiniPanelUI.PenState.Processing)
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

    // ── Public API ───────────────────────────────────────────────────────────

    public bool IsOpen => _root != null && _root.activeSelf;

    public void Open(PenMiniPanelUI pen)
    {
        // [FIX 2026-09-04] Chặn click xuyên khi đang ở Bếp (scene phụ load additive) / đang mở popup.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        if (pen == null || pen.CurrentState != PenMiniPanelUI.PenState.Processing) return;
        _pen = pen;
        _openedAtFrame = Time.frameCount;

        if (_frameBgSpr == null || _fillGreenSpr == null)
        {
            LoadDesignAssets();
        }

        ApplyLoadedSprites();

        RefreshDisplay();
        UpdateScreenPosition();
        _root.SetActive(true);
        AcquireInputLock();
    }

    private void ApplyLoadedSprites()
    {
        if (_rootImg != null && _frameBgSpr != null) { _rootImg.sprite = _frameBgSpr; _rootImg.type = Image.Type.Sliced; }
        if (_trackImg != null && _trackBgSpr != null) { _trackImg.sprite = _trackBgSpr; _trackImg.type = Image.Type.Sliced; }
        if (_fillImg != null && _fillGreenSpr != null) { _fillImg.sprite = _fillGreenSpr; }
        if (_btnImg != null && _btnBlueSpr != null) { _btnImg.sprite = _btnBlueSpr; _btnImg.type = Image.Type.Sliced; }
        
        if (_diamondIconImg != null)
        {
            if (_diamondIconSpr != null)
            {
                _diamondIconImg.sprite = _diamondIconSpr;
                _diamondIconImg.preserveAspect = true;
                _diamondIconImg.color = Color.white;
                _diamondIconImg.enabled = true;
            }
            else if (_diamondIconImg.sprite != null)
            {
                _diamondIconImg.preserveAspect = true;
                _diamondIconImg.color = Color.white;
                _diamondIconImg.enabled = true;
            }
            else
            {
                _diamondIconImg.enabled = false;
            }
        }
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
        _pen = null;
        ReleaseInputLock();
    }

    // ── Load Sprites & Font ──────────────────────────────────────────────────

    private void LoadDesignAssets()
    {
        // 1. FontVo
        _fontVo = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");

        // 2. Sprites từ Editor AssetDatabase
#if UNITY_EDITOR
        _frameBgSpr     = LoadSpriteAsset("Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_frame_bg.png");
        _trackBgSpr     = LoadSpriteAsset("Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_track_bg.png");
        _fillGreenSpr   = LoadSpriteAsset("Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_fill_green.png");
        _btnBlueSpr     = LoadSpriteAsset("Assets/Assetsgame/popup/ui_building_svg/generated_sprites/proc_btn_blue.png");
        _diamondIconSpr = LoadSpriteAsset("Assets/Assetsgame/kimcuong.png") ?? LoadSpriteAsset("Assets/Assetsgame/kimcuong-removebg-preview.png");
#endif

        // Fallback: nếu chưa load được (vd: runtime), lấy trực tiếp từ CropProcessPopupUI hoặc UI khác trong Scene
        if (_diamondIconSpr == null || _frameBgSpr == null)
        {
            var cropPopups = FindObjectsByType<CropProcessPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cropPopup in cropPopups)
            {
                cropPopup.AutoBindComponents();
                if (_fillGreenSpr == null && cropPopup.progressFill != null) _fillGreenSpr = cropPopup.progressFill.sprite;
                if (_btnBlueSpr == null && cropPopup.btnSpeedUp != null && cropPopup.btnSpeedUp.image != null) _btnBlueSpr = cropPopup.btnSpeedUp.image.sprite;
                if (_diamondIconSpr == null && cropPopup.imgDiamondIcon != null && cropPopup.imgDiamondIcon.sprite != null) _diamondIconSpr = cropPopup.imgDiamondIcon.sprite;
                if (_frameBgSpr == null)
                {
                    var cropImg = cropPopup.GetComponent<Image>();
                    if (cropImg != null) _frameBgSpr = cropImg.sprite;
                }
                if (_trackBgSpr == null)
                {
                    var trackTr = cropPopup.transform.Find("Track_Bar");
                    if (trackTr != null)
                    {
                        var tImg = trackTr.GetComponent<Image>();
                        if (tImg != null) _trackBgSpr = tImg.sprite;
                    }
                }
            }
        }
    }

    private static Sprite LoadSpriteAsset(string path)
    {
#if UNITY_EDITOR
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return spr;
#else
        return null;
#endif
    }


    // ── UI Builder (Điêu khắc 1:1 theo BuildingProcessUIBuilderTool) ────────

    private void BuildUI()
    {
        // Canvas Screen Space Overlay — luôn ở trên cùng
        var canvasGO = new GameObject("PenProcessCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Root Panel (Size 360 x 84)
        _root = new GameObject("PenProcessPopup", typeof(RectTransform));
        _root.transform.SetParent(canvasGO.transform, false);

        var rootRt = _root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(360f, 84f);
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot     = new Vector2(0.5f, 0f);

        // Khung Nền Kem Viền Gỗ Nâu (Frame Base)
        _rootImg = _root.AddComponent<Image>();
        if (_frameBgSpr != null)
        {
            _rootImg.sprite = _frameBgSpr;
            _rootImg.type   = Image.Type.Sliced;
        }
        _rootImg.color = Color.white;
        _rootImg.raycastTarget = true;

        // 1. Tên Chuồng (Header Text) - Pos (-48, 52), Size (230, 28)
        RectTransform nameRect = CreateRect(_root.transform, "Txt_CropName", new Vector2(230f, 28f), new Vector2(-48f, 52f));
        _txtName = CreateText(nameRect, "CHUỒNG GÀ", 22f, Color.white, _fontVo, TextAlignmentOptions.Center);
        var nameOutline = nameRect.gameObject.AddComponent<Outline>();
        nameOutline.effectColor    = new Color(0.2f, 0.12f, 0.05f, 1f);
        nameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 2. Rãnh Tiến Độ Nâu (Track Bar) - Pos (-48, 0), Size (230, 38)
        RectTransform trackRect = CreateRect(_root.transform, "Track_Bar", new Vector2(230f, 38f), new Vector2(-48f, 0f));
        _trackImg = trackRect.gameObject.AddComponent<Image>();
        if (_trackBgSpr != null)
        {
            _trackImg.sprite = _trackBgSpr;
            _trackImg.type   = Image.Type.Sliced;
        }
        _trackImg.color = Color.white;
        _trackImg.raycastTarget = false;

        // 3. Thanh Xanh Lá Gradient 3D Fill - Pos (0, 0), Size (222, 30)
        RectTransform fillRect = CreateRect(trackRect, "Progress_Fill", new Vector2(222f, 30f), Vector2.zero);
        _fillImg = fillRect.gameObject.AddComponent<Image>();
        if (_fillGreenSpr != null)
            _fillImg.sprite = _fillGreenSpr;
        _fillImg.type        = Image.Type.Filled;
        _fillImg.fillMethod  = Image.FillMethod.Horizontal;
        _fillImg.fillOrigin  = (int)Image.OriginHorizontal.Left;
        _fillImg.fillAmount  = 0.5f;
        _fillImg.color       = Color.white;
        _fillImg.raycastTarget = false;

        // 4. Text Thời Gian Còn Lại ("00:45") - Pos (0, 0), Size (210, 30)
        RectTransform timeRect = CreateRect(trackRect, "Txt_TimeRemaining", new Vector2(210f, 30f), Vector2.zero);
        _txtTime = CreateText(timeRect, "00:45", 20f, Color.white, _fontVo, TextAlignmentOptions.Center);
        var timeOutline = timeRect.gameObject.AddComponent<Outline>();
        timeOutline.effectColor    = new Color(0.18f, 0.31f, 0.06f, 1f);
        timeOutline.effectDistance = new Vector2(1f, -1f);

        // 5. Nút Kim Cương Xanh Dương 3D (Btn_SpeedUp) - Pos (124, 0), Size (88, 60)
        RectTransform btnRect = CreateRect(_root.transform, "Btn_SpeedUp", new Vector2(88f, 60f), new Vector2(124f, 0f));
        _btnImg = btnRect.gameObject.AddComponent<Image>();
        if (_btnBlueSpr != null)
        {
            _btnImg.sprite = _btnBlueSpr;
            _btnImg.type   = Image.Type.Sliced;
        }
        _btnImg.color = Color.white;
        _btnGem = btnRect.gameObject.AddComponent<Button>();
        _btnGem.targetGraphic = _btnImg;
        _btnGem.onClick.AddListener(OnSpeedUpClicked);

        // 5a. Icon Kim Cương - Pos (-16, 0), Size (32, 32)
        RectTransform diaIconRect = CreateRect(btnRect, "Icon_Diamond", new Vector2(32f, 32f), new Vector2(-16f, 0f));
        _diamondIconImg = diaIconRect.gameObject.AddComponent<Image>();
        if (_diamondIconSpr != null)
            _diamondIconImg.sprite = _diamondIconSpr;
        _diamondIconImg.preserveAspect = true;
        _diamondIconImg.color          = Color.white;
        _diamondIconImg.raycastTarget  = false;

        // 5b. Text Số Lượng Kim Cương Cần Dùng - Pos (18, 0), Size (36, 30)
        RectTransform costRect = CreateRect(btnRect, "Txt_GemCost", new Vector2(36f, 30f), new Vector2(18f, 0f));
        _txtGemCost = CreateText(costRect, "1", 22f, Color.white, _fontVo, TextAlignmentOptions.Center);
        var costOutline = costRect.gameObject.AddComponent<Outline>();
        costOutline.effectColor    = new Color(0.11f, 0.36f, 0.53f, 1f);
        costOutline.effectDistance = new Vector2(1f, -1f);

        _root.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (_pen == null) return;

        if (_txtName    != null) _txtName.text    = _pen.GetPenDisplayName();
        if (_txtGemCost != null) _txtGemCost.text = _pen.SpeedUpGemCost.ToString();

        float remaining = _pen.GetRemainingSeconds();
        float total     = Mathf.Max(1f, _pen.EffectiveFeedSeconds);

        if (_fillImg != null) _fillImg.fillAmount = Mathf.Clamp01(1f - remaining / total);
        if (_txtTime != null)
        {
            int m = Mathf.FloorToInt(remaining / 60f);
            int s = Mathf.FloorToInt(remaining % 60f);
            _txtTime.text = $"{m:D2}:{s:D2}";
        }
    }

    private void UpdateScreenPosition()
    {
        if (_pen == null || _canvas == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos  = _pen.transform.position + new Vector3(0f, 1.85f, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) return;

        var canvasRt = _canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out Vector2 localPos))
        {
            _root.GetComponent<RectTransform>().anchoredPosition = localPos;
        }
    }

    private void OnSpeedUpClicked()
    {
        if (_pen == null) return;
        _pen.TrySpeedUpGem();
        Close();
    }

    private bool IsPointerOverRoot()
    {
        if (_root == null) return false;
        var rt = _root.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
    }

    private void AcquireInputLock()
    {
        if (_inputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _inputLockHeld = true;
    }

    private void ReleaseInputLock()
    {
        if (!_inputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _inputLockHeld = false;
    }

    private void OnDisable() => ReleaseInputLock();

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static TMP_Text CreateText(Transform parent, string defaultText, float fontSize,
        Color color, TMP_FontAsset font, TextAlignmentOptions align)
    {
        var txt = parent.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) txt.font = font;
        txt.text          = defaultText;
        txt.fontSize      = fontSize;
        txt.color         = color;
        txt.alignment     = align;
        txt.raycastTarget = false;
        return txt;
    }
}
