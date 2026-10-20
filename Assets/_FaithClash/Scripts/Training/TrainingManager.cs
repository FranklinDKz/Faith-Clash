using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FaithClash.Combat;

namespace FaithClash.Training
{
    /// <summary>Training mode HUD (IMGUI for now) and damage / DPS statistics.</summary>
    public class TrainingManager : MonoBehaviour
    {
        public static TrainingManager Instance { get; private set; }

        [SerializeField] MeleeCombat player;

        float totalDamage, windowStart, lastHitTime, dps;
        int hits;
        bool showHelp = true;

        void Awake() => Instance = this;

        public void RegisterDamage(float amount)
        {
            if (hits == 0 || Time.time - lastHitTime > 3f) { totalDamage = 0; hits = 0; windowStart = Time.time; }
            totalDamage += amount; hits++; lastHitTime = Time.time;
            dps = totalDamage / Mathf.Max(0.5f, Time.time - windowStart);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (k.f1Key.wasPressedThisFrame) showHelp = !showHelp;
            if (k.tKey.wasPressedThisFrame) { totalDamage = 0; hits = 0; dps = 0; }
        }

        void OnGUI()
        {
            var box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, padding = new RectOffset(10, 10, 8, 8) };
            GUI.Box(new Rect(10, 10, 230, 120), "FAITH CLASH - TREINO", box);
            GUI.Label(new Rect(20, 40, 220, 24), $"Dano total: {totalDamage:0}");
            GUI.Label(new Rect(20, 60, 220, 24), $"Golpes: {hits}   DPS: {dps:0.0}");
            if (player != null)
            {
                GUI.Label(new Rect(20, 80, 220, 24), $"Combo: {player.ComboCount}");
                GUI.Label(new Rect(20, 100, 220, 24), player.IsBlocking ? "Defendendo" : "");
            }

            if (!showHelp) return;
            GUI.Box(new Rect(Screen.width - 270, 10, 260, 190),
                "CONTROLES (F1 esconde)\n\nWASD mover | Shift correr\nEspaco pular | Ctrl esquiva\nMouse E: golpe leve (combo x3)\nMouse D: golpe pesado\nQ defender | T zerar stats\nR reiniciar | Esc soltar mouse", box);
        }
    }
}
