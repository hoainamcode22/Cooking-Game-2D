using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CowSlotUI : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private Image imgCow;

    [Header("Growth")]
    [SerializeField] private Image growthFill;
    [SerializeField] private TMP_Text txtGrowthPercent;

    [Header("Harvest")]
    [SerializeField] private Image harvestFill;
    [SerializeField] private TMP_Text txtHarvestTime;

    public void SetInactive()
    {
        if (imgCow != null) imgCow.enabled = false;
        SetFill(growthFill, 0f);
        SetFill(harvestFill, 0f);

        if (txtGrowthPercent != null) txtGrowthPercent.text = "--";
        if (txtHarvestTime != null) txtHarvestTime.text = "--";
    }

    public void SetIdle()
    {
        if (imgCow != null) imgCow.enabled = true;
        SetFill(growthFill, 0f);
        SetFill(harvestFill, 0f);

        if (txtGrowthPercent != null) txtGrowthPercent.text = "0%";
        if (txtHarvestTime != null) txtHarvestTime.text = "Chưa cho ăn";
    }

    public void SetGrowing(float progress01, float secondsLeft)
    {
        if (imgCow != null) imgCow.enabled = true;
        SetFill(growthFill, progress01);
        SetFill(harvestFill, 0f);

        if (txtGrowthPercent != null)
            txtGrowthPercent.text = Mathf.RoundToInt(progress01 * 100f) + "%";

        if (txtHarvestTime != null)
            txtHarvestTime.text = Mathf.CeilToInt(secondsLeft) + "s";
    }

    public void SetHarvesting(float progress01, float secondsLeft)
    {
        if (imgCow != null) imgCow.enabled = true;
        SetFill(growthFill, 1f);
        SetFill(harvestFill, progress01);

        if (txtGrowthPercent != null) txtGrowthPercent.text = "100%";
        if (txtHarvestTime != null) txtHarvestTime.text = Mathf.CeilToInt(secondsLeft) + "s";
    }

    public void SetReady()
    {
        if (imgCow != null) imgCow.enabled = true;
        SetFill(growthFill, 1f);
        SetFill(harvestFill, 1f);

        if (txtGrowthPercent != null) txtGrowthPercent.text = "100%";
        if (txtHarvestTime != null) txtHarvestTime.text = "Sẵn sàng";
    }

    private void SetFill(Image fill, float value)
    {
        if (fill == null) return;
        fill.fillAmount = Mathf.Clamp01(value);
    }
}