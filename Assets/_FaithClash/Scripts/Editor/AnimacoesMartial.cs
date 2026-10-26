#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Monta os Animator Controllers do jogador e dos inimigos com as animações do pacote "Martial Art Animations" e com os clipes de pulo/obstáculo
    /// e o parado enviados pelo usuário (pasta Personagens/JogadorPrincipal/Animacoes/Manuais). Todos os clipes são Humanoid e ficam no lugar
    /// (quem move o corpo é o PlayerController).
    /// </summary>
    public static class AnimacoesMartial
    {
        public const string PastaMartial = "Assets/Martial Art Animations Sample/Animations";
        const string PastaManual = "Assets/Personagens/JogadorPrincipal/Animacoes/Manuais";

        // clipe -> repete?
        static readonly (string arquivo, bool repete)[] Martial =
        {
            ("Atk_P_1", false), ("Atk_P_2", false), ("Atk_P_3", false), ("Atk_K_1", false), ("Atk_K_2", false), ("Atk_K_3", false),
            ("Dodge_B", false), ("Dodge_F", false), ("Quickstep_B", false), ("Quickstep_F", false),
            ("Fight_Idle", true), ("Guard_Idle", true), ("Walk_B", true), ("Walk_F", true),
            ("GetUp_A", false), ("Hit_A", false), ("Knock_A", false), ("Taunt", false),
            ("Grab_Start", false), ("Grab_Hit_A", false), ("Grab_Hit_B", false), ("Grab_Break_A", false), ("Grab_Break_B", false), ("Grab_Idle_A", true), ("Grab_Idle_B", true),
        };
        // animações do usuário no projeto (nome fixo, repete?). As novas vêm da pasta D:\Faith Clash\Jogador\Jogador Principal Homem\Animacoes (ver ImportarDoUsuario).
        static readonly (string arquivo, bool repete)[] Manuais =
        {
            ("Parado_Manual", true), ("Rasteira_Usuario", false), ("Pulo_Correndo", false),
            ("Andar", true), ("Correr_A", true), ("Correr_B", true), ("Strafe_Direita", true), ("Strafe_Esquerda", true),
            ("Virar_180", false), ("Escada", true),
            ("Queda_Alta", false), ("Descer_Escada", true), ("Mureta_1", false), ("Mureta_2", false),
        };

        const string PastaUsuario = @"D:\Faith Clash\Jogador\Jogador Principal Homem\Animacoes";
        // arquivo na pasta do usuário -> nome no projeto
        static readonly Dictionary<string, string> NomesConhecidos = new Dictionary<string, string>
        {
            { "andando so no W.fbx", "Andar" },
            { "correndo no shift.fbx", "Correr_A" },
            { "Correndo pressionando shift.fbx", "Correr_B" },
            { "correndo no shift ou andando normal com o foco Q ativo pela direita.fbx", "Strafe_Direita" },
            { "correndo no shift ou andando normal com o foco Q ativo pela esquerda.fbx", "Strafe_Esquerda" },
            { "Mudar de direcao para tras enquanto esta correndo sprint com shift pressionado.fbx", "Virar_180" },
            { "subindo escadas correndo ou andando.fbx", "Escada" },
            { "caindo de lugares altos como o objeto MURO DE ESCALADA segurando o shift.fbx", "Queda_Alta" },
            { "descendo escadas correndo ou andando.fbx", "Descer_Escada" },
            { "pulando obstaculos como a MURETA enquanto corre no shift 1.fbx", "Mureta_1" },
            { "pulando obstaculos como a MURETA enquanto corre no shift 2.fbx", "Mureta_2" },
        };

        /// <summary>Copia para Manuais os FBX novos da pasta do usuário (com nome seguro). Só os que têm nome conhecido.</summary>
        public static void ImportarDoUsuario()
        {
            if (!Directory.Exists(PastaUsuario)) return;
            Directory.CreateDirectory(PastaManual);
            foreach (var f in Directory.GetFiles(PastaUsuario, "*.fbx"))
            {
                string nome = Path.GetFileName(f);
                if (!NomesConhecidos.TryGetValue(nome, out var dest)) { Debug.LogWarning("[Animações] arquivo sem destino conhecido: " + nome); continue; }
                string alvo = Path.Combine(PastaManual, dest + ".fbx");
                if (!File.Exists(alvo) || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(alvo)) { File.Copy(f, alvo, true); Debug.Log("[Animações] importado: " + nome + " -> " + dest); }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Faith Clash/Ferramentas/Configurar Animações (Martial)")]
        public static void ConfigurarClipes()
        {
            ImportarDoUsuario();
            foreach (var (arq, rep) in Martial) Ajustar($"{PastaMartial}/{arq}.fbx", rep, false);
            foreach (var (arq, rep) in Manuais) if (File.Exists($"{PastaManual}/{arq}.fbx")) Ajustar($"{PastaManual}/{arq}.fbx", rep, true);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Humanoid, em loop quando for o caso, sem deslocamento (XZ, altura e giro presos à pose).</summary>
        const string FbxPersonagem = "Assets/Personagens/JogadorPrincipal/Mesh/JogadorPrincipal.fbx";

        internal static void Ajustar(string caminho, bool repete, bool criarAvatar)
        {
            var imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
            if (imp == null) { Debug.LogWarning("[Animações] não achei " + caminho); return; }
            if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; if (criarAvatar) imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; imp.SaveAndReimport(); imp = (ModelImporter)AssetImporter.GetAtPath(caminho); }
            // animações do usuário: usam o esqueleto (avatar) do próprio personagem. Cada FBX exportado tem o 1º quadro da animação no lugar
            // da pose de repouso; criando um avatar por arquivo, o retarget ficava torto (braços erguidos, pés para fora, tronco inclinado).
            if (criarAvatar)
            {
                // mesmo esqueleto do personagem, mas a hierarquia do arquivo difere um pouco (raiz com o nome do arquivo): em vez de copiar o avatar,
                // o avatar do clipe recebe a POSE DE REPOUSO do personagem, osso por osso
                var impP = AssetImporter.GetAtPath(FbxPersonagem) as ModelImporter;
                if (impP != null)
                {
                    var hdP = impP.humanDescription;
                    var repouso = new Dictionary<string, SkeletonBone>();
                    foreach (var sbn in hdP.skeleton) repouso[sbn.name] = sbn;
                    if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; imp.sourceAvatar = null; imp.SaveAndReimport(); imp = (ModelImporter)AssetImporter.GetAtPath(caminho); }
                    var hd = imp.humanDescription;
                    var esq = hd.skeleton; int trocados = 0; bool mudou = false;
                    for (int i = 0; i < esq.Length; i++)
                    {
                        if (!repouso.TryGetValue(esq[i].name, out var r)) continue;
                        if (Quaternion.Angle(esq[i].rotation, r.rotation) > 0.01f || (esq[i].position - r.position).sqrMagnitude > 1e-8f) mudou = true;
                        esq[i].rotation = r.rotation; esq[i].position = r.position; esq[i].scale = r.scale; trocados++;
                    }
                    if (mudou)
                    {
                        hd.skeleton = esq;
                        imp.humanDescription = hd;
                        imp.SaveAndReimport(); imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
                        Debug.Log($"[Animações] {Path.GetFileName(caminho)}: pose de repouso do personagem aplicada ({trocados} ossos)");
                    }
                }
            }
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = imp.defaultClipAnimations;
            if (clips.Length == 0) return;
            var c = clips[0];
            c.name = Path.GetFileNameWithoutExtension(caminho);
            c.loopTime = repete; c.loopPose = repete;
            // clipes do usuário (parkour, pulos, rasteira, corrida): o deslocamento do clipe NÃO entra na pose (o corpo fica sobre a cápsula);
            // sem "bake into pose" ele vira movimento de raiz, que é descartado (applyRootMotion desligado) — quem leva o personagem é o código.
            // Golpes do pacote Martial continuam com a pose travada como antes.
            bool livre = criarAvatar;
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootPositionXZ = !livre; c.keepOriginalPositionXZ = !livre;
            c.lockRootHeightY = true; c.keepOriginalPositionY = !livre; c.heightFromFeet = livre;   // pés no chão (quem sobe no pulo é a física)
            imp.clipAnimations = new[] { c };
            imp.SaveAndReimport();
        }

        static AnimationClip Clipe(string caminho)
        {
            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        }
        static AnimationClip M(string nome) => Clipe($"{PastaMartial}/{nome}.fbx");
        static AnimationClip U(string nome) => Clipe($"{PastaManual}/{nome}.fbx");

        static AnimatorState Estado(AnimatorStateMachine sm, string nome, AnimationClip clip, float velocidade = 1f, bool parametroVelocidade = true)
        {
            var st = sm.AddState(nome);
            st.motion = clip; st.speed = velocidade; st.iKOnFeet = false;
            if (parametroVelocidade) { st.speedParameterActive = true; st.speedParameter = "VelAcao"; }
            return st;
        }

        static AnimatorController Novo(string destino)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            // reaproveita o mesmo arquivo (mesmo GUID): o prefab e a cena aberta continuam apontando para ele (apagar e recriar deixava o personagem em pose T)
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(destino);
            if (ctrl != null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(destino)) if (o != null && o != ctrl) Object.DestroyImmediate(o, true);
                ctrl.parameters = new AnimatorControllerParameter[0];
                var smNova = new AnimatorStateMachine { name = "Base Layer", hideFlags = HideFlags.HideInHierarchy };
                AssetDatabase.AddObjectToAsset(smNova, ctrl);
                ctrl.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", stateMachine = smNova, defaultWeight = 1f } };
            }
            else ctrl = AnimatorController.CreateAnimatorControllerAtPath(destino);
            foreach (var n in new[] { "Velocidade", "EscalaPassada", "VelY", "VelAcao", "MoveX", "MoveY" }) ctrl.AddParameter(n, AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Espelho", AnimatorControllerParameterType.Bool);
            var ps = ctrl.parameters;
            for (int i = 0; i < ps.Length; i++) if (ps[i].name == "VelAcao" || ps[i].name == "EscalaPassada") ps[i].defaultFloat = 1f;
            ctrl.parameters = ps;
            var camadas = ctrl.layers; camadas[0].iKPass = true; ctrl.layers = camadas;
            return ctrl;
        }

        public static AnimatorController CriarJogador(string destino)
        {
            var ctrl = Novo(destino);
            var sm = ctrl.layers[0].stateMachine;
            var luta = M("Fight_Idle"); var parado = U("Parado_Manual") ?? luta; var andar0 = M("Walk_F"); var tras = M("Walk_B");
            var andar = U("Andar") ?? andar0; var correrA = U("Correr_A") ?? andar0; var correrB = U("Correr_B") ?? correrA;
            var stD = U("Strafe_Direita") ?? andar; var stE = U("Strafe_Esquerda") ?? andar;
            bool temCorrida = U("Correr_A") != null;
            // Mixamo: locomoção com os ciclos baixados (andar, correr médio/rápido, sprint, trás, strafe, correr com espada)
            var mAndar = U("Mix_Andar"); var mCorM = U("Mix_CorrerMedio"); var mCorR = U("Mix_CorrerRapido"); var mSprint = U("Mix_Sprint"); var mTras = U("Mix_Tras");
            var mStE = U("Mix_StrafeE"); var mStD = U("Mix_StrafeD"); var mEsp = U("Mix_EspadaCorrer");
            bool mix = mAndar != null && mCorM != null && mCorR != null && mSprint != null;

            // locomoção: o parado do usuário; os modos de combate usam a guarda de luta só com o alvo travado. Andar a 1,8 m/s; correr e sprint = o ciclo de andar acelerado.
            void Loco(string nome, AnimationClip idle, bool espada = false)
            {
                ctrl.CreateBlendTreeInController(nome, out BlendTree arv);
                arv.blendParameter = "Velocidade"; arv.useAutomaticThresholds = false;
                if (mix)
                {
                    // velocidades naturais dos ciclos (m/s): andar 1,4; correr médio 4,0; correr rápido 5,4; sprint 6,8; correr com espada 5,4
                    arv.AddChild(idle, 0f); arv.AddChild(mAndar, 1.8f); arv.AddChild(mCorM, 4.0f);
                    arv.AddChild(espada && mEsp != null ? mEsp : mCorR, 5.4f); arv.AddChild(espada && mEsp != null ? mEsp : mSprint, 7.0f);
                    var fm = arv.children; fm[1].timeScale = 1.29f; fm[2].timeScale = 1.0f; fm[3].timeScale = 1.0f; fm[4].timeScale = espada && mEsp != null ? 1.3f : 1.03f; arv.children = fm;
                    var estm = sm.states.First(s => s.state.name == nome).state;
                    estm.speedParameterActive = true; estm.speedParameter = "EscalaPassada";
                    return;
                }
                arv.AddChild(idle, 0f); arv.AddChild(andar, 1.8f);
                if (temCorrida) { arv.AddChild(correrA, 4.6f); arv.AddChild(correrB, 7.5f); }
                else { arv.AddChild(andar, 5.0f); arv.AddChild(andar, 7.5f); var f0 = arv.children; f0[2].timeScale = 2.1f; f0[3].timeScale = 2.9f; arv.children = f0; }
                var est = sm.states.First(s => s.state.name == nome).state;
                est.speedParameterActive = true; est.speedParameter = "EscalaPassada";
            }
            Loco("Locomocao", parado); Loco("LocomocaoFerido", parado);
            Loco("LocomocaoCombate", parado); Loco("LocomocaoEspada", parado, true); Loco("LocomocaoEspadaDupla", parado, true);
            // com o alvo travado: anda para a frente/trás/lados olhando para o inimigo
            {
                ctrl.CreateBlendTreeInController("LocomocaoAlvo", out BlendTree alvo);
                alvo.blendType = BlendTreeType.FreeformDirectional2D; alvo.blendParameter = "MoveX"; alvo.blendParameterY = "MoveY"; alvo.useAutomaticThresholds = false;
                alvo.AddChild(luta, new Vector2(0, 0));
                alvo.AddChild(andar, new Vector2(0, 1.8f)); alvo.AddChild(tras, new Vector2(0, -1.8f));
                alvo.AddChild(stE, new Vector2(-1.8f, 0)); alvo.AddChild(stD, new Vector2(1.8f, 0));
                alvo.AddChild(correrA, new Vector2(0, 5f)); alvo.AddChild(tras, new Vector2(0, -5f));
                alvo.AddChild(stE, new Vector2(-5f, 0)); alvo.AddChild(stD, new Vector2(5f, 0));
                if (mix)
                {
                    var fa = alvo.children;   // ordem: 0 luta, 1 andar, 2 trás, 3 esq, 4 dir, 5 frente rápido, 6 trás rápido, 7 esq rápido, 8 dir rápido
                    fa[5].motion = mCorR; fa[5].timeScale = 1.0f;
                    if (mTras != null) { fa[6].motion = mTras; fa[6].timeScale = 1.39f; }
                    if (mStE != null) { fa[7].motion = mStE; fa[7].timeScale = 1.43f; }
                    if (mStD != null) { fa[8].motion = mStD; fa[8].timeScale = 1.43f; }
                    fa[1].motion = mAndar; fa[1].timeScale = 1.29f;
                    alvo.children = fa;
                }
            }
            sm.defaultState = sm.states.First(s => s.state.name == "Locomocao").state;

            // combate corpo a corpo (Martial Art Animations): golpes, pegadas, reações e esquivas
            foreach (var n in new[] { "Atk_P_1", "Atk_P_2", "Atk_P_3", "Atk_K_1", "Atk_K_2", "Atk_K_3", "Taunt", "Grab_Start", "Grab_Hit_A", "Grab_Hit_B", "Grab_Break_A", "Grab_Break_B" })
            {
                var c = M(n); if (c == null) continue;
                Estado(sm, n, c, Mathf.Max(1f, c.length / 0.9f));
            }
            Estado(sm, "Hit_A", M("Hit_A")); Estado(sm, "Knock_A", M("Knock_A")); Estado(sm, "GetUp_A", M("GetUp_A"));
            Estado(sm, "Guard_Idle", M("Guard_Idle")); Estado(sm, "Grab_Idle_A", M("Grab_Idle_A")); Estado(sm, "Grab_Idle_B", M("Grab_Idle_B"));
            Estado(sm, "Dodge_F", M("Dodge_F")); Estado(sm, "Dodge_B", M("Dodge_B")); Estado(sm, "Quickstep_F", M("Quickstep_F")); Estado(sm, "Quickstep_B", M("Quickstep_B"));

            // pulo: o "pular correndo no shift" do usuário serve para todos os pulos (parado, correndo, sprint), no ar e no pouso
            var pulo = U("Pulo_Correndo");
            if (pulo != null)
            {
                Estado(sm, "PuloImpulso", pulo, 1.0f);
                Estado(sm, "PuloCorrendoLivre", pulo);
                Estado(sm, "PuloCorrendo", pulo);
                var noAr = Estado(sm, "NoAr", pulo, 0f, false); noAr.cycleOffset = 0.45f;   // pose do ápice
                var pouso = Estado(sm, "PousoLeve", pulo, 1.8f); pouso.cycleOffset = 0.7f;
            }
            // virar 180° correndo, escada e subir em objeto médio (animações do usuário)
            var vira = U("Virar_180"); if (vira != null) { Estado(sm, "U_MudarDirecao", vira); Estado(sm, "U_Katana180", vira); }
            var esc = U("Escada"); if (esc != null) Estado(sm, "U_Escada", esc, 1f, true);
            var escD = U("Descer_Escada"); if (escD != null) Estado(sm, "U_EscadaDescer", escD, 1f, true);
            var mu1 = U("Mureta_1"); if (mu1 != null) Estado(sm, "U_Mureta1", mu1);
            var mu2 = U("Mureta_2"); if (mu2 != null) Estado(sm, "U_Mureta2", mu2);
            var queda = U("Queda_Alta"); if (queda != null) { var qa = Estado(sm, "U_QuedaAr", queda, 0f, false); qa.cycleOffset = 0.02f; }   // pose de queda longa (primeiro quadro do clipe)
            // Mixamo: rolamento de pouso, magia e corrida na parede
            var rolar = U("Mix_Rolar"); if (rolar != null) Estado(sm, "U_Rolar", rolar);
            var magia = U("Mix_Magia"); if (magia != null) Estado(sm, "Mx_Magia", magia);
            var parede = U("Mix_WallRun"); if (parede != null) Estado(sm, "U_WallRun", parede);
            // rasteira (slide por baixo): a do usuário
            var rasteira = U("Rasteira_Usuario");
            if (rasteira != null) { Estado(sm, "Rasteira", rasteira, Mathf.Max(1f, rasteira.length / 0.9f)); Estado(sm, "U_RolarCorrer", rasteira); }

            EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
            Debug.Log("[Animações] controller do jogador (Martial + parado, rasteira e pulo do usuário) criado: " + destino);
            return ctrl;
        }

        public static AnimatorController CriarInimigo(string destino)
        {
            var ctrl = Novo(destino);
            var sm = ctrl.layers[0].stateMachine;
            var luta = M("Fight_Idle"); var andar = M("Walk_F");
            Estado(sm, "Mx_Fighting_Idle", luta, 1f, false);
            Estado(sm, "Andar", andar, 1f, false);
            Estado(sm, "Correr", andar, 2.2f, false);
            Estado(sm, "Hit_A", M("Hit_A"), 1f, false);
            Estado(sm, "Knock_A", M("Knock_A"), 1f, false);
            Estado(sm, "Atq0", M("Atk_P_1"), 1f, false); Estado(sm, "Atq1", M("Atk_K_1"), 1f, false); Estado(sm, "Atq2", M("Atk_P_3"), 1f, false);
            sm.defaultState = sm.states.First(s => s.state.name == "Mx_Fighting_Idle").state;
            EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
            Debug.Log("[Animações] controller dos inimigos (Martial) criado: " + destino);
            return ctrl;
        }
    }
}
#endif
