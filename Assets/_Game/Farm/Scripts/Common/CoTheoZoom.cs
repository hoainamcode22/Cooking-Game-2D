// ============================================================================
//  CoTheoZoom — icon tren map GIU CO TREN MAN HINH khi zoom (2026-09-24)
//  Zoom XA (ortho lon) -> icon to len; zoom GAN -> icon nho lai. doManh = 1 la giu dung co tren
//  man hinh, 0 la tat (co world co dinh nhu cu). Mac dinh 0.8: van to/nho nhe theo zoom cho tu nhien.
//  1 instance "cam lai" tinh he so 1 lan / khi ortho doi > 0.5 -> re, khong Update thua.
//  Tu gan cho: ReadyIcon (o dat / chau hoa san thu hoach), LockIcon. Gan tay: Tools > VFX > 8.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CoTheoZoom : MonoBehaviour
{
    [Tooltip("1 = giu dung co tren man hinh khi zoom. 0 = tat. 0.8 = to/nho nhe theo zoom.")]
    [Range(0f, 1f)] public float doManh = 0.8f;

    private Vector3 _goc;
    private bool _daChup;
    private static readonly List<CoTheoZoom> _ds = new List<CoTheoZoom>(64);
    private static float _orthoCu = -1f;
    private static float _heSo = 1f;
    private static Camera _cam;

    private void Awake() { ChupGoc(); }

    private void ChupGoc()
    {
        if (_daChup) return;
        _daChup = true;
        _goc = transform.localScale;
    }

    private void OnEnable()
    {
        ChupGoc();
        _ds.Add(this);
        Ap();
    }

    private void OnDisable()
    {
        _ds.Remove(this);
        if (_daChup) transform.localScale = _goc;
    }

    private void LateUpdate()
    {
        if (_ds.Count == 0 || _ds[0] != this) return;          // chi 1 instance tinh
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || !_cam.orthographic) return;
        float o = _cam.orthographicSize;
        if (Mathf.Abs(o - _orthoCu) < 0.5f) return;
        _orthoCu = o;
        _heSo = ZoomScaleHelper.HeSo(_cam);
        for (int i = 0; i < _ds.Count; i++) if (_ds[i] != null) _ds[i].Ap();
    }

    private void Ap()
    {
        float k = Mathf.Pow(_heSo, doManh);
        transform.localScale = new Vector3(_goc.x * k, _goc.y * k, _goc.z);
    }

    /// <summary>Gan cho moi SpriteRenderer trong scene co ten nam trong danh sach (bo qua da co).</summary>
    public static int GanTheoTen(params string[] ten)
    {
        var tap = new HashSet<string>(ten);
        int n = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr == null || !tap.Contains(sr.gameObject.name)) continue;
            if (sr.GetComponent<CoTheoZoom>() != null) continue;
            sr.gameObject.AddComponent<CoTheoZoom>();
            n++;
        }
        return n;
    }
}
