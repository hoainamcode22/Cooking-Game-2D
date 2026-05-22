using UnityEngine;
using UnityEngine.UI;

public static class PopupPillApplier
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        Sprite pill = CreatePillSprite();
        ApplyToPopup("Popup_seed", pill);
        ApplyToPopup("Popup_hoa", pill);
    }

    static void ApplyToPopup(string popupName, Sprite pill)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allObjects)
        {
            if (go.name != popupName || !go.scene.IsValid()) continue;

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = pill;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.1f, 0.16f, 0.1f, 0.78f);
            }
            return;
        }
        Debug.LogWarning($"[PopupPillApplier] Không tìm thấy popup: {popupName}");
    }

    static Sprite CreatePillSprite()
    {
        const int W = 200, H = 100;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = H / 2f;
        var pixels = new Color[W * H];

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                pixels[y * W + x] = IsInsidePill(x + 0.5f, y + 0.5f, W, H, r)
                    ? Color.white
                    : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        int border = (int)r;
        return Sprite.Create(tex,
            new Rect(0, 0, W, H),
            new Vector2(0.5f, 0.5f),
            100f, 0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    static bool IsInsidePill(float px, float py, int w, int h, float r)
    {
        float cy = h / 2f;
        float lcx = r;
        float rcx = w - r;
        float dx, dy;

        if (px < lcx)
        {
            dx = px - lcx; dy = py - cy;
            return dx * dx + dy * dy <= r * r;
        }
        if (px > rcx)
        {
            dx = px - rcx; dy = py - cy;
            return dx * dx + dy * dy <= r * r;
        }
        return true;
    }
}
