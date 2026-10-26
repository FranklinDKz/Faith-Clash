using UnityEngine;

namespace FaithClash.Training
{
    /// <summary>Balança suavemente (cipós e raízes pendurados).</summary>
    public class Balanco : MonoBehaviour
    {
        Quaternion baseRot; float fase, amp;
        void Awake() { baseRot = transform.localRotation; fase = Random.value * 10f; amp = Random.Range(2f, 5f); }
        void Update()
        {
            float a = Mathf.Sin(Time.time * 0.9f + fase) * amp;
            transform.localRotation = baseRot * Quaternion.Euler(a, 0f, a * 0.6f);
        }
    }
}
