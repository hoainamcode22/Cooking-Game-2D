using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using InputTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Hệ thống drag-drop kiểu Hay Day cho các object di chuyển được (chuồng bò, chuồng gà, etc).
/// - Hoạt động khi EditModeManager.IsEditMode == true
/// - Khi Edit Mode OFF: script bị disable (tất cả ObjectDragHandler)
/// - Khi Edit Mode ON: click object → drag ngay (threshold 15px để phân biệt tap vs drag)
/// - Khi drag: object scale up, hiện shadow, snap vào grid 50x50
/// - Highlight ô hợp lệ (xanh) / không hợp lệ (đỏ)
/// - Thả xuống: nếu hợp lệ → đặt, không → bounce về vị trí cũ
///
/// Gắn script này lên object di chuyển được (chuồng bò, chuồng gà, chuồng heo, kho, bếp, chợ).
/// Cần component: BoxCollider2D (hoặc CircleCollider2D)
/// </summary>
public class ObjectDragHandler : MonoBehaviour
{
	[Header("Drag Detection")]
	[SerializeField] private float dragThreshold = 15f; // Pixel tối thiểu để tính là drag (phân biệt tap vs drag)

	[Header("Drag Visual")]
	[SerializeField] private float dragScaleMultiplier = 1.1f; // Scale khi drag (1.1x)
	[SerializeField] private SpriteRenderer shadowSprite; // Shadow dưới object khi drag
	[SerializeField] private float shadowAlphaActive = 0.6f; // Alpha của shadow khi drag

	[Header("Grid Snap")]
	[SerializeField] private float gridSize = 50f; // Kích thước grid tương ứng với tilemap

	[Header("Placement Validation")]
	[SerializeField] private Vector2 collisionCheckSize = new Vector2(50f, 50f); // Kích thước check overlap
	[SerializeField] private float collisionCheckPadding = 0.1f; // Padding extra để check
	[SerializeField] private LayerMask obstacleLayerMask; // Layer của các object khác để check va chạm
	[SerializeField] private LayerMask groundLayerMask; // Layer của đất có thể đặt object

	[Header("Visual Feedback")]
	[SerializeField] private SpriteRenderer placementIndicator; // Sprite hiển thị grid ô đặt (tạm)
	[SerializeField] private Color validPlacementColor = Color.green; // Màu xanh cho ô hợp lệ
	[SerializeField] private Color invalidPlacementColor = Color.red; // Màu đỏ cho ô không hợp lệ

	[Header("Bounce Animation")]
	[SerializeField] private float bounceReturnDuration = 0.3f; // Thời gian bounce về vị trí cũ
	[SerializeField] private float bounceHeight = 0.2f; // Độ cao bounce khi trả về

	// ── Biến static toàn cầu ────────────────────────────────────────────────
	public static bool IsDraggingObject { get; private set; }

	// ── Biến nội bộ ──────────────────────────────────────────────────────────
	private Camera mainCamera;
	private Collider2D objectCollider;
	private Vector3 originalPosition;
	private Vector3 originalScale;
	private Color originalSpriteColor;
	private SpriteRenderer objectSprite;

	// Drag tracking
	private bool pressHeld;
	private bool isDraggingNow;
	private Vector2 pressStartScreenPos;
	private Vector3 dragStartWorldPos;
	private Vector3 currentDragWorldPos;

	// ──────────────────────────────────────────────────────────────────────────

	private void OnEnable()
	{
		EnhancedTouchSupport.Enable();
	}

	private void OnDisable()
	{
		EnhancedTouchSupport.Disable();
		// Nếu script bị disable khi đang drag → cleanup
		if (isDraggingNow)
		{
			EndDragging();
		}
	}

	private void Awake()
	{
		mainCamera = Camera.main;
		objectCollider = GetComponent<Collider2D>();
		objectSprite = GetComponent<SpriteRenderer>();

		if (objectCollider == null)
			Debug.LogError($"[ObjectDragHandler] {name} không có Collider2D!");

		originalPosition = transform.position;
		originalScale = transform.localScale;
		originalSpriteColor = objectSprite != null ? objectSprite.color : Color.white;

		// Ẩn shadow ban đầu
		if (shadowSprite != null)
			SetShadowAlpha(0f);
	}

