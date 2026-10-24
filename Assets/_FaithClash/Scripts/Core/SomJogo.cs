using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>
    /// Efeitos sonoros do jogo. Os sons ficam em Resources/Sons e são tocados pelo nome do arquivo
    /// (ex.: "Espada_Corte1"). Tem variação de tom, corte de cauda (para sons longos) e ambiente em loop.
    /// </summary>
    public class SomJogo : MonoBehaviour
    {
        static SomJogo instancia;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        AudioSource ambiente, drone;
        readonly Queue<AudioSource> pool = new Queue<AudioSource>();

        public static SomJogo Instancia
        {
            get
            {
                if (instancia == null)
                {
                    var go = new GameObject("Som do Jogo");
                    instancia = go.AddComponent<SomJogo>();
                    DontDestroyOnLoad(go);
                    instancia.IniciarAmbiente();
                }
                return instancia;
            }
        }

        public static AudioClip Clipe(string nome)
        {
            if (!cache.TryGetValue(nome, out var c))
            {
                c = Resources.Load<AudioClip>("Sons/" + nome);
                if (c == null) Debug.LogWarning("[Som] Não encontrado: " + nome);
                cache[nome] = c;
            }
            return c;
        }

        /// <summary>Toca um som. <paramref name="duracaoMax"/> corta sons longos (com fade-out nos últimos 25%).</summary>
        public static void Tocar(string nome, Vector3? pos = null, float volume = 1f, float tom = 1f, float variacao = 0.06f,
            float duracaoMax = 0f, float inicio = 0f, float atraso = 0f)
        {
            var c = Clipe(nome);
            if (c == null) return;
            Instancia.StartCoroutine(Instancia.Rotina(c, pos, volume, tom + Random.Range(-variacao, variacao), duracaoMax, inicio, atraso));
        }

        /// <summary>Toca uma lista de sons, escolhendo um ao acaso.</summary>
        public static void TocarUm(string[] nomes, Vector3? pos = null, float volume = 1f, float tom = 1f)
        {
            if (nomes == null || nomes.Length == 0) return;
            Tocar(nomes[Random.Range(0, nomes.Length)], pos, volume, tom);
        }

        AudioSource Pegar()
        {
            if (pool.Count > 0)
            {
                var s = pool.Dequeue();
                if (s != null) return s;
            }
            var go = new GameObject("fonte");
            go.transform.SetParent(transform);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            return a;
        }

        IEnumerator Rotina(AudioClip c, Vector3? pos, float volume, float tom, float duracaoMax, float inicio, float atraso)
        {
            if (atraso > 0f) yield return new WaitForSeconds(atraso);
            var a = Pegar();
            a.clip = c;
            a.volume = volume;
            a.pitch = tom;
            a.spatialBlend = pos.HasValue ? 0.6f : 0f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.maxDistance = 40f;
            if (pos.HasValue) a.transform.position = pos.Value;
            a.time = Mathf.Clamp(inicio, 0f, Mathf.Max(0f, c.length - 0.05f));
            a.Play();
            float total = (duracaoMax > 0f ? Mathf.Min(duracaoMax, c.length - inicio) : c.length - inicio) / Mathf.Max(0.1f, tom);
            float t = 0f;
            float fadeIni = total * 0.75f;
            while (t < total)
            {
                t += Time.deltaTime;
                if (duracaoMax > 0f && t > fadeIni) a.volume = volume * Mathf.Clamp01((total - t) / (total - fadeIni));
                yield return null;
            }
            a.Stop();
            pool.Enqueue(a);
        }

        void IniciarAmbiente()
        {
            ambiente = CriarLoop("Ambiente_Treino", 0.28f);
            drone = CriarLoop("Ambiente_Drone", 0.08f);
        }

        AudioSource CriarLoop(string nome, float vol)
        {
            var c = Clipe(nome);
            if (c == null) return null;
            var go = new GameObject("loop " + nome);
            go.transform.SetParent(transform);
            var a = go.AddComponent<AudioSource>();
            a.clip = c; a.loop = true; a.volume = vol; a.spatialBlend = 0f;
            a.Play();
            return a;
        }
    }
}
