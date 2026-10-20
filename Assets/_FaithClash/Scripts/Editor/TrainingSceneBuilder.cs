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
    /// <summary>Menu: Faith Clash > Criar Cena de Treino. Monta uma cena de treino jogável com formas primitivas.</summary>
    public static class TrainingSceneBuilder
    {
        const string SceneDir = "Assets/_FaithClash/Scenes";
        const string MatDir = "Assets/_FaithClash/Materials";
        const string ScenePath = SceneDir + "/Training.unity";

        [MenuItem("Faith Clash/Criar Cena de Treino")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Ground", new Color(0.28f, 0.3f, 0.33f));

            // Paredes da arena
            var wallMat = Mat("Wall", new Color(0.18f, 0.19f, 0.22f));
            float half = 25f;
            for (int i = 0; i < 4; i++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = "Wall" + i;
                bool ns = i < 2;
                float s = (i % 2 == 0 ? 1 : -1) * half;
                w.transform.position = ns ? new Vector3(0, 2, s) : new Vector3(s, 2, 0);
                w.transform.localScale = ns ? new Vector3(half * 2, 4, 1) : new Vector3(1, 4, half * 2);
                w.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            // Player
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.1f, 0);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            player.AddComponent<PlayerController>();
            var combat = player.AddComponent<MeleeCombat>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("Player", new Color(0.15f, 0.45f, 0.9f));

            var pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0, 1.1f, 0);
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Katana";
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            blade.transform.SetParent(pivot, false);
            blade.transform.localPosition = new Vector3(0, 0, 0.8f);
            blade.transform.localScale = new Vector3(0.06f, 0.06f, 1.4f);
            blade.GetComponent<Renderer>().sharedMaterial = Mat("Blade", new Color(0.85f, 0.88f, 0.95f));

            var so = new SerializedObject(combat);
            so.FindProperty("weaponPivot").objectReferenceValue = pivot;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Câmera
            var camGo = Camera.main.gameObject;
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            var camSo = new SerializedObject(tpc);
            camSo.FindProperty("target").objectReferenceValue = player.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();

            // Bonecos de treino
            Vector3[] spots = { new Vector3(0, 0, 5), new Vector3(-4, 0, 7), new Vector3(4, 0, 7) };
            for (int i = 0; i < spots.Length; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                d.name = "TrainingDummy" + (i + 1);
                d.transform.position = spots[i] + Vector3.up;
                var r = d.GetComponent<Renderer>();
                r.sharedMaterial = Mat("Dummy", new Color(0.85f, 0.55f, 0.2f));
                d.AddComponent<Health>();
                var dummy = d.AddComponent<TrainingDummy>();
                var dso = new SerializedObject(dummy);
                dso.FindProperty("body").objectReferenceValue = r;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Gerenciador
            var mgr = new GameObject("TrainingManager").AddComponent<TrainingManager>();
            var mso = new SerializedObject(mgr);
            mso.FindProperty("player").objectReferenceValue = combat;
            mso.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("[Faith Clash] Cena de treino criada em " + ScenePath + ". Aperte Play!");
        }

        static Material Mat(string name, Color color)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            m.SetColor("_BaseColor", color);
            m.color = color;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
