using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FaithClash.Training;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Peças da caverna da Fase 1: paredes e teto de rocha (texturas Realistic Terrain), estalactites, pedras, cristais brilhantes,
    /// cogumelos, musgo, cipós, tochas com fogo, vagalumes e o fosso de espinhos.
    /// </summary>
    public static class CavernaBuilder
    {
        const string PastaTex = "Assets/Mapas/Texturas de Terreno/Realistic Terrain Textures Lite/Textures/";
        const string PastaMat = "Assets/_FaithClash/Materials/Fase1/";
        const string PastaMalha = "Assets/_FaithClash/Materials/Fase1/Malhas/";

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        internal static Transform raiz;
        static readonly List<Renderer> brilhos = new List<Renderer>();

        public static void Limpar() { cache.Clear(); brilhos.Clear(); }

        // ------------------------------------------------------------------ materiais
        static void ConfigurarTextura(string arquivo, bool normal)
        {
            string caminho = PastaTex + arquivo;
            var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (imp == null) return;
            bool mudou = false;
            if (imp.maxTextureSize != 1024) { imp.maxTextureSize = 1024; mudou = true; }
            if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; mudou = true; }
            if (imp.textureCompression != TextureImporterCompression.Compressed) { imp.textureCompression = TextureImporterCompression.Compressed; mudou = true; }
            if (mudou) imp.SaveAndReimport();
        }

        public static Material Rocha(string nome, string textura, Vector2 tiling, Color tinta, float brilho = 0.12f)
        {
            string chave = $"{nome}_{tiling.x:0.0}x{tiling.y:0.0}";
            if (cache.TryGetValue(chave, out var m)) return m;
            Directory.CreateDirectory(PastaMat);
            ConfigurarTextura(textura + ".tif", false);
            ConfigurarTextura(textura + "_N.png", true);
            string caminho = $"{PastaMat}F1_{chave}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, caminho); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PastaTex + textura + ".tif");
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(PastaTex + textura + "_N.png");
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTextureScale = tiling; }
            if (nrm != null) { m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            m.SetColor("_BaseColor", tinta);
            m.SetFloat("_Smoothness", brilho);
            EditorUtility.SetDirty(m);
            cache[chave] = m;
            return m;
        }

        public static Material Cor(string nome, Color cor, float emissao = 0f, float suave = 0.3f, float metal = 0f)
        {
            string chave = "c_" + nome;
            if (cache.TryGetValue(chave, out var m)) return m;
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}F1_cor_{nome}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, caminho); }
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", suave); m.SetFloat("_Metallic", metal);
            if (emissao > 0f) { m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; m.SetColor("_EmissionColor", cor * emissao); }
            EditorUtility.SetDirty(m);
            cache[chave] = m;
            return m;
        }

        internal static Material Particula(string nome, Color cor)
        {
            string chave = "p_" + nome;
            if (cache.TryGetValue(chave, out var m)) return m;
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}F1_part_{nome}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, caminho); }
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 1f); m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
            m.SetColor("_BaseColor", cor);
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null) m.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(m);
            cache[chave] = m;
            return m;
        }

        // ------------------------------------------------------------------ malhas
        /// <summary>Cone/pirâmide de <paramref name="lados"/> lados com base no plano y=0 e ponta em y=1. <paramref name="prisma"/> = fração de prisma antes da ponta (cristais).</summary>
        public static Mesh Cone(string nome, int lados, float prisma = 0f, float afunila = 1f)
        {
            string caminho = PastaMalha + nome + ".asset";
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (existente != null) return existente;
            Directory.CreateDirectory(PastaMalha);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float r = 0.5f;
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), b1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                if (prisma > 0f)
                {
                    Vector3 c0 = b0 * afunila + Vector3.up * prisma, c1 = b1 * afunila + Vector3.up * prisma;
                    int k = v.Count;
                    v.AddRange(new[] { b0, b1, c1, c0 }); uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, prisma), new Vector2(0, prisma) });
                    tri.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
                    int t = v.Count;
                    v.AddRange(new[] { c0, c1, Vector3.up }); uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1) });
                    tri.AddRange(new[] { t, t + 2, t + 1 });
                }
                else
                {
                    int k = v.Count;
                    v.AddRange(new[] { b0, b1, Vector3.up }); uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1) });
                    tri.AddRange(new[] { k, k + 2, k + 1 });
                }
            }
            var m = new Mesh { name = nome };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, caminho);
            return m;
        }

        internal static GameObject Malha(string nome, Mesh malha, Material mat, Vector3 pos, Vector3 escala, Quaternion rot, Transform pai = null)
        {
            var g = new GameObject(nome, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(pai != null ? pai : raiz, false);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = escala;
            g.GetComponent<MeshFilter>().sharedMesh = malha;
            g.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        internal static GameObject Prim(PrimitiveType t, string nome, Material mat, Vector3 pos, Vector3 escala, Quaternion rot, bool colisor = false, Transform pai = null)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = nome;
            g.transform.SetParent(pai != null ? pai : raiz, false);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!colisor) { var c = g.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
            return g;
        }

        // ------------------------------------------------------------------ montagem
        /// <summary>Cria o invólucro da caverna e a decoração. <paramref name="pai"/> recebe tudo; <paramref name="abismoZ0/Z1"/> delimitam o fosso de espinhos.</summary>
        public static void Montar(Transform pai, float z0, float z1, float largura, float abismoZ0, float abismoZ1, float yEspinhos)
        {
            raiz = new GameObject("Caverna").transform;
            raiz.SetParent(pai, false);
            brilhos.Clear();
            Random.InitState(1234);
            float meia = largura * 0.5f;
            float comp = z1 - z0, cz = (z0 + z1) * 0.5f;

            var rocha = Rocha("rocha", "GroundPatchCracked01", new Vector2(comp / 8f, 2.5f), new Color(0.62f, 0.64f, 0.7f), 0.2f);
            var rochaEscura = Rocha("rocha_teto", "GroundPatchCracked01", new Vector2(comp / 8f, 3f), new Color(0.36f, 0.38f, 0.44f), 0.15f);
            var pedra = Rocha("pedra", "GroundPatchCracked01", new Vector2(1.2f, 1.2f), new Color(0.5f, 0.52f, 0.58f), 0.25f);
            var terra = Rocha("terra", "Ground004", new Vector2(24f, 6f), new Color(0.55f, 0.5f, 0.48f), 0.1f);

            // paredes (rocha com normal map), teto e fundo do fosso
            foreach (float lado in new[] { -1f, 1f })
            {
                var parede = Prim(PrimitiveType.Cube, "Parede da caverna", rocha, new Vector3(lado * (meia + 4.5f), 3f, cz), new Vector3(9f, 32f, comp), Quaternion.identity);
                parede.isStatic = true;
            }
            Prim(PrimitiveType.Cube, "Teto da caverna", rochaEscura, new Vector3(0, 12.5f, cz), new Vector3(largura + 20f, 3f, comp), Quaternion.identity);
            Prim(PrimitiveType.Cube, "Fundo do fosso", terra, new Vector3(0, yEspinhos - 0.6f, (abismoZ0 + abismoZ1) * 0.5f), new Vector3(largura + 2f, 1f, abismoZ1 - abismoZ0), Quaternion.identity);
            Prim(PrimitiveType.Cube, "Fundo do túnel", rocha, new Vector3(0, 3f, z0 - 0.5f), new Vector3(largura + 18f, 24f, 1f), Quaternion.identity);
            Prim(PrimitiveType.Cube, "Fim do túnel", rocha, new Vector3(0, 3f, z1 + 0.5f), new Vector3(largura + 18f, 24f, 1f), Quaternion.identity);

            // pedras grandes nas laterais, formando contorno irregular
            var pedras = new GameObject("Pedras").transform; pedras.SetParent(raiz, false);
            for (int i = 0; i < 150; i++)
            {
                float lado = Random.value < 0.5f ? -1f : 1f;
                float s = Random.Range(1.2f, 4.2f);
                float x = lado * (meia + Random.Range(0.4f, 1.8f));
                Vector3 p = new Vector3(x, Random.Range(-0.2f, 4.5f), Random.Range(z0, z1));
                var deb = CavernaDetalhes.Pronta("RockDebris");
                if (deb != null) Malha("Pedra", deb, pedra, p - Vector3.up * s * 0.3f, new Vector3(s * 1.4f, s * Random.Range(0.8f, 1.5f), s * 1.4f), Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 360f), Random.Range(-12f, 12f)), pedras);
                else Prim(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere, "Pedra", pedra, p, new Vector3(s, s * Random.Range(0.7f, 1.6f), s * Random.Range(0.8f, 1.6f)), Random.rotation, false, pedras);
            }

            // estalactites no teto e estalagmites junto às paredes
            var cone = CavernaDetalhes.EspetoReal() ?? Cone("cone6", 6);
            var estal = new GameObject("Estalactites").transform; estal.SetParent(raiz, false);
            for (int i = 0; i < 160; i++)
            {
                float h = Random.Range(1.5f, 5.5f);
                float topo = 11f;
                Vector3 p = new Vector3(Random.Range(-meia - 1f, meia + 1f), topo, Random.Range(z0, z1));
                float r = Random.Range(0.5f, 1.4f);
                Malha("Estalactite", cone, pedra, p, new Vector3(r, -h, r), Quaternion.Euler(0, Random.Range(0, 360f), 0), estal);
            }
            for (int i = 0; i < 90; i++)
            {
                float lado = Random.value < 0.5f ? -1f : 1f;
                float h = Random.Range(0.6f, 2.8f), r = Random.Range(0.4f, 1.2f);
                Vector3 p = new Vector3(lado * (meia + Random.Range(0.1f, 1.4f)), -0.1f, Random.Range(z0, z1));
                Malha("Estalagmite", cone, pedra, p, new Vector3(r, h, r), Quaternion.Euler(0, Random.Range(0, 360f), 0), estal);
            }

            // cristais brilhantes (cores vivas) com luz
            var cristalMalha = Cone("cristal6", 6, 0.62f, 0.8f);
            var cores = new[] { new Color(0.1f, 0.9f, 1f), new Color(0.75f, 0.3f, 1f), new Color(0.2f, 1f, 0.6f), new Color(1f, 0.35f, 0.7f) };
            var grupoCristal = new GameObject("Cristais").transform; grupoCristal.SetParent(raiz, false);
            for (int g = 0; g < 26; g++)
            {
                float lado = g % 2 == 0 ? -1f : 1f;
                var c = cores[g % cores.Length];
                var mat = Cor("cristal_" + (g % cores.Length), c, 2.2f, 0.85f);
                float z = z0 + 6f + g * (comp - 12f) / 25f + Random.Range(-2f, 2f);
                Vector3 baseP = new Vector3(lado * (meia + Random.Range(-0.1f, 0.9f)), Random.Range(0f, 0.2f), z);
                int n = Random.Range(3, 6);
                for (int k = 0; k < n; k++)
                {
                    float h = Random.Range(0.6f, 1.8f), r = Random.Range(0.18f, 0.42f);
                    var q = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0, 360f), Random.Range(-25f, 25f) + (lado > 0 ? 15f : -15f));
                    var go = Malha("Cristal", cristalMalha, mat, baseP + new Vector3(Random.Range(-0.4f, 0.4f) * 1f, 0, Random.Range(-0.4f, 0.4f)), new Vector3(r, h, r), q, grupoCristal);
                    brilhos.Add(go.GetComponent<Renderer>());
                }
                if (g % 2 == 0)
                {
                    var luz = new GameObject("Luz de cristal").AddComponent<Light>();
                    luz.transform.SetParent(grupoCristal, false); luz.transform.position = baseP + Vector3.up * 1.2f + new Vector3(-lado * 0.8f, 0, 0);
                    luz.color = c; luz.range = 9f; luz.intensity = 2.2f;
                }
            }
            // cristais no teto, só decoração
            for (int g = 0; g < 18; g++)
            {
                var c = cores[g % cores.Length];
                var mat = Cor("cristal_" + (g % cores.Length), c, 2.2f, 0.85f);
                Vector3 p = new Vector3(Random.Range(-meia, meia), 11f, Random.Range(z0 + 4f, z1 - 4f));
                for (int k = 0; k < 3; k++)
                {
                    float h = Random.Range(0.5f, 1.4f), r = Random.Range(0.16f, 0.34f);
                    var go = Malha("Cristal do teto", cristalMalha, mat, p + new Vector3(Random.Range(-0.4f, 0.4f), 0, Random.Range(-0.4f, 0.4f)), new Vector3(r, -h, r), Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360f), Random.Range(-15f, 15f)), grupoCristal);
                    brilhos.Add(go.GetComponent<Renderer>());
                }
            }

            // vegetação: musgo, cogumelos luminosos e cipós
            var veg = new GameObject("Vegetação").transform; veg.SetParent(raiz, false);
            var musgo = Rocha("musgo", "Ground005", new Vector2(1.5f, 1.5f), new Color(0.45f, 0.85f, 0.4f), 0.05f);
            var talo = Cor("talo", new Color(0.85f, 0.8f, 0.7f), 0f, 0.2f);
            var chapeus = new[] { Cor("cogumelo_a", new Color(0.2f, 0.7f, 1f), 1.8f, 0.6f), Cor("cogumelo_b", new Color(1f, 0.4f, 0.8f), 1.8f, 0.6f), Cor("cogumelo_c", new Color(0.5f, 1f, 0.4f), 1.6f, 0.6f) };
            var cipo = Cor("cipo", new Color(0.18f, 0.5f, 0.2f), 0f, 0.2f);
            for (int i = 0; i < 70; i++)
            {
                float lado = Random.value < 0.5f ? -1f : 1f;
                float x = lado * (meia + Random.Range(-0.2f, 1.6f));
                float z = Random.Range(z0 + 2f, z1 - 2f);
                Prim(PrimitiveType.Sphere, "Musgo", musgo, new Vector3(x, 0.05f, z), new Vector3(Random.Range(1.4f, 3.2f), Random.Range(0.35f, 0.7f), Random.Range(1.4f, 3.2f)), Quaternion.Euler(0, Random.Range(0, 360f), 0), false, veg);
            }
            for (int i = 0; i < 60; i++)
            {
                float lado = Random.value < 0.5f ? -1f : 1f;
                Vector3 p = new Vector3(lado * (meia + Random.Range(-0.3f, 1.2f)), 0f, Random.Range(z0 + 2f, z1 - 2f));
                int tipo = Random.Range(0, 3);
                int n = Random.Range(2, 5);
                for (int k = 0; k < n; k++)
                {
                    float h = Random.Range(0.18f, 0.5f);
                    Vector3 q = p + new Vector3(Random.Range(-0.35f, 0.35f), 0, Random.Range(-0.35f, 0.35f));
                    Prim(PrimitiveType.Cylinder, "Talo", talo, q + Vector3.up * h * 0.5f, new Vector3(0.05f, h * 0.5f, 0.05f), Quaternion.identity, false, veg);
                    var cap = Prim(PrimitiveType.Sphere, "Chapéu", chapeus[tipo], q + Vector3.up * h, new Vector3(h * 0.9f, h * 0.45f, h * 0.9f), Quaternion.identity, false, veg);
                    brilhos.Add(cap.GetComponent<Renderer>());
                }
                if (i % 4 == 0)
                {
                    var luz = new GameObject("Luz do cogumelo").AddComponent<Light>();
                    luz.transform.SetParent(veg, false); luz.transform.position = p + Vector3.up * 0.7f;
                    luz.color = chapeus[tipo].GetColor("_BaseColor"); luz.range = 4.5f; luz.intensity = 1.2f;
                }
            }
            for (int i = 0; i < 90; i++)
            {
                float comprimento = Random.Range(2f, 6f);
                Vector3 p = new Vector3(Random.Range(-meia - 0.5f, meia + 0.5f), 11f, Random.Range(z0 + 2f, z1 - 2f));
                var c = Prim(PrimitiveType.Cylinder, "Cipó", cipo, p, new Vector3(0.05f, comprimento * 0.5f, 0.05f), Quaternion.identity, false, veg);
                // pivô no topo: filho deslocado
                var pivo = new GameObject("Pivô do cipó").transform;
                pivo.SetParent(veg, false); pivo.position = p;
                c.transform.SetParent(pivo, true); c.transform.localPosition = new Vector3(0, -comprimento * 0.5f, 0);
                pivo.gameObject.AddComponent<Balanco>();
                // folhas
                for (int k = 0; k < 4; k++)
                    Prim(PrimitiveType.Sphere, "Folha", musgo, pivo.position + new Vector3(0, -comprimento * (0.25f + 0.2f * k), 0), new Vector3(0.35f, 0.08f, 0.2f), Quaternion.Euler(0, Random.Range(0, 360f), Random.Range(-30f, 30f)), false, pivo);
            }

            // fosso de espinhos
            Espinhos(abismoZ0, abismoZ1, largura + 1f, yEspinhos);

            // partículas: vagalumes e poeira
            Vagalumes(pai, z0, z1, largura);

            var amb = new GameObject("Ambiente da Caverna").AddComponent<AmbienteCaverna>();
            amb.transform.SetParent(pai, false);
            var so = new SerializedObject(amb);
            var arr = so.FindProperty("cristais");
            arr.arraySize = brilhos.Count;
            for (int i = 0; i < brilhos.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = brilhos[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            CavernaDetalhes.Aplicar(pai, z0, z1, meia, yEspinhos, new[] { 88.5f, 95f, 101.5f });
        }

        // ------------------------------------------------------------------ espinhos
        static void Espinhos(float z0, float z1, float largura, float yBase)
        {
            var real = CavernaDetalhes.EspetoReal();
            var cone = real != null ? real : Cone("cone4", 4);
            var ferro = Cor("espinho", new Color(0.62f, 0.63f, 0.7f), 0f, 0.75f, 0.9f);
            var ci = new List<CombineInstance>();
            float passo = 0.55f;
            for (float x = -largura * 0.5f; x <= largura * 0.5f; x += passo)
                for (float z = z0; z <= z1; z += passo)
                {
                    float h = Random.Range(1.3f, 1.9f), r = Random.Range(0.28f, 0.42f);
                    var pos = new Vector3(x + Random.Range(-0.15f, 0.15f), yBase, z + Random.Range(-0.15f, 0.15f));
                    ci.Add(new CombineInstance { mesh = cone, transform = Matrix4x4.TRS(pos, Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0, 360f), Random.Range(-4f, 4f)), new Vector3(real != null ? r * 2.2f : r, h, real != null ? r * 2.2f : r)) });
                }
            var m = new Mesh { name = "Espinhos", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.CombineMeshes(ci.ToArray(), true, true);
            m.RecalculateBounds();
            Directory.CreateDirectory(PastaMalha);
            AssetDatabase.CreateAsset(m, PastaMalha + "Espinhos_fosso.asset");
            var g = Malha("Espinhos", m, ferro, Vector3.zero, Vector3.one, Quaternion.identity);
            // luz avermelhada sinistra sobre os espinhos
            for (float z = z0 + 3f; z < z1; z += 9f)
            {
                var luz = new GameObject("Luz dos espinhos").AddComponent<Light>();
                luz.transform.SetParent(raiz, false); luz.transform.position = new Vector3(0, yBase + 1.5f, z);
                luz.color = new Color(1f, 0.25f, 0.2f); luz.range = 8f; luz.intensity = 1.8f;
            }
        }

        // ------------------------------------------------------------------ partículas
        static void Vagalumes(Transform pai, float z0, float z1, float largura)
        {
            var go = new GameObject("Vagalumes");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(0, 3.5f, (z0 + z1) * 0.5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = 9f; main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 1f, 0.9f, 0.9f), new Color(1f, 0.9f, 0.5f, 0.9f));
            main.maxParticles = 700; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 80f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(largura, 6f, z1 - z0);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                       new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = Particula("vagalume", Color.white);
        }

        // ------------------------------------------------------------------ tocha com fogo
        public static void Tocha(Transform pai, Vector3 pos)
        {
            var pote = Cor("tocha_pote", new Color(0.18f, 0.14f, 0.12f), 0f, 0.4f, 0.6f);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Braseiro";
            b.transform.SetParent(pai, false);
            b.transform.position = pos + Vector3.up * 0.6f; b.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
            b.GetComponent<Renderer>().sharedMaterial = pote;
            Object.DestroyImmediate(b.GetComponent<Collider>());
            var bacia = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bacia.name = "Bacia"; bacia.transform.SetParent(b.transform, false);
            bacia.transform.localPosition = new Vector3(0, 1f, 0); bacia.transform.localScale = new Vector3(1.8f, 0.22f, 1.8f);
            bacia.GetComponent<Renderer>().sharedMaterial = pote;
            Object.DestroyImmediate(bacia.GetComponent<Collider>());

            var fogo = new GameObject("Fogo");
            fogo.transform.SetParent(pai, false);
            fogo.transform.position = pos + Vector3.up * 1.35f;
            var ps = fogo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 0.8f), new Color(1f, 0.25f, 0.05f, 0.8f));
            main.gravityModifier = -0.15f; main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 28f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.12f;
            var tam = ps.sizeOverLifetime; tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0), new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.5f), new GradientColorKey(new Color(0.3f, 0.05f, 0.02f), 1) },
                       new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            fogo.GetComponent<ParticleSystemRenderer>().sharedMaterial = Particula("fogo", Color.white);

            var luz = new GameObject("Luz da tocha").AddComponent<Light>();
            luz.transform.SetParent(pai, false); luz.transform.position = pos + Vector3.up * 1.9f;
            luz.color = new Color(1f, 0.62f, 0.3f); luz.range = 11f; luz.intensity = 2.6f;
            luz.gameObject.AddComponent<LuzOscilante>();
        }

        // ------------------------------------------------------------------ clima
        public static void ClimaDeCaverna()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.4f, 0.55f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.07f, 0.1f);
            RenderSettings.fogDensity = 0.011f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { l.intensity = 0.6f; l.color = new Color(0.55f, 0.65f, 1f); l.shadows = LightShadows.None; }
        }
    }
}
