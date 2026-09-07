using UnityEngine;

/// <summary>
/// Cong kiem quyen mo TAU CHO HANG. MOT cho duy nhat giu con so cap yeu cau.
///
/// Vi sao tach class rieng thay vi viet `if (level < 3)` tai cho: popup ga tau co NHIEU
/// duong mo, khoa mot duong ma quen duong kia thi nguoi choi cap 1 van vao duoc bang
/// duong con lai:
///   1. Click nha ga ngoai world      -> TrainStationBuilding.HandleClick()
///   2. Popup "Dang van chuyen" ban giao sang popup nhan thuong
///      -> TrainProcessPopupUI -> TrainStationMasterPopupUI.OpenPopup()
///   3. Bat cu code nao goi TrainStationMasterPopupUI.OpenPopup() (tutorial / deeplink sau nay)
/// Viet so 3 o ba cho thi lan sau doi cap la chac chan lech mot cho.
///
/// Vi sao la cap 3: Sep chot 06/09/2026. Trung luon voi
/// AnimalGuideController.TrainMinLevel = 3 (con thu huong dan cung chi nhac ve tau tu cap 3),
/// nen loi nhac va quyen mo khong bao gio lech nhau.
/// </summary>
public static class TrainGateAccess
{
    /// <summary>Cap toi thieu de mo duoc tau cho hang. DOI SO O DAY, khong rai ra cho khac.</summary>
    public const int RequiredLevel = 3;

    /// <summary>Thong bao hien cho nguoi choi khi chua du cap. Ghep tu <see cref="RequiredLevel"/>
    /// chu khong go lai so, doi cap mot cho la cau thong bao doi theo.</summary>
    public static readonly string LockedMessage = $"Tau hoa mo o cap {RequiredLevel} nhe!";

    /// <summary>
    /// Cap nguoi choi hien tai. Doc `PlayerProgressManager` truoc, `FarmLevelManager` sau,
    /// dung thu tu ma `CookingGateAccess.CurrentLevel` dang dung, de hai cong khong bao gio
    /// doc ra hai cap khac nhau.
    /// </summary>
    public static int CurrentLevel
    {
        get
        {
            if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
            if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
            return 1;
        }
    }

    /// <summary>true = da du cap theo <see cref="RequiredLevel"/>.</summary>
    public static bool DuCap => CurrentLevel >= RequiredLevel;

    /// <summary>
    /// true = he tau dang co viec do dang (chuyen dang chay, tau thuong dang ve, hoac con toa
    /// thuong chua thu). Dung de KHONG KEP STATE: neu mot save cu (hoac mot lan tut cap) de lai
    /// chuyen dang chay, van phai cho mo popup de nguoi choi thu not, neu chan thi hang cua ho
    /// bi giam trong popup khong bao gio mo lai duoc.
    /// </summary>
    public static bool CoViecDoDang
    {
        get
        {
            if (TrainManager.Instance == null) return false;
            return TrainManager.Instance.State != TrainState.WaitingForLoad;
        }
    }

    /// <summary>Duoc phep mo popup tau hay khong.</summary>
    public static bool CanOpen => DuCap || CoViecDoDang;

    /// <summary>
    /// Kiem quyen va TU hien thong bao khi bi chan. Tra true = duoc phep mo popup tau.
    /// Gop ca viec bao vao day de khong noi nao chan IM LANG: tre em bam mai vao nha ga ma
    /// game khong noi gi se tuong game bi loi.
    /// </summary>
    public static bool CanOpenOrWarn()
    {
        if (CanOpen) return true;

        FarmUIManager.Instance?.ShowHint(LockedMessage);
        {
            Debug.Log($"[Train] Chan mo tau: dang cap {CurrentLevel}, can cap {RequiredLevel}.");
        }
        return false;
    }
}
