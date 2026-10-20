using UnityEngine;
using UnityEngine.InputSystem;

namespace FaithClash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [SerializeField] float turnSpeed = 14f;
        [SerializeField] float jumpHeight = 1.3f;
        [SerializeField] float gravity = -25f;

        [Header("Dodge")]
        [SerializeField] float dodgeSpeed = 12f;
        [SerializeField] float dodgeTime = 0.25f;
        [SerializeField] float dodgeCooldown = 0.6f;

        CharacterController cc;
        Transform cam;
        float verticalVelocity;
        float dodgeTimer, dodgeReady;
        Vector3 dodgeDir;

        public bool IsDodging => dodgeTimer > 0f;
        public bool IsGrounded => cc.isGrounded;
        public Vector3 Velocity { get; private set; }
        /// <summary>Definido pelo combate para reduzir a velocidade ao atacar ou defender.</summary>
        public float MoveMultiplier { get; set; } = 1f;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cam = Camera.main != null ? Camera.main.transform : null;
        }

        void Update()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;

            Vector2 input = ReadMove();
            bool sprint = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
                       || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);

            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            Vector3 wish = forward * input.y + right * input.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            if (WasDodgePressed() && Time.time >= dodgeReady && !IsDodging)
            {
                dodgeDir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                dodgeTimer = dodgeTime;
                dodgeReady = Time.time + dodgeCooldown;
            }

            Vector3 horizontal;
            if (IsDodging)
            {
                dodgeTimer -= Time.deltaTime;
                horizontal = dodgeDir * dodgeSpeed;
                transform.rotation = Quaternion.LookRotation(dodgeDir);
            }
            else
            {
                horizontal = wish * ((sprint ? sprintSpeed : walkSpeed) * MoveMultiplier);
                if (wish.sqrMagnitude > 0.01f && MoveMultiplier > 0.5f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish), turnSpeed * Time.deltaTime);
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (cc.isGrounded && WasJumpPressed() && !IsDodging)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            Velocity = horizontal + Vector3.up * verticalVelocity;
            cc.Move(Velocity * Time.deltaTime);
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

        static bool WasDodgePressed() =>
            (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }
}
