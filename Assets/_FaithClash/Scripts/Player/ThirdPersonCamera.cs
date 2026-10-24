using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;

namespace FaithClash.Player
{
    /// <summary>
    /// Câmera de terceira pessoa no estilo Naraka: Bladepoint / Elden Ring: órbita livre com o mouse, segue o jogador
    /// com suavização, não atravessa paredes, abre o campo de visão ao correr / dar dash e treme levemente nos golpes.
    /// Com alvo travado (Q) mantém o inimigo no centro da tela e o jogador um pouco abaixo.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Header("Enquadramento")]
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] float shoulderOffset = 0.35f;
        [SerializeField] float distance = 4.3f;
        [SerializeField] float distanceSprint = 5.0f;
        [SerializeField] float seguirSuavizacao = 0.06f;
        [Header("Controle")]
        [SerializeField] float mouseSensitivity = 0.11f;
        [SerializeField] float stickSensitivity = 140f;
        [SerializeField] Vector2 pitchLimits = new Vector2(-35f, 62f);
        [SerializeField] LayerMask obstructionMask = ~0;
        [Header("Campo de visão")]
        [SerializeField] float fovBase = 62f;
        [SerializeField] float fovSprint = 72f;
        [SerializeField] float fovDash = 80f;
        [Header("Foco no alvo")]
        [SerializeField] float lockSmoothing = 7f;
        [SerializeField] float lockPitch = 14f;

        float yaw, pitch = 14f;
        TravaAlvo trava;
        PlayerController jogador;
        Camera cam;
        Vector3 pivotSuave;
        Vector3 velPivot;
        float distAtual, fovAtual;
        float tremor;
        Vector3 deslocTremor;

        public void SetTarget(Transform t) => target = t;

        void Awake() => cam = GetComponent<Camera>();

        void Start()
        {
            yaw = transform.eulerAngles.y;
            distAtual = distance;
            fovAtual = fovBase;
            MostrarCursor(false);
            if (target != null) pivotSuave = target.position + pivotOffset;
        }

        void OnEnable() { TremorGlobal.Evento += AoTremer; }
        void OnDisable() { TremorGlobal.Evento -= AoTremer; }
        void AoTremer(float forca) { tremor = Mathf.Max(tremor, forca); }

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
            if (jogador == null) jogador = target.GetComponent<PlayerController>();

            // ' (aspas) mostra/esconde o mouse; clicar no jogo esconde de novo
            var k = Keyboard.current;
            if (k != null && (k.quoteKey.wasPressedThisFrame || k.backquoteKey.wasPressedThisFrame))
                MostrarCursor(Cursor.lockState == CursorLockMode.Locked);
            else if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                MostrarCursor(false);

            float dt = Time.deltaTime;
            if (trava != null && trava.Travado)
            {
                // Elden Ring: o alvo fica no centro, o jogador um pouco abaixo; ângulo vertical contido
                Vector3 v = trava.PontoFoco - (target.position + pivotOffset);
                float yawAlvo = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                float horiz = Mathf.Max(2.5f, new Vector2(v.x, v.z).magnitude);
                float pitchAlvo = Mathf.Clamp(lockPitch - Mathf.Atan2(v.y, horiz) * Mathf.Rad2Deg, 6f, 24f);
                float suave = 1f - Mathf.Exp(-lockSmoothing * dt);
                yaw = Mathf.LerpAngle(yaw, yawAlvo, suave);
                pitch = Mathf.Lerp(pitch, pitchAlvo, suave);
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Vector2.zero;
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * stickSensitivity * dt;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
            }

            // pivô suave (segue o jogador sem tranco, principalmente no pulo e no dash)
            Vector3 pivotAlvo = target.position + pivotOffset;
            pivotSuave.x = Mathf.SmoothDamp(pivotSuave.x, pivotAlvo.x, ref velPivot.x, seguirSuavizacao);
            pivotSuave.z = Mathf.SmoothDamp(pivotSuave.z, pivotAlvo.z, ref velPivot.z, seguirSuavizacao);
            pivotSuave.y = Mathf.SmoothDamp(pivotSuave.y, pivotAlvo.y, ref velPivot.y, seguirSuavizacao * 2.2f);

            // campo de visão e distância dinâmicos
            float fovAlvo = fovBase, distAlvo = distance;
            if (jogador != null)
            {
                if (jogador.IsDashing) { fovAlvo = fovDash; distAlvo = distanceSprint + 0.4f; }
                else if (jogador.IsSprinting) { fovAlvo = fovSprint; distAlvo = distanceSprint; }
            }
            fovAtual = Mathf.Lerp(fovAtual, fovAlvo, 1f - Mathf.Exp(-6f * dt));
            distAtual = Mathf.Lerp(distAtual, distAlvo, 1f - Mathf.Exp(-4f * dt));
            if (cam != null) cam.fieldOfView = fovAtual;

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = pivotSuave + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * shoulderOffset;
            Vector3 dir = rot * Vector3.back;
            float dist = distAtual;
            if (Physics.SphereCast(pivot, 0.28f, dir, out RaycastHit hit, distAtual, obstructionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                dist = Mathf.Max(0.6f, hit.distance - 0.05f);

            // tremor de impacto
            if (tremor > 0.001f)
            {
                deslocTremor = Random.insideUnitSphere * tremor * 0.12f;
                tremor = Mathf.MoveTowards(tremor, 0f, dt * 2.5f);
            }
            else deslocTremor = Vector3.zero;

            transform.SetPositionAndRotation(pivot + dir * dist + rot * deslocTremor, rot);
        }
    }

    /// <summary>Canal estático para qualquer sistema pedir tremor de câmera (golpes, explosões).</summary>
    public static class TremorGlobal
    {
        public static event System.Action<float> Evento;
        public static void Disparar(float forca) => Evento?.Invoke(forca);
    }
}
