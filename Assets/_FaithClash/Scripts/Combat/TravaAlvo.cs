using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Foco no alvo (lock-on) estilo Elden Ring / Naruto Storm: Q trava/destrava no inimigo à frente,
    /// a roda do mouse troca de alvo. Com alvo travado a câmera acompanha o alvo, o personagem anda de lado
    /// olhando para ele e os golpes avançam até o alvo.
    /// </summary>
    public class TravaAlvo : MonoBehaviour
    {
        [SerializeField] float alcance = 25f;
        [SerializeField] float alturaFoco = 1.1f;

        Camera cam;

        public Transform Alvo { get; private set; }
        public bool Travado => Alvo != null;
        public Vector3 PontoFoco => Alvo != null ? Alvo.position + Vector3.up * (alturaFoco - 1f) : Vector3.zero;

        void Update()
        {
            if (cam == null) cam = Camera.main;

            bool alternar = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                            || (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame);
            if (alternar)
            {
                if (Travado) Alvo = null;
                else Alvo = Procurar(null, 0);
            }

            if (Travado && Mouse.current != null)
            {
                float roda = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(roda) > 0.1f)
                {
                    var outro = Procurar(Alvo, roda > 0 ? 1 : -1);
                    if (outro != null) Alvo = outro;
                }
            }

            if (Travado)
            {
                var boneco = Alvo.GetComponent<TrainingDummy>();
                bool morto = boneco != null && boneco.Vida.IsDead;
                if (morto || Vector3.Distance(transform.position, Alvo.position) > alcance * 1.3f) Alvo = null;
            }
        }

        /// <summary>
        /// Escolhe o alvo mais próximo do centro da câmera. Com <paramref name="lado"/> ≠ 0, escolhe o próximo
        /// alvo à direita (1) ou à esquerda (-1) do atual.
        /// </summary>
        Transform Procurar(Transform atual, int lado)
        {
            Vector3 frente = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : transform.forward;
            Transform melhor = null;
            float melhorNota = float.MaxValue;
            foreach (var d in TrainingDummy.Todos)
            {
                if (d.Vida.IsDead || d.transform == atual) continue;
                Vector3 v = d.transform.position - transform.position;
                v.y = 0f;
                float dist = v.magnitude;
                if (dist > alcance) continue;
                float angulo = Vector3.SignedAngle(frente, v, Vector3.up);
                if (lado != 0 && atual != null)
                {
                    float anguloAtual = Vector3.SignedAngle(frente, atual.position - transform.position, Vector3.up);
                    float delta = (angulo - anguloAtual) * lado;
                    if (delta <= 0f) continue;
                    if (delta < melhorNota) { melhorNota = delta; melhor = d.transform; }
                    continue;
                }
                float nota = Mathf.Abs(angulo) + dist * 2f;
                if (Mathf.Abs(angulo) < 100f && nota < melhorNota) { melhorNota = nota; melhor = d.transform; }
            }
            return melhor;
        }
    }
}
