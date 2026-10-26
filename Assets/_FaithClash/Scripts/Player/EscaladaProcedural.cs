using UnityEngine;

namespace FaithClash.Player
{
    /// <summary>
    /// Animação de escalada/parkour feita em código (sem clipe), em cima da pose de parado:
    /// 1) IK humanoide leva mãos e pés a pontos de apoio (o Parkour define os alvos a cada quadro);
    /// 2) uma camada dinâmica mexe no corpo todo: quadril, coluna, peito, ombros, pescoço e cabeça (torção, arqueio, inclinação, encolher de ombros, olhar),
    ///    com resposta suave (mola) para o movimento ter peso e continuidade.
    /// Fica no objeto do Animator (precisa do IK Pass ligado na camada 0).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EscaladaProcedural : MonoBehaviour
    {
        static readonly AvatarIKGoal[] Metas = { AvatarIKGoal.LeftHand, AvatarIKGoal.RightHand, AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot };
        public const int MaoE = 0, MaoD = 1, PeE = 2, PeD = 3;

        /// <summary>Quanto de cada movimento do corpo vai para cada osso (soma 1 em cada eixo).</summary>
        static readonly float[] PartePeito = { 0.30f, 0.40f, 0.30f };   // coluna, peito, peito superior

        Animator animator;
        readonly Vector3[] alvo = new Vector3[4];
        readonly float[] peso = new float[4];
        bool ativo;
        Transform quadril, coluna, peito, peitoAlto, pescoco, cabeca, ombroE, ombroD;

        // alvos da dinâmica do corpo (graus); o corpo segue com uma mola
        float tTorcao, tArqueio, tInclinacao, tQuadrilGiro, tQuadrilInclina, tOmbroE, tOmbroD, tCabecaInclina;
        float torcao, arqueio, inclinacao, quadrilGiro, quadrilInclina, ombroEv, ombroDv, cabecaInclina;
        Vector3 pontoOlhar; float tOlharPeso, olharPeso;
        /// <summary>Velocidade da resposta (maior = mais seco, menor = mais macio).</summary>
        public float Resposta { get; set; } = 22f;

        public int Chamadas { get; private set; }
        public Vector3 AlvoMaoD => alvo[MaoD];
        public bool Ativo => ativo;

        void Awake() { animator = GetComponent<Animator>(); }

        /// <summary>Leva a meta (mão/pé) até o ponto do mundo com a força indicada (0 a 1).</summary>
        public void Definir(int meta, Vector3 pos, float forca)
        {
            alvo[meta] = pos; peso[meta] = Mathf.Clamp01(forca); ativo = true;
        }

        /// <summary>Dinâmica do corpo: torção do tronco (+ = para a direita), arqueio (+ = inclina para a frente), inclinação lateral (+ = para a esquerda),
        /// giro e inclinação do quadril, ombros (encolher, graus) e inclinação da cabeça.</summary>
        public void Corpo(float torcao, float arqueio, float inclinacao, float quadrilGiro = 0f, float quadrilInclina = 0f, float ombroE = 0f, float ombroD = 0f, float cabecaInclina = 0f)
        {
            tTorcao = torcao; tArqueio = arqueio; tInclinacao = inclinacao; tQuadrilGiro = quadrilGiro; tQuadrilInclina = quadrilInclina; tOmbroE = ombroE; tOmbroD = ombroD; tCabecaInclina = cabecaInclina;
            ativo = true;
        }

        public void Olhar(Vector3 ponto, float forca) { pontoOlhar = ponto; tOlharPeso = Mathf.Clamp01(forca); }

        /// <summary>Solta tudo (volta à pose normal).</summary>
        public void Limpar()
        {
            for (int i = 0; i < 4; i++) peso[i] = 0f;
            tTorcao = tArqueio = tInclinacao = tQuadrilGiro = tQuadrilInclina = tOmbroE = tOmbroD = tCabecaInclina = 0f;
            tOlharPeso = 0f;
            ativo = false;
        }

        void OnAnimatorIK(int camada)
        {
            if (animator == null) return;
            Chamadas++;
            for (int i = 0; i < 4; i++)
            {
                float w = ativo ? peso[i] : 0f;
                animator.SetIKPositionWeight(Metas[i], w);
                animator.SetIKRotationWeight(Metas[i], 0f);
                if (w > 0f) animator.SetIKPosition(Metas[i], alvo[i]);
            }
            if (olharPeso > 0.01f)
            {
                animator.SetLookAtWeight(olharPeso, 0.35f, 0.85f, 1f, 0.5f);
                animator.SetLookAtPosition(pontoOlhar);
            }
            if (!ativo) return;
            // joelhos e cotovelos para fora/para frente (sem isso o IK dobra as juntas para qualquer lado)
            Vector3 f = transform.forward, d = transform.right;
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, peso[PeE]); animator.SetIKHintPosition(AvatarIKHint.LeftKnee, transform.position + Vector3.up * 0.6f + f * 0.9f - d * 0.35f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, peso[PeD]); animator.SetIKHintPosition(AvatarIKHint.RightKnee, transform.position + Vector3.up * 0.6f + f * 0.9f + d * 0.35f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, peso[MaoE]); animator.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.position + Vector3.up * 1.4f - f * 0.5f - d * 0.8f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, peso[MaoD]); animator.SetIKHintPosition(AvatarIKHint.RightElbow, transform.position + Vector3.up * 1.4f - f * 0.5f + d * 0.8f);
        }

        void Ossos()
        {
            if (quadril != null || animator == null || !animator.isHuman) return;
            quadril = animator.GetBoneTransform(HumanBodyBones.Hips);
            coluna = animator.GetBoneTransform(HumanBodyBones.Spine);
            peito = animator.GetBoneTransform(HumanBodyBones.Chest);
            peitoAlto = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            pescoco = animator.GetBoneTransform(HumanBodyBones.Neck);
            cabeca = animator.GetBoneTransform(HumanBodyBones.Head);
            ombroE = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            ombroD = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        }

        // gira o osso em volta dos eixos do corpo (frente, direita, cima), por cima do que a animação já fez
        void Girar(Transform osso, float yaw, float pitch, float roll)
        {
            if (osso == null || (Mathf.Abs(yaw) + Mathf.Abs(pitch) + Mathf.Abs(roll)) < 0.01f) return;
            osso.rotation = Quaternion.AngleAxis(yaw, transform.up) * Quaternion.AngleAxis(pitch, transform.right) * Quaternion.AngleAxis(roll, transform.forward) * osso.rotation;
        }

        void LateUpdate()
        {
            Ossos();
            if (quadril == null) return;
            float a = 1f - Mathf.Exp(-Resposta * Time.deltaTime);
            torcao = Mathf.Lerp(torcao, tTorcao, a); arqueio = Mathf.Lerp(arqueio, tArqueio, a); inclinacao = Mathf.Lerp(inclinacao, tInclinacao, a);
            quadrilGiro = Mathf.Lerp(quadrilGiro, tQuadrilGiro, a); quadrilInclina = Mathf.Lerp(quadrilInclina, tQuadrilInclina, a);
            ombroEv = Mathf.Lerp(ombroEv, tOmbroE, a); ombroDv = Mathf.Lerp(ombroDv, tOmbroD, a); cabecaInclina = Mathf.Lerp(cabecaInclina, tCabecaInclina, a);
            olharPeso = Mathf.Lerp(olharPeso, tOlharPeso, 1f - Mathf.Exp(-10f * Time.deltaTime));
            if (!ativo && Mathf.Abs(torcao) + Mathf.Abs(arqueio) + Mathf.Abs(inclinacao) + Mathf.Abs(quadrilGiro) + Mathf.Abs(quadrilInclina) + Mathf.Abs(ombroEv) + Mathf.Abs(ombroDv) + Mathf.Abs(cabecaInclina) < 0.02f) return;

            Girar(quadril, quadrilGiro, 0f, quadrilInclina);
            // tronco: a torção, o arqueio e a inclinação se repartem entre coluna, peito e peito superior
            Transform[] tronco = { coluna, peito, peitoAlto };
            for (int i = 0; i < tronco.Length; i++)
                if (tronco[i] != null) Girar(tronco[i], torcao * PartePeito[i], arqueio * PartePeito[i], inclinacao * PartePeito[i]);
            // ombros encolhem (sobem) e acompanham o braço que sobe
            Girar(ombroE, 0f, 0f, ombroEv); Girar(ombroD, 0f, 0f, -ombroDv);
            // pescoço e cabeça compensam parte do tronco (a cabeça tenta ficar reta) e inclinam para o lado do movimento
            Girar(pescoco, -torcao * 0.25f, -arqueio * 0.3f, cabecaInclina * 0.5f);
            Girar(cabeca, -torcao * 0.2f, -arqueio * 0.3f, cabecaInclina * 0.5f);
        }
    }
}
