using System.Collections;
using UnityEngine;

namespace FaithClash.Combat
{
    /// <summary>Relâmpago de verdade: linha recortada entre dois pontos que pisca e some.</summary>
    public class RelampagoVisual : MonoBehaviour
    {
        LineRenderer lr;
        Vector3 a, b;
        float vida, t, largura;
        Color cor;
        float proximaTroca;

        public static RelampagoVisual Criar(Vector3 de, Vector3 para, Color cor, float duracao = 0.25f, float largura = 0.18f)
        {
            var go = new GameObject("Relâmpago");
            var r = go.AddComponent<RelampagoVisual>();
            r.a = de; r.b = para; r.vida = duracao; r.largura = largura; r.cor = cor;
            r.lr = go.AddComponent<LineRenderer>();
            r.lr.sharedMaterial = Efeitos.MaterialAditivo;
            r.lr.useWorldSpace = true;
            r.lr.numCornerVertices = 3;
            r.lr.startColor = r.lr.endColor = cor;
            r.Refazer();
            var l = new GameObject("Luz").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.position = Vector3.Lerp(de, para, 0.5f);
            l.color = cor; l.range = 8f; l.intensity = 4f;
            return r;
        }

        void Refazer()
        {
            int n = Mathf.Clamp((int)(Vector3.Distance(a, b) * 1.6f), 6, 40);
            lr.positionCount = n + 1;
            Vector3 dir = (b - a);
            Vector3 u = Vector3.Cross(dir, Vector3.up); if (u.sqrMagnitude < 0.01f) u = Vector3.right; u.Normalize();
            Vector3 v = Vector3.Cross(dir.normalized, u);
            float desvio = Mathf.Min(1.2f, dir.magnitude * 0.08f + 0.15f);
            for (int i = 0; i <= n; i++)
            {
                float f = i / (float)n;
                Vector3 p = Vector3.Lerp(a, b, f);
                if (i > 0 && i < n) p += (u * Random.Range(-1f, 1f) + v * Random.Range(-1f, 1f)) * desvio;
                lr.SetPosition(i, p);
            }
            lr.widthMultiplier = largura * Random.Range(0.7f, 1.3f);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= vida) { Destroy(gameObject); return; }
            if (t >= proximaTroca) { Refazer(); proximaTroca = t + 0.04f; }
            float k = 1f - t / vida;
            var c = cor; c.a = k;
            lr.startColor = lr.endColor = c;
        }
    }

    /// <summary>Feixe de energia (Kamehameha): cilindro brilhante que segue as mãos e causa dano em linha.</summary>
    public class FeixeEnergia : MonoBehaviour
    {
        public System.Func<Vector3> origem;
        public System.Func<Vector3> direcao;
        public Color cor = Color.cyan;
        public float comprimento = 20f, largura = 1.2f, duracao = 1.5f, danoPorTick = 22f, tick = 0.12f, empurrao = 6f;
        public EstilosElementais dono;

        Transform nucleo, halo;
        Light luz;
        float t, proximoTick;

        void Start()
        {
            nucleo = Cilindro("Núcleo", Color.Lerp(cor, Color.white, 0.8f)).transform;
            halo = Cilindro("Halo", new Color(cor.r, cor.g, cor.b, 0.5f)).transform;
            luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.color = cor; luz.range = 14f; luz.intensity = 6f;
            Atualizar(0f);
        }

        GameObject Cilindro(string nome, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = nome;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var m = new Material(Efeitos.MaterialAditivo);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        void Atualizar(float k)
        {
            Vector3 o = origem(), d = direcao().normalized;
            float cresce = Mathf.Clamp01(t / 0.18f), some = Mathf.Clamp01((duracao - t) / 0.3f);
            float esc = Mathf.Min(cresce, some);
            float pulso = 1f + Mathf.Sin(t * 50f) * 0.08f;
            float comp = comprimento * Mathf.Min(1f, cresce * 1.4f);
            transform.SetPositionAndRotation(o + d * comp * 0.5f, Quaternion.FromToRotation(Vector3.up, d));
            nucleo.localScale = new Vector3(largura * 0.55f * esc * pulso, comp * 0.5f, largura * 0.55f * esc * pulso);
            halo.localScale = new Vector3(largura * 1.2f * esc * pulso, comp * 0.5f, largura * 1.2f * esc * pulso);
            luz.intensity = 6f * esc;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= duracao) { Destroy(gameObject); return; }
            Atualizar(t);
            if (t >= proximoTick)
            {
                proximoTick = t + tick;
                Vector3 o = origem(), d = direcao().normalized;
                dono?.DanoEmLinha(o, o + d * comprimento, largura * 0.8f, danoPorTick, empurrao, d);
                Efeitos.Rajada(o + d * Random.Range(1f, comprimento), Quaternion.identity, cor, 6, new Vector2(1f, 4f), new Vector2(0.1f, 0.3f), new Vector2(0.2f, 0.5f), raio: largura * 0.4f);
            }
        }
    }

    /// <summary>Faz o objeto seguir um osso (mão, cabeça) com um deslocamento: usado nas cargas de energia.</summary>
    public class SeguirOsso : MonoBehaviour
    {
        public System.Func<Vector3> alvo;
        void LateUpdate() { if (alvo != null) transform.position = alvo(); }
    }

    public static class EfeitosAnime
    {
        static Mesh esferaInvertida;

        public static Mesh EsferaInvertida
        {
            get
            {
                if (esferaInvertida != null) return esferaInvertida;
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var original = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tmp);
                esferaInvertida = new Mesh { name = "Esfera Invertida" };
                esferaInvertida.vertices = original.vertices;
                var n = original.normals; for (int i = 0; i < n.Length; i++) n[i] = -n[i];
                esferaInvertida.normals = n;
                esferaInvertida.uv = original.uv;
                var tri = original.triangles; System.Array.Reverse(tri);
                esferaInvertida.triangles = tri;
                return esferaInvertida;
            }
        }

        /// <summary>Esfera de energia que cresce (carga de Kamehameha, Rasengan, Chidori); some sozinha depois da duração.</summary>
        public static GameObject Esfera(Vector3 pos, Color cor, float tamanhoFinal, float duracao, System.Func<Vector3> segue = null, bool girar = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Esfera de Energia";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            var m = new Material(Efeitos.MaterialAditivo);
            var c = Color.Lerp(cor, Color.white, 0.55f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            go.GetComponent<Renderer>().sharedMaterial = m;
            var t = go.AddComponent<EfeitoTemporario>();
            t.Configurar(duracao);
            t.Escala(Vector3.one * 0.1f, Vector3.one * tamanhoFinal);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(go.transform, false);
            luz.color = cor; luz.range = 6f; luz.intensity = 3f;
            if (segue != null) go.AddComponent<SeguirOsso>().alvo = segue;
            var ps = Efeitos.Rajada(pos, Quaternion.identity, cor, 1, new Vector2(0.3f, 1.2f), new Vector2(0.05f, 0.12f), new Vector2(0.3f, 0.6f), ParticleSystemShapeType.Sphere, tamanhoFinal * 0.5f, 0f, true, 360f, false);
            ps.transform.SetParent(go.transform, true);
            var main = ps.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 60f; em.SetBursts(new ParticleSystem.Burst[0]);
            ps.Play();
            Object.Destroy(go, duracao + 0.1f);
            return go;
        }

        /// <summary>Domo grande visto de dentro (expansão de domínio).</summary>
        public static GameObject Domo(Vector3 centro, float raio, Color cor, float duracao, float opacidade = 0.55f)
        {
            var go = new GameObject("Domo");
            go.transform.position = centro;
            go.AddComponent<MeshFilter>().sharedMesh = EsferaInvertida;
            var mat = Efeitos.LitTransparente(new Color(cor.r, cor.g, cor.b, opacidade), 0.2f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var t = go.AddComponent<EfeitoTemporario>();
            t.Configurar(duracao);
            t.Escala(Vector3.one * raio * 0.2f, Vector3.one * raio * 2f);
            Object.Destroy(go, duracao + 0.1f);
            return go;
        }
    }
}
