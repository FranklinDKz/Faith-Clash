using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;

namespace FaithClash.Player
{
    /// <summary>
    /// Movimento do jogador (controles no estilo Naraka: Bladepoint):
    /// WASD move, Shift corre (sprint), Espaço pula, C dá dash para frente, Ctrl desliza.
    /// Pular contra uma parede em sprint inicia a corrida na parede.
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
        [SerializeField] float jumpHeight = 1.4f;
        [SerializeField] float gravity = -30f;
        [SerializeField] float velocidadeTerminal = -45f;
        [SerializeField] float tempoCoiote = 0.12f;
        [SerializeField] float bufferPulo = 0.12f;
        [SerializeField] LayerMask camadasChao = ~0;

        [Header("Dash (C)")]
        [SerializeField] float dashSpeed = 19f;
        [SerializeField] float dashTime = 0.3f;
        [SerializeField] float dashCooldown = 0.9f;

        [Header("Deslize (Ctrl)")]
        [SerializeField] float slideSpeed = 11f;
        [SerializeField] float slideTime = 0.6f;
        [SerializeField] float slideCooldown = 0.7f;
        [SerializeField] float rollSpeed = 6.5f;
        [SerializeField] float rollTime = 0.8f;

        [Header("Corrida na parede")]
        [SerializeField] float wallCheckDistance = 1.1f;
        [SerializeField] float wallRunTime = 0.9f;

        [Header("Animação")]
        [SerializeField] AnimadorProtagonista animador;

        CharacterController cc;
        TravaAlvo trava;
        Transform cam;
        float verticalVelocity, tempoNoChao, ultimoNoChao, tempoNoAr, ultimoTempoNoAr, pularEm = -10f;
        float dashTimer, dashReady, slideTimer, slideReady, wallTimer;
        Vector3 burstDir, velHoriz;
        bool rolando;
        bool noChaoAntes = true;
        float alturaOriginal;
        Vector3 centroOriginal;

        public bool IsDashing => dashTimer > 0f;
        public bool IsDodging => slideTimer > 0f;
        public bool IsWallRunning => wallTimer > 0f;
        public bool IsBusy => IsDodging || IsDashing || wallTimer > 0f;
        /// <summary>Chão sólido sob o personagem (com tolerância de "coiote").</summary>
        public bool IsGrounded => cc.isGrounded || (Time.time - ultimoNoChao) < 0.05f;
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 NormalDoChao { get; private set; } = Vector3.up;
        public bool Congelado { get; set; }
        /// <summary>Definido pelo combate para reduzir a velocidade ao atacar ou defender.</summary>
        public float MoveMultiplier { get; set; } = 1f;

        public event System.Action Dashed;
        public event System.Action Pulou;
        public event System.Action<float> Aterrissou;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            trava = GetComponent<TravaAlvo>();
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

            Vector2 input = cursorPreso ? ReadMove() : Vector2.zero;
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
                animador?.Acao("Dash", 0.5f, 0.04f);
                Dashed?.Invoke();
            }

            // ---------- deslize (Ctrl)
            if (cursorPreso && WasSlidePressed() && Time.time >= slideReady && !IsBusy && noChao)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? new Vector3(wish.x, 0, wish.z).normalized : transform.forward;
                rolando = !(sprint && wish.sqrMagnitude > 0.05f);   // parado/andando: rolamento; correndo: deslize
                slideTimer = rolando ? rollTime : slideTime;
                slideReady = Time.time + slideCooldown;
                transform.rotation = Quaternion.LookRotation(burstDir);
                animador?.Acao(rolando ? "Rolamento" : "Deslizar", rolando ? 1.3f : 1.2f, 0.05f);
                DefineAltura(rolando ? 1.1f : 1.0f);
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
                float tot = rolando ? rollTime : slideTime;
                alvoHoriz = burstDir * ((rolando ? rollSpeed : slideSpeed) * Mathf.Lerp(0.4f, 1f, slideTimer / tot));
                velHoriz = alvoHoriz;
                if (slideTimer <= 0f) DefineAltura(alturaOriginal);
            }
            else
            {
                IsSprinting = sprint && wish.sqrMagnitude > 0.05f;
                float vel = (IsSprinting ? sprintSpeed : runSpeed) * MoveMultiplier;
                alvoHoriz = wish * vel;
                // aceleração suave (sem "patinar" nem parar bruscamente)
                float a = (noChao ? aceleracao : aceleracao * 0.35f) * Time.deltaTime;
                velHoriz = Vector3.MoveTowards(velHoriz, alvoHoriz, a);
                AtualizaRotacao(wish);
                if (animador != null) animador.Sprint = IsSprinting;
            }

            // ---------- pulo (com buffer e tempo de coiote) e corrida na parede
            if (cursorPreso && WasJumpPressed()) pularEm = Time.time;
            bool podePular = noChao || (Time.time - ultimoNoChao) < tempoCoiote;
            if (Time.time - pularEm < bufferPulo && !IsBusy)
            {
                if (IsSprinting && ParedeAFrente())
                {
                    wallTimer = wallRunTime;
                    pularEm = -10f;
                    animador?.Acao("CorridaParede", 0.95f, 0.08f);
                }
                else if (podePular)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    ultimoNoChao = -10f;
                    pularEm = -10f;
                    animador?.Pulou();
                    Pulou?.Invoke();
                    noChao = false;
                }
            }

            if (wallTimer > 0f)
            {
                wallTimer -= Time.deltaTime;
                verticalVelocity = Mathf.Lerp(5f, 0.5f, 1f - wallTimer / wallRunTime);
                velHoriz = transform.forward * 1.2f;
            }
            else if (IsDashing)
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);
            else if (noChao && verticalVelocity <= 0f)
                verticalVelocity = -4f;   // gruda no chão (desce rampas e degraus sem "quicar")
            else
                verticalVelocity = Mathf.Max(velocidadeTerminal, verticalVelocity + gravity * Time.deltaTime);

            Velocity = velHoriz + Vector3.up * verticalVelocity;
            var flags = cc.Move(Velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;

            bool agoraNoChao = cc.isGrounded;
            if (!agoraNoChao) ultimoTempoNoAr = tempoNoAr;
            if (agoraNoChao && !noChaoAntes)
            {
                if (ultimoTempoNoAr > 0.2f) { animador?.Pousar(ultimoTempoNoAr); Aterrissou?.Invoke(ultimoTempoNoAr); }
            }
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

        bool ParedeAFrente()
        {
            Vector3 origem = transform.position + Vector3.up * 1f;
            return Physics.Raycast(origem, transform.forward, out RaycastHit hit, wallCheckDistance, camadasChao, QueryTriggerInteraction.Ignore)
                   && Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.3f && !hit.transform.IsChildOf(transform);
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
