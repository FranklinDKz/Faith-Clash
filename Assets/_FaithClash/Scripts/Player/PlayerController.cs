using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;
using FaithClash.Core;

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
        [SerializeField] float runSpeed = 5.4f;   // = corrida do usuário (4,02 m/s) x 1,35
        [SerializeField] float sprintSpeed = 6.6f;   // mesma corrida mais acelerada (x 1,65): a arrancada continua no mesmo estilo
        [SerializeField] float velAndar = 1.96f;   // Alt: andar (casa com a animação de andar)
        [SerializeField] float turnSpeed = 22f;
        [SerializeField] float travadoNormal = 3.1f;   // velocidade com o alvo travado
        [SerializeField] float travadoRapido = 6.0f;   // segurando Shift com o alvo travado
        [SerializeField] float aceleracao = 38f;
        [SerializeField] float jumpHeight = 2.2f;   // o personagem tem poder: salta mais alto que uma pessoa comum
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
        StatusJogador status;
        ModoCombate modoCombate;
        Health vida;
        GameObject ultimoAtacante;
        float ultimoDanoEm = -10f, ultimoDano;
        Transform cam;
        float verticalVelocity, tempoNoChao, ultimoNoChao, tempoNoAr, ultimoTempoNoAr, pularEm = -10f, apiceY;
        float dashTimer, dashReady, slideTimer, slideReady, atordoadoAte, avancoAte, avancoVel;
        Vector3 avancoDir;
        Vector3 burstDir, velHoriz;
        bool deslizando, andando;
        float freioPronto;
        Vector3 posSegura;
        float velAntesDaEsquiva;
        Vector3 posAnterior;
        Parkour parkour;
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
        public float TempoNoAr => tempoNoAr;
        public float VelocidadeVertical => verticalVelocity;
        public Vector3 Velocity { get; private set; }
        public Vector3 NormalDoChao { get; private set; } = Vector3.up;

        // ---- ganchos dos testes automáticos (nada disso é usado no jogo normal)
        public static bool TesteAtivo; public static Vector2? TesteMover; public static bool TesteSprint; public static int TesteEspacoFrame = -1, TesteDashFrame = -1;
        public static void TesteApertarDash() { TesteDashFrame = Time.frameCount + 1; }
        public static void TesteApertarEspaco() { TesteEspacoFrame = Time.frameCount + 1; }
        // ---- física (parâmetros do MotionCore "Third Person RPG": inclinação máxima, deslize, velocidade em rampa, arrasto de forças externas)
        [Header("Física (MotionCore)")]
        [SerializeField] float deslizeMaximo = 8f;                // m/s em ladeiras mais íngremes que o limite
        [SerializeField] float fatorSubida = 0.85f, fatorDescida = 1.03f;
        [SerializeField] float arrastoForcaExterna = 5f;
        CharacterMovement.IMovingPlatform plataforma;
        Collider colisorChao;
        Vector3 forcaExterna;
        float travaArAte;
        /// <summary>Durante um impulso (pulo de parede) a direção apertada não corta o lançamento.</summary>
        public void TravarControleNoAr(float segundos) { travaArAte = Time.time + segundos; }
        public string DiagFisica
        {
            get
            {
                bool bateu = Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out RaycastHit hd, 1.2f, camadasChao, QueryTriggerInteraction.Ignore);
                var mp = bateu ? hd.collider.GetComponentInParent<CharacterMovement.MovingPlatform>() : null;
                var ip = bateu ? hd.collider.GetComponentInParent<CharacterMovement.IMovingPlatform>() : null;
                return $"plataforma={(plataforma != null)} cc.noChao={cc.isGrounded} raio={(bateu ? hd.collider.name : "nada")} MovingPlatform={(mp != null)} IMovingPlatform={(ip != null)} velPlat={(plataforma != null ? plataforma.GetVelocityAtPoint(transform.position) : Vector3.zero)} camadasChao={camadasChao.value}";
            }
        }

        /// <summary>Empurrão externo (explosão, golpe, vento): decai com arrasto, como as forças externas do MotionCore.</summary>
        public void AplicarForcaExterna(Vector3 impulso) { forcaExterna += impulso; }
        public bool Congelado { get; set; }
        /// <summary>O parkour (pulo de parede, vault, escalada) move o personagem por conta própria: o controle normal fica parado.</summary>
        public bool ControleExterno { get; set; }
        /// <summary>Teletransporte instantâneo (Raijin, Rinnegan).</summary>
        public void Teleportar(Vector3 pos, Quaternion rot)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            cc.enabled = true;
            velHoriz = Vector3.zero;
            verticalVelocity = -2f;
        }
        public void DefinirVelocidadeExterna(Vector3 v) { Velocity = v; }
        /// <summary>Devolve o controle ao jogador já com uma velocidade (saída de vault, pulo de parede).</summary>
        float diagExterno, diagDy, yFimAnt; CollisionFlags diagFlags;
        public static bool TesteSegurandoEspaco;
        public string DiagVertical => $"movido-fora={diagExterno:0.000} dy-move={diagDy:0.000} flags={diagFlags} " + $"vv={verticalVelocity:0.0} vh=({velHoriz.x:0.0},{velHoriz.z:0.0}) trava={(Time.time < travaArAte)} ext={ForcaExterna().y:0.0} desl={Deslizar().y:0.0} plat={VelocidadePlataforma().y:0.0} cc.v={cc.velocity.y:0.0}";

        public void Impulsar(Vector3 horizontal, float vertical)
        {
            velHoriz = new Vector3(horizontal.x, 0f, horizontal.z);
            verticalVelocity = vertical;
            pularEm = -10f;   // um Espaço apertado durante a ação não vira pulo logo depois
            if (vertical > 0.1f) ultimoNoChao = -10f;
            else
            {
                // saída de uma ação com o pé no chão: conta como "no chão" (sem animação de queda nem de pouso)
                ultimoNoChao = Time.time;
                tempoNoAr = 0f; ultimoTempoNoAr = 0f;
                apiceY = transform.position.y;
                noChaoAntes = true;
            }
            ControleExterno = false;
        }
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
            status = GetComponent<StatusJogador>();
            modoCombate = GetComponent<ModoCombate>();
            parkour = GetComponent<Parkour>();
            vida = GetComponent<Health>();
            if (vida != null) vida.Damaged += (d, p, o) => { ultimoDanoEm = Time.time; ultimoDano = d; ultimoAtacante = o; };
            cam = Camera.main != null ? Camera.main.transform : null;
            alturaOriginal = cc.height;
            centroOriginal = cc.center;
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 55f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;
        }

        // ---------------- água e poder sobre a gravidade
        float profAgua, aguaAte;
        bool puloDuploUsado;
        /// <summary>Profundidade da água em volta do corpo (m); 0 fora da água.</summary>
        public float ProfAgua => Time.time < aguaAte ? profAgua : 0f;
        public bool NadandoOuVadeando => ProfAgua > 0.15f;
        public bool Flutuando => ProfAgua > 1.2f;
        /// <summary>Chamado todo quadro pelo volume de água em que o personagem está.</summary>
        public void DefinirAgua(float profundidade) { profAgua = profundidade; aguaAte = Time.time + 0.12f; }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // empurra objetos físicos (caixas, barris, troncos) em proporção à velocidade; pesados cedem menos
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            float v = new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
            rb.AddForceAtPosition(new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized * Mathf.Min(rb.mass, 80f) * v * 0.9f, hit.point, ForceMode.Force);
        }

        readonly System.Collections.Generic.HashSet<IDamageable> acertadosNoDash = new System.Collections.Generic.HashSet<IDamageable>();

        /// <summary>O dash também machuca: quem estiver no caminho leva um dano pequeno e é empurrado.</summary>
        void DanoDoDash()
        {
            if (!IsDashing) { if (acertadosNoDash.Count > 0) acertadosNoDash.Clear(); return; }
            foreach (var col in Physics.OverlapSphere(transform.position + Vector3.up * 1f, 1.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var alvo = col.GetComponentInParent<IDamageable>();
                if (alvo == null || alvo.IsDead || !acertadosNoDash.Add(alvo)) continue;
                alvo.TakeDamage(10f * (combate != null ? combate.DamageMultiplier : 1f), col.ClosestPoint(transform.position), gameObject);
                col.GetComponentInParent<FaithClash.Training.TrainingDummy>()?.Empurrar(transform.forward * 4f);
                SomJogo.Tocar("Soco_Leve1", col.ClosestPoint(transform.position), 0.7f, 1.2f);
            }
        }

        int versaoAjustes = -1;
        /// <summary>Lê Resources/AjustesMovimento (editável no Inspector, inclusive no Play).</summary>
        void AplicarAjustes()
        {
            var A = AjustesMovimento.Atual;
            velAndar = A.velAndar; runSpeed = A.velCorrida; sprintSpeed = A.velSprint; aceleracao = A.aceleracao; turnSpeed = A.giro;
            jumpHeight = A.alturaPulo; gravity = A.gravidade; multiplicadorQueda = A.multiplicadorQueda; multiplicadorPuloCurto = A.multiplicadorPuloCurto; controleNoAr = A.controleNoAr;
            if (A.versao != versaoAjustes && Mathf.Approximately(cc.height, alturaOriginal) && !deslizando)
            {
                versaoAjustes = A.versao;
                cc.radius = A.raioCapsula; cc.stepOffset = Mathf.Min(A.degrau, A.alturaCapsula * 0.5f); cc.slopeLimit = A.rampaMaxima;
                cc.height = A.alturaCapsula; cc.center = new Vector3(0f, A.alturaCapsula * 0.5f, 0f); alturaOriginal = A.alturaCapsula;
            }
        }

        void Update()
        {
            AplicarAjustes();
            DanoDoDash();
            if (ControleExterno) return;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            bool cursorPreso = (Cursor.lockState == CursorLockMode.Locked || TesteAtivo) && !Congelado;
            bool noChao = cc.isGrounded;
            if (noChao) { ultimoNoChao = Time.time; tempoNoAr = 0f; } else tempoNoAr += Time.deltaTime;
            // chão sumiu (caiu para fora do mapa): volta ao último ponto seguro em vez de cair para sempre
            if (posSegura == Vector3.zero) posSegura = transform.position;
            if (noChao) posSegura = transform.position;
            else if (transform.position.y < posSegura.y - 40f) { Teleportar(posSegura + Vector3.up * 0.3f, transform.rotation); return; }

            Vector2 input = cursorPreso && !Atordoado ? ReadMove() : Vector2.zero;
            bool sprint = cursorPreso && (TesteSprint || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
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
                Efeitos.Rapidez(transform.position + Vector3.up * 1f, burstDir);
                animador?.Acao("Dash", 0.5f, 0.04f, 1.7f);
                animador?.DashDinamico(dashTime);
                Dashed?.Invoke();
            }

            // ---------- rolamento (Ctrl): esquiva no chão, cancela o golpe
            if (cursorPreso && WasSlidePressed() && Time.time >= slideReady && !IsBusy && noChao && !Atordoado)
            {
                burstDir = wish.sqrMagnitude > 0.01f ? new Vector3(wish.x, 0, wish.z).normalized : transform.forward;
                velAntesDaEsquiva = new Vector3(velHoriz.x, 0f, velHoriz.z).magnitude;
                slideReady = Time.time + slideCooldown;
                combate?.Cancelar();
                if (Kawarimi()) goto fimEsquiva;
                // Ctrl = rasteira (slide por baixo) na direção do movimento, parado ou correndo
                {
                    float d = AjustesMovimento.Atual.duracaoRasteira;
                    slideTimer = d;
                    deslizando = true;
                    transform.rotation = Quaternion.LookRotation(burstDir);
                    float lenR = animador != null ? animador.DuracaoClipePorEstado("Rasteira", 1.53f) : 1.53f;
                    animador?.Acao("U_RolarCorrer", d + 0.4f, 0.05f, lenR * 0.62f / d);   // a rasteira do usuário
                    if (animador != null && animador.Movimento != null) animador.Movimento.Ativo = false;   // o código move o corpo (rasteira desacelerando)
                    velAntesDaEsquiva = Mathf.Max(velAntesDaEsquiva, AjustesMovimento.Atual.velRasteira);
                    velHoriz = burstDir * velAntesDaEsquiva;
                    DefineAltura(AjustesMovimento.Atual.alturaCapsulaRasteira);
                }
                fimEsquiva:;
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
                alvoHoriz = deslizando ? burstDir * (Mathf.Max(AjustesMovimento.Atual.velRasteira, velAntesDaEsquiva) * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(slideTimer / Mathf.Max(0.1f, AjustesMovimento.Atual.duracaoRasteira)))) : burstDir * (Mathf.Max(rollSpeed, velAntesDaEsquiva) * Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(slideTimer / rollTime)));   // deslize: quem move é o root motion da animação
                if (slideTimer <= 0f && deslizando) { deslizando = false; freioPronto = Time.time + 0.7f; velHoriz = transform.forward * Mathf.Max(runSpeed, velAntesDaEsquiva); if (animador != null && animador.Movimento != null) animador.Movimento.Eixo = Vector3.zero; }
                velHoriz = alvoHoriz;
                if (slideTimer <= 0f) { DefineAltura(alturaOriginal); if (velAntesDaEsquiva > 3f) velHoriz = burstDir * velAntesDaEsquiva; }
            }
            else
            {
                andando = cursorPreso && Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;
                IsSprinting = sprint && !andando && wish.sqrMagnitude > 0.05f;
                bool travado = trava != null && trava.Travado;
                // alvo travado (estilo Elden Ring): anda/corre olhando para o inimigo, com velocidades que casam com os ciclos de andar de lado e de costas
                float vel = (travado ? (IsSprinting ? travadoRapido : travadoNormal) : (IsSprinting ? sprintSpeed : runSpeed)) * MoveMultiplier * (1f - 0.55f * Mathf.Clamp01(ProfAgua / 1.3f)) * (modoCombate != null && modoCombate.Atual == Modo.Espada ? (modoCombate.DuasMaos ? 0.58f : 0.8f) : 1f);
                if (andando && !travado) vel = Mathf.Min(vel, velAndar);
                if (vida != null && !vida.IsDead && vida.Current / vida.Max < 0.2f && !travado) vel = Mathf.Min(vel, velAndar);   // muito ferido: só anda mancando
                alvoHoriz = wish * vel * FatorDeladeira(wish);

                // sprint e o jogador inverte o movimento: animação de virar 180° (o corpo já vira para o novo lado)
                if (noChao && IsSprinting && !travado && !IsBusy && Time.time >= freioPronto && wish.sqrMagnitude > 0.25f && animador != null && !animador.OcupadoComAcao)
                {
                    Vector3 vh = new Vector3(velHoriz.x, 0f, velHoriz.z);
                    if (vh.magnitude > 4.8f && Vector3.Angle(vh, wish) > 140f)
                    {
                        float lenV = animador.DuracaoClipePorEstado("U_MudarDirecao", 2.17f);
                        float velV = lenV / 0.9f;
                        animador.Acao("U_MudarDirecao", 0.95f, 0.08f, velV);
                        if (animador.Movimento != null) animador.Movimento.Ativo = false;
                        freioPronto = Time.time + 1.4f;
                    }
                }
                // aceleração suave (sem "patinar" nem parar bruscamente)
                float a = (noChao ? aceleracao : aceleracao * controleNoAr) * Time.deltaTime;
                if (Time.time < travaArAte && !noChao) alvoHoriz = velHoriz;
                velHoriz = Vector3.MoveTowards(velHoriz, alvoHoriz, a);
                AtualizaRotacao(wish);
                if (animador != null) animador.Sprint = IsSprinting;
            }

            // ---------- pulo (com buffer e tempo de coiote); segurar o Espaço = mais alto
            bool apertouPulo = cursorPreso && !Atordoado && WasJumpPressed() && (parkour == null || !parkour.ConsumiuEspaco);
            if (apertouPulo) pularEm = Time.time;
            bool podePular = noChao || (Time.time - ultimoNoChao) < tempoCoiote;
            if (noChao) puloDuploUsado = false;
            // salto de poder: um segundo impulso no ar (sem sair da física: só um pouco além da gravidade)
            if (apertouPulo && !podePular && !puloDuploUsado && !IsBusy && !ControleExterno && tempoNoAr > 0.18f && !Flutuando)
            {
                puloDuploUsado = true;
                verticalVelocity = Mathf.Sqrt(jumpHeight * 0.7f * -2f * gravity);
                SomJogo.Tocar("Pulo", transform.position, 0.6f, 1.3f, 0.05f);
                CatalogoVFX.Tocar("poeira_nuvem", transform.position + Vector3.up * 0.1f, Quaternion.identity, null, 0.6f, 1.5f);
                CatalogoVFX.Tocar("ki_circulo", transform.position + Vector3.up * 0.1f, Quaternion.identity, null, 0.45f, 1f);
                pularEm = -10f;
            }
            if (Time.time - pularEm < bufferPulo && !IsBusy && podePular)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * (ProfAgua > 0.5f ? 0.55f : 1f) * -2f * gravity);
                SomJogo.Tocar("Pulo", transform.position, 0.55f, 1f, 0.08f);
                ultimoNoChao = -10f;
                pularEm = -10f;
                combate?.Cancelar();
                animador?.Pulou();
                Pulou?.Invoke();
                noChao = false;
            }

            bool segurandoPulo = (TesteAtivo && TesteSegurandoEspaco) || cursorPreso && ((Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                                 || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed));
            if (IsDashing)
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);
            else if (noChao && verticalVelocity <= 0f)
                verticalVelocity = -4f;   // gruda no chão (desce rampas e degraus sem "quicar")
            else
            {
                float g = gravity;
                if (verticalVelocity < 0f) g *= multiplicadorQueda;
                else if (!segurandoPulo && Time.time >= travaArAte) g *= multiplicadorPuloCurto;   // no pulo de parede o salto não é cortado
                verticalVelocity = Mathf.Max(velocidadeTerminal, verticalVelocity + g * Time.deltaTime);
            }
            // na água funda o corpo flutua (empuxo) e o Espaço nada para cima
            if (ProfAgua > 1.1f && !IsDashing)
            {
                float alvoV = Mathf.Clamp((1.25f - ProfAgua) * 2.6f, -2f, 2f) + (segurandoPulo ? 3.2f : 0f);
                verticalVelocity = Mathf.Lerp(verticalVelocity, alvoV, 5f * Time.deltaTime);
                noChao = false;
            }
            else if (ProfAgua > 0.2f && verticalVelocity < -3f) verticalVelocity = Mathf.Lerp(verticalVelocity, -3f, 8f * Time.deltaTime);   // a água amortece a queda
            Vector3 extra = Time.time < avancoAte && !IsBusy ? avancoDir * avancoVel : Vector3.zero;
            extra += VelocidadePlataforma() + Deslizar() + ForcaExterna();
            Velocity = velHoriz + extra + Vector3.up * verticalVelocity;
            diagExterno = transform.position.y - yFimAnt; float yAntesMove = transform.position.y;
            if (!cc.enabled) cc.enabled = true;
            var flags = cc.Move(Velocity * Time.deltaTime);
            diagDy = transform.position.y - yAntesMove; diagFlags = flags; yFimAnt = transform.position.y;
            // durante viradas/arrancadas/deslizes quem move é a animação: a velocidade informada é a real (parkour, câmera e animação usam)
            if (deslizando && Time.deltaTime > 0f)
            {
                Vector3 real = (transform.position - posAnterior) / Time.deltaTime;
                Velocity = new Vector3(real.x, Velocity.y, real.z);
            }
            posAnterior = transform.position;
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

        /// <summary>
        /// Kawarimi no Jutsu (Substituição): apertando Ctrl logo depois de levar um golpe, o jogador deixa um tronco no lugar, recupera o dano
        /// e reaparece atrás do atacante. Custa Fé.
        /// </summary>
        bool Kawarimi()
        {
            if (Time.time - ultimoDanoEm > 0.5f || ultimoAtacante == null || status == null || vida == null || vida.IsDead) return false;
            if (!status.GastarFe(80f)) return false;
            Vector3 origem = transform.position;
            var tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(tronco.GetComponent<Collider>());
            tronco.name = "Tronco (Kawarimi)";
            tronco.transform.position = origem + Vector3.up * 0.9f;
            tronco.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            tronco.GetComponent<Renderer>().material.color = new Color(0.35f, 0.22f, 0.12f);
            Destroy(tronco, 1.1f);
            CatalogoVFX.Tocar("teleporte", origem + Vector3.up, Quaternion.identity, null, 1f, 2f);
            Vector3 alvo = ultimoAtacante.transform.position - ultimoAtacante.transform.forward * 1.7f;
            alvo.y = origem.y;
            cc.enabled = false;
            transform.position = alvo;
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(ultimoAtacante.transform.position - alvo, Vector3.up));
            cc.enabled = true;
            vida.Heal(ultimoDano);
            CatalogoVFX.Tocar("teleporte", alvo + Vector3.up, Quaternion.identity, null, 1f, 2f);
            SomJogo.Tocar("Movimento_Vento", alvo, 0.8f, 1.1f, duracaoMax: 1f);
            status.Avisar("Kawarimi no Jutsu!");
            animador?.Acao("Dash", 0.35f, 0.04f, 2f);
            slideTimer = 0f;
            ultimoDanoEm = -10f;
            TremorGlobal.Disparar(0.5f);
            return true;
        }

        void AtualizaNormalDoChao()
        {
            Vector3 origem0 = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origem0, Vector3.down, out RaycastHit h0, 1.2f, camadasChao, QueryTriggerInteraction.Ignore) && !h0.transform.IsChildOf(transform))
            {
                colisorChao = h0.collider;
                plataforma = h0.collider.GetComponentInParent<CharacterMovement.IMovingPlatform>();
            }
            else { colisorChao = null; plataforma = null; }
            Vector3 origem = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origem, Vector3.down, out RaycastHit h, 1.2f, camadasChao, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(transform))
                NormalDoChao = Vector3.Lerp(NormalDoChao, h.normal, 0.5f);
            else NormalDoChao = Vector3.up;
        }

        /// <summary>Plataforma móvel (MotionCore): quem está em cima anda junto, inclusive quando ela gira.</summary>
        Vector3 VelocidadePlataforma()
        {
            if (plataforma == null || !cc.isGrounded) return Vector3.zero;
            Vector3 v = plataforma.GetVelocityAtPoint(transform.position);
            float giro = plataforma.AngularVelocity.y * Mathf.Rad2Deg * Time.deltaTime;
            if (Mathf.Abs(giro) > 0.001f) transform.Rotate(0f, giro, 0f, Space.World);
            return new Vector3(v.x, Mathf.Min(0f, v.y), v.z);
        }

        /// <summary>Ladeira mais íngreme que o limite: o personagem escorrega morro abaixo.</summary>
        Vector3 Deslizar()
        {
            if (!cc.isGrounded || IsDashing) return Vector3.zero;
            float ang = Vector3.Angle(NormalDoChao, Vector3.up);
            if (ang <= cc.slopeLimit + 1f) return Vector3.zero;
            Vector3 descida = Vector3.ProjectOnPlane(Vector3.down, NormalDoChao).normalized;
            return descida * Mathf.Min(deslizeMaximo, 2f + (ang - cc.slopeLimit) * 0.3f);
        }

        Vector3 ForcaExterna()
        {
            if (forcaExterna.sqrMagnitude < 0.0004f) { forcaExterna = Vector3.zero; return Vector3.zero; }
            Vector3 v = forcaExterna;
            forcaExterna = Vector3.Lerp(forcaExterna, Vector3.zero, 1f - Mathf.Exp(-arrastoForcaExterna * Time.deltaTime));
            return v;
        }

        /// <summary>Subir ladeira cansa (x0,85), descer ajuda (x1,03), como no MotionCore.</summary>
        float FatorDeladeira(Vector3 desejo)
        {
            if (!cc.isGrounded || desejo.sqrMagnitude < 0.01f) return 1f;
            float ang = Vector3.Angle(NormalDoChao, Vector3.up);
            if (ang < 3f) return 1f;
            Vector3 descida = Vector3.ProjectOnPlane(Vector3.down, NormalDoChao).normalized;
            float d = Vector3.Dot(desejo.normalized, descida) * Mathf.Clamp01(ang / 20f);
            return d > 0f ? Mathf.Lerp(1f, fatorDescida, d) : Mathf.Lerp(1f, fatorSubida, -d);
        }

        /// <summary>Encolhe/restaura a cápsula (deslize por baixo de obstáculos). Valor negativo = altura original.</summary>
        /// <summary>Depois de rasteira/parkour: sem animação de freada/arrancada por um tempo (sai correndo).</summary>
        public void AdiarTransicoes(float segundos) { freioPronto = Mathf.Max(freioPronto, Time.time + segundos); }


        public void DefinirAltura(float altura)
        {
            if (altura < 0f) altura = alturaOriginal;
            cc.height = altura;
            cc.center = new Vector3(centroOriginal.x, altura * 0.5f, centroOriginal.z);
        }

        /// <summary>Há espaço para ficar de pé aqui?</summary>
        public bool PodeLevantar() =>
            !Physics.SphereCast(transform.position + Vector3.up * (cc.radius + 0.05f), cc.radius * 0.9f, Vector3.up, out _, alturaOriginal - cc.radius * 2f, camadasChao, QueryTriggerInteraction.Ignore);

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
            if (trava != null && trava.Travado)
            {
                Vector3 alvo = Vector3.ProjectOnPlane(trava.Alvo.position - transform.position, Vector3.up);
                if (alvo.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(alvo), turnSpeed * Time.deltaTime);
                return;
            }
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
            if (TesteMover.HasValue) return TesteMover.Value;
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
            TesteEspacoFrame == Time.frameCount
            || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        static bool WasDashPressed() =>
            TesteDashFrame == Time.frameCount
            || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        static bool WasSlidePressed() =>
            (Keyboard.current != null && Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }
}
