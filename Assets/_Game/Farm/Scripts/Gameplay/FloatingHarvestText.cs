using TMPro;
using UnityEngine;

public class FloatingHarvestText : MonoBehaviour
{
    [SerializeField] private TMP_Text txt;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 1.2f, 0f);

    private Vector3 startPos;
    private Vector3 endPos;
    private float timer;

    public void Setup(string content)
    {
        if (txt != null)
            txt.text = content;

        startPos = transform.position;
        endPos = startPos + moveOffset;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (txt != null)
        {
            Color c = txt.color;
            c.a = 1f - t;
            txt.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}