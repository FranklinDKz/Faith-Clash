using System.Collections.Generic;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.Training
{
    /// <summary>
    /// Volume de água com física: empuxo proporcional ao volume submerso (madeira boia, pedra afunda), arrasto, respingos e ondas na entrada,
    /// ondas dos passos, e o personagem vadeia (mais lento), flutua em água funda e nada com o Espaço. Use num objeto com BoxCollider (trigger).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class AguaFisica : MonoBehaviour
    {
        [SerializeField] float densidadeAgua = 1000f;
        [SerializeField] float arrasto = 2.4f;

        BoxCollider volume;
        readonly List<Rigidbody> corpos = new List<Rigidbody>();
        PlayerController jogador;
        bool jogadorDentro;
        float proximaOnda, proximaGota;

        float Superficie => volume.bounds.max.y;

        void Awake()
        {
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
        }

        void OnTriggerEnter(Collider c)
        {
            var rb = c.attachedRigidbody;
            if (rb == null || rb.isKinematic || corpos.Contains(rb) || rb.GetComponentInParent<PlayerController>() != null) return;
            corpos.Add(rb);
            float v = rb.linearVelocity.magnitude;
            if (v > 1.2f) Respingo(new Vector3(rb.position.x, Superficie, rb.position.z), Mathf.Clamp(v * Mathf.Sqrt(rb.mass) * 0.08f, 0.3f, 2.5f));
        }

        void OnTriggerExit(Collider c)
        {
            var rb = c.attachedRigidbody;
            if (rb != null) corpos.Remove(rb);
        }

        void FixedUpdate()
        {
            for (int i = corpos.Count - 1; i >= 0; i--)
            {
                var rb = corpos[i];
                if (rb == null) { corpos.RemoveAt(i); continue; }
                var col = rb.GetComponentInChildren<Collider>();
                if (col == null) continue;
                var b = col.bounds;
                float f = Mathf.Clamp01((Superficie - b.min.y) / Mathf.Max(0.05f, b.size.y));
                if (f <= 0f) continue;
                float vol = b.size.x * b.size.y * b.size.z * 0.62f;            // volume aproximado do corpo
                rb.AddForce(Vector3.up * densidadeAgua * vol * f * 9.81f);     // empuxo (Arquimedes)
                rb.linearVelocity *= 1f - Mathf.Clamp01(arrasto * Time.fixedDeltaTime * f);
                rb.angularVelocity *= 1f - Mathf.Clamp01(1.6f * Time.fixedDeltaTime * f);
            }
        }

        void Update()
        {
            if (jogador == null) jogador = FindAnyObjectByType<PlayerController>();
            if (jogador != null)
            {
                Vector3 p = jogador.transform.position;
                var b = volume.bounds;
                bool dentro = p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z && p.y < Superficie && p.y > b.min.y - 0.5f;
                float prof = Superficie - p.y;
                if (dentro && prof > 0.02f)
                {
                    jogador.DefinirAgua(prof);
                    if (!jogadorDentro)
                    {
                        float v = Mathf.Abs(jogador.Velocity.y) + new Vector3(jogador.Velocity.x, 0, jogador.Velocity.z).magnitude * 0.3f;
                        Respingo(new Vector3(p.x, Superficie, p.z), Mathf.Clamp(v * 0.18f, 0.5f, 2.2f));
                    }
                    float vh = new Vector3(jogador.Velocity.x, 0, jogador.Velocity.z).magnitude;
                    if (vh > 0.8f && Time.time > proximaOnda)
                    {
                        proximaOnda = Time.time + Mathf.Lerp(0.5f, 0.2f, Mathf.Clamp01(vh / 6f));
                        AguaOndas.Adicionar(new Vector3(p.x, Superficie, p.z), 0.35f + vh * 0.08f);
                        SomJogo.Tocar("Agua_Splash", p, 0.18f + vh * 0.03f, Random.Range(1.4f, 1.9f), 0.1f, duracaoMax: 0.35f);
                        if (vh > 3f) CatalogoVFX.Tocar("agua_splash", new Vector3(p.x, Superficie, p.z), Quaternion.identity, null, 0.35f, 1.2f);
                    }
                }
                else if (jogadorDentro)
                {
                    AguaOndas.Adicionar(new Vector3(p.x, Superficie, p.z), 0.5f);
                    SomJogo.Tocar("Agua_Splash", p, 0.3f, 1.3f, 0.1f, duracaoMax: 0.6f);
                }
                jogadorDentro = dentro && prof > 0.02f;
            }
            // corpos flutuando criam ondas suaves enquanto se movem
            foreach (var rb in corpos)
            {
                if (rb == null || rb.linearVelocity.magnitude < 0.5f || Time.time < proximaGota) continue;
                proximaGota = Time.time + 0.25f;
                AguaOndas.Adicionar(new Vector3(rb.position.x, Superficie, rb.position.z), 0.25f);
            }
        }

        void Respingo(Vector3 pos, float forca)
        {
            AguaOndas.Adicionar(pos, forca);
            CatalogoVFX.Tocar("agua_splash", pos, Quaternion.identity, null, Mathf.Clamp(forca, 0.5f, 2.5f), 2.5f);
            SomJogo.Tocar("Agua_Splash", pos, Mathf.Clamp(0.4f + forca * 0.3f, 0.4f, 1f), Mathf.Lerp(1.3f, 0.85f, Mathf.Clamp01(forca / 2f)), 0.05f);
            SomJogo.Tocar("Agua_Impacto", pos, Mathf.Clamp(0.2f + forca * 0.25f, 0.2f, 0.8f), 1f, 0.05f, duracaoMax: 1.4f);
        }
    }
}
