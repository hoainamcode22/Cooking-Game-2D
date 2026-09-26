using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// HÀNG CHỜ khách du lịch trước nhà hàng cooking (GDD BOAT-002 §3.3).
///
/// Đặt trên object "QueueAnchor" (tool TouristVisitorSetupTool sinh). Vị trí anchor
/// = chỗ khách ĐỨNG ĐẦU hàng đứng; các slot sau nối dài theo <see cref="queueDirection"/>
/// cách nhau <c>queueSpacing</c> (đọc từ TouristBoatConfig, manager bơm vào qua
/// <see cref="Configure"/> — KHÔNG hardcode số ở đây, luật "mọi số qua Config").
///
/// Luật hàng chờ:
///   • Khách mới vào slot TRỐNG NHỎ NHẤT (danh sách compact nên = cuối hàng).
///   • Khách rời đi → cả hàng DỒN LÊN 1 slot; mọi khách phía sau được báo slot mới
///     qua <see cref="TouristAgent.OnQueueSlotChanged"/> để bước lên.
///   • [Sếp chốt 2026-08-29] Hàng chờ KHÔNG còn quyết định việc mở bubble: mọi khách
///     đều có bubble, nở lần lượt do TouristVisitorManager điều phối. Cờ isFront chỉ
///     còn mang nghĩa vị trí (đứng đầu hàng), dùng để soi trạng thái lúc debug.
///
/// MỘT hàng chung cho cả 3 bến (1 nhà hàng → 1 hàng; GDD §3.3 viết anchor số ít).
/// Thuần dữ liệu + toạ độ — không tự di chuyển ai, agent tự đi tới slot của mình.
/// </summary>
public class TouristQueue : MonoBehaviour
{
    [Header("Hình dạng hàng")]
    [Tooltip("Hướng NỐI DÀI hàng tính từ anchor (khách sau đứng về phía này). " +
             "Mặc định chéo xuống-phải cho hợp góc nhìn isometric — Sếp chỉnh trong Inspector.")]
    [SerializeField] private Vector2 queueDirection = new Vector2(0.9f, -0.45f);

    // Khoảng cách giữa 2 khách (unit world) — manager bơm từ config.queueSpacing.
    private float _spacing = 60f;

    // Danh sách COMPACT: index trong list = slot index, phần tử 0 = đầu hàng.
    private readonly List<TouristAgent> _agents = new List<TouristAgent>();

    /// <summary>Khách đang đứng đầu hàng (null nếu hàng rỗng).</summary>
    public TouristAgent Front => _agents.Count > 0 ? _agents[0] : null;

    /// <summary>Số khách đang trong hàng.</summary>
    public int Count => _agents.Count;

    /// <summary>
    /// [BOAT-TUT 2026-09-10] Danh sach khach trong hang (CHI DOC) — cinematic tutorial
    /// doc de biet khi nao moi nguoi da xep hang xong. Khong cap phat moi, khong sua duoc.
    /// </summary>
    public IReadOnlyList<TouristAgent> Agents => _agents;

    /// <summary>Manager gọi 1 lần lúc boot để bơm spacing từ TouristBoatConfig.</summary>
    public void Configure(float spacing)
    {
        if (spacing > 0.01f) _spacing = spacing;
    }

    /// <summary>
    /// Xếp khách vào slot trống nhỏ nhất (cuối hàng vì danh sách compact).
    /// Trả về slot index; -1 nếu agent null / đã ở trong hàng.
    /// </summary>
    public int Enqueue(TouristAgent agent)
    {
        if (agent == null || _agents.Contains(agent)) return -1;
        _agents.Add(agent);
        return _agents.Count - 1;
    }

