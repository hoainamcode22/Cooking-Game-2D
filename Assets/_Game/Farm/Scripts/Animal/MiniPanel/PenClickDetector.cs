using UnityEngine;
using UnityEngine.InputSystem;

public class PenClickDetector : MonoBehaviour
{
    [SerializeField] private PenMiniPanelUI miniPanel;
    [SerializeField] private Camera         mainCamera;
    [SerializeField] private Collider2D     targetCollider;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = FindCameraFromSiblings() ?? Camera.main;

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

    }

    // Lấy camera từ sibling component (CowPenClickOpen/PigPenClickOpen…) đã được gán sẵn trong Inspector
    private Camera FindCameraFromSiblings()
    {
        foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
        {
            if (mb == this) continue;
            var field = mb.GetType().GetField("mainCamera",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) continue;
            var cam = field.GetValue(mb) as Camera;
            if (cam != null)
            {
                return cam;
            }
        }
        return null;
    }

    private void Start()
    {
    }

    // =====================================================================
    //  [2026-09-25] VUNG BAM = BAO LOI cua hinh hang rao (BarnSprite): cham vao bat ky cho nao
    //  cua chuong (coc, thanh go, ben trong) deu trung. Tinh tu luoi sprite, khong can collider moi,
    //  tu dung khi chuong xoay / keo di / doi hinh.
    // =====================================================================
    private SpriteRenderer _hangRao;
    private Sprite _spriteDaTinh;
    private Vector2[] _vo;

    private static Transform TimSau(Transform t, string ten)
    {
        if (t.name == ten) return t;
        for (int i = 0; i < t.childCount; i++) { var r = TimSau(t.GetChild(i), ten); if (r != null) return r; }
        return null;
    }

    private bool TrongVungChuong(Vector2 w)
    {
        if (_hangRao == null)
        {
            var t = TimSau(transform, "BarnSprite");
            if (t != null) _hangRao = t.GetComponent<SpriteRenderer>();
            if (_hangRao == null) return false;
        }
        if (!_hangRao.enabled || !_hangRao.gameObject.activeInHierarchy || _hangRao.sprite == null) return false;
        if (_spriteDaTinh != _hangRao.sprite) { _spriteDaTinh = _hangRao.sprite; _vo = BaoLoi(_hangRao.sprite.vertices); }
        if (_vo == null || _vo.Length < 3) return false;
        Vector2 p = _hangRao.transform.InverseTransformPoint(new Vector3(w.x, w.y, _hangRao.transform.position.z));
        if (_hangRao.flipX) p.x = -p.x;
        if (_hangRao.flipY) p.y = -p.y;
        // da giac loi, dinh xep nguoc chieu kim dong ho: diem nam ben TRAI moi canh
        for (int i = 0; i < _vo.Length; i++)
        {
            Vector2 a = _vo[i], b = _vo[(i + 1) % _vo.Length];
            if ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) < 0f) return false;
        }
        return true;
    }

    /// <summary>Bao loi (monotone chain), tra ve dinh nguoc chieu kim dong ho.</summary>
    private static Vector2[] BaoLoi(Vector2[] ds)
    {
        if (ds == null || ds.Length < 3) return null;
        var d = (Vector2[])ds.Clone();
        System.Array.Sort(d, (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        var h = new Vector2[d.Length * 2];
        int k = 0;
        for (int i = 0; i < d.Length; i++)
        {
            while (k >= 2 && Cheo(h[k - 2], h[k - 1], d[i]) <= 0f) k--;
            h[k++] = d[i];
        }
        for (int i = d.Length - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && Cheo(h[k - 2], h[k - 1], d[i]) <= 0f) k--;
            h[k++] = d[i];
        }
        var kq = new Vector2[Mathf.Max(0, k - 1)];
        System.Array.Copy(h, kq, kq.Length);
        return kq;
    }

    private static float Cheo(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    private void Update()
    {
        if (!TryGetPointerScreenPos(out Vector2 screenPos)) return;
        TryOpenPanel(screenPos);
    }

    private static bool TryGetPointerScreenPos(out Vector2 screenPos)
    {
        screenPos = default;

        bool newApi = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool oldApi = Input.GetMouseButtonDown(0);

        // [FIX 2026-09-06 vong3] Cho nay truoc kia la "if (newApi || oldApi)" KHONG CO THAN
        // (Debug.Log bi script don log xoa mat), nen no nuot luon cau "if (newApi)" ngay duoi
        // lam than cua no. Ket qua chay tinh co van dung, nhung day la cai bay: them bat ky
        // dong nao vao giua se doi logic. Da bo han cai if rong do.
        if (newApi)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (oldApi)
        {
            screenPos = Input.mousePosition;
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }

    private void TryOpenPanel(Vector2 screenPos)
    {
        if (mainCamera == null || targetCollider == null || miniPanel == null)
        {
            return;
        }

        // [FIX 2026-09-06 vong3] Kiem TRUNG CHUONG truoc, roi moi kiem cong khoa input.
        // OverlapPoint khong co tac dung phu nen doi cho la an toan; doi de chi ghi log khi
        // nguoi choi THUC SU bam trung chuong nay (khong spam Console moi cu click man hinh).
        Vector3 worldPt = mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2  = new Vector2(worldPt.x, worldPt.y);
        bool hit = targetCollider.OverlapPoint(world2) || TrongVungChuong(world2);

        if (!hit) return;

        // [2026-09-25] Dang mo popup (Settings, Kho...) ma cham vao popup -> khong mo chuong phia sau
        if (WorldClickGuard.ConTroTrenPopup(screenPos)) return;

        // [FIX 2026-09-06 vong8] Nguoi choi vua NHAN san pham bang bong bong trong frame nay.
        // Thu hoach xong state ve Idle ngay, nen neu khong chan thi CHINH cu click do se bi
        // hieu la "bam vao chuong dang doi" va mo tiep khay cho an - dung 1 cu bam ra 2 viec.
        if (PenMiniPanelUI.VuaThuBangBongBong)
        {
            Debug.Log("[PenClick] '" + name + "': bo qua, vua nhan san pham bang bong bong frame nay.");
            return;
        }

        if (FarmInputLock.BlockWorldClickBySceneOrPopup)
        {
            Debug.Log("[PenClick] '" + name + "': trung chuong nhung BI CHAN (BySceneOrPopup). cooking=" + FarmInputLock.IsCookingMode + " popupLock=" + FarmInputLock.IsPopupOpen + " keoHat=" + FarmInputLock.IsDraggingSeed + " keoLiem=" + FarmInputLock.IsDraggingSickle + " seedPopup=" + FarmInputLock.IsSeedPopupOpen + " market=" + FarmInputLock.IsMarketPopupOpen + " editMode=" + EditModeManager.IsEditMode);
            return;
        }

        if (TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial)
        {
            if (!TutorialManager.Instance.CurrentStepAllowsPenInteraction())
            {
                Debug.Log("[PenClick] '" + name + "': bo qua vi dang chay Tutorial buoc khac.");
                return;
            }
        }

        Debug.Log("[PenClick] '" + name + "': TRUNG chuong. state=" + miniPanel.CurrentState + " panelDangMo=" + miniPanel.IsPanelOpen());

        if (miniPanel.CurrentState == PenMiniPanelUI.PenState.Processing)
        {
            var popup = PenProcessPopupUI.Instance ?? FindFirstObjectByType<PenProcessPopupUI>(FindObjectsInactive.Include);
            if (popup == null)
            {
                var go = new GameObject("PenProcessPopupUI_Host", typeof(PenProcessPopupUI));
                popup = go.GetComponent<PenProcessPopupUI>();
            }
            if (popup != null)
            {
                popup.Open(miniPanel);
                return;
            }
        }

        if (miniPanel.IsPanelOpen())
        {
            Debug.Log("[PenClick] '" + name + "': panel CUA CHINH chuong nay dang mo => dong lai (toggle).");
            miniPanel.ClosePanel();
            return;
        }

        Debug.Log("[PenClick] '" + name + "': goi OpenPanel().");
        miniPanel.OpenPanel();
    }
}
