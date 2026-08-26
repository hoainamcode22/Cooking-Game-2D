using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExportTrainUIPackage
{
    public class TrainProcessPopupUI : MonoBehaviour
    {
        public static TrainProcessPopupUI Instance { get; private set; }

        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";

        [Header("Header & Panels")]
        public Image imgFrameWood;
        public Image imgPanelPaper;
        public Image imgRibbonBanner;
        public TextMeshProUGUI txtTitle;
        public Button btnClose;

        [Header("Mini Track & Train Animation")]
        public RectTransform trackBox;
        public Image imgTrackBox;
        public RectTransform miniTrain;
        public Image imgMiniTrain;
        public Image imgSmokePuff;

        [Header("Status & Timer")]
        public TextMeshProUGUI txtStatus;
        public GameObject timerBox;
        public Image imgTimerBox;
        public TextMeshProUGUI txtTimer;
        public Image imgProgressBar;

        [Header("Sent Cargo Chips")]
        public GameObject cargoChipsContainer;
        public Image[] chipContainers = new Image[3];
        public Image[] chipIcons = new Image[3];
        public TextMeshProUGUI[] chipAmounts = new TextMeshProUGUI[3];

        [Header("Buttons")]
        public Button btnSpeedUp;
        public Image imgSpeedUp;
        public TextMeshProUGUI txtSpeedUp;
        public Button btnRaGa;
        public Image imgRaGa;
        public TextMeshProUGUI txtRaGa;

        [Header("Timer State")]
        public float totalDuration = 134f; // 2:14
        public float remainingTime = 134f;
        public bool isArrived => remainingTime <= 0f;

        private void Awake()
        {
            Instance = this;
            AutoBindComponents();
            if (btnClose != null)
                btnClose.onClick.AddListener(ClosePopup);
            if (btnSpeedUp != null)
                btnSpeedUp.onClick.AddListener(OnSpeedUpClicked);
            if (btnRaGa != null)
                btnRaGa.onClick.AddListener(OnRaGaClicked);
        }

        private void OnEnable()
        {
            ApplyThemeSprites();
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime <= 0f)
                {
                    remainingTime = 0f;
                    OnTrainArrived();
                }
                UpdateTimerAndTrainPosition();
            }
        }

        public void OpenPopup(float duration = 134f)
        {
            totalDuration = duration;
            remainingTime = duration;
            gameObject.SetActive(true);
            ApplyThemeSprites();
            RefreshUI();
        }

        public void ClosePopup()
        {
            gameObject.SetActive(false);
        }

        public void ApplyThemeSprites()
        {
            AutoBindComponents();

            if (imgFrameWood != null)
            {
                imgFrameWood.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
                imgFrameWood.type = Image.Type.Sliced;
                imgFrameWood.color = Color.white;
            }

            if (imgPanelPaper != null)
            {
                imgPanelPaper.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_panel_paper.png");
                imgPanelPaper.type = Image.Type.Sliced;
                imgPanelPaper.color = Color.white;
            }

            if (imgRibbonBanner != null)
            {
                imgRibbonBanner.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
                imgRibbonBanner.type = Image.Type.Sliced;
                imgRibbonBanner.color = Color.white;
            }

            if (btnClose != null)
            {
                var img = btnClose.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = TrainSpriteLoader.GetSprite("Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png")
                              ?? TrainSpriteLoader.GetSprite("Assets/Assetsgame/btnX.png");
                    img.preserveAspect = true;
                }
            }

            if (imgTrackBox != null)
            {
                imgTrackBox.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/mini_train_track_bg.png");
                imgTrackBox.type = Image.Type.Sliced;
                imgTrackBox.color = Color.white;
            }

            if (imgMiniTrain != null)
            {
                imgMiniTrain.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/train_popup_mini_horizontal.png");
                imgMiniTrain.preserveAspect = true;
                imgMiniTrain.color = Color.white;
            }

            if (imgTimerBox != null)
            {
                imgTimerBox.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/timer_box_dark.png");
                imgTimerBox.type = Image.Type.Sliced;
                imgTimerBox.color = Color.white;
            }

            if (imgSpeedUp != null)
            {
                imgSpeedUp.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_blue_gem_3d.png");
                imgSpeedUp.type = Image.Type.Sliced;
                imgSpeedUp.color = Color.white;
            }

            if (imgRaGa != null)
            {
                imgRaGa.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_green_3d.png");
                imgRaGa.type = Image.Type.Sliced;
                imgRaGa.color = Color.white;
            }

            for (int i = 0; i < chipContainers.Length; i++)
            {
                if (chipContainers[i] != null)
                {
                    chipContainers[i].sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
                    chipContainers[i].type = Image.Type.Sliced;
                    chipContainers[i].color = Color.white;
                }
            }
        }

        public void RefreshUI()
        {
            ApplyThemeSprites();

            if (isArrived)
            {
                if (txtTitle != null) txtTitle.text = "TÀU ĐÃ VỀ!";
                if (txtStatus != null)
                {
                    txtStatus.text = "Tàu đã về — có hàng cho bạn!";
                    txtStatus.color = new Color(0.30f, 0.56f, 0.11f);
                }
                if (txtTimer != null) txtTimer.text = "00:00";
                if (btnSpeedUp != null) btnSpeedUp.gameObject.SetActive(false);
                if (btnRaGa != null) btnRaGa.gameObject.SetActive(true);
            }
            else
            {
                if (txtTitle != null) txtTitle.text = "ĐANG VẬN CHUYỂN";
                if (txtStatus != null)
                {
                    txtStatus.text = "Đang vận chuyển...";
                    txtStatus.color = new Color(0.36f, 0.20f, 0.09f);
                }
                if (btnSpeedUp != null) btnSpeedUp.gameObject.SetActive(true);
                if (btnRaGa != null) btnRaGa.gameObject.SetActive(false);
            }

            // Setup cargo chips
            for (int i = 0; i < 3; i++)
            {
                if (i < TrainItemDatabase.SampleCrops.Count)
                {
                    var req = TrainItemDatabase.SampleCrops[i];
                    if (chipAmounts[i] != null) chipAmounts[i].text = $"x{req.targetAmount}";
                    if (chipIcons[i] != null)
                    {
                        chipIcons[i].sprite = TrainSpriteLoader.GetSprite(req.iconPath);
                        chipIcons[i].color = Color.white;
                        chipIcons[i].enabled = true;
                    }
                }
            }

            UpdateTimerAndTrainPosition();
        }

        private void UpdateTimerAndTrainPosition()
        {
            int mins = Mathf.FloorToInt(remainingTime / 60f);
            int secs = Mathf.FloorToInt(remainingTime % 60f);
            if (txtTimer != null)
                txtTimer.text = $"{mins:00}:{secs:00}";

            float progress = Mathf.Clamp01(1f - (remainingTime / totalDuration));
            if (imgProgressBar != null)
                imgProgressBar.fillAmount = progress;

            if (miniTrain != null)
            {
                float posX = Mathf.Lerp(-140f, 140f, progress);
                miniTrain.anchoredPosition = new Vector2(posX, miniTrain.anchoredPosition.y);
            }
        }

        private void OnSpeedUpClicked()
        {
            remainingTime = 0f;
            OnTrainArrived();
        }

        private void OnTrainArrived()
        {
            RefreshUI();
        }

        private void OnRaGaClicked()
        {
            ClosePopup();
            var masterPopup = TrainStationMasterPopupUI.Instance
                ?? FindFirstObjectByType<TrainStationMasterPopupUI>(FindObjectsInactive.Include);

            if (masterPopup != null)
            {
                masterPopup.OpenPopup(TrainState.RewardReadyToCollect);
            }
        }

        public void AutoBindComponents()
        {
            if (imgFrameWood == null) imgFrameWood = GetComponent<Image>();
            if (imgPanelPaper == null) imgPanelPaper = transform.Find("Paper_Panel")?.GetComponent<Image>();
            if (imgRibbonBanner == null) imgRibbonBanner = transform.Find("Ribbon_Banner")?.GetComponent<Image>();
            if (txtTitle == null) txtTitle = transform.Find("Ribbon_Banner/Txt_Title")?.GetComponent<TextMeshProUGUI>();
            if (btnClose == null) btnClose = transform.Find("Btn_close")?.GetComponent<Button>();

            if (trackBox == null) trackBox = transform.Find("Paper_Panel/Mini_Track_Box")?.GetComponent<RectTransform>();
            if (imgTrackBox == null && trackBox != null) imgTrackBox = trackBox.GetComponent<Image>();
            if (miniTrain == null) miniTrain = transform.Find("Paper_Panel/Mini_Track_Box/Mini_Train")?.GetComponent<RectTransform>();
            if (imgMiniTrain == null && miniTrain != null) imgMiniTrain = miniTrain.GetComponent<Image>();

            if (txtStatus == null) txtStatus = transform.Find("Paper_Panel/Txt_Status")?.GetComponent<TextMeshProUGUI>();
            if (timerBox == null) timerBox = transform.Find("Paper_Panel/Timer_Box")?.gameObject;
            if (imgTimerBox == null && timerBox != null) imgTimerBox = timerBox.GetComponent<Image>();
            if (txtTimer == null) txtTimer = transform.Find("Paper_Panel/Timer_Box/Txt_Time")?.GetComponent<TextMeshProUGUI>();
            if (imgProgressBar == null) imgProgressBar = transform.Find("Paper_Panel/Progress_Bar/Fill")?.GetComponent<Image>();

            if (cargoChipsContainer == null) cargoChipsContainer = transform.Find("Paper_Panel/Cargo_Chips")?.gameObject;
            if (cargoChipsContainer != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var c = cargoChipsContainer.transform.Find($"Chip_{i + 1}");
                    if (c != null)
                    {
                        chipContainers[i] = c.GetComponent<Image>();
                        chipIcons[i] = c.Find("Img_Icon")?.GetComponent<Image>();
                        chipAmounts[i] = c.Find("Txt_Amount")?.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            if (btnSpeedUp == null) btnSpeedUp = transform.Find("Paper_Panel/Btn_SpeedUp")?.GetComponent<Button>();
            if (imgSpeedUp == null && btnSpeedUp != null) imgSpeedUp = btnSpeedUp.GetComponent<Image>();
            if (txtSpeedUp == null) txtSpeedUp = transform.Find("Paper_Panel/Btn_SpeedUp/Txt_SpeedUp")?.GetComponent<TextMeshProUGUI>();

            if (btnRaGa == null) btnRaGa = transform.Find("Paper_Panel/Btn_RaGa")?.GetComponent<Button>();
            if (imgRaGa == null && btnRaGa != null) imgRaGa = btnRaGa.GetComponent<Image>();
            if (txtRaGa == null) txtRaGa = transform.Find("Paper_Panel/Btn_RaGa/Txt_RaGa")?.GetComponent<TextMeshProUGUI>();
        }
    }
}
