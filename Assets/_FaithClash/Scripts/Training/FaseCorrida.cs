using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.UI;

namespace FaithClash.Training
{
    /// <summary>
    /// Fase 1 — corrida contra o tempo: atravesse o percurso de obstáculos (só com SPACE: vault, subida, escalada, deslize, pulos) e chegue ao dojo
    /// antes que o tempo acabe. Checkpoints dão tempo extra e salvam o ponto de retorno; cair no abismo volta ao último checkpoint com uma penalidade.
    /// </summary>
    public class FaseCorrida : MonoBehaviour
    {
        [SerializeField] PlayerController jogador;
        [SerializeField] float tempoLimite = 110f;
        [SerializeField] float bonusCheckpoint = 6f;
        [SerializeField] float penalidadeQueda = 4f;
        [SerializeField] float yAbismo = -3f;
        [SerializeField] Transform[] checkpoints;
        [SerializeField] Transform chegada;
        [SerializeField] float larguraChegada = 3.2f;

        public static FaseCorrida Instancia { get; private set; }
        public float TempoRestante { get; private set; }
        public float TempoDecorrido { get; private set; }
        public bool Iniciou { get; private set; }
        public bool Venceu { get; private set; }
        public bool Perdeu { get; private set; }
        public int CheckpointAtual { get; private set; } = -1;
        public int Quedas { get; private set; }

        Text txtTempo, txtBanner, txtAviso, txtDica;
        float avisoAte, reinicioEm, flashAte, alertaEm;
        Image flash;
        Font fonte;
        Vector3 inicio;

        void Awake()
        {
            Instancia = this;
            RegrasFase.PoderesBloqueados = true; RegrasFase.SemMapa = true; RegrasFase.SemItens = true;
            SomJogo.AmbienteCaverna(true);
            TempoRestante = tempoLimite;
            fonte = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" }, 32);
            MontarUI();
        }

        void OnDestroy() { if (Instancia == this) { Instancia = null; RegrasFase.Limpar(); } }

        void Start()
        {
            if (jogador == null) jogador = FindAnyObjectByType<PlayerController>();
            inicio = jogador != null ? jogador.transform.position : Vector3.zero;
            if (jogador != null)
            {
                // lanterna do herói: mantém o personagem visível mesmo longe das tochas
                var luz = new GameObject("Lanterna do jogador").AddComponent<Light>();
                luz.transform.SetParent(jogador.transform, false); luz.transform.localPosition = new Vector3(0f, 2.4f, -0.4f);
                luz.color = new Color(1f, 0.86f, 0.62f); luz.range = 8f; luz.intensity = 1.6f; luz.shadows = LightShadows.None;
            }
            var hud = FindAnyObjectByType<HUDJogo>();
            hud?.DefinirMissao("Caverna do Eco", "Fase 1: Rumo ao Dojo", "Chegue ao Sensei antes que o tempo acabe");
            Aviso("Corra pela caverna! Seu Sensei espera no dojo. Poderes bloqueados.", 4f);
        }

