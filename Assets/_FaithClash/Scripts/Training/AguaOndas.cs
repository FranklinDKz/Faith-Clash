using UnityEngine;

namespace FaithClash.Training
{
    /// <summary>Ondas na superfície: cada mergulho, passo ou golpe na água cria um anel que o shader da água desenha (até 8 ao mesmo tempo).</summary>
    public class AguaOndas : MonoBehaviour
    {
        static AguaOndas instancia;
        static readonly Vector4[] ondas = new Vector4[8];
        static int proxima;

        public static void Zerar() { instancia = null; proxima = 0; for (int i = 0; i < 8; i++) ondas[i] = Vector4.zero; }

        public static void Adicionar(Vector3 pos, float forca)
        {
            if (instancia == null) instancia = new GameObject("Ondas da Água").AddComponent<AguaOndas>();
            ondas[proxima] = new Vector4(pos.x, pos.z, Time.timeSinceLevelLoad, Mathf.Clamp(forca, 0.1f, 2f));
            proxima = (proxima + 1) % ondas.Length;
        }

        void Update() { Shader.SetGlobalVectorArray("_FC_Ondas", ondas); }
    }
}
