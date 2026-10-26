namespace FaithClash.Core
{
    /// <summary>Regras da fase atual consultadas pelo HUD e pelos poderes. Cada fase liga o que precisa em Awake e limpa em OnDestroy.</summary>
    public static class RegrasFase
    {
        public static bool PoderesBloqueados;
        public static bool SemMapa;
        public static bool SemItens;

        public static void Limpar() { PoderesBloqueados = false; SemMapa = false; SemItens = false; }
    }
}
