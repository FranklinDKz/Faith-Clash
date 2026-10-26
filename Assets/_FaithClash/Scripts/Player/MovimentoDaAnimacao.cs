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
        /// <summary>Parkour (vault, escalada): o deslocamento da animação atravessa obstáculos (o CharacterController fica desligado).</summary>
        public bool SemColisao { get; set; }
        /// <summary>Parkour: aplica também a subida/descida da animação (multiplicada por <see cref="EscalaY"/>).</summary>
        public bool UsarY { get; set; }
        public float EscalaY { get; set; } = 1f;
        /// <summary>Se diferente de zero, o deslocamento horizontal fica só nesse eixo (parkour: não escorrega para o lado).</summary>
        public Vector3 Eixo { get; set; }

        void Awake()
        {
            animator = GetComponent<Animator>();
            cc = GetComponentInParent<CharacterController>();
        }

        void OnAnimatorMove()
        {
            if (!Ativo || cc == null) return;
            Vector3 d = animator.deltaPosition;
            Vector3 hz = new Vector3(d.x, 0f, d.z);
            if (Eixo.sqrMagnitude > 0.01f) hz = Eixo * Vector3.Dot(hz, Eixo);
            Vector3 m = hz * Escala + Vector3.up * (UsarY ? d.y * EscalaY : 0f);
            if (m.sqrMagnitude < 1e-10f) return;
            if (SemColisao || !cc.enabled) cc.transform.position += m;
            else cc.Move(UsarY || !cc.isGrounded ? m : m + Vector3.down * 2.4f * Time.deltaTime);   // mantém o pé no chão só quando já está no chão (por segundo, não por quadro: antes puxava o pulo para baixo em FPS alto)
        }
    }
}