	private void Update()
	{
		// ────── CHECK: Edit Mode? Nếu OFF → không xử lý input ──────
		if (!EditModeManager.IsEditMode)
			return;

		// Chọn nhánh input: Editor/Desktop dùng chuột, mobile dùng cảm ứng
#if UNITY_EDITOR || UNITY_STANDALONE
		HandleMouseInput();
#else
		HandleTouchInput();
#endif
	}

	/// <summary>Xử lý input chuột (Editor / Desktop)</summary>
	private void HandleMouseInput()
	{
		if (EditModeManager.IsEditMode)
			Debug.Log($"[DragHandler] {name} Update | pressHeld={pressHeld} | isDragging={isDraggingNow}");

		var mouse = Mouse.current;
		if (mouse == null)
			return;

		// ── BƯỚC 1: Nhấn chuột xuống ──
		if (mouse.leftButton.wasPressedThisFrame)
		{
			Vector2 screenPos = mouse.position.ReadValue();
			if (IsPointerOverThisObject(screenPos))
			{
				pressHeld = true;
				pressStartScreenPos = screenPos;
				dragStartWorldPos = ScreenToWorldPoint(screenPos);
			}
		}

		// ── BƯỚC 2: Đang giữ — kiểm tra đã di chuyển đủ dragThreshold chưa ──
		if (pressHeld && !isDraggingNow && mouse.leftButton.isPressed)
		{
			float movedPixels = Vector2.Distance(mouse.position.ReadValue(), pressStartScreenPos);
			if (movedPixels > dragThreshold)
			{
				StartDragging();
				isDraggingNow = true;
			}
		}

		// ── BƯỚC 3: Đang drag ──
		if (isDraggingNow && mouse.leftButton.isPressed)
		{
			Vector2 currentScreenPos = mouse.position.ReadValue();
			currentDragWorldPos = ScreenToWorldPoint(currentScreenPos);
			UpdateDragPosition();
		}

		// ── BƯỚC 4: Thả chuột ──
		if (mouse.leftButton.wasReleasedThisFrame)
		{
			if (isDraggingNow)
			{
				EndDragging();
				isDraggingNow = false;
			}

			pressHeld = false;
			pressStartScreenPos = Vector2.zero;
		}
	}