        void MontarUI()
        {
            var go = new GameObject("UI da Fase", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            txtTempo = Texto(go.transform, "Tempo", 68, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(600, 100), TextAnchor.MiddleCenter, FontStyle.Bold);
            txtAviso = Texto(go.transform, "Aviso", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1200, 60), TextAnchor.MiddleCenter, FontStyle.Bold);
            txtBanner = Texto(go.transform, "Banner", 54, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1500, 260), TextAnchor.MiddleCenter, FontStyle.Bold);
            var fo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            fo.transform.SetParent(go.transform, false);
            var frt = fo.GetComponent<RectTransform>(); frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            flash = fo.GetComponent<Image>(); flash.color = new Color(0.8f, 0f, 0f, 0f); flash.raycastTarget = false;
            txtDica = Texto(go.transform, "Dica", 22, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1500, 40), TextAnchor.MiddleCenter, FontStyle.Normal);
            txtDica.text = "Shift: correr   SPACE: pular / vault / subir / escalar / deslizar por baixo   Ctrl: rolar";
            txtDica.color = new Color(1f, 1f, 1f, 0.75f);
        }

        Text Texto(Transform pai, string nome, int tamanho, Vector2 ancora, Vector2 pos, Vector2 tam, TextAnchor alin, FontStyle estilo)
        {
            var o = new GameObject(nome, typeof(RectTransform), typeof(Text), typeof(Shadow));
            o.transform.SetParent(pai, false);
            var rt = o.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = ancora; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            var t = o.GetComponent<Text>();
            t.font = fonte; t.fontSize = tamanho; t.alignment = alin; t.fontStyle = estilo;
            t.color = new Color(0.97f, 0.92f, 0.8f); t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var s = o.GetComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.9f); s.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        void Aviso(string msg, float segundos)
        {
            txtAviso.text = msg;
            avisoAte = Time.time + segundos;
        }

        void Update()
        {
            if (jogador == null) return;
            if (Venceu || Perdeu)
            {
                if (Time.time >= reinicioEm && (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Perdeu)))
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (!Iniciou && jogador.transform.position.z > inicio.z + 3f) { Iniciou = true; Aviso("Valendo!", 1.2f); }
            if (Iniciou)
            {
                TempoDecorrido += Time.deltaTime;
                TempoRestante -= Time.deltaTime;
                if (TempoRestante <= 0f) { Perder(); return; }
            }

            // checkpoints (na ordem)
            int prox = CheckpointAtual + 1;
            if (checkpoints != null && prox < checkpoints.Length && checkpoints[prox] != null && jogador.transform.position.z >= checkpoints[prox].position.z)
            {
                CheckpointAtual = prox;
                TempoRestante += bonusCheckpoint;
                Aviso($"Checkpoint {prox + 1}/{checkpoints.Length}!  +{bonusCheckpoint:0} s", 2.2f);
                SomJogo.Tocar("Checkpoint", null, 0.8f, 1f, 0f);
                CatalogoVFX.Tocar("cura_circulo", jogador.transform.position + Vector3.up * 0.05f, Quaternion.identity, null, 0.3f, 3f);
                CatalogoVFX.Tocar("faisca_amarela", jogador.transform.position + Vector3.up * 1f, Quaternion.identity, null, 1f, 2f);
            }

            // abismo
            if (jogador.transform.position.y < yAbismo) Cair();

            // chegada
            if (chegada != null && Vector3.Distance(new Vector3(jogador.transform.position.x, 0, jogador.transform.position.z), new Vector3(chegada.position.x, 0, chegada.position.z)) < larguraChegada) Vencer();

            // textos
            float t = Mathf.Max(0f, TempoRestante);
            txtTempo.text = $"{Mathf.FloorToInt(t / 60f):0}:{Mathf.FloorToInt(t % 60f):00}.{Mathf.FloorToInt((t * 10f) % 10f)}";
            if (t < 15f && Time.time > alertaEm) { alertaEm = Time.time + (t < 6f ? 0.5f : 1f); SomJogo.Tocar("Alerta_Tempo", null, 0.5f, 1f, 0f); }
            txtTempo.color = t < 15f ? Color.Lerp(new Color(1f, 0.25f, 0.2f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f)) : new Color(0.97f, 0.92f, 0.8f);
            if (Time.time > avisoAte) txtAviso.text = "";
            flash.color = new Color(0.8f, 0f, 0f, Mathf.Clamp01((flashAte - Time.time) / 0.7f) * 0.55f);
            if (Iniciou && txtDica.text.Length > 0 && TempoDecorrido > 12f) txtDica.text = "";
        }

        void Cair()
        {
            Quedas++;
            TempoRestante -= penalidadeQueda;
            Vector3 pos = CheckpointAtual >= 0 ? checkpoints[CheckpointAtual].position : inicio;
            jogador.Teleportar(pos + Vector3.up * 0.1f, Quaternion.identity);
            FindAnyObjectByType<ThirdPersonCamera>()?.DefinirYaw(0f);
            Aviso($"Empalado nos espinhos!  -{penalidadeQueda:0} s", 2.2f);
            flashAte = Time.time + 0.7f;
            CatalogoVFX.Tocar("poeira_nuvem", jogador.transform.position + Vector3.up * 0.2f, Quaternion.identity, null, 1f, 2.5f);
            CatalogoVFX.Tocar("terra_respingo", jogador.transform.position, Quaternion.identity, null, 1f, 2.5f);
            SomJogo.Tocar("Espinho", jogador.transform.position, 1f, 1f, 0.05f);
            SomJogo.Tocar("Morte_Jogador", jogador.transform.position, 0.5f, 1.3f, duracaoMax: 1.2f);
            TremorGlobal.Disparar(0.9f);
        }

        void Vencer()
        {
            Venceu = true;
            reinicioEm = Time.time + 1.5f;
            jogador.Congelado = true;
            string nota = TempoDecorrido < tempoLimite * 0.45f ? "SENSEI: \"Impressionante. Você voou até aqui!\"" : TempoDecorrido < tempoLimite * 0.7f ? "SENSEI: \"Chegou a tempo. Vamos ao treinamento.\"" : "SENSEI: \"Por pouco... mas chegou. Vamos ao treinamento.\"";
            txtBanner.text = $"CHEGOU A TEMPO!\nTempo: {TempoDecorrido:0.0} s   Quedas: {Quedas}\n<size=30>{nota}\nEnter: jogar de novo</size>";
            txtBanner.supportRichText = true;
            SomJogo.Tocar("Vitoria", null, 0.9f, 1f, 0f);
            // fogos de artifício no dojo (azul, verde e amarelo) + brilho do Sensei
            Vector3 c = chegada != null ? chegada.position + Vector3.forward * 4f : jogador.transform.position + Vector3.forward * 4f;
            CatalogoVFX.Tocar("fogos_azul", c + new Vector3(-4f, 6f, 0f), Quaternion.identity, null, 1f, 6f);
            CatalogoVFX.Tocar("fogos_verde", c + new Vector3(4f, 7f, 0f), Quaternion.identity, null, 1f, 6f);
            CatalogoVFX.Tocar("fogos_amarelo", c + new Vector3(0f, 8f, 1f), Quaternion.identity, null, 1f, 6f);
            CatalogoVFX.Tocar("poder_estrela", jogador.transform.position + Vector3.up * 0.1f, Quaternion.identity, null, 1f, 4f);
        }

        void Perder()
        {
            Perdeu = true;
            reinicioEm = Time.time + 4f;
            jogador.Congelado = true;
            txtBanner.supportRichText = true;
            txtBanner.text = "O TEMPO ACABOU!\n<size=30>O Sensei já foi embora... Tentando de novo.</size>";
            SomJogo.Tocar("Morte_Jogador", jogador.transform.position, 0.6f, 1f, duracaoMax: 2f);
        }
    }
}
