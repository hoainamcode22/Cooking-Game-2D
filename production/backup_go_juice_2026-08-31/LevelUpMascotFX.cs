using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [JUICE PACK T3 — 2026-08-31, đổi tên từ LevelUpMascotUI vì asset db từ chối import file cũ] Nhân vật MASCOT ăn mừng trên popup Level-Up
/// (tham chiếu video: con sói cười trong huy hiệu LEVEL UP, nhún nhảy rầm rộ).
///
/// Game mình: 4-5 nhân vật nửa thân theo bộ avatar có sẵn, mỗi con 1 sprite sheet
/// 12 frame do đội vẽ giao vào: Resources/LevelUpMascots/{id}/frame_01..frame_12
/// (id: cowboy, chef_female, flower_girl, boy, lumberjack — khớp bộ avatar).
///
/// Cách dùng (gọi từ LevelUpPopupUI sau khi dựng badge — 1 dòng, chờ duyệt wire):
///     LevelUpMascotFX.AttachTo(badgeRect, levelReached);
///
/// • Mascot chọn theo level (xoay vòng) → mỗi lần lên cấp gặp mặt mới, vui hơn.
/// • Phát 12 frame @ 12fps lặp + nhún nảy scale (unscaled time — popup pause vẫn chạy).
/// • CHƯA có art: hiện avatar tĩnh từ Resources/Avatars (có sẵn 8 file) + vẫn nhún —
///   nghĩa là wire được NGAY hôm nay, đội vẽ giao sheet là tự mượt.
/// </summary>
public class LevelUpMascotFX : MonoBehaviour
{
    public static readonly string[] MascotIds =
        { "cowboy", "chef_female", "flower_girl", "boy", "lumberjack" };

    private const float Fps = 12f;
    private Sprite[] _frames;
    private Image _img;

    /// <summary>Gắn mascot vào giữa/đỉnh khung badge của popup Level-Up.</summary>
    public static LevelUpMascotFX AttachTo(RectTransform parent, int levelReached)
    {
        if (parent == null) return null;
        // Popup mở lại nhiều lần → dọn mascot cũ trước, không nhân đôi.
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.name.StartsWith("Mascot_")) Object.Destroy(c.gameObject);
        }
        string id = MascotIds[Mathf.Abs(levelReached) % MascotIds.Length];

        var go = new GameObject($"Mascot_{id}", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 30f);
        rt.sizeDelta = new Vector2(230f, 230f);

        var ui = go.AddComponent<LevelUpMascotFX>();
        ui._img = go.GetComponent<Image>();
        ui._img.raycastTarget = false;
        ui._img.preserveAspect = true;
        ui.LoadFrames(id);
        return ui;
    }

    private void LoadFrames(string id)
    {
        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = 1; i <= 12; i++)
        {
            var s = Resources.Load<Sprite>($"LevelUpMascots/{id}/frame_{i:00}");
            if (s != null) list.Add(s);
        }
        if (list.Count == 0)
        {
            // Fallback: avatar tĩnh có sẵn — popup vẫn có mặt nhân vật ngay hôm nay.
            var av = Resources.Load<Sprite>($"Avatars/avatar_{id}");
            if (av == null) av = Resources.Load<Sprite>("Avatars/avatar_boy");
            if (av != null) list.Add(av);
        }
        _frames = list.ToArray();
        if (_frames.Length > 0)
        {
            _img.sprite = _frames[0];
            StartCoroutine(Animate());
            StartCoroutine(Bounce());
        }
        else gameObject.SetActive(false);   // không có gì để hiện — im lặng, không lỗi
    }

    private IEnumerator Animate()
    {
        if (_frames.Length < 2) yield break;
        int i = 0; float acc = 0f;
        while (true)
        {
            acc += Time.unscaledDeltaTime;
            if (acc >= 1f / Fps)
            {
                acc -= 1f / Fps;
                i = (i + 1) % _frames.Length;
                _img.sprite = _frames[i];
            }
            yield return null;
        }
    }

    /// <summary>Nhún nảy "rầm rộ": vào bằng easeOutBack to, rồi lắc lư thở nhẹ vô hạn.</summary>
    private IEnumerator Bounce()
    {
        var rt = (RectTransform)transform;
        Vector3 baseScale = Vector3.one;
        float e = 0f; const float inT = 0.42f;
        while (e < inT)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / inT);
            float s = 1f + 1.9f * Mathf.Pow(k - 1f, 3) + 1.9f * Mathf.Pow(k - 1f, 2);
            rt.localScale = baseScale * Mathf.Max(0.01f, s);
            yield return null;
        }
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + 0.05f * Mathf.Sin(t * 3.6f);
            float r = 2.6f * Mathf.Sin(t * 2.2f);
            rt.localScale = baseScale * s;
            rt.localRotation = Quaternion.Euler(0f, 0f, r);
            yield return null;
        }
    }
}
