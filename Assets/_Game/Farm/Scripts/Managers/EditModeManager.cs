using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý chế độ sắp xếp (Edit Mode) farm.
/// - Toggle edit mode: bật/tắt chế độ sắp xếp
/// - Khi bật: overlay vàng nhạt, hiện text "Chế độ sắp xếp", enable ObjectDragHandler
/// - Khi tắt: bỏ overlay, ẩn text, disable ObjectDragHandler
///
/// Singleton pattern — gắn lên Systems object ở scene.
/// </summary>
public class EditModeManager : MonoBehaviour
{
	[Header("Edit Mode Visual")]
	[SerializeField] private Image overlayImage; // UI Image fullscreen, dùng tint overlay
	[SerializeField] private Color overlayColorActive = new Color(1f, 1f, 0f, 0.1f); // Vàng nhạt, alpha 0.1
	[SerializeField] private Color overlayColorInactive = Color.clear;

	[Header("Edit Mode UI")]
	[SerializeField] private Text editModeLabel; // Text "Chế độ sắp xếp"

	// ── Singleton ────────────────────────────────────────────────────────────
	private static EditModeManager instance;
	public static EditModeManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<EditModeManager>();
				if (instance == null)
					Debug.LogError("[EditModeManager] Không tìm thấy EditModeManager trong scene!");
			}
			return instance;
		}
	}

	// ── Public Static Properties ─────────────────────────────────────────────
	public static bool IsEditMode { get; private set; }

	// ── Event ────────────────────────────────────────────────────────────────
	/// <summary>Event khi Edit Mode thay đổi (true = bật, false = tắt)</summary>
	public static event System.Action<bool> OnEditModeChanged;

	// ──────────────────────────────────────────────────────────────────────────

	private void Awake()
	{
		// Singleton setup
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (instance != this)
		{
			Destroy(gameObject);
			return;
		}

		// Init trạng thái
		IsEditMode = false;
		UpdateEditModeVisuals(false);
	}

	private void Start()
	{
		// Đảm bảo overlay được gắn
		if (overlayImage == null)
		{
			Debug.LogWarning("[EditModeManager] Overlay Image không được gắn! Tìm kiếm tự động...");
			overlayImage = FindObjectOfType<Canvas>()?.GetComponentInChildren<Image>();
		}

		// Đảm bảo label được gắn
		if (editModeLabel == null)
		{
			Debug.LogWarning("[EditModeManager] Edit Mode Label không được gắn! Tìm kiếm tự động...");
			editModeLabel = FindObjectOfType<Canvas>()?.GetComponentInChildren<Text>();
		}
	}

	private void Update()
	{
		// Debug: Nhấn E để toggle Edit Mode
		if (Input.GetKeyDown(KeyCode.E))
		{
			ToggleEditMode();
		}
	}

	/// <summary>Bật/tắt chế độ sắp xếp</summary>
	public void ToggleEditMode()
	{
		Debug.Log($"[EditMode] ToggleEditMode CALLED! Current={IsEditMode}");
		var handlers = FindObjectsOfType<ObjectDragHandler>();
		Debug.Log($"[EditMode] Found {handlers.Length} ObjectDragHandler in scene");
		IsEditMode = !IsEditMode;
		UpdateEditModeVisuals(IsEditMode);
		UpdateObjectDragHandlers(IsEditMode);

		// Phát sự kiện
		OnEditModeChanged?.Invoke(IsEditMode);

		Debug.Log($"[EditModeManager] Edit Mode: {(IsEditMode ? "BẬT" : "TẮT")}");
	}

	/// <summary>Cập nhật visual khi Edit Mode bật/tắt</summary>
	private void UpdateEditModeVisuals(bool isActive)
	{
		// Cập nhật overlay
		if (overlayImage != null)
		{
			Color targetColor = isActive ? overlayColorActive : overlayColorInactive;
			overlayImage.color = targetColor;
		}

		// Cập nhật label
		if (editModeLabel != null)
		{
			editModeLabel.enabled = isActive;
			if (isActive)
				editModeLabel.text = "Chế độ sắp xếp";
		}
	}

	/// <summary>Enable/Disable ObjectDragHandler trên tất cả object di chuyển được</summary>
	private void UpdateObjectDragHandlers(bool isActive)
	{
		ObjectDragHandler[] dragHandlers = FindObjectsOfType<ObjectDragHandler>();

		foreach (var handler in dragHandlers)
		{
			// Enable/Disable script component
			handler.enabled = isActive;
		}

		Debug.Log($"[EditModeManager] {dragHandlers.Length} ObjectDragHandler(s) → {(isActive ? "enabled" : "disabled")}");
	}

	/// <summary>Bật Edit Mode (nếu chưa bật)</summary>
	public void EnableEditMode()
	{
		if (!IsEditMode)
			ToggleEditMode();
	}

	/// <summary>Tắt Edit Mode (nếu đang bật)</summary>
	public void DisableEditMode()
	{
		if (IsEditMode)
			ToggleEditMode();
	}
}

