using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FaithClash.Core;

namespace FaithClash.UI
{
    /// <summary>
    /// Tela de carregamento: fundo com imagens que se alternam, dica, título da cena e barra de progresso até 100%.
    /// A cena nova só é ativada depois que o carregamento chega a 100%.
    /// </summary>
    public class TelaCarregamento : MonoBehaviour
    {
        static TelaCarregamento instancia;
        public static bool Ocupada => instancia != null && instancia.ocupada;

        static readonly string[] Dicas =
        {
            "SPACE no ar contra uma parede: corra na parede e salte de uma parede para a outra.",
            "Segure Shift para correr. Obstáculos baixos são transpostos sozinhos, sem perder a velocidade.",
            "Aperte Q perto de um inimigo para focar nele — a postura de combate só aparece com o alvo travado.",
            "Ctrl correndo faz uma rasteira por baixo de barras e vãos baixos.",
            "Equipe até 4 poderes no menu (I) e troque entre eles com as teclas 2 a 5.",
            "Segure E para concentrar Fé. Poderes maiores pedem mais carga.",
            "No Cofre do menu principal você muda skins, itens e cosméticos do seu personagem.",
            "Esc abre a pausa: opções, poderes, personagem e voltar ao menu.",
        };

        Canvas canvas;
        CanvasGroup grupo;
        RawImage fundoA, fundoB;
        Text titulo, dica, percentual, rotuloEstado;
        RectTransform barraPreenchida;
        Texture2D[] imagens;
        bool ocupada;

        internal static void ResetarEstatico() { instancia = null; }

        public static void Carregar(string cena)
        {
            if (instancia == null)
            {
                var go = new GameObject("Tela de Carregamento");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<TelaCarregamento>();
                instancia.Montar();
            }
            if (instancia.ocupada) return;
            instancia.StartCoroutine(instancia.Rotina(cena));
        }

        // ================================================================== montagem
        void Montar()
        {
            imagens = Resources.LoadAll<Texture2D>("Carregamento");
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(es);
            }
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 0.5f;
            grupo = go.GetComponent<CanvasGroup>();

            var preto = UIKit.Painel(go.transform, "Preto", Color.black, false); UIKit.Esticar(preto.rectTransform);
            fundoA = NovaImagem(go.transform, "Fundo A"); fundoB = NovaImagem(go.transform, "Fundo B");
            fundoB.color = new Color(1, 1, 1, 0);

            var logoTex = Resources.Load<Texture2D>("Menu/logo");
            if (logoTex != null)
            {
                var logo = new GameObject("Logo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                logo.transform.SetParent(go.transform, false); logo.texture = logoTex; logo.raycastTarget = false;
                UIKit.Ancorar(logo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -330), new Vector2(630, -30));
            }
            // véu escuro embaixo para o texto ler bem
            var veu = UIKit.Painel(go.transform, "Véu", new Color(0.01f, 0.01f, 0.03f, 0.78f), false);
            UIKit.Ancorar(veu.rectTransform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 250));

            titulo = UIKit.Rotulo(go.transform, "", 54, UIKit.Ouro, TextAnchor.LowerLeft, FontStyle.Bold);
            UIKit.Ancorar(titulo.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(110, 160), new Vector2(-110, 235));
            dica = UIKit.Rotulo(go.transform, "", 26, UIKit.Texto, TextAnchor.UpperLeft);
            UIKit.Ancorar(dica.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(110, 95), new Vector2(-300, 150));

            var trilho = UIKit.Painel(go.transform, "Barra", new Color(1, 1, 1, 0.15f), false);
            UIKit.Ancorar(trilho.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(110, 62), new Vector2(-110, 70));
            var cheio = UIKit.Painel(trilho.transform, "Preenchimento", UIKit.Ouro, false);
            barraPreenchida = cheio.rectTransform;
            barraPreenchida.anchorMin = Vector2.zero; barraPreenchida.anchorMax = new Vector2(0, 1); barraPreenchida.offsetMin = barraPreenchida.offsetMax = Vector2.zero;
            percentual = UIKit.Rotulo(go.transform, "0%", 34, UIKit.Ouro, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Ancorar(percentual.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 90), new Vector2(-110, 150));
            rotuloEstado = UIKit.Rotulo(go.transform, "CARREGANDO", 16, UIKit.TextoFraco, TextAnchor.LowerLeft);
            UIKit.Ancorar(rotuloEstado.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(110, 30), new Vector2(-110, 58));

