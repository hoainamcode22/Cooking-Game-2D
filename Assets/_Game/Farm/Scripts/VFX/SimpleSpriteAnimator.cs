using UnityEngine;

/// <summary>
/// Chay mot day sprite theo fps roi tu huy (hoac lap lai).
///
/// 🔴 VONG 13 — VI SAO CLASS NAY PHAI NAM RIENG MOT FILE:
/// Unity chi cap `fileID 11500000` cho class TRUNG TEN FILE. MonoBehaviour thu hai
/// nam chung file KHONG co asset entry rieng, nen khi nao co ai keo no vao prefab/scene
/// (hoac Unity serialize lai) thi component do se duoc ghi nham thanh class CHINH cua file.
/// Day dung la nguyen nhan cua bug "5 popup tau de len nhau" o vong 7
/// (StationWagonSlotUI nam chung file voi TrainStationMasterPopupUI).
/// Truoc vong 13, class nay nam chung file voi RainSplashManager — tach ra de dap tat mam.
/// </summary>
public class SimpleSpriteAnimator : MonoBehaviour
{
    public Sprite[] sprites;
    public float fps = 15f;
    public bool destroyOnEnd = true;

    private SpriteRenderer _sr;
    private float _timer;
    private int _frame;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null && sprites != null && sprites.Length > 0)
            _sr.sprite = sprites[0];
    }

    private void Update()
    {
        if (_sr == null || sprites == null || sprites.Length == 0) return;

        _timer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, fps);
        while (_timer >= frameTime)
        {
            _timer -= frameTime;
            _frame++;
            if (_frame >= sprites.Length)
            {
                if (destroyOnEnd) { Destroy(gameObject); return; }
                _frame = 0;
            }
            _sr.sprite = sprites[_frame];
        }
    }
}
