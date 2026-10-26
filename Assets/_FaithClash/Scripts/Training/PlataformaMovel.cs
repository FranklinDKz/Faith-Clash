using UnityEngine;

namespace FaithClash.Training
{
    /// <summary>Plataforma que vai e volta (Rigidbody cinemático) com o MovingPlatform do MotionCore: o jogador em cima é carregado.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlataformaMovel : MonoBehaviour
    {
        public Vector3 deslocamento = new Vector3(8f, 0f, 0f);
        public float periodo = 6f;
        Vector3 origem;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            origem = transform.position;
        }

        void FixedUpdate()
        {
            float t = Mathf.Sin(Time.time * Mathf.PI * 2f / periodo) * 0.5f + 0.5f;
            rb.MovePosition(origem + deslocamento * t);
        }
    }
}
