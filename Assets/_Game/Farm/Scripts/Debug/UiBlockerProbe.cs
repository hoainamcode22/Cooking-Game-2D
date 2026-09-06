using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ★ CÔNG CỤ CHẨN ĐOÁN 2026-09-03 — "cầm chuột kéo map mà không nhúc nhích"
///
/// Sếp báo: build EXE cũng vậy ⇒ KHÔNG phải lag/hiệu năng, mà là INPUT BỊ CHẶN CỨNG.
/// Bằng chứng cơ chế: <c>CameraController.cs:241</c> và <c>:333</c> đều bỏ qua thao tác kéo
/// khi <c>EventSystem.current.IsPointerOverGameObject()</c> == true. Nên chỉ cần MỘT lớp UI
/// trong suốt phủ kín màn hình mà còn bật raycast là map "chết cứng" — đúng triệu chứng.
///
/// Quét scene tĩnh cho ra 8 nghi phạm phủ full-screen + raycastTarget=1 + đang bật:
///   Tutorial_Canvas/Dim_Background · Tutorial_Canvas/Tutorial_GuideBoard
///   Canvas_Popup/MillPopup_Root/PopupRoot/Dim · Canvas_MarketPopup/Panel_Dim (3840×2160)
///   Popup_LevelUp_Township/Root_HienThi/Bg_NenToi · …/V2_TapCatcher
///   Canvas_Popup/Sickle_Bottom_Tray/BG_Image · Tutorial_Canvas/NPC_Dialog_Popup/NPC_Background
/// Không đoán trong số đó — script này CHỈ ĐỌC và in ra đích danh cái đang chặn thật.
///
/// CÁCH DÙNG: bấm Play → giữ chuột kéo map. Nếu map không nhúc nhích, Console in ngay
/// đường dẫn hierarchy của mọi UI đang nằm dưới con trỏ (thủ phạm là dòng đầu).
/// Bấm F9 để in bất cứ lúc nào. Bấm F10 để in TOÀN BỘ lớp phủ full-screen đang bật.
///
/// An toàn: chỉ Debug.Log, KHÔNG sửa/tắt gì. Tự tắt sau <see cref="MaxLogs"/> lần in để
/// không rác Console. Xoá file này là xong, không hệ nào phụ thuộc.
/// </summary>
[DisallowMultipleComponent]
public class UiBlockerProbe : MonoBehaviour
{
    private const int   MaxLogs      = 12;
    private const float LogCooldown  = 1.5f;

