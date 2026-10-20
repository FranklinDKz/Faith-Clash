using System;
using UnityEngine;

namespace FaithClash.Core
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public event Action<float, Vector3, GameObject> Damaged;
        public event Action Died;

        void Awake() => Current = maxHealth;

        public void TakeDamage(float amount, Vector3 hitPoint, GameObject source)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, hitPoint, source);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        public void ResetHealth() => Current = maxHealth;
    }
}
