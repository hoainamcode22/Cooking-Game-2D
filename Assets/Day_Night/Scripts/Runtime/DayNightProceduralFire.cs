using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace Day_Night
{
    [ExecuteAlways]
    public class DayNightProceduralFire : MonoBehaviour
    {
        [Min(0.05f)] public float Width = 1.1f;
        [Min(0.05f)] public float Height = 1.1f;
        [Range(0.1f, 4f)] public float FlickerSpeed = 1.8f;
        [Range(0f, 1f)] public float FlickerAmount = 0.22f;
        public Texture2D FlameTexture;
        public string SortingLayerName = "Foreground";
        public int SortingOrder = 80;

        private static readonly Color GlowColor = new Color(1f, 0.88f, 0.06f, 0.34f);
        private static readonly Color OuterColor = new Color(1f, 0.25f, 0.02f, 0.72f);
        private static readonly Color MidColor = new Color(1f, 0.58f, 0.04f, 0.86f);
        private static readonly Color InnerColor = new Color(1f, 0.96f, 0.33f, 0.95f);

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;
        private Material material;
        private VisualEffect visualEffect;

        private void OnEnable()
        {
            EnsureSetup();
            PlayVfx();
            UpdateFire();
        }

        private void OnValidate()
        {
            EnsureSetup();
            UpdateFire();
        }

        private void Update()
        {
            PlayVfx();
            UpdateFire();
        }

        private void EnsureSetup()
        {
            visualEffect = GetComponent<VisualEffect>();

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "DayNight Procedural Fire";
                mesh.hideFlags = HideFlags.DontSave;
                meshFilter.sharedMesh = mesh;
            }

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                material = new Material(shader);
                material.name = "DayNight Procedural Fire Material";
                material.hideFlags = HideFlags.DontSave;
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 1f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.One);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingLayerName = SortingLayerName;
            meshRenderer.sortingOrder = SortingOrder;

            if (FlameTexture != null)
            {
                material.SetTexture("_BaseMap", FlameTexture);
                material.SetTexture("_MainTex", FlameTexture);
            }
        }

        private void PlayVfx()
        {
            if (visualEffect == null)
            {
                return;
            }

            if (!visualEffect.enabled)
            {
                visualEffect.enabled = true;
            }

            visualEffect.Play();
        }

        private void UpdateFire()
        {
            if (mesh == null)
            {
                return;
            }

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            float wobbleA = Mathf.Sin(time * FlickerSpeed * 5.1f) * FlickerAmount;
            float wobbleB = Mathf.Sin(time * FlickerSpeed * 7.3f + 1.7f) * FlickerAmount;

            Vector3 lossyScale = transform.lossyScale;
            float scaleX = Mathf.Max(Mathf.Abs(lossyScale.x), 0.0001f);
            float scaleY = Mathf.Max(Mathf.Abs(lossyScale.y), 0.0001f);
            float invX = 1f / scaleX;
            float invY = 1f / scaleY;

            Vector3[] vertices = new Vector3[32];
            Vector2[] uv = new Vector2[32];
            Color[] colors = new Color[32];
            int[] triangles = new int[48];
            int quad = 0;

            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0f, 0.17f), Width * 1.25f, Height * 0.55f, GlowColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(-0.2f + wobbleA * 0.08f, 0.3f), Width * 0.72f, Height * 0.82f, OuterColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0.18f + wobbleB * 0.08f, 0.34f), Width * 0.68f, Height * 0.78f, OuterColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0.02f - wobbleA * 0.08f, 0.48f), Width * 0.58f, Height * 0.96f, MidColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(-0.12f + wobbleB * 0.06f, 0.43f), Width * 0.44f, Height * 0.7f, MidColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0.1f - wobbleB * 0.05f, 0.38f), Width * 0.38f, Height * 0.58f, InnerColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0f + wobbleA * 0.05f, 0.58f), Width * 0.28f, Height * 0.58f, InnerColor, invX, invY);
            AddBlob(vertices, uv, colors, triangles, ref quad, new Vector2(0f, 0.18f), Width * 0.55f, Height * 0.28f, InnerColor, invX, invY);

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        private static void AddBlob(
            Vector3[] vertices,
            Vector2[] uv,
            Color[] colors,
            int[] triangles,
            ref int quad,
            Vector2 center,
            float width,
            float height,
            Color color,
            float inverseScaleX,
            float inverseScaleY)
        {
            int vertexIndex = quad * 4;
            int triangleIndex = quad * 6;
            float halfWidth = width * 0.5f * inverseScaleX;
            float halfHeight = height * 0.5f * inverseScaleY;
            Vector3 localCenter = new Vector3(center.x * inverseScaleX, center.y * inverseScaleY, -0.001f * quad);

            vertices[vertexIndex] = localCenter + new Vector3(-halfWidth, -halfHeight, 0f);
            vertices[vertexIndex + 1] = localCenter + new Vector3(-halfWidth, halfHeight, 0f);
            vertices[vertexIndex + 2] = localCenter + new Vector3(halfWidth, halfHeight, 0f);
            vertices[vertexIndex + 3] = localCenter + new Vector3(halfWidth, -halfHeight, 0f);

            uv[vertexIndex] = new Vector2(0f, 0f);
            uv[vertexIndex + 1] = new Vector2(0f, 1f);
            uv[vertexIndex + 2] = new Vector2(1f, 1f);
            uv[vertexIndex + 3] = new Vector2(1f, 0f);

            colors[vertexIndex] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;

            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;

            quad++;
        }
    }
}
