using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn lên Sickle_Icon bên trong Sickle_Bottom_Tray.
/// Khi người chơi nhấn xuống icon, kích hoạt SickleController ở world-space.
/// Toàn bộ logic gặt/harvest do SickleController xử lý — script này chỉ làm cầu nối.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class SickleTrayIcon : MonoBehaviour, IPointerDownHandler
{
    private Camera mainCam;

    private void Awake() => mainCam = Camera.main;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null) return;

        // Chuyển toạ độ màn hình → world space tại z=0
        // depth = khoảng cách camera → mặt phẳng z=0 (chuẩn 2D: camera z=-10)
        float depth  = Mathf.Abs(mainCam.transform.position.z);
        Vector3 world = mainCam.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, depth));
        world.z = 0f;

        FarmUIManager.Instance?.ShowSickleTool(world);
    }
}
