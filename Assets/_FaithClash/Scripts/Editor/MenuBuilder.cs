using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FaithClash.UI;

namespace FaithClash.EditorTools
{
    /// <summary>Cria as imagens pintadas (menu e carregamento) e as cenas Carregamento e MenuPrincipal, e põe tudo na ordem certa do Build Settings.</summary>
    public static class MenuBuilder
    {
        const string PastaCenas = "Assets/_FaithClash/Scenes";
        const string PastaRes = "Assets/_FaithClash/Resources";

        // só as cores da logo: preto, ouro, violeta e branco-pérola. ouro/violeta = intensidade do brilho de cada lado; sol = posição horizontal
        struct Paleta { public float ouro, violeta, sol; }

        static readonly Paleta[] Paletas =
        {
            new Paleta { ouro = 1.0f, violeta = 1.0f, sol = 0.5f },
            new Paleta { ouro = 1.2f, violeta = 0.35f, sol = 0.32f },
            new Paleta { ouro = 0.35f, violeta = 1.2f, sol = 0.68f },
            new Paleta { ouro = 0.9f, violeta = 0.8f, sol = 0.4f },
            new Paleta { ouro = 0.8f, violeta = 1.0f, sol = 0.6f },
        };
        static readonly Color Ouro = new Color(1f, 0.78f, 0.28f), Violeta = new Color(0.55f, 0.2f, 1f), Perola = new Color(1f, 0.95f, 0.82f);

        static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        [MenuItem("Faith Clash/Criar Menu Principal e Carregamento")]
        public static void Construir()
        {
            Directory.CreateDirectory(PastaRes + "/Carregamento"); Directory.CreateDirectory(PastaRes + "/Menu"); Directory.CreateDirectory(PastaCenas);
            for (int i = 0; i < Paletas.Length; i++) Salvar(Pintar(Paletas[i], 11 + i * 7, i == 0), PastaRes + "/Carregamento/arte_" + (i + 1) + ".png");
            Salvar(Pintar(Paletas[0], 11, true), PastaRes + "/Menu/fundo.png");
            AssetDatabase.Refresh();
            GerarLogo();

            CriarCena("Carregamento", go => go.AddComponent<CarregamentoInicial>());
            CriarCena("MenuPrincipal", go => go.AddComponent<MenuPrincipal>());
            AjustarBuildSettings();
            Debug.Log("[Faith Clash] Menu principal e tela de carregamento criados.");
        }

        public static void AjustarBuildSettings()
        {
            var lista = new List<EditorBuildSettingsScene>();
            foreach (var n in new[] { "Carregamento", "MenuPrincipal", "Treino", "Fase1_Corrida" })
            {
                string c = PastaCenas + "/" + n + ".unity";
                if (File.Exists(c)) lista.Add(new EditorBuildSettingsScene(c, true));
            }
            EditorBuildSettings.scenes = lista.ToArray();
        }

