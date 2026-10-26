using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FaithClash.Core
{
    /// <summary>
    /// Suporte a controles (Xbox, PS4/PS5, sem fio por Bluetooth ou genéricos HID — o Input System os mapeia para <see cref="Gamepad"/>):
    /// com o menu aberto, o analógico esquerdo move o cursor e o botão A/X clica; qualquer botão do controle prende o cursor de volta ao jogo.
    /// </summary>
    public class ControleGamepad : MonoBehaviour
    {
        Vector2 pos;
        bool clicando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            if (FindAnyObjectByType<ControleGamepad>() != null) return;
            var go = new GameObject("Controle (gamepad)");
            DontDestroyOnLoad(go);
            go.AddComponent<ControleGamepad>();
        }

        void Update()
        {
            var pad = Gamepad.current; var mouse = Mouse.current;
            if (pad == null || mouse == null) return;
            if (!EstadoJogo.MenuAberto)
            {
                if (clicando) { InputState.Change(mouse.leftButton, 0f); clicando = false; }
                pos = mouse.position.ReadValue();
                return;
            }
            Vector2 s = pad.leftStick.ReadValue() + pad.dpad.ReadValue();
            if (s.sqrMagnitude > 0.04f)
            {
                pos = mouse.position.ReadValue() + s * (900f * Time.unscaledDeltaTime);
                pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - 1f); pos.y = Mathf.Clamp(pos.y, 0f, Screen.height - 1f);
                mouse.WarpCursorPosition(pos);
                InputState.Change(mouse.position, pos);
            }
            Vector2 rolar = pad.rightStick.ReadValue();
            if (Mathf.Abs(rolar.y) > 0.2f) InputState.Change(mouse.scroll, new Vector2(0f, rolar.y * 20f));
            bool bt = pad.buttonSouth.isPressed;
            if (bt != clicando) { InputState.Change(mouse.leftButton, bt ? 1f : 0f); clicando = bt; }
        }
    }
}
