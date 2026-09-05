using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FarmInputLock
{
    /// <summary>True while the seed selection popup is visible.</summary>
    public static bool IsSeedPopupOpen  { get; set; }

    /// <summary>True while the player is dragging a seed icon.</summary>
    public static bool IsDraggingSeed   { get; set; }

    /// <summary>True while the player is dragging the sickle tool.</summary>
    public static bool IsDraggingSickle { get; set; }

    /// <summary>True while the generated Market popup is visible.</summary>
    public static bool IsMarketPopupOpen { get; set; }

    /// <summary>True when player is in Cooking scene / Cooking mode.</summary>
    public static bool IsCookingMode
    {
        get
        {
            if (FarmUIManager.Instance != null && FarmUIManager.Instance.IsCookingMode) return true;
            // [FIX 2026-09-04] Ten scene Bep THAT la "SampleScene" (FarmUIManager.cookingSceneName),
            // khong phai "SCN_Cooking" — scene do khong ton tai trong project ⇒ nhanh du phong nay
            // truoc day la DEAD CODE, khong bao gio dung. Nay sua cho dung de con luoi an toan
            // khi FarmUIManager.Instance tam thoi null.
            var cookingScene = SceneManager.GetSceneByName("SampleScene");
            if (cookingScene.IsValid() && cookingScene.isLoaded) return true;
            return false;
        }
    }

    /// <summary>True when world object clicks should be completely blocked (popups, UI, cooking, edit mode).</summary>
    public static bool BlockWorldInteraction
    {
        get
        {
            if (IsCookingMode) return true;
            if (popupLockCount > 0 || IsPopupOpen) return true;
            if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return true;
            if (IsDraggingSeed || IsDraggingSickle) return true;
            if (IsSeedPopupOpen || IsMarketPopupOpen) return true;
            if (EditModeManager.IsEditMode) return true;
            if (ConTroTrenUiThat()) return true;
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  [FIX 2026-09-04] Vi sao KHONG duoc dung EventSystem.IsPointerOverGameObject()
    //
    //  Main Camera co component Physics2DRaycaster voi eventMask = Everything
    //  (Main Camera.prefab:101, m_Bits 4294967295). Vi vay IsPointerOverGameObject()
    //  tra TRUE khi con tro nam tren BAT KY Collider2D nao trong the gioi — tuc la
    //  DUNG LUC nguoi choi bam vao chuong/ruong/nha. Dung no lam hang rao cho world-click
    //  se chan luon CHINH cai click hop le do (bang chung do tai cho: UiBlockerProbe bao
    //  "BoatSystem/Dock_02/LockUI" — mot Collider2D — la UI dang che con tro).
    //
    //  Ham duoi chi tinh la "tren UI" khi hit den tu GraphicRaycaster (Canvas + Graphic).
    //  Hit tu Physics2DRaycaster (va cham world) duoc BO QUA.
    // ═══════════════════════════════════════════════════════════════════════════
    private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>
        _uiHits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(16);

    public static bool ConTroTrenUiThat()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        Vector2 pos;
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current;
        pos = m != null ? m.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        pos = (Vector2)Input.mousePosition;
#endif
        var data = new UnityEngine.EventSystems.PointerEventData(es) { position = pos };
        _uiHits.Clear();
        es.RaycastAll(data, _uiHits);
        for (int i = 0; i < _uiHits.Count; i++)
            if (_uiHits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }

    /// <summary>
    /// [MOI 2026-09-04] Cong danh RIENG cho cac handler OnMouseDown/OnMouseUpAsButton.
    /// Giong BlockWorldInteraction nhung KHONG kiem UI duoi con tro — vi OnMouseDown chi
    /// no ra khi con tro DANG nam tren collider cua chinh vat do; kiem them se tu chan minh.
    /// Dung cai nay de chan click xuyen khi: dang o Bep (scene phu load additive),
    /// dang mo popup, dang keo hat/liem, hoac dang o Edit Mode.
    /// </summary>
    public static bool BlockWorldClickBySceneOrPopup
    {
        get
        {
            if (IsCookingMode) return true;
            if (popupLockCount > 0 || IsPopupOpen) return true;
            if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return true;
            if (IsDraggingSeed || IsDraggingSickle) return true;
            if (IsSeedPopupOpen || IsMarketPopupOpen) return true;
            if (EditModeManager.IsEditMode) return true;
            return false;
        }
    }

    private static int popupLockCount;
    private static int suppressWorldClickUntilFrame = -1;

    public static bool IsPopupOpen => popupLockCount > 0;

    // Resets all flags when entering Play mode (SubsystemRegistration = rất sớm, trước scene đầu tiên)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitOnPlayMode()
    {
        ResetAll();

        // Đăng ký callback reset khi mỗi scene mới được load trong phiên chơi
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetAll();
    }

    /// <summary>Reset tất cả flag về trạng thái mặc định (không chặn input).</summary>
    public static void ResetAll()
    {
        IsSeedPopupOpen  = false;
        IsDraggingSeed   = false;
        IsDraggingSickle = false;
        IsMarketPopupOpen = false;
        popupLockCount = 0;
        suppressWorldClickUntilFrame = -1;
    }

    /// <summary>True when map panning should be blocked.</summary>
    public static bool BlockMapPan
    {
        get
        {
            if (popupLockCount > 0 || IsPopupOpen)
                return true;

            if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
                return true;

            if (IsDraggingSeed || IsDraggingSickle)
                return true;

            if (IsSeedPopupOpen || IsMarketPopupOpen)
                return true;

            return false;
        }
    }

    /// <summary>True when map zoom should be blocked (e.g. sickle/harvest mode active).</summary>
    public static bool BlockMapZoom => IsDraggingSickle;

    public static void RegisterPopupOpen()
    {
        popupLockCount++;
    }

    public static void RegisterPopupClose()
    {
        if (popupLockCount > 0)
            popupLockCount--;

        SuppressWorldClickForCurrentFrame();
    }

    public static void SuppressWorldClickForCurrentFrame()
    {
        suppressWorldClickUntilFrame = Mathf.Max(suppressWorldClickUntilFrame, Time.frameCount);
    }

    private static bool IsWorldClickSuppressed => Time.frameCount <= suppressWorldClickUntilFrame;

    public static void SetPopupRaycastBlock(GameObject popupRoot, bool isBlocking)
    {
        if (popupRoot == null)
            return;

        if (isBlocking)
        {
            if (popupRoot.GetComponent<UIRaycastBlocker>() == null)
                popupRoot.AddComponent<UIRaycastBlocker>();

            Image image = popupRoot.GetComponent<Image>();
            if (image == null && popupRoot.GetComponent<RectTransform>() != null)
            {
                image = popupRoot.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.001f);
            }

            if (image != null)
                image.raycastTarget = true;
        }

        CanvasGroup canvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = isBlocking;
            canvasGroup.interactable = isBlocking;
        }
        else if (isBlocking)
        {
            canvasGroup = popupRoot.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }
}
