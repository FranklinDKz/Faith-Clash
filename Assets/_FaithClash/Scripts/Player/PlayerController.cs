using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;

namespace FaithClash.Player
{
    /// <summary>
    /// Movimento do jogador (controles no estilo Naraka: Bladepoint):
    /// WASD move, Shift corre (sprint), Espaço pula (segurar = pulo mais alto), C dá dash para frente, Ctrl rola.
    /// Física: CharacterController com colisão, gravidade, degraus, rampas e aderência ao chão.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] float runSpeed = 5.5f;
        [SerializeField] float sprintSpeed = 9f;
        [SerializeField] float turnSpeed = 14f;
        [SerializeField] float aceleracao = 38f;
        [SerializeField] float jumpHeight = 1.5f;
        [SerializeField] float gravity = -26f;
        [SerializeField] float multiplicadorQueda = 1.45f;     // cai mais rápido do que sobe (peso)
        [SerializeField] float multiplicadorPuloCurto = 2.2f;  // soltar o Espaço cedo corta o pulo
        [SerializeField] float controleNoAr = 0.45f;
        [SerializeField] float velocidadeTerminal = -45f;
        [SerializeField] float tempoCoiote = 0.12f;
        [SerializeField] float bufferPulo = 0.12f;
        [SerializeField] LayerMask camadasChao = ~0;

        [Header("Dash (C)")]
        [SerializeField] float dashSpeed = 19f;
        [SerializeField] float dashTime = 0.3f;
        [SerializeField] float dashCooldown = 0.9f;

        [Header("Rolamento (Ctrl)")]
        [SerializeField] float slideCooldown = 0.7f;
        [SerializeField] float rollSpeed = 6.5f;
        [SerializeField] float rollTime = 0.8f;

        [Header("Animação")]
        [SerializeField] AnimadorProtagonista animador;

        CharacterController cc;
        TravaAlvo trava;
        MeleeCombat combate;
        Transform cam;
        float verticalVelocity, tempoNoChao, ultimoNoChao, tempoNoAr, ultimoTempoNoAr, pularEm = -10f, apiceY;
        float dashTimer, dashReady, slideTimer, slideReady, atordoadoAte, avancoAte, avancoVel;
        Vector3 avancoDir;
        Vector3 burstDir, velHoriz;
        bool noChaoAntes = true;
        float alturaOriginal;
        Vector3 centroOriginal;

        public bool IsDashing => dashTimer > 0f;
        public bool IsDodging => slideTimer > 0f;
        public bool IsBusy => IsDodging || IsDashing;
        public bool Atordoado => Time.time < atordoadoAte;
        /// <summary>Chão sólido sob o personagem (com tolerância de "coiote").</summary>
        public bool IsGrounded => cc.isGrounded || (Time.time - ultimoNoChao) < 0.05f;
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 NormalDoChao { get; private set; } = Vector3.up;
        public bool Congelado { get; set; }
        /// <summary>Definido pelo combate para reduzir a velocidade ao atacar ou defender.</summary>
        public float MoveMultiplier { get; set; } = 1f;

        /// <summary>Prende o personagem no lugar (pouso pesado, itens, gestos de poder).</summary>
        /// <summary>Passo à frente durante o golpe (o taijutsu fecha a distância até o inimigo): anda <paramref name="distancia"/> metros em <paramref name="tempo"/> s.</summary>
        public void Avancar(Vector3 direcao, float distancia, float tempo)
        {
            if (distancia <= 0.01f || tempo <= 0.01f) return;
            direcao.y = 0f;
            avancoDir = direcao.normalized;
            avancoVel = distancia / tempo;
            avancoAte = Time.time + tempo;
        }

        public void Atordoar(float segundos) => atordoadoAte = Mathf.Max(atordoadoAte, Time.time + segundos);

