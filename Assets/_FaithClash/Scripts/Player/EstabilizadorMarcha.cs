using UnityEngine;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Tira o "bambeio" das corridas e caminhadas: o quadril balança para os lados e gira alguns graus a cada passada.
    /// Mede esse balanço (giro do quadril em relação ao modelo), separa a média e contra-gira o modelo, para o personagem andar reto.
    /// Também inclina o tronco para a frente correndo e gira os pés para dentro (ajustes em Faith Clash > Ajustes de Movimento).
    /// Só age na locomoção (não nos golpes, pulos e ações). Fica no objeto do Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EstabilizadorMarcha : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float forcaGiro = 0.9f;
        [SerializeField] float constanteMedia = 0.5f;

        Animator animator;
        PlayerController jogador;
        AnimadorProtagonista anim;
        Transform quadril, dirPerna, esqPerna, peE, peD;
        float yawMedio, peso, inclFrenteAtual;
        bool iniciado;
        Quaternion rotBase;
        Vector3 posBase;
        /// <summary>Inclinação fixa (graus, em volta do quadril; negativo = para trás) — usada na rasteira. Zero = só a inclinação da corrida.</summary>
        public float InclinacaoFrente { get; set; }
        /// <summary>Deslocamento vertical do modelo (m): compensa a altura que a animação de subir alcança sozinha.</summary>
        public float DeslocY { get; set; }

        void Awake()
        {
            animator = GetComponent<Animator>();
            jogador = GetComponentInParent<PlayerController>();
            anim = GetComponentInParent<AnimadorProtagonista>();
            rotBase = transform.localRotation;
            posBase = transform.localPosition;
        }

        void LateUpdate()
        {
            if (animator == null || !animator.isHuman || jogador == null) return;
            if (quadril == null)
            {
                quadril = animator.GetBoneTransform(HumanBodyBones.Hips);
                dirPerna = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                esqPerna = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                if (quadril == null || dirPerna == null || esqPerna == null) return;
            }

            var A = AjustesMovimento.Atual;
            forcaGiro = A.estabilizarGiroQuadril;
            Vector3 horiz = jogador.Velocity; horiz.y = 0f;
            bool loco = jogador.IsGrounded && horiz.magnitude > 1.2f && (anim == null || !anim.OcupadoComAcao) && !jogador.ControleExterno;
            peso = Mathf.MoveTowards(peso, loco ? 1f : 0f, Time.deltaTime * 6f);

            // balanço medido no espaço do próprio modelo (não depende da correção aplicada no quadro anterior)
            Vector3 dir = transform.InverseTransformPoint(dirPerna.position) - transform.InverseTransformPoint(esqPerna.position);
            dir.y = 0f;
            float yaw = dir.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(Vector3.forward, Vector3.Cross(dir.normalized, Vector3.up), Vector3.up) : 0f;
            if (!iniciado) { yawMedio = yaw; iniciado = true; }
            float a = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, constanteMedia));
            if (loco) yawMedio = Mathf.LerpAngle(yawMedio, yaw, a);
            float dYaw = Mathf.DeltaAngle(yawMedio, yaw) * forcaGiro * peso;

            // correndo, o tronco inclina para a frente (mais no sprint)
            float vh = horiz.magnitude;
            float inclCorrida = vh < A.velAndar + 0.5f ? 0f : Mathf.Lerp(A.inclinacaoCorrida, A.inclinacaoSprint, Mathf.InverseLerp(A.velCorrida, A.velSprint, vh)) * Mathf.Clamp01((vh - A.velAndar) / Mathf.Max(0.5f, A.velCorrida - A.velAndar));
            float alvoFrente = Mathf.Abs(InclinacaoFrente) > 0.01f ? InclinacaoFrente : inclCorrida * peso;
            inclFrenteAtual = Mathf.Lerp(inclFrenteAtual, alvoFrente, 1f - Mathf.Exp(-10f * Time.deltaTime));
            var rel = Quaternion.Euler(inclFrenteAtual, -dYaw, 0f);
            var pivo = new Vector3(0f, 0.9f, 0f);   // gira em volta do quadril, não dos pés
            transform.localRotation = rotBase * rel;
            transform.localPosition = posBase + (pivo - rel * pivo) + Vector3.up * DeslocY;

            // pés virados para fora: gira cada pé para dentro em volta do eixo vertical do corpo
            if (Mathf.Abs(A.giroPesParaDentro) > 0.01f)
            {
                if (peE == null) { peE = animator.GetBoneTransform(HumanBodyBones.LeftFoot); peD = animator.GetBoneTransform(HumanBodyBones.RightFoot); }
                if (peE != null) peE.rotation = Quaternion.AngleAxis(A.giroPesParaDentro, transform.up) * peE.rotation;
                if (peD != null) peD.rotation = Quaternion.AngleAxis(-A.giroPesParaDentro, transform.up) * peD.rotation;
            }
        }
    }
}
