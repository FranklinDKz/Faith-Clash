using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>
    /// Foco no alvo (lock-on) estilo Elden Ring / Naruto Storm:
    ///  - Q trava no inimigo mais próximo do centro da câmera.
    ///  - Com o alvo travado, Q de novo (ou a roda do mouse) troca para o próximo alvo; se não houver outro, solta.
    ///  - Segurar Q solta o foco.
    /// Sem alvo travado ainda existe uma assistência leve de mira nos ataques (veja <see cref="MeleeCombat"/>).
    /// </summary>
    public class TravaAlvo : MonoBehaviour
    {
        [SerializeField] float alcance = 25f;
        [SerializeField] float alturaFoco = 1.1f;
        [SerializeField] float tempoSegurar = 0.4f;

        Camera cam;
        float qDesde = -1f;
        bool soltou;

        public Transform Alvo { get; private set; }
        public bool Travado => Alvo != null;
        public Vector3 PontoFoco
        {
            get
            {
                if (Alvo == null) return Vector3.zero;
                // centro do peito: pela cápsula do inimigo (não os pés)
                var ccA = Alvo.GetComponentInParent<CharacterController>();
                if (ccA != null) return ccA.bounds.center + Vector3.up * ccA.bounds.extents.y * 0.35f;
                var col = Alvo.GetComponentInParent<Collider>();
                if (col != null) return col.bounds.center + Vector3.up * col.bounds.extents.y * 0.35f;
                return Alvo.position + Vector3.up * 1.3f;
            }
        }
        /// <summary>Alvo trocado; o segundo parâmetro é o ângulo (graus) entre o alvo antigo e o novo.</summary>
        public event Action<float> AlvoTrocado;

        void Update()
        {
            if (cam == null) cam = Camera.main;
            var k = Keyboard.current;

            if (k != null && k.qKey.wasPressedThisFrame) { qDesde = Time.time; soltou = false; }
            if (k != null && k.qKey.isPressed && qDesde > 0f && !soltou && Travado && Time.time - qDesde > tempoSegurar)
            {
                Alvo = null;       // segurou: solta o foco
                soltou = true;
            }
            if (k != null && k.qKey.wasReleasedThisFrame)
            {
                if (!soltou)
                {
                    if (!Travado) { Alvo = Procurar(null, 0); if (Alvo != null) SomJogo.Tocar("UI_Travar", null, 0.5f); }
                    else TrocarAlvo(1);
                }
                qDesde = -1f;
            }
            if (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame)
            {
                if (!Travado) Alvo = Procurar(null, 0);
                else TrocarAlvo(1);
            }

            if (Travado && Mouse.current != null)
            {
                float roda = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(roda) > 0.1f) TrocarAlvo(roda > 0 ? 1 : -1);
            }

            if (Travado)
            {
                var boneco = Alvo.GetComponent<TrainingDummy>();
                bool morto = boneco != null && boneco.Vida.IsDead;
                if (morto || Vector3.Distance(transform.position, Alvo.position) > alcance * 1.3f) Alvo = null;
            }
        }

        void TrocarAlvo(int lado)
        {
            var antigo = Alvo;
            var novo = Procurar(antigo, lado) ?? Procurar(antigo, -lado);
            if (novo == null) { Alvo = null; return; }   // nenhum outro alvo: solta
            Alvo = novo;
            SomJogo.Tocar("UI_Clique", null, 0.7f);
            float ang = Vector3.Angle(Vector3.ProjectOnPlane(antigo.position - transform.position, Vector3.up),
                                      Vector3.ProjectOnPlane(novo.position - transform.position, Vector3.up));
            AlvoTrocado?.Invoke(ang);
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
