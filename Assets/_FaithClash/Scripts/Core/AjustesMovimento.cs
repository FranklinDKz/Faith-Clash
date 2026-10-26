using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>
    /// Todos os números do movimento do jogador num só lugar: velocidade, pulo, colisão (cápsula), obstáculos, rasteira, parede e pose.
    /// Fica em Resources/AjustesMovimento.asset. Pode ser editado no Inspector durante o Play: vale na hora e fica salvo.
    /// Menu: Faith Clash > Ajustes de Movimento.
    /// </summary>
    [CreateAssetMenu(fileName = "AjustesMovimento", menuName = "Faith Clash/Ajustes de Movimento")]
    public class AjustesMovimento : ScriptableObject
    {
        static AjustesMovimento atual;
        public static AjustesMovimento Atual
        {
            get
            {
                if (atual == null) atual = Resources.Load<AjustesMovimento>("AjustesMovimento");
                if (atual == null) { atual = CreateInstance<AjustesMovimento>(); atual.hideFlags = HideFlags.DontSave; }
                return atual;
            }
        }
        internal static void ResetarEstatico() { atual = null; }

        /// <summary>Muda sempre que algo é editado no Inspector (quem guarda cópia sabe que precisa reaplicar).</summary>
        [System.NonSerialized] public int versao;
        void OnValidate() { versao++; }

        [Header("Velocidades (m/s)")]
        [Range(0.5f, 4f)] public float velAndar = 1.96f;
        [Range(2f, 10f)] public float velCorrida = 5.4f;
        [Range(3f, 14f)] public float velSprint = 6.6f;
        [Tooltip("Quão rápido chega na velocidade (m/s²)")] [Range(5f, 80f)] public float aceleracao = 38f;
        [Tooltip("Velocidade de giro do corpo")] [Range(4f, 40f)] public float giro = 22f;

        [Header("Pulo e gravidade")]
        [Tooltip("Altura do pulo segurando Espaço (m)")] [Range(0.4f, 3f)] public float alturaPulo = 1.0f;
        [Range(-60f, -8f)] public float gravidade = -26f;
        [Tooltip("Cai mais rápido do que sobe")] [Range(1f, 3f)] public float multiplicadorQueda = 1.45f;
        [Tooltip("Soltar o Espaço cedo corta o pulo")] [Range(1f, 4f)] public float multiplicadorPuloCurto = 2.2f;
        [Tooltip("Controle da direção no ar (0 = nenhum, 1 = total)")] [Range(0f, 1f)] public float controleNoAr = 0.45f;

        [Header("Colisão (cápsula do personagem)")]
        [Range(1.2f, 2.2f)] public float alturaCapsula = 1.75f;
        [Range(0.15f, 0.6f)] public float raioCapsula = 0.3f;
        [Tooltip("Degrau que sobe andando (m)")] [Range(0.05f, 0.7f)] public float degrau = 0.4f;
        [Tooltip("Rampa máxima que sobe andando (graus)")] [Range(20f, 75f)] public float rampaMaxima = 55f;

        [Header("Rasteira (Ctrl correndo / Espaço sob barras)")]
        [Range(0.4f, 2f)] public float duracaoRasteira = 0.95f;
        [Tooltip("Velocidade mínima da rasteira (m/s)")] [Range(2f, 12f)] public float velRasteira = 6f;
        [Tooltip("Altura da cápsula deslizando (m)")] [Range(0.5f, 1.2f)] public float alturaCapsulaRasteira = 0.75f;

        [Header("Pose (correções visuais)")]
        [Tooltip("Inclinação do tronco para a frente correndo (graus)")] [Range(0f, 25f)] public float inclinacaoCorrida = 7f;
        [Tooltip("Inclinação do tronco para a frente no sprint (graus)")] [Range(0f, 30f)] public float inclinacaoSprint = 12f;
        [Tooltip("Gira os pés para dentro (graus) — corrige pés virados para fora")] [Range(-40f, 40f)] public float giroPesParaDentro = 0f;
        [Tooltip("Tira o giro do quadril nas passadas (0 = nada, 1 = tudo)")] [Range(0f, 1f)] public float estabilizarGiroQuadril = 0.5f;
        [Tooltip("Velocidade das animações de andar/correr (1 = normal)")] [Range(0.5f, 2f)] public float velocidadeAnimCorrida = 1f;
    }
}
