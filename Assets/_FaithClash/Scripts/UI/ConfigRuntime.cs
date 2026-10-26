using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.UI
{
    /// <summary>
    /// Cria os sistemas globais ao iniciar qualquer cena (menu, efeitos de velocidade, personalização) e aplica as configurações salvas:
    /// qualidade, resolução, vsync, FPS, render scale, sombras, pós-processamento, neblina, brilho e velocidade do jogo.
    /// </summary>
    public class ConfigRuntime : MonoBehaviour
    {
        public static readonly string[] NomesQualidade = { "Baixa", "Média", "Alta", "Ultra" };
        public static readonly string[] NomesFps = { "Livre", "30", "60", "120", "144", "240" };
        public static readonly int[] ValoresFps = { 0, 30, 60, 120, 144, 240 };

        static ConfigRuntime instancia;
        UniversalRenderPipelineAsset urp;
        float escalaOriginal, distSombraOriginal; int msaaOriginal;
        bool guardou;
        float ultimaVelocidade = -1f;
        Volume volumeBrilho; ColorAdjustmentsRef brilhoRef;

        class ColorAdjustmentsRef { public ColorAdjustments ca; }

        internal static void ResetarEstatico() { instancia = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            if (instancia != null) return;
            var go = new GameObject("Sistemas do Jogo");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<ConfigRuntime>();
            go.AddComponent<MenuJogo>();
            go.AddComponent<EfeitoVelocidade>();
            go.AddComponent<Personalizacao>();
        }

        void Awake()
        {
            PerfilJogo.Mudou += Aplicar;
            SceneManager.sceneLoaded += AoCarregarCena;
            Aplicar();
        }

        void OnDestroy() { PerfilJogo.Mudou -= Aplicar; SceneManager.sceneLoaded -= AoCarregarCena; Restaurar(); }
        void OnApplicationQuit() { Restaurar(); }

        void AoCarregarCena(Scene s, LoadSceneMode m) { volumeBrilho = null; ultimaVelocidade = -1f; Aplicar(); }

        void Restaurar()
        {
            // o asset do URP vive no projeto: devolve os valores originais para não "sujar" o arquivo
            if (guardou && urp != null)
            {
                urp.renderScale = escalaOriginal; urp.shadowDistance = distSombraOriginal; urp.msaaSampleCount = msaaOriginal;
            }
            Time.timeScale = 1f;
        }

        void Update()
        {
            // velocidade do jogo (o menu pausa por conta própria)
            if (!EstadoJogo.MenuAberto)
            {
                float v = PerfilJogo.D.velocidadeJogo;
                if (!Mathf.Approximately(Time.timeScale, v)) Time.timeScale = v;
            }
            AplicarSomLoops();
        }

        void AplicarSomLoops() { SomJogo.AtualizarVolumes(); }

        // ------------------------------------------------------------------ aplicar
        public void Aplicar()
        {
            var d = PerfilJogo.D;
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null && !guardou)
            {
                escalaOriginal = urp.renderScale; distSombraOriginal = urp.shadowDistance; msaaOriginal = urp.msaaSampleCount; guardou = true;
            }
            QualitySettings.vSyncCount = d.vsync ? 1 : 0;
            Application.targetFrameRate = d.limiteFps > 0 ? d.limiteFps : -1;
            if (urp != null)
            {
                int q = Mathf.Clamp(d.qualidade, 0, 3);
                float[] dist = { 25f, 45f, 70f, 110f };
                int[] msaa = { 1, 2, 4, 4 };
                urp.renderScale = Mathf.Clamp(d.escalaRender, 0.5f, 1.5f);
                urp.shadowDistance = d.sombras ? dist[q] : 0.01f;
                urp.msaaSampleCount = msaa[q];
            }
            QualitySettings.anisotropicFiltering = d.qualidade >= 2 ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
            QualitySettings.lodBias = new[] { 0.7f, 1f, 1.5f, 2f }[Mathf.Clamp(d.qualidade, 0, 3)];
            QualitySettings.globalTextureMipmapLimit = d.qualidade == 0 ? 1 : 0;
            RenderSettings.fog = d.neblina;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.shadows = d.sombras ? LightShadows.Soft : LightShadows.None;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null && cam.targetTexture == null) data.renderPostProcessing = d.posProcessamento;
            }
            AplicarBrilho(d.brilho);

            if (d.resLargura > 0 && (Screen.width != d.resLargura || Screen.height != d.resAltura || Screen.fullScreen != d.telaCheia))
                Screen.SetResolution(d.resLargura, d.resAltura, d.telaCheia ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            else if (Screen.fullScreen != d.telaCheia && !Application.isEditor)
                Screen.fullScreen = d.telaCheia;
            SomJogo.AtualizarVolumes();
        }

        void AplicarBrilho(float b)
        {
            if (volumeBrilho == null)
            {
                foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
                    if (v.isGlobal && v.profile != null) { volumeBrilho = v; break; }
                if (volumeBrilho == null) return;
                if (!volumeBrilho.profile.TryGet(out ColorAdjustments ca)) ca = volumeBrilho.profile.Add<ColorAdjustments>(true);
                ca.postExposure.overrideState = true;
                brilhoRef = new ColorAdjustmentsRef { ca = ca };
            }
            if (brilhoRef != null && brilhoRef.ca != null) brilhoRef.ca.postExposure.value = (b - 1f) * 2f;
        }

        // ------------------------------------------------------------------ resolução
        public static string[] ResolucoesDisponiveis()
        {
            var l = new List<string>();
            foreach (var r in Screen.resolutions)
            {
                string s = $"{r.width}x{r.height}";
                if (!l.Contains(s)) l.Add(s);
            }
            string atual = $"{Screen.width}x{Screen.height}";
            if (!l.Contains(atual)) l.Add(atual);
            return l.ToArray();
        }

        public static int IndiceResolucao(string[] lista)
        {
            var d = PerfilJogo.D;
            string alvo = d.resLargura > 0 ? $"{d.resLargura}x{d.resAltura}" : $"{Screen.width}x{Screen.height}";
            int i = System.Array.IndexOf(lista, alvo);
            return i < 0 ? lista.Length - 1 : i;
        }

        public static void DefinirResolucao(string s)
        {
            var p = s.Split('x');
            if (p.Length != 2) return;
            PerfilJogo.D.resLargura = int.Parse(p[0]); PerfilJogo.D.resAltura = int.Parse(p[1]);
            PerfilJogo.Alterado();
        }

        public static int IndiceFps(int valor) { int i = System.Array.IndexOf(ValoresFps, valor); return i < 0 ? 0 : i; }
    }
}
