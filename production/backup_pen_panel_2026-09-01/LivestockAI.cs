using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assetsgame.Animals
{
    /// <summary>
    /// AI điều khiển con vật trong chuồng (Gà, Bò thịt, Bò sữa, Heo):
    /// - Di chuyển thông minh trong phạm vi sàn chuồng, không chạy lệch ra ngoài hàng rào.
    /// - Đi bộ mượt mà, đồng bộ với Animator (Speed float) và flip hướng mặt trái/phải.
    /// - Lúc CHƯA cho ăn (Hungry/Idle): Đi lại nhiều, bồn chồn, thỉnh thoảng kêu la đòi ăn.
    /// - Lúc ĐÃ cho ăn (Processing/Fed): Ngoan ngoãn, chậm rãi, đứng yên nhai/nghỉ ngơi nhiều hơn.
    /// - Lúc SẴN SÀNG thu hoạch (Ready): Đứng hướng về phía trước, sẵn sàng thu hoạch.
    /// - SortingGroup & SortingOrder cao (base 600+) và dynamic theo Y để không bị chìm hay che lấp tứ chi.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class LivestockAI : MonoBehaviour
    {
        [Header("Movement Bounds (Tọa độ Local trong chuồng)")]
        [Tooltip("Giới hạn di chuyển nhỏ nhất bên trong hàng rào chuồng")]
        public Vector2 localBoundsMin = new Vector2(-0.85f, -0.40f);
        [Tooltip("Giới hạn di chuyển lớn nhất bên trong hàng rào chuồng")]
        public Vector2 localBoundsMax = new Vector2(0.85f, 0.30f);
        [Tooltip("Tự động nhận diện biên chuồng (để false để dùng tọa độ chính xác bên trong hàng rào)")]
        public bool autoCalculateBounds = false;

        [Header("Speeds")]
        public float walkSpeed = 0.5f;
        public float hungryWalkSpeed = 0.95f;

        [Header("Idle Durations")]
        [Tooltip("Thời gian đứng yên khi ĐÓI (đi lại nhiều, đứng ít)")]
        public float minIdleHungry = 1.0f;
        public float maxIdleHungry = 2.5f;

        [Tooltip("Thời gian đứng yên khi ĐÃ CHO ĂN (no nê, ngoan ngoãn, đứng lâu)")]
        public float minIdleFed = 3.5f;
        public float maxIdleFed = 7.0f;

        [Header("Audio (Âm thanh kêu lúc đói)")]
        public AudioClip[] soundClips;
        [Range(0f, 1f)] public float soundVolume = 0.6f;
        [Range(0f, 1f)] public float hungryCryChance = 0.4f;

        [Header("Sorting")]
        public string sortingLayerName = "CongTrinh";
        public int baseSortingOrder = 600;

        private Vector3 startLocalPos;
        private Vector3 targetLocalPos;
        private bool isMoving;
        private PenMiniPanelUI parentPen;
        private Animator animator;
        private SortingGroup sortingGroup;
        private AudioSource audioSource;
        private Coroutine roamCoroutine;
        private float originalScaleX;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
                sortingGroup = gameObject.AddComponent<SortingGroup>();

            sortingGroup.sortingLayerName = sortingLayerName;
            sortingGroup.sortingOrder = baseSortingOrder;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }

            originalScaleX = Mathf.Abs(transform.localScale.x);
            if (originalScaleX < 0.01f) originalScaleX = 1f;
        }

        private void Start()
        {
            startLocalPos = transform.localPosition;
            targetLocalPos = startLocalPos;

            FindParentPen();

            if (autoCalculateBounds && transform.parent != null)
            {
                CalculatePenBounds();
            }

            // Đảm bảo vị trí ban đầu nằm trong bounds
            ClampInsideBounds(ref startLocalPos);
            transform.localPosition = startLocalPos;
            targetLocalPos = startLocalPos;

            // Thiết lập sorting nội bộ của các chi
            SetupLimbSorting();

            roamCoroutine = StartCoroutine(RoamRoutine());
        }

        private void FindParentPen()
        {
            Transform current = transform;
            while (current != null)
            {
                parentPen = current.GetComponentInChildren<PenMiniPanelUI>(true);
                if (parentPen == null)
                    parentPen = current.GetComponentInParent<PenMiniPanelUI>();

                if (parentPen != null)
                    break;
                current = current.parent;
            }
        }

        private void CalculatePenBounds()
        {
            Transform penRoot = transform.parent;
            if (penRoot == null) return;

            // Tìm collider hoặc BarnSprite của chuồng để tính kích thước sàn
            BoxCollider2D box = penRoot.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                float halfW = Mathf.Max(1.2f, box.size.x * 0.35f);
                float centerY = box.offset.y;
                float halfH = Mathf.Max(0.5f, box.size.y * 0.25f);

                localBoundsMin = new Vector2(-halfW, centerY - halfH * 0.8f);
                localBoundsMax = new Vector2(halfW, centerY + halfH * 1.1f);
            }
            else
            {
                SpriteRenderer barnSr = penRoot.Find("BarnSprite")?.GetComponent<SpriteRenderer>();
                if (barnSr != null && barnSr.sprite != null)
                {
                    Bounds b = barnSr.sprite.bounds;
                    float halfW = Mathf.Max(1.2f, b.extents.x * 0.6f);
                    float centerY = barnSr.transform.localPosition.y;
                    localBoundsMin = new Vector2(-halfW, centerY - 0.6f);
                    localBoundsMax = new Vector2(halfW, centerY + 0.7f);
                }
            }
        }

        private void SetupLimbSorting()
        {
            // SortingGroup đã gom tất cả các renderer con thành 1 khối.
            // Chuẩn hóa sorting nội bộ để tứ chi không bị chồng chéo lỗi:
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string n = sr.gameObject.name.ToLower();
                if (n.Contains("shadow"))
                {
                    sr.sortingOrder = -5;
                }
                else if (n.Contains("back") || n.Contains("sau") || n.Contains("leg_b") || n.Contains("leg1") || n.Contains("leg3"))
                {
                    sr.sortingOrder = 0;
                }
                else if (n.Contains("body") || n.Contains("than") || n.Contains("torso"))
                {
                    sr.sortingOrder = 2;
                }
                else if (n.Contains("tail") || n.Contains("duoi"))
                {
                    sr.sortingOrder = 1;
                }
                else if (n.Contains("head") || n.Contains("dau"))
                {
                    sr.sortingOrder = 4;
                }
                else if (n.Contains("front") || n.Contains("truoc") || n.Contains("leg_f") || n.Contains("leg0") || n.Contains("leg2"))
                {
                    sr.sortingOrder = 6;
                }
                else if (n.Contains("eye") || n.Contains("ear") || n.Contains("horn") || n.Contains("comb") || n.Contains("mat"))
                {
                    sr.sortingOrder = 8;
                }
            }
        }

        private void Update()
        {
            UpdateDynamicSorting();
        }

        private void UpdateDynamicSorting()
        {
            if (sortingGroup != null)
            {
                // Dynamic sorting theo trục Y: con đứng thấp hơn (Y nhỏ hơn) sẽ ở phía trước (Order cao hơn)
                sortingGroup.sortingLayerName = sortingLayerName;
                sortingGroup.sortingOrder = baseSortingOrder + Mathf.RoundToInt(-transform.localPosition.y * 50f);
            }
        }

        private IEnumerator RoamRoutine()
        {
            while (true)
            {
                PenMiniPanelUI.PenState state = PenMiniPanelUI.PenState.Idle;
                if (parentPen != null)
                {
                    state = parentPen.CurrentState;
                }

                // ── 1. TRẠNG THÁI READY (Đã có sản phẩm / sẵn sàng thu hoạch) ───────
                if (state == PenMiniPanelUI.PenState.Ready)
                {
                    SetMoving(false);
                    // Đứng hướng ra trước, ngoan ngoãn chờ thu hoạch
                    SetFacing(1f);
                    yield return new WaitForSeconds(1.5f);
                    continue;
                }

                // ── 2. CHỌN ĐIỂM ĐẾN MỚI TRONG BOUNDS CHUỒNG ─────────────────────────
                if (!isMoving)
                {
                    targetLocalPos = GetRandomTargetInBounds();
                    isMoving = true;
                    SetMoving(true);

                    // Nếu đang đói, thỉnh thoảng kêu la đòi ăn
                    if (state == PenMiniPanelUI.PenState.Idle && Random.value < hungryCryChance)
                    {
                        PlayAnimalSound();
                    }
                }

                // ── 3. DI CHUYỂN TỚI ĐÍCH ─────────────────────────────────────────────
                float currentSpeed = (state == PenMiniPanelUI.PenState.Idle) ? hungryWalkSpeed : walkSpeed;

                while (Vector3.Distance(transform.localPosition, targetLocalPos) > 0.06f)
                {
                    // Cập nhật hướng quay mặt
                    float dx = targetLocalPos.x - transform.localPosition.x;
                    if (Mathf.Abs(dx) > 0.02f)
                    {
                        SetFacing(Mathf.Sign(dx));
                    }

                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPos, currentSpeed * Time.deltaTime);
                    yield return null;
                }

                // ── 4. ĐÃ TỚI ĐÍCH: DỪNG LẠI & NGHỈ NGƠI ─────────────────────────────
                transform.localPosition = targetLocalPos;
                isMoving = false;
                SetMoving(false);

                // Thời gian nghỉ: Đói thì bồn chồn (nghỉ ngắn), No thì ngoan ngoãn (nghỉ lâu)
                float idleWait = (state == PenMiniPanelUI.PenState.Processing)
                    ? Random.Range(minIdleFed, maxIdleFed)
                    : Random.Range(minIdleHungry, maxIdleHungry);

                yield return new WaitForSeconds(idleWait);
            }
        }

        private Vector3 GetRandomTargetInBounds()
        {
            float rx = Random.Range(localBoundsMin.x, localBoundsMax.x);
            float ry = Random.Range(localBoundsMin.y, localBoundsMax.y);

            // Điều chỉnh biên góc để con vật không đi vào 4 góc nhọn của hàng rào quả trám
            float xRatio = Mathf.Abs(rx) / Mathf.Max(0.1f, localBoundsMax.x);
            float yMaxAllowed = Mathf.Lerp(localBoundsMax.y, 0f, xRatio * 0.60f);
            float yMinAllowed = Mathf.Lerp(localBoundsMin.y, 0f, xRatio * 0.60f);
            ry = Mathf.Clamp(ry, yMinAllowed, yMaxAllowed);

            return new Vector3(rx, ry, transform.localPosition.z);
        }

        private void ClampInsideBounds(ref Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, localBoundsMin.x, localBoundsMax.x);
            float xRatio = Mathf.Abs(pos.x) / Mathf.Max(0.1f, localBoundsMax.x);
            float yMaxAllowed = Mathf.Lerp(localBoundsMax.y, 0f, xRatio * 0.60f);
            float yMinAllowed = Mathf.Lerp(localBoundsMin.y, 0f, xRatio * 0.60f);
            pos.y = Mathf.Clamp(pos.y, yMinAllowed, yMaxAllowed);
        }

        private void SetMoving(bool moving)
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, moving ? 1f : 0f);
            }
        }

        private void SetFacing(float sign)
        {
            Vector3 scale = transform.localScale;
            scale.x = originalScaleX * sign;
            transform.localScale = scale;
        }

        private void SetupDefaultAudioClipsIfMissing()
        {
            if (soundClips != null && soundClips.Length > 0) return;

            string n = (gameObject.name + " " + (transform.parent != null ? transform.parent.name : "")).ToLowerInvariant();
            List<AudioClip> loaded = new List<AudioClip>();

            if (n.Contains("chicken") || n.Contains("ga") || n.Contains("gà"))
            {
                var c1 = Resources.Load<AudioClip>("Audio/Animals/Chicken-001");
                var c2 = Resources.Load<AudioClip>("Audio/Animals/Chicken-002");
                if (c1 != null) loaded.Add(c1);
                if (c2 != null) loaded.Add(c2);
            }
            else if (n.Contains("pig") || n.Contains("heo") || n.Contains("lợn"))
            {
                var c1 = Resources.Load<AudioClip>("Audio/Animals/Pig-001");
                var c2 = Resources.Load<AudioClip>("Audio/Animals/Pig-002");
                if (c1 != null) loaded.Add(c1);
                if (c2 != null) loaded.Add(c2);
            }
            else
            {
                var c1 = Resources.Load<AudioClip>("Audio/Animals/Cow-001");
                var c2 = Resources.Load<AudioClip>("Audio/Animals/Cow-002");
                if (c1 != null) loaded.Add(c1);
                if (c2 != null) loaded.Add(c2);
            }

            if (loaded.Count > 0)
                soundClips = loaded.ToArray();
        }

        public void PlayAnimalSound(bool forced = false)
        {
            SetupDefaultAudioClipsIfMissing();
            if (soundClips == null || soundClips.Length == 0 || audioSource == null) return;

            // Kiểm tra camera zoom và khoảng cách nếu không phải tương tác trực tiếp
            if (!forced)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                float dist = Vector2.Distance(cam.transform.position, transform.position);
                if (dist > 12f || (cam.orthographic && cam.orthographicSize > 12f))
                    return;
            }

            AudioClip clip = soundClips[Random.Range(0, soundClips.Length)];
            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.92f, 1.08f);
                float vol = forced ? Mathf.Min(1f, soundVolume * 1.35f) : (soundVolume * 0.40f);
                audioSource.PlayOneShot(clip, vol);
            }
        }

        private void OnMouseDown()
        {
            if (EditModeManager.IsEditMode) return;
            PlayAnimalSound(true);
            if (parentPen != null && !parentPen.IsPanelOpen())
                parentPen.OpenPanel();
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ hộp giới hạn di chuyển trong Editor
            Transform p = transform.parent != null ? transform.parent : transform;
            Vector3 center = p.TransformPoint(new Vector3((localBoundsMin.x + localBoundsMax.x) * 0.5f, (localBoundsMin.y + localBoundsMax.y) * 0.5f, 0f));
            Vector3 size = new Vector3(Mathf.Abs(localBoundsMax.x - localBoundsMin.x), Mathf.Abs(localBoundsMax.y - localBoundsMin.y), 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
