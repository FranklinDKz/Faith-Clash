using UnityEngine;
using FaithClash.Core;

namespace FaithClash.UI
{
    /// <summary>Primeira cena do jogo: carrega o perfil e leva ao menu principal pela tela de carregamento (100%).</summary>
    public class CarregamentoInicial : MonoBehaviour
    {
        void Start()
        {
            PerfilJogo.Carregar();
            Cenas.Ir(Cenas.Menu);
        }
    }
}