    /// <summary>
    /// Khách rời hàng (được phục vụ / hết kiên nhẫn): gỡ khỏi danh sách rồi DỒN HÀNG —
    /// báo slot mới cho từng khách phía sau để họ bước lên. Agent nào đang ĐI BỘ trên
    /// đường đất sẽ chỉ ghi nhận slot, không đổi hướng giữa chừng (QA M-3).
    /// </summary>
    public void Remove(TouristAgent agent)
    {
        int idx = _agents.IndexOf(agent);
        if (idx < 0) return;

        _agents.RemoveAt(idx);

        // Chỉ những khách ĐỨNG SAU vị trí vừa trống mới đổi slot.
        for (int i = idx; i < _agents.Count; i++)
        {
            TouristAgent a = _agents[i];
            if (a == null) continue; // phòng thủ: agent bị destroy giữa chừng
            a.OnQueueSlotChanged(i, GetSlotPosition(i), isFront: i == 0);
        }
    }

    /// <summary>Khách này có đang đứng đầu hàng không.</summary>
    public bool IsFront(TouristAgent agent)
    {
        return agent != null && _agents.Count > 0 && _agents[0] == agent;
    }

    // [2026-09-25] HANG CHO XEP DOC DUONG DAT: manager dua duong di bo (ket thuc o anchor) vao day,
    // slot i nam lui lai i * spacing THEO duong -> hang khach nam tren duong dat, khong tran ra hoa.
    private Vector3[] _duong;
    public void DatDuong(Vector3[] duongDenAnchor)
    {
        if (duongDenAnchor == null || duongDenAnchor.Length < 2) return;
        var ds = new List<Vector3>(duongDenAnchor.Length + 1) { transform.position };
        for (int i = duongDenAnchor.Length - 1; i >= 0; i--)
            if ((duongDenAnchor[i] - ds[ds.Count - 1]).sqrMagnitude > 4f) ds.Add(duongDenAnchor[i]);
        if (ds.Count >= 2) _duong = ds.ToArray();
        // [2026-09-25 v2] Hang cho THANG 1 hang nhu truoc: huong = huong duong dat ngay truoc anchor
        // (lui lai ~1.5 slot theo duong), khong be cong theo tung khuc duong nua.
        _huongThang = Vector3.zero;
        if (_duong != null)
        {
            float can = _spacing * 1.5f;
            Vector3 q = _duong[_duong.Length - 1];
            for (int i = 1; i < _duong.Length; i++)
            {
                float d = Vector3.Distance(_duong[i - 1], _duong[i]);
                if (can <= d) { q = Vector3.Lerp(_duong[i - 1], _duong[i], d > 0.001f ? can / d : 0f); break; }
                can -= d;
            }
            Vector3 h = q - transform.position; h.z = 0f;
            if (h.sqrMagnitude > 1f) _huongThang = h.normalized;
            // [2026-09-25 v3] Sep: dung HANG NGANG (canh nhau theo chieu ngang man hinh) -> khong chong len nhau, de cham.
            if (xepTheoDuongDat && _huongThang.sqrMagnitude > 0.5f)
            {
                // [2026-09-25 v4] Sep: "xep hang ngang theo duong dirt" -> 1 hang THANG doc theo truc iso cua duong
                // (duong dat chay cheo 2:1), chon truc iso gan huong duong nhat. Cach nhau >= cachNgangToiThieu.
                float sx = _huongThang.x < 0f ? -1f : 1f, sy = _huongThang.y < 0f ? -1f : 1f;
                // [2026-09-26 v5] Sep: huong tiep can (duong khach di toi) cat NGANG con duong -> hang dung cheo qua duong.
                // Nay do thang tren Tilemap_IsoDirt: thu 4 huong truc iso, chon nhanh duong dat DAI nhat tai anchor.
                // Khong do duoc -> lay truc iso VUONG GOC voi huong tiep can (doc theo con duong).
                Vector3 theoDuong;
                if (HuongTheoDuongDat(new Vector3(sx * 0.894f, sy * 0.447f, 0f), out theoDuong)) _huongThang = theoDuong;
                else _huongThang = new Vector3(sx * 0.894f, -sy * 0.447f, 0f);
            }
            else if (xepHangNgang && _huongThang.sqrMagnitude > 0.5f)
                _huongThang = new Vector3(_huongThang.x < 0f ? -1f : 1f, 0f, 0f);
        }
    }
    private Vector3 _huongThang;

    private static Tilemap _dirt;

