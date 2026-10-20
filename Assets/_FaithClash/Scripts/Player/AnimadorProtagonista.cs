using UnityEngine;

namespace FaithClash.Player
{
    /// <summary>
    /// Liga o movimento do jogador ao Animator (animações do Mixamo). Ações (ataque, dash, deslize, pulo...)
    /// tocam por tempo determinado; fora delas o personagem alterna entre locomoção, defesa e queda.
    /// </summary>
    public class AnimadorProtagonista : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerController jogador;
        [SerializeField] float suavizacaoVelocidade = 10f;

        static readonly int VelocidadeId = Animator.StringToHash("Velocidade");
        static readonly int LocomocaoId = Animator.StringToHash("Locomocao");
        static readonly int DefesaId = Animator.StringToHash("Defesa");
        static readonly int PuloArId = Animator.StringToHash("PuloAr");

        float acaoAte, velocidade, ultimaMudanca;
        bool naAcao, noAr, defendendo;
        Vector3 direcaoAnterior;

        public bool Defendendo { get => defendendo; set => defendendo = value; }
        /// <summary>Informado pelo PlayerController (sprint ativo).</summary>
        public bool Sprint { get; set; }
        public bool OcupadoComAcao => naAcao;

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            jogador = GetComponent<PlayerController>();
        }

        /// <summary>Toca um estado do Animator pelo nome durante a duração informada.</summary>
        public void Acao(string estado, float duracao, float fade = 0.06f)
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(estado, fade, 0, 0f);
            naAcao = true;
            noAr = false;
            acaoAte = Time.time + duracao;
        }

        public void Pulou()
        {
            Acao("Pulo", 0.5f, 0.05f);
            noAr = true;
        }

        public void Aterrissou()
        {
            noAr = false;
            Acao("Aterrissagem", 0.3f, 0.05f);
        }

        void Update()
        {
            if (animator == null || jogador == null) return;

            Vector3 v = jogador.Velocity;
            v.y = 0f;
            float alvo = v.magnitude;
            float anterior = velocidade;
            velocidade = Mathf.Lerp(velocidade, alvo, suavizacaoVelocidade * Time.deltaTime);
            animator.SetFloat(VelocidadeId, velocidade);

            if (naAcao)
            {
                if (Time.time < acaoAte) return;
                naAcao = false;
                animator.CrossFadeInFixedTime(noAr ? PuloArId : LocomocaoId, 0.12f);
            }

            bool chao = jogador.IsGrounded;
            if (!chao && !noAr)
            {
                // caiu de uma borda
                noAr = true;
                animator.CrossFadeInFixedTime(PuloArId, 0.1f);
            }
            else if (chao && noAr)
            {
                noAr = false;
                animator.CrossFadeInFixedTime(LocomocaoId, 0.1f);
            }

            if (!chao) return;

            // arranque: sai do parado direto para o sprint / começa a andar
            if (anterior < 0.8f && alvo > 3f)
            {
                Acao(Sprint ? "ParadoParaCorrida" : "IniciarAndar", Sprint ? 0.45f : 0.4f, 0.08f);
                return;
            }

            // mudança brusca de direção correndo
            if (alvo > 4f)
            {
                Vector3 dir = v.normalized;
                if (direcaoAnterior != Vector3.zero && Vector3.Angle(direcaoAnterior, dir) > 140f && Time.time - ultimaMudanca > 0.8f)
                {
                    ultimaMudanca = Time.time;
                    direcaoAnterior = dir;
                    Acao("MudarDirecao", 0.4f, 0.05f);
                    return;
                }
                direcaoAnterior = dir;
            }
            else direcaoAnterior = Vector3.zero;

            bool emDefesa = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == DefesaId;
            if (defendendo && !emDefesa) animator.CrossFadeInFixedTime(DefesaId, 0.1f);
            else if (!defendendo && emDefesa) animator.CrossFadeInFixedTime(LocomocaoId, 0.12f);
        }
    }
}
