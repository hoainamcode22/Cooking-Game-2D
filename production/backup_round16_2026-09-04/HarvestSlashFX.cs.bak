using UnityEngine;
using System.Collections;

public class HarvestSlashFX : MonoBehaviour
{
    public static void Spawn(Vector3 position)
    {
        GameObject go = new GameObject("HarvestSlashFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.4f, 0.9f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 1f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Crop";
        renderer.sortingOrder = 50;
        
        // Bụi trắng chém (Slash)
        GameObject slashGo = new GameObject("SlashWhite");
        slashGo.transform.SetParent(go.transform);
        slashGo.transform.localPosition = Vector3.zero;
        ParticleSystem slashPs = slashGo.AddComponent<ParticleSystem>();
        var smain = slashPs.main;
        smain.startColor = Color.white;
        smain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        smain.startSpeed = 0f;
        smain.duration = 0.2f;
        smain.loop = false;
        smain.playOnAwake = true;
        var semission = slashPs.emission;
        semission.rateOverTime = 0;
        semission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 1) });
        var srenderer = slashPs.GetComponent<ParticleSystemRenderer>();
        srenderer.sortingLayerName = "Crop";
        srenderer.sortingOrder = 51;

        Destroy(go, 1.5f);
    }
}
