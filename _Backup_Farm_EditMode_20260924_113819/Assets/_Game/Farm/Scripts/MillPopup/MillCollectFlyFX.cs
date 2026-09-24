using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BAY VỀ KHO — người chơi bấm THU, icon sản phẩm bung ra rồi bay về nút KHO ở HUD.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO KHÔNG DÙNG LẠI HarvestFeedbackSpawner
/// ══════════════════════════════════════════════════════════════════════════
/// `HarvestFeedbackSpawner.SpawnHarvestFly` bay trong KHÔNG GIAN WORLD (prefab
/// `PF_HarvestFlyItem_World_Clean`, đích là `FX_Target_Warehouse` — cái nhà kho trên đồng).
/// Popup máy xay là UI phủ kín màn hình kèm lớp Dim đen 55% ⇒ icon world sẽ bay SAU lớp
/// dim, người chơi không thấy gì. Vì vậy hiệu ứng này phải là UI thuần.
///
/// ══════════════════════════════════════════════════════════════════════════
///  CANVAS NÀO — CẠM BẪY THỨ TỰ VẼ
/// ══════════════════════════════════════════════════════════════════════════
/// Trong SCN_Farm:  Canvas_HUD sortingOrder 100  ·  Canvas_Popup 150  ·
///                  MillPopup_Root (overrideSorting) 400.
/// `CoinFlyFX` gắn xu vào Canvas_HUD — nếu bắt chước y hệt thì icon bay dưới popup và biến
/// mất. Ở đây icon được gắn vào CANVAS CỦA POPUP (order 400) nên luôn nằm trên; toạ độ đích
/// (nút KHO nằm ở Canvas_HUD) được quy đổi bằng cặp
/// <c>WorldToScreenPoint</c> → <c>ScreenPointToLocalPointInRectangle</c>. Cả hai canvas đều
/// ScreenSpaceOverlay nên camera = null và phép quy đổi là chính xác bất kể CanvasScaler
/// hai bên có khác nhau.
///
/// ══════════════════════════════════════════════════════════════════════════
///  THỜI GIAN KHÔNG SCALE
/// ══════════════════════════════════════════════════════════════════════════
/// `CoinFlyFX` dùng <c>Time.deltaTime</c> — nó chạy lúc đang chơi nên không sao. Popup này
/// mở lúc <c>timeScale = 0</c> nên ở đây BẮT BUỘC <c>Time.unscaledDeltaTime</c>.
/// </summary>
[DisallowMultipleComponent]
public class MillCollectFlyFX : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Canvas để gắn icon bay. NÊN là canvas của popup (MillPopup_Root, order 400).\n" +
             "ĐỂ TRỐNG ⇒ code tự tìm canvas gần nhất phía trên.")]
    [SerializeField] private Canvas canvasBay;

    [Tooltip("TUỲ CHỌN. Đích bay — nút KHO ở HUD (`Tab_Warehouse`).\n" +
             "ĐỂ TRỐNG ⇒ code tự tìm qua TownshipHUDController.Instance.btnTabWarehouse, " +
             "không thấy nữa thì bay về góc dưới-trái màn hình (nơi cụm nav HUD nằm).")]
    [SerializeField] private RectTransform diemDen;

    [Header("Số lượng & nhịp")]
    [Tooltip("Số icon bay mỗi lần thu. Nhiều icon cho cảm giác 'được nhiều', không phải số thật.")]
    [Range(1, 8)]
    [SerializeField] private int soIcon = 3;

    [Tooltip("Cạnh icon lúc mới bung, pixel.")]
    [SerializeField] private float kichCoIcon = 46f;

    [Tooltip("Thời gian bung ra khỏi slot, giây.")]
    [SerializeField] private float thoiGianBung = 0.16f;

    [Tooltip("Bán kính bung ngẫu nhiên, pixel.")]
    [SerializeField] private float banKinhBung = 46f;

    [Tooltip("Thời gian bay từ chỗ bung về kho, giây.")]
    [SerializeField] private float thoiGianBay = 0.62f;

    [Tooltip("Độ trễ giữa hai icon liên tiếp, giây.")]
    [SerializeField] private float treGiuaIcon = 0.06f;

    [Tooltip("Tỉ lệ co nhỏ lúc chạm kho.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float scaleCuoi = 0.4f;

    [Tooltip("Độ vồng của đường bay, pixel. 0 = bay thẳng.")]
    [SerializeField] private float doVong = 90f;

    /// <summary>Mọi icon đang sống — dùng để dọn sạch trong OnDisable.</summary>
    private readonly List<GameObject> _dangBay = new List<GameObject>();

    private Canvas        _canvasDaTim;
    private RectTransform _canvasRt;
    private bool          _daCanhBaoThieuCanvas;

    private void OnDisable()
    {
        DonSach();
    }

    /// <summary>
    /// Huỷ mọi icon đang bay.
    ///
    /// ⚠ PHẢI ĐƯỢC GỌI TỪ `MillPopupUI.Close()`, KHÔNG thể trông vào `OnDisable`:
    /// component này nằm trên node GỐC của popup (node mang Canvas), còn `Close()` chỉ tắt
    /// node con `PopupRoot`. Node gốc vẫn active ⇒ `OnDisable` KHÔNG chạy ⇒ icon tiếp tục
    /// bay lơ lửng trên mặt đồng ruộng sau khi popup đã đóng. Icon lại được gắn thẳng vào
    /// canvas (chứ không vào PopupRoot) nên nó cũng không bị tắt theo.
    /// </summary>
    public void DonSach()
    {
        // Coroutine chết theo component khi disable, nhưng ở đây có thể component vẫn sống
        // ⇒ dừng tường minh rồi mới huỷ object.
        StopAllCoroutines();

        for (int i = 0; i < _dangBay.Count; i++)
        {
            if (_dangBay[i] != null) Destroy(_dangBay[i]);
        }
        _dangBay.Clear();
    }

    // ─────────────────────────── API CÔNG KHAI ───────────────────────────

    /// <summary>
    /// Bung <paramref name="icon"/> ra từ <paramref name="tuDau"/> rồi bay về nút KHO.
    /// </summary>
    /// <param name="icon">Sprite sản phẩm. null ⇒ không làm gì (thà không có hiệu ứng còn
    /// hơn bay một ô vuông trắng).</param>
    /// <param name="tuDau">Node xuất phát — thường là slot vừa bấm THU. null ⇒ dùng node này.</param>
    public void Bay(Sprite icon, RectTransform tuDau)
    {
        if (!isActiveAndEnabled) return;
        if (icon == null) return;

        Canvas c = CanvasBay;
        if (c == null)
        {
            if (!_daCanhBaoThieuCanvas)
            {
                _daCanhBaoThieuCanvas = true;
                Debug.LogWarning("[MILL] MillCollectFlyFX không tìm được Canvas ⇒ bỏ hiệu ứng bay về kho.", this);
            }
            return;
        }

        _canvasRt = c.transform as RectTransform;
        if (_canvasRt == null) return;

        RectTransform nguon = (tuDau != null) ? tuDau : (transform as RectTransform);
        if (nguon == null) return;

        // TÂM chứ không phải `.position`: thẻ slot neo góc trên-trái nên `.position` là mép
        // trên-trái, icon sẽ bung ra từ góc thẻ thay vì từ giữa. Xem MillRectUtil.
        Vector2 batDau = QuyVeCanvas(MillRectUtil.TamWorld(nguon));
        Vector2 dich   = LayDiemDen();

        int n = Mathf.Clamp(soIcon, 1, 8);
        for (int i = 0; i < n; i++)
            StartCoroutine(BayMotIcon(icon, batDau, dich, i * Mathf.Max(0f, treGiuaIcon)));
    }

    // ─────────────────────────── COROUTINE ───────────────────────────

    private IEnumerator BayMotIcon(Sprite icon, Vector2 batDau, Vector2 dich, float tre)
    {
        if (tre > 0f)
        {
            float doi = 0f;
            while (doi < tre)
            {
                doi += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        RectTransform it = TaoIcon(icon);
        if (it == null) yield break;

        Vector2 diemBung = batDau + Random.insideUnitCircle * Mathf.Max(0f, banKinhBung);
        float   xoay     = Random.Range(-180f, 180f);

        it.anchoredPosition = batDau;

        // ── Pha 1: bung nhẹ ra khỏi slot ──
        float tongBung = Mathf.Max(0.02f, thoiGianBung);
        float t = 0f;
        while (t < tongBung)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tongBung);
            k = k * (2f - k);                              // ease-out rẻ tiền, không cấp phát
            it.anchoredPosition = Vector2.Lerp(batDau, diemBung, k);
            it.Rotate(0f, 0f, xoay * Time.unscaledDeltaTime);
            yield return null;
        }

        // ── Pha 2: bay về kho theo đường vồng, co nhỏ dần ──
        Vector2 giua = (diemBung + dich) * 0.5f + Vector2.up * doVong;

        float tongBay = Mathf.Max(0.05f, thoiGianBay);
        t = 0f;
        Image img = it.GetComponent<Image>();

        while (t < tongBay)
        {
            t += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(t / tongBay);
            float k   = raw * raw * (3f - 2f * raw);        // smoothstep

            it.anchoredPosition = Bezier(diemBung, giua, dich, k);

            float s = Mathf.Lerp(1f, scaleCuoi, raw);
            it.localScale = new Vector3(s, s, 1f);
            it.Rotate(0f, 0f, xoay * Time.unscaledDeltaTime);

            // Mờ hẳn ở 20% cuối để icon "nhập kho" chứ không bật tắt đột ngột.
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - Mathf.InverseLerp(0.8f, 1f, raw);
                img.color = c;
            }

            yield return null;
        }

        _dangBay.Remove(it.gameObject);
        Destroy(it.gameObject);
    }

    // ─────────────────────────── NỘI BỘ ───────────────────────────

    private RectTransform TaoIcon(Sprite icon)
    {
        var go = new GameObject("MillCollectFly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = _canvasRt.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(_canvasRt, false);

        // Neo trùng pivot canvas — cùng lý do như MillIntakeFX, xem MillRectUtil.
        MillRectUtil.DatNeoTheoPivotCha(rt, _canvasRt);

        float c = (kichCoIcon > 1f) ? kichCoIcon : 46f;
        rt.sizeDelta  = new Vector2(c, c);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();

        Image im = go.GetComponent<Image>();
        im.sprite         = icon;
        im.raycastTarget  = false;   // icon bay qua nút, không được ăn click
        im.preserveAspect = true;
        im.color          = Color.white;

        _dangBay.Add(go);
        return rt;
    }

    /// <summary>
    /// Toạ độ đích trong hệ cục bộ của canvas bay.
    /// Ưu tiên: field wire tay → nút KHO của HUD → góc dưới-trái màn hình.
    /// </summary>
    private Vector2 LayDiemDen()
    {
        RectTransform muc = diemDen;

        if (muc == null)
        {
            // Không cache kết quả: HUD có thể được dựng lại bởi TownshipHUDBuilderTool giữa
            // hai lần mở popup, cache lại là giữ một tham chiếu đã chết.
            var hud = FarmGame.UI.TownshipHUDController.Instance;
            if (hud != null && hud.btnTabWarehouse != null)
                muc = hud.btnTabWarehouse.transform as RectTransform;
        }

        if (muc != null)
            return QuyVeCanvas(MillRectUtil.TamWorld(muc));

        // Dự phòng: cụm nav HUD nằm ở góc DƯỚI-TRÁI (BottomLeft_Nav_Group), nên bay về đó.
        Vector2 manHinh = new Vector2(Screen.width * 0.22f, Screen.height * 0.10f);
        return QuyVeCanvasTuManHinh(manHinh);
    }

    private Vector2 QuyVeCanvas(Vector3 viTriWorld)
    {
        Camera cam = LayCamera();
        Vector2 manHinh = RectTransformUtility.WorldToScreenPoint(cam, viTriWorld);
        return QuyVeCanvasTuManHinh(manHinh);
    }

    private Vector2 QuyVeCanvasTuManHinh(Vector2 manHinh)
    {
        Camera cam = LayCamera();

        Vector2 cucBo;
        if (_canvasRt != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, manHinh, cam, out cucBo))
            return cucBo;

        return Vector2.zero;
    }

    private Camera LayCamera()
    {
        Canvas c = CanvasBay;
        if (c == null) return null;

        // ScreenSpaceOverlay ⇒ PHẢI truyền null. Canvas lồng nhau trả về renderMode của
        // canvas gốc nên phép so này vẫn đúng cho MillPopup_Root.
        return (c.renderMode == RenderMode.ScreenSpaceOverlay) ? null : c.worldCamera;
    }

    private Canvas CanvasBay
    {
        get
        {
            if (canvasBay != null) return canvasBay;

            if (_canvasDaTim == null) _canvasDaTim = GetComponentInParent<Canvas>();
            return _canvasDaTim;
        }
    }

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }
}
