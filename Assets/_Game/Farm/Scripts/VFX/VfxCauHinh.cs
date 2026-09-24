// ============================================================================
//  VfxCauHinh — cong tac chung cho VFX nhe (2026-09-24)
//    CheDoNhe : may yeu (RAM < 3.5GB hoac <= 4 nhan) -> cac hieu ung tu giam so luong hat.
//               Ep bat/tat de test: VfxCauHinh.EpCheDoNhe = true/false (null = tu dong).
//  GioNongTrai — gio dung chung cho ca nong trai: 1 ham tinh thuan (khong Update, khong cap phat).
//    HeSo(x) : 0.75..1.35, co "con gio" lan tu trai sang phai theo thoi gian -> cay nghieng theo dot.
// ============================================================================
using UnityEngine;

public static class VfxCauHinh
{
    public static bool? EpCheDoNhe;
    private static int _daTinh = -1;

    public static bool CheDoNhe
    {
        get
        {
            if (EpCheDoNhe.HasValue) return EpCheDoNhe.Value;
            if (_daTinh < 0)
                _daTinh = (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3500) || SystemInfo.processorCount <= 4 ? 1 : 0;
            return _daTinh == 1;
        }
    }

    /// <summary>So luong theo che do: may yeu lay khoang 1/2.</summary>
    public static int SoLuong(int binhThuong) => CheDoNhe ? Mathf.Max(1, binhThuong / 2) : binhThuong;
}

public static class GioNongTrai
{
    [Tooltip("Toc do con gio lan qua ban do (don vi world / giay).")]
    public static float TocDoLan = 420f;
    public static float ChuKy = 9f;

    /// <summary>He so gio tai hoanh do x (world). Gia tri 0.75..1.35.</summary>
    public static float HeSo(float x)
    {
        float t = Time.time;
        float song = Mathf.Sin((t * TocDoLan - x) / (TocDoLan * ChuKy) * Mathf.PI * 2f);
        float dot = Mathf.Max(0f, song);
        return 0.75f + 0.6f * dot * dot;
    }
}
