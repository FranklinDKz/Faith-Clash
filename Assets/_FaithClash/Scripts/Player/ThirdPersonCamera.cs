using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;

namespace FaithClash.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] float shoulderOffset = 0.45f;   // câmera sobre o ombro direito
        [SerializeField] float distance = 3.4f;
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float stickSensitivity = 140f;
        [SerializeField] Vector2 pitchLimits = new Vector2(-25f, 70f);
        [SerializeField] LayerMask obstructionMask = ~0;
        [Header("Foco no alvo")]
        [SerializeField] float lockSmoothing = 8f;
        [SerializeField] float lockPitch = 12f;

        float yaw, pitch = 15f;
        TravaAlvo trava;

        public void SetTarget(Transform t) => target = t;

        void Start()
        {
            yaw = transform.eulerAngles.y;
            MostrarCursor(false);
        }

        static void MostrarCursor(bool mostrar)
        {
            Cursor.lockState = mostrar ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mostrar;
        }

        void OnApplicationFocus(bool foco)
        {
            if (foco && Cursor.lockState != CursorLockMode.None) MostrarCursor(false);
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (trava == null) trava = target.GetComponent<TravaAlvo>();

            // tecla ' (aspas) mostra/esconde o mouse; clicar no jogo esconde de novo
            var k = Keyboard.current;
            if (k != null && (k.quoteKey.wasPressedThisFrame || k.backquoteKey.wasPressedThisFrame))
                MostrarCursor(Cursor.lockState == CursorLockMode.Locked);
            else if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                MostrarCursor(false);

            if (trava != null && trava.Travado)
            {
                // olha para o alvo, mantendo o jogador em primeiro plano
                Vector3 v = trava.PontoFoco - (target.position + pivotOffset);
                float yawAlvo = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                float pitchAlvo = Mathf.Clamp(lockPitch - Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg, pitchLimits.x, pitchLimits.y);
                float suave = 1f - Mathf.Exp(-lockSmoothing * Time.deltaTime);
                yaw = Mathf.LerpAngle(yaw, yawAlvo, suave);
                pitch = Mathf.Lerp(pitch, pitchAlvo, suave);
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Vector2.zero;
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * stickSensitivity * Time.deltaTime;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * shoulderOffset;
            Vector3 dir = rot * Vector3.back;
            float dist = distance;
            if (Physics.SphereCast(pivot, 0.25f, dir, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                dist = Mathf.Max(0.5f, hit.distance);

            transform.SetPositionAndRotation(pivot + dir * dist, rot);
        }
    }
}
