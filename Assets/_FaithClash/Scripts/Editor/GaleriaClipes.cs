using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FaithClash.EditorTools
{
    /// <summary>
    /// Ferramenta de conferência: fotografa cada clipe do controller do jogador em 8 instantes (frente-diagonal e lado),
    /// sem entrar no Play, e grava Logs/galeria/NN_clipe.png (uma faixa por clipe). Rode em batch SEM -nographics.
    /// </summary>
    public static class GaleriaClipes
    {
        public static void Rodar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            var an = go.GetComponentInChildren<Animator>();
            var ctrl = an.runtimeAnimatorController;
            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane); chao.transform.localScale = Vector3.one * 3f;

            var camGo = new GameObject("Cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.25f, 0.32f); cam.fieldOfView = 34f;
            const int T = 300;
            var rt = new RenderTexture(T, T, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var tex = new Texture2D(T, T, TextureFormat.RGB24, false);
            Directory.CreateDirectory("Logs/galeria");
            foreach (var f in Directory.GetFiles("Logs/galeria")) File.Delete(f);

            var clipes = ctrl.animationClips.Where(c => c != null).Distinct().OrderBy(c => c.name).ToList();
            var rel = new System.Text.StringBuilder();
            int idx = 0;
            foreach (var clip in clipes)
            {
                var faixa = new Texture2D(T * 8, T * 2, TextureFormat.RGB24, false);
                var graph = PlayableGraph.Create("galeria");
                var saida = AnimationPlayableOutput.Create(graph, "saida", an);
                var cp = AnimationClipPlayable.Create(graph, clip);
                cp.SetApplyFootIK(false);
                saida.SetSourcePlayable(cp);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                float minPe = 99f, maxQuadril = -99f, minQuadril = 99f;
                for (int i = 0; i < 8; i++)
                {
                    double t = clip.length * i / 8.0;
                    cp.SetTime(t); graph.Evaluate();
                    var q = an.GetBoneTransform(HumanBodyBones.Hips); var pl = an.GetBoneTransform(HumanBodyBones.LeftFoot); var pr = an.GetBoneTransform(HumanBodyBones.RightFoot);
                    if (q != null) { maxQuadril = Mathf.Max(maxQuadril, q.position.y); minQuadril = Mathf.Min(minQuadril, q.position.y); }
                    if (pl != null && pr != null) minPe = Mathf.Min(minPe, Mathf.Min(pl.position.y, pr.position.y));
                    Vector3 c = (q != null ? q.position : Vector3.up) ; c.y = Mathf.Max(c.y, 0.9f);
                    for (int v = 0; v < 2; v++)
                    {
                        Vector3 dir = v == 0 ? new Vector3(0.55f, 0.15f, 0.83f) : new Vector3(1f, 0.12f, 0f);
                        camGo.transform.position = c + dir.normalized * 4.6f; camGo.transform.LookAt(c);
                        cam.Render();
                        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, T, T), 0, 0); tex.Apply(); RenderTexture.active = null;
                        faixa.SetPixels(i * T, (1 - v) * T, T, T, tex.GetPixels());
                    }
                }
                graph.Destroy();
                faixa.Apply();
                File.WriteAllBytes($"Logs/galeria/{idx:00}_{clip.name}.png", faixa.EncodeToPNG());
                Object.DestroyImmediate(faixa);
                rel.AppendLine($"{idx:00} {clip.name} dur={clip.length:0.00}s loop={clip.isLooping} quadril min/max={minQuadril:0.00}/{maxQuadril:0.00} pe min={minPe:0.00} humanMotion={clip.humanMotion}");
                idx++;
            }
            File.WriteAllText("Logs/galeria/relatorio.txt", rel.ToString());
            Debug.Log("[Galeria] " + idx + " clipes fotografados");
        }
    }
}
