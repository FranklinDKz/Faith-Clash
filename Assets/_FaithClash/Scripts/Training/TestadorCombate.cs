using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Training
{
    /// <summary>
    /// Teste automático de jogo (Faith Clash > Ferramentas > Testar Combate, dentro do Editor): injeta teclas e cliques do jogador
    /// e registra, em Ferramentas/analise/teste_combate.txt, o que o Animator tocou (estado e tempo), os impactos, o dano,
    /// a física do pulo e da queda, o modo de combate (5, G, 1-4), a meditação (E) e se algo tocou sozinho.
    /// </summary>
    public class TestadorCombate : MonoBehaviour
    {
        public const string ChaveSessao = "FC_TESTAR_COMBATE";
#if UNITY_EDITOR
        readonly StringBuilder log = new StringBuilder();
        readonly HashSet<Key> teclas = new HashSet<Key>();
        bool botaoEsq, botaoDir;
        Animator animator;
        Dictionary<int, string> nomes = new Dictionary<int, string>();
        PlayerController jogador;
        MeleeCombat combate;
        ModoCombate modo;
        StatusJogador status;
        AnimadorProtagonista animadorScript;
        string ultimoEstado = "";
        float t0;
        bool registrando;

        void L(string s) => log.AppendLine((registrando ? $"[{Time.time - t0,6:0.00}] " : "") + s);

        IEnumerator Start()
        {
            if (SessionState.GetInt(ChaveSessao, 0) != 1) yield break;
            SessionState.SetInt(ChaveSessao, 0);
            yield return null;

            jogador = FindAnyObjectByType<PlayerController>();
            if (jogador == null) { Debug.LogError("[TesteCombate] Jogador não encontrado."); yield break; }
            combate = jogador.GetComponent<MeleeCombat>();
            modo = jogador.GetComponent<ModoCombate>();
            status = jogador.GetComponent<StatusJogador>();
            animadorScript = jogador.GetComponent<AnimadorProtagonista>();
            animator = animadorScript.Animator;
            var ctrl = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
            foreach (var s in ctrl.layers[0].stateMachine.states) nomes[Animator.StringToHash(s.state.name)] = s.state.name;
            Cursor.lockState = CursorLockMode.Locked;
            Directory.CreateDirectory("Ferramentas/analise/fotos");
            foreach (var f in Directory.GetFiles("Ferramentas/analise/fotos", "*.png")) File.Delete(f);
            combate.AttackStarted += p => L($"  evento: golpe iniciado (passo {p})");
            combate.AttackImpact += (p, ok) => L($"  evento: IMPACTO passo {p} acertou={ok}");
            status.Aviso += a => L("  aviso HUD: " + a);

            L("== inicio: o que o Animator toca nos primeiros 1,5 s (deve ser só Locomocao) ==");
            yield return Janela(1.5f);

            // ------------------------------------------------ 0) nada toca sozinho
            L("== 0) PARADO 6 s e CORRENDO 4 s: só pode aparecer Locomocao ==");
            Posicionar(new Vector3(0, 0.1f, -3f), 0f);
            yield return Janela(6f);
            Tecla(Key.W, true); Tecla(Key.LeftShift, true);
            yield return Janela(4f);
            Tecla(Key.W, false); Tecla(Key.LeftShift, false);
            yield return Janela(1.2f);

            // ------------------------------------------------ 1) modo Combate: cadeia leve
            yield return Modo(Key.Digit5, "Combate");
            foreach (var passo in new[] { "cadeia leve (4 toques, 0,35 s entre eles)", "ramo S no 3º", "ramo A no 4º" })
            {
                var boneco = PreparaBoneco();
                L("== 1) COMBATE: " + passo + " ==");
                registrando = true; t0 = Time.time;
                for (int i = 0; i < 4; i++)
                {
                    if (passo.Contains("S no 3") && i == 2) Tecla(Key.S, true);
                    if (passo.Contains("A no 4") && i == 3) Tecla(Key.A, true);
                    yield return Clique(0.06f);
                    yield return Janela(0.1f);
                    Tecla(Key.S, false); Tecla(Key.A, false);
                    yield return Janela(0.3f);
                }
                yield return Janela(1.2f);
                L($"  boneco: vida {boneco.Vida.Current:0}/{boneco.Vida.Max:0}  combo={combate.ComboCount}");
                registrando = false;
            }

            // ------------------------------------------------ 2) golpes fortes (segurar o clique)
            foreach (var antes in new[] { 0, 1, 2, 3 })
            {
                var boneco = PreparaBoneco();
                L($"== 2) COMBATE: golpe forte depois de {antes} golpes leves ==");
                registrando = true; t0 = Time.time;
                for (int i = 0; i < antes; i++) { yield return Clique(0.06f); yield return Janela(0.35f); }
                yield return Segurar(0.4f);
                yield return Janela(1.6f);
                L($"  boneco: vida {boneco.Vida.Current:0}/{boneco.Vida.Max:0}");
                registrando = false;
            }
            {
                var boneco = PreparaBoneco(4.5f);
                L("== 2b) COMBATE: correndo + clique (joelhada voadora) ==");
                registrando = true; t0 = Time.time;
                Tecla(Key.W, true); Tecla(Key.LeftShift, true);
                yield return Janela(0.7f);
                yield return Clique(0.06f);
                Tecla(Key.W, false); Tecla(Key.LeftShift, false);
                yield return Janela(1.4f);
                L($"  boneco: vida {boneco.Vida.Current:0}/{boneco.Vida.Max:0}");
                registrando = false;
            }

            // ------------------------------------------------ 3) espada
            yield return Modo(Key.G, "Espada");
            {
                var boneco = PreparaBoneco();
                L("== 3) ESPADA: 4 cortes + pesado ==");
                registrando = true; t0 = Time.time;
                for (int i = 0; i < 4; i++) { yield return Clique(0.06f); yield return Janela(0.5f); }
                yield return Janela(1.5f);
                yield return Segurar(0.4f);
                yield return Janela(2.4f);
                L($"  boneco: vida {boneco.Vida.Current:0}/{boneco.Vida.Max:0}");
                registrando = false;
            }

            // ------------------------------------------------ 4) poder: só gestos de jutsu
            yield return Modo(Key.Digit1, "Poder");
            foreach (var el in new[] { (Key.Digit1, "Fogo"), (Key.Digit2, "Água"), (Key.Digit3, "Terra"), (Key.Digit4, "Ar") })
            {
                var boneco = PreparaBoneco(4f);
                status.GanharFe(900f);
                L($"== 4) PODER {el.Item2}: clique (leve), segurar (forte), F ==");
                Tecla(el.Item1, true); yield return Janela(0.1f); Tecla(el.Item1, false); yield return Janela(0.3f);
                registrando = true; t0 = Time.time;
                float fe0 = status.Fe;
                yield return Clique(0.06f);
                for (int fi = 0; fi < 14; fi++) { Foto("Jutsu_" + el.Item2, fi); yield return new WaitForSeconds(0.2f); }
                L($"  Fé gasta no leve: {fe0 - status.Fe:0}");
                fe0 = status.Fe;
                yield return Segurar(0.4f); yield return Janela(3.2f);
                L($"  Fé gasta no forte: {fe0 - status.Fe:0}");
                Tecla(Key.F, true); yield return Janela(0.08f); Tecla(Key.F, false); yield return Janela(3.2f);
                L($"  boneco: vida {boneco.Vida.Current:0}/{boneco.Vida.Max:0}");
                registrando = false;
            }

            // ------------------------------------------------ 5) meditar (E)
            yield return Modo(Key.Digit5, "Combate");
            {
                Posicionar(new Vector3(0, 0.1f, -3f), 0f);
                status.GastarFe(status.Fe - 100f);
                L("== 5) MEDITAR: segurar E por 2 s, depois mover ==");
                registrando = true; t0 = Time.time;
                float fe0 = status.Fe;
                Tecla(Key.E, true);
                yield return Janela(2f);
                L($"  Fé: {fe0:0} -> {status.Fe:0} (meditando={status.Meditando})");
                Tecla(Key.W, true); yield return Janela(0.3f); Tecla(Key.W, false);
                L($"  depois de mover: meditando={status.Meditando}");
                Tecla(Key.E, false);
                yield return Janela(0.8f);
                registrando = false;
            }

            // ------------------------------------------------ 6) pulo e queda
            L("== 6) PULO (toque), PULO (segurando) e QUEDA de 3 m e de 8 m ==");
            foreach (var seg in new[] { false, true })
            {
                Posicionar(new Vector3(0, 0.1f, -3f), 0f);
                yield return Janela(0.6f);
                registrando = true; t0 = Time.time;
                float y0 = jogador.transform.position.y, apice = 0f;
                L(seg ? "  -- pulo segurando o Espaço 0,6 s --" : "  -- pulo curto (toque) --");
                Tecla(Key.Space, true);
                float soltar = seg ? 0.6f : 0.05f;
                float tIni = Time.time; bool soltou = false;
                while (Time.time - tIni < 2.2f)
                {
                    if (!soltou && Time.time - tIni >= soltar) { Tecla(Key.Space, false); soltou = true; }
                    apice = Mathf.Max(apice, jogador.transform.position.y - y0);
                    Amostra();
                    yield return null;
                }
                Tecla(Key.Space, false);
                L($"  altura máxima do pulo: {apice:0.00} m");
                registrando = false;
            }
            foreach (var altura in new[] { 3f, 8f })
            {
                Posicionar(new Vector3(0, 0.1f + altura, -3f), 0f);
                registrando = true; t0 = Time.time;
                L($"  -- queda de {altura} m --");
                float tIni = Time.time; float menorPe = 9f;
                var pes = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
                while (Time.time - tIni < 3.2f)
                {
                    foreach (var p in pes) menorPe = Mathf.Min(menorPe, p.position.y - 0.1f);
                    Amostra();
                    yield return null;
                }
                L($"  pé mais baixo em relação ao chão: {menorPe:0.000} m");
                registrando = false;
            }

            // ------------------------------------------------ 7) esquiva cancela golpe
            if (modo.Atual != FaithClash.Combat.Modo.Combate) yield return Modo(Key.Digit5, "Combate");
            {
                PreparaBoneco();
                L("== 7) golpe + Ctrl (rolamento cancela) + C (dash) ==");
                registrando = true; t0 = Time.time;
                yield return Clique(0.06f); yield return Janela(0.1f);
                Tecla(Key.LeftCtrl, true); yield return Janela(0.06f); Tecla(Key.LeftCtrl, false);
                yield return Janela(1.6f);
                yield return Clique(0.06f); yield return Janela(0.1f);
                Tecla(Key.C, true); yield return Janela(0.06f); Tecla(Key.C, false);
                yield return Janela(1.2f);
                L($"  atacando agora? {combate.IsAttacking}");
                registrando = false;
            }

            // ------------------------------------------------ 8) fotos (vista lateral) de cada golpe, para conferir a pose no corpo real
            yield return FotosDosGolpes();

            Tecla(Key.W, false);
            Directory.CreateDirectory("Ferramentas/analise");
            File.WriteAllText("Ferramentas/analise/teste_combate.txt", log.ToString(), Encoding.UTF8);
            Debug.Log("[TesteCombate] Pronto: Ferramentas/analise/teste_combate.txt");
            EditorApplication.isPlaying = false;
        }

        // ------------------------------------------------------------------ fotos
        Camera camFoto;
        RenderTexture rtFoto;
        Texture2D texFoto;

        IEnumerator FotosDosGolpes()
        {
            Directory.CreateDirectory("Ferramentas/analise/fotos");
            var go = new GameObject("CamFoto");
            camFoto = go.AddComponent<Camera>();
            camFoto.enabled = false;
            camFoto.clearFlags = CameraClearFlags.SolidColor;
            camFoto.backgroundColor = new Color(0.35f, 0.4f, 0.48f);
            camFoto.fieldOfView = 38f;
            rtFoto = new RenderTexture(220, 280, 24);
            texFoto = new Texture2D(220, 280, TextureFormat.RGB24, false);
            if (modo.Atual != FaithClash.Combat.Modo.Combate) yield return Modo(Key.Digit5, "Combate");
            foreach (var golpe in new[] { "Jab", "Direto", "Gancho", "ChuteLateral", "ChuteBaixo", "GiroCostas", "ChuteFrontal", "Cotovelada", "Joelhada", "ChuteMachado", "ChuteLado", "ChuteCostas", "Voadora", "VoadoraGiratoria", "Corte1", "Corte2", "Corte3", "CorteGiratorio", "EspadaSalto", "Estocada", "Mx_Mma_Kick", "Mx_Side_Kick", "Mx_Roundhouse_Kick", "Mx_Spin_Flip_Kick", "Mx_Martelo_3", "Mx_Meia_Lua_De_Compasso", "Mx_Armada", "Mx_Hurricane_Kick", "Mx_Illegal_Elbow_Punch", "Mx_Illegal_Knee", "Mx_Knee_Kick_Lead", "Mx_Elbow_Uppercut_Combo", "Mx_Surprise_Uppercut", "Mx_Knee_Jabs_To_Uppercut", "Mx_Standing_Melee_Combo_Attack_Ver_2", "Mx_Flying_Knee_Punch_Combo", "Mx_Flying_Kick", "Mx_Standing_Melee_Run_Jump_Attack", "Mx_Two_Hand_Sword_Combo", "Mx_One_Hand_Sword_Combo", "Mx_Dual_Weapon_Combo", "Mx_Great_Sword_High_Spin_Attack", "Mx_Great_Sword_Jump_Attack" })
            {
                PreparaBoneco(1.9f);
                yield return Janela(0.5f);
                combate.TestarGolpe(golpe);
                for (int i = 0; i < 16; i++)
                {
                    Foto(golpe, i);
                    yield return new WaitForSeconds(0.065f);
                }
                yield return Janela(0.8f);
            }
            foreach (var passo in new[] { "Pulo", "Rolamento" })
            {
                Posicionar(new Vector3(0, 0.1f, -3f), 0f);
                yield return Janela(0.6f);
                if (passo == "Pulo") Tecla(Key.Space, true); else Tecla(Key.LeftCtrl, true);
                for (int i = 0; i < 16; i++)
                {
                    Foto(passo, i);
                    if (i == 2) { Tecla(Key.Space, false); Tecla(Key.LeftCtrl, false); }
                    yield return new WaitForSeconds(passo == "Pulo" ? 0.07f : 0.09f);
                }
                yield return Janela(0.8f);
            }
            foreach (var nome in new[] { "CastFogo", "CastVento", "CastEnergia", "CastTerra", "PowerUp", "Curar", "MeditarEntrar", "Dano", "Morte", "Dash", "Defesa", "SacarEspada", "PuloNinja", "PegarItem", "Mx_Standing_2H_Magic_Attack_05", "Mx_Standing_2H_Magic_Area_Attack_01", "Mx_Standing_2H_Magic_Area_Attack_02", "Mx_Two_Hand_Spell_Casting", "Mx_Wide_Arm_Spell_Casting", "Mx_Magic_Spell_Casting", "Mx_Strong_Gesture", "Mx_Standing_Dodge_Left", "Mx_Standing_Dodge_Backward", "Mx_Standing_Yell", "Mx_Great_Sword_Power_Up", "Mx_Stylish_Flip", "Mx_Run_To_Flip", "Mx_Ninja_Idle", "Mx_Big_Hit_To_Head", "Mx_Standing_React_Death_Forward" })
            {
                Posicionar(new Vector3(0, 0.1f, -3f), 0f);
                yield return Janela(0.6f);
                float dur = Mathf.Min(InfoGolpes.Duracao(nome, 1.5f), 2.4f);
                animadorScript.Acao(nome, dur);
                for (int i = 0; i < 16; i++)
                {
                    Foto(nome, i);
                    yield return new WaitForSeconds(dur / 16f);
                }
                yield return Janela(0.5f);
                animadorScript.Encerrar();
                yield return Janela(0.5f);
            }
            Destroy(go);
        }

        void CriarCamFoto()
        {
            var go = new GameObject("CamFoto");
            camFoto = go.AddComponent<Camera>();
            camFoto.enabled = false;
            camFoto.clearFlags = CameraClearFlags.SolidColor;
            camFoto.backgroundColor = new Color(0.35f, 0.4f, 0.48f);
            camFoto.fieldOfView = 38f;
            rtFoto = new RenderTexture(220, 280, 24);
            texFoto = new Texture2D(220, 280, TextureFormat.RGB24, false);
        }

        void Foto(string nome, int i)
        {
            if (camFoto == null) { Directory.CreateDirectory("Ferramentas/analise/fotos"); CriarCamFoto(); }
            var p = jogador.transform;
            camFoto.transform.position = p.position + Vector3.right * 4.2f + Vector3.up * 1.15f + Vector3.back * 0.6f;
            camFoto.transform.LookAt(p.position + Vector3.up * 0.95f + Vector3.forward * 0.4f);
            camFoto.targetTexture = rtFoto;
            camFoto.Render();
            RenderTexture.active = rtFoto;
            texFoto.ReadPixels(new Rect(0, 0, 220, 280), 0, 0);
            texFoto.Apply();
            RenderTexture.active = null;
            camFoto.targetTexture = null;
            File.WriteAllBytes($"Ferramentas/analise/fotos/{nome}_{i:00}.png", texFoto.EncodeToPNG());
        }

        // ------------------------------------------------------------------ utilidades
        IEnumerator Modo(Key tecla, string esperado)
        {
            while (combate.IsAttacking) yield return null;   // só troca de modo depois do golpe terminar
            Tecla(tecla, true); yield return Janela(0.08f); Tecla(tecla, false);
            yield return Janela(1.2f);
            string atual = modo.Atual.ToString();
            L($"modo depois da tecla {tecla}: {atual} (esperado {esperado}) {(atual == esperado || (esperado == "Combate" && atual == "Combate") ? "OK" : "<<< DIFERENTE")}");
        }

        TrainingDummy PreparaBoneco(float distancia = 2.8f)
        {
            var b = TrainingDummy.Todos[0];
            b.Vida.ResetHealth();
            Posicionar(new Vector3(0, 0.1f, -3f), 0f);
            b.transform.position = new Vector3(0, 0f, -3f + distancia);
            b.transform.rotation = Quaternion.identity;
            return b;
        }

        void Posicionar(Vector3 p, float giro)
        {
            var cc = jogador.GetComponent<CharacterController>();
            cc.enabled = false;
            jogador.transform.position = p;
            jogador.transform.rotation = Quaternion.Euler(0, giro, 0);
            cc.enabled = true;
            combate.Cancelar();
        }

        void Tecla(Key k, bool apertada)
        {
            if (apertada) teclas.Add(k); else teclas.Remove(k);
            var arr = new Key[teclas.Count]; teclas.CopyTo(arr);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(arr));
        }

        void Mouse(bool esq, bool dir)
        {
            botaoEsq = esq; botaoDir = dir;
            var st = new MouseState();
            if (esq) st = st.WithButton(MouseButton.Left);
            if (dir) st = st.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, st);
        }

        IEnumerator Clique(float duracao) { Mouse(true, botaoDir); yield return Janela(duracao); Mouse(false, botaoDir); }
        IEnumerator Segurar(float duracao) { Mouse(true, botaoDir); yield return Janela(duracao); Mouse(false, botaoDir); }

        /// <summary>Espera o tempo informado registrando cada mudança de estado do Animator.</summary>
        IEnumerator Janela(float duracao)
        {
            float fim = Time.time + duracao;
            while (Time.time < fim)
            {
                Amostra();
                yield return null;
            }
        }

        void Amostra()
        {
            string e = EstadoAtual();
            if (e == ultimoEstado) return;
            var info = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            L($"  animação: {e}   (vel.anim={info.speed * info.speedMultiplier:0.00} y={jogador.transform.position.y:0.00} velY={jogador.Velocity.y:0.0})");
            ultimoEstado = e;
        }

        string EstadoAtual()
        {
            var info = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            return nomes.TryGetValue(info.shortNameHash, out var n) ? n : "?";
        }
#endif
    }
}
