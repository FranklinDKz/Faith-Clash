using System.Collections.Generic;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Liga o movimento do jogador ao Animator (animações do Mixamo e do taijutsu).
    /// Locomoção em blend trees (poder / espada / de costas), ar em blend tree pela velocidade vertical (subida, topo, queda),
    /// ações só quando o jogador pede (golpes, dash, pulo, rolamento...), pouso conforme a altura da queda, passos e morte.
    /// Nada toca "sozinho": parado fica no idle, correndo fica na corrida.
    /// </summary>
    public class AnimadorProtagonista : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerController jogador;

        static readonly int VelocidadeId = Animator.StringToHash("Velocidade");
        static readonly int EscalaPassadaId = Animator.StringToHash("EscalaPassada");
        static readonly int VelAcaoId = Animator.StringToHash("VelAcao");
        static readonly int EspelhoId = Animator.StringToHash("Espelho");
        static readonly int VelYId = Animator.StringToHash("VelY");
        static readonly int MoveXId = Animator.StringToHash("MoveX");
        static readonly int MoveYId = Animator.StringToHash("MoveY");
        static readonly int DefesaId = Animator.StringToHash("Defesa");
        static readonly int NoArId = Animator.StringToHash("NoAr");

        // estados em que o deslocamento do clipe (root motion horizontal) move o personagem
        static readonly HashSet<string> EstadosComMovimento = new HashSet<string>
        {
            "Corte1", "Corte2", "Corte3", "CorteGiratorio", "EspadaSalto", "Estocada", "CorteCima", "CorteDiagonal", "ContraAtaque",
            "Cotovelada", "Joelhada", "ChuteFrontal", "ChuteMuay", "ChuteLateral", "ChuteLado", "ChuteCostas", "ChuteMachado", "GiroCostas",
            "ChuteBaixo", "Voadora", "VoadoraGiratoria", "CombinacaoMuay",
        };
        static readonly string[] Mortes = { "Morte" };

        // tempos (s) dos passos dentro do arquivo Passos_Correr / Passos_Andar (medidos pela análise dos sons)
        static readonly float[] PassosCorrer = { 0.12f, 0.42f, 0.68f, 1.23f, 1.52f, 1.83f, 2.43f, 2.75f, 3.06f, 3.37f };
        static readonly float[] PassosAndar = { 0.06f, 0.57f, 1.09f, 1.6f, 2.11f, 2.59f, 3.12f };

        MovimentoDaAnimacao movimento;
        ModoCombate modo;
        TravaAlvo trava;
        Health vida;
        string estadoLoco = "Locomocao";
        float acaoAte, velocidade, distanciaPasso, pausaAte;
        bool naAcao, noAr, defendendo, morto, emPouso, quedaLonga;
        EscaladaProcedural corpoDin;
        EstabilizadorMarcha estabDin;
        Parkour parkourDin;
        static bool ladoDash;
        string estadoAtual = "";

        public bool Defendendo { get => defendendo; set => defendendo = value; }
        /// <summary>Informado pelo PlayerController (sprint ativo).</summary>
        public bool Sprint { get; set; }
        public bool OcupadoComAcao => naAcao;
        /// <summary>Nome da ação em andamento (ou null).</summary>
        public string AcaoAtual => naAcao ? estadoAtual : null;
        public bool Morto => morto;
        public bool EmPouso => emPouso;
        public Animator Animator => animator;

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            jogador = GetComponent<PlayerController>();
        }

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (jogador == null) jogador = GetComponent<PlayerController>();
            movimento = animator != null ? animator.GetComponent<MovimentoDaAnimacao>() : null;
            modo = GetComponent<ModoCombate>();
            trava = GetComponent<TravaAlvo>();
            vida = GetComponent<Health>();
            parkourDin = GetComponent<Parkour>();
            if (animator != null) { corpoDin = animator.GetComponent<EscaladaProcedural>(); estabDin = animator.GetComponent<EstabilizadorMarcha>(); }
        }

        void OnEnable() { if (vida != null) { vida.Died += Morrer; vida.Damaged += AoReceberDano; } }
        void OnDisable() { if (vida != null) { vida.Died -= Morrer; vida.Damaged -= AoReceberDano; } }

        void AoReceberDano(float dano, Vector3 ponto, GameObject origem)
        {
            // reação ao golpe recebido (não interrompe quem está defendendo)
            if (!morto && !defendendo && !emPouso && !(parkourDin != null && parkourDin.Ocupado)) Acao("Dano", 0.5f, 0.05f);
        }

        /// <summary>
        /// Toca um estado do Animator pelo nome durante a duração informada (em segundos já na velocidade usada).
        /// <paramref name="velocidade"/> acelera/atrasa o clipe; <paramref name="espelho"/> troca esquerda por direita.
        /// </summary>
        /// <summary>Golpes e gestos sem clipe próprio usam um dos estados do pacote de animações (espada, soco, chute, dano), escolhido pelo nome.</summary>
        // golpes e gestos do jogo -> estados do Animator (animações "Martial Art Animations" e as do usuário). Os combos seguem a ordem dos clipes.
        static readonly System.Collections.Generic.Dictionary<string, string> MapaEstados = new System.Collections.Generic.Dictionary<string, string>
        {
            // taijutsu (modo Combate): socos em sequência, chutes de finalização
            { "Mx_KF_Palma", "Atk_P_1" }, { "Mx_Side_Kick", "Atk_K_1" }, { "Mx_Roundhouse_Kick", "Atk_K_2" }, { "Mx_KF_Chute_Giratorio_Avanco", "Atk_K_3" },
            { "ChuteFrontal", "Atk_K_1" }, { "Cotovelada", "Atk_P_2" }, { "Joelhada", "Atk_K_2" }, { "ChuteMachado", "Atk_K_3" },
            { "ChuteBaixo", "Atk_K_1" }, { "GiroCostas", "Atk_K_3" }, { "ChuteLado", "Atk_K_2" }, { "ChuteCostas", "Atk_K_3" },
            { "VoadoraGiratoria", "Atk_K_3" }, { "Voadora", "Atk_K_2" }, { "Mx_Flying_Kick", "Atk_K_3" },
            { "Mx_KF_Crescente_Aereo", "Atk_K_2" }, { "Mx_KF_Borboleta_Avanco", "Atk_K_3" },
            { "Mx_KF_Rasteira_Frente", "Atk_K_1" }, { "Mx_KF_Rasteira_Tras", "Atk_K_2" }, { "Mx_KF_Rasteira_360_Frente", "Atk_K_2" }, { "Mx_KF_Rasteira_360", "Atk_K_3" },
            { "Mx_Standing_Melee_Combo_Attack_Ver_2", "Atk_P_3" },
            // espada
            { "Corte1", "Atk_P_1" }, { "Corte2", "Atk_P_2" }, { "Corte3", "Atk_P_3" }, { "CorteGiratorio", "Atk_K_3" }, { "EspadaSalto", "Atk_K_2" }, { "Estocada", "Atk_P_3" },
            { "Mx_Great_Sword_Slash", "Atk_P_2" }, { "Mx_Great_Sword_Attack", "Atk_P_3" }, { "Mx_Two_Hand_Sword_Combo", "Atk_K_3" },
            { "Mx_SS_Cross_Slash", "Atk_P_1" }, { "Mx_SS_Downward_Slash", "Atk_P_2" }, { "Mx_SS_Hilt_Melee", "Atk_K_1" }, { "Mx_SS_Slash_Combo", "Atk_K_3" },
            { "Mx_SS_Power_Slash", "Atk_P_3" }, { "Mx_SS_Jump_Attack", "Atk_K_2" },
            // movimento
            { "Dash", "Quickstep_F" }, { "Rolamento", "Dodge_F" }, { "U_RolarCorrer", "Rasteira" }, { "Mx_Running_Slide", "Rasteira" },
            { "Mx_Standing_Dodge_Backward", "Dodge_B" }, { "Mx_Standing_Dodge_Left", "Quickstep_F" }, { "Mx_Standing_Dodge_Right", "Quickstep_F" },
            // reações
            { "Dano", "Hit_A" }, { "Empurrado", "Knock_A" }, { "Defesa", "Guard_Idle" }, { "PowerUp", "Taunt" },
        };

        static int HashEstavel(string t) { unchecked { int h = 23; foreach (char c in t) h = h * 31 + c; return h & 0x7fffffff; } }

        /// <summary>Golpes e gestos sem clipe próprio usam um dos estados das animações (soco, chute, gesto, dano), escolhido pelo nome.</summary>
        string Resolver(string estado)
        {
            if (animator == null || string.IsNullOrEmpty(estado)) return estado;
            if (animator.HasState(0, Animator.StringToHash(estado))) return estado;   // estado com o nome exato (ex.: as animações do usuário que substituem as do jogo) vale mais que o mapa
            if (MapaEstados.TryGetValue(estado, out var mapeado) && animator.HasState(0, Animator.StringToHash(mapeado))) return mapeado;
            int n = HashEstavel(estado) % 3;
            string r;
            if (estado.Contains("Dano") || estado.Contains("Empurrado")) r = "Hit_A";
            else if (animator.HasState(0, Animator.StringToHash("Mx_Magia")) && (estado.Contains("Cast") || estado.Contains("Magic") || estado.Contains("Spell") || estado.Contains("Shinra") || estado.Contains("Roar"))) r = "Mx_Magia";
            else if (estado.Contains("Cast") || estado.Contains("Magic") || estado.Contains("PowerUp") || estado.Contains("Roar") || estado.Contains("Curar") || estado.Contains("Gesture") || estado.Contains("Shinra") || estado.Contains("Spell") || estado.Contains("Meditar") || estado.Contains("Interagir") || estado.Contains("Pegar")) r = n == 0 ? "Taunt" : n == 1 ? "Grab_Start" : "Grab_Hit_A";
            else if (estado.Contains("Kick") || estado.Contains("Chute") || estado.Contains("Voadora") || estado.Contains("Joelh") || estado.Contains("Knee") || estado.Contains("Armada") || estado.Contains("Martelo") || estado.Contains("Crescente")) r = "Atk_K_" + (n + 1);
            else if (estado.StartsWith("Mx_PK") || estado.StartsWith("U_") || estado.StartsWith("Pulo") || estado.StartsWith("Pouso") || estado.StartsWith("Mx_Jump") || estado.StartsWith("Mx_Wall")) return estado;
            else r = "Atk_P_" + (n + 1);
            return animator.HasState(0, Animator.StringToHash(r)) ? r : estado;
        }

        public void Acao(string estado, float duracao, float fade = 0.06f, float velocidade = 1f, bool espelho = false, float inicio = 0f)
        {
            if (animator == null || morto) return;
            int hash = Animator.StringToHash(Resolver(estado));
            if (!animator.HasState(0, hash)) { Debug.LogWarning($"[Animador] Estado ausente: {estado} (controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "nenhum")}, ativo={animator.isActiveAndEnabled}, iniciado={animator.isInitialized}, obj={animator.gameObject.name})"); return; }
            animator.SetFloat(VelAcaoId, velocidade);
            animator.SetBool(EspelhoId, espelho);
            animator.CrossFadeInFixedTime(hash, fade, 0, inicio);
            naAcao = true;
            noAr = false;
            acaoAte = Time.time + duracao;
            estadoAtual = estado;
            if (movimento != null)
            {
                movimento.Ativo = EstadosComMovimento.Contains(estado) || estado.StartsWith("Mx_") || estado.StartsWith("U_");
                movimento.Escala = 1f;
            }
        }

        /// <summary>Hit-stop: congela a animação por um instante quando o golpe acerta (a ação dura esse tempo a mais).</summary>
        public void Pausar(float segundos)
        {
            if (animator == null) return;
            animator.speed = 0.02f;
            pausaAte = Time.time + segundos;
            acaoAte += segundos;
        }

        public MovimentoDaAnimacao Movimento => movimento;

        /// <summary>Duração (s) do clipe do estado (já resolvido pelo mapa de estados); usado para casar a velocidade da animação com a do movimento.</summary>
        public float DuracaoClipePorEstado(string estado, float padrao) => DuracaoClipe(Resolver(estado), padrao);

        float DuracaoClipe(string nome, float padrao)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return padrao;
            foreach (var c in animator.runtimeAnimatorController.animationClips) if (c != null && c.name == nome) return c.length;
            return padrao;
        }

        /// <summary>Virada de 90° andando. Esquerda = espelhado.</summary>
        public float Virar90(bool esquerda)
        {
            float d = InfoGolpes.Duracao("U_Virar90", 2.4f) * 0.75f / 1.4f;
            Acao("U_Virar90", d + 0.2f, 0.1f, 1.4f, esquerda);
            return d;
        }

        public void EscalarMovimento(float escala) { if (movimento != null) movimento.Escala = escala; }

        /// <summary>Termina a ação atual na hora e volta à locomoção (usado ao cancelar golpes, meditação...).</summary>
        public void Encerrar(float fade = 0.15f)
        {
            if (animator == null || morto) return;
            naAcao = false;
            if (movimento != null) { movimento.Ativo = false; movimento.Eixo = Vector3.zero; }
            animator.SetBool(EspelhoId, false);
            animator.CrossFadeInFixedTime(jogador != null && !jogador.IsGrounded && !ChaoPerto(0.35f) ? NoArId : Animator.StringToHash(EstadoLoco()), fade);
        }

        /// <summary>Tempo real (s, já na velocidade do golpe) desde o início do estado informado, lido do próprio Animator; se não estiver tocando, devolve <paramref name="reserva"/>.</summary>
        public float TempoDoClipe(string estado, float reserva)
        {
            if (animator == null) return reserva;
            int hash = Animator.StringToHash(Resolver(estado));
            var atual = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0))
            {
                var prox = animator.GetNextAnimatorStateInfo(0);
                if (prox.shortNameHash == hash) return prox.normalizedTime * prox.length;
            }
            return atual.shortNameHash == hash ? atual.normalizedTime * atual.length : reserva;
        }

        /// <summary>Verdadeiro se o estado existe no Animator (as animações novas do usuário entram por arquivos em Anim/Usuario).</summary>
        public bool TemEstado(string estado) => animator != null && animator.HasState(0, Animator.StringToHash(estado));

        public void Pulou()
        {
            float vhor = jogador != null ? new Vector3(jogador.Velocity.x, 0f, jogador.Velocity.z).magnitude : 0f;
            if (Sprint && TemEstado("PuloCorrendo")) Acao("PuloCorrendo", 0.85f, 0.05f, 1.2f);
            else if (vhor > 3.5f && TemEstado("PuloCorrendoLivre")) { float len = DuracaoClipe("Running_Jump", 0.9f); float dj = Mathf.Min(0.9f, len); Acao("PuloCorrendoLivre", dj, 0.05f, len / dj); }
            else if (TemEstado("PuloImpulso"))
            {
                // pulo parado: a animação inteira acompanha o tempo no ar (antes tocava 0,3 s e congelava no ar)
                float vy = jogador != null ? Mathf.Max(jogador.VelocidadeVertical, 1f) : 6f;
                float tAr = Mathf.Clamp(2f * vy / Mathf.Abs(Core.AjustesMovimento.Atual.gravidade), 0.35f, 1.5f);
                float len = DuracaoClipe("Pulo", 1f);
                Acao("PuloImpulso", tAr, 0.05f, Mathf.Clamp(len / (tAr + 0.25f), 0.5f, 4f));
            }
            noAr = true;
        }

        public void Aterrissou()
        {
            noAr = false;
            animator?.CrossFadeInFixedTime(EstadoLoco(), 0.1f);
        }

        /// <summary>Pouso conforme a altura da queda (m) e a velocidade: leve, pesado (fica preso um instante) ou rolando.</summary>
        public void Pousar(float alturaDaQueda, float velHoriz = 0f)
        {
            noAr = false;
            bool eraQuedaLonga = quedaLonga;
            quedaLonga = false;
            if (corpoDin != null && !(parkourDin != null && parkourDin.Ocupado)) corpoDin.Limpar();
            Vector3 p = transform.position;
            if (alturaDaQueda > 2.4f)
            {
                SomJogo.Tocar("Pouso", p, Mathf.Clamp(0.6f + alturaDaQueda * 0.05f, 0.6f, 1f), 0.8f);
                Poeira(p, Mathf.Clamp(10 + (int)(alturaDaQueda * 2f), 12, 30), Mathf.Clamp(0.9f + alturaDaQueda * 0.08f, 1f, 2.2f));
                TremorGlobal.Disparar(Mathf.Clamp(0.4f + alturaDaQueda * 0.05f, 0.4f, 1f));
                // em movimento: rola para a frente e segue (parkour); parado ou devagar: pouso pesado de super-herói
                if (velHoriz >= 3f && (TemEstado("U_Rolar") || TemEstado("Dodge_F"))) StartCoroutine(PousoRolando(alturaDaQueda, velHoriz));
                else StartCoroutine(PousoAgachado(alturaDaQueda, true));
                return;
            }
            if (alturaDaQueda > 1.1f)
            {
                SomJogo.Tocar("Passos_Correr", p, 0.7f, 0.85f, inicio: 0.42f, duracaoMax: 0.3f);
                Poeira(p, 8, 0.9f);
                TremorGlobal.Disparar(0.22f);
                StartCoroutine(PousoAgachado(alturaDaQueda, false));
                return;
            }
            SomJogo.Tocar("Passos_Correr", p, 0.6f, 0.9f, inicio: 0.42f, duracaoMax: 0.25f);
            if (velHoriz < 3.5f && !naAcao)
            {
                if (TemEstado("PousoLeve")) Acao("PousoLeve", 0.45f, 0.05f, 2.8f);
                jogador?.Atordoar(0.12f);
            }
            else animator.CrossFadeInFixedTime(EstadoLoco(), 0.12f);
        }

        /// <summary>Pouso de queda alta em movimento: rola para a frente (a animação de rolamento do pacote), conserva a velocidade e segue correndo.</summary>
        System.Collections.IEnumerator PousoRolando(float altura, float velHoriz)
        {
            emPouso = true;
            bool mixRolar = TemEstado("U_Rolar");
            float dur = mixRolar ? 0.75f : 0.62f;
            float len = DuracaoClipePorEstado(mixRolar ? "U_Rolar" : "Rolamento", mixRolar ? 2.43f : 1.67f);
            Acao(mixRolar ? "U_Rolar" : "Rolamento", dur + 0.1f, 0.05f, mixRolar ? len * 0.78f / dur : len / dur);
            if (movimento != null) movimento.Ativo = false;
            Vector3 dir = Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up); if (dir.sqrMagnitude < 0.1f) dir = transform.forward; dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);
            float vel = Mathf.Clamp(velHoriz * 0.9f, 5f, 8.5f);
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (morto || jogador == null) { emPouso = false; yield break; }
                jogador.Impulsar(dir * Mathf.Lerp(vel, vel * 0.7f, t / dur), -2f);   // rola no chão, com a gravidade segurando o corpo
                yield return null;
            }
            emPouso = false;
            Encerrar(0.1f);
        }

        /// <summary>Pouso agachado (queda média ou pesada): os pés ficam firmes no chão, o quadril desce e amortece, uma mão toca o chão no pouso pesado, e o corpo sobe de novo.</summary>
        System.Collections.IEnumerator PousoAgachado(float altura, bool pesado)
        {
            emPouso = true;
            if (corpoDin == null || estabDin == null) { emPouso = false; yield break; }
            Acao("Locomocao", 3f, 0.05f, 1f);   // base: parado; a postura vem do IK e da dinâmica do corpo
            Vector3 p = transform.position;
            Vector3 f = transform.forward, d = transform.right;
            float chaoY = p.y;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var h, 1.5f, ~0, QueryTriggerInteraction.Ignore)) chaoY = h.point.y;
            float fundo = pesado ? Mathf.Clamp(0.30f + altura * 0.03f, 0.35f, 0.55f) : Mathf.Clamp(0.12f + altura * 0.08f, 0.15f, 0.28f);
            float dur = pesado ? 0.85f : 0.4f;
            Vector3 peE = new Vector3(p.x, chaoY + 0.1f, p.z) - d * 0.28f - f * 0.1f, peD = new Vector3(p.x, chaoY + 0.1f, p.z) + d * 0.28f + f * 0.35f;
            Vector3 mao = new Vector3(p.x, chaoY + 0.05f, p.z) + d * 0.22f + f * 0.45f;
            if (pesado) jogador?.Atordoar(0.5f);
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (morto) break;
                float k = t / dur;
                float desce = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, pesado ? 0.18f : 0.3f, k)), sobe = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pesado ? 0.65f : 0.5f, 1f, k));
                float prof = desce * (1f - sobe);
                estabDin.DeslocY = -fundo * prof;
                corpoDin.Definir(EscaladaProcedural.PeE, peE, 1f - sobe * 0.6f);
                corpoDin.Definir(EscaladaProcedural.PeD, peD, 1f - sobe * 0.6f);
                if (pesado) corpoDin.Definir(EscaladaProcedural.MaoD, mao, prof);
                corpoDin.Corpo(pesado ? 8f * prof : 0f, (pesado ? 32f : 18f) * prof, pesado ? -6f * prof : 0f, 0f, 0f, 8f * prof, 8f * prof, 0f);
                yield return null;
            }
            estabDin.DeslocY = 0f;
            corpoDin.Limpar();
            emPouso = false;
            if (!morto) Encerrar(0.15f);
        }

        /// <summary>Corpo do dash: se abaixa e se joga para a frente (tronco inclinado, ombros e quadril girando em lados alternados), derrapa e se recupera ao terminar.</summary>
        public void DashDinamico(float duracao)
        {
            if (corpoDin == null || estabDin == null || morto) return;
            StartCoroutine(CorpoDoDash(duracao));
        }

        System.Collections.IEnumerator CorpoDoDash(float duracao)
        {
            ladoDash = !ladoDash;
            float lado = ladoDash ? 1f : -1f;
            float total = duracao + 0.2f;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                if (morto || (parkourDin != null && parkourDin.Ocupado)) break;
                float k = t / total;
                float ant = Mathf.Sin(Mathf.Clamp01(t / 0.06f) * Mathf.PI * 0.5f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.14f, t)));   // preparação: agacha e puxa para trás
                float rajada = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.16f, t)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duracao * 0.7f, duracao + 0.15f, t)));   // corpo todo para a frente
                float freada = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duracao * 0.75f, duracao + 0.05f, t)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duracao + 0.05f, total, t)));
                estabDin.DeslocY = -0.1f * ant - 0.12f * freada;
                estabDin.InclinacaoFrente = 24f * rajada - 8f * ant - 10f * freada;
                corpoDin.Corpo(lado * 24f * rajada, 10f * rajada - 6f * ant, lado * 9f * rajada, -lado * 16f * rajada, lado * 6f * rajada, 10f * rajada, 10f * rajada, lado * 5f * rajada);
                yield return null;
            }
            if (estabDin != null) { estabDin.DeslocY = 0f; estabDin.InclinacaoFrente = 0f; }
            if (corpoDin != null && !(parkourDin != null && parkourDin.Ocupado)) corpoDin.Limpar();
        }

        System.Collections.IEnumerator ArrancarApos(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            if (morto || jogador == null) yield break;
            Encerrar(0.12f);
            jogador.Impulsar(transform.forward * 6f, 0f);   // sai correndo, nunca parado
        }

        static void Poeira(Vector3 p, int qtd, float raio)
        {
            Efeitos.Rajada(p + Vector3.up * 0.05f, Quaternion.Euler(-90f, 0f, 0f), new Color(0.7f, 0.65f, 0.55f, 0.6f), qtd, new Vector2(0.8f, raio), new Vector2(0.12f, 0.3f), new Vector2(0.4f, 0.8f),
                ParticleSystemShapeType.Circle, 0.3f, -0.05f, false);
        }

        void Morrer()
        {
            if (morto) return;
            Acao(Mortes[Random.Range(0, Mortes.Length)], 99f, 0.1f);
            morto = true;
            if (movimento != null) movimento.Ativo = false;
            if (jogador != null) jogador.Congelado = true;
            SomJogo.Tocar("Morte_Jogador", transform.position, 0.9f, duracaoMax: 6f);
        }

        bool ChaoPerto(float distancia) =>
            Physics.SphereCast(transform.position + Vector3.up * 0.4f, 0.25f, Vector3.down, out var h, 0.4f + distancia, ~0, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(transform);

        string EstadoLoco()
        {
            if (trava != null && trava.Travado) return "LocomocaoAlvo";
            if (vida != null && !vida.IsDead && vida.Current / vida.Max < 0.2f && (modo == null || modo.Atual != Modo.Espada)) return "LocomocaoFerido";
            if (modo != null && modo.Atual == Modo.Espada) return modo.Grip == Empunhadura.DuasMaos ? "LocomocaoEspadaDupla" : "LocomocaoEspada";
            if (modo != null && modo.Atual == Modo.Combate) return "LocomocaoCombate";
            return "Locomocao";
        }

        float yAnterior, subida;
        int naEscada;   // 0 não, 1 subindo, 2 descendo
        static readonly int EscadaId = Animator.StringToHash("U_Escada"), EscadaDescId = Animator.StringToHash("U_EscadaDescer"), QuedaArId = Animator.StringToHash("U_QuedaAr");

        void Update()
        {
            if (animator == null || jogador == null || morto) return;
            if (pausaAte > 0f && Time.time >= pausaAte) { pausaAte = 0f; animator.speed = 1f; }

            Vector3 v = jogador.Velocity;
            float velY = v.y;
            v.y = 0f;
            float alvo = v.magnitude;
            velocidade = Mathf.Lerp(velocidade, alvo, 1f - Mathf.Exp(-10f * Time.deltaTime));
            animator.SetFloat(VelocidadeId, velocidade);
            // as animações de andar/correr/sprint têm velocidade natural (0,97 / 5,36 / 8,25 m/s): acelera o ciclo só acima do sprint natural
            animator.SetFloat(EscalaPassadaId, Mathf.Clamp(velocidade / 7.5f, 1f, 1.1f) * Core.AjustesMovimento.Atual.velocidadeAnimCorrida);
            animator.SetFloat(VelYId, velY, 0.06f, Time.deltaTime);

            // subindo escada (ou rampa íngreme) andando/correndo: a animação de escada, enquanto sobe
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float taxaY = (transform.position.y - yAnterior) / dt;
            if (taxaY > 6f || taxaY < -12f) taxaY = 0f;
            subida = Mathf.Lerp(subida, taxaY, 1f - Mathf.Exp(-8f * Time.deltaTime));
            yAnterior = transform.position.y;
            // escada (só degraus: o chão sob os pés é plano; rampa não conta): sobe ou desce com a animação própria
            bool piso = jogador.NormalDoChao.y > 0.99f;
            if (naEscada != 0 && (naAcao || !piso || !jogador.IsGrounded || alvo < 0.8f || EstadoLoco() == "LocomocaoAlvo" || (naEscada == 1 ? subida < 0.3f : subida > -0.3f)))
            {
                naEscada = 0;
                if (!naAcao && jogador.IsGrounded) animator.CrossFadeInFixedTime(Animator.StringToHash(EstadoLoco()), 0.15f);
            }
            else if (naEscada == 0 && !naAcao && jogador.IsGrounded && !noAr && piso && Mathf.Abs(subida) > 0.9f && alvo > 1.4f && EstadoLoco() != "LocomocaoAlvo")
            {
                int id = subida > 0f ? EscadaId : EscadaDescId;
                if (animator.HasState(0, id))
                {
                    naEscada = subida > 0f ? 1 : 2;
                    animator.SetFloat(VelAcaoId, Mathf.Clamp(alvo / 4.2f, 0.8f, 1.5f));
                    animator.CrossFadeInFixedTime(id, 0.15f);
                }
            }
            if (naEscada != 0) { animator.SetFloat(VelAcaoId, Mathf.Clamp(alvo / 4.2f, 0.8f, 1.5f)); Passos(alvo); return; }

            if (naAcao)
            {
                if (Time.time < acaoAte) return;
                naAcao = false;
                if (movimento != null) movimento.Ativo = false;
                animator.SetBool(EspelhoId, false);
                animator.CrossFadeInFixedTime(noAr ? NoArId : Animator.StringToHash(EstadoLoco()), 0.14f);
            }

            // no ar (pulo ou queda): a blend tree escolhe subida / topo / queda pela velocidade vertical
            // só entra na animação de queda se está mesmo no ar (um tempinho sem chão e nada logo abaixo dos pés)
            if (!jogador.IsGrounded && (noAr || (jogador.TempoNoAr > 0.12f && !ChaoPerto(0.35f))))
            {
                if (!noAr) { noAr = true; animator.CrossFadeInFixedTime(NoArId, 0.12f); }
                if (!quedaLonga && jogador.TempoNoAr > 0.55f && jogador.VelocidadeVertical < -5f && !naAcao && animator.HasState(0, QuedaArId))
                {
                    quedaLonga = true;
                    animator.CrossFadeInFixedTime(QuedaArId, 0.3f);
                }
                if (quedaLonga && corpoDin != null && !(parkourDin != null && parkourDin.Ocupado))
                {
                    float w = Time.time * 9f;
                    corpoDin.Corpo(Mathf.Sin(w) * 10f, -6f, Mathf.Cos(w * 0.8f) * 8f, Mathf.Sin(w * 1.1f) * 8f, Mathf.Cos(w) * 5f, 10f + Mathf.Sin(w) * 6f, 10f - Mathf.Sin(w) * 6f, Mathf.Sin(w) * 6f);
                }
                return;
            }
            // rede de segurança: no chão e ainda no estado de queda (saída de parkour/slide/ação) volta à locomoção
            if (!noAr && !naAcao && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == NoArId && !animator.IsInTransition(0))
            {
                estadoLoco = EstadoLoco();
                animator.CrossFadeInFixedTime(Animator.StringToHash(estadoLoco), 0.1f);
            }
            if (noAr)
            {
                quedaLonga = false;
                // voltou ao chão sem passar pelo pouso (salto curto, saída de parkour): volta à locomoção em vez de ficar preso no estado NoAr
                noAr = false;
                estadoLoco = EstadoLoco();
                animator.CrossFadeInFixedTime(Animator.StringToHash(estadoLoco), 0.12f);
            }

            // alvo travado: o personagem encara o inimigo e anda/corre para os lados e para trás (blend 2D pela velocidade local)
            string loco = EstadoLoco();
            if (loco == "LocomocaoAlvo")
            {
                Vector3 local = transform.InverseTransformDirection(v);
                animator.SetFloat(MoveXId, local.x, 0.08f, Time.deltaTime);
                animator.SetFloat(MoveYId, local.z, 0.08f, Time.deltaTime);
            }
            if (loco != estadoLoco)
            {
                estadoLoco = loco;
                animator.CrossFadeInFixedTime(Animator.StringToHash(loco), 0.2f);
            }

            // defesa
            bool emDefesa = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == DefesaId;
            if (defendendo && !emDefesa) animator.CrossFadeInFixedTime(DefesaId, 0.1f);
            else if (!defendendo && emDefesa) animator.CrossFadeInFixedTime(Animator.StringToHash(loco), 0.12f);

            Passos(alvo);
        }

        void Passos(float vel)
        {
            if (vel < 1.5f) { distanciaPasso = 0f; return; }
            distanciaPasso += vel * Time.deltaTime;
            float passada = vel > 7f ? 2.6f : 1.9f;
            if (distanciaPasso < passada) return;
            distanciaPasso = 0f;
            bool correr = vel > 4f;
            float[] tempos = correr ? PassosCorrer : PassosAndar;
            float t = tempos[Random.Range(0, tempos.Length)];
            SomJogo.Tocar(correr ? "Passos_Correr" : "Passos_Andar", transform.position, correr ? 0.55f : 0.45f, 1f, 0.08f,
                inicio: Mathf.Max(0f, t - 0.03f), duracaoMax: 0.24f);
            if (RegrasFase.SemMapa) SomJogo.Tocar("Passos_Pedra", transform.position, correr ? 0.75f : 0.5f, correr ? 1f : 0.85f, 0.1f);   // pedra da caverna
        }
    }
}