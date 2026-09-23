using System.Collections;
using UnityEngine;

/// <summary>
/// Hieu ung "dang chon" cho o dat / chau hoa khi cham vao (2026-09-23).
///  - Mot ban sao sprite nen (GroundSprite) ve bang material UNLIT, mau vang am, nhap nhay alpha
///    0.25 -> 0.6 trong 0.6s roi mo dan: ban unlit khong bi Light2D lam toi nen ban dem van sang.
///  - Nen o dat "nay" nhe 1.0 -> 1.06 -> 1.0 cho cam giac bam trung.
/// Chon o khac thi o cu tat ngay (chi 1 o sang mot luc). Dung unscaledDeltaTime.
/// </summary>
public class SelectionGlowFX : MonoBehaviour
{
    public Color mauSang = new Color(1f, 0.93f, 0.55f, 1f);
    public float thoiGianNhay = 0.6f;
    public float thoiGianTat = 0.25f;
    public float nayScale = 0.06f;

    private static SelectionGlowFX _dangSang;
    private static Material _matUnlit;
    private SpriteRenderer _nguon, _sang;
    private Coroutine _co;

    /// <summary>Goi tu PlotController khi o dat duoc chon. nguon = SpriteRenderer nen cua o.</summary>
    public static void Play(Component chu, SpriteRenderer nguon)
    {
        if (chu == null || nguon == null || nguon.sprite == null) return;
        var fx = chu.GetComponent<SelectionGlowFX>();
        if (fx == null) fx = chu.gameObject.AddComponent<SelectionGlowFX>();
        fx._nguon = nguon;
        fx.Select();
    }

    public void Select()
    {
        if (_nguon == null) return;
        if (_dangSang != null && _dangSang != this) _dangSang.TatNgay();
        _dangSang = this;
        DamBaoLopSang();
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoNhay());
    }

    private void DamBaoLopSang()
    {
        if (_sang == null)
        {
            var go = new GameObject("SelectGlow");
            go.transform.SetParent(_nguon.transform, false);
            _sang = go.AddComponent<SpriteRenderer>();
            if (_matUnlit == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                if (sh != null) _matUnlit = new Material(sh) { name = "SelectGlow_Unlit" };
            }
            if (_matUnlit != null) _sang.sharedMaterial = _matUnlit;
        }
        _sang.sprite = _nguon.sprite;
        _sang.flipX = _nguon.flipX; _sang.flipY = _nguon.flipY;
        _sang.sortingLayerID = _nguon.sortingLayerID;
        _sang.sortingOrder = _nguon.sortingOrder + 1;   // tren nen, duoi cay (cay = 560+)
        _sang.enabled = true;
        // [FIX 2026-09-23] KHONG dong vao scale cua o dat nua. Ban truoc chup lai scale goc o MOI lan
        // cham -> cham lien tuc khi o dang nay (1.06x) thi "goc" moi = 1.06x, cham tiep 1.12x...
        // o dat to dan den khong lo. Nay chi lop sang (con) nay len, o dat giu nguyen tuyet doi.
    }

    private IEnumerator CoNhay()
    {
        float t = 0f;
        while (t < thoiGianNhay)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / thoiGianNhay);
            float a = Mathf.Lerp(0.25f, 0.6f, 0.5f + 0.5f * Mathf.Sin(k * Mathf.PI * 4f));
            _sang.color = new Color(mauSang.r, mauSang.g, mauSang.b, a);
            _sang.transform.localScale = Vector3.one * (1f + nayScale * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        _sang.transform.localScale = Vector3.one;
        float a0 = _sang.color.a; t = 0f;
        while (t < thoiGianTat)
        {
            t += Time.unscaledDeltaTime;
            _sang.color = new Color(mauSang.r, mauSang.g, mauSang.b, Mathf.Lerp(a0, 0f, t / thoiGianTat));
            yield return null;
        }
        _sang.enabled = false;
        _co = null;
        if (_dangSang == this) _dangSang = null;
    }

    private void TatNgay()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        if (_sang != null) { _sang.enabled = false; _sang.transform.localScale = Vector3.one; }
    }

    private void OnDisable() => TatNgay();
}
