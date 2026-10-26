using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Efeitos dos poderes sobre o cenário: poças de água reais (Suiton), noite com nuvens carregadas e sem chuva (Kirin), destruição de objetos em volta
    /// proporcional à Fé (Shinra Tensei) e a previsão de movimentos dos inimigos (Sharingan).
    /// </summary>
    public class MundoEfeitos : MonoBehaviour
    {
        static MundoEfeitos instancia;
        public static MundoEfeitos Instancia
        {
            get
            {
                if (instancia == null) { var go = new GameObject("Efeitos do Mundo"); instancia = go.AddComponent<MundoEfeitos>(); }
                return instancia;
            }
        }

        public static void ResetarEstatico() { instancia = null; noiteAtiva = false; }

        // ------------------------------------------------------------------ poças de água
        static Material matAgua;
        public static void Poca(Vector3 pos, float raio, float duracao = 45f)
        {
            if (!Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var h, 6f, ~0, QueryTriggerInteraction.Ignore)) return;
            Instancia.StartCoroutine(Instancia.PocaRotina(h.point + Vector3.up * 0.02f, raio, duracao));
        }

        IEnumerator PocaRotina(Vector3 p, float raio, float duracao)
        {
            if (matAgua == null)
            {
                matAgua = Resources.Load<Material>("Materiais/Agua");
                if (matAgua == null) matAgua = Efeitos.LitTransparente(new Color(0.16f, 0.45f, 0.85f, 0.6f), 0.97f);
            }
            // poça de verdade: água realista (shader FaithClash/Agua, fina) + volume de física (empuxo, ondas nos passos, respingos, vadear)
            var corpo = new GameObject("Poça d'água");
            corpo.transform.position = p + Vector3.up * 0.03f;
            var vol = corpo.AddComponent<BoxCollider>();
            vol.isTrigger = true;
            vol.size = new Vector3(raio * 1.7f, 0.2f, raio * 1.7f);
            vol.center = new Vector3(0f, -0.1f, 0f);
            corpo.AddComponent<AguaFisica>();
            var mat = new Material(matAgua);
            if (mat.HasProperty("_Espuma")) { mat.SetFloat("_Espuma", 0.04f); mat.SetFloat("_ProfundidadeVisao", 0.14f); mat.SetFloat("_Onda", 0.004f); mat.SetFloat("_Refracao", 0.03f); }
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Superfície";
            Destroy(disco.GetComponent<Collider>());
            disco.transform.SetParent(corpo.transform, false);
            disco.GetComponent<Renderer>().sharedMaterial = mat;
            disco.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            disco.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Vector3 alvo = new Vector3(raio * 2f, 0.004f, raio * 2f * Random.Range(0.75f, 1f));
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { disco.transform.localScale = Vector3.Lerp(Vector3.zero, alvo, Mathf.SmoothStep(0, 1, t / 0.5f)); yield return null; }
            CatalogoVFX.Tocar("agua_ondas", p, Quaternion.identity, null, Mathf.Clamp(raio * 0.5f, 0.6f, 2.5f), 3f);
            float prox = 0f;
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                if (t > prox) { prox = t + Random.Range(2f, 5f); CatalogoVFX.Tocar("agua_ondas", p + Random.insideUnitSphere * raio * 0.5f, Quaternion.identity, null, 0.5f, 2.5f); }
                yield return null;
            }
            for (float t = 0f; t < 3f; t += Time.deltaTime) { disco.transform.localScale = Vector3.Lerp(alvo, new Vector3(alvo.x * 0.6f, 0.002f, alvo.z * 0.6f), t / 3f); yield return null; }
            Destroy(corpo);
            Destroy(mat);
        }

        // ------------------------------------------------------------------ Kirin: céu noturno com nuvens pesadas, sem chuva
        static bool noiteAtiva;
        public static void Noite(float duracao)
        {
            if (noiteAtiva) return;
            Instancia.StartCoroutine(Instancia.NoiteRotina(duracao));
        }

        IEnumerator NoiteRotina(float duracao)
        {
            noiteAtiva = true;
            var sol = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light direcional = null;
            foreach (var l in sol) if (l.type == LightType.Directional) { direcional = l; break; }
            float intensidade0 = direcional != null ? direcional.intensity : 1f;
            Color corLuz0 = direcional != null ? direcional.color : Color.white;
            Color ambiente0 = RenderSettings.ambientLight; Color neblina0 = RenderSettings.fogColor;
            var skyOriginal = RenderSettings.skybox;
            // trabalha numa cópia do material do céu: o asset do projeto nunca é alterado
            var sky = skyOriginal != null ? new Material(skyOriginal) : null;
            if (sky != null) RenderSettings.skybox = sky;
            float expo0 = sky != null && sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
            Color tinta0 = sky != null && sky.HasProperty("_Tint") ? sky.GetColor("_Tint") : Color.gray;
            var nuvens = CriarNuvens();
            var emissao = nuvens.emission;

            const float entra = 2.2f, sai = 3.5f;
            for (float t = 0f; t < duracao + sai; t += Time.deltaTime)
            {
                float k = t < entra ? t / entra : t < duracao ? 1f : 1f - (t - duracao) / sai;
                k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(k));
                if (direcional != null) { direcional.intensity = Mathf.Lerp(intensidade0, intensidade0 * 0.12f, k); direcional.color = Color.Lerp(corLuz0, new Color(0.35f, 0.45f, 0.9f), k); }
                RenderSettings.ambientLight = Color.Lerp(ambiente0, new Color(0.05f, 0.07f, 0.16f), k);
                RenderSettings.fogColor = Color.Lerp(neblina0, new Color(0.03f, 0.04f, 0.09f), k);
                if (sky != null)
                {
                    if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(expo0, 0.12f, k));
                    if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", Color.Lerp(tinta0, new Color(0.15f, 0.2f, 0.45f), k));
                }
                emissao.rateOverTime = 14f * k;
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) nuvens.transform.position = pc.transform.position + Vector3.up * 52f;
                yield return null;
            }
            if (direcional != null) { direcional.intensity = intensidade0; direcional.color = corLuz0; }
            RenderSettings.ambientLight = ambiente0; RenderSettings.fogColor = neblina0;
            if (sky != null)
            {
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", expo0);
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", tinta0);
            }
            if (skyOriginal != null) RenderSettings.skybox = skyOriginal;
            Destroy(nuvens.gameObject, 12f);
            noiteAtiva = false;
        }

        static Texture2D texNuvem;
        ParticleSystem CriarNuvens()
        {
            if (texNuvem == null)
            {
                const int n = 128;
                texNuvem = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ruido = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.5f + Mathf.PerlinNoise(x * 0.2f + 9f, y * 0.2f) * 0.25f;
                        float a = Mathf.Clamp01((1f - r) * 1.8f) * (0.55f + ruido);
                        texNuvem.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
                    }
                texNuvem.Apply();
            }
            var go = new GameObject("Nuvens do Kirin");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = 30f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(34f, 70f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.16f, 0.18f, 0.28f, 0.85f), new Color(0.3f, 0.32f, 0.42f, 0.85f));
            main.maxParticles = 220; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.x = 2.5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(190f, 6f, 190f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f); mat.SetFloat("_SrcBlend", 5f); mat.SetFloat("_DstBlend", 10f); mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000;
            mat.SetTexture("_BaseMap", texNuvem); mat.SetColor("_BaseColor", Color.white);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            go.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            var em = ps.emission; em.rateOverTime = 0f;
            // nuvens já espalhadas de cara
            ps.Emit(120);
            ps.Play();
            return ps;
        }

        // ------------------------------------------------------------------ Shinra Tensei: destruição do cenário em volta
        /// <summary>Destrói/arremessa os objetos do cenário num raio; <paramref name="forca"/> 0..1 (Fé).</summary>
        public static int Destruir(Vector3 centro, float raio, float forca, Transform ignorar = null)
        {
            var feitos = new HashSet<GameObject>();
            int n = 0;
            foreach (var col in Physics.OverlapSphere(centro, raio, ~0, QueryTriggerInteraction.Ignore))
            {
                if (ignorar != null && col.transform.IsChildOf(ignorar)) continue;
                if (col.GetComponentInParent<CharacterController>() != null || col.GetComponentInParent<Health>() != null) continue;
                var b = col.bounds;
                if (b.size.x > 22f || b.size.z > 22f || b.size.y > 14f) continue;            // chão e paredes do mundo ficam
                if (b.max.y < 0.15f) continue;                                                 // lajes ao nível do chão
                if (col is MeshCollider mc && !mc.convex) continue;
                var go = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
                if (!feitos.Add(go)) continue;
                if (!go.TryGetComponent(out Rigidbody rb)) rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = false; rb.mass = 30f;
                Vector3 dir = (b.center - centro); dir.y = Mathf.Abs(dir.y) * 0.3f + 0.4f;
                rb.AddForce(dir.normalized * Mathf.Lerp(8f, 40f, forca) * (1f - Mathf.Clamp01(dir.magnitude / raio) * 0.5f), ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * Mathf.Lerp(3f, 12f, forca), ForceMode.VelocityChange);
                CatalogoVFX.Tocar("terra_pedras", b.center, Quaternion.identity, null, Mathf.Clamp(b.size.magnitude * 0.4f, 0.6f, 2.5f), 2f);
                Instancia.StartCoroutine(Instancia.DesfazerRotina(go, Random.Range(1.6f, 2.8f)));
                n++;
                if (n >= 60) break;
            }
            // cratera escura no chão
            if (Physics.Raycast(centro + Vector3.up * 2f, Vector3.down, out var h, 8f, ~0, QueryTriggerInteraction.Ignore))
                Instancia.StartCoroutine(Instancia.CrateraRotina(h.point + Vector3.up * 0.015f, raio * 0.9f));
            return n;
        }

        IEnumerator DesfazerRotina(GameObject go, float espera)
        {
            yield return new WaitForSeconds(espera);
            if (go == null) yield break;
            Vector3 e0 = go.transform.localScale;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                if (go == null) yield break;
                go.transform.localScale = Vector3.Lerp(e0, Vector3.zero, t / 0.6f);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        static Material matCratera;
        IEnumerator CrateraRotina(Vector3 p, float raio)
        {
            if (matCratera == null) matCratera = Efeitos.LitTransparente(new Color(0.03f, 0.03f, 0.035f, 0.7f), 0.1f);
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = "Cratera";
            Destroy(d.GetComponent<Collider>());
            d.GetComponent<Renderer>().sharedMaterial = matCratera;
            d.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            d.transform.position = p;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { d.transform.localScale = new Vector3(raio * 2f * (t / 0.4f), 0.003f, raio * 2f * (t / 0.4f)); yield return null; }
            d.transform.localScale = new Vector3(raio * 2f, 0.003f, raio * 2f);
            yield return new WaitForSeconds(30f);
            Destroy(d);
        }

        // ------------------------------------------------------------------ Sharingan: lentidão + previsão de movimentos
        public static void Previsao(float duracao, Transform jogador)
        {
            Instancia.StartCoroutine(Instancia.PrevisaoRotina(duracao, jogador));
        }

        IEnumerator PrevisaoRotina(float duracao, Transform jogador)
        {
            var linhas = new Dictionary<TrainingDummy, LineRenderer>();
            var ultimas = new Dictionary<TrainingDummy, Vector3>();
            var mat = Efeitos.LitTransparente(new Color(1f, 0.1f, 0.1f, 0.8f), 0.1f);
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                foreach (var d in TrainingDummy.Todos)
                {
                    if (d == null || d.Vida.IsDead) continue;
                    if (jogador != null && Vector3.Distance(d.transform.position, jogador.position) > 30f) continue;
                    if (!linhas.TryGetValue(d, out var lr))
                    {
                        lr = new GameObject("Previsão").AddComponent<LineRenderer>();
                        lr.positionCount = 3; lr.startWidth = 0.14f; lr.endWidth = 0.02f; lr.sharedMaterial = mat;
                        lr.startColor = new Color(1f, 0.15f, 0.1f); lr.endColor = new Color(1f, 0.15f, 0.1f, 0f);
                        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        linhas[d] = lr; ultimas[d] = d.transform.position;
                    }
                    Vector3 v = (d.transform.position - ultimas[d]) / Mathf.Max(Time.deltaTime, 0.0001f);
                    ultimas[d] = d.transform.position;
                    Vector3 dir = v.sqrMagnitude > 0.2f ? v.normalized : d.transform.forward;
                    Vector3 o = d.transform.position + Vector3.up * 0.9f;
                    float comp = Mathf.Clamp(v.magnitude * 0.9f + 2.5f, 2.5f, 8f);
                    lr.SetPosition(0, o); lr.SetPosition(1, o + dir * comp * 0.5f + Vector3.up * 0.1f); lr.SetPosition(2, o + dir * comp);
                }
                yield return null;
            }
            foreach (var kv in linhas) if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
    }
}
