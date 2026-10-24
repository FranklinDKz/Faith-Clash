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
        static Dictionary<string, (float dur, float imp)> dados;

        static void Carregar()
        {
            dados = new Dictionary<string, (float, float)>();
            var txt = Resources.Load<TextAsset>("InfoGolpes");
            if (txt == null) { Debug.LogWarning("[InfoGolpes] Resources/InfoGolpes.txt ausente: rode Faith Clash > Preparar Jogador."); return; }
            foreach (var linha in txt.text.Split('\n'))
            {
                var p = linha.Trim().Split(';');
                if (p.Length < 3) continue;
                dados[p[0]] = (float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Duração do clipe somada à da recuperação ("Rec"), se houver.</summary>
        public static float Duracao(string clipe, float padrao = 1f)
        {
            if (dados == null) Carregar();
            if (!dados.TryGetValue(clipe, out var d)) return padrao;
            return d.dur + (dados.TryGetValue(clipe + "Rec", out var r) ? r.dur : 0f);
        }

        public static float Impacto(string clipe, float padrao = 0.3f)
        {
            if (dados == null) Carregar();
            return dados.TryGetValue(clipe, out var d) && d.imp >= 0f ? d.imp : padrao;
        }
    }
}
