using UnityEngine;

/// <summary>
/// Gắn lên nhân vật (dân làng, thợ búa, khách du lịch, đầu bếp).
/// Phát tiếng chào vui nhộn (character_greet.wav) và nảy mẩy mẩy khi:
/// 1. Người chơi click/chạm vào nhân vật.
/// 2. Camera zoom lại gần nhân vật.
/// </summary>
public class CharacterVoiceReaction : MonoBehaviour
{
    [SerializeField] private float jumpHeight = 15f;
    [SerializeField] private float cooldown = 1.2f;

    private float _lastGreetTime = -999f;
    private bool _isJumping;
    private Vector3 _baseLocalPos;

    private void Awake()
    {
        _baseLocalPos = transform.localPosition;
    }

    private void OnMouseDown()
    {
        TryGreet();
    }

    public void TryGreet()
    {
        if (Time.unscaledTime - _lastGreetTime < cooldown) return;
        _lastGreetTime = Time.unscaledTime;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterGreet();
        }

        if (!_isJumping)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    private System.Collections.IEnumerator JumpRoutine()
    {
        _isJumping = true;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.localPosition = _baseLocalPos + new Vector3(0f, yOffset, 0f);
            yield return null;
        }

        transform.localPosition = _baseLocalPos;
        _isJumping = false;
    }
}
