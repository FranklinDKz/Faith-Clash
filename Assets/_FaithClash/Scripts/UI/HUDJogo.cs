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
    /// HUD compacto e discreto: vida/Fé finas no canto, barra do inimigo no topo, poderes equipados e itens em fichas pequenas embaixo,
    /// minimapa e missão só quando a fase tem mapa. Poderes, itens e mapa só aparecem se existirem (e se a fase permitir).
    /// </summary>
    public class HUDJogo : MonoBehaviour
    {
        [SerializeField] Transform jogador;
        [SerializeField] string regiao = "Campos de Pedra Cinzenta";
        [SerializeField] string missao = "Juramento Quebrado";
        [SerializeField] string alvoMissao = "Prelado Corrompido";
        [SerializeField] float raioMinimapa = 27f;
        [SerializeField] float horaInicial = 14.5833f;
        [SerializeField] float minutosPorSegundo = 1f;

        const int NElem = 10;
        static readonly string[] Glifos = { "火", "水", "土", "風", "雷", "砂", "気", "眼", "域", "輪" };
        static readonly string[] IconesElementos = { "elemento_fogo", "elemento_agua", "elemento_terra", "elemento_ar" };
        static readonly Sprite[] discos = new Sprite[NElem];
        static Font fonteJpEstatica;
        public static Font FonteJapones => fonteJpEstatica != null ? fonteJpEstatica : (fonteJpEstatica = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic", "Arial Unicode MS" }, 32));

        public static Sprite IconeElemento(int i) => IconeDe(i);
        public static string Glifo(int i) => Glifos[i];

        static Sprite IconeDe(int i)
        {
            if (i < IconesElementos.Length) return Resources.Load<Sprite>("HUD/" + IconesElementos[i]);
            if (discos[i] != null) return discos[i];
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var cor = EstilosElementais.Cores[i];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f + 0.5f) / (n / 2f), dy = (y - n / 2f + 0.5f) / (n / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - r) * 18f);
                    float luz = Mathf.Clamp01(1.1f - r * 0.9f + (dy * 0.25f));
                    var c = Color.Lerp(cor * 0.35f, Color.Lerp(cor, Color.white, 0.35f), luz);
                    if (r > 0.86f) c = Color.Lerp(c, cor * 0.25f, (r - 0.86f) * 6f);
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return discos[i] = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite Spr(string nome) => Resources.Load<Sprite>("HUD/" + nome);

        // referências do jogo
        StatusJogador status;
        MeleeCombat combate;
        EstilosElementais estilos;
        TravaAlvo trava;
        ModoCombate modoCombate;
        Health vidaJogador;

        // interface
        RectTransform raiz;
        Image hpFill, feFill, alvoFill;
        Text hpTxt, feTxt, nivelTxt, cargasTxt, escudosTxt, alvoNome, relogio, regiaoTxt, missaoTxt, missaoObj, statsTxt, avisoTxt, modoTxt, comboNum, tecnicasTxt, dicaTxt;
        GameObject alvoGrupo, comboGrupo, grupoFe, grupoCargas, grupoMapa, grupoMissao, grupoItens, grupoSlots, dicaPainel;
        RectTransform mira, seta, mapaArea;
        readonly Image[] slotImg = new Image[4], slotBorda = new Image[4], slotFundo = new Image[4];
        readonly Text[] slotTecla = new Text[4], slotGlifo = new Text[4];
        readonly GameObject[] slotObj = new GameObject[4];
        Image semPoderImg; Text semPoderTxt;
        readonly GameObject[] itemObj = new GameObject[4];
        readonly Text[] itemQtd = new Text[4];
        readonly RectTransform[] itemRt = new RectTransform[4];
        readonly List<RectTransform> marcadores = new List<RectTransform>();
        Camera camMapa;
        float avisoAte, dicaAte = 14f;
        bool semMissaoInimiga, missaoCumprida, dicaOculta;

        static readonly Color Ouro = new Color(1f, 0.82f, 0.4f);
        static readonly Color Painel = new Color(0.04f, 0.05f, 0.09f, 0.62f);

        void Awake()
        {
            if (jogador == null) { var pc = FindAnyObjectByType<PlayerController>(); if (pc != null) jogador = pc.transform; }
            if (jogador != null)
            {
                status = jogador.GetComponent<StatusJogador>();
                combate = jogador.GetComponent<MeleeCombat>();
                estilos = jogador.GetComponent<EstilosElementais>();
                trava = jogador.GetComponent<TravaAlvo>();
                modoCombate = jogador.GetComponent<ModoCombate>();
                vidaJogador = jogador.GetComponent<Health>();
            }
            Montar();
            CriarCameraMinimapa();
        }

        public void DefinirMissao(string novaRegiao, string titulo, string objetivo)
        {
            semMissaoInimiga = true;
            if (regiaoTxt != null) regiaoTxt.text = novaRegiao;
            if (missaoTxt != null) missaoTxt.text = titulo;
            if (missaoObj != null) missaoObj.text = objetivo;
            if (statsTxt != null) statsTxt.text = "";
        }

        public void AtualizarObjetivo(string objetivo) { if (missaoObj != null) missaoObj.text = objetivo; }

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

        void Start() { AtualizarElemento(estilos != null ? estilos.Atual : Elemento.Fogo); PerfilJogo.Mudou += AoMudarPerfil; }
        void OnDestroy() { PerfilJogo.Mudou -= AoMudarPerfil; }
        void AoMudarPerfil() { if (estilos != null) AtualizarElemento(estilos.Atual); }

        // ================================================================== montagem
        RectTransform No(Transform pai, string nome, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tam)
        {
            var rt = UIKit.Retangulo(pai, nome);
            rt.anchorMin = rt.anchorMax = ancora; rt.pivot = pivo; rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return rt;
        }

        Image Fundo(Transform pai, Color cor, bool arredondado = true) => UIKit.Painel(pai, "fundo", cor, arredondado);

        Text Tx(Transform pai, string texto, int tam, Color cor, TextAnchor al, FontStyle est = FontStyle.Normal, bool sombra = true)
        {
            var t = UIKit.Rotulo(pai, texto, tam, cor, al, est);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (sombra) { var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.9f); s.effectDistance = new Vector2(1.2f, -1.2f); }
            return t;
        }

        Image BarraFina(Transform pai, Vector2 pos, Vector2 tam, Color cor, out Text txt)
        {
            var fundo = Fundo(pai, new Color(0f, 0f, 0f, 0.55f));
            var rt = fundo.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = pos; rt.sizeDelta = tam;
            var fill = new GameObject("fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(rt, false);
            fill.sprite = UIKit.Branco; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = cor; fill.raycastTarget = false;
            UIKit.Esticar(fill.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
            txt = Tx(rt, "", 11, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold, false);
            UIKit.Esticar(txt.rectTransform, 0, 0, 6, 0);
            return fill;
        }

        void Montar()
        {
            var go = new GameObject("Canvas HUD", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            raiz = UIKit.Retangulo(go.transform, "Área");
            UIKit.Esticar(raiz);

            MontarJogador();
            MontarAlvo();
            MontarTopoDireito();
            MontarPoderes();
            MontarItens();
            MontarCanto();

            mira = new GameObject("mira", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            mira.SetParent(raiz, false);
            mira.anchorMin = mira.anchorMax = new Vector2(0.5f, 0.5f); mira.sizeDelta = new Vector2(22, 22);
            var mi = mira.GetComponent<Image>(); mi.sprite = Spr("mira_alvo"); mi.raycastTarget = false;
            mira.gameObject.SetActive(false);

            avisoTxt = Tx(raiz, "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var art = avisoTxt.rectTransform; art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.28f); art.sizeDelta = new Vector2(1000, 50);
        }

        void MontarJogador()
        {
            var g = No(raiz, "Jogador", new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(380, 92));
            var fundo = Fundo(g, Painel); UIKit.Esticar(fundo.rectTransform);
            var ret = new GameObject("retrato", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ret.transform.SetParent(g, false); ret.sprite = Spr("retrato"); ret.raycastTarget = false;
            var rr = ret.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0, 0.5f); rr.pivot = new Vector2(0, 0.5f); rr.anchoredPosition = new Vector2(12, 0); rr.sizeDelta = new Vector2(66, 66);
            var anel = new GameObject("anel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            anel.transform.SetParent(g, false); anel.sprite = Spr("moldura_circular"); anel.raycastTarget = false;
            var ar = anel.rectTransform; ar.anchorMin = ar.anchorMax = new Vector2(0, 0.5f); ar.pivot = new Vector2(0, 0.5f); ar.anchoredPosition = new Vector2(2, 0); ar.sizeDelta = new Vector2(86, 86);
            nivelTxt = Tx(g, "Nv. 25", 15, new Color(1, 1, 1, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var nr = nivelTxt.rectTransform; nr.anchorMin = nr.anchorMax = new Vector2(0, 1); nr.pivot = new Vector2(0, 1); nr.anchoredPosition = new Vector2(100, -8); nr.sizeDelta = new Vector2(260, 20);
            hpFill = BarraFina(g, new Vector2(100, -32), new Vector2(264, 14), new Color(0.88f, 0.16f, 0.2f), out hpTxt);
            grupoFe = new GameObject("Fé", typeof(RectTransform)); grupoFe.transform.SetParent(g, false);
            UIKit.Esticar((RectTransform)grupoFe.transform);
            feFill = BarraFina(grupoFe.transform, new Vector2(100, -50), new Vector2(264, 10), new Color(0.25f, 0.55f, 1f), out feTxt);
            grupoCargas = new GameObject("Cargas", typeof(RectTransform)); grupoCargas.transform.SetParent(g, false);
            UIKit.Esticar((RectTransform)grupoCargas.transform);
            cargasTxt = Tx(grupoCargas.transform, "◆ 3", 13, Ouro, TextAnchor.MiddleLeft, FontStyle.Bold);
            var cr = cargasTxt.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0, 1); cr.pivot = new Vector2(0, 1); cr.anchoredPosition = new Vector2(100, -66); cr.sizeDelta = new Vector2(60, 18);
            escudosTxt = Tx(g, "⛨ 2", 13, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var er = escudosTxt.rectTransform; er.anchorMin = er.anchorMax = new Vector2(0, 1); er.pivot = new Vector2(0, 1); er.anchoredPosition = new Vector2(160, -66); er.sizeDelta = new Vector2(60, 18);
        }

        void MontarAlvo()
        {
            alvoGrupo = new GameObject("Alvo", typeof(RectTransform));
            var g = (RectTransform)alvoGrupo.transform; g.SetParent(raiz, false);
            g.anchorMin = g.anchorMax = new Vector2(0.5f, 1); g.pivot = new Vector2(0.5f, 1); g.anchoredPosition = new Vector2(0, -92); g.sizeDelta = new Vector2(420, 40);
            alvoNome = Tx(g, "", 16, new Color(1, 1, 1, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Ancorar(alvoNome.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -22), new Vector2(0, 0));
            var fundo = Fundo(g, new Color(0, 0, 0, 0.55f)); var fr = fundo.rectTransform;
            UIKit.Ancorar(fr, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 14));
            alvoFill = new GameObject("fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            alvoFill.transform.SetParent(fr, false); alvoFill.sprite = UIKit.Branco; alvoFill.type = Image.Type.Filled; alvoFill.fillMethod = Image.FillMethod.Horizontal;
            alvoFill.color = new Color(0.9f, 0.25f, 0.2f); alvoFill.raycastTarget = false; UIKit.Esticar(alvoFill.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
        }

        void MontarTopoDireito()
        {
            relogio = Tx(raiz, "", 14, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight);
            var rr = relogio.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(1, 1); rr.pivot = new Vector2(1, 1); rr.anchoredPosition = new Vector2(-28, -14); rr.sizeDelta = new Vector2(140, 20);

            grupoMapa = new GameObject("Minimapa", typeof(RectTransform));
            var g = (RectTransform)grupoMapa.transform; g.SetParent(raiz, false);
            g.anchorMin = g.anchorMax = new Vector2(1, 1); g.pivot = new Vector2(1, 1); g.anchoredPosition = new Vector2(-28, -40); g.sizeDelta = new Vector2(150, 150);
            var mascara = new GameObject("máscara", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<Image>();
            mascara.transform.SetParent(g, false); mascara.sprite = UIKit.Circulo; mascara.raycastTarget = false; mascara.GetComponent<Mask>().showMaskGraphic = false;
            UIKit.Esticar(mascara.rectTransform);
            mapaArea = mascara.rectTransform;
            var mapa = new GameObject("Mapa", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            mapa.transform.SetParent(mascara.transform, false); UIKit.Esticar(mapa.rectTransform); mapa.color = new Color(0.8f, 0.8f, 0.8f); mapa.raycastTarget = false;
            var anel = new GameObject("anel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            anel.transform.SetParent(g, false); anel.sprite = Spr("moldura_circular"); anel.raycastTarget = false; UIKit.Esticar(anel.rectTransform, -10, -10, -10, -10);
            seta = new GameObject("seta", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            seta.SetParent(mascara.transform, false); seta.anchorMin = seta.anchorMax = new Vector2(0.5f, 0.5f); seta.sizeDelta = new Vector2(20, 20);
            var si = seta.GetComponent<Image>(); si.sprite = Spr("seta_jogador"); si.raycastTarget = false;

            grupoMissao = new GameObject("Missão", typeof(RectTransform));
            var m = (RectTransform)grupoMissao.transform; m.SetParent(raiz, false);
            m.anchorMin = m.anchorMax = new Vector2(1, 1); m.pivot = new Vector2(1, 1); m.anchoredPosition = new Vector2(-28, -210); m.sizeDelta = new Vector2(300, 90);
            regiaoTxt = Tx(m, regiao, 12, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleRight);
            UIKit.Ancorar(regiaoTxt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -18), Vector2.zero);
            missaoTxt = Tx(m, missao, 17, Ouro, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Ancorar(missaoTxt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -42), new Vector2(0, -18));
            missaoObj = Tx(m, "Derrote o " + alvoMissao, 13, new Color(1, 1, 1, 0.85f), TextAnchor.UpperRight);
            missaoObj.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIKit.Ancorar(missaoObj.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -74), new Vector2(0, -42));
            statsTxt = Tx(m, "", 12, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleRight);
            UIKit.Ancorar(statsTxt.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 18));
        }

        void MontarPoderes()
        {
            grupoSlots = new GameObject("Poderes", typeof(RectTransform));
            var g = (RectTransform)grupoSlots.transform; g.SetParent(raiz, false);
            g.anchorMin = g.anchorMax = new Vector2(0.5f, 0); g.pivot = new Vector2(0.5f, 0); g.anchoredPosition = new Vector2(0, 24); g.sizeDelta = new Vector2(420, 96);
            var chip = Fundo(g, Painel); var cr = chip.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0, 0); cr.pivot = new Vector2(0, 0); cr.anchoredPosition = new Vector2(0, 8); cr.sizeDelta = new Vector2(52, 52);
            semPoderImg = chip;
            semPoderTxt = Tx(cr, "1\n<size=10>livre</size>", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIKit.Esticar(semPoderTxt.rectTransform);
            for (int s = 0; s < 4; s++)
            {
                var o = new GameObject("Slot " + (s + 2), typeof(RectTransform)); slotObj[s] = o;
                var rt = (RectTransform)o.transform; rt.SetParent(g, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(64 + 30 + s * 88, 4 + 30); rt.sizeDelta = new Vector2(60, 60);
                slotFundo[s] = Fundo(rt, Painel); UIKit.Esticar(slotFundo[s].rectTransform);
                slotBorda[s] = new GameObject("borda", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                slotBorda[s].transform.SetParent(rt, false); slotBorda[s].sprite = UIKit.Arredondado; slotBorda[s].type = Image.Type.Sliced; slotBorda[s].raycastTarget = false;
                slotBorda[s].color = new Color(1, 1, 1, 0.0f); UIKit.Esticar(slotBorda[s].rectTransform, -3, -3, -3, -3);
                slotBorda[s].transform.SetAsFirstSibling();
                slotImg[s] = new GameObject("ícone", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                slotImg[s].transform.SetParent(rt, false); slotImg[s].raycastTarget = false; UIKit.Esticar(slotImg[s].rectTransform, 6, 6, 6, 6);
                slotGlifo[s] = Tx(rt, "", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                slotGlifo[s].font = FonteJapones; UIKit.Esticar(slotGlifo[s].rectTransform);
                slotTecla[s] = Tx(rt, (s + 2).ToString(), 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Ancorar(slotTecla[s].rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-9, -8), new Vector2(9, 10));
                slotTecla[s].gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            }
            tecnicasTxt = Tx(g, "", 14, Ouro, TextAnchor.MiddleCenter, FontStyle.Normal);
            var tr = tecnicasTxt.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0); tr.pivot = new Vector2(0.5f, 0); tr.anchoredPosition = new Vector2(0, 72); tr.sizeDelta = new Vector2(900, 22);
        }

        void MontarItens()
        {
            grupoItens = new GameObject("Itens", typeof(RectTransform));
            var g = (RectTransform)grupoItens.transform; g.SetParent(raiz, false);
            g.anchorMin = g.anchorMax = new Vector2(1, 0); g.pivot = new Vector2(1, 0); g.anchoredPosition = new Vector2(-28, 24); g.sizeDelta = new Vector2(240, 56);
            for (int i = 0; i < 4; i++)
            {
                var item = status != null ? status.Itens[i] : null;
                var o = new GameObject("Item " + i, typeof(RectTransform)); itemObj[i] = o;
                var rt = (RectTransform)o.transform; rt.SetParent(g, false); itemRt[i] = rt;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); rt.sizeDelta = new Vector2(52, 52);
                var f = Fundo(rt, Painel); UIKit.Esticar(f.rectTransform);
                var ic = new GameObject("ícone", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(rt, false); ic.sprite = Spr(item != null ? item.icone : "item_pocao"); ic.raycastTarget = false; UIKit.Esticar(ic.rectTransform, 7, 7, 7, 7);
                itemQtd[i] = Tx(rt, "0", 12, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                UIKit.Esticar(itemQtd[i].rectTransform, 0, 2, 4, 0);
                var k = Tx(rt, item != null ? item.tecla : "?", 11, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Esticar(k.rectTransform, 4, 0, 0, 2);
            }
        }

        void MontarCanto()
        {
            modoTxt = Tx(raiz, "", 14, new Color(1, 1, 1, 0.8f), TextAnchor.LowerLeft);
            modoTxt.supportRichText = true;
            var mr = modoTxt.rectTransform; mr.anchorMin = mr.anchorMax = new Vector2(0, 0); mr.pivot = new Vector2(0, 0); mr.anchoredPosition = new Vector2(28, 26); mr.sizeDelta = new Vector2(420, 24);
            comboGrupo = new GameObject("Combo", typeof(RectTransform));
            var cg = (RectTransform)comboGrupo.transform; cg.SetParent(raiz, false);
            cg.anchorMin = cg.anchorMax = new Vector2(0, 0.5f); cg.pivot = new Vector2(0, 0.5f); cg.anchoredPosition = new Vector2(40, 0); cg.sizeDelta = new Vector2(160, 90);
            comboNum = Tx(cg, "0", 56, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Esticar(comboNum.rectTransform);
            var ct = Tx(cg, "COMBO", 13, Ouro, TextAnchor.LowerLeft, FontStyle.Bold);
            UIKit.Esticar(ct.rectTransform, 4, -6, 0, 0);
            dicaPainel = new GameObject("Dica", typeof(RectTransform));
            var dr = (RectTransform)dicaPainel.transform; dr.SetParent(raiz, false);
            dr.anchorMin = dr.anchorMax = new Vector2(0, 0); dr.pivot = new Vector2(0, 0); dr.anchoredPosition = new Vector2(28, 56); dr.sizeDelta = new Vector2(360, 88);
            var df = Fundo(dr, Painel); UIKit.Esticar(df.rectTransform);
            dicaTxt = Tx(dr, "", 12, new Color(1, 1, 1, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, false);
            dicaTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIKit.Esticar(dicaTxt.rectTransform, 10, 6, 10, 6);
        }

        // ================================================================== atualização
        void Update()
        {
            if (raiz != null && raiz.gameObject.activeSelf == EstadoJogo.MenuAberto) raiz.gameObject.SetActive(!EstadoJogo.MenuAberto);
            var k = Keyboard.current;
            if (k != null && k.tabKey.wasPressedThisFrame) { dicaOculta = !dicaOculta; dicaAte = Time.time + 14f; }

            bool poderes = PerfilJogo.PoderesDisponiveis;
            if (vidaJogador != null)
            {
                hpFill.fillAmount = vidaJogador.Current / vidaJogador.Max;
                hpTxt.text = $"{vidaJogador.Current:0}";
            }
            if (status != null)
            {
                feFill.fillAmount = status.Fe / status.FeMaxima;
                feTxt.text = $"{Mathf.RoundToInt(100f * status.Fe / status.FeMaxima)}%";
                nivelTxt.text = $"Nv. {status.Nivel}  ·  <color=#B9C2D6>{status.Titulo}</color>";
                cargasTxt.text = $"◆ {status.Cargas}";
                escudosTxt.text = $"⛨ {status.Escudos}";
            }
            if (grupoFe.activeSelf != poderes) grupoFe.SetActive(poderes);
            if (grupoCargas.activeSelf != poderes) grupoCargas.SetActive(poderes);

            int combo = combate != null ? combate.ComboCount : 0;
            comboGrupo.SetActive(combo >= 2);
            comboNum.text = combo.ToString();

            AtualizarAlvo();
            AtualizarMira();
            AtualizarRelogio();
            AtualizarSlots(poderes);
            AtualizarItens();
            AtualizarMapa();
            AtualizarModo(poderes);
            if (!semMissaoInimiga) AtualizarMissao();
            if (TrainingManager.Instance != null && !semMissaoInimiga) statsTxt.text = $"Treino · dano {TrainingManager.Instance.TotalDamage:0} · DPS {TrainingManager.Instance.Dps:0.0}";

            bool mostraDica = PerfilJogo.D.dicasNaTela && !RegrasFase.PoderesBloqueados && !dicaOculta && Time.time < dicaAte;
            if (dicaPainel.activeSelf != mostraDica) dicaPainel.SetActive(mostraDica);

            if (Time.time > avisoAte) avisoTxt.text = "";
            else { var c = avisoTxt.color; c.a = Mathf.Clamp01((avisoAte - Time.time) / 0.4f); avisoTxt.color = c; }
        }

        void AtualizarSlots(bool poderes)
        {
            if (grupoSlots.activeSelf != poderes) grupoSlots.SetActive(poderes);
            if (!poderes) return;
            var modoAtual = modoCombate != null ? modoCombate.Atual : Modo.Poder;
            semPoderImg.color = modoAtual == Modo.Combate ? new Color(0.2f, 0.28f, 0.45f, 0.9f) : Painel;
            for (int s = 0; s < 4; s++)
            {
                int e = PerfilJogo.D.slots[s];
                bool vazio = e < 0;
                bool sel = !vazio && estilos != null && (int)estilos.Atual == e && modoAtual == Modo.Poder;
                slotImg[s].gameObject.SetActive(!vazio && e < 4);
                slotGlifo[s].gameObject.SetActive(!vazio && e >= 4);
                if (!vazio)
                {
                    if (e < 4) slotImg[s].sprite = IconeDe(e);
                    else { slotGlifo[s].text = Glifos[e]; slotGlifo[s].color = Color.Lerp(EstilosElementais.Cores[e], Color.white, 0.35f); }
                }
                var cor = vazio ? Color.white : EstilosElementais.Cores[e];
                slotFundo[s].color = vazio ? new Color(1, 1, 1, 0.08f) : Color.Lerp(Painel, cor, sel ? 0.5f : 0.18f);
                var b = slotBorda[s].color;
                slotBorda[s].color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(b.a, sel ? 0.95f : 0f, 10f * Time.deltaTime));
                float esc = Mathf.Lerp(slotObj[s].transform.localScale.x, sel ? 1.14f : 1f, 10f * Time.deltaTime);
                slotObj[s].transform.localScale = Vector3.one * esc;
                slotImg[s].color = modoAtual == Modo.Poder ? Color.white : new Color(1, 1, 1, 0.45f);
            }
        }

        void AtualizarItens()
        {
            bool algum = false;
            var vis = new List<int>();
            if (status != null && !RegrasFase.SemItens) for (int i = 0; i < 4; i++) if (status.Itens[i].quantidade > 0) vis.Add(i);
            for (int i = 0; i < 4; i++)
            {
                int pos = vis.IndexOf(i);
                itemObj[i].SetActive(pos >= 0);
                if (pos < 0) continue;
                algum = true;
                itemRt[i].anchoredPosition = new Vector2(-(vis.Count - 1 - pos) * 58f, 0);
                itemQtd[i].text = status.Itens[i].quantidade.ToString();
            }
            grupoItens.SetActive(algum);
        }

        void AtualizarMapa()
        {
            bool mapa = !RegrasFase.SemMapa;
            if (grupoMapa.activeSelf != mapa) { grupoMapa.SetActive(mapa); if (camMapa != null) camMapa.enabled = mapa; }
            ((RectTransform)grupoMissao.transform).anchoredPosition = new Vector2(-28, mapa ? -210 : -40);
            if (mapa) AtualizarMinimapa();
        }

        void AtualizarModo(bool poderes)
        {
            var modoAtual = modoCombate != null ? modoCombate.Atual : Modo.Poder;
            string alt = poderes ? "[1] sem poder" : "";
            if (modoAtual == Modo.Espada) modoTxt.text = $"<b>Katana</b> {(modoCombate != null && modoCombate.Grip == Empunhadura.DuasMaos ? "duas mãos" : "uma mão")}  <color=#9AA3B8>[H] empunhadura  [G] sair</color>";
            else if (modoAtual == Modo.Combate) modoTxt.text = "<b>Combate</b>  <color=#9AA3B8>[G] katana</color>";
            else modoTxt.text = $"<b>Poder</b>  <color=#9AA3B8>[G] katana  {alt}</color>";
        }

        void AtualizarElemento(Elemento e)
        {
            if (tecnicasTxt == null) return;
            var n = EstilosElementais.NomesTecnicas[(int)e];
            tecnicasTxt.text = $"<color=#FFFFFF>clique</color> {n[0]}   <color=#FFFFFF>segurar</color> {n[1]}   <color=#FFFFFF>F</color> {n[2]}";
            tecnicasTxt.color = Color.Lerp(Ouro, EstilosElementais.Cores[(int)e], 0.45f);
            int sl = PerfilJogo.SlotDe((int)e);
            if (dicaTxt != null)
                dicaTxt.text = $"<b>Poder {EstilosElementais.NomesElementos[(int)e]}</b>  [{(sl >= 0 ? (sl + 2).ToString() : "I")}]\nClique: golpe leve · segurar: forte · F: especial · R: roxo\nQ trava alvo · C dash · Ctrl rola · E concentra Fé\n[I] poderes  [P] personagem  [Esc] opções  [Tab] fechar";
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
                    if (dist < melhor && !d.Vida.IsDead) { melhor = dist; alvo = d; }
                }
            }
            alvoGrupo.SetActive(alvo != null);
            if (alvo == null) return;
            alvoNome.text = $"{alvo.Nome}  <color=#9AA3B8>Nv. {alvo.Nivel}</color>";
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
            int h = Mathf.FloorToInt(horas), m = Mathf.FloorToInt((horas - h) * 60f);
            relogio.text = $"{(h % 12 == 0 ? 12 : h % 12):00}:{m:00} {(h >= 12 ? "PM" : "AM")}";
        }

        void AtualizarMissao()
        {
            if (missaoCumprida) return;
            foreach (var d in TrainingDummy.Todos)
                if (d.Nome == alvoMissao && d.Vida.IsDead)
                {
                    missaoCumprida = true;
                    missaoObj.text = "<color=#9BE58A>✔</color> " + alvoMissao + " derrotado";
                    MostrarAviso("Missão concluída: " + missao);
                }
        }

        // ================================================================== minimapa
        void CriarCameraMinimapa()
        {
            var rt = new RenderTexture(256, 256, 16) { name = "Minimapa" };
            var go = new GameObject("Câmera do Minimapa");
            go.transform.SetParent(transform, false);
            camMapa = go.AddComponent<Camera>();
            camMapa.orthographic = true; camMapa.orthographicSize = raioMinimapa; camMapa.targetTexture = rt;
            camMapa.clearFlags = CameraClearFlags.SolidColor; camMapa.backgroundColor = new Color(0.12f, 0.13f, 0.12f);
            camMapa.farClipPlane = 300f; camMapa.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapaArea.GetComponentInChildren<RawImage>().texture = rt;
            camMapa.enabled = !RegrasFase.SemMapa;
        }

        void AtualizarMinimapa()
        {
            if (jogador == null || camMapa == null) return;
            camMapa.enabled = true;
            camMapa.transform.position = jogador.position + Vector3.up * 120f;
            seta.localEulerAngles = new Vector3(0, 0, -jogador.eulerAngles.y);
            float raioPx = mapaArea.rect.width * 0.5f;
            int n = 0;
            foreach (var d in TrainingDummy.Todos)
            {
                Vector3 rel = d.transform.position - jogador.position;
                var p = new Vector2(rel.x, rel.z) / raioMinimapa * raioPx;
                if (p.magnitude > raioPx - 8f || d.Vida.IsDead) continue;
                if (n >= marcadores.Count)
                {
                    var im = new GameObject("marcador", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(mapaArea, false); im.sprite = Spr("marcador_inimigo"); im.raycastTarget = false;
                    var r = im.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(14, 14);
                    marcadores.Add(r);
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
            avisoTxt.color = Color.white;
            avisoAte = Time.time + 1.8f;
        }
    }
}