        public event System.Action Dashed;
        public event System.Action Pulou;
        public event System.Action<float> Aterrissou;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            trava = GetComponent<TravaAlvo>();
            combate = GetComponent<MeleeCombat>();
            cam = Camera.main != null ? Camera.main.transform : null;
            alturaOriginal = cc.height;
            centroOriginal = cc.center;
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 55f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;
        }

        void Update()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked && !Congelado;
            bool noChao = cc.isGrounded;
            if (noChao) { ultimoNoChao = Time.time; tempoNoAr = 0f; } else tempoNoAr += Time.deltaTime;

            Vector2 input = cursorPreso && !Atordoado ? ReadMove() : Vector2.zero;
            bool sprint = cursorPreso && ((Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
                       || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed));

            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            Vector3 wish = forward * input.y + right * input.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            AtualizaNormalDoChao();
            // acompanha rampas: o movimento fica paralelo ao chão
            if (noChao && wish.sqrMagnitude > 0.001f)
                wish = Vector3.ProjectOnPlane(wish, NormalDoChao).normalized * wish.magnitude;

            // ---------- dash (C): arrancada para frente
            if (cursorPreso && WasDashPressed() && Time.time >= dashReady && !IsBusy)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? new Vector3(wish.x, 0, wish.z).normalized : forward;
                dashTimer = dashTime;
                dashReady = Time.time + dashCooldown;
                transform.rotation = Quaternion.LookRotation(burstDir);
                combate?.Cancelar();   // o dash cancela o golpe (esquiva estilo Naraka)
                FaithClash.Core.CatalogoVFX.Tocar("poeira", transform.position + Vector3.up * 0.2f, Quaternion.LookRotation(-burstDir), null, 1.2f, 2f);
                animador?.Acao("Dash", 0.5f, 0.04f, 1.7f);
                Dashed?.Invoke();
            }

            // ---------- rolamento (Ctrl): esquiva no chão, cancela o golpe
            if (cursorPreso && WasSlidePressed() && Time.time >= slideReady && !IsBusy && noChao && !Atordoado)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? new Vector3(wish.x, 0, wish.z).normalized : transform.forward;
                slideReady = Time.time + slideCooldown;
                combate?.Cancelar();
                // esquiva para o lado / para trás (estilo Naruto Storm / Naraka): passo rápido sem virar; para frente ou parado: rolamento
                float pFrente = Vector3.Dot(burstDir, transform.forward), pLado = Vector3.Dot(burstDir, transform.right);
                string esquiva = null;
                if (wish.sqrMagnitude > 0.01f)
                {
                    if (pFrente < -0.6f) esquiva = "Mx_Standing_Dodge_Backward";
                    else if (pLado > 0.7f) esquiva = "Mx_Standing_Dodge_Right";
                    else if (pLado < -0.7f) esquiva = "Mx_Standing_Dodge_Left";
                }
                if (esquiva != null)
                {
                    slideTimer = 0.5f;
                    animador?.Acao(esquiva, 0.55f, 0.05f, 1.9f);
                    DefineAltura(alturaOriginal);
                }
                else
                {
                    slideTimer = rollTime;
                    transform.rotation = Quaternion.LookRotation(burstDir);
                    animador?.Acao("Rolamento", 1.3f, 0.05f, 1.13f);
                    DefineAltura(1.1f);
                }
            }
            Vector3 alvoHoriz;
            if (IsDashing)
            {
                dashTimer -= Time.deltaTime;
                alvoHoriz = burstDir * dashSpeed;
                velHoriz = alvoHoriz;
            }
            else if (IsDodging)
            {
                slideTimer -= Time.deltaTime;
                alvoHoriz = burstDir * (rollSpeed * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(slideTimer / rollTime)));
                velHoriz = alvoHoriz;
                if (slideTimer <= 0f) DefineAltura(alturaOriginal);
            }
            else
            {
                IsSprinting = sprint && wish.sqrMagnitude > 0.05f;
                float vel = (IsSprinting ? sprintSpeed : runSpeed) * MoveMultiplier;
                alvoHoriz = wish * vel;
                // aceleração suave (sem "patinar" nem parar bruscamente)
                float a = (noChao ? aceleracao : aceleracao * controleNoAr) * Time.deltaTime;
                velHoriz = Vector3.MoveTowards(velHoriz, alvoHoriz, a);
                AtualizaRotacao(wish);
                if (animador != null) animador.Sprint = IsSprinting;
            }

            // ---------- pulo (com buffer e tempo de coiote); segurar o Espaço = mais alto
            if (cursorPreso && !Atordoado && WasJumpPressed()) pularEm = Time.time;
            bool podePular = noChao || (Time.time - ultimoNoChao) < tempoCoiote;
            if (Time.time - pularEm < bufferPulo && !IsBusy && podePular)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                ultimoNoChao = -10f;
                pularEm = -10f;
                combate?.Cancelar();
                animador?.Pulou();
                Pulou?.Invoke();
                noChao = false;
            }

            bool segurandoPulo = cursorPreso && ((Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                                 || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed));
            if (IsDashing)
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);
            else if (noChao && verticalVelocity <= 0f)
                verticalVelocity = -4f;   // gruda no chão (desce rampas e degraus sem "quicar")
            else
            {
                float g = gravity;
                if (verticalVelocity < 0f) g *= multiplicadorQueda;
                else if (!segurandoPulo) g *= multiplicadorPuloCurto;
                verticalVelocity = Mathf.Max(velocidadeTerminal, verticalVelocity + g * Time.deltaTime);
            }
            Vector3 extra = Time.time < avancoAte && !IsBusy ? avancoDir * avancoVel : Vector3.zero;
            Velocity = velHoriz + extra + Vector3.up * verticalVelocity;
            var flags = cc.Move(Velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;

            bool agoraNoChao = cc.isGrounded;
            if (!agoraNoChao) { ultimoTempoNoAr = tempoNoAr; apiceY = Mathf.Max(apiceY, transform.position.y); }
            if (agoraNoChao && !noChaoAntes)
            {
                if (ultimoTempoNoAr > 0.12f) { animador?.Pousar(apiceY - transform.position.y, velHoriz.magnitude); Aterrissou?.Invoke(ultimoTempoNoAr); }
            }
            if (agoraNoChao) apiceY = transform.position.y;
            noChaoAntes = agoraNoChao;
        }

        void AtualizaNormalDoChao()
        {
            Vector3 origem = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origem, Vector3.down, out RaycastHit h, 1.2f, camadasChao, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(transform))
                NormalDoChao = Vector3.Lerp(NormalDoChao, h.normal, 0.5f);
            else NormalDoChao = Vector3.up;
        }

        void DefineAltura(float altura)
        {
            if (Mathf.Approximately(cc.height, altura)) return;
            if (altura > cc.height)
            {
                // só levanta se houver espaço acima
                if (Physics.SphereCast(transform.position + Vector3.up * cc.radius, cc.radius * 0.9f, Vector3.up, out _, alturaOriginal - cc.radius * 2f, camadasChao, QueryTriggerInteraction.Ignore))
                { slideTimer = 0.05f; return; }
            }
            cc.height = altura;
            cc.center = new Vector3(centroOriginal.x, altura * 0.5f, centroOriginal.z);
        }

        void AtualizaRotacao(Vector3 wish)
        {
            if (MoveMultiplier < 0.5f) return;
            if (wish.sqrMagnitude > 0.01f)
            {
                Vector3 h = new Vector3(wish.x, 0, wish.z);
                if (h.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(h), turnSpeed * Time.deltaTime);
            }
            else if (trava != null && trava.Travado)
            {
                Vector3 paraAlvo = Vector3.ProjectOnPlane(trava.Alvo.position - transform.position, Vector3.up);
                if (paraAlvo.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(paraAlvo), turnSpeed * 0.6f * Time.deltaTime);
            }
        }

        static Vector2 ReadMove()
        {
            Vector2 v = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed) v.y += 1;
                if (k.sKey.isPressed) v.y -= 1;
                if (k.dKey.isPressed) v.x += 1;
                if (k.aKey.isPressed) v.x -= 1;
            }
            if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
        }

        static bool WasJumpPressed() =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        static bool WasDashPressed() =>
            (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        static bool WasSlidePressed() =>
            (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }
}
