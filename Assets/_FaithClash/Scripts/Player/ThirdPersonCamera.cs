using UnityEngine;
using UnityEngine.InputSystem;

namespace FaithClash.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] float distance = 4.5f;
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float stickSensitivity = 140f;
        [SerializeField] Vector2 pitchLimits = new Vector2(-25f, 70f);
        [SerializeField] LayerMask obstructionMask = ~0;

        float yaw, pitch = 15f;

        public void SetTarget(Transform t) => target = t;

        void Start()
        {
            yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Vector2.zero;
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * stickSensitivity * Time.deltaTime;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 dir = rot * Vector3.back;
            float dist = distance;
            if (Physics.SphereCast(pivot, 0.25f, dir, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                dist = Mathf.Max(0.5f, hit.distance);

            transform.SetPositionAndRotation(pivot + dir * dist, rot);
        }
    }
}
