using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>
    /// Duração e momento do impacto (em segundos, velocidade 1) de cada clipe de golpe. Os números são gerados por
    /// Faith Clash > Preparar Jogador a partir da tabela de cortes (Resources/InfoGolpes.txt), então sempre batem com a animação.
    /// </summary>
    public static class InfoGolpes
    {
        static Dictionary<string, (float dur, float[] imp)> dados;

        static void Carregar()
        {
            dados = new Dictionary<string, (float, float[])>();
            var txt = Resources.Load<TextAsset>("InfoGolpes");
            if (txt == null) { Debug.LogWarning("[InfoGolpes] Resources/InfoGolpes.txt ausente: rode Faith Clash > Preparar Jogador."); return; }
            foreach (var linha in txt.text.Split('\n'))
            {
                var p = linha.Trim().Split(';');
                if (p.Length < 3) continue;
                var imps = new List<float>();
                foreach (var s in p[2].Split(',')) if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) && f >= 0f) imps.Add(f);
                dados[p[0]] = (float.Parse(p[1], CultureInfo.InvariantCulture), imps.ToArray());
            }
        }

        /// <summary>Duração do clipe somada à da recuperação ("Rec"), se houver.</summary>
        public static float Duracao(string clipe, float padrao = 1f)
        {
            if (dados == null) Carregar();
            if (!dados.TryGetValue(clipe, out var d)) return padrao;
            return d.dur + (dados.TryGetValue(clipe + "Rec", out var r) ? r.dur : 0f);
        }

        /// <summary>Momentos (s) de todos os acertos do clipe (combos de vários golpes); vazio se o clipe não tem impacto.</summary>
        public static float[] Impactos(string clipe)
        {
            if (dados == null) Carregar();
            return dados.TryGetValue(clipe, out var d) ? d.imp : new float[0];
        }

        public static float Impacto(string clipe, float padrao = 0.3f)
        {
            if (dados == null) Carregar();
            return dados.TryGetValue(clipe, out var d) && d.imp.Length > 0 ? d.imp[0] : padrao;
        }
    }
}
