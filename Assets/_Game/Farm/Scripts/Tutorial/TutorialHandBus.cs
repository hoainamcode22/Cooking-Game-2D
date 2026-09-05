using System;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
// [V6] TRỌNG TÀI BÀN TAY — CHỈ MỘT BÀN TAY TẠI MỘT THỜI ĐIỂM
// ═══════════════════════════════════════════════════════════════════════════
//
// VÌ SAO CÓ FILE NÀY: ba script cầm BA object tay khác nhau trong scene và
// không ai biết ai:
//   • TutorialManager._handPointer          → Hand_Click_Plot                (tay tĩnh)
//   • TutorialDragHintAnimator._hand        → Hand_Drag_Seed                 (tay kéo)
//   • TutorialActionHandGuide._hand         → Hand_Action_Plot_…             (tay hành động)
//   • TutorialPhantomDemoManager            → Phantom_Hand                   (tay ảo ảnh)
// Ở bước kéo hạt, tay tĩnh chỉ ô đất CÒN tay kéo chỉ ô "Lúa" ⇒ hai bàn tay
// cùng hiện, người chơi không biết nghe theo tay nào.
//
// CÁCH LÀM — CỐ Ý TỐI GIẢN, ÍT RỦI RO NHẤT:
// Bus KHÔNG tự đi tắt object của người khác (dễ hỏng khi ref rơi / scene đổi).
// Bus chỉ GIỮ TRẠNG THÁI "ai đang là chủ bàn tay". Mỗi script tự hỏi bus trước
// khi bật tay của mình, và tự ẩn tay của mình khi nhả quyền.
//
// KHÔNG phải MonoBehaviour, KHÔNG phụ thuộc scene ⇒ gọi được từ bất cứ đâu,
// kể cả khi object tương ứng đã bị xoá khỏi scene.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Bốn loại bàn tay có thể xuất hiện trong tutorial (KhongCo = đang rảnh).</summary>
public enum LoaiTay
{
    KhongCo,
    TayTinh,      // TutorialManager._handPointer      (Hand_Click_Plot)
    TayKeo,       // TutorialDragHintAnimator._hand    (Hand_Drag_Seed)
    TayHanhDong,  // TutorialActionHandGuide._hand     (Hand_Action_…)
    TayAoAnh,     // TutorialPhantomDemoManager        (Phantom_Hand)
}

/// <summary>
/// Trọng tài "một tay một lúc". Chủ mới GIÀNH quyền bằng <see cref="Nhan"/>,
/// nhả bằng <see cref="Nha"/>. Không tự tắt object của ai — người gọi tự lo phần đó.
/// </summary>
public static class TutorialHandBus
{
    /// <summary>Ai đang giữ quyền hiện tay. <see cref="LoaiTay.KhongCo"/> = đang rảnh.</summary>
    public static LoaiTay ChuHienTai { get; private set; } = LoaiTay.KhongCo;

    /// <summary>
    /// Chủ mới giành quyền. <paramref name="anTayKhac"/> (có thể null) được gọi kèm
    /// CHỦ CŨ để người gọi tự đi ẩn bàn tay của chủ cũ nếu muốn.
    /// Gọi với <see cref="LoaiTay.KhongCo"/> đồng nghĩa nhả sạch.
    /// </summary>
    public static void Nhan(LoaiTay chu, Action<LoaiTay> anTayKhac = null)
    {
        if (chu == LoaiTay.KhongCo)
        {
            NhaTatCa();
            return;
        }

        LoaiTay cu = ChuHienTai;
        ChuHienTai = chu;   // đặt TRƯỚC khi gọi callback: chủ cũ có gọi Nha() cũng không cướp lại được

        if (anTayKhac == null || cu == LoaiTay.KhongCo || cu == chu) return;

        // Callback là code của người khác — hỏng ở đó không được phép làm hỏng trọng tài.
        try { anTayKhac(cu); }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialHandBus] Lỗi khi ẩn tay của chủ cũ '{cu}': {e.Message}");
        }
    }

    /// <summary>
    /// Chủ nhả quyền. CHỈ nhả khi đúng là chủ hiện tại — chủ cũ gọi muộn sẽ không
    /// cướp quyền của chủ mới (đây là chỗ dễ sinh lỗi "mất tay" nhất).
    /// </summary>
    public static void Nha(LoaiTay chu)
    {
        if (chu != LoaiTay.KhongCo && ChuHienTai == chu)
            ChuHienTai = LoaiTay.KhongCo;
    }

    /// <summary>Nhả sạch — dùng khi sang bước mới hoặc dọn toàn bộ UI tutorial.</summary>
    public static void NhaTatCa()
    {
        ChuHienTai = LoaiTay.KhongCo;
    }

    /// <summary>Có chủ nào KHÁC <paramref name="chu"/> đang giữ tay không.</summary>
    public static bool ChuKhacDangGiu(LoaiTay chu)
    {
        return ChuHienTai != LoaiTay.KhongCo && ChuHienTai != chu;
    }

    /// <summary>
    /// Đặt lại khi vào Play. Cần thiết vì static không tự reset khi bật
    /// "Enter Play Mode without Domain Reload" — bỏ qua sẽ dính chủ cũ của phiên trước.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DatLaiKhiVaoPlay()
    {
        ChuHienTai = LoaiTay.KhongCo;
    }
}
