using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Camada de animação procedural por cima dos clipes, parte por parte do corpo:
    /// <list type="bullet">
    /// <item>Pescoço/cabeça/olhos: olham para o inimigo travado ou para onde a câmera aponta (IK de olhar do Animator).</item>
    /// <item>Dedos: punho fechado no taijutsu, mão aberta e tensa nos golpes de palma e jutsus, empunhadura na katana, mão relaxada parado.</item>
    /// <item>Tronco: inclina para a frente ao acelerar e para dentro da curva ao virar; respira quando está parado.</item>
    /// <item>Expressão: as sobrancelhas franzem em combate e sobem quando leva dano.</item>
    /// </list>
    /// Fica no objeto do Animator; roda depois da animação (LateUpdate) e no passe de IK.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Animator))]
    public class CorpoVivo : MonoBehaviour
    {
        [Header("Olhar")]
        [SerializeField] float pesoCabeca = 0.6f, pesoCorpo = 0.12f, pesoOlhos = 0.9f;
        [Header("Tronco")]
        [SerializeField] float inclinacaoFrente = 7f, inclinacaoCurva = 9f, respiracao = 1.4f;
        [Header("Dedos")]
        [SerializeField, Range(0f, 1f)] float forcaDedos = 0.85f;

        Animator animator;
        PlayerController jogador;
        AnimadorProtagonista anim;
        ModoCombate modo;
        TravaAlvo trava;
        MeleeCombat combate;
        Core.Health vida;
        Parkour parkour;
        float pesoPes;
        Transform cam, peito, coluna, sobrancelhas;
        Vector3 sobrancelhasBase;
        float pesoOlhar, inclF, inclL, ultimaVel, ultimoYaw, danoEm = -10f;
        float curlDir, curlEsq, pesoNinja;
        Transform bracoD, bracoE, antebracoD, antebracoE;

        // dedos: [mão][dedo][segmento]
        readonly Transform[,,] dedos = new Transform[2, 5, 3];
        Transform maoD, maoE;

        void Awake()
        {
            animator = GetComponent<Animator>();
            jogador = GetComponentInParent<PlayerController>();
            anim = GetComponentInParent<AnimadorProtagonista>();
            modo = GetComponentInParent<ModoCombate>();
            trava = GetComponentInParent<TravaAlvo>();
            combate = GetComponentInParent<MeleeCombat>();
            vida = GetComponentInParent<Core.Health>();
            parkour = GetComponentInParent<Parkour>();
            if (vida != null) vida.Damaged += (d, p, o) => danoEm = Time.time;
        }

        void Start()
        {
            if (animator == null || !animator.isHuman) return;
            peito = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            coluna = animator.GetBoneTransform(HumanBodyBones.Spine);
            maoD = animator.GetBoneTransform(HumanBodyBones.RightHand);
            bracoD = animator.GetBoneTransform(HumanBodyBones.RightUpperArm); bracoE = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            antebracoD = animator.GetBoneTransform(HumanBodyBones.RightLowerArm); antebracoE = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            maoE = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var ossos = new[,]
            {
                { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightIndexProximal, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightRingProximal, HumanBodyBones.RightLittleProximal },
                { HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftLittleProximal },
            };
            for (int m = 0; m < 2; m++)
                for (int f = 0; f < 5; f++)
                    for (int s = 0; s < 3; s++)
                        dedos[m, f, s] = animator.GetBoneTransform((HumanBodyBones)((int)ossos[m, f] + s));
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Eyebrows") { sobrancelhas = t; sobrancelhasBase = t.localPosition; }
        }

        // ------------------------------------------------------------------ olhar (pescoço, cabeça, olhos)
        void OnAnimatorIK(int camada)
        {
            if (animator == null || jogador == null) return;
            // (o IK de mãos e pés da escalada fica em EscaladaProcedural)
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            bool travado = trava != null && trava.Travado;
            bool emAcao = anim != null && anim.OcupadoComAcao;
            // a cabeça só vira para o inimigo travado (e parado ou andando devagar); correndo/de lado fica reta, sem olhar para os lados
            float vel = Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up).magnitude;
            float alvoPeso = jogador.ControleExterno ? 0f : travado ? (vel > 4f ? 0.25f : 0.8f) : (vel < 0.5f && !emAcao ? 0.3f : 0f);
            pesoOlhar = Mathf.MoveTowards(pesoOlhar, alvoPeso, Time.deltaTime * 4f);
            if (pesoOlhar < 0.01f) return;
            Vector3 ponto;
            if (travado) ponto = trava.PontoFoco;
            else ponto = transform.position + Vector3.up * 1.6f + transform.forward * 8f;
            animator.SetLookAtWeight(pesoOlhar, 0f, pesoCabeca * 0.7f, pesoOlhos, 0.4f);
            animator.SetLookAtPosition(ponto);
        }

        /// <summary>
        /// Ao transpor um obstáculo, as pernas do clipe podem passar por dentro dele (o clipe foi feito para outra altura/espessura):
        /// os pés (e os joelhos, pela dica) são levantados para ficarem sempre por cima do topo enquanto estão sobre o obstáculo.
        /// </summary>
        void PesAcimaDoObstaculo()
        {
            var c = parkour != null ? parkour.ObstaculoVault : null;
            pesoPes = Mathf.MoveTowards(pesoPes, c != null ? 1f : 0f, Time.deltaTime * 20f);
            if (c == null && pesoPes < 0.01f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f); animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f); animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f); animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                return;
            }
            if (c == null) return;
            Bounds b = c.bounds;
            float topo = b.max.y;
            for (int i = 0; i < 2; i++)
            {
                var meta = i == 0 ? AvatarIKGoal.LeftFoot : AvatarIKGoal.RightFoot;
                var joelho = i == 0 ? AvatarIKHint.LeftKnee : AvatarIKHint.RightKnee;
                Vector3 p = animator.GetIKPosition(meta);
                float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x), dz = Mathf.Max(b.min.z - p.z, 0f, p.z - b.max.z);
                float f = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dz * dz) / 0.4f);   // 1 = em cima/dentro, cai a 0 a 40 cm do obstáculo
                float acima = topo + 0.13f;
                bool precisa = p.y < acima && p.y > b.min.y - 0.1f;
                float w = precisa ? f * pesoPes : 0f;
                animator.SetIKPositionWeight(meta, w);
                animator.SetIKRotationWeight(meta, 0f);
                if (w > 0f)
                {
                    animator.SetIKPosition(meta, new Vector3(p.x, acima, p.z));
                    animator.SetIKHintPosition(joelho, new Vector3(p.x, acima + 0.35f, p.z) + transform.forward * 0.45f);
                    animator.SetIKHintPositionWeight(joelho, w * 0.6f);
                }
                else animator.SetIKHintPositionWeight(joelho, 0f);

                // mãos: apoiam por cima do topo, nunca atravessam o obstáculo
                var mao = i == 0 ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
                Vector3 pm = animator.GetIKPosition(mao);
                float mx = Mathf.Max(b.min.x - pm.x, 0f, pm.x - b.max.x), mz = Mathf.Max(b.min.z - pm.z, 0f, pm.z - b.max.z);
                float fm = Mathf.Clamp01(1f - Mathf.Sqrt(mx * mx + mz * mz) / 0.3f);
                bool dentro = pm.y < topo + 0.04f && pm.y > b.min.y;
                animator.SetIKPositionWeight(mao, dentro ? fm * pesoPes : 0f);
                animator.SetIKRotationWeight(mao, 0f);
                if (dentro) animator.SetIKPosition(mao, new Vector3(pm.x, topo + 0.05f, pm.z));
            }
        }

        void LateUpdate()
        {
            // sem inclinação do tronco, respiração nem dedos procedurais: o corpo segue só as animações
            return;
            if (animator == null || !animator.isHuman || jogador == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 vh = Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up);
            float vel = vh.magnitude;
            bool emAcao = anim != null && anim.OcupadoComAcao;
            string acao = anim != null ? anim.AcaoAtual : null;

            // ---------------- tronco: inclinação de aceleração e curva
            float acel = (vel - ultimaVel) / dt; ultimaVel = vel;
            float yaw = jogador.transform.eulerAngles.y;
            float giro = Mathf.DeltaAngle(ultimoYaw, yaw) / dt; ultimoYaw = yaw;
            bool locomocao = !emAcao && jogador.IsGrounded && !jogador.ControleExterno;
            float alvoF = locomocao ? Mathf.Clamp(acel * 0.6f + vel * 0.6f, -inclinacaoFrente, inclinacaoFrente) : 0f;
            float alvoL = locomocao && vel > 1.5f ? Mathf.Clamp(-giro * 0.04f * Mathf.Clamp01(vel / 5f), -inclinacaoCurva, inclinacaoCurva) : 0f;
            inclF = Mathf.Lerp(inclF, alvoF, 1f - Mathf.Exp(-6f * dt));
            inclL = Mathf.Lerp(inclL, alvoL, 1f - Mathf.Exp(-6f * dt));
            if (coluna != null)
            {
                Transform raiz = jogador.transform;
                coluna.rotation = Quaternion.AngleAxis(inclF, raiz.right) * Quaternion.AngleAxis(inclL, raiz.forward) * coluna.rotation;
            }
            // respiração (parado)
            if (peito != null && !emAcao && vel < 0.3f)
            {
                float r = Mathf.Sin(Time.time * Mathf.PI * 2f / 3.6f) * respiracao;
                peito.rotation = Quaternion.AngleAxis(-r, jogador.transform.right) * peito.rotation;
            }

            // ---------------- dedos
            float alvoD, alvoE;   // 0 = mão aberta e tensa, 0,3 = relaxada, 0,8 = empunhadura, 1 = punho
            bool jutsu = acao != null && (acao.Contains("Magic") || acao.Contains("Spell") || acao.Contains("Cast") || acao.Contains("Shinra") || acao.Contains("Gesture") || acao.Contains("Roar") || acao.Contains("Heal"));
            if (modo != null && modo.Atual == Modo.Espada) { alvoD = 0.85f; alvoE = modo.DuasMaos ? 0.85f : (combate != null && combate.IsAttacking ? 0.9f : 0.35f); }
            else if (jutsu || (acao != null && acao.Contains("Palma"))) { alvoD = 0.05f; alvoE = 0.05f; }
            else if (modo != null && modo.Atual == Modo.Combate) { alvoD = 1f; alvoE = 1f; }
            else if (vel > 4f) { alvoD = 0.6f; alvoE = alvoD; }
            else { alvoD = 0.32f; alvoE = 0.32f; }
            curlDir = Mathf.Lerp(curlDir, alvoD, 1f - Mathf.Exp(-14f * dt));
            curlEsq = Mathf.Lerp(curlEsq, alvoE, 1f - Mathf.Exp(-14f * dt));
            Dobrar(0, maoD, curlDir);
            Dobrar(1, maoE, curlEsq);

            // ---------------- expressão (sobrancelhas)
            if (sobrancelhas != null)
            {
                bool brava = (modo != null && modo.Atual != Modo.Poder) || (combate != null && combate.IsAttacking) || jutsu;
                bool dor = Time.time - danoEm < 0.6f;
                float y = dor ? 0.004f : brava ? -0.003f : 0f;
                Vector3 alvo = sobrancelhasBase + Vector3.up * y / Mathf.Max(0.0001f, transform.lossyScale.y);
                sobrancelhas.localPosition = Vector3.Lerp(sobrancelhas.localPosition, alvo, 1f - Mathf.Exp(-10f * dt));
            }
        }

        static readonly float[,] Dobra = { { 25f, 25f, 20f }, { 85f, 100f, 65f } };   // [aberta|punho][segmento]

        /// <summary>Dobra os dedos de uma mão até a curvatura pedida (0 = esticados, 1 = punho), sem depender dos eixos do esqueleto.</summary>
        void Dobrar(int m, Transform mao, float curl)
        {
            if (mao == null) return;
            var ind = dedos[m, 1, 0]; var med = dedos[m, 2, 0]; var min = dedos[m, 4, 0];
            if (ind == null || med == null || min == null) return;
            Vector3 frente = (med.position - mao.position).normalized;
            Vector3 lado = (ind.position - min.position).normalized;
            Vector3 palma = (m == 0 ? Vector3.Cross(frente, lado) : Vector3.Cross(lado, frente)).normalized;
            for (int f = 1; f < 5; f++)
            {
                Vector3 dirPai = frente;
                for (int s = 0; s < 3; s++)
                {
                    var b = dedos[m, f, s];
                    if (b == null) break;
                    Vector3 dir = s < 2 && dedos[m, f, s + 1] != null ? (dedos[m, f, s + 1].position - b.position).normalized : (s > 0 ? (b.position - dedos[m, f, s - 1].position).normalized : dirPai);
                    float atual = Vector3.Angle(dirPai, dir) * Mathf.Sign(Vector3.Dot(dir, palma) + 0.0001f);
                    float desejado = Mathf.Lerp(Dobra[0, s] * 0.25f, Dobra[1, s], curl);
                    float delta = (desejado - atual) * forcaDedos;
                    Vector3 alvoDir = Vector3.RotateTowards(dir, palma, delta * Mathf.Deg2Rad, 0f);
                    b.rotation = Quaternion.FromToRotation(dir, alvoDir) * b.rotation;
                    dirPai = alvoDir;
                }
            }
            // polegar: fecha por cima no punho, abre na mão espalmada
            var p0 = dedos[m, 0, 0];
            if (p0 != null && dedos[m, 0, 1] != null)
            {
                Vector3 dir = (dedos[m, 0, 1].position - p0.position).normalized;
                Vector3 alvo = Vector3.Slerp(dir, (palma + frente * 0.6f).normalized, (curl - 0.4f) * 0.5f * forcaDedos);
                p0.rotation = Quaternion.FromToRotation(dir, alvo) * p0.rotation;
            }
        }
    }
}
