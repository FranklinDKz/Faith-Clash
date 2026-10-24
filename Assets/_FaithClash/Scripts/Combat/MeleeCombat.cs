using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Ataques corpo a corpo, por modo (os modos não se misturam):
    ///  - Combate (tecla 5): taijutsu. Toque = cadeia Jab → Direto → Gancho → Chute; segurar = golpe forte que muda conforme o ponto da cadeia
    ///    (Uppercut, Cotovelada, Joelhada, Chute Machado); S no 3º golpe = chute baixo, A/D no 4º = giro de costas; correndo = joelhada voadora.
    ///  - Espada (G): combo de 4 cortes e golpe pesado com salto.
    ///  - Poder (1-4): sem socos nem chutes — toque conjura a técnica leve do estilo, segurar conjura a técnica forte (gasta Fé).
    /// Botão direito defende; Ctrl e C (rolamento e dash) cancelam o golpe. Sem alvo travado há uma assistência leve de mira;
    /// com alvo travado (Q) o personagem encara o alvo e avança até ele. O impacto é lido do tempo real da animação.
    /// </summary>
    public class MeleeCombat : MonoBehaviour
    {
        struct Golpe
        {
            public string clip;
            public float dano, duracao, impacto, alcance, raio, desloc;   // tempos em segundos com o clipe em velocidade 1; desloc = quanto o clipe "anda" para a frente (m)
            public float vel, empurrao, avanco;   // avanco = passo à frente que o código dá até o inimigo (m)
            public bool espelho;
            public Golpe(string clip, float dano, float duracao, float impacto, float alcance, float raio, float desloc, float vel = 1f, float empurrao = 2f, float avanco = 0f, bool espelho = false)
            {
                this.avanco = avanco;
                this.clip = clip; this.dano = dano; this.duracao = duracao; this.impacto = impacto; this.alcance = alcance; this.raio = raio;
                this.desloc = desloc; this.vel = vel; this.empurrao = empurrao; this.espelho = espelho;
            }
            public float Duracao => duracao / vel;
        }

        [SerializeField] AnimadorProtagonista animador;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] float comboWindow = 0.65f;
        [SerializeField] float comboCounterReset = 2.5f;
        [SerializeField] float tempoPesado = 0.26f;   // segurando o botão esquerdo por esse tempo = golpe pesado / técnica forte
        [SerializeField] float maxHomingDistance = 7f;
        [Header("Assistência de mira (sem lock-on)")]
        [SerializeField] float assistDistancia = 7f;
        [SerializeField] float assistAngulo = 50f;
        [SerializeField] float assistGiroMaximo = 28f;

        // Duração e impacto de cada golpe vêm da própria animação (InfoGolpes, gerado pelo Preparar Jogador).
        static Golpe G(string clip, float dano, float alcance, float raio, float vel = 1f, float empurrao = 3f, float avanco = 0f, float desloc = 0f) =>
            new Golpe(clip, dano, InfoGolpes.Duracao(clip), InfoGolpes.Impacto(clip), alcance, raio, desloc, vel, empurrao, avanco);

        // ---- Espada
        static Golpe[] CombosEspada => new[]
        {
            G("Corte1", 14, 2.2f, 1.1f, 1f, 3f, 0.8f),
            G("Corte2", 15, 2.2f, 1.1f, 1f, 3f, 0.6f),
            G("Corte3", 17, 2.3f, 1.2f, 1.1f, 3.5f, 0.9f),
            G("CorteGiratorio", 26, 2.6f, 1.4f, 1.15f, 6f, 0.7f),
        };
        static Golpe PesadoEspada => G("EspadaSalto", 40, 2.8f, 1.5f, 1.1f, 8f, 1.4f);
        static Golpe CorridaEspada => G("Estocada", 30, 2.6f, 1.2f, 1f, 7f, 2.2f);

        // ---- Combate (taijutsu): UAL (jab, direto, gancho) + Motifect (chutes, joelhada, cotovelada)
        static Golpe[] CadeiaCombate => new[]
        {
            G("Jab", 7, 1.7f, 0.9f, 1f, 1.5f, 0.6f),
            G("Direto", 10, 1.8f, 0.9f, 1f, 2.5f, 0.9f),
            G("Gancho", 12, 1.8f, 1.0f, 1f, 3f, 0.7f),
            G("ChuteLateral", 16, 2.1f, 1.1f, 1.3f, 6f, 0.9f),
        };
        static Golpe ChuteBaixo => G("ChuteBaixo", 11, 2.0f, 1.0f, 1.4f, 3f, 0.9f);
        static Golpe GiroCostas => G("GiroCostas", 18, 1.9f, 1.2f, 1.3f, 7f, 0.6f);
        static Golpe[] PesadosCombate => new[]
        {
            G("ChuteFrontal", 20, 2.0f, 1.0f, 1.3f, 8f, 0.6f),
            G("Cotovelada", 19, 1.6f, 0.9f, 1.4f, 6f, 0.9f),
            G("Joelhada", 22, 1.7f, 0.9f, 1.4f, 7f, 0.9f),
            G("ChuteMachado", 30, 2.1f, 1.2f, 1.3f, 9f, 0.8f),
        };
        static Golpe ChuteLado => G("ChuteLado", 24, 2.1f, 1.0f, 1.3f, 9f, 0.7f);
        static Golpe ChuteCostas => G("ChuteCostas", 26, 2.1f, 1.0f, 1.3f, 10f, 0.7f);
        static Golpe ChuteGiratorioPesado => G("VoadoraGiratoria", 30, 2.3f, 1.2f, 1.3f, 10f, 1.5f);
        static Golpe JoelhadaVoadora => G("Voadora", 24, 2.2f, 1.1f, 1.5f, 8f, 1.6f);

        PlayerController controller;
        TravaAlvo trava;
        ModoCombate modo;
        EstilosElementais estilos;
        Golpe current;
        Action callbackImpacto;
        bool attacking, impactDone, heavy, queuedLight, queuedHeavy;
        float attackTimer, lastAttackEnd, comboResetAt, pausaAte;
        int comboIndex, currentStep;
        float lmbDesde = -1f;
        bool heavyDisparado, blockAnterior;
        float ignoraAte;       // o clique que captura o cursor (ou que inicia o Play) não vira golpe
        bool cursorPresoAntes;
        bool ramoS, ramoLado;   // S / A / D apertados no momento do clique (valem mesmo se o golpe entrar na fila)

        /// <summary>Acertos seguidos (contador "Combo" do HUD).</summary>
        public int ComboCount { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool IsAttacking => attacking;
        /// <summary>Multiplicador de dano (itens / buffs).</summary>
        public float DamageMultiplier { get; set; } = 1f;
        /// <summary>Quando verdadeiro, ignora a entrada de ataque (ex.: técnica segurando o botão).</summary>
        public bool InputLocked { get; set; }

        public event Action<float> HitLanded;
        /// <summary>(passo do combo 1..n, 0 no golpe pesado)</summary>
        public event Action<int> AttackStarted;
        /// <summary>(passo do combo 1..n ou 0 no golpe pesado, acertou alguém)</summary>
        public event Action<int, bool> AttackImpact;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            trava = GetComponent<TravaAlvo>();
            modo = GetComponent<ModoCombate>();
            estilos = GetComponent<EstilosElementais>();
        }

        bool EmEspada => modo != null && modo.Atual == Modo.Espada;
        bool EmPoder => modo != null && modo.Atual == Modo.Poder;
        bool EmCombate => modo == null || modo.Atual == Modo.Combate;

        void Update()
        {
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked;
            if (cursorPreso && !cursorPresoAntes) ignoraAte = Time.time + 0.35f;
            cursorPresoAntes = cursorPreso;
            bool entradaLivre = Time.time >= ignoraAte && Time.timeSinceLevelLoad > 0.8f;
            // estilo Naraka: clique rápido = golpe leve, segurar o botão esquerdo = golpe pesado, botão direito = defesa
            bool lightPressed = false, heavyPressed = false;
            var m = Mouse.current;
            if (!entradaLivre) { lmbDesde = -1f; heavyDisparado = true; }   // botão que já estava apertado antes não conta
            if (!InputLocked && cursorPreso && entradaLivre && m != null)
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
            if ((lightPressed || heavyPressed) && Keyboard.current != null)
            {
                var kb = Keyboard.current;
                ramoS = kb.sKey.isPressed;
                ramoLado = kb.aKey.isPressed || kb.dKey.isPressed;
            }
            bool podeDefender = !attacking && !controller.IsBusy && !controller.Atordoado;
            IsBlocking = podeDefender && cursorPreso &&
                         ((m != null && m.rightButton.isPressed) || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed));
            if (IsBlocking && !blockAnterior) SomJogo.Tocar("Defesa_Bloqueio", transform.position, 0.5f, 1.15f);
            blockAnterior = IsBlocking;

            controller.MoveMultiplier = attacking ? 0.25f : IsBlocking ? 0.4f : 1f;
            if (animador != null) animador.Defendendo = IsBlocking;

            if (attacking)
            {
                TickAttack();
                if (attacking && lightPressed && !heavy) queuedLight = true;
                if (attacking && heavyPressed) queuedHeavy = true;
            }
            else if (!controller.IsBusy && !controller.Atordoado && !(animador != null && animador.Morto))
            {
                if (heavyPressed) Pesado();
                else if (lightPressed) Leve();
            }

            if (ComboCount > 0 && Time.time > comboResetAt) ComboCount = 0;
        }

        // ------------------------------------------------------------------ escolha do golpe
        void Leve()
        {
            if (EmPoder)
            {
                if (estilos != null) estilos.TecnicaLeve();
                return;
            }
            if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
            if (EmEspada)
            {
                if (controller.IsSprinting && controller.Velocity.sqrMagnitude > 16f) { Begin(CorridaEspada, false, 5); return; }
                var cadeiaEspada = CombosEspada;
                int ie = comboIndex % cadeiaEspada.Length;
                Begin(cadeiaEspada[ie], false, ie + 1);
                return;
            }
            if (controller.IsSprinting && controller.Velocity.sqrMagnitude > 16f) { Begin(JoelhadaVoadora, false, 5); return; }
            var cadeia = CadeiaCombate;
            int i = comboIndex % cadeia.Length;
            Golpe g = cadeia[i];
            if (i == 2 && ramoS) g = ChuteBaixo;          // ramo: S no 3º golpe
            else if (i == 3 && ramoLado) g = GiroCostas;  // ramo: A/D no 4º golpe
            Begin(g, false, i + 1);
        }

        void Pesado()
        {
            if (EmPoder)
            {
                if (estilos != null) estilos.TecnicaForte();
                return;
            }
            if (Time.time - lastAttackEnd > comboWindow) comboIndex = 0;
            if (EmEspada) { Begin(PesadoEspada, true, 0); return; }
            if (controller.IsSprinting && controller.Velocity.sqrMagnitude > 16f) { Begin(ChuteGiratorioPesado, true, 0); return; }
            if (ramoLado) { Begin(ChuteLado, true, 0); return; }       // ramo: A/D + segurar = chute lateral
            if (ramoS) { Begin(ChuteCostas, true, 0); return; }         // ramo: S + segurar = chute para trás
            var pesados = PesadosCombate;
            Begin(pesados[Mathf.Min(comboIndex, pesados.Length - 1)], true, 0);
        }

        void Begin(Golpe a, bool isHeavy, int step)
        {
            current = a; heavy = isHeavy; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false; queuedHeavy = false;
            currentStep = step; callbackImpacto = null;
            float escala = AjustaMira(a, out float falta);
            if (animador != null) { animador.Acao(a.clip, a.Duracao, 0.06f, a.vel, a.espelho); animador.EscalarMovimento(escala); }
            if (a.avanco > 0f) controller.Avancar(transform.forward, falta < 0f ? a.avanco * 0.5f : Mathf.Min(falta, a.avanco), a.impacto / a.vel);
            TocarGolpe(a, step);
            AttackStarted?.Invoke(step);
        }

        /// <summary>Usado pelos testes automáticos: executa um golpe pelo nome do clipe (Jab, Uppercut, Corte1...).</summary>
        public bool TestarGolpe(string clip)
        {
            if (attacking) return false;
            var todos = new List<Golpe>();
            todos.AddRange(CombosEspada); todos.AddRange(CadeiaCombate); todos.AddRange(PesadosCombate);
            todos.AddRange(new[] { PesadoEspada, CorridaEspada, ChuteBaixo, GiroCostas, ChuteLado, ChuteCostas, ChuteGiratorioPesado, JoelhadaVoadora });
            foreach (var g in todos)
                if (g.clip == clip) { Begin(g, false, 1); return true; }
            return false;
        }

        /// <summary>Gesto de uma técnica: toca o clipe e chama <paramref name="noImpacto"/> no momento da liberação.</summary>
        public bool IniciarAcaoEspecial(string clip, float duracao, float impacto, Action noImpacto)
        {
            if (attacking || controller.IsBusy || controller.Atordoado) return false;
            current = new Golpe(clip, 0, duracao, impacto, 0f, 0f, 0f);
            heavy = false; attacking = true; impactDone = false; attackTimer = 0f; queuedLight = false; queuedHeavy = false; currentStep = 99;
            callbackImpacto = noImpacto;
            AjustaMira(current, out _);
            animador?.Acao(clip, duracao);
            return true;
        }

        /// <summary>Interrompe o golpe atual (dash, rolamento, pulo). A cadeia do combo recomeça.</summary>
        public void Cancelar()
        {
            queuedLight = false; queuedHeavy = false; callbackImpacto = null;
            attacking = false;
            comboIndex = 0; lastAttackEnd = Time.time - 10f;   // a cadeia do combo recomeça do primeiro golpe
        }

        /// <summary>
        /// Com alvo travado encara o alvo; sem alvo, assistência leve de mira. Devolve a escala do deslocamento do
        /// clipe (para o golpe terminar perto do inimigo, sem passar dele nem ficar curto).
        /// </summary>
        float AjustaMira(Golpe a, out float falta)
        {
            falta = -1f;   // -1 = sem alvo
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
            if (alvo == null) return 1f;
            float distancia = Vector3.ProjectOnPlane(alvo.position - transform.position, Vector3.up).magnitude;
            if (distancia > maxHomingDistance) return 1f;
            float necessario = Mathf.Max(0f, distancia - a.alcance * 0.7f);
            falta = necessario;
            if (a.desloc < 0.2f) return 1f;
            return Mathf.Clamp(necessario / a.desloc, 0f, 1.6f);
        }

        void TickAttack()
        {
            if (Time.time < pausaAte) return;   // hit-stop: o golpe e a animação param juntos por um instante
            attackTimer += Time.deltaTime;

            // o impacto sai quando a ANIMAÇÃO chega no quadro do golpe (lê o tempo real do Animator; o cronômetro é só reserva)
            float tReal = animador != null ? animador.TempoDoClipe(current.clip, attackTimer) : attackTimer;
            if (!impactDone && tReal >= current.impacto / current.vel)
            {
                impactDone = true;
                bool acertou = current.raio > 0f && ResolveHit();
                if (callbackImpacto != null) callbackImpacto();
                else AttackImpact?.Invoke(currentStep, acertou);
            }

            float fim = current.Duracao;
            // cancelamento: depois do impacto o próximo golpe pode sair antes da recuperação acabar (combo fluido)
            float janela = current.impacto / current.vel + (fim - current.impacto / current.vel) * 0.4f;
            bool encadeia = (queuedLight || queuedHeavy) && impactDone && attackTimer >= janela && callbackImpacto == null;
            if (attackTimer >= fim || encadeia)
            {
                bool pesadoNaFila = queuedHeavy, levenaFila = queuedLight;
                attacking = false;
                lastAttackEnd = Time.time;
                comboIndex = heavy || currentStep == 5 ? 0 : comboIndex + 1;
                if (pesadoNaFila) Pesado();
                else if (levenaFila) Leve();
            }
        }

        bool ResolveHit()
        {
            Vector3 center = transform.position + Vector3.up + transform.forward * (current.alcance * 0.6f);
            bool any = false;
            Vector3 ponto = center;
            foreach (var col in Physics.OverlapSphere(center, current.raio, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                float dano = current.dano * DamageMultiplier;
                ponto = col.ClosestPoint(center);
                target.TakeDamage(dano, ponto, gameObject);
                var boneco = col.GetComponentInParent<TrainingDummy>();
                if (boneco != null) boneco.Empurrar(transform.forward * current.empurrao);
                HitLanded?.Invoke(dano);
                any = true;
            }
            if (any)
            {
                RegisterComboHit();
                SomImpacto();
                TremorGlobal.Disparar(heavy ? 1.2f : 0.55f);
                Faiscas(ponto);
                float parar = heavy ? 0.09f : 0.05f;
                pausaAte = Time.time + parar;
                animador?.Pausar(parar);
            }
            return any;
        }

        void Faiscas(Vector3 p)
        {
            Color cor = EmEspada ? new Color(1f, 0.95f, 0.8f) : new Color(1f, 0.7f, 0.3f);
            Efeitos.Rajada(p, Quaternion.LookRotation(-transform.forward), cor, heavy ? 28 : 14, new Vector2(2.5f, 7f), new Vector2(0.04f, 0.1f), new Vector2(0.15f, 0.35f),
                ParticleSystemShapeType.Cone, 0.1f, 0.6f);
            Efeitos.Clarao(p, cor, heavy ? 6f : 3f, 4f, 0.12f);
        }

        static readonly string[] SomEspada = { "Espada_Corte1", "Espada_Corte2", "Espada_CortePesado" };
        static readonly string[] SomSoco = { "Soco_Leve1", "Soco_Leve2", "Soco_Leve3" };

        // O som do golpe sai um pouco antes do momento do impacto, junto com a "chicotada" da animação.
        void TocarGolpe(Golpe a, int step)
        {
            if (a.raio <= 0f) return;
            float atraso = Mathf.Max(0f, a.impacto / a.vel - 0.14f);
            Vector3 p = transform.position;
            if (EmEspada)
            {
                string n = a.clip == "Estocada" ? "Espada_Estocada" : (heavy || a.clip == "CorteGiratorio") ? "Espada_CortePesado" : SomEspada[Mathf.Clamp(step - 1, 0, 1)];
                SomJogo.Tocar(n, p, 0.9f, heavy ? 0.9f : 1f, atraso: atraso);
            }
            else
            {
                bool chute = a.clip.StartsWith("Chute") || a.clip.StartsWith("Voadora") || a.clip == "Joelhada" || a.clip == "GiroCostas";
                string n = heavy ? "Soco_Forte" : SomSoco[Mathf.Clamp(step - 1, 0, 2)];
                SomJogo.Tocar(n, p, heavy ? 0.9f : 0.75f, chute ? 0.8f : 1f, 0.05f, atraso: atraso);
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