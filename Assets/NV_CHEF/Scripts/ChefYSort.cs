using UnityEngine;

/// <summary>
/// Y-SORT CHO ĐẦU BẾP — quyết định đầu bếp bị công trình che hay che công trình.
///
/// CÔNG THỨC:  sortingOrder = baseOrder - round(y * orderPerUnitY)
/// Vật ở DƯỚI (y nhỏ hơn) = gần camera hơn trong góc nhìn top-down 2.5D -> order LỚN hơn -> vẽ ĐÈ lên.
/// Dấu này TRÙNG với Assets/NV_01/Scripts/YSortIso.cs (order = -y * sortScale) nên 2 hệ tương thích.
///
/// ══ VÌ SAO VIẾT RIÊNG THAY VÌ DÙNG YSortIso.cs ══
/// Đã đọc YSortIso.cs. Nó ĐÚNG về công thức nhưng thiếu 3 thứ mà đầu bếp bắt buộc phải có:
///
/// 1) THIẾU baseOrder. Công trình trong Assets/_Game/Farm/CÔNG TRÌNH/* đang dùng
///    m_SortingOrder: 500 CỐ ĐỊNH (kiểm chứng: House_01/02, Đài nước, giếng_01, Bù nhìn...).
///    YSortIso cho order = -y, tức mốc so sánh là 0 -> đầu bếp chỉ đè công trình khi y < -500,
///    hoàn toàn không liên quan tới y của công trình. Có baseOrder = 500 thì mốc giao nhau về
///    đúng dải order của công trình.
/// 2) THIẾU điều khiển sortingLayer. Prefab dễ bị để sai layer mà không ai biết.
///    Component này TỰ ĐẶT layer mỗi lần chạy -> chống việc copy layer rác từ prefab cũ.
/// 3) LÃNG PHÍ. YSortIso tính lại mỗi LateUpdate. Đầu bếp ĐỨNG YÊN vĩnh viễn; nếu Edric rải
///    20 con thì đó là 20 phép tính vô ích/frame. Component này chỉ tính lại khi y ĐỔI THẬT.
///
/// ══ ⚠ CẢNH BÁO TÌNH TRẠNG DỰ ÁN (đọc kỹ) ══
/// SCN_Farm.unity có 218 SpriteRenderer trỏ m_SortingLayerID: 1669604809 — sorting layer NÀY ĐÃ BỊ XOÁ
/// (TagManager chỉ còn Bottom / Default / Objects / ObjectsFront / Foreground).
/// ID chết -> Unity coi như layer index 0 (= Bottom), tức NẰM DƯỚI layer "Objects".
/// Hệ quả: tới khi 218 renderer đó chưa được trỏ lại về "Objects", đầu bếp sẽ LUÔN vẽ trên công trình,
/// bất kể order — vì so sánh LAYER thắng so sánh ORDER.
/// Sửa (NGOÀI phạm vi thư mục NV_CHEF nên tôi KHÔNG tự sửa): trỏ 218 renderer đó về sorting layer "Objects".
/// Sau khi sửa, đầu bếp sẽ che/bị che đúng ngay, không cần đổi gì trong NV_CHEF.
/// </summary>
// [ExecuteAlways] BẮT BUỘC: nếu chỉ chạy lúc Play thì trong Scene view đầu bếp giữ
// order 500 ghi sẵn trong prefab → kéo vào map là thấy nó nổi đè hết công trình,
// chỉ đúng lại khi bấm Play. Designer sẽ tưởng bị lỗi.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class ChefYSort : MonoBehaviour
{
    [Tooltip("Sorting layer của đầu bếp. Dự án dùng 'Objects' cho vật thể Y-sort chung với công trình.")]
    public string sortingLayerName = "Objects";

    [Tooltip("Order nền.\n" +
             "• 500 = khớp dải order công trình (m_SortingOrder: 500) — dùng SAU KHI đã sửa sorting layer chết.\n" +
             "• 0   = khớp Player NV_01 (YSortIso dùng mốc 0) — dùng để 2 nhân vật sort đúng với nhau.\n" +
             "Hiện tại công trình đang ở sorting layer CHẾT (dưới 'Objects') nên đầu bếp LUÔN vẽ trên " +
             "công trình bất kể số này. Với đầu bếp đứng nấu ở quầy hàng thì đó thường là điều bạn muốn.")]
    public int baseOrder = 500;

    [Tooltip("Số bậc order cho mỗi 1 world unit theo Y. Map này toạ độ cỡ ±2000 world unit nên để 1: " +
             "order ra khoảng 500±2000, còn AN TOÀN trong giới hạn ±32767 của Order in Layer. " +
             "ĐỪNG để 100 — order sẽ tràn và bị kẹp sai.")]
    public float orderPerUnitY = 1f;

    [Tooltip("Điểm dùng để sort. Để TRỐNG = dùng chính transform này (pivot sprite là Bottom-Center " +
             "nên transform.position.y CHÍNH LÀ chỗ chân đứng -> sort đúng ngay).")]
    public Transform sortPoint;

    [Tooltip("BẬT nếu sau này đầu bếp có di chuyển/được kéo bằng script. " +
             "TẮT (mặc định) vẫn tự cập nhật khi y đổi, chỉ là bỏ qua khi y không đổi cho nhẹ.")]
    public bool luonCapNhat = false;

    private SpriteRenderer _sr;
    private float _lastY = float.NaN;
    private bool _daCanhBaoTran;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // Đặt layer NGAY tại Awake để không bao giờ dùng layer rác kế thừa từ prefab khác.
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            if (SortingLayerTonTai(sortingLayerName)) _sr.sortingLayerName = sortingLayerName;
            else Debug.LogWarning($"[ChefYSort] '{name}': không có sorting layer '{sortingLayerName}'. " +
                                  "Giữ nguyên layer hiện tại. Tạo layer trong Project Settings > Tags and Layers.", this);
        }
    }

    private void Start()  => ApDung();     // tính 1 lần ngay khi vào scene
    private void LateUpdate() => ApDung(); // LateUpdate: sau mọi chuyển động của frame

    private void ApDung()
    {
        if (_sr == null) return;

        float y = (sortPoint != null ? sortPoint.position.y : transform.position.y);
        // Bỏ qua nếu y không đổi -> gần như miễn phí cho NPC đứng yên.
        if (!luonCapNhat && y == _lastY) return;
        _lastY = y;

        long order = (long)baseOrder - Mathf.RoundToInt(y * orderPerUnitY);

        if (order > short.MaxValue || order < short.MinValue)
        {
            if (!_daCanhBaoTran)
            {
                _daCanhBaoTran = true;
                Debug.LogWarning($"[ChefYSort] '{name}': order {order} TRÀN giới hạn ±32767 " +
                                 $"(y={y:0.#}, orderPerUnitY={orderPerUnitY}). Giảm orderPerUnitY xuống.", this);
            }
            order = Mathf.Clamp((int)order, short.MinValue, short.MaxValue);
        }

        _sr.sortingOrder = (int)order;
    }

    private static bool SortingLayerTonTai(string n)
    {
        foreach (var l in SortingLayer.layers) if (l.name == n) return true;
        return false;
    }
}
