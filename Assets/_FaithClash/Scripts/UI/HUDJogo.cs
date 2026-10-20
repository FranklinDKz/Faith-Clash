using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.UI
{
    /// <summary>
    /// HUD principal, montado por código a partir das imagens em Resources/HUD.
    /// Layout de referência 1920x1080 (as posições abaixo são em pixels a partir do canto superior esquerdo).
    /// </summary>
    public class HUDJogo : MonoBehaviour
    {
        [SerializeField] Transform jogador;
        [SerializeField] string regiao = "Campos de Pedra Cinzenta";
        [SerializeField] string missao = "Juramento Quebrado";
        [SerializeField] string alvoMissao = "Prelado Corrompido";
        [SerializeField] float raioMinimapa = 27f;
        [SerializeField] float horaInicial = 14.5833f;   // 14:35
        [SerializeField] float minutosPorSegundo = 1f;

        static readonly Color Ouro = new Color(0.93f, 0.84f, 0.62f);
        static readonly Color Creme = new Color(0.94f, 0.91f, 0.84f);
        static readonly Color Cinza = new Color(0.78f, 0.76f, 0.72f);

        StatusJogador status;
        MeleeCombat combate;
        EstilosElementais estilos;
        TravaAlvo trava;
        Health vidaJogador;
        Font fonte;
        RectTransform raiz;

        Image hpFill, feFill, alvoFill, feBaseFill;
        Text hpTxt, feTxt, nivelTxt, tituloTxt, chamaTxt, escudoTxt, alvoNome, alvoNivel, alvoSub, relogio,
             missaoObj, feBaseTxt, comboNum, avisoTxt, statsTxt, dicaTitulo, dicaCorpo, dicaRodape;
        GameObject alvoGrupo, comboGrupo, dicaPainel;
        Image dicaIcone;
        readonly RectTransform[] orbes = new RectTransform[4];
        readonly Image[] orbesBrilho = new Image[4];
        readonly Image[] pips = new Image[3];
        readonly Text[] itensQtd = new Text[4];
        Sprite pipCheio, pipVazio;
        RectTransform seta, mapaArea, mira;
        readonly List<RectTransform> marcadores = new List<RectTransform>();
        Camera camMapa;
        float avisoAte;
        int modoDica;          // 0 = dica do estilo, 1 = dica de combate, 2 = escondida
        bool missaoCumprida;

        static readonly string[] NomesElementos = { "FOGO", "ÁGUA", "TERRA", "AR" };
        static readonly string[] IconesElementos = { "elemento_fogo", "elemento_agua", "elemento_terra", "elemento_ar" };
        static readonly string[][] Dicas =
        {
            new[] { "DICA DE COMBO — ESTILO FOGO", "Aperte [1] para ativar o Estilo Fogo.\n3x Mouse Esquerdo: <color=#FF9A3C>Corte Ígneo.</color>\n4x Mouse Esquerdo: <color=#FF9A3C>Chama Explosiva.</color>", "Perfeito para dano rápido e agressivo." },
            new[] { "DICA DE COMBO — ESTILO ÁGUA", "Aperte [2] para ativar o Estilo Água.\n3x Mouse Esquerdo: Rajada de Água.\n4x Mouse Esquerdo: Prisão de Água.", "Controle o inimigo com pressão e aprisionamento." },
            new[] { "DICA DE COMBO — ESTILO TERRA", "Aperte [3] para ativar o Estilo Terra.\n3x Mouse Esquerdo: Lança de Pedra.\nSegure Mouse Esquerdo: Muralha de Terra.", "Excelente para controle de área e defesa." },
            new[] { "DICA DE COMBO — ESTILO AR", "Aperte [4] para ativar o Estilo Ar.\nSegure <color=#F2C14E>Mouse Esquerdo:</color> uma força repulsiva é liberada do corpo do usuário em todas as direções, criando um globo de vento que empurra tudo ao redor.", "Ideal para afastar inimigos e abrir espaço." },
        };

        void Awake()
        {
            if (jogador == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) jogador = pc.transform;
            }
            if (jogador != null)
            {
                status = jogador.GetComponent<StatusJogador>();
                combate = jogador.GetComponent<MeleeCombat>();
                estilos = jogador.GetComponent<EstilosElementais>();
                trava = jogador.GetComponent<TravaAlvo>();
                vidaJogador = jogador.GetComponent<Health>();
            }
            fonte = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" }, 32);
            Montar();
            CriarCameraMinimapa();
        }

        void OnEnable()
        {
            if (estilos != null) { estilos.ElementoMudou += AtualizarElemento; estilos.TecnicaUsada += MostrarTecnica; }
            if (status != null) status.Aviso += MostrarAviso;
        }

        void OnDisable()
        {
            if (estilos != null) { estilos.ElementoMudou -= AtualizarElemento; estilos.TecnicaUsada -= MostrarTecnica; }
            if (status != null) status.Aviso -= MostrarAviso;
        }

        // ================================================================== montagem
        void Montar()
        {
            var canvasGo = new GameObject("Canvas HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            raiz = new GameObject("Area 1920x1080", typeof(RectTransform)).GetComponent<RectTransform>();
            raiz.SetParent(canvasGo.transform, false);
            raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.sizeDelta = new Vector2(1920, 1080);

            MontarJogador();
            MontarAlvo();
            MontarTopoDireito();
            MontarDica();
            MontarElementos();
            MontarArma();
            MontarItens();
            MontarCombo();

            mira = Img("mira_alvo", Vector2.zero, new Vector2(70, 70)).rectTransform;
            Centralizar(mira);
            mira.gameObject.SetActive(false);

            avisoTxt = Txt("", 34, Creme, TextAnchor.MiddleCenter, new Vector2(960, 620), new Vector2(900, 60));
            avisoTxt.fontStyle = FontStyle.Bold;
        }

        void MontarJogador()
        {
            Img("retrato", new Vector2(98, 100), new Vector2(112, 112));
            Img("moldura_circular", new Vector2(98, 100), new Vector2(172, 172));
            nivelTxt = Txt("Nv. 25", 22, Creme, TextAnchor.MiddleLeft, new Vector2(236, 62), new Vector2(110, 30));
            tituloTxt = Txt("Portador da Fé", 20, Creme, TextAnchor.MiddleLeft, new Vector2(425, 62), new Vector2(240, 30));
            hpFill = Barra(new Vector2(365, 92), new Vector2(400, 26), new Color(0.72f, 0.1f, 0.09f), out hpTxt);
            feFill = Barra(new Vector2(365, 122), new Vector2(400, 26), new Color(0.12f, 0.42f, 0.86f), out feTxt);
            Img("icone_chama", new Vector2(192, 158), new Vector2(24, 24));
            chamaTxt = Txt("3", 18, Creme, TextAnchor.MiddleLeft, new Vector2(226, 158), new Vector2(40, 24));
            Img("icone_escudo", new Vector2(262, 158), new Vector2(24, 24));
            escudoTxt = Txt("2", 18, Creme, TextAnchor.MiddleLeft, new Vector2(296, 158), new Vector2(40, 24));
        }

        void MontarAlvo()
        {
            alvoGrupo = new GameObject("Alvo", typeof(RectTransform));
            alvoGrupo.transform.SetParent(raiz, false);
            Esticar(alvoGrupo.GetComponent<RectTransform>());
            var g = alvoGrupo.transform;
            alvoNome = Txt("Prelado Corrompido", 26, Creme, TextAnchor.MiddleCenter, new Vector2(960, 46), new Vector2(600, 36), g);
            alvoNivel = Txt("Nv. 26", 18, Creme, TextAnchor.MiddleRight, new Vector2(1225, 50), new Vector2(120, 30), g);
            alvoFill = Barra(new Vector2(960, 74), new Vector2(660, 22), new Color(0.62f, 0.08f, 0.08f), out _, g, false);
            Img("icone_objetivo", new Vector2(906, 101), new Vector2(22, 22), g);
            alvoSub = Txt("Fé Maculada", 18, Cinza, TextAnchor.MiddleLeft, new Vector2(990, 101), new Vector2(160, 26), g);
        }

        void MontarTopoDireito()
        {
            relogio = Txt("02:35 PM", 22, Creme, TextAnchor.MiddleRight, new Vector2(1780, 32), new Vector2(160, 30));
            Img("icone_sol", new Vector2(1884, 32), new Vector2(30, 30));

            // minimapa circular
            var mascara = Img("circulo", new Vector2(1722, 184), new Vector2(232, 232));
            mascara.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            mapaArea = mascara.rectTransform;
            var mapa = new GameObject("Mapa", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            mapa.transform.SetParent(mascara.transform, false);
            Esticar(mapa.rectTransform);
            mapa.color = new Color(0.75f, 0.75f, 0.75f);
            mapa.name = "Mapa";
            var sombra = Img("brilho", Vector2.zero, new Vector2(232, 232), mascara.transform);
            sombra.color = new Color(0, 0, 0, 0f);
            seta = Img("seta_jogador", Vector2.zero, new Vector2(26, 26), mascara.transform).rectTransform;
            Centralizar(seta);
            Img("moldura_circular", new Vector2(1722, 184), new Vector2(300, 300));
            Txt("N", 22, Creme, TextAnchor.MiddleCenter, new Vector2(1722, 56), new Vector2(40, 30)).fontStyle = FontStyle.Bold;
            Img("tecla", new Vector2(1852, 92), new Vector2(28, 28));
            Txt("M", 16, Creme, TextAnchor.MiddleCenter, new Vector2(1852, 92), new Vector2(28, 28));

            Txt(regiao, 20, Creme, TextAnchor.MiddleCenter, new Vector2(1722, 330), new Vector2(360, 30));
            Img("linha_ornamental", new Vector2(1735, 383), new Vector2(330, 14));
            Img("icone_objetivo", new Vector2(1592, 410), new Vector2(28, 28));
            Txt(missao, 25, Ouro, TextAnchor.MiddleLeft, new Vector2(1735, 410), new Vector2(260, 34));
            Img("icone_objetivo", new Vector2(1612, 457), new Vector2(16, 16));
            missaoObj = Txt("Derrote o " + alvoMissao, 17, Creme, TextAnchor.MiddleLeft, new Vector2(1755, 457), new Vector2(260, 26));
            statsTxt = Txt("", 15, Cinza, TextAnchor.MiddleLeft, new Vector2(1755, 490), new Vector2(260, 24));
        }

        void MontarDica()
        {
            dicaPainel = new GameObject("Dica de Combo", typeof(RectTransform));
            dicaPainel.transform.SetParent(raiz, false);
            Esticar(dicaPainel.GetComponent<RectTransform>());
            var g = dicaPainel.transform;
            var p = Img("painel", new Vector2(290, 565), new Vector2(510, 230), g);
            p.type = Image.Type.Sliced;
            Img("moldura_orbe", new Vector2(290, 445), new Vector2(110, 110), g);
            dicaIcone = Img("elemento_fogo", new Vector2(290, 445), new Vector2(62, 62), g);
            dicaTitulo = Txt("", 22, Ouro, TextAnchor.MiddleLeft, new Vector2(300, 507), new Vector2(460, 32), g);
            dicaCorpo = Txt("", 19, Creme, TextAnchor.UpperLeft, new Vector2(305, 580), new Vector2(450, 100), g);
            dicaCorpo.lineSpacing = 1.15f;
            dicaRodape = Txt("", 16, Cinza, TextAnchor.MiddleLeft, new Vector2(305, 650), new Vector2(450, 26), g);
        }

        void MontarElementos()
        {
            float[] xs = { 752, 897, 1034, 1172 };
            for (int i = 0; i < 4; i++)
            {
                orbesBrilho[i] = Img("brilho", new Vector2(xs[i], 855), new Vector2(200, 200));
                orbesBrilho[i].color = new Color(EstilosElementais.Cores[i].r, EstilosElementais.Cores[i].g, EstilosElementais.Cores[i].b, 0f);
                var orbe = Img(IconesElementos[i], new Vector2(xs[i], 855), new Vector2(98, 98)).rectTransform;
                Img("moldura_orbe", Vector2.zero, new Vector2(150, 150), orbe).rectTransform.anchoredPosition = Vector2.zero;
                Centralizar(orbe.GetChild(0) as RectTransform);
                orbes[i] = orbe;
                Img("tecla", new Vector2(xs[i], 913), new Vector2(28, 28));
                Txt((i + 1).ToString(), 17, Creme, TextAnchor.MiddleCenter, new Vector2(xs[i], 913), new Vector2(28, 28));
                Txt(NomesElementos[i], 17, Creme, TextAnchor.MiddleCenter, new Vector2(xs[i], 942), new Vector2(120, 24));
            }
            Img("linha_ornamental", new Vector2(962, 991), new Vector2(560, 18));
            feBaseFill = Barra(new Vector2(962, 972), new Vector2(540, 20), new Color(0.27f, 0.45f, 0.9f), out _, null, false);
            Img("pip_cheio", new Vector2(962, 968), new Vector2(40, 40)).color = Creme;
            feBaseTxt = Txt("FÉ 62%", 20, Creme, TextAnchor.MiddleCenter, new Vector2(962, 1016), new Vector2(200, 28));
        }

        void MontarArma()
        {
            Img("arma_espada", new Vector2(126, 884), new Vector2(132, 132));
            Img("anel_simples", new Vector2(126, 884), new Vector2(176, 176));
            Txt("Espada Espiritual", 18, Creme, TextAnchor.MiddleLeft, new Vector2(300, 948), new Vector2(200, 26));
            pipCheio = Sprite("pip_cheio");
            pipVazio = Sprite("pip_vazio");
            for (int i = 0; i < 3; i++) pips[i] = Img("pip_vazio", new Vector2(220 + i * 25, 982), new Vector2(20, 20));
            Img("tecla", new Vector2(124, 994), new Vector2(30, 30));
            Txt("R", 17, Creme, TextAnchor.MiddleCenter, new Vector2(124, 994), new Vector2(30, 30));
        }

        void MontarItens()
        {
            float[] xs = { 1525, 1628, 1729, 1828 };
            for (int i = 0; i < 4; i++)
            {
                var item = status != null ? status.Itens[i] : null;
                Img("slot", new Vector2(xs[i], 932), new Vector2(76, 76));
                Img(item != null ? item.icone : "item_pocao", new Vector2(xs[i], 932), new Vector2(64, 64));
                itensQtd[i] = Txt("0", 18, Creme, TextAnchor.LowerRight, new Vector2(xs[i] + 10, 944), new Vector2(50, 30));
                Img("tecla", new Vector2(xs[i], 994), new Vector2(30, 30));
                Txt(item != null ? item.tecla : "?", 17, Creme, TextAnchor.MiddleCenter, new Vector2(xs[i], 994), new Vector2(30, 30));
            }
        }

        void MontarCombo()
        {
            comboGrupo = new GameObject("Combo", typeof(RectTransform));
            comboGrupo.transform.SetParent(raiz, false);
            Esticar(comboGrupo.GetComponent<RectTransform>());
            var g = comboGrupo.transform;
            comboNum = Txt("0", 76, Creme, TextAnchor.MiddleCenter, new Vector2(126, 392), new Vector2(220, 90), g);
            comboNum.fontStyle = FontStyle.Bold;
            Txt("Combo", 26, Creme, TextAnchor.MiddleCenter, new Vector2(126, 446), new Vector2(200, 34), g);
            Img("linha_ornamental", new Vector2(150, 470), new Vector2(240, 12), g);
        }

        // ================================================================== atualização
        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.tabKey.wasPressedThisFrame) { modoDica = (modoDica + 1) % 3; AtualizarDica(); }

            if (vidaJogador != null)
            {
                hpFill.fillAmount = vidaJogador.Current / vidaJogador.Max;
                hpTxt.text = $"{vidaJogador.Current:0} / {vidaJogador.Max:0}";
            }
            if (status != null)
            {
                feFill.fillAmount = status.Fe / status.FeMaxima;
                feTxt.text = $"{status.Fe:0} / {status.FeMaxima:0}";
                feBaseFill.fillAmount = status.Fe / status.FeMaxima;
                feBaseTxt.text = $"FÉ {Mathf.RoundToInt(100f * status.Fe / status.FeMaxima)}%";
                nivelTxt.text = "Nv. " + status.Nivel;
                tituloTxt.text = status.Titulo;
                chamaTxt.text = status.Cargas.ToString();
                escudoTxt.text = status.Escudos.ToString();
                for (int i = 0; i < 3; i++) pips[i].sprite = i < status.Cargas ? pipCheio : pipVazio;
                for (int i = 0; i < 4; i++) itensQtd[i].text = status.Itens[i].quantidade.ToString();
            }

            int combo = combate != null ? combate.ComboCount : 0;
            comboGrupo.SetActive(combo >= 2);
            comboNum.text = combo.ToString();

            AtualizarAlvo();
            AtualizarMira();
            AtualizarRelogio();
            AtualizarMinimapa();
            AtualizarMissao();

            if (TrainingManager.Instance != null)
                statsTxt.text = $"Treino — Dano: {TrainingManager.Instance.TotalDamage:0}   DPS: {TrainingManager.Instance.Dps:0.0}";

            if (Time.time > avisoAte) avisoTxt.text = "";
            else
            {
                var c = avisoTxt.color;
                c.a = Mathf.Clamp01((avisoAte - Time.time) / 0.4f);
                avisoTxt.color = c;
            }

            for (int i = 0; i < 4; i++)
            {
                bool sel = estilos != null && (int)estilos.Atual == i;
                float alvoEsc = sel ? 1.12f : 1f;
                orbes[i].localScale = Vector3.Lerp(orbes[i].localScale, Vector3.one * alvoEsc, 10f * Time.deltaTime);
                var cor = orbesBrilho[i].color;
                cor.a = Mathf.Lerp(cor.a, sel ? 0.75f + Mathf.Sin(Time.time * 4f) * 0.1f : 0f, 8f * Time.deltaTime);
                orbesBrilho[i].color = cor;
            }
        }

        void Start()
        {
            AtualizarElemento(estilos != null ? estilos.Atual : Elemento.Fogo);
        }

        void AtualizarElemento(Elemento e)
        {
            if (dicaIcone == null) return;
            dicaIcone.sprite = Sprite(IconesElementos[(int)e]);
            if (modoDica == 2) modoDica = 0;
            AtualizarDica();
        }

        void AtualizarDica()
        {
            dicaPainel.SetActive(modoDica != 2);
            if (modoDica == 0)
            {
                int i = estilos != null ? (int)estilos.Atual : 0;
                dicaTitulo.text = Dicas[i][0];
                dicaCorpo.text = Dicas[i][1];
                dicaRodape.text = Dicas[i][2];
                dicaIcone.sprite = Sprite(IconesElementos[i]);
            }
            else if (modoDica == 1)
            {
                dicaTitulo.text = "Dica de Combate";
                dicaCorpo.text = "Combine elementos para causar\nExplosão Elemental e aumentar seu dano.\nOrdem recomendada:\n<color=#5AA9FF>ÁGUA</color> → <color=#FF5A3C>FOGO</color> → <color=#E0B060>TERRA</color> → <color=#B98CFF>AR</color>";
                dicaRodape.text = "[Tab] Fechar";
                dicaIcone.sprite = Sprite("elemento_ar");
            }
        }

        void AtualizarAlvo()
        {
            TrainingDummy alvo = trava != null && trava.Travado ? trava.Alvo.GetComponent<TrainingDummy>() : null;
            float melhor = 30f;
            if (alvo == null && jogador != null)
            {
                foreach (var d in TrainingDummy.Todos)
                {
                    float dist = Vector3.Distance(d.transform.position, jogador.position);
                    if (dist < melhor) { melhor = dist; alvo = d; }
                }
            }
            alvoGrupo.SetActive(alvo != null);
            if (alvo == null) return;
            alvoNome.text = alvo.Nome;
            alvoNivel.text = "Nv. " + alvo.Nivel;
            alvoSub.text = alvo.Subtitulo;
            alvoFill.fillAmount = Mathf.Lerp(alvoFill.fillAmount, alvo.Vida.Current / alvo.Vida.Max, 12f * Time.deltaTime);
        }

        void AtualizarMira()
        {
            bool ativo = trava != null && trava.Travado && Camera.main != null;
            if (ativo)
            {
                Vector3 tela = Camera.main.WorldToScreenPoint(trava.PontoFoco);
                ativo = tela.z > 0f;
                if (ativo && RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, tela, null, out Vector2 local))
                {
                    mira.anchoredPosition = local;
                    mira.localEulerAngles = new Vector3(0, 0, Time.time * 40f);
                }
            }
            mira.gameObject.SetActive(ativo);
        }

        void AtualizarRelogio()
        {
            float horas = (horaInicial + Time.time * minutosPorSegundo / 60f) % 24f;
            int h = Mathf.FloorToInt(horas);
            int m = Mathf.FloorToInt((horas - h) * 60f);
            string ampm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            relogio.text = $"{h12:00}:{m:00} {ampm}";
        }

        void AtualizarMissao()
        {
            if (missaoCumprida) return;
            foreach (var d in TrainingDummy.Todos)
            {
                if (d.Nome == alvoMissao && d.Vida.IsDead)
                {
                    missaoCumprida = true;
                    missaoObj.text = "<color=#9BE58A>✔</color> " + alvoMissao + " derrotado";
                    MostrarAviso("Missão concluída: " + missao);
                }
            }
        }

        // ================================================================== minimapa
        void CriarCameraMinimapa()
        {
            var rt = new RenderTexture(256, 256, 16) { name = "Minimapa" };
            var go = new GameObject("Câmera do Minimapa");
            go.transform.SetParent(transform, false);
            camMapa = go.AddComponent<Camera>();
            camMapa.orthographic = true;
            camMapa.orthographicSize = raioMinimapa;
            camMapa.targetTexture = rt;
            camMapa.clearFlags = CameraClearFlags.SolidColor;
            camMapa.backgroundColor = new Color(0.12f, 0.13f, 0.12f);
            camMapa.farClipPlane = 300f;
            camMapa.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapaArea.GetComponentInChildren<RawImage>().texture = rt;
        }

        void AtualizarMinimapa()
        {
            if (jogador == null || camMapa == null) return;
            camMapa.transform.position = jogador.position + Vector3.up * 120f;
            seta.localEulerAngles = new Vector3(0, 0, -jogador.eulerAngles.y);

            float raioPx = mapaArea.sizeDelta.x * 0.5f;
            int n = 0;
            foreach (var d in TrainingDummy.Todos)
            {
                Vector3 rel = d.transform.position - jogador.position;
                var p = new Vector2(rel.x, rel.z) / raioMinimapa * raioPx;
                if (p.magnitude > raioPx - 10f) continue;
                if (n >= marcadores.Count)
                {
                    var m = Img("marcador_inimigo", Vector2.zero, new Vector2(24, 24), mapaArea).rectTransform;
                    Centralizar(m);
                    marcadores.Add(m);
                }
                marcadores[n].gameObject.SetActive(true);
                marcadores[n].anchoredPosition = p;
                n++;
            }
            for (int i = n; i < marcadores.Count; i++) marcadores[i].gameObject.SetActive(false);
            seta.SetAsLastSibling();
        }

        // ================================================================== avisos
        void MostrarTecnica(string nome, Color cor)
        {
            avisoTxt.text = nome + "!";
            avisoTxt.color = Color.Lerp(cor, Color.white, 0.25f);
            avisoAte = Time.time + 1.4f;
        }

        void MostrarAviso(string msg)
        {
            avisoTxt.text = msg;
            avisoTxt.color = Creme;
            avisoAte = Time.time + 1.6f;
        }

        // ================================================================== auxiliares de UI
        static Sprite Sprite(string nome) => Resources.Load<Sprite>("HUD/" + nome);

        Image Img(string sprite, Vector2 pos, Vector2 tam, Transform pai = null)
        {
            var go = new GameObject(sprite, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pai != null ? pai : raiz, false);
            var img = go.GetComponent<Image>();
            img.sprite = Sprite(sprite);
            img.raycastTarget = false;
            img.preserveAspect = false;
            Posicionar(img.rectTransform, pos, tam, pai);
            return img;
        }

        Text Txt(string texto, int tamanho, Color cor, TextAnchor alinhamento, Vector2 pos, Vector2 tam, Transform pai = null)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(pai != null ? pai : raiz, false);
            var t = go.GetComponent<Text>();
            t.font = fonte;
            t.text = texto;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = alinhamento;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var s = go.GetComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.85f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
            Posicionar(t.rectTransform, pos, tam, pai);
            return t;
        }

        Image Barra(Vector2 pos, Vector2 tam, Color cor, out Text texto, Transform pai = null, bool comTexto = true)
        {
            var fundo = Img("barra_fundo", pos, tam, pai);
            fundo.type = Image.Type.Sliced;
            var fill = Img("barra_preenchimento", Vector2.zero, new Vector2(tam.x - 34, tam.y - 12), fundo.transform);
            fill.rectTransform.anchoredPosition = new Vector2(-3, 0);
            Centralizar(fill.rectTransform);
            fill.rectTransform.anchoredPosition = new Vector2(-2, 0);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = cor;
            texto = null;
            if (comTexto)
            {
                texto = Txt("", 17, Creme, TextAnchor.MiddleRight, Vector2.zero, new Vector2(tam.x - 50, tam.y), fundo.transform);
                Centralizar(texto.rectTransform);
                texto.rectTransform.anchoredPosition = new Vector2(-10, 0);
            }
            return fill;
        }

        void Posicionar(RectTransform rt, Vector2 pos, Vector2 tam, Transform pai)
        {
            rt.sizeDelta = tam;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var rp = pai != null ? pai.GetComponent<RectTransform>() : null;
            bool grupoTelaCheia = rp != null && rp.anchorMin == Vector2.zero && rp.anchorMax == Vector2.one;
            if (pai == null || pai == raiz || grupoTelaCheia)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(pos.x, -pos.y);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
            }
        }

        static void Centralizar(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
