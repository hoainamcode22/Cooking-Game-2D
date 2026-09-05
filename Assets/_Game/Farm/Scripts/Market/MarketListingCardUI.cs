using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  THẺ HÀNG 2 TẦNG — tầng trên là hàng, tầng dưới là NGƯỜI BÁN
/// ══════════════════════════════════════════════════════════════════════════
///
/// Toàn bộ hierarchy của thẻ nằm trong PREFAB do
/// Tools/Farm/Chợ/Dựng lại UI Bảng Tin Chợ sinh ra. Script này CHỈ đổ dữ liệu,
/// không tạo GameObject nào — đúng bài học từ UnifiedTaskPopupUI 1433 dòng.
/// Hỗ trợ kéo vuốt cảm ứng trên Mobile và chuột trên PC mượt mà.
/// </summary>
public class MarketListingCardUI : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("Tầng trên — vật phẩm")]
    [SerializeField] private Image      imageIcon;
    [SerializeField] private TMP_Text   textItemName;
    [SerializeField] private TMP_Text   textQuantity;
    [SerializeField] private TMP_Text   textPrice;

    [Header("Tầng dưới — người bán")]
    [SerializeField] private Image      imageSellerAvatar;
    [SerializeField] private TMP_Text   textSellerInitial;
    [SerializeField] private TMP_Text   textSellerName;
    [SerializeField] private TMP_Text   textSellerLevel;

    [Header("Nhãn & trạng thái")]
    [SerializeField] private GameObject badgeDeal;          // nhãn "HỜI" khi rẻ hơn giá NPC
    [SerializeField] private TMP_Text   textDeal;
    [SerializeField] private GameObject badgePlayer;        // nhãn "CỦA BẠN"
    [SerializeField] private GameObject overlaySoldOut;
    [SerializeField] private Image      imageCardFrame;     // đổi màu viền theo danh mục

    [Header("Tương tác")]
    [SerializeField] private Button     buttonBuy;
    [SerializeField] private CanvasGroup canvasGroup;

    private string          listingId;
    private Action<string>  onBuyRequested;
    private Coroutine       revealCoroutine;
    private ScrollRect      _parentScrollRect;

    /// <summary>Id của listing đang hiển thị — MarketBoardUI dùng để tái sử dụng thẻ.</summary>
    public string ListingId => listingId;
    public Sprite ItemSprite => imageIcon != null ? imageIcon.sprite : null;
    public Vector3 IconScreenPosition => imageIcon != null ? imageIcon.transform.position : transform.position;

    private ScrollRect ParentScrollRect
    {
        get
        {
            if (_parentScrollRect == null)
                _parentScrollRect = GetComponentInParent<ScrollRect>();
            return _parentScrollRect;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (ParentScrollRect != null) ParentScrollRect.OnScroll(eventData);
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>Đổ dữ liệu một mặt hàng lên thẻ.</summary>
    public void Bind(MarketListing listing, MarketItemVisual visual, Action<string> buyCallback)
    {
        if (listing == null)
            return;

        listingId      = listing.ListingId;
        onBuyRequested = buyCallback;

        // ── Tầng trên ────────────────────────────────────────────────────
        if (imageIcon != null)
        {
            imageIcon.sprite  = visual.Icon;
            // Icon rỗng: giữ ô hiện hình bằng màu danh mục thay vì để lỗ trắng.
            // CHỖ CHỜ ART — gán icon vào asset là ô này tự có hình.
            imageIcon.enabled = true;
            imageIcon.color   = visual.Icon != null
                ? Color.white
                : MarketCategoryUtil.GetAccentColor(listing.Category);
        }

        if (textItemName != null)
            textItemName.text = string.IsNullOrEmpty(visual.DisplayName)
                ? MarketPriceTable.GetDisplayName(listing.ItemId)
                : visual.DisplayName;

        if (textQuantity != null)
            textQuantity.text = listing.Quantity.ToString();

        if (textPrice != null)
            textPrice.text = listing.TotalPrice.ToString("N0");

        if (imageCardFrame != null)
        {
            Color accent = MarketCategoryUtil.GetAccentColor(listing.Category);
            // Pha loãng về nền bảng để viền chỉ gợi ý danh mục, không chọi với icon
            imageCardFrame.color = Color.Lerp(MarketBoardPalette.CardBase, accent, 0.35f);
        }

        // ── Tầng dưới: Người bán ─────────────────────────────────────────
        if (imageSellerAvatar != null)
        {
            Sprite avSp = MarketSellerDirectory.GetAvatarSprite(listing.SellerAvatarIndex);
            if (avSp != null)
            {
                imageSellerAvatar.sprite = avSp;
                imageSellerAvatar.color  = Color.white;
                imageSellerAvatar.preserveAspect = true;
                if (textSellerInitial != null) textSellerInitial.gameObject.SetActive(false);
            }
            else
            {
                imageSellerAvatar.color = MarketSellerDirectory.GetAvatarColor(listing.SellerAvatarIndex);
                if (textSellerInitial != null)
                {
                    textSellerInitial.gameObject.SetActive(true);
                    textSellerInitial.text = MarketSellerDirectory.GetAvatarInitial(listing.SellerName);
                }
            }
        }

        if (textSellerName != null)
            textSellerName.text = listing.SellerName;

        if (textSellerLevel != null)
            textSellerLevel.text = listing.SellerLevel.ToString();

        // ── Nhãn ─────────────────────────────────────────────────────────
        int discount = listing.DiscountPercentVsNpc();
        bool isDeal  = discount <= -10;   // rẻ hơn 10% trở lên mới đáng gắn nhãn

        if (badgeDeal != null)
            badgeDeal.SetActive(isDeal);
        if (textDeal != null && isDeal)
            textDeal.text = discount + "%";

        if (badgePlayer != null)
            badgePlayer.SetActive(listing.IsPlayerListing);

        if (overlaySoldOut != null)
            overlaySoldOut.SetActive(listing.Status != MarketListingStatus.Active);

        if (buttonBuy != null)
        {
            buttonBuy.onClick.RemoveAllListeners();
            // Hàng của chính mình thì không cho bấm mua — mua lại chỉ mất tiền vô ích
            buttonBuy.interactable = listing.Status == MarketListingStatus.Active && !listing.IsPlayerListing;
            buttonBuy.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void HandleBuyClicked()
    {
        onBuyRequested?.Invoke(listingId);
    }

    /// <summary>Đánh dấu đã bán — thẻ mờ đi và không bấm được nữa.</summary>
    public void MarkSoldOut()
    {
        if (overlaySoldOut != null)
            overlaySoldOut.SetActive(true);

        if (buttonBuy != null)
            buttonBuy.interactable = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 0.55f;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HIỆN SO LE (stagger)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hiện thẻ sau một khoảng trễ: mờ→rõ + phóng nhẹ.
    /// Rẻ nhất trong mọi hiệu ứng mà làm cả bảng tin "sống" hẳn — 12 thẻ bụp ra
    /// cùng lúc trông như trang web tải xong, hiện lần lượt trông như hàng đang được bày.
    /// </summary>
    public void PlayReveal(float delaySeconds)
    {
        if (!gameObject.activeInHierarchy)
            return;   // StartCoroutine trên object đang tắt sẽ ném lỗi

        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        revealCoroutine = StartCoroutine(RevealRoutine(delaySeconds));
    }

    /// <summary>Bỏ qua hiệu ứng, hiện ngay. Dùng khi đổi tab — chờ lần nữa là bực.</summary>
    public void ShowImmediate()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        transform.localScale = Vector3.one;
    }

    private IEnumerator RevealRoutine(float delaySeconds)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        transform.localScale = Vector3.one * 0.86f;

        // Time.unscaledDeltaTime: popup vẫn phải chạy mượt kể cả khi game đang pause
        float waited = 0f;
        while (waited < delaySeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        const float duration = 0.16f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (canvasGroup != null)
                canvasGroup.alpha = eased;

            transform.localScale = Vector3.LerpUnclamped(Vector3.one * 0.86f, Vector3.one, eased);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        transform.localScale = Vector3.one;
        revealCoroutine = null;
    }
}
