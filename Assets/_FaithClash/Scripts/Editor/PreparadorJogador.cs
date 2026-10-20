using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Menu: Faith Clash > Preparar Jogador.
    /// Importa o personagem do Mixamo (rosto remodelado) como Humanoid, aplica todas as animações do Mixamo
    /// (cortadas em golpes), monta o Animator e gera o prefab do jogador com a katana na mão.
    /// </summary>
    public static class PreparadorJogador
    {
        const string Pasta = "Assets/_FaithClash/Art/Characters/Jogador";
        const string PastaAnim = Pasta + "/Animacoes";
        const string Fbx = Pasta + "/Jogador.fbx";
        const string FbxKatana = "Assets/_FaithClash/Art/Weapons/Katana.fbx";
        const string PastaMat = "Assets/_FaithClash/Materials/Jogador";
        const string CaminhoController = "Assets/_FaithClash/Animations/Jogador.controller";
        public const string CaminhoPrefab = "Assets/_FaithClash/Prefabs/Characters/Jogador.prefab";

        // arquivo (sem extensão) -> clipes: nome, frame inicial, frame final (relativos ao início), loop
        static readonly Dictionary<string, (string nome, int ini, int fim, bool loop)[]> Recortes = new Dictionary<string, (string, int, int, bool)[]>
        {
            { "MudarDirecao", new[] { ("MudarDirecao", 0, 65, false) } },
            { "AgachadoParaCorrida", new[] { ("Dash", 0, 15, false) } },
            { "SacarEspada", new[] { ("SacarEspada", 0, 24, false) } },
            { "ComboSoco", new[] { ("Soco1", 0, 28, false), ("Soco2", 29, 56, false), ("Soco3", 57, 80, false) } },
            { "Virar180Espada", new[] { ("Virar180", 0, 17, false) } },
            { "EspadaSaltoAtaque", new[] { ("EspadaSalto", 0, 65, false) } },
            { "EspadaCorte", new[] { ("Corte1", 0, 36, false), ("Corte2", 37, 64, false), ("Corte3", 65, 106, false) } },
            { "ParadoParaCorrida", new[] { ("ParadoParaCorrida", 0, 24, false) } },
            { "Pulo", new[] { ("Pulo", 0, 40, false) } },
            { "Aterrissagem", new[] { ("Aterrissagem", 0, 36, false) } },
            { "PuloAr", new[] { ("PuloAr", 0, 35, true) } },
            { "Martelo", new[] { ("Martelo", 0, 39, false) } },
            { "PegarItem", new[] { ("PegarItem", 0, 36, false) } },
            { "CorrerFrente", new[] { ("CorrerFrente", 0, 27, true) } },
            { "Deslizar", new[] { ("Deslizar", 0, 46, false) } },
            { "Sprint", new[] { ("Sprint", 0, 16, true) } },
            { "CorridaPadrao", new[] { ("CorridaPadrao", 0, 22, true) } },
            { "IniciarAndar", new[] { ("IniciarAndar", 0, 40, false) } },
            { "EspadaEscudoAtaque", new[] { ("Estocada", 0, 70, false) } },
            { "EspadaEscudoVirar", new[] { ("EspadaVirar", 0, 28, false) } },
            { "CorridaParede", new[] { ("CorridaParede", 0, 28, true) } },
        };

        // estados soltos do Animator: clipe -> velocidade de reprodução
        static readonly (string estado, string clipe, float vel)[] Estados =
        {
            ("Corte1", "Corte1", 1.7f), ("Corte2", "Corte2", 1.7f), ("Corte3", "Corte3", 1.7f), ("Estocada", "Estocada", 1.8f),
            ("EspadaSalto", "EspadaSalto", 1.6f), ("Soco1", "Soco1", 1.5f), ("Soco2", "Soco2", 1.5f), ("Soco3", "Soco3", 1.4f),
            ("Martelo", "Martelo", 1.5f), ("Dash", "Dash", 1.8f), ("Deslizar", "Deslizar", 1.1f), ("SacarEspada", "SacarEspada", 1.4f),
            ("Virar180", "Virar180", 1.6f), ("EspadaVirar", "EspadaVirar", 1.5f), ("PegarItem", "PegarItem", 1.3f),
            ("Pulo", "Pulo", 1.3f), ("PuloAr", "PuloAr", 1f), ("Aterrissagem", "Aterrissagem", 1.4f),
            ("ParadoParaCorrida", "ParadoParaCorrida", 1.6f), ("IniciarAndar", "IniciarAndar", 1.8f), ("MudarDirecao", "MudarDirecao", 2f),
            ("CorridaParede", "CorridaParede", 1.2f), ("Defesa", "Defesa", 1f), ("Dano", "Dano", 1.3f), ("Morte", "Morte", 1f),
        };

        static readonly HashSet<string> LoopsBase = new HashSet<string> { "Parado", "Andar", "Defesa" };

        [MenuItem("Faith Clash/Preparar Jogador")]
        public static void Preparar()
        {
            if (!File.Exists(Fbx)) { Debug.LogError("[Faith Clash] FBX não encontrado: " + Fbx); return; }
            var avatar = ConfigurarModelo();
            ConfigurarAnimacoes(avatar);
            var controller = CriarAnimator();
            CriarPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Jogador pronto: " + CaminhoPrefab);
        }

        // ------------------------------------------------------------------ modelo e materiais
        static Material Mat(string nome, Color cor, float suav, string baseMap = null, string normal = null)
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
            m.SetFloat("_Smoothness", suav);
            m.SetFloat("_Metallic", 0f);
            if (baseMap != null) m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap));
            if (normal != null)
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Avatar ConfigurarModelo()
        {
            var materiais = new Dictionary<string, Material>
            {
                { "Ch06_body", Mat("Roupa", Color.white, 0.25f, Pasta + "/Ch06_1001_Diffuse.png", Pasta + "/Ch06_1001_Normal.png") },
                { "Ch06_body1", Mat("Tenis", Color.white, 0.3f, Pasta + "/Ch06_1002_Diffuse.png", Pasta + "/Ch06_1002_Normal.png") },
                { "Pele", Mat("Pele", new Color(0.6f, 0.42f, 0.32f), 0.3f) },
                { "Rosto", Mat("Rosto", Color.white, 0.25f, Pasta + "/Rosto.png") },
                { "Cabelo", Mat("Cabelo", new Color(0.035f, 0.032f, 0.035f), 0.45f) },
            };
            var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var kv in materiais)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            Debug.Log("[Faith Clash] Avatar do jogador válido: " + (avatar != null && avatar.isValid && avatar.isHuman));
            return avatar;
        }

        // ------------------------------------------------------------------ animações
        static void ConfigurarAnimacoes(Avatar avatarJogador)
        {
            foreach (var arquivo in Directory.GetFiles(PastaAnim, "*.fbx"))
            {
                string caminho = arquivo.Replace('\\', '/');
                string chave = Path.GetFileNameWithoutExtension(caminho);
                var imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
                if (imp == null) continue;
                imp.animationType = ModelImporterAnimationType.Human;
                bool baseAntiga = chave == "AnimacoesBase";
                // cada arquivo cria o próprio avatar Humanoid (os ossos têm os mesmos nomes do Mixamo)
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();

                imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
                var padrao = imp.defaultClipAnimations;
                var clips = new List<ModelImporterClipAnimation>();
                if (baseAntiga)
                {
                    foreach (var c in padrao)
                    {
                        string nome = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
                        c.name = nome;
                        c.loopTime = c.loopPose = LoopsBase.Contains(nome);
                        c.lockRootRotation = true; c.lockRootPositionXZ = true; c.lockRootHeightY = false;
                        clips.Add(c);
                    }
                }
                else if (Recortes.TryGetValue(chave, out var cortes) && padrao.Length > 0)
                {
                    var b = padrao[0];
                    foreach (var (nome, ini, fim, loop) in cortes)
                    {
                        var c = new ModelImporterClipAnimation
                        {
                            name = nome,
                            takeName = b.takeName,
                            firstFrame = b.firstFrame + ini,
                            lastFrame = b.firstFrame + fim,
                            loopTime = loop,
                            loopPose = loop,
                            lockRootRotation = true,
                            lockRootPositionXZ = true,
                            lockRootHeightY = false,
                            keepOriginalOrientation = false,
                            keepOriginalPositionXZ = false,
                            keepOriginalPositionY = false,
                        };
                        clips.Add(c);
                    }
                }
                imp.clipAnimations = clips.ToArray();
                imp.SaveAndReimport();
            }
        }

        static Dictionary<string, AnimationClip> TodosClipes()
        {
            var d = new Dictionary<string, AnimationClip>();
            foreach (var arquivo in Directory.GetFiles(PastaAnim, "*.fbx"))
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(arquivo.Replace('\\', '/')).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview__")) d[c.name] = c;
            return d;
        }

        static AnimatorController CriarAnimator()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoController));
            if (File.Exists(CaminhoController)) AssetDatabase.DeleteAsset(CaminhoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            var clips = TodosClipes();
            Debug.Log("[Faith Clash] Clipes: " + string.Join(", ", clips.Keys.OrderBy(k => k)));
            var sm = ctrl.layers[0].stateMachine;

            var loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree arvore);
            arvore.blendParameter = "Velocidade";
            arvore.useAutomaticThresholds = false;
            void Add(string clipe, float limiar) { if (clips.TryGetValue(clipe, out var c)) arvore.AddChild(c, limiar); else Debug.LogWarning("[Faith Clash] Clipe ausente: " + clipe); }
            Add("Parado", 0f);
            Add("Andar", 2.5f);
            Add("CorridaPadrao", 5.5f);
            Add("Sprint", 9f);
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            foreach (var (estado, clipe, vel) in Estados)
            {
                if (!clips.TryGetValue(clipe, out var clip)) { Debug.LogWarning("[Faith Clash] Clipe ausente: " + clipe); continue; }
                var st = sm.AddState(estado);
                st.motion = clip;
                st.speed = vel;
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // ------------------------------------------------------------------ prefab
        static void CriarPrefab(AnimatorController ctrl)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoPrefab));
            var raiz = new GameObject("Jogador");
            raiz.transform.position = new Vector3(0, 0.1f, 0);
            var cc = raiz.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            var pc = raiz.AddComponent<PlayerController>();
            var vida = raiz.AddComponent<Health>();
            var combate = raiz.AddComponent<MeleeCombat>();
            var anim = raiz.AddComponent<AnimadorProtagonista>();
            raiz.AddComponent<StatusJogador>();
            raiz.AddComponent<EstilosElementais>();
            raiz.AddComponent<TravaAlvo>();
            raiz.AddComponent<ModoCombate>();
            var vso = new SerializedObject(vida);
            vso.FindProperty("maxHealth").floatValue = 1850f;
            vso.ApplyModifiedPropertiesWithoutUndo();

            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
            modelo.name = "Modelo";
            modelo.transform.SetParent(raiz.transform, false);
            var animator = modelo.GetComponent<Animator>();
            if (animator == null) animator = modelo.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) animator.avatar = avatar;

            AnexarKatana(modelo, animator);

            Ligar(pc, "animador", anim);
            Ligar(combate, "animador", anim);
            Ligar(anim, "animator", animator);
            Ligar(anim, "jogador", pc);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Object.DestroyImmediate(raiz);
        }

        /// <summary>Katana presa na mão direita; a orientação é calculada a partir da pose do golpe de espada.</summary>
        static void AnexarKatana(GameObject modelo, Animator animator)
        {
            var katanaFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxKatana);
            if (katanaFbx == null || animator == null || !animator.isHuman) return;
            var mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var clipes = TodosClipes();

            // direção do cabo no espaço local da mão direita (mão esquerda no pomo, mão direita à frente)
            Vector3 eixoLocal = Vector3.up;
            Vector3 posLocal = Vector3.zero;
            if (clipes.TryGetValue("Corte1", out var corte))
            {
                // amostra a pose numa cópia temporária (o prefab final fica na pose original)
                var temp = Object.Instantiate(modelo);
                var anim = temp.GetComponent<Animator>();
                var m1 = anim.GetBoneTransform(HumanBodyBones.RightHand);
                var m2 = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                corte.SampleAnimation(temp, 0.4f);
                Vector3 d = (m1.position - m2.position).normalized;
                eixoLocal = m1.InverseTransformDirection(d).normalized;
                posLocal = m1.InverseTransformPoint(m1.position - d * 0.02f);
                Object.DestroyImmediate(temp);
                Debug.Log("[Faith Clash] Eixo da katana (local da mão): " + eixoLocal);
            }

            var k = (GameObject)PrefabUtility.InstantiatePrefab(katanaFbx);
            k.name = "Katana";
            k.transform.SetParent(mao, false);
            // o FBX da katana tem a lâmina em +Z (Blender +Y)
            k.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, eixoLocal);
            k.transform.localPosition = posLocal;
            k.transform.localScale = Vector3.one / Mathf.Max(0.0001f, mao.lossyScale.x);   // os ossos do Mixamo vêm com escala 0,01
            Debug.Log("[Faith Clash] Katana: escala do mundo " + k.transform.lossyScale + " | mão: " + mao.lossyScale + " | modelo: " + modelo.transform.lossyScale);
            var rends = k.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0) Debug.Log("[Faith Clash] Katana tamanho (mundo): " + rends[0].bounds.size);
            k.SetActive(false);
        }

        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
