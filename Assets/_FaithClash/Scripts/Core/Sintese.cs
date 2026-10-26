using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>Sons gerados por código (ondas, ruído filtrado, envelopes): efeitos de caverna, movimento, interface, graves de impacto e músicas ambiente.</summary>
    public static class Sintese
    {
        const int Taxa = 32000;
        const float Tau = Mathf.PI * 2f;
        static System.Random rnd = new System.Random(7);

        static float Ruido() => (float)(rnd.NextDouble() * 2.0 - 1.0);

        static AudioClip Criar(string nome, float dur, Func<float, float> f, int taxa = Taxa, bool normalizar = false)
        {
            int n = Mathf.CeilToInt(dur * taxa);
            var d = new float[n];
            float pico = 0.0001f;
            for (int i = 0; i < n; i++) { d[i] = f(i / (float)taxa); pico = Mathf.Max(pico, Mathf.Abs(d[i])); }
            float g = normalizar ? 0.9f / pico : 1f;
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i] * g, -1f, 1f);
            var c = AudioClip.Create(nome, n, 1, taxa, false);
            c.SetData(d, 0);
            return c;
        }

        static float Nota(int semitons) => 440f * Mathf.Pow(2f, semitons / 12f);   // 0 = lá4

        public static AudioClip Gerar(string nome)
        {
            switch (nome)
            {
                case "Gota":
                {
                    float ph = 0f;
                    return Criar(nome, 0.55f, t =>
                    {
                        float fr = Mathf.Lerp(2100f, 850f, Mathf.Clamp01(t / 0.07f));
                        ph += fr / Taxa;
                        float e = Mathf.Exp(-t * 34f) + (t > 0.17f ? Mathf.Exp(-(t - 0.17f) * 26f) * 0.3f : 0f) + (t > 0.31f ? Mathf.Exp(-(t - 0.31f) * 22f) * 0.12f : 0f);
                        return Mathf.Sin(Tau * ph) * e * 0.55f;
                    });
                }
                case "Passos_Pedra":
                {
                    float y = 0f;
                    return Criar(nome, 0.2f, t =>
                    {
                        y += 0.16f * (Ruido() - y);
                        return (y * Mathf.Exp(-t * 38f) * 2.2f + Mathf.Sin(Tau * 85f * t) * Mathf.Exp(-t * 28f) * 0.7f) * Mathf.Clamp01(t * 800f) * 0.8f;
                    });
                }
                case "Pouso":
                {
                    float y = 0f, ph = 0f;
                    return Criar(nome, 0.6f, t =>
                    {
                        y += 0.07f * (Ruido() - y);
                        ph += Mathf.Lerp(95f, 34f, Mathf.Clamp01(t / 0.35f)) / Taxa;
                        return (y * Mathf.Exp(-t * 12f) * 3f + Mathf.Sin(Tau * ph) * Mathf.Exp(-t * 8f) * 0.95f) * Mathf.Clamp01(t * 600f) * 0.85f;
                    });
                }
                case "Whoosh":
                    return Passagem(nome, 0.5f, 0.02f, 0.28f, 0.9f);
                case "Pulo":
                    return Passagem(nome, 0.28f, 0.04f, 0.3f, 0.7f);
                case "Deslize":
                {
                    float y = 0f, y2 = 0f;
                    return Criar(nome, 0.8f, t =>
                    {
                        float x = Ruido();
                        y += 0.22f * (x - y); y2 += 0.04f * (x - y2);
                        float env = Mathf.Clamp01(t / 0.06f) * Mathf.Clamp01((0.8f - t) / 0.3f);
                        return (y - y2) * env * (0.7f + 0.3f * Mathf.Sin(Tau * 23f * t)) * 1.4f;
                    });
                }
                case "Escalar":
                {
                    float y = 0f, y2 = 0f;
                    return Criar(nome, 0.6f, t =>
                    {
                        float x = Ruido();
                        y += 0.5f * (x - y); y2 += 0.2f * (x - y2);
                        float ph = t % 0.17f;
                        return (y - y2) * Mathf.Exp(-ph * 16f) * Mathf.Clamp01(ph * 400f) * Mathf.Clamp01((0.6f - t) / 0.12f) * 1.6f;
                    });
                }
                case "Espinho":
                {
                    float y = 0f;
                    return Criar(nome, 0.9f, t =>
                    {
                        y += 0.25f * (Ruido() - y);
                        float metal = (Mathf.Sin(Tau * 1100f * t) + 0.7f * Mathf.Sin(Tau * 1720f * t) + 0.5f * Mathf.Sin(Tau * 2310f * t)) * Mathf.Exp(-t * 9f) * 0.18f;
                        float golpe = Ruido() * Mathf.Exp(-t * 70f) * 0.7f;
                        float baixo = Mathf.Sin(Tau * Mathf.Lerp(90f, 40f, Mathf.Clamp01(t / 0.3f)) * t) * Mathf.Exp(-t * 9f) * 0.9f;
                        float carne = y * Mathf.Exp(-t * 14f) * 1.8f;
                        return (metal + golpe + baixo + carne) * Mathf.Clamp01(t * 1000f);
                    });
                }
                case "Cristal":
                    return Criar(nome, 1.8f, t =>
                    {
                        float v = Mathf.Sin(Tau * 1318f * t) * Mathf.Exp(-t * 2.2f) + 0.6f * Mathf.Sin(Tau * 1975f * t) * Mathf.Exp(-t * 3f)
                                  + 0.4f * Mathf.Sin(Tau * 2637f * t) * Mathf.Exp(-t * 4f) + 0.25f * Mathf.Sin(Tau * 3951f * t) * Mathf.Exp(-t * 6f);
                        return v * 0.3f * Mathf.Clamp01(t * 500f);
                    });
                case "Checkpoint":
                    return Criar(nome, 1.0f, t =>
                    {
                        float s = 0f;
                        int[] n = { 0, 4, 7, 12 };
                        for (int i = 0; i < 4; i++)
                        {
                            float tt = t - i * 0.09f;
                            if (tt < 0f) continue;
                            float f = Nota(n[i] + 3);
                            s += (Mathf.Sin(Tau * f * tt) + 0.3f * Mathf.Sin(Tau * f * 2f * tt)) * Mathf.Exp(-tt * 5f) * Mathf.Clamp01(tt * 400f);
                        }
                        return s * 0.28f;
                    });
                case "Vitoria":
                    return Criar(nome, 2.6f, t =>
                    {
                        float s = 0f;
                        int[] n = { -2, 3, 7, 10, 15 };
                        float[] ini = { 0f, 0.14f, 0.28f, 0.42f, 0.66f };
                        for (int i = 0; i < 5; i++)
                        {
                            float tt = t - ini[i];
                            if (tt < 0f) continue;
                            float f = Nota(n[i]);
                            float dur = i == 4 ? 1.9f : 0.5f;
                            s += (Mathf.Sin(Tau * f * tt) + 0.4f * Mathf.Sin(Tau * f * 2f * tt) + 0.2f * Mathf.Sin(Tau * f * 3f * tt)) * Mathf.Exp(-tt * (4f / dur)) * Mathf.Clamp01(tt * 300f);
                        }
                        return s * 0.22f;
                    });
                case "Alerta_Tempo":
                    return Criar(nome, 0.35f, t =>
                    {
                        float tt = t % 0.17f;
                        return t < 0.3f ? Mathf.Sin(Tau * 988f * t) * Mathf.Exp(-tt * 18f) * 0.45f : 0f;
                    });
                case "UI_Hover":
                    return Criar(nome, 0.05f, t => Mathf.Sin(Tau * 2400f * t) * Mathf.Exp(-t * 90f) * 0.35f);
                case "Cachoeira":
                {
                    float y = 0f, z = 0f;
                    return Criar(nome, 6f, t =>
                    {
                        float x = Ruido();
                        y += 0.32f * (x - y); z += 0.05f * (x - z);
                        return (y - z * 0.6f) * (0.75f + 0.25f * Mathf.Sin(Tau * t / 6f)) * 0.8f;
                    }, Taxa, true);
                }
                case "Tocha_Crepita":
                {
                    float y = 0f, estalo = 0f;
                    return Criar(nome, 4f, t =>
                    {
                        y += 0.05f * (Ruido() - y);
                        if (rnd.NextDouble() < 0.0007) estalo = 0.5f + (float)rnd.NextDouble() * 0.5f;
                        estalo *= 0.992f;
                        return y * 1.3f + Ruido() * estalo * 0.4f;
                    });
                }
                case "Rumble_Caverna":
                {
                    float y = 0f, z = 0f;
                    return Criar(nome, 8f, t =>
                    {
                        y += 0.012f * (Ruido() - y); z += 0.05f * (y - z);
                        return z * 14f * (0.75f + 0.25f * Mathf.Sin(Tau * t / 8f)) + Mathf.Sin(Tau * 48f * t) * 0.07f;
                    }, Taxa, true);
                }
                case "Sub_Leve": return Sub(nome, 0.35f, 120f, 58f, 13f);
                case "Sub_Impacto": return Sub(nome, 0.6f, 88f, 38f, 7f);
                case "Sub_Explosao": return Sub(nome, 1.3f, 72f, 24f, 3.2f);
            }
            return null;
        }

        static AudioClip Sub(string nome, float dur, float f0, float f1, float queda)
        {
            float ph = 0f, y = 0f;
            return Criar(nome, dur, t =>
            {
                ph += Mathf.Lerp(f0, f1, 1f - Mathf.Exp(-t * 7f)) / Taxa;
                y += 0.08f * (Ruido() - y);
                return (Mathf.Sin(Tau * ph) * Mathf.Exp(-t * queda) + y * Mathf.Exp(-t * 60f) * 1.2f) * Mathf.Clamp01(t * 900f);
            });
        }

        /// <summary>Ruído filtrado que passa como uma rajada de vento / movimento do corpo.</summary>
        static AudioClip Passagem(string nome, float dur, float kBaixo, float kAlto, float amp)
        {
            float y1 = 0f, y2 = 0f;
            return Criar(nome, dur, t =>
            {
                float u = t / dur;
                float k1 = Mathf.Lerp(kBaixo * 3f, kAlto, Mathf.Sin(u * Mathf.PI * 0.5f));
                float k2 = k1 * 0.25f;
                float x = Ruido();
                y1 += k1 * (x - y1); y2 += k2 * (x - y2);
                float env = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.6f);
                return (y1 - y2) * env * amp * 2.4f;
            });
        }

        // ------------------------------------------------------------------ músicas ambiente (geradas fora da thread principal)
        public static void GerarMusicaAsync(string nome, out int taxa, out Task<float[]> tarefa)
        {
            const int t = 22050;
            taxa = t;
            if (nome == "Musica_Caverna") tarefa = Task.Run(() => MusicaCaverna(t));
            else tarefa = Task.Run(() => MusicaTreino(t));
        }

        static float[] MusicaCaverna(int taxa)
        {
            const float dur = 32f;
            int n = (int)(dur * taxa);
            var d = new float[n];
            var r = new System.Random(11);
            // Lá menor — Fá — Dó — Sol; cada acorde 8 s, notas em semitons a partir de lá2
            int[][] acordes = { new[] { 0, 3, 7, 12 }, new[] { -4, 0, 3, 8 }, new[] { 3, 7, 10, 15 }, new[] { -2, 2, 5, 10 } };
            for (int c = 0; c < 4; c++)
            {
                int i0 = (int)(c * 8f * taxa), i1 = (int)((c + 1) * 8f * taxa);
                foreach (int semi in acordes[c])
                {
                    float f = 110f * Mathf.Pow(2f, semi / 12f);
                    for (int i = i0 - taxa; i < i1 + taxa; i++)
                    {
                        int idx = (i % n + n) % n;
                        float t = (i - i0) / (float)taxa;
                        float env = Mathf.SmoothStep(0f, 1f, (t + 1f) / 2.2f) * Mathf.SmoothStep(0f, 1f, (8f + 1f - t) / 2.2f);
                        float s = 0f;
                        for (int h = 1; h <= 3; h++)
                            s += (Mathf.Sin(Tau * (f * h + 0.9f) * i / taxa) + Mathf.Sin(Tau * (f * h - 0.9f) * i / taxa)) * (0.5f / h);
                        d[idx] += s * env * 0.05f;
                    }
                }
            }
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)taxa;
                d[i] += Mathf.Sin(Tau * 55f * t) * 0.07f * (0.7f + 0.3f * Mathf.Sin(Tau * t / 16f));
                d[i] *= 0.8f + 0.2f * Mathf.Sin(Tau * 0.125f * t);
            }
            // sinos esparsos (escala pentatônica menor), com decaimento longo
            int[] pent = { 12, 15, 17, 19, 22, 24 };
            float tn = 2.5f;
            while (tn < dur - 4f)
            {
                float f = 110f * Mathf.Pow(2f, pent[r.Next(pent.Length)] / 12f);
                int i0 = (int)(tn * taxa);
                for (int i = 0; i < 3 * taxa && i0 + i < n; i++)
                {
                    float tt = i / (float)taxa;
                    float s = Mathf.Sin(Tau * f * tt) * Mathf.Exp(-tt * 1.6f) + 0.4f * Mathf.Sin(Tau * f * 2.76f * tt) * Mathf.Exp(-tt * 3.5f);
                    d[i0 + i] += s * 0.13f * Mathf.Clamp01(tt * 300f);
                }
                tn += 3f + (float)r.NextDouble() * 4f;
            }
            return Limitar(d, 0.5f);
        }

        static float[] MusicaTreino(int taxa)
        {
            const float dur = 32f;
            int n = (int)(dur * taxa);
            var d = new float[n];
            var r = new System.Random(5);
            // escala hirajoshi em ré: ré, mi, fá, lá, si♭ — "koto" dedilhado sobre drone
            int[] esc = { 0, 2, 3, 7, 8, 12, 14, 15 };
            float passo = 60f / 84f / 2f;
            for (float tn = 0f; tn < dur - 1f; tn += passo)
            {
                if (r.NextDouble() < 0.42) continue;
                float f = 146.83f * Mathf.Pow(2f, esc[r.Next(esc.Length)] / 12f);
                int i0 = (int)(tn * taxa);
                for (int i = 0; i < (int)(1.6f * taxa) && i0 + i < n; i++)
                {
                    float tt = i / (float)taxa;
                    float s = (Mathf.Sin(Tau * f * tt) + 0.5f * Mathf.Sin(Tau * f * 2f * tt) * Mathf.Exp(-tt * 6f) + 0.25f * Mathf.Sin(Tau * f * 3f * tt) * Mathf.Exp(-tt * 9f)) * Mathf.Exp(-tt * 3.2f);
                    d[i0 + i] += s * 0.16f * Mathf.Clamp01(tt * 800f);
                }
            }
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)taxa;
                float pad = Mathf.Sin(Tau * 73.42f * t) * 0.09f + Mathf.Sin(Tau * 110f * t) * 0.045f + Mathf.Sin(Tau * 146.83f * t) * 0.03f;
                d[i] += pad * (0.75f + 0.25f * Mathf.Sin(Tau * t / 16f));
            }
            return Limitar(d, 0.5f);
        }

        static float[] Limitar(float[] d, float alvo)
        {
            float pico = 0.0001f;
            for (int i = 0; i < d.Length; i++) pico = Mathf.Max(pico, Mathf.Abs(d[i]));
            float g = alvo / pico;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
            return d;
        }
    }
}
