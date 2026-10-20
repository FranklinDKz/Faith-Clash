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
    /// Estilos de combate 1-Fogo, 2-Água, 3-Terra, 4-Ar. Técnicas saem no 3º e 4º golpe do combo
    /// ou segurando o botão esquerdo (Terra/Ar). R solta o Golpe Espiritual (gasta uma carga).
    /// Usar Água → Fogo → Terra → Ar em sequência causa a Explosão Elemental.
    /// </summary>
    public class EstilosElementais : MonoBehaviour
    {
        [SerializeField] float tempoSegurar = 0.45f;
        [SerializeField] float custo3x = 60f, custo4x = 120f, custoSegurar = 150f;

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
        Animator animator;
        Transform mao, antebraco;
        ParticleSystem aura;
        Light luzLamina;
        float segurandoDesde = -1f;
        bool tecnicaSegurarUsada;
        readonly List<(Elemento e, float t)> historico = new List<(Elemento, float)>();

        public Elemento Atual { get; private set; } = Elemento.Fogo;
        public event Action<Elemento> ElementoMudou;
        public event Action<string, Color> TecnicaUsada;

        void Awake()
        {
            combate = GetComponent<MeleeCombat>();
            status = GetComponent<StatusJogador>();
            animador = GetComponent<AnimadorProtagonista>();
            animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (animator != null && animator.isHuman)
            {
                mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
                antebraco = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }
            CriarAura();
            Selecionar(Elemento.Fogo);
        }

        void OnEnable() { if (combate != null) combate.AttackImpact += AoImpacto; }
        void OnDisable() { if (combate != null) combate.AttackImpact -= AoImpacto; }

        public void Selecionar(Elemento e)
        {
            Atual = e;
            if (aura != null)
            {
                var main = aura.main;
                main.startColor = new ParticleSystem.MinMaxGradient(Cores[(int)e], Color.Lerp(Cores[(int)e], Color.white, 0.6f));
            }
            if (luzLamina != null) luzLamina.color = Cores[(int)e];
            ElementoMudou?.Invoke(e);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) Selecionar(Elemento.Fogo);
                if (k.digit2Key.wasPressedThisFrame) Selecionar(Elemento.Agua);
                if (k.digit3Key.wasPressedThisFrame) Selecionar(Elemento.Terra);
                if (k.digit4Key.wasPressedThisFrame) Selecionar(Elemento.Ar);
                if (k.rKey.wasPressedThisFrame && status != null && status.GastarCarga()) GolpeEspiritual();
            }

            var m = Mouse.current;
            if (m == null) return;
            if (m.leftButton.wasPressedThisFrame) { segurandoDesde = Time.time; tecnicaSegurarUsada = false; }
            if (m.leftButton.isPressed && segurandoDesde > 0f && !tecnicaSegurarUsada && Time.time - segurandoDesde > tempoSegurar)
            {
                tecnicaSegurarUsada = true;
                if (Atual == Elemento.Terra) TentarTecnica(custoSegurar, MuralhaDeTerra);
                else if (Atual == Elemento.Ar) TentarTecnica(custoSegurar, GloboDeVento);
            }
            if (m.leftButton.wasReleasedThisFrame) segurandoDesde = -1f;
        }

        void LateUpdate()
        {
            if (aura == null || mao == null || antebraco == null) return;
            Vector3 dir = (mao.position - antebraco.position).normalized;
            aura.transform.position = mao.position + dir * 0.6f;
            aura.transform.rotation = Quaternion.LookRotation(dir);
            if (luzLamina) luzLamina.intensity = 1.2f + Mathf.Sin(Time.time * 9f) * 0.25f;
        }

        void CriarAura()
        {
            var go = new GameObject("Aura da Lâmina");
            aura = go.AddComponent<ParticleSystem>();
            aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = aura.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = aura.emission;
            em.rateOverTime = 140f;
            var sh = aura.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(0.03f, 0.03f, 0.85f);
            var col = aura.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Efeitos.MaterialAditivo;
            aura.Play();

            var lgo = new GameObject("Luz da Lâmina");
            lgo.transform.SetParent(go.transform, false);
            luzLamina = lgo.AddComponent<Light>();
            luzLamina.type = LightType.Point;
            luzLamina.range = 3.5f;
            luzLamina.intensity = 1.2f;
        }

        void AoImpacto(int passo, bool acertou)
        {
            if (passo == 3)
            {
                switch (Atual)
                {
                    case Elemento.Fogo: TentarTecnica(custo3x, CorteIgneo); break;
                    case Elemento.Agua: TentarTecnica(custo3x, RajadaDeAgua); break;
                    case Elemento.Terra: TentarTecnica(custo3x, LancaDePedra); break;
                    case Elemento.Ar: TentarTecnica(custo3x * 0.5f, Lufada); break;
                }
            }
            else if (passo == 4)
            {
                if (Atual == Elemento.Fogo) TentarTecnica(custo4x, ChamaExplosiva);
                else if (Atual == Elemento.Agua) TentarTecnica(custo4x, PrisaoDeAgua);
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
        Vector3 Frente(float dist) => transform.position + Vector3.up * 1f + transform.forward * dist;

        int Dano(Vector3 centro, float raio, float dano, float empurrao = 0f, float prender = 0f)
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
                    if (empurrao > 0f) boneco.Empurrar((boneco.transform.position - centro).normalized * empurrao);
                    if (prender > 0f) boneco.Prender(prender);
                }
                combate?.RegisterComboHit();
                n++;
            }
            return n;
        }

        void Anunciar(string nome) => TecnicaUsada?.Invoke(nome, Cores[(int)Atual]);

        // ------------------------------------------------------------------ Fogo
        void CorteIgneo()
        {
            Anunciar("Corte Ígneo");
            Vector3 p = Frente(1.3f);
            Efeitos.Rajada(p, Quaternion.LookRotation(Vector3.up, transform.forward), Cores[0], 90, new Vector2(4, 9),
                new Vector2(0.15f, 0.4f), new Vector2(0.25f, 0.5f), ParticleSystemShapeType.Circle, 1.4f, 0f, true, 180f);
            Efeitos.Clarao(p, Cores[0], 5f, 7f, 0.35f);
            Dano(p, 2.4f, 22f, 2f);
        }

        void ChamaExplosiva()
        {
            Anunciar("Chama Explosiva");
            Vector3 p = Frente(2.2f);
            Efeitos.Rajada(p, Quaternion.identity, Cores[0], 160, new Vector2(4, 12), new Vector2(0.3f, 0.8f), new Vector2(0.4f, 0.9f), raio: 0.5f, gravidade: -0.4f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.25f, 0.2f, 0.18f), 40, new Vector2(1, 3), new Vector2(0.6f, 1.4f), new Vector2(0.8f, 1.6f), raio: 0.8f, gravidade: -0.2f, aditivo: false);
            Efeitos.Clarao(p, Cores[0], 12f, 14f, 0.6f);
            Dano(p, 3.5f, 45f, 8f);
        }

        // ------------------------------------------------------------------ Água
        void RajadaDeAgua()
        {
            Anunciar("Rajada de Água");
            var go = new GameObject("Rajada de Água");
            go.transform.SetPositionAndRotation(Frente(1f), transform.rotation);
            go.AddComponent<ProjetilElemental>().Configurar(this, Cores[1], 16f, 1.1f, 25f, 6f);
        }

        void PrisaoDeAgua()
        {
            Anunciar("Prisão de Água");
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

        // ------------------------------------------------------------------ Terra
        void LancaDePedra()
        {
            Anunciar("Lança de Pedra");
            var mat = Efeitos.LitOpaco(new Color(0.45f, 0.35f, 0.22f), 0.15f);
            Vector3 frente = transform.forward;
            Vector3 lado = transform.right;
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
            Anunciar("Muralha de Terra");
            animador?.Acao("AtaquePesado", 0.6f);
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

        // ------------------------------------------------------------------ Ar
        void Lufada()
        {
            Anunciar("Lufada");
            Vector3 p = Frente(1.5f);
            Efeitos.Rajada(p, Quaternion.LookRotation(transform.forward), Color.white, 60, new Vector2(6, 12), new Vector2(0.05f, 0.15f), new Vector2(0.2f, 0.4f), ParticleSystemShapeType.Cone, 0.4f);
            Dano(p, 2.2f, 12f, 7f);
        }

        void GloboDeVento()
        {
            Anunciar("Globo de Vento");
            animador?.Acao("Defesa", 0.6f);
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

        // ------------------------------------------------------------------ especiais
        void GolpeEspiritual()
        {
            TecnicaUsada?.Invoke("Golpe Espiritual", new Color(1f, 0.9f, 0.6f));
            animador?.Acao("AtaquePesado", 0.85f);
            Vector3 p = Frente(2.5f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(1f, 0.9f, 0.6f), 200, new Vector2(5, 14), new Vector2(0.15f, 0.5f), new Vector2(0.5f, 1f), raio: 0.6f);
            Efeitos.Clarao(p, new Color(1f, 0.9f, 0.6f), 14f, 16f, 0.7f);
            Dano(p, 4.5f, 80f, 10f);
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

        internal MeleeCombat Combate => combate;
    }
}
