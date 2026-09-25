using System.Collections.Generic;
using UnityEngine;

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
    }

    /// <summary>Toạ độ world của slot thứ <paramref name="slotIndex"/> (0 = anchor).</summary>
    public Vector3 GetSlotPosition(int slotIndex)
    {
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
