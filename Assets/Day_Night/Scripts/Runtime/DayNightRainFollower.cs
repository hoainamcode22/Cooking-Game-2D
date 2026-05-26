using UnityEngine;

namespace Day_Night
{
    [ExecuteAlways]
    public class DayNightRainFollower : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset;
        public bool FollowZ;
        public bool FollowInEditMode;

        private void LateUpdate()
        {
            if (!Application.isPlaying && !FollowInEditMode)
            {
                return;
            }

            Transform target = Target;

            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (target == null)
            {
                return;
            }

            Vector3 nextPosition = target.position + Offset;
            if (!FollowZ)
            {
                nextPosition.z = transform.position.z;
            }

            transform.position = nextPosition;
        }
    }
}
