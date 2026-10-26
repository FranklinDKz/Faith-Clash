using System;
using System.Collections.Generic;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Training
{
    /// <summary>
    /// Inimigo de treino com corpo e animações de verdade (Adam / Guarda / Lu): fica parado em guarda, persegue o jogador quando ele chega perto,
    /// ataca em intervalos, reage aos golpes (leve, pesado, derrubado) e morre com uma animação. Trabalha junto do <see cref="TrainingDummy"/>
    /// (vida, nome no HUD, empurrão e renascimento). A tecla L liga/desliga a agressividade dos inimigos.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class InimigoIA : MonoBehaviour
    {
        [Serializable]
        public class Ataque
        {
            public string clip = "Mx_Standing_Melee_Punch";
            public float dano = 35f;
            public float alcance = 1.9f;
            public float velocidade = 1f;
        }

        [SerializeField] Animator animator;
        [SerializeField] Ataque[] ataques;
        [SerializeField] string[] mortes = { "Mx_Standing_React_Death_Backward", "Mx_Standing_React_Death_Forward", "Mx_Falling_Back_Death" };
        [SerializeField] float velAndar = 2.0f, velCorrer = 4.6f, distanciaVisao = 16f, distanciaCorrer = 6f, intervaloAtaque = 2.2f;
        [SerializeField] float giro = 7f;

        enum Estado { Parado, Perseguir, Atacar, Reagir, Caido, Morto }

        public static bool Agressivos = true;
        /// <summary>Sharingan: os inimigos agem em câmera lenta (1 = normal).</summary>
        public static float Lentidao = 1f;

        Estado estado = Estado.Parado;
        TrainingDummy dummy;
        bool congelado;
        CharacterController cc;
        Transform jogador;
        Health vidaJogador;
        MeleeCombat combateJogador;
        readonly Dictionary<string, float> duracoes = new Dictionary<string, float>();
        float vy, timerEstado, proximoAtaque, velSuave;
        Ataque ataqueAtual;
        float impactoEm;
        bool impactoFeito;
        string animAtual = "";
        Vector3 casa;
        float ultimaAnimTrocada;

        public bool Morto => estado == Estado.Morto;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            dummy = GetComponent<TrainingDummy>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var c in animator.runtimeAnimatorController.animationClips) duracoes[c.name] = c.length;
            casa = transform.position;
            proximoAtaque = Time.time + 1.5f;
        }

        float Dur(string clip, float padrao = 1f) => duracoes.TryGetValue(clip, out var d) ? d : padrao;

        void Tocar(string estadoNome, float fade = 0.12f, float vel = 1f)
        {
            if (animator == null) return;
            if (!animator.HasState(0, Animator.StringToHash(estadoNome)))
            {
                string r = estadoNome.Contains("Idle") ? "Mx_Fighting_Idle"
                         : estadoNome.Contains("Death") || estadoNome.Contains("Falling") || estadoNome.Contains("Knockdown") || estadoNome.Contains("Thrown") || estadoNome.Contains("Shoved") ? "Knock_A"
                         : estadoNome.Contains("React") || estadoNome.Contains("Hit") || estadoNome.Contains("Stumble") ? "Hit_A"
                         : "Atq" + ((estadoNome.Length * 7 + (estadoNome.Length > 0 ? estadoNome[estadoNome.Length - 1] : 0)) % 3);
                if (!animator.HasState(0, Animator.StringToHash(r))) return;
                estadoNome = r;
            }
            animator.speed = vel * Lentidao;
            if (animAtual == estadoNome && estadoNome != "") { animator.CrossFadeInFixedTime(estadoNome, fade, 0, 0f); }
            else animator.CrossFadeInFixedTime(estadoNome, fade, 0, 0f);
            animAtual = estadoNome;
            ultimaAnimTrocada = Time.time;
        }

        void Update()
        {
            if (estado == Estado.Morto) { Gravidade(); return; }

            // preso (Prisão de Água, Chidori, domínio...): congelado no lugar
            if (dummy != null && dummy.Preso)
            {
                if (!congelado) { congelado = true; if (animator != null) animator.speed = 0f; }
                Gravidade();
                return;
            }
            if (congelado) { congelado = false; if (animator != null) animator.speed = Lentidao; }
            if (animator != null && estado != Estado.Atacar && animator.speed != Lentidao && Lentidao != 1f) animator.speed = Lentidao;
            float dt = Time.deltaTime * Lentidao;

            if (jogador == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) { jogador = pc.transform; vidaJogador = pc.GetComponent<Health>(); combateJogador = pc.GetComponent<MeleeCombat>(); }
            }

            Vector3 mover = Vector3.zero;
            float velAlvo = 0f;
            Vector3 paraJogador = jogador != null ? Vector3.ProjectOnPlane(jogador.position - transform.position, Vector3.up) : Vector3.zero;
            float dist = paraJogador.magnitude;
            bool jogadorVivo = vidaJogador != null && !vidaJogador.IsDead;
            bool ativo = Agressivos && jogadorVivo && dist < distanciaVisao;

            switch (estado)
            {
                case Estado.Parado:
                case Estado.Perseguir:
                    if (!ativo)
                    {
                        if (estado == Estado.Perseguir) { estado = Estado.Parado; Tocar("Mx_Fighting_Idle", 0.25f); }
                        break;
                    }
                    float alcance = Alcance();
                    if (dist > alcance * 0.9f)
                    {
                        if (estado != Estado.Perseguir) estado = Estado.Perseguir;
                        bool correr = dist > distanciaCorrer;
                        velAlvo = correr ? velCorrer : velAndar;
                        mover = Desviar(paraJogador.normalized) * velAlvo * Lentidao;
                        Virar(mover, 1.5f);
                        string anim = correr ? "Correr" : "Andar";
                        if (animAtual != anim) Tocar(anim, 0.2f);
                    }
                    else
                    {
                        Virar(paraJogador);
                        if (animAtual != "Mx_Fighting_Idle") Tocar("Mx_Fighting_Idle", 0.2f);
                        if (Time.time >= proximoAtaque && Mathf.Abs(jogador.position.y - transform.position.y) <= 1.0f) Atacar();
                    }
                    break;

                case Estado.Atacar:
                    timerEstado += dt;
                    if (!impactoFeito && timerEstado < impactoEm) Virar(paraJogador, 0.6f);   // mira até o golpe sair
                    if (!impactoFeito && timerEstado >= impactoEm) { impactoFeito = true; AcertarJogador(); }
                    if (timerEstado >= Dur(ataqueAtual.clip) / ataqueAtual.velocidade * 0.92f)
                    {
                        estado = Estado.Parado;
                        animator.speed = 1f;
                        Tocar("Mx_Fighting_Idle", 0.2f);
                        proximoAtaque = Time.time + intervaloAtaque * UnityEngine.Random.Range(0.8f, 1.4f);
                    }
                    break;

                case Estado.Reagir:
                    timerEstado -= dt;
                    if (timerEstado <= 0f) { estado = Estado.Parado; animator.speed = 1f; Tocar("Mx_Fighting_Idle", 0.2f); proximoAtaque = Mathf.Max(proximoAtaque, Time.time + 0.6f); }
                    break;

                case Estado.Caido:
                    timerEstado -= dt;
                    if (timerEstado <= 0f) { estado = Estado.Parado; animator.speed = 1f; Tocar("Mx_Fighting_Idle", 0.3f); proximoAtaque = Time.time + 1.2f; }
                    break;
            }

            velSuave = Mathf.Lerp(velSuave, velAlvo, 10f * Time.deltaTime);
            Gravidade(mover);
        }

        // ---- navegação simples: desvia de obstáculos (props, paredes) e de outros inimigos, e destrava quando fica preso
        float ladoDesvio = 1f, presoTempo, trocaLado;
        Vector3 ultimaPos;

        Vector3 Desviar(Vector3 dir)
        {
            Vector3 centro = transform.position + Vector3.up * 0.9f;
            float raio = 0.35f;
            Vector3 res = dir;
            // evita outros inimigos (separação)
            foreach (var o in TrainingDummy.Todos)
            {
                if (o == null || o.gameObject == gameObject) continue;
                Vector3 d = transform.position - o.transform.position; d.y = 0f;
                float m = d.magnitude;
                if (m < 2.2f && m > 0.01f) res += d / m * (2.2f - m) * 0.8f;
            }
            res.y = 0f;
            if (res.sqrMagnitude > 0.001f) res.Normalize(); else res = dir;
            // obstáculos à frente: raios em leque, escolhe o lado livre
            if (Bloqueado(centro, res, raio, 1.6f))
            {
                Vector3 melhor = res; bool achou = false;
                for (int i = 1; i <= 6 && !achou; i++)
                {
                    foreach (float s in new[] { ladoDesvio, -ladoDesvio })
                    {
                        Vector3 c = Quaternion.Euler(0f, s * i * 25f, 0f) * res;
                        if (!Bloqueado(centro, c, raio, 1.6f)) { melhor = c; achou = true; if (s != ladoDesvio) ladoDesvio = s; break; }
                    }
                }
                res = achou ? melhor : Quaternion.Euler(0f, ladoDesvio * 90f, 0f) * res;
            }
            // preso (não saiu do lugar): troca o lado do desvio
            if ((transform.position - ultimaPos).sqrMagnitude < 0.0004f) presoTempo += Time.deltaTime; else presoTempo = 0f;
            ultimaPos = transform.position;
            if (presoTempo > 0.5f && Time.time > trocaLado) { ladoDesvio = -ladoDesvio; trocaLado = Time.time + 1f; presoTempo = 0f; }
            return res;
        }

        bool Bloqueado(Vector3 origem, Vector3 dir, float raio, float dist)
        {
            if (!Physics.SphereCast(origem, raio, dir, out var hit, dist, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider.transform.IsChildOf(transform)) return false;
            if (jogador != null && hit.collider.transform.IsChildOf(jogador)) return false;
            if (hit.normal.y > 0.7f) return false;   // chão/rampa
            return true;
        }

        float Alcance() => ataques != null && ataques.Length > 0 ? ataques[0].alcance : 1.8f;

        void Virar(Vector3 dir, float fator = 1f)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), giro * fator * Time.deltaTime);
        }

        void Gravidade(Vector3 mover = default)
        {
            if (cc == null || !cc.enabled) return;
            vy = cc.isGrounded && vy <= 0f ? -2f : vy - 25f * Time.deltaTime;
            cc.Move((mover + Vector3.up * vy) * Time.deltaTime);
        }

        void Atacar()
        {
            if (ataques == null || ataques.Length == 0) return;
            ataqueAtual = ataques[UnityEngine.Random.Range(0, ataques.Length)];
            estado = Estado.Atacar;
            timerEstado = 0f;
            impactoFeito = false;
            float dur = Dur(ataqueAtual.clip);
            impactoEm = InfoGolpes.Impacto(ataqueAtual.clip, dur * 0.45f) / ataqueAtual.velocidade;
            Tocar(ataqueAtual.clip, 0.1f, ataqueAtual.velocidade);
        }

        void AcertarJogador()
        {
            if (vidaJogador == null || vidaJogador.IsDead || jogador == null) return;
            Vector3 para = jogador.position - transform.position;
            float dy = jogador.position.y - transform.position.y;
            para.y = 0f;
            if (Mathf.Abs(dy) > 1.0f) return;   // o jogador está em cima de algo (ou muito abaixo): fora do alcance
            if (para.magnitude > ataqueAtual.alcance + 0.6f || Vector3.Angle(transform.forward, para) > 70f) return;   // errou: o jogador saiu do alcance
            float dano = ataqueAtual.dano;
            bool bloqueou = combateJogador != null && combateJogador.IsBlocking && Vector3.Angle(jogador.forward, -para) < 100f;
            if (bloqueou)
            {
                dano *= 0.15f;
                SomJogo.Tocar("Defesa_Bloqueio", jogador.position, 0.8f, 1f);
                CatalogoVFX.Tocar("escudo", jogador.position + Vector3.up, Quaternion.identity, null, 0.6f, 1.2f);
            }
            else
            {
                SomJogo.Tocar("Soco_Impacto", jogador.position, 0.9f, 0.9f);
                CatalogoVFX.Tocar("impacto_leve", jogador.position + Vector3.up * 1.2f, Quaternion.identity, null, 0.9f, 1.5f);
            }
            vidaJogador.TakeDamage(dano, jogador.position + Vector3.up * 1.2f, gameObject);
            TremorGlobal.Disparar(bloqueou ? 0.4f : 0.9f);
        }

        static readonly string[] LeveFrente = { "Mx_Hit_To_Body", "Mx_Light_Hit_To_Head", "Mx_Standing_React_Small_From_Front", "Mx_Receive_Punch_To_The_Face", "Mx_Taking_Punch", "Mx_Zombie_Reaction_Hit" };
        static readonly string[] LeveTras = { "Mx_Standing_React_Small_From_Back", "Mx_Hit_To_The_Legs" };
        static readonly string[] LeveEsq = { "Mx_Standing_React_Small_From_Left", "Mx_Hit_On_Side_Of_Body", "Mx_Side_Hit" };
        static readonly string[] LeveDir = { "Mx_Standing_React_Small_From_Right", "Mx_Hit_On_Side_Of_Head", "Mx_Side_Hit" };
        static readonly string[] PesadaFrente = { "Mx_Big_Hit_To_Head", "Mx_Big_Stomach_Hit", "Mx_Medium_Hit_To_Head", "Mx_Standing_React_Large_From_Front" };
        static readonly string[] PesadaTras = { "Mx_Standing_React_Large_From_Back", "Mx_Stumble_Backwards" };
        static readonly string[] PesadaEsq = { "Mx_Standing_React_Large_From_Left", "Mx_Big_Side_Hit" };
        static readonly string[] PesadaDir = { "Mx_Standing_React_Large_From_Right", "Mx_Big_Side_Hit" };
        static readonly string[] Derrubadas = { "Mx_Livershot_Knockdown", "Mx_Getting_Thrown", "Mx_Shoved_Reaction_With_Spin", "Mx_Falling_Flat_Impact" };

        /// <summary>Chamado pelo TrainingDummy quando leva um golpe. <paramref name="paraAtacante"/> aponta do inimigo para quem bateu: a reação depende do lado do golpe (frente, costas, esquerda, direita).</summary>
        public void Reagir(Vector3 paraAtacante, float dano)
        {
            if (estado == Estado.Morto) return;
            animator.speed = Lentidao;
            paraAtacante.y = 0f;
            string[] lista;
            if (dano >= 28f)
            {
                estado = Estado.Caido;
                // golpe forte: o inimigo é arremessado (sobe e voa para trás), estilo Naruto Storm
                if (cc != null && cc.isGrounded) vy = Mathf.Min(7f, 2.5f + dano * 0.06f);
                string clip = Escolher(Derrubadas);
                timerEstado = Mathf.Min(Dur(clip, 1.6f), 2.0f) * 0.85f;
                Tocar(clip, 0.06f);
                return;
            }
            bool pesado = dano >= 16f;
            float lado = paraAtacante.sqrMagnitude > 0.01f ? Vector3.SignedAngle(transform.forward, paraAtacante, Vector3.up) : 0f;   // 0 = frente, ±90 = lados, 180 = atrás
            if (Mathf.Abs(lado) < 55f) lista = pesado ? PesadaFrente : LeveFrente;
            else if (Mathf.Abs(lado) > 125f) lista = pesado ? PesadaTras : LeveTras;
            else if (lado < 0f) lista = pesado ? PesadaEsq : LeveEsq;
            else lista = pesado ? PesadaDir : LeveDir;
            string c = Escolher(lista);
            estado = Estado.Reagir;
            timerEstado = Mathf.Clamp(Dur(c, 0.8f) * 0.8f, 0.35f, 1.1f);
            Tocar(c, 0.04f);
        }

        string Escolher(string[] lista)
        {
            for (int i = 0; i < 6; i++)
            {
                string c = lista[UnityEngine.Random.Range(0, lista.Length)];
                if (duracoes.ContainsKey(c) && c != ultimaReacao) { ultimaReacao = c; return c; }
            }
            foreach (var c in lista) if (duracoes.ContainsKey(c)) return c;
            return "Mx_Fighting_Idle";
        }
        string ultimaReacao = "";

        /// <summary>Reação própria de uma técnica (relâmpago, Shinra Tensei): toca a animação do usuário e fica caído o tempo dela.</summary>
        public void ReacaoEspecial(string clip)
        {
            if (estado == Estado.Morto || !duracoes.ContainsKey(clip)) return;
            animator.speed = Lentidao;
            estado = Estado.Caido;
            timerEstado = Dur(clip, 2f) * 0.9f;
            Tocar(clip, 0.05f);
        }

        public void Morrer(Vector3 dirGolpe)
        {
            if (estado == Estado.Morto) return;
            estado = Estado.Morto;
            animator.speed = 1f;
            Tocar(mortes[UnityEngine.Random.Range(0, mortes.Length)], 0.05f);
            SomJogo.Tocar("Morte_Jogador", transform.position, 0.5f, 1.2f, duracaoMax: 2f);
        }

        public void Renascer(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            cc.enabled = true;
            estado = Estado.Parado;
            animator.speed = 1f;
            Tocar("Mx_Fighting_Idle", 0.2f);
            proximoAtaque = Time.time + 2f;
        }

        /// <summary>Empurrão (golpes, jutsus): move com colisão.</summary>
        public void Mover(Vector3 delta)
        {
            if (cc != null && cc.enabled) cc.Move(delta);
        }

        public void Imobilizar(float segundos)
        {
            if (estado == Estado.Morto) return;
            estado = Estado.Caido;
            timerEstado = segundos;
        }
    }
}
