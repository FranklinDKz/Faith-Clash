using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;
using FaithClash.UI;

namespace FaithClash.EditorTools
{
    /// <summary>Menu: Faith Clash > Criar Cena de Treino. Monta a arena simples com o protagonista, bonecos e o HUD.</summary>
    public static class TrainingSceneBuilder
    {
        const string PastaCena = "Assets/_FaithClash/Scenes";
        const string PastaMat = "Assets/_FaithClash/Materials";
        const string CaminhoCena = PastaCena + "/Treino.unity";

        [MenuItem("Faith Clash/Criar Cena de Treino")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            if (prefab == null)
            {
                Debug.LogError("[Faith Clash] Rode primeiro: Faith Clash > Preparar Jogador");
                return;
            }

            var cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera cam = null;
            foreach (var go in cena.GetRootGameObjects())
            {
                if (go.TryGetComponent(out Camera c)) { go.name = "Câmera Principal"; cam = c; }
                else if (go.TryGetComponent(out Light _)) go.name = "Luz Direcional";
            }

            CriarTerreno();

            var matParede = CavernaBuilder.Rocha("treino_parede", "GroundPatchCracked01", new Vector2(12f, 1.4f), new Color(0.62f, 0.64f, 0.7f), 0.2f);
            string[] lados = { "Norte", "Sul", "Leste", "Oeste" };
            float metade = 25f;
            for (int i = 0; i < 4; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Parede " + lados[i];
                bool ns = i < 2;
                float s = (i % 2 == 0 ? 1 : -1) * metade;
                p.transform.position = ns ? new Vector3(0, 2, s) : new Vector3(s, 2, 0);
                p.transform.localScale = ns ? new Vector3(metade * 2, 4, 1) : new Vector3(1, 4, metade * 2);
                p.GetComponent<Renderer>().sharedMaterial = matParede;
            }

            var jogador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            jogador.transform.position = new Vector3(0, 0.1f, 0);

            var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            Ligar(tpc, "target", jogador.transform);

            // inimigos de treino: Guarda, Lu e Adam (Adam Character Pack) com IA e animações
            var posicoes = new[] { new Vector3(0, 0, 7), new Vector3(-6, 0, 11), new Vector3(6, 0, 11) };
            var elenco = PreparadorInimigos.Elenco;
            for (int i = 0; i < elenco.Length; i++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorInimigos.CaminhoPrefab(elenco[i].chave));
                if (pf == null) { Debug.LogError("[Faith Clash] Rode antes: Faith Clash > Preparar Inimigos"); continue; }
                var e = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                e.name = elenco[i].nome;
                e.transform.position = posicoes[i];
                e.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }

            CriarObstaculos(CavernaBuilder.Rocha("treino_obstaculo", "Ground004", new Vector2(1.6f, 1.2f), new Color(0.78f, 0.66f, 0.56f), 0.15f));
            Decorar(cena);
            // duas paredes paralelas (5 m entre elas) para treinar correr na parede e pular de uma parede à outra
            {
                var matP = CavernaBuilder.Rocha("treino_parede", "GroundPatchCracked01", new Vector2(12f, 1.4f), new Color(0.62f, 0.64f, 0.7f), 0.2f);
                var raizP = new GameObject("Paredes de Parkour").transform;
                float[] xs = { -7.5f, -13.0f };
                for (int i = 0; i < xs.Length; i++)
                {
                    var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    w.name = "Parede de Parkour " + (i + 1);
                    w.transform.SetParent(raizP, false);
                    w.transform.position = new Vector3(xs[i], 3.5f, 12.5f);   // 23 m (de z=1 a 24, sem bater na rampa): ~5 s correndo na parede
                    w.transform.localScale = new Vector3(0.5f, 7f, 23f);
                    w.GetComponent<Renderer>().sharedMaterial = matP;
                }
            }
            // plataforma móvel (física do MotionCore): vai e volta, e quem está em cima é carregado
            {
                var pm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pm.name = "Plataforma Móvel";
                pm.transform.position = new Vector3(9f, 0.25f, -23f);
                pm.transform.localScale = new Vector3(4f, 0.5f, 4f);
                pm.GetComponent<Renderer>().sharedMaterial = CavernaBuilder.Rocha("treino_obstaculo", "Ground004", new Vector2(1.6f, 1.2f), new Color(0.55f, 0.6f, 0.7f), 0.2f);
                var prb = pm.AddComponent<Rigidbody>(); prb.isKinematic = true;
                var mp = pm.AddComponent<CharacterMovement.MovingPlatform>();
                var smp = new SerializedObject(mp); smp.FindProperty("preferRigidbodyVelocity").boolValue = false; smp.ApplyModifiedPropertiesWithoutUndo();   // plataforma cinemática: velocidade pela posição
                pm.AddComponent<PlataformaMovel>().deslocamento = new Vector3(0f, 0f, 12f);
            }
            CriarAmbiente(cena);
            new GameObject("Gerenciador de Treino").AddComponent<TrainingManager>();
            var hud = new GameObject("HUD").AddComponent<HUDJogo>();
            Ligar(hud, "jogador", jogador.transform);

