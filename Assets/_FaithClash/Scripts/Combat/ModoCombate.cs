using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Combat
{
    public enum Modo { Espada, Poder }

    /// <summary>
    /// Dois modos de combate, que não se misturam: <b>Espada</b> (katana, só corta) ou <b>Poder</b> (punhos + jutsus
    /// e técnicas de maldição). G alterna. Apertar 1-4 (estilo elemental) entra direto no modo Poder.
    /// </summary>
    public class ModoCombate : MonoBehaviour
    {
        [SerializeField] GameObject katana;
        [SerializeField] float atrasoKatana = 0.45f;   // a katana aparece no meio da animação de sacar

        AnimadorProtagonista animador;
        MeleeCombat combate;
        Coroutine mostrando;

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
            if (k != null && k.gKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) Alternar();
        }

        public void Alternar() => Definir(Atual == Modo.Espada ? Modo.Poder : Modo.Espada);

        public void Definir(Modo m)
        {
            if (m == Atual) return;
            if (combate != null && combate.IsAttacking) return;
            Atual = m;
            if (mostrando != null) StopCoroutine(mostrando);
            if (m == Modo.Espada)
            {
                animador?.Acao("SacarEspada", 0.8f, 0.08f);
                SomJogo.Tocar("Espada_Sacar", transform.position, 0.9f, atraso: 0.25f);
                mostrando = StartCoroutine(MostrarKatana(true));
            }
            else
            {
                if (katana != null) katana.SetActive(false);
                animador?.Acao("EnvainharEspada", 0.8f, 0.08f);
                SomJogo.Tocar("Poder_Ativar", transform.position, 0.7f);
            }
            if (m == Modo.Espada) SomJogo.Tocar("Poder_Desativar", transform.position, 0.5f, 1.3f);
            Mudou?.Invoke(m);
        }

        IEnumerator MostrarKatana(bool mostrar)
        {
            yield return new WaitForSeconds(atrasoKatana);
            if (katana != null) katana.SetActive(mostrar);
        }
    }
}
