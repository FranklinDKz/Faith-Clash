using System;
using System.Collections.Generic;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>
    /// Catálogo de efeitos visuais prontos (pacotes gratuitos da Asset Store: Hovl Studio, Jean Moreno, Vefects, Synty, Tazo).
    /// O Preparar Jogador preenche a lista; o resto do jogo chama <c>CatalogoVFX.Tocar("chave", posição)</c>.
    /// Se a chave não existir, não faz nada (os efeitos procedurais continuam valendo).
    /// </summary>
    public class CatalogoVFX : MonoBehaviour
    {
        [Serializable] public struct Item { public string chave; public GameObject prefab; public float escala; }

        [SerializeField] List<Item> itens = new List<Item>();
        static readonly Dictionary<string, Item> mapa = new Dictionary<string, Item>();

        public List<Item> Itens => itens;

        void Awake()
        {
            mapa.Clear();
            foreach (var i in itens) if (i.prefab != null) mapa[i.chave] = i;
        }

        /// <summary>Instancia o efeito e o destrói depois de <paramref name="duracao"/> s. Devolve o objeto (ou null).</summary>
        public static GameObject Tocar(string chave, Vector3 pos, Quaternion? rot = null, Transform pai = null, float escala = 1f, float duracao = 3f)
        {
            if (!mapa.TryGetValue(chave, out var it)) return null;
            var go = Instantiate(it.prefab, pos, rot ?? Quaternion.identity);
            float e = (it.escala > 0f ? it.escala : 1f) * escala;
            go.transform.localScale = it.prefab.transform.localScale * e;
            if (pai != null) go.transform.SetParent(pai, true);
            Destroy(go, duracao);
            return go;
        }
    }
}
