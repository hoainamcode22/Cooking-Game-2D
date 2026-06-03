using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JudgeAvatarProfileButton : MonoBehaviour
{
    [Header("Click")]
    [SerializeField] private Button openProfileButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private AvatarProfilePopupUI popupUI;

    [Header("Avatar")]
    [SerializeField] private Image avatarFrame;
    [SerializeField] private Image avatarImage;

    [Header("Circular EXP")]
    [SerializeField] private Image circleExpFill;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtExpLabel;

    [Header("Options")]
    [SerializeField] private string expLabel = "EXP";

    private bool started;

    private void Reset()
    {
        AutoWireFromChildren();
        ConfigureCircleFill();
    }

    private void Awake()
    {
        AutoWireFromChildren();
        ConfigureCircleFill();
    }

    private void Start()
    {
        started = true;
        BindEvents();
        SubscribeProgress();
        RefreshImmediate();
    }

    private void OnEnable()
    {
        BindEvents();
        SubscribeProgress();

        if (started)
            RefreshImmediate();
    }

    private void OnDisable()
    {
        if (openProfileButton != null)
            openProfileButton.onClick.RemoveListener(OpenProfilePopup);

        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged -= HandleExpChanged;
        }
    }

    private void OnValidate()
    {
        ConfigureCircleFill();

        if (txtExpLabel != null)
            txtExpLabel.text = expLabel;
    }

    public void OpenProfilePopup()
    {
        if (popupUI == null)
            popupUI = AvatarProfilePopupUI.FindOrCreate(avatarImage);

        if (popupUI != null)
        {
            popupUI.SetOutsideAvatar(avatarImage);
            popupUI.OpenPopup();
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            Debug.Log("[JudgeAvatarProfileButton] Popup avatar is not ready.");
    }

    public void RefreshImmediate()
    {
        if (PlayerProgressManager.Instance == null)
            return;

        int level = PlayerProgressManager.Instance.Level;
        int currentExp = PlayerProgressManager.Instance.CurrentExp;
        int requiredExp = PlayerProgressManager.Instance.RequiredExpCurrentLevel;

        if (txtLevel != null)
            txtLevel.text = level.ToString();

        if (txtExpLabel != null)
            txtExpLabel.text = expLabel;

        ApplyExpFill(currentExp, requiredExp);
    }

    private void AutoWireFromChildren()
    {
        if (avatarFrame == null)
            avatarFrame = GetComponent<Image>();

        if (avatarImage == null)
        {
            Transform avatar = transform.Find("Avata");
            if (avatar == null)
                avatar = transform.Find("AvatarImage");

            if (avatar != null)
                avatarImage = avatar.GetComponent<Image>();
        }

        if (circleExpFill == null)
        {
            Transform fill = transform.Find("Img_CircleExpFill");
            if (fill != null)
                circleExpFill = fill.GetComponent<Image>();
        }

        if (openProfileButton == null)
        {
            Transform hitArea = transform.Find("Button_OpenAvatarProfile");
            if (hitArea != null)
                openProfileButton = hitArea.GetComponent<Button>();
        }

        if (txtExpLabel == null)
        {
            Transform label = transform.Find("Txt_EXP");
            if (label != null)
                txtExpLabel = label.GetComponent<TMP_Text>();
        }

        if (popupUI == null && popupRoot != null)
            popupUI = popupRoot.GetComponent<AvatarProfilePopupUI>();
    }

    private void ConfigureCircleFill()
    {
        if (circleExpFill == null)
            return;

        circleExpFill.type = Image.Type.Filled;
        circleExpFill.fillMethod = Image.FillMethod.Radial360;
        circleExpFill.fillOrigin = (int)Image.Origin360.Top;
        circleExpFill.fillClockwise = true;
        circleExpFill.preserveAspect = true;
        circleExpFill.raycastTarget = false;
    }

    private void BindEvents()
    {
        if (openProfileButton == null)
            return;

        openProfileButton.onClick.RemoveListener(OpenProfilePopup);
        openProfileButton.onClick.AddListener(OpenProfilePopup);
    }

    private void SubscribeProgress()
    {
        if (PlayerProgressManager.Instance == null)
            return;

        PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged -= HandleExpChanged;
        PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged += HandleExpChanged;
    }

    private void HandleLevelChanged(int level)
    {
        if (txtLevel != null)
            txtLevel.text = level.ToString();

        RefreshImmediate();
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        ApplyExpFill(currentExp, requiredExp);
    }

    private void ApplyExpFill(int currentExp, int requiredExp)
    {
        if (circleExpFill == null)
            return;

        circleExpFill.fillAmount = requiredExp <= 0
            ? 1f
            : Mathf.Clamp01((float)currentExp / requiredExp);
    }
}
