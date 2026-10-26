using UnityEngine;
using FaithClash.Core;

namespace FaithClash.Training
{
    /// <summary>Vida da caverna: pingos d'água sonoros perto do jogador, eco ocasional e cristais pulsando.</summary>
    public class AmbienteCaverna : MonoBehaviour
    {
        [SerializeField] Transform jogador;
        [SerializeField] Renderer[] cristais;
        [SerializeField] Vector3[] cachoeiras;
        UnityEngine.AudioSource[] somCachoeira;
        float proximoPingo, proximoEco, proximaQueda = 12f;
        MaterialPropertyBlock bloco;
        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");
        Color[] baseCor;

        void Start()
        {
            if (jogador == null) { var p = FindAnyObjectByType<FaithClash.Player.PlayerController>(); if (p != null) jogador = p.transform; }
            bloco = new MaterialPropertyBlock();
            if (cristais != null)
            {
                baseCor = new Color[cristais.Length];
                for (int i = 0; i < cristais.Length; i++) if (cristais[i] != null) baseCor[i] = cristais[i].sharedMaterial.GetColor(Emissao);
            }
            if (cachoeiras != null)
            {
                somCachoeira = new UnityEngine.AudioSource[cachoeiras.Length];
                for (int i = 0; i < cachoeiras.Length; i++)
                {
                    var go = new GameObject("Som da cachoeira");
                    go.transform.SetParent(transform, false); go.transform.position = cachoeiras[i];
                    var a = go.AddComponent<UnityEngine.AudioSource>();
                    a.clip = SomJogo.Clipe("Cachoeira"); a.loop = true; a.spatialBlend = 1f; a.minDistance = 3f; a.maxDistance = 28f; a.rolloffMode = UnityEngine.AudioRolloffMode.Linear;
                    a.Play();
                    somCachoeira[i] = a;
                }
            }
            proximoPingo = Time.time + 1f; proximoEco = Time.time + 8f;
        }

        void Update()
        {
            if (jogador == null || bloco == null) return;
            if (somCachoeira != null)
                foreach (var a in somCachoeira) if (a != null) a.volume = 0.7f * PerfilJogo.Volume("ambiente");
            if (Time.time >= proximoPingo)
            {
                proximoPingo = Time.time + Random.Range(1.4f, 4.5f);
                Vector3 p = jogador.position + new Vector3(Random.Range(-6f, 6f), Random.Range(2f, 6f), Random.Range(-4f, 14f));
                SomJogo.Tocar("Gota", p, Random.Range(0.35f, 0.7f), Random.Range(0.8f, 1.3f), 0.1f);
            }
            if (Time.time >= proximaQueda)
            {
                proximaQueda = Time.time + Random.Range(14f, 28f);
                Vector3 p = jogador.position + new Vector3(Random.Range(-4f, 4f), 7f, Random.Range(8f, 20f));
                CatalogoVFX.Tocar("pedras_cair", p, Quaternion.identity, null, 1f, 4f);
                SomJogo.Tocar("Passos_Pedra", p, 0.8f, 0.6f, 0.1f, atraso: 0.6f);
                SomJogo.Tocar("Pouso", p, 0.35f, 0.9f, 0.1f, atraso: 0.9f);
            }
            if (Time.time >= proximoEco)
            {
                proximoEco = Time.time + Random.Range(12f, 25f);
                SomJogo.Tocar("Movimento_Vento", jogador.position + Vector3.up * 3f, 0.25f, 0.55f, 0.05f, 2.5f);
            }
            if (cristais != null)
                for (int i = 0; i < cristais.Length; i++)
                {
                    if (cristais[i] == null) continue;
                    float k = 0.7f + 0.5f * Mathf.Sin(Time.time * 1.3f + i * 1.7f);
                    cristais[i].GetPropertyBlock(bloco);
                    bloco.SetColor(Emissao, baseCor[i] * k);
                    cristais[i].SetPropertyBlock(bloco);
                }
        }
    }
}
