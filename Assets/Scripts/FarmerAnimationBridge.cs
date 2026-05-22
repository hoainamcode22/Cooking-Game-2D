using UnityEngine;

/// <summary>
/// Cầu nối giữa FarmerBehavior (AI logic) và Animator (animation).
/// Tự cập nhật Speed từ Rigidbody2D velocity, flip sprite theo hướng đi.
/// Gọi SetWatering() / PlayCelebrate() từ FarmerBehavior khi cần.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class FarmerAnimationBridge : MonoBehaviour
{
    Animator     anim;
    Rigidbody2D  rb;
    Vector2      lastPos;

    // Cache hash để tránh string lookup mỗi frame
    static readonly int SpeedHash     = Animator.StringToHash("Speed");
    static readonly int WaterHash     = Animator.StringToHash("IsWatering");
    static readonly int CelebrateHash = Animator.StringToHash("Celebrate");

    void Awake()
    {
        anim    = GetComponent<Animator>();
        rb      = GetComponent<Rigidbody2D>();
        lastPos = transform.position;
    }

    void Update()
    {
        // Tính speed từ velocity (Unity 6) hoặc delta position fallback
#if UNITY_6000_0_OR_NEWER
        float speed = rb.linearVelocity.magnitude;
        float vx    = rb.linearVelocity.x;
#else
        float speed = rb.velocity.magnitude;
        float vx    = rb.velocity.x;
#endif

        // Fallback: nếu Rigidbody đứng yên thì tính từ delta position
        if (speed < 0.01f)
            speed = ((Vector2)transform.position - lastPos).magnitude / Time.deltaTime;

        anim.SetFloat(SpeedHash, speed);

        // Flip nhân vật theo hướng di chuyển ngang
        if (vx > 0.05f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (vx < -0.05f)
            transform.localScale = new Vector3(-1f, 1f, 1f);

        lastPos = transform.position;
    }

    /// <summary>
    /// Gọi từ FarmerBehavior khi bắt đầu/kết thúc tưới nước.
    /// Ví dụ: bridge.SetWatering(true) khi tới ô đất, bridge.SetWatering(false) khi xong.
    /// </summary>
    public void SetWatering(bool on) => anim.SetBool(WaterHash, on);

    /// <summary>
    /// Gọi từ FarmerBehavior khi lúa đạt lever 2 (ăn mừng).
    /// Ví dụ: bridge.PlayCelebrate() trong callback OnCropLevelUp.
    /// </summary>
    public void PlayCelebrate() => anim.SetTrigger(CelebrateHash);
}
