namespace FaithClash.Core
{
    /// <summary>Estado global simples: menu aberto (pausa o jogo e libera o mouse) e a vista de personagem da câmera.</summary>
    public static class EstadoJogo
    {
        public static bool MenuAberto;
        /// <summary>Deslocamento horizontal da câmera (NDC) para o personagem aparecer ao lado do painel do menu.</summary>
        public static float DeslocCamera;
        /// <summary>Câmera gira devagar ao redor do personagem (aba Personagem do menu).</summary>
        public static bool VistaPersonagem;
        /// <summary>Rasteira por baixo de um obstáculo: a câmera chega perto e baixa para um bom ângulo.</summary>
        public static bool CameraDeslize;
    }
}
