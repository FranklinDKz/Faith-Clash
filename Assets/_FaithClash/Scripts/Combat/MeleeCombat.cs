using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Combat
{
    /// <summary>
    /// Training-mode melee: 3-hit light combo (LMB), heavy attack (RMB), block (Q).
    /// Hits are resolved with an overlap sphere in front of the player at the impact moment of each swing.
    /// Visuals are procedural until real animations arrive.
    /// </summary>
    public class MeleeCombat : MonoBehaviour
    {
        struct Attack
        {
            public float damage, duration, impactTime, reach, radius, lunge;
        }

        [SerializeField] Transform weaponPivot;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float comboWindow = 0.6f;

        static readonly Attack[] Light =
        {
            new Attack { damage = 8,  duration = 0.40f, impactTime = 0.15f, reach = 1.8f, radius = 0.9f, lunge = 2f },
            new Attack { damage = 10, duration = 0.40f, impactTime = 0.15f, reach = 1.8f, radius = 0.9f, lunge = 2f },
            new Attack { damage = 16, duration = 0.60f, impactTime = 0.25f, reach = 2.0f, radius = 1.1f, lunge = 4f },
        };
        static readonly Attack Heavy = new Attack { damage = 28, duration = 0.85f, impactTime = 0.45f, reach = 2.1f, radius = 1.2f, lunge = 3f };

        PlayerController controller;
        CharacterController cc;
        Attack current;
        bool attacking, impactDone, heavy, queuedLight;
        float attackTimer, lastAttackEnd, comboResetAt;
        int comboIndex;

        public int ComboCount { get; private set; }
        public bool IsBlocking { get; private set; }
        public event Action<float> HitLanded;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            bool lightPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
            bool heavyPressed = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
            IsBlocking = !attacking && !controller.IsDodging &&
                         ((Keyboard.current != null && Keyboard.current.qKey.isPressed)
                          || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed));

            controller.MoveMultiplier = attacking ? 0.25f : IsBlocking ? 0.4f : 1f;

            if (attacking)
            {
                TickAttack();
                if (lightPressed && !heavy) queuedLight = true;
            }
            else if (!controller.IsDodging)
            {
                if (heavyPressed) Begin(Heavy, true);
                else if (lightPressed) StartLight();
            }

            if (!attacking && ComboCount > 0 && Time.time > comboResetAt) { ComboCount = 0; comboIndex = 0; }
        }

        void StartLight()
        {
            if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
            Begin(Light[comboIndex % Light.Length], false);
        }

        void Begin(Attack a, bool isHeavy)
        {
            current = a; heavy = isHeavy; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false;
            if (weaponPivot) weaponPivot.localRotation = Quaternion.identity;
        }

        void TickAttack()
        {
            attackTimer += Time.deltaTime;
            float t = attackTimer / current.duration;

            if (current.lunge > 0f && attackTimer < current.impactTime)
                cc.Move(transform.forward * (current.lunge * Time.deltaTime));

            if (weaponPivot)
            {
                bool even = comboIndex % 2 == 0;
                if (heavy) weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-110f, 80f, t), 0f, 0f);
                else weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(even ? -80f : 80f, even ? 80f : -80f, t), 0f);
            }

            if (!impactDone && attackTimer >= current.impactTime) { impactDone = true; ResolveHit(); }

            if (attackTimer >= current.duration)
            {
                attacking = false;
                lastAttackEnd = Time.time;
                if (weaponPivot) weaponPivot.localRotation = Quaternion.identity;
                comboIndex = heavy ? 0 : comboIndex + 1;
                if (queuedLight) StartLight();
            }
        }

        void ResolveHit()
        {
            Vector3 center = transform.position + Vector3.up + transform.forward * (current.reach * 0.6f);
            bool any = false;
            foreach (var col in Physics.OverlapSphere(center, current.radius, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                target.TakeDamage(current.damage, col.ClosestPoint(center), gameObject);
                HitLanded?.Invoke(current.damage);
                any = true;
            }
            if (any) { ComboCount++; comboResetAt = Time.time + comboWindow + 1f; }
        }
    }
}
