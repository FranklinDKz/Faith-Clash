using UnityEngine;

namespace FaithClash.Training
{
    /// <summary>Luz de tocha que oscila como fogo.</summary>
    public class LuzOscilante : MonoBehaviour
    {
        Light luz; float basei, fase;
        void Awake() { luz = GetComponent<Light>(); basei = luz != null ? luz.intensity : 1f; fase = Random.value * 100f; }
        void Update()
        {
            if (luz == null) return;
            luz.intensity = basei * (0.8f + 0.35f * Mathf.PerlinNoise(Time.time * 6f, fase) + 0.1f * Mathf.Sin(Time.time * 17f + fase));
        }
    }
}
