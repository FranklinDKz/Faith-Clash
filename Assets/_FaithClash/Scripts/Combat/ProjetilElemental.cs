using System.Collections.Generic;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>Projétil das técnicas elementais (Rajada de Água).</summary>
    public class ProjetilElemental : MonoBehaviour
    {
        EstilosElementais dono;
        Color cor;
        float velocidade, vida, dano, empurrao, t;
        ParticleSystem rastro;
        readonly HashSet<Health> atingidos = new HashSet<Health>();

        public void Configurar(EstilosElementais d, Color c, float vel, float duracao, float dn, float emp)
        {
            dono = d; cor = c; velocidade = vel; vida = duracao; dano = dn; empurrao = emp;
            rastro = Efeitos.Rajada(transform.position, transform.rotation, cor, 1, Vector2.zero, new Vector2(0.4f, 0.7f), new Vector2(0.3f, 0.5f));
            rastro.transform.SetParent(transform, false);
            rastro.transform.localPosition = Vector3.zero;
            var main = rastro.main;
            main.loop = true;
            main.duration = 1f;
            var em = rastro.emission;
            em.rateOverTime = 120f;
            var sh = rastro.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.5f;
            rastro.Play();
            Destroy(rastro.gameObject, duracao + 1f);
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += transform.forward * (velocidade * Time.deltaTime);
            foreach (var col in Physics.OverlapSphere(transform.position, 1.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (dono != null && col.transform.IsChildOf(dono.transform)) continue;
                var h = col.GetComponentInParent<Health>();
                if (h == null || h.IsDead || !atingidos.Add(h)) continue;
                h.TakeDamage(dano, col.ClosestPoint(transform.position), dono != null ? dono.gameObject : null);
                col.GetComponentInParent<TrainingDummy>()?.Empurrar(transform.forward * empurrao);
                if (dono != null) dono.Combate?.RegisterComboHit();
                Efeitos.Rajada(transform.position, Quaternion.identity, cor, 40, new Vector2(2, 6), new Vector2(0.08f, 0.2f), new Vector2(0.3f, 0.6f), raio: 0.4f, gravidade: 0.8f);
            }
            if (t >= vida)
            {
                if (rastro != null)
                {
                    rastro.transform.SetParent(null);
                    rastro.Stop();
                }
                Destroy(gameObject);
            }
        }
    }
}
