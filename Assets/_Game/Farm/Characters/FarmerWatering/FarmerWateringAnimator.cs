using UnityEngine;

/// <summary>
/// Runtime controller cho nhân vật FarmerWatering.
/// Gắn vào root prefab PFB_FarmerWatering.
/// </summary>
public class FarmerWateringAnimator : MonoBehaviour
{
    public Animator animator;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void SetMoving(bool moving)
    {
        if (animator) animator.SetBool("IsMoving", moving);
    }

    public void PlayWatering()
    {
        if (animator) animator.SetTrigger("Water");
    }

    public void PlayCelebrate()
    {
        if (animator) animator.SetTrigger("Celebrate");
    }
}
