using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Dieu khien nhan vat nong dan doc lap cho game 2D top-down/isometric.
/// Script nay khong phu thuoc vao cac script khac trong project.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class FarmerActionController : MonoBehaviour
{
    [Header("Di chuyen")]
    [Tooltip("Toc do di chuyen cua nhan vat. Tang gia tri nay neu muon nong dan di nhanh hon.")]
    [SerializeField] private float movementSpeed = 3.5f;

    [Tooltip("Bat de nhan vat tiep tuc nhin ve huong vua di chuyen khi dung lai.")]
    [SerializeField] private bool rememberLastDirection = true;

    [Header("Phim thao tac")]
    [Tooltip("Phim tuoi cay. Co the doi thanh Space, F hoac phim khac trong Inspector.")]
    [SerializeField] private KeyCode waterKey = KeyCode.Space;

    [Tooltip("Phim nhay an mung. Co the doi thanh C hoac phim khac trong Inspector.")]
    [SerializeField] private KeyCode celebrateKey = KeyCode.C;

    [Header("Thoi gian hanh dong")]
    [Tooltip("Thoi gian khoa di chuyen khi tuoi cay. Nen khop voi do dai animation WaterPlants.")]
    [SerializeField] private float wateringDuration = 0.8f;

    [Tooltip("Thoi diem goi su kien tuoi cay sau khi bam phim. Dung de tac dong len o dat/cay trong game.")]
    [SerializeField] private float wateringEventDelay = 0.25f;

    [Tooltip("Thoi gian khoa di chuyen khi nhay an mung. Nen khop voi do dai animation Celebrate.")]
    [SerializeField] private float celebrateDuration = 0.8f;

    [Header("Animator Parameters")]
    [Tooltip("Ten parameter Float trong Animator dung cho huong ngang.")]
    [SerializeField] private string directionXParameter = "Direction X";

    [Tooltip("Ten parameter Float trong Animator dung cho huong doc.")]
    [SerializeField] private string directionYParameter = "Direction Y";

    [Tooltip("Ten parameter Float trong Animator dung cho toc do.")]
    [SerializeField] private string speedParameter = "Speed";

    [Tooltip("Ten Trigger trong Animator de phat animation tuoi cay.")]
    [SerializeField] private string waterTriggerParameter = "Water";

    [Tooltip("Ten Trigger trong Animator de phat animation nhay an mung.")]
    [SerializeField] private string celebrateTriggerParameter = "Celebrate";

    [Header("Su kien tuy bien")]
    [Tooltip("Keo tha ham xu ly vao day de tuoi o dat/cay ma khong can sua script nay.")]
    public UnityEvent OnWaterPlants;

    private Rigidbody2D body2D;
    private Animator animator;
    private Vector2 movementInput;
    private Vector2 lastDirection = Vector2.down;
    private Coroutine actionRoutine;

    private int directionXHash;
    private int directionYHash;
    private int speedHash;
    private int waterTriggerHash;
    private int celebrateTriggerHash;

    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = Mathf.Max(0f, value);
    }

    public bool IsBusy => actionRoutine != null;

    private void Awake()
    {
        body2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        CacheAnimatorHashes();
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        wateringDuration = Mathf.Max(0f, wateringDuration);
        wateringEventDelay = Mathf.Max(0f, wateringEventDelay);
        celebrateDuration = Mathf.Max(0f, celebrateDuration);
        CacheAnimatorHashes();
    }

    private void Update()
    {
        if (actionRoutine != null)
        {
            movementInput = Vector2.zero;
            UpdateAnimator(Vector2.zero);
            return;
        }

        movementInput = ReadMovementInput();

        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }

        if (movementInput.sqrMagnitude > 0.0001f)
        {
            lastDirection = movementInput.normalized;
        }

        UpdateAnimator(movementInput);

        if (WasKeyPressed(waterKey))
        {
            actionRoutine = StartCoroutine(PlayWateringRoutine());
        }
        else if (WasKeyPressed(celebrateKey))
        {
            actionRoutine = StartCoroutine(PlayCelebrateRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (actionRoutine != null)
        {
            return;
        }

        Vector2 nextPosition = body2D.position + movementInput * movementSpeed * Time.fixedDeltaTime;
        body2D.MovePosition(nextPosition);
    }

    private IEnumerator PlayWateringRoutine()
    {
        StopImmediately();
        animator.ResetTrigger(celebrateTriggerHash);
        animator.SetTrigger(waterTriggerHash);

        float eventTime = Mathf.Min(wateringEventDelay, wateringDuration);
        if (eventTime > 0f)
        {
            yield return new WaitForSeconds(eventTime);
        }

        OnWaterPlants?.Invoke();

        float remainingTime = wateringDuration - eventTime;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        actionRoutine = null;
    }

    private IEnumerator PlayCelebrateRoutine()
    {
        StopImmediately();
        animator.ResetTrigger(waterTriggerHash);
        animator.SetTrigger(celebrateTriggerHash);

        if (celebrateDuration > 0f)
        {
            yield return new WaitForSeconds(celebrateDuration);
        }

        actionRoutine = null;
    }

    private void StopImmediately()
    {
        movementInput = Vector2.zero;
        UpdateAnimator(Vector2.zero);

#if UNITY_6000_0_OR_NEWER
        body2D.linearVelocity = Vector2.zero;
#else
        body2D.velocity = Vector2.zero;
#endif
    }

    private void UpdateAnimator(Vector2 input)
    {
        if (animator == null)
        {
            return;
        }

        Vector2 facing = input.sqrMagnitude > 0.0001f ? input.normalized : lastDirection;
        float speed = input.magnitude;

        if (!rememberLastDirection && speed <= 0.0001f)
        {
            facing = Vector2.down;
        }

        animator.SetFloat(directionXHash, facing.x);
        animator.SetFloat(directionYHash, facing.y);
        animator.SetFloat(speedHash, speed);
    }

    private void CacheAnimatorHashes()
    {
        directionXHash = Animator.StringToHash(directionXParameter);
        directionYHash = Animator.StringToHash(directionYParameter);
        speedHash = Animator.StringToHash(speedParameter);
        waterTriggerHash = Animator.StringToHash(waterTriggerParameter);
        celebrateTriggerHash = Animator.StringToHash(celebrateTriggerParameter);
    }

    private Vector2 ReadMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float x = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                y += 1f;
            }

            Vector2 inputSystemMovement = new Vector2(x, y);
            if (inputSystemMovement.sqrMagnitude > 0.0001f)
            {
                return inputSystemMovement;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            vertical -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            vertical += 1f;
        }

        Vector2 directKeyMovement = new Vector2(horizontal, vertical);
        if (directKeyMovement.sqrMagnitude > 0.0001f)
        {
            return directKeyMovement;
        }

        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    private bool WasKeyPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && TryGetInputSystemKey(keyCode, out Key inputKey))
        {
            KeyControl keyControl = keyboard[inputKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetInputSystemKey(KeyCode keyCode, out Key inputKey)
    {
        // Ten KeyCode pho bien nhu Space, C, F, UpArrow trung voi Input System Key.
        return System.Enum.TryParse(keyCode.ToString(), true, out inputKey);
    }
#endif
}
