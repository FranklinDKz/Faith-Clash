using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.UI
{
    /// <summary>
    /// Menu principal: CAMPANHA, PRATICAR, COFRE (skins, itens, cosméticos, editar personagem), OPÇÕES e SAIR.
    /// Navega com mouse, setas/WASD + Enter ou direcional do controle. Montado todo por código.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        // ------------------------------------------------------------------ item do menu (hover)
        class ItemMenu : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
        {
            public int indice; public MenuPrincipal dono; public Text texto; public Text marca;
            public void OnPointerEnter(PointerEventData e) { dono.Selecionar(indice, false); }
            public void OnPointerClick(PointerEventData e) { dono.Selecionar(indice, false); dono.Ativar(); }
        }

        struct Entrada { public string nome; public string detalhe; public Action acao; }

        Canvas canvas;
        RawImage fundo;
        readonly List<ItemMenu> itens = new List<ItemMenu>();
        Entrada[] entradas;
        Text detalhe;
        int indice;

        // sub-painel (opções / cofre)
        GameObject subRaiz;
        RectTransform subConteudo;
        Text subTitulo, subDica;
        readonly List<Button> subAbas = new List<Button>();
        readonly List<Action> atualizadores = new List<Action>();
        string[] subNomes; Action<int, Transform>[] subPaginas;
        int subAba;
        bool subAberto;
        float tempo;

        public static MenuPrincipal Atual;

        /// <summary>Gancho do teste automático (Training/PilotoMenu).</summary>
        public void TesteAcionar(string o)
        {
            switch (o)
            {
                case "campanha": Selecionar(0, true); Ativar(); break;
                case "praticar": Selecionar(1, true); Ativar(); break;
                case "cofre": Selecionar(2, true); Ativar(); break;
                case "opcoes": Selecionar(3, true); Ativar(); break;
                case "aba2": MostrarAba(2); break;
                case "aba3": MostrarAba(3); break;
                case "voltar": FecharSub(); break;
            }
        }

        void Start()
        {
            Atual = this;
            Time.timeScale = 1f; EstadoJogo.MenuAberto = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Montar();
            PerfilJogo.Mudou += AoMudarPerfil;
        }

        void OnDestroy() { PerfilJogo.Mudou -= AoMudarPerfil; }

        void AoMudarPerfil() { foreach (var a in atualizadores) a?.Invoke(); }

        // ================================================================== montagem
        void Montar()
        {
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var go = new GameObject("Canvas do Menu Principal", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;

            // fundo pintado, que respira devagar
            var tex = Resources.Load<Texture2D>("Menu/fundo");
            var fundoGo = new GameObject("Fundo", typeof(RectTransform), typeof(RawImage));
            fundoGo.transform.SetParent(go.transform, false);
            fundo = fundoGo.GetComponent<RawImage>(); fundo.texture = tex; fundo.raycastTarget = false;
            if (tex == null) fundo.color = new Color(0.04f, 0.05f, 0.1f);
            UIKit.Esticar(fundo.rectTransform);

            // escurece a esquerda para o menu ler bem
            var lado = UIKit.Painel(go.transform, "Sombra lateral", new Color(0.01f, 0.015f, 0.04f, 0.72f), false);
            UIKit.Ancorar(lado.rectTransform, new Vector2(0, 0), new Vector2(0.42f, 1), Vector2.zero, Vector2.zero);
            var borda = UIKit.Painel(go.transform, "Borda", new Color(1f, 0.78f, 0.28f, 0.35f), false);
            UIKit.Ancorar(borda.rectTransform, new Vector2(0.42f, 0), new Vector2(0.42f, 1), new Vector2(0, 0), new Vector2(2, 0));

            var logoTex = Resources.Load<Texture2D>("Menu/logo");
            var logo = new GameObject("Logo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            logo.transform.SetParent(go.transform, false); logo.texture = logoTex; logo.raycastTarget = false; logo.enabled = logoTex != null;
            UIKit.Ancorar(logo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -470), new Vector2(810, -38));

            entradas = new[]
            {
                new Entrada { nome = "CAMPANHA", detalhe = "Fase 1 — corrida contra o tempo", acao = () => Cenas.Ir(Cenas.Campanha) },
                new Entrada { nome = "PRATICAR", detalhe = "Arena de treino livre: parkour, combate e poderes", acao = () => Cenas.Ir(Cenas.Treino) },
                new Entrada { nome = "COFRE", detalhe = "Skins, itens, cosméticos e edição do personagem da campanha", acao = AbrirCofre },
                new Entrada { nome = "OPÇÕES", detalhe = "Gráficos, áudio, jogabilidade, câmera e controles", acao = AbrirOpcoes },
                new Entrada { nome = "SAIR", detalhe = "Fechar o jogo", acao = Cenas.SairDoJogo },
            };
            var lista = UIKit.Retangulo(go.transform, "Lista");
            UIKit.Ancorar(lista, new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(120, 170), new Vector2(0, -455));
            var linha = UIKit.Painel(lista, "Linha vertical", new Color(1, 1, 1, 0.18f), false);
            UIKit.Ancorar(linha.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(2, 0));
            float h = 84f;
            for (int i = 0; i < entradas.Length; i++)
            {
                var it = UIKit.Retangulo(lista, "Item " + entradas[i].nome);
                UIKit.Ancorar(it, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -(i + 1) * h), new Vector2(0, -i * h));
                var img = it.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0.001f);
                var m = UIKit.Rotulo(it, "◆", 20, UIKit.Ouro, TextAnchor.MiddleLeft);
                UIKit.Ancorar(m.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(22, 0), new Vector2(60, 0));
                var t = UIKit.Rotulo(it, entradas[i].nome, 46, UIKit.TextoFraco, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.Ancorar(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(66, 0), Vector2.zero);
                var item = it.gameObject.AddComponent<ItemMenu>();
                item.indice = i; item.dono = this; item.texto = t; item.marca = m;
                itens.Add(item);
            }
            detalhe = UIKit.Rotulo(go.transform, "", 22, UIKit.Texto, TextAnchor.MiddleLeft);
            UIKit.Ancorar(detalhe.rectTransform, new Vector2(0, 0), new Vector2(0.42f, 0), new Vector2(116, 90), new Vector2(-40, 150));
            var rod = UIKit.Rotulo(go.transform, "↑ ↓  navegar     Enter  escolher     Esc  voltar", 16, UIKit.TextoFraco, TextAnchor.MiddleLeft);
            UIKit.Ancorar(rod.rectTransform, new Vector2(0, 0), new Vector2(0.42f, 0), new Vector2(116, 36), new Vector2(-40, 80));
            var versao = UIKit.Rotulo(go.transform, "Faith Clash", 14, UIKit.TextoFraco, TextAnchor.LowerRight);
            UIKit.Ancorar(versao.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 20), new Vector2(-24, 50));

            MontarSubPainel(go.transform);
            Selecionar(0, true);
        }

        void MontarSubPainel(Transform pai)
        {
            var raiz = UIKit.Painel(pai, "Sub-painel", new Color(0.01f, 0.012f, 0.03f, 1f), false);
            UIKit.Esticar(raiz.rectTransform);
            subRaiz = raiz.gameObject;
            if (fundo != null && fundo.texture != null)
            {
                var img = new GameObject("Fundo escurecido", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                img.transform.SetParent(raiz.transform, false); img.texture = fundo.texture; img.color = new Color(0.22f, 0.22f, 0.22f, 1f); img.raycastTarget = false;
                UIKit.Esticar(img.rectTransform);
            }

            var barra = UIKit.Painel(raiz.transform, "Barra superior", new Color(0f, 0f, 0f, 0.92f), false);
            UIKit.Ancorar(barra.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -74), Vector2.zero);
            var bloco = UIKit.Painel(barra.transform, "Título", UIKit.Ouro, false);
            UIKit.Ancorar(bloco.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(280, 0));
            subTitulo = UIKit.Rotulo(bloco.transform, "", 28, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Esticar(subTitulo.rectTransform);
            var abas = UIKit.Retangulo(barra.transform, "Abas");
            UIKit.Ancorar(abas, Vector2.zero, Vector2.one, new Vector2(300, 0), new Vector2(-20, 0));
            abas.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 6;
            var hl = abas.GetComponent<HorizontalLayoutGroup>(); hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;

            subConteudo = UIKit.Retangulo(raiz.transform, "Conteúdo");
            UIKit.Ancorar(subConteudo, new Vector2(0.1f, 0), new Vector2(0.9f, 1), new Vector2(0, 100), new Vector2(0, -100));

            var rodape = UIKit.Painel(raiz.transform, "Rodapé", new Color(0f, 0f, 0f, 0.92f), false);
            UIKit.Ancorar(rodape.rectTransform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 68));
            var voltar = UIKit.Botao(rodape.transform, "VOLTAR", new Color(0.1f, 0.12f, 0.2f), FecharSub, 22);
            UIKit.Ancorar(voltar.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(14, 10), new Vector2(190, -10));
            var padrao = UIKit.Botao(rodape.transform, "RESTAURAR PADRÃO", new Color(0.1f, 0.12f, 0.2f), () => PerfilJogo.Restaurar(), 20);
            UIKit.Ancorar(padrao.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(200, 10), new Vector2(470, -10));
            subDica = UIKit.Rotulo(rodape.transform, "", 17, UIKit.TextoFraco, TextAnchor.MiddleRight);
            UIKit.Ancorar(subDica.rectTransform, Vector2.zero, Vector2.one, new Vector2(490, 0), new Vector2(-24, 0));
            subRaiz.SetActive(false);
            this.abasRaiz = abas;
        }
        RectTransform abasRaiz;

        // ================================================================== seleção
        void Selecionar(int i, bool silencioso)
        {
            if (subAberto) return;
            indice = (i + itens.Count) % itens.Count;
            for (int k = 0; k < itens.Count; k++)
            {
                bool sel = k == indice;
                itens[k].texto.color = sel ? Color.white : UIKit.TextoFraco;
                itens[k].texto.rectTransform.localScale = Vector3.one * (sel ? 1.06f : 1f);
                itens[k].marca.enabled = sel;
            }
            detalhe.text = entradas[indice].detalhe;
            if (!silencioso) SomJogo.Tocar("UI_Elemento", null, 0.3f, 1.6f);
        }

        void Ativar()
        {
            if (subAberto || Cenas.Carregando) return;
            SomJogo.Tocar("UI_Clique", null, 0.5f, 1.15f);
            entradas[indice].acao();
        }

        // ================================================================== sub-painéis
        void AbrirOpcoes()
        {
            Abrir("OPÇÕES",
                new[] { "GRÁFICOS", "ÁUDIO", "JOGABILIDADE", "CÂMERA", "CONTROLES" },
                new Action<int, Transform>[]
                {
                    (i, c) => PaginasMenu.Graficos(c, atualizadores),
                    (i, c) => PaginasMenu.Audio(c, atualizadores),
                    (i, c) => PaginasMenu.Jogabilidade(c, atualizadores),
                    (i, c) => PaginasMenu.Camera(c, atualizadores),
                    (i, c) => PaginasMenu.Controles(c, atualizadores),
                }, "Suas escolhas ficam salvas neste computador.");
        }

        void AbrirCofre()
        {
            Abrir("COFRE",
                new[] { "SKINS", "ITENS", "COSMÉTICOS", "EDITAR PERSONAGEM DA CAMPANHA" },
                new Action<int, Transform>[]
                {
                    (i, c) => { PaginasMenu.Estilos(c, atualizadores); PaginasMenu.Corpo(c, atualizadores); },
                    (i, c) => { PaginasMenu.Katana(c, atualizadores); Poderes(c); },
                    (i, c) => PaginasMenu.Roupas(c, atualizadores),
                    (i, c) => { PaginasMenu.Estilos(c, atualizadores); PaginasMenu.Corpo(c, atualizadores); PaginasMenu.Roupas(c, atualizadores); PaginasMenu.Katana(c, atualizadores); Poderes(c); },
                }, "Tudo é salvo na hora e vale para a campanha e para o modo Praticar.");
        }

        void Poderes(Transform c)
        {
            UIKit.Titulo(c, "Poderes equipados (até 4)", UIKit.Magenta);
            for (int e = 0; e < 10; e++)
            {
                int el = e;
                var t = EstilosElementais.NomesTecnicas[e];
                UIKit.Alternador(c, EstilosElementais.NomesElementos[e], () => PerfilJogo.Equipado(el), v => PerfilJogo.Alternar(el), atualizadores, t[0] + "  ·  " + t[1] + "  ·  " + t[2]);
            }
        }

        void Abrir(string titulo, string[] nomes, Action<int, Transform>[] paginas, string dica)
        {
            subNomes = nomes; subPaginas = paginas; subAberto = true;
            subTitulo.text = titulo; subDica.text = dica;
            for (int i = abasRaiz.childCount - 1; i >= 0; i--) Destroy(abasRaiz.GetChild(i).gameObject);
            subAbas.Clear();
            for (int i = 0; i < nomes.Length; i++)
            {
                int idx = i;
                var b = UIKit.Botao(abasRaiz, nomes[i], new Color(0, 0, 0, 0), () => MostrarAba(idx), 20);
                var le = b.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 40 + nomes[i].Length * 14f;
                subAbas.Add(b);
            }
            subRaiz.SetActive(true);
            MostrarAba(0);
        }

        void MostrarAba(int i)
        {
            subAba = (i + subNomes.Length) % subNomes.Length;
            for (int k = 0; k < subAbas.Count; k++)
            {
                var t = subAbas[k].GetComponentInChildren<Text>();
                t.color = k == subAba ? UIKit.Ouro : UIKit.TextoFraco;
                var img = subAbas[k].GetComponent<Image>();
                img.color = k == subAba ? new Color(1f, 0.78f, 0.28f, 0.14f) : new Color(0, 0, 0, 0);
            }
            for (int k = subConteudo.childCount - 1; k >= 0; k--) Destroy(subConteudo.GetChild(k).gameObject);
            atualizadores.Clear();
            var c = UIKit.AreaRolavel(subConteudo, out var scroll);
            UIKit.Esticar(scroll.GetComponent<RectTransform>());
            subPaginas[subAba](subAba, c);
            AoMudarPerfil();
            SomJogo.Tocar("UI_Elemento", null, 0.3f, 1.4f);
        }

        void FecharSub()
        {
            if (!subAberto) return;
            subAberto = false; subRaiz.SetActive(false);
            PerfilJogo.Salvar();
            SomJogo.Tocar("UI_Travar", null, 0.4f, 1.3f);
            Selecionar(indice, true);
        }

        // ================================================================== entrada e animação
        void Update()
        {
            tempo += Time.unscaledDeltaTime;
            if (fundo != null && fundo.texture != null)
            {
                float z = 1.04f + Mathf.Sin(tempo * 0.15f) * 0.02f;
                fundo.rectTransform.localScale = new Vector3(z, z, 1f);
                fundo.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(tempo * 0.1f) * 14f, 0f);
            }
            if (Cenas.Carregando) return;
            var k = Keyboard.current; var pad = UnityEngine.InputSystem.Gamepad.current;
            bool cima = false, baixo = false, ok = false, voltar = false, esq = false, dir = false;
            if (k != null)
            {
                cima = k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame; baixo = k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
                esq = k.leftArrowKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame; dir = k.rightArrowKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
                ok = k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame; voltar = k.escapeKey.wasPressedThisFrame;
            }
            if (pad != null)
            {
                cima |= pad.dpad.up.wasPressedThisFrame; baixo |= pad.dpad.down.wasPressedThisFrame;
                esq |= pad.leftShoulder.wasPressedThisFrame; dir |= pad.rightShoulder.wasPressedThisFrame;
                ok |= pad.buttonSouth.wasPressedThisFrame; voltar |= pad.buttonEast.wasPressedThisFrame;
            }
            if (subAberto)
            {
                if (voltar) FecharSub();
                else if (esq) MostrarAba(subAba - 1);
                else if (dir) MostrarAba(subAba + 1);
            }
            else
            {
                if (cima) Selecionar(indice - 1, false);
                else if (baixo) Selecionar(indice + 1, false);
                else if (ok) Ativar();
            }
        }
    }
}
