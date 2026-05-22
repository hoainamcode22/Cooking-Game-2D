using UnityEngine;

/// <summary>
/// Gắn vào root NPC cũ (Animation_cuocdat).
/// Bridge giữa lệnh animation cũ (FarmerBehavior) và FarmerWateringAnimator trên child.
/// </summary>
[DisallowMultipleComponent]
public class FarmerAnimationAdapter : MonoBehaviour
{
    private FarmerWateringAnimator      _watering;
    private FarmerWateringLifeController _life;

    private void Awake()
    {
        _watering = GetComponentInChildren<FarmerWateringAnimator>(true);
        _life     = GetComponent<FarmerWateringLifeController>();

        if (_watering == null)
            Debug.LogWarning($"[FarmerAnimationAdapter:{name}] FarmerWateringAnimator not found in children");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    /// LiftHoe → Idle
    public void PlayIdle()
    {
        Debug.Log("[FarmerAnimationAdapter] SetMoving false");
        _watering?.SetMoving(false);
    }

    /// WalkHoe → Walk
    public void PlayWalk()
    {
        Debug.Log("[FarmerAnimationAdapter] SetMoving true");
        _watering?.SetMoving(true);
    }

    /// StartWork → chuỗi tưới + ăn mừng + đi dạo
    public void PlayWork(PlotController plot)
    {
        if (_life != null)
        {
            Debug.Log("[FarmerAnimationAdapter] PlayWorkLifeSequence");
            _life.PlayWorkLifeSequence(plot);
        }
        else
        {
            _watering?.PlayWatering();
        }
    }

    /// Rest → Idle
    public void PlayRest()
    {
        Debug.Log("[FarmerAnimationAdapter] SetMoving false (rest)");
        _watering?.SetMoving(false);
    }
}
