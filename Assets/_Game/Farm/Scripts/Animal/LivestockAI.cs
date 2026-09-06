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
        public Vector2 localBoundsMin = new Vector2(-1.25f, 1.25f);
        [Tooltip("Giới hạn di chuyển lớn nhất bên trong hàng rào chuồng")]
        public Vector2 localBoundsMax = new Vector2(1.25f, 2.50f);
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

        [Header("Sorting (con vat PHAI NOI TREN rao chuong - bug Sep bao 2026-09-06)")]
        // [BUG Sep bao 2026-09-06] "CongTrinh" KHONG ton tai trong ProjectSettings/TagManager.asset
        // (chi co Bottom/Default/Objects/ObjectsFront/Foreground - xem TouristSortingLayers.cs).
        // Rao chuong (BarnSprite, Pen_01..04.prefab) cung dang ket o 1 sorting layer ID da bi xoa
        // (1669604809) - layer "ma" do rat co the chinh la "CongTrinh" cu. Vi KHONG chac Unity dang
        // xep rao vao dau (khong mo duoc Unity Editor de kiem tai day), con vat PHAI dung tren 1
        // sorting layer THAT, KHONG duoc tiep tuc hardcode ten layer khong ton tai nhu ban cu -
        // giong het bug "CongTrinh" da gap o khach du lich (xem TouristSortingLayers.cs). TAI DUNG
        // nguyen bo uu tien Visitor cua khach, KHONG bia quy uoc moi. "Objects" o day KHONG phai
        // "ObjectsFront" vi "ObjectsFront" da danh rieng cho tau khach (TrainPathFollower, order 650).
        [Tooltip("Sorting layer cua con vat. DE TRONG (mac dinh) = tu giai theo " +
                 "TouristSortingLayers.Visitor ('Objects', du phong 'Default'). Go ten khac chi khi " +
                 "layer do CO THAT trong Project Settings > Tags and Layers.")]
        public string sortingLayerName = "";
        [Tooltip("Order goc truoc khi cong phan Y-dong (xem UpdateDynamicSorting). Co san kep " +
                 "FenceSortingOrderFloor ben duoi de khong bao gio tut xuong duoi rao.")]
        public int baseSortingOrder = 600;

        /// <summary>
        /// San kep sortingOrder - PHAI lon hon order co dinh 500 ma BarnSprite dang dung o CA 4
        /// prefab Pen_01/02/03/04 (da kiem tra thuc te trong file .prefab). Rao hien chi co DUNG 1
        /// SpriteRenderer duy nhat (chuongmoigiasuc.png, phu ca 4 canh chuong) nen KHONG THE vua cho
        /// con vat noi tren rao-truoc vua chim sau rao-sau bang so - san nay CHON luon noi tren toan
        /// bo rao, chap nhan mat che khuat rao-truoc cho toi khi co art 2 lop (xem spec DEV C).
        /// </summary>
        public const int FenceSortingOrderFloor = 512;

        [Header("Chan doan sorting (DEV D 2026-09-06)")]
        [Tooltip("Bat = in log [Livestock] ra Console: layer + order THAT SU cua con vat va cua " +
                 "BarnSprite (rao chuong), de doc 1 dong la biet ngay ai dang nam tren ai.")]
        public bool logSortingDiagnostics = true;

        private Vector3 startLocalPos;
        private Vector3 targetLocalPos;
        private bool isMoving;
        private PenMiniPanelUI parentPen;
        private Animator animator;
        private SortingGroup sortingGroup;
        private AudioSource audioSource;
        private Coroutine roamCoroutine;
        private float originalScaleX;
        private string _resolvedSortingLayerName;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
                sortingGroup = gameObject.AddComponent<SortingGroup>();

            // Giai layer NGAY luc Awake (tai dung TouristSortingLayers - xem comment o [Header]
            // phia tren): field cu/prefab cu co the con luu "CongTrinh" (khong ton tai).
            _resolvedSortingLayerName = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);
            sortingGroup.sortingLayerName = _resolvedSortingLayerName;
            sortingGroup.sortingOrder = Mathf.Max(baseSortingOrder, FenceSortingOrderFloor);

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

            if (logSortingDiagnostics)
                StartCoroutine(SortingDiagnosticRoutine());

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
                sortingGroup.sortingLayerName = _resolvedSortingLayerName;
                int order = baseSortingOrder + Mathf.RoundToInt(-transform.localPosition.y * 50f);
                // San kep: KHONG BAO GIO de con vat tut xuong duoi order co dinh cua rao chuong (500).
                sortingGroup.sortingOrder = Mathf.Max(order, FenceSortingOrderFloor);
            }
        }

        /// <summary>
        /// [CHAN DOAN 2026-09-06 - DEV D] In ra Console layer + order THAT SU cua con vat va cua
        /// BarnSprite (rao chuong), kem ket luan ai nam tren ai. Log 2 moc:
        ///   - "frame-1": ngay sau khi moi Awake/Start cua scene da chay xong.
        ///   - "sau-1s" : bat cac script ghi de MUON (vi du PlacementManager.FixBuildingRenderSorting
        ///                chay luc dat/di doi cong trinh).
        /// So sanh dung thu tu Unity: layer VALUE truoc, cung layer moi xet den order.
        /// </summary>
        private IEnumerator SortingDiagnosticRoutine()
        {
            yield return null;
            LogSortingDiagnostic("frame-1");

            yield return new WaitForSeconds(1f);
            LogSortingDiagnostic("sau-1s");
        }

        private void LogSortingDiagnostic(string moc)
        {
            if (sortingGroup == null) return;

            string conVatLayer = sortingGroup.sortingLayerName;
            int    conVatOrder = sortingGroup.sortingOrder;
            int    conVatValue = SortingLayer.GetLayerValueFromName(conVatLayer);
            int    conVatId    = sortingGroup.sortingLayerID;

            // Tim "BarnSprite" (rao chuong) o bat ky doi cha nao phia tren.
            SpriteRenderer barn = null;
            Transform p = transform.parent;
            while (p != null && barn == null)
            {
                Transform t = p.Find("BarnSprite");
                if (t != null) barn = t.GetComponent<SpriteRenderer>();
                p = p.parent;
            }

            if (barn == null)
            {
                Debug.Log($"[Livestock] {name} ({moc}): CON VAT layer='{conVatLayer}' id={conVatId} " +
                          $"value={conVatValue} order={conVatOrder} || KHONG tim thay 'BarnSprite' o cay cha.", this);
                return;
            }

            string raoLayer = barn.sortingLayerName;
            int    raoOrder = barn.sortingOrder;
            int    raoValue = SortingLayer.GetLayerValueFromName(raoLayer);
            int    raoId    = barn.sortingLayerID;

            bool conVatOTren = (conVatValue != raoValue) ? (conVatValue > raoValue)
                                                         : (conVatOrder > raoOrder);

            Debug.Log($"[Livestock] {name} ({moc}): " +
                      $"CON VAT layer='{conVatLayer}' id={conVatId} value={conVatValue} order={conVatOrder}  ||  " +
                      $"RAO 'BarnSprite' layer='{raoLayer}' id={raoId} value={raoValue} order={raoOrder}  =>  " +
                      (conVatOTren ? "CON VAT VE TREN RAO (dung y do ban va)"
                                   : "RAO DE LEN CON VAT (SAI - bao Dev D)"), this);
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

            // Tâm sàn chuồng theo trục Y
            float centerY = (localBoundsMin.y + localBoundsMax.y) * 0.5f;
            float halfH = (localBoundsMax.y - localBoundsMin.y) * 0.5f;
            float halfW = Mathf.Max(0.1f, Mathf.Max(Mathf.Abs(localBoundsMin.x), Mathf.Abs(localBoundsMax.x)));

            // Điều chỉnh biên góc để con vật đi chuẩn trong sàn quả trám (isometric diamond)
            float xRatio = Mathf.Clamp01(Mathf.Abs(rx) / halfW);
            float ySpread = halfH * (1f - xRatio * 0.55f);
            float yMaxAllowed = centerY + ySpread;
            float yMinAllowed = centerY - ySpread;
            ry = Mathf.Clamp(ry, yMinAllowed, yMaxAllowed);

            return new Vector3(rx, ry, transform.localPosition.z);
        }

        private void ClampInsideBounds(ref Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, localBoundsMin.x, localBoundsMax.x);
            float centerY = (localBoundsMin.y + localBoundsMax.y) * 0.5f;
            float halfH = (localBoundsMax.y - localBoundsMin.y) * 0.5f;
            float halfW = Mathf.Max(0.1f, Mathf.Max(Mathf.Abs(localBoundsMin.x), Mathf.Abs(localBoundsMax.x)));

            float xRatio = Mathf.Clamp01(Mathf.Abs(pos.x) / halfW);
            float ySpread = halfH * (1f - xRatio * 0.55f);
            float yMaxAllowed = centerY + ySpread;
            float yMinAllowed = centerY - ySpread;
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
                // [FIX 2026-09-06] Nhan he so am luong chung — truoc day tieng gia suc
                // KHONG theo thanh truot "Am thanh VFX" trong Cai dat.
                audioSource.PlayOneShot(clip, vol * AudioManager.SfxGain);
            }
        }

        private void OnMouseDown()
        {
        // [FIX 2026-09-04] Chặn click xuyên khi đang ở Bếp (scene phụ load additive) / đang mở popup.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
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
