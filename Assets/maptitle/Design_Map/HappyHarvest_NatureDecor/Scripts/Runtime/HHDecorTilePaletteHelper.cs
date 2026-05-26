using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class HHDecorTilePaletteHelper : MonoBehaviour
{
    private void OnEnable()
    {
        var tilemap = GetComponentInChildren<Tilemap>();
        if (tilemap != null)
        {
            tilemap.CompressBounds();
        }
    }
}
