using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Farm.UI
{
    [RequireComponent(typeof(Button))]
    public class UIRewardButtonFX : MonoBehaviour, IPointerUpHandler
    {
        [Header("FX Settings")]
        [SerializeField] private GameObject particlePrefab;

        public void OnPointerUp(PointerEventData eventData)
        {
            PlayFX();
        }

        private void PlayFX()
        {
            if (particlePrefab != null)
            {
                Instantiate(particlePrefab, transform.position, Quaternion.identity, transform.parent);
            }
            else
            {
                Debug.Log($"[UIRewardButtonFX] Particle effect triggered on {gameObject.name}");
            }
        }
    }
}
