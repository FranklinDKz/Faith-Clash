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
            public float dano, duracao, impacto, alcance, raio, desloc;   // desloc = quanto o clipe "anda" para a frente (m)
        }

        [SerializeField] AnimadorProtagonista animador;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float comboWindow = 0.7f;
        [SerializeField] float comboCounterReset = 2.5f;
        [SerializeField] float tempoPesado = 0.28f;   // segurando o botão esquerdo por esse tempo = golpe pesado
        [SerializeField] float maxHomingDistance = 7f;
        [Header("Assistência de mira (sem lock-on)")]
        [SerializeField] float assistDistancia = 7f;
        [SerializeField] float assistAngulo = 50f;
        [SerializeField] float assistGiroMaximo = 28f;

        // tempos medidos nos clipes do Mixamo (duracao e impacto em segundos, velocidade 1)
        static readonly Golpe[] CombosEspada =
        {
            new Golpe { clip = "Corte1", dano = 14, duracao = 1.20f, impacto = 0.70f, alcance = 2.2f, raio = 1.1f, desloc = 1.0f },
            new Golpe { clip = "Corte2", dano = 15, duracao = 0.93f, impacto = 0.42f, alcance = 2.2f, raio = 1.1f, desloc = 0.9f },
            new Golpe { clip = "Corte3", dano = 17, duracao = 1.37f, impacto = 0.38f, alcance = 2.3f, raio = 1.2f, desloc = 1.0f },
            new Golpe { clip = "CorteGiratorio", dano = 26, duracao = 2.40f, impacto = 1.25f, alcance = 2.6f, raio = 1.4f, desloc = 0.95f },
        };
        static readonly Golpe PesadoEspada = new Golpe { clip = "EspadaSalto", dano = 40, duracao = 2.10f, impacto = 1.00f, alcance = 2.8f, raio = 1.5f, desloc = 2.9f };
        static readonly Golpe CorridaEspada = new Golpe { clip = "AtaqueDeslizante", dano = 30, duracao = 2.10f, impacto = 1.10f, alcance = 2.6f, raio = 1.4f, desloc = 3.2f };

        static readonly Golpe[] CombosPoder =
        {
            new Golpe { clip = "Soco1", dano = 8, duracao = 0.93f, impacto = 0.40f, alcance = 1.8f, raio = 0.9f, desloc = 0.6f },
            new Golpe { clip = "Soco2", dano = 10, duracao = 0.90f, impacto = 0.65f, alcance = 1.8f, raio = 0.9f, desloc = 0.8f },
            new Golpe { clip = "Soco3", dano = 12, duracao = 0.80f, impacto = 0.70f, alcance = 1.9f, raio = 1.0f, desloc = 1.5f },
            new Golpe { clip = "Chute", dano = 18, duracao = 1.45f, impacto = 0.50f, alcance = 2.0f, raio = 1.1f, desloc = 0f },
        };
        static readonly Golpe PesadoPoder = new Golpe { clip = "ChuteGiratorio", dano = 28, duracao = 1.30f, impacto = 0.42f, alcance = 2.1f, raio = 1.2f, desloc = 0f };

        PlayerController controller;
        CharacterController cc;
        TravaAlvo trava;
        ModoCombate modo;
        Golpe current;
        Action callbackImpacto;
        EstilosElementais estilos;
        bool attacking, impactDone, heavy, queuedLight;
        float attackTimer, lastAttackEnd, comboResetAt;
        int comboIndex, currentStep;
        float lmbDesde = -1f;
        bool heavyDisparado, blockAnterior;

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
            estilos = GetComponent<EstilosElementais>();
        }

        void Update()
        {
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked;
            // estilo Naraka: clique rápido = golpe leve, segurar o botão esquerdo = golpe pesado, botão direito = defesa
            bool lightPressed = false, heavyPressed = false;
            var m = Mouse.current;
            if (!InputLocked && cursorPreso && m != null)
            {
                if (m.leftButton.wasPressedThisFrame) { lmbDesde = Time.time; heavyDisparado = false; }
                if (m.leftButton.isPressed && lmbDesde > 0f && !heavyDisparado && Time.time - lmbDesde >= tempoPesado)
                { heavyPressed = true; heavyDisparado = true; }
                if (m.leftButton.wasReleasedThisFrame)
                {
                    if (!heavyDisparado && lmbDesde > 0f) lightPressed = true;
                    lmbDesde = -1f;
                }
            }
            if (!InputLocked && cursorPreso && Gamepad.current != null)
            {
                lightPressed |= Gamepad.current.buttonWest.wasPressedThisFrame;
                heavyPressed |= Gamepad.current.buttonNorth.wasPressedThisFrame;
            }
            IsBlocking = !attacking && !controller.IsBusy && cursorPreso &&
                         ((m != null && m.rightButton.isPressed) || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed));
            if (IsBlocking && !blockAnterior) SomJogo.Tocar("Defesa_Bloqueio", transform.position, 0.5f, 1.15f);
            blockAnterior = IsBlocking;

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
                else if (lightPressed && EmEspada && controller.IsSprinting) Begin(CorridaEspada, false, 5);
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
            // no modo Poder, o 3º e o 4º golpe viram o gesto da técnica do estilo (se houver Fé)
            if (!EmEspada && !isHeavy && estilos != null && (step == 3 || step == 4) && estilos.TecnicaParaPasso(step, out string cClip, out float cDur, out float cImp))
                a = new Golpe { clip = cClip, dano = 0, duracao = cDur, impacto = cImp, alcance = 0f, raio = 0f, desloc = 0f };
            current = a; heavy = isHeavy; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false;
            currentStep = step; callbackImpacto = null;
            float escala = AjustaMira(a);
            if (animador != null) { animador.Acao(a.clip, a.duracao); animador.EscalarMovimento(escala); }
            if (a.raio > 0f) TocarGolpe(a, step);
            AttackStarted?.Invoke(step);
        }

        /// <summary>Gesto de uma técnica (tecla F): toca o clipe e chama <paramref name="noImpacto"/> no momento da liberação.</summary>
        public bool IniciarAcaoEspecial(string clip, float duracao, float impacto, Action noImpacto)
        {
            if (attacking || controller.IsBusy) return false;
            current = new Golpe { clip = clip, dano = 0, duracao = duracao, impacto = impacto, alcance = 0f, raio = 0f, desloc = 0f };
            heavy = false; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false; currentStep = 99;
            callbackImpacto = noImpacto;
            AjustaMira(current);
            animador?.Acao(clip, duracao);
            return true;
        }

        /// <summary>
        /// Com alvo travado encara o alvo; sem alvo, assistência leve de mira. Devolve a escala do deslocamento do
        /// clipe (para o golpe terminar perto do inimigo, sem passar dele nem ficar curto).
        /// </summary>
        float AjustaMira(Golpe a)
        {
            Transform alvo = null;
            if (trava != null && trava.Travado)
            {
                alvo = trava.Alvo;
                Vector3 v = Vector3.ProjectOnPlane(alvo.position - transform.position, Vector3.up);
                if (v.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(v);
            }
            else
            {
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
                if (alvo != null)
                {
                    Vector3 dir = Vector3.ProjectOnPlane(alvo.position - transform.position, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), assistGiroMaximo);
                }
            }
            if (alvo == null || a.desloc < 0.2f) return 1f;
            float distancia = Vector3.ProjectOnPlane(alvo.position - transform.position, Vector3.up).magnitude;
            if (distancia > maxHomingDistance) return 1f;
            float necessario = Mathf.Max(0f, distancia - a.alcance * 0.7f);
            return Mathf.Clamp(necessario / a.desloc, 0f, 1.6f);
        }

        void TickAttack()
        {
            attackTimer += Time.deltaTime;

            if (!impactDone && attackTimer >= current.impacto)
            {
                impactDone = true;
                bool acertou = current.raio > 0f && ResolveHit();
                if (callbackImpacto != null) callbackImpacto();
                else AttackImpact?.Invoke(currentStep, acertou);
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
            if (any) { RegisterComboHit(); SomImpacto(); TremorGlobal.Disparar(heavy ? 1.2f : 0.55f); }
            return any;
        }

        static readonly string[] SomEspada = { "Espada_Corte1", "Espada_Corte2", "Espada_CortePesado" };
        static readonly string[] SomSoco = { "Soco_Leve1", "Soco_Leve2", "Soco_Leve3" };

        // O som do golpe sai um pouco antes do momento do impacto, junto com a "chicotada" da animação.
        void TocarGolpe(Golpe a, int step)
        {
            float atraso = Mathf.Max(0f, a.impacto - 0.18f);
            Vector3 p = transform.position;
            if (EmEspada)
            {
                string n = a.clip == "AtaqueDeslizante" ? "Espada_Estocada" : (heavy || a.clip == "CorteGiratorio") ? "Espada_CortePesado" : SomEspada[Mathf.Clamp(step - 1, 0, 1)];
                SomJogo.Tocar(n, p, 0.9f, heavy ? 0.9f : 1f, atraso: atraso);
            }
            else
            {
                string n = heavy ? "Soco_Forte" : SomSoco[Mathf.Clamp(step - 1, 0, 2)];
                SomJogo.Tocar(n, p, heavy ? 0.9f : 0.75f, 1f, atraso: atraso);
            }
        }

        void SomImpacto()
        {
            Vector3 p = transform.position + transform.forward * 1.5f;
            if (EmEspada) SomJogo.Tocar("Espada_Impacto", p, 0.85f, heavy ? 0.85f : 1f);
            else SomJogo.Tocar("Soco_Impacto", p, 0.9f, heavy ? 0.8f : 1f);
        }

        /// <summary>Conta um acerto no combo (também usado pelas técnicas).</summary>
        public void RegisterComboHit()
        {
            ComboCount++;
            comboResetAt = Time.time + comboCounterReset;
        }
    }
}
