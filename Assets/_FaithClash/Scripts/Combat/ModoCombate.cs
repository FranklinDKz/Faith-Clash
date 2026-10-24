using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Combat
{
    public enum Modo { Espada, Poder, Combate }

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
            Mudou?.Invoke(Atual);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || Cursor.lockState != CursorLockMode.Locked) return;
            if (k.gKey.wasPressedThisFrame) Alternar();
            if (k.digit5Key.wasPressedThisFrame) AlternarCombate();
        }

        /// <summary>G: Espada ⇄ Poder (a partir do modo Combate, saca a espada).</summary>
        public void Alternar() => Definir(Atual == Modo.Espada ? Modo.Poder : Modo.Espada);

        /// <summary>5: entra no modo só-combate; apertar de novo volta ao modo em que estava.</summary>
        public void AlternarCombate() => Definir(Atual == Modo.Combate ? (anterior == Modo.Combate ? Modo.Poder : anterior) : Modo.Combate);

        public void Definir(Modo m)
        {
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
                    animador?.Acao("SacarEspada", 0.8f, 0.08f);
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