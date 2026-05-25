using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterScrollEffect : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.2f;

    private Material _mat;
    private float _offset;

    void Start()
    {
        _mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        _offset += scrollSpeed * Time.deltaTime;
        _mat.SetTextureOffset("_MainTex", new Vector2(0f, _offset));
    }
}
