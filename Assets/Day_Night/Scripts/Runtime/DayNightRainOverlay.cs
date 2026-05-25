using System.Collections.Generic;
using UnityEngine;

namespace Day_Night
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DayNightRainOverlay : MonoBehaviour
    {
        public Vector2 AreaSize = new Vector2(90f, 55f);
        [Range(16, 1200)] public int DropCount = 620;
        public float FallSpeed = 22f;
        public float DropLength = 0.8f;
        public float DropWidth = 0.018f;
        public Vector2 Slant = new Vector2(-0.08f, -1f);
        public Color DropColor = new Color(0.58f, 0.72f, 1f, 0.58f);
        public string SortingLayerName = "Foreground";
        public int SortingOrder = 120;
        public bool FollowMainCamera = true;
        public Camera CameraOverride;
        public float CameraAreaPadding = 1.25f;

        private readonly List<Vector3> vertices = new List<Vector3>(2000);
        private readonly List<Color> colors = new List<Color>(2000);
        private readonly List<int> triangles = new List<int>(3000);
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Material material;

        private void OnEnable()
        {
            EnsureRenderer();
            RebuildMesh();
        }

        private void OnDisable()
        {
            if (mesh != null)
            {
                mesh.Clear();
            }
        }

        private void Update()
        {
            FollowCamera();
            RebuildMesh();
        }

        private void OnValidate()
        {
            DropCount = Mathf.Max(16, DropCount);
            AreaSize.x = Mathf.Max(4f, AreaSize.x);
            AreaSize.y = Mathf.Max(4f, AreaSize.y);
            DropLength = Mathf.Max(0.05f, DropLength);
            DropWidth = Mathf.Max(0.005f, DropWidth);
            EnsureRenderer();
            RebuildMesh();
        }

        private void EnsureRenderer()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "Day Night Rain Overlay";
                mesh.hideFlags = HideFlags.DontSave;
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (material == null)
            {
                Shader shader = Shader.Find("Day_Night/RainOverlay");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader != null)
                {
                    material = new Material(shader);
                    material.name = "Day Night Rain Overlay Material";
                    material.hideFlags = HideFlags.DontSave;
                    material.renderQueue = 3000;
                }
            }

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
                meshRenderer.sortingLayerName = SortingLayerName;
                meshRenderer.sortingOrder = SortingOrder;
            }
        }

        private void FollowCamera()
        {
            if (!FollowMainCamera)
            {
                return;
            }

            Camera targetCamera = CameraOverride != null ? CameraOverride : Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 position = targetCamera.transform.position;
            position.z = 0f;
            transform.position = position;

            if (targetCamera.orthographic)
            {
                float height = targetCamera.orthographicSize * 2f * CameraAreaPadding;
                AreaSize = new Vector2(height * targetCamera.aspect, height);
            }
        }

        private void RebuildMesh()
        {
            if (mesh == null)
            {
                return;
            }

            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            Vector2 direction = Slant.sqrMagnitude > 0.001f ? Slant.normalized : Vector2.down;
            Vector2 tangent = new Vector2(-direction.y, direction.x) * DropWidth;
            Vector2 segment = direction * DropLength;
            float time = (Application.isPlaying ? Time.time : Time.realtimeSinceStartup) * FallSpeed;

            for (int i = 0; i < DropCount; i++)
            {
                Vector2 basePosition = GetDropBasePosition(i, time);
                Vector2 start = basePosition - segment * 0.5f;
                Vector2 end = basePosition + segment * 0.5f;
                int vertexIndex = vertices.Count;

                vertices.Add(new Vector3(start.x - tangent.x, start.y - tangent.y, 0f));
                vertices.Add(new Vector3(start.x + tangent.x, start.y + tangent.y, 0f));
                vertices.Add(new Vector3(end.x + tangent.x, end.y + tangent.y, 0f));
                vertices.Add(new Vector3(end.x - tangent.x, end.y - tangent.y, 0f));

                Color dropColor = DropColor;
                dropColor.a *= Mathf.Lerp(0.45f, 1f, Hash01(i * 59 + 23));

                colors.Add(dropColor);
                colors.Add(dropColor);
                colors.Add(dropColor);
                colors.Add(dropColor);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private Vector2 GetDropBasePosition(int index, float time)
        {
            float randomX = Hash01(index * 17 + 3);
            float randomY = Hash01(index * 31 + 11);
            float randomSpeed = Mathf.Lerp(0.65f, 1.35f, Hash01(index * 47 + 19));

            float x = (randomX - 0.5f) * AreaSize.x;
            float y = Mathf.Repeat(randomY * AreaSize.y - time * randomSpeed, AreaSize.y) - AreaSize.y * 0.5f;
            x += Mathf.Sin((time * 0.05f) + index) * 0.2f;

            return new Vector2(x, y);
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                uint x = (uint)value;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (x & 0x00ffffff) / 16777215f;
            }
        }
    }
}
