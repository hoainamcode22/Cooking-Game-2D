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

        private bool _popupInputLockHeld;

        /// <summary>Slot data THẬT của toa đang chọn — nguồn sự thật duy nhất là TrainManager.</summary>
        private global::TrainWagonSlotData GetSlot()
        {
            var mgr = TrainManager.Instance;
            if (mgr == null || mgr.SlotData == null) return null;
            if (currentWagonIndex < 0 || currentWagonIndex >= mgr.SlotData.Length) return null;
            return mgr.SlotData[currentWagonIndex];
        }

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
            if (TrainManager.Instance != null)
                TrainManager.Instance.OnStateChanged += HandleTrainStateChanged;
        }

        private void OnDisable()
        {
            if (TrainManager.Instance != null)
                TrainManager.Instance.OnStateChanged -= HandleTrainStateChanged;
            ReleasePopupInputBlock();
        }

        /// <summary>Tàu rời state nạp hàng (vd: đã nạp đủ, khởi hành) → popup tự đóng.</summary>
        private void HandleTrainStateChanged(global::TrainState s)
        {
            if (s != global::TrainState.WaitingForLoad && gameObject.activeSelf)
                ClosePopup();
        }

        public void OpenForWagon(int wagonIndex)
        {
            currentWagonIndex = wagonIndex;

            // Bật ancestor đang tắt, nhưng DỪNG ở Canvas gần nhất — không bật nhầm cây UI khác
            Transform tr = transform.parent;
            while (tr != null)
            {
                if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
                if (tr.GetComponent<Canvas>() != null) break;
                tr = tr.parent;
            }

            gameObject.SetActive(true);
            AcquirePopupInputBlock();
            ApplyThemeSprites();
            RefreshUI();
        }

        public void ClosePopup()
        {
            ReleasePopupInputBlock();
            gameObject.SetActive(false);
            if (TrainStationMasterPopupUI.Instance != null && TrainStationMasterPopupUI.Instance.gameObject.activeSelf)
            {
                TrainStationMasterPopupUI.Instance.RefreshUI();
            }
        }

        private void AcquirePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, true);
            if (!_popupInputLockHeld)
            {
                FarmInputLock.RegisterPopupOpen();
                _popupInputLockHeld = true;
            }
        }

        private void ReleasePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, false);
            if (_popupInputLockHeld)
            {
                FarmInputLock.RegisterPopupClose();
                _popupInputLockHeld = false;
            }
        }

        public void ApplyThemeSprites()
        {
            AutoBindComponents();

            if (imgFrameWood != null)
            {
                TrainSpriteLoader.Assign(imgFrameWood, $"{ShopSvgDir}/shop_panel.png", $"{SpritesDir}/popup_frame_wood.png");
                imgFrameWood.type = Image.Type.Sliced;
                imgFrameWood.color = Color.white;
            }

            if (imgPanelPaper != null)
            {
                TrainSpriteLoader.Assign(imgPanelPaper, $"{ShopSvgDir}/shop_card_inner.png", $"{SpritesDir}/popup_panel_paper.png");
                imgPanelPaper.type = Image.Type.Sliced;
                imgPanelPaper.color = Color.white;
            }

            if (imgRibbonBanner != null)
            {
                TrainSpriteLoader.Assign(imgRibbonBanner, $"{ShopSvgDir}/shop_banner_ribbon.png", $"{SpritesDir}/ribbon_banner_gold.png");
                imgRibbonBanner.type = Image.Type.Sliced;
                imgRibbonBanner.color = Color.white;
            }

            if (btnClose != null)
            {
                var img = btnClose.GetComponent<Image>();
                if (img != null)
                {
                    TrainSpriteLoader.Assign(img, "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png", "Assets/Assetsgame/btnX.png");
                    img.preserveAspect = true;
                }
            }

            if (imgIconDisc != null)
            {
                TrainSpriteLoader.Assign(imgIconDisc, $"{SpritesDir}/icon_disc_large.png");
                imgIconDisc.color = Color.white;
                imgIconDisc.preserveAspect = true;
            }

            if (imgProgressTrack != null)
            {
                TrainSpriteLoader.Assign(imgProgressTrack, $"{SpritesDir}/progress_track_bar.png");
                imgProgressTrack.type = Image.Type.Sliced;
                imgProgressTrack.color = Color.white;
            }

            if (imgProgressFill != null)
            {
                TrainSpriteLoader.Assign(imgProgressFill, $"{SpritesDir}/progress_fill_green.png");
                imgProgressFill.type = Image.Type.Filled;
                imgProgressFill.fillMethod = Image.FillMethod.Horizontal;
                imgProgressFill.color = Color.white;
            }

            if (imgThemHang != null)
            {
                TrainSpriteLoader.Assign(imgThemHang, $"{SpritesDir}/btn_green_3d.png");
                imgThemHang.type = Image.Type.Sliced;
                imgThemHang.color = Color.white;
            }

            if (imgNapTatCa != null)
            {
                TrainSpriteLoader.Assign(imgNapTatCa, $"{SpritesDir}/btn_yellow_3d.png");
                imgNapTatCa.type = Image.Type.Sliced;
                imgNapTatCa.color = Color.white;
            }

            if (imgDaDuHang != null)
            {
                TrainSpriteLoader.Assign(imgDaDuHang, $"{SpritesDir}/btn_disabled_3d.png");
                imgDaDuHang.type = Image.Type.Sliced;
                imgDaDuHang.color = Color.white;
            }

            if (imgNoteBox != null)
            {
                TrainSpriteLoader.Assign(imgNoteBox, $"{ShopSvgDir}/shop_card_outer.png", $"{SpritesDir}/bubble_cargo_req.png");
                imgNoteBox.type = Image.Type.Sliced;
                imgNoteBox.color = Color.white;
            }
        }

        public void RefreshUI()
        {
            ApplyThemeSprites();

            var mgr  = TrainManager.Instance;
            var slot = GetSlot();
            if (slot == null || slot.mode != global::TrainWagonSlotMode.CargoRequest)
            {
                // Không có dữ liệu toa (toa trống / sai state) → đóng cho an toàn
                if (gameObject.activeSelf) ClosePopup();
                return;
            }

            int slotCount = (mgr != null && mgr.SlotData != null) ? mgr.SlotData.Length : 4;

            // ĐỌC THẬT TỪ KHO HÀNG (FarmInventoryManager)
            stockCount = (FarmInventoryManager.Instance != null)
                ? FarmInventoryManager.Instance.GetAmount(slot.itemId)
                : 0;

            bool isDone = slot.IsCargoComplete;

            if (txtWagonTag != null)
            {
                txtWagonTag.text = isDone
                    ? $"Toa số {currentWagonIndex + 1} / {slotCount} — đã đủ hàng"
                    : $"Toa số {currentWagonIndex + 1} / {slotCount}";
            }

            if (txtItemName != null) txtItemName.text = slot.displayName;

            if (txtStock != null)
            {
                txtStock.text = $"Trong kho: x{stockCount}";
                txtStock.color = (stockCount > 0) ? new Color(0.54f, 0.39f, 0.22f) : new Color(0.85f, 0.25f, 0.20f);
            }

            // Progress bar
            float fillPct = slot.requiredAmount > 0
                ? Mathf.Clamp01((float)slot.currentAmount / slot.requiredAmount) : 0f;
            if (imgProgressFill != null) imgProgressFill.fillAmount = fillPct;

            if (txtProgressAmount != null)
                txtProgressAmount.text = $"{slot.currentAmount} / {slot.requiredAmount}";

            // Icon — sprite thật từ TrainCargoData asset (chạy được cả trong build)
            if (imgIcon != null)
            {
                if (slot.icon != null) imgIcon.sprite = slot.icon;
                imgIcon.enabled = imgIcon.sprite != null;
                imgIcon.color = Color.white;
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
                    if (imgNoteIcon != null && slot.icon != null)
                        imgNoteIcon.sprite = slot.icon;
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

                // Thiếu hàng → báo rõ "bạn chưa đủ hàng" ngay trong popup
                if (noteBox != null)
                {
                    bool showLack = stockCount <= 0;
                    noteBox.SetActive(showLack);
                    if (showLack && txtNote != null)
                        txtNote.text = $"Bạn chưa đủ hàng — trồng/sản xuất thêm {slot.displayName} rồi quay lại nhé!";
                    if (showLack && imgNoteIcon != null && slot.icon != null)
                        imgNoteIcon.sprite = slot.icon;
                }
            }
        }

        private void OnAddSingleCargo()
        {
            // TrainManager tự trừ kho, báo mission, refresh toa world, save, và tự khởi hành khi đủ
            TrainManager.Instance?.TryAddOneItemToSlot(currentWagonIndex);
            if (gameObject.activeSelf) RefreshUI();
            if (TrainStationMasterPopupUI.Instance != null && TrainStationMasterPopupUI.Instance.gameObject.activeSelf)
                TrainStationMasterPopupUI.Instance.RefreshUI();
        }

        private void OnAddAllCargo()
        {
            TrainManager.Instance?.TryLoadAllToSlot(currentWagonIndex);
            if (gameObject.activeSelf) RefreshUI();
            if (TrainStationMasterPopupUI.Instance != null && TrainStationMasterPopupUI.Instance.gameObject.activeSelf)
                TrainStationMasterPopupUI.Instance.RefreshUI();
        }

        private int CountIncompleteWagons()
        {
            var mgr = TrainManager.Instance;
            if (mgr == null || mgr.SlotData == null) return 0;

            int count = 0;
            foreach (var s in mgr.SlotData)
                if (s != null && s.mode == global::TrainWagonSlotMode.CargoRequest && !s.IsCargoComplete)
                    count++;
            return count;
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
