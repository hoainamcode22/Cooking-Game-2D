using TMPro;
using UnityEngine;

public class CenterCookingPanelUI : MonoBehaviour
{
    [Header("Top Target UI")]
    [SerializeField] private TargetFlavorBoxUI targetFlavorBoxUI;

    [Header("Cook Button UI")]
    [SerializeField] private TMP_Text txtCookSubmitScore;

    public void BindDish(DishData dishData)
    {
        if (targetFlavorBoxUI != null)
            targetFlavorBoxUI.BindDish(dishData);
    }

    public void ClearCenter()
    {
        if (targetFlavorBoxUI != null)
            targetFlavorBoxUI.ClearUI();

        SetCookSubmitScore(0);
    }

    public void SetCookSubmitScore(int score)
    {
        if (txtCookSubmitScore != null)
            txtCookSubmitScore.text = score + " Score";
    }
}