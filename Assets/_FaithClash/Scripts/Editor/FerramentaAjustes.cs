using UnityEditor;
using UnityEngine;
using FaithClash.Core;

namespace FaithClash.EditorTools
{
    /// <summary>Menu Faith Clash > Ajustes de Movimento: abre no Inspector o arquivo com velocidade, pulo, colisão, obstáculos, rasteira, parede e pose.</summary>
    public static class FerramentaAjustes
    {
        const string Caminho = "Assets/_FaithClash/Resources/AjustesMovimento.asset";

        public static AjustesMovimento Garantir()
        {
            var a = AssetDatabase.LoadAssetAtPath<AjustesMovimento>(Caminho);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<AjustesMovimento>();
                AssetDatabase.CreateAsset(a, Caminho);
                AssetDatabase.SaveAssets();
                Debug.Log("[Faith Clash] Ajustes de Movimento criados em " + Caminho);
            }
            return a;
        }

        [MenuItem("Faith Clash/Ajustes de Movimento")]
        public static void Abrir()
        {
            var a = Garantir();
            Selection.activeObject = a;
            EditorGUIUtility.PingObject(a);
        }
    }
}
