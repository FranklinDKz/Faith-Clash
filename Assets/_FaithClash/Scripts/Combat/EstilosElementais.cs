using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.Combat
{
    public enum Elemento { Fogo, Agua, Terra, Ar }

    /// <summary>
    /// Modo Poder: jutsus inspirados em Naruto e técnicas amaldiçoadas inspiradas em Jujutsu Kaisen.
    /// 1-Fogo (Katon), 2-Água (Suiton), 3-Terra (Doton), 4-Ar (Fuuton). Os jutsus saem no 3º e 4º golpe do combo
    /// ou segurando o botão esquerdo (Terra/Ar). R solta o Roxo (Técnica Reversa) gastando uma carga espiritual.
    /// O golpe pesado tem chance de Faísca Negra (Kokusen). Água → Fogo → Terra → Ar em sequência causa a Explosão Elemental.
    /// </summary>
    public class EstilosElementais : MonoBehaviour
    {
        [SerializeField] float tempoSegurar = 0.45f;
        [SerializeField] float custo3x = 60f, custo4x = 100f, custoSegurar = 150f;
        [SerializeField, Range(0f, 1f)] float chanceFaiscaNegra = 0.3f;

        public static readonly Color[] Cores =
        {
            new Color(1f, 0.45f, 0.08f),
            new Color(0.25f, 0.65f, 1f),
            new Color(0.85f, 0.65f, 0.3f),
            new Color(0.75f, 0.55f, 1f),
        };

        MeleeCombat combate;
        StatusJogador status;
        AnimadorProtagonista animador;
        ModoCombate modo;
        Animator animator;
        Transform mao;
        ParticleSystem aura;
        Light luzAura;
        float segurandoDesde = -1f;
        bool tecnicaSegurarUsada;
        readonly List<(Elemento e, float t)> historico = new List<(Elemento, float)>();

        public Elemento Atual { get; private set; } = Elemento.Fogo;
        public event Action<Elemento> ElementoMudou;
        public event Action<string, Color> TecnicaUsada;
        internal MeleeCombat Combate => combate;

        bool EmPoder => modo == null || modo.Atual == Modo.Poder;

        void Awake()
        {
            combate = GetComponent<MeleeCombat>();
            status = GetComponent<StatusJogador>();
            animador = GetComponent<AnimadorProtagonista>();
            modo = GetComponent<ModoCombate>();
            animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (animator != null && animator.isHuman) mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
            CriarAura();
            Selecionar(Elemento.Fogo);
        }

        void OnEnable()
        {
            if (combate != null) combate.AttackImpact += AoImpacto;
            if (modo != null) modo.Mudou += AoMudarModo;
        }

        void OnDisable()
        {
            if (combate != null) combate.AttackImpact -= AoImpacto;
            if (modo != null) modo.Mudou -= AoMudarModo;
        }

        void AoMudarModo(Modo m)
        {
            if (aura != null)
            {
                if (m == Modo.Poder) aura.Play(); else aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (luzAura != null) luzAura.enabled = m == Modo.Poder;
        }

        public void Selecionar(Elemento e)
        {
            Atual = e;
            if (aura != null)
            {
                var main = aura.main;
                main.startColor = new ParticleSystem.MinMaxGradient(Cores[(int)e], Color.Lerp(Cores[(int)e], Color.white, 0.6f));
            }
            if (luzAura != null) luzAura.color = Cores[(int)e];
            ElementoMudou?.Invoke(e);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && Cursor.lockState == CursorLockMode.Locked)
            {
                if (k.digit1Key.wasPressedThisFrame) Escolher(Elemento.Fogo);
                if (k.digit2Key.wasPressedThisFrame) Escolher(Elemento.Agua);
                if (k.digit3Key.wasPressedThisFrame) Escolher(Elemento.Terra);
                if (k.digit4Key.wasPressedThisFrame) Escolher(Elemento.Ar);
                if (k.rKey.wasPressedThisFrame)
                {
                    if (!EmPoder) status?.Avisar("Guarde a espada (G) para usar poderes");
                    else if (status != null && status.GastarCarga()) Roxo();
                }
            }

            var m = Mouse.current;
            if (m == null) return;
            if (m.leftButton.wasPressedThisFrame) { segurandoDesde = Time.time; tecnicaSegurarUsada = false; }
            if (EmPoder && m.leftButton.isPressed && segurandoDesde > 0f && !tecnicaSegurarUsada && Time.time - segurandoDesde > tempoSegurar)
            {
                tecnicaSegurarUsada = true;
                if (Atual == Elemento.Terra) TentarTecnica(custoSegurar, MuralhaDeTerra);
                else if (Atual == Elemento.Ar) TentarTecnica(custoSegurar, Reppusho);
            }
            if (m.leftButton.wasReleasedThisFrame) segurandoDesde = -1f;
        }

        void Escolher(Elemento e)
        {
            modo?.Definir(Modo.Poder);
            Selecionar(e);
        }

        void LateUpdate()
        {
            if (aura == null || mao == null) return;
            aura.transform.position = mao.position;
            if (luzAura) luzAura.intensity = 1.4f + Mathf.Sin(Time.time * 9f) * 0.3f;
        }

        void CriarAura()
        {
            var go = new GameObject("Aura da Mão");
            aura = go.AddComponent<ParticleSystem>();
            aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = aura.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.gravityModifier = -0.15f;
            var em = aura.emission;
            em.rateOverTime = 90f;
            var sh = aura.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.11f;
            var col = aura.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Efeitos.MaterialAditivo;

            var lgo = new GameObject("Luz da Aura");
            lgo.transform.SetParent(go.transform, false);
            luzAura = lgo.AddComponent<Light>();
            luzAura.type = LightType.Point;
            luzAura.range = 3.5f;
            luzAura.intensity = 1.4f;
            if (modo != null && modo.Atual != Modo.Poder) { aura.Stop(); luzAura.enabled = false; }
            else aura.Play();
        }

        void AoImpacto(int passo, bool acertou)
        {
            if (!EmPoder) return;

            if (passo == 0)
            {
                if (acertou && UnityEngine.Random.value < chanceFaiscaNegra) FaiscaNegra();
                return;
            }
            if (passo == 3)
            {
                switch (Atual)
                {
                    case Elemento.Fogo: TentarTecnica(custo3x, GoukakyuuNoJutsu); break;
                    case Elemento.Agua: TentarTecnica(custo3x, SuiryuudanNoJutsu); break;
                    case Elemento.Terra: TentarTecnica(custo3x, DoryuudanNoJutsu); break;
                    case Elemento.Ar: TentarTecnica(custo3x * 0.5f, DaitoppaNoJutsu); break;
                }
            }
            else if (passo == 4)
            {
                switch (Atual)
                {
                    case Elemento.Fogo: TentarTecnica(custo4x, HousenkaNoJutsu); break;
                    case Elemento.Agua: TentarTecnica(custo4x, SuirouNoJutsu); break;
                    case Elemento.Ar: TentarTecnica(custo4x, Azul); break;
                }
            }
        }

        void TentarTecnica(float custo, Action tecnica)
        {
            if (status != null && !status.GastarFe(custo)) return;
            tecnica();
            RegistrarSequencia(Atual);
        }

        void RegistrarSequencia(Elemento e)
        {
            historico.Add((e, Time.time));
            historico.RemoveAll(h => Time.time - h.t > 12f);
            int n = historico.Count;
            if (n >= 4 && historico[n - 4].e == Elemento.Agua && historico[n - 3].e == Elemento.Fogo &&
                historico[n - 2].e == Elemento.Terra && historico[n - 1].e == Elemento.Ar)
            {
                historico.Clear();
                ExplosaoElemental();
            }
        }

        // ------------------------------------------------------------------ utilidades
        Vector3 Frente(float dist) => transform.position + Vector3.up * 1.1f + transform.forward * dist;

        Vector3 DirecaoAlvo()
        {
            var trava = GetComponent<TravaAlvo>();
            if (trava != null && trava.Travado)
            {
                Vector3 v = trava.PontoFoco + Vector3.up * 0.2f - Frente(0f);
                if (v.sqrMagnitude > 0.01f) return v.normalized;
            }
            return transform.forward;
        }

        int Dano(Vector3 centro, float raio, float dano, float empurrao = 0f, float prender = 0f, bool puxar = false)
        {
            int n = 0;
            var vistos = new HashSet<IDamageable>();
            foreach (var col in Physics.OverlapSphere(centro, raio, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var alvo = col.GetComponentInParent<IDamageable>();
                if (alvo == null || alvo.IsDead || !vistos.Add(alvo)) continue;
                alvo.TakeDamage(dano * (combate != null ? combate.DamageMultiplier : 1f), col.ClosestPoint(centro), gameObject);
                var boneco = col.GetComponentInParent<TrainingDummy>();
                if (boneco != null)
                {
                    Vector3 dir = puxar ? (centro - boneco.transform.position) : (boneco.transform.position - centro);
                    if (empurrao > 0f) boneco.Empurrar(dir.normalized * empurrao);
                    if (prender > 0f) boneco.Prender(prender);
                }
                combate?.RegisterComboHit();
                n++;
            }
            return n;
        }

        void Anunciar(string nome) => TecnicaUsada?.Invoke(nome, Cores[(int)Atual]);

        void Disparar(Vector3 origem, Vector3 dir, ProjetilElemental.Opcoes o)
        {
            var go = new GameObject("Projétil");
            go.transform.SetPositionAndRotation(origem, Quaternion.LookRotation(dir));
            go.AddComponent<ProjetilElemental>().Configurar(this, o);
        }

        // ------------------------------------------------------------------ Katon (fogo)
        void GoukakyuuNoJutsu()
        {
            Anunciar("Katon: Goukakyuu no Jutsu");
            Disparar(Frente(1.2f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = Cores[0], velocidade = 14f, duracao = 1.3f, dano = 55f, empurrao = 9f, tamanho = 2.2f, raioImpacto = 4f, explodir = true, particulas = 260,
            });
        }

        void HousenkaNoJutsu()
        {
            Anunciar("Katon: Housenka no Jutsu");
            Vector3 d0 = DirecaoAlvo();
            for (int i = -2; i <= 2; i++)
            {
                Vector3 d = Quaternion.AngleAxis(i * 13f, Vector3.up) * d0;
                Disparar(Frente(1f), d, new ProjetilElemental.Opcoes
                {
                    cor = Cores[0], velocidade = 17f, duracao = 0.9f, dano = 20f, empurrao = 4f, tamanho = 0.9f, raioImpacto = 1.8f, explodir = true, particulas = 90,
                });
            }
        }

        // ------------------------------------------------------------------ Suiton (água)
        void SuiryuudanNoJutsu()
        {
            Anunciar("Suiton: Suiryuudan no Jutsu");
            Disparar(Frente(1f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = Cores[1], velocidade = 17f, duracao = 1.5f, dano = 45f, empurrao = 8f, tamanho = 1.5f, serpentear = true, particulas = 320,
            });
        }

        void SuirouNoJutsu()
        {
            Anunciar("Suiton: Suirou no Jutsu");
            TrainingDummy alvo = null;
            float melhor = 7f;
            foreach (var d in TrainingDummy.Todos)
            {
                float dist = Vector3.Distance(d.transform.position, transform.position);
                if (dist < melhor && !d.Vida.IsDead) { melhor = dist; alvo = d; }
            }
            Vector3 p = alvo != null ? alvo.transform.position : Frente(2.5f);
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(esfera.GetComponent<Collider>());
            esfera.name = "Prisão de Água";
            esfera.transform.position = p;
            var r = esfera.GetComponent<Renderer>();
            r.material = Efeitos.LitTransparente(new Color(0.3f, 0.65f, 1f, 0.38f), 0.95f);
            var t = esfera.AddComponent<EfeitoTemporario>();
            t.Configurar(2.6f);
            t.Escala(Vector3.one * 0.3f, Vector3.one * 2.6f);
            Efeitos.Rajada(p, Quaternion.identity, Cores[1], 80, new Vector2(0.5f, 2.5f), new Vector2(0.06f, 0.18f), new Vector2(1f, 2f), raio: 1.2f);
            if (alvo != null) Dano(alvo.transform.position, 0.8f, 30f, 0f, 2.5f);
        }

        // ------------------------------------------------------------------ Doton (terra)
        void DoryuudanNoJutsu()
        {
            Anunciar("Doton: Doryuudan no Jutsu");
            var mat = Efeitos.LitOpaco(new Color(0.45f, 0.35f, 0.22f), 0.15f);
            Vector3 frente = DirecaoAlvo(); frente.y = 0f; frente.Normalize();
            Vector3 lado = Vector3.Cross(Vector3.up, frente);
            for (int i = 0; i < 7; i++)
            {
                Vector3 p = transform.position + frente * (1.6f + i * 0.9f) + lado * UnityEngine.Random.Range(-0.5f, 0.5f);
                Espinho(p, UnityEngine.Random.Range(1.2f, 2.4f), mat, 1.4f + i * 0.04f);
                Dano(p + Vector3.up * 0.6f, 1.1f, 9f, 1.5f);
            }
            Efeitos.Rajada(transform.position + frente * 3f, Quaternion.identity, new Color(0.6f, 0.5f, 0.35f), 60, new Vector2(1, 4), new Vector2(0.1f, 0.3f), new Vector2(0.5f, 1f), ParticleSystemShapeType.Box, 1.5f, 0.6f, false);
        }

        void MuralhaDeTerra()
        {
            Anunciar("Doton: Doryuuheki");
            var mat = Efeitos.LitOpaco(new Color(0.45f, 0.35f, 0.22f), 0.15f);
            Vector3 centro = transform.position + transform.forward * 3f;
            for (int i = -6; i <= 6; i++)
            {
                Vector3 p = centro + transform.right * (i * 0.55f);
                var e = Espinho(p, UnityEngine.Random.Range(2.2f, 3.4f), mat, 6f);
                e.AddComponent<CapsuleCollider>();
            }
            Efeitos.Rajada(centro, Quaternion.identity, new Color(0.6f, 0.5f, 0.35f), 120, new Vector2(1, 5), new Vector2(0.15f, 0.4f), new Vector2(0.6f, 1.2f), ParticleSystemShapeType.Box, 3f, 0.8f, false);
            Dano(centro + Vector3.up, 3.2f, 20f, 6f);
        }

        GameObject Espinho(Vector3 pos, float altura, Material mat, float duracao)
        {
            var go = new GameObject("Espinho de Pedra");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-12f, 12f));
            go.transform.localScale = new Vector3(altura * 0.45f, altura, altura * 0.45f);
            go.AddComponent<MeshFilter>().sharedMesh = Efeitos.MalhaEspinho;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var t = go.AddComponent<EfeitoTemporario>();
            t.Configurar(duracao);
            t.Emergir(altura);
            return go;
        }

        // ------------------------------------------------------------------ Fuuton (vento)
        void DaitoppaNoJutsu()
        {
            Anunciar("Fuuton: Daitoppa");
            Vector3 p = Frente(1.5f);
            Efeitos.Rajada(p, Quaternion.LookRotation(transform.forward), Color.white, 120, new Vector2(8, 16), new Vector2(0.06f, 0.2f), new Vector2(0.25f, 0.5f), ParticleSystemShapeType.Cone, 0.7f);
            Dano(p + transform.forward * 1.5f, 3f, 14f, 9f);
        }

        void Reppusho()
        {
            Anunciar("Fuuton: Reppushou");
            Vector3 p = transform.position + Vector3.up;
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(esfera.GetComponent<Collider>());
            esfera.name = "Globo de Vento";
            esfera.transform.position = p;
            var r = esfera.GetComponent<Renderer>();
            r.material = Efeitos.LitTransparente(new Color(0.9f, 0.95f, 1f, 0.22f), 0.6f);
            var t = esfera.AddComponent<EfeitoTemporario>();
            t.Configurar(0.7f);
            t.Escala(Vector3.one * 0.5f, Vector3.one * 12f);
            t.Desbotar(r);
            Efeitos.Rajada(p, Quaternion.identity, Color.white, 220, new Vector2(8, 16), new Vector2(0.05f, 0.18f), new Vector2(0.3f, 0.6f), raio: 0.6f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.75f, 0.9f, 0.6f), 60, new Vector2(5, 10), new Vector2(0.08f, 0.16f), new Vector2(0.5f, 1f), raio: 1f, gravidade: 0.2f, aditivo: false);
            Dano(p, 6f, 15f, 14f);
        }

        // ------------------------------------------------------------------ técnicas amaldiçoadas (Jujutsu Kaisen)
        /// <summary>Azul: atrai os inimigos para um ponto à frente.</summary>
        void Azul()
        {
            TecnicaUsada?.Invoke("Técnica Maldita: Azul", new Color(0.25f, 0.55f, 1f));
            Vector3 p = Frente(3.2f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.25f, 0.55f, 1f), 200, new Vector2(1, 5), new Vector2(0.1f, 0.35f), new Vector2(0.6f, 1.2f), raio: 1.2f);
            Efeitos.Clarao(p, new Color(0.25f, 0.55f, 1f), 10f, 12f, 0.9f);
            Dano(p, 6f, 25f, 12f, 0.8f, true);
        }

        /// <summary>Roxo: esfera de energia que apaga tudo no caminho (gasta carga espiritual).</summary>
        void Roxo()
        {
            TecnicaUsada?.Invoke("Técnica Reversa: Roxo", new Color(0.7f, 0.3f, 1f));
            animador?.Acao("Martelo", 0.8f);
            Disparar(Frente(1.3f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = new Color(0.7f, 0.3f, 1f), velocidade = 22f, duracao = 1.2f, dano = 130f, empurrao = 14f, tamanho = 3f, raioImpacto = 6f, explodir = true, particulas = 320,
            });
        }

        /// <summary>Faísca Negra (Kokusen): golpe pesado perfeito causa dano extra.</summary>
        void FaiscaNegra()
        {
            TecnicaUsada?.Invoke("Faísca Negra!", new Color(1f, 0.15f, 0.2f));
            Vector3 p = Frente(1.8f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(1f, 0.1f, 0.15f), 90, new Vector2(4, 12), new Vector2(0.08f, 0.25f), new Vector2(0.3f, 0.6f), raio: 0.4f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.05f, 0.0f, 0.05f), 40, new Vector2(2, 6), new Vector2(0.3f, 0.8f), new Vector2(0.3f, 0.7f), raio: 0.5f, aditivo: false);
            Efeitos.Clarao(p, new Color(1f, 0.1f, 0.15f), 14f, 10f, 0.25f);
            Dano(p, 2.2f, 60f, 6f);
        }

        void ExplosaoElemental()
        {
            TecnicaUsada?.Invoke("Explosão Elemental", Color.white);
            Vector3 p = Frente(2.5f);
            for (int i = 0; i < 4; i++)
                Efeitos.Rajada(p, Quaternion.identity, Cores[i], 90, new Vector2(4, 14), new Vector2(0.2f, 0.6f), new Vector2(0.6f, 1.2f), raio: 0.8f);
            Efeitos.Clarao(p, Color.white, 16f, 20f, 0.9f);
            Dano(p, 6f, 120f, 12f);
        }
    }
}
