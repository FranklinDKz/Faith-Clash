using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>Nível, Fé (energia), cargas espirituais, itens rápidos (Z, X, B, V) e meditação (segurar E) para recuperar Fé.</summary>
    public class StatusJogador : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public string nome;
            public string tecla;
            public int quantidade;
            public string icone;
        }

        [SerializeField] int nivel = 25;
        [SerializeField] string titulo = "Portador da Fé";
        [SerializeField] float feMaxima = 980f;
        [SerializeField] float feInicial = 620f;
        [SerializeField] float regeneracaoFe = 14f;
        [SerializeField] float fePorSegundoMeditando = 110f;
        [SerializeField] int acertosPorCarga = 8;

        public Item[] Itens =
        {
            new Item { nome = "Poção de Vida", tecla = "Z", quantidade = 18, icone = "item_pocao" },
            new Item { nome = "Cristal de Fé", tecla = "X", quantidade = 7, icone = "item_cristal" },
            new Item { nome = "Lanterna Sagrada", tecla = "B", quantidade = 3, icone = "item_lanterna" },
            new Item { nome = "Frasco de Fúria", tecla = "V", quantidade = 2, icone = "item_frasco" },
        };

        Health vida;
        MeleeCombat combate;
        AnimadorProtagonista animador;
        PlayerController controle;
        ParticleSystem auraMeditacao;
        float furiaAte, proximoGesto;
        GameObject vfxMeditar;
        int acertosAcumulados;
        GameObject lanterna;

        public int Nivel => nivel;
        public string Titulo => titulo;
        public float Fe { get; private set; }
        public float FeMaxima => feMaxima;
        public int Cargas { get; private set; } = 1;
        public int Escudos { get; private set; } = 2;
        public bool Meditando { get; private set; }
        public bool FuriaAtiva => Time.time < furiaAte;
        public Health Vida => vida;

        public event Action<string> Aviso;

        void Awake()
        {
            vida = GetComponent<Health>();
            combate = GetComponent<MeleeCombat>();
            animador = GetComponent<AnimadorProtagonista>();
            controle = GetComponent<PlayerController>();
            Fe = feInicial;
        }

        void OnEnable()
        {
            if (combate != null) combate.HitLanded += AoAcertar;
            if (vida != null) vida.Damaged += AoSerAtingido;
        }

        void OnDisable()
        {
            if (combate != null) combate.HitLanded -= AoAcertar;
            if (vida != null) vida.Damaged -= AoSerAtingido;
        }

        void AoSerAtingido(float dano, Vector3 ponto, GameObject origem) => PararMeditacao();

        void AoAcertar(float dano)
        {
            if (Cargas >= 3) return;
            if (++acertosAcumulados >= acertosPorCarga)
            {
                acertosAcumulados = 0;
                Cargas++;
            }
        }

        public void Avisar(string msg) => Aviso?.Invoke(msg);

        public bool GastarFe(float custo)
        {
            if (Fe < custo) { Aviso?.Invoke("Fé insuficiente"); return false; }
            Fe -= custo;
            return true;
        }

        public void GanharFe(float valor) => Fe = Mathf.Min(feMaxima, Fe + valor);

        public bool GastarCarga()
        {
            if (Cargas <= 0) { Aviso?.Invoke("Sem cargas espirituais"); return false; }
            Cargas--;
            return true;
        }

        void Update()
        {
            GanharFe(regeneracaoFe * Time.deltaTime);
            if (combate != null) combate.DamageMultiplier = FuriaAtiva ? 1.5f : 1f;

            var k = Keyboard.current;
            if (k == null) return;
            Meditar(k);
            if (k.zKey.wasPressedThisFrame) Usar(0);
            if (k.xKey.wasPressedThisFrame) Usar(1);
            if (k.bKey.wasPressedThisFrame) Usar(2);
            if (k.vKey.wasPressedThisFrame) Usar(3);
        }

        // ------------------------------------------------------------------ meditação (segurar E)
        /// <summary>Segurar E: fica parado em concentração e recupera Fé bem mais rápido. Qualquer movimento, golpe, pulo ou dano interrompe.</summary>
        void Meditar(Keyboard k)
        {
            bool segurando = k.eKey.isPressed && Cursor.lockState == CursorLockMode.Locked;
            bool podeIniciar = controle != null && controle.IsGrounded && !controle.IsBusy && !controle.Atordoado
                               && (combate == null || (!combate.IsAttacking && !combate.IsBlocking)) && (animador == null || !animador.OcupadoComAcao)
                               && (vida == null || !vida.IsDead);
            if (!Meditando)
            {
                if (!segurando || !podeIniciar) return;
                if (Fe >= feMaxima - 1f) { if (k.eKey.wasPressedThisFrame) Aviso?.Invoke("Fé cheia"); return; }
                Meditando = true;
                proximoGesto = 0f;
                SomJogo.Tocar("Poder_Ativar", transform.position, 0.45f, 0.7f, duracaoMax: 1.4f);
                CriarAuraMeditacao();
                vfxMeditar = CatalogoVFX.Tocar("meditar", transform.position, Quaternion.identity, transform, 1f, 120f);
                CatalogoVFX.Tocar("cura_circulo", transform.position, Quaternion.identity, null, 1f, 4f);
            }

            bool moveu = k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed || k.spaceKey.isPressed
                         || k.cKey.isPressed || k.leftCtrlKey.isPressed || (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed));
            if (!segurando || moveu || controle == null || !controle.IsGrounded || controle.IsBusy || (combate != null && combate.IsAttacking))
            {
                PararMeditacao(!moveu && controle != null && controle.IsGrounded && !controle.IsBusy);   // soltou o E parado: levanta com calma
                return;
            }

            GanharFe(fePorSegundoMeditando * Time.deltaTime);
            if (Fe >= feMaxima - 1f) { PararMeditacao(); Aviso?.Invoke("Fé restaurada"); return; }
            if (proximoGesto == 0f)
            {
                animador?.Acao("MeditarEntrar", 999f, 0.25f);   // senta e segue sozinho para o ciclo de meditação
                proximoGesto = 1f;
                SomJogo.Tocar("UI_Elemento", transform.position, 0.25f, 0.6f);
            }
            if (auraMeditacao != null) auraMeditacao.transform.position = transform.position + Vector3.up * 0.1f;
        }

        void PararMeditacao(bool levantar = false)
        {
            if (!Meditando) return;
            Meditando = false;
            if (vfxMeditar != null) Destroy(vfxMeditar);
            if (auraMeditacao != null) { auraMeditacao.Stop(true, ParticleSystemStopBehavior.StopEmitting); Destroy(auraMeditacao.gameObject, 2f); auraMeditacao = null; }
            if (levantar) animador?.Acao("MeditarSair", 0.9f, 0.2f, 1.15f);
            else animador?.Encerrar(0.25f);
        }

        void CriarAuraMeditacao()
        {
            if (auraMeditacao != null) return;
            auraMeditacao = Efeitos.Rajada(transform.position, Quaternion.Euler(-90f, 0f, 0f), new Color(0.45f, 0.75f, 1f), 1, new Vector2(0.4f, 1.3f),
                new Vector2(0.05f, 0.14f), new Vector2(0.8f, 1.6f), ParticleSystemShapeType.Circle, 0.9f, -0.35f, autoDestruir: false);
            auraMeditacao.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = auraMeditacao.main;
            main.duration = 1f;
            main.loop = true;
            var em = auraMeditacao.emission;
            em.SetBursts(new ParticleSystem.Burst[0]);
            em.rateOverTime = 60f;
            auraMeditacao.Play();
        }
        void Usar(int i)
        {
            PararMeditacao();
            var item = Itens[i];
            if (item.quantidade <= 0) { Aviso?.Invoke(item.nome + " esgotado"); return; }
            item.quantidade--;
            GetComponent<AnimadorProtagonista>()?.Acao(i <= 1 ? "Curar" : i == 3 ? "PowerUp" : "PegarItem", i <= 1 ? 1.33f : i == 3 ? 1.1f : 0.83f, 0.08f);
            StartCoroutine(AplicarItemDepois(i, item));
        }

        System.Collections.IEnumerator AplicarItemDepois(int i, Item item)
        {
            yield return new WaitForSeconds(i <= 1 ? 0.7f : 0.4f);
            Vector3 p = transform.position + Vector3.up;
            string[] sons = { "UI_Aviso", "Poder_Ativar", "UI_Clique", "Fogo_Magia" };
            SomJogo.Tocar(sons[i], transform.position, 0.7f, i == 1 ? 1.25f : 1f, duracaoMax: 1.2f);
            switch (i)
            {
                case 0:
                    if (vida != null) vida.Heal(450f);
                    Efeitos.Rajada(p, Quaternion.identity, new Color(1f, 0.3f, 0.3f), 30, new Vector2(1, 3), new Vector2(0.1f, 0.25f), new Vector2(0.5f, 1f), raio: 0.5f, gravidade: -0.3f);
                    break;
                case 1:
                    GanharFe(300f);
                    Efeitos.Rajada(p, Quaternion.identity, new Color(0.4f, 0.7f, 1f), 30, new Vector2(1, 3), new Vector2(0.1f, 0.25f), new Vector2(0.5f, 1f), raio: 0.5f, gravidade: -0.3f);
                    break;
                case 2:
                    if (lanterna == null)
                    {
                        lanterna = new GameObject("Lanterna Sagrada");
                        lanterna.transform.SetParent(transform, false);
                        lanterna.transform.localPosition = new Vector3(0.4f, 1.8f, 0.2f);
                        var l = lanterna.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.8f, 0.5f);
                        l.range = 12f;
                        l.intensity = 3f;
                    }
                    Destroy(lanterna, 30f);
                    break;
                case 3:
                    furiaAte = Time.time + 10f;
                    Efeitos.Rajada(p, Quaternion.identity, new Color(1f, 0.5f, 0.1f), 40, new Vector2(2, 4), new Vector2(0.1f, 0.3f), new Vector2(0.4f, 0.8f), raio: 0.6f);
                    break;
            }
            Aviso?.Invoke(item.nome + " usado");
        }
    }
}