        static void CriarCena(string nome, System.Action<GameObject> aoCriar)
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Câmera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>(); c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.01f, 0.012f, 0.03f); c.cullingMask = 0;
            var go = new GameObject(nome);
            aoCriar(go);
            EditorSceneManager.SaveScene(cena, PastaCenas + "/" + nome + ".unity");
        }

        // ------------------------------------------------------------------ pintura procedural (céu, brilho, estrelas, cristas de montanha, guerreiro)
        static Texture2D Pintar(Paleta p, int seed, bool guerreiro)
        {
            const int L = 1920, A = 1080;
            var pix = new Color32[L * A];
            float hor = A * 0.46f;
            var rnd = new System.Random(seed);
            for (int y = 0; y < A; y++)
                for (int x = 0; x < L; x++)
                {
                    float u = x / (float)L;
                    float dx = (u - p.sol) * 1.6f, dy = (y - hor) / (A * 0.7f);
                    float halo = Mathf.Exp(-(dx * dx * 2.2f + dy * dy * 5f));
                    Color lado = Color.Lerp(Ouro * p.ouro, Violeta * p.violeta, Mathf.SmoothStep(0f, 1f, (u - (p.sol - 0.28f)) / 0.56f));
                    Color c = lado * (halo * 0.85f) + Perola * Mathf.Exp(-(dx * dx * 14f + dy * dy * 40f)) * 0.55f;
                    pix[y * L + x] = c;
                }
            for (int i = 0; i < 320; i++)
            {
                int x = rnd.Next(L), y = rnd.Next((int)(A * 0.55f), A); float f = (float)rnd.NextDouble();
                Color baseC = (Color)pix[y * L + x]; Color est = rnd.Next(3) == 0 ? Violeta : (rnd.Next(2) == 0 ? Ouro : Perola);
                pix[y * L + x] = Color.Lerp(baseC, est, 0.35f + 0.65f * f);
            }
            int camadas = 6;
            for (int k = 0; k < camadas; k++)
            {
                float prof = k / (camadas - 1f);
                float baseY = Mathf.Lerp(A * 0.42f, A * 0.1f, prof), amp = Mathf.Lerp(A * 0.10f, A * 0.2f, prof), freq = Mathf.Lerp(0.0016f, 0.004f, prof);
                for (int x = 0; x < L; x++)
                {
                    float u = x / (float)L;
                    Color nevoa = Color.Lerp(Ouro * p.ouro, Violeta * p.violeta, Mathf.SmoothStep(0f, 1f, (u - (p.sol - 0.3f)) / 0.6f));
                    Color serra = Color.Lerp(new Color(0.07f, 0.06f, 0.09f), Color.black, prof);
                    float n = Mathf.PerlinNoise(x * freq + seed * 3.1f + k * 17f, k * 4.3f) * 0.75f + Mathf.PerlinNoise(x * freq * 3.2f + k * 9f, seed + k) * 0.25f;
                    int topo = Mathf.Clamp((int)(baseY + n * amp), 0, A - 1);
                    for (int y = 0; y <= topo; y++)
                    {
                        float neb = Mathf.Clamp01(1f - (topo - y) / (A * 0.16f));
                        float borda = Mathf.Clamp01(1f - (topo - y) / 5f);        // fio de luz na crista
                        Color c = Color.Lerp(serra, nevoa * 0.5f, neb * Mathf.Lerp(0.5f, 0.1f, prof));
                        c += nevoa * borda * Mathf.Lerp(0.35f, 0.08f, prof);
                        pix[y * L + x] = c;
                    }
                }
            }
            if (guerreiro)
            {
                Color pr = Color.black;
                int gx = (int)(L * 0.2f), gy = (int)(A * 0.2f);
                for (int x = gx - 60; x <= gx + 60; x++) for (int y = gy - 40; y <= gy; y++) if (x >= 0 && x < L && y >= 0) pix[y * L + x] = pr;
                Disco(pix, L, A, gx, gy + 70, 10, pr);
                for (int y = gy; y < gy + 62; y++)
                {
                    float w = Mathf.Lerp(18f, 8f, (y - gy) / 62f);
                    for (int x = (int)(gx - w); x <= gx + w * 0.8f; x++) if (x >= 0 && x < L) pix[y * L + x] = pr;
                }
                for (int i = 0; i < 90; i++) { int x = gx + 14 + i / 4, y = gy + 10 + i; if (x < L && y < A) pix[y * L + x] = pr; }
            }
            var tex = new Texture2D(L, A, TextureFormat.RGBA32, false);
            tex.SetPixels32(pix); tex.Apply();
            return tex;
        }

        /// <summary>A logo original tem fundo preto: o preto vira transparente (alfa = brilho) para ficar sobre qualquer fundo.</summary>
        static void GerarLogo()
        {
            const string origem = "Assets/_FaithClash/Art/Logo/FaithClash_Logo.png", destino = PastaRes + "/Menu/logo.png";
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(origem));
            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                float m = Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b)) / 255f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((m - 0.04f) * 3.2f));
                px[i] = new Color32(px[i].r, px[i].g, px[i].b, (byte)(a * 255f));
            }
            tex.SetPixels32(px); tex.Apply();
            Salvar(tex, destino);
            var imp = (TextureImporter)AssetImporter.GetAtPath(destino);
            imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true; imp.SaveAndReimport();
        }

        static void Disco(Color32[] pix, int L, int A, int cx, int cy, int r, Color c)
        {
            for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) { int px = cx + x, py = cy + y; if (px >= 0 && px < L && py >= 0 && py < A) pix[py * L + px] = c; }
        }

        static void Salvar(Texture2D tex, string caminho)
        {
            File.WriteAllBytes(caminho, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = false; imp.maxTextureSize = 2048; imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp; imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.alphaSource = TextureImporterAlphaSource.None;
            imp.SaveAndReimport();
        }
    }
}
