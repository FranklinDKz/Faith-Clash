using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FaithClash.Training
{
    /// <summary>Estatísticas de dano / DPS do Modo Treino. O HUD lê estes valores.</summary>
    public class TrainingManager : MonoBehaviour
    {
        public static TrainingManager Instance { get; private set; }

        float totalDamage, windowStart, lastHitTime;

        public float TotalDamage => totalDamage;
        public float Dps { get; private set; }
        public int Hits { get; private set; }

        void Awake()
        {
            Instance = this;
            var som = FaithClash.Core.SomJogo.Instancia;   // liga o som ambiente
        }

        public void RegisterDamage(float amount)
        {
            if (Hits == 0 || Time.time - lastHitTime > 3f) { totalDamage = 0; Hits = 0; windowStart = Time.time; }
            totalDamage += amount; Hits++; lastHitTime = Time.time;
            Dps = totalDamage / Mathf.Max(0.5f, Time.time - windowStart);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.f5Key.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (k.tKey.wasPressedThisFrame) { totalDamage = 0; Hits = 0; Dps = 0; }
        }
    }
}
