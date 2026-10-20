using UnityEngine;

namespace FaithClash.Combat
{
    /// <summary>Componente auxiliar: anima subida/descida, escala, transparência e luz e se destrói.</summary>
    public class EfeitoTemporario : MonoBehaviour
    {
        float vida, t;
        Light luz;
        float luzInicial;
        Vector3 escalaInicial, escalaFinal;
        bool animarEscala;
        float subida;      // altura que sobe do chão no começo e desce no fim
        Vector3 posBase;
        Renderer rend;
        Color corInicial;
        bool desbotar;

        public void Configurar(float duracao, Light l = null)
        {
            vida = duracao;
            luz = l;
            if (luz) luzInicial = luz.intensity;
        }

        public void Escala(Vector3 de, Vector3 ate)
        {
            escalaInicial = de; escalaFinal = ate; animarEscala = true;
            transform.localScale = de;
        }

        public void Emergir(float altura)
        {
            subida = altura;
            posBase = transform.position;
            transform.position = posBase - Vector3.up * altura;
        }

        public void Desbotar(Renderer r)
        {
            rend = r;
            corInicial = r.material.GetColor("_BaseColor");
            desbotar = true;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / vida);
            if (luz) luz.intensity = luzInicial * (1f - k);
            if (animarEscala) transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, 1f - (1f - k) * (1f - k));
            if (subida > 0f)
            {
                float sobe = Mathf.Clamp01(t / 0.15f);
                float desce = Mathf.Clamp01((vida - t) / 0.4f);
                transform.position = posBase - Vector3.up * (subida * (1f - Mathf.Min(sobe, desce)));
            }
            if (desbotar)
            {
                var c = corInicial;
                c.a *= 1f - k;
                rend.material.SetColor("_BaseColor", c);
            }
            if (t >= vida) Destroy(gameObject);
        }
    }
}
