using System.Collections;
using UnityEngine;

public class FarmerNPCController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform homePoint;
    [SerializeField] private Animator animator;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private Vector3 standOffset = new Vector3(0f, -0.15f, 0f);

    [Header("Water Timing")]
    [SerializeField] private float baseWaterIntervalRealSeconds = 12f;
    [SerializeField] private int baseReduceGrowSeconds = 15;
    [SerializeField] private float waterAnimDuration = 0.8f;

    [Header("Animator Params")]
    [SerializeField] private string walkBoolName = "IsWalking";
    [SerializeField] private string waterTriggerName = "DoWater";

    private bool isBusy;
    private float nextWaterTime;
    private float boostMultiplier = 1f;
    private float boostExpireTime = -1f;

    private void Awake()
    {
        if (homePoint == null)
        {
            GameObject go = new GameObject("FarmerHomePoint_Auto");
            go.transform.position = transform.position;
            homePoint = go.transform;
        }
    }

    private void Update()
    {
        UpdateBoostState();

        if (isBusy)
            return;

        if (Time.time < nextWaterTime)
            return;

        if (FarmManager.Instance == null)
            return;

        PlotController targetPlot = FarmManager.Instance.GetNextGrowingPlot();
        if (targetPlot == null)
            return;

        StartCoroutine(WaterRoutine(targetPlot));
    }

    public void NotifyNewGrowingPlot()
    {
        nextWaterTime = Mathf.Min(nextWaterTime, Time.time + 1f);
    }

    public void ApplyBoost(float multiplier, float durationSeconds)
    {
        boostMultiplier = Mathf.Max(boostMultiplier, multiplier);
        boostExpireTime = Time.time + durationSeconds;
    }

    [ContextMenu("Test Ad Boost")]
    public void ApplyAdBoostTest()
    {
        ApplyBoost(1.5f, 300f);
        Debug.Log("Farmer nhận Ad Boost: tưới nhanh hơn trong 5 phút thật.");
    }

    [ContextMenu("Test Gem Boost")]
    public void ApplyGemBoostTest()
    {
        ApplyBoost(2.5f, 600f);
        Debug.Log("Farmer nhận Gem Boost: tưới rất nhanh trong 10 phút thật.");
    }

    private IEnumerator WaterRoutine(PlotController plot)
    {
        if (plot == null)
            yield break;

        isBusy = true;

        Vector3 targetPos = plot.transform.position + standOffset;

        yield return MoveTo(targetPos);

        SetWalking(false);

        if (animator != null && !string.IsNullOrEmpty(waterTriggerName))
            animator.SetTrigger(waterTriggerName);

        yield return new WaitForSeconds(waterAnimDuration);

        if (plot != null && plot.IsPlanted && !plot.IsReadyToHarvest())
        {
            int reduceSeconds = GetCurrentWaterReduceSeconds();
            plot.ApplyWaterBonus(reduceSeconds);

            Debug.Log($"Farmer tưới Plot {plot.PlotId}, giảm {reduceSeconds}s");
        }

        yield return new WaitForSeconds(0.1f);

        if (homePoint != null)
            yield return MoveTo(homePoint.position);

        SetWalking(false);

        nextWaterTime = Time.time + GetCurrentWaterInterval();
        isBusy = false;
    }

    private IEnumerator MoveTo(Vector3 targetPos)
    {
        SetWalking(true);

        while (Vector3.Distance(transform.position, targetPos) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
        SetWalking(false);
    }

    private void SetWalking(bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolName))
            animator.SetBool(walkBoolName, value);
    }

    private void UpdateBoostState()
    {
        if (boostExpireTime < 0f)
            return;

        if (Time.time >= boostExpireTime)
        {
            boostMultiplier = 1f;
            boostExpireTime = -1f;
        }
    }

    private float GetCurrentWaterInterval()
    {
        return Mathf.Max(2f, baseWaterIntervalRealSeconds / boostMultiplier);
    }

    private int GetCurrentWaterReduceSeconds()
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseReduceGrowSeconds * boostMultiplier));
    }


}