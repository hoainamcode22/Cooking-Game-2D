// Disabled: duplicate of HappyHarvest/Scripts/Effects/EmitParticles.cs
#if false
using UnityEngine;

namespace HappyHarvest
{
    public class EmitParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private int particleCount = 1;


        public void Emit()
        {
            particles.Emit(particleCount);
        }
    }
}
#endif // false
