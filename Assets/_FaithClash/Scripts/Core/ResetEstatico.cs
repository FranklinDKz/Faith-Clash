using UnityEngine;
using FaithClash.UI;

namespace FaithClash.Core
{
    /// <summary>O projeto entra no Play sem recarregar o domínio: zera aqui os estáticos para cada execução começar limpa.</summary>
    static class ResetEstatico
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Zerar()
        {
            EstadoJogo.MenuAberto = false; EstadoJogo.DeslocCamera = 0f; EstadoJogo.VistaPersonagem = false; EstadoJogo.CameraDeslize = false;
            RegrasFase.Limpar();
            FaithClash.Training.AguaOndas.Zerar();
            FaithClash.Combat.EstilosElementais.DominioAtivo = false;
            FaithClash.Combat.MundoEfeitos.ResetarEstatico();
            SomJogo.ResetarEstatico();
            ConfigRuntime.ResetarEstatico();
            TelaCarregamento.ResetarEstatico();
            AjustesMovimento.ResetarEstatico();
            PerfilJogo.ResetarEstatico();
            Time.timeScale = 1f;
        }
    }
}
