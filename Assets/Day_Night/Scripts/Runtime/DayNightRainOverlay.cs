using System.Collections.Generic;
using UnityEngine;

namespace Day_Night
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DayNightRainOverlay : MonoBehaviour
    {
        public Vector2 AreaSize = new Vector2(2600f, 1600f);
        [Range(16, 6000)] public int DropCount = 160;
        public float FallSpeed = 850f;
        public float DropLength = 78f;
        public float DropWidth = 2.2f;
        public float DropHeadSize = 4.5f;
        public Vector2 Slant = new Vector2(-0.08f, -1f);
        public Color DropColor = new Color(0.86f, 0.96f, 1f, 0.72f);
        public string SortingLayerName = "Foreground";
        public int SortingOrder = 260;
        public bool FollowMainCamera = true;
        public Camera CameraOverride;
        public float CameraAreaPadding = 1.25f;

        private readonly List<Vector3> vertices = new List<Vector3>(2000);
        private readonly List<Color> colors = new List<Color>(2000);
        private readonly List<int> triangles = new List<int>(3000);
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Material material;

        // ── [PERF P0] Tach TOPOLOGY (chi dung 1 lan) khoi VI TRI DINH (moi frame) ──────────
        // Truoc day Update() goi RebuildMesh() moi frame: Clear 3 List, sinh lai 8 vert + 8 color
        // + 12 index cho tung hat, roi mesh.Clear() + SetVertices + SetColors + SetTriangles +
        // RecalculateBounds(). Voi 160 hat = 1.280 vert dung lai moi frame — vo ich, vi index va
        // mau KHONG doi. Nay: index/mau dung 1 lan khi so hat (hoac mau) doi; moi frame chi ghi
        // de vi tri dinh va SetVertices. Ket qua hinh anh giu nguyen.
        private const int MAX_DROPS_RUNTIME = 250;   // tran an toan cho mobile (khong doi default serialize)
        private const int VERTS_PER_DROP = 8;        // 4 dinh than hat + 4 dinh dau hat
        private const int INDICES_PER_DROP = 12;     // 2 quad = 4 tam giac

        private int   _builtDropCount = -1;          // so hat da dung topology
        private Color _builtColor;                   // mau da nap vao mesh
        private bool  _colorsDirty = true;
        private bool  _meshCoData;                   // mesh dang co du lieu hay da bi Clear
        private float _editorNextRebuildTime;        // ham throttle preview trong Edit Mode

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
                _meshCoData = false;
                _builtDropCount = -1;
            }
        }

        private void Update()
        {
            // [PERF P0] Sap tat / khong mua => KHONG lam gi ca.
            if (!isActiveAndEnabled) return;

            if (DropCount <= 0 || DropColor.a <= 0.0001f)
            {
                XoaMeshNeuCon();
                return;
            }

            // [PERF] [ExecuteAlways] van giu, nhung trong Edit Mode chi ve lai ~10 fps cho du xem
            // preview — khong dot CPU cua Editor.
            if (!Application.isPlaying)
            {
                if (Time.realtimeSinceStartup < _editorNextRebuildTime) return;
                _editorNextRebuildTime = Time.realtimeSinceStartup + 0.1f;
            }

            FollowCamera();
            RebuildMesh();
        }

        /// <summary>[PERF] Don mesh dung 1 lan khi tat mua, roi thoi.</summary>
        private void XoaMeshNeuCon()
        {
            if (mesh != null && _meshCoData)
            {
                mesh.Clear();
                _meshCoData = false;
                _builtDropCount = -1;
            }
        }

        private void OnValidate()
        {
            DropCount = Mathf.Max(16, DropCount);
            AreaSize.x = Mathf.Max(4f, AreaSize.x);
            AreaSize.y = Mathf.Max(4f, AreaSize.y);
            DropLength = Mathf.Max(0.05f, DropLength);
            DropWidth = Mathf.Max(0.005f, DropWidth);
            DropHeadSize = Mathf.Max(0.1f, DropHeadSize);
            EnsureRenderer();
            _builtDropCount = -1;   // [PERF] param doi trong Inspector => dung lai topology 1 lan
            _colorsDirty    = true;
            RebuildMesh();
        }

        private void EnsureRenderer()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "Day Night Rain Overlay";
                mesh.hideFlags = HideFlags.DontSave;
                mesh.MarkDynamic();                 // [PERF] buffer ghi lai moi frame
                GetComponent<MeshFilter>().sharedMesh = mesh;
                _builtDropCount = -1;               // mesh moi => phai dung lai topology
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
                    material.renderQueue = 4500;
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

        /// <summary>
        /// [PERF P0] Diem vao moi frame. Chi dung lai topology khi so hat doi; chi nap lai mau khi
        /// DropColor doi; con lai chi ghi de vi tri dinh. Giu nguyen ten/ham cu de cho goi khac
        /// (OnEnable / OnValidate) khong phai doi.
        /// </summary>
        private void RebuildMesh()
        {
            if (mesh == null)
            {
                return;
            }

            // Tran an toan cho mobile — KHONG doi gia tri serialize, chi kep luc chay.
            int soHat = Mathf.Clamp(Mathf.Min(DropCount, MAX_DROPS_RUNTIME), 0, MAX_DROPS_RUNTIME);
            if (soHat <= 0)
            {
                XoaMeshNeuCon();
                return;
            }

            bool dungLaiTopology = (soHat != _builtDropCount) || !_meshCoData;

            if (dungLaiTopology)
            {
                DungTopology(soHat);
            }

            CapNhatViTriDinh(soHat);
            mesh.SetVertices(vertices);

            if (dungLaiTopology || _colorsDirty || _builtColor != DropColor)
            {
                NapMau(soHat);
                mesh.SetColors(colors);
                _builtColor = DropColor;
                _colorsDirty = false;
            }
        }

        /// <summary>
        /// [PERF] Dung index buffer + cap phat san List — CHI chay khi so hat doi.
        /// Bo RecalculateBounds(): thay bang bounds co dinh that to (mesh luon bam theo camera nen
        /// khong bao gio bi culling sai).
        /// </summary>
        private void DungTopology(int soHat)
        {
            int soDinh  = soHat * VERTS_PER_DROP;
            int soIndex = soHat * INDICES_PER_DROP;

            vertices.Clear();
            colors.Clear();
            triangles.Clear();
            if (vertices.Capacity  < soDinh)  vertices.Capacity  = soDinh;
            if (colors.Capacity    < soDinh)  colors.Capacity    = soDinh;
            if (triangles.Capacity < soIndex) triangles.Capacity = soIndex;

            for (int i = 0; i < soDinh; i++)
            {
                vertices.Add(Vector3.zero);
                colors.Add(Color.white);
            }

            for (int i = 0; i < soHat; i++)
            {
                int v = i * VERTS_PER_DROP;

                // Quad than hat (v+0..v+3)
                triangles.Add(v);
                triangles.Add(v + 1);
                triangles.Add(v + 2);
                triangles.Add(v);
                triangles.Add(v + 2);
                triangles.Add(v + 3);

                // Quad dau hat (v+4..v+7)
                triangles.Add(v + 4);
                triangles.Add(v + 5);
                triangles.Add(v + 6);
                triangles.Add(v + 4);
                triangles.Add(v + 6);
                triangles.Add(v + 7);
            }

            CapNhatViTriDinh(soHat);
            NapMau(soHat);

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, false);   // false = khong tinh lai bounds
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(100000f, 100000f, 1f));

            _builtDropCount = soHat;
            _builtColor     = DropColor;
            _colorsDirty    = false;
            _meshCoData     = true;
        }

        /// <summary>[PERF] Phan DUY NHAT chay moi frame: ghi de vi tri 8 dinh cua tung hat.</summary>
        private void CapNhatViTriDinh(int soHat)
        {
            Vector2 direction = Slant.sqrMagnitude > 0.001f ? Slant.normalized : Vector2.down;
            Vector2 tangent = new Vector2(-direction.y, direction.x) * DropWidth;
            Vector2 segment = direction * DropLength;
            float time = (Application.isPlaying ? Time.time : Time.realtimeSinceStartup) * FallSpeed;
            float halfHead = DropHeadSize * 0.5f;

            for (int i = 0; i < soHat; i++)
            {
                Vector2 basePosition = GetDropBasePosition(i, time);
                Vector2 start = basePosition - segment * 0.5f;
                Vector2 end = basePosition + segment * 0.5f;
                int v = i * VERTS_PER_DROP;

                vertices[v]     = new Vector3(start.x - tangent.x, start.y - tangent.y, 0f);
                vertices[v + 1] = new Vector3(start.x + tangent.x, start.y + tangent.y, 0f);
                vertices[v + 2] = new Vector3(end.x + tangent.x, end.y + tangent.y, 0f);
                vertices[v + 3] = new Vector3(end.x - tangent.x, end.y - tangent.y, 0f);

                vertices[v + 4] = new Vector3(end.x - halfHead, end.y - halfHead, 0f);
                vertices[v + 5] = new Vector3(end.x + halfHead, end.y - halfHead, 0f);
                vertices[v + 6] = new Vector3(end.x + halfHead, end.y + halfHead, 0f);
                vertices[v + 7] = new Vector3(end.x - halfHead, end.y + halfHead, 0f);
            }
        }

        /// <summary>
        /// [PERF] Mau chi phu thuoc chi so hat + DropColor => nap lai khi DropColor doi, khong moi frame.
        /// Cong thuc giu Y NGUYEN ban cu (than: alpha * Lerp(0.45,1); dau: alpha * 1.25 kep 1).
        /// </summary>
        private void NapMau(int soHat)
        {
            for (int i = 0; i < soHat; i++)
            {
                int v = i * VERTS_PER_DROP;

                Color dropColor = DropColor;
                dropColor.a *= Mathf.Lerp(0.45f, 1f, Hash01(i * 59 + 23));

                colors[v]     = dropColor;
                colors[v + 1] = dropColor;
                colors[v + 2] = dropColor;
                colors[v + 3] = dropColor;

                Color headColor = dropColor;
                headColor.a = Mathf.Min(1f, headColor.a * 1.25f);

                colors[v + 4] = headColor;
                colors[v + 5] = headColor;
                colors[v + 6] = headColor;
                colors[v + 7] = headColor;
            }
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
