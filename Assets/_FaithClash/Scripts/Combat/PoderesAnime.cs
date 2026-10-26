using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Player;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Poderes de anime: Raio (Raiton), Areia (Gaara), Ki (Kamehameha, Rasengan), Dōjutsu (Sharingan, Chibaku Tensei, Rinnegan) e Domínio
    /// (Raijin Voador, Vazio Infinito, Santuário Malevolente), além do Shinra Tensei, do fogo pela boca e da água pelas mãos.
    /// </summary>
    public partial class EstilosElementais
    {
        public struct Tec
        {
            public string nome, gesto;
            public float custo;
            public Action acao;
            public string id;
            public Action<float> inicio;   // chamado assim que o gesto começa (carga de energia); recebe o tempo até a liberação
        }

        public static readonly string[] NomesElementos = { "FOGO", "ÁGUA", "TERRA", "AR", "RAIO", "AREIA", "KI", "SHARINGAN", "DOMÍNIO", "RINNEGAN" };

        /// <summary>Nomes das três técnicas de cada estilo: clique, segurar o clique, F.</summary>
        public static readonly string[][] NomesTecnicas =
        {
            new[] { "Katon: Goukakyuu (fogo pela boca)", "Katon: Housenka", "Katon: Gouryuuka (dragão de fogo)" },
            new[] { "Suiton: Suiryuudan (das mãos)", "Suiton: Suirou (prisão)", "Suiton: Daibakufu (onda)" },
            new[] { "Doton: Doryuudan", "Doton: Doryuuheki (muralha)", "Doton: Terremoto" },
            new[] { "Fuuton: Daitoppa", "Técnica Maldita: Azul (atrai)", "Fuuton: Grande Rajada" },
            new[] { "Raiton: Raikou (relâmpago)", "Chidori Nagashi (arcos)", "Kirin (raio do céu)" },
            new[] { "Suna: Sabaku Taisou (onda de areia)", "Sabaku Kyuu (caixão de areia)", "Suna no Tate (escudo de areia)" },
            new[] { "Ki: Rajada de Ki", "Kamehameha", "Rasengan" },
            new[] { "Sharingan (lentidão + previsão, 5 s; recarga 10 min)", "—", "—" },
            new[] { "Raijin Voador (flash)", "Expansão: Vazio Infinito", "Expansão: Santuário Malevolente" },
            new[] { "Rinnegan: Teleporte", "Chibaku Tensei", "Shinra Tensei (recarga 5 s, mais forte com a Fé cheia)" },
        };

        Transform OsseoDe(HumanBodyBones b) => animator != null && animator.isHuman ? animator.GetBoneTransform(b) : null;

        Vector3 BocaPos()
        {
            var c = OsseoDe(HumanBodyBones.Head);
            return c != null ? c.position + transform.forward * 0.2f - Vector3.up * 0.07f : Frente(0.4f) + Vector3.up * 0.5f;
        }

        Vector3 MaosPos()
        {
            var d = OsseoDe(HumanBodyBones.RightHand); var e = OsseoDe(HumanBodyBones.LeftHand);
            if (d != null && e != null) return (d.position + e.position) * 0.5f + transform.forward * 0.1f;
            return d != null ? d.position : Frente(0.7f);
        }

        Vector3 MaoDireita() { var d = OsseoDe(HumanBodyBones.RightHand); return d != null ? d.position + transform.forward * 0.1f : Frente(0.7f); }

        /// <summary>Dano em uma linha (feixes): cápsula entre dois pontos.</summary>
        public int DanoEmLinha(Vector3 a, Vector3 b, float raio, float dano, float empurrao, Vector3 dir)
        {
            int n = 0;
            var vistos = new HashSet<IDamageable>();
            foreach (var col in Physics.OverlapCapsule(a, b, raio, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                var alvo = col.GetComponentInParent<IDamageable>();
                if (alvo == null || alvo.IsDead || !vistos.Add(alvo)) continue;
                alvo.TakeDamage(dano * (combate != null ? combate.DamageMultiplier : 1f), col.ClosestPoint(a), gameObject);
                var boneco = col.GetComponentInParent<TrainingDummy>();
                if (boneco != null && empurrao > 0f) boneco.Empurrar(dir.normalized * empurrao);
                combate?.RegisterComboHit();
                n++;
            }
            return n;
        }

        TrainingDummy MaisPerto(float raio, bool preferirTravado = true)
        {
            var trava = GetComponent<TravaAlvo>();
            if (preferirTravado && trava != null && trava.Travado)
            {
                var d = trava.Alvo.GetComponentInParent<TrainingDummy>();
                if (d != null && !d.Vida.IsDead) return d;
            }
            TrainingDummy melhor = null; float md = raio;
            foreach (var d in TrainingDummy.Todos)
            {
                if (d.Vida.IsDead) continue;
                float dist = Vector3.Distance(d.transform.position, transform.position);
                // prefere o que está na frente
                if (Vector3.Dot(transform.forward, (d.transform.position - transform.position).normalized) < 0.2f) dist += 4f;
                if (dist < md) { md = dist; melhor = d; }
            }
            return melhor;
        }

        void Carga(Color cor, float tamanho, float tempo, Func<Vector3> pos)
        {
            EfeitosAnime.Esfera(pos(), cor, tamanho, tempo, pos);
            CatalogoVFX.Tocar("ki_circulo", transform.position + Vector3.up * 0.1f, Quaternion.identity, null, 0.8f, tempo + 0.3f);
            SomJogo.Tocar("Roxo_Carga", transform.position, 0.5f, 1.3f, duracaoMax: Mathf.Min(tempo, 1.2f));
        }

        // ------------------------------------------------------------------ tabela de técnicas
        Tec Obter(Elemento e, int tipo)
        {
            float leve = custoLeve, forte = custoForte, fin = custoFinal;
            switch (e)
            {
                case Elemento.Fogo:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[0][0], custo = leve, gesto = "Mx_Roar", acao = GoukakyuuNoJutsu };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[0][1], custo = forte, gesto = "Mx_Standing_2H_Magic_Attack_05", acao = HousenkaNoJutsu };
                    return new Tec { nome = NomesTecnicas[0][2], custo = fin, gesto = "Mx_Yelling_Out", acao = GouryuukaNoJutsu };
                case Elemento.Agua:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[1][0], custo = leve, gesto = "Mx_Standing_2H_Magic_Attack_01", acao = SuiryuudanNoJutsu };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[1][1], custo = forte, gesto = "Mx_Standing_2H_Magic_Area_Attack_02", acao = SuirouNoJutsu };
                    return new Tec { nome = NomesTecnicas[1][2], custo = fin, gesto = "Mx_Standing_2H_Magic_Attack_03", acao = DaibakufuNoJutsu };
                case Elemento.Terra:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[2][0], custo = leve, gesto = "CastTerra", acao = DoryuudanNoJutsu };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[2][1], custo = fin, gesto = "Mx_Standing_2H_Magic_Area_Attack_01", acao = MuralhaDeTerra };
                    return new Tec { nome = NomesTecnicas[2][2], custo = fin, gesto = "Mx_Standing_2H_Magic_Attack_04", acao = Terremoto };
                case Elemento.Ar:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[3][0], custo = leve * 0.5f, gesto = "CastVento", acao = DaitoppaNoJutsu };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[3][1], custo = forte, gesto = "Mx_Two_Hand_Spell_Casting", acao = Azul };
                    return new Tec { nome = NomesTecnicas[3][2], custo = fin, gesto = "CastVento", acao = DaitoppaNoJutsu };
                case Elemento.Raio:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[4][0], custo = leve, gesto = "Mx_Standing_1H_Magic_Attack_01", acao = Raikou, inicio = t => Carga(CoresRaio, 0.5f, t, MaoDireita) };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[4][1], custo = forte, gesto = "Mx_Standing_2H_Magic_Attack_02", acao = ChidoriNagashi, inicio = t => Carga(CoresRaio, 0.8f, t, MaosPos) };
                    return new Tec { nome = NomesTecnicas[4][2], custo = fin, gesto = "Mx_Standing_1H_Magic_Attack_03", acao = Kirin };
                case Elemento.Areia:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[5][0], custo = leve, gesto = "Mx_Standing_2H_Magic_Attack_01", acao = SabakuTaisou };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[5][1], custo = forte, gesto = "Mx_Standing_1H_Cast_Spell_01", acao = SabakuKyuu };
                    return new Tec { nome = NomesTecnicas[5][2], custo = fin, gesto = "Mx_Standing_2H_Cast_Spell_01", acao = SunaNoTate };
                case Elemento.Ki:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[6][0], custo = leve * 0.7f, gesto = "Mx_Standing_1H_Magic_Attack_02", acao = RajadaDeKi };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[6][1], custo = fin, gesto = "Mx_Standing_2H_Magic_Attack_05", acao = Kamehameha, inicio = t => Carga(CoresKi, 1.2f, t, MaosPos) };
                    return new Tec { nome = NomesTecnicas[6][2], custo = forte, gesto = "Mx_Standing_1H_Magic_Attack_01", acao = Rasengan, inicio = t => Carga(new Color(0.4f, 0.8f, 1f), 0.9f, t, MaoDireita) };
                case Elemento.Olhos:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[7][0], custo = leve, gesto = "Mx_Strong_Gesture", acao = Sharingan, id = "sharingan" };
                    return new Tec { nome = "—", custo = 0f, gesto = "Mx_Strong_Gesture", acao = null };
                case Elemento.Rinnegan:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[9][0], custo = forte, gesto = "Mx_Hands_Forward_Gesture", acao = TeleporteRinnegan };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[9][1], custo = fin, gesto = "Mx_Standing_2H_Magic_Area_Attack_01", acao = ChibakuTensei };
                    return new Tec { nome = NomesTecnicas[9][2], custo = fin, gesto = "U_ShinraTensei", acao = ShinraTensei, id = "shinra" };
                default:
                    if (tipo == 0) return new Tec { nome = NomesTecnicas[8][0], custo = leve, gesto = "Mx_Standing_1H_Cast_Spell_01", acao = RaijinVoador };
                    if (tipo == 1) return new Tec { nome = NomesTecnicas[8][1], custo = 300f, gesto = "Mx_Magic_Spell_Casting", acao = VazioInfinito };
                    return new Tec { nome = NomesTecnicas[8][2], custo = 300f, gesto = "Mx_Wide_Arm_Spell_Casting", acao = SantuarioMalevolente };
            }
        }

        static readonly Color CoresRaio = new Color(0.55f, 0.75f, 1f);
        static readonly Color CoresKi = new Color(0.3f, 0.8f, 1f);

        float sharinganPronto, shinraPronto;

        bool Lancar(int tipo)
        {
            if (RegrasFase.PoderesBloqueados || !PerfilJogo.Equipado((int)Atual)) return false;
            var t = Obter(Atual, tipo);
            if (t.acao == null) { status?.Avisar("Este estilo só tem a técnica do clique"); return false; }
            if (DominioAtivo) { status?.Avisar("Domínio ativo — só sai quando a Fé estiver abaixo de 10%"); return false; }
            if (t.id == "sharingan" && Time.time < sharinganPronto) { status?.Avisar($"Sharingan recarregando: {Mathf.CeilToInt((sharinganPronto - Time.time) / 60f)} min"); return false; }
            if (t.id == "shinra" && Time.time < shinraPronto) { status?.Avisar($"Shinra Tensei em {Mathf.CeilToInt(shinraPronto - Time.time)} s"); return false; }
            var g = Gesto(t.gesto);
            if (status != null && status.Fe < t.custo) { status.Avisar("Fé insuficiente"); return false; }
            float imp = t.gesto.StartsWith("Mx_") ? Mathf.Clamp(InfoGolpes.Impacto(t.gesto, 0.45f * g.dur), 0.2f, g.dur) : g.imp;
            float vel = Mathf.Clamp(g.dur / 1.7f, 1f, 2.2f);   // gestos longos do Mixamo ficam rápidos: no máximo ~1,7 s
            bool ok = combate.IniciarAcaoEspecial(g.clip, g.dur, imp, () => TentarTecnica(t.custo, t.acao), vel);
            if (ok) t.inicio?.Invoke(imp / vel);
            return ok;
        }

        // ------------------------------------------------------------------ fogo / água / terra / ar (novos)
        void GouryuukaNoJutsu()
        {
            Anunciar("Katon: Gouryuuka no Jutsu");
            SomJogo.Tocar("Fogo_Lancar", transform.position, 1f, 0.85f);
            SomJogo.Tocar("Fogo_Impacto", Frente(8f), 1f, 0.8f, atraso: 0.6f);
            TremorGlobal.Disparar(1.6f);
            CatalogoVFX.Tocar("fogo_sopro", BocaPos(), Quaternion.LookRotation(DirecaoAlvo()), null, 1.6f, 1.6f);
            Disparar(BocaPos(), DirecaoAlvo(), new ProjetilElemental.Opcoes
            {
                cor = Cores[0], velocidade = 18f, duracao = 1.6f, dano = 70f, empurrao = 10f, tamanho = 2.4f, raioImpacto = 4.5f, explodir = true, serpentear = true, particulas = 300,
                vfxRastro = "fogo_grande", vfxExplosao = "fogo_explosao",
            });
        }

        void DaibakufuNoJutsu()
        {
            Anunciar("Suiton: Daibakufu no Jutsu");
            { Vector3 dd = DirecaoAlvo(); dd.y = 0f; dd.Normalize(); for (int pi = 0; pi < 5; pi++) MundoEfeitos.Poca(transform.position + dd * (2f + pi * 2.2f) + UnityEngine.Random.insideUnitSphere * 0.5f, UnityEngine.Random.Range(1.8f, 3f), 50f); }
            SomJogo.Tocar("Agua_Explosao", transform.position, 1f, 0.9f, duracaoMax: 3f);
            TremorGlobal.Disparar(1.4f);
            Vector3 dir = DirecaoAlvo(); dir.y = 0f; dir.Normalize();
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = transform.position + dir * (2f + i * 2.2f);
                CatalogoVFX.Tocar("agua_splash", p, Quaternion.identity, null, 2.5f, 2.5f);
                Efeitos.Rajada(p + Vector3.up, Quaternion.LookRotation(dir), Cores[1], 60, new Vector2(4, 9), new Vector2(0.15f, 0.4f), new Vector2(0.5f, 1f), ParticleSystemShapeType.Cone, 1.2f, 0.4f);
                Dano(p + Vector3.up, 2.8f, 14f, 8f);
            }
            CatalogoVFX.Tocar("agua_ondas", transform.position, Quaternion.identity, null, 3f, 3f);
        }

        void Terremoto()
        {
            Anunciar("Doton: Terremoto");
            SomJogo.Tocar("Terra_Muralha", transform.position, 1f, 0.7f);
            TremorGlobal.Disparar(2.4f);
            CatalogoVFX.Tocar("terra_explosao", transform.position, Quaternion.identity, null, 2.2f, 3f);
            var mat = Efeitos.LitOpaco(new Color(0.45f, 0.35f, 0.22f), 0.15f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f; float r = 3f + (i % 3) * 1.6f;
                Espinho(transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, UnityEngine.Random.Range(1.2f, 2.2f), mat, 1.6f);
            }
            Dano(transform.position, 7.5f, 45f, 10f);
        }

        /// <summary>Shinra Tensei: repulsão total em volta do jogador (Pain).</summary>
        void ShinraTensei()
        {
            shinraPronto = Time.time + 5f;
            float f = status != null ? Mathf.Clamp01(feAntes / Mathf.Max(1f, status.FeMaxima)) : 1f;   // Fé cheia = mais forte
            float raioShinra = Mathf.Lerp(7f, 26f, f);
            Anunciar("Shinra Tensei");
            SomJogo.Tocar("Ar_Reppushou", transform.position, 1f, 0.7f, duracaoMax: 2.5f);
            SomJogo.Tocar("Explosao_Elemental", transform.position, 0.8f, 1.2f, duracaoMax: 3f);
            TremorGlobal.Disparar(2.6f);
            Vector3 p = transform.position + Vector3.up * 1f;
            CatalogoVFX.Tocar("vento_faiscas", p, Quaternion.identity, null, 4f, 3f);
            CatalogoVFX.Tocar("vento_fumaca", transform.position, Quaternion.identity, null, 2.5f, 3f);
            for (int i = 0; i < 3; i++)
            {
                var anel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(anel.GetComponent<Collider>());
                anel.name = "Onda de Repulsão";
                anel.transform.position = p;
                var r = anel.GetComponent<Renderer>();
                r.material = Efeitos.LitTransparente(new Color(0.85f, 0.92f, 1f, 0.25f - i * 0.06f), 0.9f);
                var t = anel.AddComponent<EfeitoTemporario>();
                t.Configurar(0.5f + i * 0.12f);
                t.Escala(Vector3.one * 0.5f, Vector3.one * (30f - i * 5f));
                t.Desbotar(r);
            }
            Efeitos.Rajada(p, Quaternion.identity, Color.white, 260, new Vector2(10, 24), new Vector2(0.06f, 0.2f), new Vector2(0.4f, 0.8f), raio: 0.6f);
            Efeitos.Clarao(p, Color.white, 10f, 18f, 0.4f);
            Dano(p, raioShinra, Mathf.Lerp(30f, 140f, f), Mathf.Lerp(10f, 40f, f));
            ReacaoEmArea(p, raioShinra, "U_InimigoShinra");
            MundoEfeitos.Destruir(transform.position, raioShinra, f, transform);
        }

        /// <summary>Faz os inimigos atingidos tocarem a reação própria da técnica.</summary>
        void ReacaoEmArea(Vector3 centro, float raio, string clip)
        {
            foreach (var d in TrainingDummy.Todos)
            {
                if (d.Vida.IsDead || Vector3.Distance(d.transform.position, centro) > raio) continue;
                d.GetComponent<InimigoIA>()?.ReacaoEspecial(clip);
            }
        }

        // ------------------------------------------------------------------ raio
        void Raikou()
        {
            Anunciar("Raiton: Raikou");
            SomJogo.Tocar("Roxo_Feixe", transform.position, 0.6f, 1.8f, duracaoMax: 1.2f);
            Vector3 o = MaoDireita(), d = DirecaoAlvo();
            RelampagoVisual.Criar(o, o + d * 4f, CoresRaio, 0.18f, 0.14f);
            Disparar(o, d, new ProjetilElemental.Opcoes
            {
                cor = CoresRaio, velocidade = 32f, duracao = 0.7f, dano = 38f, empurrao = 5f, tamanho = 0.8f, raioImpacto = 2f, explodir = true, particulas = 120,
                vfxRastro = "raio_trilha", vfxExplosao = "raio_hit",
            });
        }

        void ChidoriNagashi()
        {
            Anunciar("Chidori Nagashi");
            SomJogo.Tocar("Roxo_Feixe", transform.position, 0.8f, 2f, duracaoMax: 1.5f);
            SomJogo.Tocar("Soco_Forte", transform.position, 0.6f, 1.4f);
            TremorGlobal.Disparar(1f);
            Vector3 o = transform.position + Vector3.up * 1.2f;
            CatalogoVFX.Tocar("raio_aura", transform.position, Quaternion.identity, transform, 1.4f, 1.2f);
            var alvos = new List<TrainingDummy>();
            foreach (var d in TrainingDummy.Todos)
                if (!d.Vida.IsDead && Vector3.Distance(d.transform.position, transform.position) < 10f) alvos.Add(d);
            alvos.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude));
            if (alvos.Count == 0) { RelampagoVisual.Criar(o, o + transform.forward * 5f, CoresRaio, 0.3f); Dano(Frente(2f), 3f, 30f, 6f); return; }
            Vector3 anterior = o;
            for (int i = 0; i < Mathf.Min(5, alvos.Count); i++)
            {
                Vector3 p = alvos[i].transform.position + Vector3.up * 1.1f;
                RelampagoVisual.Criar(anterior, p, CoresRaio, 0.35f, 0.2f);
                CatalogoVFX.Tocar("raio_hit", p, Quaternion.identity, null, 1.2f, 1.5f);
                alvos[i].Vida.TakeDamage(42f * (combate != null ? combate.DamageMultiplier : 1f), p, gameObject);
                alvos[i].GetComponent<InimigoIA>()?.ReacaoEspecial("U_InimigoRaio");
                anterior = p;
            }
            Efeitos.Clarao(o, CoresRaio, 8f, 10f, 0.3f);
        }

        void Kirin()
        {
            Anunciar("Kirin");
            MundoEfeitos.Noite(16f);
            SomJogo.Tocar("Roxo_Explosao", transform.position, 1f, 1.1f, duracaoMax: 4f);
            TremorGlobal.Disparar(2.4f);
            var alvo = MaisPerto(25f);
            Vector3 p = alvo != null ? alvo.transform.position : transform.position + transform.forward * 9f;
            StartCoroutine(KirinRotina(p));
        }

        IEnumerator KirinRotina(Vector3 p)
        {
            Efeitos.Clarao(p + Vector3.up * 6f, CoresRaio, 12f, 40f, 0.9f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 topo = p + Vector3.up * 45f + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
                RelampagoVisual.Criar(topo, p + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0, UnityEngine.Random.Range(-0.4f, 0.4f)), CoresRaio, 0.5f, 0.6f);
                yield return new WaitForSeconds(0.06f);
            }
            CatalogoVFX.Tocar("raio_hit", p + Vector3.up, Quaternion.identity, null, 3f, 2f);
            CatalogoVFX.Tocar("raio_aura", p, Quaternion.identity, null, 2.5f, 2.5f);
            CatalogoVFX.Tocar("fogo_explosao", p, Quaternion.identity, null, 1.4f, 2f);
            Efeitos.Rajada(p + Vector3.up, Quaternion.identity, CoresRaio, 160, new Vector2(4, 14), new Vector2(0.1f, 0.35f), new Vector2(0.4f, 0.9f), raio: 0.6f);
            Efeitos.Clarao(p + Vector3.up, Color.white, 18f, 24f, 0.6f);
            TremorGlobal.Disparar(2.8f);
            Dano(p + Vector3.up, 4.2f, 150f, 12f);
            ReacaoEmArea(p, 4.5f, "U_InimigoRaio");
        }

        // ------------------------------------------------------------------ areia
        static readonly Color CorAreia = new Color(0.86f, 0.7f, 0.38f);

        void SabakuTaisou()
        {
            Anunciar("Suna: Sabaku Taisou");
            SomJogo.Tocar("Terra_Doryuudan", transform.position, 0.9f, 1.3f);
            TremorGlobal.Disparar(1.2f);
            Vector3 dir = DirecaoAlvo();
            Disparar(MaosPos(), dir, new ProjetilElemental.Opcoes
            {
                cor = CorAreia, velocidade = 16f, duracao = 1.3f, dano = 40f, empurrao = 10f, tamanho = 2.4f, raioImpacto = 3.2f, explodir = true, particulas = 380,
                vfxRastro = "areia_trilha", vfxExplosao = "terra_pedras",
            });
            Efeitos.Rajada(MaosPos(), Quaternion.LookRotation(dir), CorAreia, 120, new Vector2(5, 12), new Vector2(0.1f, 0.3f), new Vector2(0.5f, 1.1f), ParticleSystemShapeType.Cone, 0.9f, 0.1f, false);
        }

        void SabakuKyuu()
        {
            Anunciar("Suna: Sabaku Kyuu");
            SomJogo.Tocar("Terra_Muralha", transform.position, 0.9f, 1.2f);
            var alvo = MaisPerto(14f);
            Vector3 p = alvo != null ? alvo.transform.position : Frente(4f);
            StartCoroutine(CaixaoDeAreia(p, alvo));
        }

        IEnumerator CaixaoDeAreia(Vector3 p, TrainingDummy alvo)
        {
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(esfera.GetComponent<Collider>());
            esfera.name = "Caixão de Areia";
            esfera.transform.position = p + Vector3.up * 1f;
            esfera.GetComponent<Renderer>().material = Efeitos.LitOpaco(CorAreia, 0.1f);
            if (alvo != null) alvo.Prender(2.2f);
            var t = esfera.AddComponent<EfeitoTemporario>();
            t.Configurar(2.4f);
            t.Escala(Vector3.one * 0.4f, Vector3.one * 3f);
            Efeitos.Rajada(p + Vector3.up, Quaternion.identity, CorAreia, 140, new Vector2(1, 5), new Vector2(0.1f, 0.3f), new Vector2(0.8f, 1.5f), raio: 1.4f, aditivo: false);
            yield return new WaitForSeconds(1.8f);
            esfera.transform.localScale = Vector3.one * 1.1f;   // esmaga
            CatalogoVFX.Tocar("terra_explosao", p, Quaternion.identity, null, 1.3f, 2f);
            TremorGlobal.Disparar(1.5f);
            SomJogo.Tocar("Explosao_Elemental", p, 0.7f, 1.2f, duracaoMax: 2f);
            Dano(p + Vector3.up, 2.2f, 80f, 5f);
        }

        void SunaNoTate()
        {
            Anunciar("Suna no Tate");
            SomJogo.Tocar("Terra_Muralha", transform.position, 0.8f, 1.4f);
            var vida = GetComponent<Health>();
            if (vida != null) StartCoroutine(Escudo(vida, 4.5f));
        }

        IEnumerator Escudo(Health vida, float duracao)
        {
            vida.Invulneravel = true;
            var domo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(domo.GetComponent<Collider>());
            domo.name = "Escudo de Areia";
            domo.transform.SetParent(transform, false);
            domo.transform.localPosition = Vector3.up * 1f;
            domo.transform.localScale = Vector3.one * 2.6f;
            var r = domo.GetComponent<Renderer>();
            r.material = Efeitos.LitTransparente(new Color(CorAreia.r, CorAreia.g, CorAreia.b, 0.4f), 0.2f);
            var ps = Efeitos.Rajada(transform.position + Vector3.up, Quaternion.identity, CorAreia, 1, new Vector2(0.3f, 1f), new Vector2(0.08f, 0.2f), new Vector2(0.6f, 1.2f), ParticleSystemShapeType.Sphere, 1.4f, 0f, false, 360f, false);
            var main = ps.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ps.transform.SetParent(transform, true);
            var em = ps.emission; em.rateOverTime = 80f; em.SetBursts(new ParticleSystem.Burst[0]);
            ps.Play();
            yield return new WaitForSeconds(duracao);
            vida.Invulneravel = false;
            Destroy(domo);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(ps.gameObject, 2f);
        }

        // ------------------------------------------------------------------ ki
        void RajadaDeKi()
        {
            Anunciar("Rajada de Ki");
            SomJogo.Tocar("Fogo_Lancar", transform.position, 0.6f, 1.6f, duracaoMax: 1f);
            Vector3 d0 = DirecaoAlvo();
            StartCoroutine(RajadaKi(d0));
        }

        IEnumerator RajadaKi(Vector3 d0)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.AngleAxis(UnityEngine.Random.Range(-5f, 5f), Vector3.up) * d0;
                Disparar(MaoDireita(), d, new ProjetilElemental.Opcoes
                {
                    cor = CoresKi, velocidade = 30f, duracao = 0.7f, dano = 20f, empurrao = 3f, tamanho = 0.6f, raioImpacto = 1.6f, explodir = true, particulas = 70,
                    vfxExplosao = "ki_faiscas",
                });
                SomJogo.Tocar("Soco_Impacto", transform.position, 0.3f, 1.7f);
                yield return new WaitForSeconds(0.1f);
            }
        }

        void Kamehameha()
        {
            Anunciar("Kamehameha!");
            SomJogo.Tocar("Roxo_Feixe", transform.position, 1f, 0.9f, duracaoMax: 2.5f);
            SomJogo.Tocar("Explosao_Elemental", transform.position, 0.6f, 1.1f, atraso: 0.2f, duracaoMax: 2f);
            TremorGlobal.Disparar(2.2f);
            var go = new GameObject("Kamehameha");
            var f = go.AddComponent<FeixeEnergia>();
            f.dono = this; f.cor = CoresKi; f.comprimento = 26f; f.largura = 1.6f; f.duracao = 1.6f; f.danoPorTick = 24f; f.tick = 0.12f; f.empurrao = 8f;
            f.origem = MaosPos; f.direcao = DirecaoAlvo;
            CatalogoVFX.Tocar("ki_faiscas", MaosPos(), Quaternion.identity, null, 2f, 2f);
            Efeitos.Clarao(MaosPos(), CoresKi, 8f, 10f, 0.6f);
        }

        void Rasengan()
        {
            Anunciar("Rasengan!");
            SomJogo.Tocar("Roxo_Feixe", transform.position, 0.7f, 1.6f, duracaoMax: 1.5f);
            StartCoroutine(RasenganRotina());
        }

        IEnumerator RasenganRotina()
        {
            var pc = GetComponent<PlayerController>();
            var alvo = MaisPerto(12f);
            Vector3 dir = alvo != null ? Vector3.ProjectOnPlane(alvo.transform.position - transform.position, Vector3.up) : transform.forward;
            float dist = alvo != null ? Mathf.Clamp(dir.magnitude - 1.3f, 1f, 9f) : 3f;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);
            var esfera = EfeitosAnime.Esfera(MaoDireita(), new Color(0.4f, 0.8f, 1f), 0.9f, 0.5f, MaoDireita);
            pc?.Avancar(dir, dist, 0.28f);
            Efeitos.Rapidez(transform.position + Vector3.up, dir, 1f);
            yield return new WaitForSeconds(0.3f);
            Vector3 p = transform.position + dir * 1.2f + Vector3.up * 1.1f;
            CatalogoVFX.Tocar("ki_faiscas", p, Quaternion.identity, null, 2f, 2f);
            CatalogoVFX.Tocar("impacto_forte", p, Quaternion.identity, null, 0.7f, 2f);
            SomJogo.Tocar("Soco_Forte", p, 1f, 0.8f);
            TremorGlobal.Disparar(1.8f);
            Efeitos.Rajada(p, Quaternion.identity, new Color(0.4f, 0.85f, 1f), 140, new Vector2(4, 12), new Vector2(0.1f, 0.3f), new Vector2(0.3f, 0.7f), raio: 0.5f);
            Efeitos.Clarao(p, new Color(0.4f, 0.8f, 1f), 10f, 12f, 0.4f);
            Dano(p, 2.6f, 85f, 14f);
        }

        // ------------------------------------------------------------------ dōjutsu
        Light luzOlhos;

        void Sharingan()
        {
            Anunciar("Sharingan");
            sharinganPronto = Time.time + 600f;   // 10 minutos para usar de novo
            MundoEfeitos.Previsao(5f, transform);
            SomJogo.Tocar("Poder_Ativar", transform.position, 0.8f, 0.8f, duracaoMax: 1.6f);
            CatalogoVFX.Tocar("maldito", transform.position + Vector3.up * 1.4f, Quaternion.identity, transform, 0.6f, 2f);
            StartCoroutine(SharinganRotina(5f));
        }

        IEnumerator SharinganRotina(float duracao)
        {
            if (luzOlhos == null)
            {
                var cab = OsseoDe(HumanBodyBones.Head);
                luzOlhos = new GameObject("Olhos do Sharingan").AddComponent<Light>();
                luzOlhos.transform.SetParent(cab != null ? cab : transform, false);
                luzOlhos.transform.localPosition = Vector3.zero;
                luzOlhos.color = new Color(1f, 0.05f, 0.05f); luzOlhos.range = 2.5f; luzOlhos.intensity = 6f;
            }
            luzOlhos.enabled = true;
            InimigoIA.Lentidao = 0.35f;
            yield return new WaitForSeconds(duracao);
            InimigoIA.Lentidao = 1f;
            if (luzOlhos != null) luzOlhos.enabled = false;
        }

        void ChibakuTensei()
        {
            Anunciar("Chibaku Tensei");
            SomJogo.Tocar("Azul_Atrair", transform.position, 1f, 0.7f, duracaoMax: 4f);
            var alvo = MaisPerto(20f);
            Vector3 p = alvo != null ? alvo.transform.position : Frente(8f);
            StartCoroutine(ChibakuRotina(p));
        }

        IEnumerator ChibakuRotina(Vector3 p)
        {
            Vector3 centro = p + Vector3.up * 7f;
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(esfera.GetComponent<Collider>());
            esfera.name = "Chibaku Tensei";
            esfera.transform.position = centro;
            esfera.GetComponent<Renderer>().material = Efeitos.LitOpaco(new Color(0.16f, 0.14f, 0.13f), 0.05f);
            var t = esfera.AddComponent<EfeitoTemporario>();
            t.Configurar(3.6f);
            t.Escala(Vector3.one * 0.5f, Vector3.one * 5f);
            CatalogoVFX.Tocar("vazio_trilha", centro, Quaternion.identity, esfera.transform, 2f, 3.6f);
            CatalogoVFX.Tocar("poder_aura", centro, Quaternion.identity, esfera.transform, 2.5f, 3.6f);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(esfera.transform, false); luz.color = new Color(0.6f, 0.3f, 1f); luz.range = 14f; luz.intensity = 5f;
            float tempo = 0f;
            while (tempo < 3f)
            {
                tempo += Time.deltaTime;
                foreach (var d in TrainingDummy.Todos)
                {
                    if (d.Vida.IsDead) continue;
                    Vector3 rel = centro - d.transform.position;
                    if (rel.magnitude > 16f) continue;
                    var ia = d.GetComponent<InimigoIA>();
                    Vector3 mov = rel.normalized * Mathf.Min(rel.magnitude, 6.5f) * Time.deltaTime;
                    if (ia != null) ia.Mover(mov);
                    d.Prender(0.3f);
                }
                if (tempo % 0.3f < Time.deltaTime) Efeitos.Rajada(centro + UnityEngine.Random.insideUnitSphere * 8f, Quaternion.identity, new Color(0.4f, 0.3f, 0.3f), 8, new Vector2(-3, -1), new Vector2(0.15f, 0.4f), new Vector2(0.4f, 0.8f), raio: 0.3f, aditivo: false);
                yield return null;
            }
            CatalogoVFX.Tocar("meteoros", centro, Quaternion.identity, null, 1.6f, 4f);
            CatalogoVFX.Tocar("explosao_vermelha", centro, Quaternion.identity, null, 2.4f, 3f);
            SomJogo.Tocar("Explosao_Elemental", centro, 1f, 0.8f, duracaoMax: 5f);
            TremorGlobal.Disparar(3f);
            Efeitos.Clarao(centro, new Color(0.7f, 0.4f, 1f), 16f, 24f, 0.8f);
            Dano(centro, 12f, 110f, 4f);
        }

        void TeleporteRinnegan()
        {
            var pc = GetComponent<PlayerController>();
            if (pc == null) return;
            Vector3 dir = DirecaoAlvo(); dir.y = 0f; dir.Normalize();
            var alvo = MaisPerto(30f);
            Vector3 destino;
            if (alvo != null) destino = alvo.transform.position - alvo.transform.forward * 1.6f;
            else
            {
                float dist = 18f;
                if (Physics.SphereCast(transform.position + Vector3.up, 0.4f, dir, out var hit, dist, ~0, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(1f, hit.distance - 0.6f);
                destino = transform.position + dir * dist;
            }
            destino.y = transform.position.y;
            Anunciar("Rinnegan: Teleporte");
            CatalogoVFX.Tocar("roxo_portal", transform.position + Vector3.up, Quaternion.identity, null, 0.8f, 1.5f);
            CatalogoVFX.Tocar("teleporte", transform.position, Quaternion.identity, null, 1.2f, 2f);
            pc.Teleportar(destino, alvo != null ? Quaternion.LookRotation(Vector3.ProjectOnPlane(alvo.transform.position - destino, Vector3.up)) : transform.rotation);
            CatalogoVFX.Tocar("roxo_portal", destino + Vector3.up, Quaternion.identity, null, 0.8f, 1.5f);
            CatalogoVFX.Tocar("teleporte", destino, Quaternion.identity, null, 1.2f, 2f);
            SomJogo.Tocar("Azul_Atrair", destino, 0.7f, 1.4f, duracaoMax: 1.2f);
            Efeitos.Clarao(destino + Vector3.up, new Color(0.7f, 0.4f, 1f), 8f, 10f, 0.3f);
        }

        // ------------------------------------------------------------------ domínio
        void RaijinVoador()
        {
            var pc = GetComponent<PlayerController>();
            var alvo = MaisPerto(28f);
            if (pc == null) return;
            Anunciar("Raijin Voador");
            Color amarelo = new Color(1f, 0.9f, 0.3f);
            Vector3 origem = transform.position + Vector3.up;
            Vector3 destino = alvo != null ? alvo.transform.position - alvo.transform.forward * 1.4f : transform.position + transform.forward * 12f;
            destino.y = transform.position.y;
            RelampagoVisual.Criar(origem, destino + Vector3.up, amarelo, 0.3f, 0.3f);
            CatalogoVFX.Tocar("faisca_amarela", origem, Quaternion.identity, null, 1.5f, 1.5f);
            pc.Teleportar(destino, alvo != null ? Quaternion.LookRotation(Vector3.ProjectOnPlane(alvo.transform.position - destino, Vector3.up)) : transform.rotation);
            CatalogoVFX.Tocar("faisca_amarela", destino + Vector3.up, Quaternion.identity, null, 1.5f, 1.5f);
            CatalogoVFX.Tocar("raio_aura", destino, Quaternion.identity, null, 1.2f, 1.5f);
            SomJogo.Tocar("Roxo_Feixe", destino, 0.8f, 2.2f, duracaoMax: 0.8f);
            Efeitos.Clarao(destino + Vector3.up, amarelo, 10f, 12f, 0.25f);
            TremorGlobal.Disparar(1f);
            if (alvo != null) alvo.Vida.TakeDamage(55f * (combate != null ? combate.DamageMultiplier : 1f), alvo.transform.position + Vector3.up, gameObject);
            Efeitos.Rapidez(destino + Vector3.up, (destino - origem).normalized, 1.2f);
        }

        void VazioInfinito()
        {
            Anunciar("Expansão de Domínio: Vazio Infinito");
            SomJogo.Tocar("Roxo_Explosao", transform.position, 1f, 0.6f, duracaoMax: 5f);
            TremorGlobal.Disparar(2.4f);
            Vector3 c = transform.position + Vector3.up;
            float dur1 = DuracaoDominio();
            EfeitosAnime.Domo(c, 15f, new Color(0.04f, 0.05f, 0.18f), dur1, 0.75f);
            CatalogoVFX.Tocar("dominio_domo", transform.position, Quaternion.identity, null, 4f, 6f);
            CatalogoVFX.Tocar("poder_estrela", c, Quaternion.identity, null, 4f, 6f);
            CatalogoVFX.Tocar("ki_circulo", transform.position, Quaternion.identity, null, 3f, 5f);
            StartCoroutine(DominioRotina(c, 15f, dur1, 8f, 0.4f, false));
        }

        void SantuarioMalevolente()
        {
            Anunciar("Expansão de Domínio: Santuário Malevolente");
            SomJogo.Tocar("Explosao_Elemental", transform.position, 1f, 0.7f, duracaoMax: 5f);
            TremorGlobal.Disparar(2.6f);
            Vector3 c = transform.position + Vector3.up;
            float dur2 = DuracaoDominio();
            EfeitosAnime.Domo(c, 16f, new Color(0.25f, 0.0f, 0.02f), dur2, 0.7f);
            CatalogoVFX.Tocar("dominio_domo", transform.position, Quaternion.identity, null, 4f, 5f);
            CatalogoVFX.Tocar("portal_vermelho", c, Quaternion.identity, null, 3f, 5f);
            StartCoroutine(DominioRotina(c, 16f, dur2, 0f, 0.1f, true));
        }

        const float DrenoDominio = 0.065f;   // fração da Fé máxima gasta por segundo dentro do domínio

        /// <summary>Quanto o domínio dura: até a Fé chegar a 10% (gasta DrenoDominio por segundo).</summary>
        float DuracaoDominio()
        {
            if (status == null) return 8f;
            float max = Mathf.Max(1f, status.FeMaxima);
            return Mathf.Clamp((status.Fe / max - 0.1f) / DrenoDominio + 0.5f, 3f, 40f);
        }

        IEnumerator DominioRotina(Vector3 centro, float raio, float duracao, float dps, float tick, bool cortes)
        {
            float t = 0f, prox = 0f;
            DominioAtivo = true;
            status?.Avisar("Domínio ativo — só sai quando a Fé estiver abaixo de 10%");
            while (true)
            {
                if (status != null)
                {
                    status.DrenarFe(status.FeMaxima * DrenoDominio * Time.deltaTime);
                    if (status.Fe <= status.FeMaxima * 0.1f) break;
                }
                else if (t >= duracao) break;
                if (t > 60f) break;
                t += Time.deltaTime;
                foreach (var d in TrainingDummy.Todos)
                {
                    if (d.Vida.IsDead) continue;
                    if (Vector3.Distance(d.transform.position, centro) < raio) d.Prender(0.3f);
                }
                if (t >= prox)
                {
                    prox = t + tick;
                    foreach (var d in TrainingDummy.Todos)
                    {
                        if (d.Vida.IsDead || Vector3.Distance(d.transform.position, centro) > raio) continue;
                        Vector3 p = d.transform.position + Vector3.up * UnityEngine.Random.Range(0.4f, 1.6f) + UnityEngine.Random.insideUnitSphere * 0.6f;
                        if (cortes)
                        {
                            CatalogoVFX.Tocar(UnityEngine.Random.value < 0.5f ? "corte_vermelho" : "raio_corte", p, UnityEngine.Random.rotation, null, 1.2f, 0.8f);
                            SomJogo.Tocar("Espada_Impacto", p, 0.35f, UnityEngine.Random.Range(0.9f, 1.4f));
                            d.Vida.TakeDamage(9f * (combate != null ? combate.DamageMultiplier : 1f), p, gameObject);
                        }
                        else if (dps > 0f) d.Vida.TakeDamage(dps * tick * (combate != null ? combate.DamageMultiplier : 1f), p, gameObject);
                    }
                    if (cortes) TremorGlobal.Disparar(0.4f);
                }
                yield return null;
            }
            DominioAtivo = false;
            status?.Avisar("O domínio se desfez");
        }
    }
}
