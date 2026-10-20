using UnityEngine;

namespace FaithClash.Player
{
    /// <summary>
    /// Liga o movimento do jogador ao Animator. Ações (ataque, esquiva) tocam por tempo determinado;
    /// fora delas o personagem alterna entre locomoção, defesa e queda.
    /// </summary>
    public class AnimadorProtagonista : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerController jogador;
        [SerializeField] float suavizacaoVelocidade = 12f;

        static readonly int VelocidadeId = Animator.StringToHash("Velocidade");
        static readonly int LocomocaoId = Animator.StringToHash("Locomocao");
        static readonly int DefesaId = Animator.StringToHash("Defesa");
        static readonly int PularId = Animator.StringToHash("Pular");

        float acaoAte, velocidade;
        bool naAcao, noAr, defendendo;

        public bool Defendendo
        {
            get => defendendo;
            set => defendendo = value;
        }

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

        void Update()
        {
            if (animator == null || jogador == null) return;

            Vector3 v = jogador.Velocity;
            v.y = 0f;
            velocidade = Mathf.Lerp(velocidade, v.magnitude, suavizacaoVelocidade * Time.deltaTime);
            animator.SetFloat(VelocidadeId, velocidade);

            if (naAcao)
            {
                if (Time.time < acaoAte) return;
                naAcao = false;
                animator.CrossFadeInFixedTime(LocomocaoId, 0.12f);
            }

            bool chao = jogador.IsGrounded;
            if (!chao && !noAr)
            {
                noAr = true;
                animator.CrossFadeInFixedTime(PularId, 0.08f);
            }
            else if (chao && noAr)
            {
                noAr = false;
                animator.CrossFadeInFixedTime(LocomocaoId, 0.1f);
            }

            if (chao)
            {
                bool emDefesa = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == DefesaId;
                if (defendendo && !emDefesa) animator.CrossFadeInFixedTime(DefesaId, 0.1f);
                else if (!defendendo && emDefesa) animator.CrossFadeInFixedTime(LocomocaoId, 0.12f);
            }
        }
    }
}
