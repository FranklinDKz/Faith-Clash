#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using FaithClash.Player;

namespace FaithClash.Training
{
    /// <summary>
    /// Teste automático do movimento (roda no Editor, inclusive em modo batch): parado, correr, pulo normal, transposição de obstáculos, subir muro,
    /// deslizar, plataforma móvel e parede. Liga-se com a variável de ambiente FC_TESTE=1 (ver Editor/TesteJogo.cs) e escreve Logs/teste_movimento.txt.
    /// </summary>
    public class PilotoTeste : MonoBehaviour
    {
        const string Arquivo = "Logs/teste_movimento.txt";
        PlayerController pc; Parkour pk; AnimadorProtagonista an; CharacterController cc;
        readonly StringBuilder sb = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            if (System.Environment.GetEnvironmentVariable("FC_TESTE") != "1") return;
            new GameObject("PilotoTeste").AddComponent<PilotoTeste>();
        }

        void L(string t) { sb.AppendLine(t); }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(2.0f);
            pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) { L("ERRO: jogador não encontrado"); Fim(); yield break; }
            pk = pc.GetComponent<Parkour>(); an = pc.GetComponent<AnimadorProtagonista>(); cc = pc.GetComponent<CharacterController>();
            PlayerController.TesteAtivo = true;
            if (System.Environment.GetEnvironmentVariable("FC_POSES") == "1") StartCoroutine(CapturarPoses());
            StartCoroutine(MonitorEstados());
            if (System.Environment.GetEnvironmentVariable("FC_CLIPES") == "1") { yield return Galeria(); Fim(); yield break; }
            if (System.Environment.GetEnvironmentVariable("FC_FOCO") == "1")
            {
                // foco: pulos e rasteira (as animações que ficam)
                yield return PuloNormal();
                yield return PuloSegurando(false);
                yield return PuloSegurando(true);
                yield return Obstaculo("Barra de Deslize correndo", new Vector3(-4f, 0.1f, -2.5f), 0f, true, 1.2f);
                
                
                
                yield return Obstaculo("Plataforma", new Vector3(-6f, 0.1f, -9.5f), 180f, true, 0.5f, -1f);
                yield return Obstaculo("Mureta 1 correndo", new Vector3(-16f, 0.1f, 1.5f), 0f, true);
                yield return Obstaculo("Mureta 2 correndo", new Vector3(-16f, 0.1f, 8.2f), 0f, true);
                yield return Dash();
                yield return Quedas();
                yield return Obstaculo("Muro de Escalada", new Vector3(-16f, 0.1f, 12.8f), 0f, true);
                // escadas testadas
                Fim(); yield break;
            }
            L($"cena={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} jogador={pc.transform.position} cc.altura={cc.height} raio={cc.radius}");
            yield return Parado();
            yield return Correr();
            yield return PuloNormal();
            yield return PuloSegurando(false);
            yield return PuloSegurando(true);
            yield return Obstaculo("Barra de Deslize correndo", new Vector3(-4f, 0.1f, -2.5f), 0f, true, 1.2f);
            yield return Plataforma();
            Fim();
        }

        void Fim()
        {
            PlayerController.TesteMover = null; PlayerController.TesteSprint = false; PlayerController.TesteAtivo = false;
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Arquivo, sb.ToString());
            UnityEditor.EditorApplication.Exit(0);
        }

        void Reposicionar(Vector3 pos, float yaw)
        {
            PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
            pc.Teleportar(pos, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Com FC_CLIPES=1: toca cada estado do Animator parado, num ponto livre, e fotografa 8 quadros (Logs/clipes/estado_N.png) de frente-diagonal.</summary>
        IEnumerator Galeria()
        {
            Directory.CreateDirectory("Logs/clipes");
            foreach (var e in FindObjectsByType<FaithClash.Training.InimigoIA>(FindObjectsInactive.Exclude)) e.gameObject.SetActive(false);
            pc.transform.position = new Vector3(0f, 0.05f, -20f); Physics.SyncTransforms();
            yield return new WaitForSeconds(1f);
            var camGo = new GameObject("CamClipes"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.25f, 0.28f, 0.35f); cam.fieldOfView = 38f;
            var rt = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
            string[] estados = { "PuloImpulso", "PuloCorrendo", "PuloCorrendoLivre", "Rasteira", "NoAr", "PousoLeve" };
            foreach (var st in estados)
            {
                if (!an.TemEstado(st)) { L("sem estado: " + st); continue; }
                float len = Mathf.Clamp(an.DuracaoClipePorEstado(st, 1f), 0.5f, 3f);
                an.Acao(st, len + 0.2f, 0.03f, 1f);
                L($"[clipe] {st} duração={len:0.00}s");
                for (int i = 0; i < 16; i++)
                {
                    yield return new WaitForSeconds(len / 16f);
                    { var q = an.Animator.GetBoneTransform(HumanBodyBones.Hips); var pL = an.Animator.GetBoneTransform(HumanBodyBones.LeftFoot); var pR = an.Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                      L($"[fase] {st} t={(i + 1) / 16f:0.00} quadril={q.position.y - pc.transform.position.y:0.00} peMin={Mathf.Min(pL.position.y, pR.position.y) - pc.transform.position.y:0.00}"); }
                    if (i % 2 == 1)
                    {
                    var c = pc.transform.position + Vector3.up * 1.0f;
                    if (an != null && an.Animator != null && an.Animator.isHuman) { var hq = an.Animator.GetBoneTransform(HumanBodyBones.Hips); if (hq != null) c = hq.position; }
                    camGo.transform.position = c + (pc.transform.forward * 3.2f + pc.transform.right * 2.2f) + Vector3.up * 0.4f; camGo.transform.LookAt(c);
                    cam.Render();
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes($"Logs/clipes/{st}_{i / 2}.png", tex.EncodeToPNG());
                    }
                }
                yield return new WaitForSeconds(0.4f);
                pc.transform.position = new Vector3(0f, 0.05f, -20f); Physics.SyncTransforms();
            }
        }

        /// <summary>Com FC_POSES=1: fotografa o personagem de lado a cada 0,2 s durante todo o teste (Logs/poses/NNNN_acao.png), para conferir as poses das animações.</summary>
        IEnumerator CapturarPoses()
        {
            System.IO.Directory.CreateDirectory("Logs/poses");
            var camGo = new GameObject("CamPoses"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.25f, 0.28f, 0.35f); cam.fieldOfView = 40f; cam.cullingMask = ~0;
            var rt = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var tex = new Texture2D(768, 768, TextureFormat.RGB24, false);
            int n = 0;
            while (true)
            {
                if (pc != null)
                {
                    var c = pc.transform.position + Vector3.up * 1.0f;
                    Vector3 lado = Vector3.Cross(Vector3.up, pc.transform.forward).normalized;
                    Vector3 melhor = c + lado * 5.2f + Vector3.up * 0.3f;
                    foreach (var cand in new[] { lado, -lado, pc.transform.forward, -pc.transform.forward })
                    {
                        Vector3 pos = c + cand * 5.2f + Vector3.up * 0.4f;
                        if (!Physics.Linecast(c, pos, ~0, QueryTriggerInteraction.Ignore)) { melhor = pos; break; }
                    }
                    camGo.transform.position = melhor; camGo.transform.LookAt(c);
                    cam.Render();
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); tex.Apply(); RenderTexture.active = null;
                    string nome = (an != null && an.AcaoAtual != null ? an.AcaoAtual : "loco").Replace(" ", "_");
                    System.IO.File.WriteAllBytes($"Logs/poses/{n:0000}_{nome}.png", tex.EncodeToPNG());
                    if (an != null && an.Animator != null && an.Animator.isHuman) { var q = an.Animator.GetBoneTransform(HumanBodyBones.Hips); var pe = an.Animator.GetBoneTransform(HumanBodyBones.LeftFoot); L($"[quadro {n:0000}] {nome} capsula.y={pc.transform.position.y:0.00} quadril.y={(q != null ? q.position.y : 0f):0.00} pe.y={(pe != null ? pe.position.y : 0f):0.00} modelo.y={an.Animator.transform.position.y:0.00} ik={(an.Animator.GetComponent<EscaladaProcedural>() != null ? an.Animator.GetComponent<EscaladaProcedural>().Chamadas + "/" + an.Animator.GetComponent<EscaladaProcedural>().Ativo + " alvoMaoD=" + an.Animator.GetComponent<EscaladaProcedural>().AlvoMaoD : "sem")} maoD={an.Animator.GetBoneTransform(HumanBodyBones.RightHand).position}"); }
                    n++;
                }
                yield return new WaitForSeconds(an != null && an.AcaoAtual != null && ((an.AcaoAtual == "Rasteira") || (pk != null && pk.Ocupado)) ? 0.05f : 0.2f);
            }
        }

        IEnumerator Parado()
        {
            Reposicionar(new Vector3(0f, 0.1f, -2f), 0f);
            yield return new WaitForSeconds(0.8f);
            float y0 = pc.transform.position.y;
            yield return new WaitForSeconds(2f);
            { var ai = pc.GetComponentInChildren<Animator>(); var inf = ai.GetCurrentAnimatorClipInfo(0); var nomes = new System.Text.StringBuilder(); foreach (var c in inf) if (c.weight > 0.01f) nomes.Append(c.clip.name + "(" + c.weight.ToString("0.0") + ") "); L("[parado] clipes tocando: " + nomes); }
            L($"[parado] y inicial={y0:0.000} y depois de 2 s={pc.transform.position.y:0.000} (variação {Mathf.Abs(pc.transform.position.y - y0) * 100f:0.0} cm) noChao={pc.IsGrounded} cabeça={AlturaCabeca():0.00} m");
        }

        float AlturaCabeca()
        {
            var animator = pc.GetComponentInChildren<Animator>();
            var h = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            return h != null ? h.position.y - pc.transform.position.y : -1f;
        }

        string Clipes()
        {
            var ai = an.Animator; var st = ai.GetCurrentAnimatorStateInfo(0); var sb2 = new System.Text.StringBuilder();
            foreach (var c in ai.GetCurrentAnimatorClipInfo(0)) if (c.weight > 0.01f) sb2.Append(c.clip.name + "(" + c.weight.ToString("0.0") + ") ");
            string nome = "?"; foreach (var n in new[] { "Rasteira", "Locomocao", "LocomocaoAlvo", "NoAr", "PousoLeve", "PuloImpulso", "PuloCorrendo", "PuloCorrendoLivre", "Combate", "Espada", "EspadaDupla", "Ferido" }) if (st.shortNameHash == Animator.StringToHash(n)) nome = n;
            return $"estado={nome} t={st.normalizedTime:0.00} vel={st.speed:0.00}x{st.speedMultiplier:0.00} transicao={ai.IsInTransition(0)} chao={pc.IsGrounded} clipes={sb2} Velocidade={ai.GetFloat("Velocidade"):0.00}";
        }

        IEnumerator Correr()
        {
            Reposicionar(new Vector3(0f, 0.1f, -4f), 0f);
            yield return new WaitForSeconds(0.5f);
            PlayerController.TesteMover = new Vector2(0, 1);
            yield return new WaitForSeconds(1.0f);
            L($"[andar/correr] velocidade={Horiz(pc.Velocity):0.00} m/s (corrida), cabeça={AlturaCabeca():0.00} m");
            L("[andar/correr] " + Clipes()); yield return new WaitForSeconds(0.23f); L("[andar/correr] +0,23 s " + Clipes());
            PlayerController.TesteSprint = true;
            yield return new WaitForSeconds(1.2f);
            L($"[sprint] velocidade={Horiz(pc.Velocity):0.00} m/s, cabeça={AlturaCabeca():0.00} m, acao={an.AcaoAtual}");
            L("[sprint] " + Clipes()); yield return new WaitForSeconds(0.23f); L("[sprint] +0,23 s " + Clipes());
            PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
            yield return new WaitForSeconds(1f);
        }

        static float Horiz(Vector3 v) { v.y = 0; return v.magnitude; }

        IEnumerator PuloSegurando(bool duplo)
        {
            Reposicionar(new Vector3(0f, 0.1f, -6f), 0f);
            yield return new WaitForSeconds(0.6f);
            var cab = an.Animator.GetBoneTransform(HumanBodyBones.Head);
            float y0 = pc.transform.position.y, ymax = y0, cab0 = cab.position.y, cabMax = cab0;
            PlayerController.TesteSegurandoEspaco = true;
            PlayerController.TesteApertarEspaco();
            var acoes = new System.Text.StringBuilder(); string ult = "";
            bool apertou2 = false;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                yield return null;
                ymax = Mathf.Max(ymax, pc.transform.position.y); cabMax = Mathf.Max(cabMax, cab.position.y);
                var a = an.AcaoAtual ?? "(loco/ar)"; if (a != ult) { acoes.Append($" {t:0.00}:{a}"); ult = a; }
                if (duplo && !apertou2 && t > 0.35f) { PlayerController.TesteApertarEspaco(); apertou2 = true; }
                if (t > 0.5f && pc.IsGrounded) break;
            }
            PlayerController.TesteSegurandoEspaco = false;
            L($"[pulo {(duplo ? "duplo" : "segurando")}] cápsula subiu={(ymax - y0):0.00} m, cabeça subiu={(cabMax - cab0):0.00} m, ações:{acoes}");
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator PuloNormal()
        {
            Reposicionar(new Vector3(0f, 0.1f, -6f), 0f);
            yield return new WaitForSeconds(0.6f);
            float y0 = pc.transform.position.y, ymax = y0, t0 = Time.time;
            PlayerController.TesteApertarEspaco();
            float tNoAr = 0f; bool decolou = false;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime)
            {
                yield return null;
                float y = pc.transform.position.y; ymax = Mathf.Max(ymax, y);
                if (!pc.IsGrounded) { decolou = true; tNoAr += Time.deltaTime; }
                else if (decolou && t > 0.3f) break;
            }
            L($"[pulo normal] altura do salto={(ymax - y0):0.00} m, tempo no ar={tNoAr:0.00} s, acao final={an.AcaoAtual}");
        }

        IEnumerator Obstaculo(string nome, Vector3 inicio, float yaw, bool correndo, float alturaRaio = 0.5f, float sentido = 1f)
        {
            Reposicionar(inicio, yaw);
            yield return new WaitForSeconds(0.7f);
            PlayerController.TesteMover = new Vector2(0, sentido);
            PlayerController.TesteSprint = correndo;
            // acelera
            yield return new WaitForSeconds(correndo ? 0.9f : 0.5f);
            // procura o obstáculo à frente e aperta Espaço a ~2,2 m
            float tent = 0f; bool apertou = false; Vector3 posApertou = default; float velApertou = 0f;
            while (tent < 3f && !apertou)
            {
                tent += Time.deltaTime;
                if (Physics.Raycast(pc.transform.position + Vector3.up * alturaRaio, pc.transform.forward, out var h, 1.7f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<CharacterController>() == null)
                {
                    PlayerController.TesteApertarEspaco(); apertou = true; posApertou = pc.transform.position; velApertou = Horiz(pc.Velocity);
                    break;
                }
                yield return null;
            }
            if (!apertou) { L($"[{nome}] não achou obstáculo à frente (pos={pc.transform.position})"); yield break; }
            // acompanha a ação
            float y0 = pc.transform.position.y, ymax = y0, xzIni = 0; Vector3 pIni = pc.transform.position; float tIni = Time.time;
            bool emAcao = false; string acao = ""; float tAcao = 0f; Vector3 pAcao0 = default, pAcao1 = default; float vMax = 0f;
            for (float t = 0f; t < 7f; t += Time.deltaTime)
            {
                yield return null;
                Vector3 p = pc.transform.position; ymax = Mathf.Max(ymax, p.y); vMax = Mathf.Max(vMax, Horiz(pc.Velocity));
                if (an.AcaoAtual != null && !emAcao) { emAcao = true; acao = an.AcaoAtual; pAcao0 = p; tAcao = Time.time; }
                if (emAcao && (an.AcaoAtual == null || an.AcaoAtual != acao) && t > 0.25f) { pAcao1 = p; break; }
            }
            if (pAcao1 == default) pAcao1 = pc.transform.position;
            float dur = Time.time - tAcao;
            L($"[{nome}] decisão='{pk.UltimaDecisao}' aperto a {Distancia(posApertou, pc.transform):0.00} m, vel={velApertou:0.00} m/s, ação='{acao}' dur={dur:0.00} s, subiu={(ymax - y0):0.00} m, deslocou={Vector3.Distance(new Vector3(pAcao0.x, 0, pAcao0.z), new Vector3(pAcao1.x, 0, pAcao1.z)):0.00} m (vel média da ação {(dur > 0 ? Vector3.Distance(new Vector3(pAcao0.x, 0, pAcao0.z), new Vector3(pAcao1.x, 0, pAcao1.z)) / dur : 0):0.00} m/s), y final={pc.transform.position.y:0.00}, noChao={pc.IsGrounded}");
            PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
            yield return new WaitForSeconds(0.5f); L($"[{nome}] +0,5 s depois: {Clipes()} acao={an.AcaoAtual}");
            yield return new WaitForSeconds(0.7f); L($"[{nome}] +1,2 s depois: {Clipes()} acao={an.AcaoAtual}");
        }

        float Distancia(Vector3 de, Transform t) => Vector3.Distance(new Vector3(de.x, 0, de.z), new Vector3(t.position.x, 0, t.position.z));


        string ultimoClipe = "";
        IEnumerator MonitorEstados()
        {
            while (true)
            {
                if (an != null && an.Animator != null)
                {
                    string dom = "?"; float mx = 0f;
                    foreach (var ci in an.Animator.GetCurrentAnimatorClipInfo(0)) if (ci.weight > mx) { mx = ci.weight; dom = ci.clip.name; }
                    string acao = an.AcaoAtual ?? "";
                    string chave = dom + "|" + acao;
                    if (chave != ultimoClipe) { ultimoClipe = chave; L($"[mud t={Time.time:0.00}] clipe={dom} acao={acao} chao={(pc != null && pc.IsGrounded)} y={(pc != null ? pc.transform.position.y : 0f):0.00}"); }
                }
                yield return null;
            }
        }

        IEnumerator Dash()
        {
            Reposicionar(new Vector3(0f, 0.1f, -4f), 0f);
            yield return new WaitForSeconds(0.6f);
            PlayerController.TesteMover = new Vector2(0, 1);
            yield return new WaitForSeconds(0.5f);
            L("[dash] antes: " + Clipes());
            PlayerController.TesteApertarDash();
            for (int i = 0; i < 12; i++) { yield return new WaitForSeconds(0.06f); L($"[dash] +{(i + 1) * 0.06f:0.00}s vel={Horiz(pc.Velocity):0.00} acao={an.AcaoAtual}"); }
            PlayerController.TesteMover = null;
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Quedas()
        {
            foreach (var (nome, pos, mover, sprint) in new[] { ("queda 6 m correndo", new Vector3(1f, 6f, -1f), 1f, true), ("queda 5 m parado", new Vector3(4f, 5f, -1f), 0f, false), ("queda 2 m andando", new Vector3(7f, 2.1f, -1f), 1f, false) })
            {
                Reposicionar(pos, 0f);
                PlayerController.TesteMover = mover > 0f ? new Vector2(0, 1) : (Vector2?)null; PlayerController.TesteSprint = sprint;
                for (int i = 0; i < 28; i++) { yield return new WaitForSeconds(0.1f); if (i % 3 == 0) L($"[{nome}] +{(i + 1) * 0.1f:0.0}s y={pc.transform.position.y:0.00} vel={Horiz(pc.Velocity):0.00} acao={an.AcaoAtual} {Clipes()}"); }
                PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
                yield return new WaitForSeconds(0.4f);
            }
        }

        IEnumerator Escadas()
        {
            foreach (var (nome, pos, yaw, sent) in new[] { ("escada subindo", new Vector3(12f, 0.1f, -8.5f), 0f, 1f), ("escada descendo", new Vector3(12f, 1.15f, -2.7f), 180f, -1f), ("RAMPA subindo", new Vector3(-12f, 0.1f, -9.5f), 0f, 1f) })
            {
                Reposicionar(pos, yaw);
                yield return new WaitForSeconds(0.8f);
                PlayerController.TesteMover = new Vector2(0, sent); PlayerController.TesteSprint = true;
                for (int i = 0; i < 12; i++) { yield return new WaitForSeconds(0.12f); L($"[{nome}] y={pc.transform.position.y:0.00} vel={Horiz(pc.Velocity):0.00} normal={pc.NormalDoChao.y:0.000} {Clipes()}"); }
                PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
            }
        }

        IEnumerator VirarCosta()
        {
            Reposicionar(new Vector3(8f, 0.1f, 4f), 0f);
            yield return new WaitForSeconds(0.7f);
            PlayerController.TesteMover = new Vector2(0, 1); PlayerController.TesteSprint = true;
            yield return new WaitForSeconds(1.3f);
            L($"[virar] correndo a {Horiz(pc.Velocity):0.00} m/s, estado: {Clipes()}");
            PlayerController.TesteMover = new Vector2(0, -1);
            for (int i = 0; i < 40; i++) { yield return new WaitForSeconds(0.05f); if (i % 4 == 0) L($"[virar] +{(i + 1) * 0.05f:0.00}s vel={Horiz(pc.Velocity):0.00} acao={an.AcaoAtual} {Clipes()}"); }
            PlayerController.TesteMover = null; PlayerController.TesteSprint = false;
        }

        IEnumerator Plataforma()
        {
            var plat = GameObject.Find("Plataforma Móvel");
            if (plat == null) { L("[plataforma] não achei a plataforma"); yield break; }
            Reposicionar(plat.transform.position + Vector3.up * 0.7f, 0f);
            yield return new WaitForSeconds(0.6f);
            Vector3 rel0 = pc.transform.position - plat.transform.position;
            float z0 = pc.transform.position.z;
            yield return new WaitForSeconds(1.0f);
            L($"[plataforma] diag: {pc.DiagFisica}");
            yield return new WaitForSeconds(1.5f);
            Vector3 rel1 = pc.transform.position - plat.transform.position;
            L($"[plataforma] deslocamento relativo no início={rel0}, depois de 2,5 s={rel1}; jogador andou z={(pc.transform.position.z - z0):0.00} m, plataforma está em z={plat.transform.position.z:0.00}");
        }

    }
}
#endif
