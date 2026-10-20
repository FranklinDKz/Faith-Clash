using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Combat;
using FaithClash.Core;

namespace FaithClash.Player
{
    /// <summary>Nível, Fé (energia), cargas espirituais e itens rápidos (Z, X, C, V) do jogador.</summary>
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
        float furiaAte;
        int acertosAcumulados;
        GameObject lanterna;

        public int Nivel => nivel;
        public string Titulo => titulo;
        public float Fe { get; private set; }
        public float FeMaxima => feMaxima;
        public int Cargas { get; private set; } = 1;
        public int Escudos { get; private set; } = 2;
        public bool FuriaAtiva => Time.time < furiaAte;
        public Health Vida => vida;

        public event Action<string> Aviso;

        void Awake()
        {
            vida = GetComponent<Health>();
            combate = GetComponent<MeleeCombat>();
            Fe = feInicial;
        }

        void OnEnable()
        {
            if (combate != null) combate.HitLanded += AoAcertar;
        }

        void OnDisable()
        {
            if (combate != null) combate.HitLanded -= AoAcertar;
        }

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
            if (k.zKey.wasPressedThisFrame) Usar(0);
            if (k.xKey.wasPressedThisFrame) Usar(1);
            if (k.bKey.wasPressedThisFrame) Usar(2);
            if (k.vKey.wasPressedThisFrame) Usar(3);
        }

        void Usar(int i)
        {
            var item = Itens[i];
            if (item.quantidade <= 0) { Aviso?.Invoke(item.nome + " esgotado"); return; }
            item.quantidade--;
            GetComponent<AnimadorProtagonista>()?.Acao("PegarItem", 0.6f, 0.08f);
            StartCoroutine(AplicarItemDepois(i, item));
        }

        System.Collections.IEnumerator AplicarItemDepois(int i, Item item)
        {
            yield return new WaitForSeconds(0.35f);
            Vector3 p = transform.position + Vector3.up;
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
