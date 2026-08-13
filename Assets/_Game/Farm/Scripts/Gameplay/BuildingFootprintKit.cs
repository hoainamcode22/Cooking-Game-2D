using System.Collections;
using UnityEngine;

/// <summary>
/// BỘ KIT NỀN CHO CÔNG TRÌNH ĐANG ĐỨNG YÊN.
///
/// ══════════════════════════════════════════════════════════════════════════
///  NÓ THAY THẾ CÁI GÌ
/// ══════════════════════════════════════════════════════════════════════════
/// `EditableBuilding.footprintVisual` là một ô `GameObject` phải kéo tay vào Inspector
/// cho từng công trình. Kiểm tra dữ liệu thật cho thấy nó HỎNG ở mọi nơi:
///   • `House_01` → `{fileID: 0}` (bỏ trống)
///   • `House_02`, `Chauhoa_1/2`, `Pen_02`, `May_01..03`, cả 6 `Plot_0x` trong scene
///     → cùng trỏ tới fileID 8293749280720246623 trong `House_01.prefab`, mà object
///     đó KHÔNG TỒN TẠI trong file → tham chiếu gãy → null lúc chạy.
/// Nên `SetFootprintActive()` là lệnh rỗng ở toàn bộ công trình: vào Edit Mode không
/// có tấm thảm nào hiện lên.
///
/// Component này TỰ DỰNG kit lúc chạy, không phụ thuộc ô kéo tay nào. Gắn vào là có,
/// không cần nhớ gán gì.
///
/// ══════════════════════════════════════════════════════════════════════════
///  KIT GỒM GÌ
/// ══════════════════════════════════════════════════════════════════════════
///   Kit_Nen (gốc)
///     ├── Tham_Nen          thảm hình thoi bo góc, rỗng ruột
///     ├── Vien_0..3         4 vạch nét đứt chạy dọc 4 cạnh
///     ├── Ngoac_0..3        4 ngoặc chữ L ôm 4 góc vùng ô
///     └── Chip_Keo          chip "nắm để kéo" nổi trên nóc
///
/// Mỗi phần đều là `SpriteRenderer` riêng, tên cố định → chủ dự án vẽ art xong chỉ
/// việc thay `sprite`, không phải sửa code. Ảnh phải TRẮNG/XÁM vì màu do code nhuộm.
/// </summary>
[DisallowMultipleComponent]
public class BuildingFootprintKit : MonoBehaviour
{
    private const string TenGoc = "Kit_Nen";

    [Header("Kích thước vùng ô (0 = tự suy từ collider)")]
    [Tooltip("Số ô ngang × dọc mà công trình chiếm. Tool cài đặt tự điền theo asset dữ liệu.")]
    [SerializeField] private Vector2Int soO = Vector2Int.zero;

    [Header("Chỗ chờ art — để trống thì dùng hình vẽ bằng code")]
    [SerializeField] private Sprite spriteTham;
    [SerializeField] private Sprite spriteNgoac;
    [SerializeField] private Sprite spriteVach;
    [SerializeField] private Sprite spriteChip;

    [Header("Màu")]
    [Tooltip("Màu thảm khi công trình đứng yên trong Edit Mode.")]
    [SerializeField] private Color mauTham = new Color(0.37f, 0.85f, 0.66f, 0.55f);
    [Tooltip("Màu ngoặc góc và viền nét đứt.")]
    [SerializeField] private Color mauVien = new Color(0.42f, 0.95f, 0.72f, 0.95f);
    [Tooltip("Màu chip nắm kéo.")]
    [SerializeField] private Color mauChip = new Color(1f, 0.98f, 0.86f, 0.92f);

    [Header("Tinh chỉnh")]
    [Tooltip("Chiều cao chip nắm kéo so với đỉnh vùng ô, tính theo world unit.")]
    [SerializeField] private float caoChip = 46f;
    [Tooltip("Bật nhịp thở nhè nhẹ cho ngoặc góc.")]
    [SerializeField] private bool nhipTho = true;

    private Transform        _goc;
    private SpriteRenderer   _tham;
    private SpriteRenderer[] _ngoac;
    private SpriteRenderer[] _vach;
    private SpriteRenderer   _chip;
    private Coroutine        _nhip;
    private bool             _dangHien;

