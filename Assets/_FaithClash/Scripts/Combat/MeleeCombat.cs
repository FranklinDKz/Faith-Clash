using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Ataques corpo a corpo, por modo:
    ///  - Espada: combo de 4 cortes (botão esquerdo) e golpe pesado com salto (botão direito).
    ///  - Poder: combo de punhos/chutes (estilo Naruto) e chute pesado. O 3º e 4º golpe liberam as técnicas elementais.
    /// F defende. Sem alvo travado há uma assistência leve: o personagem vira um pouco em direção ao inimigo mais
    /// próximo na frente dele ao atacar. Com alvo travado (Q) ele encara o alvo e avança até ele.
    /// </summary>
    public class MeleeCombat : MonoBehaviour
    {
        struct Golpe
        {
            public string clip;
            public float dano, duracao, impacto, alcance, raio, avanco;
        }

        [SerializeField] AnimadorProtagonista animador;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float comboWindow = 0.7f;
        [SerializeField] float comboCounterReset = 2.5f;
        [SerializeField] float maxHomingDistance = 7f;
        [Header("Assistência de mira (sem lock-on)")]
        [SerializeField] float assistDistancia = 7f;
        [SerializeField] float assistAngulo = 50f;
        [SerializeField] float assistGiroMaximo = 28f;

        static readonly Golpe[] CombosEspada =
        {
            new Golpe { clip = "Corte1", dano = 14, duracao = 0.62f, impacto = 0.45f, alcance = 2.2f, raio = 1.1f, avanco = 3f },
            new Golpe { clip = "Corte2", dano = 15, duracao = 0.50f, impacto = 0.27f, alcance = 2.2f, raio = 1.1f, avanco = 3f },
            new Golpe { clip = "Corte3", dano = 17, duracao = 0.65f, impacto = 0.27f, alcance = 2.3f, raio = 1.2f, avanco = 3.5f },
            new Golpe { clip = "Estocada", dano = 26, duracao = 1.0f, impacto = 0.67f, alcance = 2.6f, raio = 1.2f, avanco = 5f },
        };
        static readonly Golpe PesadoEspada = new Golpe { clip = "EspadaSalto", dano = 40, duracao = 1.1f, impacto = 0.67f, alcance = 2.8f, raio = 1.5f, avanco = 5f };

        static readonly Golpe[] CombosPoder =
        {
            new Golpe { clip = "Soco1", dano = 8, duracao = 0.55f, impacto = 0.27f, alcance = 1.8f, raio = 0.9f, avanco = 2.5f },
            new Golpe { clip = "Soco2", dano = 10, duracao = 0.58f, impacto = 0.44f, alcance = 1.8f, raio = 0.9f, avanco = 2.5f },
            new Golpe { clip = "Soco3", dano = 12, duracao = 0.55f, impacto = 0.48f, alcance = 1.9f, raio = 1.0f, avanco = 3f },
            new Golpe { clip = "Martelo", dano = 18, duracao = 0.8f, impacto = 0.29f, alcance = 2.0f, raio = 1.1f, avanco = 3.5f },
        };
        static readonly Golpe PesadoPoder = new Golpe { clip = "Martelo", dano = 28, duracao = 0.85f, impacto = 0.29f, alcance = 2.1f, raio = 1.2f, avanco = 3f };

        PlayerController controller;
        CharacterController cc;
        TravaAlvo trava;
        ModoCombate modo;
        Golpe current;
        Transform homingTarget;
        float homingSpeed;
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
        /// <summary>(passo do combo 1..4, 0 no golpe pesado)</summary>
        public event Action<int> AttackStarted;
        /// <summary>(passo do combo 1..4 ou 0 no golpe pesado, acertou alguém)</summary>
        public event Action<int, bool> AttackImpact;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
            trava = GetComponent<TravaAlvo>();
            modo = GetComponent<ModoCombate>();
        }

        void Update()
        {
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked;
            bool lightPressed = !InputLocked && cursorPreso && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame));
            bool heavyPressed = !InputLocked && cursorPreso && ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame));
            IsBlocking = !attacking && !controller.IsBusy && cursorPreso &&
                         ((Keyboard.current != null && Keyboard.current.fKey.isPressed)
                          || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed));

            controller.MoveMultiplier = attacking ? 0.25f : IsBlocking ? 0.4f : 1f;
            if (animador != null) animador.Defendendo = IsBlocking;

            if (attacking)
            {
                TickAttack();
                if (lightPressed && !heavy) queuedLight = true;
            }
            else if (!controller.IsBusy)
            {
                if (heavyPressed) Begin(Pesado, true, 0);
                else if (lightPressed) StartLight();
            }

            if (ComboCount > 0 && Time.time > comboResetAt) ComboCount = 0;
        }

        bool EmEspada => modo != null && modo.Atual == Modo.Espada;
        Golpe[] Combo => EmEspada ? CombosEspada : CombosPoder;
        Golpe Pesado => EmEspada ? PesadoEspada : PesadoPoder;

        void StartLight()
        {
            if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
            int i = comboIndex % Combo.Length;
            Begin(Combo[i], false, i + 1);
        }

        void Begin(Golpe a, bool isHeavy, int step)
        {
            current = a; heavy = isHeavy; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false;
            currentStep = step;
            homingTarget = null;
            AjustaMira(a);
            if (animador != null) animador.Acao(a.clip, a.duracao);
            AttackStarted?.Invoke(step);
        }

        /// <summary>Com alvo travado encara o alvo e avança; sem alvo, assistência leve de mira.</summary>
        void AjustaMira(Golpe a)
        {
            if (trava != null && trava.Travado)
            {
                Vector3 v = Vector3.ProjectOnPlane(trava.Alvo.position - transform.position, Vector3.up);
                if (v.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(v);
                float folga = v.magnitude - a.alcance * 0.75f;
                if (folga > 0f && v.magnitude <= maxHomingDistance)
                {
                    homingTarget = trava.Alvo;
                    homingSpeed = Mathf.Min(18f, folga / Mathf.Max(0.05f, a.impacto));
                }
                return;
            }

            Transform alvo = null;
            float melhor = float.MaxValue;
            foreach (var d in TrainingDummy.Todos)
            {
                if (d.Vida.IsDead) continue;
                Vector3 v = Vector3.ProjectOnPlane(d.transform.position - transform.position, Vector3.up);
                float dist = v.magnitude;
                if (dist > assistDistancia || dist < 0.05f) continue;
                float ang = Vector3.Angle(transform.forward, v);
                if (ang > assistAngulo) continue;
                float nota = ang + dist * 3f;
                if (nota < melhor) { melhor = nota; alvo = d.transform; }
            }
            if (alvo == null) return;
            Vector3 dir = Vector3.ProjectOnPlane(alvo.position - transform.position, Vector3.up);
            Quaternion desejada = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desejada, assistGiroMaximo);
        }

        void TickAttack()
        {
            attackTimer += Time.deltaTime;

            if (attackTimer < current.impacto)
            {
                if (homingTarget != null)
                {
                    Vector3 v = Vector3.ProjectOnPlane(homingTarget.position - transform.position, Vector3.up);
                    if (v.magnitude > current.alcance * 0.75f)
                        cc.Move(v.normalized * (Mathf.Max(homingSpeed, current.avanco) * Time.deltaTime));
                }
                else if (current.avanco > 0f)
                    cc.Move(transform.forward * (current.avanco * Time.deltaTime));
            }

            if (!impactDone && attackTimer >= current.impacto)
            {
                impactDone = true;
                bool acertou = ResolveHit();
                AttackImpact?.Invoke(currentStep, acertou);
            }

            if (attackTimer >= current.duracao)
            {
                attacking = false;
                lastAttackEnd = Time.time;
                comboIndex = heavy ? 0 : comboIndex + 1;
                if (queuedLight) StartLight();
            }
        }

        bool ResolveHit()
        {
            Vector3 center = transform.position + Vector3.up + transform.forward * (current.alcance * 0.6f);
            bool any = false;
            foreach (var col in Physics.OverlapSphere(center, current.raio, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                float dano = current.dano * DamageMultiplier;
                target.TakeDamage(dano, col.ClosestPoint(center), gameObject);
                HitLanded?.Invoke(dano);
                any = true;
            }
            if (any) RegisterComboHit();
            return any;
        }

        /// <summary>Conta um acerto no combo (também usado pelas técnicas).</summary>
        public void RegisterComboHit()
        {
            ComboCount++;
            comboResetAt = Time.time + comboCounterReset;
        }
    }
}
