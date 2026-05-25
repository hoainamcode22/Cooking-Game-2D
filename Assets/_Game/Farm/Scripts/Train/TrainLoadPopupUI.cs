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

    // true khi popup đang thực sự hiển thị (toggle trực tiếp gameObject)
    public bool IsOpen => gameObject.activeSelf;

    void Awake()
    {
        if (Btn_themhang != null) Btn_themhang.onClick.AddListener(OnThemHangClicked);
        if (Btn_close    != null) Btn_close.onClick.AddListener(ClosePopup);
        gameObject.SetActive(false);
    }

    public void OpenForCargoSlot(int slotIndex, TrainWagonSlotData slotData)
    {
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
        gameObject.SetActive(false);
        _selectedSlotIndex = -1;
        _selectedSlot      = null;
    }

    private void OnThemHangClicked()
    {
        if (_selectedSlot == null || _selectedSlotIndex < 0) return;
        if (TrainManager.Instance == null) return;
        TrainManager.Instance.TryAddOneItemToSlot(_selectedSlotIndex);
    }
}
