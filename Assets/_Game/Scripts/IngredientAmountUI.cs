using TMPro;
using UnityEngine;

public class IngredientAmountUI : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private int amount;

    public int Amount => amount;

    private void Start()
    {
        RefreshUI();
    }
    public void InitAmount(int startAmount)
{
    amount = Mathf.Max(0, startAmount);
    RefreshUI();
}

    public void SetAmount(int amount)
    {
        this.amount = Mathf.Max(0, amount);
        RefreshUI();
    }

    public bool DecreaseOne()
    {
        if (amount <= 0)
            return false;

        amount--;
        RefreshUI();
        return true;
    }

    public void IncreaseOne()
    {
        amount++;
        RefreshUI();
    }

    private void RefreshUI()// cập nhật hiển thị số lượng trên UI, nếu amountText không null thì sẽ hiển thị số lượng hiện tại của amount lên UI
    {
        if (amountText != null)
            amountText.text = amount.ToString();
    }
}
