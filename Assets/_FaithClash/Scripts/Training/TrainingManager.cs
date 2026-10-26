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
            if (k.lKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
            {
                InimigoIA.Agressivos = !InimigoIA.Agressivos;
                FindAnyObjectByType<FaithClash.Player.StatusJogador>()?.Avisar(InimigoIA.Agressivos ? "Inimigos agressivos (L)" : "Inimigos parados (L)");
            }
            if (k.tKey.wasPressedThisFrame) { totalDamage = 0; Hits = 0; Dps = 0; }
            if (k.jKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) Horda(10);
        }

        GameObject[] modelos;

        void Start()
        {
            // guarda cópias (desativadas) dos inimigos da cena para chamar hordas depois, mesmo que os originais morram
            var lista = new System.Collections.Generic.List<GameObject>();
            foreach (var d in new System.Collections.Generic.List<TrainingDummy>(TrainingDummy.Todos))
            {
                if (d == null) continue;
                var c = Instantiate(d.gameObject, new Vector3(0, -200f, 0), Quaternion.identity);
                c.name = d.gameObject.name + " (modelo)";
                c.SetActive(false);
                lista.Add(c);
            }
            modelos = lista.ToArray();
        }

        /// <summary>J: chama uma horda de inimigos em volta do jogador para treinar combate rápido contra vários.</summary>
        void Horda(int quantidade)
        {
            var pj = FindAnyObjectByType<FaithClash.Player.PlayerController>();
            if (pj == null) return;
            if (modelos == null || modelos.Length == 0) return;
            int vivos = 0;
            foreach (var d in TrainingDummy.Todos) if (!d.Vida.IsDead) vivos++;
            InimigoIA.Agressivos = true;
            for (int i = 0; i < quantidade; i++)
            {
                float a = (i + Random.value * 0.6f) / quantidade * Mathf.PI * 2f;
                float r = Random.Range(9f, 14f);
                Vector3 pos = pj.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                pos.x = Mathf.Clamp(pos.x, -27f, 27f); pos.z = Mathf.Clamp(pos.z, -27f, 27f);
                var modelo = modelos[i % modelos.Length];
                if (modelo == null) continue;
                var novo = Instantiate(modelo, pos, Quaternion.LookRotation(pj.transform.position - pos));
                novo.name = modelo.name.Replace(" (modelo)", "") + " (horda)";
                novo.SetActive(true);
                var vida = novo.GetComponent<FaithClash.Core.Health>();
                if (vida != null) vida.ResetHealth();
                FaithClash.Core.CatalogoVFX.Tocar("poeira", pos, Quaternion.identity, null, 1.2f, 2f);
            }
            pj.GetComponent<FaithClash.Player.StatusJogador>()?.Avisar($"Horda! +{quantidade} inimigos (J)");
        }
    }
}
