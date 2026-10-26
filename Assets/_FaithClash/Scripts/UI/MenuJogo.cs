using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.UI
{
    /// <summary>
    /// Menu do jogo (Esc): três abas — PODERES (inventário com 4 slots), PERSONAGEM (aparência) e CONFIGURAÇÕES (gráficos, jogabilidade, áudio, controles).
    /// I abre direto os poderes, P o personagem. Pausa o jogo, libera o mouse e salva tudo automaticamente (PerfilJogo).
    /// </summary>
    public class MenuJogo : MonoBehaviour
    {
        enum Aba { Pausa, Poderes, Personagem, Configuracoes }

        Canvas canvas;
        GameObject raiz;
        RectTransform areaConteudo;
        readonly List<Action> atualizadores = new List<Action>();
        readonly List<Button> botoesAba = new List<Button>();
        Aba aba = Aba.Pausa;
        bool aberto;
        float escalaAntes = 1f;
        Text dica;

        // poderes
        readonly List<(int elemento, Image fundo, Text selo)> cartoes = new List<(int, Image, Text)>();

        public static bool EstaAberto => EstadoJogo.MenuAberto;
        static MenuJogo atual;
        void Awake() { atual = this; }
        /// <summary>Gancho do teste automático (Training/PilotoMenu).</summary>
        public static void TestePausa() { if (atual != null) atual.Abrir(Aba.Pausa); }

        void Update()
        {
            var k = Keyboard.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (FindAnyObjectByType<PlayerController>() == null) return;
            if (pad != null)
            {
                if (pad.startButton.wasPressedThisFrame) { if (aberto) Fechar(); else Abrir(Aba.Pausa); return; }
                if (pad.selectButton.wasPressedThisFrame) { if (aberto && aba == Aba.Poderes) Fechar(); else Abrir(Aba.Poderes); return; }
                if (aberto && pad.buttonEast.wasPressedThisFrame) { Fechar(); return; }
            }
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) { if (aberto) Fechar(); else Abrir(Aba.Pausa); }
            else if (k.iKey.wasPressedThisFrame) { if (aberto && aba == Aba.Poderes) Fechar(); else Abrir(Aba.Poderes); }
            else if (k.pKey.wasPressedThisFrame) { if (aberto && aba == Aba.Personagem) Fechar(); else Abrir(Aba.Personagem); }
        }

        void OnDestroy() { if (aberto) { Time.timeScale = escalaAntes; EstadoJogo.MenuAberto = false; } PerfilJogo.Mudou -= Atualizar; }

        // ================================================================== abrir / fechar
        void Abrir(Aba qual)
        {
            if (canvas == null) Montar();
            if (!aberto)
            {
                aberto = true;
                EstadoJogo.MenuAberto = true;
                escalaAntes = Time.timeScale > 0.01f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                raiz.SetActive(true);
                SomJogo.Tocar("UI_Elemento", null, 0.5f, 1.1f);
            }
            MostrarAba(qual);
        }

        public void Fechar()
        {
            if (!aberto) return;
            aberto = false;
            EstadoJogo.MenuAberto = false;
            EstadoJogo.DeslocCamera = 0f; EstadoJogo.VistaPersonagem = false;
            Time.timeScale = escalaAntes;
            raiz.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            PerfilJogo.Salvar();
            SomJogo.Tocar("UI_Travar", null, 0.4f, 1.3f);
        }

        // ================================================================== montagem
        void Montar()
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(es);
            }
            var go = new GameObject("Canvas do Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;
            raiz = go;

            // cortina escura
            var cortina = UIKit.Painel(go.transform, "Cortina", new Color(0.01f, 0.01f, 0.03f, 0.55f), false);
            UIKit.Esticar(cortina.rectTransform);

            // painel principal à direita (o personagem aparece à esquerda, deslocado pela câmera)
            var painel = UIKit.Painel(go.transform, "Painel", UIKit.Fundo);
            UIKit.Ancorar(painel.rectTransform, new Vector2(0.655f, 0.05f), new Vector2(0.985f, 0.95f), Vector2.zero, Vector2.zero);
            UIKit.Contorno(painel, new Color(1f, 0.78f, 0.28f, 0.35f), 2f);

            // cabeçalho com abas
            var topo = UIKit.Retangulo(painel.transform, "Topo");
            UIKit.Ancorar(topo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -50), new Vector2(-12, -10));
            string[] nomes = { "PAUSA  [Esc]", "PODERES  [I]", "PERSONAGEM  [P]", "OPÇÕES" };
            Color[] cores = { new Color(0.4f, 0.16f, 0.65f), new Color(0.55f, 0.28f, 0.95f), new Color(0.6f, 0.45f, 0.1f), new Color(0.25f, 0.22f, 0.38f) };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var b = UIKit.Botao(topo, nomes[i], cores[i], () => Abrir((Aba)idx), 14);
                var rt = b.GetComponent<RectTransform>();
                UIKit.Ancorar(rt, new Vector2(i / 4f, 0), new Vector2((i + 1) / 4f, 1), new Vector2(i == 0 ? 0 : 3, 2), new Vector2(i == 3 ? 0 : -3, -2));
                botoesAba.Add(b);
            }

            // conteúdo
            areaConteudo = UIKit.Retangulo(painel.transform, "Conteúdo da aba");
            UIKit.Ancorar(areaConteudo, Vector2.zero, Vector2.one, new Vector2(12, 44), new Vector2(-12, -58));

            // rodapé
            dica = UIKit.Rotulo(painel.transform, "", 12, UIKit.TextoFraco, TextAnchor.MiddleLeft);
            UIKit.Ancorar(dica.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 6), new Vector2(-190, 38));
            var fechar = UIKit.Botao(painel.transform, "FECHAR", new Color(0.18f, 0.12f, 0.26f), Fechar, 14);
            UIKit.Ancorar(fechar.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-92, 8), new Vector2(-12, 36));
            var reset = UIKit.Botao(painel.transform, "PADRÕES", new Color(0.25f, 0.27f, 0.42f), () => { PerfilJogo.Restaurar(); }, 12);
            UIKit.Ancorar(reset.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-182, 8), new Vector2(-98, 36));

            PerfilJogo.Mudou += Atualizar;
            raiz.SetActive(false);
        }

        void Atualizar() { foreach (var a in atualizadores) a?.Invoke(); AtualizarCartoes(); }

        void MostrarAba(Aba qual)
        {
            aba = qual;
            for (int i = 0; i < botoesAba.Count; i++)
            {
                var img = botoesAba[i].GetComponent<Image>();
                if (!img.TryGetComponent(out Outline o)) o = img.gameObject.AddComponent<Outline>();
                o.effectColor = i == (int)qual ? Color.white : new Color(0, 0, 0, 0); o.effectDistance = new Vector2(3, -3);
                botoesAba[i].GetComponentInChildren<Text>().color = i == (int)qual ? Color.white : new Color(1, 1, 1, 0.65f);
            }
            for (int i = areaConteudo.childCount - 1; i >= 0; i--) Destroy(areaConteudo.GetChild(i).gameObject);
            atualizadores.Clear(); cartoes.Clear();
            EstadoJogo.VistaPersonagem = qual == Aba.Personagem;
            EstadoJogo.DeslocCamera = qual == Aba.Configuracoes || qual == Aba.Pausa ? 0.18f : 0.3f;
            switch (qual)
            {
                case Aba.Pausa: MontarPausa(); dica.text = "O jogo está pausado.  Esc continua."; break;
                case Aba.Poderes: MontarPoderes(); dica.text = "Clique num poder para equipar/remover (até 4).  Em jogo: 1 = sem poder, 2–5 = poderes equipados."; break;
                case Aba.Personagem: MontarPersonagem(); dica.text = "Tudo é salvo e aplicado na hora no seu personagem."; break;
                default: MontarConfiguracoes(); dica.text = "Suas escolhas ficam salvas neste computador."; break;
            }
            SomJogo.Tocar("UI_Elemento", null, 0.35f, 1.5f);
        }

        // ================================================================== aba: pausa
        void MontarPausa()
        {
            string cena = SceneManager.GetActiveScene().name;
            var titulo = UIKit.Rotulo(areaConteudo, "JOGO PAUSADO", 34, UIKit.Ouro, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Ancorar(titulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), new Vector2(0, 0));
            var sub = UIKit.Rotulo(areaConteudo, Cenas.TituloDe(cena), 16, UIKit.TextoFraco, TextAnchor.UpperCenter);
            UIKit.Ancorar(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -78), new Vector2(0, -54));

            var lista = UIKit.Retangulo(areaConteudo, "Botões da pausa");
            UIKit.Ancorar(lista, Vector2.zero, Vector2.one, new Vector2(30, 10), new Vector2(-30, -92));
            var vlg = lista.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
            void Item(string txt, Color cor, UnityAction ao) { var b = UIKit.Botao(lista, txt, cor, ao, 22); b.gameObject.AddComponent<LayoutElement>().minHeight = 48; }
            Item("CONTINUAR", new Color(0.6f, 0.45f, 0.1f), Fechar);
            Item("REINICIAR", new Color(0.3f, 0.2f, 0.5f), () => { Fechar(); Cenas.Ir(cena); });
            Item("PODERES", new Color(0.4f, 0.22f, 0.7f), () => Abrir(Aba.Poderes));
            Item("PERSONAGEM", new Color(0.5f, 0.38f, 0.1f), () => Abrir(Aba.Personagem));
            Item("OPÇÕES", new Color(0.28f, 0.2f, 0.45f), () => Abrir(Aba.Configuracoes));
            if (cena != "MenuPrincipal") Item("VOLTAR AO MENU PRINCIPAL", new Color(0.25f, 0.2f, 0.1f), () => { Fechar(); Cenas.Ir("MenuPrincipal"); });
            Item("SAIR DO JOGO", new Color(0.12f, 0.1f, 0.16f), Cenas.SairDoJogo);
        }

        // ================================================================== aba: poderes
        void MontarPoderes()
        {
            if (RegrasFase.PoderesBloqueados)
            {
                var aviso = UIKit.Painel(areaConteudo, "Aviso", new Color(0.55f, 0.12f, 0.18f, 0.9f));
                UIKit.Ancorar(aviso.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), Vector2.zero);
                var t = UIKit.Rotulo(aviso.transform, "🔒  Os poderes estão bloqueados nesta fase. Você pode montar o inventário, mas só poderá usá-lo depois.", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Esticar(t.rectTransform, 12, 0, 12, 0);
            }
            float topo = RegrasFase.PoderesBloqueados ? 62f : 0f;

            // grade 3x3 de cartões
            var grade = UIKit.Retangulo(areaConteudo, "Grade");
            UIKit.Ancorar(grade, Vector2.zero, Vector2.one, new Vector2(0, 92), new Vector2(0, -topo));
            const int colunas = 2, linhas = 5;
            for (int e = 0; e < 10; e++)
            {
                int el = e;
                int c = e % colunas, l = e / colunas;
                var cor = EstilosElementais.Cores[e];
                var cartao = UIKit.Botao(grade, "", Color.Lerp(UIKit.Cartao, cor, 0.14f), () => AlternarPoder(el), 10);
                var rt = cartao.GetComponent<RectTransform>();
                float gx = 0.01f, gy = 0.01f;
                UIKit.Ancorar(rt, new Vector2(c / (float)colunas + gx, 1f - (l + 1) / (float)linhas + gy), new Vector2((c + 1) / (float)colunas - gx, 1f - l / (float)linhas - gy), Vector2.zero, Vector2.zero);
                var fundo = cartao.GetComponent<Image>();
                var icone = new GameObject("Ícone", typeof(RectTransform), typeof(Image));
                icone.transform.SetParent(rt, false);
                var ii = icone.GetComponent<Image>(); ii.sprite = HUDJogo.IconeElemento(e); ii.raycastTarget = false;
                UIKit.Ancorar((RectTransform)icone.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -24), new Vector2(56, 24));
                if (e >= 4)
                {
                    var glifo = UIKit.Rotulo(icone.transform, HUDJogo.Glifo(e), 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    glifo.font = HUDJogo.FonteJapones; UIKit.Esticar(glifo.rectTransform);
                }
                var nome = UIKit.Rotulo(rt, EstilosElementais.NomesElementos[e], 15, Color.Lerp(cor, Color.white, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Ancorar(nome.rectTransform, Vector2.zero, Vector2.one, new Vector2(66, 4), new Vector2(-6, -5));
                var tec = EstilosElementais.NomesTecnicas[e];
                var det = UIKit.Rotulo(rt, $"<color=#FFFFFF>Clique</color>  {tec[0]}\n<color=#FFFFFF>Segurar</color>  {tec[1]}\n<color=#FFFFFF>[F]</color>  {tec[2]}", 10, UIKit.TextoFraco, TextAnchor.MiddleLeft);
                UIKit.Ancorar(det.rectTransform, Vector2.zero, Vector2.one, new Vector2(66, 3), new Vector2(-4, -22));
                var selo = UIKit.Rotulo(rt, "", 11, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                UIKit.Ancorar(selo.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-8, -5));
                cartoes.Add((e, fundo, selo));
            }

            // slots equipados
            var barra = UIKit.Retangulo(areaConteudo, "Slots");
            UIKit.Ancorar(barra, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), new Vector2(0, 84));
            var titulo = UIKit.Rotulo(barra, "EQUIPADOS", 12, UIKit.Ouro, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Ancorar(titulo.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(0, 0));
            var semPoder = UIKit.Painel(barra, "Sem poder", new Color(0.2f, 0.22f, 0.32f));
            UIKit.Ancorar(semPoder.rectTransform, new Vector2(0, 0), new Vector2(0.17f, 0), new Vector2(0, 0), new Vector2(0, 62));
            var sp = UIKit.Rotulo(semPoder.transform, "1\n<size=10>SEM PODER</size>", 20, UIKit.Texto, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Esticar(sp.rectTransform);
            for (int s = 0; s < 4; s++)
            {
                int slot = s;
                var b = UIKit.Botao(barra, "", UIKit.Cartao, () => { int el = PerfilJogo.D.slots[slot]; if (el >= 0) PerfilJogo.Alternar(el); }, 10);
                var rt = b.GetComponent<RectTransform>();
                float x0 = 0.19f + s * 0.2025f;
                UIKit.Ancorar(rt, new Vector2(x0, 0), new Vector2(x0 + 0.19f, 0), new Vector2(0, 0), new Vector2(0, 62));
                var txt = UIKit.Rotulo(rt, "", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Esticar(txt.rectTransform, 6, 4, 6, 4);
                var img = b.GetComponent<Image>();
                atualizadores.Add(() =>
                {
                    int el = PerfilJogo.D.slots[slot];
                    if (el < 0) { img.color = new Color(1f, 1f, 1f, 0.07f); txt.text = $"<size=20>{slot + 2}</size>\n<size=10><color=#8890B0>vazio</color></size>"; }
                    else
                    {
                        var c = EstilosElementais.Cores[el];
                        img.color = Color.Lerp(UIKit.Cartao, c, 0.45f);
                        txt.text = $"<size=20>{slot + 2}</size>\n<size=11>{EstilosElementais.NomesElementos[el]}</size>";
                    }
                });
            }
            Atualizar();
        }

        void AlternarPoder(int elemento)
        {
            if (!PerfilJogo.Alternar(elemento))
            {
                SomJogo.Tocar("UI_Travar", null, 0.5f, 0.6f);
                if (dica != null) dica.text = "<color=#FF7080>Os 4 slots estão cheios — remova um poder primeiro.</color>";
                return;
            }
            SomJogo.Tocar(PerfilJogo.Equipado(elemento) ? "Poder_Ativar" : "Poder_Desativar", null, 0.35f, 1.4f, duracaoMax: 0.5f);
        }

        void AtualizarCartoes()
        {
            foreach (var (e, fundo, selo) in cartoes)
            {
                if (fundo == null) continue;
                int s = PerfilJogo.SlotDe(e);
                var cor = EstilosElementais.Cores[e];
                fundo.color = Color.Lerp(UIKit.Cartao, cor, s >= 0 ? 0.42f : 0.12f);
                if (!fundo.TryGetComponent(out Outline o)) o = fundo.gameObject.AddComponent<Outline>();
                o.effectColor = s >= 0 ? cor : new Color(0, 0, 0, 0); o.effectDistance = new Vector2(3, -3);
                selo.text = s >= 0 ? $"SLOT {s + 2}  ✔" : "";
                selo.color = Color.Lerp(cor, Color.white, 0.5f);
            }
        }

        // ================================================================== aba: personagem
        void MontarPersonagem()
        {
            var c = UIKit.AreaRolavel(areaConteudo, out var scroll);
            UIKit.Esticar(scroll.GetComponent<RectTransform>());
            PaginasMenu.Estilos(c, atualizadores); PaginasMenu.Corpo(c, atualizadores); PaginasMenu.Roupas(c, atualizadores); PaginasMenu.Katana(c, atualizadores);
            Atualizar();
        }

        // ================================================================== aba: configurações
        void MontarConfiguracoes()
        {
            var c = UIKit.AreaRolavel(areaConteudo, out var scroll);
            UIKit.Esticar(scroll.GetComponent<RectTransform>());
            PaginasMenu.Graficos(c, atualizadores); PaginasMenu.Jogabilidade(c, atualizadores); PaginasMenu.Camera(c, atualizadores);
            PaginasMenu.Audio(c, atualizadores); PaginasMenu.Controles(c, atualizadores);
            Atualizar();
        }
    }
}
