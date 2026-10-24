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

            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
            chao.name = "Chão";
            chao.transform.localScale = new Vector3(5f, 1f, 5f);
            chao.GetComponent<Renderer>().sharedMaterial = Mat("Chao", new Color(0.28f, 0.3f, 0.33f));

            var matParede = Mat("Parede", new Color(0.18f, 0.19f, 0.22f));
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

            var bonecos = new (string nome, Vector3 pos)[]
            {
                ("Prelado Corrompido", new Vector3(0, 0, 7)),
                ("Sentinela do Lago", new Vector3(-6, 0, 11)),
                ("Colosso Corrompido", new Vector3(6, 0, 11)),
            };
            var matBoneco = Mat("Boneco", new Color(0.85f, 0.55f, 0.2f));
            foreach (var (nome, p) in bonecos)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                d.name = nome;
                d.transform.position = p + Vector3.up;
                var r = d.GetComponent<Renderer>();
                r.sharedMaterial = matBoneco;
                var h = d.AddComponent<Health>();
                var hs = new SerializedObject(h);
                hs.FindProperty("maxHealth").floatValue = 1200f;
                hs.ApplyModifiedPropertiesWithoutUndo();
                var boneco = d.AddComponent<TrainingDummy>();
                var dso = new SerializedObject(boneco);
                dso.FindProperty("body").objectReferenceValue = r;
                dso.FindProperty("nome").stringValue = nome;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }

            CriarObstaculos(matParede);
            var testadores = new GameObject("Testador de Animações");
            testadores.AddComponent<TestadorAnimacoes>();
            testadores.AddComponent<TestadorCombate>();
            new GameObject("Gerenciador de Treino").AddComponent<TrainingManager>();
            var hud = new GameObject("HUD").AddComponent<HUDJogo>();
            Ligar(hud, "jogador", jogador.transform);

            Directory.CreateDirectory(PastaCena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AdicionarAosBuildSettings(CaminhoCena);
            Debug.Log("[Faith Clash] Cena de treino criada em " + CaminhoCena + ". Aperte Play!");
        }

        /// <summary>Rampa, degraus e caixotes para testar colisão, gravidade e os pés no chão.</summary>
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
