using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.UI
{
    /// <summary>
    /// Điều khiển toàn bộ HUD chính (Township HUD) theo thiết kế mới:
    /// - Cụm Top-Left: Khung Avatar bo góc độc lập + Thanh EXP Bar (Nền tối + Fill xanh + Ngôi sao cấp độ).
    /// - Cụm Top-Right: 2 Capsule Vàng & Kim Cương + Nút Cài Đặt.
    /// - Cụm Bottom-Left: 4 Tab Điều Hướng (CỬA HÀNG, KHO, NHIỆM VỤ, BẢN ĐỒ).
    /// </summary>
    public class TownshipHUDController : MonoBehaviour
    {
        public static TownshipHUDController Instance { get; private set; }

        [Header("── Top-Left: Avatar ──")]
        public Button btnAvatar;
        public Image imgAvatar;

        [Header("── Top-Left: EXP & Level ──")]
        public Image imgExpFill;
        public TMP_Text txtExp;
        public TMP_Text txtLevel;

        [Header("── Top-Right: Currencies ──")]
        public TMP_Text txtGold;
        public TMP_Text txtDiamond;
        public Button btnAddGold;
        public Button btnAddDiamond;

        [Header("── Top-Right: Settings ──")]
        public Button btnSettings;

        [Header("── Left Side: Mission Button & Quick Widget (Chấm đỏ) ──")]
        public Button btnMission;
        public GameObject goMissionBadge;
        public GameObject goMissionWidget;
        public Image imgMissionItem;
        public TMP_Text txtMissionTitle;
        public TMP_Text txtMissionDesc;
        public Image imgMissionProgressFill;
        public TMP_Text txtMissionProgress;
        public Button btnMissionGo;

        [Header("── [VÒNG 8] Chống chồng lấn HUD ──")]
        [Tooltip("TRUE: ẩn 2 nút + trên khung Vàng và khung Kim Cương. Bỏ tick là hiện lại ngay — object KHÔNG bị xoá.")]
        [SerializeField] private bool anNutCong = true;

        [Tooltip("TRUE: gỡ Ngôi Sao Cấp ra khỏi EXP_Bar_Container (giữ nguyên chỗ đứng trên màn hình) để FX phóng to thanh EXP không kéo sao đè lên khung avatar.")]
        [SerializeField] private bool tachSaoCapKhoiThanhExp = true;

        [Tooltip("TRUE: chặn trần phóng to của thanh EXP / khung Vàng / khung Kim Cương để FX nảy không đẩy chúng đè vào nhau.")]
        [SerializeField] private bool chanTranPhongTo = true;

        [Tooltip("Phần khe hở mỗi bên được phép dùng cho cú nảy. 0.45 = 45%, hai bên cùng nảy vẫn còn hở 10%.")]
        [SerializeField, Range(0.1f, 0.9f)] private float phanKheHoDuocDung = 0.45f;

        [Tooltip("[VÒNG 11] Kê dư BẮT BUỘC còn lại giữa 2 mép khi cả hai bên đã nảy hết trần (px hệ 1920×1080). Field MỚI nên giá trị scene không đè được. Đây là con số cho phép kéo khung Vàng sát khung Kim Cương mà vẫn không đè: luật % cũ để kê dư teo theo khe hở, số này giữ kê dư đứng yên.")]
        [SerializeField] private float keDuAnToanToiThieu = 7.5f;

        [Tooltip("Cỡ chữ nhỏ nhất khi số dài ra (số tự co để không tràn khung). Đặt 0 = tắt tự co.")]
        [SerializeField] private float coChuNhoNhatKhiSoDai = 20f;

        [Header("── [VÒNG 10] Neo 2 cụm HUD theo rìa màn hình ──")]
        // Ba field dưới đây là field MỚI (tên chưa từng có trong scene) nên giá trị đã lưu
        // trong SCN_Farm KHÔNG đè được mặc định ở đây.
        [Tooltip("TRUE: lúc vào màn và mỗi khi xoay máy / đổi cỡ cửa sổ, đo mép ngoài THẬT của 2 cụm HUD rồi nhích cụm cha sao cho cách rìa màn hình đúng số lề bên dưới. Bỏ tick là code KHÔNG đụng anchoredPosition của cụm nào.")]
        [SerializeField] private bool neoCumTheoLeManHinh = true;

        [Tooltip("Lề tối thiểu (px hệ 1920×1080) từ mép ngoài cụm HUD tới rìa màn hình. Số đo THẬT của scene hiện nay là 12 px bên trái / 14 px bên phải, nên để 12 là gần như không đổi gì. ĐÂY LÀ NÚM 'RA RÌA THÊM': giảm số này thì cả 2 cụm nhích ra rìa, không phải sửa scene.")]
        [SerializeField] private float leRiaToiThieu = 12f;

        [Tooltip("Lề thêm tính TỪ MÉP VÙNG AN TOÀN mà hệ điều hành báo (tai thỏ / lỗ camera / bo góc). Máy không khuyết thì Screen.safeArea = cả màn hình nên số này không có tác dụng gì.")]
        [SerializeField] private float leThemTrongVungAnToan = 8f;

        [Tooltip("Chặn cứng: một lần neo không được nhích cụm quá số px này. Phép đo lỗi cũng không thể ném cụm HUD ra ngoài màn hình.")]
        [SerializeField] private float nhichToiDaMoiLan = 80f;

        [Header("── Bottom-Left: Navigation Tabs ──")]
        public Button btnTabShop;
        public Button btnTabWarehouse;
        public Button btnTabMarket;
        public Button btnTabCooking;
        public Button btnTabFishing;
        [HideInInspector] public Button btnTabMission;
        [HideInInspector] public Button btnTabMap;

        // [VONG 8] KHONG dung new CultureInfo("vi-VN"): build IL2CPP bat Invariant Globalization
        // se nem CultureNotFoundException NGAY trong static constructor => chet ca HUD.
        // Dung NumberFormatInfo tu khai bao, giong BoatDockSlot.cs / DockPurchasePopupUI.cs.
        // Ky tu phan nhom giu Y NGUYEN nhu dang thay tren may Sep: tien dung ".", EXP dung " ".
        private static readonly NumberFormatInfo DinhDangTien = new NumberFormatInfo
        {
            NumberGroupSeparator   = ".",
            NumberDecimalSeparator = ",",
            NumberGroupSizes       = new[] { 3 },
        };

        private static readonly NumberFormatInfo DinhDangExp = new NumberFormatInfo
        {
            NumberGroupSeparator   = " ",
            NumberDecimalSeparator = ",",
            NumberGroupSizes       = new[] { 3 },
        };

        // ── [VÒNG 10] Trạng thái của phần neo rìa ──────────────────────────────
        private Canvas        _canvasGoc;
        private RectTransform _cumTrai;
        private RectTransform _cumPhai;
        private float _xGocCumTrai;
        private float _xGocCumPhai;
        private bool  _daLuuXGocTrai;
        private bool  _daLuuXGocPhai;
        private int   _manHinhWLucNeo = -1;
        private int   _manHinhHLucNeo = -1;
        private Rect  _vungAnToanLucNeo;

        // Đệm 4 góc dùng chung cho phép đo neo — chỉ chạy lúc vào màn / đổi cỡ màn hình.
        private static readonly Vector3[] GocNeoBuf = new Vector3[4];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (goMissionWidget != null)
                goMissionWidget.SetActive(false);

            SetupButtonListeners();
            ApDungChongDeHUD();
            SubscribeEvents();
            RefreshAllUI();
            UpdateMissionBadge();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// [VÒNG 10] Chỉ làm một việc: canh lại 2 cụm khi màn hình đổi (xoay máy, đổi cỡ cửa
        /// sổ, chia đôi màn hình, hệ điều hành báo lại vùng an toàn). Không đổi gì thì thoát
        /// ngay ở dòng đầu — không tốn gì trong lúc chơi.
        /// </summary>
        private void Update()
        {
            if (!neoCumTheoLeManHinh) return;
            if (Screen.width == _manHinhWLucNeo && Screen.height == _manHinhHLucNeo &&
                CungMotVungAnToan(Screen.safeArea, _vungAnToanLucNeo)) return;

            NeoHaiCumTheoRia();
        }

        // ── Gán sự kiện Click ──────────────────────────────────────────────────

        private void SetupButtonListeners()
        {
            if (btnAvatar != null)
            {
                btnAvatar.onClick.RemoveAllListeners();
                btnAvatar.onClick.AddListener(OnAvatarClicked);
            }

            if (btnMission != null)
            {
                btnMission.onClick.RemoveAllListeners();
                btnMission.onClick.AddListener(OnMissionButtonClicked);
            }

            if (btnMissionGo != null)
            {
                btnMissionGo.onClick.RemoveAllListeners();
                btnMissionGo.onClick.AddListener(OnMissionGoClicked);
            }

            if (btnTabShop != null)
            {
                btnTabShop.onClick.RemoveAllListeners();
                btnTabShop.onClick.AddListener(OnShopClicked);
            }

            if (btnTabWarehouse != null)
            {
                btnTabWarehouse.onClick.RemoveAllListeners();
                btnTabWarehouse.onClick.AddListener(OnWarehouseClicked);
            }

            if (btnTabMarket != null)
            {
                btnTabMarket.onClick.RemoveAllListeners();
                btnTabMarket.onClick.AddListener(OnMarketClicked);
            }

            if (btnTabCooking != null)
            {
                btnTabCooking.onClick.RemoveAllListeners();
                btnTabCooking.onClick.AddListener(OnCookingClicked);
            }

            if (btnTabFishing == null)
            {
                Transform nav = btnTabCooking != null ? btnTabCooking.transform.parent : transform.Find("BottomLeft_Nav_Group");
                if (nav != null)
                {
                    Transform tr = nav.Find("Tab_Fishing");
                    if (tr != null) btnTabFishing = tr.GetComponent<Button>();
                }
            }

            if (btnTabFishing != null)
            {
                btnTabFishing.onClick.RemoveAllListeners();
                btnTabFishing.onClick.AddListener(OnFishingClicked);
            }

            if (btnTabMission != null)
            {
                btnTabMission.onClick.RemoveAllListeners();
                btnTabMission.onClick.AddListener(OnMissionButtonClicked);
            }

            if (btnTabMap != null)
            {
                btnTabMap.onClick.RemoveAllListeners();
                btnTabMap.onClick.AddListener(OnCookingClicked);
            }

            if (btnAddGold != null)
            {
                btnAddGold.onClick.RemoveAllListeners();
                btnAddGold.onClick.AddListener(OnShopClicked);
            }

            if (btnAddDiamond != null)
            {
                btnAddDiamond.onClick.RemoveAllListeners();
                btnAddDiamond.onClick.AddListener(OnShopClicked);
            }

            if (btnSettings == null)
            {
                var allButtons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < allButtons.Length; i++)
                {
                    if (allButtons[i].name.Contains("Setting") || allButtons[i].name.Contains("CaiDat"))
                    {
                        btnSettings = allButtons[i];
                        break;
                    }
                }
            }

            if (btnSettings != null)
            {
                btnSettings.gameObject.layer = 5; // Layer UI
                btnSettings.onClick.RemoveAllListeners();
                btnSettings.onClick.AddListener(OnSettingsClicked);
            }
        }

        // ── Lắng nghe sự kiện dữ liệu ──────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (FarmEconomyManager.Instance != null)
            {
                FarmEconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            }

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnExpChanged += OnExpChanged;
                PlayerProgressManager.Instance.OnLevelChanged += OnLevelChanged;
            }

            if (FarmLevelManager.Instance != null)
            {
                FarmLevelManager.Instance.OnLevelChanged += OnLevelChanged;
            }

            AvatarProfilePopupUI.OnAvatarSelected += HandleAvatarChanged;
            LocalizationManager.OnChanged += HandleLanguageChanged;
        }

        private void UnsubscribeEvents()
        {
            if (FarmEconomyManager.Instance != null)
            {
                FarmEconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
            }

            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.OnExpChanged -= OnExpChanged;
                PlayerProgressManager.Instance.OnLevelChanged -= OnLevelChanged;
            }

            if (FarmLevelManager.Instance != null)
            {
                FarmLevelManager.Instance.OnLevelChanged -= OnLevelChanged;
            }

            AvatarProfilePopupUI.OnAvatarSelected -= HandleAvatarChanged;
            LocalizationManager.OnChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(string lang)
        {
            RefreshLocalizedTexts();
        }

        public void RefreshLocalizedTexts()
        {
            SetTabLabel(btnTabShop, Loc.T("CỬA HÀNG"));
            SetTabLabel(btnTabWarehouse, Loc.T("KHO"));
            SetTabLabel(btnTabMarket, Loc.T("BẢNG TIN CHỢ"));
            SetTabLabel(btnTabCooking, Loc.T("NẤU ĂN"));
            SetTabLabel(btnTabFishing, Loc.T("CÂU CÁ"));
        }

        private void SetTabLabel(Button btn, string text)
        {
            if (btn == null) return;
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = text;
            }
        }

        private void EnsureCookingIcon()
        {
            if (btnTabCooking != null)
            {
                Transform iconTr = btnTabCooking.transform.Find("Icon");
                if (iconTr != null)
                {
                    Image img = iconTr.GetComponent<Image>();
                    if (img != null && (img.sprite == null || img.sprite.name.Contains("ngoinhacoooking") || img.sprite.name.Contains("Missing")))
                    {
                        Sprite spr = Resources.Load<Sprite>("HUD/hud_icon_cooking");
                        if (spr != null)
                        {
                            img.sprite = spr;
                            img.color = Color.white;
                            img.enabled = true;
                        }
                    }
                }
            }

            if (btnTabFishing != null)
            {
                Transform iconTr = btnTabFishing.transform.Find("Icon");
                if (iconTr != null)
                {
                    Image img = iconTr.GetComponent<Image>();
                    if (img != null && (img.sprite == null || img.sprite.name.Contains("ngoinhacoooking") || img.sprite.name.Contains("Missing")))
                    {
#if UNITY_EDITOR
                        Sprite spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Fishing/Art/UI/icon_tab_fishing_hub.png");
                        if (spr != null)
                        {
                            img.sprite = spr;
                            img.color = Color.white;
                            img.enabled = true;
                        }
#endif
                    }
                }
            }
        }

        // ── Xử lý Cập nhật UI ──────────────────────────────────────────────────

        public void RefreshAllUI()
        {
            EnsureCookingIcon();
            RefreshLocalizedTexts();
            // 1. Tiền tệ
            if (FarmEconomyManager.Instance != null)
            {
                UpdateCurrency(FarmEconomyManager.Instance.Gold, FarmEconomyManager.Instance.Gems);
            }

            // 2. Cấp độ & EXP
            int currentLevel = 1;
            int currentExp = 0;
            int requiredExp = 100;

            if (PlayerProgressManager.Instance != null)
            {
                currentLevel = PlayerProgressManager.Instance.Level;
                currentExp = PlayerProgressManager.Instance.CurrentExp;
                requiredExp = PlayerProgressManager.Instance.RequiredExpForLevel(currentLevel);
            }
            else if (FarmLevelManager.Instance != null)
            {
                currentLevel = FarmLevelManager.Instance.CurrentLevel;
            }

            UpdateLevel(currentLevel);
            UpdateExp(currentExp, requiredExp);

            // 3. Avatar người chơi
            RefreshAvatar();

            // 4. Chấm than đỏ thông báo nhiệm vụ
            UpdateMissionBadge();
        }

        public void UpdateMissionBadge()
        {
            if (goMissionBadge == null) return;
            bool hasClaimable = UnifiedTaskPopupUI.HasAnyClaimableTask();
            goMissionBadge.SetActive(hasClaimable);
        }

        public void RefreshAvatar(int index = -1)
        {
            if (imgAvatar == null) return;
            if (index < 0) index = PlayerPrefs.GetInt("PLAYER_PROFILE_AVATAR_INDEX", 0);
            Sprite spr = Resources.Load<Sprite>($"Avatars/avatar_npc_{index}");
#if UNITY_EDITOR
            if (spr == null)
                spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Avatars/avatar_npc_{index}.png");
#endif
            if (spr != null) imgAvatar.sprite = spr;
        }

        private void HandleAvatarChanged(int index)
        {
            RefreshAvatar(index);
        }

        private void OnCurrencyChanged(int gold, int gems)
        {
            UpdateCurrency(gold, gems);
        }

        private void UpdateCurrency(int gold, int gems)
        {
            if (txtGold != null)
                txtGold.text = gold.ToString("N0", DinhDangTien);

            if (txtDiamond != null)
                txtDiamond.text = gems.ToString("N0", DinhDangTien);
        }

        private void OnExpChanged(int cur, int req)
        {
            UpdateExp(cur, req);
        }

        private void UpdateExp(int cur, int req)
        {
            if (req <= 0) req = 1;
            float ratio = Mathf.Clamp01((float)cur / req);

            if (imgExpFill != null)
                imgExpFill.fillAmount = ratio;

            if (txtExp != null)
                txtExp.text = cur.ToString("N0", DinhDangExp) + " / " + req.ToString("N0", DinhDangExp);
        }

        private void OnLevelChanged(int level)
        {
            UpdateLevel(level);
        }

        private void UpdateLevel(int level)
        {
            if (txtLevel != null)
                txtLevel.text = level.ToString();
        }

        // ── [VÒNG 8] CHỐNG CHỒNG LẤN HUD ──────────────────────────────────────
        //
        // BA LỖI SẾP CHỤP ĐƯỢC, và nguyên nhân ĐO ĐƯỢC trên SCN_Farm (hệ quy chiếu 1920×1080,
        // cụm HUD trái/phải đều có localScale 1.2):
        //
        //  (1) NGÔI SAO CẤP ĐÈ LÊN KHUNG AVATAR.
        //      Level_Star_Badge là CON của EXP_Bar_Container, ngồi ở x = −185 nên thò hẳn
        //      35 px ra ngoài mép trái thanh EXP. Lúc đứng yên mép trái sao (x 181.3) chỉ
        //      cách mép phải khung avatar (x 180.0) đúng 1.3 px.
        //      Khi FX gọi JuicyPulseFX phóng EXP_Bar_Container lên 1.22 lần, độ lệch −185
        //      cũng bị nhân 1.22 ⇒ sao trượt thêm 48.8 px sang trái, mép trái sao về x 120.6
        //      ⇒ ĐÈ LÊN AVATAR 59.4 px. Nhận EXP càng nhiều thì cú phóng nối nhau càng dày,
        //      mắt thấy sao "nằm luôn" trên avatar.
        //      → SỬA GỐC: đưa ngôi sao RA KHỎI object bị phóng to, giữ nguyên chỗ đứng.
        //        Sao không còn là con của thanh EXP thì cú phóng không thể kéo nó đi đâu nữa.
        //
        //  (2) KHUNG VÀNG / KIM CƯƠNG ĐÈ NHAU.
        //      Mép phải khung Vàng ở x 1468; mép trái icon Kim Cương ở x 1484.6 ⇒ chỉ hở
        //      16.6 px (icon kim cương vốn thò 16.4 px ra ngoài mép trái capsule của nó —
        //      đây là chủ ý thiết kế). Mọi cú phóng to đều ăn hết khe hở đó:
        //        · Diamond_Container phóng 1.20  ⇒ icon kim cương lùi về x 1453 ⇒ đè vào
        //          khung Vàng 14.9 px.
        //        · Cả hai khung cùng phóng      ⇒ hai capsule đè nhau 27.6 px.
        //      → SỬA GỐC: chặn TRẦN phóng to theo khe hở THẬT (xem HudChongDeKhiPhongTo).
        //
        //  (3) SỐ DÀI RA. Đã kiểm: KHÔNG có ContentSizeFitter / LayoutGroup / autoSizing nào
        //      trên cụm HUD, nên khung KHÔNG nở theo số. Nhưng 4 ô chữ đều overflowMode =
        //      Overflow + không tự co ⇒ số quá dài thì CHỮ tràn ra ngoài khung (khoảng 14 chữ
        //      số mới với tới ô bên cạnh). Bật tự co trong khoảng HẸP là hết đường tràn.
        //      KHÔNG tự chế định dạng "12,5K": cả dự án chưa có hàm rút gọn K/M nào, nên
        //      giữ đúng định dạng nhóm 3 chữ số đang dùng (BoatDockSlot / DockPurchasePopupUI).

        private void ApDungChongDeHUD()
        {
            AnHaiNutCong();
            TachSaoCapRaKhoiThanhExp();
            NeoHaiCumTheoRia();      // [VÒNG 10] phải chạy SAU khi ẩn 2 nút + (mép ngoài cụm đổi theo) và TRƯỚC khi trần phóng to đo khe hở
            KhoaChuKhongTranKhung();
            GanTranPhongTo();
        }

        /// <summary>
        /// Sếp yêu cầu xoá 2 dấu +. Dùng SetActive(false) chứ KHÔNG Destroy: bỏ tick
        /// <c>anNutCong</c> là hiện lại nguyên vẹn, scene không mất object nào.
        /// Hai nút này chỉ gắn đúng một việc là OnShopClicked() (mở Shop) — lối mở Shop vẫn
        /// còn nguyên ở tab CỬA HÀNG góc trái-dưới, nên không mất tính năng nào.
        /// </summary>
        private void AnHaiNutCong()
        {
            bool hien = !anNutCong;
            if (btnAddGold != null) btnAddGold.gameObject.SetActive(hien);
            if (btnAddDiamond != null) btnAddDiamond.gameObject.SetActive(hien);

            if (anNutCong)
            {
                Debug.Log("[TownshipHUD/VÒNG 8] Đã ẩn 2 nút + (Btn_Add_Gold, Btn_Add_Diamond) — bỏ tick anNutCong trên TownshipHUDController để hiện lại.");
            }
        }

        /// <summary>
        /// Gỡ Level_Star_Badge khỏi EXP_Bar_Container, treo lên cụm TopLeft, GIỮ NGUYÊN
        /// vị trí và cỡ trên màn hình (SetParent worldPositionStays = true) và giữ nguyên thứ
        /// tự vẽ trên cùng (SetAsLastSibling — trước đây sao cũng là con vẽ cuối).
        /// Chỉ làm khi sao ĐANG còn nằm trong thanh EXP: Sếp sửa hẳn trong scene thì hàm này
        /// tự thành vô nghĩa, không đụng gì. Không huỷ con, không dựng lại UI.
        /// </summary>
        private void TachSaoCapRaKhoiThanhExp()
        {
            if (!tachSaoCapKhoiThanhExp) return;
            if (txtLevel == null || imgExpFill == null) return;

            RectTransform thanhExp = imgExpFill.rectTransform.parent as RectTransform;   // EXP_Bar_Container
            RectTransform sao      = txtLevel.rectTransform.parent as RectTransform;     // Level_Star_Badge
            if (thanhExp == null || sao == null) return;
            if (sao.parent != thanhExp) return;                                          // đã ở ngoài rồi

            RectTransform cumTrai = thanhExp.parent as RectTransform;                    // TopLeft_Township_HUD
            if (cumTrai == null) return;

            sao.SetParent(cumTrai, true);
            sao.SetAsLastSibling();

            {
                Debug.Log("[TownshipHUD/VÒNG 8] Đã gỡ Ngôi Sao Cấp ra khỏi EXP_Bar_Container (giữ nguyên chỗ đứng) — cú phóng to thanh EXP không còn kéo sao đè lên avatar.");
            }
        }

        /// <summary>
        /// Số dài ra thì CHỮ tự co trong khoảng HẸP, khung giữ nguyên bề rộng — theo đúng cách
        /// dự án đang chống nhãn tràn ở UnlockSlotUI.ApDinhDangCaption(). Cỡ chữ lớn nhất lấy
        /// đúng cỡ Sếp đang đặt trong scene, nên số ngắn KHÔNG đổi hình dáng gì.
        /// </summary>
        private void KhoaChuKhongTranKhung()
        {
            if (coChuNhoNhatKhiSoDai <= 0f) return;
            KhoaMotO(txtGold);
            KhoaMotO(txtDiamond);
            KhoaMotO(txtExp);
            KhoaMotO(txtLevel);
        }

        private void KhoaMotO(TMP_Text txt)
        {
            if (txt == null) return;
            if (txt.enableAutoSizing) return;                 // ai đó đã bật ⇒ tôn trọng
            float coDangDat = txt.fontSize;
            if (coDangDat <= coChuNhoNhatKhiSoDai) return;    // vốn đã nhỏ hơn sàn ⇒ đừng đụng

            txt.enableAutoSizing = true;
            txt.fontSizeMin      = coChuNhoNhatKhiSoDai;
            txt.fontSizeMax      = coDangDat;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode     = TextOverflowModes.Overflow;
        }

        /// <summary>
        /// Cài trần phóng to cho 4 phần tử mà FX hay đụng tới. Trần được ĐO từ khe hở THẬT
        /// trong scene, nên Sếp kéo cho chúng xa nhau ra là cú nảy tự đầy đặn lại.
        ///
        /// Vì sao đủ 4 chỗ: JuicyPulseFX được gọi từ 5 nơi với 3 đích khác nhau —
        ///   · RewardFlyFX.FindExpTarget() trả về NGÔI SAO CẤP (1.25 lần),
        ///   · HarvestFeedbackSpawner trả về EXP_Bar_Container (1.22 lần),
        ///   · CoinFlyFX / GemFlyFX / UnifiedTaskPopupUI trả về Gold_Container /
        ///     Diamond_Container (1.20–1.22 lần).
        /// Chặn thiếu một đích là lỗi quay lại theo đường khác.
        /// </summary>
        private void GanTranPhongTo()
        {
            if (!chanTranPhongTo) return;

            RectTransform thanhExp    = imgExpFill != null ? imgExpFill.rectTransform.parent as RectTransform : null;
            RectTransform saoCap      = txtLevel   != null ? txtLevel.rectTransform.parent as RectTransform : null;
            RectTransform khungAvatar = btnAvatar  != null ? btnAvatar.transform as RectTransform : null;
            RectTransform khungVang   = txtGold    != null ? txtGold.rectTransform.parent as RectTransform : null;
            RectTransform khungGem    = txtDiamond != null ? txtDiamond.rectTransform.parent as RectTransform : null;
            RectTransform nutCaiDat   = btnSettings != null ? btnSettings.transform as RectTransform : null;

            // [VÒNG 10] Cột "đứng yên": TRUE nghĩa là tường bên đó KHÔNG BAO GIỜ bị FX phóng
            // to, nên một mình phần tử này được ăn 85% khe hở thay vì 45%. Đã soát cả dự án:
            // 10 chỗ gọi JuicyPulseFX.Play đều nhắm vào EXP_Bar_Container / Level_Star_Badge /
            // Gold_Container / Diamond_Container / Icon_Gold / Icon_Diamond / panel kho —
            // KHÔNG chỗ nào nhắm Avatar_Button hay Btn_Settings.

            // Thanh EXP: trái là khung avatar (ĐỨNG YÊN). Phải là khung Vàng của CỤM BÊN KIA —
            // ở 16:9 hai cụm cách nhau hơn 300 px nên tường này không ăn gì; nó chỉ ăn khi màn
            // hình hẹp (4:3, cửa sổ chia đôi) làm hai cụm tới sát nhau.
            CaiTran(thanhExp, khungAvatar, khungVang, true, false, true);

            // Ngôi sao cấp: tường bên trái cũng là khung avatar (ĐỨNG YÊN). Giữ yêu cầu CÙNG
            // CHA: sao còn nằm trong thanh EXP thì cha nó là thanh EXP, lúc đó chính thanh EXP
            // đã có trần riêng nên không cần chặn thêm.
            CaiTran(saoCap, khungAvatar, null, true, false, false);

            // Khung Vàng: trái là thanh EXP của CỤM BÊN KIA (chỉ ăn khi màn hình hẹp), phải là
            // khung Kim Cương (kể cả icon kim cương thò ra ngoài capsule).
            CaiTran(khungVang, thanhExp, khungGem, false, false, true);

            // Khung Kim Cương: trái là khung Vàng, phải là bánh xe cài đặt (ĐỨNG YÊN).
            CaiTran(khungGem, khungVang, nutCaiDat, false, true, false);
        }

        private void CaiTran(RectTransform khung, RectTransform trai, RectTransform phai,
                             bool traiDungYen, bool phaiDungYen, bool chapNhanTuongKhacCha)
        {
            if (khung == null) return;

            // [VÒNG 10] HudChongDeKhiPhongTo nay đổi 4 góc THẬT (world) về hệ toạ độ của cha
            // phần tử được canh giữ, nên tường khác cha vẫn cùng một thước đo. Vẫn chừa cờ
            // để những cặp cũ giữ nguyên yêu cầu cùng cha, không đổi hành vi ngoài ý muốn.
            if (!chapNhanTuongKhacCha)
            {
                if (trai != null && trai.parent != khung.parent) trai = null;
                if (phai != null && phai.parent != khung.parent) phai = null;
            }
            if (trai == khung) trai = null;
            if (phai == khung) phai = null;
            if (trai == null && phai == null) return;

            var chan = khung.GetComponent<HudChongDeKhiPhongTo>();
            if (chan == null) chan = khung.gameObject.AddComponent<HudChongDeKhiPhongTo>();
            chan.Nap(khung, trai, phai, phanKheHoDuocDung, traiDungYen, phaiDungYen, keDuAnToanToiThieu);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  [VÒNG 10] NEO 2 CỤM HUD THEO RÌA MÀN HÌNH
        //
        //  SỐ ĐO THẬT trên SCN_Farm (hệ 1920×1080, cụm localScale 1.2, 2 nút + đã ẩn):
        //    · Cụm trái  — mép ngoài là khung avatar, x = 12.0  ⇒ cách rìa trái 12.0 px
        //    · Cụm phải  — mép ngoài là bánh xe cài đặt, x = 1906.0 ⇒ cách rìa phải 14.0 px
        //  Nghĩa là HAI CỤM ĐÃ SÁT RÌA RỒI. Muốn "ra rìa thêm" bằng cách dời cụm cha thì
        //  chỉ còn 12–14 px, mà đó lại đúng là chỗ tai thỏ / bo góc điện thoại ăn vào.
        //
        //  VÌ SAO PHẢI LÀM BẰNG CODE, KHÔNG PHẢI SỬA SCENE: một con số trong scene chỉ đúng
        //  cho MỘT máy. ProjectSettings đang bật androidRenderOutsideSafeArea = 1 (Unity vẽ
        //  TRÀN xuống dưới tai thỏ) và cả dự án KHÔNG có object nào gắn SafeAreaFitter, nên
        //  hiện tại trên iPhone / Android khuyết nằm ngang, khung avatar và bánh xe cài đặt
        //  bị tai thỏ che. Ở đây đo Screen.safeArea lúc chạy rồi tự canh: máy không khuyết
        //  (Editor, PC, phần lớn Android) thì giữ đúng leRiaToiThieu = 12 px như Sếp đang
        //  thấy; máy có khuyết thì tự lùi vào vừa đủ.
        // ══════════════════════════════════════════════════════════════════════

        private void NeoHaiCumTheoRia()
        {
            if (!neoCumTheoLeManHinh) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;   // nhịp đầu trên vài thiết bị

            if (_canvasGoc == null)
            {
                var c = GetComponentInParent<Canvas>();
                if (c != null) _canvasGoc = c.rootCanvas != null ? c.rootCanvas : c;
            }
            if (_canvasGoc == null) return;

            RectTransform khungCanvas = _canvasGoc.transform as RectTransform;
            if (khungCanvas == null) return;

            TimHaiCumCha();
            if (_cumTrai == null && _cumPhai == null) return;

            float heSo = _canvasGoc.scaleFactor;
            if (heSo <= 0.0001f) heSo = 1f;

            // safeArea trả về PIXEL màn hình thật; chia hệ số CanvasScaler để về px hệ 1920×1080.
            Rect vung = Screen.safeArea;
            float khuyetTrai = Mathf.Max(0f, vung.x) / heSo;
            float khuyetPhai = Mathf.Max(0f, Screen.width - (vung.x + vung.width)) / heSo;

            float leTrai = Mathf.Max(leRiaToiThieu, khuyetTrai + leThemTrongVungAnToan);
            float lePhai = Mathf.Max(leRiaToiThieu, khuyetPhai + leThemTrongVungAnToan);

            if (_daLuuXGocTrai) NeoMotCum(_cumTrai, khungCanvas, true,  leTrai, _xGocCumTrai);
            if (_daLuuXGocPhai) NeoMotCum(_cumPhai, khungCanvas, false, lePhai, _xGocCumPhai);

            _manHinhWLucNeo   = Screen.width;
            _manHinhHLucNeo   = Screen.height;
            _vungAnToanLucNeo = vung;
        }

        /// <summary>
        /// Hai cụm cha lấy từ chính các tham chiếu Sếp đã gán trong Inspector (cha của thanh
        /// EXP và cha của khung Kim Cương) — KHÔNG dò theo tên, nên builder tool có đổi tên
        /// object cũng không hỏng. x gốc chỉ lưu MỘT lần: mọi phép neo về sau đều tính lại từ
        /// con số Sếp/Lead đặt trong scene, nên chạy bao nhiêu lần cũng không dồn lệch.
        /// </summary>
        private void TimHaiCumCha()
        {
            if (_cumTrai == null && imgExpFill != null)
            {
                RectTransform thanhExp = imgExpFill.rectTransform.parent as RectTransform;
                if (thanhExp != null) _cumTrai = thanhExp.parent as RectTransform;
            }

            if (_cumPhai == null && txtDiamond != null)
            {
                RectTransform khungGem = txtDiamond.rectTransform.parent as RectTransform;
                if (khungGem != null) _cumPhai = khungGem.parent as RectTransform;
            }

            // Lưu x gốc RIÊNG từng cụm, đúng lúc cụm đó tìm được. Lưu chung một cờ là sai:
            // nhịp đầu chỉ tìm được một cụm thì cụm còn lại bị ghi x gốc = 0 rồi khoá luôn.
            if (!_daLuuXGocTrai && _cumTrai != null)
            {
                _xGocCumTrai = _cumTrai.anchoredPosition.x;
                _daLuuXGocTrai = true;
            }

            if (!_daLuuXGocPhai && _cumPhai != null)
            {
                _xGocCumPhai = _cumPhai.anchoredPosition.x;
                _daLuuXGocPhai = true;
            }
        }

        private void NeoMotCum(RectTransform cum, RectTransform khungCanvas, bool benTrai,
                               float leMuon, float xGoc)
        {
            if (cum == null || khungCanvas == null) return;

            Vector2 vt = cum.anchoredPosition;
            vt.x = xGoc;                       // luôn đo lại từ số gốc trong scene
            cum.anchoredPosition = vt;

            float minX, maxX;
            if (!DoMepNgangTrenCanvas(cum, khungCanvas, out minX, out maxX)) return;

            float leDangCo = benTrai ? (minX - khungCanvas.rect.xMin)
                                     : (khungCanvas.rect.xMax - maxX);
            float chenh = leMuon - leDangCo;   // > 0 = phải kéo VÀO, < 0 = được nhích RA RÌA
            if (Mathf.Abs(chenh) < 0.5f) return;                   // lệch dưới nửa pixel thì đừng đụng

            chenh = Mathf.Clamp(chenh, -nhichToiDaMoiLan, nhichToiDaMoiLan);
            vt.x = benTrai ? (xGoc + chenh) : (xGoc - chenh);
            cum.anchoredPosition = vt;
        }

        /// <summary>
        /// Mép ngang THẬT của cụm, đo trong hệ toạ độ Canvas gốc (px hệ 1920×1080).
        ///
        /// CHỈ tính những con CÓ VẼ RA HÌNH (Graphic): rect của chính cụm cha rộng 650×155 và
        /// pivot nằm ngoài màn hình, lấy nó vào là ra số vô nghĩa. Con đang tắt cũng bị bỏ —
        /// đúng ý: 2 nút + đã ẩn thì không được tính vào bề rộng cụm nữa. Icon vàng / kim cương
        /// thò 16.8 px ra ngoài capsule vẫn được tính, vì chúng có Image.
        /// </summary>
        private static bool DoMepNgangTrenCanvas(RectTransform cum, RectTransform khungCanvas,
                                                 out float minX, out float maxX)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            if (cum == null || khungCanvas == null) return false;

            Graphic[] ds = cum.GetComponentsInChildren<Graphic>(false);
            for (int i = 0; i < ds.Length; i++)
            {
                if (ds[i] == null) continue;
                ds[i].rectTransform.GetWorldCorners(GocNeoBuf);
                for (int g = 0; g < 4; g++)
                {
                    float x = khungCanvas.InverseTransformPoint(GocNeoBuf[g]).x;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
            }

            return maxX - minX > 1f;
        }

        /// <summary>So 2 vùng an toàn theo từng cạnh — 1 pixel là ngưỡng đủ mịn.</summary>
        private static bool CungMotVungAnToan(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 1f && Mathf.Abs(a.y - b.y) < 1f &&
                   Mathf.Abs(a.width - b.width) < 1f && Mathf.Abs(a.height - b.height) < 1f;
        }

        // ── Xử lý Click Nút ────────────────────────────────────────────────────

        private bool IsBlockedByTutorial(string buttonType)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.DangChayTutorial)
                return false;

            if (buttonType == "shop")
            {
                return !TutorialManager.Instance.CurrentStepAllowsShopClick();
            }

            Debug.Log($"[TownshipHUD] Nút '{buttonType}' bị khóa trong lúc đang chạy Tutorial.");
            return true;
        }

        private void OnAvatarClicked()
        {
            if (IsBlockedByTutorial("avatar")) return;

            var avatarPopup = Object.FindFirstObjectByType<AvatarProfilePopupUI>(FindObjectsInactive.Include);
            if (avatarPopup != null)
            {
                avatarPopup.OpenPopup();
            }
            else
            {
                Debug.Log("[TownshipHUD] Mở Popup Hồ Sơ / Avatar!");
            }
        }

        private void OnShopClicked()
        {
            if (IsBlockedByTutorial("shop")) return;

            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OpenShop();
            }
            else
            {
                var shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
                if (shop != null) shop.OpenShop();
            }
        }

        private void OnWarehouseClicked()
        {
            if (IsBlockedByTutorial("warehouse")) return;

            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            var wh = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
            if (wh != null)
            {
                wh.OpenPopup();
            }
            else
            {
                Debug.LogWarning("[TownshipHUD] Không tìm thấy WarehousePopupUI trong Scene!");
            }
        }

        private void OnMarketClicked()
        {
            if (IsBlockedByTutorial("market")) return;

            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            // Mở Bảng Tin Chợ (OrderBoard / Stall)
            var orderBoard = Object.FindFirstObjectByType<OrderBoardPopupUI>(FindObjectsInactive.Include);
            if (orderBoard != null)
            {
                orderBoard.OpenPopup();
                return;
            }

            var stall = Object.FindFirstObjectByType<StallPopupUI>(FindObjectsInactive.Include);
            if (stall != null)
            {
                stall.OpenPopup();
                return;
            }

            Debug.Log("[TownshipHUD] Mở Bảng Tin Chợ...");
        }

        private void OnMissionButtonClicked()
        {
            if (IsBlockedByTutorial("mission")) return;

            if (goMissionWidget != null)
            {
                goMissionWidget.SetActive(!goMissionWidget.activeSelf);
            }
            else
            {
                UnifiedTaskPopupUI.OpenMission();
            }
        }

        private void OnMissionGoClicked()
        {
            if (IsBlockedByTutorial("mission")) return;

            if (goMissionWidget != null)
                goMissionWidget.SetActive(false);

            UnifiedTaskPopupUI.OpenMission();
        }

        private void OnMissionClicked()
        {
            OnMissionButtonClicked();
        }

        private void OnCookingClicked()
        {
            if (IsBlockedByTutorial("cooking")) return;

            if (goMissionWidget != null) goMissionWidget.SetActive(false);

            // Chuyển sang Cooking Scene
            Debug.Log("[TownshipHUD] Chuyển sang Nấu Ăn (Cooking Scene)...");
            if (FarmUIManager.Instance != null)
            {
                FarmUIManager.Instance.OnClick_GoCooking();
            }
            else if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene("SampleScene", SceneTransitionManager.TransitionType.CloudWipe, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
            else if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene("SampleScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }

        private void OnFishingClicked()
        {
            if (IsBlockedByTutorial("fishing")) return;

            if (goMissionWidget != null) goMissionWidget.SetActive(false);
            FarmGame.Fishing.FishingEntryPopupUI.Open();
        }

        private void OnMapClicked()
        {
            OnCookingClicked();
        }

        private void OnSettingsClicked()
        {
            if (IsBlockedByTutorial("settings")) return;

            var settings = SettingsPopupUI.FindOrCreate();
            if (settings != null)
            {
                settings.OpenPopup();
            }
            else
            {
                Debug.Log("[TownshipHUD] Mở Cài Đặt...");
            }
        }
    }
}
