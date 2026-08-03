using UnityEngine;

/// <summary>
/// Di chuyển nhân vật 4 hướng bằng WASD / phím mũi tên (khoá theo 4 hướng, không đi chéo).
/// Đẩy MoveX/MoveY/IsMoving sang Animator để blend tree chọn hoạt ảnh đúng hướng.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Tốc độ đi — ĐƠN VỊ WORLD (không theo scale nhân vật). Map dựng scale lớn thì để vài trăm. " +
             "Kéo thử trong Play cho vừa.")]
    [SerializeField] private float moveSpeed = 500f;
    [Tooltip("Để trống = tự lấy Animator ở chính object/đời con.")]
    [SerializeField] private Animator animator;

    private Rigidbody2D _rb;
    private Vector2 _input;                       // hướng đi hiện tại (đã khoá 4 hướng)
    private Vector2 _lastDir = Vector2.down;      // hướng cuối cùng → Idle quay đúng mặt

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Đọc input thô (WASD + mũi tên).
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;

        // KHOÁ 4 HƯỚNG: giữ trục mạnh hơn, bỏ trục còn lại → không đi chéo.
        if (Mathf.Abs(x) > Mathf.Abs(y)) y = 0f;
        else                              x = 0f;

        _input = new Vector2(x, y);
        if (_input.sqrMagnitude > 1f) _input.Normalize();

        bool moving = _input.sqrMagnitude > 0.01f;
        if (moving) _lastDir = _input;   // nhớ hướng để Idle quay đúng

        if (animator != null)
        {
            animator.SetFloat("MoveX", _lastDir.x);
            animator.SetFloat("MoveY", _lastDir.y);
            animator.SetBool("IsMoving", moving);
        }
    }

    private void FixedUpdate()
    {
        // Di chuyển bằng Rigidbody2D (mượt + ăn va chạm).
        _rb.MovePosition(_rb.position + _input * moveSpeed * Time.fixedDeltaTime);
    }
}
