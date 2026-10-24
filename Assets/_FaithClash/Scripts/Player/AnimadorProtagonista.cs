using System.Collections;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Liga o movimento do jogador ao Animator (animações do Mixamo).
    /// Locomoção em blend trees (poder / espada / de costas), ações por tempo (golpes, dash, pulo, rolamento...),
    /// pouso conforme a altura da queda, passos sincronizados com a distância andada e morte.
    /// </summary>
    public class AnimadorProtagonista : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerController jogador;

        static readonly int VelocidadeId = Animator.StringToHash("Velocidade");
        static readonly int EscalaPassadaId = Animator.StringToHash("EscalaPassada");
        static readonly int DefesaId = Animator.StringToHash("Defesa");
        static readonly int PuloArId = Animator.StringToHash("PuloAr");

        static readonly string[] EstadosComMovimento =
        {
            "Corte1", "Corte2", "Corte3", "CorteGiratorio", "EspadaSalto", "AtaqueDeslizante", "AtaqueAereo",
            "Soco1", "Soco2", "Soco3", "Chute", "ChuteGiratorio", "PousoRolamento",
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
        float acaoAte, velocidade, ultimaMudanca, distanciaPasso, tempoParado;
        bool naAcao, noAr, defendendo, morto;
        Vector3 direcaoAnterior;
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
            if (!morto) Acao("Dano", 0.5f, 0.05f);
        }

        /// <summary>Toca um estado do Animator pelo nome durante a duração informada.</summary>
        public void Acao(string estado, float duracao, float fade = 0.06f)
        {
            if (animator == null || morto) return;
            int hash = Animator.StringToHash(estado);
            if (!animator.HasState(0, hash)) { Debug.LogWarning("[Animador] Estado ausente: " + estado); return; }
            animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
            naAcao = true;
            noAr = false;
            acaoAte = Time.time + duracao;
            estadoAtual = estado;
            if (movimento != null)
            {
                movimento.Ativo = System.Array.IndexOf(EstadosComMovimento, estado) >= 0;
                movimento.Escala = 1f;
            }
        }

        public void EscalarMovimento(float escala) { if (movimento != null) movimento.Escala = escala; }

        public void Pulou()
        {
            Acao("Pulo", 0.55f, 0.05f);
            noAr = true;
        }

        public void Aterrissou()
        {
            noAr = false;
            animator?.CrossFadeInFixedTime(EstadoLoco(), 0.1f);
        }

        /// <summary>Pouso conforme o tempo de queda (queda alta = pouso pesado ou rolamento).</summary>
        public void Pousar(float tempoDeQueda)
        {
            noAr = false;
            Vector3 v = jogador.Velocity; v.y = 0f;
            if (tempoDeQueda > 1.1f || (tempoDeQueda > 0.7f && v.magnitude > 6f)) { Acao("PousoRolamento", 1.4f, 0.05f); SomJogo.Tocar("Soco_Forte", transform.position, 0.6f, 0.7f); }
            else if (tempoDeQueda > 0.65f) { Acao("PousoPesado", 1.0f, 0.05f); SomJogo.Tocar("Passos_Andar", transform.position, 0.8f, 0.8f, inicio: 0.5f, duracaoMax: 0.35f); }
            else
            {
                SomJogo.Tocar("Passos_Correr", transform.position, 0.6f, 0.9f, inicio: 0.42f, duracaoMax: 0.25f);
                animator.CrossFadeInFixedTime(EstadoLoco(), 0.12f);
            }
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

            Vector3 v = jogador.Velocity;
            v.y = 0f;
            float alvo = v.magnitude;
            float anterior = velocidade;
            velocidade = Mathf.Lerp(velocidade, alvo, 1f - Mathf.Exp(-10f * Time.deltaTime));
            animator.SetFloat(VelocidadeId, velocidade);
            animator.SetFloat(EscalaPassadaId, velocidade < 1f ? 1f : Mathf.Lerp(1.35f, 1.6f, Mathf.InverseLerp(5.2f, 8.5f, velocidade)));

            if (naAcao)
            {
                if (Time.time < acaoAte) return;
                naAcao = false;
                if (movimento != null) movimento.Ativo = false;
                animator.CrossFadeInFixedTime(noAr ? PuloArId : Animator.StringToHash(EstadoLoco()), 0.14f);
            }

            bool chao = jogador.IsGrounded;
            if (!chao && !noAr)
            {
                noAr = true;   // caiu de uma borda
                animator.CrossFadeInFixedTime(PuloArId, 0.12f);
            }
            if (!chao) return;
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

            // arranque
            if (anterior < 0.8f && alvo > 3.5f && !recuando)
            {
                Acao(Sprint ? "ParadoParaCorrida" : "IniciarAndar", Sprint ? 0.6f : 0.7f, 0.08f);
                return;
            }

            // mudança brusca de direção correndo
            if (alvo > 4.5f)
            {
                Vector3 dir = v.normalized;
                if (direcaoAnterior != Vector3.zero && Vector3.Angle(direcaoAnterior, dir) > 140f && Time.time - ultimaMudanca > 0.9f)
                {
                    ultimaMudanca = Time.time;
                    direcaoAnterior = dir;
                    bool espada = modo != null && modo.Atual == Modo.Espada;
                    Acao(espada ? "Virar180Espada" : "Virar180Poder", espada ? 0.55f : 0.6f, 0.05f);
                    return;
                }
                direcaoAnterior = dir;
            }
            else direcaoAnterior = Vector3.zero;

            // defesa
            bool emDefesa = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == DefesaId;
            if (defendendo && !emDefesa) animator.CrossFadeInFixedTime(DefesaId, 0.1f);
            else if (!defendendo && emDefesa) animator.CrossFadeInFixedTime(Animator.StringToHash(loco), 0.12f);

            // descanso: depois de um tempo parado sem fazer nada, relaxa
            if (alvo < 0.1f && !defendendo)
            {
                tempoParado += Time.deltaTime;
                if (tempoParado > 12f && (modo == null || modo.Atual == Modo.Poder)) { tempoParado = 0f; Acao("IdleDescanso", 3f, 0.4f); }
            }
            else tempoParado = 0f;

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
