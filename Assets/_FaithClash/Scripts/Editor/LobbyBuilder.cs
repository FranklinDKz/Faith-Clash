#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using FaithClash.Player;
using FaithClash.Training;
using FaithClash.UI;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Lobby = mapa "Terreno Alagado" (Flooded Grounds) convertido para URP, com o jogador, portais para o Treino e a Fase 1
    /// e uma galeria que expõe os assets importados (espadas, árvore e efeitos) para você avaliar e apagar o que não quiser.
    /// </summary>
    public static class LobbyBuilder
    {
        const string Origem = "Assets/Mapas/Mapa Terreno Alagado/Scenes/Scene_A.unity";
        const string Destino = "Assets/_FaithClash/Scenes/Lobby.unity";

        [MenuItem("Faith Clash/Criar Lobby")]
        public static void Construir()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            if (prefab == null) { Debug.LogError("[Faith Clash] Rode antes: Preparar Jogador"); return; }
            if (!File.Exists(Origem)) { Debug.LogError("[Faith Clash] Mapa não encontrado: " + Origem); return; }
            ConversorMateriais.ConverterTudo();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destino) != null) AssetDatabase.DeleteAsset(Destino);
            AssetDatabase.CopyAsset(Origem, Destino);
            var cena = EditorSceneManager.OpenScene(Destino, OpenSceneMode.Single);

            // terreno: material URP; câmeras e ouvintes antigos saem
            var matTerreno = AssetDatabase.LoadAssetAtPath<Material>("Assets/_FaithClash/Materials/TerrenoURP.mat");
            if (matTerreno == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (sh != null) { matTerreno = new Material(sh); AssetDatabase.CreateAsset(matTerreno, "Assets/_FaithClash/Materials/TerrenoURP.mat"); }
            }
            Vector3 centro = Vector3.zero; bool achou = false;
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                if (matTerreno != null) t.materialTemplate = matTerreno;
                if (!achou)
                {
                    centro = t.transform.position + new Vector3(t.terrainData.size.x * 0.5f, 0, t.terrainData.size.z * 0.5f);
                    centro.y = t.SampleHeight(centro) + t.transform.position.y; achou = true;
                }
            }
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) Object.DestroyImmediate(c.gameObject);
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) Object.DestroyImmediate(l);

            Vector3 spawn = centro + Vector3.up * 1.2f;
            var jogador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            jogador.transform.position = spawn;
            var camGo = new GameObject("Câmera Principal"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.nearClipPlane = 0.04f; cam.farClipPlane = 800f;
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            Ligar(tpc, "target", jogador.transform);
            var hud = new GameObject("HUD").AddComponent<HUDJogo>();
            Ligar(hud, "jogador", jogador.transform);
            if (!Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l => l.type == LightType.Directional))
            {
                var lg = new GameObject("Luz Direcional").AddComponent<Light>();
                lg.type = LightType.Directional; lg.shadows = LightShadows.Soft; lg.transform.rotation = Quaternion.Euler(45, -30, 0);
            }
            var vgo = new GameObject("Pós-processamento");
            var vol = vgo.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = GraficosUpgrade.PerfilPos();

            Portal("Portal do Treino", "Treino", spawn + new Vector3(6, 0, 4), new Color(0.2f, 0.7f, 1f));
            Portal("Portal da Fase 1", "Fase1_Corrida", spawn + new Vector3(-6, 0, 4), new Color(1f, 0.5f, 0.2f));
            Galeria(spawn + new Vector3(0, 0, 14));

            EditorSceneManager.SaveScene(cena, Destino);
            var lista = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();
            foreach (var p in new[] { Destino, "Assets/_FaithClash/Scenes/Treino.unity", "Assets/_FaithClash/Scenes/Fase1_Corrida.unity" })
                if (File.Exists(p) && !lista.Any(s => s.path == p)) lista.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log("[Faith Clash] Lobby criado em " + Destino + " (galeria + portais)");
        }

        static void Portal(string nome, string cena, Vector3 pos, Color cor)
        {
            if (Physics.Raycast(pos + Vector3.up * 50, Vector3.down, out var h, 200f)) pos = h.point;
            var go = new GameObject(nome); go.transform.position = pos;
            var col = go.AddComponent<CapsuleCollider>(); col.isTrigger = true; col.height = 3f; col.radius = 1.2f; col.center = Vector3.up * 1.5f;
            go.AddComponent<PortalCena>().cena = cena;
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder); vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.up * 1.5f; vis.transform.localScale = new Vector3(2.2f, 1.5f, 2.2f);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(cor.r, cor.g, cor.b, 0.35f));
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", cor * 2.5f);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_SrcBlend", 5); m.SetFloat("_DstBlend", 10); m.SetFloat("_ZWrite", 0);
            m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(m, "Assets/_FaithClash/Materials/" + nome.Replace(' ', '_') + ".mat");
            vis.GetComponent<Renderer>().sharedMaterial = m;
            Rotulo(nome, go.transform.position + Vector3.up * 3.6f, 1.2f);
        }

        static void Rotulo(string texto, Vector3 pos, float tam)
        {
            var go = new GameObject("Rótulo: " + texto); go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = texto; tm.characterSize = 0.12f * tam; tm.fontSize = 60; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.font = f; go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            go.AddComponent<RotuloOlhaCamera>();
        }

        /// <summary>Uma fileira por pacote, com o nome em cima: serve para ver o que cada asset é antes de apagar.</summary>
        static void Galeria(Vector3 origem)
        {
            var grupos = new (string pasta, string titulo, int max)[]
            {
                ("Assets/Props/Sete Espadas", "Sete Espadas", 7), ("Assets/Props/Espada Samurai", "Espada Samurai", 3), ("Assets/Props/Arvore Carvalho", "Árvore Carvalho", 2),
                ("Assets/Efeitos/Efeitos de Magia", "Efeitos de Magia", 4), ("Assets/Efeitos/Efeitos Cartoon", "Efeitos Cartoon", 4), ("Assets/Efeitos/Fogo e Rastros", "Fogo e Rastros", 4),
                ("Assets/Efeitos/Pacote de Efeitos de Jogo 3D", "Efeitos de Jogo 3D", 5), ("Assets/Efeitos/Colecao de Efeitos de Jogo", "Coleção de Efeitos", 4),
                ("Assets/Efeitos/Efeito Buraco Negro", "Buraco Negro", 2), ("Assets/Efeitos/Efeitos Rapidos", "Efeitos Rápidos", 3), ("Assets/Efeitos/Efeitos Simples", "Efeitos Simples", 3),
                ("Assets/Efeitos/Efeito de Sangue", "Sangue", 2), ("Assets/Efeitos/Efeitos de Aura", "Aura", 3), ("Assets/Efeitos/Particulas da Unity", "Partículas da Unity", 3),
            };
            var raiz = new GameObject("Galeria de Assets").transform; raiz.position = origem;
            for (int g = 0; g < grupos.Length; g++)
            {
                var (pasta, titulo, max) = grupos[g];
                if (!AssetDatabase.IsValidFolder(pasta)) continue;
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { pasta }).Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => { var l = p.ToLowerInvariant(); return !l.Contains("demo") && !l.Contains("example") && !l.Contains("_lod") && !l.Contains("parts") && !l.Contains("/ui") && !l.Contains("canvas"); })
                    .Take(max).ToList();
                float x0 = (g % 2 == 0 ? -1 : 1) * 4f, z = (g / 2) * 5f;
                Rotulo(titulo, origem + new Vector3(x0, 3.2f, z), 1f);
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]), raiz);
                    var p = origem + new Vector3(x0 + (g % 2 == 0 ? -i : i) * 2.2f, 0, z);
                    if (Physics.Raycast(p + Vector3.up * 50, Vector3.down, out var h, 200f)) p = h.point;
                    go.transform.position = p;
                }
            }
        }

        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo); var pr = so.FindProperty(campo);
            if (pr != null) { pr.objectReferenceValue = valor; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif
