using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.UI
{
    /// <summary>Efeitos de velocidade na tela: linhas radiais de anime, aberração cromática e desfoque de movimento que crescem com a corrida, o dash e o deslize.</summary>
    public class EfeitoVelocidade : MonoBehaviour
    {
        RawImage linhas;
        RectTransform rt;
        PlayerController jogador;
        float intensidade, proximoGiro, buscar;
        Volume vol; ChromaticAberration chroma; MotionBlur borrao;
        float chromaBase, borraoBase;

        void Awake()
        {
            var go = new GameObject("Canvas de Velocidade", typeof(Canvas));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 5;
            var l = new GameObject("Linhas de velocidade", typeof(RectTransform), typeof(RawImage));
            l.transform.SetParent(go.transform, false);
            rt = (RectTransform)l.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(3400f, 3400f);
            linhas = l.GetComponent<RawImage>();
            linhas.texture = GerarTextura();
            linhas.raycastTarget = false;
            linhas.color = new Color(1f, 1f, 1f, 0f);
        }

        static Texture2D GerarTextura()
        {
            const int n = 512;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var rnd = new System.Random(3);
            const int setores = 140;
            var forca = new float[setores];
            var comp = new float[setores];
            for (int i = 0; i < setores; i++) { forca[i] = (float)rnd.NextDouble(); comp[i] = 0.35f + (float)rnd.NextDouble() * 0.5f; }
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f) * setores;
                    int s = Mathf.Clamp((int)ang, 0, setores - 1);
                    float frac = ang - s;
                    float fino = Mathf.Clamp01(1f - Mathf.Abs(frac - 0.5f) * 5f);   // linha fina no meio do setor
                    float a = forca[s] > 0.45f ? (forca[s] - 0.45f) / 0.55f : 0f;
                    float radial = Mathf.SmoothStep(comp[s] * 0.55f, comp[s] * 0.55f + 0.25f, r) * (1f - Mathf.SmoothStep(0.95f, 1f, r));
                    px[y * n + x] = new Color(1f, 1f, 1f, a * fino * radial);
                }
            t.SetPixels(px); t.Apply();
            return t;
        }

        void Update()
        {
            if (jogador == null)
            {
                if (Time.unscaledTime > buscar) { buscar = Time.unscaledTime + 1f; jogador = FindAnyObjectByType<PlayerController>(); }
            }
            float alvo = 0f;
            if (jogador != null && !EstadoJogo.MenuAberto)
            {
                float vel = Vector3.ProjectOnPlane(jogador.Velocity, Vector3.up).magnitude;
                alvo = Mathf.Clamp01((vel - 5.6f) / 2.2f);
                if (jogador.IsDashing) alvo = 1f;
                else if (jogador.IsDodging) alvo = Mathf.Max(alvo, 0.7f);
                alvo *= PerfilJogo.D.efeitoVelocidade;
            }
            intensidade = Mathf.MoveTowards(intensidade, alvo, Time.unscaledDeltaTime * (alvo > intensidade ? 4f : 2.2f));
            var cor = linhas.color; cor.a = Mathf.Clamp01(intensidade * 0.5f); linhas.color = cor;
            linhas.enabled = cor.a > 0.01f;
            if (Time.unscaledTime > proximoGiro)
            {
                proximoGiro = Time.unscaledTime + 0.045f;
                rt.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
                float e = 1f + 0.08f * Random.value;
                rt.localScale = new Vector3(e, e, 1f);
            }
            AtualizarPos();
        }

        void AtualizarPos()
        {
            if (vol == null)
            {
                foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
                    if (v.isGlobal && v.profile != null)
                    {
                        vol = v;
                        if (!v.profile.TryGet(out chroma)) chroma = v.profile.Add<ChromaticAberration>(true);
                        if (!v.profile.TryGet(out borrao)) borrao = v.profile.Add<MotionBlur>(true);
                        chromaBase = chroma.intensity.overrideState ? chroma.intensity.value : 0f;
                        borraoBase = borrao.intensity.overrideState ? borrao.intensity.value : 0f;
                        chroma.intensity.overrideState = true; borrao.intensity.overrideState = true;
                        borrao.quality.overrideState = true; borrao.quality.value = MotionBlurQuality.Low;
                        break;
                    }
                if (vol == null) return;
            }
            float k = PerfilJogo.D.posProcessamento ? intensidade : 0f;
            chroma.intensity.value = Mathf.Clamp01(chromaBase + 0.4f * k);
            borrao.intensity.value = Mathf.Clamp01(borraoBase + 0.35f * k);
        }
    }
}
