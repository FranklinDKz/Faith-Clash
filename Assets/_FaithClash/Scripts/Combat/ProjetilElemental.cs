using System.Collections.Generic;
using UnityEngine;
using FaithClash.Core;
using FaithClash.Training;

namespace FaithClash.Combat
{
    /// <summary>Projétil das técnicas (bola de fogo, dragão de água, Roxo...).</summary>
    public class ProjetilElemental : MonoBehaviour
    {
        public struct Opcoes
        {
            public Color cor;
            public float velocidade, duracao, dano, empurrao, tamanho, raioImpacto;
            public bool serpentear, explodir, puxar;
            public int particulas;
        }

        EstilosElementais dono;
        Opcoes o;
        float t;
        ParticleSystem rastro;
        Light luz;
        Vector3 dirBase;
        readonly HashSet<Health> atingidos = new HashSet<Health>();

        public void Configurar(EstilosElementais d, Opcoes opcoes)
        {
            dono = d; o = opcoes;
            dirBase = transform.forward;
            rastro = Efeitos.Rajada(transform.position, transform.rotation, o.cor, 1, Vector2.zero,
                new Vector2(0.35f, 0.6f) * o.tamanho, new Vector2(0.3f, 0.6f));
            rastro.transform.SetParent(transform, false);
            rastro.transform.localPosition = Vector3.zero;
            var main = rastro.main;
            main.loop = true;
            main.duration = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f * o.tamanho, 0.7f * o.tamanho);
            var em = rastro.emission;
            em.rateOverTime = o.particulas > 0 ? o.particulas : 120f;
            var sh = rastro.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.35f * o.tamanho;
            rastro.Play();
            var lgo = new GameObject("Luz");
            lgo.transform.SetParent(transform, false);
            luz = lgo.AddComponent<Light>();
            luz.color = o.cor;
            luz.range = 6f * o.tamanho;
            luz.intensity = 4f;
            Destroy(rastro.gameObject, o.duracao + 1f);
        }

        void Update()
        {
            t += Time.deltaTime;
            Vector3 dir = dirBase;
            if (o.serpentear)
            {
                Vector3 lado = Vector3.Cross(Vector3.up, dirBase);
                dir = (dirBase + lado * (Mathf.Cos(t * 9f) * 0.55f)).normalized;
            }
            transform.position += dir * (o.velocidade * Time.deltaTime);

            foreach (var col in Physics.OverlapSphere(transform.position, 0.6f * o.tamanho + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (dono != null && col.transform.IsChildOf(dono.transform)) continue;
                var h = col.GetComponentInParent<Health>();
                if (h == null || h.IsDead || atingidos.Contains(h)) continue;
                Impacto(h, col);
                if (o.explodir) { Encerrar(); return; }
            }
            if (t >= o.duracao)
            {
                if (o.explodir) Explodir(transform.position);
                Encerrar();
            }
        }

        void Impacto(Health h, Collider col)
        {
            atingidos.Add(h);
            if (o.explodir)
            {
                Explodir(transform.position);
                return;
            }
            h.TakeDamage(o.dano, col.ClosestPoint(transform.position), dono != null ? dono.gameObject : null);
            var boneco = col.GetComponentInParent<TrainingDummy>();
            if (boneco != null)
                boneco.Empurrar(o.puxar ? (transform.position - boneco.transform.position).normalized * o.empurrao : dirBase * o.empurrao);
            dono?.Combate?.RegisterComboHit();
            Efeitos.Rajada(transform.position, Quaternion.identity, o.cor, 40, new Vector2(2, 6), new Vector2(0.08f, 0.2f), new Vector2(0.3f, 0.6f), raio: 0.4f, gravidade: 0.8f);
        }

        void Explodir(Vector3 pos)
        {
            float raio = o.raioImpacto > 0f ? o.raioImpacto : 3f;
            Efeitos.Rajada(pos, Quaternion.identity, o.cor, 150, new Vector2(4, 12), new Vector2(0.3f, 0.8f) * o.tamanho, new Vector2(0.4f, 0.9f), raio: 0.5f, gravidade: -0.3f);
            Efeitos.Clarao(pos, o.cor, 12f, 14f, 0.6f);
            foreach (var col in Physics.OverlapSphere(pos, raio, ~0, QueryTriggerInteraction.Ignore))
            {
                if (dono != null && col.transform.IsChildOf(dono.transform)) continue;
                var h = col.GetComponentInParent<Health>();
                if (h == null || h.IsDead) continue;
                h.TakeDamage(o.dano, col.ClosestPoint(pos), dono != null ? dono.gameObject : null);
                var boneco = col.GetComponentInParent<TrainingDummy>();
                if (boneco != null) boneco.Empurrar((boneco.transform.position - pos).normalized * o.empurrao);
                dono?.Combate?.RegisterComboHit();
            }
        }

        void Encerrar()
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
