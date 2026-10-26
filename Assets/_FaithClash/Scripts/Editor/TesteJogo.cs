#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Teste de movimento automático. Em modo batch: Unity.exe -batchmode -nographics -projectPath ... -executeMethod FaithClash.EditorTools.TesteJogo.Rodar (sem -quit)
    /// com a variável de ambiente FC_TESTE=1. O resultado fica em Logs/teste_movimento.txt e o Unity fecha sozinho.
    /// </summary>
    public static class TesteJogo
    {
        [MenuItem("Faith Clash/Ferramentas/Testar Movimento (automático)")]
        public static void Rodar()
        {
            System.Environment.SetEnvironmentVariable("FC_TESTE", "1");
            EditorSceneManager.OpenScene("Assets/_FaithClash/Scenes/Treino.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            Debug.Log("[Teste] entrando no Play para o teste de movimento...");
        }

        public static void RodarMenu()
        {
            System.Environment.SetEnvironmentVariable("FC_TESTE_MENU", "1");
            EditorSceneManager.OpenScene("Assets/_FaithClash/Scenes/Carregamento.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }
}
#endif
