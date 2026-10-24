using UnityEngine;

namespace FaithClash.Player
{
    /// <summary>
    /// Ajuste de pés ao terreno (rampas, degraus, caixas) SEM alterar a animação em chão plano:
    /// cada pé só se desloca pela diferença de altura entre o chão sob ele e o chão sob o personagem.
    /// A bacia desce quando um pé apoiado precisa descer, e sobe quando uma pose baixa (deslize, rolamento, queda)
    /// faria o pé / a ponta do pé atravessar o chão. Fica no mesmo objeto do Animator (o "Modelo").
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PesNoChao : MonoBehaviour
    {
        [SerializeField] LayerMask camadasChao = ~0;
        [SerializeField] float alcanceAcima = 0.6f;
        [SerializeField] float alcanceAbaixo = 0.8f;
        [SerializeField] float distanciaAtivacao = 0.18f;   // só ajusta com o pé a menos disso do chão
        [SerializeField] float alturaMinTornozelo = 0.075f; // tornozelo nunca abaixo disso do chão
        [SerializeField] float alturaMinPonta = 0.025f;     // ponta do pé nunca abaixo disso do chão
        [SerializeField] float suavizacao = 16f;

        Animator animator;
        PlayerController jogador;
        float pesoGeral = 1f, deslocBacia;

        void Awake()
        {
            animator = GetComponent<Animator>();
            jogador = GetComponentInParent<PlayerController>();
        }

        bool Chao(Vector3 de, out RaycastHit h)
        {
            return Physics.Raycast(de + Vector3.up * alcanceAcima, Vector3.down, out h, alcanceAcima + alcanceAbaixo, camadasChao, QueryTriggerInteraction.Ignore)
                   && !h.transform.IsChildOf(jogador != null ? jogador.transform : transform);
        }

        void OnAnimatorIK(int camada)
        {
            if (animator == null || !animator.isHuman) return;
            bool ativo = jogador != null && jogador.IsGrounded && !jogador.IsDashing && !jogador.IsWallRunning;
            pesoGeral = Mathf.MoveTowards(pesoGeral, ativo ? 1f : 0f, Time.deltaTime * 6f);

            var objetivos = new[] { AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot };
            var ossoPe = new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
            var ossoPonta = new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes };

            // chão sob o personagem (referência: onde a animação "acha" que o chão está)
            float chaoBase = jogador != null ? jogador.transform.position.y : transform.position.y;
            if (jogador != null && Chao(jogador.transform.position, out RaycastHit hb)) chaoBase = hb.point.y;

            float menorDesc = 0f, maiorSobe = 0f;
            var alvoPos = new Vector3[2];
            var alvoRot = new Quaternion[2];
            var peso = new float[2];
            for (int i = 0; i < 2; i++)
            {
                Vector3 pe = animator.GetIKPosition(objetivos[i]);
                var tPonta = animator.GetBoneTransform(ossoPonta[i]);
                if (!Chao(pe, out RaycastHit h)) continue;

                float delta = h.point.y - chaoBase;                     // quanto o chão sob este pé é mais alto/baixo
                float folga = pe.y - h.point.y;                          // altura do tornozelo sobre o chão sob o pé
                float wGeo = Mathf.Clamp01(1f - Mathf.Max(0f, folga - 0.14f) / distanciaAtivacao);   // pé no ar: sem IK
                alvoPos[i] = new Vector3(pe.x, pe.y + delta, pe.z);
                alvoRot[i] = Quaternion.FromToRotation(Vector3.up, h.normal) * animator.GetIKRotation(objetivos[i]);
                peso[i] = wGeo * pesoGeral * (Mathf.Abs(delta) > 0.01f ? 1f : 0f);
                menorDesc = Mathf.Min(menorDesc, delta * wGeo);

                // anti-penetração: pose baixa (deslize, rolamento, queda) não pode enfiar pé / ponta no chão
                float subir = Mathf.Max(h.point.y + alturaMinTornozelo - pe.y, tPonta != null ? h.point.y + alturaMinPonta - tPonta.position.y : 0f);
                maiorSobe = Mathf.Max(maiorSobe, subir);
            }

            float alvoBacia = Mathf.Clamp(menorDesc, -0.2f, 0f) + Mathf.Clamp(maiorSobe, 0f, 0.3f);
            alvoBacia *= pesoGeral;
            deslocBacia = Mathf.Lerp(deslocBacia, alvoBacia, 1f - Mathf.Exp(-suavizacao * Time.deltaTime));
            animator.bodyPosition += Vector3.up * deslocBacia;

            for (int i = 0; i < 2; i++)
            {
                animator.SetIKPositionWeight(objetivos[i], peso[i]);
                animator.SetIKRotationWeight(objetivos[i], peso[i] * 0.8f);
                if (peso[i] > 0f)
                {
                    animator.SetIKPosition(objetivos[i], alvoPos[i] + Vector3.up * deslocBacia * 0f);
                    animator.SetIKRotation(objetivos[i], alvoRot[i]);
                }
            }
        }
    }
}
