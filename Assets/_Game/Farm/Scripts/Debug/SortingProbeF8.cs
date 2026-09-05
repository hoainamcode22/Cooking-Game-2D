#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ★ CÔNG CỤ CHẨN ĐOÁN 2026-09-03 — "khách du lịch đè lên tàu thuỷ"
///
/// BỐI CẢNH: Sếp báo mắt thấy khách vẽ ĐÈ LÊN tàu. Đội đã đọc code 2 vòng và kết luận
/// VỀ LÝ THUYẾT là không thể: <c>TouristBoatController.Start()</c> (dòng ~118-119) ép cứng
/// thân tàu vào <c>sortingLayerName = "ObjectsFront", sortingOrder = 200</c>, còn khách
/// (<c>TouristAgent</c>) chạy ở layer <c>"Objects"</c> — Unity so LAYER trước, "ObjectsFront"
/// luôn có value lớn hơn "Objects" nên tàu phải luôn thắng. Nhưng Sếp thấy tận mắt ⇒ có gì đó
/// code-reading không thấy được. Nghi phạm hàng đầu KHÔNG nằm trong 2 script trên: một
/// <see cref="SortingGroup"/> (Unity 2D) khi bọc quanh một GameObject sẽ làm MỌI Renderer con
/// bên trong nó, khi so sánh với vật thể NGOÀI group, dùng sortingLayer/sortingOrder của CHÍNH
/// GROUP đó — bất kể renderer con tự đặt gì. Nếu có một SortingGroup vô tình nằm ở đâu đó phía
/// trên tàu (ví dụ trên node "Dock_XX" cha) mà chưa được set đúng "ObjectsFront"/200, thì dòng
/// code ép cứng ở TouristBoatController.Start() BỊ VÔ HIỆU HOÁ hoàn toàn cho việc so sánh
/// xuyên-object — đúng kiểu bug "lý thuyết không thể nhưng thực tế thấy được".
///
/// Công cụ này KHÔNG đoán tiếp — nó ĐO giá trị sortingLayer/sortingOrder THẬT đang chạy trong
/// Play Mode (kể cả hiệu lực của SortingGroup cha, tìm bằng cách leo ngược hierarchy), rồi tự
/// PHÁN XỬ theo đúng luật Unity xem ai vẽ trên ai, để có bằng chứng đưa Lead thay vì tiếp tục
/// tranh luận trên giấy.
///
/// CÁCH DÙNG:
///   1) Bấm Play. Tự động gắn vào scene (không cần kéo component tay).
///   2) Diễn lại tình huống Sếp thấy: cho khách đi lại gần một con tàu đang đậu/chạy.
///   3) Bấm phím <b>F8</b> — Console in một báo cáo gộp (không tự lặp lại) gồm 4 phần:
///        A) Mọi tàu (TouristBoatController) + toàn bộ Renderer con của từng tàu.
///        B) Mọi khách (TouristAgent) + SortingGroup + toàn bộ Renderer con của từng khách.
///        C) PHÁN XỬ: mọi cặp (khách, tàu) đang gần nhau (&lt; 15 đơn vị world) — ai vẽ trên
///           ai theo đúng luật Unity (so sortingLayer TRƯỚC bằng SortingLayer.GetLayerValueFromName,
///           bằng nhau mới so sortingOrder). Dòng nào có "❌ SAI" nghĩa là KHÁCH đang vẽ đè lên
///           TÀU tại đúng thời điểm bấm F8 — đó chính là bằng chứng cần tìm.
///        D) Bảng đối chiếu toàn bộ Sorting Layer thật của project (tên · id · value).
///
/// ĐỌC KẾT QUẢ: mỗi dòng phán xử ở Phần C có ghi rõ giá trị "hiệu lực" (effective) đã dùng —
/// nếu dòng đó ghi nguồn là "SortingGroup 'X'" thay vì "Renderer riêng", tức là component X
/// (không phải TouristBoatController hay TouristAgent) mới là thứ ĐANG THỰC SỰ quyết định thứ tự
/// vẽ — đây là nơi cần soi tiếp nếu Phần C báo SAI.
///
/// AN TOÀN: chỉ Debug.Log, KHÔNG sửa/tắt bất cứ gì trong scene. Toàn bộ logic bọc try/catch —
/// lỗi (nếu có) chỉ in cảnh báo, KHÔNG bao giờ ném exception làm hỏng Play Mode. Bọc trong
/// UNITY_EDITOR/DEVELOPMENT_BUILD nên không lọt vào bản release. Xoá file này là xong, không hệ
/// nào phụ thuộc.
/// </summary>
[DisallowMultipleComponent]
public class SortingProbeF8 : MonoBehaviour
{
    private const int MaxDongMoiPhan = 40;
    private const float KhoangCachXet = 15f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        try
        {
            if (UnityEngine.Object.FindAnyObjectByType<SortingProbeF8>() != null) return;

            GameObject go = new GameObject("~SortingProbeF8");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<SortingProbeF8>();
            UnityEngine.Object.DontDestroyOnLoad(go);

            Debug.Log("[SortingProbe] Sẵn sàng — bấm F8 trong Play Mode để chụp báo cáo " +
                      "sortingLayer/sortingOrder THẬT của mọi khách + mọi tàu, và để công cụ tự " +
                      "phán xử ai đang vẽ đè lên ai.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SortingProbe] Không khởi tạo được (bỏ qua, không ảnh hưởng game): " + ex.Message);
        }
    }

    private void Update()
    {
        try
        {
            if (DaBamF8()) InBaoCao();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SortingProbe] Lỗi khi tạo báo cáo (đã chặn lại, Play Mode vẫn an toàn): " + ex);
        }
    }

    private static bool DaBamF8()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) return Keyboard.current.f8Key.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(KeyCode.F8);
    }

    // ─── Giá trị sorting "hiệu lực" (effective) ─────────────────────────────
    // Nếu một GameObject (hoặc bất kỳ cha nào của nó) có SortingGroup đang bật, thì khi so
    // sánh với vật thể NGOÀI group đó, Unity dùng sortingLayer/sortingOrder của GROUP NGOÀI
    // CÙNG tìm được trên đường lên gốc — KHÔNG dùng giá trị riêng của Renderer con nữa. Đây là
    // nơi thường bị bỏ sót khi chỉ đọc code của TouristBoatController/TouristAgent.
    private struct HieuLucSorting
    {
        public string layerName;
        public int order;
        public string nguon;
    }

    private static HieuLucSorting LayHieuLuc(Transform batDauTu, string layerRieng, int orderRieng, string tenRieng)
    {
        SortingGroup ngoaiCung = null;
        Transform t = batDauTu;
        int guard = 0;
        while (t != null && guard++ < 64)
        {
            SortingGroup sg = t.GetComponent<SortingGroup>();
            if (sg != null && sg.enabled) ngoaiCung = sg; // đi tới đâu ghi đè tới đó ⇒ cuối vòng là group NGOÀI CÙNG
            t = t.parent;
        }

        if (ngoaiCung != null)
        {
            return new HieuLucSorting
            {
                layerName = ngoaiCung.sortingLayerName,
                order = ngoaiCung.sortingOrder,
                nguon = "SortingGroup '" + DuongDan(ngoaiCung.gameObject) + "'"
            };
        }

        return new HieuLucSorting
        {
            layerName = layerRieng,
            order = orderRieng,
            nguon = tenRieng
        };
    }

    private static int GiaTriLayer(string ten)
    {
        try
        {
            return SortingLayer.GetLayerValueFromName(string.IsNullOrEmpty(ten) ? "Default" : ten);
        }
        catch
        {
            return 0;
        }
    }

    // ─── Báo cáo chính ───────────────────────────────────────────────────
    private void InBaoCao()
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine();
        sb.AppendLine("══════════════════ [SortingProbe] F8 — BÁO CÁO SORTING (khách vs tàu) ══════════════════");

        TouristBoatController[] tau = SafeFindAll<TouristBoatController>();
        TouristAgent[] khach = SafeFindAll<TouristAgent>();

        InPhanA_Tau(sb, tau);
        InPhanB_Khach(sb, khach);
        InPhanC_PhanXu(sb, khach, tau);
        InPhanD_BangLayer(sb);

        sb.AppendLine("══════════════════════════════════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    private static T[] SafeFindAll<T>() where T : UnityEngine.Object
    {
        try
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }
        catch
        {
            return Array.Empty<T>();
        }
    }

    private void InPhanA_Tau(StringBuilder sb, TouristBoatController[] tau)
    {
        sb.AppendLine();
        sb.AppendLine("── PHẦN A — MỌI TÀU (TouristBoatController) ─────────────────────────────────");
        if (tau == null || tau.Length == 0)
        {
            sb.AppendLine("   (không tìm thấy TouristBoatController nào trong scene)");
            return;
        }

        int dong = 0, boQua = 0;
        for (int i = 0; i < tau.Length; i++)
        {
            TouristBoatController b = tau[i];
            if (b == null) continue;

            sb.AppendLine($"  Tàu #{i}: {DuongDan(b.gameObject)}  ·  vị trí world = {Fmt(b.transform.position)}");

            Renderer[] rends = b.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0)
            {
                sb.AppendLine("     (tàu này không có Renderer con nào)");
                continue;
            }

            for (int j = 0; j < rends.Length; j++)
            {
                Renderer r = rends[j];
                if (r == null) continue;
                if (dong >= MaxDongMoiPhan) { boQua++; continue; }
                dong++;
                sb.AppendLine("     " + DongRenderer(r));
            }
        }
        if (boQua > 0) sb.AppendLine($"   …còn {boQua} dòng renderer nữa (đã ẩn bớt).");
    }

    private void InPhanB_Khach(StringBuilder sb, TouristAgent[] khach)
    {
        sb.AppendLine();
        sb.AppendLine("── PHẦN B — MỌI KHÁCH (TouristAgent) ────────────────────────────────────────");
        if (khach == null || khach.Length == 0)
        {
            sb.AppendLine("   (không tìm thấy TouristAgent nào trong scene)");
            return;
        }

        int dong = 0, boQua = 0;
        for (int i = 0; i < khach.Length; i++)
        {
            TouristAgent k = khach[i];
            if (k == null) continue;

            SortingGroup sg = k.GetComponent<SortingGroup>();
            sb.AppendLine($"  Khách #{i}: {DuongDan(k.gameObject)}  ·  vị trí world = {Fmt(k.transform.position)}");
            if (sg != null)
            {
                sb.AppendLine($"     SortingGroup: layer='{sg.sortingLayerName}' (value={GiaTriLayer(sg.sortingLayerName)}) " +
                               $"· order={sg.sortingOrder} · enabled={sg.enabled}");
            }
            else
            {
                sb.AppendLine("     SortingGroup: (KHÔNG CÓ — bất thường, TouristAgent lẽ ra luôn có do [RequireComponent]).");
            }

            Renderer[] rends = k.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0)
            {
                sb.AppendLine("     (khách này không có Renderer con nào)");
                continue;
            }

            for (int j = 0; j < rends.Length; j++)
            {
                Renderer r = rends[j];
                if (r == null) continue;
                if (dong >= MaxDongMoiPhan) { boQua++; continue; }
                dong++;
                sb.AppendLine("     " + DongRenderer(r));
            }
        }
        if (boQua > 0) sb.AppendLine($"   …còn {boQua} dòng renderer nữa (đã ẩn bớt).");
    }

    private void InPhanC_PhanXu(StringBuilder sb, TouristAgent[] khach, TouristBoatController[] tau)
    {
        sb.AppendLine();
        sb.AppendLine("── PHẦN C — PHÁN XỬ: AI VẼ ĐÈ LÊN AI (cặp cách nhau < 15 đơn vị) ────────────");

        if (khach == null || khach.Length == 0 || tau == null || tau.Length == 0)
        {
            sb.AppendLine("   (thiếu khách hoặc thiếu tàu trong scene lúc này ⇒ chưa kiểm tra được cặp nào)");
            return;
        }

        int dong = 0, boQua = 0;
        bool coCapNaoGan = false;
        bool phatHienSai = false;

        for (int ik = 0; ik < khach.Length; ik++)
        {
            TouristAgent k = khach[ik];
            if (k == null) continue;

            Renderer rKhachDauTien = k.GetComponentInChildren<Renderer>(true);
            HieuLucSorting hlKhach = LayHieuLuc(
                k.transform,
                rKhachDauTien != null ? rKhachDauTien.sortingLayerName : "Objects",
                rKhachDauTien != null ? rKhachDauTien.sortingOrder : 0,
                rKhachDauTien != null
                    ? "Renderer riêng '" + DuongDan(rKhachDauTien.gameObject) + "' (không thấy SortingGroup nào bao ngoài — bất thường)"
                    : "(không có Renderer/SortingGroup nào để đọc — bất thường)");

            for (int it = 0; it < tau.Length; it++)
            {
                TouristBoatController b = tau[it];
                if (b == null) continue;

                float khoangCach = Vector3.Distance(k.transform.position, b.transform.position);
                if (khoangCach >= KhoangCachXet) continue;
                coCapNaoGan = true;

                Renderer[] rendsTau = b.GetComponentsInChildren<Renderer>(true);
                if (rendsTau == null || rendsTau.Length == 0) continue;

                for (int jr = 0; jr < rendsTau.Length; jr++)
                {
                    Renderer rTau = rendsTau[jr];
                    if (rTau == null) continue;

                    HieuLucSorting hlTau = LayHieuLuc(
                        rTau.transform, rTau.sortingLayerName, rTau.sortingOrder,
                        "Renderer riêng '" + DuongDan(rTau.gameObject) + "' (không có SortingGroup nào bao ngoài)");

                    int gtKhach = GiaTriLayer(hlKhach.layerName);
                    int gtTau = GiaTriLayer(hlTau.layerName);

                    string ketLuan;
                    string check;
                    if (gtKhach > gtTau)
                    {
                        ketLuan = "KHÁCH vẽ trên";
                        check = "❌ SAI";
                        phatHienSai = true;
                    }
                    else if (gtTau > gtKhach)
                    {
                        ketLuan = "TÀU vẽ trên";
                        check = "✅";
                    }
                    else if (hlKhach.order > hlTau.order)
                    {
                        ketLuan = "KHÁCH vẽ trên (cùng layer, order khách lớn hơn)";
                        check = "❌ SAI";
                        phatHienSai = true;
                    }
                    else if (hlTau.order > hlKhach.order)
                    {
                        ketLuan = "TÀU vẽ trên (cùng layer, order tàu lớn hơn)";
                        check = "✅";
                    }
                    else
                    {
                        ketLuan = "KHÔNG XÁC ĐỊNH (dễ nhấp nháy — trùng cả layer lẫn order)";
                        check = "⚠️";
                    }

                    if (dong < MaxDongMoiPhan)
                    {
                        dong++;
                        sb.AppendLine($"  KHÁCH {DuongDan(k.gameObject)} ({hlKhach.layerName}/{hlKhach.order}, value={gtKhach}) " +
                                      $"[nguồn: {hlKhach.nguon}]");
                        sb.AppendLine($"    vs TÀU {DuongDan(rTau.gameObject)} ({hlTau.layerName}/{hlTau.order}, value={gtTau}) " +
                                      $"[nguồn: {hlTau.nguon}]");
                        sb.AppendLine($"    ⇒ {ketLuan}  {check}   (cách nhau {khoangCach:0.0} đơn vị world)");
                    }
                    else
                    {
                        boQua++;
                    }
                }
            }
        }

        if (!coCapNaoGan)
        {
            sb.AppendLine("   (không có cặp khách-tàu nào đang cách nhau < 15 đơn vị lúc bấm F8 — " +
                           "hãy đứng gần tàu lúc khách còn trên/quanh tàu rồi bấm F8 lại)");
        }
        if (boQua > 0)
        {
            sb.AppendLine($"   …còn {boQua} dòng phán xử nữa (đã ẩn bớt).");
        }
        if (coCapNaoGan)
        {
            sb.AppendLine(phatHienSai
                ? "  ⚠️  KẾT LUẬN: PHÁT HIỆN ÍT NHẤT 1 CẶP KHÁCH ĐANG VẼ ĐÈ LÊN TÀU (xem dòng ❌ SAI ở trên) — gửi nguyên đoạn Console này cho Lead."
                : "  KẾT LUẬN: không phát hiện cặp nào khách đè lên tàu trong số các cặp đang gần nhau lúc bấm F8 này.");
        }
    }

    private void InPhanD_BangLayer(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("── PHẦN D — BẢNG SORTING LAYER THẬT CỦA PROJECT (name · id · value) ────────");
        try
        {
            SortingLayer[] layers = SortingLayer.layers;
            if (layers == null || layers.Length == 0)
            {
                sb.AppendLine("   (không đọc được SortingLayer.layers)");
                return;
            }
            for (int i = 0; i < layers.Length; i++)
            {
                sb.AppendLine($"   [{i}] {layers[i].name}  ·  id={layers[i].id}  ·  value={layers[i].value}");
            }
            sb.AppendLine("   (value CÀNG LỚN ⇒ vẽ CÀNG SAU / CÀNG TRÊN; đây là thứ Phần C dùng để phán xử)");
        }
        catch (Exception ex)
        {
            sb.AppendLine("   (lỗi đọc bảng layer: " + ex.Message + ")");
        }
    }

    // ─── Tiện ích dùng chung ─────────────────────────────────────────────
    private static string DongRenderer(Renderer r)
    {
        GameObject go = r.gameObject;
        return $"{DuongDan(go)}  ·  {r.GetType().Name}  ·  layer='{r.sortingLayerName}'  ·  " +
               $"sortingLayerID={r.sortingLayerID}  ·  sortingOrder={r.sortingOrder}  ·  " +
               $"enabled={r.enabled}  ·  activeInHierarchy={go.activeInHierarchy}  ·  " +
               $"vị trí world = {Fmt(go.transform.position)}";
    }

    private static string Fmt(Vector3 v) => $"({v.x:0.##}, {v.y:0.##}, {v.z:0.##})";

    private static string DuongDan(GameObject go)
    {
        if (go == null) return "(null)";
        var sb = new StringBuilder(go.name);
        Transform t = go.transform.parent;
        int guard = 0;
        while (t != null && guard++ < 16)
        {
            sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }
}
#endif
