using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>
    /// Efeitos sonoros e música do jogo. Os arquivos ficam em Resources/Sons e tocam pelo nome ("Espada_Corte1"); o que não existe como arquivo
    /// (pingos, passos de pedra, pouso, deslize, escalada, espinhos, cristais, música ambiente, graves de impacto) é sintetizado na hora.
    /// Cada som tem categoria (passos, combate, poderes, ambiente, interface) com volume próprio nas configurações, ganho de mixagem
    /// e uma camada de grave (sub-bass) opcional, controlada por "Graves" — para os golpes e poderes terem peso e o jogo não soar vazio.
    /// </summary>
    public class SomJogo : MonoBehaviour
    {
        static SomJogo instancia;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        readonly Queue<AudioSource> pool = new Queue<AudioSource>();
        readonly List<(AudioSource fonte, float baseVol, string categoria)> loops = new List<(AudioSource, float, string)>();
        AudioSource ambiente, drone, rumble, musica, tocha;
        bool modoCaverna;

        internal static void ResetarEstatico() { instancia = null; cache.Clear(); }

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

        void Update()
        {
            // saiu da fase da caverna: volta ao ambiente do treino (feito aqui, nunca dentro de OnDestroy)
            if (modoCaverna && !RegrasFase.SemMapa) AmbienteCaverna(false);
        }

        // ------------------------------------------------------------------ mixagem
        // ganho de mixagem e peso de grave (0..1) por som
        static readonly Dictionary<string, (float ganho, float grave)> Mix = new Dictionary<string, (float, float)>
        {
            { "Soco_Forte", (1.1f, 0.85f) }, { "Soco_Impacto", (1.05f, 0.6f) }, { "Soco_Leve1", (0.95f, 0.3f) }, { "Soco_Leve2", (0.95f, 0.3f) }, { "Soco_Leve3", (0.95f, 0.3f) },
            { "Espada_Impacto", (1f, 0.5f) }, { "Espada_CortePesado", (1f, 0.45f) }, { "Espada_Estocada", (1f, 0.3f) },
            { "Defesa_Bloqueio", (1f, 0.6f) },
            { "Explosao_Elemental", (1.1f, 1f) }, { "Roxo_Explosao", (1.1f, 1f) }, { "Fogo_Impacto", (1f, 0.7f) }, { "Agua_Explosao", (1f, 0.8f) }, { "Agua_Impacto", (1f, 0.5f) },
            { "Terra_Doryuudan", (1f, 0.7f) }, { "Terra_Muralha", (1.1f, 0.9f) }, { "Fogo_Lancar", (1f, 0.35f) }, { "Roxo_Feixe", (0.9f, 0.4f) }, { "Azul_Atrair", (1f, 0.5f) },
            { "Poder_Ativar", (0.95f, 0.4f) }, { "Roxo_Carga", (0.95f, 0.35f) }, { "Morte_Jogador", (1f, 0.6f) },
            { "Pouso", (1f, 0.65f) }, { "Espinho", (1.1f, 0.7f) }, { "Passos_Pedra", (0.9f, 0.2f) }, { "Whoosh", (0.9f, 0f) },
        };

        static string Categoria(string nome)
        {
            if (nome.StartsWith("Passos")) return "passos";
            if (nome.StartsWith("Espada") || nome.StartsWith("Soco") || nome.StartsWith("Defesa") || nome.StartsWith("Morte") || nome == "Espinho" || nome == "Pouso") return "combate";
            if (nome.StartsWith("Fogo") || nome.StartsWith("Agua") || nome.StartsWith("Terra") || nome.StartsWith("Roxo") || nome.StartsWith("Azul") || nome.StartsWith("Ar_") || nome.StartsWith("Poder") || nome.StartsWith("Explosao")) return "poderes";
            if (nome.StartsWith("Ambiente") || nome == "Gota" || nome == "Cachoeira" || nome.StartsWith("Rumble") || nome == "Cristal" || nome.StartsWith("Tocha") || nome == "Movimento_Vento") return "ambiente";
            if (nome.StartsWith("UI_") || nome == "Checkpoint" || nome == "Vitoria" || nome == "Alerta_Tempo") return "ui";
            return "efeitos";
        }

        // ------------------------------------------------------------------ clipes
        public static AudioClip Clipe(string nome)
        {
            if (!cache.TryGetValue(nome, out var c))
            {
                c = Resources.Load<AudioClip>("Sons/" + nome);
                if (c == null) c = Sintese.Gerar(nome);
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
            string cat = Categoria(nome);
            Mix.TryGetValue(nome, out var mix);
            float ganho = mix.ganho > 0f ? mix.ganho : 1f;
            float vol = volume * ganho * PerfilJogo.Volume(cat);
            float t = tom + UnityEngine.Random.Range(-variacao, variacao);
            Instancia.StartCoroutine(Instancia.Rotina(c, pos, vol, t, duracaoMax, inicio, atraso));
            if (mix.grave > 0f && PerfilJogo.D.volGraves > 0.01f)
            {
                string sub = mix.grave < 0.5f ? "Sub_Leve" : mix.grave < 0.9f ? "Sub_Impacto" : "Sub_Explosao";
                var sc = Clipe(sub);
                if (sc != null)
                    Instancia.StartCoroutine(Instancia.Rotina(sc, null, volume * mix.grave * PerfilJogo.D.volGraves * 0.9f * PerfilJogo.Volume(cat), Mathf.Clamp(tom, 0.8f, 1.25f), Mathf.Min(duracaoMax > 0f ? duracaoMax : 99f, sub == "Sub_Explosao" ? 1.2f : 0.6f), 0f, atraso));
            }
        }

        /// <summary>Toca uma lista de sons, escolhendo um ao acaso.</summary>
        public static void TocarUm(string[] nomes, Vector3? pos = null, float volume = 1f, float tom = 1f)
        {
            if (nomes == null || nomes.Length == 0) return;
            Tocar(nomes[UnityEngine.Random.Range(0, nomes.Length)], pos, volume, tom);
        }

        AudioSource Pegar()
        {
            while (pool.Count > 0)
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
            if (atraso > 0f) yield return new WaitForSecondsRealtime(atraso);
            var a = Pegar();
            a.clip = c;
            a.volume = Mathf.Clamp(volume, 0f, 2f);
            a.pitch = tom;
            a.spatialBlend = pos.HasValue ? 0.6f : 0f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.maxDistance = 40f;
            a.ignoreListenerPause = true;
            if (pos.HasValue) a.transform.position = pos.Value;
            a.time = Mathf.Clamp(inicio, 0f, Mathf.Max(0f, c.length - 0.05f));
            a.Play();
            float total = (duracaoMax > 0f ? Mathf.Min(duracaoMax, c.length - inicio) : c.length - inicio) / Mathf.Max(0.1f, tom);
            float t = 0f;
            float fadeIni = total * 0.75f;
            float vBase = a.volume;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                if (duracaoMax > 0f && t > fadeIni) a.volume = vBase * Mathf.Clamp01((total - t) / (total - fadeIni));
                yield return null;
            }
            a.Stop();
            pool.Enqueue(a);
        }

        // ------------------------------------------------------------------ ambiente e música
        void IniciarAmbiente()
        {
            ambiente = CriarLoop("Ambiente_Treino", 0.28f, "ambiente");
            drone = CriarLoop("Ambiente_Drone", 0.08f, "ambiente");
            StartCoroutine(IniciarMusica("Musica_Treino", 0.32f));
        }

        /// <summary>Liga o ambiente de caverna (rumor grave, tochas, música sombria) e desliga o ambiente do treino.</summary>
        public static void AmbienteCaverna(bool ligar)
        {
            var s = Instancia;
            if (s.modoCaverna == ligar) return;
            s.modoCaverna = ligar;
            if (s.ambiente != null) s.ambiente.mute = ligar;
            if (s.drone != null) s.drone.mute = ligar;
            if (ligar)
            {
                if (s.rumble == null) s.rumble = s.CriarLoop("Rumble_Caverna", 0.5f, "ambiente");
                if (s.tocha == null) s.tocha = s.CriarLoop("Tocha_Crepita", 0.22f, "ambiente");
                if (s.rumble != null) s.rumble.mute = false;
                if (s.tocha != null) s.tocha.mute = false;
                if (s.musica != null) { s.musica.Stop(); Destroy(s.musica.gameObject); s.musica = null; }
                s.StartCoroutine(s.IniciarMusica("Musica_Caverna", 0.42f));
            }
            else
            {
                if (s.rumble != null) s.rumble.mute = true;
                if (s.tocha != null) s.tocha.mute = true;
                if (s.musica != null) { s.musica.Stop(); Destroy(s.musica.gameObject); s.musica = null; }
                s.StartCoroutine(s.IniciarMusica("Musica_Treino", 0.32f));
            }
        }

        IEnumerator IniciarMusica(string nome, float vol)
        {
            if (!cache.TryGetValue(nome, out var c) || c == null)
            {
                Sintese.GerarMusicaAsync(nome, out int taxa, out Task<float[]> tarefa);
                while (!tarefa.IsCompleted) yield return null;
                if (tarefa.Result == null) yield break;
                c = AudioClip.Create(nome, tarefa.Result.Length, 1, taxa, false);
                c.SetData(tarefa.Result, 0);
                cache[nome] = c;
            }
            if (musica != null) { musica.Stop(); Destroy(musica.gameObject); }
            var go = new GameObject("música " + nome);
            go.transform.SetParent(transform);
            musica = go.AddComponent<AudioSource>();
            musica.clip = c; musica.loop = true; musica.spatialBlend = 0f; musica.ignoreListenerPause = true;
            loops.Add((musica, vol, "musica"));
            AtualizarVolumes();
            musica.Play();
        }

        AudioSource CriarLoop(string nome, float vol, string categoria)
        {
            var c = Clipe(nome);
            if (c == null) return null;
            var go = new GameObject("loop " + nome);
            go.transform.SetParent(transform);
            var a = go.AddComponent<AudioSource>();
            a.clip = c; a.loop = true; a.spatialBlend = 0f; a.ignoreListenerPause = true;
            loops.Add((a, vol, categoria));
            a.volume = vol * PerfilJogo.Volume(categoria);
            a.Play();
            return a;
        }

        /// <summary>Reaplica os volumes das configurações nos sons em loop (ambiente e música).</summary>
        public static void AtualizarVolumes()
        {
            if (instancia == null) return;
            foreach (var (fonte, baseVol, cat) in instancia.loops)
                if (fonte != null) fonte.volume = Mathf.Clamp(baseVol * PerfilJogo.Volume(cat), 0f, 1.5f);
        }
    }
}
