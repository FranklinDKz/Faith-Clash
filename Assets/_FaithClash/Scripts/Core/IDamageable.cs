using UnityEngine;

namespace FaithClash.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(float amount, Vector3 hitPoint, GameObject source);
    }
}