            grupo.alpha = 0f; go.SetActive(false);
        }

        static RawImage NovaImagem(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(pai, false);
            var ri = go.GetComponent<RawImage>(); ri.raycastTarget = false;
            UIKit.Esticar(ri.rectTransform);
            return ri;
        }

        // ================================================================== carregamento
        IEnumerator Rotina(string cena)
        {
            ocupada = true;
            Time.timeScale = 1f; EstadoJogo.MenuAberto = false;
            canvas.gameObject.SetActive(true);
            titulo.text = Cenas.TituloDe(cena);
            dica.text = "DICA  —  " + Dicas[Random.Range(0, Dicas.Length)];
            percentual.text = "0%"; barraPreenchida.anchorMax = new Vector2(0, 1);
            rotuloEstado.text = "CARREGANDO";
            int atual = imagens.Length > 0 ? Random.Range(0, imagens.Length) : -1;
            fundoA.texture = atual >= 0 ? imagens[atual] : null; fundoA.color = atual >= 0 ? Color.white : new Color(0.05f, 0.06f, 0.1f);
            fundoB.color = new Color(1, 1, 1, 0);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = false;

            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { grupo.alpha = t / 0.4f; yield return null; }
            grupo.alpha = 1f;

            var op = SceneManager.LoadSceneAsync(cena);
            if (op == null) { rotuloEstado.text = "CENA NÃO ENCONTRADA: " + cena; yield return new WaitForSecondsRealtime(2f); Fechar(); yield break; }
            op.allowSceneActivation = false;

            float inicio = Time.unscaledTime, mostrado = 0f, troca = Time.unscaledTime + 4f;
            const float minimo = 2.6f;
            while (true)
            {
                float real = Mathf.Clamp01(op.progress / 0.9f);
                float tempo = Mathf.Clamp01((Time.unscaledTime - inicio) / minimo);
                float alvo = Mathf.Min(real, tempo);               // a barra nunca passa do tempo mínimo nem do progresso real
                mostrado = Mathf.MoveTowards(mostrado, alvo, Time.unscaledDeltaTime * 0.6f);
                barraPreenchida.anchorMax = new Vector2(mostrado, 1);
                percentual.text = Mathf.FloorToInt(mostrado * 100f) + "%";
                if (Time.unscaledTime > troca && imagens.Length > 1) { troca = Time.unscaledTime + 4f; StartCoroutine(TrocarImagem(atual = (atual + 1) % imagens.Length)); }
                if (mostrado >= 1f && op.progress >= 0.9f) break;
                yield return null;
            }
            percentual.text = "100%"; rotuloEstado.text = "PRONTO";
            yield return new WaitForSecondsRealtime(0.35f);
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            // deixa a cena iniciar (Awake/Start, shaders) antes de revelar
            for (int i = 0; i < 20; i++) yield return null;
            Time.timeScale = 1f;
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { grupo.alpha = 1f - t / 0.5f; yield return null; }
            Fechar();
        }

        IEnumerator TrocarImagem(int indice)
        {
            fundoB.texture = imagens[indice];
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime) { fundoB.color = new Color(1, 1, 1, t); yield return null; }
            fundoA.texture = imagens[indice]; fundoA.color = Color.white; fundoB.color = new Color(1, 1, 1, 0);
        }

        void Fechar()
        {
            canvas.gameObject.SetActive(false);
            ocupada = false;
            var sceneAtual = SceneManager.GetActiveScene().name;
            bool jogo = sceneAtual != Cenas.Menu && sceneAtual != Cenas.Boot;
            Cursor.lockState = jogo ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !jogo;
        }
    }
}
