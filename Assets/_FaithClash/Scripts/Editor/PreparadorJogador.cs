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
    /// Importa o personagem (Humanoid), corta as animações do Mixamo em clipes por função, monta o Animator
    /// e gera o prefab do jogador (com katana, IK dos pés e movimento da animação).
    /// As animações mantêm a altura e a velocidade originais do Mixamo; o deslocamento horizontal dos golpes
    /// é aplicado pelo script MovimentoDaAnimacao (com colisão).
    /// </summary>
    public static class PreparadorJogador
    {
        const string Pasta = "Assets/_FaithClash/Art/Characters/Jogador";
        const string PastaAnim = Pasta + "/Animacoes2";
        const string PastaAnimBase = Pasta + "/Animacoes";
        const string Fbx = Pasta + "/Jogador.fbx";
        const string FbxKatana = "Assets/_FaithClash/Art/Weapons/Katana.fbx";
        const string PastaMat = "Assets/_FaithClash/Materials/Jogador";
        const string CaminhoController = "Assets/_FaithClash/Animations/Jogador.controller";
        public const string CaminhoPrefab = "Assets/_FaithClash/Prefabs/Characters/Jogador.prefab";

        // arquivo (sem extensão) -> clipes: nome, frame inicial, frame final (-1 = até o fim; 30 fps), loop
        static readonly Dictionary<string, (string nome, int ini, int fim, bool loop)[]> Recortes = new Dictionary<string, (string, int, int, bool)[]>
        {
            // ---- parado / locomoção
            { "OffensiveIdle", new[] { ("IdlePoder", 0, -1, true) } },
            { "OrcIdle", new[] { ("IdleEspada", 0, -1, true) } },
            { "HappyIdle", new[] { ("IdleDescanso", 0, -1, true) } },
            { "StandardRun_Antiga", new[] { ("Correr", 0, 22, true) } },
            { "Sprint_Antiga", new[] { ("Sprint", 0, 16, true) } },
            { "GreatSwordRun", new[] { ("CorrerEspada", 0, 18, true) } },
            { "RunningBackward", new[] { ("CorrerTras", 0, 19, true) } },
            { "WalkingBackwards", new[] { ("AndarTras", 0, 36, true) } },
            { "InjuredWalking", new[] { ("AndarFerido", 0, 30, true) } },
            { "IdleToSprint_Antiga", new[] { ("ParadoParaCorrida", 0, 24, false) } },
            { "StartWalking_Antiga", new[] { ("IniciarAndar", 0, 40, false) } },
            { "ChangeDirection", new[] { ("MudarDirecao", 0, 65, false) } },
            { "Transition", new[] { ("Transicao", 0, -1, false) } },
            // ---- movimento especial
            { "CrouchedToSprinting_Antiga", new[] { ("Dash", 0, 15, false) } },
            { "RunningSlide_Antiga", new[] { ("Deslizar", 0, 46, false) } },
            { "Running", new[] { ("Rolamento", 0, 64, false) } },
            { "Jump_Antiga", new[] { ("Pulo", 0, 40, false) } },
            { "Jumping_Antiga", new[] { ("PuloAr", 0, 35, true) } },
            { "JumpingDown", new[] { ("PousoPesado", 0, 60, false) } },
            { "FallingToRoll", new[] { ("PousoRolamento", 0, -1, false) } },
            { "WallRun_Antiga", new[] { ("CorridaParede", 0, 28, true) } },
            { "GreatSword180Turn", new[] { ("Virar180Espada", 0, 24, false) } },
            { "SwordAndShield180Turn", new[] { ("Virar180Poder", 0, 26, false) } },
            { "SwordAndShieldTurn_Antiga", new[] { ("Virar90", 0, 29, false) } },
            // ---- espada
            { "DrawAGreatSword2", new[] { ("SacarEspada", 0, 24, false) } },
            { "SheathAGreatSword2", new[] { ("EnvainharEspada", 0, 34, false) } },
            { "GreatSwordSlash", new[] { ("Corte1", 0, 36, false), ("Corte2", 37, 64, false), ("Corte3", 65, 106, false) } },
            { "SwordAndShieldSlash", new[] { ("CorteGiratorio", 0, 73, false) } },
            { "GreatSwordJumpAttack", new[] { ("EspadaSalto", 0, 65, false) } },
            { "GreatSwordSlideAttack", new[] { ("AtaqueDeslizante", 0, 64, false) } },
            { "SwordAndShieldAttack", new[] { ("AtaqueAereo", 0, 70, false) } },
            { "StandingBlockIdle", new[] { ("Defesa", 0, 83, true) } },
            // ---- poder (punhos, chutes e magias)
            { "FlyingKneePunchCombo", new[] { ("Soco1", 0, 28, false), ("Soco2", 29, 56, false), ("Soco3", 57, 80, false) } },
            { "GreatSwordKick", new[] { ("Chute", 0, 45, false) } },
            { "Martelo2_Antiga", new[] { ("ChuteGiratorio", 0, 39, false) } },
            { "Standing2HMagicAttack03", new[] { ("CastFogo", 0, 54, false) } },
            { "Standing2HMagicAttack04", new[] { ("CastVento", 0, 51, false) } },
            { "Standing2HCastSpell01", new[] { ("CastEnergia", 0, 60, false) } },
            { "SwordAndShieldCasting", new[] { ("CastTerra", 0, 72, false) } },
            { "SwordAndShieldPowerUp", new[] { ("PowerUp", 0, 72, false) } },
            { "MagicHeal", new[] { ("Curar", 0, 80, false) } },
            { "PickUpItem_Antiga", new[] { ("PegarItem", 0, 36, false) } },
            // ---- morte
            { "StandingReactDeathBackward", new[] { ("MorteTras", 0, -1, false) } },
            { "StandingReactDeathForward", new[] { ("MorteFrente", 0, -1, false) } },
            { "FallingBackDeath", new[] { ("MorteQueda", 0, -1, false) } },
            { "FallingForwardDeath", new[] { ("MorteQuedaFrente", 0, -1, false) } },
            { "SwordAndShieldDeath", new[] { ("MorteJoelhos", 0, -1, false) } },
            { "TwoHandedSwordDeath", new[] { ("MorteEspada", 0, -1, false) } },
        };

        // estados soltos do Animator (nome do estado = nome do clipe). velocidade 1 = exatamente como veio do Mixamo
        static readonly string[] EstadosSoltos =
        {
            "ParadoParaCorrida", "IniciarAndar", "MudarDirecao", "Transicao", "Dash", "Deslizar", "Rolamento", "Pulo", "PuloAr", "PousoPesado", "PousoRolamento",
            "CorridaParede", "Virar180Espada", "Virar180Poder", "Virar90", "SacarEspada", "EnvainharEspada", "Corte1", "Corte2", "Corte3",
            "CorteGiratorio", "EspadaSalto", "AtaqueDeslizante", "AtaqueAereo", "Defesa", "Soco1", "Soco2", "Soco3", "Chute", "ChuteGiratorio",
            "CastFogo", "CastVento", "CastEnergia", "CastTerra", "PowerUp", "Curar", "PegarItem",
            "MorteTras", "MorteFrente", "MorteQueda", "MorteQuedaFrente", "MorteJoelhos", "MorteEspada", "Dano", "IdleDescanso", "AndarFerido",
        };

        static readonly Dictionary<string, float> VelocidadeEstado = new Dictionary<string, float> { { "Rolamento", 1.6f } };

        [MenuItem("Faith Clash/Preparar Jogador")]
        public static void Preparar()
        {
            if (!File.Exists(Fbx)) { Debug.LogError("[Faith Clash] FBX não encontrado: " + Fbx); return; }
            ConfigurarModelo();
            ConfigurarAnimacoes();
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

        static void ConfigurarModelo()
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
        }

        // ------------------------------------------------------------------ animações
        static void ConfigurarAnimacoes()
        {
            var arquivos = Directory.GetFiles(PastaAnim, "*.fbx").Concat(Directory.GetFiles(PastaAnimBase, "*.fbx")).ToArray();
            foreach (var arquivo in arquivos)
            {
                string caminho = arquivo.Replace('\\', '/');
                string chave = Path.GetFileNameWithoutExtension(caminho);
                bool baseAntiga = chave == "AnimacoesBase";
                bool usado = baseAntiga || (Recortes.TryGetValue(chave, out var c0) && c0.Length > 0);
                var imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
                if (imp == null) continue;
                if (!usado)
                {
                    // arquivo não usado: não importa animação (fica só catalogado)
                    imp.importAnimation = false;
                    imp.SaveAndReimport();
                    continue;
                }
                imp.animationType = ModelImporterAnimationType.Human;
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
                        if (nome != "Dano") continue;
                        c.name = nome;
                        c.loopTime = c.loopPose = false;
                        c.lockRootRotation = false; c.lockRootPositionXZ = false; c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                        clips.Add(c);
                    }
                }
                else if (padrao.Length > 0)
                {
                    var b = padrao[0];
                    int total = Mathf.RoundToInt(b.lastFrame - b.firstFrame);
                    foreach (var (nome, ini, fim, loop) in Recortes[chave])
                    {
                        int f = fim < 0 ? total : Mathf.Min(fim, total);
                        clips.Add(new ModelImporterClipAnimation
                        {
                            name = nome,
                            takeName = b.takeName,
                            firstFrame = b.firstFrame + ini,
                            lastFrame = b.firstFrame + f,
                            loopTime = loop,
                            loopPose = loop,
                            // animação como veio do Mixamo: a altura do quadril é a original;
                            // o deslocamento horizontal NÃO é "assado": fica separado e é aplicado (ou não) pelo script
                            lockRootHeightY = true,
                            keepOriginalPositionY = true,
                            heightOffset = 0f,
                            lockRootPositionXZ = false,
                            keepOriginalPositionXZ = false,
                            lockRootRotation = false,
                            keepOriginalOrientation = false,
                            rotationOffset = 0f,
                        });
                    }
                }
                imp.clipAnimations = clips.ToArray();
                imp.SaveAndReimport();
            }
        }

        static Dictionary<string, AnimationClip> TodosClipes()
        {
            var d = new Dictionary<string, AnimationClip>();
            var arquivos = Directory.GetFiles(PastaAnim, "*.fbx").Concat(Directory.GetFiles(PastaAnimBase, "*.fbx"));
            foreach (var arquivo in arquivos)
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
            ctrl.AddParameter("EscalaPassada", AnimatorControllerParameterType.Float);
            var clips = TodosClipes();
            Debug.Log("[Faith Clash] Clipes (" + clips.Count + "): " + string.Join(", ", clips.Keys.OrderBy(k => k)));
            var sm = ctrl.layers[0].stateMachine;
            ctrl.layers[0].iKPass = true;
            var camadas = ctrl.layers;
            camadas[0].iKPass = true;
            ctrl.layers = camadas;

            void Arvore(string nome, (string clipe, float limiar)[] itens)
            {
                var st = ctrl.CreateBlendTreeInController(nome, out BlendTree arvore);
                arvore.blendParameter = "Velocidade";
                arvore.useAutomaticThresholds = false;
                foreach (var (clipe, limiar) in itens)
                {
                    if (clips.TryGetValue(clipe, out var c)) arvore.AddChild(c, limiar);
                    else Debug.LogWarning("[Faith Clash] Clipe ausente: " + clipe);
                }
                var estado = sm.states.First(s => s.state.name == nome).state;
                estado.speedParameterActive = true;
                estado.speedParameter = "EscalaPassada";
            }
            Arvore("Locomocao", new[] { ("IdlePoder", 0f), ("Correr", 5.2f), ("Sprint", 8.5f) });
            Arvore("LocomocaoEspada", new[] { ("IdleEspada", 0f), ("CorrerEspada", 5.2f), ("Sprint", 8.5f) });
            Arvore("LocomocaoTras", new[] { ("IdlePoder", 0f), ("AndarTras", 2.4f), ("CorrerTras", 5.2f) });
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            foreach (var nome in EstadosSoltos)
            {
                if (!clips.TryGetValue(nome, out var clip)) { Debug.LogWarning("[Faith Clash] Clipe ausente: " + nome); continue; }
                var st = sm.AddState(nome);
                st.motion = clip;
                st.speed = VelocidadeEstado.TryGetValue(nome, out var v) ? v : 1f;
                st.iKOnFeet = false;
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
            cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f; cc.slopeLimit = 55f; cc.skinWidth = 0.04f;
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
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) animator.avatar = avatar;
            modelo.AddComponent<MovimentoDaAnimacao>();
            modelo.AddComponent<PesNoChao>();

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

            Vector3 eixoLocal = Vector3.up;
            Vector3 posLocal = Vector3.zero;
            if (clipes.TryGetValue("Corte1", out var corte))
            {
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
            k.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, eixoLocal);
            k.transform.localPosition = posLocal;
            k.transform.localScale = Vector3.one / Mathf.Max(0.0001f, mao.lossyScale.x);   // os ossos do Mixamo vêm com escala 0,01
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
