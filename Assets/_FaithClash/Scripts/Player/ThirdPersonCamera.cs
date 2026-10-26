using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;
using FaithClash.Core;

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
        /// <summary>Gira a câmera para olhar na direção (graus) — usado ao teletransportar o jogador e nos testes.</summary>
        public void DefinirYaw(float graus) { yaw = graus; }
        TravaAlvo trava;
        PlayerController jogador;
        Camera cam;
        Vector3 pivotSuave;
        Vector3 velPivot;
        float distAtual, fovAtual;
        float tremor;
        Vector3 deslocTremor;
        float rolagem, shiftAtual, balanco, vyAnterior, zoom = 1f, zoomAlvo = 1f, deslizeK;
        static readonly RaycastHit[] acertos = new RaycastHit[10];
        bool noChaoAntes = true;

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
        void AoTremer(float forca) { tremor = Mathf.Max(tremor, forca * PerfilJogo.D.tremorCamera); }

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
            bool menu = EstadoJogo.MenuAberto;
            var k = Keyboard.current;
            if (!menu)
            {
                if (k != null && (k.quoteKey.wasPressedThisFrame || k.backquoteKey.wasPressedThisFrame))
                    MostrarCursor(Cursor.lockState == CursorLockMode.Locked);
                else if (Cursor.lockState != CursorLockMode.Locked && ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))))
                    MostrarCursor(false);
            }

            float dt = menu ? Time.unscaledDeltaTime : Time.deltaTime;
            if (menu)
            {
                // vista do menu: gira devagar ao redor do personagem, mais perto e na altura do peito
                if (EstadoJogo.VistaPersonagem) yaw += 16f * dt;
                pitch = Mathf.Lerp(pitch, 9f, 1f - Mathf.Exp(-4f * dt));
            }
            else if (trava != null && trava.Travado)
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
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity * PerfilJogo.D.sensibilidade;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * stickSensitivity * dt;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);
            }

            // pivô suave (segue o jogador sem tranco, principalmente no pulo e no dash)
            Vector3 pivotAlvo = target.position + pivotOffset;
            if (menu) pivotAlvo.y -= 0.25f;
            deslizeK = Mathf.MoveTowards(deslizeK, EstadoJogo.CameraDeslize && !menu ? 1f : 0f, dt * 4f);
            pivotAlvo.y -= 0.7f * deslizeK;
            pivotSuave.x = Mathf.SmoothDamp(pivotSuave.x, pivotAlvo.x, ref velPivot.x, seguirSuavizacao, Mathf.Infinity, dt);
            pivotSuave.z = Mathf.SmoothDamp(pivotSuave.z, pivotAlvo.z, ref velPivot.z, seguirSuavizacao, Mathf.Infinity, dt);
            pivotSuave.y = Mathf.SmoothDamp(pivotSuave.y, pivotAlvo.y, ref velPivot.y, seguirSuavizacao * 2.2f, Mathf.Infinity, dt);

            // campo de visão (configurável) e distância dinâmicos: abre ao correr e no dash, proporcional ao efeito de velocidade
            float ef = PerfilJogo.D.efeitoVelocidade;
            float fb = PerfilJogo.D.fov;
            float fovAlvo = fb, distAlvo = menu ? 2.7f : distance;
            if (jogador != null && !menu)
            {
                if (jogador.IsDashing) { fovAlvo = fb + (fovDash - fovBase) * ef; distAlvo = distanceSprint + 0.4f; }
                else if (jogador.IsSprinting) { fovAlvo = fb + (fovSprint - fovBase) * ef; distAlvo = distanceSprint; }
            }
            // inclinação lateral ao correr de lado, balanço dos passos e tremor de pouso
            float rolAlvo = 0f, balAlvo = 0f;
            if (jogador != null && !menu)
            {
                Vector3 vl = Quaternion.Inverse(Quaternion.Euler(0f, yaw, 0f)) * Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up);
                rolAlvo = Mathf.Clamp(-vl.x * 0.45f, -3f, 3f) * ef;
                if (jogador.IsGrounded) balAlvo = Mathf.Clamp01(vl.magnitude / 6f);
                bool chao = jogador.IsGrounded;
                if (chao && !noChaoAntes && vyAnterior < -5f)
                {
                    float f = Mathf.Clamp01((-vyAnterior - 5f) / 9f);
                    TremorGlobal.Disparar(0.25f + f * 0.9f);
                    SomJogo.Tocar("Pouso", jogador.transform.position, 0.45f + f * 0.55f, 1f - f * 0.15f);
                }
                noChaoAntes = chao; vyAnterior = jogador.Velocity.y;
            }
            rolagem = Mathf.Lerp(rolagem, rolAlvo, 1f - Mathf.Exp(-6f * dt));
            balanco = Mathf.Lerp(balanco, balAlvo, 1f - Mathf.Exp(-5f * dt));
            // scroll do mouse: aproxima a câmera (ou volta ao normal); não precisa afastar além do padrão
            if (!menu && Mouse.current != null)
            {
                float sc = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(sc) > 0.01f) zoomAlvo = Mathf.Clamp(zoomAlvo - Mathf.Sign(sc) * 0.16f, 0.32f, 1f);
            }
            zoom = Mathf.Lerp(zoom, zoomAlvo, 1f - Mathf.Exp(-9f * dt));
            distAlvo *= zoom;
            // rasteira por baixo: câmera perto e baixa
            distAlvo = Mathf.Lerp(distAlvo, 2.1f, deslizeK);
            if (deslizeK > 0.01f) pitch = Mathf.Lerp(pitch, 2f, deslizeK * 5f * dt);
            fovAtual = Mathf.Lerp(fovAtual, fovAlvo, 1f - Mathf.Exp(-6f * dt));
            distAtual = Mathf.Lerp(distAtual, distAlvo, 1f - Mathf.Exp(-4f * dt));
            if (cam != null) cam.fieldOfView = fovAtual;

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = pivotSuave + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * shoulderOffset;
            Vector3 dir = rot * Vector3.back;
            float dist = distAtual;
            // o ponto de apoio não pode estar atrás de uma parede em relação à cabeça do personagem
            Vector3 cabecaP = target.position + Vector3.up * 1.5f;
            if (Physics.Linecast(cabecaP, pivot, out RaycastHit hp, obstructionMask, QueryTriggerInteraction.Ignore) && !hp.transform.IsChildOf(target))
                pivot = hp.point + hp.normal * 0.18f;
            // a câmera pára na primeira superfície que não é o próprio personagem (nunca atravessa parede para ver o outro lado)
            int nh = Physics.SphereCastNonAlloc(pivot, 0.3f, dir, acertos, distAtual + 0.1f, obstructionMask, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            for (int ih = 0; ih < nh; ih++)
            {
                if (acertos[ih].transform.IsChildOf(target) || acertos[ih].distance <= 0f) continue;
                if (acertos[ih].distance < menor) menor = acertos[ih].distance;
            }
            if (menor < float.MaxValue) dist = Mathf.Max(0.4f, menor - 0.08f);
            if (cam != null) cam.nearClipPlane = 0.04f;

            // tremor de impacto
            if (tremor > 0.001f)
            {
                deslocTremor = Random.insideUnitSphere * tremor * 0.12f;
                tremor = Mathf.MoveTowards(tremor, 0f, dt * 2.5f);
            }
            else deslocTremor = Vector3.zero;

            // balanço dos passos (leve, só em solo correndo)
            Vector3 bob = jogador != null && balanco > 0.01f ? new Vector3(0f, Mathf.Sin(Time.time * 11f) * 0.012f * balanco * PerfilJogo.D.tremorCamera, 0f) : Vector3.zero;
            transform.SetPositionAndRotation(pivot + dir * dist + rot * (deslocTremor + bob), rot * Quaternion.Euler(0f, 0f, rolagem));

            // desloca a imagem para o lado quando o menu está aberto (o personagem aparece ao lado do painel)
            if (cam != null)
            {
                shiftAtual = Mathf.Lerp(shiftAtual, EstadoJogo.DeslocCamera, 1f - Mathf.Exp(-8f * (menu ? Time.unscaledDeltaTime : Time.deltaTime)));
                cam.ResetProjectionMatrix();
                if (Mathf.Abs(shiftAtual) > 0.002f)
                {
                    var p = cam.projectionMatrix;
                    p.m02 = shiftAtual;
                    cam.projectionMatrix = p;
                }
            }
        }
    }

    /// <summary>Canal estático para qualquer sistema pedir tremor de câmera (golpes, explosões).</summary>
    public static class TremorGlobal
    {
        public static event System.Action<float> Evento;
        public static void Disparar(float forca) => Evento?.Invoke(forca);
    }
}
