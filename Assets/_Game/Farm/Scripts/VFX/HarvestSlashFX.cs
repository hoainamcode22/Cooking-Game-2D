using UnityEngine;
using System.Collections;

public class HarvestSlashFX : MonoBehaviour
{
    public static void Spawn(Vector3 position)
    {
        GameObject go = new GameObject("HarvestSlashFX");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // VONG 16 — FIX: AddComponent<ParticleSystem>() tu dong PLAY ngay, ma
        // Unity khong cho doi main.duration khi he dang chay
        // ("Setting the duration while system is still playing is not supported").
        // Dung han lai, cau hinh xong roi Play() lai o cuoi.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        slashPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        // Chùm hạt sao vàng lấp lánh (Golden Sparkles)
        GameObject sparkleGo = new GameObject("SparkleGold");
        sparkleGo.transform.SetParent(go.transform);
        sparkleGo.transform.localPosition = Vector3.zero;
        ParticleSystem sparklePs = sparkleGo.AddComponent<ParticleSystem>();
        sparklePs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var spMain = sparklePs.main;
        spMain.startColor = new Color(1f, 0.92f, 0.35f, 0.95f);
        spMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        spMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
        spMain.duration = 0.5f;
        spMain.loop = false;
        spMain.playOnAwake = true;
        spMain.gravityModifier = 0.4f;

        var spEmission = sparklePs.emission;
        spEmission.rateOverTime = 0;
        spEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 16) });

        var spShape = sparklePs.shape;
        spShape.shapeType = ParticleSystemShapeType.Sphere;
        spShape.radius = 0.4f;

        var spRenderer = sparklePs.GetComponent<ParticleSystemRenderer>();
        spRenderer.sortingLayerName = "Crop";
        spRenderer.sortingOrder = 52;

        // Cau hinh xong moi cho chay — dung thu tu nay se het canh bao duration.
        ps.Play(true);
        slashPs.Play(true);
        sparklePs.Play(true);

        // Nếu có prefab Sparkle_ellow của Lana Studio thì bung thêm hào quang
        var sparklePrefab = Resources.Load<GameObject>("VFX/Sparkle_ellow");
        if (sparklePrefab != null)
        {
            GameObject sp = Instantiate(sparklePrefab, position, Quaternion.identity);
            sp.transform.localScale = Vector3.one * 15f; // Map nông trại scale to
            Destroy(sp, 1.2f);
        }

        Destroy(go, 1.5f);
    }
}
