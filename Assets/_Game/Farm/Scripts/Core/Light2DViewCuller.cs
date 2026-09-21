using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// [PERF 2026-09-21] CULL LIGHT2D THEO KHUNG CAMERA.
/// Profiler: Light2D.LateUpdate 520 calls / 1.6ms CPU + moi den ve vao light texture MOI FRAME (GPU).
/// Nguon: Prefab_Pinetree (93) + Prefab_Bush (86) long shadow.prefab co 2 Light2D lam bong => 358 den,
/// + log 39, den nha, ... Phan lon nam NGOAI man hinh. Den ngoai khung => tat component (khong render,
/// khong LateUpdate). Kiem tra 5 lan/giay, 520 phep so hinh chu nhat ~0.05ms. Bo qua den Global.
/// Tat co che: Light2DViewCuller.Bat = false.
/// </summary>
public class Light2DViewCuller : MonoBehaviour
{
    public static bool Bat = true;
    private const float NHIP = 0.2f;
    /// <summary>Le them quanh khung (ti le theo be rong khung) de den sat mep khong nhap nhay.</summary>
    private const float LE_TI_LE = 0.35f;

    private static Light2DViewCuller _inst;
    private readonly List<Light2D> _dens = new List<Light2D>(600);
    private readonly List<float>   _banKinh = new List<float>(600);
    private Camera _cam;
    private float _lanKe;
    private bool  _canGom = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void KhoiTao()
    {
        if (_inst != null) return;
        var go = new GameObject("~Light2DViewCuller");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<Light2DViewCuller>();
        SceneManager.sceneLoaded += (s, m) => { if (_inst != null) _inst._canGom = true; };
    }

    /// <summary>Goi khi code sinh them den luc chay (khong bat buoc; culler tu gom lai moi 5s).</summary>
    public static void YeuCauGomLai() { if (_inst != null) _inst._canGom = true; }

    private float _lanGom;

    private void Gom()
    {
        _dens.Clear(); _banKinh.Clear();
        var tat = FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < tat.Length; i++)
        {
            var l = tat[i];
            if (l == null || l.lightType == Light2D.LightType.Global) continue;
            _dens.Add(l);
            _banKinh.Add(UocBanKinh(l));
        }
        _canGom = false; _lanGom = Time.unscaledTime;
    }

    private static float UocBanKinh(Light2D l)
    {
        var sc = l.transform.lossyScale; float k = Mathf.Max(Mathf.Abs(sc.x), Mathf.Abs(sc.y));
        switch (l.lightType)
        {
            case Light2D.LightType.Point:  return l.pointLightOuterRadius * k;
            case Light2D.LightType.Sprite:
            {
                var sp = l.lightCookieSprite;
                float r = sp != null ? Mathf.Max(sp.bounds.extents.x, sp.bounds.extents.y) : 1f;
                return r * k * 1.2f;
            }
            default: // Freeform / Parametric: uoc theo shapePath
            {
                var path = l.shapePath; float r = 1f;
                if (path != null) for (int i = 0; i < path.Length; i++) r = Mathf.Max(r, Mathf.Abs(path[i].x), Mathf.Abs(path[i].y));
                return r * k * 1.2f;
            }
        }
    }

    private void LateUpdate()
    {
        if (!Bat) return;
        float gio = Time.unscaledTime;
        if (gio - _lanKe < NHIP) return;
        _lanKe = gio;

        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        if (_canGom || gio - _lanGom > 5f) Gom();
        if (_dens.Count == 0) return;

        float h = _cam.orthographicSize, w = h * _cam.aspect;
        Vector3 c = _cam.transform.position;
        float le = w * LE_TI_LE;
        float xMin = c.x - w - le, xMax = c.x + w + le, yMin = c.y - h - le, yMax = c.y + h + le;

        for (int i = 0; i < _dens.Count; i++)
        {
            var l = _dens[i];
            if (l == null) continue;
            Vector3 p = l.transform.position; float r = _banKinh[i];
            bool thay = p.x + r >= xMin && p.x - r <= xMax && p.y + r >= yMin && p.y - r <= yMax;
            if (l.enabled != thay) l.enabled = thay;
        }
    }

    private void OnDestroy()
    {
        // Tra lai trang thai bat cho moi den de khong "mat den" khi component bi huy.
        for (int i = 0; i < _dens.Count; i++) if (_dens[i] != null) _dens[i].enabled = true;
    }
}
