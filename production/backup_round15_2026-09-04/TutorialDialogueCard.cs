using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CARD HỘI THOẠI TUTORIAL V2 — khung bo góc + gõ chữ + nút "Tiếp tục" thật.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// CHỦ FILE: DEV-UI. Không Dev nào khác sửa file này (luật "mỗi file một chủ").
///
/// THAY CHO CÁI GÌ — 3 vấn đề đo được ngày 04/09 trong SCN_Farm.unity:
///   ① `NPC_Background` có `m_Sprite: {fileID:0}` và `m_Color.a: 0` ⇒ khung hội thoại
///      TRONG SUỐT HOÀN TOÀN, chữ nằm trần trên nền game, rất khó đọc trên ruộng nhiều màu.
///   ② Không có nút "Tiếp tục" — cả panel 800×200 là Button vô hình ⇒ người chơi
///      không biết phải bấm đâu để đi tiếp.
///   ③ Typewriter cũ (`TutorialManager.cs:861`) nối chuỗi từng ký tự `text += c` ⇒
///      mỗi câu thoại sinh hàng trăm string rác cho GC. Ở đây dùng
///      `TMP_Text.maxVisibleCharacters` — gán text MỘT LẦN, chỉ tăng số ký tự hiện.
///
/// KHUNG DÙNG LẠI (Sếp chốt 04/09, không vẽ mới):
///   Assets/Export_Kitchen_UI_Package/Sprites/panel_paper_cream.png — 9-slice border {24,24,24,24}
///   ⇒ đặt Image.type = Sliced là co giãn mọi kích thước không vỡ viền.
///
/// AN TOÀN: card này KHÔNG xoá `NPC_Dialog_Popup` cũ. TutorialManager giữ cờ `useV2Dialogue`
/// — tắt cờ là tutorial về nguyên bản cũ 100%.
///
/// [TutorialV2]
/// </summary>
[DisallowMultipleComponent]
public class TutorialDialogueCard : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    [Header("◆ Thành phần (Editor tool tự gán — bỏ trống sẽ tự dò)")]
    [Tooltip("Object bọc cả cụm card + NPC. Bật/tắt cái này là ẩn/hiện toàn bộ hội thoại.")]
    [SerializeField] private GameObject root;

    [Tooltip("CanvasGroup của root — dùng để fade vào/ra.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("RectTransform của riêng tấm card (không gồm NPC) — cái này chạy animation bung ra.")]
    [SerializeField] private RectTransform cardRect;

    [Tooltip("Text thoại. BẮT BUỘC là TMP để dùng được maxVisibleCharacters.")]
    [SerializeField] private TMP_Text bodyText;

    [Tooltip("Nút 'Tiếp tục' — chỉ hiện SAU KHI gõ xong chữ.")]
    [SerializeField] private Button continueButton;

    [Tooltip("Mũi tên ▶ trên nút Tiếp tục — nảy nhẹ để kéo mắt người chơi.")]
    [SerializeField] private RectTransform continueChevron;

    [Tooltip("NPC đứng bên trái card.")]
    [SerializeField] private TutorialNpcActor npc;

    [Header("◆ Gõ chữ")]
    [Tooltip("Số ký tự mỗi giây. 45-60 là dễ đọc; cao hơn thành 'hiện phựt', mất cảm giác kể chuyện.")]
    [SerializeField] private float charsPerSecond = 52f;

    [Tooltip("Bấm vào card lúc đang gõ = hiện hết chữ ngay (giữ thói quen cũ của người chơi).")]
    [SerializeField] private bool tapToSkipTyping = true;

    [Header("◆ Animation vào / ra")]
    [SerializeField] private float showDuration = 0.28f;
    [SerializeField] private float hideDuration = 0.18f;

    [Tooltip("Card bắt đầu ở tỉ lệ này rồi bung về 1.0 (ease-out-back).")]
    [SerializeField] private float showFromScale = 0.92f;

    [Tooltip("Card trượt lên từ dưới bao nhiêu pixel khi hiện.")]
    [SerializeField] private float showSlidePixels = 40f;

    [Header("◆ Nhịp nảy của mũi tên Tiếp tục")]
    [SerializeField] private float chevronBouncePixels = 6f;
    [SerializeField] private float chevronBounceCycle  = 0.75f;

    // ═══════════════════════════════════════════════════════════════════════
    private Coroutine _typeRoutine;
    private Coroutine _showRoutine;
    private Coroutine _chevronRoutine;

    private Action _onContinue;
    private Action _onTap;
    private bool   _dangGoChu;
    private Vector2 _cardHomePos;
    private Vector2 _chevronHomePos;
    private bool   _daLuuViTriGoc;

    /// <summary>Đang gõ chữ dở dang hay không (TutorialManager hỏi để xử lý tap).</summary>
    public bool DangGoChu => _dangGoChu;

    /// <summary>Card đang mở hay không.</summary>
    public bool DangMo => root != null && root.activeSelf;

    // ═══════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        TuDoThanhPhanThieu();
        LuuViTriGoc();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(BamTiepTuc);
            continueButton.onClick.AddListener(BamTiepTuc);
        }

        // ⚠ TUYỆT ĐỐI KHÔNG tự SetActive(false) khi root CHÍNH LÀ object này.
        // Lý do (QA bắt được 04/09): Editor tool để object TẮT sẵn ⇒ Awake chưa chạy.
        // Lần Show() đầu gọi root.SetActive(true) → Unity chạy Awake NGAY trong lời gọi đó
        // → nếu Awake tắt lại thì object tắt tức thì, mọi StartCoroutine sau đó đều hỏng
        // ("Coroutine couldn't be started because the game object is inactive") ⇒ BƯỚC
        // TUTORIAL ĐẦU TIÊN KHÔNG HIỆN GÌ và người chơi kẹt luôn.
        if (root != null && root != gameObject) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(BamTiepTuc);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // API công khai — TutorialManager gọi vào đây
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mở card, gõ <paramref name="noiDung"/>, cho NPC diễn <paramref name="clip"/>.
    /// Gõ xong mới hiện nút Tiếp tục; bấm nút thì gọi <paramref name="khiBamTiepTuc"/>.
    /// Truyền <c>null</c> cho callback nếu bước này chờ người chơi THAO TÁC (không phải bấm tiếp).
    /// </summary>
    /// <param name="khiBamTiepTuc">Bấm NÚT "Tiếp tục". Null ⇒ bước này không có nút.</param>
    /// <param name="khiChamCard">
    /// Chạm vào BẤT KỲ đâu trên card (kể cả bước không có nút Tiếp tục).
    /// Đây là thứ khôi phục đúng hành vi bản cũ — nơi cả tấm NPC_Dialog_Popup là một Button
    /// nối vào TutorialManager.NextStep. Thiếu nó, các bước chờ THAO TÁC
    /// (WaitForSpeedUp, WaitForHarvest...) mất sạch đường thoát và tutorial KẸT CỨNG
    /// (QA vòng 2 bắt được ở bước L1L2_15_FlowerSpeedUp — lớp dim không lỗ nuốt hết click).
    /// </param>
    public void Show(string noiDung, TutorialNpcClip clip = TutorialNpcClip.Talk,
                     Action khiBamTiepTuc = null, Action khiChamCard = null)
    {
        TuDoThanhPhanThieu();
        LuuViTriGoc();

        _onContinue = khiBamTiepTuc;
        _onTap      = khiChamCard;

        if (root != null && !root.activeSelf) root.SetActive(true);

        // Bước chờ thao tác thì KHÔNG có nút Tiếp tục — nếu không người chơi bấm Tiếp tục
        // để nhảy qua và bỏ luôn thao tác cần học.
        AnNutTiepTuc();

        if (npc != null)
        {
            if (npc.gameObject.activeSelf) npc.Play(clip);
            else                            npc.PlayEnter(clip);
        }

        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = gameObject.activeInHierarchy ? StartCoroutine(ChayHienCard()) : null;

        BatDauGoChu(noiDung);
    }

    /// <summary>Đóng card (fade + tụt xuống). An toàn khi gọi lúc card đang đóng.</summary>
    public void Hide()
    {
        if (root == null || !root.activeSelf) return;

        DungGoChu();
        AnNutTiepTuc();

        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ChayAnCard());
    }

    /// <summary>
    /// Hiện hết chữ ngay lập tức. Giữ đúng hành vi `SkipTyping()` cũ để người chơi
    /// quen tay không bị hụt.
    /// </summary>
    public void SkipTyping()
    {
        if (!_dangGoChu) return;
        DungGoChu();
        if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        GoXong();
    }

    /// <summary>
    /// Bấm vào bất kỳ đâu trên card. Đang gõ ⇒ hiện hết chữ. Gõ xong rồi ⇒ coi như bấm Tiếp tục
    /// (chỉ khi bước này CÓ nút Tiếp tục). Gán vào Button của tấm card.
    /// </summary>
    public void BamVaoCard()
    {
        if (_dangGoChu)
        {
            if (tapToSkipTyping) SkipTyping();
            return;
        }

        if (continueButton != null && continueButton.gameObject.activeSelf)
        {
            BamTiepTuc();
            return;
        }

        // Bước KHÔNG có nút Tiếp tục (đang chờ người chơi thao tác): chạm card vẫn phải
        // báo lên Manager, y như bản cũ. Manager tự quyết định advance hay chỉ dismiss.
        // KHÔNG xoá _onTap — người chơi có thể chạm nhiều lần, giống hệt Button cũ.
        _onTap?.Invoke();
    }

    /// <summary>
    /// [VÒNG 14] Hiện nút LỐI THOÁT khi watchdog phát hiện bước bị kẹt.
    /// Dùng lại chính nút "Tiếp tục" nhưng đổi chữ, để không phải dựng thêm nút mới —
    /// và quan trọng hơn: người chơi đã quen bấm đúng chỗ đó.
    /// </summary>
    public void HienNutBoQua(string nhan, Action khiBam)
    {
        _onContinue = khiBam;

        if (continueButton == null) return;

        var lbl = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null) lbl.text = nhan;

        HienNutTiepTuc();
    }

    /// <summary>Trả nhãn nút về "Tiếp tục" sau khi đã thoát khỏi bước kẹt.</summary>
    public void TraLaiNhanTiepTuc()
    {
        if (continueButton == null) return;
        var lbl = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null) lbl.text = "Tiếp tục";
    }

    /// <summary>Đổi clip NPC giữa chừng mà không đụng tới chữ (VD: chuyển Talk → Point).</summary>
    public void DoiClipNpc(TutorialNpcClip clip)
    {
        if (npc != null) npc.Play(clip);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Gõ chữ — maxVisibleCharacters, KHÔNG nối chuỗi (0 rác GC)
    // ═══════════════════════════════════════════════════════════════════════

    private void BatDauGoChu(string noiDung)
    {
        DungGoChu();

        if (bodyText == null) return;

        bodyText.text = noiDung ?? string.Empty;
        bodyText.ForceMeshUpdate();               // cập nhật textInfo.characterCount ngay
        bodyText.maxVisibleCharacters = 0;

        int tong = bodyText.textInfo.characterCount;
        if (tong <= 0) { GoXong(); return; }

        // Chỉ bật cờ SAU KHI coroutine chắc chắn khởi động được. Nếu object đang inactive,
        // StartCoroutine trả null — bật cờ trước sẽ khiến _dangGoChu kẹt TRUE vĩnh viễn,
        // nút Tiếp tục không bao giờ ăn (QA bắt 04/09).
        _typeRoutine = gameObject.activeInHierarchy ? StartCoroutine(ChayGoChu(tong)) : null;

        if (_typeRoutine != null)
        {
            _dangGoChu = true;
        }
        else
        {
            bodyText.maxVisibleCharacters = int.MaxValue;   // hiện hết chữ, đừng để trắng card
            GoXong();
        }
    }

    private IEnumerator ChayGoChu(int tongKyTu)
    {
        float cps = Mathf.Max(1f, charsPerSecond);
        float hien = 0f;

        while (hien < tongKyTu)
        {
            // unscaledDeltaTime: tutorial có bước mở lúc Time.timeScale = 0.
            hien += cps * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min(tongKyTu, Mathf.FloorToInt(hien));
            yield return null;
        }

        bodyText.maxVisibleCharacters = int.MaxValue;
        _typeRoutine = null;
        GoXong();
    }

    private void DungGoChu()
    {
        if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
        _dangGoChu = false;
    }

    private void GoXong()
    {
        _dangGoChu = false;
        if (_onContinue != null) HienNutTiepTuc();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Nút Tiếp tục
    // ═══════════════════════════════════════════════════════════════════════

    private void HienNutTiepTuc()
    {
        if (continueButton == null) return;
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;

        if (continueChevron != null && _chevronRoutine == null && gameObject.activeInHierarchy)
            _chevronRoutine = StartCoroutine(ChayNayMuiTen());
    }

    private void AnNutTiepTuc()
    {
        if (continueButton != null)
        {
            continueButton.interactable = false;
            continueButton.gameObject.SetActive(false);
        }

        if (_chevronRoutine != null) { StopCoroutine(_chevronRoutine); _chevronRoutine = null; }
        if (continueChevron != null && _daLuuViTriGoc) continueChevron.anchoredPosition = _chevronHomePos;
    }

    private void BamTiepTuc()
    {
        if (_dangGoChu) { SkipTyping(); return; }

        Action cb = _onContinue;
        _onContinue = null;
        AnNutTiepTuc();
        cb?.Invoke();
    }

    private IEnumerator ChayNayMuiTen()
    {
        float chuKy = Mathf.Max(0.2f, chevronBounceCycle);
        float bienDo = chevronBouncePixels;
        float t = 0f;

        while (true)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Sin(t / chuKy * Mathf.PI * 2f);
            continueChevron.anchoredPosition = _chevronHomePos + new Vector2(s * bienDo, 0f);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Animation vào / ra
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator ChayHienCard()
    {
        float dur = Mathf.Max(0.01f, showDuration);
        float t = 0f;

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / dur);
            float e = EaseOutBack(r, 1.1f);

            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(r * 1.6f);

            if (cardRect != null)
            {
                float sc = Mathf.LerpUnclamped(showFromScale, 1f, e);
                cardRect.localScale = new Vector3(sc, sc, 1f);
                cardRect.anchoredPosition = Vector2.LerpUnclamped(
                    _cardHomePos + new Vector2(0f, -showSlidePixels), _cardHomePos, e);
            }
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition = _cardHomePos;
        }
        _showRoutine = null;
    }

    private IEnumerator ChayAnCard()
    {
        float dur = Mathf.Max(0.01f, hideDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / dur);

            if (canvasGroup != null) canvasGroup.alpha = 1f - r;
            if (cardRect != null)
                cardRect.anchoredPosition = Vector2.Lerp(_cardHomePos, _cardHomePos + new Vector2(0f, -showSlidePixels * 0.6f), r);

            yield return null;
        }

        if (cardRect != null) cardRect.anchoredPosition = _cardHomePos;
        if (canvasGroup != null) canvasGroup.alpha = 1f;   // trả về 1 để lần mở sau không bị mờ
        if (npc != null) npc.Stop();
        if (root != null) root.SetActive(false);
        _showRoutine = null;
    }

    private static float EaseOutBack(float t, float doVot)
    {
        float c1 = 1.70158f * doVot;
        float c3 = c1 + 1f;
        float p  = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    private void TuDoThanhPhanThieu()
    {
        if (root == null) root = gameObject;
        if (canvasGroup == null) canvasGroup = root.GetComponent<CanvasGroup>();
        if (bodyText == null) bodyText = GetComponentInChildren<TMP_Text>(true);
        if (npc == null) npc = GetComponentInChildren<TutorialNpcActor>(true);
    }

    private void LuuViTriGoc()
    {
        if (_daLuuViTriGoc) return;
        if (cardRect != null) _cardHomePos = cardRect.anchoredPosition;
        if (continueChevron != null) _chevronHomePos = continueChevron.anchoredPosition;
        _daLuuViTriGoc = true;
    }
}
