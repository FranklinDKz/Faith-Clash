using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>Instala os pacotes que o projeto deve precisar (tudo em uma única resolução do Package Manager).</summary>
    public static class InstaladorDePacotes
    {
        static readonly string[] Pacotes =
        {
            "com.unity.cinemachine",           // câmeras
            "com.unity.animation.rigging",     // IK (mira, olhar, mãos na arma)
            "com.unity.timeline",              // cutscenes do modo História
            "com.unity.ai.navigation",         // NavMesh para inimigos
            "com.unity.probuilder",            // protótipo de cenários
            "com.unity.visualeffectgraph",     // VFX (jutsus, poderes)
            "com.unity.addressables",          // carregamento de conteúdo
            "com.unity.localization",          // idiomas
            "com.unity.formats.fbx",           // exportar/importar FBX
            "com.unity.netcode.gameobjects",   // online (1v1 e PvP)
            "com.unity.services.multiplayer",  // salas, matchmaking e sessões
            "com.unity.services.authentication",
            "com.unity.services.cloudsave",    // salvar progresso na nuvem
        };

        static AddAndRemoveRequest pedido;

        [MenuItem("Faith Clash/Instalar Pacotes Recomendados")]
        public static void Instalar()
        {
            Debug.Log("[Faith Clash] Instalando pacotes: " + string.Join(", ", Pacotes));
            pedido = Client.AddAndRemove(Pacotes);
            EditorApplication.update += Acompanhar;
        }

        static void Acompanhar()
        {
            if (pedido == null || !pedido.IsCompleted) return;
            EditorApplication.update -= Acompanhar;
            if (pedido.Status == StatusCode.Success)
                Debug.Log("[Faith Clash] Pacotes instalados: " + string.Join(", ", pedido.Result.Select(p => p.name + "@" + p.version)));
            else
                Debug.LogError("[Faith Clash] Falha ao instalar pacotes: " + pedido.Error?.message);
            pedido = null;
        }
    }
}
