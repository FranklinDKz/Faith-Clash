using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Rasteira por baixo de barras e tetos baixos (slide): Espaço correndo de frente para algo que deixa um vão de 0,65 a 1,55 m embaixo.
    /// (O Ctrl, que faz a rasteira em qualquer lugar, fica no PlayerController.) Os outros movimentos de parkour foram removidos.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerController))]
    public class Parkour : MonoBehaviour
    {
        PlayerController jogador;
        AnimadorProtagonista animador;
        CharacterController cc;
        MeleeCombat combate;
        bool ocupado;
        float proximoEm, ultimaRapidez;

        public bool Ocupado => ocupado;
        /// <summary>Sempre nulo (o vault foi removido); o CorpoVivo ainda consulta.</summary>
        public Collider ObstaculoVault => null;
        /// <summary>Verdadeiro no quadro em que o Espaço foi usado pela rasteira (o PlayerController não pula).</summary>
        public bool ConsumiuEspaco { get; private set; }
        /// <summary>Última ação tentada (para testes): nome e motivo.</summary>
        public string UltimaDecisao { get; private set; } = "";

        void Awake()
        {
            jogador = GetComponent<PlayerController>();
            animador = GetComponent<AnimadorProtagonista>();
            cc = GetComponent<CharacterController>();
            combate = GetComponent<MeleeCombat>();
        }

        void OnDisable()
        {
            if (cc != null) cc.enabled = true;
            PrepararIK(); SoltarIK();
            if (ocupado && animador != null) animador.Encerrar(0.05f);
            ocupado = false;
        }

        void Update()
        {
            ConsumiuEspaco = false;
            if (!ocupado && !cc.enabled) cc.enabled = true;
            if (!ocupado && animador != null && animador.AcaoAtual == "Locomocao" && !animador.EmPouso) { animador.Encerrar(0.08f); SoltarIK(); }   // rede de segurança: ação de parkour que ficou presa   // rede de segurança: uma ação interrompida não deixa o controlador desligado
            Vector3 vh = Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up);
            float vel = vh.magnitude;
            if (!ocupado && jogador.IsGrounded && jogador.IsSprinting && vel > 5f && Time.time > ultimaRapidez + 0.2f)
            {
                Efeitos.Rapidez(transform.position + Vector3.up * 1f, vh.normalized, 0.45f);
                ultimaRapidez = Time.time;
            }

            if (ocupado || Time.time < proximoEm) return;
            if ((Cursor.lockState != CursorLockMode.Locked && !PlayerController.TesteAtivo) || jogador.Congelado || jogador.Atordoado || jogador.IsBusy || jogador.ControleExterno) return;
            if (combate != null && (combate.IsAttacking || combate.IsBlocking)) return;
            var k = Keyboard.current;
            bool espaco = PlayerController.TesteEspacoFrame == Time.frameCount || (k != null && k.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (!espaco || !jogador.IsGrounded) return;
            Vector3 dir = vel > 1f ? vh.normalized : transform.forward;
            if (TentarObstaculo(dir, vel)) ConsumiuEspaco = true;
            else if (PerfilJogo.D.deslize && TentarDeslizar(dir, vel)) ConsumiuEspaco = true;
        }

        static bool Valido(Collider c) => c != null && !c.isTrigger && c.GetComponentInParent<CharacterController>() == null && c.GetComponentInParent<Health>() == null;

        bool Lancar(Vector3 origem, Vector3 dir, float dist, out RaycastHit hit)
        {
            if (Physics.SphereCast(origem, 0.22f, dir, out hit, dist, ~0, QueryTriggerInteraction.Ignore) && Valido(hit.collider) && !hit.collider.transform.IsChildOf(transform))
                return true;
            hit = default;
            return false;
        }

        /// <summary>Espaço de frente para um obstáculo (parado, andando ou correndo): mureta fina = salto por cima; objeto médio (até 1,35 m) = sobe nele; muro alto (até 3,2 m) = escala.
        /// Rampa e degraus não contam (a parede precisa ser vertical e ter pelo menos 0,6 m).</summary>
        bool TentarObstaculo(Vector3 dir, float vel)
        {
            Vector3 p0 = transform.position;
            if (!Lancar(p0 + Vector3.up * 0.3f, dir, vel < 2f ? 1.2f : 1.8f, out var frente)) return false;
            if (Mathf.Abs(Vector3.Dot(frente.normal, Vector3.up)) > 0.35f) return false;
            Vector3 sobre = frente.point + dir * 0.4f;
            if (!Physics.Raycast(new Vector3(sobre.x, p0.y + 3.7f, sobre.z), Vector3.down, out var topo, 3.7f, ~0, QueryTriggerInteraction.Ignore) || !Valido(topo.collider)) return false;
            float h = topo.point.y - p0.y;
            if (h < 0.6f) return false;
            if (h > 3.2f) { UltimaDecisao = $"altura {h:0.00} m: alto demais"; return false; }
            if (Physics.CheckCapsule(topo.point + Vector3.up * 0.4f, topo.point + Vector3.up * 1.5f, 0.28f, ~0, QueryTriggerInteraction.Ignore)) { UltimaDecisao = "sem espaço em cima"; return false; }
            // profundidade do topo (quanto o objeto continua na direção do salto)
            float prof = 0.4f;
            for (float d = 0.6f; d <= 1.8f; d += 0.2f)
            {
                Vector3 q = frente.point + dir * d;
                if (Physics.Raycast(new Vector3(q.x, topo.point.y + 0.4f, q.z), Vector3.down, out var t2, 0.7f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(t2.point.y - topo.point.y) < 0.15f) prof = d; else break;
            }
            if (h <= 1.4f && prof < 1.1f)
            {
                // mureta: salta por cima e cai do outro lado
                Vector3 fimXZ = frente.point + dir * (prof + 0.9f);
                if (!Physics.Raycast(new Vector3(fimXZ.x, topo.point.y + 0.5f, fimXZ.z), Vector3.down, out var chao, h + 1.5f, ~0, QueryTriggerInteraction.Ignore)) { UltimaDecisao = "sem chão do outro lado da mureta"; return false; }
                if (Physics.CheckCapsule(chao.point + Vector3.up * 0.4f, chao.point + Vector3.up * 1.5f, 0.28f, ~0, QueryTriggerInteraction.Ignore)) { UltimaDecisao = "sem espaço do outro lado da mureta"; return false; }
                UltimaDecisao = $"saltar mureta (altura {h:0.00} m, espessura {prof:0.00} m)";
                StartCoroutine(Saltar(dir, frente.point, prof, h, chao.point + Vector3.up * 0.02f, Random.value < 0.5f ? "U_Mureta1" : "U_Mureta2", vel));
                return true;
            }
            if (prof < 0.8f) { UltimaDecisao = "topo estreito demais para subir"; return false; }
            if (h <= 1.35f)
            {
                UltimaDecisao = $"subir (altura {h:0.00} m)";
                StartCoroutine(Subir(dir, frente.point, topo.point.y, vel));
                return true;
            }
            UltimaDecisao = $"escalar muro (altura {h:0.00} m)";
            StartCoroutine(Escalar(dir, frente.point, topo.point.y));
            return true;
        }

        /// <summary>Salto por cima de mureta: o corpo atravessa a mureta no instante em que a animação está por cima dela (a altura vem do clipe; a diferença para a altura real é compensada) e sai mantendo o ritmo da corrida.</summary>
        IEnumerator Saltar(Vector3 dir, Vector3 face, float espessura, float altura, Vector3 pouso, string estado, float velEntrada)
        {
            ocupado = true;
            jogador.ControleExterno = true;
            combate?.Cancelar();
            transform.rotation = Quaternion.LookRotation(dir);
            var mov = animador != null ? animador.Movimento : null;
            var est = GetComponentInChildren<EstabilizadorMarcha>();
            bool um = estado == "U_Mureta1";
            float alturaClipe = um ? 1.1f : 0.82f;   // altura do obstáculo em que cada animação foi gravada
            float velClipe = 1.75f;
            float dur = (animador != null ? animador.DuracaoClipePorEstado(estado, 1.2f) : 1.2f) / velClipe;
            animador?.Acao(estado, dur + 0.6f, 0.06f, velClipe);
            if (mov != null) { mov.Ativo = false; mov.Eixo = Vector3.zero; }
            Vector3 ini = transform.position;
            float u0 = -Vector3.Dot(face - ini, dir);   // posição ao longo da direção, medida a partir da face da mureta
            float uFim = espessura + 0.9f;
            // chaves (tempo normalizado, posição relativa à face): pés plantados antes da face, quadril no meio da mureta, pouso depois da face de trás
            float[] kt = { 0f, 0.22f, 0.50f, 0.78f, 1f };
            float[] ku = { u0, Mathf.Min(-0.6f, u0 * 0.5f), espessura * 0.5f, espessura + 0.5f, uFim };
            float yIni = ini.y;
            cc.enabled = false;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                int i = 0; while (i < kt.Length - 2 && k > kt[i + 1]) i++;
                float u = Mathf.Lerp(ku[i], ku[i + 1], Mathf.InverseLerp(kt[i], kt[i + 1], k));
                Vector3 pos = ini + dir * (u - u0);
                pos.y = Mathf.Lerp(yIni, pouso.y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, k)));
                transform.position = pos;
                float lift = (altura - alturaClipe) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.26f, k)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.82f, k)));
                if (est != null) est.DeslocY = lift;
                jogador.DefinirVelocidadeExterna(Vector3.zero);
                yield return null;
            }
            transform.position = new Vector3(pouso.x, pouso.y, pouso.z);
            if (est != null) est.DeslocY = 0f;
            cc.enabled = true;
            if (mov != null) { mov.Ativo = false; mov.Eixo = Vector3.zero; }
            jogador.Impulsar(dir * Mathf.Max(velEntrada, AjustesMovimento.Atual.velCorrida), 0f);   // sai no mesmo ritmo da corrida
            jogador.AdiarTransicoes(0.5f);
            animador?.Encerrar(0.1f);
            ocupado = false;
            proximoEm = Time.time + 0.25f;
        }

        // ---------------------------------------------------------------- animações de subir feitas em código (IK de mãos e pés + corpo dinâmico)

        EscaladaProcedural ik;
        EstabilizadorMarcha estab;

        void PrepararIK()
        {
            if (ik == null) ik = GetComponentInChildren<EscaladaProcedural>();
            if (estab == null) estab = GetComponentInChildren<EstabilizadorMarcha>();
        }

        void SoltarIK()
        {
            if (ik != null) ik.Limpar();
            if (estab != null) { estab.InclinacaoFrente = 0f; estab.DeslocY = 0f; }
        }

        static float Suave(float a, float b, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));
        /// <summary>Chega um pouco além do destino e volta (dá "estalo" ao agarrar e ao chutar).</summary>
        static float Estalo(float k)
        {
            const float c1 = 1.9f, c3 = c1 + 1f;
            float m = k - 1f;
            return 1f + c3 * m * m * m + c1 * m * m;
        }

        /// <summary>Um apoio (mão ou pé) que fica preso num ponto da parede e, quando o corpo sobe, salta para o próximo ponto acima, com estalo.</summary>
        sealed class Apoio
        {
            public float y, de, para, andamento; public bool movendo; public float limiar, passo, tempoPasso;
            public Apoio(float y0, float limiar, float passo, float tempoPasso) { y = y0; this.limiar = limiar; this.passo = passo; this.tempoPasso = tempoPasso; }
            /// <summary>Atualiza com a altura desejada; devolve a altura atual do apoio e o quanto ele está solto da parede (0 = preso, 1 = no meio do passo).</summary>
            public float Passo(float desejado, float maximo, float dt, out float levantado)
            {
                if (!movendo && desejado - y > limiar && y + passo <= maximo) { movendo = true; de = y; para = y + passo; andamento = 0f; }
                levantado = 0f;
                if (movendo)
                {
                    andamento += dt / tempoPasso;
                    float k = Mathf.Clamp01(andamento);
                    y = Mathf.LerpUnclamped(de, para, Estalo(k));
                    levantado = Mathf.Sin(k * Mathf.PI);
                    if (k >= 1f) { movendo = false; y = para; }
                }
                return y;
            }
        }

        /// <summary>Escalada de muro alto, feita em código e rápida: salta até a parede (mãos e pés agarram com impacto), sobe trocando os apoios em diagonal
        /// (cada passo torce o tronco, inclina o quadril, encolhe o ombro e leva a cabeça para o próximo apoio), e na borda se joga por cima e sai correndo.</summary>
        IEnumerator Escalar(Vector3 dir, Vector3 face, float topoY)
        {
            ocupado = true;
            jogador.ControleExterno = true;
            combate?.Cancelar();
            PrepararIK();
            transform.rotation = Quaternion.LookRotation(dir);
            var mov = animador != null ? animador.Movimento : null;
            if (mov != null) { mov.Ativo = false; mov.Eixo = Vector3.zero; }
            animador?.Acao("Locomocao", 30f, 0.08f, 1f);   // base: pose de parado; o resto vai por IK e pela dinâmica do corpo
            Vector3 dirD = Vector3.Cross(Vector3.up, dir);   // direita
            Vector3 faceXZ = new Vector3(face.x, 0f, face.z);
            Vector3 ini = transform.position;
            Vector3 ancora = faceXZ - dir * 0.40f; ancora.y = ini.y;
            const float quadril = 0.92f, velSobe = 2.3f, ombroAcima = 0.95f, durSalto = 0.26f;
            float y0 = ini.y + 0.5f;                       // o salto agarra a parede um pouco acima do chão
            float baseFim = topoY - 0.85f;                 // pés do personagem quando o peito chega à borda
            float dist = Mathf.Max(0.05f, baseFim - y0), durSobe = dist / velSobe;
            float yAlto = topoY - 0.05f;                   // as mãos não passam da borda durante a subida
            var mE = new Apoio(y0 + 1.45f, 0.28f, 0.55f, 0.17f); var mD = new Apoio(y0 + 1.75f, 0.28f, 0.55f, 0.17f);
            var pE = new Apoio(y0 + 0.25f, 0.30f, 0.50f, 0.17f); var pD = new Apoio(y0 + 0.05f, 0.30f, 0.50f, 0.17f);
            cc.enabled = false;
            float t = 0f;
            // 1) salto até a parede: sobe em arco, tronco inclinado para trás e depois para a frente; mãos e pés estalam na parede
            while (t < durSalto)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / durSalto);
                Vector3 p = Vector3.Lerp(ini, ancora, 1f - (1f - k) * (1f - k));
                p.y = Mathf.Lerp(ini.y, y0, k) + 0.25f * Mathf.Sin(k * Mathf.PI);
                transform.position = p;
                float wm = Suave(0.25f, 0.8f, k), wp = Suave(0.5f, 0.92f, k);
                ik.Definir(EscaladaProcedural.MaoE, Vector3.Lerp(ini + Vector3.up * 1.2f + dir * 0.5f, faceXZ - dirD * 0.22f - dir * 0.03f + Vector3.up * mE.y, Estalo(wm)), wm);
                ik.Definir(EscaladaProcedural.MaoD, Vector3.Lerp(ini + Vector3.up * 1.4f + dir * 0.5f, faceXZ + dirD * 0.22f - dir * 0.03f + Vector3.up * mD.y, Estalo(wm)), wm);
                ik.Definir(EscaladaProcedural.PeE, Vector3.Lerp(ini + Vector3.up * 0.5f + dir * 0.2f, faceXZ - dirD * 0.16f - dir * 0.10f + Vector3.up * (pE.y + 0.1f), Estalo(wp)), wp);
                ik.Definir(EscaladaProcedural.PeD, Vector3.Lerp(ini + Vector3.up * 0.7f + dir * 0.1f, faceXZ + dirD * 0.16f - dir * 0.10f + Vector3.up * (pD.y + 0.1f), Estalo(wp)), wp);
                ik.Corpo(0f, Mathf.Lerp(-14f, 12f, Suave(0.4f, 1f, k)), 0f, 0f, 0f, 8f * wm, 8f * wm, -6f);
                ik.Olhar(faceXZ + Vector3.up * (y0 + 2.4f), 0.8f);
                jogador.DefinirVelocidadeExterna(Vector3.zero);
                yield return null;
            }
            transform.position = new Vector3(ancora.x, y0, ancora.z);
            TremorGlobal.Disparar(0.22f);
            // 2) subida pela parede
            t = 0f;
            float baseY = y0;
            while (t < durSobe)
            {
                float dt = Time.deltaTime; t += dt;
                float p = Mathf.Clamp01(t / durSobe);
                baseY = y0 + dist * p + 0.03f * Mathf.Sin(t * Mathf.PI * 2f * 3.4f);
                float impacto = Mathf.Exp(-t / 0.08f);
                transform.position = new Vector3(ancora.x, baseY, ancora.z);
                float quad = baseY + quadril;
                float yMaoE = mE.Passo(quad + ombroAcima - 0.25f, yAlto, dt, out float lME), yMaoD = mD.Passo(quad + ombroAcima, yAlto, dt, out float lMD);
                float yPeE = pE.Passo(quad - 0.40f, 99f, dt, out float lPE), yPeD = pD.Passo(quad - 0.40f, 99f, dt, out float lPD);
                ik.Definir(EscaladaProcedural.MaoE, faceXZ - dirD * 0.22f - dir * (0.03f + 0.14f * lME) + Vector3.up * yMaoE, 1f);
                ik.Definir(EscaladaProcedural.MaoD, faceXZ + dirD * 0.22f - dir * (0.03f + 0.14f * lMD) + Vector3.up * yMaoD, 1f);
                ik.Definir(EscaladaProcedural.PeE, faceXZ - dirD * 0.16f - dir * (0.10f + 0.16f * lPE) + Vector3.up * (yPeE + 0.1f), 1f);
                ik.Definir(EscaladaProcedural.PeD, faceXZ + dirD * 0.16f - dir * (0.10f + 0.16f * lPD) + Vector3.up * (yPeD + 0.1f), 1f);
                // marcha em diagonal: mão direita + pé esquerdo juntos; o corpo todo responde
                float sw = (lMD + lPE) - (lME + lPD);
                ik.Corpo(sw * 16f, 10f + 3f * Mathf.Sin(t * 20f), sw * 8f, -sw * 12f, sw * 7f, lME * 14f, lMD * 14f, sw * 6f);
                ik.Olhar(faceXZ + Vector3.up * Mathf.Min(topoY + 0.4f, baseY + 2.4f) + dirD * sw * 0.2f, 0.85f);
                if (estab != null) estab.DeslocY = -0.07f * impacto;
                jogador.DefinirVelocidadeExterna(Vector3.zero);
                yield return null;
            }
            // 3) borda: mãos empurram, o corpo se joga por cima (joelhos recolhidos), pousa em cima e sai correndo
            float durBorda = 0.5f; t = 0f;
            Vector3 pe0E = faceXZ - dirD * 0.16f - dir * 0.10f + Vector3.up * (pE.y + 0.1f), pe0D = faceXZ + dirD * 0.16f - dir * 0.10f + Vector3.up * (pD.y + 0.1f);
            Vector3 topoPeE = faceXZ + dir * 0.32f - dirD * 0.15f + Vector3.up * (topoY + 0.1f), topoPeD = faceXZ + dir * 0.32f + dirD * 0.15f + Vector3.up * (topoY + 0.1f);
            Vector3 topoMaoE = faceXZ + dir * 0.28f - dirD * 0.2f + Vector3.up * (topoY + 0.03f), topoMaoD = faceXZ + dir * 0.28f + dirD * 0.2f + Vector3.up * (topoY + 0.03f);
            Vector3 maoE0 = faceXZ - dirD * 0.22f - dir * 0.03f + Vector3.up * mE.y, maoD0 = faceXZ + dirD * 0.22f - dir * 0.03f + Vector3.up * mD.y;
            Vector3 pAntes = transform.position;
            Vector3 pTopo = faceXZ + dir * 0.55f; pTopo.y = topoY + 0.03f;
            while (t < durBorda)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / durBorda);
                Vector3 p = pAntes;
                p.y = Mathf.Lerp(pAntes.y, pTopo.y, Suave(0f, 0.7f, k)) + 0.2f * Mathf.Sin(Suave(0.2f, 0.85f, k) * Mathf.PI);
                Vector3 xz = Vector3.Lerp(pAntes, pTopo, Suave(0.3f, 0.85f, k)); p.x = xz.x; p.z = xz.z;
                transform.position = p;
                float kMao = Suave(0f, 0.2f, k), soltaMao = 1f - Suave(0.5f, 0.75f, k);
                ik.Definir(EscaladaProcedural.MaoE, Vector3.Lerp(maoE0, topoMaoE, kMao), soltaMao);
                ik.Definir(EscaladaProcedural.MaoD, Vector3.Lerp(maoD0, topoMaoD, kMao), soltaMao);
                float kE = Suave(0.2f, 0.62f, k), kD = Suave(0.3f, 0.75f, k), soltaPe = 1f - Suave(0.9f, 1f, k);
                ik.Definir(EscaladaProcedural.PeE, Vector3.Lerp(pe0E, topoPeE, kE) + Vector3.up * (0.3f * Mathf.Sin(kE * Mathf.PI)), soltaPe);
                ik.Definir(EscaladaProcedural.PeD, Vector3.Lerp(pe0D, topoPeD, kD) + Vector3.up * (0.3f * Mathf.Sin(kD * Mathf.PI)), soltaPe);
                float arco = Mathf.Sin(Suave(0f, 0.85f, k) * Mathf.PI);
                ik.Corpo(14f * arco, 24f * arco, 10f * arco, 12f * arco, -6f * arco, 10f * arco, 10f * arco, 5f * arco);
                ik.Olhar(pTopo + dir * 3f + Vector3.up * 1.5f, 0.7f);
                if (estab != null) estab.DeslocY = -0.15f * Mathf.Sin(Suave(0.7f, 1f, k) * Mathf.PI);   // amortece no pouso
                jogador.DefinirVelocidadeExterna(Vector3.zero);
                yield return null;
            }
            transform.position = pTopo;
            SoltarIK();
            cc.enabled = true;
            TremorGlobal.Disparar(0.18f);
            jogador.Impulsar(dir * 3.2f, 0f);   // sai em movimento
            jogador.AdiarTransicoes(0.3f);
            animador?.Encerrar(0.08f);
            ocupado = false;
            proximoEm = Time.time + 0.25f;
        }

        /// <summary>Subir em objeto médio, feito em código e rápido (estilo parkour + luta): agacha, explode para cima com uma mão apoiada na borda,
        /// o corpo gira no ar (joelho da frente puxado), pousa em cima com amortecimento e sai correndo.</summary>
        IEnumerator Subir(Vector3 dir, Vector3 face, float topoY, float velEntrada)
        {
            ocupado = true;
            jogador.ControleExterno = true;
            combate?.Cancelar();
            PrepararIK();
            transform.rotation = Quaternion.LookRotation(dir);
            var mov = animador != null ? animador.Movimento : null;
            if (mov != null) { mov.Ativo = false; mov.Eixo = Vector3.zero; }
            animador?.Acao("Locomocao", 30f, 0.06f, 1f);
            Vector3 dirD = Vector3.Cross(Vector3.up, dir);
            Vector3 faceXZ = new Vector3(face.x, 0f, face.z);
            Vector3 ini = transform.position;
            Vector3 antes = faceXZ - dir * 0.45f; antes.y = ini.y;
            Vector3 pTopo = faceXZ + dir * 0.5f; pTopo.y = topoY + 0.03f;
            Vector3 chaoE = antes - dirD * 0.15f + Vector3.up * 0.1f, chaoD = antes + dirD * 0.15f + dir * 0.1f + Vector3.up * 0.1f;
            Vector3 joelhoE = faceXZ + dir * 0.05f - dirD * 0.15f + Vector3.up * (topoY + 0.42f), tucoD = faceXZ - dir * 0.1f + dirD * 0.15f + Vector3.up * (topoY + 0.12f);
            Vector3 topoPeE = faceXZ + dir * 0.3f - dirD * 0.15f + Vector3.up * (topoY + 0.1f), topoPeD = faceXZ + dir * 0.4f + dirD * 0.15f + Vector3.up * (topoY + 0.1f);
            Vector3 maoApoio = faceXZ + dir * 0.18f + dirD * 0.18f + Vector3.up * (topoY + 0.03f), maoApoioE = faceXZ + dir * 0.28f - dirD * 0.2f + Vector3.up * (topoY + 0.03f);
            float dur = 0.62f, t = 0f;
            cc.enabled = false;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                // corpo: agacha (0 a 0,2), explode e voa por cima (0,2 a 0,7), pousa e amortece (0,7 a 1)
                Vector3 p = Vector3.Lerp(ini, antes, Suave(0f, 0.2f, k));
                Vector3 xz = Vector3.Lerp(antes, pTopo, Suave(0.22f, 0.8f, k)); if (k > 0.2f) { p.x = xz.x; p.z = xz.z; }
                float voo = Suave(0.2f, 0.72f, k);
                p.y = Mathf.Lerp(ini.y, pTopo.y, voo) + 0.38f * Mathf.Sin(voo * Mathf.PI);
                transform.position = p;
                float agacha = -0.2f * Suave(0.03f, 0.2f, k) * (1f - Suave(0.2f, 0.32f, k));
                float amortece = -0.16f * Mathf.Sin(Suave(0.68f, 1f, k) * Mathf.PI);
                if (estab != null) estab.DeslocY = agacha + amortece;
                // mãos: a direita apoia na borda e empurra (pivô do giro), a esquerda toca de leve
                float mD = Suave(0.26f, 0.36f, k) * (1f - Suave(0.52f, 0.64f, k)), mE = 0.6f * Suave(0.4f, 0.48f, k) * (1f - Suave(0.58f, 0.68f, k));
                ik.Definir(EscaladaProcedural.MaoD, maoApoio, mD);
                ik.Definir(EscaladaProcedural.MaoE, maoApoioE, mE);
                // pernas: pés firmes no chão no agachamento; depois o joelho da frente sobe e a outra perna recolhe; no fim os pés pousam em cima
                float solta = Suave(0.2f, 0.3f, k), pousa = Suave(0.55f, 0.72f, k), fim = 1f - Suave(0.92f, 1f, k);
                float noChao = 1f - solta;
                Vector3 alvoE = k < 0.45f ? Vector3.Lerp(chaoE, joelhoE, Suave(0.2f, 0.42f, k)) : Vector3.Lerp(joelhoE, topoPeE, pousa);
                Vector3 alvoD = k < 0.5f ? Vector3.Lerp(chaoD, tucoD, Suave(0.2f, 0.48f, k)) : Vector3.Lerp(tucoD, topoPeD, Suave(0.6f, 0.78f, k));
                ik.Definir(EscaladaProcedural.PeE, alvoE, (k < 0.2f ? 1f : 1f) * fim);
                ik.Definir(EscaladaProcedural.PeD, alvoD, fim);
                // corpo todo: inclinado para a frente no impulso, tronco girando em volta da mão de apoio, quadril e ombros acompanhando
                float ar = Mathf.Sin(Suave(0.2f, 0.8f, k) * Mathf.PI);
                float inicio = Suave(0f, 0.2f, k) * (1f - Suave(0.2f, 0.3f, k));
                ik.Corpo(26f * ar, 20f * inicio + 14f * ar, 15f * ar, -14f * ar, 8f * ar, 10f * ar, 12f * ar, 6f * ar);
                ik.Olhar(pTopo + dir * 3f + Vector3.up * 1.4f, 0.75f);
                jogador.DefinirVelocidadeExterna(Vector3.zero);
                yield return null;
            }
            transform.position = pTopo;
            SoltarIK();
            cc.enabled = true;
            TremorGlobal.Disparar(0.15f);
            jogador.Impulsar(dir * Mathf.Max(velEntrada * 0.8f, 4.5f), 0f);   // sai correndo
            jogador.AdiarTransicoes(0.3f);
            animador?.Encerrar(0.08f);
            ocupado = false;
            proximoEm = Time.time + 0.2f;
        }

        bool TentarDeslizar(Vector3 dir, float vel)
        {
            if (vel < 2.5f) return false;
            Vector3 p0 = transform.position;
            // algo na altura da cabeça à frente, e livre na altura do joelho
            if (!Lancar(p0 + Vector3.up * 1.5f, dir, 3f, out var cabeca)) return false;
            if (Lancar(p0 + Vector3.up * 0.35f, dir, cabeca.distance + 1.2f, out _)) return false;
            Vector3 sob = p0 + dir * (cabeca.distance + 0.35f);
            if (!Physics.Raycast(sob + Vector3.up * 0.05f, Vector3.up, out var teto, 2f, ~0, QueryTriggerInteraction.Ignore)) return false;
            float vao = teto.distance + 0.05f;
            if (vao < 0.65f || vao > 1.55f) { UltimaDecisao = $"vão de {vao:0.00} m não serve para deslizar"; return false; }
            // espessura da barra (até onde o teto continua)
            float espessura = 0.3f;
            for (float s = 0.3f; s < 4f; s += 0.2f)
                if (!Physics.Raycast(sob + dir * s + Vector3.up * 0.05f, Vector3.up, 2f, ~0, QueryTriggerInteraction.Ignore)) { espessura = s; break; }
            UltimaDecisao = $"deslizar (vão {vao:0.00} m)";
            StartCoroutine(Deslizar(dir, Mathf.Max(cabeca.distance + espessura + 1.4f, (cabeca.distance + espessura + 0.4f) / 0.6f)));
            return true;
        }

        IEnumerator Deslizar(Vector3 dir, float distancia)
        {
            ocupado = true;
            jogador.ControleExterno = true;
            combate?.Cancelar();
            transform.rotation = Quaternion.LookRotation(dir);
            var aj = AjustesMovimento.Atual;
            jogador.DefinirAltura(aj.alturaCapsulaRasteira);
            EstadoJogo.CameraDeslize = true;
            var mov = animador != null ? animador.Movimento : null;
            float dur = aj.duracaoRasteira;
            float lenR = animador != null ? animador.DuracaoClipePorEstado("Rasteira", 1.53f) : 1.53f;
            animador?.Acao("Rasteira", dur + 0.6f, 0.06f, lenR * 0.62f / dur);
            SomJogo.Tocar("Deslize", transform.position, 0.7f, 1f, 0.05f);
            if (mov != null) { mov.Ativo = false; mov.Eixo = Vector3.zero; }
            float velDeslize = Mathf.Max(distancia / dur, aj.velRasteira);
            Efeitos.Rapidez(transform.position + Vector3.up * 0.5f, dir, 0.8f);
            SomJogo.Tocar("Movimento_Vento", transform.position, 0.45f, 1.2f, duracaoMax: 0.6f);
            Vector3 anterior = transform.position;
            float t = 0f;
            while (t < dur || !jogador.PodeLevantar())
            {
                t += Time.deltaTime;
                cc.Move(Vector3.down * 2f * Time.deltaTime);
                if (t <= dur) cc.Move(dir * (velDeslize * Mathf.Lerp(1f, 0.55f, t / dur)) * Time.deltaTime);   // desliza desacelerando
                if (t > dur) cc.Move(dir * 2.5f * Time.deltaTime);   // ainda debaixo da barra: continua deslizando
                jogador.DefinirVelocidadeExterna((transform.position - anterior) / Mathf.Max(Time.deltaTime, 0.001f));
                anterior = transform.position;
                if (t > dur + 2f) break;
                yield return null;
            }
            if (mov != null) { mov.Escala = 1f; mov.Eixo = Vector3.zero; mov.Ativo = false; }
            EstadoJogo.CameraDeslize = false;
            jogador.DefinirAltura(-1f);
            jogador.Impulsar(dir * Mathf.Max(5f, aj.velCorrida), 0f);   // sai da rasteira correndo
            jogador.AdiarTransicoes(0.7f);
            animador?.Encerrar(0.12f);
            ocupado = false;
            proximoEm = Time.time + 0.25f;
        }
    }
}
