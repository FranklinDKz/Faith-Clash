using UnityEditor;
using FaithClash.Training;

namespace FaithClash.EditorTools
{
    public static class MenuTesteAnimacoes
    {
        [MenuItem("Faith Clash/Ferramentas/Testar Animações")]
        public static void Testar()
        {
            SessionState.SetInt(TestadorAnimacoes.ChaveSessao, 1);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Faith Clash/Ferramentas/Testar Combate")]
        public static void TestarCombate()
        {
            SessionState.SetInt(TestadorCombate.ChaveSessao, 1);
            EditorApplication.isPlaying = true;
        }
    }
}
