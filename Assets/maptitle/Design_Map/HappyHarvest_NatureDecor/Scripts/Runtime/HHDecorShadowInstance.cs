using UnityEngine;

public class HHDecorShadowInstance : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private SpriteRenderer shadowRenderer;

    private void LateUpdate()
    {
        if (targetRenderer == null || shadowRenderer == null)
            return;

        Color color = shadowRenderer.color;
        color.a = Mathf.Min(color.a, targetRenderer.color.a);
        shadowRenderer.color = color;
    }
}
