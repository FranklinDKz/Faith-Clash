using UnityEngine;
using UnityEngine.Rendering;

namespace FaithClash.Combat
{
    /// <summary>Efeitos visuais gerados por código (partículas, luzes, rochas, esferas) — sem assets externos.</summary>
    public static class Efeitos
    {
        static Material matAditivo, matAlfa;
        static Mesh malhaEspinho;

        static Texture2D TexturaBrilho => Resources.Load<Texture2D>("HUD/brilho");

        public static Material MaterialAditivo
        {
            get
            {
                if (matAditivo == null) matAditivo = CriarParticula(true);
                return matAditivo;
            }
        }

        public static Material MaterialAlfa
        {
            get
            {
                if (matAlfa == null) matAlfa = CriarParticula(false);
                return matAlfa;
            }
        }

        static Material CriarParticula(bool aditivo)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(sh);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (aditivo) m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)RenderQueue.Transparent;
            var tex = TexturaBrilho;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            return m;
        }

        /// <summary>Material Lit transparente (água, vento).</summary>
        public static Material LitTransparente(Color cor, float suavidade = 0.9f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", suavidade);
            return m;
        }

        public static Material LitOpaco(Color cor, float suavidade = 0.2f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", suavidade);
            return m;
        }

        /// <summary>Explosão de partículas genérica.</summary>
        /// <summary>Partículas de rapidez: riscos finos azul-brancos que ficam para trás do movimento (dash, avanço, parkour).</summary>
        public static void Rapidez(Vector3 pos, Vector3 dir, float escala = 1f)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            var ps = Rajada(pos, Quaternion.LookRotation(-dir.normalized), new Color(0.65f, 0.85f, 1f, 0.9f), (int)(46 * escala), new Vector2(7f, 16f),
                new Vector2(0.025f, 0.06f), new Vector2(0.14f, 0.3f), ParticleSystemShapeType.Cone, 0.45f * escala, 0f, true);
            var sh = ps.shape; sh.angle = 5f; sh.radius = 0.55f * escala;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 7f; r.velocityScale = 0.06f;
        }

        public static ParticleSystem Rajada(Vector3 pos, Quaternion rot, Color cor, int quantidade, Vector2 velocidade,
            Vector2 tamanho, Vector2 vida, ParticleSystemShapeType forma = ParticleSystemShapeType.Sphere,
            float raio = 0.3f, float gravidade = 0f, bool aditivo = true, float arco = 360f, bool autoDestruir = true)
        {
            var go = new GameObject("Efeito");
            go.transform.SetPositionAndRotation(pos, rot);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidade.x, velocidade.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tamanho.x, tamanho.y);
            main.startColor = new ParticleSystem.MinMaxGradient(cor, Color.Lerp(cor, Color.white, 0.5f));
            main.gravityModifier = gravidade;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(quantidade, 10);

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)quantidade) });

            var sh = ps.shape;
            sh.shapeType = forma;
            sh.radius = raio;
            sh.arc = arco;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = aditivo ? MaterialAditivo : MaterialAlfa;

            ps.Play();
            if (autoDestruir) Object.Destroy(go, vida.y + 0.5f);
            return ps;
        }

        /// <summary>Luz pontual que some sozinha.</summary>
        public static void Clarao(Vector3 pos, Color cor, float intensidade = 6f, float alcance = 8f, float duracao = 0.4f)
        {
            var go = new GameObject("Clarao");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = cor;
            l.range = alcance;
            l.intensity = intensidade;
            var t = go.AddComponent<EfeitoTemporario>();
            t.Configurar(duracao, l);
        }

        public static Mesh MalhaEspinho
        {
            get
            {
                if (malhaEspinho != null) return malhaEspinho;
                const int lados = 6;
                var v = new Vector3[lados * 3];
                var tris = new int[lados * 3];
                for (int i = 0; i < lados; i++)
                {
                    float a0 = i * Mathf.PI * 2 / lados, a1 = (i + 1) * Mathf.PI * 2 / lados;
                    float r0 = (i % 2 == 0) ? 0.5f : 0.38f, r1 = ((i + 1) % 2 == 0) ? 0.5f : 0.38f;
                    v[i * 3] = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                    v[i * 3 + 1] = new Vector3(0.05f, 1f, 0.03f);
                    v[i * 3 + 2] = new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1);
                    tris[i * 3] = i * 3; tris[i * 3 + 1] = i * 3 + 1; tris[i * 3 + 2] = i * 3 + 2;
                }
                malhaEspinho = new Mesh { name = "Espinho", vertices = v, triangles = tris };
                malhaEspinho.RecalculateNormals();
                malhaEspinho.RecalculateBounds();
                return malhaEspinho;
            }
        }
    }
}
