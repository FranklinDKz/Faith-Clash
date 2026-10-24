using UnityEngine;

namespace FaithClash.Player
{
    /// <summary>
    /// Aplica o deslocamento horizontal que vem da própria animação (golpes, rolamento) ao CharacterController,
    /// com colisão — o personagem avança exatamente o que o clipe do Mixamo "anda". Fica no objeto do Animator.
    /// Nas animações de locomoção o deslocamento é ignorado (quem move é o PlayerController).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MovimentoDaAnimacao : MonoBehaviour
    {
        CharacterController cc;
        Animator animator;
        /// <summary>Multiplicador do deslocamento (ex.: reduzir quando o alvo está perto).</summary>
        public float Escala { get; set; } = 1f;
        /// <summary>Quando verdadeiro, o deslocamento do clipe move o personagem.</summary>
        public bool Ativo { get; set; }

        void Awake()
        {
            animator = GetComponent<Animator>();
            cc = GetComponentInParent<CharacterController>();
        }

        void OnAnimatorMove()
        {
            if (!Ativo || cc == null || !cc.enabled) return;
            Vector3 d = animator.deltaPosition;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-8f) return;
            cc.Move(d * Escala);
        }
    }
}
