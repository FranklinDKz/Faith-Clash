using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Menu: Faith Clash > Ferramentas > Converter Materiais para URP.
    /// Pacotes antigos da Asset Store usam o shader "Standard" ou shaders legados de partículas, que ficam rosa no URP.
    /// Esta ferramenta troca por "Universal Render Pipeline/Lit" / "Particles/Unlit" mantendo texturas, cores, normal, metal, emissão e transparência.
    /// </summary>
    public static class ConversorMateriais
    {
        [MenuItem("Faith Clash/Ferramentas/Converter Materiais para URP")]
        public static void ConverterTudo()
        {
            int n = 0, pulados = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                if (caminho.StartsWith("Assets/_FaithClash")) continue;
                var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
                if (m == null || m.shader == null) continue;
                if (Converter(m)) n++; else pulados++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Faith Clash] Materiais convertidos para URP: {n} (já compatíveis ou sem conversão: {pulados})");
        }

        static bool Converter(Material m)
        {
            string s = m.shader.name;
            bool standard = s == "Standard" || s == "Standard (Specular setup)" || s.StartsWith("Legacy Shaders/Diffuse") || s.StartsWith("Legacy Shaders/Bumped") || s == "Nature/Tree Soft Occlusion Leaves" || s.StartsWith("Mobile/Diffuse") || s.StartsWith("Mobile/Bumped");
            bool particula = s.StartsWith("Legacy Shaders/Particles") || s.StartsWith("Particles/") && !s.Contains("Universal") || s.StartsWith("Mobile/Particles");
            if (!standard && !particula && !(m.shader.name == "Hidden/InternalErrorShader")) return false;
            Shader destino = Shader.Find(particula ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit");
            if (destino == null) return false;

            Texture tex = Tex(m, "_MainTex"), normal = Tex(m, "_BumpMap"), metal = Tex(m, "_MetallicGlossMap"), emissao = Tex(m, "_EmissionMap"), oclusao = Tex(m, "_OcclusionMap");
            Vector2 esc = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one, off = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
            Color cor = m.HasProperty("_Color") ? m.GetColor("_Color") : m.HasProperty("_TintColor") ? m.GetColor("_TintColor") : Color.white;
            Color emi = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float metalico = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
            float suave = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.3f;
            float escalaNormal = m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f;
            float modo = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;   // 0 opaco, 1 recorte, 2 fade, 3 transparente
            float corte = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
            bool aditivo = particula && (s.Contains("Additive") || s.Contains("Add"));

            m.shader = destino;
            m.SetColor("_BaseColor", cor);
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", esc); m.SetTextureOffset("_BaseMap", off); }
            if (!particula)
            {
                m.SetFloat("_Smoothness", suave);
                m.SetFloat("_Metallic", metalico);
                if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", escalaNormal); m.EnableKeyword("_NORMALMAP"); }
                if (metal != null) { m.SetTexture("_MetallicGlossMap", metal); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                if (oclusao != null) { m.SetTexture("_OcclusionMap", oclusao); }
                if (emissao != null || emi.maxColorComponent > 0.01f)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emi);
                    if (emissao != null) m.SetTexture("_EmissionMap", emissao);
                }
                if (modo == 1f) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", corte); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
                else if (modo >= 2f) Transparente(m, false);
            }
            else Transparente(m, aditivo);
            EditorUtility.SetDirty(m);
            return true;
        }

        static Texture Tex(Material m, string nome) => m.HasProperty(nome) ? m.GetTexture(nome) : null;

        static void Transparente(Material m, bool aditivo)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", aditivo ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
    }
}
