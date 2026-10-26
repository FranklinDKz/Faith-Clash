using UnityEngine;

namespace FaithClash.Training
{
    /// <summary>Faz o texto do lobby (rótulos de portais e da galeria) ficar virado para a câmera.</summary>
    public class RotuloOlhaCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main;
            if (c != null) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position);
        }
    }
}
