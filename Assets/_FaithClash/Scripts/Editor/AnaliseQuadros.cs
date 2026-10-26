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
    /// Analisa uma animação quadro a quadro: renderiza de lado (câmera fixa, com marcas de altura a cada 0,5 m) e mede,
    /// em cada quadro, a altura e o avanço das mãos, pés e quadril. Saída: Logs/quadros/NOME.png e NOME.txt.
    /// Uso (batch, com gráficos): -executeMethod FaithClash.EditorTools.AnaliseQuadros.Rodar com FC_CLIPES_ANALISE=Rasteira_Usuario,Pulo_Correndo
    /// </summary>
    public static class AnaliseQuadros
    {
        public static void Rodar()
        {
            var nomes = (System.Environment.GetEnvironmentVariable("FC_CLIPES_ANALISE") ?? "Rasteira_Usuario").Split(',');
            int n = int.TryParse(System.Environment.GetEnvironmentVariable("FC_QUADROS"), out var q) ? q : 24;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreparadorJogador.CaminhoPrefab);
            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            var an = go.GetComponentInChildren<Animator>();
            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane); chao.transform.localScale = Vector3.one * 3f;
            // marcas de altura (postes finos atrás do personagem) a cada 0,5 m
            for (int i = 1; i <= 6; i++)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube); m.transform.position = new Vector3(-0.9f, i * 0.5f, 1.2f); m.transform.localScale = new Vector3(0.02f, 0.02f, 4f);
                m.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = i % 2 == 0 ? Color.yellow : Color.white };
            }
            var camGo = new GameObject("Cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.25f, 0.32f);
            cam.orthographic = true; cam.orthographicSize = 1.9f;
            camGo.transform.position = new Vector3(6f, 1.5f, 0.6f); camGo.transform.LookAt(new Vector3(0f, 1.5f, 0.6f));
            const int T = 260;
            var rt = new RenderTexture(T, T, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var tex = new Texture2D(T, T, TextureFormat.RGB24, false);
            Directory.CreateDirectory("Logs/quadros");
            Transform Osso(HumanBodyBones b) => an.GetBoneTransform(b);
            foreach (var nome in nomes)
            {
                var clip = an.runtimeAnimatorController.animationClips.FirstOrDefault(c => c != null && c.name == nome.Trim());
                if (clip == null) clip = AssetDatabase.LoadAllAssetsAtPath($"Assets/Personagens/JogadorPrincipal/Animacoes/Manuais/{nome.Trim()}.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
                if (clip == null) { Debug.LogWarning("[Quadros] não achei " + nome); continue; }
                if (System.Environment.GetEnvironmentVariable("FC_RAIZ") == "1") { Raiz(an, clip, n); continue; }
                int cols = 8, linhas = Mathf.CeilToInt(n / (float)cols);
                var folha = new Texture2D(T * cols, T * linhas, TextureFormat.RGB24, false);
                var txt = new System.Text.StringBuilder($"{clip.name} duração={clip.length:0.00}s\nquadro t(%) quadril.y maoE.y maoD.y maoE.z maoD.z peE.y peD.y peE.z peD.z\n");
                var graph = PlayableGraph.Create("quadros");
                var saida = AnimationPlayableOutput.Create(graph, "s", an);
                var cp = AnimationClipPlayable.Create(graph, clip); cp.SetApplyFootIK(false);
                saida.SetSourcePlayable(cp);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                for (int i = 0; i < n; i++)
                {
                    double t = clip.length * i / (n - 1.0);
                    cp.SetTime(t); graph.Evaluate();
                    var h = Osso(HumanBodyBones.Hips).position; var mE = Osso(HumanBodyBones.LeftHand).position; var mD = Osso(HumanBodyBones.RightHand).position;
                    var pE = Osso(HumanBodyBones.LeftFoot).position; var pD = Osso(HumanBodyBones.RightFoot).position;
                    txt.AppendLine($"{i,2} {100.0 * i / (n - 1):000} {h.y:0.00} {mE.y:0.00} {mD.y:0.00} {mE.z:0.00} {mD.z:0.00} {pE.y:0.00} {pD.y:0.00} {pE.z:0.00} {pD.z:0.00}");
                    cam.Render();
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, T, T), 0, 0); tex.Apply(); RenderTexture.active = null;
                    folha.SetPixels((i % cols) * T, (linhas - 1 - i / cols) * T, T, T, tex.GetPixels());
                }
                graph.Destroy();
                folha.Apply();
                File.WriteAllBytes($"Logs/quadros/{clip.name}.png", folha.EncodeToPNG());
                File.WriteAllText($"Logs/quadros/{clip.name}.txt", txt.ToString());
                Object.DestroyImmediate(folha);
            }
            Debug.Log("[Quadros] pronto"); if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // desloca a raiz com o movimento do clipe (root motion) e registra raiz, quadril, mãos e pés no mundo, quadro a quadro
        static void Raiz(Animator an, AnimationClip clip, int n)
        {
            an.applyRootMotion = true;
            var raiz = an.transform; Vector3 p0 = raiz.position; Quaternion r0 = raiz.rotation;
            var graph = PlayableGraph.Create("raiz");
            var saida = AnimationPlayableOutput.Create(graph, "s", an);
            var cp = AnimationClipPlayable.Create(graph, clip); cp.SetApplyFootIK(false);
            saida.SetSourcePlayable(cp);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var txt = new System.Text.StringBuilder($"{clip.name} dur={clip.length:0.00}s" + System.Environment.NewLine + "q t% raiz(x,y,z) quadril(y,z) peE(y,z) peD(y,z) maoE(y,z) maoD(y,z)" + System.Environment.NewLine);
            float dt = clip.length / (n - 1);
            cp.SetTime(0); graph.Evaluate(0f);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) graph.Evaluate(dt);
                Vector3 r = raiz.position - p0;
                Vector3 h = an.GetBoneTransform(HumanBodyBones.Hips).position, pe = an.GetBoneTransform(HumanBodyBones.LeftFoot).position, pd = an.GetBoneTransform(HumanBodyBones.RightFoot).position;
                Vector3 me = an.GetBoneTransform(HumanBodyBones.LeftHand).position, md = an.GetBoneTransform(HumanBodyBones.RightHand).position;
                txt.AppendLine($"{i,2} {100f * i / (n - 1):000} ({r.x:0.00},{r.y:0.00},{r.z:0.00}) {h.y:0.00},{h.z:0.00} {pe.y:0.00},{pe.z:0.00} {pd.y:0.00},{pd.z:0.00} {me.y:0.00},{me.z:0.00} {md.y:0.00},{md.z:0.00}");
            }
            graph.Destroy();
            raiz.position = p0; raiz.rotation = r0; an.applyRootMotion = false;
            // curvas da raiz do clipe (RootT: deslocamento normalizado pelo tamanho do avatar)
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                if (!b.propertyName.StartsWith("RootT.")) continue;
                var c = AnimationUtility.GetEditorCurve(clip, b);
                txt.AppendLine(b.propertyName + ": " + string.Join(" ", Enumerable.Range(0, n).Select(i => c.Evaluate(clip.length * i / (n - 1)).ToString("0.00"))));
            }
            File.WriteAllText($"Logs/quadros/{clip.name}_raiz.txt", txt.ToString());
        }
    }
}
