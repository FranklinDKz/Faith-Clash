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
        const string PastaYuta = "Assets/_FaithClash/Art/Characters/Yuta";
        const string Fbx = "Assets/Personagens/JogadorPrincipal/Mesh/JogadorPrincipal.fbx";
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
            public readonly int[] Impactos;   // golpes de vários acertos (combos): quadros de cada impacto
            public readonly bool Loop, MovimentoXZ;
            public Clipe(string nome, string arquivo, string take, int ini, int fim, bool loop, int impacto = -1, bool movimentoXZ = false, int[] impactos = null)
            { Nome = nome; Arquivo = arquivo; Take = take; Ini = ini; Fim = fim; Loop = loop; Impacto = impacto; MovimentoXZ = movimentoXZ; Impactos = impactos ?? (impacto >= 0 ? new[] { impacto } : new int[0]); }
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
            new Clipe("Rolamento", "UAL1.fbx", "Roll", 0, -1, false),
            new Clipe("PuloImpulso", "UAL1.fbx", "Jump_Start", 0, 40, false),
            new Clipe("NoAr", "UAL1.fbx", "Jump_Loop", 0, -1, true),
            new Clipe("PousoLeve", "UAL1.fbx", "Jump_Land", 0, -1, false),
            new Clipe("PousoPesado", "UAL2.fbx", "NinjaJump_Land", 0, -1, false),
            new Clipe("PuloNinja", "UAL2.fbx", "NinjaJump_Start", 0, -1, false),
            new Clipe("AndarAgachado", "UAL1.fbx", "Crouch_Fwd_Loop", 0, -1, true),
            // ---------------- movimento especial
            new Clipe("Dash", "UAL2.fbx", "Sword_Dash", 0, 26, false, 8, true),
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

        const string CsvMixamo = PastaAnim + "/cortes_mixamo.csv";
        static Clipe[] todos;

        /// <summary>Tabela fixa + cortes das animações do Mixamo (CSV gerado por Ferramentas/Blender/gerar_cortes_mixamo.py).</summary>
        static Clipe[] Todos
        {
            get
            {
                if (todos != null) return todos;
                var lista = new List<Clipe>(Clipes);
                if (File.Exists(CsvMixamo))
                    foreach (var linha in File.ReadAllLines(CsvMixamo))
                    {
                        var p = linha.Split(';');
                        if (p.Length < 7) continue;
                        var hits = p[5].Length > 0 ? p[5].Split(',').Select(int.Parse).ToArray() : new int[0];
                        lista.Add(new Clipe(p[0], p[1], null, int.Parse(p[2]), int.Parse(p[3]), p[4] == "1", hits.Length > 0 ? hits[0] : -1, p[6] == "1", hits));
                    }
                return todos = lista.ToArray();
            }
        }

        /// <summary>Clipes de parkour cuja subida/descida move o personagem (root motion vertical, pelos pés).</summary>
        public static readonly HashSet<string> ParkourComAltura = new HashSet<string>
        {
            "Mx_Vault_Over_Box", "Mx_Sprint_To_Wall_Climb", "Mx_PK_Pull_Up_Ledge", "Mx_PK_Climb_Onto_Box", "Mx_PK_Step_Jump_Over",
            "U_Descer", "U_DescerVariante", "U_DescerObstaculo", "U_Vault1", "U_Vault2", "U_Vault3", "U_SubirCorrendo", "U_SubirAndando", "U_SubirAlto",
        };

        static readonly Dictionary<string, float> VelocidadeEstado = new Dictionary<string, float>();

        [MenuItem("Faith Clash/Preparar Jogador")]
        public static void Preparar()
        {
            todos = null;
            orientacoes = null;
            AssetDatabase.Refresh();
            if (!File.Exists(Fbx)) { Debug.LogError("[Faith Clash] Modelo não encontrado: " + Fbx); return; }
            ConfigurarModelo();
            if (Directory.Exists(AnimacoesMartial.PastaMartial)) AnimacoesMartial.ConfigurarClipes();
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
            const string T = "Assets/Personagens/JogadorPrincipal/Texturas/";
            // cor, metálico/rugosidade (R = metálico, A = suavidade) do Meshy; o material "Material_1" do FBX usa essas texturas
            var mat = Mat("JogadorPrincipal", Color.white, 0.5f, T + "Base.png");
            var mapaMetal = AssetDatabase.LoadAssetAtPath<Texture2D>(T + "MetalicoSuave.png");
            var tiMetal = AssetImporter.GetAtPath(T + "MetalicoSuave.png") as TextureImporter;
            if (tiMetal != null && tiMetal.sRGBTexture) { tiMetal.sRGBTexture = false; tiMetal.SaveAndReimport(); }
            if (mapaMetal != null)
            {
                mat.SetTexture("_MetallicGlossMap", mapaMetal); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_Metallic", 1f);
            }
            EditorUtility.SetDirty(mat);

            var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material_1"), mat);
            imp.SaveAndReimport();

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var bb = r.sharedMesh.bounds;
                foreach (var c in new[] { bb.min, bb.max })
                {
                    float y = r.transform.TransformPoint(c).y; minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            }
            Debug.Log("[Faith Clash] Altura do modelo importado: " + (maxY - minY) + " m (escala " + imp.globalScale + ")");

            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            Debug.Log("[Faith Clash] Avatar do jogador válido: " + (avatar != null && avatar.isValid && avatar.isHuman));
        }

        // ------------------------------------------------------------------ animações
        static string Curto(string takeName) => takeName.Contains("|") ? takeName.Substring(takeName.LastIndexOf('|') + 1) : takeName;

        // ------------------------------------------------------------------ orientação dos clipes
        const string CsvOrientacoes = PastaAnim + "/orientacoes.csv";
        static Dictionary<string, float> orientacoes;

        /// <summary>Giro (graus) aplicado à pose de cada clipe para que ele comece olhando para a frente do personagem.</summary>
        static float Orientacao(string nome)
        {
            if (orientacoes == null)
            {
                orientacoes = new Dictionary<string, float>();
                if (File.Exists(CsvOrientacoes))
                    foreach (var l in File.ReadAllLines(CsvOrientacoes))
                    {
                        var p = l.Split(';');
                        if (p.Length == 2 && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) orientacoes[p[0]] = v;
                    }
            }
            return orientacoes.TryGetValue(nome, out var g) ? g : 0f;
        }

        /// <summary>Mede a orientação (yaw) de clipes que ficam dentro de blend trees (idle/andar/correr), amostrando o clipe no modelo.</summary>
        [MenuItem("Faith Clash/Ferramentas/Medir Clipes de Locomoção")]
        public static void MedirLocomocao()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            var anim = go.GetComponentInChildren<Animator>();
            var dir = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var esq = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var sb = new StringBuilder();
            foreach (var kv in TodosClipes().Where(k => k.Key == "U_RolarCorrer" || k.Key == "U_PousoAlto" || k.Key == "U_Vault1" || k.Key == "U_DescerObstaculo"))
            {
                string r = "";
                var amostras = new List<float>();
                for (int a = 0; a <= 20; a++) amostras.Add(Mathf.Min(0.99f, a / 20f));
                foreach (float f in amostras)
                {
                    kv.Value.SampleAnimation(anim.gameObject, kv.Value.length * f);
                    Vector3 d = dir.position - esq.position; d.y = 0f;
                    r += Vector3.SignedAngle(go.transform.forward, Vector3.Cross(d.normalized, Vector3.up), Vector3.up).ToString("0") + " ";
                }
                sb.AppendLine(kv.Key + " (" + kv.Value.length.ToString("0.00") + "s): " + r);
            }
            Object.DestroyImmediate(go);
            File.WriteAllText("Ferramentas/analise/orient_loco.txt", sb.ToString());
            Debug.Log("[Faith Clash] Medição de locomoção: Ferramentas/analise/orient_loco.txt");
        }

        static Dictionary<string, AnimationClip> TodosClipes()
        {
            var d = new Dictionary<string, AnimationClip>();
            foreach (var arquivo in Todos.Select(c => c.Arquivo).Distinct())
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(PastaAnim + "/" + arquivo).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview__")) d[c.name] = c;
            return d;
        }

        static void GerarInfo()
        {
            var clips = TodosClipes();
            var sb = new StringBuilder();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var c in Todos)
            {
                if (!clips.TryGetValue(c.Nome, out var clip)) continue;
                string impactos = c.Impactos.Length == 0 ? "-1" : string.Join(",", c.Impactos.Select(h => ((h - c.Ini) / 30f).ToString("0.000", ci)));
                sb.AppendLine($"{c.Nome};{clip.length.ToString("0.000", ci)};{impactos}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoInfo));
            File.WriteAllText(CaminhoInfo, sb.ToString());
            AssetDatabase.ImportAsset(CaminhoInfo);
        }

        static readonly string[] RemovidasDoJogador =
        {
            "PuloImpulso", "NoAr", "PousoLeve", "PousoPesado", "PuloNinja", "Mx_Running_Jump", "Mx_PK_Running_Jump_Landing", "Mx_PK_Sprint_Jump", "Mx_PK_Jump_From_Idle", "Mx_Big_Jump",
            "Correr", "Sprint", "CorrerTras", "U_Correr", "Mx_Fast_Run", "Mx_Sprint", "Mx_Two_Cycle_Sprint",
            "Mx_Standing_Run_Forward", "Mx_Standing_Run_Back", "Mx_Standing_Run_Left", "Mx_Standing_Run_Right",
            "Mx_Sprint_Forward", "Mx_Sprint_Backward", "Mx_Sprint_Left", "Mx_Sprint_Right", "Mx_SS_Run", "Mx_Great_Sword_Run",
            "U_Vault1", "U_Vault2", "U_Vault3", "Mx_PK_Step_Jump_Over", "Mx_Jump_Over", "Mx_Jump_Away", "Mx_Vault_Over_Box",
        };

        /// <summary>Só refaz o Animator (sem reimportar o modelo): usado pela janela Animações do Jogador.</summary>
        public static void RefazerAnimator()
        {
            CriarAnimator();
            GerarInfo();
            AssetDatabase.SaveAssets();
        }

        static AnimatorController CriarAnimator()
        {
            if (Directory.Exists(AnimacoesMartial.PastaMartial)) return AnimacoesMartial.CriarJogador(CaminhoController);
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoController));
            if (File.Exists(CaminhoController)) AssetDatabase.DeleteAsset(CaminhoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("EscalaPassada", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Espelho", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VelAcao", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            var ps = ctrl.parameters;
            for (int p = 0; p < ps.Length; p++) if (ps[p].name == "VelAcao" || ps[p].name == "EscalaPassada") ps[p].defaultFloat = 1f;
            ctrl.parameters = ps;
            var clips = TodosClipes();
            // animações de pular e correr do jogador removidas (o usuário vai colocar as dele); só a arrancada (U_ArrancadaCorrer) fica. Os inimigos continuam com as deles.
            foreach (var nome in RemovidasDoJogador) clips.Remove(nome);
            Debug.Log("[Faith Clash] Clipes (" + clips.Count + "): " + string.Join(", ", clips.Keys.OrderBy(k => k)));
            var sm = ctrl.layers[0].stateMachine;
            var camadas = ctrl.layers;
            camadas[0].iKPass = true;
            ctrl.layers = camadas;

            void Arvore(string nome, (string clipe, float limiar)[] itens, Dictionary<string, float> escalas = null)
            {
                ctrl.CreateBlendTreeInController(nome, out BlendTree arvore);
                arvore.blendParameter = "Velocidade";
                arvore.useAutomaticThresholds = false;
                foreach (var (clipe, limiar) in itens)
                {
                    if (clips.TryGetValue(clipe, out var c)) arvore.AddChild(c, limiar);
                    else if (RemovidasDoJogador.Contains(clipe) && clips.TryGetValue(itens[0].clipe, out var parado)) arvore.AddChild(parado, limiar);   // sem animação de correr: fica estático
                    else Debug.LogWarning("[Faith Clash] Clipe ausente: " + clipe);
                }
                if (escalas != null)
                {
                    var filhos = arvore.children;
                    for (int i = 0; i < filhos.Length; i++)
                        if (filhos[i].motion != null && escalas.TryGetValue(filhos[i].motion.name, out var ts)) filhos[i].timeScale = ts;
                    arvore.children = filhos;
                }
                var estado = sm.states.First(s => s.state.name == nome).state;
                estado.speedParameterActive = true;
                estado.speedParameter = "EscalaPassada";
            }
            // limiares = velocidade natural de cada ciclo (medida nos arquivos com movimento): andar 0,97, correr 5,36, sprint 8,25 m/s
            // corridas de parkour (Mixamo): Fast Run 5,15 m/s (jogador corre a 5,5) e Sprint 5,62 m/s (jogador dispara a 7,5): o timeScale iguala a passada à velocidade
            var escalasParkour = new Dictionary<string, float> { { "Mx_Fast_Run", 5.5f / 5.15f }, { "Mx_Sprint", 7.5f / 5.62f } };
            // animações do usuário (parado, andando, correndo, ferido) + sprint do Mixamo; o timeScale casa a passada com a velocidade
            var escalasUsuario = new Dictionary<string, float> { { "U_Andar", 1.2f } };
            // corrida = sprint = a mesma animação do usuário (combina com a arrancada); a velocidade do jogo acompanha a da animação
            void ArvoreCorrida(string nome, string idle)
            {
                ctrl.CreateBlendTreeInController(nome, out BlendTree arv);
                arv.blendParameter = "Velocidade";
                arv.useAutomaticThresholds = false;
                // sem animações de correr (removidas): em velocidade de corrida o personagem fica estático (parado) até o usuário colocar as dele
                foreach (var (clipe, limiar, ts) in new[] { (idle, 0f, 1f), ("U_Andar", 1.96f, 1.2f), ("U_Correr", 5.4f, 1.35f), ("U_Correr", 6.6f, 1.65f) })
                    if (clips.TryGetValue(clipe, out var c)) arv.AddChild(c, limiar);
                    else if (clips.TryGetValue(idle, out var parado)) arv.AddChild(parado, limiar);
                var f = arv.children;
                float[] tss = { 1f, 1.2f, 1f, 1f };
                for (int i = 0; i < f.Length && i < tss.Length; i++) f[i].timeScale = tss[i];
                arv.children = f;
                var est = sm.states.First(s => s.state.name == nome).state;
                est.speedParameterActive = false;
            }
            ArvoreCorrida("Locomocao", "U_Parado");
            Arvore("LocomocaoFerido", new[] { ("U_Parado", 0f), ("U_Ferido", 1.63f) });
            Arvore("LocomocaoEspadaDupla", new[] { ("Mx_Great_Sword_Idle", 0f), ("Mx_Great_Sword_Walk", 0.94f), ("Mx_Great_Sword_Run", 3.2f) },
                new Dictionary<string, float> { { "Mx_Great_Sword_Run", 3.2f / 2.4f } });
            Arvore("LocomocaoEspada", new[] { ("Mx_SS_Idle", 0f), ("Mx_SS_Walk", 1.37f), ("Mx_SS_Run", 4.4f) },
                new Dictionary<string, float> { { "Mx_SS_Run", 4.4f / 3.78f } });
            ArvoreCorrida("LocomocaoCombate", "Mx_KF_Guarda_Aberta");
            Arvore("LocomocaoTras", new[] { ("IdlePoder", 0f), ("CorrerTras", 5.0f) });
            // com o alvo travado (estilo Elden Ring): andar/correr/sprint em 8 direções olhando para o inimigo; MoveX/MoveY = velocidade local (m/s)
            {
                ctrl.CreateBlendTreeInController("LocomocaoAlvo", out BlendTree alvo);
                alvo.blendType = BlendTreeType.FreeformDirectional2D;
                alvo.blendParameter = "MoveX"; alvo.blendParameterY = "MoveY";
                alvo.useAutomaticThresholds = false;
                void Pt(string clipe, float x, float y)
                {
                    if (clips.TryGetValue(clipe, out var c)) alvo.AddChild(c, new Vector2(x, y));
                    else if (RemovidasDoJogador.Contains(clipe) && clips.TryGetValue("Mx_Fighting_Idle", out var parado)) alvo.AddChild(parado, new Vector2(x, y));
                    else Debug.LogWarning("[Faith Clash] Clipe ausente: " + clipe);
                }
                Pt("Mx_Fighting_Idle", 0f, 0f);
                // velocidades naturais medidas nos clipes (m/s): andar 1,5 / 1,1 / 0,9 / 1,5; correr 3,3 / 1,8 / 2,9 / 2,9; sprint 6,2
                Pt("U_Andar", 0f, 1.96f); Pt("U_AlvoTras", 0f, -1.03f); Pt("U_AlvoEsquerda", -1.58f, 0f); Pt("U_AlvoDireita", 1.58f, 0f);   // andar com foco: animações do usuário
                Pt("Mx_Standing_Run_Forward", 0f, 3.3f); Pt("Mx_Standing_Run_Back", 0f, -2.2f); Pt("Mx_Standing_Run_Left", -2.9f, 0f); Pt("Mx_Standing_Run_Right", 2.9f, 0f);
                Pt("Mx_Sprint_Forward", 0f, 6.2f); Pt("Mx_Sprint_Backward", 0f, -6.2f); Pt("Mx_Sprint_Left", -6.2f, 0f); Pt("Mx_Sprint_Right", 6.2f, 0f);
            }
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            var estados = new Dictionary<string, AnimatorState>();
            foreach (var c in Todos)
            {
                if (c.Nome.StartsWith("Idle") || c.Nome == "Andar" || c.Nome == "Correr" || c.Nome == "Sprint" || c.Nome == "Mx_Fast_Run" || c.Nome == "Mx_SS_Idle" || c.Nome == "Mx_SS_Walk" || c.Nome == "Mx_SS_Run" || c.Nome == "Mx_Great_Sword_Idle" || c.Nome == "Mx_Great_Sword_Walk" || c.Nome == "Mx_Great_Sword_Run" || c.Nome == "Mx_Sprint" || c.Nome == "CorrerTras") continue;   // já nas árvores
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
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.875f, 0);   // cápsula do tamanho do modelo (1,75 m)
            cc.stepOffset = 0.4f; cc.slopeLimit = 55f; cc.skinWidth = 0.04f;
            var pc = raiz.AddComponent<PlayerController>();
            var vida = raiz.AddComponent<Health>();
            var combate = raiz.AddComponent<MeleeCombat>();
            var anim = raiz.AddComponent<AnimadorProtagonista>();
            raiz.AddComponent<StatusJogador>();
            raiz.AddComponent<EstilosElementais>();
            raiz.AddComponent<TravaAlvo>();
            raiz.AddComponent<Parkour>();
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
            modelo.AddComponent<EmpunhaduraKatana>();
            modelo.AddComponent<EstabilizadorMarcha>();
            modelo.AddComponent<EscaladaProcedural>();
            modelo.AddComponent<CorpoVivo>();

            AnexarKatana(modelo, animator);
            ConfigurarVFX(raiz);

            Ligar(pc, "animador", anim);
            Ligar(combate, "animador", anim);
            Ligar(anim, "animator", animator);
            Ligar(anim, "jogador", pc);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Object.DestroyImmediate(raiz);
        }

        /// <summary>
        /// Katana na mão direita, segurada como uma espada de verdade: o cabo cruza a palma (do dedo mínimo para o indicador) e a lâmina sai pelo lado do indicador.
        /// Cria também o ponto "PegadaEsquerda" (cabo, perto do pomo) onde a mão esquerda se prende na empunhadura de duas mãos.
        /// </summary>
        static void AnexarKatana(GameObject modelo, Animator animator)
        {
            var katanaFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxKatana);
            if (katanaFbx == null || animator == null || !animator.isHuman) return;
            var mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform Dedo(HumanBodyBones b) => animator.GetBoneTransform(b);
            var indicador = Dedo(HumanBodyBones.RightIndexProximal);
            var minimo = Dedo(HumanBodyBones.RightLittleProximal);
            var medio = Dedo(HumanBodyBones.RightMiddleProximal);
            var anelar = Dedo(HumanBodyBones.RightRingProximal);
            if (indicador == null || minimo == null || medio == null) return;

            Vector3 L(Transform t) => mao.InverseTransformPoint(t.position);
            Vector3 eixo = (L(indicador) - L(minimo)).normalized;                       // cabo atravessa a palma
            Vector3 dedos = (L(medio) - Vector3.zero).normalized;                         // direção dos dedos
            Vector3 palma = (L(indicador) + L(minimo) + L(medio) + (anelar != null ? L(anelar) : L(medio))) / 4f;
            palma -= dedos * 0.012f;
            Vector3 lado = Vector3.Cross(eixo, dedos).normalized;                          // normal da palma
            // inclina a lâmina um pouco para a frente (pulso relaxado)
            eixo = (eixo + dedos * 0.35f).normalized;

            var k = (GameObject)PrefabUtility.InstantiatePrefab(katanaFbx);
            k.name = "Katana";
            k.transform.SetParent(mao, false);
            float escalaMao = Mathf.Max(0.0001f, mao.lossyScale.x);
            k.transform.localScale = Vector3.one / escalaMao;
            // a lâmina do modelo aponta para +Z; o fio fica voltado para fora da palma (-normal)
            k.transform.localRotation = Quaternion.LookRotation(eixo, lado);
            const float gripDireita = 0.035f;   // ponto do cabo (m) onde a mão direita pega
            k.transform.localPosition = palma - k.transform.localRotation * Vector3.forward * (gripDireita / escalaMao);

            var pegada = new GameObject("PegadaEsquerda").transform;
            pegada.SetParent(k.transform, false);
            pegada.localPosition = new Vector3(0f, 0f, -0.095f);
            pegada.localRotation = Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(-90f, 0f, 0f);
            k.SetActive(false);
            Debug.Log("[Faith Clash] Katana montada: eixo local " + eixo + ", palma " + palma);
        }

        // chave do jogo -> (nome do prefab, escala). Pacotes gratuitos da Asset Store (veja Docs/CREDITOS_ASSETS.md).
        static readonly (string chave, string prefab, float escala)[] Efeitos =
        {
            ("impacto_leve", "CFXR3 Hit Misc A", 1f), ("impacto_forte", "CFXR _POW_", 1f), ("impacto_espada", "CFXR4 Sword Hit PLAIN (Cross)", 1f),
            ("impacto_fogo", "CFXR3 Hit Fire B (Air)", 1f), ("impacto_gelo", "CFXR3 Hit Ice B (Air)", 1f), ("impacto_raio", "CFXR3 Hit Electric C (Air)", 1f),
            ("corte_vermelho", "Charge slash red", 1f), ("corte_azul", "Charge slash blue", 1f), ("corte_area", "AoE slash orange", 1f),
            ("fogo_sopro", "CFXR Fire Breath", 1f), ("fogo_grande", "VFX_Fire_01_Big", 1f), ("fogo_explosao", "CFXR3 Fire Explosion B", 1f), ("fogo_chao", "VFX_Fire_Floor_01", 1f),
            ("agua_splash", "CFXR Water Splash (Smaller)", 1.5f), ("agua_ondas", "CFXR Water Ripples", 1.5f), ("agua_trilha", "VFX_Trail_Water", 1f),
            ("terra_pedras", "Stones hit", 1f), ("terra_corte", "Stone slash", 1f), ("terra_explosao", "Ground AOE explosion", 1f),
            ("vento_fumaca", "Smoke vortex", 1f), ("vento_faiscas", "Sparks explode white", 1f), ("raio_corte", "Electro slash", 1f), ("raio_aura", "Lightning aura", 1f),
            ("roxo_circulo", "Magic circle", 1f), ("roxo_portal", "Portal red", 1f), ("azul_portal", "Portal blue", 1f), ("azul_plexus", "Plexus AoE", 1f),
            ("poder_aura", "Buff", 1f), ("poder_estrela", "Star aura", 1f), ("cura_circulo", "Healing circle", 1f), ("meditar", "L_Recover_hold", 1f),
            ("escudo", "Magic shield blue", 1f), ("teleporte", "Teleport", 1f), ("poeira", "Dust puff", 1f), ("poeira_chao", "Dust ground", 1f),
            ("explosao_vermelha", "Red energy explosion", 1f),
            ("ki_circulo", "Magic circle 2", 1f), ("ki_faiscas", "Sparks explode blue", 1f), ("raio_trilha", "VFX_Trail_Electric", 1f), ("raio_hit", "Electro hit", 1f),
            ("areia_trilha", "VFX_Trail_Earth", 1f), ("maldito", "CFXR2 _CURSED_", 1f), ("vazio_trilha", "VFX_Trail_Void", 1f), ("dominio_domo", "Smoke hemisphere loop", 1f),
            ("sangue_espinho", "BloodSprayFX", 1f), ("fogos_azul", "FX_Fireworks_Blue_Large", 1f), ("fogos_verde", "FX_Fireworks_Green_Large", 1f), ("fogos_amarelo", "FX_Fireworks_Yellow_Large", 1f),
            ("pedras_cair", "FX_Rubble", 1f), ("explosao_pedras", "FX_Explosion_Rubble", 1f), ("poeira_nuvem", "FX_Dust_Prefab_01", 1f), ("terra_respingo", "FX_DirtSplatter", 1f), ("vapor", "FX_Steam", 1f),
            ("faisca_amarela", "Sparks explode yellow", 1f), ("portal_vermelho", "Portal red", 1f), ("raio_luz", "FX_Lightray", 1f), ("meteoros", "Meteors AOE", 1f), ("laser_area", "Laser AOE", 1f), ("congelar", "Freeze circle", 1f),
        };

        static void ConfigurarVFX(GameObject raiz)
        {
            var cat = raiz.AddComponent<CatalogoVFX>();
            var so = new SerializedObject(cat);
            var lista = so.FindProperty("itens");
            lista.ClearArray();
            int n = 0;
            foreach (var (chave, nome, escala) in Efeitos)
            {
                var guid = AssetDatabase.FindAssets(nome + " t:Prefab").FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == nome);
                if (guid == null) { Debug.LogWarning("[Faith Clash] VFX ausente: " + nome); continue; }
                lista.InsertArrayElementAtIndex(n);
                var e = lista.GetArrayElementAtIndex(n++);
                e.FindPropertyRelative("chave").stringValue = chave;
                e.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                e.FindPropertyRelative("escala").floatValue = escala;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Faith Clash] Efeitos visuais ligados: " + n + "/" + Efeitos.Length);
        }

        static void Ligar(Object alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
