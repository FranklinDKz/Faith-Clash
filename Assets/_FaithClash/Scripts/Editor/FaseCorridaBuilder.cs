using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;
using FaithClash.UI;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Menu: Faith Clash > Criar Fase 1 (Corrida contra o Tempo). Monta a cena do percurso de parkour: largada, caixotes e muretas para
    /// transpor, barras para deslizar por baixo, muros para escalar, abismos para pular, escada de caixas e o dojo com o Sensei no fim.
    /// Os blocos usam as texturas de protótipo do pacote Dynamic Parkour System (laranja = transpor, amarelo = escalar, azul = piso).
    /// </summary>
    public static class FaseCorridaBuilder
    {
        const string PastaCena = "Assets/_FaithClash/Scenes";
        const string CaminhoCena = PastaCena + "/Fase1_Corrida.unity";
        const string PastaMat = "Assets/_FaithClash/Materials/Fase1";
        const string PastaTex = "Assets/Sistemas/Sistema de Parkour/Resources/Materials/";

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Transform raiz;

        [MenuItem("Faith Clash/Criar Fase 1 (Corrida contra o Tempo)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            if (prefab == null) { Debug.LogError("[Faith Clash] Rode primeiro: Faith Clash > Preparar Jogador"); return; }

            cache.Clear();
            CavernaBuilder.Limpar();
            var cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera cam = null;
            foreach (var go in cena.GetRootGameObjects())
            {
                if (go.TryGetComponent(out Camera c)) { go.name = "Câmera Principal"; cam = c; }
                else if (go.TryGetComponent(out Light _)) go.name = "Luz Direcional";
            }
            raiz = new GameObject("Percurso").transform;

            ConstruirPercurso(out var checkpoints, out Transform sensei);

            var jogador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            jogador.transform.position = new Vector3(0, 0.1f, 2f);
            var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            Ligar(tpc, "target", jogador.transform);

            var hud = new GameObject("HUD").AddComponent<HUDJogo>();
            Ligar(hud, "jogador", jogador.transform);

            var fase = new GameObject("Fase 1").AddComponent<FaseCorrida>();
            var so = new SerializedObject(fase);
            so.FindProperty("jogador").objectReferenceValue = jogador.GetComponent<PlayerController>();
            so.FindProperty("chegada").objectReferenceValue = sensei;
            var arr = so.FindProperty("checkpoints");
            arr.arraySize = checkpoints.Count;
            for (int i = 0; i < checkpoints.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = checkpoints[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            TrainingSceneBuilder.CriarAmbiente(cena);
            CavernaBuilder.ClimaDeCaverna();

            Directory.CreateDirectory(PastaCena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            var lista = new List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes) if (File.Exists(s.path)) lista.Add(s);
            if (!lista.Exists(s => s.path == CaminhoCena)) lista.Add(new EditorBuildSettingsScene(CaminhoCena, true));
            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log("[Faith Clash] Fase 1 criada em " + CaminhoCena + ". Aperte Play!");
        }

        // ------------------------------------------------------------------ percurso
        static void ConstruirPercurso(out List<Transform> checkpoints, out Transform sensei)
        {
            checkpoints = new List<Transform>();
            const float L = 9f;   // largura da pista

            // início
            Piso(-2f, 14f, L);
            Arco(2f, new Color(0.75f, 0.12f, 0.1f));
            // 1) transpor (vault): caixote baixo e depois duas muretas
            Bloco("Caixote (transpor)", 16f, 0f, 0.9f, 0.7f, 6.5f, "orange");
            Piso(14f, 42f, L);
            Bloco("Mureta A", 26f, 0f, 1.0f, 0.6f, 6.5f, "orange");
            Bloco("Mureta B", 32f, 0f, 1.2f, 0.6f, 6.5f, "orange");
            Checkpoint(checkpoints, 38f);
            // 2) deslizar por baixo
            Bloco("Barra de deslize", 46f, 1.0f, 0.7f, 1.0f, 6.5f, "blue");   // vão de 1 m embaixo
            Bloco("Pilar da barra E", 46f, 0f, 1.0f, 1.0f, 1.0f, "blue", -3.6f);
            Bloco("Pilar da barra D", 46f, 0f, 1.0f, 1.0f, 1.0f, "blue", 3.6f);
            Piso(42f, 70f, L);
            // 3) escalar um muro e descer pelos degraus
            Bloco("Muro (escalar)", 56f, 0f, 2.4f, 1.0f, 8.5f, "yellow");
            Bloco("Degrau do muro", 58.0f, 0f, 1.2f, 3.0f, 8.5f, "yellow");
            Checkpoint(checkpoints, 64f);
            // 4) abismo + plataformas
            Piso(70f, 76f, L);
            // vão de 3 m: 76..79
            Piso(79f, 84f, L);
            // plataformas flutuantes com vãos
            Piso(86.5f, 90.5f, 5f);
            Piso(93f, 97f, 5f, 0.5f);
            Piso(99.5f, 103.5f, 5f);
            Checkpoint(checkpoints, 100.5f);
            Piso(106f, 126f, L);
            // 5) escada de caixas (subir três degraus) e descer
            Bloco("Caixa 1 (subir)", 110f, 0f, 1.0f, 3.2f, 7.5f, "orange");
            Bloco("Caixa 2 (subir)", 113.2f, 0f, 1.9f, 3.2f, 7.5f, "orange");
            Bloco("Caixa 3 (subir)", 116.4f, 0f, 2.8f, 3.2f, 7.5f, "orange");
            Bloco("Plataforma alta", 120.4f, 0f, 2.8f, 4.8f, 7.5f, "blue");
            Checkpoint(checkpoints, 128f);
            Piso(126f, 190f, L);
            // 6) trecho final: caixote, barra, mureta e muro
            Bloco("Caixote final", 134f, 0f, 1.1f, 0.8f, 6.5f, "orange");
            Bloco("Barra final", 142f, 1.0f, 0.7f, 1.0f, 6.5f, "blue");
            Bloco("Pilar da barra final E", 142f, 0f, 1.0f, 1.0f, 1.0f, "blue", -3.6f);
            Bloco("Pilar da barra final D", 142f, 0f, 1.0f, 1.0f, 1.0f, "blue", 3.6f);
            Bloco("Mureta final", 150f, 0f, 0.8f, 0.6f, 6.5f, "orange");
            Checkpoint(checkpoints, 154f);
            Bloco("Muro final (escalar)", 160f, 0f, 1.9f, 1.0f, 8.5f, "yellow");
            // dojo
            Arco(172f, new Color(0.55f, 0.1f, 0.08f));
            sensei = Sensei(182f);
            CavernaDetalhes.Dojo(raiz, 174f, 191f);

            // laterais: paredes invisíveis para não sair da pista, pilares de lanterna e o abismo
            var parede = new GameObject("Limite da pista");
            parede.transform.SetParent(raiz, false);
            foreach (float x in new[] { -L * 0.5f - 0.5f, L * 0.5f + 0.5f })
            {
                var b = parede.AddComponent<BoxCollider>();
                b.center = new Vector3(x, 3f, 90f); b.size = new Vector3(1f, 8f, 200f);
            }
            var fim = parede.AddComponent<BoxCollider>(); fim.center = new Vector3(0, 3f, 192f); fim.size = new Vector3(20f, 8f, 1f);
            var ini = parede.AddComponent<BoxCollider>(); ini.center = new Vector3(0, 3f, -3f); ini.size = new Vector3(20f, 8f, 1f);
            for (float z = -1f; z < 180f; z += 12f)
                foreach (float x in new[] { -L * 0.5f - 0.2f, L * 0.5f + 0.2f })
                    CavernaBuilder.Tocha(raiz, new Vector3(x, 0f, z));
            CavernaBuilder.Montar(raiz, -3f, 192f, L, 66f, 112f, -4.3f);
        }

        // ------------------------------------------------------------------ peças
        static void Piso(float z0, float z1, float largura, float topo = 0f)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = $"Piso {z0:0}-{z1:0}";
            b.transform.SetParent(raiz, false);
            float esp = 1.2f;
            b.transform.position = new Vector3(0f, topo - esp * 0.5f, (z0 + z1) * 0.5f);
            b.transform.localScale = new Vector3(largura, esp, z1 - z0);
            b.GetComponent<Renderer>().sharedMaterial = CavernaBuilder.Rocha("chao", "Ground003", new Vector2(largura * 0.35f, (z1 - z0) * 0.35f), new Color(0.75f, 0.7f, 0.68f), 0.12f);
        }

        /// <summary>Bloco com a base em <paramref name="base0"/>, altura <paramref name="alt"/>, centro em z.</summary>
        static void Bloco(string nome, float z, float base0, float alt, float prof, float larg, string cor, float x = 0f)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = nome;
            b.transform.SetParent(raiz, false);
            b.transform.position = new Vector3(x, base0 + alt * 0.5f, z);
            b.transform.localScale = new Vector3(larg, alt, prof);
            var til = new Vector2(Mathf.Max(1f, larg * 0.35f), Mathf.Max(1f, alt * 0.8f));
            // pedra e musgo no lugar das texturas de protótipo; a cor continua indicando a ação (marrom = transpor, verde = escalar, cinza = barra/apoio)
            b.GetComponent<Renderer>().sharedMaterial = cor == "orange" ? CavernaBuilder.Rocha("obst_marrom", "Ground004", til, new Color(1.25f, 0.85f, 0.6f), 0.15f)
                : cor == "yellow" ? CavernaBuilder.Rocha("obst_musgo", "Ground005", til, new Color(0.7f, 1f, 0.55f), 0.1f)
                : CavernaBuilder.Rocha("obst_cinza", "GroundPatchCracked01", til, new Color(0.7f, 0.74f, 0.85f), 0.25f);
        }

        static void Checkpoint(List<Transform> lista, float z)
        {
            var p = new GameObject($"Checkpoint {lista.Count + 1}").transform;
            p.SetParent(raiz, false);
            p.position = new Vector3(0, 0f, z);
            lista.Add(p);
            // marcador: dois postes com faixa e luz
            foreach (float x in new[] { -4.2f, 4.2f })
            {
                var poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poste.name = "Poste do checkpoint";
                poste.transform.SetParent(p, false);
                poste.transform.localPosition = new Vector3(x, 1.5f, 0);
                poste.transform.localScale = new Vector3(0.18f, 1.5f, 0.18f);
                poste.GetComponent<Renderer>().sharedMaterial = Cor("checkpoint", new Color(0.2f, 0.85f, 0.55f), true);
                Object.DestroyImmediate(poste.GetComponent<Collider>());
            }
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(p, false);
            luz.transform.localPosition = new Vector3(0, 3.2f, 0);
            luz.color = new Color(0.3f, 1f, 0.6f); luz.range = 9f; luz.intensity = 2f;
        }

        static void Arco(float z, Color cor)
        {
            var mat = Cor("arco_" + ColorUtility.ToHtmlStringRGB(cor), cor);
            foreach (float x in new[] { -4.9f, 4.9f })
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Pilar do arco"; p.transform.SetParent(raiz, false);
                p.transform.position = new Vector3(x, 2.6f, z); p.transform.localScale = new Vector3(0.6f, 5.2f, 0.6f);
                p.GetComponent<Renderer>().sharedMaterial = mat;
            }
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Viga do arco"; v.transform.SetParent(raiz, false);
            v.transform.position = new Vector3(0, 5.3f, z); v.transform.localScale = new Vector3(11.4f, 0.5f, 0.9f);
            v.GetComponent<Renderer>().sharedMaterial = mat;
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.name = "Telhado do arco"; t.transform.SetParent(raiz, false);
            t.transform.position = new Vector3(0, 5.8f, z); t.transform.localScale = new Vector3(12.6f, 0.3f, 1.6f);
            t.GetComponent<Renderer>().sharedMaterial = Cor("telhado", new Color(0.12f, 0.12f, 0.14f));
            foreach (var c in new[] { v, t }) { var col = c.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col); }
        }

        static void Lanterna(Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "Poste"; p.transform.SetParent(raiz, false);
            p.transform.position = pos + Vector3.up * 1.4f; p.transform.localScale = new Vector3(0.14f, 1.4f, 0.14f);
            p.GetComponent<Renderer>().sharedMaterial = Cor("poste", new Color(0.12f, 0.1f, 0.09f));
            Object.DestroyImmediate(p.GetComponent<Collider>());
            var l = new GameObject("Lanterna").AddComponent<Light>();
            l.transform.SetParent(raiz, false); l.transform.position = pos + Vector3.up * 3f;
            l.color = new Color(1f, 0.75f, 0.4f); l.range = 8f; l.intensity = 1.4f;
        }

        static Transform Sensei(float z)
        {
            var go = new GameObject("Sensei");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(0, 0, z);
            go.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorInimigos.CaminhoPrefab("Guarda"));
            if (pf != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(pf, go.transform);
                m.name = "Corpo do Sensei";
                m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
                PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var c in m.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(c);
                var cc = m.GetComponent<CharacterController>(); if (cc != null) Object.DestroyImmediate(cc);
                m.transform.localScale = Vector3.one * 1.05f;
            }
            var luz = new GameObject("Luz do Sensei").AddComponent<Light>();
            luz.transform.SetParent(go.transform, false); luz.transform.localPosition = new Vector3(0, 3.5f, 1.5f);
            luz.color = new Color(1f, 0.85f, 0.6f); luz.range = 10f; luz.intensity = 2.5f;
            var chegada = new GameObject("Chegada").transform;
            chegada.SetParent(raiz, false);
            chegada.position = new Vector3(0, 0, z - 4f);
            return chegada;
        }

        // ------------------------------------------------------------------ materiais
        static Material Textura(string nome, Vector2 tiling)
        {
            string chave = $"{nome}_{tiling.x:0.0}x{tiling.y:0.0}";
            if (cache.TryGetValue(chave, out var m)) return m;
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}/F1_{chave}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, caminho); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PastaTex + "prototype_512x512_" + nome + (nome == "blue" ? "1" : "") + ".png");
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTextureScale = tiling; }
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            cache[chave] = m;
            return m;
        }

        static Material Cor(string nome, Color cor, bool emissivo = false)
        {
            if (cache.TryGetValue("c_" + nome, out var m)) return m;
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}/F1_cor_{nome}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, caminho); }
            m.SetColor("_BaseColor", cor);
            if (emissivo) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", cor * 1.6f); }
            EditorUtility.SetDirty(m);
            cache["c_" + nome] = m;
            return m;
        }

        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
