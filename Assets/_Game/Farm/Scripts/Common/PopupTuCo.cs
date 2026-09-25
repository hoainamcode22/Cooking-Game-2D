// ============================================================================
//  PopupTuCo — popup TU CO / DICH cho lot man hinh dien thoai (2026-09-24)
//  Gan vao TAM BANG cua popup (khong phai nen mo). Moi lan bat: dung PopupFitClamp (chung voi
//  Shop / Bang don hang) -> uu tien DICH vao trong, chi CO khi to hon vung an toan (~SafeArea).
//  Man PC 16:9 bang da vua -> KHONG doi gi (Play giong Edit mode). Chi tac dung khi man hep / co tai tho.
//  Gan / go hang loat: Tools > Farm Game > Mobile > 2 / 3. Scale + vi tri goc luu san luc gan (Edit mode).
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PopupTuCo : MonoBehaviour
{
    [Tooltip("Le chua moi mep vung an toan (don vi canvas).")]
    public float le = 16f;
    [SerializeField, HideInInspector] private Vector3 scaleGoc = Vector3.one;
    [SerializeField, HideInInspector] private Vector2 viTriGoc;
    [SerializeField, HideInInspector] private bool daLuu;

    private bool _doLai;

    /// <summary>Tool goi luc gan (Edit mode): chup scale + vi tri Sep da can tay.</summary>
    public void LuuGoc()
    {
        var rt = (RectTransform)transform;
        scaleGoc = rt.localScale;
        viTriGoc = rt.anchoredPosition;
        daLuu = true;
    }

    private void OnEnable()
    {
        if (!daLuu) LuuGoc();
        Vua();
        _doLai = true;                      // CanvasScaler / layout co the chua chay xong frame nay
    }

    private void LateUpdate()
    {
        if (!_doLai) return;
        _doLai = false;
        Vua();
    }

    private void Vua()
    {
        PopupFitClamp.VuaKhung((RectTransform)transform, scaleGoc, viTriGoc, le);
    }
}
