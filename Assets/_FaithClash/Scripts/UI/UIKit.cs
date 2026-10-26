using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FaithClash.UI
{
    /// <summary>Toca um tique quando o mouse passa por cima de um botão.</summary>
    public class SomHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
    {
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) { FaithClash.Core.SomJogo.Tocar("UI_Hover", null, 0.6f, 1f, 0.03f); }
    }

    /// <summary>Peças de interface montadas por código: painéis arredondados, botões, alternadores, controles deslizantes, seletores e amostras de cor.</summary>
    public static class UIKit
    {
        public static readonly Color Fundo = new Color(0.04f, 0.045f, 0.08f, 0.97f);
        public static readonly Color Cartao = new Color(0.10f, 0.11f, 0.19f, 1f);
        public static readonly Color CartaoClaro = new Color(0.16f, 0.17f, 0.28f, 1f);
        public static readonly Color Ouro = new Color(1f, 0.78f, 0.28f);
        public static readonly Color Ciano = new Color(0.66f, 0.4f, 1f);
        public static readonly Color Magenta = new Color(0.82f, 0.62f, 1f);
        public static readonly Color Verde = new Color(1f, 0.92f, 0.66f);
        public static readonly Color Texto = new Color(0.93f, 0.94f, 0.99f);
        public static readonly Color TextoFraco = new Color(0.62f, 0.65f, 0.78f);

        static Font fonte;
        static Sprite arred, circulo;

        public static Font Fonte
        {
            get
            {
                if (fonte == null) fonte = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Semibold", "Segoe UI", "Arial" }, 32);
                return fonte;
            }
        }

        public static Sprite Arredondado
        {
            get
            {
                if (arred != null) return arred;
                const int n = 64, r = 18;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Max(r - x, x - (n - 1 - r))), dy = Mathf.Max(0, Mathf.Max(r - y, y - (n - 1 - r)));
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                    }
                t.Apply();
                return arred = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            }
        }

        static Sprite branco;
        /// <summary>Quadrado branco 4x4 (para barras preenchíveis e linhas).</summary>
        public static Sprite Branco
        {
            get
            {
                if (branco != null) return branco;
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int i = 0; i < 16; i++) t.SetPixel(i % 4, i / 4, Color.white);
                t.Apply();
                return branco = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        public static Sprite Circulo
        {
            get
            {
                if (circulo != null) return circulo;
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                        t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d)));
                    }
                t.Apply();
                return circulo = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // ---------------------------------------------------------------- básicos
        public static RectTransform Retangulo(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        public static void Esticar(RectTransform rt, float esq = 0, float baixo = 0, float dir = 0, float cima = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(esq, baixo); rt.offsetMax = new Vector2(-dir, -cima);
        }

        public static void Ancorar(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        public static Image Painel(Transform pai, string nome, Color cor, bool arredondado = true)
        {
            var rt = Retangulo(pai, nome);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = cor;
            if (arredondado) { img.sprite = Arredondado; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
            return img;
        }

        public static Text Rotulo(Transform pai, string texto, int tamanho, Color cor, TextAnchor alin = TextAnchor.MiddleLeft, FontStyle estilo = FontStyle.Normal)
        {
            var rt = Retangulo(pai, "Texto");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Fonte; t.text = texto; t.fontSize = tamanho; t.color = cor; t.alignment = alin; t.fontStyle = estilo;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = false;
            return t;
        }

        public static Button Botao(Transform pai, string texto, Color cor, UnityAction aoClicar, int tamanho = 24)
        {
            var img = Painel(pai, "Botão", cor);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.selectedColor = Color.white; cb.fadeDuration = 0.06f;
            b.colors = cb;
            b.onClick.AddListener(aoClicar);
            b.onClick.AddListener(() => FaithClash.Core.SomJogo.Tocar("UI_Clique", null, 0.5f, 1.15f, 0.04f));
            img.gameObject.AddComponent<SomHover>();
            var t = Rotulo(img.transform, texto, tamanho, Texto, TextAnchor.MiddleCenter, FontStyle.Bold);
            Esticar(t.rectTransform, 6, 2, 6, 2);
            return b;
        }

        public static Image Contorno(Image alvo, Color cor, float espessura = 3f)
        {
            if (!alvo.TryGetComponent(out Outline o)) o = alvo.gameObject.AddComponent<Outline>();
            o.effectColor = cor; o.effectDistance = new Vector2(espessura, -espessura);
            return alvo;
        }

        // ---------------------------------------------------------------- controles de configuração
        public sealed class Linha
        {
            public RectTransform raiz, controle;
            public Text rotulo;
        }

        /// <summary>Linha de configuração: rótulo à esquerda e área de controle à direita (dentro de um grupo de layout vertical).</summary>
        public static Linha NovaLinha(Transform conteudo, string rotulo, float altura = 40f, string dica = null)
        {
            var fundo = Painel(conteudo, "Linha", new Color(1f, 1f, 1f, 0.045f));
            var le = fundo.gameObject.AddComponent<LayoutElement>();
            le.minHeight = altura; le.preferredHeight = altura;
            var l = new Linha { raiz = fundo.rectTransform };
            l.rotulo = Rotulo(fundo.transform, rotulo, 18, Texto);
            Ancorar(l.rotulo.rectTransform, new Vector2(0, 0), new Vector2(0.44f, 1), new Vector2(18, 0), new Vector2(0, 0));
            if (!string.IsNullOrEmpty(dica))
            {
                l.rotulo.text = $"{rotulo}\n<size=12><color=#9AA0C0>{dica}</color></size>";
            }
            l.controle = Retangulo(fundo.transform, "Controle");
            Ancorar(l.controle, new Vector2(0.46f, 0), new Vector2(1, 1), new Vector2(0, 6), new Vector2(-14, -6));
            return l;
        }

        public static void Titulo(Transform conteudo, string texto, Color cor)
        {
            var rt = Retangulo(conteudo, "Seção");
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 38; le.preferredHeight = 38;
            var barra = Painel(rt, "barra", cor); Ancorar(barra.rectTransform, new Vector2(0, 0.15f), new Vector2(0, 0.85f), new Vector2(0, 0), new Vector2(7, 0));
            var t = Rotulo(rt, texto.ToUpperInvariant(), 17, cor, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ancorar(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 0), Vector2.zero);
        }

        public static void Alternador(Transform conteudo, string rotulo, Func<bool> ler, Action<bool> gravar, List<Action> atualizadores, string dica = null)
        {
            var l = NovaLinha(conteudo, rotulo, dica == null ? 40f : 50f, dica);
            Image fundo = null; Text txt = null;
            var b = Botao(l.controle, "", Cartao, () => { gravar(!ler()); }, 15);
            fundo = b.GetComponent<Image>(); txt = b.GetComponentInChildren<Text>();
            Ancorar(b.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-130, -14), new Vector2(0, 14));
            Action atualizar = () =>
            {
                bool v = ler();
                fundo.color = v ? new Color(0.6f, 0.45f, 0.08f) : new Color(0.15f, 0.13f, 0.2f);
                txt.text = v ? "LIGADO" : "DESLIGADO";
            };
            atualizadores.Add(atualizar); atualizar();
        }

        public static void Deslizante(Transform conteudo, string rotulo, float min, float max, Func<float> ler, Action<float> gravar, List<Action> atualizadores,
            string formato = "0.00", string dica = null, Color? cor = null)
        {
            var l = NovaLinha(conteudo, rotulo, dica == null ? 40f : 50f, dica);
            var cr = cor ?? Ciano;
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(l.controle, false);
            var rt = (RectTransform)go.transform;
            Ancorar(rt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -10), new Vector2(-84, 10));
            var bg = Painel(rt, "Fundo", new Color(0f, 0f, 0f, 0.55f));
            Ancorar(bg.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -3), new Vector2(0, 3));
            var area = Retangulo(rt, "Área do preenchimento");
            Ancorar(area, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -3), new Vector2(0, 3));
            var fill = Painel(area, "Preenchimento", cr);
            var frt = fill.rectTransform; frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(0, 1); frt.offsetMin = frt.offsetMax = Vector2.zero;
            var areaMao = Retangulo(rt, "Área da alça");
            Esticar(areaMao, 8, 0, 8, 0);
            var alca = new GameObject("Alça", typeof(RectTransform), typeof(Image));
            alca.transform.SetParent(areaMao, false);
            var ai = alca.GetComponent<Image>(); ai.sprite = Circulo; ai.color = Color.white;
            ((RectTransform)alca.transform).sizeDelta = new Vector2(18, 18);
            var s = go.GetComponent<Slider>();
            s.fillRect = frt; s.handleRect = (RectTransform)alca.transform; s.targetGraphic = ai;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max;
            var val = Rotulo(l.controle, "", 16, Ouro, TextAnchor.MiddleRight, FontStyle.Bold);
            Ancorar(val.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-78, 0), new Vector2(0, 0));
            bool interno = false;
            Action atualizar = () => { interno = true; s.value = ler(); val.text = ler().ToString(formato); interno = false; };
            s.onValueChanged.AddListener(v => { if (interno) return; gravar(v); val.text = v.ToString(formato); });
            atualizadores.Add(atualizar); atualizar();
        }

        public static void Opcoes(Transform conteudo, string rotulo, string[] nomes, Func<int> ler, Action<int> gravar, List<Action> atualizadores, string dica = null)
        {
            var l = NovaLinha(conteudo, rotulo, dica == null ? 40f : 50f, dica);
            var esq = Botao(l.controle, "◄", CartaoClaro, () => gravar((ler() - 1 + nomes.Length) % nomes.Length), 14);
            Ancorar(esq.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-250, -14), new Vector2(-222, 14));
            var dir = Botao(l.controle, "►", CartaoClaro, () => gravar((ler() + 1) % nomes.Length), 14);
            Ancorar(dir.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, -14), new Vector2(0, 14));
            var valor = Rotulo(l.controle, "", 16, Ciano, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ancorar(valor.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-218, 0), new Vector2(-32, 0));
            Action atualizar = () => { int i = Mathf.Clamp(ler(), 0, nomes.Length - 1); valor.text = nomes[i]; };
            atualizadores.Add(atualizar); atualizar();
        }

        public static void Amostras(Transform conteudo, string rotulo, Color[] cores, Func<int> ler, Action<int> gravar, List<Action> atualizadores)
        {
            var l = NovaLinha(conteudo, rotulo, 44f);
            var fundos = new List<Image>();
            int n = cores.Length;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var b = Botao(l.controle, "", cores[i], () => gravar(idx), 10);
                var rt = b.GetComponent<RectTransform>();
                float w = 26f, esp = 5f;
                float x = -(n - 1 - i) * (w + esp);
                Ancorar(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(x - w, -w / 2), new Vector2(x, w / 2));
                fundos.Add(b.GetComponent<Image>());
            }
            Action atualizar = () =>
            {
                int sel = ler();
                for (int i = 0; i < n; i++)
                {
                    if (!fundos[i].TryGetComponent(out Outline o)) o = fundos[i].gameObject.AddComponent<Outline>();
                    o.effectColor = i == sel ? Color.white : new Color(0, 0, 0, 0);
                    o.effectDistance = new Vector2(3, -3);
                    fundos[i].rectTransform.localScale = Vector3.one * (i == sel ? 1.12f : 1f);
                }
            };
            atualizadores.Add(atualizar); atualizar();
        }

        public static Button BotaoLinha(Transform conteudo, string rotulo, string textoBotao, Color cor, UnityAction ao)
        {
            var l = NovaLinha(conteudo, rotulo, 40f);
            var b = Botao(l.controle, textoBotao, cor, ao, 15);
            Ancorar(b.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-200, -14), new Vector2(0, 14));
            return b;
        }

        // ---------------------------------------------------------------- rolagem
        public static RectTransform AreaRolavel(Transform pai, out ScrollRect scroll)
        {
            var vp = Painel(pai, "Janela", new Color(0, 0, 0, 0.001f), false);
            vp.gameObject.AddComponent<RectMask2D>();
            scroll = vp.gameObject.AddComponent<ScrollRect>();
            var conteudo = Retangulo(vp.transform, "Conteúdo");
            conteudo.anchorMin = new Vector2(0, 1); conteudo.anchorMax = new Vector2(1, 1); conteudo.pivot = new Vector2(0.5f, 1);
            conteudo.offsetMin = new Vector2(0, 0); conteudo.offsetMax = new Vector2(0, 0);
            var vlg = conteudo.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5; vlg.padding = new RectOffset(2, 14, 2, 16);
            vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var csf = conteudo.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = conteudo; scroll.viewport = vp.rectTransform;
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 45f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return conteudo;
        }
    }
}