/*
╔════════════════════════════════════════════════════════════════════════════════╗
║                        HƯỚNG DẪN GẮN SCRIPT                                    ║
╠════════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║ 1. GĂN SCRIPT LÊN OBJECT NÀO?                                                  ║
║    → Systems object (hoặc GameObject trống tên "EditModeManager")             ║
║                                                                                ║
║ 2. COMPONENT CẦN THIẾT:                                                        ║
║    ✓ Không cần component nào đặc biệt                                          ║
║                                                                                ║
║ 3. INSPECTOR SETUP:                                                            ║
║    ✓ Overlay Image: Gắn UI Image fullscreen (tint overlay)                     ║
║      → Nên là CanvasGroup Image hoặc Image tại layer trên cùng                 ║
║      → Color mặc định: (1, 1, 0, 0.1) — vàng nhạt                            ║
║                                                                                ║
║    ✓ Edit Mode Label: Gắn UI Text hiển thị "Chế độ sắp xếp"                   ║
║      → Nên là Text component trong Canvas                                      ║
║      → Font lớn, màu nổi bật (ví dụ: trắng hoặc vàng)                         ║
║                                                                                ║
║ 4. INPUT:                                                                      ║
║    ✓ Nhấn E để toggle Edit Mode                                                ║
║    ✓ Hoặc gọi EditModeManager.Instance.ToggleEditMode()                        ║
║                                                                                ║
║ 5. FLOW EDIT MODE:                                                              ║
║    - Bật (E):                                                                  ║
║      1. IsEditMode = true                                                      ║
║      2. Overlay tint vàng (alpha 0.1)                                          ║
║      3. Hiện text "Chế độ sắp xếp"                                             ║
║      4. Enable tất cả ObjectDragHandler                                        ║
║      5. Event OnEditModeChanged(true) phát                                     ║
║      6. Click object → drag ngay (không cần giữ 0.5s)                          ║
║      7. Camera pan bình thường (không drag object)                             ║
║                                                                                ║
║    - Tắt (E):                                                                  ║
║      1. IsEditMode = false                                                     ║
║      2. Bỏ overlay                                                              ║
║      3. Ẩn text "Chế độ sắp xếp"                                               ║
║      4. Disable tất cả ObjectDragHandler                                       ║
║      5. Event OnEditModeChanged(false) phát                                    ║
║      6. Object không thể kéo (ObjectDragHandler bị disable)                    ║
║                                                                                ║
║ 6. INTEGRATION:                                                                ║
║    ✓ ObjectDragHandler: Thêm check Edit Mode ở Update()                        ║
║    ✓ CameraController: Khi Edit Mode ON → logic pan thay đổi                   ║
║                                                                                ║
║ 7. USAGE CODE:                                                                 ║
║    - Toggle: EditModeManager.Instance.ToggleEditMode();                        ║
║    - Check: if (EditModeManager.IsEditMode) { ... }                            ║
║    - Listen: EditModeManager.OnEditModeChanged += OnEditModeChanged;            ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
*/
