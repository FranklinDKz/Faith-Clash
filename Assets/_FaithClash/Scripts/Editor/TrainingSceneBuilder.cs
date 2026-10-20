using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.EditorTools
{
    /// <summary>Menu: Faith Clash > Criar Cena de Treino. Monta a cena de treino com o protagonista e bonecos.</summary>
    public static class TrainingSceneBuilder
    {
        const string PastaCena = "Assets/_FaithClash/Scenes";
        const string PastaMat = "Assets/_FaithClash/Materials";
        const string CaminhoCena = PastaCena + "/Treino.unity";

        [MenuItem("Faith Clash/Criar Cena de Treino")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorProtagonista.CaminhoPrefab);
            if (prefab == null)
            {
                Debug.LogError("[Faith Clash] Rode primeiro: Faith Clash > Preparar Protagonista");
                return;
            }

            var cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var go in cena.GetRootGameObjects())
            {
                if (go.name == "Main Camera") go.name = "Câmera Principal";
                else if (go.name == "Directional Light") go.name = "Luz Direcional";
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
            var combate = jogador.GetComponent<MeleeCombat>();

            var camGo = Camera.main.gameObject;
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            var camSo = new SerializedObject(tpc);
            camSo.FindProperty("target").objectReferenceValue = jogador.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();

            Vector3[] pontos = { new Vector3(0, 0, 5), new Vector3(-4, 0, 7), new Vector3(4, 0, 7) };
            for (int i = 0; i < pontos.Length; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                d.name = "Boneco de Treino " + (i + 1);
                d.transform.position = pontos[i] + Vector3.up;
                var r = d.GetComponent<Renderer>();
                r.sharedMaterial = Mat("Boneco", new Color(0.85f, 0.55f, 0.2f));
                d.AddComponent<Health>();
                var boneco = d.AddComponent<TrainingDummy>();
                var dso = new SerializedObject(boneco);
                dso.FindProperty("body").objectReferenceValue = r;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }

            var mgr = new GameObject("Gerenciador de Treino").AddComponent<TrainingManager>();
            var mso = new SerializedObject(mgr);
            mso.FindProperty("player").objectReferenceValue = combate;
            mso.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(PastaCena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AdicionarAosBuildSettings(CaminhoCena);
            Debug.Log("[Faith Clash] Cena de treino criada em " + CaminhoCena + ". Aperte Play!");
        }

        static Material Mat(string nome, Color cor)
        {
            Directory.CreateDirectory(PastaMat);
            string caminho = $"{PastaMat}/{nome}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", cor);
            AssetDatabase.CreateAsset(m, caminho);
            return m;
        }

        static void AdicionarAosBuildSettings(string caminho)
        {
            var lista = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes)
                if (File.Exists(s.path)) lista.Add(s);
            if (!lista.Exists(s => s.path == caminho)) lista.Add(new EditorBuildSettingsScene(caminho, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
