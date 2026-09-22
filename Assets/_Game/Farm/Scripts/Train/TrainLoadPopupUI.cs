using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainLoadPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image    img_icon;
    [SerializeField] private TMP_Text txt_soluong;
    [SerializeField] private Button   Btn_themhang;
    [SerializeField] private Button   Btn_close;

    private int                _selectedSlotIndex = -1;
    private TrainWagonSlotData _selectedSlot;
    private bool               _popupInputLockHeld;

    // true khi popup đang thực sự hiển thị (toggle trực tiếp gameObject)
    public bool IsOpen => gameObject.activeSelf;

    void Awake()
    {
        if (Btn_themhang != null) Btn_themhang.onClick.AddListener(OnThemHangClicked);
        if (Btn_close    != null) Btn_close.onClick.AddListener(ClosePopup);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// [SUA 2026-09-22] Day la popup CU, nam san trong SCN_Farm o duoi Canvas_Popup — tuc la
    /// ANH EM cua popup nha ga, KHONG nam trong khung popup nao. Vi vay khi no bat len, mon
    /// hang hien "ngoai" popup, lech han sang trai (anchoredPosition x = -601).
    ///
    /// Dung ra no khong bao gio duoc dung den: popup that la
    /// ExportTrainUIPackage.TrainLoadPopupUI (co khung go + nut close giong popup nha ga).
    /// Nhung 3 prefab popup nam ngoai Resources/ va o TrainStationBuilding chua gan Inspector,
    /// nen trong BUILD ANDROID chung khong bao gio duoc tao ra -> rot xuong popup cu nay.
    /// Da chuyen 3 prefab vao Assets/_Game/Resources/Train/ nen build tu tao duoc.
    ///
    /// Chot chan duoi day de chac chan: neu popup that co mat thi day hang sang no va
    /// KHONG bat popup cu len nua.
    /// </summary>
    public void OpenForCargoSlot(int slotIndex, TrainWagonSlotData slotData)
    {
        var pkg = ExportTrainUIPackage.TrainLoadPopupUI.Instance
               ?? FindFirstObjectByType<ExportTrainUIPackage.TrainLoadPopupUI>(FindObjectsInactive.Include);
        if (pkg != null)
        {
            gameObject.SetActive(false);
            pkg.OpenForWagon(slotIndex);
            return;
        }

        _selectedSlotIndex = slotIndex;
        _selectedSlot      = slotData;

        // Bật tất cả parent trước khi bật chính nó
        Transform p = transform.parent;
        while (p != null)
        {
            p.gameObject.SetActive(true);
            p = p.parent;
        }
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
        RefreshPopup();
    }

    public void RefreshPopup()
    {
        if (_selectedSlot == null) return;
        if (img_icon != null)
        {
            img_icon.sprite  = _selectedSlot.icon;
            img_icon.enabled = _selectedSlot.icon != null;
        }
        if (txt_soluong != null)
            txt_soluong.text = $"{_selectedSlot.currentAmount} / {_selectedSlot.requiredAmount}";
        if (Btn_themhang != null)
            Btn_themhang.interactable = !_selectedSlot.IsCargoComplete;
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
        _selectedSlotIndex = -1;
        _selectedSlot      = null;
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);

        if (!_popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            _popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);

        if (_popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            _popupInputLockHeld = false;
        }
    }

    private void OnThemHangClicked()
    {
        if (_selectedSlot == null || _selectedSlotIndex < 0) return;
        if (TrainManager.Instance == null) return;
        TrainManager.Instance.TryAddOneItemToSlot(_selectedSlotIndex);
    }
}
