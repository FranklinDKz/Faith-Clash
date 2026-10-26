using System.Collections.Generic;
using UnityEngine;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>
    /// Aplica a aparência salva (PerfilJogo) no personagem: tom de pele, cabelo (modelo e cor), barba, roupas peça por peça (principal, detalhes, calça,
    /// calçado), capuz, luvas, acessórios, tipo de katana e a segunda katana (duas katanas no estilo uma mão).
    /// </summary>
    public class Personalizacao : MonoBehaviour
    {
        public static readonly string[] NomesCabelo = { "Nenhum", "Raspado", "Repartido", "Longo", "Coque" };
        static readonly string[] ArquivosCabelo = { null, "Hair_Buzzed", "Hair_SimpleParted", "Hair_Long", "Hair_Buns" };
        public static readonly string[] NomesKatana = { "Aço clássico", "Sangue", "Gelo", "Ouro", "Sombra", "Jade" };
        static readonly Color[] LaminaKatana =
        {
            new Color(0.82f, 0.85f, 0.9f), new Color(0.8f, 0.1f, 0.12f), new Color(0.55f, 0.9f, 1f), new Color(1f, 0.8f, 0.3f), new Color(0.12f, 0.1f, 0.18f), new Color(0.3f, 0.9f, 0.55f),
        };
        static readonly Color[] BrilhoKatana =
        {
            Color.black, new Color(1f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 1f), new Color(1f, 0.7f, 0.1f), new Color(0.55f, 0.2f, 1f), new Color(0.1f, 1f, 0.5f),
        };

        public static readonly Color[] CoresCabelo =
        {
            new Color(0.06f, 0.05f, 0.05f), new Color(0.2f, 0.12f, 0.08f), new Color(0.4f, 0.26f, 0.14f), new Color(0.9f, 0.75f, 0.4f),
            new Color(0.62f, 0.2f, 0.1f), new Color(0.92f, 0.92f, 0.95f), new Color(0.15f, 0.4f, 0.95f), new Color(0.9f, 0.1f, 0.2f), new Color(0.6f, 0.25f, 0.9f),
        };

        public static readonly Color[] CoresRoupa =
        {
            new Color(0.07f, 0.07f, 0.09f), new Color(0.26f, 0.27f, 0.3f), new Color(0.92f, 0.92f, 0.94f), new Color(0.75f, 0.1f, 0.14f), new Color(0.1f, 0.18f, 0.45f),
            new Color(0.12f, 0.4f, 0.2f), new Color(0.4f, 0.15f, 0.6f), new Color(0.95f, 0.72f, 0.2f), new Color(0.95f, 0.45f, 0.1f), new Color(0.1f, 0.75f, 0.85f),
        };

        public static readonly Color[] Peles =
        {
            new Color(0.97f, 0.82f, 0.72f), new Color(0.93f, 0.74f, 0.6f), new Color(0.85f, 0.64f, 0.46f), new Color(0.73f, 0.51f, 0.35f),
            new Color(0.58f, 0.4f, 0.27f), new Color(0.43f, 0.29f, 0.2f), new Color(0.3f, 0.2f, 0.15f), new Color(0.8f, 0.86f, 0.92f),
        };

        static readonly int Base = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock bloco;
        Transform jogador, modelo;
        readonly Dictionary<string, Renderer> pecas = new Dictionary<string, Renderer>();
        Renderer cabelo, barba;
        string cabeloAtual = "";
        Transform katanaDireita, katanaEsquerda;
        ModoCombate modo;
        float proxima;

        void Awake() { PerfilJogo.Mudou += Aplicar; }
        void OnDestroy() { PerfilJogo.Mudou -= Aplicar; }

        void Update()
        {
            if (Time.unscaledTime >= proxima)
            {
                proxima = Time.unscaledTime + 0.5f;
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc == null) { jogador = null; return; }
                if (pc.transform != jogador) { jogador = pc.transform; Preparar(); Aplicar(); }
            }
            AtualizarSegundaKatana();
        }

        // ------------------------------------------------------------------ preparo
        void Preparar()
        {
            pecas.Clear(); cabelo = barba = null; cabeloAtual = ""; katanaEsquerda = null;
            modo = jogador.GetComponent<ModoCombate>();
            foreach (var r in jogador.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Cabeca") modelo = r.transform.parent;
                pecas[r.name] = r;
            }
            katanaDireita = null;
            foreach (var t in jogador.GetComponentsInChildren<Transform>(true)) if (t.name == "Katana") katanaDireita = t;
        }

        // ------------------------------------------------------------------ aplicar
        static void Registrar(string msg)
        {
#if UNITY_EDITOR
            System.IO.File.AppendAllText("Ferramentas/analise/personalizacao.txt", msg + System.Environment.NewLine);
#endif
        }

        void Aplicar()
        {
            try { AplicarInterno(); }
            catch (System.Exception e) { Registrar("ERRO: " + e); Debug.LogException(e); }
        }

        void AplicarInterno()
        {
            if (jogador == null || modelo == null) { Registrar("sem jogador/modelo"); return; }
            if (bloco == null) bloco = new MaterialPropertyBlock();
            var d = PerfilJogo.D;
            Color pele = Peles[Mathf.Clamp(d.tomPele, 0, Peles.Length - 1)];
            Color principal = Roupa(d.corRoupa), acento = Roupa(d.corAcento), calca = Roupa(d.corCalca), calcado = Roupa(d.corCalcado);
            Color corCabelo = CoresCabelo[Mathf.Clamp(d.corCabelo, 0, CoresCabelo.Length - 1)];

            // pele (cabeça e mãos)
            Tingir("Cabeca", 0, pele * 1.15f);
            Tingir("Male_Ranger_Arms", 1, pele * 1.15f);
            // roupa por peça
            Tingir("Male_Ranger_Body", 0, principal);
            Tingir("Male_Ranger_Arms", 0, principal);
            Tingir("Male_Ranger_Body_Belt_1", 0, acento);
            Tingir("Male_Ranger_Body_Belt_2", 0, acento);
            Tingir("Male_Ranger_Acc_Pauldron", 0, acento);
            Tingir("Male_Ranger_Arms_Bracer", 0, acento);
            Tingir("Male_Ranger_Legs", 0, calca);
            Tingir("Male_Ranger_Feet_Boots", 0, calcado);
            Tingir("Eyebrows", 0, corCabelo);
            Mostrar("Male_Ranger_Head_Hood", d.capuz);
            Mostrar("Male_Ranger_Arms_Bracer", d.luvas);
            Mostrar("Male_Ranger_Body_Belt_1", d.acessorios);
            Mostrar("Male_Ranger_Body_Belt_2", d.acessorios);
            Mostrar("Male_Ranger_Acc_Pauldron", d.acessorios);

            AplicarCabelo(d.estiloCabelo, corCabelo, d.barba);
            AplicarKatana(d.katana);
        }

        static Color Roupa(int i) => CoresRoupa[Mathf.Clamp(i, 0, CoresRoupa.Length - 1)] * 1.5f;

        void Tingir(string peca, int indiceMaterial, Color cor)
        {
            if (!pecas.TryGetValue(peca, out var r) || r == null) return;
            cor.a = 1f;
            r.GetPropertyBlock(bloco, indiceMaterial);
            bloco.SetColor(Base, cor);
            r.SetPropertyBlock(bloco, indiceMaterial);
        }

        void Mostrar(string peca, bool v)
        {
            if (pecas.TryGetValue(peca, out var r) && r != null) r.enabled = v;
        }

        // ------------------------------------------------------------------ cabelo e barba (malhas do pacote, presas ao esqueleto do personagem)
        void AplicarCabelo(int estilo, Color cor, bool comBarba)
        {
            string arquivo = ArquivosCabelo[Mathf.Clamp(estilo, 0, ArquivosCabelo.Length - 1)];
            if (arquivo != cabeloAtual)
            {
                if (cabelo != null) Destroy(cabelo.gameObject);
                cabelo = arquivo != null ? InstanciarMalha(arquivo, "Cabelo_Estilo") : null;
                cabeloAtual = arquivo;
            }
            if (comBarba && barba == null) barba = InstanciarMalha("Hair_Beard", "Barba");
            if (!comBarba && barba != null) { Destroy(barba.gameObject); barba = null; }
            foreach (var r in new[] { cabelo, barba })
            {
                if (r == null) continue;
                r.GetPropertyBlock(bloco);
                bloco.SetColor(Base, cor);
                r.SetPropertyBlock(bloco);
            }
        }

        Renderer InstanciarMalha(string arquivo, string nome)
        {
            var prefab = Resources.Load<GameObject>("Cabelos/" + arquivo);
            if (prefab == null) { Debug.LogWarning("[Personalização] Cabelo não encontrado: " + arquivo); return null; }
            string textura = arquivo == "Hair_Long" || arquivo == "Hair_Buns" ? "Cabelo2" : "Cabelo1";
            var mat = Resources.Load<Material>("Cabelos/" + textura);
            var ossos = new Dictionary<string, Transform>();
            foreach (var t in modelo.parent.GetComponentsInChildren<Transform>(true)) if (!ossos.ContainsKey(t.name)) ossos[t.name] = t;
            Renderer baseR = pecas.TryGetValue("Cabeca", out var c) ? c : null;
            var cabeca = baseR as SkinnedMeshRenderer;
            var inst = Instantiate(prefab);
            Renderer resultado = null;
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var bs = smr.bones;
                for (int i = 0; i < bs.Length; i++)
                {
                    Transform t = null;
                    if (bs[i] != null && ossos.TryGetValue(bs[i].name, out t)) bs[i] = t;
                    else bs[i] = ossos.TryGetValue("head", out var h) ? h : (cabeca != null ? cabeca.rootBone : null);
                }
                smr.bones = bs;
                smr.rootBone = cabeca != null ? cabeca.rootBone : null;
                smr.transform.SetParent(modelo, false);
                smr.transform.localPosition = Vector3.zero; smr.transform.localRotation = Quaternion.identity; smr.transform.localScale = Vector3.one;
                smr.name = nome;
                if (mat != null) smr.sharedMaterial = mat;
                smr.updateWhenOffscreen = true;
                resultado = smr;
                var cab = ossos.TryGetValue("head", out var hd) ? hd : null;
                Registrar($"[Cabelo] {arquivo}: ossos={bs.Length} limites mundo={smr.bounds.center:0.00}/{smr.bounds.size:0.00} cabeça={(cab != null ? cab.position.ToString("0.00") : "?")} mat={(mat != null ? mat.name : "nulo")} escalaLocal={smr.transform.lossyScale}");
                break;
            }
            Destroy(inst);
            return resultado;
        }

        // ------------------------------------------------------------------ katanas
        void AplicarKatana(int tipo)
        {
            tipo = Mathf.Clamp(tipo, 0, LaminaKatana.Length - 1);
            foreach (var k in new[] { katanaDireita, katanaEsquerda })
            {
                if (k == null) continue;
                foreach (var r in k.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string n = mats[i].name;
                        if (n.StartsWith("Aco"))
                        {
                            mats[i].SetColor(Base, LaminaKatana[tipo]);
                            if (BrilhoKatana[tipo].maxColorComponent > 0.01f) { mats[i].EnableKeyword("_EMISSION"); mats[i].SetColor("_EmissionColor", BrilhoKatana[tipo] * 1.3f); }
                            else { mats[i].SetColor("_EmissionColor", Color.black); }
                            mats[i].SetFloat("_Metallic", tipo == 4 ? 0.2f : 0.85f);
                        }
                        else if (n.StartsWith("Ouro")) mats[i].SetColor(Base, tipo == 3 ? new Color(1f, 0.85f, 0.35f) : tipo == 1 ? new Color(0.6f, 0.08f, 0.1f) : new Color(0.85f, 0.65f, 0.2f));
                    }
                    r.materials = mats;
                }
            }
        }

        /// <summary>Duas katanas: a segunda aparece na mão esquerda no estilo uma mão (tecla H) e some ao guardar a espada.</summary>
        void AtualizarSegundaKatana()
        {
            if (jogador == null || katanaDireita == null || modo == null) return;
            bool quer = PerfilJogo.D.duasKatanas && modo.Atual == Modo.Espada && modo.Grip == Empunhadura.UmaMao && katanaDireita.gameObject.activeInHierarchy;
            if (quer && katanaEsquerda == null) CriarSegundaKatana();
            if (katanaEsquerda != null && katanaEsquerda.gameObject.activeSelf != quer) katanaEsquerda.gameObject.SetActive(quer);
        }

        void CriarSegundaKatana()
        {
            var anim = jogador.GetComponentInChildren<Animator>();
            if (anim == null || !anim.isHuman) return;
            var maoE = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            var maoD = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (maoE == null || maoD == null) return;
            var copia = Instantiate(katanaDireita.gameObject, maoE);
            copia.name = "Katana (esquerda)";
            // espelha a pose da direita para a esquerda: posição com x invertido e rotação espelhada no plano YZ da mão
            Vector3 p = katanaDireita.localPosition;
            copia.transform.localPosition = new Vector3(-p.x, -p.y, -p.z) * 1f;
            Quaternion q = katanaDireita.localRotation;
            copia.transform.localRotation = new Quaternion(q.x, -q.y, -q.z, q.w);
            copia.transform.localScale = katanaDireita.localScale;
            katanaEsquerda = copia.transform;
            AplicarKatana(PerfilJogo.D.katana);
        }
    }
}
