using UnityEngine;

public class PlotCropVisual : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform[] cropPoints = new Transform[4];

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Crop";
    [SerializeField] private int sortingOrder = 2;

    [Header("Normalized Height")]
    [SerializeField] private float sproutHeight = 3.00f;
    [SerializeField] private float growingHeight = 3.00f;
    [SerializeField] private float readyHeight = 3.00f;

    private SpriteRenderer[] slotRenderers;

    private void Awake()
    {
        EnsureSetup();
    }

    private void OnValidate()
    {
        AutoFindPoints();
    }

    [ContextMenu("Auto Find Points")]
    public void AutoFindPoints()
    {
        if (cropPoints == null || cropPoints.Length != 4)
            cropPoints = new Transform[4];

        cropPoints[0] = transform.Find("CropPoint_1");
        cropPoints[1] = transform.Find("CropPoint_2");
        cropPoints[2] = transform.Find("CropPoint_3");
        cropPoints[3] = transform.Find("CropPoint_4");
    }

    private void EnsureSetup()
    {
        AutoFindPoints();

        if (slotRenderers != null && slotRenderers.Length == cropPoints.Length)
            return;

        slotRenderers = new SpriteRenderer[cropPoints.Length];

        for (int i = 0; i < cropPoints.Length; i++)
        {
            Transform point = cropPoints[i];
            if (point == null) continue;

            Transform visual = point.Find("Visual");
            GameObject go;

            if (visual == null)
            {
                go = new GameObject("Visual");
                go.transform.SetParent(point, false);
            }
            else
            {
                go = visual.gameObject;
            }

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();

            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;
            sr.sprite = null;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            slotRenderers[i] = sr;
        }
    }

    public void ClearAll()
    {
        EnsureSetup();

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            if (slotRenderers[i] == null) continue;

            slotRenderers[i].enabled = false;
            slotRenderers[i].sprite = null;
            slotRenderers[i].transform.localPosition = Vector3.zero;
            slotRenderers[i].transform.localScale = Vector3.one;
        }
    }

    public void ShowCrop(CropData crop, float progress01)
    {
        EnsureSetup();

        if (crop == null)
        {
            ClearAll();
            return;
        }

        Sprite sprite = crop.GetStageSprite(progress01);
        if (sprite == null)
        {
            ClearAll();
            return;
        }

        float targetHeight = GetTargetHeight(progress01);
        Vector2 offset = crop.GetStageOffset(progress01);
        Vector3 normalizedScale = GetNormalizedScale(sprite, targetHeight);

        for (int i = 0; i < slotRenderers.Length; i++)
        {
            SpriteRenderer sr = slotRenderers[i];
            if (sr == null) continue;

            sr.enabled = true;
            sr.sprite = sprite;
            sr.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = normalizedScale;
        }
    }

    private float GetTargetHeight(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (progress01 >= 1f) return readyHeight;
        if (progress01 < 0.5f) return sproutHeight;
        return growingHeight;
    }

    private Vector3 GetNormalizedScale(Sprite sprite, float targetHeight)
    {
        if (sprite == null) return Vector3.one;

        float h = sprite.bounds.size.y;
        if (h <= 0.0001f) return Vector3.one;

        float scale = targetHeight / h;
        return Vector3.one * scale;
    }
}