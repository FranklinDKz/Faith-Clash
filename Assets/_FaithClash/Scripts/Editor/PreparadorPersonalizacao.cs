using System.IO;
using UnityEditor;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>Prepara os assets da personalização: materiais de cabelo (recorte de alfa) e a importação dos cabelos/barba do pacote.</summary>
    public static class PreparadorPersonalizacao
    {
        const string Pasta = "Assets/_FaithClash/Resources/Cabelos";
        const string Tex = "Assets/_FaithClash/Art/Characters/Jogador/Modelo/Texturas/";

        [MenuItem("Faith Clash/Ferramentas/Preparar Personalização")]
        public static void Preparar()
        {
            AssetDatabase.Refresh();
            foreach (var n in new[] { "T_Hair_1_Normal.png", "T_Hair_2_Normal.png" })
            {
                var ti = AssetImporter.GetAtPath(Tex + n) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
            Material1("Cabelo1", "T_Hair_1");
            Material1("Cabelo2", "T_Hair_2");
            foreach (var f in Directory.GetFiles(Pasta, "*.fbx"))
            {
                var imp = AssetImporter.GetAtPath(f.Replace('\\', '/')) as ModelImporter;
                if (imp == null) continue;
                imp.importAnimation = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Personalização preparada");
        }

        static void Material1(string nome, string textura)
        {
            string caminho = $"{Pasta}/{nome}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, caminho); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + textura + "_BaseColor.png"));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + textura + "_Normal.png"));
            m.EnableKeyword("_NORMALMAP");
            m.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.1f));
            m.SetFloat("_Smoothness", 0.35f); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);
            m.renderQueue = 2450;
            EditorUtility.SetDirty(m);
        }
    }
}
