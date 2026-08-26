using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExportTrainUIPackage
{
    public class TrainLoadPopupUI : MonoBehaviour
    {
        public static TrainLoadPopupUI Instance { get; private set; }

        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";
        private const string ShopSvgDir = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";

        [Header("Frame & Panel")]
        public Image imgFrameWood;
        public Image imgPanelPaper;
        public Image imgRibbonBanner;
        public TextMeshProUGUI txtTitle;
        public Button btnClose;

        [Header("Wagon Tag & Info")]
        public TextMeshProUGUI txtWagonTag;
        public Image imgIconDisc;
        public Image imgIcon;
        public TextMeshProUGUI txtItemName;
        public TextMeshProUGUI txtStock;

        [Header("Progress Bar")]
        public Image imgProgressTrack;
        public Image imgProgressFill;
        public TextMeshProUGUI txtProgressAmount;

        [Header("Buttons State 2 (Loading)")]
        public Button btnThemHang;
        public Image imgThemHang;
        public TextMeshProUGUI txtThemHang;
        public Button btnNapTatCa;
        public Image imgNapTatCa;
        public TextMeshProUGUI txtNapTatCa;

        [Header("Buttons State 3 (Complete)")]
        public GameObject btnDaDuHang;
        public Image imgDaDuHang;
        public TextMeshProUGUI txtDaDuHang;
        public GameObject noteBox;
        public Image imgNoteBox;
        public Image imgNoteIcon;
        public TextMeshProUGUI txtNote;

        [Header("Current Target")]
        public int currentWagonIndex = 0;
        public int stockCount = 0;

        private void Awake()
        {
            Instance = this;
            if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
            if (btnThemHang != null) btnThemHang.onClick.AddListener(OnAddSingleCargo);
            if (btnNapTatCa != null) btnNapTatCa.onClick.AddListener(OnAddAllCargo);
        }

        private void OnEnable()
        {
            ApplyThemeSprites();
            RefreshUI();
        }

        public void OpenForWagon(int wagonIndex)
        {
            currentWagonIndex = wagonIndex;
            gameObject.SetActive(true);
            ApplyThemeSprites();
            RefreshUI();
        }

        public void ClosePopup()
        {
            gameObject.SetActive(false);
            if (TrainStationMasterPopupUI.Instance != null)
            {
                TrainStationMasterPopupUI.Instance.RefreshUI();
                TrainStationMasterPopupUI.Instance.CheckAndTriggerDepartureIfAllComplete();
            }
        }

        public void ApplyThemeSprites()
        {
            AutoBindComponents();

            if (imgFrameWood != null)
            {
                imgFrameWood.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_panel.png")
                                   ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
                imgFrameWood.type = Image.Type.Sliced;
                imgFrameWood.color = Color.white;
            }

            if (imgPanelPaper != null)
            {
                imgPanelPaper.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_inner.png")
                                    ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_panel_paper.png");
                imgPanelPaper.type = Image.Type.Sliced;
                imgPanelPaper.color = Color.white;
            }

            if (imgRibbonBanner != null)
            {
                imgRibbonBanner.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_banner_ribbon.png")
                                      ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
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

            if (imgIconDisc != null)
            {
                imgIconDisc.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/icon_disc_large.png");
                imgIconDisc.color = Color.white;
                imgIconDisc.preserveAspect = true;
            }

            if (imgProgressTrack != null)
            {
                imgProgressTrack.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/progress_track_bar.png");
                imgProgressTrack.type = Image.Type.Sliced;
                imgProgressTrack.color = Color.white;
            }

            if (imgProgressFill != null)
            {
                imgProgressFill.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/progress_fill_green.png");
                imgProgressFill.type = Image.Type.Filled;
                imgProgressFill.fillMethod = Image.FillMethod.Horizontal;
                imgProgressFill.color = Color.white;
            }

            if (imgThemHang != null)
            {
                imgThemHang.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_green_3d.png");
                imgThemHang.type = Image.Type.Sliced;
                imgThemHang.color = Color.white;
            }

            if (imgNapTatCa != null)
            {
                imgNapTatCa.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_yellow_3d.png");
                imgNapTatCa.type = Image.Type.Sliced;
                imgNapTatCa.color = Color.white;
            }

            if (imgDaDuHang != null)
            {
                imgDaDuHang.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_disabled_3d.png");
                imgDaDuHang.type = Image.Type.Sliced;
                imgDaDuHang.color = Color.white;
            }

            if (imgNoteBox != null)
            {
                imgNoteBox.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                                 ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
                imgNoteBox.type = Image.Type.Sliced;
                imgNoteBox.color = Color.white;
            }
        }

        public void RefreshUI()
        {
            ApplyThemeSprites();

            int dataIdx = Mathf.Clamp(currentWagonIndex, 0, TrainItemDatabase.SampleCrops.Count - 1);
            var req = TrainItemDatabase.SampleCrops[dataIdx];

            // ĐỌC THẬT TỪ KHO HÀNG (FarmInventoryManager)
            stockCount = (FarmInventoryManager.Instance != null) 
                ? FarmInventoryManager.Instance.GetAmount(req.itemId) 
                : 0;

            bool isDone = req.isComplete;

            if (txtWagonTag != null)
            {
                txtWagonTag.text = isDone ? $"Toa số {currentWagonIndex + 1} / 4 — đã đủ hàng" : $"Toa số {currentWagonIndex + 1} / 4";
            }

            if (txtItemName != null) txtItemName.text = req.itemName;
            
            if (txtStock != null)
            {
                txtStock.text = $"Trong kho: x{stockCount}";
                txtStock.color = (stockCount > 0) ? new Color(0.54f, 0.39f, 0.22f) : new Color(0.85f, 0.25f, 0.20f);
            }

            // Progress bar
            float fillPct = Mathf.Clamp01((float)req.currentAmount / req.targetAmount);
            if (imgProgressFill != null)
            {
                imgProgressFill.fillAmount = fillPct;
            }

            if (txtProgressAmount != null)
            {
                txtProgressAmount.text = $"{req.currentAmount} / {req.targetAmount}";
            }

            // Load icon
            if (imgIcon != null)
            {
                imgIcon.sprite = TrainSpriteLoader.GetSprite(req.iconPath);
                imgIcon.color = Color.white;
                imgIcon.enabled = true;
            }

            // Button states (Screen 2 vs Screen 3)
            if (isDone)
            {
                if (btnThemHang != null) btnThemHang.gameObject.SetActive(false);
                if (btnNapTatCa != null) btnNapTatCa.gameObject.SetActive(false);
                if (btnDaDuHang != null) btnDaDuHang.SetActive(true);
                if (noteBox != null)
                {
                    noteBox.SetActive(true);
                    int remaining = CountIncompleteWagons();
                    if (txtNote != null)
                    {
                        txtNote.text = remaining > 0
                            ? $"Còn {remaining} toa chưa đủ — nạp xong các toa yêu cầu, tàu sẽ khởi hành."
                            : "Đã nạp đủ tất cả toa! Tàu đang chuẩn bị khởi hành vận chuyển.";
                    }
                    if (imgNoteIcon != null)
                    {
                        imgNoteIcon.sprite = TrainSpriteLoader.GetSprite(req.iconPath);
                    }
                }
            }
            else
            {
                if (btnThemHang != null)
                {
                    btnThemHang.gameObject.SetActive(true);
                    btnThemHang.interactable = (stockCount > 0);
                }
                if (btnNapTatCa != null)
                {
                    btnNapTatCa.gameObject.SetActive(true);
                    btnNapTatCa.interactable = (stockCount > 0);
                }
                if (btnDaDuHang != null) btnDaDuHang.SetActive(false);
                if (noteBox != null) noteBox.SetActive(false);
            }
        }

        private void OnAddSingleCargo()
        {
            int dataIdx = Mathf.Clamp(currentWagonIndex, 0, TrainItemDatabase.SampleCrops.Count - 1);
            var req = TrainItemDatabase.SampleCrops[dataIdx];

            if (req.currentAmount >= req.targetAmount) return;

            // KIỂM TRA VÀ TRỪ TRỰC TIẾP TRONG KHO THẬT
            if (FarmInventoryManager.Instance != null)
            {
                if (FarmInventoryManager.Instance.RemoveItem(req.itemId, 1))
                {
                    req.currentAmount++;
                    RefreshUI();
                    CheckAllCargoComplete();
                }
                else
                {
                    Debug.LogWarning($"[Train] Không đủ vật phẩm '{req.itemName}' ({req.itemId}) trong kho!");
                }
            }
            else
            {
                Debug.LogWarning("[Train] FarmInventoryManager chưa khởi tạo!");
            }
        }

        private void OnAddAllCargo()
        {
            int dataIdx = Mathf.Clamp(currentWagonIndex, 0, TrainItemDatabase.SampleCrops.Count - 1);
            var req = TrainItemDatabase.SampleCrops[dataIdx];

            int needed = req.targetAmount - req.currentAmount;
            if (needed <= 0) return;

            if (FarmInventoryManager.Instance != null)
            {
                int inStock = FarmInventoryManager.Instance.GetAmount(req.itemId);
                int toAdd = Mathf.Min(needed, inStock);

                if (toAdd > 0 && FarmInventoryManager.Instance.RemoveItem(req.itemId, toAdd))
                {
                    req.currentAmount += toAdd;
                    RefreshUI();
                    CheckAllCargoComplete();
                }
                else
                {
                    Debug.LogWarning($"[Train] Không có đủ '{req.itemName}' trong kho để nạp tất cả!");
                }
            }
        }

        private int CountIncompleteWagons()
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i < TrainItemDatabase.SampleCrops.Count && !TrainItemDatabase.SampleCrops[i].isComplete)
                    count++;
            }
            return count;
        }

        private void CheckAllCargoComplete()
        {
            if (CountIncompleteWagons() == 0)
            {
                Invoke(nameof(TransitionToTransit), 1.0f);
            }
        }

        private void TransitionToTransit()
        {
            ClosePopup();
            if (TrainStationMasterPopupUI.Instance != null)
                TrainStationMasterPopupUI.Instance.ClosePopup();

            var procPopup = TrainProcessPopupUI.Instance
                ?? FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);

            if (procPopup != null)
            {
                procPopup.OpenPopup(134f);
            }
        }

        public void AutoBindComponents()
        {
            if (imgFrameWood == null) imgFrameWood = GetComponent<Image>();
            if (imgPanelPaper == null) imgPanelPaper = transform.Find("Paper_Panel")?.GetComponent<Image>();
            if (imgRibbonBanner == null) imgRibbonBanner = transform.Find("Ribbon_Banner")?.GetComponent<Image>();
            if (txtTitle == null) txtTitle = transform.Find("Ribbon_Banner/Txt_Title")?.GetComponent<TextMeshProUGUI>();
            if (btnClose == null) btnClose = transform.Find("Btn_close")?.GetComponent<Button>();

            if (txtWagonTag == null) txtWagonTag = transform.Find("Paper_Panel/Txt_WagonTag")?.GetComponent<TextMeshProUGUI>();
            if (imgIconDisc == null) imgIconDisc = transform.Find("Paper_Panel/Icon_Disc")?.GetComponent<Image>();
            if (imgIcon == null) imgIcon = transform.Find("Paper_Panel/Icon_Disc/Img_Icon")?.GetComponent<Image>();
            if (txtItemName == null) txtItemName = transform.Find("Paper_Panel/Txt_ItemName")?.GetComponent<TextMeshProUGUI>();
            if (txtStock == null) txtStock = transform.Find("Paper_Panel/Txt_Stock")?.GetComponent<TextMeshProUGUI>();

            if (imgProgressTrack == null) imgProgressTrack = transform.Find("Paper_Panel/Progress_Track")?.GetComponent<Image>();
            if (imgProgressFill == null) imgProgressFill = transform.Find("Paper_Panel/Progress_Track/Progress_Fill")?.GetComponent<Image>();
            if (txtProgressAmount == null) txtProgressAmount = transform.Find("Paper_Panel/Progress_Track/Txt_Soluong")?.GetComponent<TextMeshProUGUI>();

            if (btnThemHang == null) btnThemHang = transform.Find("Paper_Panel/Btn_themhang")?.GetComponent<Button>();
            if (imgThemHang == null && btnThemHang != null) imgThemHang = btnThemHang.GetComponent<Image>();
            if (txtThemHang == null) txtThemHang = transform.Find("Paper_Panel/Btn_themhang/Txt_Them")?.GetComponent<TextMeshProUGUI>();

            if (btnNapTatCa == null) btnNapTatCa = transform.Find("Paper_Panel/Btn_napTatCa")?.GetComponent<Button>();
            if (imgNapTatCa == null && btnNapTatCa != null) imgNapTatCa = btnNapTatCa.GetComponent<Image>();
            if (txtNapTatCa == null) txtNapTatCa = transform.Find("Paper_Panel/Btn_napTatCa/Txt_All")?.GetComponent<TextMeshProUGUI>();

            if (btnDaDuHang == null) btnDaDuHang = transform.Find("Paper_Panel/Btn_DaDuHang")?.gameObject;
            if (imgDaDuHang == null && btnDaDuHang != null) imgDaDuHang = btnDaDuHang.GetComponent<Image>();
            if (txtDaDuHang == null) txtDaDuHang = transform.Find("Paper_Panel/Btn_DaDuHang/Txt_Label")?.GetComponent<TextMeshProUGUI>();

            if (noteBox == null) noteBox = transform.Find("Paper_Panel/Note_Box")?.gameObject;
            if (imgNoteBox == null && noteBox != null) imgNoteBox = noteBox.GetComponent<Image>();
            if (imgNoteIcon == null && noteBox != null) imgNoteIcon = noteBox.transform.Find("Img_Icon")?.GetComponent<Image>();
            if (txtNote == null && noteBox != null) txtNote = noteBox.transform.Find("Txt_Note")?.GetComponent<TextMeshProUGUI>();
        }
    }
}
