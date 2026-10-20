using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Ferramenta de depuração: monta uma folha de imagens com poses de cada animação do jogador
    /// (Faith Clash > Ferramentas > Capturar Poses). Saída: Ferramentas/poses_debug.png
    /// </summary>
    public static class CapturadorDePoses
    {
        const string Saida = "Ferramentas/poses_debug.png";

        static readonly (string clipe, float[] tempos)[] Lista =
        {
            ("Corte1", new[] { 0f, 0.2f, 0.4f, 0.6f, 0.9f }), ("Corte2", new[] { 0f, 0.15f, 0.3f, 0.45f, 0.7f }),
            ("Corte3", new[] { 0f, 0.15f, 0.3f, 0.5f, 0.8f }), ("Estocada", new[] { 0f, 0.4f, 0.8f, 1.2f, 1.8f }),
            ("EspadaSalto", new[] { 0f, 0.4f, 0.8f, 1.2f, 1.8f }), ("Soco1", new[] { 0f, 0.15f, 0.3f, 0.5f, 0.8f }),
            ("Soco2", new[] { 0f, 0.15f, 0.3f, 0.5f, 0.8f }), ("Soco3", new[] { 0f, 0.15f, 0.3f, 0.5f, 0.75f }),
            ("Martelo", new[] { 0f, 0.2f, 0.4f, 0.6f, 1.0f }), ("Dash", new[] { 0f, 0.1f, 0.2f, 0.35f, 0.5f }),
            ("Deslizar", new[] { 0f, 0.3f, 0.6f, 1.0f, 1.4f }), ("SacarEspada", new[] { 0f, 0.2f, 0.4f, 0.6f, 0.8f }),
            ("CorridaPadrao", new[] { 0f, 0.15f, 0.3f, 0.45f, 0.6f }), ("Parado", new[] { 0f, 0.3f, 0.6f, 0.9f, 1.2f }),
        };

        [MenuItem("Faith Clash/Ferramentas/Capturar Poses")]
        public static void Capturar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            if (prefab == null) { Debug.LogError("[Faith Clash] Prefab do jogador não encontrado."); return; }
            var clipes = new Dictionary<string, AnimationClip>();
            foreach (var arq in Directory.GetFiles("Assets/_FaithClash/Art/Characters/Jogador/Animacoes", "*.fbx"))
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(arq.Replace('\\', '/')).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview__")) clipes[c.name] = c;

            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.position = new Vector3(500, 0, 500);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == "Katana") t.gameObject.SetActive(true);
            var animator = go.GetComponentInChildren<Animator>();
            animator.enabled = false;
            var modelo = animator.gameObject;
            var quadril = animator.GetBoneTransform(HumanBodyBones.Hips);

            var luz = new GameObject("luz").AddComponent<Light>();
            luz.type = LightType.Directional; luz.transform.rotation = Quaternion.Euler(40, 150, 0); luz.intensity = 1.5f;
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.5f, 0.55f);
            cam.fieldOfView = 40f;
            var rt = new RenderTexture(260, 340, 24);
            cam.targetTexture = rt;

            int colunas = 5;
            var sheet = new Texture2D(colunas * rt.width, Lista.Length * rt.height, TextureFormat.RGB24, false);
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            for (int l = 0; l < Lista.Length; l++)
            {
                var (nome, tempos) = Lista[l];
                if (!clipes.TryGetValue(nome, out var clipe)) continue;
                for (int c = 0; c < tempos.Length; c++)
                {
                    clipe.SampleAnimation(modelo, Mathf.Min(tempos[c], clipe.length));
                    Vector3 foco = new Vector3(quadril.position.x, 1.0f, quadril.position.z);
                    Vector3 f = go.transform.forward;
                    camGo.transform.position = foco + (f * 3.6f + go.transform.right * 2.4f) + Vector3.up * 0.5f;
                    camGo.transform.LookAt(foco);
                    RenderTexture.active = rt;
                    cam.Render();
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    sheet.SetPixels(c * rt.width, (Lista.Length - 1 - l) * rt.height, rt.width, rt.height, tex.GetPixels());
                }
            }
            RenderTexture.active = null;
            File.WriteAllBytes(Saida, sheet.EncodeToPNG());
            Debug.Log("[Faith Clash] Poses salvas em " + Saida);
            Object.DestroyImmediate(go); Object.DestroyImmediate(luz.gameObject); Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt); Object.DestroyImmediate(sheet); Object.DestroyImmediate(tex);
        }
    }
}
