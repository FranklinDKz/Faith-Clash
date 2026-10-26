using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Combat
{
    public enum Modo { Espada, Poder, Combate }

    /// <summary>Katana com uma mão (animações "Sword": escudo opcional) ou com as duas (animações "Great" e Two Hand).</summary>
    public enum Empunhadura { UmaMao, DuasMaos }

    /// <summary>
    /// Três modos de combate, que não se misturam: <b>Combate</b> (só taijutsu: punhos, chutes, joelhadas — sem espada nem poderes),
    /// <b>Espada</b> (katana, só corta) e <b>Poder</b> (jutsus e técnicas de maldição).
    /// G alterna Espada/Poder, 5 liga ou desliga o modo Combate (voltando ao modo anterior), 1-4 (estilo elemental) entram no Poder.
    /// </summary>
    public class ModoCombate : MonoBehaviour
    {
        [SerializeField] GameObject katana;
        [SerializeField] float atrasoKatana = 0.45f;   // a katana aparece no meio da animação de sacar

        AnimadorProtagonista animador;
        MeleeCombat combate;
        Coroutine mostrando;
        Modo anterior = Modo.Poder;

        public Modo Atual { get; private set; } = Modo.Poder;
        public Empunhadura Grip { get; private set; } = Empunhadura.DuasMaos;
        public bool DuasMaos => Atual == Modo.Espada && Grip == Empunhadura.DuasMaos;
        public event Action<Empunhadura> GripMudou;
        public event Action<Modo> Mudou;

        void Awake()
        {
            animador = GetComponent<AnimadorProtagonista>();
            combate = GetComponent<MeleeCombat>();
            if (katana == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                    if (t.name == "Katana") katana = t.gameObject;
            }
        }

        void Start()
        {
            if (katana != null) katana.SetActive(false);
            if (!PerfilJogo.PoderesDisponiveis && Atual == Modo.Poder) { Atual = Modo.Combate; anterior = Modo.Combate; }
            Mudou?.Invoke(Atual);
        }

        void Update()
        {
            var k = Keyboard.current; var pad = Gamepad.current;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if ((k != null && k.gKey.wasPressedThisFrame) || (pad != null && pad.dpad.up.wasPressedThisFrame)) Alternar();
            if (((k != null && k.hKey.wasPressedThisFrame) || (pad != null && pad.dpad.down.wasPressedThisFrame)) && Atual == Modo.Espada && (combate == null || !combate.IsAttacking)) AlternarEmpunhadura();
        }

        /// <summary>G: Espada ⇄ Poder (a partir do modo Combate, saca a espada).</summary>
        public void Alternar() => Definir(Atual == Modo.Espada ? Modo.Poder : Modo.Espada);   // sem poderes disponíveis: Espada ⇄ Combate

        /// <summary>H (com a katana): alterna entre uma mão e duas mãos.</summary>
        public void AlternarEmpunhadura()
        {
            Grip = Grip == Empunhadura.DuasMaos ? Empunhadura.UmaMao : Empunhadura.DuasMaos;
            SomJogo.Tocar("Espada_Sacar", transform.position, 0.5f, 1.2f, duracaoMax: 0.8f);
            GripMudou?.Invoke(Grip);
            animador?.Encerrar(0.15f);
        }

        /// <summary>5: entra no modo só-combate; apertar de novo volta ao modo em que estava.</summary>
        public void AlternarCombate() => Definir(Atual == Modo.Combate ? (anterior == Modo.Combate ? Modo.Poder : anterior) : Modo.Combate);

        public void Definir(Modo m)
        {
            if (EstilosElementais.DominioAtivo) return;   // dentro do domínio não dá para trocar de modo
            if (m == Modo.Poder && !PerfilJogo.PoderesDisponiveis) m = Modo.Combate;
            if (m == Atual) return;
            if (combate != null && combate.IsAttacking) return;
            var saiu = Atual;
            anterior = saiu;
            Atual = m;
            if (mostrando != null) StopCoroutine(mostrando);
            switch (m)
            {
                case Modo.Espada:
                    CatalogoVFX.Tocar("corte_azul", transform.position + Vector3.up, transform.rotation, null, 0.8f, 2f);
                    if (Grip == Empunhadura.DuasMaos) animador?.Acao("U_SacarKatanaDupla", 0.8f, 0.08f, 1f);
                    else animador?.Acao("SacarEspada", 0.8f, 0.08f);
                    SomJogo.Tocar("Espada_Sacar", transform.position, 0.9f, atraso: 0.25f);
                    SomJogo.Tocar("Poder_Desativar", transform.position, 0.5f, 1.3f);
                    mostrando = StartCoroutine(MostrarKatana(true));
                    break;
                case Modo.Poder:
                    CatalogoVFX.Tocar("poder_aura", transform.position, Quaternion.identity, transform, 1f, 2.5f);
                    GuardarEspada(saiu);
                    SomJogo.Tocar("Poder_Ativar", transform.position, 0.7f);
                    break;
                case Modo.Combate:
                    GuardarEspada(saiu);
                    SomJogo.Tocar("UI_Travar", transform.position, 0.6f, 0.8f);
                    SomJogo.Tocar("Poder_Desativar", transform.position, 0.35f, 1.4f, duracaoMax: 1.2f);
                    break;
            }
            Mudou?.Invoke(m);
        }

        void GuardarEspada(Modo saiu)
        {
            if (katana != null) katana.SetActive(false);
            if (saiu == Modo.Espada) animador?.Acao("EnvainharEspada", 0.8f, 0.08f);
        }

        IEnumerator MostrarKatana(bool mostrar)
        {
            yield return new WaitForSeconds(atrasoKatana);
            if (katana != null) katana.SetActive(mostrar);
        }
    }
}