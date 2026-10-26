using UnityEngine;
using FaithClash.UI;

namespace FaithClash.Core
{
    /// <summary>Troca de cenas sempre pela tela de carregamento (imagens, dicas e barra até 100%).</summary>
    public static class Cenas
    {
        public const string Boot = "Carregamento", Menu = "MenuPrincipal", Treino = "Treino", Campanha = "Fase1_Corrida";

        public static bool Carregando => TelaCarregamento.Ocupada;

        public static void Ir(string cena) { TelaCarregamento.Carregar(cena); }

        public static string TituloDe(string cena)
        {
            switch (cena)
            {
                case Menu: return "MENU PRINCIPAL";
                case Treino: return "MODO PRATICAR — ARENA DE TREINO";
                case Campanha: return "CAMPANHA — FASE 1";
                default: return cena.ToUpperInvariant();
            }
        }

        public static void SairDoJogo()
        {
            PerfilJogo.Salvar();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
