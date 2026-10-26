using UnityEngine;
using UnityEngine.SceneManagement;
using FaithClash.Player;

namespace FaithClash.Training
{
    /// <summary>Portal do lobby: ao entrar, carrega outra cena (Treino, Fase 1...).</summary>
    [RequireComponent(typeof(Collider))]
    public class PortalCena : MonoBehaviour
    {
        public string cena;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider c)
        {
            if (string.IsNullOrEmpty(cena) || c.GetComponentInParent<PlayerController>() == null) return;
            Time.timeScale = 1f;
            SceneManager.LoadScene(cena);
        }
    }
}
