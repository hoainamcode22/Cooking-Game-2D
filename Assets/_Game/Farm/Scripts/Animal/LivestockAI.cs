using UnityEngine;
using System.Collections;

namespace Assetsgame.Animals
{
    public class LivestockAI : MonoBehaviour
    {
        [Header("Movement Config")]
        public float roamRadius = 1.5f;
        public float walkSpeed = 0.5f;
        public float fastWalkSpeed = 1.2f;
        
        [Header("State Durations")]
        public float minIdleTimeHungry = 3f;
        public float maxIdleTimeHungry = 7f;
        public float minIdleTimeFed = 1f;
        public float maxIdleTimeFed = 3f;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private bool isMoving;
        private PenMiniPanelUI parentPen;
        private Coroutine roamCoroutine;
        private SpriteRenderer[] renderers;
        private UnityEngine.Rendering.SortingGroup sortingGroup;

        private void Start()
        {
            startPosition = transform.localPosition;
            targetPosition = startPosition;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            
            sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                sortingGroup.sortingLayerName = "CongTrinh";
            }
            
            // Find the pen this animal belongs to
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

            roamCoroutine = StartCoroutine(RoamRoutine());
        }

        private void Update()
        {
            UpdateSorting();
        }

        private void UpdateSorting()
        {
            if (sortingGroup != null)
            {
                // Base 500 để không bị chìm xuống dưới đất (World/Grass thường ở order thấp hơn)
                // Dùng localPosition.y thay vì position.y để chỉ tính lệch nội bộ trong chuồng,
                // tránh việc Y thế giới quá lớn làm order bị âm -> chìm dưới đất.
                sortingGroup.sortingOrder = 500 + Mathf.RoundToInt(transform.localPosition.y * -100f);
            }
        }

        private IEnumerator RoamRoutine()
        {
            while (true)
            {
                // Determine current state
                PenMiniPanelUI.PenState state = PenMiniPanelUI.PenState.Idle;
                if (parentPen != null)
                {
                    state = parentPen.CurrentState;
                }

                if (state == PenMiniPanelUI.PenState.Ready)
                {
                    // Ready to harvest: stand still near the front/start
                    if (Vector3.Distance(transform.localPosition, startPosition) > 0.1f)
                    {
                        MoveTowardsLocal(startPosition, walkSpeed);
                    }
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                if (!isMoving)
                {
                    // Pick a random point within roam radius
                    Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
                    targetPosition = startPosition + new Vector3(randomCircle.x, randomCircle.y, 0);
                    isMoving = true;
                }

                // Move towards target
                float speed = state == PenMiniPanelUI.PenState.Processing ? fastWalkSpeed : walkSpeed;
                MoveTowardsLocal(targetPosition, speed);

                // Flip sprite based on direction by scaling parent X
                float dirX = targetPosition.x - transform.localPosition.x;
                if (Mathf.Abs(dirX) > 0.05f)
                {
                    float sign = dirX < 0 ? -1f : 1f;
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * sign;
                    transform.localScale = scale;
                }

                // Check if reached target
                if (Vector3.Distance(transform.localPosition, targetPosition) < 0.1f)
                {
                    isMoving = false;
                    
                    // Idle based on state
                    float waitTime = state == PenMiniPanelUI.PenState.Processing 
                        ? Random.Range(minIdleTimeFed, maxIdleTimeFed) 
                        : Random.Range(minIdleTimeHungry, maxIdleTimeHungry);
                    
                    yield return new WaitForSeconds(waitTime);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void MoveTowardsLocal(Vector3 target, float speed)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }
    }
}
