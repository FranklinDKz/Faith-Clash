using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Melhora os gráficos: configura o URP (MSAA, sombras 4K, HDR, textura de profundidade/opaca), cria o perfil de pós-processamento completo
    /// (bloom, ACES, motion blur, profundidade de campo, aberração, grão, vinheta) e o material de água realista (shader FaithClash/Agua + normal map gerado).
    /// </summary>
    public static class GraficosUpgrade
    {
        const string PastaRes = "Assets/_FaithClash/Resources/Materiais";
        const string CaminhoNormal = PastaRes + "/AguaNormal.png";
        const string CaminhoAgua = PastaRes + "/Agua.mat";
        const string CaminhoPerfil = "Assets/_FaithClash/Settings/Graficos_PosProcessamento.asset";

        [MenuItem("Faith Clash/Ferramentas/Melhorar Gráficos")]
        public static void Rodar()
        {
            MaterialAgua();
            ConfigurarURP();
            PerfilPos();
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Gráficos melhorados (URP, água, pós-processamento)");
        }

        // ------------------------------------------------------------------ água
        static Texture2D NormalAgua()
        {
            Directory.CreateDirectory(PastaRes);
            var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoNormal);
            if (existente != null) return existente;
            const int n = 256;
            var rnd = new System.Random(21);
            const int ondas = 14;
            var kx = new int[ondas]; var ky = new int[ondas]; var amp = new float[ondas]; var fase = new float[ondas];
            for (int i = 0; i < ondas; i++)
            {
                kx[i] = rnd.Next(-9, 10); ky[i] = rnd.Next(-9, 10); if (kx[i] == 0 && ky[i] == 0) kx[i] = 3;
                amp[i] = 1f / Mathf.Sqrt(kx[i] * kx[i] + ky[i] * ky[i]) * (0.5f + (float)rnd.NextDouble());
                fase[i] = (float)rnd.NextDouble() * 6.28f;
            }
            var altura = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n, h = 0f;
                    for (int i = 0; i < ondas; i++) h += amp[i] * Mathf.Sin(Mathf.PI * 2f * (kx[i] * u + ky[i] * v) + fase[i]);
                    altura[y * n + x] = h;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = altura[y * n + (x + 1) % n] - altura[y * n + (x - 1 + n) % n];
                    float dy = altura[((y + 1) % n) * n + x] - altura[((y - 1 + n) % n) * n + x];
                    var nv = new Vector3(-dx * 3.2f, -dy * 3.2f, 1f).normalized;
                    tex.SetPixel(x, y, new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f));
                }
            tex.Apply();
            File.WriteAllBytes(CaminhoNormal, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CaminhoNormal);
            var imp = (TextureImporter)AssetImporter.GetAtPath(CaminhoNormal);
            imp.textureType = TextureImporterType.NormalMap; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoNormal);
        }

        public static Material MaterialAgua()
        {
            Directory.CreateDirectory(PastaRes);
            var normal = NormalAgua();
            var m = AssetDatabase.LoadAssetAtPath<Material>(CaminhoAgua);
            var shader = Shader.Find("FaithClash/Agua");
            if (shader == null) { Debug.LogWarning("[Faith Clash] Shader FaithClash/Agua não encontrado"); return m; }
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, CaminhoAgua); }
            m.shader = shader;
            // texturas do Suimono Water System (ondas calmas/turbulentas e máscara de espuma) alimentam o shader URP próprio
            const string ST = "Assets/Agua/Agua Suimono/TEXTURES/";
            Texture2D Suimono(string arq, bool normalMap)
            {
                var imp = AssetImporter.GetAtPath(ST + arq) as TextureImporter;
                if (imp == null) return null;
                if (normalMap && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
                if (!normalMap && imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; imp.sRGBTexture = false; imp.SaveAndReimport(); }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(ST + arq);
            }
            var calma = Suimono("tex_WaveCalm_normal.psd", true); var turb = Suimono("tex_WaveTurb_normal.psd", true); var espuma = Suimono("tex_foamMask.psd", false);
            m.SetTexture("_Normal", calma != null ? calma : normal);
            m.SetTexture("_Normal2", turb != null ? turb : normal);
            if (espuma != null) m.SetTexture("_FoamTex", espuma);
            m.renderQueue = 3000;
            m.SetFloat("_Brilho", 1.4f); m.SetFloat("_NormalForca", 0.5f); m.SetFloat("_NormalEscala", 0.14f); m.SetFloat("_Reflexo", 0.9f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ URP
        static void ConfigurarURP()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (a == null) continue;
                var so = new SerializedObject(a);
                void Set(string nome, int v) { var p = so.FindProperty(nome); if (p != null) p.intValue = v; }
                void SetF(string nome, float v) { var p = so.FindProperty(nome); if (p != null) p.floatValue = v; }
                Set("m_SupportsHDR", 1);
                Set("m_MSAA", 4);
                Set("m_RequireDepthTexture", 1);
                Set("m_RequireOpaqueTexture", 1);
                Set("m_MainLightShadowmapResolution", 4096);
                Set("m_ShadowCascadeCount", 4);
                Set("m_SoftShadowsSupported", 1);
                Set("m_SoftShadowQuality", 3);
                Set("m_ColorGradingMode", 1);   // HDR
                Set("m_ColorGradingLutSize", 32);
                SetF("m_ShadowDistance", 90f);
                SetF("m_ShadowNormalBias", 0.8f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
        }

        // ------------------------------------------------------------------ pós-processamento
        public static VolumeProfile PerfilPos()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoPerfil));
            AssetDatabase.DeleteAsset(CaminhoPerfil);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, CaminhoPerfil);

            var bloom = p.Add<Bloom>(true);
            bloom.intensity.Override(0.9f); bloom.threshold.Override(0.95f); bloom.scatter.Override(0.72f); bloom.highQualityFiltering.Override(true);
            bloom.tint.Override(new Color(1f, 0.96f, 0.9f));
            var tone = p.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var cor = p.Add<ColorAdjustments>(true);
            cor.contrast.Override(16f); cor.saturation.Override(12f); cor.postExposure.Override(0.1f);
            var sm = p.Add<ShadowsMidtonesHighlights>(true);
            sm.shadows.Override(new Vector4(0.88f, 0.95f, 1.12f, -0.12f)); sm.highlights.Override(new Vector4(1.1f, 1.03f, 0.94f, 0.06f));
            var bal = p.Add<WhiteBalance>(true); bal.temperature.Override(6f); bal.tint.Override(-2f);
            var vig = p.Add<Vignette>(true); vig.intensity.Override(0.3f); vig.smoothness.Override(0.55f);
            var mb = p.Add<MotionBlur>(true); mb.mode.Override(MotionBlurMode.CameraAndObjects); mb.quality.Override(MotionBlurQuality.Medium); mb.intensity.Override(0.35f); mb.clamp.Override(0.06f);
            var dof = p.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(28f); dof.gaussianEnd.Override(90f); dof.gaussianMaxRadius.Override(1.6f); dof.highQualitySampling.Override(true);
            var ca = p.Add<ChromaticAberration>(true); ca.intensity.Override(0.06f);
            var gr = p.Add<FilmGrain>(true); gr.type.Override(FilmGrainLookup.Thin1); gr.intensity.Override(0.12f); gr.response.Override(0.8f);
            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }
    }
}