    /// <summary>Số ô công trình chiếm — tool cài đặt gọi lúc gắn kit.</summary>
    public Vector2Int SoO { get => soO; set => soO = value; }

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        EditModeManager.OnEditModeChanged -= DoiTrangThai;
        EditModeManager.OnEditModeChanged += DoiTrangThai;

        // Bật lại đúng trạng thái HIỆN TẠI: công trình vừa được đặt xuống GIỮA lúc người
        // chơi đang ở trong Edit Mode thì sự kiện đã bắn xong từ trước khi nó tồn tại.
        DoiTrangThai(EditModeManager.IsEditMode);
    }

    private void OnDisable()
    {
        EditModeManager.OnEditModeChanged -= DoiTrangThai;
        DungNhip();
    }

    private void DoiTrangThai(bool batEdit)
    {
        // Đang bị nhấc lên để di chuyển thì kit của Ghost lo, kit đứng yên phải im.
        if (batEdit && PlacementManager.IsPlacingNewObject) batEdit = false;

        if (batEdit == _dangHien && _goc != null) return;
        _dangHien = batEdit;

        if (batEdit)
        {
            BaoDamCoKit();
            CapNhatKichThuoc();
            if (_goc != null) _goc.gameObject.SetActive(true);
            BatNhip();
        }
        else
        {
            DungNhip();
            if (_goc != null) _goc.gameObject.SetActive(false);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  DỰNG
    // ═════════════════════════════════════════════════════════════════════════

    private void BaoDamCoKit()
    {
        if (_goc != null) return;

        Transform co = transform.Find(TenGoc);
        if (co != null)
        {
            _goc = co;
        }
        else
        {
            var go = new GameObject(TenGoc);
            go.layer = gameObject.layer;
            _goc = go.transform;
            _goc.SetParent(transform, false);
        }

        // Chuẩn hoá scale: prefab công trình dùng root scale 100 (quy ước "1 unit sprite
        // = 1 ô"). Chia ngược để BÊN TRONG kit, 1 đơn vị = 1 world unit — nhờ vậy mọi số
        // đo dưới đây đọc thẳng ra world unit, không phải rải phép chia 100 khắp nơi.
        Vector3 s = transform.lossyScale;
        _goc.localPosition = Vector3.zero;
        _goc.localRotation = Quaternion.identity;
        _goc.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
            1f);

        int thuTu = ThuTuVeDuoiChan();

        _tham = TaoRenderer("Tham_Nen", spriteTham ?? PlacementKitSpriteFactory.ThamHinhThoi(),
                            mauTham, thuTu);

        _vach = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
            _vach[i] = TaoRenderer($"Vien_{i}", spriteVach ?? PlacementKitSpriteFactory.VachNetDut(),
                                   mauVien, thuTu + 1);

        _ngoac = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
            _ngoac[i] = TaoRenderer($"Ngoac_{i}", spriteNgoac ?? PlacementKitSpriteFactory.NgoacGoc(),
                                    mauVien, thuTu + 2);

        _chip = TaoRenderer("Chip_Keo", spriteChip ?? PlacementKitSpriteFactory.ChipNamKeo(),
                            mauChip, thuTu + 3);
    }

    /// <summary>
    /// Kit phải nằm DƯỚI công trình, không đè lên mặt tiền.
    /// Lấy sortingOrder nhỏ nhất trong các SpriteRenderer của công trình rồi lùi thêm,
    /// nhưng CHỈ trong các renderer không thuộc kit — nếu không, mỗi lần bật lại kit
    /// nó lại tự lùi thêm một bậc và trôi dần xuống dưới mặt đất.
    /// </summary>
    private int ThuTuVeDuoiChan()
    {
        int min = int.MaxValue;
        string layer = null;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            if (_goc != null && sr.transform.IsChildOf(_goc)) continue;

            if (sr.sortingOrder < min)
            {
                min = sr.sortingOrder;
                layer = sr.sortingLayerName;
            }
        }

        if (min == int.MaxValue) { min = 500; layer = "Objects"; }
        _layerVe = layer;
        return min - 4;
    }

    private string _layerVe = "Objects";

    private SpriteRenderer TaoRenderer(string ten, Sprite spr, Color mau, int thuTu)
    {
        Transform co = _goc.Find(ten);
        GameObject go = co != null ? co.gameObject : new GameObject(ten);
        if (co == null) go.transform.SetParent(_goc, false);
        go.layer = gameObject.layer;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = spr;
        sr.color = mau;
        sr.sortingLayerName = _layerVe;
        sr.sortingOrder = thuTu;
        return sr;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ĐẶT VỊ TRÍ THEO VÙNG Ô
    // ═════════════════════════════════════════════════════════════════════════

    private void CapNhatKichThuoc()
    {
        if (_goc == null) return;

        Vector2Int o = soO;
        if (o.x <= 0 || o.y <= 0) o = SuySoOTuCollider();

        float w = o.x * PlacementManager.CELL;
        float h = o.y * PlacementManager.CELL;

        // Vùng ô mọc LÊN từ chân công trình (quy ước "V8" của PlacementManager: điểm neo
        // là mép dưới + giữa ngang). Nên tâm vùng ô nằm cao hơn gốc object đúng h/2.
        Vector3 tam = new Vector3(0f, h * 0.5f, 0f);

        if (_tham != null)
        {
            _tham.transform.localPosition = tam;
            DatKichThuoc(_tham, w, h);
        }

        // ── 4 vạch nét đứt dọc 4 cạnh ────────────────────────────────────────
        if (_vach != null)
        {
            float day = 7f;
            DatVach(_vach[0], new Vector3(0f, h, 0f), w * 0.86f, day, 0f);      // trên
            DatVach(_vach[1], new Vector3(0f, 0f, 0f), w * 0.86f, day, 0f);      // dưới
            DatVach(_vach[2], new Vector3(-w * 0.5f, h * 0.5f, 0f), h * 0.86f, day, 90f);  // trái
            DatVach(_vach[3], new Vector3(w * 0.5f, h * 0.5f, 0f), h * 0.86f, day, 90f);   // phải
        }

        // ── 4 ngoặc chữ L ôm 4 góc ───────────────────────────────────────────
        if (_ngoac != null)
        {
            // Cạnh ngoặc lấy theo cạnh NGẮN của vùng ô để công trình 7×5 không có ngoặc
            // to bằng nửa chiều ngang. Kẹp lại để vật 1×1 vẫn nhìn ra là ngoặc.
            float canh = Mathf.Clamp(Mathf.Min(w, h) * 0.32f, 26f, 62f);

            //  góc:   0 = trên-trái, 1 = trên-phải, 2 = dưới-phải, 3 = dưới-trái
            //  sprite gốc vẽ cho góc trên-trái → xoay 0/-90/180/90 độ
            DatNgoac(_ngoac[0], new Vector3(-w * 0.5f, h, 0f), canh, 0f);
            DatNgoac(_ngoac[1], new Vector3(w * 0.5f, h, 0f), canh, -90f);
            DatNgoac(_ngoac[2], new Vector3(w * 0.5f, 0f, 0f), canh, 180f);
            DatNgoac(_ngoac[3], new Vector3(-w * 0.5f, 0f, 0f), canh, 90f);
        }

        if (_chip != null)
        {
            _chip.transform.localPosition = new Vector3(0f, h + caoChip, 0f);
            DatKichThuoc(_chip, 54f, 54f);
        }

        // Mốc gốc đã cũ vì vừa đổi kích thước — buộc nhịp thở chụp lại ở khung sau.
        _scaleGocNgoac = null;
    }

    private void DatVach(SpriteRenderer sr, Vector3 viTri, float dai, float day, float goc)
    {
        if (sr == null) return;
        sr.transform.localPosition = viTri;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, goc);
        DatKichThuoc(sr, dai, day);
    }

    private void DatNgoac(SpriteRenderer sr, Vector3 viTri, float canh, float goc)
    {
        if (sr == null) return;
        sr.transform.localPosition = viTri;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, goc);
        DatKichThuoc(sr, canh, canh);
    }

    /// <summary>
    /// Ép SpriteRenderer về đúng kích thước world mong muốn.
    ///
    /// KHÔNG dùng `drawMode = Sliced` + `size`: sprite vẽ bằng code không có border
    /// 9-slice, đặt Sliced là Unity kéo giãn phần giữa và bo góc bị méo thành hình thang.
    /// Đổi `localScale` theo `sprite.bounds` là cách duy nhất giữ đúng tỉ lệ hình.
    /// </summary>
    private static void DatKichThuoc(SpriteRenderer sr, float rong, float cao)
    {
        if (sr == null || sr.sprite == null) return;

        Vector2 b = sr.sprite.bounds.size;
        if (b.x <= 0.0001f || b.y <= 0.0001f) return;

        sr.transform.localScale = new Vector3(rong / b.x, cao / b.y, 1f);
    }

    /// <summary>
    /// Chưa ai điền `soO` thì suy từ collider, làm tròn LÊN theo ô lưới.
    /// Làm tròn lên chứ không phải xuống: thảm nhỏ hơn công trình trông như đặt lệch,
    /// còn thảm to hơn một chút thì vẫn đọc được là "vùng của cái này".
    /// </summary>
    private Vector2Int SuySoOTuCollider()
    {
        Bounds? gop = null;
        foreach (var c in GetComponentsInChildren<Collider2D>(true))
        {
            if (c == null || c.isTrigger) continue;
            gop = gop.HasValue ? Gop(gop.Value, c.bounds) : c.bounds;
        }

        if (!gop.HasValue)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null) continue;
                if (_goc != null && sr.transform.IsChildOf(_goc)) continue;
                gop = gop.HasValue ? Gop(gop.Value, sr.bounds) : sr.bounds;
            }
        }

        if (!gop.HasValue) return Vector2Int.one;

        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(gop.Value.size.x / PlacementManager.CELL)),
            Mathf.Max(1, Mathf.CeilToInt(gop.Value.size.y / PlacementManager.CELL)));
    }

    private static Bounds Gop(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    // ═════════════════════════════════════════════════════════════════════════
    //  NHỊP THỞ
    // ═════════════════════════════════════════════════════════════════════════

    private void BatNhip()
    {
        if (!nhipTho || _nhip != null || !isActiveAndEnabled) return;
        _nhip = StartCoroutine(CoNhip());
    }

    private void DungNhip()
    {
        if (_nhip != null) { StopCoroutine(_nhip); _nhip = null; }
    }

    // Kích thước GỐC do `DatKichThuoc` đặt. Bắt buộc phải nhớ lại: nhân hệ số vào
    // `localScale` hiện tại thì mỗi khung hình lại nhân thêm một lần, ngoặc góc phình
    // to vô hạn sau vài giây. Luôn tính từ mốc gốc.
    private Vector3[] _scaleGocNgoac;
    private float     _yGocChip;

    private IEnumerator CoNhip()
    {
        // Lệch pha theo vị trí công trình để 20 cái nhà trên map không thở cùng nhịp —
        // cùng nhịp thì cả màn hình đập như đèn nháy, rất khó chịu.
        float lech = Mathf.Repeat(transform.position.x * 0.013f + transform.position.y * 0.021f, 6.283f);

        while (true)
        {
            GhiNhoMocGoc();

            float k = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 2.1f + lech);

            if (_ngoac != null && _scaleGocNgoac != null)
            {
                for (int i = 0; i < _ngoac.Length; i++)
                {
                    if (_ngoac[i] == null) continue;
                    Vector3 g = _scaleGocNgoac[i];
                    _ngoac[i].transform.localScale = new Vector3(g.x * k, g.y * k, 1f);
                }
            }

            if (_chip != null)
            {
                _chip.transform.localPosition = new Vector3(
                    0f,
                    _yGocChip + Mathf.Sin(Time.unscaledTime * 2.6f + lech) * 4f,
                    0f);
            }

            yield return null;
        }
    }

    /// <summary>Chụp lại kích thước/vị trí gốc sau mỗi lần `CapNhatKichThuoc` chạy.</summary>
    private void GhiNhoMocGoc()
    {
        if (_scaleGocNgoac != null) return;
        if (_ngoac == null) return;

        _scaleGocNgoac = new Vector3[_ngoac.Length];
        for (int i = 0; i < _ngoac.Length; i++)
            _scaleGocNgoac[i] = _ngoac[i] != null ? _ngoac[i].transform.localScale : Vector3.one;

        if (_chip != null) _yGocChip = _chip.transform.localPosition.y;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2Int o = soO.x > 0 && soO.y > 0 ? soO : Vector2Int.one;
        float w = o.x * PlacementManager.CELL, h = o.y * PlacementManager.CELL;

        Gizmos.color = new Color(0.37f, 0.85f, 0.66f, 0.9f);
        Vector3 tam = transform.position + new Vector3(0f, h * 0.5f, 0f);
        Gizmos.DrawWireCube(tam, new Vector3(w, h, 0.1f));
    }
#endif
}
