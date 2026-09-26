// ============================================================================
//  UIFireFrames — lua lo bep (UI Image) chay theo bo frame MOI ve (Restaurant_v3/stove_fire_sheet) (2026-09-25)
//  Gan tren Oven/Oven_Fire, Fx_ComboFire, Fx_LuaNho_0..2 (Tools: Edric Tools > Bep (Kitchen) > 1).
//  KHONG dong vao bat/tat, scale, alpha, anh sang (KitchenJuiceFX / Fx_ComboGlow van lo nhu cu) -
//  chi thay SPRITE moi frame (LateUpdate, sau moi script khac) nen lua cu khong con hien lai.
//  Chay theo unscaled time: popup pause (timeScale 0) lua van chay.
//  Giu spriteCu / giuTiLeCu de tool tra lai y nguyen.
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UIFireFrames : MonoBehaviour
{
    public Sprite[] frames = new Sprite[0];
    [Min(1f)] public float fps = 10f;

    [HideInInspector] public Sprite spriteCu;
    [HideInInspector] public bool giuTiLeCu;

    private Image _img;
    private float _t;

    private void Awake()
    {
        _img = GetComponent<Image>();
        _t = Random.Range(0f, 1f);
        if (_img != null && frames != null && frames.Length > 0 && frames[0] != null) _img.sprite = frames[0];
    }

    private void LateUpdate()
    {
        if (_img == null || !_img.enabled || frames == null || frames.Length == 0) return;
        _t += Time.unscaledDeltaTime;
        var sp = frames[(int)(_t * fps) % frames.Length];
        if (sp != null && _img.sprite != sp) _img.sprite = sp;
    }
}
