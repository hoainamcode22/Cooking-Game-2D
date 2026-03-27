using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehouseSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text txtSoLuong;

    public void SetEmpty()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;   // ẩn icon
        }

        if (txtSoLuong != null)
            txtSoLuong.text = "";   // xóa số lượng

        gameObject.SetActive(true); // giữ slot/khung vẫn hiện
    }

    public void SetData(Sprite itemIcon, int amount)
    {
        if (icon != null)
        {
            icon.sprite = itemIcon;
            icon.enabled = itemIcon != null;
        }

        if (txtSoLuong != null)
            txtSoLuong.text = "x" + amount;

        gameObject.SetActive(true);
    }
}