            Directory.CreateDirectory(PastaCena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AdicionarAosBuildSettings(CaminhoCena);
            Debug.Log("[Faith Clash] Cena de treino criada em " + CaminhoCena + ". Aperte Play!");
        }

        /// <summary>Visual mais realista: céu 8K, névoa, sombras suaves e pós-processamento (bloom, ACES, vinheta, contraste, oclusão).</summary>
        internal static void CriarAmbiente(UnityEngine.SceneManagement.Scene cena)
        {
            // o material do pacote usa shader Standard (não funciona como céu): cria um Skybox/Panoramic com a textura 8K
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Ceus/Ceus 8K/Skyboxes/Texture/sky-3.png");
            string caminhoCeu = "Assets/_FaithClash/Materials/Ceu.mat";
            if (tex != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                if (ti != null && ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
                var sky = AssetDatabase.LoadAssetAtPath<Material>(caminhoCeu);
                if (sky == null) { Directory.CreateDirectory("Assets/_FaithClash/Materials"); sky = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(sky, caminhoCeu); }
                sky.SetTexture("_MainTex", tex);
                sky.SetFloat("_Exposure", 1.1f);
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1f));
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.68f);
            foreach (var go in cena.GetRootGameObjects())
                if (go.TryGetComponent(out Light l) && l.type == LightType.Directional)
                {
                    l.shadows = LightShadows.Soft; l.intensity = 1.35f; l.color = new Color(1f, 0.95f, 0.85f);
                    go.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                }
            var perfil = GraficosUpgrade.PerfilPos();
            var vgo = new GameObject("Pós-processamento");
            var vol = vgo.AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = perfil;
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData dados)) { dados.renderPostProcessing = true; dados.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
        }

        /// <summary>Rampa, degraus e caixotes para testar colisão, gravidade e os pés no chão.</summary>
        const float LagoX0 = -24f, LagoX1 = -10f, LagoZ0 = -22f, LagoZ1 = -12f;
        const float FundoLago = -1.6f, SuperficieLago = -0.15f;

        static GameObject Laje(Transform pai, string nome, float x0, float x1, float z0, float z1, float topo, float fundo, Material mat)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = nome; c.transform.SetParent(pai, false);
            c.transform.position = new Vector3((x0 + x1) * 0.5f, (topo + fundo) * 0.5f, (z0 + z1) * 0.5f);
            c.transform.localScale = new Vector3(x1 - x0, topo - fundo, z1 - z0);
            c.GetComponent<Renderer>().sharedMaterial = mat;
            return c;
        }

        /// <summary>Terreno com um lago de verdade: bacia funda, degraus de entrada, paredes de terra e fundo de pedra.</summary>
        static void CriarTerreno()
        {
            var pai = new GameObject("Chão").transform;
            Material Terra(float w, float h) => CavernaBuilder.Rocha("treino_chao", "Ground003", new Vector2(w * 0.36f, h * 0.36f), new Color(0.85f, 0.82f, 0.8f), 0.18f);
            Laje(pai, "Terreno Norte", -25f, 25f, LagoZ1, 25f, 0f, -3f, Terra(50f, 37f));
            Laje(pai, "Terreno Sul", -25f, 25f, -25f, LagoZ0, 0f, -3f, Terra(50f, 3f));
            Laje(pai, "Terreno Oeste", -25f, LagoX0, LagoZ0, LagoZ1, 0f, -3f, Terra(1f, 10f));
            Laje(pai, "Terreno Leste", LagoX1, 25f, LagoZ0, LagoZ1, 0f, -3f, Terra(35f, 10f));
            var pedra = CavernaBuilder.Rocha("treino_fundo", "GroundPatchCracked01", new Vector2(4f, 3f), new Color(0.5f, 0.52f, 0.58f), 0.3f);
            Laje(pai, "Fundo do Lago", LagoX0, LagoX1, LagoZ0, LagoZ1, FundoLago, -3f, pedra);
            // degraus de entrada na água (margem norte), de 23 cm cada
            for (int i = 0; i < 7; i++)
            {
                float topo = -0.23f * (i + 1);
                Laje(pai, "Degrau do Lago " + (i + 1), -20f, -14f, LagoZ1 - 0.9f * (i + 1), LagoZ1 - 0.9f * i, topo, FundoLago, pedra);
            }
        }

        static GameObject CorpoFisico(Transform pai, string nome, PrimitiveType tipo, Vector3 pos, Vector3 escala, float massa, Material mat, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(tipo);
            g.name = nome; g.transform.SetParent(pai, false);
            g.transform.position = pos; g.transform.localScale = escala; g.transform.rotation = rot ?? Quaternion.identity;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            var rb = g.AddComponent<Rigidbody>();
            rb.mass = massa; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 0.05f; rb.angularDamping = 0.25f;
            return g;
        }

        /// <summary>Lago com física, objetos que obedecem à gravidade (caixas, barris, troncos), juncos que balançam e poeira no ar.</summary>
        static void Decorar(UnityEngine.SceneManagement.Scene cena)
        {
            var pai = new GameObject("Decoração").transform;
            var agua = GraficosUpgrade.MaterialAgua();
            var rocha = CavernaBuilder.Rocha("treino_rocha", "GroundPatchCracked01", new Vector2(1.2f, 1.2f), new Color(0.55f, 0.57f, 0.63f), 0.25f);
            float cx = (LagoX0 + LagoX1) * 0.5f, cz = (LagoZ0 + LagoZ1) * 0.5f, lw = LagoX1 - LagoX0, ld = LagoZ1 - LagoZ0;
            if (agua != null)
            {
                var lago = GameObject.CreatePrimitive(PrimitiveType.Plane);
                lago.name = "Água do Lago"; lago.transform.SetParent(pai, false);
                lago.transform.position = new Vector3(cx, SuperficieLago, cz); lago.transform.localScale = new Vector3(lw / 10f, 1f, ld / 10f);
                Object.DestroyImmediate(lago.GetComponent<Collider>());
                lago.GetComponent<Renderer>().sharedMaterial = agua;
                lago.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var vol = new GameObject("Volume de Água (física)");
                vol.transform.SetParent(pai, false);
                vol.transform.position = new Vector3(cx, (FundoLago + SuperficieLago) * 0.5f, cz);
                var bc = vol.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(lw, SuperficieLago - FundoLago, ld);
                vol.AddComponent<AguaFisica>();
            }
            var detrito = CavernaDetalhes.Pronta("RockDebris");
            var rnd = new System.Random(5);
            CavernaBuilder.raiz = pai;
            if (detrito != null)
            {
                // pedras na margem do lago (com colisor) e espalhadas pela arena
                for (int i = 0; i < 30; i++)
                {
                    float t01 = i / 30f * 4f; int lado = (int)t01; float f = t01 - lado;
                    Vector3 pos = lado == 0 ? new Vector3(LagoX0 + f * lw, 0f, LagoZ1 + 0.9f) : lado == 1 ? new Vector3(LagoX1 + 0.9f, 0f, LagoZ1 - f * ld) : lado == 2 ? new Vector3(LagoX1 - f * lw, 0f, LagoZ0 - 0.9f) : new Vector3(LagoX0 - 0.9f, 0f, LagoZ0 + f * ld);
                    if (lado == 0 && pos.x > -21f && pos.x < -13f) continue;   // deixa a entrada dos degraus livre
                    float t = 0.9f + (float)rnd.NextDouble() * 1.2f;
                    var r = CavernaBuilder.Malha("Pedra da margem", detrito, rocha, pos, new Vector3(t, t * 0.6f, t), Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0), pai);
                    var mc = r.AddComponent<MeshCollider>(); mc.convex = true;
                }
                for (int i = 0; i < 40; i++)
                {
                    float x = (float)(rnd.NextDouble() * 2 - 1) * 23.5f, z = (float)(rnd.NextDouble() * 2 - 1) * 23.5f;
                    if ((Mathf.Abs(x) < 8f && Mathf.Abs(z) < 14f) || (x < LagoX1 + 2f && z < LagoZ1 + 2f && x > LagoX0 - 2f && z > LagoZ0 - 2f)) continue;
                    float t = 0.5f + (float)rnd.NextDouble() * 1.1f;
                    CavernaBuilder.Malha("Pedra", detrito, rocha, new Vector3(x, 0f, z), new Vector3(t, t * 0.55f, t), Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0), pai);
                }
            }

            // objetos físicos em escala real: caixas 0,8 m (25 kg), barris 0,6 x 0,9 m (30 kg), troncos de 3 m, tábuas
            var caixa = CavernaBuilder.Cor("caixa_madeira", new Color(0.5f, 0.33f, 0.17f), 0f, 0.25f);
            var ferro = CavernaBuilder.Cor("ferro_banda", new Color(0.16f, 0.16f, 0.18f), 0f, 0.6f, 0.8f);
            var tonel = CavernaBuilder.Cor("barril_madeira", new Color(0.38f, 0.24f, 0.12f), 0f, 0.3f);
            var tronco = CavernaBuilder.Cor("tronco", new Color(0.3f, 0.2f, 0.12f), 0f, 0.15f);
            Vector3[] caixas = { new Vector3(10f, 0.4f, 9f), new Vector3(10f, 1.2f, 9f), new Vector3(11f, 0.4f, 9.1f), new Vector3(-6f, 0.4f, 8f), new Vector3(-6.9f, 0.4f, 8.2f), new Vector3(3f, 0.4f, 17f), new Vector3(-12f, 0.4f, 0f), new Vector3(-8f, 0.4f, -9f) };
            for (int i = 0; i < caixas.Length; i++)
            {
                var c = CorpoFisico(pai, "Caixa", PrimitiveType.Cube, caixas[i], Vector3.one * 0.8f, 25f, caixa, Quaternion.Euler(0, (i * 37) % 90, 0));
                foreach (float y in new[] { -0.3f, 0.3f })
                {
                    var banda = GameObject.CreatePrimitive(PrimitiveType.Cube); banda.name = "Banda de ferro";
                    banda.transform.SetParent(c.transform, false); banda.transform.localPosition = new Vector3(0, y, 0); banda.transform.localScale = new Vector3(1.04f, 0.1f, 1.04f);
                    Object.DestroyImmediate(banda.GetComponent<Collider>()); banda.GetComponent<Renderer>().sharedMaterial = ferro;
                }
            }
            Vector3[] barris = { new Vector3(-9f, 0.45f, 12f), new Vector3(-9.8f, 0.45f, 12.4f), new Vector3(14f, 0.45f, -14f), new Vector3(7f, 0.45f, -16f), new Vector3(0f, 0.45f, 20f) };
            foreach (var pos in barris)
            {
                var bar = CorpoFisico(pai, "Barril", PrimitiveType.Cylinder, pos, new Vector3(0.6f, 0.45f, 0.6f), 30f, tonel);
                foreach (float y in new[] { -0.55f, 0.55f })
                {
                    var anel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); anel.name = "Aro de ferro";
                    anel.transform.SetParent(bar.transform, false); anel.transform.localPosition = new Vector3(0, y * 0.75f, 0); anel.transform.localScale = new Vector3(1.04f, 0.06f, 1.04f);
                    Object.DestroyImmediate(anel.GetComponent<Collider>()); anel.GetComponent<Renderer>().sharedMaterial = ferro;
                }
            }
            CorpoFisico(pai, "Tronco", PrimitiveType.Cylinder, new Vector3(5f, 0.3f, 12f), new Vector3(0.36f, 1.5f, 0.36f), 110f, tronco, Quaternion.Euler(0, 40f, 90f));
            CorpoFisico(pai, "Tronco", PrimitiveType.Cylinder, new Vector3(-14f, 0.3f, 6f), new Vector3(0.3f, 1.5f, 0.3f), 90f, tronco, Quaternion.Euler(0, 100f, 90f));
            // flutuando no lago: troncos, tábuas e barris
            CorpoFisico(pai, "Tronco flutuante", PrimitiveType.Cylinder, new Vector3(cx + 2f, 0.4f, cz), new Vector3(0.34f, 1.7f, 0.34f), 100f, tronco, Quaternion.Euler(0, 20f, 90f));
            CorpoFisico(pai, "Barril flutuante", PrimitiveType.Cylinder, new Vector3(cx - 3f, 0.6f, cz + 1f), new Vector3(0.6f, 0.45f, 0.6f), 30f, tonel);
            for (int i = 0; i < 3; i++)
                CorpoFisico(pai, "Tábua", PrimitiveType.Cube, new Vector3(cx + i * 1.4f - 1.5f, 0.5f + i * 0.2f, cz - 2f), new Vector3(2.2f, 0.07f, 0.45f), 8f, caixa, Quaternion.Euler(0, 15f * i, 0));

            // juncos nas margens: balançam com o vento
            var folha = CavernaBuilder.Cone("cone3", 3);
            var juncoMat = CavernaBuilder.Cor("junco", new Color(0.3f, 0.55f, 0.22f), 0.08f, 0.2f);
            for (int g = 0; g < 18; g++)
            {
                float a = (float)rnd.NextDouble() * 4f; int lado = (int)a; float f = a - lado;
                Vector3 b = lado == 0 ? new Vector3(LagoX0 + f * lw, 0f, LagoZ1 + 0.3f) : lado == 1 ? new Vector3(LagoX1 + 0.3f, 0f, LagoZ1 - f * ld) : lado == 2 ? new Vector3(LagoX1 - f * lw, 0f, LagoZ0 - 0.3f) : new Vector3(LagoX0 - 0.3f, 0f, LagoZ0 + f * ld);
                if (lado == 0 && b.x > -21f && b.x < -13f) continue;
                for (int k = 0; k < 9; k++)
                {
                    float h = 0.9f + (float)rnd.NextDouble() * 1.1f;
                    var j = CavernaBuilder.Malha("Junco", folha, juncoMat, b + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.6f, new Vector3(0.05f, h, 0.05f), Quaternion.identity, pai);
                    j.AddComponent<Balanco>();
                }
            }

            // poeira / pólen flutuando na luz do sol
            var poeira = new GameObject("Partículas de ar"); poeira.transform.SetParent(pai, false); poeira.transform.position = new Vector3(0, 3.5f, 0);
            var ps = poeira.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.prewarm = true; main.startLifetime = 12f; main.startSpeed = 0.15f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 0.96f, 0.85f, 0.6f); main.maxParticles = 500; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 45f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(50f, 7f, 50f);
            var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.6f; ruido.frequency = 0.25f;
            poeira.GetComponent<ParticleSystemRenderer>().sharedMaterial = CavernaBuilder.Particula("poeira_ar", Color.white);
        }

        static void CriarObstaculos(Material mat)
        {
            var pai = new GameObject("Obstáculos de Teste").transform;
            GameObject Cubo(string nome, Vector3 pos, Vector3 esc, Vector3 rot)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = nome;
                c.transform.SetParent(pai);
                c.transform.position = pos;
                c.transform.localScale = esc;
                c.transform.rotation = Quaternion.Euler(rot);
                c.GetComponent<Renderer>().sharedMaterial = mat;
                return c;
            }
            Cubo("Rampa", new Vector3(-12f, 0.9f, -4f), new Vector3(5f, 0.3f, 8f), new Vector3(-14f, 0f, 0f));
            for (int i = 0; i < 5; i++)
                Cubo("Degrau " + (i + 1), new Vector3(12f, 0.1f + i * 0.2f, -6f + i * 0.9f), new Vector3(5f, 0.2f + i * 0.4f, 0.9f), Vector3.zero);
            Cubo("Caixote Baixo", new Vector3(0f, 0.3f, -9f), new Vector3(2f, 0.6f, 2f), Vector3.zero);
            Cubo("Caixote Alto", new Vector3(4f, 0.9f, -10f), new Vector3(2f, 1.8f, 2f), new Vector3(0f, 25f, 0f));
            Cubo("Plataforma", new Vector3(-6f, 0.6f, -14f), new Vector3(4f, 1.2f, 4f), Vector3.zero);
            // percurso de parkour: muretas para vault, muro alto para escalar e duas paredes para correr nelas
            Cubo("Mureta 1", new Vector3(-16f, 0.5f, 6f), new Vector3(4f, 1f, 0.6f), Vector3.zero);
            Cubo("Mureta 2", new Vector3(-16f, 0.6f, 11f), new Vector3(4f, 1.2f, 0.6f), Vector3.zero);
            Cubo("Muro de Escalada", new Vector3(-16f, 1.2f, 16f), new Vector3(4f, 2.4f, 1f), Vector3.zero);
            Cubo("Barra de Deslize", new Vector3(-4f, 1.35f, 4f), new Vector3(4f, 0.7f, 1f), Vector3.zero);   // vão de 1 m embaixo: deslizar
            Cubo("Parede de Parkour A", new Vector3(16f, 3f, 0f), new Vector3(0.8f, 6f, 14f), Vector3.zero);
            Cubo("Parede de Parkour B", new Vector3(21f, 3f, 0f), new Vector3(0.8f, 6f, 14f), Vector3.zero);
        }
        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Mat(string nome, Color cor)
        {
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}/{nome}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, caminho);
            }
            m.SetColor("_BaseColor", cor);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AdicionarAosBuildSettings(string caminho)
        {
            var lista = new List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes)
                if (File.Exists(s.path)) lista.Add(s);
            if (!lista.Exists(s => s.path == caminho)) lista.Add(new EditorBuildSettingsScene(caminho, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
