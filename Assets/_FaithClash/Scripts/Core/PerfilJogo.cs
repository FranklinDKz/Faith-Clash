using System;
using UnityEngine;

namespace FaithClash.Core
{
    /// <summary>Tudo o que o jogador escolhe e que fica salvo: poderes equipados, configurações e aparência do personagem.</summary>
    [Serializable]
    public class DadosPerfil
    {
        public int versao = 1;

        // ---- poderes (índice do Elemento em cada um dos 4 slots, -1 = vazio)
        public int[] slots = { 0, 1, 2, 3 };

        // ---- gráficos
        public int qualidade = 3;                 // 0 muito baixa … 5 ultra (índice de QualitySettings)
        public bool sombras = true, posProcessamento = true, vsync = true, telaCheia = true, neblina = true;
        public float escalaRender = 1f, fov = 62f, brilho = 1f;
        public int limiteFps = 0;                 // 0 = livre
        public int resLargura = 0, resAltura = 0; // 0 = a resolução atual da tela

        // ---- jogabilidade
        public bool parkourAutomatico = false, assistenciaCombate = true, deslize = true, descidaAutomatica = true, dicasNaTela = true, miraAssistida = true;
        public float velocidadeJogo = 1f, sensibilidade = 1f, tremorCamera = 1f, efeitoVelocidade = 1f;
        public int estiloCorrida = 0;             // 0 normal, 1 ninja (Naruto)

        // ---- áudio (0..1.5)
        public float volMestre = 1f, volMusica = 0.7f, volEfeitos = 1f, volGraves = 0.7f, volPassos = 1f, volCombate = 1f, volPoderes = 1f, volAmbiente = 1f, volInterface = 1f;

        // ---- personagem
        public int sexo = 0;                      // 0 masculino, 1 feminino
        public int tomPele = 2, estiloCabelo = 2, corCabelo = 0, corRoupa = 0, corAcento = 0, corCalca = 0, corCalcado = 0;
        public bool capuz = true, luvas = true, acessorios = true, barba = false;
        public int katana = 0, estiloLuta = 0;
        public bool duasKatanas = false;
    }

    public static class PerfilJogo
    {
        const string Chave = "FC_Perfil_v1";
        static DadosPerfil dados;

        public static event Action Mudou;

        public static DadosPerfil D
        {
            get
            {
                if (dados == null) Carregar();
                return dados;
            }
        }

        internal static void ResetarEstatico() { dados = null; Mudou = null; }

        public static void Carregar()
        {
            try
            {
                string json = PlayerPrefs.GetString(Chave, "");
                dados = string.IsNullOrEmpty(json) ? new DadosPerfil() : JsonUtility.FromJson<DadosPerfil>(json) ?? new DadosPerfil();
            }
            catch { dados = new DadosPerfil(); }
            dados.estiloCorrida = 0;   // estilo Naruto removido
            if (dados.slots == null || dados.slots.Length != 4) dados.slots = new[] { 0, 1, 2, 3 };
        }

        public static void Salvar()
        {
            PlayerPrefs.SetString(Chave, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        /// <summary>Avisa todos os sistemas (e salva) depois de qualquer alteração do menu.</summary>
        public static void Alterado()
        {
            Salvar();
            if (Mudou == null) return;
            foreach (Action a in Mudou.GetInvocationList())
            {
                try { a(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static void Restaurar()
        {
            var antigos = D.slots;
            dados = new DadosPerfil { slots = antigos };
            Alterado();
        }

        public static bool AlgumPoder
        {
            get { foreach (int s in D.slots) if (s >= 0) return true; return false; }
        }

        /// <summary>Poderes liberados agora: algum equipado e a fase não bloqueia.</summary>
        public static bool PoderesDisponiveis => !RegrasFase.PoderesBloqueados && AlgumPoder;

        public static int SlotDe(int elemento)
        {
            for (int i = 0; i < 4; i++) if (D.slots[i] == elemento) return i;
            return -1;
        }

        public static bool Equipado(int elemento) => SlotDe(elemento) >= 0;

        /// <summary>Equipa/desequipa. Devolve falso se os 4 slots estão cheios.</summary>
        public static bool Alternar(int elemento)
        {
            int s = SlotDe(elemento);
            if (s >= 0) { D.slots[s] = -1; Alterado(); return true; }
            for (int i = 0; i < 4; i++)
                if (D.slots[i] < 0) { D.slots[i] = elemento; Alterado(); return true; }
            return false;
        }

        public static void Colocar(int slot, int elemento)
        {
            int antigo = SlotDe(elemento);
            if (antigo >= 0) D.slots[antigo] = D.slots[slot];
            D.slots[slot] = elemento;
            Alterado();
        }

        public static float Volume(string categoria)
        {
            var d = D;
            float v = d.volMestre;
            switch (categoria)
            {
                case "passos": return v * d.volEfeitos * d.volPassos;
                case "combate": return v * d.volEfeitos * d.volCombate;
                case "poderes": return v * d.volEfeitos * d.volPoderes;
                case "ambiente": return v * d.volAmbiente;
                case "ui": return v * d.volInterface;
                case "musica": return v * d.volMusica;
                default: return v * d.volEfeitos;
            }
        }
    }
}