    /// <summary>Huong (truc iso) doc theo con duong dat tai anchor: nhanh co nhieu o dat LIEN TIEP nhat.</summary>
    private bool HuongTheoDuongDat(Vector3 hTiepCan, out Vector3 kq)
    {
        kq = Vector3.zero;
        if (_dirt == null)
        {
            foreach (var t in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (t != null && t.name == "Tilemap_IsoDirt") { _dirt = t; break; }
        }
        if (_dirt == null) return false;
        Vector3 p = transform.position;
        Vector3Int c = _dirt.WorldToCell(p);
        Vector3 o = _dirt.CellToWorld(c);
        Vector3 a = _dirt.CellToWorld(c + Vector3Int.right) - o, b = _dirt.CellToWorld(c + Vector3Int.up) - o;
        a.z = 0f; b.z = 0f;
        if (a.sqrMagnitude < 1e-4f || b.sqrMagnitude < 1e-4f) return false;
        a.Normalize(); b.Normalize();
        Vector3[] ds = { a, -a, b, -b };
        float buoc = Mathf.Max(_spacing, cachNgangToiThieu * 1.15f) * 0.5f;
        Vector3 tc = hTiepCan.normalized;
        int tot = -1; float diemTot = -1f, soTot = 0;
        for (int i = 0; i < ds.Length; i++)
        {
            int so = 0;
            for (int k = 1; k <= 10; k++)
            {
                if (_dirt.HasTile(_dirt.WorldToCell(p + ds[i] * buoc * k))) so++;
                else if (k > 1) break;
            }
            float diem = so + (1f - Mathf.Abs(Vector3.Dot(ds[i], tc))) * 0.5f;   // hoa: uu tien vuong goc huong tiep can
            if (diem > diemTot) { diemTot = diem; tot = i; soTot = so; }
        }
        if (tot < 0 || soTot < 2) return false;
        kq = ds[tot];
        return true;
    }
    [Tooltip("[2026-09-25] BAT: khach dung canh nhau theo HANG NGANG man hinh (de cham). TAT: xep doc theo duong dat.")]
    [SerializeField] private bool xepHangNgang = true;
    [Tooltip("[2026-09-25 v4] BAT (uu tien): 1 hang thang doc theo duong dat (truc iso cheo). Tat = dung xepHangNgang.")]
    [SerializeField] private bool xepTheoDuongDat = true;
    [Tooltip("Khoang cach toi thieu giua 2 khach khi xep hang ngang (world).")]
    [SerializeField] private float cachNgangToiThieu = 110f;

    /// <summary>Toạ độ world của slot thứ <paramref name="slotIndex"/> (0 = anchor).</summary>
    public Vector3 GetSlotPosition(int slotIndex)
    {
        if (_huongThang.sqrMagnitude > 0.5f)
            return transform.position + _huongThang * (((xepHangNgang || xepTheoDuongDat) ? Mathf.Max(_spacing, cachNgangToiThieu * 1.15f) : _spacing) * Mathf.Max(0, slotIndex));
        if (_duong != null)
        {
            float can = _spacing * Mathf.Max(0, slotIndex);
            for (int i = 1; i < _duong.Length; i++)
            {
                float d = Vector3.Distance(_duong[i - 1], _duong[i]);
                if (can <= d) return Vector3.Lerp(_duong[i - 1], _duong[i], d > 0.001f ? can / d : 0f);
                can -= d;
            }
            Vector3 cuoi = _duong[_duong.Length - 1], huong = (cuoi - _duong[_duong.Length - 2]).normalized;
            return cuoi + huong * can;
        }
        Vector3 dir = queueDirection.sqrMagnitude > 0.0001f
            ? (Vector3)queueDirection.normalized
            : Vector3.right;
        return transform.position + dir * (_spacing * Mathf.Max(0, slotIndex));
    }

    /// <summary>Dọn sạch hàng (đổi scene / reset) — không destroy agent, chỉ quên tham chiếu.</summary>
    public void Clear()
    {
        _agents.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ 6 slot đầu để Sếp canh vị trí hàng trong Scene view (6 = visitorsMax mặc định).
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 6; i++)
            Gizmos.DrawWireSphere(GetSlotPosition(i), 12f);
    }
}
