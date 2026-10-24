using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Importa o personagem (Quaternius, CC0) como Humanoid, corta as animações gratuitas (Quaternius UAL 1 e 2, Motifect, Kevin Iglesias)
    /// em clipes por função, monta o Animator e gera o prefab do jogador (com katana, IK dos pés e movimento da animação).
    /// Tudo vem da tabela <see cref="Clipes"/>: para trocar uma animação, mude uma linha e rode o menu de novo.
    /// </summary>
    public static class PreparadorJogador
    {
        const string Pasta = "Assets/_FaithClash/Art/Characters/Jogador";
        const string PastaModelo = Pasta + "/Modelo";
        const string PastaAnim = Pasta + "/Anim";
        const string Fbx = PastaModelo + "/Jogador.fbx";
        const string FbxKatana = "Assets/_FaithClash/Art/Weapons/Katana.fbx";
        const string PastaMat = "Assets/_FaithClash/Materials/Jogador";
        const string CaminhoController = "Assets/_FaithClash/Animations/Jogador.controller";
        const string CaminhoInfo = "Assets/_FaithClash/Resources/InfoGolpes.txt";
        public const string CaminhoPrefab = "Assets/_FaithClash/Prefabs/Characters/Jogador.prefab";

        // estado/clipe, arquivo (dentro de Anim), take (null = o único; para UAL é o nome da ação), quadro inicial, final (-1 = tudo), loop, quadro do impacto (-1 = sem)
        // Os quadros são a 30 fps, contados do começo do arquivo.
        sealed class Clipe
        {
            public readonly string Nome, Arquivo, Take;
            public readonly int Ini, Fim, Impacto;
            public readonly bool Loop, MovimentoXZ;
            public Clipe(string nome, string arquivo, string take, int ini, int fim, bool loop, int impacto = -1, bool movimentoXZ = false)
            { Nome = nome; Arquivo = arquivo; Take = take; Ini = ini; Fim = fim; Loop = loop; Impacto = impacto; MovimentoXZ = movimentoXZ; }
        }

        static readonly Clipe[] Clipes =
        {
            // ---------------- parado / locomoção
            new Clipe("IdlePoder", "Magia/KI_Idle01.fbx", null, 0, -1, true),
            new Clipe("IdleEspada", "UAL1.fbx", "Sword_Idle", 0, -1, true),
            new Clipe("IdleCombate", "Motifect/muay_thai_guard_stance.fbx", null, 20, 110, true),
            new Clipe("Andar", "UAL1.fbx", "Walk_Loop", 0, -1, true),
            new Clipe("Correr", "UAL1.fbx", "Jog_Fwd_Loop", 0, -1, true),
            new Clipe("Sprint", "UAL1.fbx", "Sprint_Loop", 0, -1, true),
            new Clipe("CorrerTras", "Magia/KI_Run01_Backward.fbx", null, 0, -1, true),
            new Clipe("AndarAgachado", "UAL1.fbx", "Crouch_Fwd_Loop", 0, -1, true),
            // ---------------- movimento especial
            new Clipe("Dash", "UAL2.fbx", "Sword_Dash", 0, 26, false, 8, true),
            new Clipe("Rolamento", "UAL1.fbx", "Roll", 0, -1, false),
            new Clipe("PuloImpulso", "UAL1.fbx", "Jump_Start", 0, 40, false),
            new Clipe("NoAr", "UAL1.fbx", "Jump_Loop", 0, -1, true),
            new Clipe("PousoLeve", "UAL1.fbx", "Jump_Land", 0, -1, false),
            new Clipe("PousoPesado", "UAL2.fbx", "NinjaJump_Land", 0, -1, false),
            new Clipe("PuloNinja", "UAL2.fbx", "NinjaJump_Start", 0, -1, false),
            // ---------------- espada (UAL 2 + Motifect)
            new Clipe("Corte1", "UAL2.fbx", "Sword_Regular_A", 0, -1, false, 7, true),
            new Clipe("Corte1Rec", "UAL2.fbx", "Sword_Regular_A_Rec", 0, -1, false),
            new Clipe("Corte2", "UAL2.fbx", "Sword_Regular_B", 0, -1, false, 8, true),
            new Clipe("Corte2Rec", "UAL2.fbx", "Sword_Regular_B_Rec", 0, -1, false),
            new Clipe("Corte3", "UAL2.fbx", "Sword_Regular_C", 0, -1, false, 20, true),
            new Clipe("CorteGiratorio", "Motifect/sword_spinning_slash.fbx", null, 32, 98, false, 50, true),
            new Clipe("EspadaSalto", "UAL2.fbx", "Sword_Heavy_Combo", 0, 64, false, 15, true),
            new Clipe("Estocada", "UAL1.fbx", "Sword_Attack", 0, -1, false, 12, true),
            new Clipe("CorteCima", "Motifect/sword_overhead_strike.fbx", null, 14, 72, false, 31, true),
            new Clipe("CorteDiagonal", "Motifect/sword_slash_diagonal_down.fbx", null, 14, 66, false, 34, true),
            new Clipe("Defesa", "Motifect/sword_block_high.fbx", null, 0, 60, false),
            new Clipe("ContraAtaque", "Motifect/sword_parry_and_riposte.fbx", null, 0, 75, false, 20, true),
            new Clipe("SacarEspada", "Motifect/sword_draw_stance.fbx", null, 0, 80, false),
            new Clipe("EnvainharEspada", "Motifect/sword_sheathe.fbx", null, 0, 80, false),
            // ---------------- combate: taijutsu (UAL 1/2 + Motifect)
            new Clipe("Jab", "UAL1.fbx", "Punch_Jab", 0, -1, false, 5),
            new Clipe("Direto", "UAL1.fbx", "Punch_Cross", 0, -1, false, 6),
            new Clipe("Gancho", "UAL2.fbx", "Melee_Hook", 0, -1, false, 7),
            new Clipe("GanchoRec", "UAL2.fbx", "Melee_Hook_Rec", 0, -1, false),
            new Clipe("Cotovelada", "Motifect/muay_thai_elbow_cut.fbx", null, 0, 42, false, 16, true),
            new Clipe("Joelhada", "Motifect/muay_thai_knee_clinch.fbx", null, 14, 58, false, 32, true),
            new Clipe("ChuteFrontal", "Motifect/muay_thai_teep.fbx", null, 6, 48, false, 23, true),
            new Clipe("ChuteMuay", "Motifect/muay_thai_body_kick.fbx", null, 10, 52, false, 27, true),
            new Clipe("ChuteLateral", "Motifect/tkd_roundhouse_high.fbx", null, 14, 56, false, 28, true),
            new Clipe("ChuteLado", "Motifect/tkd_side_kick.fbx", null, 10, 52, false, 28, true),
            new Clipe("ChuteCostas", "Motifect/tkd_back_kick.fbx", null, 8, 58, false, 31, true),
            new Clipe("ChuteMachado", "Motifect/tkd_axe_kick.fbx", null, 12, 60, false, 30, true),
            new Clipe("GiroCostas", "Motifect/tkd_spinning_heel_kick.fbx", null, 32, 88, false, 54, true),
            new Clipe("ChuteBaixo", "Motifect/judo_ankle_sweep.fbx", null, 40, 98, false, 66, true),
            new Clipe("Voadora", "Motifect/tkd_jump_front_kick.fbx", null, 22, 104, false, 66, true),
            new Clipe("VoadoraGiratoria", "Motifect/tkd_jump_spinning_kick.fbx", null, 36, 120, false, 66, true),
            new Clipe("CombinacaoMuay", "Motifect/muay_thai_combination.fbx", null, 60, 175, false, 94, true),
            // ---------------- poder: gestos de jutsu (Kevin Iglesias, grátis)
            new Clipe("CastFogo", "Magia/KI_MagicAttackDirect2H01.fbx", null, 0, -1, false, 22),
            new Clipe("CastVento", "Magia/KI_MagicAttackOmni01.fbx", null, 0, -1, false, 22),
            new Clipe("CastEnergia", "Magia/KI_MagicAttackDirect1H01_R.fbx", null, 0, -1, false, 20),
            new Clipe("CastTerra", "Magia/KI_MagicAttackCall1H01_L.fbx", null, 0, -1, false, 24),
            new Clipe("PowerUp", "Magia/KI_CastingDamage01.fbx", null, 0, -1, false, 16),
            new Clipe("CastEntrar", "Magia/KI_CastingEnter01.fbx", null, 0, -1, false),
            new Clipe("CastIdle", "Magia/KI_CastingIdle01.fbx", null, 0, -1, true),
            new Clipe("CastSair", "Magia/KI_CastingExit01.fbx", null, 0, -1, false),
            new Clipe("CastFogoCarga", "Magia/KI_MagicAttackDirect2H01_Load.fbx", null, 0, -1, false),
            new Clipe("CastFogoLiberar", "Magia/KI_MagicAttackDirect2H01_Cast.fbx", null, 0, -1, false),
            // ---------------- itens, meditação, reações, morte
            new Clipe("Curar", "UAL2.fbx", "Consume", 0, -1, false),
            new Clipe("PegarItem", "UAL1.fbx", "PickUp_Table", 0, -1, false),
            new Clipe("Interagir", "UAL1.fbx", "Interact", 0, -1, false),
            new Clipe("MeditarEntrar", "UAL1.fbx", "Sitting_Enter", 0, -1, false),
            new Clipe("MeditarLoop", "UAL1.fbx", "Sitting_Idle_Loop", 0, -1, true),
            new Clipe("MeditarSair", "UAL1.fbx", "Sitting_Exit", 0, -1, false),
            new Clipe("Dano", "UAL1.fbx", "Hit_Chest", 0, -1, false),
            new Clipe("DanoCabeca", "UAL1.fbx", "Hit_Head", 0, -1, false),
            new Clipe("Empurrado", "UAL2.fbx", "Hit_Knockback", 0, -1, false),
            new Clipe("Morte", "UAL1.fbx", "Death01", 0, -1, false),
        };

        // estados que, ao terminar, seguem sozinhos para outro (golpe + recuperação, meditação)
        static readonly (string de, string para)[] Encadeados =
        {
            ("Corte1", "Corte1Rec"), ("Corte2", "Corte2Rec"), ("Gancho", "GanchoRec"), ("MeditarEntrar", "MeditarLoop"), ("CastEntrar", "CastIdle"),
        };

        static readonly Dictionary<string, float> VelocidadeEstado = new Dictionary<string, float>();

        [MenuItem("Faith Clash/Preparar Jogador")]
        public static void Preparar()
        {
            AssetDatabase.Refresh();
            if (!File.Exists(Fbx)) { Debug.LogError("[Faith Clash] Modelo não encontrado: " + Fbx); return; }
            ConfigurarModelo();
            ConfigurarAnimacoes();
            var controller = CriarAnimator();
            GerarInfo();
            CriarPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[Faith Clash] Jogador pronto: " + CaminhoPrefab);
        }

        // ------------------------------------------------------------------ modelo e materiais
        static Material Mat(string nome, Color cor, float suav, string baseMap = null, string normal = null, bool recorte = false)
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
            if (recorte)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);   // cabelo e sobrancelha visíveis dos dois lados
                m.renderQueue = 2450;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void ConfigurarModelo()
        {
            string T = PastaModelo + "/Texturas/";
            foreach (var png in Directory.GetFiles(PastaModelo + "/Texturas", "*Normal.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(png.Replace('\\', '/'));
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
            var materiais = new Dictionary<string, Material>
            {
                { "MI_Superhero_Male", Mat("Corpo", Color.white, 0.3f, T + "T_Superhero_Male_Dark.png", T + "T_Superhero_Male_Normal.png") },
                { "MI_Ranger", Mat("Roupa", Color.white, 0.25f, T + "T_Ranger_BaseColor.png", T + "T_Ranger_Normal.png") },
                { "MI_Regular_Male", Mat("Pele", Color.white, 0.3f, T + "T_Regular_Male_Dark_BaseColor.png", T + "T_Regular_Male_Normal.png") },
                { "MI_Eyes", Mat("Olhos", Color.white, 0.8f, T + "T_Eye_Brown.png", T + "T_Eye_Normal.png") },
                { "MI_Hair_1", Mat("Cabelo", new Color(0.12f, 0.1f, 0.1f), 0.35f, T + "T_Hair_1_BaseColor.png", T + "T_Hair_1_Normal.png", true) },
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
        static string Curto(string takeName) => takeName.Contains("|") ? takeName.Substring(takeName.LastIndexOf('|') + 1) : takeName;

        static void ConfigurarAnimacoes()
        {
            foreach (var grupo in Clipes.GroupBy(c => c.Arquivo))
            {
                string caminho = PastaAnim + "/" + grupo.Key;
                var imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
                if (imp == null) { Debug.LogWarning("[Faith Clash] Arquivo de animação ausente: " + caminho); continue; }
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();

                imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
                var padrao = imp.defaultClipAnimations;
                var clips = new List<ModelImporterClipAnimation>();
                foreach (var c in grupo)
                {
                    ModelImporterClipAnimation b = padrao.FirstOrDefault(p => c.Take != null ? Curto(p.name) == c.Take : true);
                    if (b == null) { Debug.LogWarning($"[Faith Clash] Take '{c.Take}' não achado em {c.Arquivo} (existem: {string.Join(", ", padrao.Select(p => Curto(p.name)))})"); continue; }
                    int total = Mathf.RoundToInt(b.lastFrame - b.firstFrame);
                    int fim = c.Fim < 0 ? total : Mathf.Min(c.Fim, total);
                    clips.Add(new ModelImporterClipAnimation
                    {
                        name = c.Nome,
                        takeName = b.takeName,
                        firstFrame = b.firstFrame + c.Ini,
                        lastFrame = b.firstFrame + fim,
                        loopTime = c.Loop,
                        loopPose = c.Loop,
                        // a altura do quadril é a original; o deslocamento horizontal só vale nos golpes que "andam" (MovimentoXZ)
                        lockRootHeightY = true,
                        keepOriginalPositionY = true,
                        heightOffset = 0f,
                        lockRootPositionXZ = !c.MovimentoXZ,
                        keepOriginalPositionXZ = false,
                        lockRootRotation = true,
                        keepOriginalOrientation = false,
                        rotationOffset = 0f,
                    });
                }
                imp.clipAnimations = clips.ToArray();
                imp.SaveAndReimport();
                var av = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Avatar>().FirstOrDefault();
                if (av == null || !av.isHuman) Debug.LogWarning("[Faith Clash] Avatar humanoide inválido em " + grupo.Key);
            }
        }

        static Dictionary<string, AnimationClip> TodosClipes()
        {
            var d = new Dictionary<string, AnimationClip>();
            foreach (var arquivo in Clipes.Select(c => c.Arquivo).Distinct())
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(PastaAnim + "/" + arquivo).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview__")) d[c.name] = c;
            return d;
        }

        static void GerarInfo()
        {
            var clips = TodosClipes();
            var sb = new StringBuilder();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var c in Clipes)
            {
                if (!clips.TryGetValue(c.Nome, out var clip)) continue;
                float impacto = c.Impacto >= 0 ? (c.Impacto - c.Ini) / 30f : -1f;
                sb.AppendLine($"{c.Nome};{clip.length.ToString("0.000", ci)};{impacto.ToString("0.000", ci)}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoInfo));
            File.WriteAllText(CaminhoInfo, sb.ToString());
            AssetDatabase.ImportAsset(CaminhoInfo);
        }

        static AnimatorController CriarAnimator()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoController));
            if (File.Exists(CaminhoController)) AssetDatabase.DeleteAsset(CaminhoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("EscalaPassada", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Espelho", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VelAcao", AnimatorControllerParameterType.Float);
            var ps = ctrl.parameters;
            for (int p = 0; p < ps.Length; p++) if (ps[p].name == "VelAcao" || ps[p].name == "EscalaPassada") ps[p].defaultFloat = 1f;
            ctrl.parameters = ps;
            var clips = TodosClipes();
            Debug.Log("[Faith Clash] Clipes (" + clips.Count + "): " + string.Join(", ", clips.Keys.OrderBy(k => k)));
            var sm = ctrl.layers[0].stateMachine;
            var camadas = ctrl.layers;
            camadas[0].iKPass = true;
            ctrl.layers = camadas;

            void Arvore(string nome, (string clipe, float limiar)[] itens)
            {
                ctrl.CreateBlendTreeInController(nome, out BlendTree arvore);
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
            // limiares = velocidade natural de cada ciclo (medida nos arquivos com movimento): andar 0,97, correr 5,36, sprint 8,25 m/s
            Arvore("Locomocao", new[] { ("IdlePoder", 0f), ("Andar", 0.97f), ("Correr", 5.36f), ("Sprint", 8.25f) });
            Arvore("LocomocaoEspada", new[] { ("IdleEspada", 0f), ("Andar", 0.97f), ("Correr", 5.36f), ("Sprint", 8.25f) });
            Arvore("LocomocaoCombate", new[] { ("IdleCombate", 0f), ("Andar", 0.97f), ("Correr", 5.36f), ("Sprint", 8.25f) });
            Arvore("LocomocaoTras", new[] { ("IdlePoder", 0f), ("CorrerTras", 5.0f) });
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            var estados = new Dictionary<string, AnimatorState>();
            foreach (var c in Clipes)
            {
                if (c.Nome.StartsWith("Idle") || c.Nome == "Andar" || c.Nome == "Correr" || c.Nome == "Sprint" || c.Nome == "CorrerTras") continue;   // já nas árvores
                if (!clips.TryGetValue(c.Nome, out var clip)) { Debug.LogWarning("[Faith Clash] Clipe ausente: " + c.Nome); continue; }
                var st = sm.AddState(c.Nome);
                st.motion = clip;
                st.speed = VelocidadeEstado.TryGetValue(c.Nome, out var v) ? v : 1f;
                st.speedParameterActive = true;      // velocidade de cada golpe é ajustada pelo script
                st.speedParameter = "VelAcao";
                st.mirrorParameterActive = true;     // golpes espelhados
                st.mirrorParameter = "Espelho";
                st.iKOnFeet = false;
                estados[c.Nome] = st;
            }
            foreach (var (de, para) in Encadeados)
            {
                if (!estados.TryGetValue(de, out var a) || !estados.TryGetValue(para, out var b)) continue;
                var t = a.AddTransition(b);
                t.hasExitTime = true; t.exitTime = 1f; t.hasFixedDuration = true; t.duration = 0.06f;
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
                corte.SampleAnimation(temp, corte.length * 0.5f);
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
            k.transform.localScale = Vector3.one / Mathf.Max(0.0001f, mao.lossyScale.x);
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
