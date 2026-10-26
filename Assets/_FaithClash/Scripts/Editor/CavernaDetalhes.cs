using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FaithClash.Training;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Acabamento da caverna da Fase 1: pedras e espetos modelados (RockDebris / RockSpike / IceShard do pacote de partículas), colunas, pilares sob as
    /// plataformas, poças, madeiramento de mina com lanternas, grama, cachoeiras, névoa baixa, feixes de luz e o dojo do Sensei (tatame, velas, suporte de katanas, estandartes).
    /// </summary>
    public static class CavernaDetalhes
    {
        const string PastaMalha = "Assets/_FaithClash/Materials/Fase1/Malhas/";

        static Mesh MalhaDe(string nome)
        {
            var g = AssetDatabase.FindAssets(nome + " t:Model").FirstOrDefault(x => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(x)) == nome);
            if (g == null) return null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g))) if (o is Mesh m && m.vertexCount > 8) return m;
            return null;
        }

        /// <summary>Malha do pacote já normalizada (base em y=0, altura 1, largura 1) e salva como asset.</summary>
        public static Mesh Pronta(string nome)
        {
            string caminho = PastaMalha + "Norm_" + nome + ".asset";
            var ja = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (ja != null) return ja;
            var m = MalhaDe(nome);
            if (m == null) return null;
            Directory.CreateDirectory(PastaMalha);
            var novo = new Mesh { name = "Norm_" + nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            novo.CombineMeshes(new[] { new CombineInstance { mesh = m, transform = Normaliza(m) } }, true, true);
            novo.RecalculateBounds();
            AssetDatabase.CreateAsset(novo, caminho);
            return novo;
        }

        /// <summary>Matriz que leva a malha para base em y=0, altura 1 e largura 1 (centrada).</summary>
        static Matrix4x4 Normaliza(Mesh m)
        {
            var b = m.bounds;
            float l = Mathf.Max(b.size.x, b.size.z, 0.0001f);
            return Matrix4x4.Scale(new Vector3(1f / l, 1f / Mathf.Max(b.size.y, 0.0001f), 1f / l)) * Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y, -b.center.z));
        }

        public static Mesh EspetoReal() => Pronta("RockSpike");

        // ------------------------------------------------------------------ acabamento geral
        public static void Aplicar(Transform raiz, float z0, float z1, float meia, float yEspinhos, float[] pilaresZ)
        {
            Random.InitState(777);
            var rocha = CavernaBuilder.Rocha("rocha", "GroundPatchCracked01", new Vector2(2f, 2f), new Color(0.55f, 0.57f, 0.64f), 0.22f);
            var rochaMusgo = CavernaBuilder.Rocha("rocha_musgo", "Ground005", new Vector2(1f, 1f), new Color(0.5f, 0.8f, 0.45f), 0.1f);
            var pai = new GameObject("Acabamento").transform; pai.SetParent(raiz, false);
            CavernaBuilder.raiz = pai;

            var detrito = Pronta("RockDebris");
            Mesh cristalReal = null;   // o IceShard do pacote é uma lasca plana: ficam só os cristais hexagonais

            // 1) pedras de verdade espalhadas na beira da pista e nas paredes
            if (detrito != null)
                for (int i = 0; i < 160; i++)
                {
                    float lado = Random.value < 0.5f ? -1f : 1f;
                    float tam = Random.Range(0.5f, 2.6f);
                    var pos = new Vector3(lado * (meia + Random.Range(-0.1f, 2.4f)), Random.Range(-0.05f, 0.1f), Random.Range(z0, z1));
                    var g = CavernaBuilder.Malha("Pedra detalhada", detrito, Random.value < 0.3f ? rochaMusgo : rocha, pos, new Vector3(tam, tam * Random.Range(0.45f, 0.9f), tam), Random.rotation, pai);
                    g.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360f), Random.Range(-15f, 15f));
                }

            // 2) colunas de pedra ligando o chão ao teto, junto das paredes
            for (float z = z0 + 18f; z < z1 - 12f; z += Random.Range(30f, 44f))
                foreach (float lado in new[] { -1f, 1f })
                {
                    if (Random.value < 0.35f) continue;
                    float x = lado * (meia + 1.4f);
                    float r = Random.Range(0.9f, 1.5f);
                    var col = CavernaBuilder.Prim(PrimitiveType.Cylinder, "Coluna de pedra", rocha, new Vector3(x, 5.5f, z), new Vector3(r, 5.6f, r), Quaternion.identity, false, pai);
                    col.transform.rotation = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(0, 360f), Random.Range(-2f, 2f));
                    var cone = CavernaBuilder.Cone("cone8", 8);
                    CavernaBuilder.Malha("Base da coluna", cone, rocha, new Vector3(x, -0.05f, z), new Vector3(r * 2.4f, 1.6f, r * 2.4f), Quaternion.identity, pai);
                    CavernaBuilder.Malha("Topo da coluna", cone, rocha, new Vector3(x, 11.1f, z), new Vector3(r * 2.6f, -2.2f, r * 2.6f), Quaternion.identity, pai);
                }

            // 3) pilares de pedra sustentando as plataformas sobre o fosso
            foreach (float zc in pilaresZ)
            {
                float yTopo = -1.2f;
                float h = yTopo - (yEspinhos - 0.6f);
                var p = CavernaBuilder.Prim(PrimitiveType.Cylinder, "Pilar de pedra", rocha, new Vector3(0f, yTopo - h * 0.5f, zc), new Vector3(1.7f, h * 0.5f, 1.7f), Quaternion.identity, false, pai);
                var cone = CavernaBuilder.Cone("cone8", 8);
                CavernaBuilder.Malha("Base do pilar", cone, rocha, new Vector3(0f, yEspinhos - 0.6f, zc), new Vector3(4.2f, 1.2f, 4.2f), Quaternion.identity, pai);
                if (detrito != null)
                    for (int k = 0; k < 5; k++)
                        CavernaBuilder.Malha("Pedra do pilar", detrito, rocha, new Vector3(Random.Range(-1.2f, 1.2f), yEspinhos - 0.5f, zc + Random.Range(-1.2f, 1.2f)), Vector3.one * Random.Range(0.5f, 1.2f), Quaternion.Euler(0, Random.Range(0, 360f), 0), pai);
            }

            // 4) cristais de gelo/energia modelados (IceShard), em grupos nas paredes
            if (cristalReal != null)
            {
                var cores = new[] { new Color(0.1f, 0.9f, 1f), new Color(0.75f, 0.3f, 1f), new Color(0.2f, 1f, 0.6f), new Color(1f, 0.35f, 0.7f) };
                for (int g = 0; g < 22; g++)
                {
                    var c = cores[g % cores.Length];
                    var mat = CavernaBuilder.Cor("cristal_" + (g % cores.Length), c, 2.2f, 0.85f);
                    float lado = g % 2 == 0 ? 1f : -1f;
                    float z = z0 + 10f + g * (z1 - z0 - 20f) / 21f + Random.Range(-3f, 3f);
                    var baseP = new Vector3(lado * (meia + Random.Range(0.2f, 1.2f)), Random.Range(0f, 3.5f), z);
                    int n = Random.Range(3, 6);
                    for (int k = 0; k < n; k++)
                    {
                        float h = Random.Range(0.8f, 2.4f);
                        var go = CavernaBuilder.Malha("Cristal modelado", cristalReal, mat, baseP + Random.insideUnitSphere * 0.3f, new Vector3(h * 0.45f, h, h * 0.45f), Quaternion.identity, pai);
                        go.transform.rotation = Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0, 360f), Random.Range(-35f, 35f) + (lado > 0 ? 30f : -30f));
                    }
                }
            }

            // 5) poças d'água (chão molhado e espelhado)
            var agua = GraficosUpgrade.MaterialAgua() ?? CavernaBuilder.Cor("poca", new Color(0.04f, 0.1f, 0.2f), 0f, 0.97f);
            for (int i = 0; i < 26; i++)
            {
                float z = Random.Range(z0 + 6f, z1 - 6f);
                if (z > 65f && z < 112f) continue;   // não nos vãos
                var pc = CavernaBuilder.Prim(PrimitiveType.Cylinder, "Poça", agua, new Vector3(Random.Range(-meia + 0.8f, meia - 0.8f), 0.012f, z), new Vector3(Random.Range(0.8f, 2.4f), 0.002f, Random.Range(0.6f, 1.8f)), Quaternion.Euler(0, Random.Range(0, 180f), 0), false, pai);
            }

            // 6) madeiramento de mina com lanternas penduradas
            var madeira = CavernaBuilder.Cor("madeira", new Color(0.3f, 0.19f, 0.1f), 0f, 0.15f);
            var ferro = CavernaBuilder.Cor("ferro_velho", new Color(0.18f, 0.18f, 0.2f), 0f, 0.5f, 0.8f);
            var lampada = CavernaBuilder.Cor("lampada", new Color(1f, 0.7f, 0.3f), 2.5f, 0.3f);
            for (float z = z0 + 22f; z < z1 - 14f; z += 36f)
            {
                if (z > 64f && z < 112f) continue;
                float alt = 5.2f;
                foreach (float lado in new[] { -1f, 1f })
                {
                    CavernaBuilder.Prim(PrimitiveType.Cube, "Poste de madeira", madeira, new Vector3(lado * (meia - 0.35f), alt * 0.5f, z), new Vector3(0.45f, alt, 0.45f), Quaternion.Euler(0, Random.Range(-4f, 4f), 0), false, pai);
                    CavernaBuilder.Prim(PrimitiveType.Cube, "Escora", madeira, new Vector3(lado * (meia - 1.0f), alt - 0.5f, z), new Vector3(0.28f, 1.6f, 0.28f), Quaternion.Euler(0, 0, lado * 45f), false, pai);
                }
                CavernaBuilder.Prim(PrimitiveType.Cube, "Viga de madeira", madeira, new Vector3(0f, alt + 0.2f, z), new Vector3(meia * 2f - 0.2f, 0.5f, 0.5f), Quaternion.identity, false, pai);
                CavernaBuilder.Prim(PrimitiveType.Cube, "Faixa de ferro", ferro, new Vector3(0f, alt + 0.2f, z), new Vector3(0.6f, 0.56f, 0.56f), Quaternion.identity, false, pai);
                // lanterna pendurada por corrente
                CavernaBuilder.Prim(PrimitiveType.Cylinder, "Corrente", ferro, new Vector3(0f, alt - 0.45f, z), new Vector3(0.03f, 0.55f, 0.03f), Quaternion.identity, false, pai);
                var lamp = CavernaBuilder.Prim(PrimitiveType.Cube, "Lanterna", lampada, new Vector3(0f, alt - 1.15f, z), new Vector3(0.36f, 0.5f, 0.36f), Quaternion.identity, false, pai);
                var luz = new GameObject("Luz da lanterna").AddComponent<Light>();
                luz.transform.SetParent(pai, false); luz.transform.position = lamp.transform.position;
                luz.color = new Color(1f, 0.7f, 0.35f); luz.range = 12f; luz.intensity = 2.2f;
                luz.gameObject.AddComponent<LuzOscilante>();
            }

            // 7) tufos de grama nas bordas (lâminas finas combinadas)
            var folha = CavernaBuilder.Cone("cone3", 3);
            var matGrama = CavernaBuilder.Cor("grama", new Color(0.25f, 0.65f, 0.25f), 0.25f, 0.2f);
            var ci = new List<CombineInstance>();
            for (int i = 0; i < 1300; i++)
            {
                float lado = Random.value < 0.5f ? -1f : 1f;
                float z = Random.Range(z0, z1);
                if (z > 74f && z < 108f && Mathf.Abs(Random.Range(-1f, 1f)) < 2f) continue;
                var p = new Vector3(lado * (meia - Random.Range(-0.4f, 1.8f)), 0f, z) + Vector3.right * Random.Range(-0.3f, 0.3f);
                float h = Random.Range(0.25f, 0.8f);
                ci.Add(new CombineInstance { mesh = folha, transform = Matrix4x4.TRS(p, Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(0, 360f), Random.Range(-18f, 18f)), new Vector3(0.06f, h, 0.06f)) });
            }
            var mg = new Mesh { name = "Grama", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mg.CombineMeshes(ci.ToArray(), true, true);
            Directory.CreateDirectory(PastaMalha);
            AssetDatabase.CreateAsset(mg, PastaMalha + "Grama_caverna.asset");
            CavernaBuilder.Malha("Grama", mg, matGrama, Vector3.zero, Vector3.one, Quaternion.identity, pai);

            // 8) cachoeiras nas paredes
            var cach = new List<Vector3>();
            foreach (var (z, lado) in new[] { (46f, -1f), (140f, 1f), (118f, -1f) })
            {
                var pos = new Vector3(lado * (meia + 1.2f), 11f, z);
                Cachoeira(pai, pos, 11f);
                cach.Add(new Vector3(lado * (meia + 0.8f), 1.5f, z));
            }
            var amb = Object.FindFirstObjectByType<AmbienteCaverna>();
            if (amb != null)
            {
                var so = new SerializedObject(amb);
                var arr = so.FindProperty("cachoeiras");
                arr.arraySize = cach.Count;
                for (int i = 0; i < cach.Count; i++) arr.GetArrayElementAtIndex(i).vector3Value = cach[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 9) névoa baixa e feixes de luz
            Nevoa(pai, z0, z1, meia);
            foreach (float z in new[] { 30f, 95f, 150f })
                Feixe(pai, new Vector3(Random.Range(-2f, 2f), 11f, z));
        }

        static void Cachoeira(Transform pai, Vector3 topo, float altura)
        {
            var go = new GameObject("Cachoeira");
            go.transform.SetParent(pai, false); go.transform.position = topo;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.6f; main.startSpeed = 0.3f; main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.9f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.7f));
            main.gravityModifier = 1.1f; main.maxParticles = 1500; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 420f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.2f, 0.1f, 1.8f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CavernaBuilder.Particula("agua_queda", Color.white);

            var baseG = new GameObject("Respingo");
            baseG.transform.SetParent(go.transform, false); baseG.transform.position = new Vector3(topo.x, 0.1f, topo.z);
            var pb = baseG.AddComponent<ParticleSystem>();
            var mb = pb.main; mb.startLifetime = 0.9f; mb.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f); mb.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            mb.startColor = new Color(0.85f, 0.95f, 1f, 0.5f); mb.gravityModifier = 0.8f;
            var eb = pb.emission; eb.rateOverTime = 70f;
            var sb = pb.shape; sb.shapeType = ParticleSystemShapeType.Cone; sb.angle = 40f; sb.radius = 0.9f;
            pb.GetComponent<ParticleSystemRenderer>().sharedMaterial = CavernaBuilder.Particula("agua_queda", Color.white);

            var luz = new GameObject("Luz da cachoeira").AddComponent<Light>();
            luz.transform.SetParent(go.transform, false); luz.transform.position = new Vector3(topo.x * 0.8f, 2.5f, topo.z);
            luz.color = new Color(0.5f, 0.8f, 1f); luz.range = 10f; luz.intensity = 1.6f;
            // poça na base
            CavernaBuilder.Prim(PrimitiveType.Cylinder, "Poça da cachoeira", (GraficosUpgrade.MaterialAgua() ?? CavernaBuilder.Cor("poca", new Color(0.04f, 0.1f, 0.2f), 0f, 0.97f)), new Vector3(topo.x * 0.93f, 0.014f, topo.z), new Vector3(3.4f, 0.002f, 3.4f), Quaternion.identity, false, pai);
        }

        static void Nevoa(Transform pai, float z0, float z1, float meia)
        {
            var go = new GameObject("Névoa baixa");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(0f, 0.7f, (z0 + z1) * 0.5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = 14f; main.startSpeed = 0.12f; main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startColor = new Color(0.45f, 0.6f, 0.75f, 0.07f); main.maxParticles = 220; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 16f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(meia * 2.4f, 1.2f, z1 - z0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = CavernaBuilder.Particula("nevoa", Color.white);
        }

        static void Feixe(Transform pai, Vector3 topo)
        {
            var cone = CavernaBuilder.Cone("cone16", 16);
            var mat = CavernaBuilder.Particula("feixe", new Color(0.6f, 0.8f, 1f, 0.07f));
            CavernaBuilder.Malha("Feixe de luz", cone, mat, topo, new Vector3(4.5f, -11.2f, 4.5f), Quaternion.identity, pai);
            var luz = new GameObject("Luz do feixe").AddComponent<Light>();
            luz.type = LightType.Spot;
            luz.transform.SetParent(pai, false); luz.transform.position = topo; luz.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            luz.color = new Color(0.65f, 0.82f, 1f); luz.range = 14f; luz.spotAngle = 30f; luz.intensity = 14f;
        }

        // ------------------------------------------------------------------ dojo do Sensei
        public static void Dojo(Transform pai, float z0, float z1)
        {
            Random.InitState(99);
            var raizDojo = new GameObject("Dojo").transform; raizDojo.SetParent(pai, false);
            CavernaBuilder.raiz = raizDojo;
            var palha = CavernaBuilder.Cor("tatame", new Color(0.72f, 0.66f, 0.4f), 0f, 0.1f);
            var borda = CavernaBuilder.Cor("tatame_borda", new Color(0.1f, 0.2f, 0.12f), 0f, 0.1f);
            var madeira = CavernaBuilder.Cor("madeira_escura", new Color(0.18f, 0.1f, 0.07f), 0f, 0.3f);
            var laca = CavernaBuilder.Cor("laca_vermelha", new Color(0.55f, 0.05f, 0.06f), 0f, 0.55f);
            var ouro = CavernaBuilder.Cor("ouro_dojo", new Color(0.95f, 0.75f, 0.25f), 0.4f, 0.7f, 0.8f);
            var ferro = CavernaBuilder.Cor("ferro_velho", new Color(0.18f, 0.18f, 0.2f), 0f, 0.5f, 0.8f);
            var cera = CavernaBuilder.Cor("cera", new Color(0.95f, 0.92f, 0.8f), 0f, 0.3f);
            var chama = CavernaBuilder.Cor("chama_vela", new Color(1f, 0.7f, 0.25f), 3f, 0.2f);
            var banner = CavernaBuilder.Cor("estandarte", new Color(0.7f, 0.06f, 0.08f), 0.15f, 0.2f);

            // tatames (pares alternando o sentido) sobre o piso do dojo
            for (float z = z0; z < z1; z += 1.8f)
                for (float x = -4.5f; x < 4.5f; x += 1.8f)
                {
                    bool giro = (int)((z - z0) / 1.8f + (x + 4.5f) / 1.8f) % 2 == 0;
                    var t = CavernaBuilder.Prim(PrimitiveType.Cube, "Tatame", palha, new Vector3(x + 0.9f, 0.02f, z + 0.9f), giro ? new Vector3(1.78f, 0.04f, 1.78f) : new Vector3(1.78f, 0.04f, 1.78f), Quaternion.identity, false, raizDojo);
                    CavernaBuilder.Prim(PrimitiveType.Cube, "Borda do tatame", borda, new Vector3(x + 0.9f, 0.022f, z + 0.9f), new Vector3(giro ? 1.8f : 0.06f, 0.04f, giro ? 0.06f : 1.8f), Quaternion.identity, false, raizDojo);
                }

            // plataforma do Sensei (degraus) e colunas laqueadas
            CavernaBuilder.Prim(PrimitiveType.Cube, "Estrado", madeira, new Vector3(0f, 0.12f, z1 - 3f), new Vector3(7f, 0.25f, 3.6f), Quaternion.identity, false, raizDojo);
            foreach (float x in new[] { -4.2f, 4.2f })
                for (float z = z0 + 3f; z < z1; z += 5f)
                {
                    CavernaBuilder.Prim(PrimitiveType.Cylinder, "Coluna laqueada", laca, new Vector3(x, 2.4f, z), new Vector3(0.45f, 2.4f, 0.45f), Quaternion.identity, false, raizDojo);
                    CavernaBuilder.Prim(PrimitiveType.Cylinder, "Base da coluna", ouro, new Vector3(x, 0.15f, z), new Vector3(0.6f, 0.15f, 0.6f), Quaternion.identity, false, raizDojo);
                    CavernaBuilder.Prim(PrimitiveType.Cylinder, "Capitel", ouro, new Vector3(x, 4.7f, z), new Vector3(0.6f, 0.1f, 0.6f), Quaternion.identity, false, raizDojo);
                }
            // vigas do teto do dojo
            for (float z = z0 + 3f; z < z1; z += 5f)
                CavernaBuilder.Prim(PrimitiveType.Cube, "Viga do dojo", madeira, new Vector3(0f, 4.95f, z), new Vector3(9.4f, 0.35f, 0.4f), Quaternion.identity, false, raizDojo);
            CavernaBuilder.Prim(PrimitiveType.Cube, "Cumeeira", madeira, new Vector3(0f, 5.0f, (z0 + z1) * 0.5f), new Vector3(0.4f, 0.35f, z1 - z0), Quaternion.identity, false, raizDojo);

            // estandartes vermelhos pendurados
            for (float z = z0 + 5.5f; z < z1; z += 5f)
                foreach (float x in new[] { -4.3f, 4.3f })
                    CavernaBuilder.Prim(PrimitiveType.Cube, "Estandarte", banner, new Vector3(x - Mathf.Sign(x) * 0.25f, 3.2f, z), new Vector3(0.05f, 2.2f, 0.9f), Quaternion.identity, false, raizDojo);

            // velas em candelabros ao redor do Sensei
            foreach (var p in new[] { new Vector2(-2.6f, z1 - 4.5f), new Vector2(2.6f, z1 - 4.5f), new Vector2(-3.4f, z1 - 1.6f), new Vector2(3.4f, z1 - 1.6f), new Vector2(-1.6f, z0 + 1.5f), new Vector2(1.6f, z0 + 1.5f) })
                Vela(raizDojo, new Vector3(p.x, 0f, p.y), cera, chama, ferro);

            // suporte de katanas
            var suporte = new GameObject("Suporte de katanas").transform; suporte.SetParent(raizDojo, false); suporte.position = new Vector3(3.9f, 0f, z0 + 4.5f);
            CavernaBuilder.Prim(PrimitiveType.Cube, "Base do suporte", madeira, suporte.position + new Vector3(0, 0.1f, 0), new Vector3(0.5f, 0.2f, 2.2f), Quaternion.identity, false, suporte);
            foreach (float zz in new[] { -1f, 1f })
                CavernaBuilder.Prim(PrimitiveType.Cube, "Montante", madeira, suporte.position + new Vector3(0, 0.7f, zz), new Vector3(0.12f, 1.2f, 0.12f), Quaternion.identity, false, suporte);
            for (int k = 0; k < 3; k++)
            {
                float y = 0.5f + k * 0.35f;
                CavernaBuilder.Prim(PrimitiveType.Cube, "Barra do suporte", madeira, suporte.position + new Vector3(0, y, 0), new Vector3(0.1f, 0.06f, 2.0f), Quaternion.identity, false, suporte);
                var lamina = CavernaBuilder.Cor("lamina_dojo", new Color(0.8f, 0.83f, 0.9f), 0f, 0.8f, 0.9f);
                CavernaBuilder.Prim(PrimitiveType.Cube, "Katana (lâmina)", lamina, suporte.position + new Vector3(0, y + 0.07f, 0.1f), new Vector3(0.025f, 0.04f, 1.5f), Quaternion.identity, false, suporte);
                CavernaBuilder.Prim(PrimitiveType.Cube, "Katana (cabo)", laca, suporte.position + new Vector3(0, y + 0.07f, -0.85f), new Vector3(0.04f, 0.05f, 0.35f), Quaternion.identity, false, suporte);
                CavernaBuilder.Prim(PrimitiveType.Cube, "Katana (guarda)", ouro, suporte.position + new Vector3(0, y + 0.07f, -0.65f), new Vector3(0.1f, 0.1f, 0.03f), Quaternion.identity, false, suporte);
            }
            // lanterna de papel sobre o Sensei
            var papel = CavernaBuilder.Cor("papel_lanterna", new Color(1f, 0.85f, 0.55f), 1.8f, 0.2f);
            var lan = CavernaBuilder.Prim(PrimitiveType.Sphere, "Lanterna de papel", papel, new Vector3(0f, 3.4f, z1 - 3f), new Vector3(0.9f, 1.2f, 0.9f), Quaternion.identity, false, raizDojo);
            var luz = new GameObject("Luz da lanterna de papel").AddComponent<Light>();
            luz.transform.SetParent(raizDojo, false); luz.transform.position = lan.transform.position;
            luz.color = new Color(1f, 0.8f, 0.5f); luz.range = 14f; luz.intensity = 3f;
        }

        static void Vela(Transform pai, Vector3 pos, Material cera, Material chama, Material ferro)
        {
            CavernaBuilder.Prim(PrimitiveType.Cylinder, "Haste do candelabro", ferro, pos + Vector3.up * 0.6f, new Vector3(0.05f, 0.6f, 0.05f), Quaternion.identity, false, pai);
            CavernaBuilder.Prim(PrimitiveType.Cylinder, "Prato do candelabro", ferro, pos + Vector3.up * 1.2f, new Vector3(0.22f, 0.02f, 0.22f), Quaternion.identity, false, pai);
            CavernaBuilder.Prim(PrimitiveType.Cylinder, "Vela", cera, pos + Vector3.up * 1.32f, new Vector3(0.08f, 0.12f, 0.08f), Quaternion.identity, false, pai);
            var fl = CavernaBuilder.Prim(PrimitiveType.Sphere, "Chama", chama, pos + Vector3.up * 1.5f, new Vector3(0.07f, 0.12f, 0.07f), Quaternion.identity, false, pai);
            var luz = new GameObject("Luz da vela").AddComponent<Light>();
            luz.transform.SetParent(pai, false); luz.transform.position = pos + Vector3.up * 1.55f;
            luz.color = new Color(1f, 0.7f, 0.35f); luz.range = 6f; luz.intensity = 1.6f;
            luz.gameObject.AddComponent<LuzOscilante>();
        }
    }
}
