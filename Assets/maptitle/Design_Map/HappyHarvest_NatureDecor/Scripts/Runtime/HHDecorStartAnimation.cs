using UnityEngine;

public class HHDecorStartAnimation : MonoBehaviour
{
    public Animation Animation;

    public void Trigger()
    {
        if (Animation != null)
            Animation.Play();
    }
}
