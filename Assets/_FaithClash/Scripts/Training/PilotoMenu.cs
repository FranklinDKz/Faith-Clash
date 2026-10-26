#if UNITY_EDITOR
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FaithClash.Core;
using FaithClash.UI;

namespace FaithClash.Training
{
    /// <summary>Teste automÃ¡tico do fluxo de menus (env FC_TESTE_MENU=1): carregamento â†’ menu â†’ opÃ§Ãµes/cofre â†’ praticar â†’ pausa â†’ voltar. Escreve Logs/teste_menu.txt e imagens Logs/menu_*.png.</summary>
    public class PilotoMenu : MonoBehaviour
    {
        readonly StringBuilder sb = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            if (System.Environment.GetEnvironmentVariable("FC_TESTE_MENU") != "1") return;
            var go = new GameObject("PilotoMenu"); DontDestroyOnLoad(go); go.AddComponent<PilotoMenu>();
        }

        void L(string t) { sb.AppendLine(t); Debug.Log("[PilotoMenu] " + t); }

        IEnumerator Foto(string nome)
        {
            yield return null;
            var camGo = new GameObject("CamFoto"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.cullingMask = ~0;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var canvases = new System.Collections.Generic.List<(Canvas c, RenderMode m)>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { canvases.Add((c, c.renderMode)); c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 1f; }
            Canvas.ForceUpdateCanvases();
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("Logs/menu_" + nome + ".png", tex.EncodeToPNG());
            foreach (var (c, m) in canvases) if (c != null) { c.renderMode = m; }
            Destroy(tex); rt.Release(); Destroy(camGo);
            yield return null;
        }

        IEnumerator EsperarCena(string cena, float limite)
        {
            float t = 0;
            while (SceneManager.GetActiveScene().name != cena && t < limite) { t += Time.unscaledDeltaTime; yield return null; }
            L($"cena ativa = {SceneManager.GetActiveScene().name} apÃ³s {t:0.0}s (carregando={Cenas.Carregando})");
            while (Cenas.Carregando && t < limite + 10) { t += Time.unscaledDeltaTime; yield return null; }
        }

        void Tecla(Key k)
        {
            var kb = Keyboard.current;
            InputSystem.QueueStateEvent(kb, new KeyboardState(k)); InputSystem.Update();
            InputSystem.QueueStateEvent(kb, new KeyboardState()); InputSystem.Update();
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            L("cena inicial = " + SceneManager.GetActiveScene().name);
            yield return Foto("1_carregando_inicio");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Foto("2_carregando_meio");
            yield return EsperarCena("MenuPrincipal", 20);
            yield return new WaitForSecondsRealtime(2f);
            yield return Foto("3_menu");
            var m = MenuPrincipal.Atual;
            L("MenuPrincipal.Atual existe = " + (m != null));
            m.TesteAcionar("opcoes"); yield return new WaitForSecondsRealtime(0.5f); yield return Foto("4_opcoes");
            m.TesteAcionar("aba2"); yield return new WaitForSecondsRealtime(0.4f); yield return Foto("5_opcoes_jogabilidade");
            m.TesteAcionar("voltar"); yield return null;
            m.TesteAcionar("cofre"); yield return new WaitForSecondsRealtime(0.5f); yield return Foto("6_cofre");
            m.TesteAcionar("aba3"); yield return new WaitForSecondsRealtime(0.4f); yield return Foto("7_cofre_editar");
            m.TesteAcionar("voltar"); yield return null;
            m.TesteAcionar("praticar");
            yield return new WaitForSecondsRealtime(1.5f); yield return Foto("8_carregando_treino");
            yield return EsperarCena("Treino", 30);
            yield return new WaitForSecondsRealtime(3f);
            yield return Foto("9_treino");
            L("timeScale no jogo = " + Time.timeScale + ", cursor travado = " + (Cursor.lockState == CursorLockMode.Locked));
            MenuJogo.TestePausa(); yield return new WaitForSecondsRealtime(0.6f);
            L("pausa: MenuAberto = " + EstadoJogo.MenuAberto + ", timeScale = " + Time.timeScale);
            yield return Foto("10_pausa");
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var t = b.GetComponentInChildren<Text>();
                if (t != null && t.text == "VOLTAR AO MENU PRINCIPAL") { b.onClick.Invoke(); L("clicou em voltar ao menu"); break; }
            }
            yield return new WaitForSecondsRealtime(1f);
            yield return EsperarCena("MenuPrincipal", 30);
            yield return new WaitForSecondsRealtime(1.5f);
            L("de volta: cena = " + SceneManager.GetActiveScene().name + ", timeScale = " + Time.timeScale + ", MenuAberto = " + EstadoJogo.MenuAberto);
            yield return Foto("11_menu_de_volta");
            Fim();
        }

        void Fim()
        {
            System.IO.File.WriteAllText("Logs/teste_menu.txt", sb.ToString());
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
#endif

