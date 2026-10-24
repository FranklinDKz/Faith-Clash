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
    /// 1-Fogo (Katon), 2-Água (Suiton), 3-Terra (Doton), 4-Ar (Fuuton). Clique = técnica leve do estilo, segurar o clique = técnica forte,
    /// F = habilidade do estilo. Cada técnica usa só o gesto de conjuração (nunca socos ou chutes). R solta o Roxo (Técnica Reversa) gastando uma carga espiritual.
    /// No modo Combate, o golpe pesado tem chance de Faísca Negra (Kokusen). Água → Fogo → Terra → Ar em sequência causa a Explosão Elemental.
    /// </summary>
    public class EstilosElementais : MonoBehaviour
    {
        [SerializeField] float custoLeve = 60f, custoForte = 100f, custoFinal = 150f;
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
                    if (!EmPoder) status?.Avisar("Entre no modo Poder (1-4) para usar poderes");
                    else if (status != null && status.Cargas <= 0) status.Avisar("Sem cargas espirituais");
                    else if (combate.IniciarAcaoEspecial("PowerUp", 2.4f, 1.2f, () => { if (status.GastarCarga()) Roxo(); }))
                        SomJogo.Tocar("Roxo_Carga", transform.position, 0.9f, duracaoMax: 1.4f);
                }
            }

            // F = habilidade ativa do estilo (como a tecla de habilidade do Naraka)
            if (k != null && Cursor.lockState == CursorLockMode.Locked && k.fKey.wasPressedThisFrame)
            {
                if (!EmPoder) status?.Avisar("Entre no modo Poder (1-4) para usar poderes");
                else Conjurar();
            }
        }

        void Escolher(Elemento e)
        {
            modo?.Definir(Modo.Poder);
            Selecionar(e);
            SomJogo.Tocar("UI_Elemento", null, 0.6f);
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

        // Faísca Negra: golpe pesado do modo Combate que acerta tem chance de causar o efeito perfeito
        void AoImpacto(int passo, bool acertou)
        {
            if (passo != 0 || !acertou || modo == null || modo.Atual != Modo.Combate) return;
            if (UnityEngine.Random.value < chanceFaiscaNegra) FaiscaNegra();
        }

        // gesto (animação do Mixamo) de cada técnica: clipe, duração e momento da liberação (s)
        static readonly (string clip, float dur, float imp) GestoFogo = ("CastFogo", 1.8f, 0.97f), GestoVento = ("CastVento", 1.7f, 0.9f),
            GestoEnergia = ("CastEnergia", 2.0f, 1.15f), GestoTerra = ("CastTerra", 2.4f, 1.6f), GestoPoder = ("PowerUp", 2.4f, 1.2f);

        /// <summary>Clique no modo Poder: técnica leve do estilo (só o gesto de conjuração, sem golpes de luta).</summary>
        public bool TecnicaLeve()
        {
            Action tecnica; (string clip, float dur, float imp) g; float custo = custoLeve;
            switch (Atual)
            {
                case Elemento.Fogo: tecnica = GoukakyuuNoJutsu; g = GestoFogo; break;
                case Elemento.Agua: tecnica = SuiryuudanNoJutsu; g = GestoEnergia; break;
                case Elemento.Terra: tecnica = DoryuudanNoJutsu; g = GestoTerra; break;
                default: tecnica = DaitoppaNoJutsu; g = GestoVento; custo = custoLeve * 0.5f; break;
            }
            return Usar(custo, tecnica, g);
        }

        /// <summary>Segurar o clique no modo Poder: técnica forte do estilo.</summary>
        public bool TecnicaForte()
        {
            Action tecnica; (string clip, float dur, float imp) g; float custo = custoForte;
            switch (Atual)
            {
                case Elemento.Fogo: tecnica = HousenkaNoJutsu; g = GestoVento; break;
                case Elemento.Agua: tecnica = SuirouNoJutsu; g = GestoTerra; break;
                case Elemento.Terra: tecnica = MuralhaDeTerra; g = GestoTerra; custo = custoFinal; break;
                default: tecnica = Azul; g = GestoEnergia; break;
            }
            return Usar(custo, tecnica, g);
        }

        bool Usar(float custo, Action tecnica, (string clip, float dur, float imp) g)
        {
            if (status != null && status.Fe < custo) { status.Avisar("Fé insuficiente"); return false; }
            return combate.IniciarAcaoEspecial(g.clip, g.dur, g.imp, () => TentarTecnica(custo, tecnica));
        }
        /// <summary>Habilidade ativa do estilo (tecla F): faz o gesto e libera a técnica no tempo certo.</summary>
        void Conjurar()
        {
            Action tecnica; float custo; (string clip, float dur, float imp) g;
            switch (Atual)
            {
                case Elemento.Fogo: tecnica = HousenkaNoJutsu; g = GestoVento; break;
                case Elemento.Agua: tecnica = SuirouNoJutsu; g = GestoTerra; break;
                case Elemento.Terra: tecnica = MuralhaDeTerra; g = GestoTerra; break;
                default: tecnica = Reppusho; g = GestoPoder; break;
            }
            custo = custoFinal;
            if (status != null && status.Fe < custo) { status.Avisar("Fé insuficiente"); return; }
            combate.IniciarAcaoEspecial(g.clip, g.dur, g.imp, () => TentarTecnica(custo, tecnica));
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
            SomJogo.Tocar("Fogo_Lancar", transform.position, 1f);
            SomJogo.Tocar("Fogo_Impacto", Frente(5f), 0.9f, atraso: 0.35f);
            TremorGlobal.Disparar(1.5f);
            Disparar(Frente(1.2f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = Cores[0], velocidade = 14f, duracao = 1.3f, dano = 55f, empurrao = 9f, tamanho = 2.2f, raioImpacto = 4f, explodir = true, particulas = 260,
            });
        }

        void HousenkaNoJutsu()
        {
            Anunciar("Katon: Housenka no Jutsu");
            SomJogo.Tocar("Fogo_Magia", transform.position, 0.9f, duracaoMax: 2.5f);
            SomJogo.Tocar("Fogo_Impacto", Frente(5f), 0.6f, 1.2f, atraso: 0.5f);
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
            SomJogo.Tocar("Agua_Splash", transform.position, 1f);
            SomJogo.Tocar("Agua_Impacto", Frente(6f), 0.8f, atraso: 0.45f, duracaoMax: 2.2f);
            Disparar(Frente(1f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = Cores[1], velocidade = 17f, duracao = 1.5f, dano = 45f, empurrao = 8f, tamanho = 1.5f, serpentear = true, particulas = 320,
            });
        }

        void SuirouNoJutsu()
        {
            Anunciar("Suiton: Suirou no Jutsu");
            SomJogo.Tocar("Agua_Explosao", transform.position, 0.8f, duracaoMax: 3f);
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
            SomJogo.Tocar("Terra_Doryuudan", transform.position, 1f);
            TremorGlobal.Disparar(1f);
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
            SomJogo.Tocar("Terra_Muralha", transform.position, 1f, 0.9f);
            TremorGlobal.Disparar(1.4f);
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
            SomJogo.Tocar("Fogo_Lancar", transform.position, 0.7f, 1.35f, duracaoMax: 1.4f);
            Vector3 p = Frente(1.5f);
            Efeitos.Rajada(p, Quaternion.LookRotation(transform.forward), Color.white, 120, new Vector2(8, 16), new Vector2(0.06f, 0.2f), new Vector2(0.25f, 0.5f), ParticleSystemShapeType.Cone, 0.7f);
            Dano(p + transform.forward * 1.5f, 3f, 14f, 9f);
        }

        void Reppusho()
        {
            Anunciar("Fuuton: Reppushou");
            SomJogo.Tocar("Ar_Reppushou", transform.position, 0.9f, 1.1f, duracaoMax: 2f);
            TremorGlobal.Disparar(1.3f);
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
            SomJogo.Tocar("Azul_Atrair", transform.position, 0.9f, duracaoMax: 3f);
            Vector3 p = Frente(3.2f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.25f, 0.55f, 1f), 200, new Vector2(1, 5), new Vector2(0.1f, 0.35f), new Vector2(0.6f, 1.2f), raio: 1.2f);
            Efeitos.Clarao(p, new Color(0.25f, 0.55f, 1f), 10f, 12f, 0.9f);
            Dano(p, 6f, 25f, 12f, 0.8f, true);
        }

        /// <summary>Roxo: esfera de energia que apaga tudo no caminho (gasta carga espiritual).</summary>
        void Roxo()
        {
            TecnicaUsada?.Invoke("Técnica Reversa: Roxo", new Color(0.7f, 0.3f, 1f));
            SomJogo.Tocar("Roxo_Feixe", transform.position, 1f, duracaoMax: 2.5f);
            SomJogo.Tocar("Roxo_Explosao", Frente(10f), 1f, atraso: 0.6f, duracaoMax: 5f);
            TremorGlobal.Disparar(2.2f);
            Disparar(Frente(1.3f), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = new Color(0.7f, 0.3f, 1f), velocidade = 22f, duracao = 1.2f, dano = 130f, empurrao = 14f, tamanho = 3f, raioImpacto = 6f, explodir = true, particulas = 320,
            });
        }

        /// <summary>Faísca Negra (Kokusen): golpe pesado perfeito causa dano extra.</summary>
        void FaiscaNegra()
        {
            TecnicaUsada?.Invoke("Faísca Negra!", new Color(1f, 0.15f, 0.2f));
            SomJogo.Tocar("Soco_Forte", transform.position, 1f, 0.8f);
            SomJogo.Tocar("Espada_Impacto", transform.position, 0.8f, 0.7f);
            TremorGlobal.Disparar(1.8f);
            Vector3 p = Frente(1.8f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(1f, 0.1f, 0.15f), 90, new Vector2(4, 12), new Vector2(0.08f, 0.25f), new Vector2(0.3f, 0.6f), raio: 0.4f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.05f, 0.0f, 0.05f), 40, new Vector2(2, 6), new Vector2(0.3f, 0.8f), new Vector2(0.3f, 0.7f), raio: 0.5f, aditivo: false);
            Efeitos.Clarao(p, new Color(1f, 0.1f, 0.15f), 14f, 10f, 0.25f);
            Dano(p, 2.2f, 60f, 6f);
        }

        void ExplosaoElemental()
        {
            TecnicaUsada?.Invoke("Explosão Elemental", Color.white);
            SomJogo.Tocar("Explosao_Elemental", Frente(5f), 1f, duracaoMax: 6f);
            TremorGlobal.Disparar(3f);
            Vector3 p = Frente(2.5f);
            for (int i = 0; i < 4; i++)
                Efeitos.Rajada(p, Quaternion.identity, Cores[i], 90, new Vector2(4, 14), new Vector2(0.2f, 0.6f), new Vector2(0.6f, 1.2f), raio: 0.8f);
            Efeitos.Clarao(p, Color.white, 16f, 20f, 0.9f);
            Dano(p, 6f, 120f, 12f);
        }
    }
}
