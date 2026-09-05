using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĐƯỜNG LINE BAO QUANH KHU NHÀ VILLAGE (yêu cầu Sếp: "hãy vẽ đường line bao quanh
/// các ngôi nhà village") — vừa TÍNH vòng, vừa VẼ vòng, vừa sinh waypoint trên vòng.
///
/// ── VÌ SAO PHẢI TỰ SINH ĐƯỜNG ────────────────────────────────────────────────
/// Project KHÔNG có đường đất nối bảng đơn → khu nhà dân. <c>Tilemap_IsoDirt</c>
/// (332 tile) chỉ chạy giữa map ↔ bến tàu. 5 ngôi nhà lại là công trình NGƯỜI CHƠI
/// TỰ MUA & TỰ ĐẶT ⇒ toạ độ ĐỘNG, không có trong scene file. Nên vòng đường phải
/// được tính lúc runtime từ vị trí nhà thật.
///
/// ── THUẬT TOÁN (đã verify bằng sandbox trên 5 toạ độ nhà thật-like) ──────────
/// 1. Quét mọi <see cref="HouseGrowthController"/> đang active → lấy transform.position.
/// 2. 0 nhà  ⇒ <see cref="HasRing"/> = false, <see cref="BuildPath"/> trả đường thẳng 2 điểm.
/// 3. 1-2 nhà ⇒ vòng = hình chữ nhật bao quanh + padding.
/// 4. ≥3 nhà ⇒ CONVEX HULL (Andrew's monotone chain, CCW) rồi NỞ RA padding theo
///    pháp tuyến từng cạnh (giao 2 đường đã dịch).
///
/// ⚠ HAI BẪY HÌNH HỌC ĐÃ TRẢ GIÁ TRONG SANDBOX MỚI TÌM RA:
///   • Góc HULL NHỌN ⇒ điểm mitre bay RẤT XA. Với 5 nhà thật-like, đỉnh hull
///     (-473,-2613) nở ra thành (1001,-3023) — cách nhà 1530 unit, vòng phình khổng lồ,
///     đường đi dài 6040 unit (14.4 giây/chiều, vượt ngưỡng). ⇒ CHẶN bằng
///     <see cref="MiterLimit"/>, quá hạn thì thay bằng CUNG TRÒN quanh đỉnh.
///   • BEVEL (nối 2 điểm) thì dây cung CẮT SÁT góc: nhà cách đường chỉ 44 unit
///     thay vì 260 ⇒ cô gái đi xuyên nhà. Nên phải dùng CUNG TRÒN bước ≤ 30°
///     (mỗi điểm cách đỉnh đúng padding, dây cung cách đỉnh ≥ padding·cos15° = 0.966·padding).
///     Sandbox đo lại: nhà gần nhất cách đường 251/260 unit. ✔
///
/// Sau khi sửa: đường đi 1948..4735 unit ⇒ 4.6-11.3 giây/chiều với walkSpeed 420. ✔
/// </summary>
public class VillageRoadRing : MonoBehaviour
{
    /// <summary>Tên GameObject chứa các mảnh đường (con của object này).</summary>
    private const string RoadRootName = "VillageRoad";

    /// <summary>Mitre dài quá <c>padding × MiterLimit</c> thì đổi sang cung tròn.</summary>
    private const float MiterLimit = 1.6f;

    /// <summary>Bước góc của cung tròn ở đỉnh nhọn (độ). 30° ⇒ dây cung ≥ 0.966·padding.</summary>
    private const float ArcStepDegrees = 30f;

    /// <summary>
    /// Waypoint sát nhau hơn khoảng này (unit) thì GỘP. Đoạn cực ngắn làm nhân vật
    /// giật hướng liên tục, và làm phép lệch làn đường về bị đẩy vượt qua đoạn kế bên
    /// (sandbox: khoảng cách an toàn tụt từ 41 xuống 27 unit).
    /// </summary>
    private const float MinWaypointGap = 80f;

    /// <summary>Sai số coi 2 nhà là cùng một điểm khi dựng hull (unit).</summary>
    private const float SamePointEpsilon = 0.5f;

    // ─── Static ─────────────────────────────────────────────────────────

    private static VillageRoadRing _instance;
    private static Sprite _quadSprite;    // 4×4 trắng, PPU 4 ⇒ bounds 1×1 unit
    private static Sprite _jointSprite;   // hình tròn khử răng cưa, bounds 1×1 unit

    /// <summary>Instance hiện có (có thể null nếu chưa ai gọi <see cref="EnsureInstance"/>).</summary>
    public static VillageRoadRing Instance => _instance;

    /// <summary>
    /// Tìm/tạo instance. <paramref name="cfg"/> null ⇒ trả null (feature flag tắt).
    /// Gọi nhiều lần vô hại.
    /// </summary>
    public static VillageRoadRing EnsureInstance(ShipperConfig cfg)
    {
        // [QA] §9: thiếu kiểm cfg.enabled ⇒ gọi với config đang TẮT vẫn sinh
        // GameObject "VillageRoadRing" trong scene. Nay return null như hợp đồng.
        if (cfg == null || !cfg.enabled) return null;

        if (_instance == null)
        {
            var go = new GameObject("VillageRoadRing");
            _instance = go.AddComponent<VillageRoadRing>();
        }

        _instance._cfg = cfg;
        return _instance;
    }

    // ─── Runtime ────────────────────────────────────────────────────────

    private ShipperConfig _cfg;

    private Vector2[] _ring = new Vector2[0];
    private Vector2   _center;
    private int       _houseCount;
    private bool      _visible = true;

    private Transform _roadRoot;

    /// <summary>Có vòng đường dùng được (≥ 3 đỉnh) hay không.</summary>
    public bool HasRing => _ring != null && _ring.Length >= 3;

    /// <summary>Số nhà đã quét được ở lần <see cref="Rebuild"/> gần nhất.</summary>
    public int HouseCount => _houseCount;

    /// <summary>Tâm vòng — <see cref="FlowerGirlShipper"/> cần để lệch làn RA NGOÀI nhất quán.</summary>
    public Vector3 RingCenter => new Vector3(_center.x, _center.y, 0f);

    /// <summary>Bản sao các đỉnh vòng (cho tool/debug). Không có vòng ⇒ mảng rỗng.</summary>
    public Vector3[] RingVertices
    {
        get
        {
            if (_ring == null) return new Vector3[0];
            var outp = new Vector3[_ring.Length];
            for (int i = 0; i < _ring.Length; i++) outp[i] = new Vector3(_ring[i].x, _ring[i].y, 0f);
            return outp;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ─── API chính ──────────────────────────────────────────────────────

    /// <summary>
    /// Quét lại nhà, tính lại vòng, vẽ lại đường. IDEMPOTENT (xoá đường cũ trước khi
    /// vẽ mới) nhưng KHÔNG RẺ — chỉ gọi khi SỐ NHÀ ĐỔI, tuyệt đối không gọi mỗi frame.
    /// </summary>
    public void Rebuild()
    {
        var houses = new List<Vector2>(8);

        HouseGrowthController[] found =
            Object.FindObjectsByType<HouseGrowthController>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            HouseGrowthController h = found[i];
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            Vector3 p = h.transform.position;
            houses.Add(new Vector2(p.x, p.y));
        }

        _houseCount = houses.Count;
        float pad = _cfg != null ? _cfg.SafeRingPadding : 260f;

        _ring   = BuildRing(houses, pad);
        _center = Centroid(_ring, houses);

        RedrawRoad();
    }

    /// <summary>
    /// Đường đi: <paramref name="from"/> → điểm gần nhất trên vòng → đi DỌC VÒNG theo
    /// CHIỀU NGẮN HƠN → điểm gần nhất với <paramref name="toHouseFront"/> → tới trước nhà.
    /// Trả <c>[điểm vào vòng, ...các đỉnh vòng đi qua..., điểm ra vòng, toHouseFront]</c>.
    /// Chưa có vòng (0 nhà / hull suy biến) ⇒ fallback đường thẳng 2 điểm, KHÔNG lỗi.
    /// </summary>
    public Vector3[] BuildPath(Vector3 from, Vector3 toHouseFront)
    {
        if (!HasRing)
            return new[] { from, toHouseFront };

        int   iA; float tA; Vector2 qA;
        int   iB; float tB; Vector2 qB;
        var f = new Vector2(from.x, from.y);
        var t = new Vector2(toHouseFront.x, toHouseFront.y);

        ClosestOnRing(f, out iA, out tA, out qA);
        ClosestOnRing(t, out iB, out tB, out qB);

        List<Vector2> forward = RingWalk(iA, tA, qA, iB, tB, qB, true);
        List<Vector2> back    = RingWalk(iA, tA, qA, iB, tB, qB, false);

        List<Vector2> chosen = PolylineLength(forward) <= PolylineLength(back) ? forward : back;
        chosen.Add(t);

        Simplify(chosen);

        var outp = new Vector3[chosen.Count];
        for (int i = 0; i < chosen.Count; i++) outp[i] = new Vector3(chosen[i].x, chosen[i].y, 0f);
        return outp;
    }

    /// <summary>
    /// Điểm ĐỨNG TRƯỚC NHÀ. Nhà có <c>SpriteRenderer</c> thì lấy <c>bounds.min.y</c>
    /// (chân nhà thật, đúng cả khi nhà đổi stage sprite) rồi cộng
    /// <c>houseFrontOffsetY</c>; không có renderer thì lấy từ transform.position.
    /// </summary>
    public Vector3 FrontOfHouse(Transform house)
    {
        if (house == null) return Vector3.zero;

        float dy = _cfg != null ? _cfg.houseFrontOffsetY : -120f;
        Vector3 p = house.position;

        Bounds b;
        if (TryGetVisualBounds(house, out b))
            return new Vector3(b.center.x, b.min.y + dy, p.z);

        return new Vector3(p.x, p.y + dy, p.z);
    }

    /// <summary>Ẩn/hiện các mảnh đường đã vẽ (logic đi đường KHÔNG bị ảnh hưởng).</summary>
    public void SetVisible(bool visible)
    {
        _visible = visible;
        if (_roadRoot != null) _roadRoot.gameObject.SetActive(visible && HasRing);
    }

    /// <summary>
    /// Bounds hình ảnh của một công trình: hợp mọi <c>SpriteRenderer</c> con đang bật.
    /// Trả false nếu không có renderer nào (bên gọi tự dùng transform.position).
    /// </summary>
    public static bool TryGetVisualBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
        if (root == null) return false;

        SpriteRenderer[] rs = root.GetComponentsInChildren<SpriteRenderer>(false);
        bool co = false;
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null || rs[i].sprite == null) continue;
            if (!co) { bounds = rs[i].bounds; co = true; }
            else       bounds.Encapsulate(rs[i].bounds);
        }
        return co;
    }

    // ─── Tính vòng ──────────────────────────────────────────────────────

    private static Vector2[] BuildRing(List<Vector2> houses, float pad)
    {
        if (houses == null || houses.Count == 0) return new Vector2[0];
        if (houses.Count <= 2) return BoundingBoxRing(houses, pad);

        List<Vector2> hull = ConvexHull(houses);
        if (hull.Count < 3) return BoundingBoxRing(houses, pad);   // nhà thẳng hàng / trùng nhau

        return OffsetRing(hull, pad);
    }

    /// <summary>Hình chữ nhật bao quanh + padding, trả theo chiều NGƯỢC KIM ĐỒNG HỒ (CCW).</summary>
    private static Vector2[] BoundingBoxRing(List<Vector2> pts, float pad)
    {
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        for (int i = 0; i < pts.Count; i++)
        {
            if (pts[i].x < x0) x0 = pts[i].x;
            if (pts[i].x > x1) x1 = pts[i].x;
            if (pts[i].y < y0) y0 = pts[i].y;
            if (pts[i].y > y1) y1 = pts[i].y;
        }
        x0 -= pad; y0 -= pad; x1 += pad; y1 += pad;

        return new[]
        {
            new Vector2(x0, y0), new Vector2(x1, y0),
            new Vector2(x1, y1), new Vector2(x0, y1),
        };
    }

    /// <summary>
    /// ANDREW'S MONOTONE CHAIN → bao lồi theo chiều NGƯỢC KIM ĐỒNG HỒ, đã bỏ
    /// điểm trùng và điểm thẳng hàng.
    /// </summary>
    private static List<Vector2> ConvexHull(List<Vector2> input)
    {
        var pts = new List<Vector2>(input);
        pts.Sort(CompareXThenY);

        // bỏ điểm trùng (2 nhà đặt gần như cùng chỗ)
        var uniq = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            if (uniq.Count > 0 && (uniq[uniq.Count - 1] - pts[i]).sqrMagnitude <
                                  SamePointEpsilon * SamePointEpsilon) continue;
            uniq.Add(pts[i]);
        }
        if (uniq.Count < 3) return uniq;

        List<Vector2> lower = HalfHull(uniq, false);
        List<Vector2> upper = HalfHull(uniq, true);

        var hull = new List<Vector2>(lower.Count + upper.Count);
        for (int i = 0; i < lower.Count - 1; i++) hull.Add(lower[i]);
        for (int i = 0; i < upper.Count - 1; i++) hull.Add(upper[i]);
        return hull;
    }

    private static List<Vector2> HalfHull(List<Vector2> sorted, bool reverse)
    {
        var h = new List<Vector2>(sorted.Count);
        int n = sorted.Count;
        for (int k = 0; k < n; k++)
        {
            Vector2 q = reverse ? sorted[n - 1 - k] : sorted[k];
            while (h.Count >= 2 && Cross(h[h.Count - 2], h[h.Count - 1], q) <= 0f)
                h.RemoveAt(h.Count - 1);
            h.Add(q);
        }
        return h;
    }

    private static int CompareXThenY(Vector2 a, Vector2 b)
    {
        int c = a.x.CompareTo(b.x);
        return c != 0 ? c : a.y.CompareTo(b.y);
    }

    private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    /// <summary>
    /// NỞ đa giác lồi (CCW) ra ngoài <paramref name="pad"/>.
    /// Góc thoải → giao 2 đường đã dịch (mitre, 1 điểm, vòng bó sát).
    /// Góc nhọn  → CUNG TRÒN quanh đỉnh (xem ghi chú đầu file: bevel cắt sát góc, không dùng).
    /// </summary>
    private static Vector2[] OffsetRing(List<Vector2> hull, float pad)
    {
        int n = hull.Count;
        var basePt = new Vector2[n];
        var dir    = new Vector2[n];
        var nrm    = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % n];
            Vector2 d = (b - a).normalized;
            Vector2 outward = new Vector2(d.y, -d.x);   // CCW ⇒ pháp tuyến hướng RA NGOÀI
            dir[i]    = d;
            nrm[i]    = outward;
            basePt[i] = a + outward * pad;
        }

        var res = new List<Vector2>(n * 3);
        float arcStep = ArcStepDegrees * Mathf.Deg2Rad;

        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            Vector2 v = hull[i];

            float den = dir[prev].x * dir[i].y - dir[prev].y * dir[i].x;
            bool dungCung = Mathf.Abs(den) < 1e-7f;

            if (!dungCung)
            {
                Vector2 diff = basePt[i] - basePt[prev];
                float tt = (diff.x * dir[i].y - diff.y * dir[i].x) / den;
                Vector2 q = basePt[prev] + dir[prev] * tt;

                if ((q - v).magnitude > pad * MiterLimit) dungCung = true;
                else                                     res.Add(q);
            }

            if (!dungCung) continue;

            float a0 = Mathf.Atan2(nrm[prev].y, nrm[prev].x);
            float a1 = Mathf.Atan2(nrm[i].y,    nrm[i].x);
            float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / arcStep));
            for (int k = 0; k <= steps; k++)
            {
                float ang = a0 + sweep * (k / (float)steps);
                res.Add(v + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * pad);
            }
        }

        return res.ToArray();
    }

    private static Vector2 Centroid(Vector2[] ring, List<Vector2> fallback)
    {
        if (ring != null && ring.Length > 0)
        {
            Vector2 s = Vector2.zero;
            for (int i = 0; i < ring.Length; i++) s += ring[i];
            return s / ring.Length;
        }
        if (fallback != null && fallback.Count > 0)
        {
            Vector2 s = Vector2.zero;
            for (int i = 0; i < fallback.Count; i++) s += fallback[i];
            return s / fallback.Count;
        }
        return Vector2.zero;
    }

    // ─── Đi dọc vòng ────────────────────────────────────────────────────

    /// <summary>Điểm gần nhất trên CHU VI vòng: trả chỉ số đoạn, tham số t ∈ [0,1] và điểm đó.</summary>
    private void ClosestOnRing(Vector2 p, out int segIndex, out float t, out Vector2 point)
    {
        segIndex = 0; t = 0f; point = _ring[0];
        float best = float.MaxValue;

        int n = _ring.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = _ring[i];
            Vector2 b = _ring[(i + 1) % n];
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            float tt = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 q = a + ab * tt;
            float d = (p - q).sqrMagnitude;
            if (d < best) { best = d; segIndex = i; t = tt; point = q; }
        }
    }

    /// <summary>Chuỗi điểm từ qA tới qB đi theo MỘT chiều dọc vòng (chưa gồm đích cuối cùng).</summary>
    private List<Vector2> RingWalk(int iA, float tA, Vector2 qA,
                                   int iB, float tB, Vector2 qB, bool forward)
    {
        int n = _ring.Length;
        var pts = new List<Vector2>(n + 3) { qA };

        if (forward)
        {
            if (iA == iB && tB >= tA) { pts.Add(qB); return pts; }
            int i = iA;
            for (int guard = 0; guard <= n; guard++)
            {
                i = (i + 1) % n;
                pts.Add(_ring[i]);
                if (i == iB) break;
            }
            pts.Add(qB);
        }
        else
        {
            if (iA == iB && tB <= tA) { pts.Add(qB); return pts; }
            int i = iA;
            int stop = (iB + 1) % n;
            for (int guard = 0; guard <= n; guard++)
            {
                pts.Add(_ring[i]);
                if (i == stop) break;
                i = (i - 1 + n) % n;
            }
            pts.Add(qB);
        }

        return pts;
    }

    private static float PolylineLength(List<Vector2> pts)
    {
        float s = 0f;
        for (int i = 0; i + 1 < pts.Count; i++) s += (pts[i + 1] - pts[i]).magnitude;
        return s;
    }

    /// <summary>
    /// GỘP waypoint sát nhau (&lt; <see cref="MinWaypointGap"/>). Luôn giữ điểm ĐẦU
    /// và điểm CUỐI. Xem ghi chú đầu file về vì sao đoạn cực ngắn là nguy hiểm.
    /// </summary>
    private static void Simplify(List<Vector2> pts)
    {
        if (pts.Count <= 2) return;

        var keep = new List<Vector2>(pts.Count) { pts[0] };
        for (int i = 1; i < pts.Count - 1; i++)
            if ((pts[i] - keep[keep.Count - 1]).magnitude >= MinWaypointGap) keep.Add(pts[i]);

        Vector2 last = pts[pts.Count - 1];
        if (keep.Count > 1 && (last - keep[keep.Count - 1]).magnitude < MinWaypointGap)
            keep[keep.Count - 1] = last;
        else
            keep.Add(last);

        pts.Clear();
        pts.AddRange(keep);
    }

    // ─── Vẽ đường ───────────────────────────────────────────────────────

    private void RedrawRoad()
    {
        // IDEMPOTENT: xoá sạch đường cũ trước khi vẽ mới
        if (_roadRoot != null)
        {
            Destroy(_roadRoot.gameObject);
            _roadRoot = null;
        }

        if (_cfg == null || !_cfg.drawRoadRing || !HasRing) return;

        var rootGo = new GameObject(RoadRootName);
        rootGo.transform.SetParent(transform, false);
        _roadRoot = rootGo.transform;

        string layer = TouristSortingLayers.Resolve(_cfg.RoadLayerPriority);
        int    order = _cfg.roadSortingOrder;
        float  width = _cfg.SafeRoadWidth;
        Sprite body  = _cfg.roadSprite != null ? _cfg.roadSprite : QuadSprite();
        Sprite joint = JointSprite();

        int n = _ring.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = _ring[i];
            Vector2 b = _ring[(i + 1) % n];
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) continue;

            MakePiece($"RoadSeg_{i:00}", body, layer, order,
                      (a + b) * 0.5f,
                      Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg,
                      len, width);

            // mối nối ở đỉnh: bịt kẽ hở giữa 2 đoạn xoay khác góc
            MakePiece($"RoadJoint_{i:00}", joint, layer, order - 1,
                      b, 0f, width, width);
        }

        rootGo.SetActive(_visible);
    }

    private void MakePiece(string pieceName, Sprite sprite, string layer, int order,
                           Vector2 pos, float angleDeg, float lengthX, float thicknessY)
    {
        var go = new GameObject(pieceName);
        go.transform.SetParent(_roadRoot, false);
        go.transform.position      = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.color            = _cfg.roadColor;
        sr.sortingLayerName = layer;
        sr.sortingOrder     = order;

        // sprite dựng bằng code có bounds 1×1 unit ⇒ scale = kích thước mong muốn.
        // roadSprite của Sếp thì chia theo bounds thật để không bị bóp méo.
        Vector2 unit = Vector2.one;
        if (sprite != null && sprite.bounds.size.x > 0.0001f && sprite.bounds.size.y > 0.0001f)
            unit = new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);

        go.transform.localScale = new Vector3(lengthX / unit.x, thicknessY / unit.y, 1f);
    }

    // ─── Sprite dựng bằng code (cache static — CHỈ tạo 1 lần cả phiên) ──

    private static Sprite QuadSprite()
    {
        if (_quadSprite != null) return _quadSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            name       = "ShipperRoadQuad",
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply(false, false);

        // PPU = 4 ⇒ bounds đúng 1×1 world unit, scale sau này là kích thước thật
        _quadSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        _quadSprite.name = "ShipperRoadQuad";
        return _quadSprite;
    }

    private static Sprite JointSprite()
    {
        if (_jointSprite != null) return _jointSprite;

        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            name       = "ShipperRoadJoint",
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        var px = new Color32[S * S];
        float r = S * 0.5f - 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = x - (S - 1) * 0.5f;
                float dy = y - (S - 1) * 0.5f;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01((r - d) / 1.5f);      // khử răng cưa 1.5px
                px[y * S + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);

        _jointSprite = Sprite.Create(tex, new Rect(0f, 0f, S, S), new Vector2(0.5f, 0.5f), S);
        _jointSprite.name = "ShipperRoadJoint";
        return _jointSprite;
    }
}
