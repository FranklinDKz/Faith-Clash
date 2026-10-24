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
        static readonly int DefesaId = Animator.StringToHash("Defesa");
        static readonly int NoArId = Animator.StringToHash("NoAr");

        // estados em que o deslocamento do clipe (root motion horizontal) move o personagem
        static readonly HashSet<string> EstadosComMovimento = new HashSet<string>
        {
            "Corte1", "Corte2", "Corte3", "CorteGiratorio", "EspadaSalto",
            "Soco1", "Soco2", "Soco3", "Chute", "ChuteGiratorio",
            "Jab", "Direto", "Gancho", "Uppercut", "Cotovelada", "Joelhada", "ChuteLateral", "ChuteBaixo", "ChuteMachado", "GiroCostas",
            "PousoRolamento",
        };
        static readonly string[] Mortes = { "MorteTras", "MorteFrente", "MorteQueda", "MorteQuedaFrente", "MorteJoelhos", "MorteEspada" };

        // tempos (s) dos passos dentro do arquivo Passos_Correr / Passos_Andar (medidos pela análise dos sons)
        static readonly float[] PassosCorrer = { 0.12f, 0.42f, 0.68f, 1.23f, 1.52f, 1.83f, 2.43f, 2.75f, 3.06f, 3.37f };
        static readonly float[] PassosAndar = { 0.06f, 0.57f, 1.09f, 1.6f, 2.11f, 2.59f, 3.12f };

        MovimentoDaAnimacao movimento;
        ModoCombate modo;
        TravaAlvo trava;
        Health vida;
        string estadoLoco = "Locomocao";
        float acaoAte, velocidade, distanciaPasso, pausaAte;
        bool naAcao, noAr, defendendo, morto;
        string estadoAtual = "";

        public bool Defendendo { get => defendendo; set => defendendo = value; }
        /// <summary>Informado pelo PlayerController (sprint ativo).</summary>
        public bool Sprint { get; set; }
        public bool OcupadoComAcao => naAcao;
        public bool Morto => morto;
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
        }

        void OnEnable() { if (vida != null) { vida.Died += Morrer; vida.Damaged += AoReceberDano; } }
        void OnDisable() { if (vida != null) { vida.Died -= Morrer; vida.Damaged -= AoReceberDano; } }

        void AoReceberDano(float dano, Vector3 ponto, GameObject origem)
        {
            // reação ao golpe recebido (não interrompe quem está defendendo)
            if (!morto && !defendendo) Acao("Dano", 0.5f, 0.05f);
        }

        /// <summary>
        /// Toca um estado do Animator pelo nome durante a duração informada (em segundos já na velocidade usada).
        /// <paramref name="velocidade"/> acelera/atrasa o clipe; <paramref name="espelho"/> troca esquerda por direita.
        /// </summary>
        public void Acao(string estado, float duracao, float fade = 0.06f, float velocidade = 1f, bool espelho = false)
        {
            if (animator == null || morto) return;
            int hash = Animator.StringToHash(estado);
            if (!animator.HasState(0, hash)) { Debug.LogWarning("[Animador] Estado ausente: " + estado); return; }
            animator.SetFloat(VelAcaoId, velocidade);
            animator.SetBool(EspelhoId, espelho);
            animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
            naAcao = true;
            noAr = false;
            acaoAte = Time.time + duracao;
            estadoAtual = estado;
            if (movimento != null)
            {
                movimento.Ativo = EstadosComMovimento.Contains(estado);
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

        public void EscalarMovimento(float escala) { if (movimento != null) movimento.Escala = escala; }

        /// <summary>Termina a ação atual na hora e volta à locomoção (usado ao cancelar golpes, meditação...).</summary>
        public void Encerrar(float fade = 0.15f)
        {
            if (animator == null || morto) return;
            naAcao = false;
            if (movimento != null) movimento.Ativo = false;
            animator.SetBool(EspelhoId, false);
            animator.CrossFadeInFixedTime(jogador != null && !jogador.IsGrounded ? NoArId : Animator.StringToHash(EstadoLoco()), fade);
        }

        /// <summary>Tempo real (s, já na velocidade do golpe) desde o início do estado informado, lido do próprio Animator; se não estiver tocando, devolve <paramref name="reserva"/>.</summary>
        public float TempoDoClipe(string estado, float reserva)
        {
            if (animator == null) return reserva;
            int hash = Animator.StringToHash(estado);
            var atual = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0))
            {
                var prox = animator.GetNextAnimatorStateInfo(0);
                if (prox.shortNameHash == hash) return prox.normalizedTime * prox.length;
            }
            return atual.shortNameHash == hash ? atual.normalizedTime * atual.length : reserva;
        }

        public void Pulou()
        {
            Acao("PuloImpulso", 0.17f, 0.04f);
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
            Vector3 p = transform.position;
            if (alturaDaQueda > 4.5f || (alturaDaQueda > 2.4f && velHoriz > 6f))
            {
                Acao("PousoRolamento", 1.4f, 0.05f);
                SomJogo.Tocar("Soco_Forte", p, 0.6f, 0.7f);
                Poeira(p, 50, 3.5f);
                TremorGlobal.Disparar(0.5f);
            }
            else if (alturaDaQueda > 2.0f)
            {
                Acao("PousoPesado", 1.0f, 0.05f);
                jogador?.Atordoar(0.7f);
                SomJogo.Tocar("Passos_Andar", p, 0.8f, 0.8f, inicio: 0.5f, duracaoMax: 0.35f);
                SomJogo.Tocar("Soco_Forte", p, 0.35f, 0.6f);
                Poeira(p, 40, 3f);
                TremorGlobal.Disparar(0.7f);
            }
            else
            {
                SomJogo.Tocar("Passos_Correr", p, 0.6f, 0.9f, inicio: 0.42f, duracaoMax: 0.25f);
                if (velHoriz < 3.5f && !naAcao)
                {
                    Acao("PousoLeve", 0.3f, 0.05f, 1.5f);
                    jogador?.Atordoar(0.12f);
                }
                else animator.CrossFadeInFixedTime(EstadoLoco(), 0.12f);
                Poeira(p, 12, 1.5f);
            }
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

        string EstadoLoco()
        {
            if (modo != null && modo.Atual == Modo.Espada) return "LocomocaoEspada";
            return "Locomocao";
        }

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
            animator.SetFloat(EscalaPassadaId, velocidade < 1f ? 1f : Mathf.Lerp(1.35f, 1.6f, Mathf.InverseLerp(5.2f, 8.5f, velocidade)));
            animator.SetFloat(VelYId, velY, 0.06f, Time.deltaTime);

            if (naAcao)
            {
                if (Time.time < acaoAte) return;
                naAcao = false;
                if (movimento != null) movimento.Ativo = false;
                animator.SetBool(EspelhoId, false);
                animator.CrossFadeInFixedTime(noAr ? NoArId : Animator.StringToHash(EstadoLoco()), 0.14f);
            }

            // no ar (pulo ou queda): a blend tree escolhe subida / topo / queda pela velocidade vertical
            if (!jogador.IsGrounded)
            {
                if (!noAr) { noAr = true; animator.CrossFadeInFixedTime(NoArId, 0.12f); }
                return;
            }
            noAr = false;

            // anda de costas: só com o alvo travado e o jogador se afastando dele
            string loco = EstadoLoco();
            bool recuando = false;
            if (trava != null && trava.Travado && alvo > 0.5f)
            {
                Vector3 paraAlvo = Vector3.ProjectOnPlane(trava.Alvo.position - transform.position, Vector3.up).normalized;
                recuando = Vector3.Dot(v.normalized, paraAlvo) < -0.6f;
            }
            if (recuando) loco = "LocomocaoTras";
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
        }
    }
}