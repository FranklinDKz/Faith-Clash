using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;

namespace FaithClash.Player
{
    /// <summary>
    /// Movimento do jogador: correr, sprint (Shift), pulo, corrida na parede (pulo contra a parede em sprint),
    /// dash para frente (C, estilo Naraka: Bladepoint) e deslize/esquiva (Ctrl).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] float runSpeed = 5.5f;
        [SerializeField] float sprintSpeed = 9f;
        [SerializeField] float turnSpeed = 14f;
        [SerializeField] float jumpHeight = 1.3f;
        [SerializeField] float gravity = -25f;

        [Header("Dash (C)")]
        [SerializeField] float dashSpeed = 19f;
        [SerializeField] float dashTime = 0.3f;
        [SerializeField] float dashCooldown = 0.9f;

        [Header("Deslize / esquiva (Ctrl)")]
        [SerializeField] float slideSpeed = 11f;
        [SerializeField] float slideTime = 0.5f;
        [SerializeField] float slideCooldown = 0.7f;

        [Header("Corrida na parede")]
        [SerializeField] float wallCheckDistance = 1.1f;
        [SerializeField] float wallRunTime = 0.9f;

        [Header("Animação")]
        [SerializeField] AnimadorProtagonista animador;

        CharacterController cc;
        TravaAlvo trava;
        Transform cam;
        float verticalVelocity;
        float dashTimer, dashReady, slideTimer, slideReady, wallTimer;
        Vector3 burstDir;
        bool noChaoAntes = true;
        float tempoNoAr, ultimoTempoNoAr;

        public bool IsDashing => dashTimer > 0f;
        public bool IsDodging => slideTimer > 0f;
        public bool IsBusy => IsDodging || IsDashing || wallTimer > 0f;
        public bool IsGrounded => cc.isGrounded;
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity { get; private set; }
        /// <summary>Definido pelo combate para reduzir a velocidade ao atacar ou defender.</summary>
        public float MoveMultiplier { get; set; } = 1f;

        public event System.Action Dashed;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            trava = GetComponent<TravaAlvo>();
            cam = Camera.main != null ? Camera.main.transform : null;
        }

        void Update()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            bool cursorPreso = Cursor.lockState == CursorLockMode.Locked;

            Vector2 input = cursorPreso ? ReadMove() : Vector2.zero;
            bool sprint = cursorPreso && ((Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
                       || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed));

            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            Vector3 wish = forward * input.y + right * input.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            // ---------- dash (C): arrancada para frente
            if (cursorPreso && WasDashPressed() && Time.time >= dashReady && !IsBusy)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? wish.normalized : forward;
                dashTimer = dashTime;
                dashReady = Time.time + dashCooldown;
                transform.rotation = Quaternion.LookRotation(burstDir);
                animador?.Acao("Dash", dashTime, 0.04f);
                Dashed?.Invoke();
            }

            // ---------- deslize (Ctrl)
            if (cursorPreso && WasSlidePressed() && Time.time >= slideReady && !IsBusy && cc.isGrounded)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                slideTimer = slideTime;
                slideReady = Time.time + slideCooldown;
                transform.rotation = Quaternion.LookRotation(burstDir);
                animador?.Acao("Deslizar", slideTime, 0.05f);
            }

            Vector3 horizontal;
            if (IsDashing)
            {
                dashTimer -= Time.deltaTime;
                horizontal = burstDir * dashSpeed;
            }
            else if (IsDodging)
            {
                slideTimer -= Time.deltaTime;
                horizontal = burstDir * (slideSpeed * Mathf.Lerp(0.4f, 1f, slideTimer / slideTime));
            }
            else
            {
                IsSprinting = sprint && wish.sqrMagnitude > 0.05f;
                float vel = (IsSprinting ? sprintSpeed : runSpeed) * MoveMultiplier;
                horizontal = wish * vel;
                AtualizaRotacao(wish);
                if (animador != null) animador.Sprint = IsSprinting;
            }

            // ---------- pulo e corrida na parede
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            bool pulo = cursorPreso && WasJumpPressed();
            if (pulo && !IsBusy)
            {
                if (IsSprinting && ParedeAFrente())
                {
                    wallTimer = wallRunTime;
                    animador?.Acao("CorridaParede", wallRunTime, 0.08f);
                }
                else if (cc.isGrounded)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    animador?.Pulou();
                }
            }

            if (wallTimer > 0f)
            {
                wallTimer -= Time.deltaTime;
                verticalVelocity = Mathf.Lerp(5f, 0.5f, 1f - wallTimer / wallRunTime);
                horizontal = transform.forward * 1.2f;
            }
            else if (!IsDashing)
                verticalVelocity += gravity * Time.deltaTime;
            else
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);

            Velocity = horizontal + Vector3.up * verticalVelocity;
            cc.Move(Velocity * Time.deltaTime);

            bool noChao = cc.isGrounded;
            tempoNoAr = noChao ? 0f : tempoNoAr + Time.deltaTime;
            if (noChao && !noChaoAntes && animador != null && tempoNoAr == 0f && ultimoTempoNoAr > 0.35f) animador.Aterrissou();
            if (!noChao) ultimoTempoNoAr = tempoNoAr;
            noChaoAntes = noChao;
        }

        void AtualizaRotacao(Vector3 wish)
        {
            if (MoveMultiplier < 0.5f) return;
            if (wish.sqrMagnitude > 0.01f)
            {
                // com alvo travado ele também olha para a direção do movimento; só encara o alvo parado/atacando
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish), turnSpeed * Time.deltaTime);
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
            return Physics.Raycast(origem, transform.forward, out RaycastHit hit, wallCheckDistance, ~0, QueryTriggerInteraction.Ignore)
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