	/// <summary>Xử lý input cảm ứng (Mobile)</summary>
	private void HandleTouchInput()
	{
		var activeTouches = InputTouch.activeTouches;

		if (activeTouches.Count == 1)
		{
			var touch = activeTouches[0];

			// ── BƯỚC 1: Ngón chạm xuống ──
			if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
			{
				if (IsPointerOverThisObject(touch.screenPosition))
				{
					pressHeld = true;
					pressStartScreenPos = touch.screenPosition;
					dragStartWorldPos = ScreenToWorldPoint(touch.screenPosition);
				}
			}

			// ── BƯỚC 2: Đang giữ — kiểm tra đã di chuyển đủ dragThreshold chưa ──
			if (pressHeld && !isDraggingNow &&
			    (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
			     touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
			{
				float movedPixels = Vector2.Distance(touch.screenPosition, pressStartScreenPos);
				if (movedPixels > dragThreshold)
				{
					StartDragging();
					isDraggingNow = true;
				}
			}

			// ── BƯỚC 3: Đang drag ──
			if (isDraggingNow && (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
			                       touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
			{
				currentDragWorldPos = ScreenToWorldPoint(touch.screenPosition);
				UpdateDragPosition();
			}

			// ── BƯỚC 4: Ngón nhấc lên ──
			if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
			    touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
			{
				if (isDraggingNow)
				{
					EndDragging();
					isDraggingNow = false;
				}

				pressHeld = false;
				pressStartScreenPos = Vector2.zero;
			}
		}
		else
		{
			// Nếu có >1 ngón → huỷ drag
			if (isDraggingNow)
			{
				EndDragging();
				isDraggingNow = false;
			}

			pressHeld = false;
			pressStartScreenPos = Vector2.zero;
		}
	}

	/// <summary>Bắt đầu drag: scale up, hiện shadow, set flag</summary>
	private void StartDragging()
	{
		IsDraggingObject = true;
		originalPosition = transform.position;
		originalScale = transform.localScale;

		// Scale up object
		transform.localScale = originalScale * dragScaleMultiplier;

		// Hiện shadow
		if (shadowSprite != null)
			SetShadowAlpha(shadowAlphaActive);

		Debug.Log($"[ObjectDragHandler] Bắt đầu drag: {name}");
	}

	/// <summary>Cập nhật vị trí khi drag: snap vào grid, highlight ô, follow pointer</summary>
	private void UpdateDragPosition()
	{
		// Di chuyển object theo pointer, snap vào grid
		Vector3 snappedPos = SnapToGrid(currentDragWorldPos);
		transform.position = snappedPos;

		// Kiểm tra vị trí có hợp lệ không
		bool isValidPlacement = IsValidPlacement(snappedPos);

		// Highlight ô
		UpdatePlacementIndicator(snappedPos, isValidPlacement);

		// Đổi màu object dựa trên hợp lệ
		if (objectSprite != null)
		{
			objectSprite.color = isValidPlacement ? originalSpriteColor : new Color(1f, 0.5f, 0.5f, 1f);
		}
	}

	/// <summary>Kết thúc drag: nếu hợp lệ → đặt, không → bounce về vị trí cũ</summary>
	private void EndDragging()
	{
		Vector3 currentPos = transform.position;
		bool isValid = IsValidPlacement(currentPos);

		// Scale về 1x
		transform.localScale = originalScale;

		// Ẩn shadow
		if (shadowSprite != null)
			SetShadowAlpha(0f);

		// Ẩn placement indicator
		if (placementIndicator != null)
			placementIndicator.enabled = false;

		// Trả màu sprite về bình thường
		if (objectSprite != null)
			objectSprite.color = originalSpriteColor;

		if (isValid)
		{
			// Vị trí hợp lệ → giữ lại
			originalPosition = currentPos;
			Debug.Log($"[ObjectDragHandler] Đặt {name} thành công tại {currentPos}");
		}
		else
		{
			// Vị trí không hợp lệ → bounce về vị trí cũ
			StartCoroutine(BounceBackAnimation(originalPosition));
			Debug.Log($"[ObjectDragHandler] {name} bounce về vị trí cũ");
		}

		IsDraggingObject = false;
	}

	/// <summary>Snap vị trí hiện tại vào grid 50x50</summary>
	private Vector3 SnapToGrid(Vector3 position)
	{
		float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
		float snappedY = Mathf.Round(position.y / gridSize) * gridSize;
		return new Vector3(snappedX, snappedY, position.z);
	}

	/// <summary>Kiểm tra vị trí có hợp lệ không (không overlap với object khác, nằm trên đất)</summary>
	private bool IsValidPlacement(Vector3 position)
	{
		// Check overlap với object khác
		Collider2D[] overlaps = Physics2D.OverlapBoxAll(
			position,
			collisionCheckSize,
			0f,
			obstacleLayerMask
		);

		// Lọc bỏ chính object này
		foreach (var collider in overlaps)
		{
			if (collider.gameObject == gameObject)
				continue;

			// Nếu có object khác → vị trí không hợp lệ
			return false;
		}

		// Check nằm trên đất hay không
		Collider2D groundCheck = Physics2D.OverlapBox(
			position,
			collisionCheckSize,
			0f,
			groundLayerMask
		);

		return groundCheck != null;
	}

	/// <summary>Cập nhật hiển thị ô đặt: xanh (hợp lệ) / đỏ (không hợp lệ)</summary>
	private void UpdatePlacementIndicator(Vector3 position, bool isValid)
	{
		if (placementIndicator == null)
			return;

		placementIndicator.enabled = true;
		placementIndicator.transform.position = position;
		placementIndicator.color = isValid ? validPlacementColor : invalidPlacementColor;
	}

	/// <summary>Animation bounce: trả object về vị trí cũ với animation</summary>
	private System.Collections.IEnumerator BounceBackAnimation(Vector3 targetPos)
	{
		Vector3 startPos = transform.position;
		float elapsedTime = 0f;

		while (elapsedTime < bounceReturnDuration)
		{
			elapsedTime += Time.deltaTime;
			float progress = elapsedTime / bounceReturnDuration;

			// Lerp vị trí
			Vector3 lerpPos = Vector3.Lerp(startPos, targetPos, progress);

			// Bounce effect: chuyển động cung (arc)
			float bounceArc = Mathf.Sin(progress * Mathf.PI) * bounceHeight;
			lerpPos.y += bounceArc;

			transform.position = lerpPos;

			yield return null;
		}

		// Đảm bảo position cuối cùng chính xác
		transform.position = targetPos;
	}

	/// <summary>Kiểm tra con trỏ có đang chạm object này không</summary>
	private bool IsPointerOverThisObject(Vector2 screenPos)
	{
		if (mainCamera == null) mainCamera = Camera.main;

		// Dùng raycast 2D thay vì bounds.Contains
		Vector3 worldPos = mainCamera.ScreenToWorldPoint(
			new Vector3(screenPos.x, screenPos.y, 0f));

		RaycastHit2D hit = Physics2D.Raycast(
			new Vector2(worldPos.x, worldPos.y),
			Vector2.zero,
			0f);

		Debug.Log($"[DragHandler] Raycast at {worldPos} → hit={hit.collider?.name}");

		return hit.collider != null && hit.collider.gameObject == gameObject;
	}

	/// <summary>Chuyển toạ độ màn hình sang world space</summary>
	private Vector3 ScreenToWorldPoint(Vector2 screenPos)
	{
		if (mainCamera == null)
			mainCamera = Camera.main;

		Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
		worldPos.z = transform.position.z;
		return worldPos;
	}

	/// <summary>Cập nhật alpha của shadow sprite</summary>
	private void SetShadowAlpha(float alpha)
	{
		if (shadowSprite == null)
			return;

		Color shadowColor = shadowSprite.color;
		shadowColor.a = alpha;
		shadowSprite.color = shadowColor;
	}
}

/*
╔════════════════════════════════════════════════════════════════════════════════╗
║                         HƯỚNG DẪN GẮN SCRIPT                                   ║
╠════════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║ 1. GĂN SCRIPT LÊN OBJECT NÀO?                                                  ║
║    - Chuồng bò (Cow Pen)         - Chuồng gà (Chicken Pen)                     ║
║    - Chuồng heo (Pig Pen)        - Kho (Warehouse)                             ║
║    - Bếp (Kitchen)               - Chợ (Market)                                ║
║    - Bất kỳ object nào di chuyển được                                          ║
║                                                                                ║
║ 2. COMPONENT CẦN THIẾT:                                                        ║
║    ✓ BoxCollider2D (hoặc CircleCollider2D) — không phải Trigger               ║
║    ✓ SpriteRenderer — để hiển thị object                                       ║
║    ✓ (Optional) SpriteRenderer riêng cho shadow dưới object                    ║
║                                                                                ║
║ 3. LAYER & TAG CẦN SETUP:                                                      ║
║    ✓ Tạo Layer "Obstacle" — gắn lên tất cả object di chuyển được               ║
║    ✓ Tạo Layer "Ground" — gắn lên tilemap/đất                                 ║
║    ✓ Assign layer vào "Obstacle Layer Mask" và "Ground Layer Mask"             ║
║                                                                                ║
║ 4. INSPECTOR SETUP:                                                            ║
║    ✓ Drag Threshold: 15px (phân biệt tap vs drag)                             ║
║    ✓ Drag Scale Multiplier: 1.1x                                               ║
║    ✓ Shadow Sprite: (gắn sprite shadow nếu có)                                ║
║    ✓ Grid Size: 50 (đồng bộ với tilemap)                                      ║
║    ✓ Collision Check Size: (50, 50)                                           ║
║    ✓ Obstacle Layer Mask: select "Obstacle"                                    ║
║    ✓ Ground Layer Mask: select "Ground"                                        ║
║    ✓ Valid/Invalid Placement Color: Xanh/Đỏ                                    ║
║    ✓ Bounce Return Duration: 0.3s                                              ║
║                                                                                ║
║ 5. FLOW EDIT MODE:                                                             ║
║    - Khi Edit Mode OFF → script disabled (EditModeManager)                     ║
║    - Khi Edit Mode ON → click object → di chuyển 15px → bắt đầu drag          ║
║    - Camera pan tự động tắt khi IsDraggingObject == true                       ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
*/