    private static readonly List<RaycastResult> _hits = new List<RaycastResult>(24);
    private int   _logged;
    private float _nextLog;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        GameObject go = new GameObject("~UiBlockerProbe");
        go.AddComponent<UiBlockerProbe>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[UiProbe] Đang theo dõi. Giữ chuột kéo map — nếu không nhúc nhích, tôi sẽ in " +
                  "thủ phạm ngay. F9 = in UI dưới con trỏ · F10 = in mọi lớp phủ full-screen.");
    }

    private void Update()
    {
        bool f9  = Input.GetKeyDown(KeyCode.F9);
        bool f10 = Input.GetKeyDown(KeyCode.F10);
        if (f10) { InLopPhu(); return; }

        // Đang giữ chuột (tức đang cố kéo map) mà EventSystem báo "con trỏ trên UI"
        // ⇒ CameraController sẽ bỏ qua ⇒ đây đúng lúc cần biết ai chặn.
        bool dangKeo = Input.GetMouseButton(0);
        bool trenUI  = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!f9 && !(dangKeo && trenUI)) return;
        if (_logged >= MaxLogs) return;
        if (Time.unscaledTime < _nextLog) return;
        _nextLog = Time.unscaledTime + LogCooldown;
        _logged++;

        InUiDuoiConTro(f9);
    }

    private void InUiDuoiConTro(bool thuCong)
    {
        var es = EventSystem.current;
        if (es == null) { Debug.Log("[UiProbe] Không có EventSystem trong scene."); return; }

        var ped = new PointerEventData(es) { position = Input.mousePosition };
        _hits.Clear();
        es.RaycastAll(ped, _hits);

        var sb = new StringBuilder();
        sb.AppendLine(thuCong ? "[UiProbe] F9 — UI dưới con trỏ:"
                              : "[UiProbe] ⛔ KÉO MAP BỊ CHẶN — UI dưới con trỏ (dòng đầu = thủ phạm):");
        if (_hits.Count == 0)
        {
            sb.AppendLine("   (không có UI nào — nếu map vẫn không kéo được thì nguyên nhân KHÔNG ở UI, " +
                          "báo Lead để soi ObjectDragHandler/EditMode/inputLock)");
        }
        // [FIX 2026-09-06] Main Camera co Physics2DRaycaster eventMask=Everything, nen
        // RaycastAll tra ve CA vat the world (bui cay, nha, decor). Nhung CameraController
        // va FarmInputLock chi coi hit tu GraphicRaycaster la "UI that". Ban cu in ca hai
        // ma khong phan biet => do oan cho bui cay/ngoi nha. Nay tach ro 2 nhom.
        int soUiThat = 0;
        for (int i = 0; i < _hits.Count && i < 8; i++)
        {
            GameObject go = _hits[i].gameObject;
            if (go == null) continue;
            bool laUiThat = _hits[i].module is UnityEngine.UI.GraphicRaycaster;
            if (laUiThat) soUiThat++;
            bool laNut = go.GetComponentInParent<UnityEngine.UI.Selectable>() != null;
            sb.AppendLine($"   {i + 1}. {(laUiThat ? "[UI THAT]" : "[world - KHONG chan map]")} {DuongDan(go)}");
            sb.AppendLine($"      layer={LayerMask.LayerToName(go.layer)} · nút bấm thật={(laNut ? "CÓ" : "KHÔNG")}");
        }
        if (soUiThat == 0)
        {
            sb.AppendLine("   ⇒ KHONG co UI that nao duoi con tro. Map bi chan boi CO KHOA, khong phai UI:");
            sb.AppendLine($"      FarmInputLock.BlockMapPan = {FarmInputLock.BlockMapPan}");
            sb.AppendLine($"      IsPopupOpen(popupLockCount>0) = {FarmInputLock.IsPopupOpen}");
            sb.AppendLine($"      PopupManager.IsAnyPopupOpen  = {(PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())}"
                        + $"  → {(PopupManager.Instance != null ? PopupManager.TenPopupDangMo() : "(khong co PopupManager)")}");
            sb.AppendLine($"      IsDraggingSeed={FarmInputLock.IsDraggingSeed} · IsDraggingSickle={FarmInputLock.IsDraggingSickle}"
                        + $" · IsSeedPopupOpen={FarmInputLock.IsSeedPopupOpen} · IsMarketPopupOpen={FarmInputLock.IsMarketPopupOpen}"
                        + $" · IsCookingMode={FarmInputLock.IsCookingMode} · EditMode={EditModeManager.IsEditMode}");
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>F10: liệt kê MỌI Graphic đang bật, ăn raycast, phủ ≥80% màn hình.</summary>
    private void InLopPhu()
    {
        var all = Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None);
        var sb = new StringBuilder();
        sb.AppendLine("[UiProbe] F10 — lớp phủ đang BẬT + ăn raycast + phủ ≥80% màn hình:");
        int n = 0;
        float manHinh = Screen.width * (float)Screen.height;

        for (int i = 0; i < all.Length; i++)
        {
            UnityEngine.UI.Graphic g = all[i];
            if (g == null || !g.isActiveAndEnabled || !g.raycastTarget) continue;

            RectTransform rt = g.rectTransform;
            if (rt == null) continue;

            Rect r = RectTransformUtility.PixelAdjustRect(rt, g.canvas);
            float dienTich = Mathf.Abs(r.width * rt.lossyScale.x) * Mathf.Abs(r.height * rt.lossyScale.y);
            if (manHinh <= 1f || dienTich < manHinh * 0.8f) continue;

            bool laNut = g.GetComponentInParent<UnityEngine.UI.Selectable>() != null;
            sb.AppendLine($"   • {DuongDan(g.gameObject)}  (~{dienTich / manHinh:0.0}× màn hình" +
                          $" · nút bấm thật={(laNut ? "CÓ" : "KHÔNG ⇒ NGHI")})");
            n++;
        }
        if (n == 0) sb.AppendLine("   (không có lớp phủ nào — input không bị UI chặn ở thời điểm này)");
        Debug.Log(sb.ToString());
    }

    private static string DuongDan(GameObject go)
    {
        var sb = new StringBuilder(go.name);
        Transform t = go.transform.parent;
        int guard = 0;
        while (t != null && guard++ < 12) { sb.Insert(0, t.name + "/"); t = t.parent; }
        return sb.ToString();
    }
}
