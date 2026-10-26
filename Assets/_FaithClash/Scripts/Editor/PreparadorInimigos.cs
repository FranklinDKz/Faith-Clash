using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Training;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Menu: Faith Clash > Preparar Inimigos. Troca os cilindros do treino pelos personagens do Adam Character Pack (Guarda, Lu e Adam),
    /// monta o Animator dos inimigos (idle de luta, andar, correr, ataques, reações, derrubado e mortes) e gera os prefabs.
    /// As animações são as mesmas do jogador (Mixamo, Quaternius) mais o ataque de inimigo do pacote DoubleL; tudo retargeta no avatar Humanoid de cada modelo.
    /// </summary>
    public static class PreparadorInimigos
    {
        const string Pacote = "Assets/Personagens/Personagens Adam";
        const string PastaAnim = "Assets/_FaithClash/Art/Characters/Jogador/Anim";
        const string PastaPrefab = "Assets/_FaithClash/Prefabs/Characters/Inimigos";
        const string Controller = "Assets/_FaithClash/Animations/Inimigo.controller";
        const string DoubleLEnemy = "Assets/Animacoes/Animacoes de RPG/FBX Unity/Enemy Attack/Enemy_Attack_1.fbx";

        public static readonly string[] Todos = { "Guarda", "Lu", "Adam" };

        sealed class Modelo
        {
            public string prefab, fbx, nome, subtitulo;
            public int nivel;
            public float escala, vida;
            public (string clip, float dano, float alcance, float vel)[] ataques;
        }

        static readonly Dictionary<string, Modelo> Modelos = new Dictionary<string, Modelo>
        {
            { "Guarda", new Modelo { prefab = "Inimigo_Guarda", fbx = Pacote + "/Guard/Guard.FBX", nome = "Prelado Corrompido", subtitulo = "Fé Maculada", nivel = 26, escala = 1f, vida = 1200,
                ataques = new[] { ("DoubleL_Enemy_Attack_1", 45f, 2.0f, 1f), ("Mx_Standing_Melee_Punch", 30f, 1.8f, 1f), ("Mx_Mma_Kick", 40f, 2.0f, 1f) } } },
            { "Lu", new Modelo { prefab = "Inimigo_Lu", fbx = Pacote + "/Lu/Lu.FBX", nome = "Sentinela do Lago", subtitulo = "Guardiã das Águas", nivel = 28, escala = 1f, vida = 1400,
                ataques = new[] { ("Mx_Roundhouse_Kick", 45f, 2.1f, 1.1f), ("Mx_Hook_Punch", 35f, 1.8f, 1.1f), ("Mx_Martelo_3", 50f, 2.0f, 1f) } } },
            { "Adam", new Modelo { prefab = "Inimigo_Adam", fbx = Pacote + "/Adam/Adam.FBX", nome = "Colosso Corrompido", subtitulo = "Titã de Ferro", nivel = 32, escala = 1.18f, vida = 2400,
                ataques = new[] { ("Mx_Illegal_Elbow_Punch", 60f, 2.0f, 0.9f), ("Mx_Cross_Punch", 55f, 2.0f, 0.9f), ("Mx_Standing_Melee_Kick", 65f, 2.2f, 0.9f) } } },
        };

        [MenuItem("Faith Clash/Preparar Inimigos")]
        public static void Preparar()
        {
            AssetDatabase.Refresh();
            ConfigurarAnimacaoDoubleL();
            var ctrl = CriarController();
            Directory.CreateDirectory(PastaPrefab);
            foreach (var k in Todos) CriarPrefab(Modelos[k], ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Inimigos prontos: " + PastaPrefab);
        }

        public static string CaminhoPrefab(string chave) => $"{PastaPrefab}/{Modelos[chave].prefab}.prefab";
        public static (string nome, string chave)[] Elenco => new[] { ("Prelado Corrompido", "Guarda"), ("Sentinela do Lago", "Lu"), ("Colosso Corrompido", "Adam") };

        static void ConfigurarAnimacaoDoubleL()
        {
            var imp = AssetImporter.GetAtPath(DoubleLEnemy) as ModelImporter;
            if (imp == null) return;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            imp = (ModelImporter)AssetImporter.GetAtPath(DoubleLEnemy);
            var clips = imp.defaultClipAnimations;
            if (clips.Length == 0) return;
            var c = clips[0];
            c.name = "DoubleL_Enemy_Attack_1";
            c.lockRootHeightY = true; c.keepOriginalPositionY = true; c.lockRootPositionXZ = true; c.lockRootRotation = true;
            imp.clipAnimations = new[] { c };
            imp.SaveAndReimport();
        }

        static Dictionary<string, AnimationClip> Clipes()
        {
            var d = new Dictionary<string, AnimationClip>();
            var fbxs = Directory.Exists(PastaAnim) ? Directory.GetFiles(PastaAnim, "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).ToList() : new List<string>();
            fbxs.Add(DoubleLEnemy);
            foreach (var f in fbxs)
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(f).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview__")) d[c.name] = c;
            return d;
        }

        static AnimatorController CriarController()
        {
            if (Directory.Exists(AnimacoesMartial.PastaMartial)) return AnimacoesMartial.CriarInimigo(Controller);
            Directory.CreateDirectory(Path.GetDirectoryName(Controller));
            if (File.Exists(Controller)) AssetDatabase.DeleteAsset(Controller);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            var sm = ctrl.layers[0].stateMachine;
            var clipes = Clipes();
            var nomes = new HashSet<string> { "Mx_Fighting_Idle", "Andar", "Correr", "Mx_Livershot_Knockdown", "Mx_Big_Hit_To_Head", "Mx_Big_Stomach_Hit", "Mx_Medium_Hit_To_Head",
                "U_InimigoRaio", "U_InimigoShinra", "Mx_Hit_To_Body", "Mx_Light_Hit_To_Head", "Mx_Standing_React_Small_From_Front", "Mx_Receive_Punch_To_The_Face", "Mx_Taking_Punch", "Mx_Zombie_Reaction_Hit", "Mx_Standing_React_Small_From_Back", "Mx_Hit_To_The_Legs", "Mx_Standing_React_Small_From_Left", "Mx_Hit_On_Side_Of_Body", "Mx_Side_Hit", "Mx_Standing_React_Small_From_Right", "Mx_Hit_On_Side_Of_Head", "Mx_Standing_React_Large_From_Front", "Mx_Standing_React_Large_From_Back", "Mx_Stumble_Backwards", "Mx_Standing_React_Large_From_Left", "Mx_Standing_React_Large_From_Right", "Mx_Big_Side_Hit", "Mx_Getting_Thrown", "Mx_Shoved_Reaction_With_Spin", "Mx_Falling_Flat_Impact", "Mx_Standing_React_Death_Backward", "Mx_Standing_React_Death_Forward", "Mx_Falling_Back_Death" };
            foreach (var m in Modelos.Values) foreach (var a in m.ataques) nomes.Add(a.clip);
            foreach (var n in nomes)
            {
                if (!clipes.TryGetValue(n, out var clip)) { Debug.LogWarning("[Faith Clash] Clipe de inimigo ausente: " + n); continue; }
                var st = sm.AddState(n);
                st.motion = clip;
            }
            if (!sm.states.Any(s => s.state.name == "Mx_Fighting_Idle")) sm.AddState("Mx_Fighting_Idle");
            sm.defaultState = sm.states.FirstOrDefault(s => s.state.name == "Mx_Fighting_Idle").state;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void CriarPrefab(Modelo m, AnimatorController ctrl)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(m.fbx);
            if (fbx == null) { Debug.LogError("[Faith Clash] Modelo não encontrado: " + m.fbx); return; }
            var raiz = new GameObject(m.prefab);
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            modelo.name = "Modelo";
            modelo.transform.SetParent(raiz.transform, false);
            // esconde armas soltas do pacote (rifle, lança): o treino é corpo a corpo
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("rifle") || n.Contains("pilum") || n.Contains("wpn") || n.Contains("weapon")) r.gameObject.SetActive(false);
            }
            var anim = modelo.GetComponent<Animator>() ?? modelo.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(m.fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) anim.avatar = avatar;
            raiz.transform.localScale = Vector3.one * m.escala;

            var cc = raiz.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.92f, 0); cc.stepOffset = 0.35f; cc.skinWidth = 0.04f;
            var h = raiz.AddComponent<Health>();
            var hs = new SerializedObject(h); hs.FindProperty("maxHealth").floatValue = m.vida; hs.ApplyModifiedPropertiesWithoutUndo();
            var d = raiz.AddComponent<TrainingDummy>();
            var ds = new SerializedObject(d);
            ds.FindProperty("nome").stringValue = m.nome;
            ds.FindProperty("subtitulo").stringValue = m.subtitulo;
            ds.FindProperty("nivel").intValue = m.nivel;
            ds.ApplyModifiedPropertiesWithoutUndo();
            var ia = raiz.AddComponent<InimigoIA>();
            var iso = new SerializedObject(ia);
            iso.FindProperty("animator").objectReferenceValue = anim;
            var arr = iso.FindProperty("ataques");
            arr.arraySize = m.ataques.Length;
            for (int i = 0; i < m.ataques.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("clip").stringValue = m.ataques[i].clip;
                e.FindPropertyRelative("dano").floatValue = m.ataques[i].dano;
                e.FindPropertyRelative("alcance").floatValue = m.ataques[i].alcance;
                e.FindPropertyRelative("velocidade").floatValue = m.ataques[i].vel;
            }
            iso.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab(Todos.First(k => Modelos[k] == m)));
            Object.DestroyImmediate(raiz);
        }
    }
}
