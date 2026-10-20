using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Combat
{
    /// <summary>
    /// Corpo a corpo: combo leve de 4 golpes (botão esquerdo), golpe pesado (botão direito) e defesa (F).
    /// Com alvo travado (Q) os golpes giram para o alvo e avançam até ele, no estilo Naruto Storm.
    /// O acerto é resolvido com uma esfera na frente do jogador no momento de impacto de cada golpe.
    /// Os estilos elementais escutam <see cref="AttackStarted"/> e <see cref="AttackImpact"/> para soltar técnicas.
    /// </summary>
    public class MeleeCombat : MonoBehaviour
    {
        struct Attack
        {
            public float damage, duration, impactTime, reach, radius, lunge;
            public string clip;
        }

        [SerializeField] Transform weaponPivot;
        [SerializeField] AnimadorProtagonista animador;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float comboWindow = 0.6f;
        [SerializeField] float comboCounterReset = 2.5f;
        [SerializeField] float maxHomingDistance = 7f;

        static readonly Attack[] Light =
        {
            new Attack { damage = 8,  duration = 0.40f, impactTime = 0.15f, reach = 1.8f, radius = 0.9f, lunge = 2f, clip = "Ataque1" },
            new Attack { damage = 10, duration = 0.40f, impactTime = 0.15f, reach = 1.8f, radius = 0.9f, lunge = 2f, clip = "Ataque2" },
            new Attack { damage = 12, duration = 0.40f, impactTime = 0.15f, reach = 1.9f, radius = 1.0f, lunge = 2.5f, clip = "Ataque1" },
            new Attack { damage = 18, duration = 0.60f, impactTime = 0.25f, reach = 2.0f, radius = 1.1f, lunge = 4f, clip = "Ataque3" },
        };
        static readonly Attack Heavy = new Attack { damage = 28, duration = 0.85f, impactTime = 0.45f, reach = 2.1f, radius = 1.2f, lunge = 3f, clip = "AtaquePesado" };

        PlayerController controller;
        CharacterController cc;
        TravaAlvo trava;
        Transform homingTarget;
        float homingSpeed;
        Attack current;
        bool attacking, impactDone, heavy, queuedLight;
        float attackTimer, lastAttackEnd, comboResetAt;
        int comboIndex, currentStep;

        /// <summary>Acertos seguidos (contador "Combo" do HUD).</summary>
        public int ComboCount { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool IsAttacking => attacking;
        /// <summary>Multiplicador de dano (itens / buffs).</summary>
        public float DamageMultiplier { get; set; } = 1f;
        /// <summary>Quando verdadeiro, ignora a entrada de ataque (ex.: técnica segurando o botão).</summary>
        public bool InputLocked { get; set; }

        public event Action<float> HitLanded;
        /// <summary>(passo do combo 1..4 ou 0 no golpe pesado)</summary>
        public event Action<int> AttackStarted;
        /// <summary>(passo do combo 1..4 ou 0 no golpe pesado, acertou alguém)</summary>
        public event Action<int, bool> AttackImpact;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
            trava = GetComponent<TravaAlvo>();
        }

        void Update()
        {
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked;
            bool lightPressed = !InputLocked && cursorPreso && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame));
            bool heavyPressed = !InputLocked && cursorPreso && ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame));
            IsBlocking = !attacking && !controller.IsDodging &&
                         ((Keyboard.current != null && Keyboard.current.fKey.isPressed)
                          || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed));

            controller.MoveMultiplier = attacking ? 0.25f : IsBlocking ? 0.4f : 1f;
            if (animador != null) animador.Defendendo = IsBlocking;

            if (attacking)
            {
                TickAttack();
                if (lightPressed && !heavy) queuedLight = true;
            }
            else if (!controller.IsDodging)
            {
                if (heavyPressed) Begin(Heavy, true, 0);
                else if (lightPressed) StartLight();
            }

            if (ComboCount > 0 && Time.time > comboResetAt) ComboCount = 0;
        }

        void StartLight()
        {
            if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
            int i = comboIndex % Light.Length;
            Begin(Light[i], false, i + 1);
        }

        void Begin(Attack a, bool isHeavy, int step)
        {
            current = a; heavy = isHeavy; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false;
            currentStep = step;
            homingTarget = null;
            if (trava != null && trava.Travado)
            {
                Vector3 v = Vector3.ProjectOnPlane(trava.Alvo.position - transform.position, Vector3.up);
                if (v.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(v);
                float folga = v.magnitude - a.reach * 0.75f;
                if (folga > 0f && v.magnitude <= maxHomingDistance)
                {
                    homingTarget = trava.Alvo;
                    homingSpeed = Mathf.Min(18f, folga / Mathf.Max(0.05f, a.impactTime));
                }
            }
            if (animador != null) animador.Acao(a.clip, a.duration);
            if (weaponPivot) weaponPivot.localRotation = Quaternion.identity;
            AttackStarted?.Invoke(step);
        }

        void TickAttack()
        {
            attackTimer += Time.deltaTime;
            float t = attackTimer / current.duration;

            if (attackTimer < current.impactTime)
            {
                if (homingTarget != null)
                {
                    Vector3 v = Vector3.ProjectOnPlane(homingTarget.position - transform.position, Vector3.up);
                    if (v.magnitude > current.reach * 0.75f)
                        cc.Move(v.normalized * (Mathf.Max(homingSpeed, current.lunge) * Time.deltaTime));
                }
                else if (current.lunge > 0f)
                    cc.Move(transform.forward * (current.lunge * Time.deltaTime));
            }

            if (weaponPivot)
            {
                bool even = comboIndex % 2 == 0;
                if (heavy) weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-110f, 80f, t), 0f, 0f);
                else weaponPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(even ? -80f : 80f, even ? 80f : -80f, t), 0f);
            }

            if (!impactDone && attackTimer >= current.impactTime)
            {
                impactDone = true;
                bool acertou = ResolveHit();
                AttackImpact?.Invoke(currentStep, acertou);
            }

            if (attackTimer >= current.duration)
            {
                attacking = false;
                lastAttackEnd = Time.time;
                if (weaponPivot) weaponPivot.localRotation = Quaternion.identity;
                comboIndex = heavy ? 0 : comboIndex + 1;
                if (queuedLight) StartLight();
            }
        }

        bool ResolveHit()
        {
            Vector3 center = transform.position + Vector3.up + transform.forward * (current.reach * 0.6f);
            bool any = false;
            foreach (var col in Physics.OverlapSphere(center, current.radius, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                float dano = current.damage * DamageMultiplier;
                target.TakeDamage(dano, col.ClosestPoint(center), gameObject);
                HitLanded?.Invoke(dano);
                any = true;
            }
            if (any) RegisterComboHit();
            return any;
        }

        /// <summary>Conta um acerto no combo (também usado pelas técnicas elementais).</summary>
        public void RegisterComboHit()
        {
            ComboCount++;
            comboResetAt = Time.time + comboCounterReset;
        }
    }
}
