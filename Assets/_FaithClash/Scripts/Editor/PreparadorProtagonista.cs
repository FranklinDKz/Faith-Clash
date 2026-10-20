using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Player;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Menu: Faith Clash > Preparar Protagonista.
    /// Cria materiais URP, configura o FBX como Humanoid, monta o Animator e gera o prefab do jogador.
    /// </summary>
    public static class PreparadorProtagonista
    {
        const string Pasta = "Assets/_FaithClash/Art/Characters/Protagonista";
        const string Fbx = Pasta + "/Protagonista.fbx";
        const string PastaMat = "Assets/_FaithClash/Materials/Protagonista";
        const string CaminhoController = "Assets/_FaithClash/Animations/Protagonista.controller";
        public const string CaminhoPrefab = "Assets/_FaithClash/Prefabs/Characters/Jogador.prefab";

        static readonly (string nome, Color cor, float suavidade, float metal)[] Materiais =
        {
            ("Pele", new Color(0.56f, 0.37f, 0.26f), 0.35f, 0f),
            ("Cabelo", new Color(0.02f, 0.018f, 0.02f), 0.45f, 0f),
            ("Olho", new Color(0.9f, 0.88f, 0.85f), 0.8f, 0f),
            ("Iris", new Color(0.22f, 0.1f, 0.05f), 0.8f, 0f),
            ("Labios", new Color(0.62f, 0.3f, 0.28f), 0.4f, 0f),
            ("Camisa", new Color(0.05f, 0.05f, 0.06f), 0.15f, 0f),
            ("Calca", new Color(0.16f, 0.17f, 0.2f), 0.15f, 0f),
            ("Bota", new Color(0.03f, 0.03f, 0.03f), 0.35f, 0f),
            ("Aco", new Color(0.78f, 0.8f, 0.85f), 0.8f, 1f),
            ("Cabo", new Color(0.45f, 0.12f, 0.1f), 0.3f, 0f),
            ("Ouro", new Color(0.85f, 0.65f, 0.2f), 0.7f, 1f),
        };

        static readonly HashSet<string> Loop = new HashSet<string> { "Parado", "Andar", "Correr", "Defesa" };

        // velocidade de reprodução para casar a animação com o tempo do golpe no código
        static readonly Dictionary<string, float> Velocidades = new Dictionary<string, float>
        {
            { "Ataque1", 1.04f }, { "Ataque2", 1.04f }, { "Ataque3", 0.97f }, { "AtaquePesado", 0.98f }, { "Esquiva", 3.0f },
        };

        [MenuItem("Faith Clash/Preparar Protagonista")]
        public static void Preparar()
        {
            if (!File.Exists(Fbx)) { Debug.LogError("[Faith Clash] FBX não encontrado: " + Fbx); return; }
            var materiais = CriarMateriais();
            ConfigurarImportador(materiais);
            var controller = CriarAnimator();
            CriarPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Protagonista pronto: " + CaminhoPrefab);
        }

        static Dictionary<string, Material> CriarMateriais()
        {
            Directory.CreateDirectory(PastaMat);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mapa = new Dictionary<string, Material>();
            foreach (var (nome, cor, suav, metal) in Materiais)
            {
                string caminho = $"{PastaMat}/{nome}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
                if (m == null)
                {
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, caminho);
                }
                m.SetColor("_BaseColor", cor);
                m.SetFloat("_Smoothness", suav);
                m.SetFloat("_Metallic", metal);
                EditorUtility.SetDirty(m);
                mapa[nome] = m;
            }
            AssetDatabase.SaveAssets();
            return mapa;
        }

        static void ConfigurarImportador(Dictionary<string, Material> materiais)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.importCameras = false;
            imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var kv in materiais)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            imp.SaveAndReimport();

            imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            var padrao = imp.defaultClipAnimations;
            Debug.Log("[Faith Clash] Clipes no FBX: " + string.Join(", ", padrao.Select(c => c.name)));
            var clips = new List<ModelImporterClipAnimation>();
            foreach (var c in padrao)
            {
                string nome = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
                if (!Velocidades.ContainsKey(nome) && !Loop.Contains(nome) &&
                    nome != "Pular" && nome != "Dano" && nome != "Morte") continue;
                c.name = nome;
                c.loopTime = Loop.Contains(nome);
                c.loopPose = c.loopTime;
                c.lockRootRotation = true;
                c.lockRootPositionXZ = true;
                c.lockRootHeightY = false;
                clips.Add(c);
            }
            imp.clipAnimations = clips.ToArray();
            imp.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            Debug.Log("[Faith Clash] Avatar Humanoid válido: " + (avatar != null && avatar.isValid && avatar.isHuman));
        }

        static AnimatorController CriarAnimator()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoController));
            if (File.Exists(CaminhoController)) AssetDatabase.DeleteAsset(CaminhoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);

            var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name, c => c);
            var sm = ctrl.layers[0].stateMachine;

            var loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree arvore);
            arvore.blendParameter = "Velocidade";
            arvore.useAutomaticThresholds = false;
            if (clips.TryGetValue("Parado", out var parado)) arvore.AddChild(parado, 0f);
            if (clips.TryGetValue("Andar", out var andar)) arvore.AddChild(andar, 4.5f);
            if (clips.TryGetValue("Correr", out var correr)) arvore.AddChild(correr, 7.5f);
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            foreach (var nome in new[] { "Ataque1", "Ataque2", "Ataque3", "AtaquePesado", "Esquiva", "Defesa", "Pular", "Dano", "Morte" })
            {
                if (!clips.TryGetValue(nome, out var clip)) { Debug.LogWarning("[Faith Clash] Clipe ausente: " + nome); continue; }
                var st = sm.AddState(nome);
                st.motion = clip;
                st.speed = Velocidades.TryGetValue(nome, out var v) ? v : 1f;
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void CriarPrefab(AnimatorController ctrl)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoPrefab));
            var raiz = new GameObject("Jogador");
            raiz.transform.position = new Vector3(0, 1.1f, 0);
            var cc = raiz.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            var pc = raiz.AddComponent<PlayerController>();
            var combate = raiz.AddComponent<MeleeCombat>();
            var anim = raiz.AddComponent<AnimadorProtagonista>();

            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
            modelo.name = "Modelo";
            modelo.transform.SetParent(raiz.transform, false);
            var animator = modelo.GetComponent<Animator>();
            if (animator == null) animator = modelo.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) animator.avatar = avatar;

            // No Humanoid o Animator já orienta o personagem para +Z; não girar o modelo.
            Ligar(pc, "animador", anim);
            Ligar(combate, "animador", anim);
            Ligar(anim, "animator", animator);
            Ligar(anim, "jogador", pc);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Object.DestroyImmediate(raiz);
        }

        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
